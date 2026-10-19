"""墨字防线 · 真人短剧买量片 v1「末日炮台」剪辑。

六个 Vidu 镜头（drama_01/clips）+ 一小段实机冰火 + 尾板。
台词字幕、钩子大字都用真实字体后期叠。分镜见 zombie_drama_v1.md。

    .venv-mock/bin/python docs/prompt/promo/drama_cut_v1.py
"""
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from promo_video_v1 import (FONT_BODY, FONT_HEAD, HAND, TV_RANGE, W, H, FPS,  # noqa: E402
                            end_card, font, gradient_text, ribbon, run)

ASSETS = Path("/Users/rosa/rosa_games/game_assets/black-rosa")
DRAMA = ASSETS / "推广/宣传视频/drama_01"
CLIPS = DRAMA / "clips"
OUT = ASSETS / "推广/宣传视频/drama_promo_01_zombie_1080x1920.mp4"
WORK = Path("/tmp/black-rosa-drama-cut")
REPO = Path(__file__).resolve().parents[3]
BGM = REPO / "CdnArt/Audio/bgm_endless.mp3"
VO_END = ASSETS / "推广/宣传视频/vo_04/line6_badao_shaoye.mp3"
GAME = Path("/tmp/black-rosa-promo3/take_frost70")
GAME_START = 4.0
GAME_SECS = 2.4
END_SECS = 2.4
BGM_GAIN = 0.16

# (镜头, 入点, 出点, 顶部大字, 飘带, [(字幕起, 止, 说话人, 台词)])；字幕时间按镜头内的秒算。
SHOTS = [
    ("s1_bonk", 0.0, 4.9, "末日炮台", "炮弹打不动僵尸？", [(3.6, 4.9, "阿强", "啊？没用？！")]),
    ("s2_aim", 0.0, 4.3, "", "", [(0.3, 2.2, "小墨", "炮弹，要穿过字。")]),
    ("s3_freeze", 0.0, 4.4, "过「冰」字", "炮弹变冰弹！", []),
    ("s4_react", 0.4, 3.3, "", "", [(0.6, 1.9, "阿强", "那这个呢？"), (2.0, 3.3, "小墨", "试试。")]),
    ("s5_burn", 0.8, 4.8, "过「火」字", "一炮烧成灰！", []),
    ("s6_boss", 0.0, 4.6, "", "", [(1.2, 3.2, "阿强", "还……还有字吗？")]),
]
SPEAKER = {"阿强": (255, 214, 64), "小墨": (150, 220, 255)}


