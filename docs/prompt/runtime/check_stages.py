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
        m = re.search(r'P\("[^"]+",\s*(Row\(([^)]*)\)|Full)', b)
        cells = [6, 6, 6] if m.group(1) == "Full" else [int(x) for x in re.findall(r"\d+", m.group(2))]
        mask = re.search(r"\.Holes\(([^)]*)\)", b)
        mask = re.findall(r'"([X.]+)"', mask.group(1)) if mask else None
        give = re.search(r"\.Give\(([^)]*)\)", b)
        give = [x.strip() for x in give.group(1).split(",")] if give else []
        limit = re.search(r"\.Limit\(([^)]*)\)", b)
        limit = [x.strip() for x in limit.group(1).split(",")] if limit else None
        rules = re.findall(r"StageRule\.(\w+)", b)
        puts = [(int(a), int(r), c) for a, r, c in re.findall(r"\.Put\((\d+),\s*(\d+),\s*(\w+)", b)]
        waves = len(re.findall(r"\bW\(", b)) + len(re.findall(r"\bBoss\(", b))
        main = re.search(r"\bBoss\((Boss\w+)", b)
        main = main.group(1) if main else None
        bosses = re.findall(r"\b(Boss[A-Z]\w+)\b", b)
        enemies = [e for e in re.findall(r"S\([\d.]+f,\s*(\w+)", b) if not e.startswith("Boss")]
        rows.append(dict(i=len(rows), ch=ch, slot=k, title=title, cells=cells, mask=mask, give=give,
                         limit=limit, rules=rules, puts=puts, waves=waves, main=main, bosses=bosses,
                         enemies=enemies))

print("关卡数")
check(len(rows) == want, f"{chapters} 章 × {size} 关 = {want}，实到 {len(rows)}")
for ch in range(chapters):
    n = sum(1 for r in rows if r["ch"] == ch)
    if n != size:
        check(False, f"第 {ch + 1} 章 {n} 关")


def open_cells(r):
    if r["mask"]:
        return {(c, y) for y, line in enumerate(r["mask"]) for c, ch in enumerate(line) if ch == "X"}
    order = [2, 3, 1, 4, 0, 5]
    return {(order[k], y) for y, n in enumerate(r["cells"]) for k in range(n)}


# ---------- 格子 ----------
print("\n格子（前三章逐行打开，不许回退；残局另算）")
plain = [r for r in rows if not r["mask"]]
seq = [sum(r["cells"]) for r in plain]
drops = [f"{plain[k]['ch'] + 1}-{plain[k]['slot'] + 1}" for k in range(1, len(plain)) if seq[k] < seq[k - 1]]
check(not drops, "开放格子数单调不减" + ("" if not drops else "  回退于 " + ", ".join(drops)))
jumps = [f"{plain[k]['ch'] + 1}-{plain[k]['slot'] + 1}" for k in range(1, len(plain))
         if len(plain[k]["cells"]) - len(plain[k - 1]["cells"]) > 1]
check(not jumps, "开放行数一行一行加")
for ch, rowsn in [(0, 1), (1, 2), (2, 3)]:
    got = {len(r["cells"]) for r in rows if r["ch"] == ch and not r["mask"]}
    check(got == {rowsn}, f"第 {ch + 1} 章只开到第 {rowsn} 排: {sorted(got)}")
bad_mask = [f"{r['ch'] + 1}-{r['slot'] + 1}" for r in rows if r["mask"]
            and (len(r["mask"]) > 3 or any(len(x) != 6 for x in r["mask"]))]
check(not bad_mask, "残局掩码都是 ≤3 行 × 6 列" + ("" if not bad_mask else ": " + ", ".join(bad_mask)))
bad_put = [f"{r['ch'] + 1}-{r['slot'] + 1} ({c},{y})" for r in rows for c, y, _ in r["puts"]
           if (c, y) not in open_cells(r)]
check(not bad_put, "残卷预置的字都落在开放格上" + ("" if not bad_put else ": " + ", ".join(bad_put)))

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
have = []
bad_limit = []
for r in rows:
    have += r["give"]
    if r["limit"]:
        off = [c for c in r["limit"] if c not in have]
        if off:
            bad_limit.append(f"{r['ch'] + 1}-{r['slot'] + 1}: {off}")
check(not bad_limit, "限字里的字都已经发过" + ("" if not bad_limit else ": " + ", ".join(bad_limit)))

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
    0: {"Walker", "Swarm", "Chubby", "Tall", "Ball", "BigHead"},
    1: {"Crawler", "Belt", "Runner", "Strafer"},
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
print("\n每关都有新东西（新字 / 新兵 / 格子变化 / 规则 / 关底）")
bland = []
prev_cells = None
for r in rows:
    new_enemy = any(first[e] is r for e in set(r["enemies"]))
    cells = sum(r["cells"]) if not r["mask"] else -1
    fresh = r["give"] or new_enemy or r["rules"] or r["limit"] or r["puts"] or r["mask"] \
        or r["bosses"] or cells != prev_cells
    if not fresh:
        bland.append(f"{r['ch'] + 1}-{r['slot'] + 1} {r['title']}")
    prev_cells = cells
