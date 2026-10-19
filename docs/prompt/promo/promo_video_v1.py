"""墨字防线 · 买量竖版视频 v1（实机录屏剪辑）。

素材来自 PROMO_CAPTURE 录屏包（Assets/Scripts/Promo），每条录像是一个目录：
f00000.jpg… 逐帧 1080x1920 @30，audio.wav 同步音效，events.csv 逐帧局面。
字幕用真实字体叠，不让模型画字。成品写到 game_assets/black-rosa/推广/宣传视频/。

    .venv-mock/bin/python docs/prompt/promo/promo_video_v1.py
"""
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

FPS = 30
W, H = 1080, 1920
TAKES = {
    "e": Path("/tmp/black-rosa-promo2/take_e"),   # 第四章关底，满级 + 10 倍金币
    "b": Path("/tmp/black-rosa-promo/take_b"),    # 第五章关底，3 倍金币，怪多
}
ASSETS = Path("/Users/rosa/rosa_games/game_assets/black-rosa")
OUT_DIR = ASSETS / "推广/宣传视频"
WORK = Path("/tmp/black-rosa-promo-cut")
REPO = Path(__file__).resolve().parents[3]
BGM = REPO / "CdnArt/Audio/bgm_battle.mp3"
HAND = REPO / "Assets/Resources/Art/Ui/guide_hand.png"
LOGO = ASSETS / "laoding/logo.png"
FONT_HEAD = "/Users/rosa/rosa_games/srpg-rosa/tools/font-src/SmileySans-Oblique.ttf"
FONT_BODY = str(ASSETS / "字体/NotoSansSC[wght].ttf")

# (录像, 起始秒, 时长, 主标题, 飘带副标题)
CUTS = [
    ("e", 132.6, 3.0, "汉字当炮弹", "一炮清一屏！"),
    ("e", 33.4, 4.6, "抽个字 装上炮", "炮弹立刻变样"),
    ("e", 38.0, 1.4, "万+箭=万箭齐发", ""),
    ("e", 83.0, 4.5, "箭雨铺满全屏！", ""),
    ("b", 114.0, 5.0, "清一波 射速翻倍", "怪越多越解压"),
    ("b", 136.5, 3.5, "道具自动放", "满屏开花"),
    ("e", 104.5, 3.5, "连杀停不下来！", ""),
]
END_SECS = 3.6

# 解说：(成片里开口的秒数, 台词)。每句要在下一句开口前说完，脚本会检查。
VOICE = "badao_shaoye"
VO_SPEED = 1.2
VO = [
    (0.15, "汉字当炮弹，一炮清一屏！"),
    (3.2, "抽一个字，装上炮，子弹立刻变样！"),
    (7.7, "万加箭，万箭齐发！箭雨直接铺满全屏！"),
    (13.6, "清掉一波，射速翻倍，怪越多越解压！"),
    (18.6, "道具自动放，满屏开花！"),
    (22.1, "连杀根本停不下来！"),
    (25.7, "墨字防线，微信搜一下，马上开玩！"),
]
VO_DIR = OUT_DIR / "vo_04"
OUT_NAME = "gameplay_promo_04_shuang_vo_1080x1920.mp4"
VIDU_AUDIO = Path.home() / ".cursor/skills/vidu-video-gen/scripts/vidu_audio.py"
BGM_GAIN = 0.42
# 录屏音效约 -8.6 LUFS、TTS 约 -24 LUFS，不先拉平人声会被炮声盖住
SFX_GAIN = 0.3
VO_GAIN = 3.2
# 录屏帧是 JPEG 全色域，不转会封成 yuvj420p，部分播放器偏色
TV_RANGE = "scale=in_range=pc:out_range=tv,format=yuv420p"


def run(cmd):
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        sys.stderr.write(r.stderr[-3000:])
        raise SystemExit(f"failed: {' '.join(map(str, cmd[:6]))}")


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


