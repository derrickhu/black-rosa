"""真人短剧首帧合字：把游戏里的冰 / 火字卡抠出来，加辉光，合进 35mm 剧照的天空。

模型画不准汉字，字由我们合进首帧，Vidu 只负责让它动。
    /usr/bin/python3 docs/prompt/promo/drama_glyph_comp.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

ART = Path("/Users/rosa/rosa_games/game_assets/black-rosa/美术/runtime/heaps_v9/raw")
D = Path("/Users/rosa/rosa_games/game_assets/black-rosa/推广/宣传视频/drama_01")
GLOW = {"ice": (120, 200, 255), "fire": (255, 140, 40)}

# (剧照, 字, 中心 x, 中心 y, 字宽, 输出)
SHOTS = [
    ("drama_s2_aim.jpg", "ice", 200, 250, 300, "s2_aim_ice.png"),
    ("drama_s3_street.jpg", "ice", 380, 440, 270, "s3_start_ice.png"),
    ("drama_s5_frozen.jpg", "ice", 380, 440, 270, "s3_end_ice.png"),
    ("drama_s4_react.jpg", "fire", 470, 220, 300, "s4_react_fire.png"),
    ("drama_s5_frozen.jpg", "fire", 380, 440, 270, "s5_start_fire.png"),
    ("drama_s5_ash.jpg", "fire", 380, 440, 270, "s5_end_fire.png"),
]


def glyph(kind):
    """卡面裁掉外框，米白底按色差抠成透明。"""
    im = Image.open(ART / f"card_{kind}.png").convert("RGB").crop((150, 150, 874, 874))
    bg = Image.new("RGB", im.size, (250, 249, 244))
    diff = ImageChops.difference(im, bg).convert("L")
    alpha = diff.point(lambda v: 0 if v < 18 else min(255, (v - 18) * 3))
    out = im.convert("RGBA")
    out.putalpha(alpha)
    return out.crop(out.getbbox())


def glowing(kind, width):
    g = glyph(kind)
    g = g.resize((width, int(g.height * width / g.width)), Image.LANCZOS)
    pad = width // 3
    w, h = g.width + pad * 2, g.height + pad * 2
    a = Image.new("L", (w, h), 0)
    a.paste(g.getchannel("A"), (pad, pad))
    col = GLOW[kind]
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for radius, gain in ((pad * 0.55, 1.0), (pad * 0.22, 1.4), (6, 1.8)):
        blur = a.filter(ImageFilter.GaussianBlur(radius)).point(lambda v, k=gain: min(255, int(v * k)))
        halo = Image.new("RGBA", (w, h), col + (0,))
        halo.putalpha(blur)
        layer.alpha_composite(halo)
    # 笔画边上一圈亮边，像字自己在发光
    rim = a.filter(ImageFilter.MaxFilter(5))
    rim = ImageChops.subtract(rim, a)
    hot = Image.new("RGBA", (w, h), tuple(min(255, c + 90) for c in col) + (0,))
    hot.putalpha(rim)
    layer.alpha_composite(hot)
    layer.alpha_composite(g, (pad, pad))
    return layer


def main():
    for still, kind, cx, cy, width, out in SHOTS:
        base = Image.open(D / "stills" / still).convert("RGBA")
        g = glowing(kind, width)
        base.alpha_composite(g, (cx - g.width // 2, cy - g.height // 2))
        base.convert("RGB").save(D / "stills" / out)
        print(out, base.size)


if __name__ == "__main__":
    main()
