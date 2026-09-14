#!/usr/bin/env python3
"""渲染首页「炮台」页的四版对比稿：现状 + 三个候选方向。

炮台页是界面元素最密的一屏（顶栏 + 炮台预览 + 皮肤格 + 5 张升级卡 + 底栏），
而顶栏和底栏是每一页共用的常驻框架，所以这一屏定了，整个 UI 就定了。

四版共用同一套几何（见 L），只换材质、配色、字形，保证对比是同一个变量。

    .venv-mock/bin/python docs/prompt/ui/render_home_mocks.py
"""
import math
import os
import random
from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H = 720, 1280
TOP_PAD, BOT_PAD = 36, 24
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "美术", "首页方案")

SONG_BOLD = ("/System/Library/Fonts/Supplemental/Songti.ttc", 1)
SONG_REG = ("/System/Library/Fonts/Supplemental/Songti.ttc", 6)
HEI_MED = ("/System/Library/Fonts/STHeiti Medium.ttc", 1)
HEI_LIGHT = ("/System/Library/Fonts/STHeiti Light.ttc", 1)

_fc = {}


def font(spec, size):
    key = (spec, size)
    if key not in _fc:
        path, idx = spec
        _fc[key] = ImageFont.truetype(path, size, index=idx)
    return _fc[key]


# 现状调色板，取自 Assets/Scripts/UI/InkTheme.cs
PAPER = (244, 239, 228)
PAPER_IN = (252, 252, 246)
INK = (26, 20, 16)
GRAPHITE = (46, 46, 46)
GRAPHITE_HI = (106, 106, 106)
GHOST = (237, 232, 223)
LOCKED = (200, 196, 188)
VERM = (192, 57, 43)       # 朱砂 / 印章红
GOLD = (201, 146, 42)
ACCEL = (60, 184, 106)
HEART = (224, 84, 70)
EXPLODE = (219, 56, 71)


class L:
    """共用几何。y 一律是「距顶栏下沿」的页内坐标。"""
    TOP_Y = TOP_PAD + 18          # 顶栏 chip 上沿
    CHIP_W, CHIP_H = 190, 58
    CHIP_DX = 190
    PAGE_Y = TOP_PAD + 104        # 内容区起点
    PAGE_H = H - (TOP_PAD + 104) - (BOT_PAD + 110)

    PREVIEW_H = 176
    SKIN_Y, SKIN_W, SKIN_H, SKIN_DX = 196, 156, 108, 168
    CARD_Y, CARD_W, CARD_H = 312, 322, 176
    CARD_DX, CARD_DY = 338, 196
    HINT_Y = 906

    TAB_Y = H - BOT_PAD - 10 - 86
    TAB_W, TAB_H, TAB_DX = 222, 86, 232


# 5 条升级线：名字、每级效果、当前等级、最大级、价、元素色
LINES = [
    ("伤害", "炮弹伤害 +8%", 2, 4, "60 墨", EXPLODE),
    ("炮台数", "多一门炮", 1, 2, "第二章开放", GRAPHITE),
    ("射速", "开火间隔 -4%", 1, 3, "40 墨", ACCEL),
    ("开局金币", "开局金币 +2", 3, 3, "已满级", GOLD),
    ("基地生命", "基地生命 +1", 0, 2, "需 10 星", HEART),
]
SKINS = [("素笔", None, "使用中"), ("朱砂", VERM, "点击换上"),
         ("青瓷", (74, 124, 140), "150 墨"), ("鎏金", (232, 180, 58), "通关解锁")]
TABS = ["炮台", "出征", "技能"]
HINT = "墨在通关时结算获得。升级只改开局条件，\n不解锁任何改装字牌。"


def text(d, xy, s, f, fill, anchor="mm"):
    d.text(xy, s, font=f, fill=fill, anchor=anchor)


