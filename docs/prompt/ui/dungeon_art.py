"""副本页的图：两张卡的横幅插画裁切加框，加一条翡翠色飘带。

    .venv-mock/bin/python docs/prompt/ui/dungeon_art.py
    .venv-mock/bin/python docs/prompt/runtime/cdn_build.py

横幅进 CdnArt/Ui/（走云存储，包里只留 256 缩略图），飘带直接进 Resources/Art/Ui/。
母版在仓库外 game_assets/black-rosa/美术/runtime/dungeon/raw/。
"""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
RAW = ROOT.parent / "game_assets" / "black-rosa" / "美术" / "runtime" / "dungeon" / "raw"
CDN = ROOT / "CdnArt" / "Ui"
UI = ROOT / "Assets" / "Resources" / "Art" / "Ui"

# 和章节插画同一道框：驼色边，1498 宽时 14px、圆角约 40px，按宽度等比。
FRAME = (210, 176, 141, 255)
OUT_W, OUT_H = 1024, 440
BORDER = 10
RADIUS = 28

# (母版, 成品, 裁切框上沿占原图高度的比例)
BANNERS = [
    ("dungeon_daily_raw.jpg", "dungeon_daily.png", 0.14),
    ("dungeon_endless_raw.jpg", "dungeon_endless.png", 0.02),
]


def crop_band(img, top_frac):
    w, h = img.size
    band_h = round(w * OUT_H / OUT_W)
    top = min(max(0, round(h * top_frac)), h - band_h)
    return img.crop((0, top, w, top + band_h)).resize((OUT_W, OUT_H), Image.LANCZOS)


def framed(img):
    img = img.convert("RGBA")
    out = Image.new("RGBA", (OUT_W, OUT_H), (0, 0, 0, 0))
    outer = Image.new("L", (OUT_W, OUT_H), 0)
    ImageDraw.Draw(outer).rounded_rectangle((0, 0, OUT_W - 1, OUT_H - 1), RADIUS, fill=255)
    inner = Image.new("L", (OUT_W, OUT_H), 0)
    ImageDraw.Draw(inner).rounded_rectangle(
        (BORDER, BORDER, OUT_W - 1 - BORDER, OUT_H - 1 - BORDER), RADIUS - BORDER // 2, fill=255)
    out.paste(Image.new("RGBA", (OUT_W, OUT_H), FRAME), (0, 0), outer)
    out.paste(img, (0, 0), inner)
    return out


def jade_ribbon():
    # 朱红飘带换成翡翠绿：红色调的像素按亮度映射到绿，描边和投影不动。
    src = Image.open(UI / "ribbon_chapter.png").convert("RGBA")
    px = src.load()
    w, h = src.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0 or r < 90 or r - max(g, b) < 50:
                continue
            k = r / 205.0
            px[x, y] = (round(47 * k), round(143 * k), round(107 * k), a)
    src.save(UI / "ribbon_daily.png", optimize=True)


def main():
    CDN.mkdir(parents=True, exist_ok=True)
    for raw, out, top in BANNERS:
        img = crop_band(Image.open(RAW / raw), top)
        framed(img).save(CDN / out, optimize=True)
        print("banner", CDN / out)
    jade_ribbon()
    print("ribbon", UI / "ribbon_daily.png")


if __name__ == "__main__":
    main()
