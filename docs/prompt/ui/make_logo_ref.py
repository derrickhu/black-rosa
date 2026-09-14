#!/usr/bin/env python3
"""拼一张 logo 参考图：上半是四字拼写条，下半是平面风格色板。

生图只收一张 --image。拼在一起才能同时锁「写成哪四个字」和「怎么平涂描边」。
色板必须是抽象几何，不能画字、不能画游戏里已有的图标主体。
"""
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SPELL = os.path.join(HERE, "..", "..", "美术", "loading", "raw", "logo_type_ref_row.png")
OUT = os.path.join(HERE, "..", "..", "美术", "loading", "raw", "logo_ref_spell_style.png")

CELL = 220
LINE = (58, 42, 32)
LW = 12
BG = (255, 0, 255)


def outlined(d, draw_fn, fill):
    draw_fn(d, LINE, LW)
    draw_fn(d, fill, 0)


def wedge(d, cx, cy, r):
    def ring(dd, col, grow):
        dd.ellipse((cx - r - grow, cy - r - grow, cx + r + grow, cy + r + grow), fill=col)
    outlined(d, ring, (246, 190, 60))
    d.chord((cx - r, cy - r, cx + r, cy + r), 25, 155, fill=(198, 140, 32))
    d.chord((cx - r, cy - r, cx + r, cy + r), 200, 330, fill=(255, 218, 120))


def slab(d, cx, cy, r):
    def box(dd, col, grow):
        dd.rounded_rectangle(
            (cx - r - grow, cy - r - grow, cx + r + grow, cy + r + grow),
            radius=r * 0.34 + grow, fill=col)
    outlined(d, box, (232, 78, 78))
    d.rectangle((cx - r, cy + r * 0.30, cx + r, cy + r), fill=(186, 52, 52))
    d.rectangle((cx - r, cy - r, cx + r, cy - r * 0.44), fill=(255, 132, 132))


def lobe(d, cx, cy, r):
    pts = [(-1.00, -0.10), (-0.62, -0.82), (0.18, -1.00), (0.92, -0.50),
           (1.00, 0.28), (0.44, 0.96), (-0.38, 0.88), (-0.90, 0.46)]

    def shape(dd, col, grow):
        k = 1.0 + grow / r
        dd.polygon([(cx + x * r * k, cy + y * r * k) for x, y in pts], fill=col)
    outlined(d, shape, (54, 176, 176))
    d.polygon([(cx + x * r, cy + y * r) for x, y in
               ((-0.90, 0.46), (0.44, 0.96), (0.70, 0.30), (-0.70, 0.16))],
              fill=(26, 132, 132))


def chevron(d, cx, cy, r):
    pts = [(0.00, -1.00), (0.86, -0.06), (0.44, -0.06), (0.44, 0.94),
           (-0.44, 0.94), (-0.44, -0.06), (-0.86, -0.06)]

    def shape(dd, col, grow):
        k = 1.0 + grow / r
        dd.polygon([(cx + x * r * k, cy + y * r * k) for x, y in pts], fill=col)
    outlined(d, shape, (140, 110, 220))
    d.rectangle((cx - r * 0.44, cy + r * 0.34, cx + r * 0.44, cy + r * 0.94),
                fill=(104, 78, 182))


def style_strip(width):
    n = 4
    h = CELL + 36
    img = Image.new("RGB", (width, h), BG)
    d = ImageDraw.Draw(img)
    gap = width / n
    r = min(CELL, gap) * 0.36
    for i, fn in enumerate((wedge, slab, lobe, chevron)):
        fn(d, gap * (i + 0.5), h / 2, r)
    return img


def main():
    spell = Image.open(SPELL).convert("RGB")
    strip = style_strip(spell.width)
    out = Image.new("RGB", (spell.width, spell.height + strip.height), BG)
    out.paste(spell, (0, 0))
    out.paste(strip, (0, spell.height))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    out.save(OUT)
    print("wrote", os.path.abspath(OUT), out.size)


if __name__ == "__main__":
    main()
