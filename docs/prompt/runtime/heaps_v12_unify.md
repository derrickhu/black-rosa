# 汉字牌 · 统一字图（v12）· 已被 v13 取代

> **这版已停用。** v12 是纯黑书法、明令禁止任何装饰，结果每个字没有个性。
> 现行版本是 **[`heaps_v13_glyphfx.md`](heaps_v13_glyphfx.md)**：字效融进笔画，加字请看那一份。
> 本文只保留画布 / 留白这部分口径。**归一化也换了** —— v12 按外接框缩到 58%，
> 结果密的字看起来重、带装饰的字被压小；v13 改成按墨量归一化，脚本是
> `heaps_v13_normalize.py`，不要再用 `heaps_v12_normalize.py`。

只用 Cursor `GenerateImage`。  
母版：`game_assets/black-rosa/美术/runtime/heaps_v12/`  
入库：`Assets/Resources/Art/heap_<key>.png`  
运行时由 `InkArt.OnCard` 套宣纸框，并按墨迹包围盒缩放到卡芯 **64%**。

以后加字：只生这一张字图，文件名 `heap_<key>.png`，同一套提示词，不要画框。  
生完后跑 `docs/prompt/runtime/heaps_v12_normalize.py`，把墨迹外接框收到画布 **58%** 再入库。

## 生图提示词（以后加字只改「字 / 色 / 文件名」）

风格参考用已过的 `heap_fire.png`。

```
Square 1:1 game glyph, not a card.

Match the attached reference EXACTLY for style, size, and margins: ONE handwritten Chinese calligraphy character, perfectly centered. The glyph bounding box occupies about 58% of the canvas. Equal empty paper margin on all four sides — at least 20% empty paper on every edge. No stroke may touch or leave the frame.

Plain warm paper-white #FCFCF6 fills the entire square. NO card frame, NO rounded rectangle, NO double border, NO star, NO number, NO English, NO watermark, NO decorative icons, NO flames, NO ice crystals, NO arrows, NO chevrons.

Thick solid brush calligraphy, closed clean edges, slight handwriting tilt, same stroke weight as the reference. NOT computer Heiti, NOT Weibei, NOT flying-white splash ink, NOT 3D shadow.

Character: {字} ({英文}). The WHOLE character is solid {黑 #1A1410 / 黄 #E8B43A}. No other colors. Correct simplified Chinese. Complete strokes, not cropped.
```

## 画布

- 1:1。浅纸白 `#FCFCF6` 铺满整张。
- **不要卡框、圆角、双线、星、数字、英文、水印、装饰图标。**
- 正中 **一个** 简体字。字的外接框约占画布 **58%**，四边留白相等。任何一笔不得贴边、不得出画。
- 手写厚墨、实心、边缘闭合、略有笔势。同一套粗细。  
  不要电脑黑体、不要魏碑、不要泼墨飞白、不要立体阴影。
- 常驻字（含蓄力字）整字 `#1A1410`。词组字整字 `#E8B43A`。不要混色。
- 效果只在炮弹上。字图上不要火苗、冰晶、箭头、人字标。

## 文件

| 文件 | 字 | 色 |
|---|---|---|
| heap_fire | 火 | 黑 |
| heap_ice | 冰 | 黑 |
| heap_split | 分 | 黑 |
| heap_track | 瞄 | 黑 |
| heap_accel | 速 | 黑 |
| heap_pierce | 穿 | 黑 |
| heap_explode | 炸 | 黑 |
| heap_heavy | 重 | 黑 |
| heap_stun | 晕 | 黑 |
| heap_sec | 秒 | 黄 |
| heap_kill | 杀 | 黄 |
| heap_myriad | 万 | 黄 |
| heap_arrow | 箭 | 黄 |
| heap_strike | 击 | 黄 |
| heap_back | 退 | 黄 |
| heap_link | 连 | 黄 |
| heap_slash | 斩 | 黄 |
