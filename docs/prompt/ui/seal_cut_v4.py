"""印章 v4 切图：洋红底抠掉，按连通块切出四色章底、空槽、锁槽和八个白纹样。

用法：.venv-mock/bin/python docs/prompt/ui/seal_cut_v4.py <medal_raw> <sockets_raw> <emblems_raw> <out_dir>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from seal_cut import bg_color, blobs, fit, merge, order  # noqa: E402


def key(im, bg, hard=70, soft=160, linked=True):
    """linked：只抠和图边连通的底色。紫色章面、深棕空槽和洋红离得近，按颜色全图抠会把章面抠成半透明。
    纹样只有白和深棕，环里、沙漏里围住的洋红也要抠，所以纹样全图抠。"""
    px = np.asarray(im.convert("RGB")).astype(np.float32)
    d = np.sqrt(((px - np.array(bg, np.float32)) ** 2).sum(axis=2))
    near = d < soft
    if not linked:
        outside = near
        return _alpha(px, d, outside, bg, hard, soft)
    outside = np.zeros_like(near)
    outside[0, :] = near[0, :]
    outside[-1, :] = near[-1, :]
    outside[:, 0] = near[:, 0]
    outside[:, -1] = near[:, -1]
    # 从四条边往里长，只长进和底色相近的像素，长不动为止。
    while True:
        grow = outside.copy()
        grow[1:, :] |= outside[:-1, :]
        grow[:-1, :] |= outside[1:, :]
        grow[:, 1:] |= outside[:, :-1]
        grow[:, :-1] |= outside[:, 1:]
        grow &= near
        if np.array_equal(grow, outside):
            break
        outside = grow
    return _alpha(px, d, outside, bg, hard, soft)


def _alpha(px, d, outside, bg, hard, soft):
    a = np.where(outside, np.clip((d - hard) / (soft - hard), 0, 1), 1.0)
    keep = np.maximum(a, 1e-3)[..., None]
    rgb = np.where(outside[..., None], (px - np.array(bg, np.float32) * (1 - keep)) / keep, px)
    out = np.dstack([np.clip(rgb, 0, 255), a * 255]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")

# 章底那张图第二行后两颗（空槽、锁槽）另出了一张，这里只取前四颗。
MEDALS = ["seal_q_green", "seal_q_blue", "seal_q_purple", "seal_q_gold", None, None]
SOCKETS = ["seal_empty", "seal_stud"]
EMBLEMS = ["seal_ico_luck", "seal_ico_edge", "seal_ico_fierce", "seal_ico_wide",
           "seal_ico_last", "seal_ico_move", "seal_ico_keep", "seal_ico_lock"]


def cut(path, names, gap, size, fill, out, linked=True):
    raw = Image.open(path).convert("RGB")
    rgba = key(raw, bg_color(raw), hard=70, soft=160, linked=linked)
    boxes = order(merge(blobs(rgba), gap=int(raw.width * gap)), raw.height)
    if len(boxes) != len(names):
        raise SystemExit(f"{path}: found {len(boxes)} blobs, want {len(names)}")
    for name, b in zip(names, boxes):
        if name is None:
            continue
        pad = 4
        part = rgba.crop((max(0, b[0] - pad), max(0, b[1] - pad), b[2] + 1 + pad, b[3] + 1 + pad))
        fit(part, size, fill).save(out / f"{name}.png")
        print(name, b)


def run(medal_raw, sockets_raw, emblems_raw, out_dir):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    cut(medal_raw, MEDALS, 0.02, 256, 0.96, out)
    cut(sockets_raw, SOCKETS, 0.02, 256, 0.96, out)
    cut(emblems_raw, EMBLEMS, 0.03, 160, 0.92, out, linked=False)
    print("ok", out)


if __name__ == "__main__":
    run(*sys.argv[1:5])
