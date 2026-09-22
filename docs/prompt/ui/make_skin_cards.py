#!/usr/bin/env python3
"""炮台皮肤卡。按词条胶囊的做法画：白边、酱油描边、瓷面、底唇。

中心是纯色，圆角和唇都收在四边里，方便九宫格拉到 224×124。
九宫格（左、下、右、上）= 30, 34, 30, 30。
"""
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
DEST = os.path.normpath(os.path.join(HERE, "..", "..", "..", "Assets", "Resources", "Art", "Ui"))

# 成图像素。四倍画再缩小，圆角有一像素抗锯齿，平涂区仍然是硬边。
W, H = 240, 156
SCALE = 4
PAD, RAD, RIM, OUT, LIP = 2, 26, 3, 7, 10
# 和 HomeScreen.SkinSlice 对齐：左、下、右、上
SLICE = (30, 34, 30, 30)

SOY = (58, 42, 32)
SEAL = (192, 57, 43)
WHITE = (255, 255, 255)


def mask(size, box, radius):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle(box, radius=radius, fill=255)
    return m


def inset(box, n):
    return (box[0] + n, box[1] + n, box[2] - n, box[3] - n)


def paint(base, m, color):
    layer = Image.new("RGBA", base.size, color + (255,))
    layer.putalpha(m)
    return Image.alpha_composite(base, layer)


def card(face, top, outline, lip):
    sw, sh = W * SCALE, H * SCALE
    im = Image.new("RGBA", (sw, sh), (0, 0, 0, 0))
    s = SCALE
    rim_box = (PAD * s, PAD * s, sw - 1 - PAD * s, sh - 1 - PAD * s)
    rad = RAD * s
    rim = mask((sw, sh), rim_box, rad)
    line = mask((sw, sh), inset(rim_box, RIM * s), rad - RIM * s)
    inner_box = inset(rim_box, (RIM + OUT) * s)
    inner_rad = max(4 * s, rad - (RIM + OUT) * s)
    inner = mask((sw, sh), inner_box, inner_rad)
    face_box = (inner_box[0], inner_box[1], inner_box[2], inner_box[3] - LIP * s)
    face_m = mask((sw, sh), face_box, inner_rad)
    # 顶上一条白瓷沿，高度收进上边九宫格，拉高时不会被拉变形。
    shelf_box = (face_box[0], face_box[1], face_box[2], face_box[1] + 8 * s)
    shelf = mask((sw, sh), shelf_box, inner_rad)
    im = paint(im, rim, WHITE)
    im = paint(im, line, outline)
    im = paint(im, inner, lip)
    im = paint(im, face_m, face)
    im = paint(im, shelf, top)
    return im.resize((W, H), Image.Resampling.LANCZOS)


def main():
    os.makedirs(DEST, exist_ok=True)
    specs = {
        "panel_skin": ((255, 250, 244), WHITE, SOY, (196, 186, 174)),
        "panel_skin_on": ((255, 244, 232), WHITE, SEAL, (226, 176, 156)),
        "panel_skin_dim": ((232, 226, 216), (244, 240, 234), (142, 132, 120), (196, 188, 176)),
    }
    for name, colors in specs.items():
        card(*colors).save(os.path.join(DEST, name + ".png"))
    print("slice", SLICE)
    print("wrote", DEST)


if __name__ == "__main__":
    main()
