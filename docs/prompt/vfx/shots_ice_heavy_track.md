# 冰 / 重击 / 追踪 · 飞行弹动态帧

只用 Cursor `GenerateImage`。母版进  
`game_assets/black-rosa/美术/runtime/vfx/{ice,heavy,track}/`

口径跟火焰弹：每套 **8 帧**，同剪影尺度循环。★1 播 00–03，★2 播 02–05，★3 播 04–07。

冰 / 追踪必须抄火弹的**形**：直泪滴，白核，硬色阶，无描边。不要大冰晶簇，不要 S 弯尾。

追踪拐弯脱尾由运行时路径拖尾画，贴图本身保持竖直。

火+冰融合弹见 [`shots_fireice.md`](shots_fireice.md)。默认无元素弹复用 `heavy_shot`，不再为普通攻击单独生图。

## 共用

2D game VFX sprite. PURE BLACK background only (#000000). NO floor, NO character, NO text, NO watermark. NO outline. Hard color steps. Traveling UP: head at the TOP, tail toward the BOTTOM. Same silhouette scale every frame. Occupies central 55-65 percent.

## 冰弹 ice_shot — 火弹形 / 冰色

直泪滴，跟火弹同一剪影。色：白核 / `#D8F2FA` / `#269EDB`。不要黑边、不要深蓝描边。各帧只闪尾，不要换形。

## 重击 heavy_shot — 黑白头+锯齿尾

圆黑弹头在上（头必须是实心黑，去背时从四角洪水填充，不要把弹头抠穿），粗白锯齿尾在下。

## 追踪 track_shot — 火弹形 / 紫色 / 直尾

直泪滴。色：白核 / 粉紫 / `#9E47DB`。尾必须直，不要钩、不要 S 弯。
