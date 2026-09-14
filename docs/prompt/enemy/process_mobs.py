#!/usr/bin/env python3
"""把 Gemini 出的常规敌人母版切成单张透明 PNG，送进 Assets/Resources/Art/。

抠底和剔碎片的逻辑跟 process_bosses.py 是同一套，直接 import 过来用，
不要复制粘贴 —— 那两个函数里的腐蚀量、连通块判据都是踩坑调出来的。

只有两点不一样：
1. MAXDIM 小一档。常规敌人半径 0.22~0.44，实机只占四五十像素，
   256 纯属浪费；boss 半径 0.62 才需要 256。
2. 多一步 fix_eye_tint。洋红底会把眼白染出粉调（`boss_fix.png` 那张四只里
   三只中招）。boss 那批是靠挑格子避开的，这批十张躲不过来，只能后处理修。

    .venv-mock/bin/python docs/prompt/enemy/process_mobs.py
"""
import os
import shutil
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from process_bosses import DEST, FINAL, RAW, drop_bleed, key_out  # noqa: E402

MAXDIM = 224
PAD = 0.04

# (成品名, 母版, 列, 行, 总列, 总行)
# runner / strafer / shield / elite 是替换现有的纯墨版 —— 按色阶表补上配件。
# shield 那张顺带修掉底部那块游离残片（原图切图时带进来的别人半个头）。
# walker 和 swarm 是 T0 纯墨，现有图本来就对，不在这张表里。
CELLS = [
    ("runner",   "mob_a_v2.png", 0, 0, 2, 2),  # 快脚   T1 红：红头巾
    ("crawler",  "mob_a_v2.png", 1, 0, 2, 2),  # 爬子   T1 黄：黄护膝，四肢爬行
    ("strafer",  "mob_a_v2.png", 0, 1, 2, 2),  # 游墨   T1 青：青斗篷
    ("shield",   "mob_a_v2.png", 1, 1, 2, 2),  # 铜盾   T2 铜+褐：铜盾
    ("splitter", "mob_b.png",    0, 0, 2, 2),  # 双生   T2 紫+白：紫背带裤+白袖标+裂缝
    ("sprinter", "mob_b.png",    1, 0, 2, 2),  # 惊风   T2 橙+红：橙护腕+红飘带
    ("mender",   "mob_b.png",    0, 1, 2, 2),  # 补墨   T2 绿+白：绿围裙白十字+白帽
    ("bulwark",  "mob_b.png",    1, 1, 2, 2),  # 厚甲   T2 灰蓝+铜：铁板+铜铆钉
    ("elite",    "mob_c.png",    0, 0, 2, 1),  # 墨尊   T3：靛蓝斗篷+铜肩甲+金腰带
    ("warden",   "mob_c.png",    1, 0, 2, 1),  # 镇守   T3：红旗+铁链+墨绿披风
]


def fix_eye_tint(img):
    """把被洋红底染粉的眼白拉回中性白。

    判据卡得很窄：**高亮 + 近白 + 红蓝同时明显高于绿**。这批的配色里没有
    浅粉色物件（红/黄/绿/青/紫/铜/金/灰蓝），所以不会误伤；紫背带裤的绿通道
    在 80 上下，离 170 的门槛很远。
    """
    px = img.load()
    w, h = img.size
    n = 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 200 or g < 170 or r < 205 or b < 205:
                continue
            if r - g > 12 and b - g > 12:
                v = max(r, g, b)
                px[x, y] = (v, v, v, a)
                n += 1
    return img, n


def normalize(img):
    bb = img.getbbox()
    if bb:
        img = img.crop(bb)
    pad = int(max(img.size) * PAD)
    canvas = Image.new("RGBA", (img.width + pad * 2, img.height + pad * 2), (0, 0, 0, 0))
    canvas.paste(img, (pad, pad), img)
    k = MAXDIM / max(canvas.size)
    if k < 1:
        canvas = canvas.resize((max(1, round(canvas.width * k)),
                                max(1, round(canvas.height * k))), Image.LANCZOS)
    return canvas


def main():
    os.makedirs(FINAL, exist_ok=True)
    sheets = {}
    for name, src, cx, cy, cols, rows in CELLS:
        if src not in sheets:
            sheets[src] = Image.open(os.path.join(RAW, src)).convert("RGBA")
        sheet = sheets[src]
        cw, chh = sheet.width // cols, sheet.height // rows
        cell = sheet.crop((cx * cw, cy * chh, (cx + 1) * cw, (cy + 1) * chh))

        img = key_out(cell)
        img, dropped = drop_bleed(img)
        img = normalize(img)
        img, eyes = fix_eye_tint(img)

        fp = os.path.join(FINAL, name + ".png")
        img.save(fp, optimize=True)
        shutil.copyfile(fp, os.path.join(DEST, name + ".png"))
        print(f"{name:<10} {src:<13} {str(img.size):<11} {os.path.getsize(fp)//1024:>3}KB"
              f"  碎片={dropped}  眼白修正={eyes}px")

    # 接触图：上排 T0/T1/T2 低阶，下排 T2 高阶和 T3，用来核对色阶递进
    order = ["walker", "swarm", "runner", "crawler", "strafer",
             "shield", "splitter", "sprinter", "mender", "bulwark",
             "elite", "warden"]
    cw = 210
    sheet = Image.new("RGBA", (cw * 6, cw * 2), (248, 245, 238, 255))
    for i, name in enumerate(order):
        p = os.path.join(DEST, name + ".png")
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGBA")
        k = (cw * 0.84) / max(im.size)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))),
                       Image.LANCZOS)
        cxp = (i % 6) * cw + cw // 2
        cyp = (i // 6) * cw + cw // 2
        sheet.paste(im, (cxp - im.width // 2, cyp - im.height // 2), im)
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
    cp = os.path.join(repo, "docs", "美术", "敌人", "_常规敌人接触图.png")
    sheet.convert("RGB").save(cp)
    print("接触图 ->", cp)


if __name__ == "__main__":
    main()
