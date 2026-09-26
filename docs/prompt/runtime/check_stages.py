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
for m in re.finditer(r"case EnemyId\.(Boss\w+):[^\n]*\n\s*return new EnemyDef\(id,\s*([\d.]+)f\s*\*\s*t", enemy_src):
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
# 照 StageCatalog.Price 重算一遍：赏金按 Density 铺开、每只保底 1，击杀墨按 InkBudget，
# 宝箱按掉率算期望。改装价格从钱袋推，墨价从 72 关首通总收入推。
print("\n经济（金币 / 改装 / 墨）")
gold_of = {m.group(1): int(m.group(2)) for m in re.finditer(
    r"case EnemyId\.(\w+):[^\n]*\n\s*return new EnemyDef\(id,\s*[\d.]+f\s*\*\s*t,\s*[\d.]+f,\s*(\d+)", enemy_src)}
gold_of.setdefault("Walker", 2)
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
draft_ref = float(re.search(r"DraftRefPurse\s*=\s*([\d.]+)f", core_src).group(1))
first_cost = int(re.search(r"FirstDraftCost\s*=\s*(\d+)", const_src).group(1))
cost_step = int(re.search(r"DraftCostStep\s*=\s*(\d+)", const_src).group(1))
start_gold = 6


def spawns_of(r):
    src = read("Data", f"StageChapter{r['ch'] + 1}.cs")
    starts = [m.start() for m in re.finditer(r"s\.Add\(P\(", src)] + [len(src)]
    b = src[starts[r["slot"]]:starts[r["slot"] + 1]]
    out = [(e, int(n) if n else 1) for e, _, n in
           re.findall(r"S\([\d.]+f,\s*(\w+)(?:,\s*(-?\d+))?(?:,\s*(\d+))?\)", b)]
    boss = re.search(r"\bBoss\((Boss\w+)", b)
    if boss:
        out += [(boss.group(1), 1)] * (2 if boss.group(1) == "BossTwin" else 1)
    return out


def econ(r):
    mul = (1.5 if "Rich" in r["rules"] else 1) * (1.2 if "Swift" in r["rules"] else 1)
    gold = kills = 0
    for e, n in spawns_of(r):
        boss = e.startswith("Boss")
        count = n * density.get(e, 1)
        if "Rich" in r["rules"] and not boss:
            count = -(-count * 5 // 4)
        purse = max(1, int(n * gold_of.get(e, 2) * mul + 0.5))
        gold += sum(max(1, purse // count + (1 if i < purse % count else 0)) for i in range(count))
        if not boss:
            kills += count
    finale = r["slot"] == size - 1
    ink = round((10 + 6 * r["ch"] + r["slot"]) * (1.5 if finale else 1))
    cg = max(3, round(gold * 0.05))
    ci = max(2, round(ink * 0.15))
    chests = kills * chest_chance + (1 if r["bosses"] else 0)
    purse_all = gold + round(chests * chest_gold_share * cg)
    m_ = max(1.0, (purse_all / draft_ref) ** 0.4)
    first_d, step = round(first_cost * m_), max(1, round(cost_step * m_))
    # 金币的七成花在改装上，余下留给技能。数一数买得起几次。
    budget, n_d, spent = (purse_all + start_gold) * 0.7, 0, 0
    while spent + first_d + n_d * step <= budget:
        spent += first_d + n_d * step
        n_d += 1
    ink_all = ink + chests * (1 - chest_gold_share) * ci
    old = (6 + 3 * r["ch"] + r["slot"] + 12) * (2 if finale else 1)
    if "Frail" in r["rules"] or "NoSpell" in r["rules"]:
        old = old * 3 // 2
    return dict(gold=gold, purse=purse_all, first=first_d, step=step, drafts=n_d, ink=ink_all, old=old)


eco = [econ(r) for r in rows]
ch_first = [sum(e["first"] for e, r in zip(eco, rows) if r["ch"] == ch) / size for ch in range(chapters)]
back = [f"第 {k + 1} 章" for k in range(1, chapters) if ch_first[k] < ch_first[k - 1]]
check(not back, "改装起价（章均）逐章不降: " + " / ".join(f"{x:.1f}" for x in ch_first))
fin_first = [e["first"] for e, r in zip(eco, rows) if r["slot"] == size - 1]
back = [f"第 {k + 1} 章" for k in range(1, chapters) if fin_first[k] < fin_first[k - 1]]
check(not back, "章底改装起价逐章不降: " + " / ".join(map(str, fin_first)))
ink_total = round(sum(e["ink"] for e in eco))
old_total = sum(e["old"] for e in eco)

forge_src = read("Data", "ForgeCatalog.cs")
spell_src = read("Data", "SpellCatalog.cs")
forge_cost = sum(int(x) for grp in re.findall(r"Cost = new\[\] \{([^}]*)\}", forge_src) for x in re.findall(r"\d+", grp))
spell_prices = [int(x) for x in re.findall(r"GoldCost = \d+, Price = (\d+)", spell_src)]
max_lv = int(re.search(r"MaxLevel = (\d+)", spell_src).group(1))
spell_cost = sum(p + lv * max(8, p // 4) for p in spell_prices for lv in range(max_lv))
skin_cost = sum(int(x) for x in re.findall(r"Name = \"[^\"]+\",[^\n]*Price = (\d+)", spell_src))
sink = forge_cost + spell_cost + skin_cost
# 买满的节奏和旧版一致：总价约为首通收入的 2.4~3 倍（旧版 2.7），剩下的靠重刷和每日首胜。
ratio = sink / max(1, ink_total)
check(2.4 <= ratio <= 3.0, f"墨价 / 首通墨收入 = {sink} / {ink_total} = {ratio:.2f}（旧收入 {old_total}）")
print(f"  墨价明细：锻造 {forge_cost}  技能 {spell_cost}  皮肤 {skin_cost}")
for ch in range(chapters):
    part = [(e, r) for e, r in zip(eco, rows) if r["ch"] == ch]
    print(f"  第 {ch + 1} 章  金币 " + " ".join(f"{e['purse']:>3}" for e, _ in part)
          + "  改装 " + " ".join(f"{e['first']}+{e['step']}×{e['drafts']}" for e, _ in part)
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
