#!/usr/bin/env python3
"""道具页美术：道具栏木架、栏位托盘、品质卡框、紫钻图标。洋红底键控成透明 PNG，裁边后缩到入库尺寸。

    .venv-mock/bin/python docs/prompt/ui/process_item_page.py
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from process_codex_art import RAW, save as _save, split  # noqa: E402

EDGE = (0x3A, 0x2A, 0x20)


# 洋红键控后半透明的外沿还带一点粉，统一染成描边色，贴在浅底上才不起粉边。
def despill(im, magenta=True, edge=EDGE):
    im = im.convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if 0 < a < 250 or (magenta and a > 0 and r > 150 and b > 150 and g < 90):
                px[x, y] = edge + (a,)
    return im


def save(items, magenta=True, edge=EDGE):
    _save([(n, despill(im, magenta, edge)) for n, im in items])


def width(items, w):
    out = []
    for name, im in items:
        h = max(1, round(im.height * w / im.width))
        out.append((name, im.resize((w, h), Image.LANCZOS)))
    return out


def round_mask(size, radius):
    from PIL import ImageDraw
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius, fill=255)
    return m


# 红牌子单独成图：画在九宫格里时它跟着边框缩放，引擎里算出来的位置和标题对不上。
# 牌子挖出来，原位用左边同一行的木边补齐。
def split_plaque(shelf):
    px = shelf.load()
    reds = [(x, y) for y in range(shelf.height // 4) for x in range(shelf.width)
            if px[x, y][3] > 200 and px[x, y][0] > 180 and px[x, y][1] < 70 and px[x, y][2] < 70]
    x0 = min(x for x, _ in reds) - 7
    x1 = max(x for x, _ in reds) + 7
    y0 = max(0, min(y for _, y in reds) - 7)
    y1 = max(y for _, y in reds) + 8
    box = (x0, y0, x1, y1)
    plaque = shelf.crop(box)
    plaque.putalpha(Image.composite(plaque.getchannel("A"), Image.new("L", plaque.size, 0),
                                    round_mask(plaque.size, 20)))
    rim = shelf.copy()
    src = x0 - (x1 - x0) - 20
    patch = shelf.crop((src, 0, src + (x1 - x0), y1 + 4))
    rim.paste(patch, (x0, 0))
    bbox = rim.getbbox()
    rim = rim.crop(bbox)
    print("  plaque", box, "rim bbox", bbox)
    return rim, plaque


def main():
    shelf = width(split(os.path.join(RAW, "item_shelf_builtin.jpg"), 1, 1, ["panel_item_shelf"],
                        flat=0, t0=36.0, t1=88.0), 900)[0][1]
    rim, plaque = split_plaque(despill(shelf))
    save([("panel_item_shelf", rim), ("panel_item_plaque", plaque)])
    slots = split(os.path.join(RAW, "item_slots_builtin.jpg"), 2, 2,
                  ["item_slot", "item_slot_on", "item_slot_lock", "item_lv_badge"],
                  canvas=256, content=0.96, flat=0, t0=36.0, t1=88.0)
    name, badge = slots[3]
    slots[3] = (name, badge.crop(badge.getbbox()))
    save(slots[:3] + width(slots[3:], 160))
    save(width(split(os.path.join(RAW, "item_cards_builtin.jpg"), 2, 2,
                     ["panel_item_card_green", "panel_item_card_blue", "panel_item_card_purple", "panel_item_card_lock"],
                     flat=0, t0=36.0, t1=88.0), 320))
    # 玫红钻和道具卡空卡面用绿底生成：洋红底会把粉色一起抠掉。
    save(split(os.path.join(RAW, "ico_diamond_rose_builtin.jpg"), 1, 1, ["ico_diamond"],
               canvas=256, content=0.86, flat=0, t0=36.0, t1=88.0), magenta=False)
    save(split(os.path.join(RAW, "ico_card_blank_builtin.jpg"), 1, 1, ["ico_card_blank"],
               canvas=256, content=0.92, flat=0, t0=36.0, t1=88.0), magenta=False)
    # 底栏「道具」页签：挎包里露出鞭炮和闹钟。写回 ico_tab_spell，预制里的引用不用改。
    # 这张洋红偏深，离鞭炮的红近，键控阈值收紧。
    save(split(os.path.join(RAW, "ico_tab_item_builtin.jpg"), 1, 1, ["ico_tab_spell"],
               canvas=192, content=0.92, flat=0, t0=20.0, t1=46.0), edge=(255, 255, 255))


if __name__ == "__main__":
    main()
