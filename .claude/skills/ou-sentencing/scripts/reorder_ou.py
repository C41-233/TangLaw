# -*- coding: utf-8 -*-
"""
reorder_ou.py — 唐律量刑表"殴打"块 重排 + 词形归一 + 同刑合并 + 去重（幂等可复用）

用法:
  python reorder_ou.py --dir <绝对/相对路径到目录> [--dry-run] [--remove-dup]
                         [--only <文件名子串>] [--rules 参见下方 RULES]

说明:
  - 默认仅打印重排后结果(含归一/合并)，不改文件；--write 才写回。
  - 只改写以 "^殴打\t" 开头、且连续成块的"殴打"行；其余字节(含末尾换行、非殴打行)原样保留。
  - 幂等: 对已排过的文件再跑，结果稳定(已归一词形不再变、无重复可去)。
  - Windows 中文路径下 git/show 含中文时，本脚本不用 show，直接读工作区文件。
"""
import re, os, subprocess, sys


# ===== 主体词形归一映射 (仅同类/可并时用, 见 process_file 的 presence 判断) =====
RULES = {
    '夫犯媵': '夫犯媵妾', '夫犯妾': '夫犯媵妾',
    '妻犯媵': '妻犯媵妾', '妻犯妾': '妻犯媵妾',
    '媵、妾犯夫': '媵妾犯夫',
    '媵犯妻': '媵妾犯妻', '妾犯妻': '媵妾犯妻',
}

# ===== 主体排序权重 (大类|级别|尊卑) =====
# 大类: 0=凡人 1=夫妻 2=特定亲属 3=通用亲属
def sortkey(bare):
    t = {'凡人': '0|0|0',
         '夫犯妻': '1|1|0', '夫犯媵妾': '1|2|0', '妻犯夫': '1|3|0',
         '妻媵妾犯夫': '1|4|0', '妻犯媵妾': '1|5|0', '媵妾犯夫': '1|6|0',
         '媵犯妻': '1|7|0', '媵妾犯妻': '1|6|0', '妾犯妻': '1|7|0',
         '妾犯媵': '1|8|0',
         '祖父母、父母': '2|1|0', '从父弟妹': '2|2|0',
         '大功兄姊': '3|1|0', '大功卑幼': '3|1|9',
         '小功尊属': '3|2|0', '小功兄姊': '3|2|1', '小功卑幼': '3|2|9',
         '缌麻尊属': '3|3|0', '缌麻兄姊': '3|3|1', '缌麻卑幼': '3|3|9',
         '大功尊长': '3|1|0', '小功尊长': '3|2|0', '缌麻尊长': '3|3|0'}
    return t.get(bare, '9|9|9')


mark_re = re.compile(r'^（(斗殴|故殴)）')


def mark_of(body):
    m = mark_re.match(body)
    return m.group(1) if m else ''


def bare_of(body):
    b = re.sub(r'^（斗殴）|^（故殴）', '', body).strip()
    p = b.find('（')
    if p >= 0:
        b = b[:p].strip()
    return b


def norm_line(line, lrules):
    cols = line.split('\t')
    body = cols[1]
    sxf = cols[2] if len(cols) >= 3 else ''
    ft = cols[3] if len(cols) >= 4 else ''
    bare = bare_of(body)
    mark = mark_of(body)
    b2 = re.sub(r'^（斗殴）|^（故殴）', '', body).strip()
    p = b2.find('（')
    con = b2[p:] if p >= 0 else ''
    norm = lrules.get(bare, bare)
    newbody = ('（%s）%s%s' % (mark, norm, con)) if mark else (norm + con)
    return '\t'.join(['殴打', newbody, sxf, ft])


LV_RE = re.compile(r'^（?(大功|小功|缌麻|期亲)(尊长|尊属|兄姊|卑幼)?(.*)$')


