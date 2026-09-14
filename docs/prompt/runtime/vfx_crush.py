#!/usr/bin/env python3
"""把 Vfx/shot_*.png 的黑底离线压成透明，顺带降到 256²。

运行时 InkFx.CrushSprite 原本在每次载入时做同一件事，为此
InkArtImporter 不得不给所有图开 isReadable（等于每张贴图多留一份 CPU 拷贝）。
离线做掉之后 Vfx 就能关掉 isReadable 并开压缩。

黑底母版先搬到仓库外 Unity 不会扫的 art_src/vfx/shots_black/。

跑：/Library/Developer/CommandLineTools/usr/bin/python3 docs/prompt/runtime/vfx_crush.py
"""

import shutil
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[3]
SRC = REPO / "Assets/Resources/Art/Vfx"
MASTER = REPO / "art_src/vfx/shots_black"

# 炮弹体槽
SHOTS = [
    "shot_fire", "shot_ice", "shot_track", "shot_explode", "shot_stun",
    "shot_kill", "shot_pierce", "shot_split", "shot_heavy", "shot_accel",
    "shot_cleave", "shot_knock", "shot_arrow",
]

# 命中三层：墨溅底 + 元素层 4 帧 + 事件层单图。
# hit_ink 是白的，没专属图的元素直接拿它染色，所以不用为每个元素再出一套。
SHOTS += [f"{base}_{i:02d}" for base in
          ("hit_ink", "hit_ice", "hit_explode", "hit_heavy", "fire_hit")
          for i in range(4)]
SHOTS += ["kill_mark", "stun_ring", "pierce_tip"]

MAX_SIZE = 256
# 原来运行时是 lum<=22 一刀切，边缘会留黑齿。这里给一段过渡把黑边吃掉，
# lum>=HI 的本体像素仍然全不透明，和旧版一致。
LO = 16.0
HI = 40.0


def crush(path: Path) -> tuple[int, int]:
    im = Image.open(path).convert("RGB")
    if max(im.size) > MAX_SIZE:
        # 先在黑底上缩，再算 alpha，避免透明像素的 RGB 渗出黑边
        im = im.resize((MAX_SIZE, MAX_SIZE), Image.LANCZOS)
    w, h = im.size
    px = im.load()
    out = Image.new("RGBA", (w, h))
    op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            lum = max(r, g, b)
            if lum <= LO:
                op[x, y] = (0, 0, 0, 0)
                continue
            t = min(1.0, (lum - LO) / (HI - LO))
            a = t * t * (3.0 - 2.0 * t)
            op[x, y] = (r, g, b, int(round(a * 255)))
    out.save(path, "PNG", optimize=True)
    return w, h


def main() -> int:
    MASTER.mkdir(parents=True, exist_ok=True)
    missing = [n for n in SHOTS if not (SRC / f"{n}.png").exists()]
    if missing:
        print("缺图：" + ", ".join(missing), file=sys.stderr)
        return 1
    for name in SHOTS:
        p = SRC / f"{name}.png"
        keep = MASTER / f"{name}.png"
        if not keep.exists():
            shutil.copy2(p, keep)
        before = p.stat().st_size / 1024
        w, h = crush(p)
        after = p.stat().st_size / 1024
        print(f"{name:14} -> {w}x{h}  {before:6.0f}KB -> {after:6.0f}KB")
    print(f"\n黑底母版留在 {MASTER.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
