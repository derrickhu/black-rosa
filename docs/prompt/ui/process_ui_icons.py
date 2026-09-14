#!/usr/bin/env python3
"""把 4x4 洋红底图标大图切成 16 张透明 PNG，压平渐变，归一化成统一画布。

为什么不用 rembg：这批图标有很粗的深色描边，rembg 常把描边外沿一起吃掉；
底色是纯洋红，键控更准也更快。

为什么要归一化成正方形画布：UiKit.Icon 用 preserveAspect 渲染，
如果按内容紧裁，宽扁的「横扫」和瘦高的「墨瓶」在同一个 size 下视觉大小会差很多。
统一放进 128x128、内容占 82%，各图标的视觉重量才一致。

    .venv-mock/bin/python docs/prompt/ui/process_ui_icons.py [大图名] [--flat N]

大图名默认 ui_icons_flat3.png。--flat N 指定量化色数，0 表示不压平。
"""
import os
import sys
from collections import Counter

from PIL import Image, ImageFilter

ASSETS = os.environ.get(
    "GAME_ASSETS", "/Users/rosa/rosa_games/game_assets/black-rosa/assets")
SPLIT = os.path.join(ASSETS, "split")
NOBG = os.path.join(ASSETS, "nobg")
FINAL = os.path.join(ASSETS, "final")

CANVAS = 128
CONTENT = 0.82          # 内容占画布比例
COLS, ROWS = 4, 4

NAMES = [
    "ico_damage", "ico_guns", "ico_rate", "ico_gold",
    "ico_hp", "ico_burst", "ico_halt", "ico_rage",
    "ico_sweep", "ico_splash", "ico_mend", "ico_stamina",
    "ico_ink", "ico_star", "ico_energy", "ico_lock",
]

SRC = "ui_icons_flat3.png"
FLAT = 6
CONTACT = "_contact.png"
_args = sys.argv[1:]
if _args and not _args[0].startswith("--"):
    SRC = _args.pop(0)


def _opt(flag, cast=str, default=None):
    if flag not in _args:
        return default
    return cast(_args[_args.index(flag) + 1])


FLAT = _opt("--flat", int, FLAT)
# 底栏图标是单独一张 2x2 的图，网格和名字都和主图标表不一样，
# 所以这三项可以从命令行覆盖，不用再抄一份两百行的切图流程。
COLS = _opt("--cols", int, COLS)
ROWS = _opt("--rows", int, ROWS)
_names = _opt("--names", str)
if _names:
    NAMES = _names.split(",")
CONTACT = _opt("--contact", str, CONTACT)
RAW = os.path.join(ASSETS, "raw", SRC)


