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

    // 关卡规则，可叠加。每条只改一处，名字就是玩家看到的那两个字。
    [Flags]
    public enum StageRule
    {
        None = 0,
        Preset = 1,     // 残卷：开局格子里已经摆好几张字
        Limited = 2,    // 限字：牌池只给指定几个字
        Swift = 4,      // 疾行：敌人快两成，金币也多两成
        Rich = 8,       // 丰年：金币多五成，刷怪多四分之一
        Frail = 16,     // 孤城：基地只有 1 血，首通墨多五成
        Masked = 32,    // 残局：格子逐格开关，能挖洞、留窄巷
        NoSpell = 64    // 禁术：技能键不能用，首通墨多五成
    }

    public readonly struct PresetCard
    {
        public readonly int Col;
        public readonly int Row;
        public readonly CardId Id;
        public readonly int Star;

        public PresetCard(int col, int row, CardId id, int star = 1)
        {
            Col = col;
            Row = row;
            Id = id;
            Star = star;
        }
    }

    public sealed class StageDef
    {
        public int Index;
        public int Chapter;
        public int Slot;
        public string Title;
        public string Name;

        // 每行开放几格，长度就是开放的行数。{2} 是「只有第一排中间两格能放字」，
        // {6,3} 是「第一排全开、第二排开中间三格」。有 Mask 时以 Mask 为准。
        public int[] OpenCells;
        // 残局用。一行一个字符串，从第一排往上；'X' 开、'.' 关，左到右对应第 0~5 列。
        public string[] Mask;

        public CardId[] Pool;
        public WaveDef[] Waves;
        public bool TeachDraft;
        public bool TeachStar;
        public StageRule Rules;
        public PresetCard[] Preset;
        public float Hp = 1f;
        public bool Finale;
        public bool Mini;
        public int StaminaCost = GameConstants.StaminaPerStage;

        // 这一关的钱袋，全部在 Build 里按波次表算好。
        // KillGold 是击杀赏金总和；GoldPurse 再加上黄箱的期望值，改装定价按它走。
        public int KillGold;
        public int GoldPurse;
        // 击杀墨预算：整关打完大约掉多少墨，按赏金比例摊到每只怪。
        public int InkBudget;
        public int ChestGold;
        public int ChestInk;
        public int DraftFirst = GameConstants.FirstDraftCost;
        public int DraftStep = GameConstants.DraftCostStep;

        public bool Has(StageRule r) => (Rules & r) != 0;

        public int OpenRows => Mask != null ? Mask.Length : OpenCells.Length;

        public bool CellOpen(int col, int row)
        {
            if (Mask == null) return StageCatalog.CellOpen(OpenCells, col, row);
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

        public string RuleLabel => StageCatalog.RuleLabel(Rules);
    }

    public static partial class StageCatalog
    {
        // 一行里格子按中间往两边开：2、3 先开，最外侧两列最后开。
        // 中间两列是敌人最密的地方，新手关只开这两格也立刻能感到「放字有用」。
        static readonly int[] ColOpenOrder = { 2, 3, 1, 4, 0, 5 };

        public static bool CellOpen(int[] openCells, int col, int row)
        {
            if (openCells == null || row < 0 || row >= openCells.Length) return false;
            int n = Mathf.Min(openCells[row], ColOpenOrder.Length);
            for (int i = 0; i < n; i++)
                if (ColOpenOrder[i] == col) return true;
            return false;
        }

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
        // 第三章以后格子已经满开，玩家变强只剩新字、星级和局外成长，曲线要比前三章平。
        static readonly float[] ChapterHp = { 1.0f, 1.3f, 1.6f, 1.9f, 2.2f, 2.5f, 2.8f, 3.1f };
        const float SlotHpStep = 0.05f;
        const float FinaleHp = 1.15f;

        public static float HpOf(int chapter, int slot, bool finale)
        {
            float c = ChapterHp[Mathf.Clamp(chapter, 0, ChapterHp.Length - 1)];
            return c * (1f + SlotHpStep * slot) * (finale ? FinaleHp : 1f);
        }

        public static string RuleLabel(StageRule rules)
        {
            var parts = new List<string>();
            if ((rules & StageRule.Preset) != 0) parts.Add("残卷");
            if ((rules & StageRule.Limited) != 0) parts.Add("限字");
            if ((rules & StageRule.Swift) != 0) parts.Add("疾行");
            if ((rules & StageRule.Rich) != 0) parts.Add("丰年");
            if ((rules & StageRule.Frail) != 0) parts.Add("孤城");
            if ((rules & StageRule.Masked) != 0) parts.Add("残局");
            if ((rules & StageRule.NoSpell) != 0) parts.Add("禁术");
            return string.Join(" · ", parts);
        }

        public static string RuleHint(StageRule rules)
        {
            var parts = new List<string>();
            if ((rules & StageRule.Preset) != 0) parts.Add("残卷：开局已摆好字");
            if ((rules & StageRule.Limited) != 0) parts.Add("限字：只抽这几个字");
            if ((rules & StageRule.Swift) != 0) parts.Add("疾行：敌快，金币多");
            if ((rules & StageRule.Rich) != 0) parts.Add("丰年：敌多，金币多");
            if ((rules & StageRule.Frail) != 0) parts.Add("孤城：基地只剩一血");
            if ((rules & StageRule.Masked) != 0) parts.Add("残局：有格子被封");
            if ((rules & StageRule.NoSpell) != 0) parts.Add("禁术：技能不能用");
            return string.Join("  ", parts);
        }

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

        // 这一关第一次出现的字：前面所有关的字池和预置字里都没有过的。
        // 不拿相邻两关做差 —— Limit 关的字池是缩过的，下一关会把老字「重新放出来」。
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
            if (s.Preset != null)
                for (int i = 0; i < s.Preset.Length; i++) into.Add(s.Preset[i].Id);
        }

        // ---------- 写关卡用的小工具 ----------

        sealed class Plan
        {
            public string Title;
            public int[] Cells;
            public string[] Mask;
            public CardId[] Gives = new CardId[0];
            public CardId[] Only;
            public StageRule Rules;
            public readonly List<PresetCard> Preset = new List<PresetCard>();
            public WaveDef[] Waves;
            public bool TeachDraft;
            public bool TeachStar;

            public Plan Give(params CardId[] ids) { Gives = ids; return this; }

            public Plan Limit(params CardId[] ids)
            {
                Only = ids;
                Rules |= StageRule.Limited;
                return this;
            }

            public Plan Put(int col, int row, CardId id, int star = 1)
            {
                Preset.Add(new PresetCard(col, row, id, star));
                Rules |= StageRule.Preset;
                return this;
            }

            public Plan Holes(params string[] rows)
            {
                Mask = rows;
                Rules |= StageRule.Masked;
                return this;
            }

            public Plan Rule(StageRule r) { Rules |= r; return this; }
            public Plan Teach(bool draft, bool star) { TeachDraft = draft; TeachStar = star; return this; }
        }

        static Plan P(string title, int[] cells, params WaveDef[] waves) =>
            new Plan { Title = title, Cells = cells, Waves = waves };

        static int[] Row(params int[] cells) => cells;
        static readonly int[] Full = { 6, 6, 6 };

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
                    OpenCells = p.Cells,
                    Mask = p.Mask,
                    Pool = p.Only ?? have.ToArray(),
                    Waves = p.Waves,
                    TeachDraft = p.TeachDraft,
                    TeachStar = p.TeachStar,
                    Rules = p.Rules,
                    Preset = p.Preset.ToArray(),
                    Hp = HpOf(ch, slot, finale),
                    Finale = finale,
                    Mini = slot == 4 && ch > 0,
                    StaminaCost = finale ? GameConstants.StaminaFinale : GameConstants.StaminaPerStage
                };
                Price(s[i]);
            }
            return s;
        }

        // 宝箱：普通怪的掉落率、黄箱占比。BattleWorld 掉箱和这里算期望用的是同一组数。
        public const float ChestChance = 0.08f;
        public const float ChestGoldShare = 0.7f;
        const float DraftRefPurse = 45f;

        public static float GoldMulOf(StageRule rules) =>
            ((rules & StageRule.Rich) != 0 ? 1.5f : 1f) * ((rules & StageRule.Swift) != 0 ? 1.2f : 1f);

        // 和 BattleWorld.Spawn 同一套摊法：一行表按 Density 铺开，赏金按表里原来的只数摊，每只保底 1。
        static void Price(StageDef s)
        {
            float mul = GoldMulOf(s.Rules);
            int gold = 0, kills = 0;
            for (int w = 0; w < s.Waves.Length; w++)
            {
                SpawnSpec[] list = s.Waves[w].Spawns;
                for (int k = 0; k < list.Length; k++)
                {
                    SpawnSpec sp = list[k];
                    bool boss = EnemyIds.IsBoss(sp.Id);
                    int count = sp.Count * EnemyCatalog.Density(sp.Id);
                    if (s.Has(StageRule.Rich) && !boss) count = Mathf.CeilToInt(count * 1.25f);
                    int purse = Mathf.Max(1, Mathf.RoundToInt(sp.Count * EnemyCatalog.Get(sp.Id, 1f).Gold * mul));
                    for (int i = 0; i < count; i++)
                        gold += Mathf.Max(1, purse / count + (i < purse % count ? 1 : 0));
                    if (!boss) kills += count;
                }
            }
            s.KillGold = Mathf.Max(1, gold);
            s.InkBudget = Mathf.RoundToInt((10 + 6 * s.Chapter + s.Slot) * (s.Finale ? 1.5f : 1f));
            s.ChestGold = Mathf.Max(3, Mathf.RoundToInt(s.KillGold * 0.05f));
            s.ChestInk = Mathf.Max(2, Mathf.RoundToInt(s.InkBudget * 0.15f));
            float chests = kills * ChestChance + (s.HasBoss ? 1f : 0f);
            s.GoldPurse = s.KillGold + Mathf.RoundToInt(chests * ChestGoldShare * s.ChestGold);
            float m = Mathf.Max(1f, Mathf.Pow(s.GoldPurse / DraftRefPurse, 0.4f));
            s.DraftFirst = Mathf.RoundToInt(GameConstants.FirstDraftCost * m);
            s.DraftStep = Mathf.Max(1, Mathf.RoundToInt(GameConstants.DraftCostStep * m));
        }
    }
}
