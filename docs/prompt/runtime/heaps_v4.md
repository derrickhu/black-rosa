# 格子改装 · 第四版（平面卡牌）

只用 Cursor `GenerateImage`。母版进  
`game_assets/black-rosa/美术/runtime/heaps_v4/`  
炮弹 / 命中特效不要动。工具**不要**立体、不要高饱和、不要动画。

战场上的鲜艳色留给穿过后的弹。卡面只做石墨线和简单形。

## 风格锁

整张图就是一张方卡，铺满画面，圆角。卡芯 `#FCFCF6`。双线石墨框 `#2E2E2E`。正中一个线符，粗细均匀的墨线，**只有石墨，不上别的色**。不要填色块、不要 3D、不要渐变、不要投影、不要发光。不要字、星、数字。

共用：

Square playing card face, edge to edge, rounded corners. Cream #FCFCF6. Double graphite #2E2E2E line border like a playing card. ONE centered pictogram, thick even ink lines only, graphite #2E2E2E, no fill color, no 3D, no gradient, no shadow, no glow. NO TEXT, no numbers, no stars, no watermark.

## 八张（各一张静帧）

| 文件 | 牌 | 线符 |
|---|---|---|
| card_fire | 加火 | 三舌火焰轮廓 |
| card_ice | 加冰 | 菱晶轮廓 |
| card_split | 分化 | 向上分叉的 Y 箭 |
| card_track | 追踪 | 弯弧箭头 |
| card_pierce | 穿透 | 竖矛，菱尖 |
| card_explode | 爆炸 | 圆 + 八短芒 |
| card_accel | 加速 | 朝上单层人字 |
| card_heavy | 重击 | 实心圆点 |

运行时同一张图，星级仍用格角小菱。不要 s2/s3，不要转头/穿刺/呼吸。
