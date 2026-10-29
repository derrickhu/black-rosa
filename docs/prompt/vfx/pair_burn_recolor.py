#!/usr/bin/env python3
"""秘卷灼烧换色：把 burn_body 四帧按亮度重新映射到三套色板，出 burn_frost / burn_blight / burn_molten。

    .venv-mock/bin/python docs/prompt/vfx/pair_burn_recolor.py

原图是平涂硬边火：深红描边、红、橙、柠檬黄。按亮度分段映射，形状和描边一笔不动，
只换颜色，所以和普通灼烧并排时读得出是同一种火、不同的秘卷。
"""
import os
import re
import uuid
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
VFX = ROOT / "Assets" / "Resources" / "Art" / "Vfx"

# 亮度断点 -> 颜色。断点按原图实测：描边 ~30，红 ~70，橙 ~130，黄 ~210。
STOPS = [0, 30, 75, 135, 210, 255]
PALETTES = {
    # 霜火：冰蓝冷焰，芯是冰白。
    "burn_frost": ["0F1F45", "1F3A7A", "2F7FD8", "52C4F5", "B8F0FF", "FFFFFF"],
    # 燎毒：毒紫描边、酸绿火身。
    "burn_blight": ["2A1238", "4A2266", "7A3FA0", "6CC02A", "C8F06A", "F4FFD0"],
    # 熔金：焦褐描边、金水火身。
    "burn_molten": ["3A1C04", "6B3A0A", "B8650E", "F0A81E", "FFE066", "FFF8D8"],
}


def hex_rgb(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.float32)


def recolor(src, colors):
    a = np.array(Image.open(src).convert("RGBA")).astype(np.float32)
    lum = 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]
    cols = [hex_rgb(c) for c in colors]
    out = np.zeros_like(a)
    for ch in range(3):
        out[..., ch] = np.interp(lum, STOPS, [c[ch] for c in cols])
    out[..., 3] = a[..., 3]
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")


def write_meta(dst, template):
    meta = Path(str(dst) + ".meta")
    if meta.exists():
        return
    src = template.read_text(encoding="utf-8")
    old = re.search(r"guid: (\S+)", src).group(1)
    # 团结认 32 位十六进制。按旧 GUID 的长度去填随机 base64，编辑器会判无效并丢掉这张图。
    guid = uuid.uuid4().hex
    src = src.replace("guid: " + old, "guid: " + guid, 1)
    src = re.sub(r"spriteID: [0-9a-f]+", "spriteID: " + os.urandom(16).hex(), src, count=1)
    meta.write_text(src, encoding="utf-8")


for name, colors in PALETTES.items():
    for i in range(4):
        src = VFX / f"burn_body_{i:02d}.png"
        dst = VFX / f"{name}_{i:02d}.png"
        recolor(src, colors).save(dst, optimize=True)
        write_meta(dst, Path(str(src) + ".meta"))
    print("wrote", name)