def rrect(d, box, r, fill=None, outline=None, width=1):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def shadow(img, box, r, blur=10, alpha=52, dy=4, spread=0):
    """柔和投影。UI 有了纵深就不像线框图了。"""
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    x0, y0, x1, y1 = box
    ImageDraw.Draw(lay).rounded_rectangle(
        (x0 - spread, y0 - spread + dy, x1 + spread, y1 + spread + dy),
        radius=r + spread, fill=(20, 16, 12, alpha))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(blur)))


def grain(img, sigma=10, opacity=30):
    """程序化纸纤维。包体零增长 —— 运行时用同样的噪声烘一张 256² 平铺即可。"""
    n = Image.effect_noise((W, H), sigma).convert("L").filter(ImageFilter.GaussianBlur(0.4))
    lay = Image.new("RGBA", (W, H), (120, 104, 82, 0))
    lay.putalpha(n.point(lambda v: int(abs(v - 128) / 128 * opacity)))
    img.alpha_composite(lay)


def wash(img, cx, cy, rad, color, alpha=30):
    """墨渍 / 淡彩晕染。复用 InkFx.BakeRadial 那套径向衰减就能出。"""
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).ellipse((cx - rad, cy - rad, cx_ := cx + rad, cy + rad),
                                fill=color + (alpha,))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(rad * 0.42)))


def _rrect_path(box, r):
    """把圆角矩形拆成 4 段直边 + 4 段圆弧，按等弧长采样成点列（含拐角）。
    必须把圆弧也走进去，否则拐角和直边接不上，会出现斜切的豁口。"""
    x0, y0, x1, y1 = box
    segs = [
        ("L", (x0 + r, y0), (x1 - r, y0)),
        ("A", (x1 - r, y0 + r), -90.0, 0.0),
        ("L", (x1, y0 + r), (x1, y1 - r)),
        ("A", (x1 - r, y1 - r), 0.0, 90.0),
        ("L", (x1 - r, y1), (x0 + r, y1)),
        ("A", (x0 + r, y1 - r), 90.0, 180.0),
        ("L", (x0, y1 - r), (x0, y0 + r)),
        ("A", (x0 + r, y0 + r), 180.0, 270.0),
    ]
    pts, corner = [], []
    for s in segs:
        if s[0] == "L":
            (ax, ay), (bx, by) = s[1], s[2]
            ln = math.hypot(bx - ax, by - ay)
            n = max(2, int(ln))
            for i in range(n):
                f = i / n
                pts.append((ax + (bx - ax) * f, ay + (by - ay) * f))
                corner.append(0.0)
        else:
            (cx, cy), a0, a1 = s[1], s[2], s[3]
            ln = abs(math.radians(a1 - a0)) * r
            n = max(3, int(ln))
            for i in range(n):
                a = math.radians(a0 + (a1 - a0) * i / n)
                pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
                corner.append(1.0)
    return pts, corner


def brush_rrect(img, box, r, stroke, width=3.0, rough=0.42, seed=7):
    """笔触描边：一支笔尖沿圆角路径走一圈。
    线宽用两条低频正弦叠加来平滑起伏（随机逐段抖动只会出噪点，不像笔画），
    拐角处再加粗一点模拟转笔时的积墨。"""
    rnd = random.Random(seed)
    p1, p2 = rnd.random() * 6.283, rnd.random() * 6.283
    pts, corner = _rrect_path(box, r)
    n = len(pts)
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    for i, (px, py) in enumerate(pts):
        t = i / n
        swell = 0.62 * math.sin(6.283 * 2.0 * t + p1) + 0.38 * math.sin(6.283 * 5.0 * t + p2)
        w = width * (1.0 + rough * swell) * (1.0 + 0.22 * corner[i])
        w = max(1.2, w)
        a = int(214 + 41 * (0.5 + 0.5 * math.sin(6.283 * 3.0 * t + p2)))
        rr = w * 0.5
        d.ellipse((px - rr, py - rr, px + rr, py + rr), fill=stroke + (min(255, a),))
    img.alpha_composite(lay)


