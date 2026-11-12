"""墨字防线 · 真人短剧买量片 v4「城墙上的保安」剪辑。

六个 Vidu 镜头（drama_04/clips）+ 一小段实机万箭 + 尾板。
播放中只叠底部台词字幕，不叠钩子大字、飘带和说明字。

    .venv-mock/bin/python docs/prompt/promo/drama_cut_v4.py
"""
import subprocess
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from promo_video_v1 import HAND, TV_RANGE, W, H, FPS, end_card, run  # noqa: E402
from drama_cut_v1 import SPEAKER, line_png  # noqa: E402

ASSETS = Path("/Users/rosa/rosa_games/game_assets/black-rosa")
VIDEO = ASSETS / "推广/宣传视频"
CLIPS = VIDEO / "drama_04/clips"
OUT = VIDEO / "drama_promo_04_wall_1080x1920.mp4"
WORK = Path("/tmp/black-rosa-drama-cut-v4")
REPO = Path(__file__).resolve().parents[3]
BGM = REPO / "CdnArt/Audio/bgm_battle.mp3"
VO_END = VIDEO / "vo_04/line6_badao_shaoye.mp3"
GAME_SRC = VIDEO / "gameplay_promo_04_shuang_1080x1920.mp4"
GAME_START = 9.6
GAME_SECS = 1.8
# 实机片顶上压着标题大字，从这一行往下裁，按 9:16 放大铺满。
GAME_TOP = 380
END_SECS = 2.2
BGM_GAIN = 0.2

SPEAKER.update({"王总": (255, 150, 120), "小川": (150, 220, 255)})

# (镜头, 入点, 出点, [(字幕起, 止, 说话人, 台词)])；字幕时间按镜头内的秒算。
SHOTS = [
    ("s1_mock", 0.0, 2.4, [(0.0, 1.3, "王总", "景区摆设炮，")]),
    ("s1_mock", 3.4, 4.8, [(3.6, 4.8, "王总", "守什么守！")]),
    ("s2_write", 0.0, 3.4, [(0.0, 1.2, "小川", "那就换个字。")]),
    ("s3_confuse", 0.8, 4.0, []),
    ("s4_arrows", 1.2, 4.6, []),
    ("s5_kill", 0.8, 5.0, []),
    ("s6_kneel", 0.0, 4.0, [(0.0, 2.6, "王总", "你……秒杀了它？"), (2.6, 4.0, "小川", "字面意思。")]),
]


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    segs, overlays = [], []
    t = 0.0
    for i, (name, a, b, lines) in enumerate(SHOTS):
        seg = WORK / f"seg{i}.mov"
        dur = b - a
        run(["ffmpeg", "-y", "-v", "error", "-ss", f"{a:.3f}", "-t", f"{dur:.3f}", "-i", str(CLIPS / f"{name}.mp4"),
             "-vf", f"scale={W}:{H}:flags=lanczos,fps={FPS},format=yuv420p",
             "-af", f"aresample=48000,afade=t=in:d=0.03,afade=t=out:st={dur - 0.05:.3f}:d=0.05",
             "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-c:a", "pcm_s16le", "-ac", "2", str(seg)])
        segs.append(seg)
        for k, (s0, s1, who, text) in enumerate(lines):
            png = WORK / f"line{i}_{k}.png"
            line_png(who, text, png)
            overlays.append((png, t + max(s0, a) - a, t + min(s1, b) - a))
        t += dur

    game = WORK / "game.mov"
    ch = 1920 - GAME_TOP
    cw = ch * 9 // 16 // 2 * 2
    run(["ffmpeg", "-y", "-v", "error", "-ss", f"{GAME_START:.3f}", "-t", f"{GAME_SECS:.3f}", "-i", str(GAME_SRC),
         "-f", "lavfi", "-t", f"{GAME_SECS:.3f}", "-i", "anullsrc=r=48000:cl=stereo",
         "-map", "0:v", "-map", "1:a",
         "-vf", f"crop={cw}:{ch}:{(1080 - cw) // 2}:{GAME_TOP},scale={W}:{H}:flags=lanczos,fps={FPS},format=yuv420p",
         "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-c:a", "pcm_s16le", "-ac", "2", str(game)])
    segs.append(game)
    vo_at = t
    t += GAME_SECS

    last = WORK / "game_last.jpg"
    run(["ffmpeg", "-y", "-v", "error", "-sseof", "-0.1", "-i", str(game), "-frames:v", "1", str(last)])
    end_jpg = WORK / "end.jpg"
    btn_y = end_card(last, end_jpg)
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
        fc.append(f"[{3 + k}:v]format=rgba,fade=t=in:st={a:.3f}:d=0.08:alpha=1,"
                  f"fade=t=out:st={b - 0.08:.3f}:d=0.08:alpha=1[o{k}]")
        fc.append(f"{prev}[o{k}]overlay=0:0:enable='between(t,{a:.3f},{b:.3f})'[v{k}]")
        prev = f"[v{k}]"
    fc.append(f"{prev}format=yuv420p,setrange=tv[vout]")
    ms = int(vo_at * 1000)
    fc.append(f"[1:a]atrim=0:{total:.3f},volume={BGM_GAIN},afade=t=out:st={total - 0.8:.3f}:d=0.8[bgm]")
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
