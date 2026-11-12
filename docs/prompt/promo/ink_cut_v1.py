"""墨字防线 · 水墨动态漫买量片「一字一炮」剪辑。

九个 Vidu 镜头（ink_01/clips，只带音效）+ 旁白 TTS + 一小段实机 + 尾板。
播放中只在底部放旁白字幕，不叠钩子大字和说明字。

    .venv-mock/bin/python docs/prompt/promo/ink_cut_v1.py
"""
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from promo_video_v1 import FONT_BODY, HAND, TV_RANGE, W, H, FPS, end_card, font, run  # noqa: E402

ASSETS = Path("/Users/rosa/rosa_games/game_assets/black-rosa")
VIDEO = ASSETS / "推广/宣传视频"
INK = VIDEO / "ink_01"
CLIPS = INK / "clips"
OUT = VIDEO / "ink_promo_01_glyph_1080x1920.mp4"
WORK = Path("/tmp/black-rosa-ink-cut-v1")
REPO = Path(__file__).resolve().parents[3]
BGM = REPO / "CdnArt/Audio/bgm_endless.mp3"
GAME_SRC = VIDEO / "gameplay_promo_05_motide_vo_1080x1920.mp4"
GAME_START = 4.2
GAME_SECS = 1.8
GAME_TOP = 480
END_SECS = 2.2
BGM_GAIN = 0.2
SFX_GAIN = 0.8

# (镜头, 入点, 出点)
SHOTS = [
    ("h1_hook", 0.0, 3.6),
    ("w_fire", 0.8, 2.6),
    ("f_fire", 0.8, 4.0),
    ("w_ice", 0.8, 2.6),
    ("i_ice", 0.8, 4.0),
    ("w_thunder", 0.8, 2.6),
    ("t_thunder", 1.0, 4.0),
    ("c_combo", 0.4, 4.0),
]
# (挂在第几个镜头, 镜头内起点, 音频, 字幕)；字幕时长跟音频走。
VO = [
    (0, 0.1, INK / "vo/q.mp3", "只能写一个字守城，你选哪个？"),
    (7, 0.2, INK / "vo/all2.mp3", "小孩子才做选择，我全都要！"),
    (8, 0.0, VIDEO / "vo_04/line6_badao_shaoye.mp3", ""),
]


def sub_png(text, path):
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    size = 72
    while True:
        f = font(FONT_BODY, size, 900)
        b = d.textbbox((0, 0), text, font=f, stroke_width=7)
        if b[2] - b[0] <= W - 120:
            break
        size -= 2
    d.text(((W - (b[2] - b[0])) // 2 - b[0], 1500 - b[1]), text, font=f,
           fill=(255, 255, 255, 255), stroke_width=7, stroke_fill=(28, 18, 12, 255))
    canvas.save(path)


def dur_of(p):
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(p)],
                         capture_output=True, text=True).stdout
    return float(out.strip())


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    segs, starts = [], []
    t = 0.0
    for i, (name, a, b) in enumerate(SHOTS):
        seg = WORK / f"seg{i}.mov"
        dur = b - a
        run(["ffmpeg", "-y", "-v", "error", "-ss", f"{a:.3f}", "-t", f"{dur:.3f}", "-i", str(CLIPS / f"{name}.mp4"),
             "-vf", f"scale={W}:{H}:flags=lanczos,fps={FPS},format=yuv420p",
             "-af", f"aresample=48000,volume={SFX_GAIN},afade=t=in:d=0.03,afade=t=out:st={dur - 0.05:.3f}:d=0.05",
             "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-c:a", "pcm_s16le", "-ac", "2", str(seg)])
        segs.append(seg)
        starts.append(t)
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
    starts.append(t)
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
    starts.append(t)
    total = t + END_SECS

    lst = WORK / "list.txt"
    lst.write_text("".join(f"file '{p}'\n" for p in segs))
    body = WORK / "body.mov"
    run(["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", str(lst),
         "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "pcm_s16le", str(body)])

    inputs = ["-i", str(body), "-stream_loop", "-1", "-i", str(BGM)]
    fc, mix = [], ["[0:a]"]
    fc.append(f"[1:a]atrim=0:{total:.3f},volume={BGM_GAIN},afade=t=out:st={total - 0.8:.3f}:d=0.8[bgm]")
    mix.append("[bgm]")
    subs = []
    for k, (seg_i, at, audio, text) in enumerate(VO):
        start = starts[seg_i] + at
        inputs += ["-i", str(audio)]
        ms = int(start * 1000)
        fc.append(f"[{2 + k}:a]aresample=48000,adelay={ms}|{ms},volume=3.0,apad=whole_dur={total:.3f}[vo{k}]")
        mix.append(f"[vo{k}]")
        if text:
            png = WORK / f"sub{k}.png"
            sub_png(text, png)
            subs.append((png, start, min(total, start + dur_of(audio))))
    prev = "[0:v]"
    base = 2 + len(VO)
    for k, (png, a, b) in enumerate(subs):
        inputs += ["-loop", "1", "-framerate", str(FPS), "-t", f"{total:.3f}", "-i", str(png)]
        fc.append(f"[{base + k}:v]format=rgba,fade=t=in:st={a:.3f}:d=0.08:alpha=1,"
                  f"fade=t=out:st={b - 0.08:.3f}:d=0.08:alpha=1[o{k}]")
        fc.append(f"{prev}[o{k}]overlay=0:0:enable='between(t,{a:.3f},{b:.3f})'[v{k}]")
        prev = f"[v{k}]"
    fc.append(f"{prev}format=yuv420p,setrange=tv[vout]")
    fc.append("".join(mix) + f"amix=inputs={len(mix)}:normalize=0,alimiter=limit=0.95,"
              "loudnorm=I=-14:TP=-1.5:LRA=11[a]")
    run(["ffmpeg", "-y", "-v", "error", *inputs, "-filter_complex", ";".join(fc),
         "-map", "[vout]", "-map", "[a]", "-t", f"{total:.3f}",
         "-c:v", "libx264", "-profile:v", "high", "-level", "4.2", "-preset", "slow", "-crf", "20",
         "-maxrate", "9M", "-bufsize", "18M", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-ac", "2",
         "-movflags", "+faststart", str(OUT)])
    print(OUT, f"{total:.2f}s")


if __name__ == "__main__":
    main()
