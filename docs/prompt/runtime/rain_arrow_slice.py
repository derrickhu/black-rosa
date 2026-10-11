#!/usr/bin/env python3
"""把落箭 2x2 白底表切成四帧。箭尖对齐到画布底部同一点，循环时尖不会跳。

白底从四边洪泛去掉。箭心是奶白，描边是深棕，洪泛停在描边上，核留得住。
每帧只留最大的一块，旁边的小火星若和箭身断开会丢掉；连在一起的会留着。

跑：/usr/bin/python3 docs/prompt/runtime/rain_arrow_slice.py <sheet.png>
"""

from collections import deque
from pathlib import Path
import sys

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[3]
OUT = REPO / "Assets/Resources/Art/Vfx"
SIZE = 256
TIP_Y = 22          # 箭尖距画布底边的像素，四帧共用
BODY_H = 196        # 箭身目标高度
BG_MIN = 228


def cut(sheet, shave=8):
    h, w = sheet.shape[:2]
    cw, ch = w // 2, h // 2
    cells = []
    for r in range(2):
        for c in range(2):
            cells.append(sheet[r * ch + shave:(r + 1) * ch - shave,
                               c * cw + shave:(c + 1) * cw - shave])
    return cells


def flood_bg(cell):
    near = cell.min(axis=2) >= BG_MIN
    h, w = near.shape
    bg = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if near[y, x] and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if near[y, x] and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and near[ny, nx] and not bg[ny, nx]:
                bg[ny, nx] = True
                q.append((ny, nx))
    return ~bg


def largest(mask):
    h, w = mask.shape
    seen = np.zeros((h, w), bool)
    best = None
    best_n = 0
    ys, xs = np.nonzero(mask)
    for y0, x0 in zip(ys, xs):
        if seen[y0, x0]:
            continue
        q = deque([(y0, x0)])
        seen[y0, x0] = True
        pix = []
        while q:
            y, x = q.popleft()
            pix.append((y, x))
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    q.append((ny, nx))
        if len(pix) > best_n:
            best_n = len(pix)
            best = pix
    out = np.zeros((h, w), bool)
    if best:
        for y, x in best:
            out[y, x] = True
    return out


def extras(mask, body):
    """箭尖以上、又没低于箭尖的小块（火星）留着，低于箭尖的丢掉，免得把尖拽偏。"""
    ys, xs = np.nonzero(body)
    if len(ys) == 0:
        return body
    tip = ys.max()
    rest = mask & ~body
    h, w = mask.shape
    seen = np.zeros((h, w), bool)
    keep = body.copy()
    ry, rx = np.nonzero(rest)
    for y0, x0 in zip(ry, rx):
        if seen[y0, x0]:
            continue
        q = deque([(y0, x0)])
        seen[y0, x0] = True
        pix = []
        low = y0
        while q:
            y, x = q.popleft()
            pix.append((y, x))
            if y > low:
                low = y
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and rest[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    q.append((ny, nx))
        if low <= tip + 2:
            for y, x in pix:
                keep[y, x] = True
    return keep


def emit(cell, mask, path):
    ys, xs = np.nonzero(mask)
    top, bot = ys.min(), ys.max()
    left, right = xs.min(), xs.max()
    # 箭尖：主体最底下一截的横向中心。图像 y 向下，底是 bot。
    band = ys >= bot - 6
    tip_x = xs[band].mean() if band.any() else (left + right) / 2
    crop = cell[top:bot + 1, left:right + 1]
    crop_m = mask[top:bot + 1, left:right + 1]
    rgba = np.zeros((crop.shape[0], crop.shape[1], 4), np.uint8)
    rgba[..., :3] = crop
    rgba[..., 3] = crop_m.astype(np.uint8) * 255
    src_h = max(1, bot - top + 1)
    scale = BODY_H / src_h
    nw = max(1, int(round((right - left + 1) * scale)))
    nh = max(1, int(round(src_h * scale)))
    im = Image.fromarray(rgba, "RGBA").resize((nw, nh), Image.Resampling.LANCZOS)
    # 缩放后尖的 x 按同样比例从裁切左边量
    tip_local = (tip_x - left) * scale
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    px = int(round(SIZE / 2 - tip_local))
    py = int(round(SIZE - TIP_Y - nh))
    canvas.paste(im, (px, py), im)
    canvas.save(path)
    print(f"  {path.name}  tip_local={tip_local:.1f} placed=({px},{py}) size={nw}x{nh}")


def main():
    src = Path(sys.argv[1])
    sheet = np.asarray(Image.open(src).convert("RGB"))
    OUT.mkdir(parents=True, exist_ok=True)
    for i, cell in enumerate(cut(sheet)):
        body = largest(flood_bg(cell))
        mask = extras(flood_bg(cell), body)
        if mask.sum() < 40:
            print(f"  frame {i} almost empty, skip")
            continue
        emit(cell, mask, OUT / f"rain_arrow_{i:02d}.png")


if __name__ == "__main__":
    main()
