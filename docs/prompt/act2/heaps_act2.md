# 第二幕字图：破 / 聚 / 返 / 千 / 钧

沿用《heaps_v13_glyphfx.md》：字身 90% 黑墨手写，只换一个小笔画成元素意象；词组字反过来金身墨饰。
Cursor `GenerateImage`，参考图给 `heap_fire.png`（常驻字）/ `heap_sec.png`（词组字），只借画法。
母版 `game_assets/black-rosa/美术/runtime/heaps_act2/`，归一化后入 `Assets/Resources/Art/heap_<key>.png`：

```
python3 docs/prompt/runtime/heaps_v13_normalize.py <母版目录> -o Assets/Resources/Art --size 256
```

| 文件 | 字 | 换哪一笔 | 意象 | 色 |
|---|---|---|---|---|
| heap_break | 破 | 皮的末捺 | 笔尖迸成三片锐利碎片，贴着笔画 | 红砖色 `#C8442A` + 杏肉色 `#F4A07A` |
| heap_gather | 聚 | 底部乑的四笔 | 四笔末端都朝字心弯成小涡 | 牛仔布蓝 `#2F5D9E` + 晴空蓝 `#9EC2F0` |
| heap_rebound | 返 | 辶的长捺 | 末端向上回勾成 U 形折返 | 孔雀羽青 `#2E9C8C` + 薄荷 `#A6E6DA` |
| heap_qian | 千 | 中央长竖 | 竖的末端坠成一枚小秤砣 | 金身 `#E8B43A`，意象墨黑 `#1A1410` |
| heap_jun | 钧 | 匀里的两短横 | 两颗实心小墨石 | 金身 `#E8B43A`，意象墨黑 `#1A1410` |

## 通用提示词

```
One single simplified Chinese character "<字>" centred on a flat light rice-paper background
the colour of fresh printer paper with a faint warm tint (#FCFCF6), filling the whole square.
Thick hand-brushed ink calligraphy, solid filled strokes with closed clean edges and a gentle
brush slant, exactly the brush language of the attached reference card glyph.
The character body is <字身色>. Every stroke keeps its standard structure: <逐笔结构>.
Only <换哪一笔> becomes <意象>, coloured <意象色>, the same size as the stroke it replaces,
attached to the character and still reading as that stroke.
Flat hand-painted two-tone shapes in the same illustration language as the brush strokes.
The character sits in the centre at about 60% of the canvas with equal empty paper on all sides.
```
