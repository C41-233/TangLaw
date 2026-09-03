# -*- coding: utf-8 -*-
"""
reorder_zhuangzhi.py — 转置表(斗殴/故殴之一~之六、斗杀/故杀)客体行重排 + 校验(幂等, 可复用)

转置表是 x-table(行=客体, 列=后果×手段档)。本脚本只按既定目标顺序重排"客体数据行",
不改任何刑罚值; 保留原换行风格(LF/CRLF)、表头行、非数据行与 "<x-table>" 标签。

目标顺序(用户既定, 见 SKILL.md 排序规则):
  凡人 > 夫妻媵妾(夫殴X > 妻殴X > 媵殴X > 妾殴X) > 特定亲属(祖父母父母 > 从父兄姊 > 从父弟妹)
  > 通用亲属(大功 > 小功 > 缌麻; 每级 尊属 > 兄姊 > 卑幼)

用法:
  python reorder_zhuangzhi.py [--write] [--only <文件子串>] [<目标目录>]
    --write 才写回; 默认 dry-run 只打印差异(仅 changed=1 时打印 OLD/NEW)。
  默认目录 = src/释义/量刑表/殴打。
"""
import os, re, sys

# 目标客体顺序(TARGET_ORDER)
TARGET = ["凡人","夫殴妻","夫殴媵妾","妻殴夫","妻殴媵妾","媵妾殴夫","媵殴妻","妾殴妻","妾殴媵","祖父母、父母","从父兄姊","从父弟妹","大功卑幼","小功尊属","小功兄姊","小功卑幼","缌麻尊属","缌麻兄姊","缌麻卑幼"]
IDX = {n:i for i,n in enumerate(TARGET)}

def first_field(line):
    c = line.split("\t", 1)
    return c[0].strip() if c else ""

def is_data(line):
    return first_field(line) in IDX

def target_files(root):
    fs = []
    for base, _, fns in os.walk(root):
        for fn in sorted(fns):
            if fn.endswith(".xml") and (re.match(r"^(斗殴|故殴)量刑表之[一二三四五六]\.xml$", fn) or fn in ("斗杀量刑表.xml", "故杀量刑表.xml")):
                fs.append(os.path.join(base, fn))
    return fs

def reorder_block_lines(lines):
    """对每个 <x-table> 块, 把客体数据行按 TARGET 顺序重排; 其余(表头/标签/空行)原样保留。"""
    out = []; i = 0; n = len(lines); changed = 0
    while i < n:
        if lines[i].strip() == "<x-table>":
            j = i + 1
            while j < n and lines[j].strip() != "</x-table>":
                j += 1
            block = lines[i:j + 1]; inner = block[1:-1]
            data = [(k, ln) for k, ln in enumerate(inner) if is_data(ln)]
            if data:
                sorted_data = sorted(data, key=lambda kv: IDX[first_field(kv[1])])
                old = [first_field(ln) for _, ln in data]
                new = [first_field(ln) for _, ln in sorted_data]
                if old != new:
                    changed += 1
                kmin = min(k for k, _ in data); kmax = max(k for k, _ in data)
                if all(is_data(inner[k]) for k in range(kmin, kmax + 1)):
                    new_inner = inner[:kmin] + [ln for _, ln in sorted_data] + inner[kmax + 1:]
                else:
                    new_inner = list(inner); slots = [k for k, _ in data]
                    for slot, (_, nl) in zip(slots, sorted_data):
                        new_inner[slot] = nl
                out.extend([block[0]] + new_inner + [block[-1]]); i = j + 1; continue
        out.append(lines[i]); i += 1
    return out, changed

def names(lines):
    return [first_field(ln) for ln in lines if is_data(ln)]

def process_file(path, write):
    with open(path, "rb") as f:
        raw = f.read()
    text = raw.decode("utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(nl)
    new_lines, changed = reorder_block_lines(lines)
    res = nl.join(new_lines)
    if write and changed:
        with open(path, "wb") as f:
            f.write(res.encode("utf-8"))
    return changed, names(lines), names(new_lines), res

def main():
    argv = sys.argv[1:]
    write = "--write" in argv
    only = ""
    target_dir = "src/释义/量刑表/殴打"
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--only":
            if i + 1 < len(argv):
                only = argv[i + 1]; i += 1
        elif a == "--write":
            pass
        elif not a.startswith("--") and os.path.isdir(a):
            target_dir = a
        i += 1
    for p in sorted(target_files(target_dir)):
        if only and only not in os.path.basename(p):
            continue
        try:
            changed, old, new, res = process_file(p, write)
            rel = os.path.relpath(p, target_dir).replace("\\", "/")
            if changed:
                print("=== %s  changed=1" % rel)
                if not write:
                    print("  OLD: %s" % " > ".join(old))
                    print("  NEW: %s" % " > ".join(new))
            else:
                print("=== %s  顺序已符合目标(无需改)" % rel)
            if write:
                print("  WROTE" if changed else "  (no change)")
        except Exception as e:
            print("ERR %s %s" % (os.path.basename(p), e))

if __name__ == "__main__":
    main()