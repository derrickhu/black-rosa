#!/usr/bin/env python3
"""按 HomeScreen.cs / UiKit.cs / ScreenFit.cs 里最终的坐标，预渲染首页三页。

这是「按代码里的数算出来的预期图」，不是实机截图 —— 坐标、尺寸、配色、字体都从
源码抄过来，用来在没法跑 Play 模式时检查有没有重叠、有没有溢出、有没有大片空白。
改了 HomeScreen 的布局常量，这里要同步。

两件事一定要按真机的数来渲，不然看不出问题：

1. 画布不是 720x1280。CanvasScaler 按宽匹配，长条屏上画布有 1558 个单位高。
   按 1280 渲的话，内容区会比真机短 280，本来会溢出的反而看着正好。
2. 顶栏要给微信右上角的胶囊按钮让位，TopPad 在长条手机上是 165 而不是 36。
   这一条是真机上「上面数值被遮挡」的根因，按 36 渲永远复现不出来。

所以下面按两种机型各渲一套：长条手机（多数玩家）和 16:9（内容区最挤的那种）。

    .venv-mock/bin/python docs/prompt/ui/render_home_final.py
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(REPO, "docs", "美术", "首页方案")
ICON_DIR = os.path.join(REPO, "Assets", "Resources", "Art", "Ui")

# 用游戏自己的两个字重，不要用系统黑体 —— 换字体后字宽变了，
# 「有没有挤出框」这件事只有用真字体渲才算得准。
FONT_REG = os.path.join(REPO, "Assets", "Resources", "Fonts", "Ink.ttf")
FONT_BOLD = os.path.join(REPO, "Assets", "Resources", "Fonts", "InkBold.ttf")

# 和 ForgeCatalog.Icon / SpellId 一一对应，顺序就是卡片顺序
FORGE_ICON = ["damage", "guns", "rate", "gold", "hp"]
SPELL_ICON = ["burst", "halt", "rage", "sweep", "splash", "mend"]
TAB_ICON = ["tab_forge", "tab_sortie", "tab_spell"]

_fc = {}


def font(path, size):
    k = (path, size)
    if k not in _fc:
        _fc[k] = ImageFont.truetype(path, size)
    return _fc[k]


def reg(size):
    return font(FONT_REG, size)


def bold(size):
    return font(FONT_BOLD, size)


def hx(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


# InkTheme 界面层调色板
OUTLINE = hx("3A2A20")
CARDFACE = hx("FFFFFF")
CARDDIM = hx("EFE9DF")
LINEDIM = hx("ACA296")
BGTOP = hx("FFEED2")
BGBOT = hx("EFD2C0")
TEXTDARK = hx("3A2A20")
TEXTMID = hx("8E806E")
TEXTDIM = hx("A89A88")
CTA = hx("F58A34")
CTADEEP = hx("C46018")
PLAIN = hx("FFFFFF")
PLAINDEEP = hx("E2DACE")
COIN = hx("F6BE3C")
TEAL = hx("36B0B0")
VIOLET = hx("8C6EDC")
ROSE = hx("E84E4E")
TABON = hx("FFE1BE")
LINE_TINT = [ROSE, VIOLET, TEAL, COIN, CTA]

# ---- HomeScreen / UiKit 布局常量 ----
CARD_W, CARD_H, COL_STEP, CARD_GAP = 322, 204, 338, 34
TAB_BAR_H, TAB_BAR_W, TAB_BAR_INNER = 128, 688, 104
TAB_ICON_ON, TAB_ICON_OFF = 54, 48
SEAL_COLS, SEAL_ROWS = 5, 4
SEAL_SIZE, SEAL_STEP_X, SEAL_STEP_Y, SEAL_TOP_Y = 128, 138, 150, 134
SORTIE_FOOT_H = 236
STAGES = 20

# 机型档。W/H 是画布尺寸（ScreenFit.CanvasW/CanvasH），不是设计尺寸。
# TopPad 按 ScreenFit 的算法：max(安全区, 胶囊下沿 + 12)。
PROFILES = [
    ("A", "长条手机 19.5:9", 720, 1558, 165, 63),
    ("B", "16:9 手机", 720, 1280, 74, 24),
]

W = H = TOP_PAD = BOT_PAD = 0
CHIP_TOP = PAGE_TOP = PAGE_BOT = PAGE_H = 0


def setup(w, h, top, bot):
    global W, H, TOP_PAD, BOT_PAD, CHIP_TOP, PAGE_TOP, PAGE_BOT, PAGE_H
    W, H, TOP_PAD, BOT_PAD = w, h, top, bot
    CHIP_TOP = TOP_PAD + 18
    PAGE_TOP = TOP_PAD + 104
    PAGE_BOT = BOT_PAD + TAB_BAR_H
    PAGE_H = H - PAGE_TOP - PAGE_BOT


def slot(i, top_y, count):
    """照搬 HomeScreen.Slot：卡不缩，先压行距，剩下的富余上下均分。"""
    rows = max(1, (count + 1) // 2)
    avail = PAGE_H - top_y - 20
    gap = max(0.0, min(CARD_GAP, (avail - rows * CARD_H) / max(1, rows - 1)))
    block = rows * CARD_H + (rows - 1) * gap
    slack = max(0.0, avail - block)
    step = CARD_H + gap
    return (((i % 2) - 0.5) * COL_STEP, (i // 2) * step + top_y + slack / 2)


LINES = [
    ("伤害", "炮弹伤害 +8%", 2, 4, "60 墨", True),
    ("炮台数", "多一门炮", 1, 2, "第二章开放", False),
    ("射速", "开火间隔 -4%", 1, 3, "40 墨", True),
    ("开局金币", "开局金币 +2", 3, 3, "已满级", False),
    ("基地生命", "基地生命 +1", 0, 2, "需 10 星", False),
]
# 下面这些文案必须和 SkinCatalog / SpellCatalog / MetaProgress 的 why 分支一字不差。
# 抄错了预览就白做 —— 这个脚手架唯一的用处就是量真实字串有没有挤出框，
# 之前这里留着一版早就改掉的旧文案（「泼一片减速」「★14」），
# 渲出来的豆腐块全是脚手架自己的字，反而掩盖了真问题。
SKINS = [("素笔", None, "使用中", True), ("朱砂", hx("C0392B"), "点击换上", True),
         ("青瓷", hx("4A7C8C"), "150 墨", False), ("鎏金", hx("E8B43A"), "通关解锁", False)]
SPELLS = [("墨爆", "最前排炸开一圈，6 点伤害", 25, "已装备 1"),
          ("定身", "全场敌人定住 1.6 秒", 35, "点击装备"),
          ("强攻", "5 秒内炮弹伤害翻倍", 45, "75 墨"),
          ("横扫", "全屏 5 点伤害并击退", 55, "需 14 星"),
          ("泼墨", "敌人最多那一列灼烧 3 秒", 50, "需 18 星"),
          ("回血", "基地回 1 血，每局限一次", 70, "通关解锁")]
# 前六关打过，第七关已解锁未打，其余锁住
STARS = [3, 3, 2, 3, 1, 2, 0] + [-1] * 13


def text(d, xy, s, f, fill, anchor="mm"):
    d.text(xy, s, font=f, fill=fill, anchor=anchor)


def shadow(img, box, r, blur=12, alpha=87, dy=7):
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    x0, y0, x1, y1 = box
    ImageDraw.Draw(lay).rounded_rectangle((x0, y0 + dy, x1, y1 + dy), radius=r,
                                          fill=(59, 41, 31, alpha))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(blur * 0.5)))


def tier(radius):
    return 28 if radius >= 24 else (20 if radius >= 16 else 12)


def tier_for(w, h):
    return tier(min(abs(w), abs(h)) * 0.30)


def card(img, box, fill, line, width=5, rad=None, sh=True):
    """UiKit.Stroke：投影 + 圆角实底 + 等宽描边。"""
    if rad is None:
        rad = tier_for(box[2] - box[0], box[3] - box[1])
    if sh:
        shadow(img, box, rad)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, radius=rad, fill=fill, outline=line, width=width)
    return d


def btn(img, box, primary=True, lift=None):
    """UiKit.Btn：根节点是压深的底唇，face 抬起来盖上去。"""
    x0, y0, x1, y1 = box
    rad = tier_for(x1 - x0, y1 - y0)
    if lift is None:
        lift = max(5, min(9, (y1 - y0) * 0.12))
    face, deep = (CTA, CTADEEP) if primary else (PLAIN, PLAINDEEP)
    shadow(img, box, rad, dy=8)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, radius=rad, fill=deep, outline=OUTLINE, width=5)
    d.rounded_rectangle((x0, y0, x1, y1 - lift), radius=rad, fill=face, outline=OUTLINE, width=5)
    return d, lift


def cannon(d, cx, cy, s, color):
    d.rounded_rectangle((cx - .22 * s, cy - .92 * s, cx + .22 * s, cy + .18 * s), radius=.08 * s, fill=color)
    d.rounded_rectangle((cx - .58 * s, cy + .06 * s, cx + .58 * s, cy + .70 * s), radius=.16 * s, fill=color)


_icons = {}


def paste_icon(img, key, cx, cy, size, live=True):
    """贴 Resources/Art/Ui 里的真图标。

    那批图都是 128x128 方画布、内容已居中，所以直接缩到 size x size
    就等价于 Unity 那边 preserveAspect 的效果。
    禁用态按 HomeScreen.CardIcon 的做法降透明度，不是压灰。
    """
    if key not in _icons:
        p = os.path.join(ICON_DIR, "ico_%s.png" % key)
        _icons[key] = Image.open(p).convert("RGBA") if os.path.exists(p) else None
    src = _icons[key]
    if src is None:
        print("  !! 缺图标 ico_%s.png" % key)
        return
    s = max(1, int(round(size)))
    ic = src.resize((s, s), Image.LANCZOS)
    if live < 1:
        ic.putalpha(ic.split()[3].point(lambda v: int(v * live)))
    img.alpha_composite(ic, (int(round(cx - s / 2)), int(round(cy - s / 2))))


def icon_plate(img, cx, cy, size, bg, live):
    box = (cx - size / 2, cy - size / 2, cx + size / 2, cy + size / 2)
    return card(img, box, bg if live else LINEDIM, OUTLINE if live else LINEDIM, 4, tier(size * 0.30))


def price_pill(img, cx, cy, s, live):
    box = (cx - 59, cy - 21, cx + 59, cy + 21)
    d = card(img, box, COIN if live else CARDDIM, OUTLINE if live else LINEDIM, 4, 16)
    text(d, (cx, cy - 1), s, bold(20), TEXTDARK if live else TEXTDIM)


def pips(d, ox, oy, lv, mx, col):
    for i in range(mx):
        cx = ox + i * 28
        d.ellipse((cx - 10, oy - 10, cx + 10, oy + 10), fill=col if i < lv else CARDDIM)


def backdrop():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for y in range(H):
        t = 1 - y / (H - 1)
        d.line([(0, y), (W, y)], fill=tuple(int(BGBOT[i] + (BGTOP[i] - BGBOT[i]) * t) for i in range(3)) + (255,))
    for ax, ay, size, col, al in ((0.12, 0.90, 620, hx("FFBE78"), 0.40), (0.86, 0.18, 700, hx("FFA894"), 0.32)):
        cx, cy = ax * W, (1 - ay) * H
        r = size / 2
        lay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(lay).ellipse((cx - r, cy - r, cx + r, cy + r), fill=col + (int(al * 255),))
        img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(r * 0.42)))
    return img


def capsule(img):
    """微信自己画在画布之上的胶囊按钮。游戏管不到它，但预渲染必须画出来 ——
    它就是「上面的数值被遮挡」的那个遮挡物，不画出来核对等于白核对。"""
    d = ImageDraw.Draw(img, "RGBA")
    h = 0.0205 * H
    w = 2.72 * h
    x1 = W - 0.0195 * W
    y1 = TOP_PAD - 12
    box = (x1 - w, y1 - h, x1, y1)
    d.rounded_rectangle(box, radius=h / 2, fill=(120, 120, 120, 90), outline=(255, 255, 255, 110), width=2)
    cy = (box[1] + box[3]) / 2
    for k in range(3):
        cx = box[0] + w * 0.27 + k * h * 0.34
        d.ellipse((cx - 2.5, cy - 2.5, cx + 2.5, cy + 2.5), fill=(255, 255, 255, 200))
    cx = box[0] + w * 0.74
    r = h * 0.3
    d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=(255, 255, 255, 200), width=3)
    d.ellipse((cx - 3, cy - 3, cx + 3, cy + 3), fill=(255, 255, 255, 200))


def top_bar(img):
    # UiKit.Chip：图标略微出框压在左端，不再垫彩色圆盘
    for x, key, val in ((-190, "stamina", "8/12"), (0, "ink", "1240"), (190, "star", "18")):
        cx = W / 2 + x
        box = (cx - 95, CHIP_TOP, cx + 95, CHIP_TOP + 58)
        card(img, box, CARDFACE, OUTLINE, 5, 28)
        r = 58 * 1.06
        paste_icon(img, key, cx - 95 + r * 0.42, CHIP_TOP + 29, r)
        d = ImageDraw.Draw(img)
        text(d, (cx + r * 0.40 - (190 - r - 16) / 2, CHIP_TOP + 29), val, bold(30), TEXTDARK, "lm")
    d = ImageDraw.Draw(img)
    text(d, (W / 2 - 190, CHIP_TOP + 62 + 13), "12:34 后 +1", reg(19), TEXTMID)


def tab_bar(img, active):
    """UiKit.TabBar：整条悬浮药丸 + 选中格淡底 + 图标放大 + 标签换粗体主色。"""
    y1 = H - (BOT_PAD + 12)
    y0 = y1 - TAB_BAR_INNER
    box = (W / 2 - TAB_BAR_W / 2, y0, W / 2 + TAB_BAR_W / 2, y1)
    card(img, box, CARDFACE, OUTLINE, 5, tier(TAB_BAR_INNER * 0.34))
    cy = (y0 + y1) / 2
    step = TAB_BAR_W / 3
    for i, t in enumerate(("炮台", "出征", "技能")):
        cx = W / 2 + (i - 1) * step
        on = i == active
        if on:
            iw, ih = step - 10, TAB_BAR_INNER - 14
            ImageDraw.Draw(img).rounded_rectangle(
                (cx - iw / 2, cy - ih / 2, cx + iw / 2, cy + ih / 2), radius=28, fill=TABON)
        paste_icon(img, TAB_ICON[i], cx, cy - 14,
                   TAB_ICON_ON if on else TAB_ICON_OFF, 1.0 if on else 0.42)
        d = ImageDraw.Draw(img)
        text(d, (cx, cy + 28), t, bold(22) if on else reg(22), CTADEEP if on else TEXTMID)


# ------------------------------------------------------------------ 出征
def page_sortie():
    img = backdrop()
    top_bar(img)
    py = PAGE_TOP

    box = (W / 2 - 262, py + 4, W / 2 + 262, py + 100)
    d = card(img, box, CARDFACE, OUTLINE, 6)
    text(d, (W / 2, py + 52), "墨字防线", bold(44), TEXTDARK)

    room = PAGE_H - SEAL_TOP_Y - SORTIE_FOOT_H
    step = min(SEAL_STEP_Y, room / SEAL_ROWS)
    size = max(96, min(SEAL_SIZE, step - 22))
    k = size / SEAL_SIZE
    print("    印章 step=%.0f size=%.0f  末行下沿=%.0f  页脚上沿=%.0f"
          % (step, size, SEAL_TOP_Y + (SEAL_ROWS - 1) * step + size, PAGE_H - SORTIE_FOOT_H))
    for i in range(STAGES):
        cx = W / 2 + ((i % SEAL_COLS) - (SEAL_COLS - 1) / 2) * SEAL_STEP_X
        ty = py + SEAL_TOP_Y + (i // SEAL_COLS) * step
        sb = (cx - size / 2, ty, cx + size / 2, ty + size)
        st = STARS[i]
        live = st >= 0
        d = card(img, sb, CARDFACE if live else CARDDIM, OUTLINE if live else LINEDIM,
                 6 if live else 5, 22 * k)
        if not live:
            paste_icon(img, "lock", cx, ty + size / 2, 48 * k)
            continue
        cy = ty + size / 2
        text(d, (cx, cy - (12 if st > 0 else 2) * k), str(i + 1), bold(round(34 * k)), TEXTDARK)
        if st == 0:
            text(d, (cx, cy + 34 * k), "免费", bold(round(17 * k)), TEAL)
            continue
        span = (st - 1) * 26 * k
        for s in range(st):
            paste_icon(img, "star", cx - span / 2 + s * 26 * k, cy + 22 * k, 24 * k)

    # Pin.Bottom：y 是「距内容区下沿」，元素占 y..y+h
    pb = py + PAGE_H
    gb = (W / 2 - 230, pb - 26 - 106, W / 2 + 230, pb - 26)
    d, lift = btn(img, gb, True)
    text(d, (W / 2, pb - 26 - 53 - lift / 2), "继续  第 7 关", bold(34), CARDFACE)

    d = ImageDraw.Draw(img)
    text(d, (W / 2, pb - 150), "左右拖动底栏炮串。金币够了点改装。\n重刷已通过的关要 2 点体力。",
         reg(22), TEXTMID, "md")
    tab_bar(img, 1)
    capsule(img)
    return img


# ------------------------------------------------------------------ 炮台
def page_forge():
    img = backdrop()
    top_bar(img)
    py = PAGE_TOP

    box = (W / 2 - 158, py + 2, W / 2 + 158, py + 178)
    d = card(img, box, CARDFACE, OUTLINE, 6)
    d.ellipse((W / 2 - 66, py + 68 - 66, W / 2 + 66, py + 68 + 66), fill=hx("FFE7C4"))
    cannon(d, W / 2, py + 68, 100, OUTLINE)
    text(d, (W / 2, py + 152), "素笔", bold(26), TEXTDARK)

    for i, (name, tint, tail, owned) in enumerate(SKINS):
        cx = W / 2 + (i - 1.5) * 168
        ty = py + 186
        sb = (cx - 78, ty, cx + 78, ty + 108)
        on = i == 0
        card(img, sb, CARDFACE if owned else CARDDIM,
             CTA if on else (OUTLINE if owned else LINEDIM), 6 if on else 4, 20)
        cy = ty + 54
        if tint:
            icon_plate(img, cx, cy - 26, 48, tint, owned)
        d = ImageDraw.Draw(img)
        if not tint:
            text(d, (cx, cy - 26), "无", bold(24), TEXTDARK if owned else TEXTDIM)
        text(d, (cx, cy + 12), name, bold(21), TEXTDARK if owned else TEXTDIM)
        text(d, (cx, cy + 36), tail, reg(16), CTA if on else TEXTMID)

    for i, (name, step, lv, mx, tail, live) in enumerate(LINES):
        gx, gy = slot(i, 306, len(LINES))
        cx = W / 2 + gx
        ty = py + gy
        if i == len(LINES) - 1:
            print("    炮台末卡下沿=%.0f  内容区高=%.0f" % (gy + CARD_H, PAGE_H))
        cb = (cx - CARD_W / 2, ty, cx + CARD_W / 2, ty + CARD_H)
        card(img, cb, CARDFACE if live else CARDDIM, OUTLINE if live else LINEDIM, 5)
        cy = ty + CARD_H / 2
        paste_icon(img, FORGE_ICON[i], cx - 114, cy - 34, 84, 1.0 if live else 0.5)
        d = ImageDraw.Draw(img)
        text(d, (cx + 25 - 90, cy - 46), name, bold(28), TEXTDARK if live else TEXTDIM, "lm")
        text(d, (cx + 25 - 90, cy - 10), step, reg(18), TEXTMID, "lm")
        pips(d, cx - 126, cy + 58, lv, mx, LINE_TINT[i] if live else LINEDIM)
        if live:
            price_pill(img, cx + 84, cy + 58, tail, True)
        else:
            text(d, (cx + 56 + 85, cy + 58), tail, reg(19), TEXTDIM, "rm")

    tab_bar(img, 0)
    capsule(img)
    return img


# ------------------------------------------------------------------ 技能
def page_spell():
    img = backdrop()
    top_bar(img)
    py = PAGE_TOP

    for s in range(2):
        cx = W / 2 + (s - 0.5) * 236
        ty = py + 4
        sb = (cx - 116, ty, cx + 116, ty + 124)
        has = s == 0
        card(img, sb, CARDFACE if has else CARDDIM, OUTLINE if has else LINEDIM, 6, 24)
        cy = ty + 62
        if not has:
            d = ImageDraw.Draw(img)
            text(d, (cx, cy), "空槽", bold(26), TEXTDIM)
            continue
        paste_icon(img, "burst", cx, cy - 32, 52)
        d = ImageDraw.Draw(img)
        text(d, (cx, cy + 18), "墨爆", bold(28), TEXTDARK)
        text(d, (cx, cy + 44), "25 能量", reg(19), TEXTMID)

    d = ImageDraw.Draw(img)
    text(d, (W / 2, py + 142 + 15), "战斗中点右下角的键释放。两个键共用一条能量。", reg(20), TEXTMID)

    for i, (name, desc, en, tail) in enumerate(SPELLS):
        gx, gy = slot(i, 192, len(SPELLS))
        cx = W / 2 + gx
        ty = py + gy
        if i == len(SPELLS) - 1:
            print("    技能末卡下沿=%.0f  内容区高=%.0f" % (gy + CARD_H, PAGE_H))
        cb = (cx - CARD_W / 2, ty, cx + CARD_W / 2, ty + CARD_H)
        owned = i < 2
        live = owned or tail.endswith("墨")
        equipped = tail.startswith("已装备")
        card(img, cb, CARDFACE if live else CARDDIM,
             CTA if equipped else (OUTLINE if live else LINEDIM), 6 if equipped else 5)
        cy = ty + CARD_H / 2
        paste_icon(img, SPELL_ICON[i], cx - 114, cy - 34, 84, 1.0 if live else 0.5)
        d = ImageDraw.Draw(img)
        text(d, (cx + 20 - 85, cy - 46), name, bold(28), TEXTDARK if live else TEXTDIM, "lm")
        text(d, (cx + 30 - 95, cy - 10), desc, reg(17), TEXTMID, "lm")
        text(d, (cx - 78 - 70, cy + 58), f"{en} 能量", reg(18), TEXTMID, "lm")
        if owned:
            text(d, (cx + 56 + 85, cy + 58), tail, bold(20), CTA if equipped else TEXTMID, "rm")
        elif live:
            price_pill(img, cx + 84, cy + 58, tail, True)
        else:
            text(d, (cx + 56 + 85, cy + 58), tail, reg(18), TEXTDIM, "rm")

    tab_bar(img, 2)
    capsule(img)
    return img


def label(img, tag):
    # 这条说明用系统字体写。游戏字体是按字表切的，不含「机」「源」这些只在
    # 脚手架里出现的字，用它写会渲成一排豆腐块，看着像游戏缺字。
    sys_f = ImageFont.truetype("/System/Library/Fonts/STHeiti Medium.ttc", 22, index=1)
    d = ImageDraw.Draw(img)
    d.rectangle((0, H - 44, W, H), fill=(24, 17, 13, 240))
    text(d, (16, H - 22), tag, sys_f, (255, 255, 255), "lm")
    text(d, (W - 16, H - 21), "按源码坐标预渲染 · 非实机截图", sys_f, (198, 190, 180), "rm")
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for pid, pname, w, h, top, bot in PROFILES:
        setup(w, h, top, bot)
        print("%s %s  画布 %dx%d  TopPad=%d BottomPad=%d  内容区 %d"
              % (pid, pname, W, H, TOP_PAD, BOT_PAD, PAGE_H))
        jobs = [("出征", page_sortie), ("炮台", page_forge), ("技能", page_spell)]
        imgs = []
        for tag, fn in jobs:
            im = label(fn(), "%s · %s" % (tag, pname)).convert("RGB")
            p = os.path.join(OUT, "%s_%s.png" % (pid, tag))
            im.save(p, quality=95)
            imgs.append(im)
        gap = 14
        sheet = Image.new("RGB", (W * 3 + gap * 4, H + gap * 2), (232, 232, 232))
        for i, im in enumerate(imgs):
            sheet.paste(im, (gap + i * (W + gap), gap))
        sp = os.path.join(OUT, "%s_三页_%s.png" % (pid, pname.replace(" ", "")))
        sheet.save(sp, quality=92)
        print("    ->", sp)


if __name__ == "__main__":
    main()
