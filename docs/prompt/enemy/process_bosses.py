#!/usr/bin/env python3
"""把 Gemini 出的 boss 母版切成单张透明 PNG，送进 Assets/Resources/Art/。

和界面图标那条管线的区别：**这里不做调色板量化**。现有敌人图本身就有柔和的
灰阶明暗（见 walker.png / boss.png），压成硬边平涂反而会和战场上其他敌人脱节。
图标那次要压是因为模型偷偷给球体加径向渐变，这次的渐变是我们要的。

沿用的教训：抠洋红时**腐蚀量必须跟着格子尺寸走**。格子 512px 时溢色带有好几像素
宽，只腐蚀 1px 会把洋红残留留在黑描边里。

    .venv-mock/bin/python docs/prompt/enemy/process_bosses.py
"""
import os
import shutil

from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
RAW = "/Users/rosa/rosa_games/game_assets/black-rosa/assets/raw"
FINAL = "/Users/rosa/rosa_games/game_assets/black-rosa/assets/final"
DEST = os.path.join(REPO, "Assets", "Resources", "Art")

MAXDIM = 256    # 导入器 maxTextureSize 就是 256，再大也会被缩
PAD = 0.04      # 四周留一点空，免得描边贴边被采样裁掉

# (成品名, 母版, 列, 行, 总列, 总行)  —— 逐格挑过的最好一版
CELLS = [
    ("boss_drum",    "boss_1_4.png",     0, 0, 2, 2),  # 鼓面：黄鼓皮+红鼓绳+铜钉
    ("boss_inkbag",  "boss_1_4.png",     1, 0, 2, 2),  # 墨囊：紫墨囊+绿束带+铜提环
    ("boss_iron",    "boss_1_4.png",     0, 1, 2, 2),  # 铁桶：灰蓝铁板+铜面罩+红缨
    ("boss_twin",    "boss_1_4.png",     1, 1, 2, 2),  # 双首：青巾+黄巾+铜锁链
    ("boss_warden",  "boss_5_8_v2.png",  0, 0, 2, 2),  # 牢头：铁链+褐皮裙+铜钥匙
    ("boss_medic",   "boss_5_8_v2.png",  1, 0, 2, 2),  # 墨医：白袍+绿十字+药囊
    ("boss_thunder", "boss_fix.png",     1, 0, 2, 2),  # 奔雷：橙红飘带+铜护腕+金脚环
    ("boss_king",    "boss_king.png",    0, 0, 1, 1),  # 墨王：金冠+朱红袍+铜甲+玉佩
]


def key_out(cell, t0=58.0, t1=104.0):
    """洋红键控。按色距算 alpha，腐蚀去彩边，再轻微羽化。"""
    w, h = cell.size
    rgb = cell.convert("RGB")
    px = rgb.load()
    alpha = Image.new("L", (w, h), 255)
    ap = alpha.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            # 洋红的特征：R、B 高而 G 低
            d = ((255 - r) ** 2 + g ** 2 + (255 - b) ** 2) ** 0.5
            if d < t0:
                ap[x, y] = 0
            elif d < t1:
                ap[x, y] = int(255 * (d - t0) / (t1 - t0))

    # 腐蚀量跟着格子尺寸走。512px 的格子溢色带好几像素宽，腐蚀 1px 压不住，
    # 洋红残留会留在深描边里。描边本身很厚，按比例掉 2~3px 在 256px 成品上看不出来。
    k = 3 if w < 200 else (5 if w < 400 else 7)
    alpha = alpha.filter(ImageFilter.MinFilter(k))
    alpha = alpha.filter(ImageFilter.GaussianBlur(max(0.6, w / 480.0)))

    out = cell.convert("RGBA")
    out.putalpha(alpha)
    return out


def drop_bleed(img, keep_ratio=0.22):
    """丢掉邻格越界进来的碎片。

    判据：连通块「贴着格子边缘」且「面积远小于主体」才删。角色四周的墨点是
    合法的分离细节，它们不碰边，所以能留下；越界的别人身体必然碰边。
    """
    w, h = img.size
    a = img.split()[3].point(lambda v: 255 if v > 110 else 0)
    seen = [False] * (w * h)
    ap = a.load()
    blobs = []
    for sy in range(h):
        for sx in range(w):
            i = sy * w + sx
            if seen[i] or ap[sx, sy] == 0:
                continue
            stack = [(sx, sy)]
            seen[i] = True
            cells = []
            edge = False
            while stack:
                x, y = stack.pop()
                cells.append((x, y))
                if x == 0 or y == 0 or x == w - 1 or y == h - 1:
                    edge = True
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        j = ny * w + nx
                        if not seen[j] and ap[nx, ny]:
                            seen[j] = True
                            stack.append((nx, ny))
            blobs.append((cells, edge))
    if not blobs:
        return img
    big = max(len(c) for c, _ in blobs)
    mask = Image.new("L", (w, h), 0)
    mp = mask.load()
    dropped = 0
    for cells, edge in blobs:
        if edge and len(cells) < big * keep_ratio:
            dropped += 1
            continue
        for x, y in cells:
            mp[x, y] = 255
    if dropped:
        al = img.split()[3]
        al = Image.composite(al, Image.new("L", (w, h), 0), mask)
        img = img.copy()
        img.putalpha(al)
    return img, dropped


def normalize(img):
    """裁到包围盒、留边、等比缩到 MAXDIM。"""
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

        fp = os.path.join(FINAL, name + ".png")
        img.save(fp, optimize=True)
        dp = os.path.join(DEST, name + ".png")
        shutil.copyfile(fp, dp)
        print(f"{name:<13} {src:<17} {img.size}  {os.path.getsize(fp)//1024:>3}KB"
              f"  碎片剔除={dropped}")

    # 接触图，方便一眼核对八个 boss 的颜色梯度
    n = len(CELLS)
    cw = 300
    sheet = Image.new("RGBA", (cw * 4, cw * 2), (248, 245, 238, 255))
    for i, (name, *_rest) in enumerate(CELLS):
        im = Image.open(os.path.join(FINAL, name + ".png")).convert("RGBA")
        k = (cw * 0.86) / max(im.size)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))),
                       Image.LANCZOS)
        cxp = (i % 4) * cw + cw // 2
        cyp = (i // 4) * cw + cw // 2
        sheet.paste(im, (cxp - im.width // 2, cyp - im.height // 2), im)
    cp = os.path.join(REPO, "docs", "美术", "敌人", "_boss接触图.png")
    sheet.convert("RGB").save(cp)
    print("接触图 ->", cp)


if __name__ == "__main__":
    main()
