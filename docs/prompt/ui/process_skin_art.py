#!/usr/bin/env python3
"""炮台皮肤这一批美术：从 raw/skins 的洋红底大图切出成品，写进 final/skins，可选 --install 拷进游戏。

    .venv-mock/bin/python docs/prompt/ui/process_skin_art.py [--install]

键控不按「离背景色多远」算，按洋红度 min(R,B)-G 算：NB2 给贴纸外圈加的那层粉色光晕
离纯洋红很远，色距键控会把它当成主体留下来，洋红度却和背景一样高。
"""
import os
import shutil
import subprocess
import sys

import numpy as np
from PIL import Image, ImageFilter

ASSETS = os.environ.get(
    "GAME_ASSETS", "/Users/rosa/rosa_games/game_assets/black-rosa/assets")
RAW = os.path.join(ASSETS, "raw", "skins")
FINAL = os.path.join(ASSETS, "final", "skins")
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
ART = os.path.join(REPO, "Assets", "Resources", "Art")

KEYS = ["plain", "cinnabar", "celadon", "gilt", "flame", "lucky"]

# (原图, 列, 行, [(格子下标, 输出相对路径, 画布宽, 画布高, 内容最大宽, 内容最大高, 对齐)])
# 对齐：bottom = 底边贴画布下沿留 margin，center = 居中。
JOBS = [
    ("cannons_show_v2.png", 3, 2,
     [(i, f"Ui/ico_skin_{KEYS[i]}.png", 480, 480, 456, 444, "bottom") for i in (0, 4, 5)]),
    ("cannons_battle_v3.png", 3, 2,
     [(i, f"cannon_{KEYS[i]}.png", 256, 256, 236, 226, "bottom") for i in (0, 4, 5)]),
    ("cannons_casual_show.png", 3, 1,
     [(i - 1, f"Ui/ico_skin_{KEYS[i]}.png", 480, 480, 456, 444, "bottom") for i in (1, 2, 3)]),
    ("cannons_casual_battle.png", 3, 1,
     [(i - 1, f"cannon_{KEYS[i]}.png", 256, 256, 236, 226, "bottom") for i in (1, 2, 3)]),
    ("shots.png", 2, 1,
     [(0, "shot_flame.png", 128, 128, 116, 116, "center"),
      (1, "shot_coin.png", 128, 128, 116, 116, "center")]),
    ("frames.png", 3, 1,
     [(0, "Ui/skin_frame.png", 160, 192, 160, 192, "center"),
      (1, "Ui/skin_frame_on.png", 160, 192, 160, 192, "center"),
      (2, "Ui/skin_frame_lock.png", 160, 192, 160, 192, "center")]),
    ("chrome.png", 2, 1,
     [(0, "Ui/skin_arrow.png", 128, 128, 128, 128, "center")]),
    ("stage_v4c.png", 1, 1,
     [(0, "Ui/skin_stage.png", 1280, 960, 1280, 960, "stretch")]),
]


def key_out(img, lo=58, hi=108):
    a = np.asarray(img.convert("RGB")).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = np.minimum(r, b) - g
    alpha = np.clip((hi - m) / float(hi - lo), 0, 1)
    # 半透明边上的洋红往回压，免得描边外沿泛紫。
    soft = alpha < 0.999
    cap = g + 40
    rr = np.where(soft, np.minimum(r, np.maximum(cap, b - 200)), r)
    bb = np.where(soft, np.minimum(b, cap), b)
    rgb = np.stack([rr, g, bb], -1).clip(0, 255).astype(np.uint8)
    out = Image.fromarray(rgb, "RGB").convert("RGBA")
    al = Image.fromarray((alpha * 255).astype(np.uint8), "L")
    k = 3 if img.width < 600 else 5
    al = al.filter(ImageFilter.MinFilter(k)).filter(ImageFilter.GaussianBlur(0.8))
    out.putalpha(al)
    return out


