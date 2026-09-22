#!/usr/bin/env python3
"""把 NB2 出的平涂特效白底表切成对齐的帧，去背后写进 Assets/Resources/Art/Vfx。

和 vfx_crush.py 的黑底加法流不同：§4.0 的平涂图是「硬边 + 深色外沿」的普通混合图，
白底不能按颜色抠（好几种弹的核心本身就是白的），所以从画布四边洪泛填充 ——
深色外沿会挡住填充，被包住的亮核因此留得下来。

三条要紧的规矩，都是踩过才定的：

1. **对齐**。一套帧必须共用同一个裁切窗口，各自裁自己的包围盒会让效果整体乱跳。
2. **锚点**。弹体按「最宽那一段的中心」水平对称居中，那是头部所在；
   弹心因此落在精灵中心偏上一个固定比例，代码按 Sink 把它沉下去，
   使可见的弹头正好压在真正的子弹位置上，尾巴甩在后面。
   （不要用「找白色核心」来定锚点：毒的核是浅黄绿、炸的亮核在球顶，都会偏。）
3. **挖洞**。环 / 水纹这类图中间是被包住的白，洪泛进不去，会留成实心白块。
   所以 holes=True 时把「被包住且够大」的白块也清掉，小块（白热核、气泡高光）留着。

跑：/usr/bin/python3 docs/prompt/runtime/vfx_flat_slice.py [任务名 ...]
不给任务名就全跑。
"""

import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[3]
RAW = REPO / "art_src/vfx/raw"
MASTER = REPO / "art_src/vfx/fire_v2"
OUT = REPO / "Assets/Resources/Art/Vfx"

BG_MIN = 232      # 三通道都不低于这个值才算「可能是白底」
HOLE_MIN = 0.06   # 被包住的白块超过主体包围盒这个比例才当洞挖掉

# 最长边输出多少像素。相机半高 7.4 → 全高 14.8 世界单位，1280px 屏上只有
# 86 px/世界单位：元素弹在屏上就 12～22 px，词组弹 45 px，256² 是过采样十倍。
# 128 对最大的机型仍有 2 倍富余，显存省掉 4 倍。
SIZE = 128
# 但 `InkSprites.Load` 是 `ppu = Max(tex.height, 64)` —— 贴图高一旦掉到 64 以下，
# 精灵世界高就不再是 1.0，登记表里的 Scale 会连带缩水。
# 水纹这种扁图（256x88 缩到 128 只剩 44 高）正好中招，所以给高度留个下限。
MIN_H = 64

