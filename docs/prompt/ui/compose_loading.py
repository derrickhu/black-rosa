#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把 logo 徽标、字标和健康游戏忠告叠到 loading 底图上。

微信团结导出的第一屏是 bgImageSrc 那张图，WASM 还没起来，代码叠不了字。
汉字必须用真字体后处理叠上去 —— 生图模型写中文会出残字。

    .venv-mock/bin/python docs/prompt/ui/compose_loading.py
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

REPO = Path(__file__).resolve().parents[3]
WORK = REPO / "docs" / "美术" / "loading"
RAW = WORK / "raw"
FINAL = WORK / "final"
OUT_DIR = REPO / "Assets" / "Art" / "Loading"

W, H = 1080, 1920
LEGAL = [
    "著作权人：深圳幸运呱科技有限公司",
    "《健康游戏忠告》",
    "抵制不良游戏，拒绝盗版游戏。注意自我保护，谨防受骗上当。",
    "适度游戏益脑，沉迷游戏伤身。合理安排时间，享受健康生活。",
]

# 游戏界面色。忠告必须能在真机上读，所以不用浅灰。
INK = (58, 42, 32)
INK_MID = (142, 128, 110)
CREAM = (255, 255, 255)
OUTLINE = (58, 42, 32)
CTA = (245, 138, 52)

# 忠告里有一批游戏字表没有的字（抵/制/盗/骗…），必须用完整黑体，
# 不能用 Ink.ttf 子集。母版不进包，只在合成这张图时用。
_FONT_CJK_CANDIDATES = [
    Path("/tmp/fontwork/NotoSansSC-var.ttf"),
    Path("/Users/rosa/rosa_games/srpg-rosa/tools/font-src/SourceHanSansSC-Medium.otf"),
    Path("/Users/rosa/rosa_games/game_assets/black-rosa/字体/NotoSansSC[wght].ttf"),
]
FONT_VAR = next(p for p in _FONT_CJK_CANDIDATES if p.exists())


def font(path, size):
    return ImageFont.truetype(str(path), size)


