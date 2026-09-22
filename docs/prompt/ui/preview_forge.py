#!/usr/bin/env python3
"""按 HomeScreen.BuildForge 里的常量，用真实 PNG 合成炮台页预览。

只管炮台页，用来在没法跑 Play 模式时检查：卡片有没有掉出盘底、名牌有没有压住
门槛小字、词条和价签有没有挤出框。九宫格按 InkArtImporter 里那套 border 拉，
和 Unity 的 Image.Type.Sliced 一致。

    .venv-mock/bin/python docs/prompt/ui/preview_forge.py
"""
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
ART = os.path.join(REPO, "Assets", "Resources", "Art", "Ui")
FONT_BOLD = os.path.join(REPO, "Assets", "Resources", "Fonts", "InkBold.ttf")
FONT_REG = os.path.join(REPO, "Assets", "Resources", "Fonts", "Ink.ttf")
OUT = os.path.join(HERE, "raw", "preview_forge.png")

CANVAS_W, CANVAS_H = 720, 1558
TOP_BAR_Y, CHIP_H, CHROME_GAP = 52, 54, 6
BOTTOM_PAD, TAB_BAR_H = 24, 236

BOARD_W = 624
BOARD_H = BOARD_W * 415 / 640
FLOOR_DROP = BOARD_H * 7.5 / 415
CARD_W, CARD_H = 214, 142
ROW_H, ROW_GAP, STRIP_H = 88, 8, 50

BORDER = {
    "panel_card": (34, 34, 34, 34),
    "panel_card_on": (34, 34, 34, 34),
    "panel_card_dim": (34, 34, 34, 34),
    "panel_name": (26, 10, 26, 10),
    "panel_row": (44, 8, 44, 8),
    "panel_row_lock": (44, 8, 44, 8),
    "panel_price": (22, 8, 22, 8),
    "panel_price_off": (22, 8, 22, 8),
}

TEXT_DARK = (0x3A, 0x2A, 0x20)
TEXT_MID = (0x8E, 0x80, 0x6E)
TEXT_DIM = (0xA8, 0x9A, 0x88)
SEAL = (0xC0, 0x39, 0x2B)

ROWS = [
    ("damage", "伤害 Lv.0", "炮弹伤害 +8%", "需 20 星", False),
    ("rate", "射速 Lv.0", "开火间隔 -8%", "16 墨", True),
    ("gold", "开局金币 Lv.1", "开局金币 +2", "32 墨", True),
    ("hp", "基地生命 Lv.0", "基地生命 +1", "50 墨", True),
]
LOCKED = ("炮台数", "多一门炮", "累积 20 星")
SKINS = [("素笔", "使用中", "on"), ("朱砂", "需 6 星", "plain"),
         ("青瓷", "需 16 星", "dim"), ("鎏金", "通关", "dim")]

_fc = {}


def font(path, size):
    key = (path, size)
    if key not in _fc:
        _fc[key] = ImageFont.truetype(path, size)
    return _fc[key]


def art(name):
    return Image.open(os.path.join(ART, name + ".png")).convert("RGBA")


def sliced(name, dw, dh):
    """九宫格拉伸，和 Unity 的 Sliced 同一套算法。"""
    img = art(name)
    dw, dh = int(round(dw)), int(round(dh))
    if name not in BORDER:
        return img.resize((dw, dh), Image.LANCZOS)
    l, b, r, t = BORDER[name]
    w, h = img.size
    out = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, w - r, l, dw - r), (w - r, w, dw - r, dw)]
    ys = [(0, t, 0, t), (t, h - b, t, dh - b), (h - b, h, dh - b, dh)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            tile = img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS)
            out.alpha_composite(tile, (dx0, dy0))
    return out


def paste_mid(base, img, cx, cy):
    base.alpha_composite(img, (int(round(cx - img.width / 2)), int(round(cy - img.height / 2))))


