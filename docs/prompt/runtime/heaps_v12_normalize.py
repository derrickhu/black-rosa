#!/usr/bin/env python3
"""Normalize v12 heap glyphs: ink bbox centered at 58% of a paper-white square.

Usage:
  python3 docs/prompt/runtime/heaps_v12_normalize.py SRC_DIR -o DST_DIR
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image
import numpy as np

PAPER = np.array([0xFC, 0xFC, 0xF6], dtype=np.float32)
FILL = 0.58
OUT = 1024


def is_paper(rgb: np.ndarray) -> np.ndarray:
    mx = rgb.max(axis=2)
    mn = rgb.min(axis=2)
    return (mx > 199) & (mn > 173) & ((mx - mn) < 36)


def ink_bbox(rgb: np.ndarray) -> tuple[int, int, int, int] | None:
    ink = ~is_paper(rgb)
    if rgb.shape[2] == 4:
        ink &= rgb[:, :, 3] > 20
    ys, xs = np.where(ink)
    if xs.size == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def normalize(src: Path, dst: Path, OUT: int = OUT) -> str:
    im = Image.open(src).convert("RGBA")
    arr = np.asarray(im)
    rgb = arr[:, :, :3].astype(np.float32)
    box = ink_bbox(rgb)
    if box is None:
        return f"SKIP empty {src.name}"
    x0, y0, x1, y1 = box
    crop = im.crop((x0, y0, x1 + 1, y1 + 1))
    gw, gh = crop.size
    before = max(gw / im.width, gh / im.height)
    box_px = max(1, int(round(OUT * FILL)))
    scale = box_px / max(gw, gh)
    nw = max(1, int(round(gw * scale)))
    nh = max(1, int(round(gh * scale)))
    glyph = crop.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (OUT, OUT), (0xFC, 0xFC, 0xF6))
    canvas.paste(glyph.convert("RGB"), ((OUT - nw) // 2, (OUT - nh) // 2), glyph.split()[-1])
    dst.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(dst, "PNG")
    after = max(nw, nh) / OUT
    return f"{src.name:24} {before:.0%} -> {after:.0%}  {dst.name}"


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("src")
    p.add_argument("-o", "--out", required=True)
    p.add_argument("--size", type=int, default=OUT)
    args = p.parse_args()
    src = Path(args.src)
    out = Path(args.out)
    files = sorted(src.glob("heap_*.png")) if src.is_dir() else [src]
    if not files:
        print("no heap_*.png", file=sys.stderr)
        return 1
    for f in files:
        print(normalize(f, out / f.name, args.size))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
