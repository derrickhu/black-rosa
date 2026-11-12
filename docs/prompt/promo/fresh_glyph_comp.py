"""v4「城墙上的保安」首帧合字。夜景里墨黑笔画看不清，笔画本身做成发光的光墨。

    .venv-mock/bin/python docs/prompt/promo/fresh_glyph_comp.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

SRC = Path("/Users/rosa/.cursor/projects/Users-rosa-rosa-games-black-rosa/assets")
ART = Path("/Users/rosa/rosa_games/black-rosa/Assets/Resources/Art")
OUT = Path("/Users/rosa/rosa_games/game_assets/black-rosa/推广/宣传视频/drama_04/stills")
VIOLET = ((186, 96, 255), (246, 228, 255))
GOLD = ((255, 176, 48), (255, 246, 214))
LOOK = {"confuse": VIOLET, "myriad": GOLD, "arrow": GOLD, "sec": GOLD, "kill": GOLD}
STILLS = ["fresh_s1_mock", "fresh_s2_write", "fresh_s3_start", "fresh_s3_end2", "fresh_s4_start",
          "fresh_s4_end", "fresh_s5_boss", "fresh_s6_kneel"]
COMP = [
    ("fresh_s2_write", [("confuse", 0.60, 0.19, 0.34)], "s2_write.png"),
    ("fresh_s3_start", [("confuse", 0.50, 0.13, 0.28)], "s3_start.png"),
    ("fresh_s3_end2", [("confuse", 0.50, 0.13, 0.28)], "s3_end.png"),
    ("fresh_s4_start", [("myriad", 0.62, 0.09, 0.22), ("arrow", 0.62, 0.22, 0.22)], "s4_start.png"),
    ("fresh_s4_end", [("myriad", 0.62, 0.09, 0.22), ("arrow", 0.62, 0.22, 0.22)], "s4_end.png"),
    ("fresh_s5_boss", [("sec", 0.16, 0.09, 0.20), ("kill", 0.16, 0.21, 0.20)], "s5_boss.png"),
]


def glyph_alpha(kind):
    im = Image.open(ART / f"heap_{kind}.png").convert("RGB")
    bg = Image.new("RGB", im.size, im.getpixel((2, 2)))
    a = ImageChops.difference(im, bg).convert("L").point(lambda v: 0 if v < 16 else min(255, (v - 16) * 4))
    return a.crop(a.getbbox())


def light_ink(kind, width):
    a = glyph_alpha(kind)
    a = a.resize((width, max(1, int(a.height * width / a.width))), Image.LANCZOS)
    pad = max(8, width // 3)
    w, h = a.width + pad * 2, a.height + pad * 2
    mask = Image.new("L", (w, h), 0)
    mask.paste(a, (pad, pad))
    glow, core = LOOK[kind]
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for radius, gain in ((pad * 0.6, 0.9), (pad * 0.25, 1.3), (pad * 0.08, 1.8)):
        blur = mask.filter(ImageFilter.GaussianBlur(radius)).point(lambda v, k=gain: min(255, int(v * k)))
        halo = Image.new("RGBA", (w, h), glow + (0,))
        halo.putalpha(blur)
        layer.alpha_composite(halo)
    fill = Image.new("RGBA", (w, h), core + (0,))
    fill.putalpha(mask)
    layer.alpha_composite(fill)
    return layer


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name in STILLS:
        Image.open(SRC / f"{name}.jpg").convert("RGB").save(OUT / f"{name}.jpg", quality=92)
    for src, marks, out in COMP:
        base = Image.open(OUT / f"{src}.jpg").convert("RGBA")
        w, h = base.size
        for kind, fx, fy, fw in marks:
            g = light_ink(kind, int(w * fw))
            base.alpha_composite(g, (int(w * fx - g.width / 2), int(h * fy - g.height / 2)))
        base.convert("RGB").save(OUT / out)
        print(out, base.size)


if __name__ == "__main__":
    main()
