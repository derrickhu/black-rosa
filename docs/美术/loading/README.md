# 启动 Loading / Logo

微信第一屏是团结导出的 `bgImageSrc`，WASM 还没起来，所以 logo 和健康忠告必须画进图里。

字标必须先用真字体排出正确的「墨字防线」，再交给 Gemini 只改画法。
直接让模型写四字，很容易写成别的字。

| 文件 | 用途 |
|---|---|
| `raw/logo_type_ref_row.png` | 正确四字拼写条（思源黑体） |
| `raw/logo_wordmark_z.png` | Gemini 连体字标（洋红底，墨字防线） |
| `raw/loading_bg.png` | 竖版底图（无字） |
| `final/logo.png` | 去底后的字标 |
| `Assets/Art/Loading/loading.jpg` | 1080×1920 封面 |
| `Assets/Resources/Art/Ui/logo.png` | 透明字标，给首页/分享用 |

融进字里的图：墨＝四滴紫墨；字＝子里一张字牌；防＝方里一枚盾；线＝绞丝是一条紫墨带。

重建：

```bash
python3 ~/.cursor/skills/gemini-image-gen/scripts/generate_images.py \
  --prompt-file docs/prompt/ui/logo_wordmark_z_prompt.txt \
  --output docs/美术/loading/raw/logo_wordmark_z.png \
  --image docs/美术/loading/raw/logo_type_ref_row.png \
  --aspect-ratio 16:9 --image-size 2K

.venv-mock/bin/python docs/prompt/ui/compose_loading.py
pngquant --quality=70-90 --speed 1 --force --output Assets/Resources/Art/Ui/logo.png -- Assets/Resources/Art/Ui/logo.png
```

封面是一张图：logo 和忠告直接叠在底上，不垫白卡。进度条才是插件另画的一层。
`game.js` 里 `designWidth/Height = 1080×1920`，`scaleMode = NO_BORDER`（刘海屏不留上下黑边），条宽 800、`bottom: 270`，忠告从 y=1696 起。

视频也行：`VideoUrl` 必须是网络 URL。先出 `bgImageSrc` 封面，片子就绪后循环播。
官方建议竖版 6–15 秒、MP4、不超过 10MB。

当前成片：`../game_assets/black-rosa/assets/raw/loading/loading3.mp4`
公网：`https://726f-rosa-env-d7grf78r5dbd37323-1414200063.tcb.qcloud.la/black-rosa/loading/loading3.mp4`
