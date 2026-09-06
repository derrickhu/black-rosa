# 冰火弹 fireice_shot

只用 Cursor `GenerateImage`。母版进  
`game_assets/black-rosa/美术/runtime/vfx/fireice/`

口径跟 [`shots_ice_heavy_track.md`](shots_ice_heavy_track.md) / 火焰弹：8 帧，同剪影尺度循环。★1 播 00–03，★2 播 02–05，★3 播 04–07。

一颗弹上同时有火和冰，不要两颗、不要对半旗、不要各播一半。

参考：`Assets/Resources/Art/Vfx/fire_shot_NN.png` + `ice_shot_NN.png`。

## 共用

2D game VFX sprite. PURE BLACK background only (#000000). NO floor, NO character, NO text, NO watermark. NO outline. Hard color steps. Traveling UP: head at the TOP, tail toward the BOTTOM. Same silhouette scale every frame. Occupies central 55-65 percent.

ONE fused fire-and-ice teardrop. White core. Fire orange `#EB6114` / yellow `#F6C44A` bites the head and one side of the tail. Ice cyan `#269EDB` / `#D8F2FA` bites the other side plus a few shards. Colors interlock on a single drop. NOT two bullets, NOT a vertical half-and-half flag.

## 帧

| 文件 | 帧 |
|---|---|
| fireice_shot_00 | 尾偏左，核中等 |
| fireice_shot_01 | 尾拉长，核更亮 |
| fireice_shot_02 | 尾偏右，火花和冰屑散开 |
| fireice_shot_03 | 尾收短，核略暗 |
| fireice_shot_04 | 中档，尾更长，核更大 |
| fireice_shot_05 | 中档，外沿更深（更深橙 + 更深青） |
| fireice_shot_06 | 高档，弹体明显变大 |
| fireice_shot_07 | 高档峰值，尾最长 |
