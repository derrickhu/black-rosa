# 加火验证特效（篝火 / 火焰弹 / 命中）

只用 Cursor `GenerateImage`。原图进 `game_assets/black-rosa/美术/runtime/vfx/fire/`，确认前不入库。

## 实现口径

- 火堆 / 火焰弹各 8 帧：前段小、后段更大更深。按星切窗口循环，不要 0–7 整段播（高星会缩回低星）
- ★1 播 00–03，★2 播 02–05，★3 播 04–07
- 火堆：宣纸底去背，普通混合。火焰弹：黑底去黑
- 命中仍 4 帧一次性，大小不跟星走
- 同屏弹共用一套图，相位错开；命中池 16

## 共用（弹 / 命中）

2D game VFX sprite. PURE BLACK background only (#000000), no gradient, no floor, no character, no text, no watermark. High contrast, crisp hard color steps: white core, yellow, orange #EB6114, deep orange-red. NO black outline, NO thick ink contour. Effect occupies the central 55-65 percent. Rest of image must be completely pure black.

## 火堆（宣纸）

Warm rice paper #F4EFE4. One compact ink-wash bonfire, centered. A few brown logs at the base, flames rising. NO black outline, NO thick contour, painterly brush fire, soft irregular flame edges. Subject in the central 60 percent. Empty paper around. NO text.

| 文件 | 帧 |
|---|---|
| fire_heap_00 | 火舌偏低，柴堆清楚 |
| fire_heap_01 | 中间火舌升高，左侧偏亮 |
| fire_heap_02 | 最高，白黄核最大 |
| fire_heap_03 | 回落，右侧火舌偏高 |
| fire_heap_04 | 中档，火舌升高，色更深 |
| fire_heap_05 | 中档，更躁，核更亮 |
| fire_heap_06 | 高档，火堆明显变大，外沿深红橙 |
| fire_heap_07 | 高档峰值，白核最大 |

## 火焰弹（头朝上，尾朝下）

Traveling UP. Bright head at the TOP, jagged flame tail toward the BOTTOM, a few ember sparks. Same silhouette scale every frame.

| 文件 | 帧 |
|---|---|
| fire_shot_00 | 尾偏左，核中等 |
| fire_shot_01 | 尾拉长，核更亮 |
| fire_shot_02 | 尾偏右，火花散开 |
| fire_shot_03 | 尾收短，核略暗 |
| fire_shot_04 | 中档，尾更长，核更大 |
| fire_shot_05 | 中档，外沿更深 |
| fire_shot_06 | 高档，弹体明显变大 |
| fire_shot_07 | 高档峰值，尾最长 |

## 命中

Starburst impact, no character.

| 文件 | 帧 |
|---|---|
| fire_hit_00 | 刚炸开，小核 |
| fire_hit_01 | 尖刺伸出 |
| fire_hit_02 | 峰值，白核最大 |
| fire_hit_03 | 碎成火花，将散尽 |
