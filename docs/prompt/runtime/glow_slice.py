#!/usr/bin/env python3
"""三格横条发光件表切成透明 PNG。

alpha 取最亮通道，颜色除回 alpha，光晕在浅色战场上仍是亮的。
用法：.venv-mock/bin/python docs/prompt/runtime/glow_slice.py <sheet> <shot> <burst> <shards>
切图写到件表同级的 slice/ 目录。
"""

import sys
from pathlib import Path

import numpy as np
from PIL import Image

LO = 14.0
PAD = 6
MAX_SIDE = 256


def unglow(rgb: np.ndarray) -> np.ndarray:
    rgb = rgb.astype(np.float32)
    lum = rgb.max(axis=2)
    a = np.clip((lum - LO) / (255.0 - LO), 0.0, 1.0) ** 0.85
    col = np.clip(rgb * (255.0 / np.maximum(lum, 1.0)[..., None]), 0, 255)
    out = np.dstack([col, a * 255.0]).astype(np.uint8)
    out[a <= 0.004] = 0
    return out


def trim(rgba: np.ndarray) -> np.ndarray:
    ys, xs = np.nonzero(rgba[..., 3] > 6)
    if len(xs) == 0:
        return rgba
    h, w = rgba.shape[:2]
    x0, x1 = max(0, int(xs.min()) - PAD), min(w, int(xs.max()) + PAD + 1)
    y0, y1 = max(0, int(ys.min()) - PAD), min(h, int(ys.max()) + PAD + 1)
    return rgba[y0:y1, x0:x1]


def main(sheet, names):
    src = Path(sheet)
    s = np.asarray(Image.open(src).convert("RGB"))
    h, w = s.shape[:2]
    cw = w // 3
    out = src.parent.parent / "slice" if src.parent.name == "raw" else src.parent / "slice"
    out.mkdir(parents=True, exist_ok=True)
    for i, name in enumerate(names):
        cell = s[8:h - 8, i * cw + 8:(i + 1) * cw - 8]
        im = Image.fromarray(trim(unglow(cell)), "RGBA")
        k = MAX_SIDE / max(im.size)
        if k < 1:
            im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        dst = out / f"{name}.png"
        im.save(dst, optimize=True)
        print(name, im.size, dst)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2:5])