def key_magenta(im, t0=46.0, t1=92.0):
    """洋红键控。腐蚀跟着分辨率走，避免描边里残留洋红。"""
    im = im.convert("RGBA")
    w, h = im.size
    px = im.load()
    # 四边中位数当背景色
    samples = []
    for x in range(0, w, 6):
        samples.append(px[x, 0][:3])
        samples.append(px[x, h - 1][:3])
    for y in range(0, h, 6):
        samples.append(px[0, y][:3])
        samples.append(px[w - 1, y][:3])
    samples.sort()
    br, bg, bb = samples[len(samples) // 2]
    alpha = Image.new("L", (w, h))
    ap = alpha.load()
    span = max(1.0, t1 - t0)
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            d = ((r - br) ** 2 + (g - bg) ** 2 + (b - bb) ** 2) ** 0.5
            t = (d - t0) / span
            ap[x, y] = 0 if t <= 0 else (255 if t >= 1 else int(t * 255))
    k = 3 if w < 200 else (5 if w < 400 else 9)
    alpha = alpha.filter(ImageFilter.MinFilter(k))
    alpha = alpha.filter(ImageFilter.GaussianBlur(max(0.6, w / 420.0)))
    im.putalpha(alpha)
    # 洋红溢色会在描边外留一圈粉边。半透明且红蓝都高过绿的像素直接丢掉。
    px = im.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 8:
                continue
            if a < 230 and r > g + 25 and b > g + 25:
                px[x, y] = (r, g, b, 0)
    return im


def trim_pad(im, pad=12):
    bbox = im.getbbox()
    if bbox is None:
        return im
    x0, y0, x1, y1 = bbox
    x0, y0 = max(0, x0 - pad), max(0, y0 - pad)
    x1, y1 = min(im.width, x1 + pad), min(im.height, y1 + pad)
    return im.crop((x0, y0, x1, y1))


def crush_dust(im):
    """中间那圈灰粉褐并成奶油描边，字心保持近黑。

    直接并进墨色会把描边吃掉，整条字变成一团黑。换成卡片风的浅描边，
    紫墨滴和盾心不动。
    """
    im = im.convert("RGBA")
    px = im.load()
    cream = (255, 246, 236)
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a < 8:
                continue
            if b > r + 15 and b > 70:
                continue
            if r > 190 and g > 140 and b > 80:
                continue
            if r > 120 and r - b > 30 and 280 < (r + g + b) < 520:
                px[x, y] = (*cream, a)
    return im


def process_wordmark(src):
    return trim_pad(key_magenta(src))


def cover(src, size):
    tw, th = size
    sw, sh = src.size
    scale = max(tw / sw, th / sh)
    nw, nh = int(sw * scale), int(sh * scale)
    im = src.resize((nw, nh), Image.LANCZOS)
    x = (nw - tw) // 2
    y = (nh - th) // 2
    return im.crop((x, y, x + tw, y + th))


def rounded(draw, box, r, fill, outline=None, width=4):
    draw.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def stroke_text(d, xy, text, f, fill, stroke, width=3, anchor="mt"):
    """浅描边让字直接压在水彩底上也能读，不是再垫一块白卡。"""
    d.text(xy, text, font=f, fill=fill, stroke_width=width, stroke_fill=stroke, anchor=anchor)


def compose(bg_rgb, wordmark):
    img = cover(bg_rgb.convert("RGB"), (W, H)).convert("RGBA")
    # 字标直接铺在空纸上。敌人大约从 y=490 才出现，整条停在那之前。
    wm_w = 860
    wm_h = int(wordmark.height * (wm_w / wordmark.width))
    wm = wordmark.resize((wm_w, wm_h), Image.LANCZOS)
    img.alpha_composite(wm, ((W - wm_w) // 2, 196))

    # 忠告也直接写在底图上。进度条由微信插件另叠一层，停在这段字上方
    # （game.js 里 bar.bottom = 280）。
    title_f = font(FONT_VAR, 26)
    body_f = font(FONT_VAR, 20)
    halo = (255, 244, 226)
    cx = W / 2
    y = 1696
    d = ImageDraw.Draw(img)
    stroke_text(d, (cx, y), LEGAL[0], body_f, INK_MID, halo, 3)
    y += 34
    stroke_text(d, (cx, y), LEGAL[1], title_f, INK, halo, 3)
    y += 36
    for line in LEGAL[2:]:
        stroke_text(d, (cx, y), line, body_f, INK, halo, 3)
        y += 32
    return img


def main():
    raw_mark = RAW / "logo_wordmark_zn.png"
    raw_bg = RAW / "loading_bg.png"
    if not raw_mark.exists():
        raise SystemExit("缺字标原图：" + str(raw_mark))
    if not raw_bg.exists():
        raise SystemExit("缺 loading 底图：" + str(raw_bg))

    FINAL.mkdir(parents=True, exist_ok=True)
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    logo = process_wordmark(Image.open(raw_mark))
    # 母版 2K 去底后有两千多宽，游戏里用不到这么大，缩到 1280 再入库。
    if logo.width > 1280:
        logo = logo.resize((1280, int(logo.height * 1280 / logo.width)), Image.LANCZOS)
    logo_p = FINAL / "logo.png"
    logo.save(logo_p, optimize=True)
    ui = REPO / "Assets" / "Resources" / "Art" / "Ui"
    ui.mkdir(parents=True, exist_ok=True)
    logo.save(ui / "logo.png", optimize=True)

    page = compose(Image.open(raw_bg), logo)
    png_p = FINAL / "loading_1080x1920.png"
    jpg_p = OUT_DIR / "loading.jpg"
    page.convert("RGB").save(png_p)
    page.convert("RGB").save(jpg_p, quality=86, optimize=True, progressive=True)
    print(f"logo  {logo.size}  {logo_p.stat().st_size/1024:.1f} KB")
    print(f"page  {page.size}  {jpg_p.stat().st_size/1024:.1f} KB  -> {jpg_p.relative_to(REPO)}")


if __name__ == "__main__":
    main()
