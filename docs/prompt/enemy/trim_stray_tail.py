#!/usr/bin/env python3
"""剔掉敌人图底部的游离残片（切图时带进来的相邻角色的一部分）。

为什么不能用 process_bosses.drop_bleed：那个的判据是「连通块贴着格子边缘」，
而这种残片四周留了 10px 白边、根本不碰边，判不出来。

这里换个判据：**按行投影找最大的空白横带**。真角色（哪怕是三只一组的墨粒）
彼此是挨着或只差几像素的，而残片和主体之间隔着一条很宽的空行带。
只要空带够宽、且下方内容只占少数像素，就把下方整段切掉。

危害不只是多一块脏东西：精灵包围盒被拉高之后，BattleView 按包围盒中心缩放，
主体会被顶到偏上，下面还会飘一个幽灵墨团。

    .venv-mock/bin/python docs/prompt/enemy/trim_stray_tail.py            # 只检查
    .venv-mock/bin/python docs/prompt/enemy/trim_stray_tail.py --apply    # 真改
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
ART = os.path.join(REPO, "Assets", "Resources", "Art")

# 空带至少要占整高这么多，才算「隔开了」，不然会把腿和脚之间的缝当成分界
GAP_MIN = 0.06
# 下方内容占总不透明像素的比例上限。超过就说明那不是残片，是主体的一部分
TAIL_MAX = 0.30


def row_mass(img):
    a = img.split()[3]
    w, h = img.size
    ap = a.load()
    out = []
    for y in range(h):
        n = 0
        for x in range(w):
            if ap[x, y] > 24:
                n += 1
        out.append(n)
    return out


def find_cut(mass):
    """返回切割行号，没有可切的返回 None。"""
    h = len(mass)
    total = sum(mass)
    if total == 0:
        return None
    # 从下往上找最后一条够宽的空带
    runs = []
    y = 0
    while y < h:
        if mass[y] == 0:
            s = y
            while y < h and mass[y] == 0:
                y += 1
            runs.append((s, y))
        else:
            y += 1
    for s, e in reversed(runs):
        if e - s < h * GAP_MIN:
            continue
        tail = sum(mass[e:])
        if tail == 0:
            continue
        if tail <= total * TAIL_MAX:
            return s, e, tail / total
    return None


def main():
    apply = "--apply" in sys.argv
    names = sorted(f[:-4] for f in os.listdir(ART)
                   if f.endswith(".png") and "/" not in f)
    hit = 0
    for name in names:
        p = os.path.join(ART, name + ".png")
        img = Image.open(p).convert("RGBA")
        cut = find_cut(row_mass(img))
        if cut is None:
            continue
        s, e, frac = cut
        hit += 1
        print(f"{name:<14} {img.size} 残片在 y>={e}，占 {frac*100:.1f}% 像素，"
              f"空带 {e-s}px")
        if not apply:
            continue
        out = img.crop((0, 0, img.width, s))
        bb = out.getbbox()
        if bb:
            out = out.crop(bb)
        pad = int(max(out.size) * 0.04)
        canvas = Image.new("RGBA", (out.width + pad * 2, out.height + pad * 2),
                           (0, 0, 0, 0))
        canvas.paste(out, (pad, pad), out)
        canvas.save(p, optimize=True)
        print(f"{'':<14} -> 裁成 {canvas.size}")
    if hit == 0:
        print("没有发现游离残片")
    elif not apply:
        print("\n以上只是检查。加 --apply 才会真改。")


if __name__ == "__main__":
    main()
