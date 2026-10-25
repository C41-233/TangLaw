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


def check_file(path: Path):
    """返回该文件的畸形清单（list[str]）。"""
    issues = []
    text = path.read_text(encoding="utf-8-sig")
    lines = text.split("\n")
    in_block = False
    header_count = None
    header_tokens = None
    header_line = None
    block_count = 0
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
            block_count += 1
            continue
        if "</x-table" in raw:
            if not in_block:
                issues.append(f"{path}:{idx}  多余的 </x-table（无对应开启）")
            in_block = False
            header_count = None
            header_tokens = None
            continue

        if not stripped:
            continue

        if in_block:
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
    return issues, block_count


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
    total_blocks = 0
    for f in files:
        try:
            issues, blocks = check_file(f)
        except UnicodeDecodeError as e:
            all_issues.append(f"{f}  非UTF-8可解码：{e}")
            continue
        total_blocks += blocks
        all_issues.extend(issues)

    if all_issues:
        print("\n".join(all_issues))
        print(f"\n畸形 {len(all_issues)} 处（{len(files)} 文件 / {total_blocks} 表）")
        sys.exit(1)
    print(f"OK {len(files)} 文件 {total_blocks} 表，未发现畸形")
    sys.exit(0)


if __name__ == "__main__":
    main()