def cannon(d, cx, cy, s, color):
    """炮台剪影，形状同 InkShape.Cannon：一根炮管 + 一个底座。"""
    d.rounded_rectangle((cx - 0.22 * s, cy - 0.92 * s, cx + 0.22 * s, cy + 0.18 * s),
                        radius=0.08 * s, fill=color)
    d.rounded_rectangle((cx - 0.58 * s, cy + 0.06 * s, cx + 0.58 * s, cy + 0.70 * s),
                        radius=0.16 * s, fill=color)


def star(d, cx, cy, r, color):
    pts = []
    for i in range(10):
        a = math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.44
        pts.append((cx + math.cos(a) * rr, cy - math.sin(a) * rr))
    d.polygon(pts, fill=color)


def diamond(d, cx, cy, r, fill=None, outline=None, w=2):
    p = [(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)]
    if fill:
        d.polygon(p, fill=fill)
    if outline:
        d.line(p + [p[0]], fill=outline, width=w)


def card_slots():
    """5 张升级卡的中心点。"""
    for i in range(5):
        cx = W // 2 + (-1 if i % 2 == 0 else 1) * L.CARD_DX // 2
        cy = L.PAGE_Y + L.CARD_Y + (i // 2) * L.CARD_DY
        yield i, cx, cy


# ---------------------------------------------------------------- 现状
def render_current():
    img = Image.new("RGBA", (W, H), PAPER + (255,))
    d = ImageDraw.Draw(img)
    f26, f18, f22 = font(HEI_LIGHT, 26), font(HEI_LIGHT, 18), font(HEI_LIGHT, 22)

    top = L.TOP_Y
    for x, icon, val in ((-L.CHIP_DX, "flame", "8/12"), (0, "circle", "1240"), (L.CHIP_DX, "star", "18")):
        cx = W // 2 + x
        if icon == "flame":
            d.polygon([(cx - 62, top + 46), (cx - 46, top + 8), (cx - 30, top + 46)], fill=GRAPHITE_HI)
        elif icon == "circle":
            d.ellipse((cx - 66, top + 9, cx - 26, top + 49), fill=GRAPHITE_HI)
        else:
            star(d, cx - 46, top + 29, 21, GRAPHITE_HI)
        text(d, (cx - 14, top + 29), val, f26, INK, "lm")
    text(d, (W // 2 - L.CHIP_DX, top + 72), "12:34 后 +1", f18, GRAPHITE_HI)

    py = L.PAGE_Y
    cannon(d, W // 2, py + 80, 104, INK)
    text(d, (W // 2, py + 172), "素笔", font(HEI_LIGHT, 24), INK)

    for i, (name, tint, tail) in enumerate(SKINS):
        cx = W // 2 + int((i - 1.5) * L.SKIN_DX)
        box = (cx - L.SKIN_W // 2, py + L.SKIN_Y, cx + L.SKIN_W // 2, py + L.SKIN_Y + L.SKIN_H)
        owned = i < 2
        d.rectangle(box, fill=(INK if owned else LOCKED))
        ins = 6 if i == 0 else 3
        d.rectangle((box[0] + ins, box[1] + ins, box[2] - ins, box[3] - ins), fill=PAPER_IN)
        if tint:
            d.ellipse((cx - 20, box[1] + 14, cx + 20, box[1] + 54), fill=tint)
        else:
            text(d, (cx, box[1] + 34), "无", font(HEI_LIGHT, 22), INK)
        text(d, (cx, box[1] + 68), name, font(HEI_LIGHT, 19), GRAPHITE)
        text(d, (cx, box[1] + 90), tail, font(HEI_LIGHT, 16), GRAPHITE_HI)

    for i, cx, cy in card_slots():
        name, step, lv, mx, price, _ = LINES[i]
        box = (cx - L.CARD_W // 2, cy, cx + L.CARD_W // 2, cy + L.CARD_H)
        live = price.endswith("墨")
        d.rectangle(box, fill=(INK if live else LOCKED))
        d.rectangle((box[0] + 5, box[1] + 5, box[2] - 5, box[3] - 5), fill=PAPER_IN)
        d.rectangle((cx - 138, cy + 30, cx - 102, cy + 66), fill=GRAPHITE_HI)
        text(d, (cx - 88, cy + 48), name, font(HEI_LIGHT, 28), INK, "lm")
        text(d, (cx - 138, cy + 96), step, font(HEI_LIGHT, 19), GRAPHITE_HI, "lm")
        for k in range(mx):
            diamond(d, cx - 128 + k * 24, cy + 138, 8, fill=(INK if k < lv else LOCKED))
        text(d, (cx + 146, cy + 138), price, f22, INK if live else GRAPHITE_HI, "rm")

    text(d, (W // 2, py + L.HINT_Y), HINT, font(HEI_LIGHT, 20), GRAPHITE_HI)

    for i, t in enumerate(TABS):
        cx = W // 2 + int((i - 1) * L.TAB_DX)
        box = (cx - L.TAB_W // 2, L.TAB_Y, cx + L.TAB_W // 2, L.TAB_Y + L.TAB_H)
        on = i == 0
        d.rectangle(box, fill=INK)
        if not on:
            d.rectangle((box[0] + 4, box[1] + 4, box[2] - 4, box[3] - 4), fill=GHOST)
        text(d, (cx, L.TAB_Y + L.TAB_H // 2), t, font(HEI_MED if on else HEI_LIGHT, 29),
             PAPER if on else GRAPHITE)
    return img


# ---------------------------------------------------------------- A 纸墨 + 朱红
def render_a():
    img = Image.new("RGBA", (W, H), PAPER + (255,))
    wash(img, 80, 130, 320, (142, 128, 102), 30)
    wash(img, 670, 1160, 360, (142, 128, 102), 24)
    grain(img, 11, 34)

    top = L.TOP_Y
    for x, kind, val in ((-L.CHIP_DX, "stam", "8/12"), (0, "ink", "1240"), (L.CHIP_DX, "star", "18")):
        cx = W // 2 + x
        box = (cx - L.CHIP_W // 2, top, cx + L.CHIP_W // 2, top + L.CHIP_H)
        shadow(img, box, L.CHIP_H // 2, 9, 36, 3)
        d = ImageDraw.Draw(img)
        rrect(d, box, L.CHIP_H // 2, fill=PAPER_IN)
        brush_rrect(img, box, L.CHIP_H // 2, INK, 2.0, 0.6, seed=40 + x)
        d = ImageDraw.Draw(img)
        if kind == "stam":
            d.ellipse((cx - 76, top + 16, cx - 48, top + 44), fill=VERM)
        elif kind == "ink":
            d.ellipse((cx - 76, top + 15, cx - 46, top + 45), fill=INK)
        else:
            star(d, cx - 61, top + 29, 18, VERM)
        text(d, (cx - 30, top + 30), val, font(HEI_MED, 28), INK, "lm")
    d = ImageDraw.Draw(img)
    text(d, (W // 2 - L.CHIP_DX, top + 74), "12:34 后 +1", font(HEI_LIGHT, 18), GRAPHITE_HI)

    py = L.PAGE_Y
    # 炮台预览：淡墨圆盘托底。右上角盖一枚朱印，像卷轴上画家的落款。
    wash(img, W // 2, py + 76, 112, (118, 106, 86), 52)
    d = ImageDraw.Draw(img)
    cannon(d, W // 2, py + 80, 104, INK)
    sb = (W // 2 + 118, py + 26, W // 2 + 166, py + 74)
    rrect(d, sb, 6, fill=VERM)
    text(d, ((sb[0] + sb[2]) // 2, (sb[1] + sb[3]) // 2 - 1), "墨", font(SONG_BOLD, 33), PAPER_IN)
    text(d, (W // 2, py + 172), "素笔", font(SONG_BOLD, 27), INK)

    for i, (name, tint, tail) in enumerate(SKINS):
        cx = W // 2 + int((i - 1.5) * L.SKIN_DX)
        box = (cx - L.SKIN_W // 2, py + L.SKIN_Y, cx + L.SKIN_W // 2, py + L.SKIN_Y + L.SKIN_H)
        owned = i < 2
        shadow(img, box, 17, 10, 42 if owned else 22, 4)
        d = ImageDraw.Draw(img)
        rrect(d, box, 17, fill=PAPER_IN)
        brush_rrect(img, box, 17, VERM if i == 0 else (INK if owned else LOCKED),
                    3.2 if i == 0 else 2.0, seed=11 + i)
        d = ImageDraw.Draw(img)
        if tint:
            d.ellipse((cx - 21, box[1] + 14, cx + 21, box[1] + 56), fill=tint)
        else:
            text(d, (cx, box[1] + 35), "无", font(SONG_REG, 25), INK)
        text(d, (cx, box[1] + 70), name, font(SONG_BOLD, 21), INK)
        text(d, (cx, box[1] + 92), tail, font(HEI_LIGHT, 15), GRAPHITE_HI)

    for i, cx, cy in card_slots():
        name, step, lv, mx, price, _ = LINES[i]
        box = (cx - L.CARD_W // 2, cy, cx + L.CARD_W // 2, cy + L.CARD_H)
        live = price.endswith("墨")
        shadow(img, box, 19, 12, 48 if live else 26, 5)
        d = ImageDraw.Draw(img)
        rrect(d, box, 19, fill=PAPER_IN)
        brush_rrect(img, box, 19, INK if live else LOCKED, 2.8 if live else 1.8, seed=3 + i)
        d = ImageDraw.Draw(img)
        d.ellipse((cx - 140, cy + 28, cx - 98, cy + 70), fill=INK if live else LOCKED)
        text(d, (cx - 84, cy + 49), name, font(SONG_BOLD, 30), INK, "lm")
        text(d, (cx - 138, cy + 98), step, font(HEI_LIGHT, 19), GRAPHITE_HI, "lm")
        for k in range(mx):
            px = cx - 126 + k * 26
            if k < lv:
                diamond(d, px, cy + 140, 9, fill=INK)
            else:
                diamond(d, px, cy + 140, 9, outline=LOCKED, w=2)
        text(d, (cx + 144, cy + 140), price, font(HEI_MED if live else HEI_LIGHT, 23),
             VERM if live else GRAPHITE_HI, "rm")

    d = ImageDraw.Draw(img)
    text(d, (W // 2, py + L.HINT_Y), HINT, font(SONG_REG, 21), GRAPHITE_HI)

    for i, t in enumerate(TABS):
        cx = W // 2 + int((i - 1) * L.TAB_DX)
        box = (cx - L.TAB_W // 2, L.TAB_Y, cx + L.TAB_W // 2, L.TAB_Y + L.TAB_H)
        on = i == 0
        shadow(img, box, 21, 11, 62 if on else 26, 5)
        d = ImageDraw.Draw(img)
        rrect(d, box, 21, fill=VERM if on else PAPER_IN)
        if not on:
            brush_rrect(img, box, 21, INK, 2.0, seed=31 + i)
        d = ImageDraw.Draw(img)
        text(d, (cx, L.TAB_Y + L.TAB_H // 2), t, font(SONG_BOLD, 32), PAPER_IN if on else INK)
    return img


# ---------------------------------------------------------------- B 纸墨 + 界面上色
def render_b():
    img = Image.new("RGBA", (W, H), PAPER + (255,))
    wash(img, 80, 130, 320, (142, 128, 102), 24)
    grain(img, 11, 28)

    top = L.TOP_Y
    for x, col, val in ((-L.CHIP_DX, ACCEL, "8/12"), (0, INK, "1240"), (L.CHIP_DX, GOLD, "18")):
        cx = W // 2 + x
        box = (cx - L.CHIP_W // 2, top, cx + L.CHIP_W // 2, top + L.CHIP_H)
        shadow(img, box, L.CHIP_H // 2, 9, 36, 3)
        d = ImageDraw.Draw(img)
        rrect(d, box, L.CHIP_H // 2, fill=PAPER_IN, outline=col, width=3)
        d.ellipse((cx - 76, top + 15, cx - 46, top + 45), fill=col)
        text(d, (cx - 30, top + 30), val, font(HEI_MED, 28), INK, "lm")
    d = ImageDraw.Draw(img)
    text(d, (W // 2 - L.CHIP_DX, top + 74), "12:34 后 +1", font(HEI_LIGHT, 18), GRAPHITE_HI)

    py = L.PAGE_Y
    wash(img, W // 2, py + 76, 112, (90, 150, 172), 60)
    d = ImageDraw.Draw(img)
    cannon(d, W // 2, py + 80, 104, INK)
    text(d, (W // 2, py + 172), "素笔", font(SONG_BOLD, 27), INK)

    for i, (name, tint, tail) in enumerate(SKINS):
        cx = W // 2 + int((i - 1.5) * L.SKIN_DX)
        box = (cx - L.SKIN_W // 2, py + L.SKIN_Y, cx + L.SKIN_W // 2, py + L.SKIN_Y + L.SKIN_H)
        owned = i < 2
        col = tint or GRAPHITE
        shadow(img, box, 17, 10, 40 if owned else 22, 4)
        d = ImageDraw.Draw(img)
        rrect(d, box, 17, fill=PAPER_IN, outline=col if owned else LOCKED, width=3)
        if tint:
            d.ellipse((cx - 21, box[1] + 14, cx + 21, box[1] + 56), fill=tint)
        else:
            text(d, (cx, box[1] + 35), "无", font(SONG_REG, 25), INK)
        text(d, (cx, box[1] + 70), name, font(SONG_BOLD, 21), INK)
        text(d, (cx, box[1] + 92), tail, font(HEI_LIGHT, 15), GRAPHITE_HI)

    for i, cx, cy in card_slots():
        name, step, lv, mx, price, col = LINES[i]
        box = (cx - L.CARD_W // 2, cy, cx + L.CARD_W // 2, cy + L.CARD_H)
        live = price.endswith("墨")
        use = col if live else LOCKED
        shadow(img, box, 19, 12, 48 if live else 26, 5)
        d = ImageDraw.Draw(img)
        rrect(d, box, 19, fill=PAPER_IN, outline=use, width=3)
        d.rounded_rectangle((box[0] + 3, cy + 3, box[0] + 18, cy + L.CARD_H - 3), radius=8, fill=use)
        d.ellipse((cx - 136, cy + 28, cx - 94, cy + 70), fill=use)
        text(d, (cx - 80, cy + 49), name, font(SONG_BOLD, 30), INK, "lm")
        text(d, (cx - 132, cy + 98), step, font(HEI_LIGHT, 19), GRAPHITE_HI, "lm")
        for k in range(mx):
            px = cx - 120 + k * 26
            if k < lv:
                diamond(d, px, cy + 140, 9, fill=use)
            else:
                diamond(d, px, cy + 140, 9, outline=LOCKED, w=2)
        text(d, (cx + 144, cy + 140), price, font(HEI_MED if live else HEI_LIGHT, 23),
             use if live else GRAPHITE_HI, "rm")

    d = ImageDraw.Draw(img)
    text(d, (W // 2, py + L.HINT_Y), HINT, font(SONG_REG, 21), GRAPHITE_HI)

    for i, t in enumerate(TABS):
        cx = W // 2 + int((i - 1) * L.TAB_DX)
        box = (cx - L.TAB_W // 2, L.TAB_Y, cx + L.TAB_W // 2, L.TAB_Y + L.TAB_H)
        on = i == 0
        shadow(img, box, 21, 11, 62 if on else 26, 5)
        d = ImageDraw.Draw(img)
        rrect(d, box, 21, fill=EXPLODE if on else PAPER_IN,
              outline=None if on else GRAPHITE, width=2)
        text(d, (cx, L.TAB_Y + L.TAB_H // 2), t, font(SONG_BOLD, 32), PAPER_IN if on else INK)
    return img


# ---------------------------------------------------------------- C 主流休闲卡片风
OUTL = (58, 42, 32)
CARD = (255, 255, 255)
C_ORANGE = (245, 138, 52)
C_TEAL = (54, 176, 176)
C_PURPLE = (140, 110, 220)
C_RED = (232, 78, 78)
C_GOLD = (246, 190, 60)
CCOL = [C_RED, C_PURPLE, C_TEAL, C_GOLD, C_ORANGE]


def render_c():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for i in range(H):
        t = i / H
        d.line([(0, i), (W, i)],
               fill=(int(255 - 16 * t), int(238 - 28 * t), int(210 - 18 * t), 255))
    wash(img, 110, 110, 280, (255, 190, 120), 95)
    wash(img, 630, 1080, 320, (255, 168, 148), 75)

    def chunky(box, r, top_col, bot_col, lift=9, ow=5):
        """立体按钮：底边压深一档模拟厚度。休闲小游戏的标准做法。"""
        x0, y0, x1, y1 = box
        shadow(img, box, r, 13, 95, 8)
        dd = ImageDraw.Draw(img)
        dd.rounded_rectangle((x0, y0 + lift, x1, y1), radius=r, fill=bot_col, outline=OUTL, width=ow)
        dd.rounded_rectangle((x0, y0, x1, y1 - lift), radius=r, fill=top_col, outline=OUTL, width=ow)
        return dd

    top = L.TOP_Y - 6
    for x, col, val, ic in ((-L.CHIP_DX, C_TEAL, "8/12", "flame"), (0, C_PURPLE, "1240", "ink"),
                            (L.CHIP_DX, C_GOLD, "18", "star")):
        cx = W // 2 + x
        box = (cx - L.CHIP_W // 2, top, cx + L.CHIP_W // 2, top + 64)
        shadow(img, box, 32, 11, 85, 6)
        d = ImageDraw.Draw(img)
        d.rounded_rectangle(box, radius=32, fill=CARD, outline=OUTL, width=5)
        d.ellipse((cx - 88, top + 7, cx - 38, top + 57), fill=col, outline=OUTL, width=4)
        if ic == "star":
            star(d, cx - 63, top + 32, 15, CARD)
        elif ic == "ink":
            d.ellipse((cx - 73, top + 22, cx - 53, top + 42), fill=CARD)
        else:
            d.polygon([(cx - 74, top + 45), (cx - 63, top + 17), (cx - 52, top + 45)], fill=CARD)
        text(d, (cx - 22, top + 32), val, font(HEI_MED, 31), OUTL, "lm")
    d = ImageDraw.Draw(img)
    text(d, (W // 2 - L.CHIP_DX, top + 82), "12:34 后 +1", font(HEI_MED, 19), (152, 114, 86))

    py = L.PAGE_Y
    pbox = (W // 2 - 158, py, W // 2 + 158, py + L.PREVIEW_H)
    shadow(img, pbox, 30, 15, 100, 10)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(pbox, radius=30, fill=CARD, outline=OUTL, width=5)
    d.ellipse((W // 2 - 66, py + 22, W // 2 + 66, py + 128), fill=(255, 231, 196))
    cannon(d, W // 2, py + 80, 98, OUTL)
    text(d, (W // 2, py + 158), "素笔", font(HEI_MED, 26), OUTL)

    for i, (name, tint, tail) in enumerate(SKINS):
        cx = W // 2 + int((i - 1.5) * L.SKIN_DX)
        box = (cx - L.SKIN_W // 2, py + L.SKIN_Y, cx + L.SKIN_W // 2, py + L.SKIN_Y + L.SKIN_H)
        owned = i < 2
        shadow(img, box, 24, 12, 85 if owned else 45, 7)
        d = ImageDraw.Draw(img)
        d.rounded_rectangle(box, radius=24, fill=CARD if owned else (238, 231, 220),
                            outline=OUTL if owned else (170, 160, 148), width=5)
        col = tint or (158, 152, 144)
        d.ellipse((cx - 26, box[1] + 12, cx + 26, box[1] + 64), fill=col, outline=OUTL, width=4)
        text(d, (cx, box[1] + 86), name, font(HEI_MED, 21), OUTL)

    for i, cx, cy in card_slots():
        name, step, lv, mx, price, _ = LINES[i]
        box = (cx - L.CARD_W // 2, cy, cx + L.CARD_W // 2, cy + L.CARD_H)
        live = price.endswith("墨")
        col = CCOL[i] if live else (176, 168, 158)
        shadow(img, box, 26, 13, 92 if live else 48, 8)
        d = ImageDraw.Draw(img)
        d.rounded_rectangle(box, radius=26, fill=CARD if live else (239, 233, 223),
                            outline=OUTL if live else (172, 162, 150), width=5)
        d.rounded_rectangle((box[0] + 14, cy + 18, box[0] + 88, cy + 92), radius=20,
                            fill=col, outline=OUTL, width=4)
        text(d, (cx - 58, cy + 44), name, font(HEI_MED, 28), OUTL, "lm")
        text(d, (cx - 58, cy + 78), step, font(HEI_MED, 18), (142, 128, 114), "lm")
        for k in range(mx):
            px = cx - 126 + k * 28
            d.ellipse((px - 10, cy + 128, px + 10, cy + 148),
                      fill=col if k < lv else (229, 223, 213), outline=OUTL, width=3)
        pb = (cx + 34, cy + 118, cx + 150, cy + 158)
        if live:
            chunky(pb, 17, C_GOLD, (190, 118, 30), lift=5, ow=4)
            d = ImageDraw.Draw(img)
        text(d, ((pb[0] + pb[2]) // 2, (pb[1] + pb[3]) // 2 - 3), price,
             font(HEI_MED, 20), OUTL if live else (152, 142, 132))

    d = ImageDraw.Draw(img)
    text(d, (W // 2, py + L.HINT_Y), HINT, font(HEI_MED, 20), (150, 116, 90))

    for i, t in enumerate(TABS):
        cx = W // 2 + int((i - 1) * L.TAB_DX)
        box = (cx - L.TAB_W // 2, L.TAB_Y, cx + L.TAB_W // 2, L.TAB_Y + L.TAB_H)
        on = i == 0
        if on:
            d = chunky(box, 27, C_ORANGE, (196, 96, 24))
        else:
            d = chunky(box, 27, CARD, (226, 218, 206))
        text(d, (cx, L.TAB_Y + L.TAB_H // 2 - 4), t, font(HEI_MED, 32), CARD if on else OUTL)
    return img


def label(img, tag, note):
    """底部贴方案标签，避免遮住顶栏。"""
    d = ImageDraw.Draw(img)
    d.rectangle((0, H - 46, W, H), fill=(18, 14, 11, 240))
    text(d, (16, H - 23), tag, font(HEI_MED, 24), (255, 255, 255), "lm")
    text(d, (W - 16, H - 22), note, font(HEI_LIGHT, 18), (206, 198, 188), "rm")
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    jobs = [
        ("0_现状.png", render_current, "现状", "纯色纸 · 直角矩形框 · 黑体 Light"),
        ("A_纸墨朱红.png", render_a, "A 纸墨 + 朱红", "纸纹 · 笔触圆角 · 投影 · 宋体"),
        ("B_界面上色.png", render_b, "B 纸墨 + 界面上色", "每条线吃元素色 · 同工艺"),
        ("C_主流休闲.png", render_c, "C 主流休闲卡片", "厚描边 · 饱和色 · 立体按钮"),
    ]
    paths = []
    for name, fn, tag, note in jobs:
        img = label(fn(), tag, note)
        p = os.path.abspath(os.path.join(OUT, name))
        img.convert("RGB").save(p, quality=95)
        paths.append(p)
        print("wrote", p)

    gap = 14
    sheet = Image.new("RGB", (W * 4 + gap * 5, H + gap * 2), (232, 232, 232))
    for i, p in enumerate(paths):
        sheet.paste(Image.open(p), (gap + i * (W + gap), gap))
    sp = os.path.abspath(os.path.join(OUT, "对比_四联.png"))
    sheet.save(sp, quality=92)
    print("wrote", sp)


if __name__ == "__main__":
    main()
