"""v2 首帧合字，并裁出参考生用的人物图。

字用游戏里的 heap_split / heap_strike / heap_back，不让模型画。
    .venv-mock/bin/python docs/prompt/promo/drama_glyph_comp_v2.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

SRC = Path("/Users/rosa/.cursor/projects/Users-rosa-rosa-games-black-rosa/assets")
ART = Path("/Users/rosa/rosa_games/black-rosa/Assets/Resources/Art")
OUT = Path("/Users/rosa/rosa_games/game_assets/black-rosa/推广/宣传视频/drama_02")
GLOW = {
    "split": (214, 226, 255),
    "strike": (232, 176, 48),
    "back": (232, 176, 48),
}

# (首帧, [(字, 中心x比例, 中心y比例, 字宽比例)], 输出名)
COMP = [
    ("drama_s2_aim.jpg", [("split", 0.20, 0.13, 0.30)], "s2_aim.png"),
    ("drama_s3_street.jpg", [("split", 0.30, 0.11, 0.22)], "s3_street.png"),
    ("drama_s4_react.jpg", [("strike", 0.50, 0.11, 0.26), ("back", 0.50, 0.27, 0.26)], "s4_react.png"),
    ("drama_s5_street.jpg", [("strike", 0.50, 0.07, 0.20), ("back", 0.50, 0.22, 0.20)], "s5_street.png"),
]

# (首帧, 裁名, 左, 上, 右, 下) 比例
CROPS = [
    ("drama_s1_faces.jpg", "s1_man", 0.00, 0.30, 0.58, 0.98),
    ("drama_s1_faces.jpg", "s1_woman", 0.46, 0.34, 1.00, 0.98),
    ("drama_s2_aim.jpg", "s2_man", 0.00, 0.28, 0.58, 0.99),
    ("drama_s2_aim.jpg", "s2_woman", 0.52, 0.32, 1.00, 0.99),
    ("drama_s4_react.jpg", "s4_man", 0.00, 0.42, 0.58, 0.99),
    ("drama_s4_react.jpg", "s4_woman", 0.42, 0.44, 1.00, 0.99),
    ("drama_s6_boss.jpg", "s6_man", 0.00, 0.36, 0.52, 0.99),
    ("drama_s6_boss.jpg", "s6_woman", 0.32, 0.42, 0.96, 0.99),
]


def glyph(kind):
    im = Image.open(ART / f"heap_{kind}.png").convert("RGB")
    bg = Image.new("RGB", im.size, im.getpixel((2, 2)))
    diff = ImageChops.difference(im, bg).convert("L")
    alpha = diff.point(lambda v: 0 if v < 16 else min(255, (v - 16) * 4))
    out = im.convert("RGBA")
    out.putalpha(alpha)
    return out.crop(out.getbbox())


def glowing(kind, width):
    g = glyph(kind)
    g = g.resize((width, max(1, int(g.height * width / g.width))), Image.LANCZOS)
    pad = max(8, width // 3)
    w, h = g.width + pad * 2, g.height + pad * 2
    a = Image.new("L", (w, h), 0)
    a.paste(g.getchannel("A"), (pad, pad))
    col = GLOW[kind]
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for radius, gain in ((pad * 0.55, 1.0), (pad * 0.22, 1.35), (5, 1.7)):
        blur = a.filter(ImageFilter.GaussianBlur(radius)).point(lambda v, k=gain: min(255, int(v * k)))
        halo = Image.new("RGBA", (w, h), col + (0,))
        halo.putalpha(blur)
        layer.alpha_composite(halo)
    rim = ImageChops.subtract(a.filter(ImageFilter.MaxFilter(5)), a)
    hot = Image.new("RGBA", (w, h), tuple(min(255, c + 80) for c in col) + (0,))
    hot.putalpha(rim)
    layer.alpha_composite(hot)
    layer.alpha_composite(g, (pad, pad))
    return layer


def main():
    stills = OUT / "stills"
    faces = OUT / "faces"
    stills.mkdir(parents=True, exist_ok=True)
    faces.mkdir(parents=True, exist_ok=True)
    for name in ("luchen_face.jpg", "suwan_face.jpg", "drama_s1_faces.jpg", "drama_s2_aim.jpg",
                 "drama_s3_street.jpg", "drama_s4_react.jpg", "drama_s5_street.jpg", "drama_s6_boss.jpg"):
        im = Image.open(SRC / name).convert("RGB")
        im.save(stills / name, quality=92)
        print("copy", name, im.size)

    for src, marks, out in COMP:
        base = Image.open(stills / src).convert("RGBA")
        w, h = base.size
        for kind, fx, fy, fw in marks:
            g = glowing(kind, int(w * fw))
            base.alpha_composite(g, (int(w * fx - g.width / 2), int(h * fy - g.height / 2)))
        base.convert("RGB").save(stills / out, quality=92)
        print(out, base.size)

    for src, name, l, t, r, b in CROPS:
        im = Image.open(stills / src).convert("RGB")
        w, h = im.size
        box = (int(w * l), int(h * t), int(w * r), int(h * b))
        im.crop(box).save(faces / f"{name}.jpg", quality=92)
        print("crop", name, box)


if __name__ == "__main__":
    main()
