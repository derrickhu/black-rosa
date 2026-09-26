#!/usr/bin/env python3
"""出征页「图鉴」贴纸：raw/act_codex.png 抠成透明图，尺寸对齐其它 ico_act_*。

提示词见 act_codex_prompt.txt。底色是偏玫红的洋红，按四边采样。
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(__file__)
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
RAW = os.path.join(ROOT, "docs", "prompt", "ui", "raw", "act_codex.png")
DEST = os.path.join(ROOT, "Assets", "Resources", "Art", "Ui", "ico_act_codex.png")
sys.argv = sys.argv[:1]
sys.path.insert(0, HERE)
from process_ui_icons import bg_color, key_out, drop_bleed  # noqa: E402

MAX_SIDE = 210


def main():
    sheet = Image.open(RAW).convert("RGBA")
    bg = bg_color(sheet)
    print("bg", bg)
    # 红包红的书皮离玫红底只有 ~100 的色距，阈值收紧免得书皮发透。
    cut, _ = drop_bleed(key_out(sheet, bg, t0=28.0, t1=56.0))
    cut = cut.crop(cut.getbbox())
    scale = MAX_SIDE / max(cut.width, cut.height)
    cut = cut.resize((max(1, round(cut.width * scale)), max(1, round(cut.height * scale))), Image.LANCZOS)
    cut.save(DEST, optimize=True)
    print(os.path.basename(DEST), cut.size, os.path.getsize(DEST))


if __name__ == "__main__":
    main()
