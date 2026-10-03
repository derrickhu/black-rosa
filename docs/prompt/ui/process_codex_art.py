#!/usr/bin/env python3
"""把图鉴标签、印鉴、卡框从洋红底大图切出来，键控成透明 PNG。"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from process_ui_icons import bg_color, drop_bleed, flatten, key_out  # noqa: E402

ASSETS = os.environ.get(
    "GAME_ASSETS", "/Users/rosa/rosa_games/game_assets/black-rosa/assets")
RAW = os.path.join(ASSETS, "raw")
DEST = os.path.abspath(os.path.join(HERE, "..", "..", "..", "Assets", "Resources", "Art", "Ui"))


def fit(img, canvas, content):
    bbox = img.getbbox()
    if bbox is None:
        return Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    art = img.crop(bbox)
    box = int(canvas * content)
    scale = box / max(art.width, art.height)
    art = art.resize((max(1, round(art.width * scale)),
                      max(1, round(art.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    out.paste(art, ((canvas - art.width) // 2, (canvas - art.height) // 2), art)
    return out


def split(src, cols, rows, names, canvas=0, content=0.86, flat=6, t0=36.0, t1=88.0):
    img = Image.open(src).convert("RGBA")
    bg = bg_color(img)
    print(os.path.basename(src), "bg", bg, img.size)
    cw, ch = img.width // cols, img.height // rows
    out = []
    for i, name in enumerate(names):
        c, r = i % cols, i // cols
        cell = img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
        cut = key_out(cell, bg, t0=t0, t1=t1)
        cut, _ = drop_bleed(cut)
        if flat:
            cut = flatten(cut, colors=flat)
        if canvas:
            cut = fit(cut, canvas, content)
        else:
            bbox = cut.getbbox()
            if bbox:
                cut = cut.crop(bbox)
        out.append((name, cut))
        print(" ", name, cut.size)
    return out


def save(items):
    os.makedirs(DEST, exist_ok=True)
    for name, im in items:
        path = os.path.join(DEST, name + ".png")
        im.save(path, optimize=True)
        print(" ->", os.path.basename(path), im.size, os.path.getsize(path))


def main():
    marks = os.path.join(RAW, "codex_marks.png")
    plates = os.path.join(RAW, "codex_plates.png")
    save(split(marks, 3, 2, [
        "codex_tab_glyph", "codex_tab_pair", "codex_tab_enemy",
        "codex_seal_off", "codex_seal_ready", "codex_seal_on",
    ], canvas=168, content=0.88, flat=0))
    save(split(plates, 2, 2, [
        "codex_card", "codex_card_on", "codex_card_dim", "codex_plaque",
    ], canvas=0, content=1, flat=0, t0=28.0, t1=72.0))


def _bg(img):
    """底色是洋红。大图外圈有白边，正中还有一道白网格，角上取样会取成白。"""
    w, h = img.size
    px = img.load()
    hits = []
    for y in range(0, h, 10):
        for x in (int(w * 0.06), int(w * 0.14), int(w * 0.86), int(w * 0.94)):
            c = px[min(w - 1, x), y][:3]
            if c[0] > 200 and c[1] < 90 and c[2] > 170:
                hits.append(c)
            if len(hits) > 12:
                hits.sort()
                return hits[len(hits) // 2]
    if hits:
        hits.sort()
        return hits[len(hits) // 2]
    return px[min(8, w - 1), min(8, h - 1)][:3]


def _cut(cell, bg, t0, t1):
    cut = key_out(cell, bg, t0=t0, t1=t1)
    cut, _ = drop_bleed(cut)
    # 键控后边角还留着几个 alpha，getbbox 会把整格空白都算进去。
    mask = cut.split()[3].point(lambda p: 255 if p > 80 else 0)
    box = mask.getbbox()
    if box:
        cut = cut.crop(box)
    return cut


def _dehalo(im):
    """贴纸图外圈那道白描边：从透明区往里灌，灌到的近白像素一起抠掉。"""
    w, h = im.size
    px = im.load()
    seen = bytearray(w * h)
    stack = []
    for x in range(w):
        stack += [(x, 0), (x, h - 1)]
    for y in range(h):
        stack += [(0, y), (w - 1, y)]

    def soft(c):
        if c[3] < 60:
            return True
        lo, hi = min(c[:3]), max(c[:3])
        return lo > 214 and hi - lo < 34 or (c[0] > 200 and c[2] > 180 and c[1] < 150)

    while stack:
        x, y = stack.pop()
        i = y * w + x
        if seen[i]:
            continue
        seen[i] = 1
        c = px[x, y]
        if not soft(c):
            continue
        px[x, y] = (0, 0, 0, 0)
        if x > 0: stack.append((x - 1, y))
        if x < w - 1: stack.append((x + 1, y))
        if y > 0: stack.append((x, y - 1))
        if y < h - 1: stack.append((x, y + 1))
    box = im.split()[3].point(lambda p: 255 if p > 80 else 0).getbbox()
    return im.crop(box) if box else im


def _paper(im, to=(252, 252, 246)):
    """卡面换成字图那张纸的底色，字图贴上去不露白方块。"""
    w, h = im.size
    px = im.load()
    ref = px[w // 2, h // 2]
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if c[3] > 200 and abs(c[0] - ref[0]) + abs(c[1] - ref[1]) + abs(c[2] - ref[2]) < 26:
                px[x, y] = to + (c[3],)
    return im


def _shrink(im, long_side):
    w, h = im.size
    m = max(w, h)
    if m <= long_side:
        return im
    s = long_side / m
    return im.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)


def _border(im):
    """九宫格边：从中心色往外走到变色的地方，边框留在四边不拉伸。"""
    w, h = im.size
    px = im.load()
    cx, cy = w // 2, h // 2
    center = px[cx, cy]

    def far(c):
        if c[3] < 40:
            return True
        return ((c[0] - center[0]) ** 2 + (c[1] - center[1]) ** 2 + (c[2] - center[2]) ** 2) ** 0.5 > 38

    left = cx
    while left > 0 and not far(px[left, cy]):
        left -= 1
    right = cx
    while right < w - 1 and not far(px[right, cy]):
        right += 1
    top = cy
    while top > 0 and not far(px[cx, top]):
        top -= 1
    bot = cy
    while bot < h - 1 and not far(px[cx, bot]):
        bot += 1
    # 再往外收 2px，金线留在边里，中心只剩纯底。
    return (min(left + 2, cx - 4), min(h - 1 - bot + 2, cy - 4),
            min(w - 1 - right + 2, cx - 4), min(top + 2, cy - 4))


def _meta(path, border):
    import uuid
    im = Image.open(path)
    cap = 1024
    guid = uuid.uuid4().hex
    sid = uuid.uuid4().hex[:32]
    l, b, r, t = border
    text = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 14
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  webStreaming: 0
  priorityLevel: 0
  uploadedMode: 2
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {cap}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {l}, y: {b}, z: {r}, w: {t}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {cap}
    maxPlaceholderSize: 32
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: WeixinMiniGame
    maxTextureSize: {cap}
    maxPlaceholderSize: 32
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {sid}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  doOverrideTextureManagerOperations: 0
  platformOperationGroupSettings: 
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta = path + ".meta"
    # 已有图保持 guid，只改边和网格，避免引用断掉。
    if os.path.exists(meta):
        old = open(meta, encoding="utf-8").read()
        old = old.replace("spriteMeshType: 0", "spriteMeshType: 1")
        import re
        old = re.sub(r"spriteBorder: \{[^}]*\}",
                     "spriteBorder: {x: %d, y: %d, z: %d, w: %d}" % (l, b, r, t), old)
        old = old.replace("maxTextureSize: 256", "maxTextureSize: 1024")
        open(meta, "w", encoding="utf-8").write(old)
    else:
        open(meta, "w", encoding="utf-8").write(text)


def v2():
    os.makedirs(DEST, exist_ok=True)
    cards = Image.open(os.path.join(RAW, "codex_v2_cards.png")).convert("RGBA")
    cw, ch = cards.width // 2, cards.height // 2
    card_names = ["codex_card_on", "codex_card", "codex_card_dim", "codex_nameplate"]
    bg = _bg(cards)
    for i, name in enumerate(card_names):
        cell = cards.crop(((i % 2) * cw, (i // 2) * ch, (i % 2 + 1) * cw, (i // 2 + 1) * ch))
        cut = _shrink(_dehalo(_cut(cell, bg, 40.0, 96.0)), 640 if name != "codex_nameplate" else 480)
        if name in ("codex_card_on", "codex_card"):
            cut = _paper(cut)
        path = os.path.join(DEST, name + ".png")
        cut.save(path, optimize=True)
        # 名牌是胶囊：左右两头整个圆帽连同金钉都不拉，只拉中段。
        if name == "codex_nameplate":
            cap = int(cut.height * 0.95)
            border = (cap, 0, cap, 0)
        else:
            border = _border(cut)
        _meta(path, border)
        print(name, cut.size, "border", border)

    chrome = Image.open(os.path.join(RAW, "codex_v2_chrome.png")).convert("RGBA")
    gw, gh = chrome.width // 2, chrome.height // 2
    chrome_names = ["codex_tab_off", "codex_tab_on", "codex_track", "codex_track_fill"]
    bg = _bg(chrome)
    for i, name in enumerate(chrome_names):
        cell = chrome.crop(((i % 2) * gw, (i // 2) * gh, (i % 2 + 1) * gw, (i // 2 + 1) * gh))
        cut = _shrink(_dehalo(_cut(cell, bg, 36.0, 80.0)), 720)
        path = os.path.join(DEST, name + ".png")
        cut.save(path, optimize=True)
        # 进度条也是胶囊，只横向拉中段，高度按原比例缩，不然凹槽会被压扁。
        if name.startswith("codex_track"):
            cap = int(cut.height * 0.55)
            border = (cap, 0, cap, 0)
        else:
            border = (0, 0, 0, 0)
        _meta(path, border)
        print(name, cut.size, "border", border, "aspect", round(cut.width / cut.height, 3))

    miles = Image.open(os.path.join(RAW, "codex_v2_miles.png")).convert("RGBA")
    bg = _bg(miles)
    boxes = {
        "codex_mile_off": (59, 126, 991, 669),
        "codex_mile_ready": (1057, 126, 1989, 669),
        "codex_mile_done": (1057, 755, 1989, 1294),
        "codex_stamp": (59, 1399, 649, 1990),
        "codex_splash": (751, 1399, 1320, 1990),
        "codex_spark": (1477, 1399, 1954, 1990),
    }
    for name, box in boxes.items():
        cut = _shrink(_dehalo(_cut(miles.crop(box), bg, 36.0, 80.0)), 480)
        path = os.path.join(DEST, name + ".png")
        cut.save(path, optimize=True)
        _meta(path, (0, 0, 0, 0))
        print(name, cut.size, "aspect", round(cut.width / max(1, cut.height), 3))


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "v2":
        v2()
    else:
        main()
