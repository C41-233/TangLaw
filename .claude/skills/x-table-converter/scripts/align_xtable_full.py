#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""align_xtable_full.py — 对 x-table 源码做**整表**列宽对齐。

与 align_xtable.py 的区别：align_xtable.py 按"连续同首列罪行"分组、只对齐多行组，
单行组原样；本脚本把每张 <x-table> 的**全部数据行作为一个对齐组**，适用于
量刑表（五行表）这类每行罪名都不同、没有连续同罪行组的表。

对齐规则（与 x-table-converter skill 一致）：
- 每列宽 = 该列全部数据行的最大格数。
- 格数计算：中文/全角（codepoint>0x7F）按 2 格；**×（U+00D7）实际渲染为半角，按 1 格**；
  其余 ASCII 按 1 格。
- 每行：列内容用**空格**补齐到该列最大宽度，列间**一个 Tab** 分隔；末列不补尾。
- 表头各列起点对齐到首个数据行对应列的起点（表头超宽时按自身宽度补齐防粘连）。
- 同文件多张 <x-table> 逐块独立对齐，不跨表。
- 写回前断言对齐前后单元格内容（token）完全一致，仅空白变化；校验各行各列起点一致。

用法：
  python align_xtable_full.py [--write] [--only <子串>] <路径 ...>
  默认 dry-run（只打印差异）；--write 才写回；--only 只处理含子串的文件。
  保留原换行（CRLF/LF）、末尾换行、非 x-table 文本、空行原样。
"""
import argparse
import difflib
import os


def width(s):
    return sum(1 if ch == '×' else (2 if ord(ch) > 0x7F else 1) for ch in s)


def split_cells(line):
    return [p.strip() for p in line.split('\t') if p.strip()]


def align_rows(header, rows):
    """header/rows: list[list[str]]，列数相同 -> (对齐表头, 对齐数据行)。"""
    ncol = len(header)
    colw = [max(width(r[c]) for r in rows) for c in range(ncol)]
    hw = [max(colw[c], width(header[c])) for c in range(ncol)]
    out = []
    out.append('\t'.join(
        header[c] + ' ' * (hw[c] - width(header[c])) if c < ncol - 1 else header[-1]
        for c in range(ncol)))
    for r in rows:
        cells = [r[c] + ' ' * (colw[c] - width(r[c])) for c in range(ncol - 1)]
        cells.append(r[-1])
        out.append('\t'.join(cells))
    return out


def col_starts(line):
    """按宽度规则计算行内各列（含末列）起点格数，用于对齐校验。"""
    offs, pos = [], 0
    cells = line.split('\t')
    for cell in cells:
        offs.append(pos)
        pos += width(cell) + 1  # +1 = 一个 Tab
    return offs


def process_block(block, path):
    """block: <x-table> 到 </x-table> 的行（含两端）。返回对齐后的块；无变化返回 None。"""
    body = [l for l in block[1:-1]]
    rows = []
    header = None
    for l in body:
        cells = split_cells(l)
        if not cells:
            continue
        if header is None:
            header = cells
        else:
            rows.append(cells)
    if header is None or not rows:
        return None
    ncol = len(header)
    for r in rows:
        if len(r) != ncol:
            raise SystemExit(f'{path}: 列数不一致（应为 {ncol}）：{r}')
    aligned = align_rows(header, rows)
    # 重建块：空行原样保留在原位置
    out, k = [], 0
    for l in body:
        if not split_cells(l):
            out.append(l)
        else:
            out.append(aligned[k])
            k += 1
    # 校验：token 一致
    def tokens(lines):
        return [split_cells(l) for l in lines if split_cells(l)]
    if tokens(body) != tokens(out):
        raise SystemExit(f'{path}: 对齐后单元格内容发生变化，中止')
    # 校验：各行列起点一致
    starts = [col_starts(l) for l in out if split_cells(l)]
    if any(s != starts[0] for s in starts):
        raise SystemExit(f'{path}: 对齐后各行列起点不一致，中止')
    if out == body:
        return None
    return [block[0]] + out + [block[-1]]


def find_xblocks(lines):
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
    nl = '\r\n' if '\r\n' in text else '\n'
    trailing_nl = text.endswith(nl)
    lines = text.split(nl)
    if lines and lines[-1] == '':
        lines = lines[:-1]

    blocks = find_xblocks(lines)
    new_lines = list(lines)
    changed = False
    for start, end in reversed(blocks):
        new_block = process_block(new_lines[start:end + 1], path)
        if new_block is not None:
            changed = True
            new_lines = new_lines[:start] + new_block + new_lines[end + 1:]
    if not changed:
        return None
    out = nl.join(new_lines)
    if trailing_nl:
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
