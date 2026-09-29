#!/usr/bin/env python3
"""把图鉴标签、印鉴、卡框从洋红底大图切出来，键控成透明 PNG。"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from process_ui_icons import bg_color, drop_bleed, flatten, key_out  # noqa: E402

ASSETS = os.environ.get(
    "GAME_ASSETS", "/Users/rosa/rosa_games/game_assets/black-rosa/assets")
RAW = os.path.join(ASSETS, "raw")
DEST = os.path.abspath(os.path.join(HERE, "..", "..", "..", "Assets", "Resources", "Art", "Ui"))


def fit(img, canvas, content):
    bbox = img.getbbox()
    if bbox is None:
        return Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    art = img.crop(bbox)
    box = int(canvas * content)
    scale = box / max(art.width, art.height)
    art = art.resize((max(1, round(art.width * scale)),
                      max(1, round(art.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    out.paste(art, ((canvas - art.width) // 2, (canvas - art.height) // 2), art)
    return out


def split(src, cols, rows, names, canvas=0, content=0.86, flat=6, t0=36.0, t1=88.0):
    img = Image.open(src).convert("RGBA")
    bg = bg_color(img)
    print(os.path.basename(src), "bg", bg, img.size)
    cw, ch = img.width // cols, img.height // rows
    out = []
    for i, name in enumerate(names):
        c, r = i % cols, i // cols
        cell = img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
        cut = key_out(cell, bg, t0=t0, t1=t1)
        cut, _ = drop_bleed(cut)
        if flat:
            cut = flatten(cut, colors=flat)
        if canvas:
            cut = fit(cut, canvas, content)
        else:
            bbox = cut.getbbox()
            if bbox:
                cut = cut.crop(bbox)
        out.append((name, cut))
        print(" ", name, cut.size)
    return out


def save(items):
    os.makedirs(DEST, exist_ok=True)
    for name, im in items:
        path = os.path.join(DEST, name + ".png")
        im.save(path, optimize=True)
        print(" ->", os.path.basename(path), im.size, os.path.getsize(path))


def main():
    marks = os.path.join(RAW, "codex_marks.png")
    plates = os.path.join(RAW, "codex_plates.png")
    save(split(marks, 3, 2, [
        "codex_tab_glyph", "codex_tab_pair", "codex_tab_enemy",
        "codex_seal_off", "codex_seal_ready", "codex_seal_on",
    ], canvas=168, content=0.88, flat=0))
    save(split(plates, 2, 2, [
        "codex_card", "codex_card_on", "codex_card_dim", "codex_plaque",
    ], canvas=0, content=1, flat=0, t0=28.0, t1=72.0))


if __name__ == "__main__":
    main()
