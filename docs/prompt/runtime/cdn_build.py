"""CdnArt/ 高清源 -> 云存储上的带 hash 成品 + 包内缩略图 + C# 对照表。

    .venv-mock/bin/python docs/prompt/runtime/cdn_build.py

加新大图：把原图按逻辑名放进 CdnArt/（如 CdnArt/Ui/chapter_9.png），跑一遍本脚本，
再把 Library/CdnOut/StreamingAssets 整个上传到云存储 black-rosa/StreamingAssets。
代码里照旧用逻辑名（"Ui/chapter_9"），走 CdnAssets 即可。

- 成品名 <名字>_<md5>.<扩展名>：内容变了 URL 就变，旧文件不删，老版本客户端照样能拉。
- 路径带 StreamingAssets，微信 SDK 会把下载结果缓存到本地（见 unity-namespace.js isCacheableFile）。
- 图片同时在 Assets/Resources/Art/<名字>.png 写一张 256 缩略图，没网或还没下完时先顶上。
"""
import hashlib
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
SRC = ROOT / "CdnArt"
OUT = ROOT / "Library" / "CdnOut" / "StreamingAssets"
THUMB_DIR = ROOT / "Assets" / "Resources" / "Art"
MANIFEST = ROOT / "Assets" / "Scripts" / "Platform" / "CdnManifest.cs"
BASE_URL = "https://726f-rosa-env-d7grf78r5dbd37323-1414200063.tcb.qcloud.la/black-rosa/StreamingAssets/"
THUMB = 256

# (逻辑名前缀, 成品格式, 最长边)。按顺序取第一条匹配的。
RULES = [
    ("Ui/chapter_", "png", 1024),
    ("Bg/", "jpg", 1280),
    ("", "png", 1024),
]


def rule(name):
    for prefix, fmt, size in RULES:
        if name.startswith(prefix):
            return fmt, size
    raise ValueError(name)


def fit(img, size):
    w, h = img.size
    k = min(1.0, size / max(w, h))
    if k >= 1.0:
        return img
    return img.resize((round(w * k), round(h * k)), Image.LANCZOS)


def encode_image(src, name, tmp):
    fmt, size = rule(name)
    img = fit(Image.open(src), size)
    if fmt == "jpg":
        img.convert("RGB").save(tmp, "JPEG", quality=82, optimize=True, progressive=True)
        return ".jpg"
    img.convert("RGBA").save(tmp, "PNG", optimize=True)
    q = subprocess.run(["pngquant", "--quality=65-90", "--speed=1", "--force", "--output", str(tmp), str(tmp)])
    if q.returncode not in (0, 99):
        raise RuntimeError(f"pngquant failed on {src}")
    return ".png"


def write_thumb(src, name):
    dst = THUMB_DIR / (name + ".png")
    img = fit(Image.open(src), THUMB)
    img = img.convert("RGBA") if "A" in img.getbands() else img.convert("RGB")
    dst.parent.mkdir(parents=True, exist_ok=True)
    img.save(dst, "PNG", optimize=True)


def main():
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True)
    entries = []
    for src in sorted(p for p in SRC.rglob("*") if p.is_file() and not p.name.startswith(".")):
        rel = src.relative_to(SRC)
        name = rel.with_suffix("").as_posix()
        tmp = OUT / ("_tmp" + src.suffix)
        if src.suffix.lower() in (".png", ".jpg", ".jpeg"):
            ext = encode_image(src, name, tmp)
            write_thumb(src, name)
            kind = "Image"
        else:
            shutil.copyfile(src, tmp)
            ext = src.suffix.lower()
            kind = "Audio"
        digest = hashlib.md5(tmp.read_bytes()).hexdigest()
        url = f"{rel.parent.as_posix()}/{rel.stem}_{digest}{ext}"
        dst = OUT / url
        dst.parent.mkdir(parents=True, exist_ok=True)
        tmp.replace(dst)
        entries.append((name, url, rel.as_posix(), kind, dst.stat().st_size))

    lines = [
        "// 由 docs/prompt/runtime/cdn_build.py 生成，别手改。",
        "using System.Collections.Generic;",
        "",
        "namespace InkLine",
        "{",
        "    public static class CdnManifest",
        "    {",
        f"        public const string BaseUrl = \"{BASE_URL}\";",
        "",
        "        // 逻辑名 -> (云上相对路径, CdnArt 下的源文件)",
        "        public static readonly Dictionary<string, (string url, string src)> Files =",
        "            new Dictionary<string, (string, string)>",
        "            {",
    ]
    for name, url, src, _, _ in entries:
        lines.append(f"                {{ \"{name}\", (\"{url}\", \"{src}\") }},")
    lines += ["            };", "    }", "}", ""]
    MANIFEST.write_text("\n".join(lines), encoding="utf-8")

    total = 0
    for name, url, _, kind, size in entries:
        total += size
        print(f"{kind:5s} {size / 1024:7.1f} KB  {name}  ->  {url}")
    print(f"{len(entries)} files, {total / 1024 / 1024:.2f} MB -> {OUT}")


if __name__ == "__main__":
    sys.exit(main())