def head_png(head, sub, path):
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    if head:
        t = gradient_text(head, font(FONT_HEAD, 150), (255, 236, 92), (255, 146, 30), (58, 30, 16), 12, tilt=3.0)
        canvas.alpha_composite(t, ((W - t.width) // 2, 150))
        if sub:
            r = ribbon(sub, font(FONT_BODY, 60, 900))
            canvas.alpha_composite(r, ((W - r.width) // 2, 150 + t.height - 18))
    canvas.save(path)


def line_png(who, text, path):
    """短剧字幕：说话人名字上色，白字粗黑边，压在画面下三分之一。"""
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    name_f = font(FONT_BODY, 50, 900)
    text_f = font(FONT_BODY, 76, 900)
    name = who + "："
    nb = d.textbbox((0, 0), name, font=name_f, stroke_width=6)
    tb = d.textbbox((0, 0), text, font=text_f, stroke_width=7)
    total = (nb[2] - nb[0]) + (tb[2] - tb[0])
    x = (W - total) // 2
    y = 1500
    d.text((x - nb[0], y + (tb[3] - tb[1]) - (nb[3] - nb[1]) - nb[1] - 4), name, font=name_f,
           fill=SPEAKER[who] + (255,), stroke_width=6, stroke_fill=(20, 12, 8, 255))
    d.text((x + nb[2] - nb[0] - tb[0], y - tb[1]), text, font=text_f,
           fill=(255, 255, 255, 255), stroke_width=7, stroke_fill=(20, 12, 8, 255))
    canvas.save(path)


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    segs, overlays = [], []
    t = 0.0
    for i, (name, a, b, head, sub, lines) in enumerate(SHOTS):
        seg = WORK / f"seg{i}.mov"
        dur = b - a
        run(["ffmpeg", "-y", "-v", "error", "-ss", f"{a:.3f}", "-t", f"{dur:.3f}", "-i", str(CLIPS / f"{name}.mp4"),
             "-vf", f"scale={W}:{H}:flags=lanczos,fps={FPS},format=yuv420p",
             "-af", f"aresample=48000,afade=t=in:d=0.03,afade=t=out:st={dur - 0.05:.3f}:d=0.05",
             "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-c:a", "pcm_s16le", "-ac", "2", str(seg)])
        segs.append(seg)
        if head:
            png = WORK / f"head{i}.png"
            head_png(head, sub, png)
            overlays.append((png, t, t + min(dur, 2.6)))
        for k, (s0, s1, who, text) in enumerate(lines):
            png = WORK / f"line{i}_{k}.png"
            line_png(who, text, png)
            overlays.append((png, t + max(s0, a) - a, t + min(s1, b) - a))
        t += dur

    game = WORK / "game.mov"
    f0 = int(round(GAME_START * FPS))
    n = int(round(GAME_SECS * FPS))
    run(["ffmpeg", "-y", "-v", "error", "-start_number", str(f0), "-framerate", str(FPS),
         "-i", str(GAME / "f%05d.jpg"), "-ss", f"{GAME_START:.3f}", "-t", f"{GAME_SECS:.3f}", "-i", str(GAME / "audio.wav"),
         "-frames:v", str(n), "-vf", TV_RANGE, "-af", "aresample=48000,volume=0.5",
         "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "pcm_s16le", "-ac", "2", str(game)])
    segs.append(game)
    png = WORK / "head_game.png"
    head_png("游戏里", "真能这么打！", png)
    overlays.append((png, t, t + GAME_SECS))
    vo_at = t
    t += GAME_SECS

    end_jpg = WORK / "end.jpg"
    btn_y = end_card(GAME / f"f{f0 + n - 1:05d}.jpg", end_jpg)
    hand = Image.open(HAND).convert("RGBA")
    hand = hand.resize((150, int(hand.height * 150 / hand.width)))
    hand.save(WORK / "hand.png")
    end = WORK / "end.mov"
    hx, hy = W // 2 + 215, btn_y + 105
    run(["ffmpeg", "-y", "-v", "error",
         "-loop", "1", "-framerate", str(FPS), "-i", str(end_jpg),
         "-loop", "1", "-framerate", str(FPS), "-i", str(WORK / "hand.png"),
         "-f", "lavfi", "-t", f"{END_SECS}", "-i", "anullsrc=r=48000:cl=stereo",
         "-filter_complex",
         f"[0:v]scale=w='{W}*(1+0.025*abs(sin(t*5.2)))':h=-2:eval=frame,crop={W}:{H}[bg];"
         f"[bg][1:v]overlay=x={hx}:y='{hy}+18*abs(sin(t*5.2))',{TV_RANGE}[v]",
         "-map", "[v]", "-map", "2:a", "-frames:v", str(int(END_SECS * FPS)),
         "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "pcm_s16le", str(end)])
    segs.append(end)
    total = t + END_SECS

    lst = WORK / "list.txt"
    lst.write_text("".join(f"file '{p}'\n" for p in segs))
    body = WORK / "body.mov"
    run(["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", str(lst),
         "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "pcm_s16le", str(body)])

    inputs = ["-i", str(body), "-stream_loop", "-1", "-i", str(BGM), "-i", str(VO_END)]
    fc, prev = [], "[0:v]"
    for k, (png, a, b) in enumerate(overlays):
        inputs += ["-loop", "1", "-framerate", str(FPS), "-t", f"{total:.3f}", "-i", str(png)]
        idx = 3 + k
        fc.append(f"[{idx}:v]format=rgba,fade=t=in:st={a:.3f}:d=0.1:alpha=1,"
                  f"fade=t=out:st={b - 0.1:.3f}:d=0.1:alpha=1[o{k}]")
        fc.append(f"{prev}[o{k}]overlay=0:0:enable='between(t,{a:.3f},{b:.3f})'[v{k}]")
        prev = f"[v{k}]"
    fc.append(f"{prev}format=yuv420p,setrange=tv[vout]")
    ms = int(vo_at * 1000)
    fc.append(f"[1:a]atrim=0:{total:.3f},volume={BGM_GAIN},afade=t=out:st={total - 1.0:.3f}:d=1.0[bgm]")
    fc.append(f"[2:a]aresample=48000,adelay={ms}|{ms},volume=3.0,apad=whole_dur={total:.3f}[vo]")
    fc.append("[0:a][bgm][vo]amix=inputs=3:normalize=0,alimiter=limit=0.95,loudnorm=I=-14:TP=-1.5:LRA=11[a]")
    run(["ffmpeg", "-y", "-v", "error", *inputs, "-filter_complex", ";".join(fc),
         "-map", "[vout]", "-map", "[a]", "-t", f"{total:.3f}",
         "-c:v", "libx264", "-profile:v", "high", "-level", "4.2", "-preset", "slow", "-crf", "20",
         "-maxrate", "9M", "-bufsize", "18M", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-ac", "2",
         "-movflags", "+faststart", str(OUT)])
    print(OUT, f"{total:.2f}s")


if __name__ == "__main__":
    main()