def gradient_text(text, fnt, top, bottom, stroke, stroke_w, tilt=0.0):
    """金币黄到橙的竖向渐变字，深巧克力描边，外加一圈投影。"""
    pad = stroke_w * 3
    box = fnt.getbbox(text, stroke_width=stroke_w)
    tw, th = box[2] - box[0] + pad * 2, box[3] - box[1] + pad * 2
    org = (pad - box[0], pad - box[1])

    shadow = Image.new("L", (tw, th), 0)
    ImageDraw.Draw(shadow).text((org[0] + 6, org[1] + 10), text, font=fnt, fill=255, stroke_width=stroke_w)
    shadow = shadow.filter(ImageFilter.GaussianBlur(5))

    outline = Image.new("L", (tw, th), 0)
    ImageDraw.Draw(outline).text(org, text, font=fnt, fill=255, stroke_width=stroke_w)
    fill_mask = Image.new("L", (tw, th), 0)
    ImageDraw.Draw(fill_mask).text(org, text, font=fnt, fill=255)

    grad = Image.new("RGBA", (tw, th))
    gd = ImageDraw.Draw(grad)
    y0, y1 = org[1] + box[1], org[1] + box[3]
    for y in range(th):
        u = min(1.0, max(0.0, (y - y0) / max(1, y1 - y0)))
        c = tuple(int(top[i] + (bottom[i] - top[i]) * u) for i in range(3))
        gd.line([(0, y), (tw, y)], fill=c + (255,))

    img = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    img.paste((0, 0, 0, 140), (0, 0), shadow)
    img.paste(stroke + (255,), (0, 0), outline)
    img.paste(grad, (0, 0), fill_mask)
    # 字顶一道白高光，像糖衣
    hi = Image.new("L", (tw, th), 0)
    ImageDraw.Draw(hi).text((org[0], org[1] - 3), text, font=fnt, fill=110)
    hi = Image.composite(hi, Image.new("L", (tw, th), 0), fill_mask)
    cut = Image.new("L", (tw, th), 0)
    ImageDraw.Draw(cut).rectangle([0, 0, tw, y0 + (y1 - y0) * 0.38], fill=255)
    hi = Image.composite(hi, Image.new("L", (tw, th), 0), cut)
    img.paste((255, 255, 255, 255), (0, 0), hi)
    if tilt:
        img = img.rotate(tilt, resample=Image.BICUBIC, expand=True)
    return img