# ---- 作业表 ----------------------------------------------------------------
# mode ball：弹体 8 帧渐变，按头部中心水平居中，共用窗口，回报 Sink
# mode hang：从上往下挂的状态层，共用包围盒，顶边对齐
# mode marks：四个互不相干的记号，各自裁各自的，可挖洞
JOBS = {
    # 火是第一版就出好的，留在表里以便一条命令重跑全套
    "fire_shot":    dict(src="fire_v2_ball", mode="ball", cols=4, rows=2),
    "ice_shot":     dict(src="v2_shot_ice", mode="ball", cols=4, rows=2),
    "poison_shot":  dict(src="v2_shot_poison", mode="ball", cols=4, rows=2),
    # NB2 把这两张排成了 4x4，只用了第 0、2 行
    "water_shot":   dict(src="v2_shot_water", mode="ball", cols=4, rows=4,
                         pick=[0, 1, 2, 3, 8, 9, 10, 11]),
    "explode_shot": dict(src="v2_shot_explode", mode="ball", cols=4, rows=4,
                         pick=[0, 1, 2, 3, 8, 9, 10, 11]),
    # 土出成了 16 级渐变，但不是严格递增。这 8 格是量过的：弹头宽 160→307 px
    # 严格递增且步长最匀（隔一格取会在第 3 格缩回去，看着像闪帧）
    "earth_shot":   dict(src="v2_shot_earth", mode="ball", cols=4, rows=4,
                         pick=[0, 1, 2, 5, 6, 7, 10, 11]),

    # 道族 / 词组：六个互不相干的弹体，各裁各的，不吃星级渐变所以每个只有 _00 一帧。
    # 名字仍带 `_shot_` 后缀，好让 InkArtImporter 的 flatVfx 规则和 InkVfx.Frames 都认得。
    "words":        dict(src="v2_shot_words", mode="marks", cols=3, rows=2,
                         # 第 6 格（分叉 Y）不落地：分分成两发子弹本身就说清了，
                         # 不需要专门的体槽图，`FormRank` 里也没有 `CardId.Split`。
                         names=["kill_shot_00", "cleave_shot_00", "knock_shot_00",
                                "arrow_shot_00", "pierce_shot_00", None]),

    "burn_body":    dict(src="fire_v2_burn", mode="rise", cols=2, rows=2, shave=14),
    "ice_crust":    dict(src="v2_ice_crust", mode="hang", cols=2, rows=2, shave=4),
    # 12 张招牌两两：每对 4 帧无级循环（不是星级渐变，星级不改它的形），
    # 所以 Window == Count == 4，`FlatFrame` 会一直循环全部四帧。
    **{f"{k}_shot": dict(src=f"v2_pair_{k}", mode="ball", cols=2, rows=2)
       for k in ("frostfire", "scorchbolt", "hailbolt", "blightfire", "conduct", "moltengold",
                 "wardgold", "rotlife", "ramearth", "coldwind", "blaze", "thundercut")},

    "dot_marks":    dict(src="v2_dot_marks", mode="marks", cols=2, rows=2,
                         names=["dot_poison", "dot_stun", "dot_confuse", "dot_ripple"],
                         holes=True),
}


def cut(sheet, cols, rows, shave=10, pick=None):
    h, w = sheet.shape[:2]
    cw, ch = w // cols, h // rows
    out = []
    for r in range(rows):
        for c in range(cols):
            out.append(sheet[r * ch + shave:(r + 1) * ch - shave,
                             c * cw + shave:(c + 1) * cw - shave])
    return out if pick is None else [out[i] for i in pick]


def _fill(seed_pred, start_pixels, shape):
    got = np.zeros(shape, bool)
    q = deque()
    for y, x in start_pixels:
        if seed_pred[y, x] and not got[y, x]:
            got[y, x] = True
            q.append((y, x))
    h, w = shape
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and seed_pred[ny, nx] and not got[ny, nx]:
                got[ny, nx] = True
                q.append((ny, nx))
    return got


def keyed(cell, holes=False):
    near_white = cell.min(axis=2) >= BG_MIN
    h, w = near_white.shape
    border = [(y, x) for x in range(w) for y in (0, h - 1)]
    border += [(y, x) for y in range(h) for x in (0, w - 1)]
    bg = _fill(near_white, border, near_white.shape)
    mask = ~bg
    if not holes or not mask.any():
        return mask
    # 被包住的白：环心、水纹中间那圈。够大的当洞挖掉，小的（亮核、高光）留着。
    ys, xs = np.nonzero(mask)
    area = max(1, (ys.max() - ys.min() + 1) * (xs.max() - xs.min() + 1))
    inner = near_white & mask
    seen = np.zeros_like(inner)
    for y, x in zip(*np.nonzero(inner)):
        if seen[y, x]:
            continue
        blob = _fill(inner & ~seen, [(y, x)], inner.shape)
        seen |= blob
        if blob.sum() / area >= HOLE_MIN:
            mask &= ~blob
    return mask


def box(mask):
    ys, xs = np.nonzero(mask)
    return ys.min(), ys.max(), xs.min(), xs.max()


def head_center(mask):
    """最宽那一段的中心就是头部中心；比找亮核稳，各元素配色差别再大也不偏。"""
    rows = mask.sum(axis=1)
    peak = rows.max()
    band = [y for y in range(len(rows)) if rows[y] >= peak * 0.85]
    cy = (band[0] + band[-1]) // 2
    xs = np.nonzero(mask[band[0]:band[-1] + 1].any(axis=0))[0]
    return cy, (xs[0] + xs[-1]) // 2


