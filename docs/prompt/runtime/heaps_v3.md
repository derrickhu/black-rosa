# 格子改装 · 第三版（工具本体）

只用 Cursor `GenerateImage`。母版进  
`game_assets/black-rosa/美术/runtime/heaps_v3/`  
确认前不入库。本批 **不要** 飞行炮弹、不要命中特效。

## 风格锁

硬边色块、无描边。2～3 档：亮 / 中 / 暗。不要水墨、软边、描边。宣纸底 `#F4EFE4`。

共用：

Flat 2D game icon, cel-shaded. NO outline. 2-3 hard color steps. Razor-sharp edges. NO watercolor, NO ink-wash. Warm rice paper #F4EFE4. Subject 55-65 percent. NO TEXT.

## 呼吸单图（★1 / ★2 / ★3 各一张）

运行时只做呼吸缩放。形不变，体积变大、细节略加。

| 文件 | 牌 | 升星 |
|---|---|---|
| fire_s1~s3 | 加火 | 一堆火，无柴。★2/★3 火堆更大、火舌更多 |
| split_s1~s3 | 分化 | 2 / 3 / 4 管分叉炮 |
| ice_s1~s3 | 冰 | 冰墙更宽更高，反光更亮 |
| explode_s1~s3 | 爆炸 | 圆弹更大，引线火星更猛 |
| accel_s1~s3 | 加速 | 绿人字 2 / 3 / 4 层，不要火 |
| heavy_s1~s3 | 重击 | 格子上的圆炮弹，更大更沉（不是飞行弹） |

## 动作帧

| 文件 | 牌 | 帧 |
|---|---|---|
| track_sN_l / c / r | 追踪 | 立地瞄准镜左右转头。每星 3 朝向 |
| pierce_sN_in / mid / out | 穿透 | 只一根矛，沿斜轴来回穿。每星 3 位 |

★1 中心 / 中间帧 = 已锁定样张。