def icon(base, name, cx, cy, size, alpha=255):
    img = art("ico_" + name)
    k = size / max(img.size)
    img = img.resize((max(1, round(img.width * k)), max(1, round(img.height * k))), Image.LANCZOS)
    if alpha < 255:
        a = img.getchannel("A").point(lambda v: v * alpha // 255)
        img.putalpha(a)
    paste_mid(base, img, cx, cy)


def text(d, s, cx, cy, size, color, bold=True, anchor="mm"):
    d.text((cx, cy), s, font=font(FONT_BOLD if bold else FONT_REG, size), fill=color, anchor=anchor)


def main():
    page_top = TOP_BAR_Y + CHIP_H + CHROME_GAP
    page_h = CANVAS_H - page_top - (BOTTOM_PAD + TAB_BAR_H)
    base = Image.new("RGBA", (CANVAS_W, CANVAS_H), (0xFC, 0xF2, 0xE2, 255))
    d = ImageDraw.Draw(base)

    # 顶栏两个药丸，只是占位，看高度关系
    for cx, label in ((CANVAS_W / 2 - 118, "12/12"), (CANVAS_W / 2 + 118, "50")):
        box = (cx - 100, TOP_BAR_Y, cx + 100, TOP_BAR_Y + CHIP_H)
        d.rounded_rectangle(box, radius=CHIP_H / 2, fill=(255, 255, 255), outline=TEXT_DARK, width=3)
        text(d, label, cx + 12, TOP_BAR_Y + CHIP_H / 2, 30, TEXT_DARK)

    board_top = page_top + 2
    board = sliced("panel_board", BOARD_W, BOARD_H)
    base.alpha_composite(board, (int((CANVAS_W - BOARD_W) / 2), int(board_top)))

    board_cy = board_top + BOARD_H / 2
    col_x = CARD_W / 2 + 10
    row_y = CARD_H / 2 + 9
    for i, (name, tail, kind) in enumerate(SKINS):
        cx = CANVAS_W / 2 + (-col_x if i % 2 == 0 else col_x)
        cy = board_cy + FLOOR_DROP + (-row_y if i // 2 == 0 else row_y)
        key = {"on": "panel_card_on", "plain": "panel_card", "dim": "panel_card_dim"}[kind]
        paste_mid(base, sliced(key, CARD_W, CARD_H), cx, cy)
        gun = "skin_plain" if kind == "on" else ("skin_cinnabar" if kind == "plain" else "skin_ghost")
        icon(base, gun, cx, cy - 12, 118)
        plate_cy = cy + CARD_H / 2 - 4
        paste_mid(base, sliced("panel_name", 96, 40), cx - 30, plate_cy)
        text(d, name, cx - 30, plate_cy, 20, TEXT_DARK if kind != "dim" else TEXT_DIM)
        if tail:
            text(d, tail, cx + 58 + 40, cy + CARD_H / 2 - 26, 16,
                 SEAL if kind == "on" else TEXT_DIM, anchor="rm")

    strip_top = board_top + BOARD_H + 8
    paste_mid(base, sliced("panel_name", 300, STRIP_H), CANVAS_W / 2, strip_top + STRIP_H / 2)
    text(d, "当前 2 门炮", CANVAS_W / 2, strip_top + STRIP_H / 2, 24, TEXT_DARK)

    list_top = strip_top + STRIP_H + 10
    view_h = page_top + page_h - list_top - 6
    d.rectangle((6, list_top, CANVAS_W - 6, list_top + view_h), outline=(0xD8, 0xC8, 0xB4), width=1)

    def row(y, ico, title, step, tail, live, locked=False):
        cy = y + ROW_H / 2
        paste_mid(base, sliced("panel_row_lock" if locked else "panel_row", 668, ROW_H),
                  CANVAS_W / 2, cy)
        icon(base, ico, CANVAS_W / 2 - 262, cy, 62 if not locked else 56, 128 if locked else 255)
        text(d, title, CANVAS_W / 2 - 50 - 170, cy - 14, 26, TEXT_DIM if locked else TEXT_DARK, anchor="lm")
        text(d, step, CANVAS_W / 2 - 50 - 170, cy + 16, 18, TEXT_DIM if locked else TEXT_MID,
             bold=False, anchor="lm")
        paste_mid(base, sliced("panel_price" if live else "panel_price_off", 146, 44),
                  CANVAS_W / 2 + 238, cy)
        if live:
            icon(base, "ink", CANVAS_W / 2 + 238 - 46, cy, 26)
        text(d, tail, CANVAS_W / 2 + 238 + (12 if live else 0), cy, 20,
             TEXT_DARK if live else TEXT_MID)

    y = list_top
    for ico, title, step, tail, live in ROWS:
        row(y, ico, title, step, tail, live)
        y += ROW_H + ROW_GAP
    row(y, "lock", LOCKED[0], LOCKED[1], LOCKED[2], False, locked=True)
    y += ROW_H

    tab_top = CANVAS_H - BOTTOM_PAD - TAB_BAR_H
    dock = sliced("tab_dock", CANVAS_W, TAB_BAR_H - 16)
    base.alpha_composite(dock, (0, int(tab_top)))
    print(f"page {page_top:.0f}..{page_top + page_h:.0f}  列表底 {y:.0f}  视口底 {list_top + view_h:.0f}")
    base.convert("RGB").save(OUT, quality=92)
    print("saved", OUT)


if __name__ == "__main__":
    main()
