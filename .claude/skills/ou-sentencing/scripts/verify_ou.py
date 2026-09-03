# -*- coding: utf-8 -*-
"""
verify_ou.py — 校验唐律量刑表"殴打"块重排是否合规(幂等, 只读)

用法:
  python verify_ou.py [--dir <目录>] [--only <文件名子串>]

校验规则:
  1) 组内标记顺序: 斗殴(0) → 故殴(1) → 无标记(2)  非降
  2) 同标记组内主体大类: 凡人(0) → 夫妻(1) → 特定亲属(2) → 通用亲属(3)  非降
  3) 无缺列行(拆分后列数 < 2)
"""
import re, os, sys

BASE = 'src/附录/4-量刑表'

def cat(bare):
    if bare == '凡人':
        return 0
    if bare in ('夫犯妻','夫犯媵妾','妻犯夫','妻犯媵妾','媵妾犯夫','媵犯妻','媵妾犯妻',
                '妾犯妻','妾犯媵','妻媵妾犯夫'):
        return 1
    if bare in ('祖父母、父母','从父弟妹'):
        return 2
    if re.match(r'^(大功|小功|缌麻|期亲)', bare):
        return 3
    return 9

def bare_of(body):
    b = re.sub(r'^（斗殴）|^（故殴）', '', body).strip()
    p = b.find('（')
    if p >= 0:
        b = b[:p].strip()
    return b

def mark_of(body):
    m = re.match(r'^（(斗殴|故殴)）', body)
    return m.group(1) if m else ''

def check_file(path):
    with open(path, encoding='utf-8') as f:
        lines = f.read().split('\n')
    probs = []
    i, n = 0, len(lines)
    while i < n:
        if re.match(r'^殴打\t', lines[i]):
            j = i
            while j < n and re.match(r'^殴打\t', lines[j]):
                j += 1
            lastmk, lastcat = -1, 9
            for ln in lines[i:j]:
                cols = ln.split('\t')
                if len(cols) < 2:
                    probs.append('缺列: %s' % ln)
                    continue
                bare = bare_of(cols[1]); mark = mark_of(cols[1])
                mk = {'斗殴': 0, '故殴': 1, '': 2}.get(mark, 2)
                c = cat(bare)
                if mk < lastmk:
                    probs.append('mark乱序 body=%s mark=%s(前%din=%d)' % (cols[1], mark, lastmk, mk))
                if mk == lastmk and c < lastcat:
                    probs.append('大类乱序 body=%s cat=%d(前%d)' % (cols[1], c, lastcat))
                lastmk, lastcat = mk, c
            i = j
        else:
            i += 1
    return probs

def main():
    argv = sys.argv[1:]
    target = BASE
    only = ''
    for a in argv:
        if a == '--dir':
            continue
        elif os.path.isdir(a):
            target = a
        elif a and not a.startswith('--'):
            only = a
    probs = []
    for root, _, fs in os.walk(target):
        for fn in sorted(fs):
            if not fn.endswith('.xml'):
                continue
            if only and only not in fn:
                continue
            p = os.path.join(root, fn)
            probs += ['%s: %s' % (fn, e) for e in check_file(p)]
    if probs:
        print('发现问题 %d 条:' % len(probs))
        for s in probs:
            print(' -', s)
    else:
        print('校验通过：%s 全部文件排序规则正确。' % target)

if __name__ == '__main__':
    main()
