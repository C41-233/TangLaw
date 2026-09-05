#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""align_xtable.py — 对 x-table 源码做组内列宽对齐。

对齐规则（与 x-table-converter skill 一致）：
- 组 = 连续同"首列（罪行）"的多行；空行视作分组边界；单行组不强求对齐。
- 组内每列宽 = 该列内容最大格数（非 ASCII codepoint>0x7F 按 2 格；半角按 1 格；Tab 视 4 格）。
- 组内每行：列内容用**空格**补齐到该列最大宽度，列间**一个 Tab** 分隔；末列（法条）不补尾。
- 仅靠补 Tab 无法对齐（列内容宽度不同），必须空格补齐列宽。

用法：
  python align_xtable.py [--write] [--only <子串>] <路径 ...>
  默认 dry-run（只打印差异）；--write 才写回；--only 只处理含子串的文件。
  保留原换行（CRLF/LF）、末尾换行、非 x-table 文本原样。
"""
import argparse
import os
import difflib


def width(s):
    return sum(2 if ord(ch) > 0x7F else 1 for ch in s)


def split_cells(line):
    # 字面反斜杠 t：列分隔
    return [p.strip() for p in line.split('\t') if len(p) > 0 if p.strip()]


def align_group(rows):
    """rows: list[list[str]] 且列数相同 -> 返回对齐后的行（空格补齐列宽 + Tab 分隔）。"""
    cols = len(rows[0])
    maxw = [max(width(cells[c]) for cells in rows) for c in range(cols)]
    out = []
    for cells in rows:
        parts = []
        for c in range(cols):
            cell = cells[c] if c < len(cells) else ''
            if c < cols - 1:
                pad = maxw[c] - width(cell)
                parts.append(cell + ' ' * max(0, pad))
            else:
                parts.append(cell)
        out.append('\t'.join(parts))
    return out


def process_block(body):
    """body: x-table 内数据行（不含 <x-table>/</x-table>、不含表头）。功能：组内对齐。"""
    if not body:
        return body
    header = body[0]
    rest = body[1:]
    # 分组（连续同首列、非空；空行分隔）
    groups = []          # (start_idx_in_rest, list_of_cells)
    cur = []
    cur_start = None
    for j, ln in enumerate(rest):
        cells = split_cells(ln)
        if not cells:
            if cur:
                groups.append((cur_start, cur))
                cur = []
                cur_start = None
        else:
            if cur and cur[0][0] == cells[0]:
                cur.append(cells)
            else:
                if cur:
                    groups.append((cur_start, cur))
                cur = [cells]
                cur_start = j
    if cur:
        groups.append((cur_start, cur))

    group_map = {g_start: g for g_start, g in groups if len(g) > 1}
    result = [header]
    idx = 0
    n = len(rest)
    while idx < n:
        if idx in group_map:
            result.extend(align_group(group_map[idx]))
            idx += len(group_map[idx])
        else:
            result.append(rest[idx])
            idx += 1
    return result


def find_xblocks(lines):
    """返回含 <x-table> 到 </x-table> 的 (start,end) 行索引列表。"""
    blocks = []
    start = None
    for i, ln in enumerate(lines):
        if ln.strip().startswith('<x-table'):
            start = i
        elif ln.strip() == '</x-table>' and start is not None:
            blocks.append((start, i))
            start = None
    return blocks


def process_file(path):
    with open(path, 'r', encoding='utf-8', newline='') as fo:
        text = fo.read()
    if '\r\n' in text:
        nl = '\r\n'
    else:
        nl = '\n'
    trailing_nl = text.endswith(nl)
    lines = text.split(nl)
    end_empty = lines and lines[-1] == ''
    if end_empty:
        lines = lines[:-1]

    blocks = find_xblocks(lines)
    new_lines = list(lines)
    changed = False
    for start, end in reversed(blocks):
        block = new_lines[start:end + 1]
        body = block[1:-1]
        new_body = process_block(body)
        if new_body != body:
            changed = True
            new_lines = new_lines[:start + 1] + new_body + new_lines[end:]
    if not changed:
        return None
    out = nl.join(new_lines)
    if end_empty and trailing_nl:
        out += nl
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('paths', nargs='+')
    ap.add_argument('--write', action='store_true')
    ap.add_argument('--only', default=None)
    args = ap.parse_args()

    for p in args.paths:
        if os.path.isdir(p):
            files = [os.path.join(r, fn) for r, _, fs in os.walk(p) for fn in fs if fn.endswith('.xml')]
        else:
            files = [p]
        for fp in files:
            if args.only and args.only not in fp:
                continue
            result = process_file(fp)
            if result is None:
                continue
            if args.write:
                with open(fp, 'w', encoding='utf-8', newline='') as fo:
                    fo.write(result)
                print('WROTE:', fp)
            else:
                print('=== DIFF:', fp, '===')
                orig = open(fp, encoding='utf-8').read()
                for line in difflib.unified_diff(orig.splitlines(), result.splitlines(), 'orig', 'new', lineterm=''):
                    print(line)


if __name__ == '__main__':
    main()
