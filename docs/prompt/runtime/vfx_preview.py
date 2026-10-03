#!/usr/bin/env python3
"""按代码里的真实常量，离线渲一张等比例预览图。

Unity 编辑器常年只有几 fps（窗口不聚焦基本不推帧），实机截图不好拿，
所以把 InkDot / InkShot 的摆位算式照抄一遍，用真素材在宣纸底上拼出来。
常量改了这里也要改，两边对不上就说明有一处写错了。

关键前提：`InkSprites.Load` 用 ppu = 贴图高建精灵，所以**每张精灵的世界高恒为 1.0**，
缩放值直接就是世界高，和代码里 `0.5f * H * sprite.bounds.size.y` 的算法一致。

跑：/usr/bin/python3 docs/prompt/runtime/vfx_preview.py [dots|shots|all]
"""

import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[3]
V = REPO / "Assets/Resources/Art/Vfx"
ART = REPO / "Assets/Resources/Art"
PPU = 300
PAPER = (0xF4, 0xEF, 0xE4)

# ---- InkDot 的分区常量 ----
FOOT, CROWN, HALO, SIDEX, BURNH, CRUSTH, SLOWW = -0.45, 0.48, 0.56, 0.30, 0.62, 0.58, 0.42
# ---- BattleView：怪的 baseScale = Radius * 2.6 ----
BASE = 0.30 * 2.6
# ---- InkShot ----
PARENT = 0.50
FLAT = {  # 元素: (帧序列, Sink, Scale)
    "fire": ("fire_shot", 0.238, 1.00),
    "ice": ("ice_shot", 0.196, 1.21),
    "water": ("water_shot", 0.019, 0.77),
    "poison": ("poison_shot", 0.233, 0.77),
    "earth": ("earth_shot", 0.116, 0.80),
    "explode": ("explode_shot", 0.007, 0.90),
}
TINT = dict(fire=(0xEB, 0x61, 0x14), ice=(0x3D, 0xA5, 0xD9), poison=(0x6A, 0xA8, 0x4F))


def load(p):
    return Image.open(p).convert("RGBA")


def tinted(im, col, k):
    a = np.asarray(im).astype(float)
    for i in range(3):
        a[..., i] *= (1 - k) + k * col[i] / 255.0
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def faded(im, k):
    a = np.asarray(im).astype(float)
    a[..., 3] *= k
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def place(cv, im, scale, cx, cy):
    """scale = 精灵的世界高（因为精灵世界高恒为 1.0）；cx,cy = 精灵中心的世界坐标"""
    h = max(1, round(scale * PPU))
    w = max(1, round(im.width / im.height * h))
    im = im.resize((w, h), Image.LANCZOS)
    cv.alpha_composite(im, (round(cx * PPU + cv.width / 2 - w / 2),
                            round(cv.height / 2 - cy * PPU - h / 2)))


