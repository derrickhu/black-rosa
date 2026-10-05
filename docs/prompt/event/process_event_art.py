#!/usr/bin/env python3
"""招财进宝的活动美术：八只偷宝老鼠 + 庙会底图。

敌人沿用 enemy/process_mobs2 的键控、去越界、去游离，成品 evt_*.png
进 Assets/Resources/Art/，战斗里 InkSprites.EnemyTheme = "evt_" 时优先取它们。
底图按 cover 裁成 720x1280、往纸色淡 12%，放进 CdnArt/Bg/，再跑 cdn_build.py。

    .venv-mock/bin/python docs/prompt/event/process_event_art.py
"""
import os
import re
import shutil
import sys
import uuid

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "enemy"))
from process_bosses import key_out, drop_bleed, normalize  # noqa: E402
from process_mobs2 import drop_far  # noqa: E402

REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
BASE = "/Users/rosa/rosa_games/game_assets/black-rosa/美术/活动招财"
RAW = os.path.join(BASE, "raw")
FINAL = os.path.join(BASE, "final")
ART = os.path.join(REPO, "Assets", "Resources", "Art")
CDN_BG = os.path.join(REPO, "CdnArt", "Bg")
MAXDIM = 224
CELL_WORK = 512

# (成品名, 母版, 列, 行)  —— 母版都是 2x2。活动怪是偷宝的老鼠，和主线的墨人分开。
CELLS = [
    ("evt_walker",  "evt_rat_a.png", 0, 0),  # 抱金元宝的灰鼠
    ("evt_ball",    "evt_rat_a.png", 1, 0),  # 缩成球的仓鼠
    ("evt_chubby",  "evt_rat_a.png", 0, 1),  # 扛米袋的胖鼠
    ("evt_bighead", "evt_rat_a.png", 1, 1),  # 大耳朵鼠
    ("evt_runner",  "evt_rat_b.png", 0, 0),  # 戴眼罩举红包的快鼠
    ("evt_swarm",   "evt_rat_b.png", 1, 0),  # 三只举金钱的小鼠
    ("evt_shield",  "evt_rat_b.png", 0, 1),  # 举锅盖的鼠
    ("evt_elite",   "evt_rat_b.png", 1, 1),  # 穿红坎肩扛算盘的鼠老大
]

PAPER = np.array([244, 239, 228], dtype=np.float32)
FADE = 0.12


def meta_like(src_name, dst_path):
    if os.path.exists(dst_path + ".meta"):
        return
    src = open(os.path.join(ART, src_name + ".meta"), encoding="utf-8").read()
    src = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, src, count=1)
    open(dst_path + ".meta", "w", encoding="utf-8").write(src)


def kill_shadow(cell):
    """脚下那块压暗的洋红影子离纯洋红太远，键控抠不掉，先刷回纯洋红。
    判据：绿几乎为零、红蓝接近、又不是黑描边。红包、粉尾巴绿都不低，碰不到。"""
    a = np.array(cell.convert("RGB")).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = (g < 60) & (np.abs(r - b) < 50) & (r > 90) & (b > 90)
    a[m] = (255, 0, 255)
    out = Image.fromarray(a.astype(np.uint8)).convert("RGBA")
    return out


def mobs():
    os.makedirs(FINAL, exist_ok=True)
    sheets = {}
    for name, src, cx, cy in CELLS:
        if src not in sheets:
            sheets[src] = Image.open(os.path.join(RAW, src)).convert("RGBA")
        sheet = sheets[src]
        cw, ch = sheet.width // 2, sheet.height // 2
        cell = sheet.crop((cx * cw, cy * ch, (cx + 1) * cw, (cy + 1) * ch))
        cell = cell.resize((CELL_WORK, CELL_WORK), Image.LANCZOS)
        img = key_out(kill_shadow(cell))
        res = drop_bleed(img)
        img = res[0] if isinstance(res, tuple) else res
        img, far = drop_far(img)
        # 老鼠的粉耳朵、粉尾巴正是 neutralize 要拉灰的那一档颜色，这里不能用。
        tinted = 0
        img = normalize(img)
        k = MAXDIM / max(img.size)
        if k < 1:
            img = img.resize((max(1, round(img.width * k)), max(1, round(img.height * k))), Image.LANCZOS)
        fp = os.path.join(FINAL, name + ".png")
        img.save(fp, optimize=True)
        dst = os.path.join(ART, name + ".png")
        shutil.copyfile(fp, dst)
        meta_like("walker.png", dst)
        print(f"{name:<12} {str(img.size):<11} {os.path.getsize(fp) // 1024:>3}KB 游离={far} 去色={tinted}")

    cw = 260
    board = Image.new("RGBA", (cw * 4, cw * 2), (248, 245, 238, 255))
    for i, (name, *_r) in enumerate(CELLS):
        im = Image.open(os.path.join(FINAL, name + ".png")).convert("RGBA")
        k = (cw * 0.86) / max(im.size)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        x = (i % 4) * cw + cw // 2
        y = (i // 4) * cw + cw // 2
        board.paste(im, (x - im.width // 2, y - im.height // 2), im)
    cp = os.path.join(BASE, "_接触图.png")
    board.convert("RGB").save(cp)
    print("接触图 ->", cp)


def backdrop():
    tw, th = 720, 1280
    im = Image.open(os.path.join(RAW, "battle_bg_event.png")).convert("RGB")
    s = max(tw / im.width, th / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    x = (im.width - tw) // 2
    y = (im.height - th) // 2
    a = np.array(im.crop((x, y, x + tw, y + th))).astype(np.float32)
    a = a * (1 - FADE) + PAPER * FADE
    out = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    out.save(os.path.join(FINAL, "battle_bg_event.png"), optimize=True)
    os.makedirs(CDN_BG, exist_ok=True)
    out.save(os.path.join(CDN_BG, "battle_bg_event.png"), optimize=True)
    print("battle_bg_event -> CdnArt/Bg（接着跑 cdn_build.py）")


if __name__ == "__main__":
    mobs()
    backdrop()
