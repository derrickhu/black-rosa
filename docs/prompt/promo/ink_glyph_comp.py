"""水墨动态漫买量片「一字一炮」首尾帧合字。

战场镜头：墨字外加一圈彩色柔光；写字特写：墨字按正片叠底压进宣纸，像刚写上去。
    .venv-mock/bin/python docs/prompt/promo/ink_glyph_comp.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

SRC = Path("/Users/rosa/.cursor/projects/Users-rosa-rosa-games-black-rosa/assets")
CARD = Path("/Users/rosa/rosa_games/game_assets/black-rosa/美术/runtime/heaps_v9/raw")
HEAP = Path("/Users/rosa/rosa_games/black-rosa/Assets/Resources/Art")
OUT = Path("/Users/rosa/rosa_games/game_assets/black-rosa/推广/宣传视频/ink_01/stills")
GLOW = {"fire": (255, 96, 40), "ice": (90, 190, 255), "thunder": (176, 96, 255)}
STILLS = ["ink_h1_hook", "ink_w_start", "ink_f_end", "ink_i_end", "ink_t_end2", "ink_c_start", "ink_c_end2"]
SKY = (0.62, 0.24, 0.34)
ROW = [("fire", 0.20, 0.22, 0.24), ("ice", 0.50, 0.22, 0.24), ("thunder", 0.80, 0.22, 0.24)]
STACK = [("fire", 0.17, 0.11, 0.19), ("ice", 0.17, 0.23, 0.19), ("thunder", 0.17, 0.35, 0.19)]
PAPER = (0.42, 0.73, 0.46)
COMP = [
    ("ink_h1_hook", ROW, "h1_hook.png", "glow"),
    ("ink_h1_hook", [("fire",) + SKY], "f_start.png", "glow"),
    ("ink_f_end", [("fire",) + SKY], "f_end.png", "glow"),
    ("ink_h1_hook", [("ice",) + SKY], "i_start.png", "glow"),
    ("ink_i_end", [("ice",) + SKY], "i_end.png", "glow"),
    ("ink_h1_hook", [("thunder",) + SKY], "t_start.png", "glow"),
    ("ink_t_end2", [("thunder",) + SKY], "t_end.png", "glow"),
    ("ink_c_start", STACK, "c_start.png", "glow"),
    ("ink_c_end2", STACK, "c_end.png", "glow"),
    ("ink_w_start", [("fire",) + PAPER], "w_fire.png", "paper"),
    ("ink_w_start", [("ice",) + PAPER], "w_ice.png", "paper"),
    ("ink_w_start", [("thunder",) + PAPER], "w_thunder.png", "paper"),
]


def glyph(kind):
    """卡面原图优先（1024），没有就用游戏内 256 的字图；米白底按色差抠透明。"""
    card = CARD / f"card_{kind}.png"
    if card.exists():
        im = Image.open(card).convert("RGB").crop((150, 150, 874, 874))
        bg = (250, 249, 244)
    else:
        im = Image.open(HEAP / f"heap_{kind}.png").convert("RGB")
        bg = im.getpixel((2, 2))
    diff = ImageChops.difference(im, Image.new("RGB", im.size, bg)).convert("L")
    alpha = diff.point(lambda v: 0 if v < 18 else min(255, (v - 18) * 3))
    out = im.convert("RGBA")
    out.putalpha(alpha)
    return out.crop(out.getbbox())


def sized(kind, width):
    g = glyph(kind)
    return g.resize((width, max(1, int(g.height * width / g.width))), Image.LANCZOS)


def glowing(kind, width):
    g = sized(kind, width)
    pad = max(8, width // 3)
    w, h = g.width + pad * 2, g.height + pad * 2
    a = Image.new("L", (w, h), 0)
    a.paste(g.getchannel("A"), (pad, pad))
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for radius, gain in ((pad * 0.55, 1.0), (pad * 0.2, 1.3)):
        blur = a.filter(ImageFilter.GaussianBlur(radius)).point(lambda v, k=gain: min(255, int(v * k)))
        halo = Image.new("RGBA", (w, h), GLOW[kind] + (0,))
        halo.putalpha(blur)
        layer.alpha_composite(halo)
    layer.alpha_composite(g, (pad, pad))
    return layer


def on_paper(base, kind, fx, fy, fw):
    """正片叠底：纸纹透过墨色，笔画边缘再晕一点。"""
    w, h = base.size
    g = sized(kind, int(w * fw))
    x, y = int(w * fx - g.width / 2), int(h * fy - g.height / 2)
    ink = Image.new("RGB", base.size, (255, 255, 255))
    soft = Image.new("RGBA", g.size, (0, 0, 0, 0))
    soft.paste(g, (0, 0))
    soft.putalpha(g.getchannel("A").filter(ImageFilter.GaussianBlur(1.2)))
    ink.paste(soft.convert("RGB"), (x, y), soft.getchannel("A"))
    return ImageChops.multiply(base.convert("RGB"), ink)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name in STILLS:
        Image.open(SRC / f"{name}.jpg").convert("RGB").save(OUT / f"{name}.jpg", quality=92)
    for src, marks, out, mode in COMP:
        base = Image.open(OUT / f"{src}.jpg").convert("RGBA")
        w, h = base.size
        for kind, fx, fy, fw in marks:
            if mode == "paper":
                base = on_paper(base, kind, fx, fy, fw).convert("RGBA")
            else:
                g = glowing(kind, int(w * fw))
                base.alpha_composite(g, (int(w * fx - g.width / 2), int(h * fy - g.height / 2)))
        base.convert("RGB").save(OUT / out)
        print(out, base.size)


if __name__ == "__main__":
    main()
