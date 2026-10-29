"""印章美术切图：色键抠掉洋红底，按连通块切出各个图标，圆章拆成金边和白色章面两层。

用法：.venv-mock/bin/python docs/prompt/ui/seal_cut.py <emblems_raw> <sockets_raw> <out_dir>
"""
import sys
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter

EMBLEMS = ["seal_ico_luck", "seal_ico_edge", "seal_ico_fierce", "seal_ico_wide",
           "seal_ico_last", "seal_ico_move", "seal_ico_keep", "seal_ico_lock"]
SOCKETS = ["seal_rim", "seal_empty", "seal_stud", "seal_dust"]
SIZES = {"seal_rim": 256, "seal_face": 256, "seal_empty": 192, "seal_stud": 64, "seal_dust": 64}


def bg_color(im):
    w, h = im.size
    px = [im.getpixel((x, y)) for x, y in ((4, 4), (w - 5, 4), (4, h - 5), (w - 5, h - 5))]
    return tuple(sum(p[i] for p in px) // len(px) for i in range(3))


def key(im, bg, hard=60, soft=150):
    """离底色越远越不透明。软边那一段把洋红溢色按比例减掉。"""
    im = im.convert("RGB")
    out = Image.new("RGBA", im.size)
    src = im.load()
    dst = out.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b = src[x, y]
            d = ((r - bg[0]) ** 2 + (g - bg[1]) ** 2 + (b - bg[2]) ** 2) ** 0.5
            if d <= hard:
                dst[x, y] = (0, 0, 0, 0)
                continue
            a = 1.0 if d >= soft else (d - hard) / (soft - hard)
            if a < 1.0:
                # 去掉混进来的底色
                r = int(max(0, min(255, (r - bg[0] * (1 - a)) / a)))
                g = int(max(0, min(255, (g - bg[1] * (1 - a)) / a)))
                b = int(max(0, min(255, (b - bg[2] * (1 - a)) / a)))
            dst[x, y] = (r, g, b, int(a * 255))
    return out


def blobs(rgba, min_area=400):
    """不透明像素的连通块，按先行后列排好（同一行的 y 容差取高度的 15%）。"""
    w, h = rgba.size
    a = rgba.split()[3].load()
    seen = bytearray(w * h)
    found = []
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            if a[x, y] < 128 or seen[y * w + x]:
                continue
            q = deque([(x, y)])
            seen[y * w + x] = 1
            x0 = x1 = x
            y0 = y1 = y
            n = 0
            while q:
                cx, cy = q.popleft()
                n += 1
                x0, x1, y0, y1 = min(x0, cx), max(x1, cx), min(y0, cy), max(y1, cy)
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx] and a[nx, ny] >= 40:
                        seen[ny * w + nx] = 1
                        q.append((nx, ny))
            if n >= min_area:
                found.append((x0, y0, x1, y1, n))
    return found


def merge(boxes, gap):
    """把离得很近的块并成一个图标（星星旁边的小星、钥匙孔等）。"""
    boxes = [list(b[:4]) for b in boxes]
    changed = True
    while changed:
        changed = False
        for i in range(len(boxes)):
            for j in range(i + 1, len(boxes)):
                a, b = boxes[i], boxes[j]
                if a[0] - gap <= b[2] and b[0] - gap <= a[2] and a[1] - gap <= b[3] and b[1] - gap <= a[3]:
                    boxes[i] = [min(a[0], b[0]), min(a[1], b[1]), max(a[2], b[2]), max(a[3], b[3])]
                    boxes.pop(j)
                    changed = True
                    break
            if changed:
                break
    return boxes


def order(boxes, h):
    tol = h * 0.15
    boxes.sort(key=lambda b: (b[1] + b[3]) / 2)
    rows = []
    for b in boxes:
        cy = (b[1] + b[3]) / 2
        if rows and abs(rows[-1][0] - cy) < tol:
            rows[-1][1].append(b)
        else:
            rows.append([cy, [b]])
    out = []
    for _, r in rows:
        out.extend(sorted(r, key=lambda b: b[0]))
    return out


def fit(im, size, fill=0.92):
    """裁掉空白，最长边放到 size 的 fill，居中贴进正方形透明画布。"""
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    s = size * fill / max(im.size)
    im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size))
    canvas.alpha_composite(im, ((size - im.width) // 2, (size - im.height) // 2))
    return canvas


def split_face(rim):
    """从圆章中心往外泛洪，近白的连通区域就是章面。章面单独出一张白图，金边上挖空。"""
    w, h = rim.size
    px = rim.load()
    face = Image.new("RGBA", rim.size)
    fp = face.load()
    q = deque([(w // 2, h // 2)])
    seen = set(q)
    while q:
        x, y = q.popleft()
        r, g, b, a = px[x, y]
        if a < 200 or min(r, g, b) < 215:
            continue
        fp[x, y] = (255, 255, 255, 255)
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= n[0] < w and 0 <= n[1] < h and n not in seen:
                seen.add(n)
                q.append(n)
    # 章面往外胀两像素，盖住金边内沿那圈抗锯齿的白边。
    mask = face.split()[3].filter(ImageFilter.MaxFilter(5))
    face = Image.new("RGBA", rim.size, (255, 255, 255, 0))
    face.putalpha(mask)
    hole = rim.copy()
    ha = hole.split()[3]
    ha.paste(0, mask=mask)
    hole.putalpha(ha)
    return hole, face


def run(emblems_path, sockets_path, out_dir):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)

    raw = Image.open(emblems_path).convert("RGB")
    cut = key(raw, bg_color(raw))
    boxes = order(merge(blobs(cut), gap=int(raw.width * 0.03)), raw.height)
    if len(boxes) != len(EMBLEMS):
        raise SystemExit(f"emblems: found {len(boxes)} blobs, want {len(EMBLEMS)}")
    for name, b in zip(EMBLEMS, boxes):
        fit(cut.crop((b[0], b[1], b[2] + 1, b[3] + 1)), 128).save(out / f"{name}.png")

    raw = Image.open(sockets_path).convert("RGB")
    cut = key(raw, bg_color(raw))
    boxes = order(merge(blobs(cut), gap=int(raw.width * 0.02)), raw.height)
    if len(boxes) != len(SOCKETS):
        raise SystemExit(f"sockets: found {len(boxes)} blobs, want {len(SOCKETS)}")
    for name, b in zip(SOCKETS, boxes):
        pad = 6
        part = cut.crop((max(0, b[0] - pad), max(0, b[1] - pad), b[2] + 1 + pad, b[3] + 1 + pad))
        if name == "seal_rim":
            part = fit(part, SIZES[name], 0.96)
            rim, face = split_face(part)
            rim.save(out / "seal_rim.png")
            face.save(out / "seal_face.png")
            continue
        fill = 0.7 if name == "seal_dust" else 0.94
        fit(part, SIZES[name], fill).save(out / f"{name}.png")
    print("ok", out)


if __name__ == "__main__":
    run(*sys.argv[1:4])
