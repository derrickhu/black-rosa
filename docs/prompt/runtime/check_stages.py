#!/usr/bin/env python3
"""静态核对二十关的递进曲线和敌人美术齐不齐。

编译器管不了这些：格子开放会不会中途缩回去、字池会不会突然掉一半、
新敌人有没有在「单独出场」的那一波露过脸、关底波总血量是不是单调上升。
这些错了游戏照样跑，只是曲线是坏的，所以只能靠读源码核。

    python3 docs/prompt/runtime/check_stages.py
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
SRC = os.path.join(REPO, "Assets", "Scripts")
ART = os.path.join(REPO, "Assets", "Resources", "Art")

fail = []


def read(*parts):
    with open(os.path.join(SRC, *parts), encoding="utf-8") as f:
        return f.read()


def check(ok, msg):
    print(("  ok   " if ok else "  FAIL ") + msg)
    if not ok:
        fail.append(msg)


stage_src = read("Data", "StageCatalog.cs")
enemy_src = read("Data", "EnemyCatalog.cs")
ids_src = read("Data", "GameIds.cs")
sprite_src = read("View", "InkSprites.cs")
const_src = read("Core", "GameConstants.cs")

# ---------- 关卡数 ----------
print("关卡数")
want = int(re.search(r"ChapterStageCount\s*=\s*(\d+)", const_src).group(1))
check(want >= 20, f"ChapterStageCount = {want}（要求 >= 20）")
slots = sorted(int(m) for m in re.findall(r"^\s*s\[(\d+)\]\s*=", stage_src, re.M))
check(slots == list(range(want)), f"s[0..{want - 1}] 全部赋值，实到 {len(slots)} 个")

# ---------- 逐关拆 ----------
# 按 s[i] = new StageDef(... ) 切块，块内再数 WaveDef / FinalWave
blocks = {}
# 切块只在 Build() 的关卡数据段里做。不截断的话最后一关会一路吃到文件末尾，
# 把 FinalWave 方法体（里面提到 BossTwin）也算成关 20 的内容。
body = stage_src[:stage_src.index("return s;")]
marks = [(int(m.group(1)), m.start()) for m in re.finditer(r"^\s*s\[(\d+)\]\s*=", body, re.M)]
marks.append((-1, len(body)))
for (idx, start), (_, end) in zip(marks, marks[1:]):
    blocks[idx] = body[start:end]

head = re.compile(r"new StageDef\(\s*(\d+)\s*,\s*\"([^\"]+)\"\s*,\s*new\[\]\s*\{([^}]*)\}\s*,\s*Pool\((\d+),")
rows = []
for i in range(want):
    m = head.search(blocks[i])
    if not m:
        check(False, f"关 {i + 1} 的 StageDef 头部没解析出来")
        continue
    decl_idx, name, cells, pool = int(m.group(1)), m.group(2), m.group(3), int(m.group(4))
    cells = [int(x) for x in re.findall(r"\d+", cells)]
    waves = len(re.findall(r"new WaveDef\(", blocks[i])) + len(re.findall(r"FinalWave\(", blocks[i]))
    bosses = re.findall(r"EnemyId\.(Boss\w+)", blocks[i])
    rows.append((i, decl_idx, name, cells, pool, waves, bosses))

print("\nStageDef 里的 index 和数组下标一致")
bad = [f"关 {i + 1}" for i, d, *_ in rows if i != d]
check(not bad, "全部一致" if not bad else "不一致: " + ", ".join(bad))

# ---------- 三条递进线单调不回退 ----------
print("\n递进曲线（格子 / 字池 / 波数 都不许中途缩回去）")


def cells_total(c):
    return sum(c)


for key, pick, label in [
    ("cells", lambda r: cells_total(r[3]), "开放格子数"),
    ("pool", lambda r: r[4], "字池大小"),
    ("waves", lambda r: r[5], "波数"),
]:
    seq = [pick(r) for r in rows]
    drops = [f"关 {i + 1}({seq[i - 1]}→{seq[i]})" for i in range(1, len(seq)) if seq[i] < seq[i - 1]]
    check(not drops, f"{label} 单调不减: {seq}" + ("" if not drops else "  回退于 " + ", ".join(drops)))

# 行数只能一行一行往上开，不能跳
rowcounts = [len(r[3]) for r in rows]
jumps = [f"关 {i + 1}" for i in range(1, len(rowcounts)) if rowcounts[i] - rowcounts[i - 1] > 1]
check(not jumps, f"开放行数逐行增加: {rowcounts}")

# 每行的格子数本身也不许回退
percell_bad = []
for i in range(1, len(rows)):
    prev, cur = rows[i - 1][3], rows[i][3]
    for r in range(len(prev)):
        if r < len(cur) and cur[r] < prev[r]:
            percell_bad.append(f"关 {i + 1} 第 {r + 1} 排")
check(not percell_bad, "各排格子数不回退" + ("" if not percell_bad else ": " + ", ".join(percell_bad)))

# ---------- 新手关 ----------
print("\n新手关（关 1）")
r0 = rows[0]
check(r0[3] == [2], f"只开第一排两格: {r0[3]}")
check(r0[4] == 1, f"只有一个字: 池子 {r0[4]}")
check(r0[5] <= 2, f"波数 <= 2: {r0[5]}")
check(not r0[6], "没有关底" if not r0[6] else "混进了 boss: " + str(r0[6]))

# ---------- 关底 ----------
print("\n关底")
noboss = [r[0] + 1 for r in rows if not r[6]]
check(noboss == [1, 2], f"只有前两关没关底，实为 {noboss}")
kinds = sorted(set(b for r in rows for b in r[6]))
check(len(kinds) == 8, f"八只 boss 全部用上: {len(kinds)} 种 {kinds}")

# 关底波总血量单调上升。基础血从 EnemyCatalog 抓，系数是 1 + idx*0.08
base = {}
for m in re.finditer(r"case EnemyId\.(Boss\w+):[^\n]*\n\s*return new EnemyDef\(id,\s*([\d.]+)f\s*\*\s*t", enemy_src):
    base[m.group(1)] = float(m.group(2))
check(len(base) == 8, f"从 EnemyCatalog 抓到八只 boss 的基础血: {len(base)}")

totals = []
for i, _d, _n, _c, _p, _w, bosses in rows:
    if not bosses:
        continue
    t = 1.0 + i * 0.08
    # FinalWave 只给「主 boss」是双首时自动刷两只，源码里那种情况只写一次。
    # 写在护卫位上的双首是实打实的一只，不能一律翻倍。
    main = re.search(r"FinalWave\(EnemyId\.(Boss\w+)", blocks[i])
    main = main.group(1) if main else None
    hp = 0.0
    for k, b in enumerate(bosses):
        dup = 2 if (b == "BossTwin" and b == main and k == bosses.index(main)) else 1
        hp += base.get(b, 0.0) * dup
    totals.append((i + 1, round(hp * t)))
drops = [f"关 {totals[k][0]}({totals[k - 1][1]}→{totals[k][1]})"
         for k in range(1, len(totals)) if totals[k][1] < totals[k - 1][1]]
check(not drops, "关底波总血单调上升: " + " ".join(str(v) for _, v in totals)
      + ("" if not drops else "  回退于 " + ", ".join(drops)))

# ---------- 敌人颜色的逐步放开 ----------
print("\n敌人放开顺序")
T0 = {"Walker", "Swarm", "Chubby", "Tall", "Ball", "BigHead"}
first = {}
for i, _d, _n, _c, _p, _w, _b in rows:
    for e in re.findall(r"EnemyId\.(\w+)", blocks[i]):
        if e.startswith("Boss"):
            continue
        first.setdefault(e, i + 1)
colored = {e: s for e, s in first.items() if e not in T0}
earliest = min(colored.values()) if colored else 99
check(earliest >= 7, f"第一个带颜色的常规兵不早于关 7，实为关 {earliest}"
      + " (" + ", ".join(e for e, s in colored.items() if s == earliest) + ")")
t0_used = sorted(e for e in first if e in T0)
check(len(t0_used) == 6, f"六只纯墨兵全用上: {t0_used}")
late = [f"{e}=关{s}" for e, s in sorted(colored.items(), key=lambda kv: kv[1])]
print("       带色兵首次登场: " + ", ".join(late))

# 一关最多引入一只新常规兵
multi = []
for i in range(want):
    n = [e for e, s in first.items() if s == i + 1]
    if len(n) > 1 and i > 1:
        multi.append(f"关 {i + 1}: {n}")
check(not multi, "关 3 起每关最多引入一只新兵" + ("" if not multi else "\n         " + "\n         ".join(multi)))

# ---------- 美术 ----------
print("\n美术")
enum_body = re.search(r"enum EnemyId\s*\{(.*?)\}", ids_src, re.S).group(1)
all_ids = [x for x in re.findall(r"^\s*(\w+),?\s*$", enum_body, re.M)]
mapped = set(re.findall(r"case EnemyId\.(\w+): return Load\(", sprite_src))
missing = [e for e in all_ids if e not in mapped and e != "Walker"]
check(not missing, f"{len(all_ids)} 个 EnemyId 都接了图" + ("" if not missing else ": 缺 " + str(missing)))

files = dict(re.findall(r'case EnemyId\.(\w+): return Load\("([^"]+)"\)', sprite_src))
files["Walker"] = "walker"
nopng = [f"{e}->{f}.png" for e, f in files.items() if not os.path.exists(os.path.join(ART, f + ".png"))]
check(not nopng, f"{len(files)} 张贴图都在盘上" + ("" if not nopng else ": 缺 " + str(nopng)))

nometa = [f for f in files.values() if not os.path.exists(os.path.join(ART, f + ".png.meta"))]
check(not nometa, "meta 齐全" if not nometa else f"待 Unity 导入: {sorted(nometa)}")

# 敌人图必须可读 —— InkSprites.Flash 要 GetPixels() 烘白色剪影
unreadable = []
for f in files.values():
    mp = os.path.join(ART, f + ".png.meta")
    if os.path.exists(mp) and "isReadable: 1" not in open(mp, encoding="utf-8").read():
        unreadable.append(f)
check(not unreadable, "全部 isReadable: 1（Flash 要 GetPixels）"
      if not unreadable else f"关掉了可读: {unreadable}")

kb = sum(os.path.getsize(os.path.join(ART, f + ".png")) for f in files.values()
         if os.path.exists(os.path.join(ART, f + ".png"))) // 1024
print(f"       {len(files)} 张敌人图共 {kb}KB")

# ---------- 明细表 ----------
print("\n明细")
print("  关   名字            开放格      字池  波数  关底")
for i, _d, name, cells, pool, waves, bosses in rows:
    short = name.split("·")[-1].strip()
    b = "+".join(b.replace("Boss", "") for b in bosses) or "—"
    print(f"  {i + 1:>2}   {short:<12}  {str(cells):<11} {pool:>3}  {waves:>3}   {b}")

print()
if fail:
    print(f"有 {len(fail)} 项没过")
    sys.exit(1)
print("全部通过")
