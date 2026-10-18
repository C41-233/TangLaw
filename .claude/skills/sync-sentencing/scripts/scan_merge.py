#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""scan_merge.py — 按 config/情节合并配置.txt 扫描投影中尚可合并的行。

只读，不修改任何文件。用于回答"按现行合并口径，目前是否还有可合并内容"，
也是合并口径变更后的全库复核手段（输出"未发现可合并内容"即合规）。

扫描范围：src/附录/4-量刑表、src/附录/5-尊卑相犯。
（3-罪刑表为简表/word 引用，无后果行；src/释义/量刑表 是基准，不参与折叠。）

A => B 生效前提（与配置文件说明一致）：
  1. A、B 同罪行、同主体、同法条；
  2. 4-量刑表 同文件即同刑罚级；5-尊卑相犯 要求法定刑相同；
  3. B 行实际存在，且 B 行手段集覆盖 A 行手段集（手段前缀不变）；
  4. "用汤、火伤人" 行名按原子代表行处理，不拆分。

用法：
  python scan_merge.py [--root <仓库根>]
  默认仓库根由脚本位置推导（.claude 与 .zcode 两种路径均可）。
"""
import argparse
import re
import sys
from pathlib import Path

MEANS_ALL = ["用手足", "用器物", "用汤、火", "用刃", "用兵刃"]
MEAN_SET = {
    "用手足": {"手足"},
    "用器物": {"器物"},
    "用汤、火": {"汤火"},
    "用刃": {"刃", "兵刃"},
    "用兵刃": {"兵刃"},
}
ZUI_NORM = {
    "斗殴": "殴打", "故殴": "殴打", "殴打": "殴打",
    "过失伤": "过失杀伤", "过失杀": "过失杀伤", "过失杀伤": "过失杀伤",
}


def load_pairs(config_path):
    pairs = {}
    in_merge = False
    for raw in config_path.read_text(encoding="utf-8-sig").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("[") and line.endswith("]"):
            in_merge = line == "[合并]"
            continue
        if not in_merge or "=>" not in line:
            continue
        a, b = [x.strip() for x in line.split("=>", 1)]
        if a and b:
            pairs[a] = b
    return pairs


def norm(token):
    """去掉后果前缀「致」（保留「致死」）；用于对齐配置名与表中缺省前缀的写法。"""
    t = token.strip()
    if t.startswith("致") and t != "致死":
        return t[1:]
    return t


def norm_components(name):
    return [norm(x) for x in name.split("、") if x.strip()]


def parse_situation(text):
    """情形列 -> (罪名标记, 主体, [(原后果名, 手段集)])；无法识别返回 None。"""
    t = text.strip()
    m0 = re.match(r"^（(斗殴|故殴)）", t)
    mark = m0.group(1) if m0 else ""
    if m0:
        t = t[m0.end():].strip()
    if "（" in t:
        subject = t.split("（")[0].strip()
        groups = re.findall(r"（([^（）]*)）", t)
        inner = groups[-1] if groups else ""
        tail = t.rsplit("）", 1)[-1].strip()
        body = (inner + tail).strip()
    else:
        subject = ""
        body = t
    if not body:
        return None
    if body.startswith("用汤、火伤人"):
        return mark, subject, [("用汤、火伤人", set(MEAN_SET["用汤、火"]))]
    m = re.match(r"用(手足|器物|汤、火|刃|兵刃)", body)
    if m:
        means = set(MEAN_SET["用" + m.group(1)])
        rest = body[m.end():].strip()
    else:
        means = set().union(*MEAN_SET.values())
        rest = body
    tokens = [x.strip() for x in rest.split("、") if x.strip()]
    if not tokens:
        return None
    return mark, subject, [(x, set(means)) for x in tokens]


def cells(line):
    return [c.strip() for c in line.split("\t") if c.strip()]


def xblocks(lines):
    out, start = [], None
    for i, l in enumerate(lines):
        if l.strip().startswith("<x-table"):
            start = i
        elif l.strip() == "</x-table>" and start is not None:
            out.append((start, i))
            start = None
    return out


def scan_file(path, rel, pairs, is_l4, findings):
    lines = [l.rstrip("\r\n") for l in path.read_text(encoding="utf-8-sig").splitlines()]
    level = path.stem if is_l4 else ""
    for bi, (s, e) in enumerate(xblocks(lines)):
        groups = {}
        for idx, l in enumerate(lines[s + 1:e]):
            cs = cells(l)
            if is_l4:
                if len(cs) < 4:
                    continue
                zui, situation, law = cs[0], cs[1], cs[-1]
                penalty = level
            else:
                if len(cs) < 5:
                    continue
                zui, situation, penalty, law = cs[0], cs[1], cs[-3], cs[-1]
            if not situation or situation in ("*", "-"):
                continue
            parsed = parse_situation(situation)
            if not parsed:
                continue
            mark, subject, conseqs = parsed
            key = (bi, ZUI_NORM.get(zui, zui), mark, subject, law, penalty)
            groups.setdefault(key, []).append((idx, situation, conseqs))

        for key, rows in groups.items():
            for idx, situation, conseqs in rows:
                for name, means in conseqs:
                    b = pairs.get(name)
                    if not b:
                        continue
                    b_parts = norm_components(b)
                    for idx2, situation2, conseqs2 in rows:
                        if idx2 == idx:
                            continue
                        names2 = {norm(n) for n, _ in conseqs2}
                        if not all(p in names2 for p in b_parts):
                            continue
                        means2 = set().union(*[m for _, m in conseqs2])
                        if not means.issubset(means2):
                            continue
                        where = f"4-量刑表[{level}]" if is_l4 else f"5-尊卑相犯[{rel}]"
                        pval = key[5]
                        findings.add(
                            f"{where} {key[1]}/{key[3]}/{key[4]}："
                            f"「{situation}（{pval}）」可并入「{situation2}（{pval}）」"
                        )


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=None)
    args = ap.parse_args()

    if args.root:
        root = Path(args.root)
    else:
        # .claude/skills/sync-sentencing/scripts -> repo；.zcode 链接路径同样成立
        root = Path(__file__).resolve().parents[4]
    config = Path(__file__).resolve().parent.parent / "config" / "情节合并配置.txt"
    if not config.exists():
        sys.exit(f"配置文件不存在：{config}")
    pairs = load_pairs(config)
    if not pairs:
        sys.exit(f"配置中未读到 [合并] 规则：{config}")

    findings = set()
    l4 = root / "src" / "附录" / "4-量刑表"
    for path in sorted(l4.rglob("*.xml")):
        scan_file(path, str(path.relative_to(root)), pairs, True, findings)

    l5 = root / "src" / "附录" / "5-尊卑相犯"
    for path in sorted(l5.rglob("*.xml")):
        scan_file(path, str(path.relative_to(root)), pairs, False, findings)

    if findings:
        print(f"发现 {len(findings)} 处可合并：")
        for f in sorted(findings):
            print(" ", f)
    else:
        print("未发现可合并内容")


if __name__ == "__main__":
    main()
