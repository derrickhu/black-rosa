#!/usr/bin/env python3
"""把入库的字图降到 256²。

母版（1024²）留在 game_assets/black-rosa/美术/runtime/heaps_v12/final/，
仓库里的只是运行时用的副本。字图最终会被 InkArt.OnCard 缩到 128~160px 的
宣纸卡上，1024 的源既吃显存（每张 4MB，开了 isReadable 再翻倍），
也让启动时 BlitSprite 的包围盒扫描多跑 16 倍像素。

只处理在母版目录里能找到同名文件的图，避免误伤没有备份的资源。

跑：/Library/Developer/CommandLineTools/usr/bin/python3 docs/prompt/runtime/heaps_downscale.py
"""

import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[3]
ART = REPO / "Assets/Resources/Art"
MASTER = Path.home() / "rosa_games/game_assets/black-rosa/美术/runtime/heaps_v12/final"

OUT = 256


def main() -> int:
    if not MASTER.is_dir():
        print(f"找不到母版目录 {MASTER}", file=sys.stderr)
        return 1
    done = 0
    for p in sorted(ART.glob("heap_*.png")):
        if not (MASTER / p.name).exists():
            continue
        im = Image.open(p)
        if max(im.size) <= OUT:
            continue
        before = p.stat().st_size / 1024
        im.convert("RGB").resize((OUT, OUT), Image.LANCZOS).save(p, "PNG", optimize=True)
        print(f"{p.name:20} {im.size[0]}x{im.size[1]} -> {OUT}x{OUT}  {before:6.0f}KB -> {p.stat().st_size / 1024:5.0f}KB")
        done += 1
    print(f"\n处理 {done} 张，母版在 {MASTER}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
