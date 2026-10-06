using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public readonly struct SpawnSpec
    {
        public readonly float Time;
        public readonly EnemyId Id;
        public readonly int Column;
        public readonly int Count;

        public SpawnSpec(float time, EnemyId id, int column = -1, int count = 1)
        {
            Time = time;
            Id = id;
            Column = column;
            Count = count;
        }
    }

    public sealed class WaveDef
    {
        public readonly float Duration;
        public readonly SpawnSpec[] Spawns;

        public WaveDef(float duration, params SpawnSpec[] spawns)
        {
            Duration = duration;
            Spawns = spawns;
        }
    }

    public sealed class StageDef
    {
        public int Index;
        public int Chapter;
        public int Slot;
        public string Title;
        public string Name;

        // 格子形状。一行一个字符串，从第一排往上；'X' 开、'.' 关，左到右对应第 0~5 列。
        // 每关手写，不要求比上一关多 —— 位置换一换，同样的字就得换一种摆法。
        public string[] Mask;

        public CardId[] Pool;
        public WaveDef[] Waves;
        // 和 Waves 一一对应的实际出怪只数：已经乘过密度和章节折扣，并按每波的密度（最后一波最密）摊过。
        // 战斗刷怪和 Price 都读它，避免两处各算一遍。
        public int[][] Bodies;
        public bool TeachDraft;
        public bool TeachStar;
        // 这一关新给的字。抽牌时加权，保证第一次就抽得到。
        public CardId[] Fresh;
        public float Hp = 1f;
        public bool Finale;
        public bool Mini;
        public int StaminaCost = GameConstants.StaminaPerStage;
        // 活动关的档位，主线关是 -1。活动关不掉墨、不出墨箱，每波按钱袋给利息。
        public int EventTier = -1;
        public bool Event => EventTier >= 0;

        // 这一关的钱袋，全部在 Build 里按波次表算好。
        // KillGold 是击杀赏金总和；GoldPurse 再加上黄箱的期望值，改装定价按它走。
        public int KillGold;
        public int GoldPurse;
        // 整关打完大约掉多少墨。掉落跟着怪走，这里只是把波次表加了一遍当统计用
        // ——宝箱定价、结算页和校验脚本读它，它不再反过来决定每只怪掉多少。
        public int InkBudget;
        public int ChestGold;
        public int ChestInk;
        public int DraftFirst = GameConstants.FirstDraftCost;
        // 小数。由 Price 按目标抽牌数反推，凑整会让曲线一下差出好几次抽牌。
        public float DraftStep = GameConstants.DraftCostStep;

        public int OpenRows => Mask.Length;

        public bool CellOpen(int col, int row)
        {
            if (row < 0 || row >= Mask.Length || col < 0 || col >= Mask[row].Length) return false;
            return Mask[row][col] == 'X';
        }

        public int OpenCount
        {
            get
            {
                int n = 0;
                for (int r = 0; r < GameConstants.Rows; r++)
                for (int c = 0; c < GameConstants.Columns; c++)
                    if (CellOpen(c, r)) n++;
                return n;
            }
        }

        // 最后一波里有 boss 才算有关底。
        public bool HasBoss
        {
            get
            {
                if (Waves == null || Waves.Length == 0) return false;
                SpawnSpec[] last = Waves[Waves.Length - 1].Spawns;
                for (int i = 0; i < last.Length; i++)
                    if (EnemyIds.IsBoss(last[i].Id)) return true;
                return false;
            }
        }
    }

    public static partial class StageCatalog
    {
        public static readonly string[] ChapterNames =
        {
            "草地", "土路", "石院", "沙漠", "森林", "河滩", "雪原", "火山"
        };

        static readonly string[] CnNum = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };

        public static string ChapterLabel(int chapter)
        {
            int n = chapter + 1;
            return n >= 1 && n <= CnNum.Length ? "第" + CnNum[n - 1] + "章" : "第" + n + "章";
        }

        // 章底数。章内每关再加 5%，章底额外 ×1.15。
        // 格子逐章慢开、第三排留给以后的章节，前几章盘面小，血量跟着压低；后面靠星级、词条和道具补上差距。
        static readonly float[] ChapterHp = { 1.15f, 1.3f, 1.4f, 1.65f, 1.85f, 2.2f, 2.5f, 2.8f };
        const float SlotHpStep = 0.05f;
        const float FinaleHp = 1.15f;

        public static float HpOf(int chapter, int slot, bool finale)
        {
            float c = ChapterHp[Mathf.Clamp(chapter, 0, ChapterHp.Length - 1)];
            return c * (1f + SlotHpStep * slot) * (finale ? FinaleHp : 1f);
        }

        // 怪的移速章节系数。前两章格子少、字少，怪慢一点玩家才来得及看清字在干什么。
        static readonly float[] ChapterSpeed = { 0.75f, 0.85f, 0.92f, 1f, 1f, 1.04f, 1.07f, 1.1f };

        public static float SpeedOf(int chapter) =>
            ChapterSpeed[Mathf.Clamp(chapter, 0, ChapterSpeed.Length - 1)];

        // 每只怪掉墨的章节系数，把每章首通的墨收入钉在锻造和道具定价对应的那条线上。
        // 前两章一局的怪多、局也长，系数反而要压低；改波次或只数之后跑 check_stages 重新对一遍。
        static readonly float[] ChapterInk = { 1.55f, 0.82f, 1.3f, 1.51f, 1.41f, 1.39f, 1.28f, 1.23f };

        public static float InkOf(int chapter) =>
            ChapterInk[Mathf.Clamp(chapter, 0, ChapterInk.Length - 1)];

        static StageDef[] _all;

        public static IReadOnlyList<StageDef> All
        {
            get
            {
                if (_all == null) _all = Build();
                return _all;
            }
        }

        public static StageDef Get(int index) => All[Mathf.Clamp(index, 0, GameConstants.StageCount - 1)];

        public static bool IsFinale(int index) => index % GameConstants.ChapterSize == GameConstants.ChapterSize - 1;

        // 这一关第一次出现的字：前面所有关的字池里都没有过的。
        public static List<CardId> NewCards(int stage)
        {
            var list = new List<CardId>();
            if (stage < 0 || stage >= GameConstants.StageCount) return list;
            var seen = new HashSet<CardId>();
            for (int i = 0; i < stage; i++) Collect(Get(i), seen);
            var here = new HashSet<CardId>();
            Collect(Get(stage), here);
            foreach (CardId id in here)
                if (!seen.Contains(id)) list.Add(id);
            list.Sort();
            return list;
        }

        static void Collect(StageDef s, HashSet<CardId> into)
        {
            if (s.Pool != null)
                for (int i = 0; i < s.Pool.Length; i++) into.Add(s.Pool[i]);
        }

        // ---------- 写关卡用的小工具 ----------

        sealed class Plan
        {
            public string Title;
            public string[] Mask;
            public CardId[] Gives = new CardId[0];
            public WaveDef[] Waves;
            public bool TeachDraft;
            public bool TeachStar;

            public Plan Give(params CardId[] ids) { Gives = ids; return this; }
            public Plan Teach(bool draft, bool star) { TeachDraft = draft; TeachStar = star; return this; }
        }

        static Plan P(string title, string[] shape, params WaveDef[] waves) =>
            new Plan { Title = title, Mask = shape, Waves = waves };

        // 格子形状，从第一排往上写。
        static string[] G(params string[] rows) => rows;

        static SpawnSpec S(float t, EnemyId id, int col = -1, int n = 1) => new SpawnSpec(t, id, col, n);

        // 波长按最后一只出场时间往后留一段，省得每波手算。
        static WaveDef W(params SpawnSpec[] s)
        {
            float last = 0f;
            for (int i = 0; i < s.Length; i++) last = Mathf.Max(last, s[i].Time);
            return new WaveDef(Mathf.Max(8f, last + 5.5f), s);
        }

        // 关底波。双首天生成对，分两列进场；其余 boss 站中间。
        // 这一波不靠时间收尾，boss 不死不算过。
        static WaveDef Boss(EnemyId boss, params SpawnSpec[] escort)
        {
            var list = new List<SpawnSpec>();
            if (boss == EnemyId.BossTwin)
            {
                list.Add(new SpawnSpec(0.3f, boss, 1));
                list.Add(new SpawnSpec(0.3f, boss, 4));
            }
            else list.Add(new SpawnSpec(0.3f, boss, 3));
            list.AddRange(escort);
            float last = 0f;
            for (int i = 0; i < escort.Length; i++) last = Mathf.Max(last, escort[i].Time);
            return new WaveDef(Mathf.Max(20f, last + 10f), list.ToArray());
        }

        // 每章非关底波的目标时长。一局要够走完「铺满棋盘 → 升星 → 顶住最后一波」这条弧，
        // 前两章也不能太短，否则格子还空着关就过了。以后加章只往这张表后面加一个数。
        static readonly float[] ChapterWaveSpan = { 16f, 18f, 19f, 20f, 22f, 22f, 23f, 23f };

        // 把作者写的出场表按它自己的跨度平铺到目标时长。72 关手工加波次不现实，
        // 也不利于扩章；平铺出来的后半段和前半段是同一套编队，节奏不会走样。
        static WaveDef[] Pace(WaveDef[] waves, int chapter)
        {
            float span = ChapterWaveSpan[Mathf.Clamp(chapter, 0, ChapterWaveSpan.Length - 1)];
            var outp = new WaveDef[waves.Length];
            for (int i = 0; i < waves.Length; i++) outp[i] = Tile(waves[i], span);
            return outp;
        }

        static WaveDef Tile(WaveDef w, float span)
        {
            SpawnSpec[] s = w.Spawns;
            if (s.Length == 0) return w;
            // 关底波不动：它不靠时间收尾，boss 不死不算过，铺第二遍就是刷两个 boss。
            for (int i = 0; i < s.Length; i++)
                if (EnemyIds.IsBoss(s[i].Id)) return w;

            float last = 0f;
            for (int i = 0; i < s.Length; i++) last = Mathf.Max(last, s[i].Time);
            float step = last + 1.5f;   // 一轮的跨度，留一点呼吸再接下一轮
            int rounds = step > 0.1f ? Mathf.Max(1, Mathf.RoundToInt(span / step)) : 1;
            if (rounds <= 1) return new WaveDef(Mathf.Max(span, last + 5.5f), s);

            var list = new List<SpawnSpec>(s.Length * rounds);
            for (int r = 0; r < rounds; r++)
            for (int i = 0; i < s.Length; i++)
            {
                SpawnSpec sp = s[i];
                list.Add(new SpawnSpec(sp.Time + r * step, sp.Id, sp.Column, sp.Count));
            }
            float end = last + (rounds - 1) * step;
            return new WaveDef(Mathf.Max(span, end + 5.5f), list.ToArray());
        }

        // 每秒出怪的相对密度。前面几波从 RampOpen 平缓爬到 RampLate：开局就有成群的怪可打、有钱可抽，
        // 最后一波之前格子能铺满、还能顶上几颗星。最后一波固定抬到 RampPeak，
        // 压迫感每关一样，不再随出场表忽多忽少。关底波 boss 本人就是压力，护送只给 RampBossWave。
        // 校验脚本按这几个名字读，改曲线只改这里。
        const float RampOpen = 0.75f;
        const float RampLate = 1.15f;
        const float RampPeak = 2f;
        const float RampBossWave = 0.9f;

        // 每章杂兵只数的折扣。钱跟着怪走，抽牌价由钱袋反推，会一起降下来。
        static readonly float[] ChapterBodies = { 1f, 1f, 0.95f, 0.95f, 0.97f, 0.98f, 1f, 1f };
        // 第一章头三关：新手引导、刚学升星、刚学冰火，怪再少一截。
        static readonly float[] FirstChapterEase = { 0.6f, 0.75f, 0.9f };

        // 整关杂兵总血（出场表 × 密度 × 章节折扣）先按「波长 × 该波密度」分到每一波，
        // 再按这一波兵种的平均血换成只数，波内按出场表的只数比例分到每一拨。
        // 按血分而不是按只数分：最后一波全是墨粒和全是胖墨，压力才一样大。
        // 关底本人不参与：双首还是两只，墨王还是一只。
        // crowd、rampOpen 只给活动关用。crowd 按档位抬或压整关怪量；
        // rampOpen 把第一波的密度从主线的 0.75 再压低，后面才爬上来。
        static int[][] RampCounts(WaveDef[] waves, int chapter, int slot, float crowd = 1f, float rampOpen = -1f,
            int easeWaves = 0, float ease = 1f)
        {
            int nW = waves.Length;
            float bodyMul = ChapterBodies[Mathf.Clamp(chapter, 0, ChapterBodies.Length - 1)] * crowd;
            if (chapter == 0 && slot < FirstChapterEase.Length) bodyMul *= FirstChapterEase[slot];
            var raw = new float[nW][];
            var floor = new int[nW][];
            var bossAt = new bool[nW][];
            var waveRaw = new float[nW];
            var waveHp = new float[nW];
            var waveFloor = new int[nW];
            var bossWave = new bool[nW];
            float hpSum = 0f;
            for (int w = 0; w < nW; w++)
            {
                SpawnSpec[] list = waves[w].Spawns;
                raw[w] = new float[list.Length];
                floor[w] = new int[list.Length];
                bossAt[w] = new bool[list.Length];
                for (int k = 0; k < list.Length; k++)
                {
                    int count = list[k].Count * EnemyCatalog.Density(list[k].Id);
                    if (EnemyIds.IsBoss(list[k].Id))
                    {
                        bossAt[w][k] = true;
                        bossWave[w] = true;
                        continue;
                    }
                    raw[w][k] = count;
                    floor[w][k] = count >= 1 ? 1 : 0;
                    float hp = count * EnemyCatalog.Base(list[k].Id).Hp;
                    waveRaw[w] += count;
                    waveHp[w] += hp;
                    waveFloor[w] += floor[w][k];
                    hpSum += hp;
                }
            }

            int last = nW - 1;
            float pre = 0f;
            for (int w = 0; w < last; w++) pre += Mathf.Max(0.01f, waves[w].Duration);
            var weight = new float[nW];
            float wSum = 0f;
            float cursor = 0f;
            for (int w = 0; w < nW; w++)
            {
                float dur = Mathf.Max(0.01f, waves[w].Duration);
                float lvl;
                if (w == last && nW > 1) lvl = bossWave[w] ? RampBossWave : RampPeak;
                else
                {
                    float u = pre > 0.01f ? Mathf.Clamp01((cursor + dur * 0.5f) / pre) : 0.5f;
                    float open = rampOpen >= 0f ? rampOpen : RampOpen;
                    lvl = open + (RampLate - open) * u;
                    if (w < easeWaves) lvl *= ease;
                }
                weight[w] = waveRaw[w] > 0f ? dur * lvl : 0f;
                wSum += weight[w];
                cursor += dur;
            }

            // 每波的目标血 ÷ 这一波每只的平均血 = 这一波该刷几只。
            float hpGoal = hpSum * bodyMul;
            var bodies = new float[nW];
            float bodySum = 0f;
            for (int w = 0; w < nW; w++)
            {
                if (weight[w] <= 0f || wSum <= 0f) continue;
                bodies[w] = hpGoal * weight[w] / wSum / (waveHp[w] / waveRaw[w]);
                bodySum += bodies[w];
            }
            int total = Mathf.Max(1, Mathf.RoundToInt(bodySum));
            int[] perWave = Share(total, bodies, waveFloor);
            var grid = new int[nW][];
            for (int w = 0; w < nW; w++)
            {
                grid[w] = Share(perWave[w], raw[w], floor[w]);
                SpawnSpec[] list = waves[w].Spawns;
                for (int k = 0; k < list.Length; k++)
                    if (bossAt[w][k]) grid[w][k] = list[k].Count * EnemyCatalog.Density(list[k].Id);
            }
            return grid;
        }

        // 按权重把 total 摊成整数，每份不低于 floor；权重为 0 的那份恒为 0。
        // 先按比例取整，零头按小数部分从大到小补；保底撑爆了总数时，从超出比例最多的那份往回扣。
        static int[] Share(int total, float[] weight, int[] floor)
        {
            int n = weight.Length;
            var outp = new int[n];
            var exact = new float[n];
            float wSum = 0f;
            for (int i = 0; i < n; i++) wSum += Mathf.Max(0f, weight[i]);
            if (wSum <= 0f) return outp;
            int got = 0;
            for (int i = 0; i < n; i++)
            {
                if (weight[i] <= 0f) continue;
                exact[i] = total * weight[i] / wSum;
                outp[i] = Mathf.Max(floor[i], Mathf.FloorToInt(exact[i]));
                got += outp[i];
            }
            int diff = total - got;
            if (diff > 0)
            {
                var order = new List<int>();
                for (int i = 0; i < n; i++)
                    if (weight[i] > 0f) order.Add(i);
                order.Sort((a, b) =>
                {
                    int c = (exact[b] - outp[b]).CompareTo(exact[a] - outp[a]);
                    return c != 0 ? c : b.CompareTo(a);
                });
                for (int k = 0; diff > 0; k = (k + 1) % order.Count)
                {
                    outp[order[k]]++;
                    diff--;
                }
            }
            while (diff < 0)
            {
                int best = -1;
                float over = float.MinValue;
                for (int i = 0; i < n; i++)
                {
                    if (weight[i] <= 0f || outp[i] <= floor[i]) continue;
                    float o = outp[i] - exact[i];
                    if (o > over) { over = o; best = i; }
                }
                if (best < 0) break;
                outp[best]--;
                diff++;
            }
            return outp;
        }

        static StageDef[] Build()
        {
            var plans = new List<Plan>();
            Chapter1(plans);
            Chapter2(plans);
            Chapter3(plans);
            Chapter4(plans);
            Chapter5(plans);
            Chapter6(plans);
            Chapter7(plans);
            Chapter8(plans);

            var have = new List<CardId>();
            var s = new StageDef[GameConstants.StageCount];
            for (int i = 0; i < s.Length; i++)
            {
                Plan p = plans[Mathf.Min(i, plans.Count - 1)];
                for (int k = 0; k < p.Gives.Length; k++)
                    if (!have.Contains(p.Gives[k])) have.Add(p.Gives[k]);
                int ch = i / GameConstants.ChapterSize;
                int slot = i % GameConstants.ChapterSize;
                bool finale = slot == GameConstants.ChapterSize - 1;
                s[i] = new StageDef
                {
                    Index = i,
                    Chapter = ch,
                    Slot = slot,
                    Title = p.Title,
                    Name = ChapterLabel(ch) + " · " + p.Title,
                    Mask = p.Mask,
                    Pool = have.ToArray(),
                    Fresh = p.Gives,
                    Waves = Pace(p.Waves, ch),
                    TeachDraft = p.TeachDraft,
                    TeachStar = p.TeachStar,
                    Hp = HpOf(ch, slot, finale),
                    Finale = finale,
                    Mini = slot == 4 && ch > 0,
                    StaminaCost = GameConstants.StaminaPerStage
                };
                s[i].Bodies = RampCounts(s[i].Waves, ch, slot);
                Price(s[i]);
            }
            return s;
        }

        // 宝箱：普通怪的掉落率、黄箱占比。BattleWorld 掉箱和这里算期望用的是同一组数。
        public const float ChestChance = 0.08f;
        public const float ChestGoldShare = 0.7f;
        // 一局的目标抽牌数：先把开放格子铺满，再把大半格子顶到二三星。
        // 抽牌费用由它反推，所以以后加章、改棋盘大小都自动对得上，不用手调。
        // 格子收紧之后每格多给几抽，一局的总抽牌数和以前差不多，火力从铺宽换成了叠星。
        const float DraftPicksPerCell = 2f;
        // 前两章一局短，按 2 算会五六秒弹一次三选一；铺满之外只多给一两抽，教升星够用。
        const float FirstChapterPicksPerCell = 1.8f;
        const float SecondChapterPicksPerCell = 1.8f;
        // 道具不花金币，钱几乎全给抽牌；留一成半给抽到中意那张之前的试错，不把定价压到刚好买满。
        const float DraftBudgetShare = 0.85f;

        // 掉落跟着怪走，这里只是把整关加一遍当统计用：校验脚本、抽牌定价和结算页读它。
        // 算法和 BattleWorld.Spawn + Make 必须一致 —— 只数用 RampCounts 摊过的 Bodies，
        // 每只掉满自己那份，章节系数保底 1。
        static void Price(StageDef s, float share = DraftBudgetShare)
        {
            float drop = EnemyCatalog.DropMul(s.Hp);
            float inkMul = drop * InkOf(s.Chapter);
            int gold = 0, kills = 0;
            float ink = 0f;
            for (int w = 0; w < s.Waves.Length; w++)
            {
                SpawnSpec[] list = s.Waves[w].Spawns;
                for (int k = 0; k < list.Length; k++)
                {
                    SpawnSpec sp = list[k];
                    bool boss = EnemyIds.IsBoss(sp.Id);
                    int count = s.Bodies != null ? s.Bodies[w][k]
                        : sp.Count * EnemyCatalog.Density(sp.Id);
                    EnemyDef d = EnemyCatalog.Base(sp.Id);
                    gold += count * EnemyCatalog.Drop(d.Gold, drop);
                    ink += count * d.Ink * inkMul;
                    if (!boss) kills += count;
                }
            }
            s.KillGold = Mathf.Max(1, gold);
            s.InkBudget = Mathf.Max(1, Mathf.RoundToInt(ink));
            s.ChestGold = Mathf.Max(3, Mathf.RoundToInt(s.KillGold * 0.05f));
            s.ChestInk = Mathf.Max(2, Mathf.RoundToInt(s.InkBudget * 0.15f));
            float chests = kills * ChestChance + (s.HasBoss ? 1f : 0f);
            s.GoldPurse = s.KillGold + Mathf.RoundToInt(chests * ChestGoldShare * s.ChestGold);

            int open = Mathf.Max(1, s.OpenCount);
            int n;
            if (s.Chapter == 0) n = Mathf.Max(open + 1, Mathf.RoundToInt(open * FirstChapterPicksPerCell));
            else if (s.Chapter == 1) n = Mathf.Max(open + 2, Mathf.RoundToInt(open * SecondChapterPicksPerCell));
            else n = Mathf.Clamp(Mathf.RoundToInt(open * DraftPicksPerCell), open + 2, open * 3);
            float budget = (s.GoldPurse + ForgeStats.Default.StartGold) * share;
            // 起价定成平均价的一半，步长再反推，费用就从便宜缓缓爬到贵，
            // 而整条曲线累计下来刚好吃满预算。
            s.DraftFirst = Mathf.Max(GameConstants.FirstDraftCost, Mathf.RoundToInt(budget / n * 0.5f));
            s.DraftStep = n > 1
                ? Mathf.Max(0f, 2f * (budget - n * s.DraftFirst) / (n * (n - 1f)))
                : 0f;
        }
    }
}
