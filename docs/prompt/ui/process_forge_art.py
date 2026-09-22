#!/usr/bin/env python3
"""把炮台页 / 底栏生图切成透明 PNG，写入 Resources/Art/Ui。

大面板不去量化，保住原型那种干净平涂；图标才压到正方形画布。
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

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


def key_out(cell, bg, t0=48.0, t1=92.0):
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
    alpha = alpha.filter(ImageFilter.GaussianBlur(max(0.5, w / 480.0)))
    cell.putalpha(alpha)
    return defringe(cell)


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
    for _ in range(3):
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


def framed(img, pad=10):
    bbox = img.getbbox()
    if bbox is None:
        return Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    art = img.crop(bbox)
    out = Image.new("RGBA", (art.width + pad * 2, art.height + pad * 2), (0, 0, 0, 0))
    out.paste(art, (pad, pad))
    return out


def save_w(img, name, dest_w):
    if dest_w and img.width != dest_w:
        h = max(1, round(img.height * dest_w / img.width))
        img = img.resize((dest_w, h), Image.LANCZOS)
    path = os.path.join(OUT, name + ".png")
    img.save(path, optimize=True)
    print(f"{name:<18} {img.size[0]:4}x{img.size[1]:<4}  {os.path.getsize(path)/1024:6.1f} KB")


def save_square(img, name, canvas=160, content=0.88):
    bbox = img.getbbox()
    if bbox is None:
        out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    else:
        art = img.crop(bbox)
        box = int(canvas * content)
        scale = box / max(art.width, art.height)
        art = art.resize((max(1, round(art.width * scale)),
                          max(1, round(art.height * scale))), Image.LANCZOS)
        out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
        out.paste(art, ((canvas - art.width) // 2, (canvas - art.height) // 2))
    path = os.path.join(OUT, name + ".png")
    out.save(path, optimize=True)
    print(f"{name:<18} {out.size[0]:4}x{out.size[1]:<4}  {os.path.getsize(path)/1024:6.1f} KB")


def split_band(sheet, y0, y1, names, widths):
    band = sheet.crop((0, y0, sheet.width, y1))
    bg = bg_color(sheet)
    cw = band.width // len(names)
    for i, name in enumerate(names):
        cell = band.crop((i * cw, 0, (i + 1) * cw, band.height))
        save_w(framed(key_out(cell, bg)), name, widths[i])


def split(src, names, cols, rows, square=False, widths=None):
    sheet = Image.open(src).convert("RGBA")
    bg = bg_color(sheet)
    cw, ch = sheet.width // cols, sheet.height // rows
    for i, name in enumerate(names):
        cx, cy = i % cols, i // cols
        cell = sheet.crop((cx * cw, cy * ch, (cx + 1) * cw, (cy + 1) * ch))
        cut = framed(key_out(cell, bg))
        if square:
            save_square(cut, name)
        else:
            w = None if not widths else widths[i]
            save_w(cut, name, w)


def panel(src, name, dest_w):
    sheet = Image.open(src).convert("RGBA")
    save_w(framed(key_out(sheet, bg_color(sheet))), name, dest_w)


def save_h(img, name, dest_h):
    """按高等比缩放。胶囊 / 卡片的圆角要留给 Unity 九宫格横向拉，
    这里非等比压一下，端头就成椭圆了。"""
    if dest_h and img.height != dest_h:
        w = max(1, round(img.width * dest_h / img.height))
        img = img.resize((w, dest_h), Image.LANCZOS)
    path = os.path.join(OUT, name + ".png")
    img.save(path, optimize=True)
    print(f"{name:<18} {img.size[0]:4}x{img.size[1]:<4}  {os.path.getsize(path)/1024:6.1f} KB")


def recolor(img, src, dst, tol=48):
    img = img.convert("RGBA")
    px = img.load()
    sr, sg, sb = src
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a < 16:
                continue
            if abs(r - sr) <= tol and abs(g - sg) <= tol and abs(b - sb) <= tol:
                px[x, y] = (dst[0], dst[1], dst[2], a)
    return img


def blobs(sheet):
    """把洋红大底上的几块壳按「先上后左」列出来，返回 bbox 列表。

    生图不会给均分格子（这批就是上两条通栏 + 下面 2×2），
    所以按行投影切带，再在带里按列投影切块。
    """
    w, h = sheet.size
    px = sheet.convert("RGB").load()
    solid = [[False] * w for _ in range(h)]
    for y in range(h):
        row = solid[y]
        for x in range(w):
            r, g, b = px[x, y]
            row[x] = not magenta(r, g, b)
    out = []
    y = 0
    while y < h:
        if not any(solid[y]):
            y += 1
            continue
        y0 = y
        while y < h and any(solid[y]):
            y += 1
        y1 = y
        cols = [any(solid[yy][x] for yy in range(y0, y1)) for x in range(w)]
        x = 0
        while x < w:
            if not cols[x]:
                x += 1
                continue
            x0 = x
            while x < w and cols[x]:
                x += 1
            if (x - x0) > 40 and (y1 - y0) > 40:
                out.append((x0, y0, x, y1))
    return out


def shell(sheet, box, pad=10):
    x0, y0, x1, y1 = box
    x0, y0 = max(0, x0 - pad), max(0, y0 - pad)
    x1, y1 = min(sheet.width, x1 + pad), min(sheet.height, y1 + pad)
    cell = sheet.crop((x0, y0, x1, y1))
    return framed(key_out(cell, bg_color(sheet)), pad=4)


def forge_v2():
    """炮台页整套壳：带字牌的棕盘、皮肤卡、名牌、词条、价签。"""
    os.makedirs(OUT, exist_ok=True)

    board_src = os.path.join(RAW, "forge_board_v2.png")
    if os.path.exists(board_src):
        sheet = Image.open(board_src).convert("RGBA")
        save_w(shell(sheet, blobs(sheet)[0], pad=6), "panel_board", 640)

    cards_src = os.path.join(RAW, "forge_cards_v2.png")
    if os.path.exists(cards_src):
        sheet = Image.open(cards_src).convert("RGBA")
        found = blobs(sheet)
        names = [("panel_card", 180), ("panel_card_on", 180),
                 ("panel_card_dim", 180), ("panel_name", 52)]
        for box, (name, dest_h) in zip(found, names):
            save_h(shell(sheet, box), name, dest_h)

    pills_src = os.path.join(RAW, "forge_pills_v2.png")
    if os.path.exists(pills_src):
        sheet = Image.open(pills_src).convert("RGBA")
        found = blobs(sheet)
        wide = [b for b in found if (b[2] - b[0]) > sheet.width * 0.6]
        short = [b for b in found if (b[2] - b[0]) <= sheet.width * 0.6]
        for box, name in zip(wide, ("panel_row", "panel_row_lock")):
            save_h(shell(sheet, box), name, 88)
        for box, name in zip(short, ("panel_price", "panel_price_off")):
            cut = shell(sheet, box)
            # 生图那版金是橙的；原型价签是铜钱那种黄（253,197,66），按面填回去
            if name == "panel_price":
                cut = recolor(cut, (206, 134, 52), (253, 197, 66), tol=52)
            save_h(cut, name, 44)


def tab_icons_v2():
    src = os.path.join(RAW, "tab_icons_v2.png")
    if not os.path.exists(src):
        return
    os.makedirs(OUT, exist_ok=True)
    sheet = Image.open(src).convert("RGBA")
    names = ("ico_tab_forge", "ico_tab_sortie", "ico_tab_spell")
    for box, name in zip(blobs(sheet), names):
        save_square(shell(sheet, box, pad=4), name, canvas=192, content=0.92)


def cards_thin():
    src = os.path.join(RAW, "forge_cards_thin.png")
    if not os.path.exists(src):
        return
    os.makedirs(OUT, exist_ok=True)
    sheet = Image.open(src).convert("RGBA")
    found = blobs(sheet)
    names = [("panel_card", 128), ("panel_card_on", 128), ("panel_card_dim", 128)]
    for box, (name, dest_h) in zip(found, names):
        save_h(shell(sheet, box), name, dest_h)


def nine(img, dw, dh):
    img = img.convert("RGBA")
    w, h = img.size
    cap = min(w, h) // 2
    cap_d = min(dw, dh) // 2
    out = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    tiles = (
        ((0, 0, cap, cap), (0, 0, cap_d, cap_d)),
        ((w - cap, 0, w, cap), (dw - cap_d, 0, dw, cap_d)),
        ((0, h - cap, cap, h), (0, dh - cap_d, cap_d, dh)),
        ((w - cap, h - cap, w, h), (dw - cap_d, dh - cap_d, dw, dh)),
        ((cap, 0, w - cap, cap), (cap_d, 0, dw - cap_d, cap_d)),
        ((cap, h - cap, w - cap, h), (cap_d, dh - cap_d, dw - cap_d, dh)),
        ((0, cap, cap, h - cap), (0, cap_d, cap_d, dh - cap_d)),
        ((w - cap, cap, w, h - cap), (dw - cap_d, dh - cap_d, dw, dh - cap_d)),
        ((cap, cap, w - cap, h - cap), (cap_d, cap_d, dw - cap_d, dh - cap_d)),
    )
    for src, dst in tiles:
        sw, sh = src[2] - src[0], src[3] - src[1]
        tw, th = dst[2] - dst[0], dst[3] - dst[1]
        if sw < 1 or sh < 1 or tw < 1 or th < 1:
            continue
        out.paste(img.crop(src).resize((tw, th), Image.LANCZOS), dst[:2])
    return out


def gold_fill(img):
    img = img.convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 16 or max(r, g, b) - min(r, g, b) < 12:
                continue
            if r > 140 and g > 90 and b < 160:
                px[x, y] = (236, 186, 74, a)
    return img


def rows():
    """竖胶囊转成横词条：白行、锁定灰行、铜币价签。"""
    os.makedirs(OUT, exist_ok=True)
    src = os.path.join(RAW, "forge_rows.png")
    if not os.path.exists(src):
        return
    sheet = Image.open(src).convert("RGBA")
    bg = bg_color(sheet)
    specs = (("panel_row", 640, 88), ("panel_row_lock", 640, 88), ("panel_price", 140, 40))
    cw, ch = sheet.width // 3, sheet.height
    for i, (name, dw, dh) in enumerate(specs):
        cell = sheet.crop((i * cw, 0, (i + 1) * cw, ch)).rotate(90, expand=True)
        cut = framed(key_out(cell, bg))
        if name == "panel_price":
            cut = gold_fill(cut)
        save_w(nine(cut, dw, dh), name, None)


def cards_only():
    os.makedirs(OUT, exist_ok=True)
    cards = os.path.join(RAW, "forge_cards.png")
    if os.path.exists(cards):
        split(cards, ["panel_card", "panel_card_on", "panel_card_dim"],
              3, 1, widths=[320, 320, 320])


def board_tagged():
    os.makedirs(OUT, exist_ok=True)
    src = os.path.join(RAW, "forge_board_tagged.png")
    if os.path.exists(src):
        panel(src, "panel_board", 688)


def spell():
    """技能页：带技印的竹竿 + 空符纸（素面 / 红框）。"""
    os.makedirs(OUT, exist_ok=True)
    rod = os.path.join(RAW, "spell_rod_only.png")
    if not os.path.exists(rod):
        rod = os.path.join(RAW, "spell_rod.png")
    if os.path.exists(rod):
        sheet = Image.open(rod).convert("RGBA")
        found = blobs(sheet)
        if not found:
            raise SystemExit("spell_rod: no art on magenta")
        save_w(shell(sheet, found[0], pad=4), "panel_spell_rod", 720)

    seal = os.path.join(RAW, "spell_seal.png")
    if os.path.exists(seal):
        sheet = Image.open(seal).convert("RGBA")
        found = blobs(sheet)
        if found:
            save_square(shell(sheet, found[0], pad=4), "panel_spell_seal", canvas=192, content=0.92)

    tags = os.path.join(RAW, "spell_tags.png")
    if not os.path.exists(tags):
        return
    sheet = Image.open(tags).convert("RGBA")
    found = blobs(sheet)
    if len(found) < 2:
        raise SystemExit("spell_tags: need two hanging tags")

    def red_score(box):
        x0, y0, x1, y1 = box
        cell = sheet.crop((x0, y0, x1, y1)).convert("RGB")
        px = cell.load()
        n = 0
        for y in range(cell.height):
            for x in range(cell.width):
                r, g, b = px[x, y]
                if r > 160 and g < 110 and b < 110 and r - g > 50:
                    n += 1
        return n

    ranked = sorted(found, key=red_score)
    plain, framed = ranked[0], ranked[-1]
    # 牌子按宽缩，红绳留在图里。运行时按原比例摆，不要拉扁。
    # 生图那圈绳比原型长一截，压到指尖那么高，才吊得在竹竿上。
    save_w(solid_cord(shell(sheet, plain, pad=4)), "panel_spell_tag", 200)
    save_w(solid_cord(shell(sheet, framed, pad=4)), "panel_spell_tag_on", 200)

    cards = os.path.join(RAW, "spell_cards.png")
    if os.path.exists(cards):
        sheet = Image.open(cards).convert("RGBA")
        found = blobs(sheet)
        names = ("panel_spell_card", "panel_spell_card_on", "panel_spell_card_lock")
        for box, name in zip(found, names):
            save_w(tea_shadow(shell(sheet, box, pad=4)), name, 360)


def tea_shadow(img):
    """洋红底抠完会在投影上留一层紫边，改成湿茶叶那种褐。"""
    import colorsys
    img = img.convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 8:
                continue
            hv, s, _ = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            if 0.70 <= hv <= 0.95 and s > 0.18:
                px[x, y] = (0, 0, 0, 0)
    return framed(img, pad=4)


def thin_rod(img, pole_scale=0.40):
    """技印保持原大，只把横竿压成原型那种细竹篾。"""
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()
    red = []
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a > 80 and r > 150 and g < 120 and b < 120 and r - g > 40:
                red.append((x, y))
    if not red:
        return img
    sx0 = min(p[0] for p in red)
    sy0 = min(p[1] for p in red)
    sx1 = max(p[0] for p in red) + 1
    sy1 = max(p[1] for p in red) + 1
    cx, cy = (sx0 + sx1) / 2.0, (sy0 + sy1) / 2.0
    rad = max(sx1 - sx0, sy1 - sy0) * 0.58
    seal = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    pole = img.copy()
    sp, pp = seal.load(), pole.load()
    for y in range(h):
        for x in range(w):
            if (x - cx) ** 2 + (y - cy) ** 2 <= rad * rad and px[x, y][3] > 0:
                sp[x, y] = px[x, y]
                pp[x, y] = (0, 0, 0, 0)
    pb = pole.getbbox()
    if pb is None:
        return img
    pole = pole.crop(pb)
    new_h = max(8, int(pole.height * pole_scale))
    pole = pole.resize((pole.width, new_h), Image.LANCZOS)
    sb = seal.getbbox()
    seal = seal.crop(sb) if sb else seal
    out_h = max(seal.height, pole.height) + 8
    out_w = max(w, pole.width + pb[0])
    out = Image.new("RGBA", (out_w, out_h), (0, 0, 0, 0))
    out.paste(pole, (pb[0], (out_h - pole.height) // 2), pole)
    out.paste(seal, (sb[0], (out_h - seal.height) // 2), seal)
    return framed(out, pad=4)


def solid_cord(img, cord_w=34, cord_h=74):
    """空心细环换成一根实心粗红绳，中间填满，才吊得住。"""
    img = img.convert("RGBA")
    px = img.load()
    w, h = img.size
    paper_y = None
    for y in range(h):
        xs = [x for x in range(w) if px[x, y][3] > 20]
        if xs and xs[-1] - xs[0] > w * 0.55:
            paper_y = y
            break
    if paper_y is None:
        return img
    paper = img.crop((0, paper_y, w, h))
    overlap = 12
    out = Image.new("RGBA", (w, cord_h + paper.height - overlap), (0, 0, 0, 0))
    out.paste(paper, (0, cord_h - overlap), paper)
    draw = ImageDraw.Draw(out)
    cx = w / 2.0
    ink = (58, 42, 32, 255)
    red = (196, 46, 48, 255)
    # 先画绕竿的实心结，再画垂下来的实心绳，整根填满。
    wrap_w, wrap_h = 40, 26
    wx0, wy0 = cx - wrap_w / 2, 2
    draw.rounded_rectangle([wx0, wy0, wx0 + wrap_w, wy0 + wrap_h], radius=wrap_h / 2, fill=ink)
    draw.rounded_rectangle([wx0 + 3, wy0 + 3, wx0 + wrap_w - 3, wy0 + wrap_h - 3],
                           radius=(wrap_h - 6) / 2, fill=red)
    sx0, sy0 = cx - cord_w / 2, 16
    draw.rounded_rectangle([sx0, sy0, sx0 + cord_w, cord_h + 4], radius=cord_w / 2, fill=ink)
    draw.rounded_rectangle([sx0 + 3.5, sy0 + 2, sx0 + cord_w - 3.5, cord_h + 2],
                           radius=(cord_w - 7) / 2, fill=red)
    return out


def shorten_cord(img, keep=0.48):
    img = img.convert("RGBA")
    px = img.load()
    w, h = img.size
    spans = []
    for y in range(h):
        xs = [x for x in range(w) if px[x, y][3] > 20]
        spans.append((xs[0], xs[-1]) if xs else None)
    paper_y = None
    for y, span in enumerate(spans):
        if span and span[1] - span[0] > w * 0.55:
            paper_y = y
            break
    if paper_y is None or paper_y < 8:
        return img
    cord = img.crop((0, 0, w, paper_y))
    paper = img.crop((0, paper_y, w, h))
    ch = max(12, int(cord.height * keep))
    cord = cord.resize((w, ch), Image.LANCZOS)
    out = Image.new("RGBA", (w, ch + paper.height), (0, 0, 0, 0))
    out.paste(cord, (0, 0), cord)
    out.paste(paper, (0, ch), paper)
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    board_tagged()
    cards_only()
    chrome = Image.open(os.path.join(RAW, "forge_chrome.png")).convert("RGBA")
    split_band(chrome, 1264, 1576, ["panel_strip", "panel_row", "panel_price"],
               [512, 620, 200])
    rows()
    panel(os.path.join(RAW, "tab_dock.png"), "tab_dock", 720)
    split(os.path.join(RAW, "tab_parts.png"),
          ["ico_tab_forge", "ico_tab_sortie", "ico_tab_spell", "tab_plaque"],
          2, 2, square=True)
    cannons = os.path.join(RAW, "skin_cannons.png")
    if os.path.exists(cannons):
        split(cannons, ["ico_skin_plain", "ico_skin_cinnabar", "ico_skin_ghost"],
              3, 1, square=True)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "cards":
        cards_only()
    elif cmd == "board":
        board_tagged()
    elif cmd == "rows":
        rows()
    elif cmd == "v2":
        forge_v2()
    elif cmd == "cards_thin":
        cards_thin()
    elif cmd == "tabs":
        tab_icons_v2()
    elif cmd == "spell":
        spell()
    else:
        main()
