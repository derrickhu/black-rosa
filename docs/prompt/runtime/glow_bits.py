"""从发光碎片图里切出单个碎片，给炮弹「环 / 绕 / 尾」槽当小件用。

碎片图（glow_slice.py 的第三栏）是一簇碎片。槽位小件要的是一块一块的：
破的一片晶、聚的一蓝一粉两颗珠、返的一道弯月。按 alpha 连通块切，
挑面积最大、且颜色彼此拉得开的前 N 块，各自裁边输出。

用法：
  .venv-mock/bin/python docs/prompt/runtime/glow_bits.py <shards.png> <out_prefix> [N]
输出 <out_prefix>0.png、<out_prefix>1.png ...
"""
import sys
from collections import deque

import numpy as np
from PIL import Image

ALPHA_CUT = 40
PAD = 4


def components(alpha):
    h, w = alpha.shape
    mask = alpha > ALPHA_CUT
    label = np.zeros((h, w), np.int32)
    comps = []
    n = 0
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or label[y, x]:
                continue
            n += 1
            q = deque([(y, x)])
            label[y, x] = n
            pts = []
            while q:
                cy, cx = q.popleft()
                pts.append((cy, cx))
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = cy + dy, cx + dx
                    if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not label[ny, nx]:
                        label[ny, nx] = n
                        q.append((ny, nx))
            comps.append((n, pts))
    return label, comps


def hue(rgb):
    r, g, b = rgb / 255.0
    mx, mn = max(r, g, b), min(r, g, b)
    if mx - mn < 1e-6:
        return 0.0
    if mx == r:
        h = ((g - b) / (mx - mn)) % 6
    elif mx == g:
        h = (b - r) / (mx - mn) + 2
    else:
        h = (r - g) / (mx - mn) + 4
    return h * 60.0


def main():
    src, prefix = sys.argv[1], sys.argv[2]
    want = int(sys.argv[3]) if len(sys.argv) > 3 else 1
    im = np.array(Image.open(src).convert("RGBA"))
    label, comps = components(im[:, :, 3])
    comps.sort(key=lambda c: -len(c[1]))
    picked = []
    for n, pts in comps:
        if len(pts) < 60:
            break
        ys = np.array([p[0] for p in pts])
        xs = np.array([p[1] for p in pts])
        rgb = im[ys, xs, :3].astype(float).mean(axis=0)
        hh = hue(rgb)
        if any(min(abs(hh - q), 360 - abs(hh - q)) < 40 for q, *_ in picked):
            continue
        picked.append((hh, n, ys, xs))
        if len(picked) == want:
            break
    if len(picked) < want:
        # 颜色拉不开就直接按大小补齐
        for n, pts in comps:
            if len(picked) == want:
                break
            if any(n == p[1] for p in picked):
                continue
            ys = np.array([p[0] for p in pts])
            xs = np.array([p[1] for p in pts])
            picked.append((0.0, n, ys, xs))
    h, w = label.shape
    for i, (_, n, ys, xs) in enumerate(picked):
        y0, y1 = max(0, ys.min() - PAD), min(h, ys.max() + PAD + 1)
        x0, x1 = max(0, xs.min() - PAD), min(w, xs.max() + PAD + 1)
        crop = im[y0:y1, x0:x1].copy()
        own = label[y0:y1, x0:x1]
        # 只留这一块：别的碎片清空，自己的软边（低于阈值的晕）保留
        other = (own != 0) & (own != n)
        crop[other] = 0
        Image.fromarray(crop).save(f"{prefix}{i}.png")
        print(f"{prefix}{i}.png {crop.shape[1]}x{crop.shape[0]}")


if __name__ == "__main__":
    main()
