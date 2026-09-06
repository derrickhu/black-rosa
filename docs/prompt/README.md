# 墨弹防线 · 生图 prompt

1. 先读 [美术风格圣经.md](../美术风格圣经.md) 和 [美术风格候选.md](../美术风格候选.md)。
2. 每份 prompt **原样粘贴** [`_principles_block.txt`](./_principles_block.txt)，再粘贴该套 `_style_block.txt`，然后写这一镜的构图。
3. 不要在单份 prompt 里另起炉灶重写风格。
4. 原图输出到仓库外 `game_assets/black-rosa/美术/风格原型/`，确认前不进游戏工程。

## 命名

`style_{a|b|c}/{shot}_prompt.txt`

`00_board` `01_lobby` `02_battle` `03_draft` `04_result` `05_units` `06_cards` `07_vfx`

## 模型

- Gemini NB2：`gemini-3.1-flash-image-preview`，页面 `--aspect-ratio 9:16`，表 `1:1`，`--image-size 1K`
- Cursor 内置 `GenerateImage`：同一份 prompt，生成后立刻拷到 `cursor/` 目录
