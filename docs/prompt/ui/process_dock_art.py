#!/usr/bin/env python3
"""把底栏卷轴 / 三个页签图标从洋红底切成透明 PNG，写入 Resources/Art/Ui。"""
import os
import sys

from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "..", "Assets", "Resources", "Art", "Ui"))


def bg_color(img):
    w, h = img.size
    px = img.load()
    samples = []
    for x in range(0, w, 4):
        samples.append(px[x, 0][:3])
        samples.append(px[x, h - 1][:3])
    for y in range(0, h, 4):
        samples.append(px[0, y][:3])
        samples.append(px[w - 1, y][:3])
    samples.sort()
    return samples[len(samples) // 2]


def key_out(cell, bg, t0=58.0, t1=104.0):
    cell = cell.convert("RGBA")
    w, h = cell.size
    px = cell.load()
    br, bg_, bb = bg
    alpha = Image.new("L", (w, h))
    ap = alpha.load()
    span = max(1.0, t1 - t0)
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            d = ((r - br) ** 2 + (g - bg_) ** 2 + (b - bb) ** 2) ** 0.5
            t = (d - t0) / span
            ap[x, y] = 0 if t <= 0 else (255 if t >= 1 else int(t * 255))
    k = 3 if w < 200 else (5 if w < 400 else 7)
    alpha = alpha.filter(ImageFilter.MinFilter(k))
    alpha = alpha.filter(ImageFilter.GaussianBlur(max(0.6, w / 420.0)))
    cell.putalpha(alpha)
    return cell


def flatten(img, colors=8):
    if colors <= 0:
        return img
    a = img.split()[3]
    rgb = img.convert("RGB")
    from collections import Counter
    px, ap = rgb.load(), a.load()
    hist = Counter()
    for y in range(0, img.height, 2):
        for x in range(0, img.width, 2):
            if ap[x, y] > 200:
                hist[px[x, y]] += 1
    fill = hist.most_common(1)[0][0] if hist else (128, 128, 128)
    for y in range(img.height):
        for x in range(img.width):
            if ap[x, y] < 8:
                px[x, y] = fill
    q = rgb.quantize(colors=colors, method=Image.Quantize.MEDIANCUT,
                     dither=Image.Dither.NONE).convert("RGB")
    out = q.convert("RGBA")
    out.putalpha(a)
    return out


def normalize_square(img, canvas=128, content=0.82):
    bbox = img.getbbox()
    if bbox is None:
        return Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    art = img.crop(bbox)
    box = int(canvas * content)
    scale = box / max(art.width, art.height)
    art = art.resize((max(1, round(art.width * scale)),
                      max(1, round(art.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    out.paste(art, ((canvas - art.width) // 2, (canvas - art.height) // 2))
    return out


def process_dock(src, dest_w=720):
    sheet = Image.open(src).convert("RGBA")
    cut = flatten(key_out(sheet, bg_color(sheet)), 10)
    bbox = cut.getbbox()
    art = cut.crop(bbox)
    pad = 8
    framed = Image.new("RGBA", (art.width + pad * 2, art.height + pad * 2), (0, 0, 0, 0))
    framed.paste(art, (pad, pad))
    h = max(1, round(framed.height * dest_w / framed.width))
    out = framed.resize((dest_w, h), Image.LANCZOS)
    path = os.path.join(OUT, "tab_dock.png")
    out.save(path, optimize=True)
    print(f"tab_dock {out.size}  {os.path.getsize(path)/1024:.1f} KB")
    return out.size


def process_icons(src, names, cols=3, rows=2, take=3):
    sheet = Image.open(src).convert("RGBA")
    bg = bg_color(sheet)
    cw, ch = sheet.width // cols, sheet.height // rows
    for i, name in enumerate(names[:take]):
        cx, cy = i % cols, i // cols
        cell = sheet.crop((cx * cw, cy * ch, (cx + 1) * cw, (cy + 1) * ch))
        cut = flatten(key_out(cell, bg), 8)
        final = normalize_square(cut)
        path = os.path.join(OUT, name + ".png")
        final.save(path, optimize=True)
        print(f"{name}  {os.path.getsize(path)/1024:.1f} KB")


def main():
    os.makedirs(OUT, exist_ok=True)
    process_dock(os.path.join(RAW, "tab_dock.png"))
    process_icons(os.path.join(RAW, "tab_icons_dock.png"),
                  ["ico_tab_forge", "ico_tab_sortie", "ico_tab_spell"])


if __name__ == "__main__":
    sys.exit(main())