def dots(cv, x0, layers):
    enemy = load(ART / "walker.png")
    col, k = None, 0.0
    for key, amt in (("burn", 0.22), ("freeze", 0.20), ("poison", 0.18)):
        if key in layers:
            col, k = TINT[{"burn": "fire", "freeze": "ice", "poison": "poison"}[key]], amt
            break
    if "slow" in layers:                                    # 地面，压在身体后面
        place(cv, faded(load(V / "dot_frost_00.png"), 0.95), 0.86 * 105 / 128 * BASE, x0, (FOOT + 0.07) * BASE)
    place(cv, tinted(enemy, col, k) if col else enemy, BASE, x0, 0)

    if "burn" in layers:                                    # 脚底往上到胸
        place(cv, faded(load(V / "burn_body_01.png"), 0.90), BURNH * BASE,
              x0, (FOOT + 0.5 * BURNH) * BASE)
    if "poison" in layers:                                  # 两侧上浮
        b = load(V / "dot_poison.png")
        for i, rise in enumerate((0.15, 0.55, 0.35, 0.75)):
            side = (-1 if i % 2 == 0 else 1) * (SIDEX + 0.04 * (i // 2))
            place(cv, faded(b, 0.92 * (1 - rise * 0.7)), 0.17 * BASE * (1 - rise * 0.3),
                  x0 + side * BASE, (-0.05 + rise * 0.46) * BASE)
    if "freeze" in layers:                                  # 头顶往下
        place(cv, faded(load(V / "ice_crust_01.png"), 0.92), CRUSTH * BASE,
              x0, (CROWN - 0.5 * CRUSTH) * BASE)
    if "stun" in layers:                                    # 头顶之上
        place(cv, faded(load(V / "dot_stun.png"), 0.95), 0.46 * BASE, x0, HALO * BASE)
    if "confuse" in layers:
        place(cv, faded(load(V / "dot_confuse.png"), 0.92), 0.34 * BASE, x0, HALO * BASE)


def draw_dots():
    cases = [([], ""), (["burn"], ""), (["freeze"], ""), (["poison"], ""), (["slow"], ""),
             (["stun"], ""), (["confuse"], ""), (["burn", "freeze"], ""),
             (["burn", "poison", "slow", "stun"], ""), (["freeze", "poison", "slow"], "")]
    cv = Image.new("RGBA", (int(1.22 * PPU * len(cases)), int(1.75 * PPU)), PAPER + (255,))
    for i, (ls, _) in enumerate(cases):
        dots(cv, (i - (len(cases) - 1) / 2) * 1.22, ls)
    cv.convert("RGB").save("/tmp/chk_dots.png")
    print("状态分区图 -> /tmp/chk_dots.png")
    print("  原样 | 烧 | 冻 | 毒x4 | 缓 | 晕 | 惑 | 烧+冻(霜火) | 烧+毒+缓+晕 | 冻+毒+缓")


def draw_shots():
    """三档星各取窗口首帧，和一只同尺寸的怪并排当比例尺。"""
    rows = []
    for name, (frames, sink, scale) in FLAT.items():
        h = int(0.62 * PPU)
        cv = Image.new("RGBA", (int(0.62 * PPU * 7), h), PAPER + (255,))
        sh = PARENT * scale
        for k, fr in enumerate((0, 3, 5, 7)):
            place(cv, load(V / f"{frames}_{fr:02d}.png"), sh,
                  (k - 2.4) * 0.40, -sink * sh)
        place(cv, load(ART / "walker.png"), BASE, 1.30, 0)
        rows.append((name, cv))
    W = max(c.width for _, c in rows)
    out = Image.new("RGB", (W, sum(c.height for _, c in rows)), PAPER)
    y = 0
    for _, c in rows:
        out.paste(c, (0, y), c)
        y += c.height
    out.save("/tmp/chk_shots.png")
    print("弹体对照 -> /tmp/chk_shots.png  每行 4 帧(00/03/05/07) + 同尺寸怪")
    print("  行序:", " ".join(FLAT))


PAIRS = {  # 招牌两两：帧序列 -> (Sink, Scale)，照抄 InkVfx.Flat
    "frostfire_shot": (-0.030, 0.70), "scorchbolt_shot": (0.003, 0.76),
    "hailbolt_shot": (-0.069, 0.96), "blightfire_shot": (-0.040, 0.91),
    "conduct_shot": (-0.008, 0.67), "moltengold_shot": (0.108, 0.79),
    "wardgold_shot": (0.076, 0.77), "rotlife_shot": (0.099, 0.79),
    "ramearth_shot": (0.109, 0.70), "coldwind_shot": (0.087, 0.61),
    "blaze_shot": (-0.113, 0.90), "thundercut_shot": (0.103, 1.06),
}
# InkShot.Mote：半径 0.62、缩放 0.22，都是弹体局部单位
MOTE_R, MOTE_S = 0.62, 0.22
ACCENT = dict(fire=(0xEB, 0x61, 0x14), ice=(0x26, 0x9E, 0xDB), water=(0x2A, 0x6F, 0xA8),
              poison=(0x7A, 0x3F, 0xA0), earth=(0x8A, 0x61, 0x34), wind=(0x6F, 0x9C, 0x86),
              thunder=(0x5B, 0x4F, 0xD1), gold=(0xC9, 0x92, 0x2A), wood=(0x4E, 0x7A, 0x33))


def mote_sprite(col):
    """照 InkFx.Mote 烘：实心亮面 + 一圈深色外沿，硬边。"""
    n = 64
    a = np.zeros((n, n, 4), float)
    c = (n - 1) * 0.5
    yy, xx = np.mgrid[0:n, 0:n]
    r = np.sqrt((xx - c) ** 2 + (yy - c) ** 2) / c
    face = r <= 0.70
    rim = (r > 0.70) & (r <= 1.0)
    for i in range(3):
        a[..., i] = np.where(face, col[i], np.where(rim, (70, 55, 45)[i] * col[i] / 255.0, 0))
    a[..., 3] = np.where(face | rim, 255, 0)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def draw_combo():
    """升级阶梯：体槽图没画到的元素各挂一颗小卫星，所以叠得越多挂得越多。"""
    # (标题, 帧序列, 帧号, 小卫星)。尺寸一律从上面那张登记表取，别在这里重抄一遍。
    cases = [
        ("1 元素 火 ★1", "fire_shot", 0, []),
        ("1 元素 火 ★3", "fire_shot", 7, []),
        ("2 元素 火+风", "fire_shot", 7, ["wind"]),
        ("3 元素 火+风+金", "fire_shot", 7, ["wind", "gold"]),
        ("两两 霜火", "frostfire_shot", 0, []),
        ("霜火+雷", "frostfire_shot", 0, ["thunder"]),
        ("两两 燎毒", "blightfire_shot", 0, []),
        ("两两 雷决", "thundercut_shot", 0, []),
    ]
    cell = 0.95
    cv = Image.new("RGBA", (int(cell * PPU * len(cases)), int(cell * PPU)), PAPER + (255,))
    for i, (_, frames, fr, motes) in enumerate(cases):
        if frames in PAIRS:
            sink, scale = PAIRS[frames]
        else:
            _, sink, scale = FLAT[frames.split("_")[0]]
        x0 = (i - (len(cases) - 1) / 2) * cell
        sh = PARENT * scale
        place(cv, load(V / f"{frames}_{fr:02d}.png"), sh, x0, -sink * sh)
        # 两颗都摆在右侧、一上一下，免得画到隔壁格里去认错主
        for k, el in enumerate(motes):
            ang = 0.85 if k == 0 else -0.85
            place(cv, mote_sprite(ACCENT[el]), MOTE_S * PARENT,
                  x0 + math.cos(ang) * MOTE_R * PARENT, math.sin(ang) * MOTE_R * PARENT)
    cv.convert("RGB").save("/tmp/chk_combo.png")
    print("组合升级阶梯 -> /tmp/chk_combo.png")
    print("  " + " | ".join(c[0] for c in cases))


if __name__ == "__main__":
    want = (sys.argv[1] if len(sys.argv) > 1 else "all").lower()
    if want in ("combo", "all"):
        draw_combo()
    if want in ("dots", "all"):
        draw_dots()
    if want in ("shots", "all"):
        draw_shots()
