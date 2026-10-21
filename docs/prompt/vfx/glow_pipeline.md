# 发光特效管线

第二幕起，新做的弹体、命中、词、秘卷都走这条。样张是「破」（`act2_glow_prompt.txt` 第一节），已经对过竹林底。

第一幕那批平涂硬边（火、冰、水、毒、土、炸，以及霜火到雷决）保持原样，不按这条重画。

## 一张图里有什么

横条三格，从左到右：

1. **弹体**，尖头朝上，飞出去的那一发能单独认出来。
2. **命中**，正对着看的爆、斩或波。
3. **粒子**，几块分开的碎片或火花，运行时往外甩。

剪影必须和已经有的光效分开：秒杀是细长红刀，连斩是蓝剑和一道月牙，击退是扁弧和椭圆环。新的一张不能再画成刀、剑或那道紫弧。

## 出图

- 工具：Cursor 内置生图。参考图只给画法，路径 `game_assets/black-rosa/美术/特效/act2_glow/word_glow_ref.png`。
- 底是纯黑 `#000000`。主体约占格子的 70%，光平滑散进黑色。
- 每格白热芯，外面三到四档饱和色。颜色写 hex。不写「樱桃红」「金币黄」这种实物色名，模型会把实物画出来。
- 提示词只写要出现的形状和颜色。
- 提示词正文记在 `docs/prompt/vfx/act2_glow_prompt.txt`。母版放 `game_assets/black-rosa/美术/特效/act2_glow/raw/`，不进游戏仓库，确认后再拷。

共用开头：

```text
A sprite sheet of three glowing magic effect layers for a cartoon mobile game. Three equal columns side by side. The background of every column and every gap is pure black #000000. Each column holds one separate element, centered, filling about 70 percent of the column width, with its glow fading smoothly into the black.

The attached image shows the target look: glowing blades, slashes and shockwaves from the same game. Match their soft saturated glow, their white-hot centres and their clean cartoon build. Use only their way of painting light; the shapes come from the list below.
```

共用收束：

```text
Clean cartoon game-effect style, smooth gradients, bright saturated glow. The image contains only these three glowing shapes on black.
```

比例 16:9。

## 去底

`docs/prompt/runtime/glow_slice.py`。alpha 取像素最亮通道，颜色除回 alpha，光晕留成半透明。低于亮度 14 当纯黑。不要 rembg，不要 `vfx_crush.py`（那是旧的压黑加法，会把光晕吃掉）。

```bash
.venv-mock/bin/python docs/prompt/runtime/glow_slice.py \
  <sheet.jpg> <name_shot> <name_burst> <name_shards>
```

输出在件表旁边的 `slice/`，最长边 256。

## 进游戏时怎么叠

战场底是浅色的。整张图如果都用加色混合，金和粉会冲成白。

- 弹体、命中图、粒子图：普通混合（`InkFx.PaintSprite`），白色着色。
- 外晕、白热芯、火星：加色（`InkFx.PaintAdd`），用这发字自己的色。
- 弹体身后留两三道残影，越远越淡，间距约 0.03 世界单位。做法照 `WordGlow`。

## 必须能和别的字拼

发光只换画法，不换五槽规则（`ShotLook`）：同槽只亮优先级最高的一个，异槽全亮。设计新字前先查它在 `GlyphTable` 里占哪个槽，按槽出图：

| 槽 | 发光版要的东西 | 例 |
|---|---|---|
| 体 | 整颗弹体图（`_shot`），只在赢下体槽时画 | 千钧、四对新秘卷 |
| 环 | 一两种单件小图，绕着任何弹体抖 | 破：金粉晶片 |
| 绕 | 单件小珠，绕着弹体转或收 | 聚：一蓝一粉光珠 |
| 尾 | 单件小图，从弹后甩出 | 返：青金弯月 |

- 环 / 绕 / 尾的字也画一张 `_shot`，只在体槽空着时借来当体。体槽被火、穿等别的字占住时，只叠它自己的小件，所以破 + 穿是穿针外面绕一圈晶片。
- 单件小图从 `_shards` 里切：`.venv-mock/bin/python docs/prompt/runtime/glow_bits.py <name_shards.png> <输出前缀> [块数]`，按连通块挑最大且颜色拉得开的几块。
- 命中：赢下命中层的就是这个字时整张换发光爆点；被别的字赢走时原墨溅照播，发光爆点缩到 0.7 叠上去。
- 代码在 `View/GlowShot.cs`。它挂在 `InkShot` 同一节点下，只让出体槽，不清空别的层。

离线预览先按这个叠到 `CdnArt/Bg/battle_bg_9.png` 上看，再决定进不进 `Assets/Resources/Art/Vfx/`。