def emit(cell, mask, top, bottom, left, right, size, name):
    h, w = mask.shape
    pt, pb = max(0, -top), max(0, bottom - (h - 1))
    pl, pr = max(0, -left), max(0, right - (w - 1))
    rgb = np.pad(cell, ((pt, pb), (pl, pr), (0, 0)), constant_values=255)
    a = np.pad(mask, ((pt, pb), (pl, pr)), constant_values=False)
    top, bottom, left, right = top + pt, bottom + pt, left + pl, right + pl
    rgb = rgb[top:bottom + 1, left:right + 1].astype(float)
    a = a[top:bottom + 1, left:right + 1].astype(float)

    pre = rgb * a[..., None]
    ph, pw = a.shape
    scale = size / max(ph, pw)
    if ph * scale < MIN_H:                 # 扁图：别让高掉到 ppu 的下限以下
        scale = MIN_H / ph
    tw, th = max(1, round(pw * scale)), max(1, round(ph * scale))
    pre_s = np.asarray(Image.fromarray(pre.astype(np.uint8)).resize((tw, th), Image.LANCZOS)).astype(float)
    a_s = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).resize((tw, th), Image.LANCZOS)).astype(float) / 255.0
    rgb_s = np.clip(pre_s / np.maximum(a_s, 1e-4)[..., None], 0, 255)
    Image.fromarray(np.dstack([rgb_s, a_s * 255]).astype(np.uint8), "RGBA").save(OUT / f"{name}.png")
    return tw, th


def run(key, cfg):
    sheet = np.asarray(Image.open(RAW / f"{cfg['src']}.png").convert("RGB")).astype(int)
    cells = cut(sheet, cfg["cols"], cfg["rows"], cfg.get("shave", 10), cfg.get("pick"))
    holes = cfg.get("holes", False)
    masks = [keyed(c, holes) for c in cells]
    mode = cfg["mode"]

    if mode == "marks":
        for name, cell, mask in zip(cfg["names"], cells, masks):
            if name is None:
                continue
            t, b, l, r = box(mask)
            w, h = emit(cell, mask, t - 6, b + 6, l - 6, r + 6, SIZE, name)
            print(f"  {name}  {w}x{h}")
        return

    if mode == "ball":
        half = up = down = 0
        anchors = []
        for mask in masks:
            cy, cx = head_center(mask)
            anchors.append((cy, cx))
            t, b, l, r = box(mask)
            half = max(half, cx - l, r - cx)
            up = max(up, cy - t)
            down = max(down, b - cy)
        half, up, down = half + 6, up + 6, down + 6
        for i, (cell, mask, (cy, cx)) in enumerate(zip(cells, masks, anchors)):
            w, h = emit(cell, mask, cy - up, cy + down, cx - half, cx + half, SIZE, f"{key}_{i:02d}")
        sink = 0.5 - up / (up + down)
        print(f"  {key}_00..{len(cells) - 1:02d}  {w}x{h}   Sink = {sink:+.3f}")
        return

    # rise / hang：共用包围盒，只是对齐的那条边不同
    t = min(box(m)[0] for m in masks)
    b = max(box(m)[1] for m in masks)
    l = min(box(m)[2] for m in masks)
    r = max(box(m)[3] for m in masks)
    pad = 6
    for i, (cell, mask) in enumerate(zip(cells, masks)):
        w, h = emit(cell, mask, t - pad, b + pad, l - pad, r + pad, SIZE, f"{key}_{i:02d}")
    print(f"  {key}_00..{len(cells) - 1:02d}  {w}x{h}")


if __name__ == "__main__":
    want = sys.argv[1:] or list(JOBS)
    MASTER.mkdir(parents=True, exist_ok=True)
    for key in want:
        if key not in JOBS:
            print("没有这个任务:", key)
            continue
        print(key + ":")
        run(key, JOBS[key])
