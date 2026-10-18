# -*- coding: utf-8 -*-
"""
restore_head.py — 把误改的同量表文件还原为 HEAD 精确字节(只读 git show, 安全)

用法:
  python restore_head.py <相对路径或目录> [--only <文件名子串>]

重要教训(Windows): os.path.join() 生成反斜杠路径, git show HEAD:<path> 会因反斜杠
找不到路径而返回空(据此"恢复"会误清空文件)。必须 path.replace(os.sep,'/') 转正斜杠再传 git。

只读命令: 仅使用 git show(只读), 不触碰 checkout/reset 等副作用 git 命令。
"""
import subprocess, os, sys

def git_show_head(rel_path):
    rel = rel_path.replace(os.sep, '/')
    r = subprocess.run(['git', 'show', 'HEAD:' + rel], capture_output=True)
    if r.returncode != 0:
        return None
    return r.stdout

def restore(path):
    rel = os.path.relpath(path, os.getcwd()).replace(os.sep, '/')
    d = git_show_head(rel)
    if d is None:
        print('SKIP(git无该路径): %s' % path)
        return False
    with open(path, 'wb') as f:
        f.write(d)
    return True

def main():
    argv = sys.argv[1:]
    only = ''
    targets = []
    for a in argv:
        if a == '--only':
            continue
        elif a.startswith('--'):
            continue
        elif os.path.isdir(a):
            targets.append(a)
        elif os.path.isfile(a):
            targets.append(a)
        else:
            only = a
    if not targets:
        targets = ['src/附录/4-量刑表']
    cnt = 0
    for t in targets:
        if os.path.isdir(t):
            for root, _, fs in os.walk(t):
                for fn in sorted(fs):
                    if not fn.endswith('.xml'):
                        continue
                    if only and only not in fn:
                        continue
                    if restore(os.path.join(root, fn)):
                        cnt += 1
        else:
            if restore(t):
                cnt += 1
    print('restored %d file(s)' % cnt)

if __name__ == '__main__':
    main()
