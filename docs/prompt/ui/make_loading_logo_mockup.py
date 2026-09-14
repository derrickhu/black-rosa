#!/usr/bin/env python3
"""把字标铺到 loading 底图上，给 Gemini 当「完整竖构图」参考。"""
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[3]
RAW = REPO / "docs" / "美术" / "loading" / "raw"
UI = REPO / "Assets" / "Resources" / "Art" / "Ui"
W, H = 1080, 1920


def cover(src, size):
    tw, th = size
    sw, sh = src.size
    scale = max(tw / sw, th / sh)
    nw, nh = int(sw * scale), int(sh * scale)
    im = src.resize((nw, nh), Image.LANCZOS)
    x = (nw - tw) // 2
    y = (nh - th) // 2
    return im.crop((x, y, x + tw, y + th))


def paste_title(bg, mark, y=196, width=860):
    page = cover(bg.convert("RGB"), (W, H)).convert("RGBA")
    mark = mark.convert("RGBA")
    h = int(mark.height * (width / mark.width))
    mark = mark.resize((width, h), Image.LANCZOS)
    page.alpha_composite(mark, ((W - width) // 2, y))
    return page


def main():
    bg = Image.open(RAW / "loading_bg.png")
    orig = Image.open(UI / "logo.png")
    k = Image.open(UI / "logo_k.png")
    RAW.mkdir(parents=True, exist_ok=True)
    a = paste_title(bg, orig)
    b = paste_title(bg, k)
    a.save(RAW / "loading_mockup_orig.png")
    b.save(RAW / "loading_mockup_k.png")
    print("wrote", RAW / "loading_mockup_orig.png", a.size)
    print("wrote", RAW / "loading_mockup_k.png", b.size)


if __name__ == "__main__":
    main()