def merge_zunzhang(block):
    """逐档尊长合并：级别 尊属 与 兄姊 的**共同后果档**合并为 尊长；独有档保留。
    仅在 无标记(空mark) 行上做；有 (斗殴)/(故殴) 标记的行不参与(标记组内尊属/兄姊各自独立)。"""
    # 只处理空mark行（尊长/尊属/兄姊 通常出现在无标记组）
    def is_plain(l):
        cols = l.split('\t')
        return len(cols) >= 2 and not re.match(r'^（(斗殴|故殴)）', cols[1])

    plain_idx = [i for i, l in enumerate(block) if is_plain(l)]
    # 收集每级别的 尊属/兄姊 共同后果
    zun = {}; xiong = {}
    for i in plain_idx:
        cols = block[i].split('\t')
        body = cols[1]
        m = re.match(r'^((大功|小功|缌麻))(尊属|兄姊)(.*)$', body)
        if m and m.group(3) in ('尊属', '兄姊'):
            lv = m.group(1); zw = m.group(3); con = m.group(4)
            if zw == '尊属':
                zun.setdefault(lv, {}).setdefault(con, i)
            else:
                xiong.setdefault(lv, {}).setdefault(con, i)
    # 找出共同档，需要重排(把共同档的尊属/兄姊 合并为尊长)
    out = []
    common_seen = {}
    for i, l in enumerate(block):
        cols = l.split('\t')
        if len(cols) >= 2:
            m = re.match(r'^((大功|小功|缌麻))(尊属|兄姊)(.*)$', cols[1])
            if m and m.group(3) in ('尊属', '兄姊') and (i in plain_idx):
                lv = m.group(1); zw = m.group(3); con = m.group(4)
                if con in zun.get(lv, {}) and con in xiong.get(lv, {}):
                    # 共同档：先把首次出现的合并为 尊长，跳过后续
                    key = '%s|%s' % (lv, con)
                    if key in common_seen:
                        continue
                    common_seen[key] = True
                    sxf = cols[2] if len(cols) >= 3 else ''
                    ft = cols[3] if len(cols) >= 4 else ''
                    newbody = '%s尊长%s' % (lv, con)
                    out.append('\t'.join(['殴打', newbody, sxf, ft]))
                    continue
        out.append(l)
    return out


def process_file(path, remove_dup, write):
    with open(path, 'rb') as f:
        raw = f.read()
    text = raw.decode('utf-8')
    # presence 判断：本文件是否同时含 妾犯妻 与 媵犯妻
    bodylist = [bare_of(l.split('\t')[1])
                for l in text.split('\n') if l.startswith('殴打\t')]
    has_q = '妾犯妻' in bodylist
    has_y = '媵犯妻' in bodylist
    local_rules = dict(RULES)
    if has_q and not has_y:
        local_rules.pop('妾犯妻', None)
    elif has_y and not has_q:
        local_rules.pop('媵犯妻', None)
    # 夫犯媵/夫犯妾: HEAD 仅 杖八十 用且齐全, presence 默认保留; 若文件只有其一则 pop
    has_fy = '夫犯媵' in bodylist
    has_fq = '夫犯妾' in bodylist
    if has_fy and not has_fq:
        local_rules.pop('夫犯媵', None)
    elif has_fq and not has_fy:
        local_rules.pop('夫犯妾', None)

    lines = text.split('\n')
    out, i, n = [], 0, len(lines)
    while i < n:
        if re.match(r'^殴打\t', lines[i]):
            block, j = [], i
            while j < n and re.match(r'^殴打\t', lines[j]):
                block.append(lines[j]); j += 1
            block = merge_zunzhang(block)  # 逐档尊长合并（仅共同后果档）
            rows = []
            for idx, ln in enumerate(block):
                cols = ln.split('\t')
                if len(cols) < 2:
                    rows.append({'mark': '', 'sk': '9|9|9', 'full': ln, 'idx': idx})
                    continue
                body = cols[1]
                sk = sortkey(local_rules.get(bare_of(body), bare_of(body)))
                rows.append({'mark': mark_of(body), 'sk': sk,
                             'full': norm_line(ln, local_rules), 'idx': idx})
            markord = {'斗殴': 0, '故殴': 1, '': 2}
            rows.sort(key=lambda r: (markord.get(r['mark'], 2), r['sk'], r['idx']))
            seen = set()
            for r in rows:
                if remove_dup and re.match(r'^殴打\t（故殴）夫犯（', r['full']):
                    continue
                if r['full'] in seen:
                    continue
                seen.add(r['full'])
                out.append(r['full'])
            i = j
        else:
            out.append(lines[i]); i += 1
    res = '\n'.join(out)
    if write:
        with open(path, 'wb') as f:
            f.write(res.encode('utf-8'))
    return res


def main():
    argv = sys.argv[1:]
    write = '--write' in argv
    remove_dup = '--remove-dup' in argv
    only = ''
    target_dir = 'src/附录/4-量刑表'
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--only':
            if i + 1 < len(argv):
                only = argv[i + 1]
                i += 1
        elif not a.startswith('--') and os.path.isdir(a):
            target_dir = a
        i += 1

    for root, _, fs in os.walk(target_dir):
        for fn in sorted(fs):
            if not fn.endswith('.xml'):
                continue
            if only and only not in fn:
                continue
            p = os.path.join(root, fn)
            try:
                res = process_file(p, remove_dup, write)
                if not write:
                    print('=== %s ===' % fn)
                    for ln in res.split('\n'):
                        if ln.startswith('殴打\t'):
                            print(ln)
                    print()
                else:
                    print('WROTE %s' % fn)
            except Exception as e:
                print('ERR %s %s' % (fn, e))


if __name__ == '__main__':
    main()
