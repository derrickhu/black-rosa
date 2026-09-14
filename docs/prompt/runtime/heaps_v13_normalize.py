#!/usr/bin/env python3
"""Normalize v13 heap glyphs so they look the same size, not measure the same size.

v12 scaled every glyph so its ink bounding box hit 58% of the canvas. That is the
wrong metric: a dense left-right compound like 瞄 fills its box almost solid and
reads far heavier than a sparse glyph like 火, and any decoration that sticks out
(速's speed lines, 穿's needle tip) inflates the box and shrinks the character
inside it.

So: start from the 58% box fit, then nudge by ink coverage toward a common target.
The nudge is clamped so a glyph never drifts far from the box rule.

Usage:
  python3 docs/prompt/runtime/heaps_v13_normalize.py SRC_DIR -o DST_DIR [--size 256]
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

FILL = 0.58        # 外接框目标占比，和 v12 一致
TARGET_COV = 0.096 # 目标墨量，取当前 25 字的中位数
MIN_ADJ, MAX_ADJ = 0.82, 1.18   # 墨量修正的上下限，防止跑飞
MIN_FILL, MAX_FILL = 0.50, 0.66 # 修正后外接框仍要落在这个区间


def ink_mask(rgb: np.ndarray) -> np.ndarray:
    """Paper is bright and near-neutral; everything else is ink or a colored motif."""
    mx = rgb.max(axis=2)
    mn = rgb.min(axis=2)
    return ~((mx > 199) & (mn > 173) & ((mx - mn) < 36))


def normalize(src: Path, dst: Path, out: int) -> str:
    im = Image.open(src).convert("RGBA")
    rgb = np.asarray(im)[:, :, :3].astype(np.float32)
    ink = ink_mask(rgb)
    ys, xs = np.where(ink)
    if xs.size == 0:
        return f"SKIP empty {src.name}"

    x0, y0, x1, y1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
    crop = im.crop((x0, y0, x1 + 1, y1 + 1))
    gw, gh = crop.size

    # 1) 先按 v12 的规矩把外接框放到 58%
    base = (out * FILL) / max(gw, gh)
    # 2) 再按墨量修正：密的字缩、疏的字放。面积按 scale^2 走，所以开平方。
    cov = ink.sum() * base * base / (out * out)
    adj = float(np.clip(np.sqrt(TARGET_COV / cov), MIN_ADJ, MAX_ADJ))
    scale = base * adj
    # 3) 兜底：修正后外接框仍要落在 50%~66%
    fill = max(gw, gh) * scale / out
    if fill > MAX_FILL:
        scale *= MAX_FILL / fill
    elif fill < MIN_FILL:
        scale *= MIN_FILL / fill

    nw = max(1, round(gw * scale))
    nh = max(1, round(gh * scale))
    glyph = crop.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (out, out), (0xFC, 0xFC, 0xF6))
    canvas.paste(glyph.convert("RGB"), ((out - nw) // 2, (out - nh) // 2), glyph.split()[-1])
    dst.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(dst, "PNG")

    after = ink.sum() * scale * scale / (out * out)
    return (f"{src.stem:14} 墨量 {cov * 100:4.1f}% -> {after * 100:4.1f}%   "
            f"框 {FILL:.0%} -> {max(nw, nh) / out:.0%}   x{adj:.2f}")


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("-o", "--out", required=True)
    p.add_argument("--size", type=int, default=256)
    args = p.parse_args()
    src = Path(args.src)
    files = sorted(src.glob("heap_*.png")) if src.is_dir() else [src]
    if not files:
        print("no heap_*.png", file=sys.stderr)
        return 1
    for f in files:
        print(normalize(f, Path(args.out) / f.name, args.size))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
