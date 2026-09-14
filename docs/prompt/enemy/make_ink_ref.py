#!/usr/bin/env python3
"""把现有敌人图拼成一张洋红底参考板，给生图模型当 --image 用。

和界面图标那次不同：那次参考板必须画抽象色板，避免模型抄内容；
这次**就是要它抄风格** —— 炭灰身体、粗黑描边、两个白点眼、补丁缝线、
周围几点墨渍。新 boss 的造型和配件靠提示词描述，身体语言照这张抄。

    .venv-mock/bin/python docs/prompt/enemy/make_ink_ref.py
"""
import os

from PIL import Image

HERE = os.path.dirname(__file__)
ART = os.path.join(HERE, "..", "..", "..", "Assets", "Resources", "Art")
OUT = os.path.join(HERE, "..", "..", "美术", "敌人", "_墨风参考.png")

PICK = ["walker", "runner", "shield", "elite"]
CELL = 320
BG = (255, 0, 255)


def main():
    os.makedirs(os.path.dirname(os.path.abspath(OUT)), exist_ok=True)
    sheet = Image.new("RGB", (CELL * 2, CELL * 2), BG)
    for i, name in enumerate(PICK):
        p = os.path.join(ART, name + ".png")
        im = Image.open(p).convert("RGBA")
        bb = im.getbbox()
        if bb:
            im = im.crop(bb)
        # 等比缩进格子，留一圈洋红
        box = int(CELL * 0.82)
        k = box / max(im.width, im.height)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))),
                       Image.LANCZOS)
        cx = (i % 2) * CELL + CELL // 2
        cy = (i // 2) * CELL + CELL // 2
        sheet.paste(im, (cx - im.width // 2, cy - im.height // 2), im)
    p = os.path.abspath(OUT)
    sheet.save(p)
    print("wrote", p, sheet.size, "使用:", PICK)


if __name__ == "__main__":
    main()
