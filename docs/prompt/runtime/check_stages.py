#!/usr/bin/env python3
"""静态核对八章 72 关的递进曲线和敌人美术齐不齐。

编译器管不了这些：格子开放会不会中途缩回去、字有没有发重、成词的两个字是不是同关给、
每关有没有新东西、章底 boss 血量是不是逐章上升。这些错了游戏照样跑，只是曲线是坏的，
所以只能靠读源码核。

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


core_src = read("Data", "StageCatalog.cs")
enemy_src = read("Data", "EnemyCatalog.cs")
ids_src = read("Data", "GameIds.cs")
sprite_src = read("View", "InkSprites.cs")
const_src = read("Core", "GameConstants.cs")

size = int(re.search(r"ChapterSize\s*=\s*(\d+)", const_src).group(1))
chapters = int(re.search(r"Chapters\s*=\s*(\d+)", const_src).group(1))
want = size * chapters
hp_c = [float(x) for x in re.findall(r"[\d.]+", re.search(r"ChapterHp\s*=\s*\{([^}]*)\}", core_src).group(1))]
slot_step = float(re.search(r"SlotHpStep\s*=\s*([\d.]+)f", core_src).group(1))
finale_mul = float(re.search(r"FinaleHp\s*=\s*([\d.]+)f", core_src).group(1))


def hp_of(ch, slot):
    return hp_c[min(ch, len(hp_c) - 1)] * (1 + slot_step * slot) * (finale_mul if slot == size - 1 else 1)


# ---------- 逐关拆 ----------
rows = []
for ch in range(chapters):
    src = read("Data", f"StageChapter{ch + 1}.cs")
    starts = [m.start() for m in re.finditer(r"s\.Add\(P\(", src)]
    starts.append(len(src))
    for k in range(len(starts) - 1):
        b = src[starts[k]:starts[k + 1]]
        title = re.search(r'P\("([^"]+)"', b).group(1)
        m = re.search(r'P\("[^"]+",\s*G\(([^)]*)\)', b)
        mask = re.findall(r'"([^"]*)"', m.group(1)) if m else []
        give = re.search(r"\.Give\(([^)]*)\)", b)
        give = [x.strip() for x in give.group(1).split(",")] if give else []
        waves = len(re.findall(r"\bW\(", b)) + len(re.findall(r"\bBoss\(", b))
        main = re.search(r"\bBoss\((Boss\w+)", b)
        main = main.group(1) if main else None
        bosses = re.findall(r"\b(Boss[A-Z]\w+)\b", b)
        enemies = [e for e in re.findall(r"S\([\d.]+f,\s*(\w+)", b) if not e.startswith("Boss")]
        rows.append(dict(i=len(rows), ch=ch, slot=k, title=title, mask=mask, give=give,
                         waves=waves, main=main, bosses=bosses, enemies=enemies))

print("关卡数")
check(len(rows) == want, f"{chapters} 章 × {size} 关 = {want}，实到 {len(rows)}")
for ch in range(chapters):
    n = sum(1 for r in rows if r["ch"] == ch)
    if n != size:
        check(False, f"第 {ch + 1} 章 {n} 关")


def open_cells(r):
    return {(c, y) for y, line in enumerate(r["mask"]) for c, ch in enumerate(line) if ch == "X"}


def tag(r):
    return f"{r['ch'] + 1}-{r['slot'] + 1}"


# ---------- 格子 ----------
# 格子到第八章章底才第一次开满；每章有自己的格数区间和最高排数，
# 章内不要求逐关变多，但相邻两关的形状必须不同，同样的字才会换一种摆法。
print("\n格子（逐章慢开，第八章章底才开满）")
bad_mask = [tag(r) for r in rows
            if not 1 <= len(r["mask"]) <= 3 or any(len(x) != 6 or set(x) - {"X", "."} for x in r["mask"])
            or "X" not in r["mask"][-1]]
check(not bad_mask, "形状都是 1~3 行 × 6 列、最上一排有格子" + ("" if not bad_mask else ": " + ", ".join(bad_mask)))
cell_range = {0: (2, 7), 1: (8, 9), 2: (10, 12), 3: (11, 13), 4: (12, 14), 5: (13, 15), 6: (14, 16), 7: (15, 18)}
row_cap = {0: 2, 1: 2, 2: 2}
for ch in range(chapters):
    part = [r for r in rows if r["ch"] == ch]
    lo, hi = cell_range.get(ch, (1, 18))
    got = [len(open_cells(r)) for r in part]
    check(all(lo <= n <= hi for n in got), f"第 {ch + 1} 章格数在 {lo}~{hi}: {got}")
    cap = row_cap.get(ch, 3)
    tall = [tag(r) for r in part if len(r["mask"]) > cap]
    if tall:
        check(False, f"第 {ch + 1} 章最多开 {cap} 排: {tall}")
full = [tag(r) for r in rows if len(open_cells(r)) == 18]
check(full == [tag(rows[-1])], "只有最后一关三排全开" + ("" if full == [tag(rows[-1])] else f": {full}"))
avg = [sum(len(open_cells(r)) for r in rows if r["ch"] == ch) / size for ch in range(chapters)]
back = [f"第 {k + 1} 章" for k in range(1, chapters) if avg[k] <= avg[k - 1]]
check(not back, "章均格数逐章上升: " + " / ".join(f"{x:.1f}" for x in avg))
same = [tag(rows[k]) for k in range(1, len(rows)) if rows[k]["mask"] == rows[k - 1]["mask"] and not rows[k]["bosses"]]
check(not same, "相邻两关形状不同（关底除外）" + ("" if not same else ": " + ", ".join(same)))

# ---------- 字 ----------
print("\n字")
card_body = re.search(r"enum CardId\s*\{(.*?)\}", ids_src, re.S).group(1)
all_cards = [x for x in re.findall(r"^\s*(\w+),?\s*$", card_body, re.M) if x != "None"]
given = [c for r in rows for c in r["give"]]
dup = sorted({c for c in given if given.count(c) > 1})
check(not dup, "每个字只发一次" + ("" if not dup else ": 重复 " + str(dup)))
missing = [c for c in all_cards if c not in given]
check(not missing, f"{len(all_cards)} 个字全部进池" + ("" if not missing else ": 缺 " + str(missing)))
pairs = [("Sec", "Kill"), ("Myriad", "Arrow"), ("Strike", "Back"), ("Link", "Slash")]
split = [f"{a}{b}" for a, b in pairs
         if any((a in r["give"]) != (b in r["give"]) for r in rows)]
check(not split, "成词的两个字同关给" + ("" if not split else ": " + str(split)))
multi = [f"{r['ch'] + 1}-{r['slot'] + 1}" for r in rows
         if len(r["give"]) > 2 or (len(r["give"]) == 2 and tuple(r["give"]) not in pairs)]
check(not multi, "一关最多给一个字（成词对算一个）")
dry = [f"第 {ch + 1} 章" for ch in range(chapters) if not any(r["give"] for r in rows if r["ch"] == ch)]
check(not dry, "每章都有新字或新词" + ("" if not dry else ": " + ", ".join(dry)))

# ---------- 关底 ----------
print("\n关底")
order = ["BossDrum", "BossInkbag", "BossIron", "BossTwin", "BossWarden", "BossThunder", "BossMedic", "BossKing"]
finales = [r for r in rows if r["slot"] == size - 1]
check([r["main"] for r in finales] == order[:chapters],
      "章底按顺序是 " + " / ".join(o.replace("Boss", "") for o in order[:chapters]))
minis = [r for r in rows if r["slot"] == 4 and r["ch"] > 0]
mini_ok = [r["main"] == order[r["ch"] - 1] for r in minis]
check(all(mini_ok), "每章第 5 关重打上一章的章底")
early = [f"{r['ch'] + 1}-{r['slot'] + 1}" for r in rows[:size - 1] if r["bosses"]]
check(not early, "第一章前 8 关没有 boss")

base = {}
for m in re.finditer(r"case EnemyId\.(Boss\w+):[^\n]*\n\s*return new EnemyDef\(id,\s*([\d.]+)f,", enemy_src):
    base[m.group(1)] = float(m.group(2))
check(len(base) == 8, f"从 EnemyCatalog 抓到八只 boss 的基础血: {len(base)}")


def boss_hp(r):
    total = 0.0
    for b in r["bosses"]:
        total += base.get(b, 0.0) * (2 if b == "BossTwin" and b == r["main"] else 1)
    return round(total * hp_of(r["ch"], r["slot"]))


fin_hp = [boss_hp(r) for r in finales]
drops = [f"第 {k + 1} 章" for k in range(1, len(fin_hp)) if fin_hp[k] <= fin_hp[k - 1]]
check(not drops, "章底 boss 总血逐章上升: " + " / ".join(map(str, fin_hp)))
mini_hp = [boss_hp(r) for r in minis]
drops = [f"第 {minis[k]['ch'] + 1} 章" for k in range(1, len(mini_hp)) if mini_hp[k] <= mini_hp[k - 1]]
check(not drops, "小关底 boss 总血逐章上升: " + " / ".join(map(str, mini_hp)))
soft = [f"第 {r['ch'] + 1} 章" for r in minis if boss_hp(r) > fin_hp[r["ch"]]]
check(not soft, "小关底比本章章底软")

# ---------- 敌人 ----------
print("\n敌人放开顺序")
plan = {
    0: {"Walker", "Chubby", "Tall", "Ball", "BigHead"},
    1: {"Swarm", "Crawler", "Belt", "Runner", "Strafer"},
    2: {"Shield", "Splitter", "Sprinter"},
    3: {"Mender", "Bulwark"},
    4: {"Elite"},
    5: {"Warden"},
}
first = {}
for r in rows:
    for e in r["enemies"]:
        first.setdefault(e, r)
for ch, names in plan.items():
    got = {e for e, r in first.items() if r["ch"] == ch}
    check(got == names, f"第 {ch + 1} 章新兵 {sorted(got)}" + ("" if got == names else f"，应为 {sorted(names)}"))
late = {e for e, r in first.items() if r["ch"] > 5}
check(not late, "第七章起不再引入新兵")

# ---------- 新鲜感 ----------
print("\n每关都有新东西（新字 / 新兵 / 格子变化 / 关底）")
bland = []
prev_mask = None
for r in rows:
    new_enemy = any(first[e] is r for e in set(r["enemies"]))
    fresh = r["give"] or new_enemy or r["bosses"] or r["mask"] != prev_mask
    if not fresh:
        bland.append(f"{tag(r)} {r['title']}")
    prev_mask = r["mask"]
check(not bland, "没有平淡关" + ("" if not bland else ": " + ", ".join(bland)))
# 第一章 6 关里有 5 关单排：同一屏怪太多，玩家还没摸清字就被淹了。
ch1_pure = {"Walker", "Chubby", "Tall", "Ball", "BigHead"}
odd = sorted({e for r in rows if r["ch"] == 0 for e in r["enemies"]} - ch1_pure)
check(not odd, "第一章只出纯墨兵" + ("" if not odd else f": {odd}"))

waves = [r["waves"] for r in rows]
check(min(waves[2:]) >= 3, f"第三关起每关至少 3 波（最少 {min(waves[2:])}）")

# ---------- 美术 ----------
print("\n美术")
enum_body = re.search(r"enum EnemyId\s*\{(.*?)\}", ids_src, re.S).group(1)
all_ids = [x for x in re.findall(r"^\s*(\w+),?\s*$", enum_body, re.M)]
mapped = set(re.findall(r"case EnemyId\.(\w+): return \"", sprite_src))
missing = [e for e in all_ids if e not in mapped and e != "Walker"]
check(not missing, f"{len(all_ids)} 个 EnemyId 都接了图" + ("" if not missing else ": 缺 " + str(missing)))

files = dict(re.findall(r'case EnemyId\.(\w+): return "([^"]+)"', sprite_src))
# 活动换皮：EventCast 每只都要有 evt_ 前缀的图。
evt_src = read("Data", "StageEvent.cs")
cast = re.search(r"EventCast = \{([^}]*)\}", evt_src).group(1).replace(" ", "").split(",")
evt_miss = [c for c in cast if not os.path.exists(os.path.join(ART, "evt_" + (files.get(c) or "walker") + ".png"))]
check(not evt_miss, f"活动 {len(cast)} 只换皮都有图" + ("" if not evt_miss else ": 缺 " + str(evt_miss)))
files["Walker"] = "walker"
nopng = [f"{e}->{f}.png" for e, f in files.items() if not os.path.exists(os.path.join(ART, f + ".png"))]
check(not nopng, f"{len(files)} 张贴图都在盘上" + ("" if not nopng else ": 缺 " + str(nopng)))

unreadable = []
for f in files.values():
    mp = os.path.join(ART, f + ".png.meta")
    if os.path.exists(mp) and "isReadable: 1" not in open(mp, encoding="utf-8").read():
        unreadable.append(f)
check(not unreadable, "全部 isReadable: 1（Flash 要 GetPixels）"
      if not unreadable else f"关掉了可读: {unreadable}")

# ---------- 经济 ----------
# 照 StageCatalog.Price 重算一遍：掉落是怪自己的固定属性，按 Density 铺开后每只掉满，
# 章节系数 DropMul 和丰年/疾行倍率分两次套、每次保底 1；宝箱按掉率算期望。
# 抽牌费用由「开放格数 × 目标抽牌数」反推，墨价从 72 关首通总收入推。
print("\n经济（金币 / 改装 / 墨）")
drop_of = {m.group(1): (int(m.group(2)), float(m.group(3))) for m in re.finditer(
    r"case EnemyId\.(\w+):[^\n]*\n\s*return new EnemyDef\(id,\s*[\d.]+f,\s*[\d.]+f,\s*(\d+),\s*([\d.]+)f", enemy_src)}
drop_of.setdefault("Walker", (2, 0.28))
hp_base = {m.group(1): float(m.group(2)) for m in re.finditer(
    r"case EnemyId\.(\w+):[^\n]*\n\s*return new EnemyDef\(id,\s*([\d.]+)f,", enemy_src)}
hp_base.setdefault("Walker", 4.5)
drop_pow = float(re.search(r"DropMul\(float t\) => Mathf\.Pow\(.+,\s*([\d.]+)f\)", enemy_src).group(1))
wave_span = [float(x) for x in re.findall(
    r"[\d.]+", re.search(r"ChapterWaveSpan\s*=\s*\{([^}]*)\}", core_src).group(1))]
ramp_open = float(re.search(r"RampOpen\s*=\s*([\d.]+)f", core_src).group(1))
ramp_late = float(re.search(r"RampLate\s*=\s*([\d.]+)f", core_src).group(1))
ramp_pow = float(re.search(r"RampPow\s*=\s*([\d.]+)f", core_src).group(1))
chapter_bodies = [float(x) for x in re.findall(
    r"[\d.]+", re.search(r"ChapterBodies\s*=\s*\{([^}]*)\}", core_src).group(1))]
chapter_ink = [float(x) for x in re.findall(
    r"[\d.]+", re.search(r"ChapterInk\s*=\s*\{([^}]*)\}", core_src).group(1))]
picks_per_cell = float(re.search(r" DraftPicksPerCell\s*=\s*([\d.]+)f", core_src).group(1))
first_picks_per_cell = float(re.search(r"FirstChapterPicksPerCell\s*=\s*([\d.]+)f", core_src).group(1))
second_picks_per_cell = float(re.search(r"SecondChapterPicksPerCell\s*=\s*([\d.]+)f", core_src).group(1))
budget_share = float(re.search(r"DraftBudgetShare\s*=\s*([\d.]+)f", core_src).group(1))


def drop(v, mul):
    return 0 if v <= 0 else max(1, round(v * mul))
dens_body = re.search(r"static int Density\(EnemyId id\)(.*?)\n        \}", enemy_src, re.S).group(1)
density = {}
pending = []
for line in dens_body.splitlines():
    c = re.search(r"case EnemyId\.(\w+):", line)
    if c:
        pending.append(c.group(1))
    r_ = re.search(r"return (\d+);", line)
    if r_ and pending:
        for e in pending:
            density[e] = int(r_.group(1))
        pending = []
chest_chance = float(re.search(r"ChestChance\s*=\s*([\d.]+)f", core_src).group(1))
chest_gold_share = float(re.search(r"ChestGoldShare\s*=\s*([\d.]+)f", core_src).group(1))
first_cost = int(re.search(r"FirstDraftCost\s*=\s*(\d+)", const_src).group(1))
start_gold = 6


def call_args(src, at):
    """从 '名字(' 的左括号开始，配对括号取出整段实参。"""
    depth, i = 0, at
    while i < len(src):
        if src[i] == "(":
            depth += 1
        elif src[i] == ")":
            depth -= 1
            if depth == 0:
                return src[at + 1:i], i + 1
        i += 1
    return "", len(src)


def waves_of(r):
    """逐波拆，并复刻 StageCatalog.Pace/Tile 和 RampCounts。

    平铺会把出场表整体后移一轮再追加一遍。RampCounts 再把只数按时间
    从疏排到密，总只数不变。怪量和波长都要跟着算，不然经济和时长对不上。
    """
    src = read("Data", f"StageChapter{r['ch'] + 1}.cs")
    starts = [m.start() for m in re.finditer(r"s\.Add\(P\(", src)] + [len(src)]
    b = src[starts[r["slot"]]:starts[r["slot"] + 1]]
    span = wave_span[min(r["ch"], len(wave_span) - 1)]

    out, i = [], 0
    while True:
        m = re.compile(r"\b(W|Boss)\(").search(b, i)
        if not m:
            break
        body, i = call_args(b, m.end() - 1)
        specs = [(float(t), e, int(n) if n else 1) for t, e, _, n in
                 re.findall(r"S\(([\d.]+)f,\s*(\w+)(?:,\s*(-?\d+))?(?:,\s*(\d+))?\)", body)]
        if m.group(1) == "Boss":
            boss = re.match(r"\s*(Boss\w+)", body).group(1)
            n_boss = 2 if boss == "BossTwin" else 1
            last = max([t for t, _, _ in specs], default=0.0)
            ev = [(0.3, boss, n_boss)] + [(t, e, n) for t, e, n in specs]
            out.append(dict(boss=True, dur=max(20.0, last + 10.0), specs=ev))
            continue
        if not specs:
            continue
        last = max(t for t, _, _ in specs)
        step_t = last + 1.5
        rounds = max(1, round(span / step_t)) if step_t > 0.1 else 1
        end = last + (rounds - 1) * step_t
        tiled = [(t + r * step_t, e, n) for r in range(rounds) for t, e, n in specs]
        out.append(dict(boss=False, dur=max(span, end + 5.5), specs=tiled))
    return ramp_bodies(out, chapter_bodies[min(r["ch"], len(chapter_bodies) - 1)])


def ramp_weight(u):
    u = min(1.0, max(0.0, u))
    return ramp_open + (ramp_late - ramp_open) * (u ** ramp_pow)


def ramp_bodies(waves, body_mul=1.0):
    """复刻 StageCatalog.RampCounts：只数按时间加权后再归一，关底本人不动。"""
    import math
    total = sum(max(0.01, w["dur"]) for w in waves)
    ev = []
    cursor = 0.0
    for w in waves:
        dur = max(0.01, w["dur"])
        for t, e, n in w["specs"]:
            boss = e.startswith("Boss")
            count = n * density.get(e, 1)
            u = min(1.0, max(0.0, (cursor + min(dur, max(0.0, t))) / total)) if total > 0.01 else 1.0
            ev.append(dict(t=cursor + t, e=e, raw=count, boss=boss, w=ramp_weight(u)))
        cursor += dur
    raw_sum = sum(e["raw"] for e in ev if not e["boss"])
    w_sum = sum(e["raw"] * e["w"] for e in ev if not e["boss"])
    raw_sum = max(1, round(raw_sum * body_mul))
    scale = raw_sum / w_sum if w_sum > 0.01 else 1.0
    final, frac, got = [], [], 0
    for e in ev:
        if e["boss"]:
            final.append(e["raw"])
            frac.append(-1.0)
            continue
        exact = e["raw"] * e["w"] * scale
        base = math.floor(exact)
        if e["raw"] >= 1 and base < 1:
            base = 1
        final.append(base)
        frac.append(exact - math.floor(exact))
        got += base
    diff = raw_sum - got
    order = sorted(range(len(ev)), key=lambda i: (frac[i], i), reverse=True)
    for i in order:
        if diff <= 0:
            break
        if ev[i]["boss"]:
            continue
        final[i] += 1
        diff -= 1
    if diff > 0:
        for i in range(len(ev) - 1, -1, -1):
            if ev[i]["boss"]:
                continue
            final[i] += diff
            break
    while diff < 0:
        best = next((i for i in range(len(ev) - 1, -1, -1) if not ev[i]["boss"] and final[i] > 1), None)
        if best is None:
            break
        final[best] -= 1
        diff += 1
    p = 0
    out = []
    for w in waves:
        specs = []
        for _ in w["specs"]:
            e = ev[p]
            specs.append((e["t"], e["e"], final[p], e["boss"]))
            p += 1
        out.append(dict(boss=w["boss"], dur=w["dur"], specs=specs))
    return out


def econ(r):
    t = hp_of(r["ch"], r["slot"])
    dmul = max(0.1, t) ** drop_pow
    waves = waves_of(r)
    dur_total = sum(w["dur"] for w in waves)
    gold = kills = 0
    ink = hp_total = 0.0
    open1 = early = late = 0
    for w in waves:
        for t_abs, e, count, boss in w["specs"]:
            g, ink_one = drop_of.get(e, (2, 1))
            gold += count * drop(g, dmul)
            ink += count * ink_one * dmul * chapter_ink[min(r["ch"], len(chapter_ink) - 1)]
            hp_total += count * hp_base.get(e, 4.5) * t
            if boss:
                continue
            kills += count
            if t_abs <= 1.2:
                open1 += count
            if t_abs <= dur_total * 0.25:
                early += count
            if dur_total * 0.55 <= t_abs <= dur_total * 0.85:
                late += count
    cg = max(3, round(gold * 0.05))
    ci = max(2, round(ink * 0.15))
    chests = kills * chest_chance + (1 if r["bosses"] else 0)
    purse_all = gold + round(chests * chest_gold_share * cg)

    open_n = max(1, len(open_cells(r)))
    if r["ch"] == 0:
        n_want = max(3, round(open_n * first_picks_per_cell))
    elif r["ch"] == 1:
        n_want = max(2, round(open_n * second_picks_per_cell))
    else:
        n_want = min(max(round(open_n * picks_per_cell), open_n + 2), open_n * 3)
    budget = (purse_all + start_gold) * budget_share
    first_d = max(first_cost, round(budget / n_want * 0.5))
    step = max(0.0, 2 * (budget - n_want * first_d) / (n_want * (n_want - 1))) if n_want > 1 else 0.0
    # 数一数这局的钱实际买得起几次抽牌。
    n_d, spent = 0, 0.0
    while spent + round(first_d + n_d * step) <= budget:
        spent += round(first_d + n_d * step)
        n_d += 1
    ink_all = ink + chests * (1 - chest_gold_share) * ci
    return dict(gold=gold, purse=purse_all, first=first_d, step=step, drafts=n_d, ink=ink_all,
                chests=chests, open=open_n, want=n_want, hp=hp_total, dur=dur_total,
                open1=open1, early=early, late=late, kills=kills)


eco = [econ(r) for r in rows]
# 抽牌起价现在是由「钱袋 ÷ 目标抽牌数」反推的，不再是一条自己写死的曲线，
# 所以只看它别掉到下限以下、且后期确实比前期贵，逐关的小起伏是内容差异，正常。
ch_first = [sum(e["first"] for e, r in zip(eco, rows) if r["ch"] == ch) / size for ch in range(chapters)]
check(min(e["first"] for e in eco) >= first_cost, f"抽牌起价都不低于下限 {first_cost}")
check(ch_first[-1] > ch_first[0] * 2, "抽牌起价末章明显高于首章: "
      + " / ".join(f"{x:.1f}" for x in ch_first))
ink_total = round(sum(e["ink"] for e in eco))

# 开局疏、后段密。总只数没变，所以钱和血还在；变的是它们挤在什么时候。
flat = [f"{r['ch'] + 1}-{r['slot'] + 1}({e['early']}>={e['late']})"
        for e, r in zip(eco, rows) if e["early"] >= e["late"]]
check(not flat, "每关前 25% 时间的怪少于 55%~85% 那一段" + ("" if not flat else ": " + ", ".join(flat)))
crowd = [f"{tag(r)}({e['kills']}/{e['open']})" for e, r in zip(eco, rows)
         if r["ch"] == 0 and e["kills"] > e["open"] * 5]
check(not crowd, "第一章每格不超过 5 只杂兵" + ("" if not crowd else ": " + ", ".join(crowd)))
print("  第一章杂兵 / 格数: " + "  ".join(f"{e['kills']}/{e['open']}" for e, r in zip(eco, rows) if r["ch"] == 0))
print(f"  开局 1 秒出怪（全关合计）{sum(e['open1'] for e in eco)}，"
      f"前 25% {sum(e['early'] for e in eco)}，中后段 {sum(e['late'] for e in eco)}")

# 单局成长：一局的钱至少要够把开放格子铺满，第三章起还要够把大半格子顶到二三星。
# 铺不满，玩家一局里就永远看不到盘面长成型，也就没有「变强」那一下。
# 前两章故意不铺满：一局短，铺满要五六秒弹一次三选一。
thin = [f"{r['ch'] + 1}-{r['slot'] + 1}({e['drafts']}<{e['open']})"
        for e, r in zip(eco, rows) if r["ch"] >= 2 and e["drafts"] < e["open"]]
check(not thin, "第三章起每关的钱都够铺满棋盘" + ("" if not thin else ": " + ", ".join(thin)))
off = [f"{r['ch'] + 1}-{r['slot'] + 1}({e['drafts']}/{e['open']})"
       for e, r in zip(eco, rows) if r["ch"] >= 2 and not 1.5 <= e["drafts"] / e["open"] <= 2.2]
check(not off, "第三章起单局抽牌数落在开放格数的 1.5~2.2 倍" + ("" if not off else ": " + ", ".join(off)))
ch_draft = [sum(e["drafts"] for e, r in zip(eco, rows) if r["ch"] == ch) / size for ch in range(chapters)]
print("  单局抽牌（章均）: " + " / ".join(f"{x:.1f}" for x in ch_draft))

# 章间收入跨度：掉落跟着怪走之后整关收入是波次表加出来的和，没有公式兜底，
# 手写波次很容易把经济带漂，所以这里盯住它逐章上升、总跨度不失控。
ch_ink = [sum(e["ink"] for e, r in zip(eco, rows) if r["ch"] == ch) for ch in range(chapters)]
# 留 5% 容差：相邻两章差个一两个百分点是编队差异，差出一成以上才是内容缺口。
back = [f"第 {k + 1} 章" for k in range(1, chapters) if ch_ink[k] < ch_ink[k - 1] * 0.95]
check(not back, "章墨收入逐章不明显下滑: " + " / ".join(f"{x:.0f}" for x in ch_ink)
      + ("" if not back else "  回退于 " + ", ".join(back)))
ink_span = ch_ink[-1] / max(1, ch_ink[0])
check(8.0 <= ink_span <= 15.0,
      f"末章 / 首章墨收入 = {ink_span:.2f}（要 8~15：末章一局约长两倍半也难得多，"
      f"收益该高出一截，但高过 15 倍前期就白刷了）")

# 战力裕度：需 DPS 是「整关总血 ÷ 波次总时长」，满配 DPS 按当时能买到的锻造等级算。
# 单位任意，看的是比值的走势 —— 裕度应当逐章缓降，不能像原来那样从 5.6 塌到 2.9。
def forged_at(cleared):
    mul = 1.0
    emit = 2
    interval = 1.0
    for m in re.finditer(r'Name = "([^"]+)", Stat[^\n]*Amount = ([\d.]+)f[^\n]*\n[^\n]*\n\s*'
                         r'Cost = new\[\] \{[^}]*\},\s*Gate = new\[\] \{([^}]*)\}', forge_src):
        name, amount = m.group(1), float(m.group(2))
        lv = sum(1 for g in re.findall(r"\d+", m.group(3)) if int(g) <= cleared)
        if name == "伤害":
            mul = 1 + amount * 0.01 * lv
        elif name == "炮台数":
            emit = min(2 + int(amount) * lv, max_emitters)
        elif name == "射速":
            interval = max(0.2, 1 - amount * 0.01 * lv)
    # 暴击是赤焰皮肤的专属词条，不是人人都有，裕度按通用词条算。
    return emit / interval * mul


forge_src = read("Data", "ForgeCatalog.cs")
max_emitters = int(re.search(r"MaxEmitters\s*=\s*(\d+)", const_src).group(1))
head = []
for ch in range(chapters):
    part = [e for e, r in zip(eco, rows) if r["ch"] == ch]
    need = sum(e["hp"] for e in part) / max(1.0, sum(e["dur"] for e in part))
    head.append(forged_at(ch * size) / need)
# 第一章是教学章，故意最松，单独要求它比第二章松。
# 第二章起格子还在逐章开，前面盘小、后面靠升级补，裕度允许缓降；
# 解锁新炮台那几章会有台阶式的回弹，所以不要求严格单调，只盯住曲线别塌。
print("  战力裕度: " + " / ".join(f"{x:.2f}" for x in head))
check(head[0] > head[1], f"第一章裕度 {head[0]:.2f} 高于第二章 {head[1]:.2f}（教学章要最松）")
tail = head[1:]
check(min(tail) / max(tail) >= 0.5,
      f"第二章起裕度最低 / 最高 = {min(tail) / max(tail):.2f}（要 ≥0.50）")
slump = tail[-1] / tail[0]
check(slump >= 0.5, f"末章裕度 / 第二章裕度 = {slump:.2f}（要 ≥0.50，低于此说明后期战力跟不上血量）")
item_src = read("Data", "ItemCatalog.cs")
chest_src = read("Data", "ChestCatalog.cs")
forge_cost = sum(int(x) for grp in re.findall(r"Cost = new\[\] \{([^}]*)\}", forge_src) for x in re.findall(r"\d+", grp))
# 道具：解锁只要卡，2~满级每级花墨，复刻 ItemCatalog.NextPrice。
max_lv = int(re.search(r"MaxLevel = (\d+)", item_src).group(1))
ink_base = int(re.search(r"InkBase = (\d+)", item_src).group(1))
ink_mul = [float(x) for x in re.search(r"InkMul = \{([^}]*)\}", item_src).group(1).replace("f", "").split(",")]
q_of = {"Green": 0, "Blue": 1, "Purple": 2}
quals = [q_of[q] for q in re.findall(r"Quality = ItemQuality\.(\w+), Cooldown", item_src)]
check(len(quals) == 9, f"道具 9 件，读到 {len(quals)} 件")
item_cost = sum(round(ink_base * ink_mul[q] * (1 + 0.65 * (lv - 1)) / 5) * 5
                for q in quals for lv in range(1, max_lv))
skin_cost = sum(int(x) for x in re.findall(r"Name = \"[^\"]+\",[^\n]*Price = (\d+)", item_src))
sink = forge_cost + item_cost + skin_cost
# 胜利宝箱：普通关按轮换表发，boss 关金箱，章底首通皇家箱。首通一遍拿到的箱子墨算额外收入。
tiers = re.findall(r"Tier = ChestTier\.(\w+),[^\n]*\n\s*InkMin = (\d+), InkMax = (\d+)", chest_src)
chest_ink_of = {t: (int(a) + int(b)) / 2 for t, a, b in tiers}
cycle = re.findall(r"ChestTier\.(\w+)", re.search(r"Cycle =\s*\{([^}]*)\}", chest_src).group(1))
chest_ink, n_cycle, got = 0.0, 0, {}
for r in rows:
    if r["slot"] == size - 1:
        t = "Royal"
    elif r["bosses"]:
        t = "Gold"
    else:
        t = cycle[n_cycle % len(cycle)]
        n_cycle += 1
    got[t] = got.get(t, 0) + 1
    chest_ink += chest_ink_of[t]
chest_ink = round(chest_ink)
income = ink_total + chest_ink
# 买满的节奏和旧版一致：总价约为首通收入的 2.4~3 倍（旧版 2.7），剩下的靠重刷和每日首胜。
# 宝箱墨是额外收入，算进分母；超线就该降箱子里的墨，而不是砍通关墨。
ratio = sink / max(1, income)
check(2.4 <= ratio <= 3.0, f"墨价 / 首通墨收入 = {sink} / ({ink_total} + 宝箱 {chest_ink}) = {ratio:.2f}")
share = chest_ink / max(1, income)
check(share <= 0.2, f"宝箱墨占首通墨收入 {share:.0%}（要 ≤20%，箱子是添头，主收入还是打怪）")
print(f"  墨价明细：锻造 {forge_cost}  道具 {item_cost}  皮肤 {skin_cost}")
print("  首通宝箱: " + "  ".join(f"{k} {v}" for k, v in got.items()))
# 钻石：首通全关 + 新手礼包 + 一轮签到，够开几次箱、补几次体力。
const_dia = {k: int(v) for k, v in re.findall(r"(Diamond(?:Stage|Boss|Finale)|GiftDiamond)\s*=\s*(\d+)", const_src)}
check_dia = sum(int(x) for x in re.search(r"CheckDiamonds = \{([^}]*)\}", const_src).group(1).split(","))
dia = const_dia["GiftDiamond"] + sum(const_dia["DiamondFinale"] if r["slot"] == size - 1
                                     else const_dia["DiamondBoss"] if r["bosses"] else const_dia["DiamondStage"]
                                     for r in rows)
print(f"  钻石：首通全关 + 礼包 {dia}，一轮签到 {check_dia}")
check(dia >= 150, f"首通全关 + 礼包的钻石 {dia} 至少够开几只金箱（要 ≥150）")

# 炮台升级门槛：逐级不降、不超总关数、露面那一关就能买第一级，最后一级留到后几章。
stage_total = chapters * size
for m in re.finditer(r'Name = "([^"]+)", Stat[^\n]*\n[^\n]*Reveal = (\d+)[^\n]*\n\s*Cost = new\[\] \{([^}]*)\},\s*Gate = new\[\] \{([^}]*)\}', forge_src):
    name, reveal = m.group(1), int(m.group(2))
    cost = [int(x) for x in re.findall(r"\d+", m.group(3))]
    gate = [int(x) for x in re.findall(r"\d+", m.group(4))]
    ok = (len(cost) == len(gate) and gate == sorted(gate) and cost == sorted(cost)
          and gate[-1] <= stage_total and gate[0] == reveal and gate[-1] >= stage_total * 0.55)
    check(ok, f"炮台 {name}：门槛 {gate}  墨价 {cost}")
for ch in range(chapters):
    part = [(e, r) for e, r in zip(eco, rows) if r["ch"] == ch]
    print(f"  第 {ch + 1} 章  金币 " + " ".join(f"{e['purse']:>4}" for e, _ in part)
          + "  抽牌 " + " ".join(f"{e['drafts']:>2}" for e, _ in part)
          + f"  时长 {sum(e['dur'] for e, _ in part) / size:>5.0f}s"
          + f"  墨 {round(sum(e['ink'] for e, _ in part))}")

# ---------- 明细表 ----------
print("\n明细")
print("  关     名字    格子                    格数  血倍   波  新字            关底")
for r in rows:
    shape = "/".join(r["mask"])
    boss = "+".join(b.replace("Boss", "") for b in r["bosses"])
    print(f"  {tag(r)}   {r['title']:<5} {shape:<22} {len(open_cells(r)):>3}  {hp_of(r['ch'], r['slot']):>5.2f}  "
          f"{r['waves']:>2}  {','.join(r['give']):<14}  {boss}")

print()
if fail:
    print(f"有 {len(fail)} 项没过")
    sys.exit(1)
print("全部通过")
