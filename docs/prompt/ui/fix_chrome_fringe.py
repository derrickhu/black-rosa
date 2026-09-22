#!/usr/bin/env python3
"""清掉炮台页 / 底栏 PNG 上的洋红毛边，并按实图像素写回紧裁结果。

key_out 会把洋红留在半透明像素里。团结 bilinear 一滤，卷轴和药丸就带一圈紫。
这里把洋红当透明，再把不透明色渗进透明像素，滤镜才不会混出紫边。
"""
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[3] / "Assets" / "Resources" / "Art" / "Ui"
NAMES = [
    "tab_dock", "tab_plaque",
    "panel_board", "panel_card", "panel_card_on", "panel_card_dim",
    "panel_strip", "panel_row", "panel_price",
]


def magenta(r, g, b):
    return r >= 140 and b >= 140 and g <= min(r, b) - 20


def defringe(im):
    im = im.convert("RGBA")
    w, h = im.size
    px = im.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if magenta(r, g, b) or a < 40:
                px[x, y] = (r, g, b, 0)
            elif a > 220:
                px[x, y] = (r, g, b, 255)
    # 不透明色往外渗几圈，透明像素带着邻色，bilinear 才不会混到洋红。
    for _ in range(4):
        nxt = im.copy()
        npx = nxt.load()
        for y in range(h):
            for x in range(w):
                if px[x, y][3] != 0:
                    continue
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < w and 0 <= yy < h and px[xx, yy][3] == 255:
                        r, g, b, _ = px[xx, yy]
                        npx[x, y] = (r, g, b, 0)
                        break
        im = nxt
        px = im.load()
    return im


def tight(im, pad=6):
    bbox = im.getbbox()
    if bbox is None:
        return im
    art = im.crop(bbox)
    out = Image.new("RGBA", (art.width + pad * 2, art.height + pad * 2), (0, 0, 0, 0))
    out.paste(art, (pad, pad))
    return defringe(out)


def slice9(im, left, bottom, right, top, dw, dh):
    w, h = im.size
    left = min(left, w // 2 - 1)
    right = min(right, w // 2 - 1)
    top = min(top, h // 2 - 1)
    bottom = min(bottom, h // 2 - 1)
    cw, ch = w - left - right, h - top - bottom
    dcw, dch = dw - left - right, dh - top - bottom
    if cw < 1 or ch < 1 or dcw < 1 or dch < 1:
        return im.resize((dw, dh), Image.LANCZOS)
    src = [
        (0, 0, left, top, 0, 0, left, top),
        (left, 0, cw, top, left, 0, dcw, top),
        (w - right, 0, right, top, dw - right, 0, right, top),
        (0, top, left, ch, 0, top, left, dch),
        (left, top, cw, ch, left, top, dcw, dch),
        (w - right, top, right, ch, dw - right, top, right, dch),
        (0, h - bottom, left, bottom, 0, dh - bottom, left, bottom),
        (left, h - bottom, cw, bottom, left, dh - bottom, dcw, bottom),
        (w - right, h - bottom, right, bottom, dw - right, dh - bottom, right, bottom),
    ]
    out = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    for sx, sy, sw, sh, dx, dy, tw, th in src:
        patch = im.crop((sx, sy, sx + sw, sy + sh))
        if (sw, sh) != (tw, th):
            patch = patch.resize((tw, th), Image.LANCZOS)
        out.paste(patch, (dx, dy), patch)
    return out


def rounded(w, h, radius, fill, stroke, sw=7):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    inset = (sw + 1) // 2
    d.rounded_rectangle(
        [inset, inset, w - 1 - inset, h - 1 - inset],
        radius=radius, fill=fill, outline=stroke, width=sw)
    return im


def pill(im, dw, dh):
    # 先等比收到目标高度，圆头还是圆的；再只拉宽。
    scale = dh / im.height
    mid = im.resize((max(1, round(im.width * scale)), dh), Image.LANCZOS)
    cap = max(8, dh // 2)
    return slice9(mid, cap, max(6, dh // 7), cap, max(6, dh // 7), dw, dh)


def main():
    jobs = {
        "tab_dock": lambda im: slice9(tight(defringe(im)), 88, 70, 88, 70, 720, 220),
        "tab_plaque": lambda im: tight(defringe(im), 8),
        "panel_board": lambda im: slice9(tight(defringe(im)), 64, 64, 64, 64, 640, 348),
        "panel_card": lambda im: rounded(188, 132, 24, (255, 255, 255, 255), (58, 42, 36, 255), 7),
        "panel_card_on": lambda im: rounded(188, 132, 24, (255, 255, 255, 255), (192, 57, 43, 255), 8),
        "panel_card_dim": lambda im: rounded(188, 132, 24, (236, 232, 226, 255), (168, 158, 148, 255), 6),
        "panel_strip": lambda im: pill(tight(defringe(im)), 420, 56),
        "panel_row": lambda im: pill(tight(defringe(im)), 620, 92),
        "panel_price": lambda im: pill(tight(defringe(im)), 118, 42),
    }
    for name, fn in jobs.items():
        path = OUT / f"{name}.png"
        im = fn(Image.open(path))
        im = defringe(im)
        im.save(path, optimize=True)
        print(f"{name:<16} {im.size[0]:4}x{im.size[1]:<4}  {path.stat().st_size/1024:6.1f} KB")


if __name__ == "__main__":
    main()
