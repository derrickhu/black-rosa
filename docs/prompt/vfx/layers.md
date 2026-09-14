# 炮弹分层特效（旧母版）

> 2026-09-08：运行时已改成程序化软光 + 色带，不再播这些噪点帧。本文件只留旧母版记录。

母版进 `game_assets/black-rosa/美术/runtime/vfx/layers/`。  
入库：`Assets/Resources/Art/Vfx/`。

不要再为元素做 8 帧整弹皮。`fire_shot` / `ice_shot` / `fireice_shot` 只当 overlay 缺省回落。

## 共用

2D game VFX sprite. PURE BLACK background only (#000000), no gradient, no floor, no character, no text, no watermark. Graphic ink-wash, 2-3 hard flat color steps, crisp edges. NOT pixel art, NOT 8-bit, NOT photo, NO thick black outline around the glow. Effect occupies the central 55-65 percent. Rest of image must be completely pure black.

## 核 ink_core（2 帧呼吸，洪水去背，保留实心黑头）

Slim vertical calligraphy ink droplet traveling UP. Glossy solid black sphere head at the TOP with a thin white crescent highlight. Short jagged white ink-splash tail toward the BOTTOM, a few tiny white ink dots. High-contrast black and white only. Fountain-pen blot, not a fat fireball.

| 文件 | 帧 |
|---|---|
| ink_core_00 | 尾略短，核中等 |
| ink_core_01 | 尾略长，高光更亮 |

## overlay（4 帧，中心空，给墨核让位）

火：`#F6C44A` / `#EB6114` / 深红橙。冰：`#D8F2FA` / `#269EDB`。头朝上，尾朝下。不要实心弹头。

| 文件 | 帧 |
|---|---|
| fire_wrap_00–03 | 火舌左右闪，中心空 |
| ice_wrap_00–03 | 霜羽左右闪，中心空 |

## 印（单帧）

| 文件 | 形 |
|---|---|
| pierce_tip | 白墨尖锋，竖直朝上 |
| stun_ring | 金黄 `#E8B43A` 破墨圈 |
| kill_mark | 朱红杀印 / 短斩 |

## 命中（各 4 帧：炸开 → 峰值 → 碎）

白核 → 饱和中层 → 深外沿。硬边星爆，不要烟团。

| 前缀 | 色 |
|---|---|
| hit_ink | 黑白墨溅 |
| hit_ice | 冰色 |
| hit_explode | `#DB3847` |
| hit_heavy | 白震圈 + 墨刺 |

现有 `fire_hit` 继续当火命中。
