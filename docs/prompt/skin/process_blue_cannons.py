#!/usr/bin/env python3
"""海浪、闪电两门炮：洋红底抠出来，开炮帧按同一只脚对齐，展台和炮弹收成游戏尺寸。"""
import os
import shutil

import numpy as np
from PIL import Image, ImageFilter

SRC = os.path.expanduser(
    "~/.cursor/projects/Users-rosa-rosa-games-black-rosa/assets")
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
ART = os.path.join(REPO, "Assets", "Resources", "Art")
RAW = os.path.expanduser("~/rosa_games/game_assets/black-rosa/assets/raw/skins")


def key_out(img, lo=48, hi=100):
    a = np.asarray(img.convert("RGB")).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = np.minimum(r, b) - g
    alpha = np.clip((hi - m) / float(hi - lo), 0, 1)
    soft = alpha < 0.999
    cap = g + 36
    rr = np.where(soft, np.minimum(r, np.maximum(cap, b - 180)), r)
    bb = np.where(soft, np.minimum(b, cap), b)
    rgb = np.stack([rr, g, bb], -1).clip(0, 255).astype(np.uint8)
    out = Image.fromarray(rgb, "RGB").convert("RGBA")
    al = Image.fromarray((alpha * 255).astype(np.uint8), "L")
    al = al.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.6))
    out.putalpha(al)
    return out


def bbox(im, thresh=20):
    a = np.asarray(im.split()[-1])
    ys, xs = np.where(a > thresh)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def load(name):
    path = os.path.join(SRC, name)
    if not os.path.exists(path):
        path = os.path.splitext(path)[0] + ".jpg"
    return key_out(Image.open(path))


def place_foot(frames, size, foot=8, top=4):
    boxes = [bbox(im) for im in frames]
    w0 = boxes[0][2] - boxes[0][0]
    scale = (size[0] - 28) / float(w0)
    room = size[1] - foot - top
    for im, box in zip(frames, boxes):
        h = (box[3] - box[1]) * scale
        if h > room:
            scale = room / float(box[3] - box[1])
    out = []
    cw, ch = size
    for im, box in zip(frames, boxes):
        crop = im.crop(box)
        nw = max(1, int(round(crop.width * scale)))
        nh = max(1, int(round(crop.height * scale)))
        crop = crop.resize((nw, nh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", size, (0, 0, 0, 0))
        x = (cw - nw) // 2
        y = ch - foot - nh
        canvas.alpha_composite(crop, (x, max(0, y)))
        out.append(canvas)
    return out


def place_bottom(im, size, pad_bottom, pad_side, pad_top):
    box = bbox(im)
    crop = im.crop(box)
    max_w = size - pad_side * 2
    max_h = size - pad_bottom - pad_top
    scale = min(max_w / crop.width, max_h / crop.height)
    nw = max(1, int(round(crop.width * scale)))
    nh = max(1, int(round(crop.height * scale)))
    crop = crop.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    x = (size - nw) // 2
    y = size - pad_bottom - nh
    canvas.alpha_composite(crop, (x, y))
    return canvas


def place_center(im, size, margin):
    box = bbox(im)
    crop = im.crop(box)
    room = size - margin * 2
    scale = min(room / crop.width, room / crop.height)
    nw = max(1, int(round(crop.width * scale)))
    nh = max(1, int(round(crop.height * scale)))
    crop = crop.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(crop, ((size - nw) // 2, (size - nh) // 2))
    return canvas


def main():
    os.makedirs(RAW, exist_ok=True)
    os.makedirs(os.path.join(ART, "CannonFire"), exist_ok=True)
    for key in ("tide", "bolt"):
        frames = [load(f"{key}_up_{i}.jpg") for i in range(7)]
        placed = place_foot(frames, (240, 298))
        for i, im in enumerate(placed):
            im.save(os.path.join(ART, "CannonFire", f"{key}_{i}.png"))
        show = place_bottom(load(f"{key}_show.jpg"), 480, 18, 12, 24)
        show.save(os.path.join(ART, "Ui", f"ico_skin_{key}.png"))
        small = show.resize((256, 256), Image.Resampling.LANCZOS)
        small.save(os.path.join(ART, f"cannon_{key}.png"))
        place_center(load(f"shot_{key}.jpg"), 128, 8).save(os.path.join(ART, f"shot_{key}.png"))
        print(key, "ok")


if __name__ == "__main__":
    main()
