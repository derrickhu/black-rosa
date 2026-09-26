#!/usr/bin/env python3
"""把 Gemini 出的章节宽图套上和第 2、3 章一样的茶色圆角边，写进 Resources/Art/Ui/chapter_N.png。

    .venv-mock/bin/python docs/prompt/ui/chapter/process_chapter_art.py 4 5 6 7 8
"""
import os
import re
import sys
import uuid

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
RAW = "/Users/rosa/rosa_games/game_assets/black-rosa/美术/章节插图/raw_v2"
OUT = os.path.join(ROOT, "Assets", "Resources", "Art", "Ui")
W, H, RAD, STROKE = 1498, 682, 52, 14
FRAME = (210, 176, 141, 255)


def cover(scene, tw, th):
    sw, sh = scene.size
    s = max(tw / sw, th / sh)
    scene = scene.resize((max(tw, round(sw * s)), max(th, round(sh * s))), Image.LANCZOS)
    x = (scene.width - tw) // 2
    y = int((scene.height - th) * 0.5)
    return scene.crop((x, y, x + tw, y + th))


def frame(scene):
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    canvas.paste(cover(scene.convert("RGBA"), W - STROKE * 2, H - STROKE * 2), (STROKE, STROKE))
    outer = Image.new("L", (W, H), 0)
    ImageDraw.Draw(outer).rounded_rectangle([0, 0, W - 1, H - 1], radius=RAD, fill=255)
    inner = Image.new("L", (W, H), 0)
    ImageDraw.Draw(inner).rounded_rectangle([STROKE, STROKE, W - 1 - STROKE, H - 1 - STROKE],
                                            radius=RAD - STROKE, fill=255)
    border = Image.new("RGBA", (W, H), FRAME)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.paste(border, (0, 0), outer)
    out.paste(canvas, (0, 0), inner)
    return out


def write_meta(name):
    dst = os.path.join(OUT, name + ".png.meta")
    if os.path.exists(dst):
        return
    src = open(os.path.join(OUT, "chapter_3.png.meta"), encoding="utf-8").read()
    src = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, src, count=1)
    open(dst, "w", encoding="utf-8").write(src)


for k in sys.argv[1:]:
    name = f"chapter_{k}"
    frame(Image.open(os.path.join(RAW, f"chapter_wide_{k}.png"))).save(os.path.join(OUT, name + ".png"), optimize=True)
    write_meta(name)
    print("wrote", name)
