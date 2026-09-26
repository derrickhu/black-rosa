#!/usr/bin/env python3
"""把出征页组件大图抠成透明 PNG，放进 Resources/Art/Ui。"""
import os
import sys
import uuid

from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
RAW = os.path.join(ROOT, "docs", "prompt", "ui", "raw")
OUT = os.path.join(ROOT, "Assets", "Resources", "Art", "Ui")
sys.path.insert(0, os.path.dirname(__file__))
from process_ui_icons import key_out, drop_bleed, flatten  # noqa: E402


def trim(img, pad=8):
    bbox = img.getbbox()
    if bbox is None:
        return img
    x0, y0, x1, y1 = bbox
    x0 = max(0, x0 - pad)
    y0 = max(0, y0 - pad)
    x1 = min(img.width, x1 + pad)
    y1 = min(img.height, y1 + pad)
    return img.crop((x0, y0, x1, y1))


def snap_magenta(img):
    """洋红边上的粉边也算背景，不然缎带和卡框会留一圈粉。"""
    img = img.convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if r > 180 and b > 150 and g < 170 and (b - g) > 25 and (r - g) > 20:
                px[x, y] = (255, 0, 255, 255)
    return img


def cut_cell(cell, bg, flat=0):
    cell = snap_magenta(cell)
    cut = key_out(cell, bg)
    cut, _ = drop_bleed(cut)
    if flat > 0:
        cut = flatten(cut, flat)
    return cut


def grid(name, cols, rows, names, size):
    sheet = Image.open(os.path.join(RAW, name)).convert("RGBA")
    bg = (255, 0, 255)
    cw, ch = sheet.width // cols, sheet.height // rows
    for i, out_name in enumerate(names):
        cx, cy = i % cols, i // cols
        cell = sheet.crop((cx * cw, cy * ch, (cx + 1) * cw, (cy + 1) * ch))
        cut = trim(cut_cell(cell, bg, flat=0), 4)
        # 正方形画布，内容和现有图标一样居中。
        side = size
        box = int(side * 0.86)
        scale = box / max(cut.width, cut.height)
        art = cut.resize((max(1, round(cut.width * scale)), max(1, round(cut.height * scale))), Image.LANCZOS)
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        canvas.paste(art, ((side - art.width) // 2, (side - art.height) // 2), art)
        dest = os.path.join(OUT, out_name + ".png")
        canvas.save(dest, optimize=True)
        print(out_name, canvas.size)


def one(name, dest, max_w):
    sheet = Image.open(os.path.join(RAW, name)).convert("RGBA")
    cut = trim(cut_cell(sheet, (255, 0, 255), flat=0), 2)
    if cut.width > max_w:
        h = max(1, round(cut.height * max_w / cut.width))
        cut = cut.resize((max_w, h), Image.LANCZOS)
    path = os.path.join(OUT, dest)
    cut.save(path, optimize=True)
    print(dest, cut.size)


def chapters():
    sheet = Image.open(os.path.join(RAW, "sortie_parts_chapters.png")).convert("RGBA")
    w, h = sheet.size
    px = sheet.load()
    # 竖向找洋红缝，把三张小图切开。
    col_mag = []
    for x in range(w):
        mag = 0
        for y in range(0, h, 4):
            r, g, b, a = px[x, y]
            if r > 220 and g < 40 and b > 220:
                mag += 1
        col_mag.append(mag > (h / 4) * 0.55)
    spans = []
    in_art = False
    start = 0
    for x, is_mag in enumerate(col_mag + [True]):
        if not is_mag and not in_art:
            in_art = True
            start = x
        elif is_mag and in_art:
            in_art = False
            if x - start > 40:
                spans.append((start, x))
    print("chapter spans", spans)
    for i, (x0, x1) in enumerate(spans[:3], start=1):
        cell = sheet.crop((x0, 0, x1, h))
        cut = trim(cut_cell(cell, (255, 0, 255), flat=0), 2)
        max_w = 720
        if cut.width > max_w:
            nh = max(1, round(cut.height * max_w / cut.width))
            cut = cut.resize((max_w, nh), Image.LANCZOS)
        dest = os.path.join(OUT, f"chapter_{i}.png")
        cut.save(dest, optimize=True)
        print(f"chapter_{i}", cut.size)


def write_meta(path, size):
    meta = path + ".meta"
    guid = uuid.uuid4().hex
    sid = uuid.uuid4().hex[:32]
    cap = 2048 if size > 256 else 256
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
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
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
    with open(meta, "w", encoding="utf-8") as f:
        f.write(text)


def main():
    os.makedirs(OUT, exist_ok=True)
    grid("sortie_parts_icons.png", 2, 2, [
        "ico_act_circle", "ico_act_checkin", "ico_act_event", "ico_act_rank",
    ], 256)
    grid("sortie_parts_nodes.png", 2, 2, [
        "ico_node_done", "ico_node_now", "ico_node_lock", "ico_node_boss",
    ], 256)
    one("sortie_parts_ribbon.png", "ribbon_chapter.png", 900)
    one("sortie_parts_card.png", "panel_chapter.png", 900)
    chapters()
    made = [
        "ico_act_circle", "ico_act_checkin", "ico_act_event", "ico_act_rank",
        "ico_node_done", "ico_node_now", "ico_node_lock", "ico_node_boss",
        "ribbon_chapter", "panel_chapter", "chapter_1", "chapter_2", "chapter_3",
    ]
    for name in made:
        path = os.path.join(OUT, name + ".png")
        im = Image.open(path)
        write_meta(path, max(im.size))
        print("meta", name, im.size)


if __name__ == "__main__":
    main()
