#!/usr/bin/env python3
"""把成词光效的黑底件表切成单张透明 PNG（秒杀 / 连斩 / 击退）。

和回旋镖那几张一样按「光」去底：alpha 取像素最亮的通道，颜色除回 alpha，
光晕的渐变就留成半透明，压在纸面背景上也是亮的，不会变成一圈灰雾。

跑：.venv-mock/bin/python docs/prompt/runtime/word_glow_slice.py <sheet.jpg> <kill.jpg>
"""

import sys
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[3]
OUT = REPO / "Assets/Resources/Art/Vfx"
MAX_SIDE = 256
LO = 14.0   # 低于它当纯黑，压掉 JPG 底噪
PAD = 6

# (件表, 列数, 行数, 列, 行, 输出名)
CELLS = [
    (0, 3, 2, 2, 0, "word_cleave_sword"),
    (0, 3, 2, 0, 1, "word_cleave_slash"),
    (0, 3, 2, 1, 1, "word_knock_wave"),
    (0, 3, 2, 2, 1, "word_knock_ring"),
    (1, 2, 1, 0, 0, "word_kill_blade"),
    (1, 2, 1, 1, 0, "word_kill_burst"),
]


def unglow(rgb: np.ndarray) -> np.ndarray:
    rgb = rgb.astype(np.float32)
    lum = rgb.max(axis=2)
    a = np.clip((lum - LO) / (255.0 - LO), 0.0, 1.0)
    a = a ** 0.85
    safe = np.maximum(lum, 1.0)[..., None]
    col = np.clip(rgb * (255.0 / safe), 0, 255)
    out = np.dstack([col, a * 255.0]).astype(np.uint8)
    out[a <= 0.004] = 0
    return out


def trim(rgba: np.ndarray) -> np.ndarray:
    ys, xs = np.nonzero(rgba[..., 3] > 6)
    if len(xs) == 0:
        return rgba
    h, w = rgba.shape[:2]
    x0, x1 = max(0, xs.min() - PAD), min(w, xs.max() + PAD + 1)
    y0, y1 = max(0, ys.min() - PAD), min(h, ys.max() + PAD + 1)
    return rgba[y0:y1, x0:x1]


def main(paths):
    sheets = [np.asarray(Image.open(p).convert("RGB")) for p in paths]
    for si, cols, rows, c, r, name in CELLS:
        s = sheets[si]
        h, w = s.shape[:2]
        cw, ch = w // cols, h // rows
        # 往里收一点：相邻格的光晕会探过格线。
        cell = s[r * ch + 8:(r + 1) * ch - 8, c * cw + 8:(c + 1) * cw - 8]
        im = Image.fromarray(trim(unglow(cell)), "RGBA")
        k = MAX_SIDE / max(im.size)
        if k < 1:
            im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        dst = OUT / f"{name}.png"
        im.save(dst, optimize=True)
        print(name, im.size, dst.stat().st_size)


if __name__ == "__main__":
    main(sys.argv[1:3])
