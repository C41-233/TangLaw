#!/usr/bin/env python3
"""check_xtable.py — x-table / 4-量刑表 结构畸形只读检查。

规则见同目录 SKILL.md：
1. 每个 <x-table> 块：数据行有效格数 == 表头有效格数（按块独立）；
2. 表头缺失（块内数据行先于表头）；
3. 数据行首格（行名）为空；
4. <x-table 未闭合；
5. src/附录/4-量刑表/ 下 x-table 块外的含 Tab 数据行必须恰好 4 个有效格。

用法：python check_xtable.py [路径...] [--only <文件名子串>]
退出码：0=无畸形；1=有畸形；2=参数/路径错误。
"""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
DEFAULT_ROOT = REPO / "src"
LIANGXING_DIR = "附录" + chr(92) + "4-量刑表" + chr(92)  # Windows 分隔符；另有正斜杠形态
LIANGXING_ALT = "附录/4-量刑表/"


def is_liangxing(path: Path) -> bool:
    s = str(path)
    return LIANGXING_ALT in s.replace(chr(92), "/") and "4-量刑表" in s


def effective_tokens(line: str):
    return [t for t in line.rstrip("\r\n").split("\t") if t.strip()]


def disp_width(s: str) -> int:
    """显示宽度：×（U+00D7）按 1 格，中文/全角（codepoint>0x7F）按 2 格，其余 ASCII 1 格。
    与 x-table-converter/align_xtable_full.py 一致。"""
    return sum(1 if ch == "×" else (2 if ord(ch) > 0x7F else 1) for ch in s)


def expected_aligned_lines(header, rows):
    """按 align_xtable_full 约定生成对齐后的行（列表[str]，不含换行）：
    每列宽=数据行最大显示宽度；空格补齐；列间单 Tab；末列不补尾；
    表头按 max(列宽, 表头自身宽) 补齐（防粘连）。"""
    ncol = len(header)
    colw = [max(disp_width(r[c]) for r in rows) for c in range(ncol)]
    hw = [max(colw[c], disp_width(header[c])) for c in range(ncol)]
    lines = []
    lines.append("\t".join(
        (header[c] + " " * (hw[c] - disp_width(header[c]))) if c < ncol - 1 else header[-1]
        for c in range(ncol)))
    for r in rows:
        cells = [r[c] + " " * (colw[c] - disp_width(r[c])) for c in range(ncol - 1)]
        cells.append(r[-1])
        lines.append("\t".join(cells))
    return lines


def check_file(path: Path):
    """返回 (该文件的畸形清单, 对齐问题清单, 块数)。"""
    issues = []
    align_issues = []
    text = path.read_text(encoding="utf-8-sig")
    lines = text.split("\n")
    in_block = False
    header_count = None
    header_tokens = None
    header_line = None
    block_count = 0
    block_raw = []  # (lineno, line) 非空行，供对齐校验
    block_uniform = True  # 列数是否一致（不一致时跳过对齐校验）
    lx = is_liangxing(path)

    for idx, raw in enumerate(lines, 1):
        stripped = raw.strip()
        if "<x-table" in raw:
            if in_block:
                issues.append(f"{path}:{idx}  嵌套的 <x-table（上一块未闭合，表头@{header_line}）")
            in_block = True
            header_count = None
            header_tokens = None
            header_line = None
            block_raw = []
            block_uniform = True
            block_count += 1
            continue
        if "</x-table" in raw:
            if not in_block:
                issues.append(f"{path}:{idx}  多余的 </x-table（无对应开启）")
                in_block = False
                header_count = None
                header_tokens = None
                continue
            # 对齐校验（仅列数一致的块；报告级，不影响退出码）
            if block_uniform and header_tokens and len(block_raw) >= 2:
                rows = [effective_tokens(bl) for _, bl in block_raw[1:]]
                if all(len(r) == len(header_tokens) for r in rows):
                    expected = expected_aligned_lines(header_tokens, rows)
                    actual = [bl.rstrip() for _, bl in block_raw]
                    bad = [n for n, (ex, ac) in enumerate(zip(expected, actual), 1) if ex != ac]
                    if bad:
                        align_issues.append(
                            f"{path}:{header_line}  未对齐 {len(bad)}/{len(actual)} 行（首行@{block_raw[0][0]+bad[0]}）"
                        )
            in_block = False
            header_count = None
            header_tokens = None
            continue

        if not stripped:
            continue

        if in_block:
            block_raw.append((idx, stripped))
            toks = effective_tokens(raw)
            if header_count is None:
                header_count = len(toks)
                header_tokens = toks
                header_line = idx
                if not toks:
                    issues.append(f"{path}:{idx}  表头行无有效格")
                continue
            if not toks:
                issues.append(f"{path}:{idx}  数据行无有效格（表头@{header_line} {header_count} 格）")
                continue
            # 行格数多于表头 = generator 会静默丢格（physicalCols 由表头决定），必须报畸形
            if len(toks) != header_count:
                block_uniform = False
                issues.append(
                    f"{path}:{idx}  列数不符 表头{header_count}/行{len(toks)}（表头@{header_line}）  {stripped[:40]}"
                )
        elif lx and "\t" in raw and not stripped.startswith("<"):
            toks = effective_tokens(raw)
            if len(toks) != 4:
                issues.append(
                    f"{path}:{idx}  4-量刑表块外行应为4格/实{len(toks)}  {stripped[:40]}"
                )

    if in_block:
        issues.append(f"{path}  文件结束仍有未闭合的 <x-table（表头@{header_line}）")
    return issues, align_issues, block_count


def main():
    args = sys.argv[1:]
    only = None
    paths = []
    i = 0
    while i < len(args):
        if args[i] == "--only":
            i += 1
            if i >= len(args):
                print("--only 缺少参数", file=sys.stderr)
                sys.exit(2)
            only = args[i]
        else:
            paths.append(args[i])
        i += 1

    roots = [Path(p) for p in paths] if paths else [DEFAULT_ROOT]
    files = []
    for r in roots:
        if not r.exists():
            print(f"路径不存在：{r}", file=sys.stderr)
            sys.exit(2)
        if r.is_file():
            files.append(r)
        else:
            files.extend(sorted(p for p in r.rglob("*.xml") if p.is_file()))
    if only:
        files = [f for f in files if only in f.name]
    if not files:
        print("未找到待检文件", file=sys.stderr)
        sys.exit(2)

    all_issues = []
    all_align = []
    total_blocks = 0
    for f in files:
        try:
            issues, align, blocks = check_file(f)
        except UnicodeDecodeError as e:
            all_issues.append(f"{f}  非UTF-8可解码：{e}")
            continue
        total_blocks += blocks
        all_issues.extend(issues)
        all_align.extend(align)

    if all_issues:
        print("\n".join(all_issues))
        print(f"\n畸形 {len(all_issues)} 处（{len(files)} 文件 / {total_blocks} 表）")
        if all_align:
            print(f"（另有未对齐 {len(all_align)} 表，明细在畸形清零后重跑可见；"
                  "修复用 x-table-converter 的 align_xtable_full.py --write）")
        sys.exit(1)
    if all_align:
        print("\n".join(all_align))
        print(f"\n对齐 {len(files)} 文件 {total_blocks} 表：未对齐 {len(all_align)} 表"
              "（报告级，不影响退出码；修复用 x-table-converter 的 align_xtable_full.py --write）")
        sys.exit(0)
    print(f"OK {len(files)} 文件 {total_blocks} 表，未发现畸形，全部对齐")
    sys.exit(0)


if __name__ == "__main__":
    main()
