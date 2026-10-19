"""墨字防线 · 买量竖版视频 v5「魔潮 · 冰火 · 关底」（实机录屏剪辑）。

和 04 错开：04 讲汉字当炮弹、万箭、连杀；这条讲新内容 ——
无尽第 38~40 波的魔化莲池、冰 + 火凑出霜火、第八章关底几秒蒸发、三星通关开宝箱。
录屏用 PROMO_CAPTURE 包的 -promoScene endless / frost / boss（见 GameFlow.Promo.cs）。
剪辑骨架复用 promo_video_v1.py，只换素材、字幕和解说。

    .venv-mock/bin/python docs/prompt/promo/promo_video_v5.py
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import promo_video_v1 as v  # noqa: E402

v.TAKES = {
    "en": Path("/tmp/black-rosa-promo3/take_endless"),  # 魔化无尽第 38 波起，三排满配
    "fr": Path("/tmp/black-rosa-promo3/take_frost70"),  # 第八章，第一排三星冰，3.5 秒送火
    "bo": Path("/tmp/black-rosa-promo3/take_boss"),     # 第八章章底最后一波，满配
}
v.CUTS = [
    ("en", 22.0, 3.0, "魔潮压境", "一炮清屏！"),
    ("fr", 3.6, 4.2, "冰字冻住 火字再烧", "冰+火=霜火！"),
    ("bo", 1.6, 2.8, "关底大魔王", "万箭齐发！"),
    ("bo", 8.0, 3.0, "8秒蒸发！", ""),
    ("en", 33.0, 4.0, "无尽50波", "魔气越来越浓"),
    ("en", 50.0, 3.5, "每周换词缀", "全服一起冲榜"),
    ("bo", 117.4, 4.0, "三星通关", "宝箱拿到手软"),
]
v.END_SECS = 4.1
v.WORK = Path("/tmp/black-rosa-promo-cut-v5")
v.VO_DIR = v.OUT_DIR / "vo_05"
v.OUT_NAME = "gameplay_promo_05_motide_vo_1080x1920.mp4"
v.VO = [
    (0.15, "魔潮来了！一炮清屏！"),
    (3.1, "冰字冻住，再塞个火字，冰火双杀！"),
    (7.25, "关底大魔王？万箭齐发！"),
    (10.1, "八秒，直接蒸发！"),
    (13.1, "无尽五十波，魔气越来越浓！"),
    (17.1, "每周换词缀，全服一起冲榜！"),
    (20.6, "三星通关，宝箱拿到手软！"),
    (24.6, "墨字防线，微信搜一下，马上开玩！"),
]

if __name__ == "__main__":
    v.main()
