#!/usr/bin/env python3
"""第二批常规兵切图：四只纯墨杂兵 + 重出的补墨/厚甲 + 束墨。

复用 process_bosses 的键控和归一化。这一批多两道处理，都是这次踩出来的：

1. `drop_far` —— 模型在「团墨」左边画了三道速度线。它们四周留白、不碰格子边缘，
   所以 `drop_bleed` 那条「贴边才删」的判据抓不到；而 BattleView 是按包围盒
   居中画敌人的，留着它们会把整只球往右顶。判据换成「离主体太远就删」：
   角色四周的墨点离身体都很近（十几像素），速度线明显更远。
2. `neutralize` —— 洋红底把眼白染成了淡紫（#DDD4F0 这一档）。判据卡在
   **亮 + 绿是最暗通道**：洋红/粉/紫系一定绿最低，而这一批的合法颜色
   （绿十字 g 最高、灰蓝甲 r 最低、铜铆钉 b 最低、芥黄 b 最低）都不满足，
   所以不会被误伤。

    .venv-mock/bin/python docs/prompt/enemy/process_mobs2.py [--report]
"""
import os
import shutil
import sys

from PIL import Image, ImageFilter

from process_bosses import RAW, FINAL, DEST, REPO, key_out, drop_bleed, normalize

MAXDIM = 224    # 常规兵比 boss 小一号，和第一批一致

# (成品名, 母版, 列, 行, 总列, 总行)
CELLS = [
    ("chubby",  "mob_d_v3.png", 0, 0, 2, 2),  # 胖墨：矮胖，血厚步慢
    ("tall",    "mob_d_v3.png", 1, 0, 2, 2),  # 高墨：瘦高，步子大
    ("ball",    "mob_d_v3.png", 0, 1, 2, 2),  # 团墨：圆球，小快脆
    ("bighead", "mob_d_v3.png", 1, 1, 2, 2),  # 大头墨：头大身小
    ("mender",  "mob_e_v2.png", 0, 0, 2, 2),  # 补墨（重出）：白帽端正戴在头顶
    ("bulwark", "mob_e_v2.png", 1, 0, 2, 2),  # 厚甲（重出）：圆头独立于肩膀
    ("belt",    "mob_e_v2.png", 1, 1, 2, 2),  # 束墨：芥黄腰带，走空列
]

# 离主体多远算游离。按格子宽度的比例给，母版尺寸变了不用重调。
FAR_GAP = 0.035


def components(alpha):
    """返回 [(像素列表, 是否贴边)]，判据和 drop_bleed 一致。"""
    w, h = alpha.size
    a = alpha.point(lambda v: 255 if v > 110 else 0)
    ap = a.load()
    seen = bytearray(w * h)
    out = []
    for sy in range(h):
        for sx in range(w):
            i = sy * w + sx
            if seen[i] or ap[sx, sy] == 0:
                continue
            stack = [(sx, sy)]
            seen[i] = 1
            cells = []
            while stack:
                x, y = stack.pop()
                cells.append((x, y))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        j = ny * w + nx
                        if not seen[j] and ap[nx, ny]:
                            seen[j] = 1
                            stack.append((nx, ny))
            out.append(cells)
    return out


def drop_far(img, gap_ratio=FAR_GAP, report=False):
    """删掉离主体太远的连通块。主体 = 面积最大的那块。"""
    w, h = img.size
    blobs = components(img.split()[3])
    if len(blobs) < 2:
        return img, 0
    blobs.sort(key=len, reverse=True)
    main = Image.new("L", (w, h), 0)
    mp = main.load()
    for x, y in blobs[0]:
        mp[x, y] = 255
    gap = max(3, int(w * gap_ratio))
    near = main.filter(ImageFilter.MaxFilter(gap * 2 + 1)).load()

    keep = Image.new("L", (w, h), 0)
    kp = keep.load()
    dropped = 0
    for cells in blobs:
        if cells is blobs[0] or any(near[x, y] for x, y in cells):
            for x, y in cells:
                kp[x, y] = 255
        else:
            dropped += 1
            if report:
                xs = [x for x, _ in cells]
                ys = [y for _, y in cells]
                print(f"      丢: 面积 {len(cells):>5}  "
                      f"x {min(xs)}-{max(xs)}  y {min(ys)}-{max(ys)}")
    if dropped:
        al = Image.composite(img.split()[3], Image.new("L", (w, h), 0), keep)
        img = img.copy()
        img.putalpha(al)
    return img, dropped


def neutralize(img):
    """把被洋红底染紫/染粉的亮部拉回中性。见文件头第 2 条。"""
    px = img.load()
    w, h = img.size
    n = 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 200:
                continue
            lo = min(r, g, b)
            hi = max(r, g, b)
            if lo > 120 and g == lo and hi - g > 10:
                px[x, y] = (hi, hi, hi, a)
                n += 1
    return img, n


def main():
    report = "--report" in sys.argv
    os.makedirs(FINAL, exist_ok=True)
    sheets = {}
    for name, src, cx, cy, cols, rows in CELLS:
        if src not in sheets:
            sheets[src] = Image.open(os.path.join(RAW, src)).convert("RGBA")
        sheet = sheets[src]
        cw, chh = sheet.width // cols, sheet.height // rows
        cell = sheet.crop((cx * cw, cy * chh, (cx + 1) * cw, (cy + 1) * chh))

        img = key_out(cell)
        img, bled = drop_bleed(img)
        if report:
            print(f"  {name}:")
        img, far = drop_far(img, report=report)
        img, tinted = neutralize(img)

        # normalize 用的是 process_bosses 里 256 的 MAXDIM，这批要 224，
        # 所以这里自己缩，不改那边的常量。
        img = normalize(img)
        k = MAXDIM / max(img.size)
        if k < 1:
            img = img.resize((max(1, round(img.width * k)),
                              max(1, round(img.height * k))), Image.LANCZOS)

        fp = os.path.join(FINAL, name + ".png")
        img.save(fp, optimize=True)
        shutil.copyfile(fp, os.path.join(DEST, name + ".png"))
        print(f"{name:<9} {src:<14} {str(img.size):<11} {os.path.getsize(fp)//1024:>3}KB"
              f"  越界={bled} 游离={far} 去色={tinted}")

    cw = 300
    cols = 4
    rows = (len(CELLS) + cols - 1) // cols
    board = Image.new("RGBA", (cw * cols, cw * rows), (248, 245, 238, 255))
    for i, (name, *_rest) in enumerate(CELLS):
        im = Image.open(os.path.join(FINAL, name + ".png")).convert("RGBA")
        k = (cw * 0.86) / max(im.size)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))),
                       Image.LANCZOS)
        cxp = (i % cols) * cw + cw // 2
        cyp = (i // cols) * cw + cw // 2
        board.paste(im, (cxp - im.width // 2, cyp - im.height // 2), im)
    cp = os.path.join(REPO, "docs", "美术", "敌人", "_第二批接触图.png")
    board.convert("RGB").save(cp)
    print("接触图 ->", cp)


if __name__ == "__main__":
    main()
