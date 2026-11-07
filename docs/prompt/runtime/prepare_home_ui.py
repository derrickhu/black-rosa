"""把首页 Figma 导出收成 Unity 能用的 RGBA PNG。

720 设计稿上的零件按两倍像素导出。整屏背景保持 720×1280：两倍不压缩会到十多 MB。
关卡圆点和星星按屏幕上的大小留大约四倍，不把组件母版原尺寸塞进包里。
"""
from pathlib import Path

from PIL import Image, ImageOps

RAW = Path("/Users/rosa/rosa_games/game_assets/black-rosa/美术/首页导出/raw")
STAR = Path("/Users/rosa/rosa_games/game_assets/black-rosa/美术/关卡星星/level-star.png")
LAWN = Path("/Users/rosa/rosa_games/game_assets/black-rosa/美术/关卡场景/chapter1-diorama-v6-nobg.png")
STAGE = Path("/Users/rosa/.cursor/figma-mcp/preview/home-stage.png")
OUT = Path("/Users/rosa/rosa_games/black-rosa/Assets/Resources/Art/Ui")

BAR = (58, 49, 47, 255)
YELLOW = (252, 197, 74)
GREEN = (138, 203, 41)
CREAM = (242, 228, 202, 255)
INK = (13, 5, 10, 255)


def save(im, name):
    im = im.convert("RGBA")
    path = OUT / name
    im.save(path, "PNG")
    print(f"{name} {im.size}")


def wipe_title_glyphs(im):
    """只去掉条文右侧的浅色章名。黄标不动，小星留着。整段收成平涂会把星纹抹掉，条就看成一块纯黑。"""
    import numpy as np
    from collections import deque
    from PIL import ImageFilter, Image

    a = np.array(im)
    h, w = a.shape[:2]
    rgb = a[:, :, :3].astype(np.int16)
    # 黄标上的白书也是浅色，种子只从标右边的章名起。
    core = (a[:, :, 3] > 180) & (rgb[:, :, 0] > 210) & (rgb[:, :, 1] > 190) & (rgb[:, :, 2] > 150)
    core[:, :200] = False
    seen = np.zeros((h, w), dtype=bool)
    q = deque()
    ys, xs = np.where(core)
    for y, x in zip(ys.tolist(), xs.tolist()):
        seen[y, x] = True
        q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            xx, yy = x + dx, y + dy
            if xx < 200 or not (0 <= xx < w and 0 <= yy < h) or seen[yy, xx]:
                continue
            if a[yy, xx, 3] < 180:
                continue
            if rgb[yy, xx, 0] > 150 and rgb[yy, xx, 1] > 130:
                seen[yy, xx] = True
                q.append((xx, yy))
    # 字边的抗锯齿比字心暗，扩两像素盖住，碰到的小星只伤到贴着字的那一两颗。
    grown = Image.fromarray((seen.astype(np.uint8) * 255)).filter(ImageFilter.MaxFilter(5))
    seen = np.array(grown) > 0
    px = im.load()
    ys, xs = np.where(seen)
    for x, y in zip(xs.tolist(), ys.tolist()):
        px[int(x), int(y)] = (58, 49, 47, 255)


