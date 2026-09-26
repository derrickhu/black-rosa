#!/usr/bin/env python3
"""排行榜名次牌：raw/rank_medals.png（2x2）抠成四张透明图放进 Resources/Art/Ui。

内置生图给的底色是偏玫红的洋红，不是纯 #FF00FF，所以背景色按四边采样。
提示词见 rank_medals_prompt.txt。
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(__file__)
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
RAW = os.path.join(ROOT, "docs", "prompt", "ui", "raw", "rank_medals.png")
OUT = os.path.join(ROOT, "Assets", "Resources", "Art", "Ui")
sys.argv = sys.argv[:1]
sys.path.insert(0, HERE)
from process_ui_icons import bg_color, key_out, drop_bleed  # noqa: E402

NAMES = ["ico_rank_1", "ico_rank_2", "ico_rank_3", "ico_rank_plate"]
SIDE = 128


def main():
    sheet = Image.open(RAW).convert("RGBA")
    bg = bg_color(sheet)
    print("bg", bg)
    cw, ch = sheet.width // 2, sheet.height // 2
    for i, name in enumerate(NAMES):
        cx, cy = i % 2, i // 2
        cell = sheet.crop((cx * cw, cy * ch, (cx + 1) * cw, (cy + 1) * ch))
        # 红包红的挂带离玫红底只有 ~77 的色距，默认阈值会把挂带抠成半透明。
        cut, _ = drop_bleed(key_out(cell, bg, t0=28.0, t1=56.0))
        box = cut.getbbox()
        cut = cut.crop(box)
        scale = SIDE * 0.94 / max(cut.width, cut.height)
        art = cut.resize((max(1, round(cut.width * scale)), max(1, round(cut.height * scale))), Image.LANCZOS)
        canvas = Image.new("RGBA", (SIDE, SIDE), (0, 0, 0, 0))
        canvas.paste(art, ((SIDE - art.width) // 2, (SIDE - art.height) // 2), art)
        dest = os.path.join(OUT, name + ".png")
        canvas.save(dest, optimize=True)
        print(name, canvas.size, os.path.getsize(dest))


if __name__ == "__main__":
    main()