def drop_edge_bits(img, step=4, keep_ratio=0.25):
    """丢掉碰到格子边、又比主体小得多的连通块（邻格越界进来的尖角、网格线残渣）。"""
    a = np.asarray(img.split()[3])
    small = a[::step, ::step] > 96
    h, w = small.shape
    label = np.zeros((h, w), np.int32)
    comps = []
    cur = 0
    for sy in range(h):
        for sx in range(w):
            if not small[sy, sx] or label[sy, sx]:
                continue
            cur += 1
            stack = [(sx, sy)]
            label[sy, sx] = cur
            area, edge = 0, False
            while stack:
                x, y = stack.pop()
                area += 1
                if x in (0, w - 1) or y in (0, h - 1):
                    edge = True
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and small[ny, nx] and not label[ny, nx]:
                        label[ny, nx] = cur
                        stack.append((nx, ny))
            comps.append((cur, area, edge))
    if not comps:
        return img
    big = max(c[1] for c in comps)
    kill = [c[0] for c in comps if c[2] and c[1] < big * keep_ratio]
    if not kill:
        return img
    mask = np.isin(label, kill)
    mask = np.repeat(np.repeat(mask, step, 0), step, 1)[:a.shape[0], :a.shape[1]]
    if mask.shape != a.shape:
        pad = np.zeros(a.shape, bool)
        pad[:mask.shape[0], :mask.shape[1]] = mask
        mask = pad
    # 粗网格上判的块，放大回去边上会差几像素，膨胀一下再抹。
    m_img = Image.fromarray((mask * 255).astype(np.uint8), "L").filter(ImageFilter.MaxFilter(2 * step + 1))
    keep = 255 - np.asarray(m_img).astype(np.int16)
    na = (a.astype(np.int16) * keep // 255).astype(np.uint8)
    out = img.copy()
    out.putalpha(Image.fromarray(na, "L"))
    return out


def place(art, cw, ch, mw, mh, align):
    bbox = art.getbbox()
    if bbox is None:
        return Image.new("RGBA", (cw, ch))
    art = art.crop(bbox)
    if align == "stretch":
        return art.resize((cw, ch), Image.LANCZOS)
    s = min(mw / art.width, mh / art.height)
    art = art.resize((max(1, round(art.width * s)), max(1, round(art.height * s))), Image.LANCZOS)
    out = Image.new("RGBA", (cw, ch))
    x = (cw - art.width) // 2
    y = (ch - art.height) // 2 if align == "center" else ch - art.height - (ch - mh) // 2
    out.alpha_composite(art, (x, y))
    return out


def main():
    install = "--install" in sys.argv
    os.makedirs(FINAL, exist_ok=True)
    made = []
    for src, cols, rows, items in JOBS:
        path = os.path.join(RAW, src)
        if not os.path.exists(path):
            sys.exit("找不到原图：" + path)
        sheet = key_out(Image.open(path))
        cw, ch = sheet.width / cols, sheet.height / rows
        for idx, rel, w, h, mw, mh, align in items:
            cx, cy = idx % cols, idx // cols
            cell = sheet.crop((round(cx * cw), round(cy * ch), round((cx + 1) * cw), round((cy + 1) * ch)))
            if cols * rows > 1:
                cell = drop_edge_bits(cell)
            out = place(cell, w, h, mw, mh, align)
            fp = os.path.join(FINAL, rel)
            os.makedirs(os.path.dirname(fp), exist_ok=True)
            out.save(fp, optimize=True)
            quality = "90-100" if rel.endswith("skin_stage.png") else "70-92"
            if shutil.which("pngquant"):
                subprocess.run(["pngquant", "--force", "--skip-if-larger", "--quality", quality,
                                "--output", fp, fp], check=False)
            made.append((rel, fp, out.getbbox()))
            print(f"  {rel:<28} {os.path.getsize(fp) / 1024:6.1f} KB  bbox={out.getbbox()}")

    contact = Image.new("RGBA", (6 * 330 + 10, 4 * 330 + 10), (150, 150, 150, 255))
    for i, (rel, fp, _) in enumerate(made):
        im = Image.open(fp).convert("RGBA")
        im.thumbnail((320, 320))
        contact.alpha_composite(im, (10 + (i % 6) * 330, 10 + (i // 6) * 330))
    cp = os.path.join(FINAL, "_contact.png")
    contact.convert("RGB").save(cp)
    print("对照表 →", cp)

    if install:
        for rel, fp, _ in made:
            dst = os.path.join(ART, rel)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copyfile(fp, dst)
        print("已拷进", ART)


if __name__ == "__main__":
    main()
