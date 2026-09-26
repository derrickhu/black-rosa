#!/usr/bin/env python3
"""战斗地面后期：按 cover 裁成 720x1280，往纸色轻淡一点，免得和顶栏、字牌抢色。

    .venv-mock/bin/python docs/prompt/battle_bg/process_battle_bg.py 1 2 3 4 5 6 7 8
"""
import os
import re
import sys
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
BASE = "/Users/rosa/rosa_games/game_assets/black-rosa/美术/战斗底图"
RAW = os.path.join(BASE, "raw_v2")
OUT = os.path.join(ROOT, "Assets", "Resources", "Art", "Bg")
PAPER = np.array([244, 239, 228], dtype=np.float32)   # InkTheme.Paper F4EFE4
TW, TH = 720, 1280
FADE = 0.12


def process(k):
    im = Image.open(os.path.join(RAW, f"battle_bg_{k}.png")).convert("RGB")
    s = max(TW / im.width, TH / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    x = (im.width - TW) // 2
    y = (im.height - TH) // 2
    a = np.array(im.crop((x, y, x + TW, y + TH))).astype(np.float32)
    a = a * (1 - FADE) + PAPER * FADE
    out = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    os.makedirs(os.path.join(BASE, "final"), exist_ok=True)
    out.save(os.path.join(BASE, "final", f"battle_bg_{k}.png"), optimize=True)
    os.makedirs(OUT, exist_ok=True)
    out.save(os.path.join(OUT, f"battle_bg_{k}.png"), optimize=True)
    print("wrote", f"battle_bg_{k}")


def write_meta(k):
    dst = os.path.join(OUT, f"battle_bg_{k}.png.meta")
    if os.path.exists(dst):
        return
    src = open(os.path.join(ROOT, "Assets", "Resources", "Art", "Ui", "chapter_3.png.meta"), encoding="utf-8").read()
    src = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, src, count=1)
    src = re.sub(r"maxTextureSize: 2048", "maxTextureSize: 1024", src)
    open(dst, "w", encoding="utf-8").write(src)


for arg in sys.argv[1:]:
    process(int(arg))
    write_meta(int(arg))
