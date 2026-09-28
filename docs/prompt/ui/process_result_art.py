#!/usr/bin/env python3
"""结算页美术：横幅、星星、碎炮台、墨点抠成透明 PNG；光芒用代码画。放进 Resources/Art/Ui。"""
import math
import os
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
RAW = os.path.join(ROOT, "docs", "prompt", "ui", "raw")
OUT = os.path.join(ROOT, "Assets", "Resources", "Art", "Ui")
sys.path.insert(0, os.path.dirname(__file__))
from process_ui_icons import key_out, drop_bleed  # noqa: E402
from process_sortie_parts import trim, write_meta  # noqa: E402


def corner_bg(img):
    px = img.convert("RGB").load()
    pts = [px[4, 4], px[img.width - 5, 4], px[4, img.height - 5], px[img.width - 5, img.height - 5]]
    return tuple(sum(p[i] for p in pts) // 4 for i in range(3))


def cut(cell, bg):
    c = key_out(cell, bg)
    c, _ = drop_bleed(c)
    return trim(c, 4)


def fit(img, max_side):
    s = max_side / max(img.width, img.height)
    if s >= 1:
        return img
    return img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)


def save(img, name):
    path = os.path.join(OUT, name + ".png")
    img.save(path, optimize=True)
    write_meta(path, max(img.size))
    print(name, img.size)


def banners():
    sheet = Image.open(os.path.join(RAW, "result_banners.png")).convert("RGBA")
    bg = corner_bg(sheet)
    h = sheet.height // 2
    save(fit(cut(sheet.crop((0, 0, sheet.width, h)), bg), 900), "result_banner_win")
    save(fit(cut(sheet.crop((0, h, sheet.width, sheet.height)), bg), 900), "result_banner_lose")


def parts():
    sheet = Image.open(os.path.join(RAW, "result_parts.png")).convert("RGBA")
    bg = corner_bg(sheet)
    cw, ch = sheet.width // 2, sheet.height // 2
    names = ["result_star_on", "result_star_off", "result_cannon_broken", "result_ink_splat"]
    sizes = [256, 256, 384, 256]
    for i, name in enumerate(names):
        x, y = i % 2, i // 2
        c = cut(sheet.crop((x * cw, y * ch, (x + 1) * cw, (y + 1) * ch)), bg)
        save(fit(c, sizes[i]), name)


def rays(n=512, count=12):
    """奶油金的放射光，一条亮一条空，往外渐隐。转起来就是休闲游戏结算那圈光。"""
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    px = img.load()
    mid = (n - 1) / 2
    seg = 2 * math.pi / count
    for y in range(n):
        for x in range(n):
            dx, dy = x - mid, y - mid
            r = math.hypot(dx, dy) / mid
            if r > 1:
                continue
            a = math.atan2(dy, dx) % seg / seg
            if not 0.08 < a < 0.66:
                continue
            edge = min(1.0, min(a - 0.08, 0.66 - a) * 40)
            fade = max(0.0, 1 - r) ** 1.2 * min(1.0, r * 6)
            px[x, y] = (255, 236, 170, int(255 * 0.9 * fade * edge))
    save(img, "result_rays")


def main():
    os.makedirs(OUT, exist_ok=True)
    banners()
    parts()
    rays()


if __name__ == "__main__":
    main()
