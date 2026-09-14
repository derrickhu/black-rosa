#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""量每个 UiKit.Label 的真实字宽，看会不会挤出框。

「字显示不全」有两种：一种是字体里没这个字（build_font.py --check 管），
另一种是字排不进给它的框。第二种在离线预渲染里看不出来 —— PIL 一行画到底，
而 Unity 的 Text 是 horizontalOverflow=Wrap，超宽会折行，
verticalOverflow=Overflow 又让它往框外顶，于是压到上下相邻的元素上。

这里直接从源码里抠出每个 Label 的「文字 / 字号 / 框宽高」，用游戏自己的字体量一遍。

    .venv-mock/bin/python docs/prompt/runtime/check_text_fit.py

文字参数不是字面量时（比如 d.Desc、cost + " 墨"），按下面 FIELD_LIT 的规则
把对应目录里该字段的所有字面量都取出来，量最长的那个。实在解析不了的会列在
「未核对」里，不当成通过。
"""

import os
import pathlib
import re
import sys

from PIL import ImageFont

REPO = pathlib.Path(__file__).resolve().parents[2].parent
SCRIPTS = REPO / "Assets" / "Scripts"
FONTS = {
    "reg": REPO / "Assets" / "Resources" / "Fonts" / "Ink.ttf",
    "bold": REPO / "Assets" / "Resources" / "Fonts" / "InkBold.ttf",
}

# 表里字段的取值都是字面量，出现在 Data 目录。`d.Desc` 这种参数按字段名
# 把所有候选值取出来量最长的 —— 卡片是同一套坐标复用的，最长的能过就都能过。
FIELD_LIT = ("Name", "Desc", "Step", "LockNote")

# 这些是运行时拼出来的串，没法从源码推。给一组最坏情况的代表值。
DYNAMIC = {
    "why": ["通关解锁", "已满级", "暂未开放", "需 18 星", "差 1200 墨", "第二章开放"],
    "tail": ["通关解锁", "已满级", "点击装备", "已装备 2", "220 墨", "需 18 星"],
    "cost": ["体力 2"],
    "value": ["12/12", "1240", "18"],
}

_fc = {}


def font(kind, size):
    k = (kind, size)
    if k not in _fc:
        _fc[k] = ImageFont.truetype(str(FONTS[kind]), size)
    return _fc[k]


def split_args(src, i):
    """从左括号后的位置开始，按顶层逗号切实参，返回 (参数表, 右括号后的位置)。"""
    args, depth, cur, quote = [], 0, [], None
    n = len(src)
    while i < n:
        c = src[i]
        if quote:
            cur.append(c)
            if c == "\\":
                cur.append(src[i + 1])
                i += 2
                continue
            if c == quote:
                quote = None
            i += 1
            continue
        if c in "\"'":
            quote = c
            cur.append(c)
        elif c in "([{":
            depth += 1
            cur.append(c)
        elif c in ")]}":
            if depth == 0:
                args.append("".join(cur).strip())
                return args, i + 1
            depth -= 1
            cur.append(c)
        elif c == "," and depth == 0:
            args.append("".join(cur).strip())
            cur = []
        else:
            cur.append(c)
        i += 1
    return args, i


LIT = re.compile(r'^"((?:[^"\\]|\\.)*)"$')
# 框的两个分量可以是 `80f * k` 这种缩放写法，只取前面那个数 —— k <= 1，
# 最坏情况就是 k = 1，按原始尺寸量正好是上界。
VEC = re.compile(r"new\s+Vector2\(\s*(-?[\d.]+)f?[^,]*,\s*(-?[\d.]+)f?[^)]*\)")
NUM = re.compile(r"^-?[\d.]+f?$")


def num(s):
    s = s.strip()
    if NUM.match(s):
        return float(s.rstrip("f"))
    # Mathf.RoundToInt(34f * k) 这类，同样取上界
    m = re.match(r"^Mathf\.\w+\(\s*(-?[\d.]+)f?", s)
    return float(m.group(1)) if m else None


def field_values(field):
    """把 Data 目录里 `Field = "..."` 的所有字面量取出来。"""
    out = []
    for p in (SCRIPTS / "Data").rglob("*.cs"):
        t = p.read_text(encoding="utf-8", errors="replace")
        out += re.findall(r'\b%s\s*=\s*"((?:[^"\\]|\\.)*)"' % field, t)
    return out


_fields = {}


def resolve(expr):
    """把 Label 的文字实参解析成一组候选字符串，解析不了返回 None。"""
    expr = expr.strip()
    m = LIT.match(expr)
    if m:
        return [m.group(1)]
    # d.Name / d.Desc / skin.Name 这类
    m = re.match(r"^[\w_]+\.(%s)$" % "|".join(FIELD_LIT), expr)
    if m:
        f = m.group(1)
        if f not in _fields:
            _fields[f] = field_values(f)
        return _fields[f] or None
    # 纯变量名，落到 DYNAMIC 表里的最坏情况
    m = re.match(r"^[\w_]+$", expr)
    if m and expr in DYNAMIC:
        return DYNAMIC[expr]
    lits = re.findall(r'"((?:[^"\\]|\\.)*)"', expr)
    if not lits:
        return None
    # 三元表达式的几个分支是「二选一」，不能拼成一句去量 —— 拼起来必然超宽，
    # 一堆假警报会把真问题埋掉。有 ? 和 : 就当成候选项分别量。
    if "?" in expr and ":" in expr:
        return lits
    # 纯拼接（"已装备 " + (slot + 1)）：数字部分按两位算
    return ["".join(lits) + "00"]


def bold_after(src, pos, name_arg):
    """这条 Label 后面紧跟着 UiKit.Bold(x) 吗 —— 粗体字宽更大，要按粗体量。"""
    return bool(re.search(r"(UiKit\.)?Bold\(", src[pos:pos + 400]))


def main():
    hits, skipped = [], []
    for path in sorted(SCRIPTS.rglob("*.cs")):
        src = path.read_text(encoding="utf-8", errors="replace")
        for m in re.finditer(r"(?:UiKit\.)?\bLabel\(", src):
            args, end = split_args(src, m.end())
            if len(args) < 6:
                continue
            # Label 自己的声明行也会被匹配到，形参带类型名，跳过
            if args[0].startswith("Transform "):
                continue
            texts = resolve(args[2])
            size = num(args[3])
            box = VEC.search(args[5])
            if texts is None or size is None or not box:
                snippet = " ".join(args[:4])[:70].replace("\n", " ")
                skipped.append((path.name, snippet))
                continue
            bw, bh = float(box.group(1).rstrip("f")), float(box.group(2).rstrip("f"))
            kind = "bold" if bold_after(src, end, args[1]) else "reg"
            f = font(kind, int(round(size)))
            for t in texts:
                if not t:
                    continue
                # 只量最长的一行：源码里的 \n 是作者自己排的换行
                widest = max(f.getlength(line) for line in t.split("\\n"))
                lines = len(t.split("\\n"))
                if widest > bw - 2:
                    hits.append((path.name, t, size, kind, widest, bw, bh, lines))
    print("== 挤出框的标签 ==")
    if not hits:
        print("  无")
    for name, t, size, kind, w, bw, bh, lines in sorted(hits, key=lambda h: -(h[4] - h[5])):
        need = lines + (1 if w > bw - 2 else 0)
        print(f"  {name:<16} {size:>3.0f}{kind:<5} 框宽 {bw:>5.0f} 实宽 {w:>6.1f}"
              f" 超 {w - bw:>5.1f}  框高 {bh:>3.0f} 需约 {need * size * 1.2:>5.0f}   「{t}」")
    if skipped:
        print(f"\n== 未核对 {len(skipped)} 处（文字参数解析不了）==")
        for name, s in skipped:
            print(f"  {name:<16} {s}")
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
