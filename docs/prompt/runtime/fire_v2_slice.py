#!/usr/bin/env python3
"""把 NB2 出的火特效白底表切成对齐的帧，去背后写进 Assets/Resources/Art/Vfx。

和 vfx_crush.py 的黑底加法流不同：火球这套是「平涂 + 深红外沿」的普通混合图，
白底不能按颜色抠（球心本身是白的），所以从画布四边洪泛填充，深红外沿会挡住
填充，被包住的白核因此留得下来。

对齐是这里最要紧的事：
- 火球按「白核质心」对齐并居中，八帧共用一个裁切半径，
  所以球心永远落在精灵中心（子弹位置），尾巴甩在后面，且高星看着更大。
- 燃烧层四帧共用一个包围盒，底边对齐，循环时火不会整体跳。

跑：/usr/bin/python3 docs/prompt/runtime/fire_v2_slice.py
"""

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[3]
RAW = REPO / "art_src/vfx/raw"
MASTER = REPO / "art_src/vfx/fire_v2"
OUT = REPO / "Assets/Resources/Art/Vfx"

BG_MIN = 232          # 三通道都不低于这个值才算「可能是白底」
CORE_MIN = 236        # 白核取色阈值


def cut(sheet, cols, rows, shave=10):
    """按等分网格切格子，边上多刮掉 shave 像素以吃掉画出来的网格线。"""
    h, w = sheet.shape[:2]
    cw, ch = w // cols, h // rows
    out = []
    for r in range(rows):
        for c in range(cols):
            out.append(sheet[r * ch + shave:(r + 1) * ch - shave,
                             c * cw + shave:(c + 1) * cw - shave])
    return out


def keyed(cell):
    """返回 (mask, core)：mask 为主体像素，core 为白核像素。"""
    near_white = cell.min(axis=2) >= BG_MIN
    h, w = near_white.shape
    bg = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if near_white[y, x] and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if near_white[y, x] and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and near_white[ny, nx] and not bg[ny, nx]:
                bg[ny, nx] = True
                q.append((ny, nx))
    mask = ~bg
    core = mask & (cell.min(axis=2) >= CORE_MIN)
    return mask, core


def box(mask):
    ys, xs = np.nonzero(mask)
    return ys.min(), ys.max(), xs.min(), xs.max()


def emit(cell, mask, top, bottom, left, right, size, name):
    """按给定裁切窗口输出：预乘后缩放，避免白边渗出。"""
    h, w = mask.shape
    pad_t, pad_b = max(0, -top), max(0, bottom - (h - 1))
    pad_l, pad_r = max(0, -left), max(0, right - (w - 1))
    rgb = np.pad(cell, ((pad_t, pad_b), (pad_l, pad_r), (0, 0)), constant_values=255)
    a = np.pad(mask, ((pad_t, pad_b), (pad_l, pad_r)), constant_values=False)
    top, bottom = top + pad_t, bottom + pad_t
    left, right = left + pad_l, right + pad_l
    rgb = rgb[top:bottom + 1, left:right + 1].astype(float)
    a = a[top:bottom + 1, left:right + 1].astype(float)

    # 透明区的 RGB 拉成边缘色，防止缩放时把白底混进外沿
    pre = rgb * a[..., None]
    ph, pw = a.shape
    scale = size / max(ph, pw)
    tw, th = max(1, round(pw * scale)), max(1, round(ph * scale))
    pre_s = np.asarray(Image.fromarray(pre.astype(np.uint8)).resize((tw, th), Image.LANCZOS)).astype(float)
    a_s = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).resize((tw, th), Image.LANCZOS)).astype(float) / 255.0
    safe = np.maximum(a_s, 1e-4)[..., None]
    rgb_s = np.clip(pre_s / safe, 0, 255)
    out = np.dstack([rgb_s, a_s * 255]).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(OUT / f"{name}.png")
    return tw, th


def do_ball():
    sheet = np.asarray(Image.open(RAW / "fire_v2_ball.png").convert("RGB")).astype(int)
    cells = cut(sheet, 4, 2)
    keys = [keyed(c) for c in cells]

    # 球心 = 白核质心。横向对球心对称（旋转时球不会甩出去），
    # 纵向贴紧内容，省掉球上方那一大片空白。
    centers = []
    half = 0
    up = down = 0
    for cell, (mask, core) in zip(cells, keys):
        ys, xs = np.nonzero(core if core.any() else mask)
        cy, cx = int(round(ys.mean())), int(round(xs.mean()))
        centers.append((cy, cx))
        t, b, l, r = box(mask)
        half = max(half, cx - l, r - cx)
        up = max(up, cy - t)
        down = max(down, b - cy)
    half += 6
    up += 6
    down += 6
    # 八帧共用同一个窗口高度，球心落在窗口里的固定比例上，代码按这个比例补偏移
    for i, (cell, (mask, _), (cy, cx)) in enumerate(zip(cells, keys, centers)):
        w, h = emit(cell, mask, cy - up, cy + down, cx - half, cx + half,
                    256, f"fire_shot_{i:02d}")
        print(f"  fire_shot_{i:02d}  {w}x{h}")
    print(f"  窗口 {2 * half + 1}x{up + down + 1}，球心在高度的 {up / (up + down):.3f} 处"
          f"（精灵中心往前挪 {0.5 - up / (up + down):+.3f} 个精灵高）")


def do_burn():
    sheet = np.asarray(Image.open(RAW / "fire_v2_burn.png").convert("RGB")).astype(int)
    cells = cut(sheet, 2, 2, shave=14)
    keys = [keyed(c) for c in cells]

    # 四帧共用包围盒，底边对齐，循环时整簇火不会跳
    t = min(box(m)[0] for m, _ in keys)
    b = max(box(m)[1] for m, _ in keys)
    l = min(box(m)[2] for m, _ in keys)
    r = max(box(m)[3] for m, _ in keys)
    pad = 6
    for i, (cell, (mask, _)) in enumerate(zip(cells, keys)):
        w, h = emit(cell, mask, t - pad, b + pad, l - pad, r + pad,
                    256, f"burn_body_{i:02d}")
        print(f"  burn_body_{i:02d}  {w}x{h}")


if __name__ == "__main__":
    MASTER.mkdir(parents=True, exist_ok=True)
    print("火球八帧：")
    do_ball()
    print("燃烧四帧：")
    do_burn()
    for src in ("fire_v2_ball.png", "fire_v2_burn.png"):
        Image.open(RAW / src).save(MASTER / src)
    print("母版留在", MASTER)
