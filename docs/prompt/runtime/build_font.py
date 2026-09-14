#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""字体子集化 / 覆盖核对。

背景：原来的 Assets/Resources/Fonts/Ink.ttf 是 Heiti TC Light 的手工子集，
只有 287 个码位。编辑器里缺的字会回落到 macOS 系统字体，所以本地看不出问题；
打进微信小游戏没有回落，缺的字直接渲染成空白 —— 真机上「炮弹伤害 +8%」显示
成「炮弹 +8%」，读起来像是故意这么写的，比报错还难发现。而且 Heiti TC 是苹果
系统字体（华文/SinoType 版权），商用发布本身就有授权风险。

现在换 Noto Sans SC（OFL 1.1，免费商用、允许改造和子集化），按工程里实际用到
的字表切两个字重出来。

    python3 docs/prompt/runtime/build_font.py --build   # 重建两个字体
    python3 docs/prompt/runtime/build_font.py --check   # 只核对覆盖，缺字则退出码 1

--check 挂在 csharp_check.sh 里跑。以后往 UI 里加中文，只要忘了重建字体，
这一步就会把缺的字和重建命令一起打出来。
"""

import argparse
import pathlib
import subprocess
import sys
import unicodedata

REPO = pathlib.Path(__file__).resolve().parents[2].parent
SCRIPTS = REPO / "Assets" / "Scripts"
OUT_DIR = REPO / "Assets" / "Resources" / "Fonts"

# 母版字体放仓库外 —— 17MB 的可变字体不进游戏仓库。脚本会按需下载。
SRC_DIR = REPO.parent / "game_assets" / "black-rosa" / "字体"
SRC_FONT = SRC_DIR / "NotoSansSC[wght].ttf"
SRC_URL = "https://github.com/google/fonts/raw/main/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf"
LICENSE_URL = "https://github.com/google/fonts/raw/main/ofl/notosanssc/OFL.txt"

# 两个字重。Unity 的合成粗体是把字形往四周抹一圈，汉字在 20px 上会糊成一团，
# 所以粗体要用真的 Bold 字重，别让引擎自己造。
WEIGHTS = [("Ink", 400), ("InkBold", 700)]

# 全角标点 / 数学符号。现在不一定全用上，但这类字符最容易在改文案时悄悄冒出来，
# 一起切进去一共才几百字节。
EXTRAS = "，。！？：；、…—·「」『』（）《》【】％×÷≤≥°　"


def cjk(ch):
    """要不要把这个字符算进字表。ASCII 单独加，这里只管非 ASCII 的可见字符。"""
    if ord(ch) < 0x80:
        return False
    return unicodedata.category(ch) not in ("Cc", "Cf", "Cs", "Co", "Cn", "Zl", "Zp")


def scan_strings(src):
    """把一个 .cs 文件里所有字符串字面量的内容拼出来。

    不能拿正则硬扫全文的中文 —— 这个工程的注释基本都是中文，扫进来字表要大四五倍。
    也不能先正则删注释再扫：字符串里出现 `//`（比如 URL）会把后半截连着中文一起删掉。
    所以老老实实按状态机走一遍，只在字符串状态里收字。
    """
    out = []
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        if c == "/" and i + 1 < n and src[i + 1] == "/":
            i = src.find("\n", i)
            if i < 0:
                break
        elif c == "/" and i + 1 < n and src[i + 1] == "*":
            i = src.find("*/", i + 2)
            if i < 0:
                break
            i += 2
        elif c == "'":
            i += 1
            while i < n and src[i] != "'":
                i += 2 if src[i] == "\\" else 1
            i += 1
        elif c == "@" and i + 1 < n and src[i + 1] == '"':
            # 逐字字符串：内部用 "" 表示一个引号，反斜杠不转义
            i += 2
            while i < n:
                if src[i] == '"':
                    if i + 1 < n and src[i + 1] == '"':
                        out.append('"')
                        i += 2
                        continue
                    i += 1
                    break
                out.append(src[i])
                i += 1
        elif c == '"':
            # 普通字符串，含内插 $"..."（内插洞里是表达式，收进去的中文也只会是字面量）
            i += 1
            while i < n and src[i] != '"':
                if src[i] == "\\":
                    i += 2
                    continue
                if src[i] == "\n":
                    break
                out.append(src[i])
                i += 1
            i += 1
        else:
            i += 1
    return "".join(out)


def charset():
    """工程实际会显示的字符集，外加 ASCII 可见字符和常用标点。"""
    used = {}
    for path in sorted(SCRIPTS.rglob("*.cs")):
        for ch in scan_strings(path.read_text(encoding="utf-8", errors="replace")):
            if cjk(ch):
                used.setdefault(ch, set()).add(path.name)
    for ch in EXTRAS:
        used.setdefault(ch, set()).add("(标点兜底)")
    chars = set(used) | {chr(c) for c in range(0x20, 0x7F)}
    return chars, used


def coverage(path):
    from fontTools.ttLib import TTFont

    font = TTFont(str(path), fontNumber=0, lazy=True)
    have = set()
    for table in font["cmap"].tables:
        have |= set(table.cmap)
    font.close()
    return have


def fetch_source():
    SRC_DIR.mkdir(parents=True, exist_ok=True)
    if not SRC_FONT.exists():
        print(f"下载母版字体 -> {SRC_FONT}")
        subprocess.run(["curl", "-sfL", "-o", str(SRC_FONT), SRC_URL], check=True)
    lic = SRC_DIR / "OFL.txt"
    if not lic.exists():
        subprocess.run(["curl", "-sfL", "-o", str(lic), LICENSE_URL], check=True)
    return lic


def build():
    from fontTools import subset
    from fontTools.ttLib import TTFont
    from fontTools.varLib import instancer

    lic = fetch_source()
    chars, _ = charset()
    text = "".join(sorted(chars))
    print(f"字表 {len(chars)} 个字符（含 ASCII 可见字符 95 个）")

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for name, wght in WEIGHTS:
        # 先切子集再定字重。反过来的话 instancer 要处理六万多个字形，慢得没必要。
        opts = subset.Options()
        opts.drop_tables += ["BASE", "DSIG", "vhea", "vmtx", "VORG"]
        opts.recalc_bounds = True
        # .notdef 一定要留轮廓。缺字渲染成空白，是这次真机 bug 最难查的地方 ——
        # 「炮弹伤害 +8%」掉成「炮弹 +8%」读起来完全通顺。留个豆腐块才看得见。
        opts.notdef_outline = True
        opts.name_IDs = ["*"]
        opts.name_legacy = True

        font = TTFont(str(SRC_FONT), fontNumber=0)
        subsetter = subset.Subsetter(options=opts)
        subsetter.populate(text=text)
        subsetter.subset(font)

        instancer.instantiateVariableFont(font, {"wght": wght}, inplace=True, updateFontNames=False)

        # 改内部名，免得在 Unity 字体列表里跟系统装的 Noto 撞名。
        # Noto 没有保留字体名（Reserved Font Name），OFL 下改名是允许的。
        family = "Ink Sans SC"
        style = "Bold" if wght >= 700 else "Regular"
        # 16/17 是「排版族名」，Unity 的 TrueTypeFontImporter 读的是这一对；
        # 只改 1/2/4/6 的话导入设置里还会写着 Noto Sans SC。
        names = {1: family, 2: style, 4: f"{family} {style}",
                 6: f"{family.replace(' ', '')}-{style}", 16: family, 17: style}
        for rec in font["name"].names:
            if rec.nameID in names:
                rec.string = names[rec.nameID]

        out = OUT_DIR / f"{name}.ttf"
        font.save(str(out))
        font.close()
        print(f"  {out.relative_to(REPO)}  {out.stat().st_size / 1024:.1f} KB  wght={wght}")

    # OFL 1.1 要求「每份拷贝都带着版权声明和许可证」。字体自己的 name 表里
    # nameID 0/13/14 已经带了声明、许可证摘要和链接（subset 时 name_IDs 全留），
    # 所以打进包里的那份是合规的。这份全文放仓库根的 LICENSES/，
    # 不进 Resources —— Resources 是整包塞进小游戏的，别放没有代码引用的文件。
    #
    # 另外：Noto Sans SC 源自思源黑体，原声明里 'Source' 是保留字体名，
    # 所以子集必须改名（这里改成 Ink Sans SC），不能沿用 Noto/Source 字样。
    lic_out = REPO / "LICENSES" / "Noto-Sans-SC-OFL.txt"
    lic_out.parent.mkdir(parents=True, exist_ok=True)
    lic_out.write_text(
        "Assets/Resources/Fonts/ 下的 Ink.ttf / InkBold.ttf 是 Noto Sans SC 的子集，\n"
        "按工程实际用到的字表切出，字重分别为 400 / 700。\n"
        "原字体 Noto Sans SC，SIL Open Font License 1.1，许可证全文见下。\n"
        "重建：python3 docs/prompt/runtime/build_font.py --build\n\n"
        + lic.read_text(encoding="utf-8"),
        encoding="utf-8",
    )
    check()


def check():
    chars, used = charset()
    bad = False
    for name, _ in WEIGHTS:
        path = OUT_DIR / f"{name}.ttf"
        if not path.exists():
            print(f"字体缺失: {path.relative_to(REPO)}")
            bad = True
            continue
        have = coverage(path)
        lack = sorted(c for c in chars if ord(c) not in have)
        if lack:
            bad = True
            print(f"{name}.ttf 缺 {len(lack)} 个字: {''.join(lack)}")
            for ch in lack[:12]:
                print(f"    {ch}  来自 {'、'.join(sorted(used.get(ch, {'?'})))}")
        else:
            print(f"{name}.ttf 覆盖 {len(chars)} 个字符，无缺")
    if bad:
        print("\n重建字体： python3 docs/prompt/runtime/build_font.py --build")
        return 1
    return 0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--build", action="store_true")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--dump", action="store_true", help="打印字表")
    a = ap.parse_args()
    if a.dump:
        chars, _ = charset()
        print("".join(sorted(c for c in chars if ord(c) >= 0x80)))
        return 0
    if a.build:
        build()
        return 0
    return check()


if __name__ == "__main__":
    sys.exit(main())
