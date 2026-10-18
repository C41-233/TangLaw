#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""verify_jiandeng.py — 校验派生量刑表 = 源量刑表 逐格减 N 等。

适用"从某主体殴打表复制派生、全表减 N 等"的场景（如 328.2 过失杀伤期亲尊长
「各减殴杀伤二等」派生 过失杀伤量刑表（兄姊））。

校验内容：
- 结构：目标表行名（含 \n 合并）与顺序必须与源表完全一致，仅允许缺失 --drop 列出的行；
- 合并无损：目标表每个 \n 合并行，其组内各行在源表中的值必须完全相同（否则不可合并）；
- 数值：目标格 = 源格减 N 等。× 与源表 × 对应；源值尾注（如 斩（以故杀论））剥离后
  按核心刑等减等比对（派生表按惯例不带注记）；
- 减等阶梯：五刑二十等；二死、三流各同为一减（绞/斩→流三千里，流*→徒三年）；
  徒三年→徒二年半（勿写成徒二年）；减无可减时报错。

用法：
  python verify_jiandeng.py <源表.xml> <目标表.xml> [--n 2] [--drop 未伤,斫射]
  全部通过输出 OK（行/格统计）并以 0 退出；否则逐条列出 FAIL 并以 1 退出。只读不改。
"""
import argparse
import re
import sys

LADDER = ["笞一十", "笞二十", "笞三十", "笞四十", "笞五十",
          "杖六十", "杖七十", "杖八十", "杖九十", "杖一百",
          "徒一年", "徒一年半", "徒二年", "徒二年半", "徒三年",
          "流二千里", "流二千五百里", "流三千里", "绞", "斩"]


def reduce1(v):
    # 二死同为一减、三流同为一减（《名例律·称加减》）
    if v in ("绞", "斩"):
        return "流三千里"
    if v.startswith("流"):
        return "徒三年"
    i = LADDER.index(v)
    if i == 0:
        raise ValueError("笞一十 已是最轻，减无可减")
    return LADDER[i - 1]


def reduce_n(v, n):
    for _ in range(n):
        v = reduce1(v)
    return v


def parse(path):
    rows = []  # [(行名含\n, [各列值])]
    with open(path, encoding="utf-8-sig") as f:
        for line in f:
            line = line.rstrip("\r\n")
            if not line or line.startswith("<x-table") or line.strip() == "</x-table>":
                continue
            cells = [c.strip() for c in line.split("\t") if c.strip()]
            if not cells or cells[0] == "后果":
                continue
            rows.append((cells[0], cells[1:]))
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("source")
    ap.add_argument("target")
    ap.add_argument("--n", type=int, default=2, help="减等数（默认 2）")
    ap.add_argument("--drop", default="", help="目标表相对源表删除的行名，逗号分隔")
    args = ap.parse_args()

    src = parse(args.source)
    dst = parse(args.target)
    dropped = [s for s in args.drop.split(",") if s]

    errors = []

    want = [name for name, _ in src if name not in dropped]
    got = [name for name, _ in dst]
    if got != want:
        for name in want:
            if name not in got:
                errors.append(f"目标缺失行：{name}")
        for name in got:
            if name not in want:
                errors.append(f"目标多余行：{name}")
        if set(got) == set(want):
            errors.append("行序与源表不一致")

    srcmap = {}
    for name, vals in src:
        for part in re.split(r"\\n", name):
            if part in srcmap and srcmap[part] != vals:
                errors.append(f"源表行名重复且值不同：{part}")
            srcmap[part] = vals

    checked = 0
    for name, vals in dst:
        parts = re.split(r"\\n", name)
        group = [srcmap.get(p) for p in parts]
        if any(g is None for g in group):
            continue  # 行名缺失已单独报过
        if any(g != group[0] for g in group):
            errors.append(f"[{name}]：\\n 组内各行源值不同，不可合并")
            continue
        s = group[0]
        if len(vals) != len(s):
            errors.append(f"[{name}]：列数不符（目标 {len(vals)}，源 {len(s)}）")
            continue
        for i, (a, b) in enumerate(zip(s, vals)):
            core = a.split("（")[0]  # 剥离尾注（如 以故杀论）
            if core == "×":
                expect = "×"
            else:
                try:
                    expect = reduce_n(core, args.n)
                except ValueError as e:
                    errors.append(f"[{name}] 第{i + 1}列：{e}")
                    continue
            if b != expect:
                errors.append(f"[{name}] 第{i + 1}列：源={a} 减{args.n}等应={expect} 实际={b}")
            checked += 1

    if errors:
        print("FAIL")
        for e in errors:
            print(" ", e)
        sys.exit(1)
    print(f"OK：{len(dst)} 行 {checked} 格全部通过「源表减{args.n}等」校验"
          + (f"（已删行：{'、'.join(dropped)}）" if dropped else ""))


if __name__ == "__main__":
    main()
