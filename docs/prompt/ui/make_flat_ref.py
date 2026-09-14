#!/usr/bin/env python3
"""画一张「平面风格参考板」，喂给生图模型当 --image 参考。

为什么需要这东西：光靠文字形容词按不住风格。第一版提示词里写了
"glossy sheen"、"soft cel shading"、"plump rounded"，结果出来一批
糖果消除那种 3D 塑料质感，和这游戏的平面厚描边完全不是一路。
所以这里用代码画几个**风格明确但造型简单**的图标，把这几件事钉死：

  - 只有平涂色块，**没有渐变**
  - 暗部是**一块硬边的深色平涂**，不是柔和的明暗过渡
  - 亮部同理，硬边，不是高光光斑
  - 统一 #3A2A20 厚描边，粗细一致
  - 没有投影、没有反光、没有厚度感

造型交给模型去发挥，风格照这张抄。

**参考板上必须是清单里没有的抽象色板。** 第一版画的是金币/红心/水滴/闪电，
而这四样恰好都在要生成的清单里，结果模型把参考当成「扩充这套图标」，
第 4 行原样抄了参考、其余 12 格自由发挥成了通用奇幻 RPG 图标。
换成抽象几何后它只能传递「怎么上色」，抄不了「画什么」。

    .venv-mock/bin/python docs/prompt/ui/make_flat_ref.py
"""
import math
import os

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "美术", "首页方案",
                   "_平面风格参考.png")

CELL = 256
COLS, ROWS = 2, 2
BG = (255, 0, 255)
LINE = (58, 42, 32)
LW = 14                      # 描边宽度，约为格子的 5.5%


def outlined(d, draw_fn, fill):
    """先用描边色画大一圈，再用填充色画小一圈 —— 等宽硬边描边。"""
    draw_fn(d, LINE, LW)
    draw_fn(d, fill, 0)


def wedge(d, cx, cy, r):
    """圆盘 + 一刀硬边切出的暗部。"""
    def ring(dd, col, grow):
        dd.ellipse((cx - r - grow, cy - r - grow, cx + r + grow, cy + r + grow), fill=col)
    outlined(d, ring, (246, 190, 60))
    d.chord((cx - r, cy - r, cx + r, cy + r), 25, 155, fill=(198, 140, 32))
    d.chord((cx - r, cy - r, cx + r, cy + r), 200, 330, fill=(255, 218, 120))


def slab(d, cx, cy, r):
    """圆角方块 + 硬边亮部/暗部分层。"""
    def box(dd, col, grow):
        dd.rounded_rectangle((cx - r - grow, cy - r - grow, cx + r + grow, cy + r + grow),
                             radius=r * 0.34 + grow, fill=col)
    outlined(d, box, (232, 78, 78))
    d.rectangle((cx - r, cy + r * 0.30, cx + r, cy + r), fill=(186, 52, 52))
    d.rectangle((cx - r, cy - r, cx + r, cy - r * 0.44), fill=(255, 132, 132))


def lobe(d, cx, cy, r):
    """不规则团块，证明异形轮廓也照样等宽描边、平涂分层。"""
    pts = [(-1.00, -0.10), (-0.62, -0.82), (0.18, -1.00), (0.92, -0.50),
           (1.00, 0.28), (0.44, 0.96), (-0.38, 0.88), (-0.90, 0.46)]

    def shape(dd, col, grow):
        k = 1.0 + grow / r
        dd.polygon([(cx + x * r * k, cy + y * r * k) for x, y in pts], fill=col)
    outlined(d, shape, (54, 176, 176))
    d.polygon([(cx + x * r, cy + y * r) for x, y in
               ((-0.90, 0.46), (0.44, 0.96), (0.70, 0.30), (-0.70, 0.16))],
              fill=(26, 132, 132))


def chevron(d, cx, cy, r):
    """尖角形，检验拐角处描边是否仍然等宽。"""
    pts = [(0.00, -1.00), (0.86, -0.06), (0.44, -0.06), (0.44, 0.94),
           (-0.44, 0.94), (-0.44, -0.06), (-0.86, -0.06)]

    def shape(dd, col, grow):
        k = 1.0 + grow / r
        dd.polygon([(cx + x * r * k, cy + y * r * k) for x, y in pts], fill=col)
    outlined(d, shape, (140, 110, 220))
    d.rectangle((cx - r * 0.44, cy + r * 0.34, cx + r * 0.44, cy + r * 0.94),
                fill=(104, 78, 182))


def main():
    img = Image.new("RGB", (CELL * COLS, CELL * ROWS), BG)
    d = ImageDraw.Draw(img)
    jobs = [wedge, slab, lobe, chevron]
    for i, fn in enumerate(jobs):
        cx = (i % COLS) * CELL + CELL / 2
        cy = (i // COLS) * CELL + CELL / 2
        fn(d, cx, cy, CELL * 0.33)
    p = os.path.abspath(OUT)
    img.save(p)
    print("wrote", p, img.size)


if __name__ == "__main__":
    main()