check(not bland, "没有平淡关" + ("" if not bland else ": " + ", ".join(bland)))

waves = [r["waves"] for r in rows]
check(min(waves[2:]) >= 3, f"第三关起每关至少 3 波（最少 {min(waves[2:])}）")

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
picks_per_cell = float(re.search(r"DraftPicksPerCell\s*=\s*([\d.]+)f", core_src).group(1))
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
    return ramp_bodies(out, "Rich" in r["rules"], chapter_bodies[min(r["ch"], len(chapter_bodies) - 1)])


def ramp_weight(u):
    u = min(1.0, max(0.0, u))
    return ramp_open + (ramp_late - ramp_open) * (u ** ramp_pow)


def ramp_bodies(waves, rich, body_mul=1.0):
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
            if rich and not boss:
                count = math.ceil(count * 1.25)
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
    mul = (1.5 if "Rich" in r["rules"] else 1) * (1.2 if "Swift" in r["rules"] else 1)
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
            gold += count * drop(drop(g, dmul), mul)
            ink += count * ink_one * dmul * mul
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
                open1=open1, early=early, late=late)


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
print(f"  开局 1 秒出怪（全关合计）{sum(e['open1'] for e in eco)}，"
      f"前 25% {sum(e['early'] for e in eco)}，中后段 {sum(e['late'] for e in eco)}")

# 单局成长：一局的钱至少要够把开放格子铺满，第三章起还要够把大半格子顶到二三星。
# 铺不满，玩家一局里就永远看不到盘面长成型，也就没有「变强」那一下。
thin = [f"{r['ch'] + 1}-{r['slot'] + 1}({e['drafts']}<{e['open']})"
        for e, r in zip(eco, rows) if e["drafts"] < e["open"]]
check(not thin, "每关的钱都够铺满棋盘" + ("" if not thin else ": " + ", ".join(thin)))
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
# 解锁新炮台那几章会有台阶式的回弹，属于设计意图，所以不要求严格单调，
# 只盯住整条曲线别塌：最低处不能比最高处低一半，末章不能比首章低三成。
print("  战力裕度: " + " / ".join(f"{x:.2f}" for x in head))
check(min(head) / max(head) >= 0.5,
      f"战力裕度最低 / 最高 = {min(head) / max(head):.2f}（要 ≥0.50）")
slump = head[-1] / head[0]
check(slump >= 0.7, f"末章裕度 / 首章裕度 = {slump:.2f}（要 ≥0.70，低于此说明后期战力跟不上血量）")
spell_src = read("Data", "SpellCatalog.cs")
forge_cost = sum(int(x) for grp in re.findall(r"Cost = new\[\] \{([^}]*)\}", forge_src) for x in re.findall(r"\d+", grp))
spell_prices = [int(x) for x in re.findall(r"GoldCost = \d+, Price = (\d+)", spell_src)]
max_lv = int(re.search(r"MaxLevel = (\d+)", spell_src).group(1))
price_step = int(re.search(r"step = Mathf\.Max\(40, d\.Price / (\d+)\)", spell_src).group(1))
spell_cost = sum(p + lv * max(40, p // price_step) for p in spell_prices for lv in range(max_lv))
skin_cost = sum(int(x) for x in re.findall(r"Name = \"[^\"]+\",[^\n]*Price = (\d+)", spell_src))
sink = forge_cost + spell_cost + skin_cost
# 买满的节奏和旧版一致：总价约为首通收入的 2.4~3 倍（旧版 2.7），剩下的靠重刷和每日首胜。
ratio = sink / max(1, ink_total)
check(2.4 <= ratio <= 3.0, f"墨价 / 首通墨收入 = {sink} / {ink_total} = {ratio:.2f}")
print(f"  墨价明细：锻造 {forge_cost}  技能 {spell_cost}  皮肤 {skin_cost}")

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
print("  关     名字    格子          血倍   波  新字            规则 / 关底")
for r in rows:
    cells = "残局" if r["mask"] else str(r["cells"])
    tag = " ".join(r["rules"])
    if r["limit"]:
        tag += " 限" + "".join(r["limit"])
    if r["puts"]:
        tag += " 残卷"
    if r["bosses"]:
        tag += " " + "+".join(b.replace("Boss", "") for b in r["bosses"])
    print(f"  {r['ch'] + 1}-{r['slot'] + 1}   {r['title']:<5} {cells:<12} {hp_of(r['ch'], r['slot']):>5.2f}  "
          f"{r['waves']:>2}  {','.join(r['give']):<14}  {tag.strip()}")

print()
if fail:
    print(f"有 {len(fail)} 项没过")
    sys.exit(1)
print("全部通过")