def wipe_dark_text(im, is_dark, fallback):
    """字是面色中间的孤岛，描边连着外沿。从边上漫过描边，剩下的深色填回面色。"""
    import numpy as np
    from PIL import ImageFilter
    from collections import deque

    a = np.array(im)
    h, w = a.shape[:2]
    dark = is_dark(a[:, :, 0], a[:, :, 1], a[:, :, 2]) & (a[:, :, 3] > 180)
    seen = np.zeros((h, w), dtype=bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if dark[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if dark[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            xx, yy = x + dx, y + dy
            if 0 <= xx < w and 0 <= yy < h and dark[yy, xx] and not seen[yy, xx]:
                seen[yy, xx] = True
                q.append((xx, yy))
    text = dark & ~seen
    grown = np.array(Image.fromarray((text * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
    grown = grown & (a[:, :, 3] > 180) & ~seen
    px = im.load()
    ys, xs = np.where(grown)
    for x, y in zip(xs.tolist(), ys.tolist()):
        px[int(x), int(y)] = (fallback[0], fallback[1], fallback[2], 255)


def recolor(im, base, target):
    lb = 0.3 * base[0] + 0.59 * base[1] + 0.11 * base[2]
    out = im.copy()
    px = out.load()
    w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 8:
                continue
            luma = 0.3 * r + 0.59 * g + 0.11 * b
            k = luma / lb if lb else 1
            px[x, y] = tuple(max(0, min(255, int(c * k))) for c in target) + (a,)
    return out


def dock_selected(dock):
    """从底栏导出里抠出选中块，抹掉烤进去的旗子和字，奶油底改成透明。"""
    import numpy as np

    src = np.array(dock)
    period = 18
    origin = 626
    painted = src.copy()
    for y in range(20, 232):
        for x in range(648, 822):
            sx = origin + ((x - origin) % period)
            painted[y, x] = src[y, sx]
    crop = painted[4:256, 598:842].copy()
    rgb = crop[:, :, :3].astype(np.int16)
    cream = np.array([242, 228, 202], dtype=np.int16)
    ink = np.array([13, 5, 10], dtype=np.int16)
    dist_cream = np.abs(rgb - cream).sum(axis=2)
    dist_ink = np.abs(rgb - ink).sum(axis=2)
    alpha = np.minimum(
        np.clip((dist_cream.astype(np.float32) - 12) * (255 / 28), 0, 255),
        np.clip((dist_ink.astype(np.float32) - 12) * (255 / 28), 0, 255),
    ).astype(np.uint8)
    crop[:, :, 3] = alpha
    return Image.fromarray(crop)


def even(im):
    w, h = im.size
    nw, nh = w - (w % 2), h - (h % 2)
    if (nw, nh) == (w, h):
        return im
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def main():
    OUT.mkdir(parents=True, exist_ok=True)

    stage = Image.open(STAGE).convert("RGB")
    swirls = Image.open(RAW / "bg.png").convert("RGBA")
    grad = Image.new("RGBA", (720, 1280), (219, 126, 101, 255))
    gp = grad.load()
    sp = stage.load()
    sw, sh = stage.size
    for y in range(1280):
        sy = min(sh - 1, y * 2 + 1)
        c = sp[8, sy]
        if c[0] < 40:
            c = sp[8, min(sh - 1, 120)]
        for x in range(720):
            gp[x, y] = (c[0], c[1], c[2], 255)
    grad.alpha_composite(swirls)
    save(grad, "home_bg.png")

    # 草地是生成的原图。Figma 里那张是放大后再导出的，进游戏会糊。
    save(Image.open(LAWN).convert("RGBA"), "home_lawn.png")

    title = Image.open(RAW / "title.png").convert("RGBA")
    wipe_title_glyphs(title)
    save(title, "home_title.png")

    go = Image.open(RAW / "go.png").convert("RGBA")
    wipe_dark_text(go, lambda r, g, b: (r < 240) & (g < 185), YELLOW)
    save(go, "home_go.png")

    opened = Image.open(RAW / "open.png").convert("RGBA")
    wipe_dark_text(opened, lambda r, g, b: (r < 100) & (g < 160), GREEN)
    save(opened, "home_open.png")

    yellow = Image.open(RAW / "arrowR.png").convert("RGBA")
    gray = Image.open(RAW / "arrowL.png").convert("RGBA")
    save(yellow, "home_arrow_on.png")
    save(ImageOps.mirror(yellow), "home_arrow_on_l.png")
    save(gray, "home_arrow_off.png")
    save(ImageOps.mirror(gray), "home_arrow_off_r.png")

    save(Image.open(RAW / "bar.png").convert("RGBA"), "home_bar.png")
    # 页签上的字留给实时文本。烤在图里会和上面那一层叠成两层。
    normal = Image.open(RAW / "tagNormal.png").convert("RGBA")
    wipe_dark_text(normal, lambda r, g, b: (r < 80) & (g < 80) & (b < 80), YELLOW)
    save(normal, "home_tag_normal.png")
    elite = Image.open(RAW / "tagElite.png").convert("RGBA")
    wipe_dark_text(elite, lambda r, g, b: (r < 80) & (g < 80) & (b < 80), (233, 98, 218))
    save(elite, "home_tag_elite.png")
    gray_tag = recolor(normal, (0xFC, 0xC5, 0x4A), (0xB7, 0xB3, 0xAB))
    save(gray_tag, "home_tag_gray.png")

    save(even(Image.open(RAW / "slotOn.png").convert("RGBA")), "home_slot_on.png")
    save(even(Image.open(RAW / "slotOff.png").convert("RGBA")), "home_slot_off.png")

    for src, name in (
        ("node0.png", "home_node_lock.png"),
        ("node1.png", "home_node_done.png"),
        ("node2.png", "home_node_now.png"),
        ("node3.png", "home_node_boss.png"),
    ):
        im = Image.open(RAW / src).convert("RGBA")
        box = im.getbbox()
        im = im.crop(box)
        nh = round(168 * im.height / im.width)
        if nh % 2:
            nh += 1
        im = im.resize((168, nh), Image.Resampling.LANCZOS)
        save(im, name)

    star = Image.open(STAR).convert("RGBA")
    star.thumbnail((96, 96), Image.Resampling.LANCZOS)
    save(even(star), "home_star.png")

    bar = Image.new("RGBA", (16, 256), CREAM)
    bp = bar.load()
    for y in range(4):
        for x in range(16):
            bp[x, y] = INK
    save(bar, "home_dock_bar.png")

    # 选中块是带圆点的整块，不能裁一条再拉宽，圆点会变成竖条。
    save(dock_selected(Image.open(RAW / "dock.png").convert("RGBA")), "home_dock_on.png")


if __name__ == "__main__":
    main()