def ribbon(text, fnt):
    """红包那种红的飘带，白字。"""
    box = fnt.getbbox(text)
    tw, th = box[2] - box[0], box[3] - box[1]
    w, h = tw + 120, th + 44
    img = Image.new("RGBA", (w + 40, h + 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    tail = 26
    body = [(20 + tail, 6), (20 + w - tail, 6), (20 + w, 6 + h // 2), (20 + w - tail, 6 + h),
            (20 + tail, 6 + h), (20, 6 + h // 2)]
    d.polygon([(x + 4, y + 7) for x, y in body], fill=(60, 20, 10, 120))
    d.polygon(body, fill=(214, 38, 40, 255), outline=(92, 20, 14, 255), width=5)
    d.text((20 + (w - tw) // 2 - box[0], 6 + (h - th) // 2 - box[1]), text, font=fnt,
           fill=(255, 250, 236, 255), stroke_width=2, stroke_fill=(120, 18, 16, 255))
    return img


def caption_png(head, sub, path):
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    size = 150 if len(head) <= 5 else 124 if len(head) <= 7 else 110
    t = gradient_text(head, font(FONT_HEAD, size), (255, 236, 92), (255, 146, 30), (58, 30, 16), 12, tilt=3.0)
    # 顶栏下沿约 y=120；游戏自己的连杀大字落在 450–900，字幕要压在它上面
    y = 128
    canvas.alpha_composite(t, ((W - t.width) // 2, y))
    if sub:
        r = ribbon(sub, font(FONT_BODY, 56, 900))
        canvas.alpha_composite(r, ((W - r.width) // 2, y + t.height - 18))
    canvas.save(path)


def end_card(bg_frame, path):
    bg = Image.open(bg_frame).convert("RGB").resize((W, H))
    bg = bg.filter(ImageFilter.GaussianBlur(18))
    shade = Image.new("RGB", (W, H), (34, 20, 12))
    bg = Image.blend(bg, shade, 0.45).convert("RGBA")

    logo = Image.open(LOGO).convert("RGBA")
    lw = 940
    logo = logo.resize((lw, int(logo.height * lw / logo.width)), Image.LANCZOS)
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([W // 2 - 560, 520 - 300, W // 2 + 560, 520 + 300], fill=(255, 214, 120, 120))
    bg.alpha_composite(glow.filter(ImageFilter.GaussianBlur(80)))
    bg.alpha_composite(logo, ((W - lw) // 2, 520 - logo.height // 2))

    tag = gradient_text("汉字当炮弹 越打越上头", font(FONT_HEAD, 84), (255, 244, 200), (255, 206, 110),
                        (58, 30, 16), 8)
    bg.alpha_composite(tag, ((W - tag.width) // 2, 820))

    # 立即开玩：游戏里那种橙色主按钮
    bw, bh, by = 620, 170, 1240
    btn = Image.new("RGBA", (bw + 30, bh + 40), (0, 0, 0, 0))
    d = ImageDraw.Draw(btn)
    d.rounded_rectangle([15, 26, 15 + bw, 26 + bh], radius=bh // 2, fill=(120, 52, 14, 255))
    d.rounded_rectangle([15, 10, 15 + bw, 10 + bh], radius=bh // 2, fill=(255, 140, 30, 255),
                        outline=(92, 40, 12, 255), width=7)
    d.rounded_rectangle([45, 22, bw - 15, 10 + bh * 0.45], radius=bh // 4, fill=(255, 190, 90, 255))
    ft = font(FONT_BODY, 84, 900)
    txt = "立即开玩"
    b = ft.getbbox(txt)
    d.text((15 + (bw - (b[2] - b[0])) // 2 - b[0], 10 + (bh - (b[3] - b[1])) // 2 - b[1]), txt, font=ft,
           fill=(255, 255, 255, 255), stroke_width=4, stroke_fill=(140, 58, 10, 255))
    bg.alpha_composite(btn, ((W - btn.width) // 2, by))

    sub = font(FONT_BODY, 40, 700)
    s = "微信小游戏 · 搜「墨字防线」"
    b = sub.getbbox(s)
    ImageDraw.Draw(bg).text(((W - (b[2] - b[0])) // 2 - b[0], 1700), s, font=sub, fill=(255, 240, 214, 255),
                            stroke_width=3, stroke_fill=(50, 26, 12, 255))
    bg.convert("RGB").save(path, quality=95)
    return by


def voice_lines(total):
    """逐句 TTS（已生成的不重复扣积分），去掉首尾静音，返回 [(wav, 开口秒, 时长)]。"""
    VO_DIR.mkdir(parents=True, exist_ok=True)
    lines = []
    for i, (at, text) in enumerate(VO):
        mp3 = VO_DIR / f"line{i}_{VOICE}.mp3"
        if not mp3.exists():
            run([sys.executable, str(VIDU_AUDIO), "tts", "--voice", VOICE, "--emotion", "happy",
                 "--speed", str(VO_SPEED), "--text", text, "--out", str(mp3)])
        wav = WORK / f"vo{i}.wav"
        run(["ffmpeg", "-y", "-v", "error", "-i", str(mp3),
             "-af", "silenceremove=start_periods=1:start_threshold=-45dB,areverse,"
                    "silenceremove=start_periods=1:start_threshold=-45dB,areverse,"
                    "aresample=48000",
             "-ac", "2", str(wav)])
        dur = float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                                    "-of", "csv=p=0", str(wav)], capture_output=True, text=True).stdout)
        lines.append((wav, at, dur))
    for k, (_, at, dur) in enumerate(lines):
        nxt = lines[k + 1][1] if k + 1 < len(lines) else total
        flag = "" if at + dur <= nxt else "  <-- 超时，删字或调 VO_SPEED"
        print(f"vo{k} {at:5.2f}→{at + dur:5.2f}s（下句 {nxt:5.2f}）{VO[k][1]}{flag}")
    return lines


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    seg_files = []
    t = 0.0
    caps = []
    for i, (take, start, dur, head, sub) in enumerate(CUTS):
        src = TAKES[take]
        f0 = int(round(start * FPS))
        n = int(round(dur * FPS))
        seg = WORK / f"seg{i}.mp4"
        run(["ffmpeg", "-y", "-v", "error",
             "-start_number", str(f0), "-framerate", str(FPS), "-i", str(src / "f%05d.jpg"),
             "-ss", f"{f0 / FPS:.4f}", "-t", f"{n / FPS:.4f}", "-i", str(src / "audio.wav"),
             "-frames:v", str(n), "-vf", TV_RANGE,
             "-af", f"afade=t=in:d=0.04,afade=t=out:st={n / FPS - 0.05:.3f}:d=0.05,aresample=48000",
             "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-pix_fmt", "yuv420p", "-r", str(FPS),
             "-c:a", "pcm_s16le", "-ac", "2", str(seg)])
        seg_files.append(seg)
        png = WORK / f"cap{i}.png"
        caption_png(head, sub, png)
        caps.append((png, t, t + n / FPS))
        t += n / FPS

    last_take, last_start, last_dur = CUTS[-1][0], CUTS[-1][1], CUTS[-1][2]
    bg_frame = TAKES[last_take] / f"f{int(round((last_start + last_dur) * FPS)) - 1:05d}.jpg"
    end_png = WORK / "end.jpg"
    btn_y = end_card(bg_frame, end_png)
    end_seg = WORK / "end.mp4"
    nend = int(END_SECS * FPS)
    hand = Image.open(HAND).convert("RGBA")
    hand = hand.resize((150, int(hand.height * 150 / hand.width)), Image.LANCZOS)
    hand.save(WORK / "hand.png")
    # 手指在按钮上点两下
    hx, hy = W // 2 + 215, btn_y + 105
    run(["ffmpeg", "-y", "-v", "error",
         "-loop", "1", "-framerate", str(FPS), "-i", str(end_png),
         "-loop", "1", "-framerate", str(FPS), "-i", str(WORK / "hand.png"),
         "-f", "lavfi", "-t", f"{END_SECS}", "-i", "anullsrc=r=48000:cl=stereo",
         "-filter_complex",
         f"[0:v]scale=w='{W}*(1+0.025*abs(sin(t*5.2)))':h=-2:eval=frame,crop={W}:{H}[bg];"
         f"[bg][1:v]overlay=x={hx}:y='{hy}+18*abs(sin(t*5.2))',{TV_RANGE}[v]",
         "-map", "[v]", "-map", "2:a", "-frames:v", str(nend),
         "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "pcm_s16le", str(end_seg)])
    seg_files.append(end_seg)
    total = t + END_SECS

    lst = WORK / "list.txt"
    lst.write_text("".join(f"file '{p}'\n" for p in seg_files))
    body = WORK / "body.mov"
    run(["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", str(lst),
         "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "pcm_s16le", str(body)])

    inputs = ["-i", str(body), "-stream_loop", "-1", "-i", str(BGM)]
    fc = []
    prev = "[0:v]"
    for k, (png, a, b) in enumerate(caps):
        inputs += ["-loop", "1", "-framerate", str(FPS), "-t", f"{total:.3f}", "-i", str(png)]
        idx = 2 + k
        fc.append(f"[{idx}:v]format=rgba,fade=t=in:st={a:.3f}:d=0.12:alpha=1,"
                  f"fade=t=out:st={b - 0.12:.3f}:d=0.12:alpha=1[c{k}]")
        nxt = f"[v{k}]"
        fc.append(f"{prev}[c{k}]overlay=0:0:enable='between(t,{a:.3f},{b:.3f})'{nxt}")
        prev = nxt
    fc.append(f"{prev}format=yuv420p,setrange=tv[vout]")
    prev = "[vout]"
    fc.append(f"[1:a]atrim=0:{total:.3f},volume={BGM_GAIN},"
              f"afade=t=out:st={total - 1.2:.3f}:d=1.2[bgm]")
    vo = voice_lines(total)
    base = 2 + len(caps)
    for k, (wav, at, _) in enumerate(vo):
        inputs += ["-i", str(wav)]
        ms = int(at * 1000)
        fc.append(f"[{base + k}:a]adelay={ms}|{ms}[vo{k}]")
    fc.append("".join(f"[vo{k}]" for k in range(len(vo))) +
              f"amix=inputs={len(vo)}:normalize=0,apad=whole_dur={total:.3f},"
              f"highpass=f=90,volume={VO_GAIN},acompressor=threshold=-14dB:ratio=3:attack=5:release=80,"
              "asplit=2[vox][key]")
    fc.append(f"[0:a]volume={SFX_GAIN}[sfx]")
    fc.append("[sfx][bgm]amix=inputs=2:normalize=0[bed]")
    # 人声一开口，音效和 BGM 自动让开
    fc.append("[bed][key]sidechaincompress=threshold=0.03:ratio=8:attack=15:release=350:makeup=1[duck]")
    fc.append("[duck][vox]amix=inputs=2:normalize=0,alimiter=limit=0.95,"
              "loudnorm=I=-14:TP=-1.5:LRA=11[a]")
    out = OUT_DIR / OUT_NAME
    run(["ffmpeg", "-y", "-v", "error", *inputs, "-filter_complex", ";".join(fc),
         "-map", prev, "-map", "[a]", "-t", f"{total:.3f}",
         "-c:v", "libx264", "-profile:v", "high", "-level", "4.2", "-preset", "slow", "-crf", "20",
         "-maxrate", "9M", "-bufsize", "18M",
         "-pix_fmt", "yuv420p", "-r", str(FPS),
         "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-ac", "2",
         "-movflags", "+faststart", str(out)])
    print(out)


if __name__ == "__main__":
    main()
