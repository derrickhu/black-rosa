#!/usr/bin/env python3
"""第二幕四只敌人：切 act2_enemies 母版，洋红抠图，写进 Assets/Resources/Art/。
幻影没有单独出图，从影姬复制一份往淡紫褪色，读起来是「影姬的分身」。

    .venv-mock/bin/python docs/prompt/enemy/process_act2.py
"""
import os
import shutil

import numpy as np
from PIL import Image

from process_bosses import DEST, FINAL, RAW, drop_bleed, key_out, normalize

SHEET = "act2_enemies.png"
# 钟魔背上的钟顶探进了右上格，按实际轮廓手切，不走等分。
BOXES = {
    "shade":      (0.00, 0.00, 0.50, 0.50),
    "monk":       (0.50, 0.00, 1.00, 0.465),
    "boss_shade": (0.00, 0.47, 0.50, 1.00),
    "boss_bell":  (0.50, 0.465, 1.00, 1.00),
}
LILAC = np.array([206, 192, 236], dtype=np.float32)


def phantom(src):
    a = np.asarray(src).astype(np.float32)
    rgb = a[:, :, :3]
    grey = rgb.mean(axis=2, keepdims=True)
    rgb = (grey * 0.35 + rgb * 0.15) + LILAC * 0.5
    a[:, :, :3] = np.clip(rgb, 0, 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def main():
    sheet = Image.open(os.path.join(RAW, SHEET)).convert("RGBA")
    w, h = sheet.size
    for name, (x0, y0, x1, y1) in BOXES.items():
        cell = sheet.crop((round(x0 * w), round(y0 * h), round(x1 * w), round(y1 * h)))
        img = key_out(cell)
        img, dropped = drop_bleed(img)
        img = normalize(img)
        fp = os.path.join(FINAL, name + ".png")
        img.save(fp, optimize=True)
        shutil.copyfile(fp, os.path.join(DEST, name + ".png"))
        print(f"{name:<11} {img.size}  碎片剔除={dropped}")
        if name == "boss_shade":
            ph = phantom(img)
            fp = os.path.join(FINAL, "phantom.png")
            ph.save(fp, optimize=True)
            shutil.copyfile(fp, os.path.join(DEST, "phantom.png"))
            print(f"{'phantom':<11} {ph.size}  从影姬褪色")


if __name__ == "__main__":
    main()