def bg_color(img):
    """从四条边采样背景色，用中位数避免个别像素带偏。"""
    w, h = img.size
    px = img.load()
    samples = []
    for x in range(0, w, 4):
        samples.append(px[x, 0][:3])
        samples.append(px[x, h - 1][:3])
    for y in range(0, h, 4):
        samples.append(px[0, y][:3])
        samples.append(px[w - 1, y][:3])
    samples.sort()
    return samples[len(samples) // 2]


def key_out(cell, bg, t0=58.0, t1=104.0):
    """洋红键控。先按色距算 alpha，再腐蚀 1px 去掉彩色毛边，最后轻微羽化。"""
    cell = cell.convert("RGBA")
    w, h = cell.size
    px = cell.load()
    br, bg_, bb = bg
    alpha = Image.new("L", (w, h))
    ap = alpha.load()
    span = max(1.0, t1 - t0)
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            d = ((r - br) ** 2 + (g - bg_) ** 2 + (b - bb) ** 2) ** 0.5
            t = (d - t0) / span
            ap[x, y] = 0 if t <= 0 else (255 if t >= 1 else int(t * 255))
    # 腐蚀一圈去洋红毛边。腐蚀量必须跟着格子尺寸走：2048 的大图每格 512px，
    # 溢色带有好几像素宽，只腐蚀 1px 会把洋红残留留在深描边里 —— 量化时它会被
    # 固化成一个栗红色槽（实测最暗色出现 (124,36,61) 这种 R、B 同时高过 G 的值），
    # 16 张图的描边色就散了。描边本身很厚，按比例掉 2~3px 在成品 128px 上看不出来。
    k = 3 if w < 200 else (5 if w < 400 else 7)
    alpha = alpha.filter(ImageFilter.MinFilter(k))
    alpha = alpha.filter(ImageFilter.GaussianBlur(max(0.6, w / 420.0)))
    cell.putalpha(alpha)
    return cell


def drop_bleed(img, keep_ratio=0.30, thr=40):
    """丢掉邻格越界进来的碎片。

    等分切格时，相邻图标的尖端会越过边界落进本格；归一化按包围盒算，
    碎片会把包围盒撑大，真图标就被缩小还偏心。
    判据：连通块「贴着格子边缘」且「面积远小于主体」才删 —— 真图标四周有
    留白不会碰边，而越界碎片必然碰边。这样又不会误删速度线、定身的小顿点
    那类合法的分离细节（它们不碰边）。
    """
    w, h = img.size
    a = img.split()[3].load()
    label = [[0] * w for _ in range(h)]
    comps = []          # (面积, 是否碰边, 像素表)
    cur = 0
    for sy in range(h):
        for sx in range(w):
            if a[sx, sy] <= thr or label[sy][sx]:
                continue
            cur += 1
            stack = [(sx, sy)]
            label[sy][sx] = cur
            pix = []
            edge = False
            while stack:
                x, y = stack.pop()
                pix.append((x, y))
                if x == 0 or y == 0 or x == w - 1 or y == h - 1:
                    edge = True
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not label[ny][nx] and a[nx, ny] > thr:
                        label[ny][nx] = cur
                        stack.append((nx, ny))
            comps.append((len(pix), edge, pix))
    if not comps:
        return img, 0
    biggest = max(c[0] for c in comps)
    alpha = img.split()[3]
    ap = alpha.load()
    killed = 0
    for area, edge, pix in comps:
        if edge and area < biggest * keep_ratio:
            for x, y in pix:
                ap[x, y] = 0
            killed += 1
    img = img.copy()
    img.putalpha(alpha)
    return img, killed


def flatten(img, colors=FLAT):
    """把柔和渐变压成硬边平涂色块。

    生图模型对球体、圆柱这类有体积感的形状总会偷偷加径向明暗，提示词里
    反复禁止也压不住（试了三版，炮弹和双炮始终是渐变）。与其继续和模型较劲，
    不如后处理里用**无抖动**的调色板量化直接压平 —— 出来就是风格参考板那种
    硬边色阶。这一步在 256px 上做，缩到 128px 时抗锯齿会自然回来，不留锯齿边。
    """
    if colors <= 0:
        return img
    a = img.split()[3]
    rgb = img.convert("RGB")
    px, ap = rgb.load(), a.load()
    # 透明区还留着原来的洋红，直接量化会白占调色板槽位，
    # 先涂成不透明区里最常见的颜色，让它并进已有色槽。
    hist = Counter()
    for y in range(0, img.height, 2):
        for x in range(0, img.width, 2):
            if ap[x, y] > 200:
                hist[px[x, y]] += 1
    fill = hist.most_common(1)[0][0] if hist else (128, 128, 128)
    for y in range(img.height):
        for x in range(img.width):
            if ap[x, y] < 8:
                px[x, y] = fill
    q = rgb.quantize(colors=colors, method=Image.Quantize.MEDIANCUT,
                     dither=Image.Dither.NONE).convert("RGB")
    out = q.convert("RGBA")
    out.putalpha(a)
    return out


def normalize(img):
    """裁到内容包围盒，等比缩放进 CANVAS，居中。"""
    bbox = img.getbbox()
    if bbox is None:
        return Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    art = img.crop(bbox)
    box = int(CANVAS * CONTENT)
    scale = box / max(art.width, art.height)
    art = art.resize((max(1, round(art.width * scale)),
                      max(1, round(art.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    out.paste(art, ((CANVAS - art.width) // 2, (CANVAS - art.height) // 2))
    return out


def main():
    if not os.path.exists(RAW):
        sys.exit("找不到原图：" + RAW)
    for d in (SPLIT, NOBG, FINAL):
        os.makedirs(d, exist_ok=True)

    sheet = Image.open(RAW).convert("RGBA")
    bg = bg_color(sheet)
    print("原图 =", SRC, " 背景色 =", bg, " 尺寸 =", sheet.size, " 量化色数 =", FLAT)
    cw, ch = sheet.width // COLS, sheet.height // ROWS

    done = []
    for i, name in enumerate(NAMES):
        cx, cy = i % COLS, i // COLS
        cell = sheet.crop((cx * cw, cy * ch, (cx + 1) * cw, (cy + 1) * ch))
        cell.save(os.path.join(SPLIT, name + ".png"))
        cut = key_out(cell, bg)
        cut, killed = drop_bleed(cut)
        cut = flatten(cut)
        cut.save(os.path.join(NOBG, name + ".png"))
        final = normalize(cut)
        fp = os.path.join(FINAL, name + ".png")
        final.save(fp, optimize=True)
        done.append((name, os.path.getsize(fp), final.getbbox(), killed))

    total = 0
    for name, size, bbox, killed in done:
        total += size
        tag = f"  清掉越界碎片 {killed}" if killed else ""
        print(f"  {name:<13} {size/1024:6.1f} KB  bbox={bbox}{tag}")
    print(f"共 {len(done)} 张，合计 {total/1024:.0f} KB")

    # 对照表，方便一眼检查有没有抠坏
    pad = 8
    sheet_w = (CANVAS + pad) * COLS + pad
    sheet_h = (CANVAS + pad) * ROWS + pad
    contact = Image.new("RGBA", (sheet_w, sheet_h), (150, 150, 150, 255))
    for i, name in enumerate(NAMES):
        im = Image.open(os.path.join(FINAL, name + ".png"))
        x = pad + (i % COLS) * (CANVAS + pad)
        y = pad + (i // COLS) * (CANVAS + pad)
        contact.alpha_composite(im, (x, y))
    cp = os.path.join(FINAL, CONTACT)
    contact.convert("RGB").save(cp)
    print("对照表 →", cp)


if __name__ == "__main__":
    main()
