"""v3 古代战场首帧合字。

    .venv-mock/bin/python docs/prompt/promo/ancient_glyph_comp.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

SRC = Path("/Users/rosa/.cursor/projects/Users-rosa-rosa-games-black-rosa/assets")
ART = Path("/Users/rosa/rosa_games/black-rosa/Assets/Resources/Art")
OUT = Path("/Users/rosa/rosa_games/game_assets/black-rosa/推广/宣传视频/drama_03")
GLOW = {
    "split": (214, 226, 255),
    "strike": (232, 176, 48),
    "back": (232, 176, 48),
}
COMP = [
    ("battle_s2_point.jpg", [("split", 0.40, 0.12, 0.26)], "s2_point.png"),
    ("battle_s3_volley.jpg", [("split", 0.22, 0.10, 0.20)], "s3_volley.png"),
    ("battle_s4_stack.jpg", [("strike", 0.55, 0.10, 0.24), ("back", 0.55, 0.25, 0.24)], "s4_stack.png"),
    ("battle_s5_push.jpg", [("strike", 0.50, 0.07, 0.18), ("back", 0.50, 0.19, 0.18)], "s5_push.png"),
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
    stills.mkdir(parents=True, exist_ok=True)
    for name in ("general_face.jpg", "master_face.jpg", "battle_s1_fire.jpg", "battle_s2_point.jpg",
                 "battle_s3_volley.jpg", "battle_s4_stack.jpg", "battle_s5_push.jpg", "battle_s6_grin.jpg"):
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


if __name__ == "__main__":
    main()
