using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 字卡：局外给字和词升级。通关第四章开放，花金箔升。
    // 一级 = 带着这个字（或成了这个词）的炮弹整发伤害 +2%，一发带好几个字取平均。
    // 第五章起怪的血按「这章大致该有几级」同步抬，前四章不动。详见 docs/字卡.md。
    public static class StudyCatalog
    {
        public const int OpenCleared = 36;
        public const int MaxLevel = 10;
        public const float PerLevel = 0.02f;

        // Cost[lv] 是从 lv 升到 lv+1 要的金箔。
        static readonly int[] Cost = { 30, 40, 55, 70, 90, 110, 135, 160, 190, 220 };

        // 金箔来源。炮台碎片和道具卡多出来的，开放后不再折墨。
        public const int ShardFoilAdvanced = 3;
        public const int ShardFoilRare = 5;
        static readonly int[] SpareFoil = { 1, 2, 5 };
        // 墨换金箔一天三次，越换越贵。后期一关首通约 33 金箔、400 墨（看双倍 800），
        // 换的汇率要明显差过去打关：一次 10 金箔，一天全换完也就够中段升一级。
        static readonly int[] SwapPrice = { 800, 1200, 1600 };
        public const int SwapFoil = 10;
        // 钻石换金箔，一天一次。比墨换贵一点，用来花掉打关多出来的钻。
        public const int GemSwapCost = 40;
        public const int GemSwapFoil = 20;
        public static int SwapPerDay => SwapPrice.Length;
        public static int SwapInkAt(int done) => SwapPrice[Mathf.Clamp(done, 0, SwapPrice.Length - 1)];
        public const int TrialPerDay = 2;
        public const int TrialBonus = 3;

        // 通关第四章之后，七日签到的墨换成金箔。第 1～4 天 3，第 5、6 天 6，第 7 天 10。看广告照样翻倍。
        public static int CheckFoilOf(int day) => day >= 7 ? 10 : day >= 5 ? 6 : 3;

        // 通关第四章之后，开木、银、金、皇家和炮台宝箱都有这个几率额外掉金箔，数量在 1～5 里取。
        public const float ChestFoilChance = 0.4f;
        public const int ChestFoilMin = 1;
        public const int ChestFoilMax = 5;

        public static int RollChestFoil()
        {
            if (Random.value >= ChestFoilChance) return 0;
            return Random.Range(ChestFoilMin, ChestFoilMax + 1);
        }

        // 第 ch 章（从 0 数）打完时，常用的几个字大致升到几级。第五章（下标 4）起才有。
        static readonly int[] Expected = { 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 9, 10 };
        // 怪血补满预期加成：后期本来就偏松，升到预期的人难度持平，没升的人吃亏。
        const float HpShare = 1f;

        public static bool IsOpen(int cleared) => cleared >= OpenCleared;

        public static int CostOf(int level) => level >= 0 && level < Cost.Length ? Cost[level] : 0;

        // 每多通一章，上限 +1。刚开放是 4，十章全通是 10，后面两章不再加。
        public static int CapOf(int cleared)
        {
            if (!IsOpen(cleared)) return 0;
            return Mathf.Clamp(4 + (cleared - OpenCleared) / GameConstants.ChapterSize, 0, MaxLevel);
        }

        public static float BonusOf(int level) => PerLevel * Mathf.Clamp(level, 0, MaxLevel + TrialBonus);

        public static int ShardFoil(SkinRarity r) => r == SkinRarity.Rare ? ShardFoilRare : ShardFoilAdvanced;

        public static int SpareFoilOf(ItemQuality q) => SpareFoil[Mathf.Clamp((int)q, 0, SpareFoil.Length - 1)];

        // 首通发的金箔。只有第五章起的主线和精英。
        public static int FirstClearFoil(StageDef s)
        {
            if (s == null || s.Endless || s.Daily || s.Event || s.Chapter < 4) return 0;
            float n = 15 + 3 * (s.Chapter - 4);
            if (s.Finale) n *= 2f;
            else if (s.HasBoss) n *= 1.5f;
            if (s.Hard) n *= 1.2f;
            return Mathf.RoundToInt(n);
        }

        public static int ExpectedLevel(int chapter) => Expected[Mathf.Clamp(chapter, 0, Expected.Length - 1)];

        public static float HpMul(int chapter) => 1f + PerLevel * ExpectedLevel(chapter) * HpShare;

        public static float HpMul(StageDef s)
        {
            if (s == null || s.Endless || s.Daily || s.Event) return 1f;
            return HpMul(s.Chapter);
        }

        // ---- 品质 ----
        // 和道具同一套颜色：1~3 级绿、4~6 蓝、7~9 紫，满级（含试用顶上去的 11~13）金。
        // 字卡页、详情页、局内棋盘和炮弹都按这一份上色，玩家在哪儿看到的都是同一个档。
        public enum Tier { None, Green, Blue, Purple, Gold }

        public static Tier TierOf(int level) =>
            level >= MaxLevel ? Tier.Gold
            : level >= 7 ? Tier.Purple
            : level >= 4 ? Tier.Blue
            : level >= 1 ? Tier.Green
            : Tier.None;

        public static string TierName(Tier t) =>
            t == Tier.Gold ? "传说" : t == Tier.Purple ? "稀有" : t == Tier.Blue ? "高级" : t == Tier.Green ? "普通" : "";

        public static Color TierColor(Tier t) =>
            t == Tier.Gold ? InkTheme.Hex("FFB52E")
            : t == Tier.Purple ? ItemCatalog.QualityColor(ItemQuality.Purple)
            : t == Tier.Blue ? ItemCatalog.QualityColor(ItemQuality.Blue)
            : t == Tier.Green ? ItemCatalog.QualityColor(ItemQuality.Green)
            : InkTheme.CtaOff;

        public static Color TierDeep(Tier t) =>
            t == Tier.Gold ? InkTheme.Hex("B9700A")
            : t == Tier.Purple ? ItemCatalog.QualityDeep(ItemQuality.Purple)
            : t == Tier.Blue ? ItemCatalog.QualityDeep(ItemQuality.Blue)
            : t == Tier.Green ? ItemCatalog.QualityDeep(ItemQuality.Green)
            : InkTheme.Outline;

        // 一发炮弹的字卡平均等级，0 表示这发不吃字卡。炮弹光圈按它分档。
        public static float ShotLevel(ShotMods m, int[] glyph, int[] word)
        {
            if (m == null || glyph == null) return 0f;
            int sum = 0;
            int n = 0;
            for (int i = 1; i < glyph.Length; i++)
            {
                if (glyph[i] < 0 || !m.Has((CardId)i)) continue;
                sum += glyph[i];
                n++;
            }
            int w = (int)m.Word;
            if (word != null && w > 0 && w < word.Length && word[w] >= 0)
            {
                sum += word[w];
                n++;
            }
            return n == 0 ? 0f : sum / (float)n;
        }

        // 一发炮弹的字卡倍率。glyph / word 是开局写进来的等级，-1 表示不参与（成词的半边字）。
        public static float ShotMul(ShotMods m, int[] glyph, int[] word) => 1f + PerLevel * ShotLevel(m, glyph, word);

        // ---- 条目 ----

        public enum Filter { All, Hurt, Status, Move, Ballistic, Area, Word }

        public static readonly Filter[] Filters =
            { Filter.All, Filter.Hurt, Filter.Status, Filter.Move, Filter.Ballistic, Filter.Area, Filter.Word };

        public static string FilterName(Filter f)
        {
            switch (f)
            {
                case Filter.Hurt: return "伤害";
                case Filter.Status: return "状态";
                case Filter.Move: return "位移";
                case Filter.Ballistic: return "弹道";
                case Filter.Area: return "范围";
                case Filter.Word: return "词";
                default: return "全部";
            }
        }

        public static bool Studyable(CodexEntry e) =>
            e.Kind == CodexKind.Word
            || (e.Kind == CodexKind.Glyph && CardCatalog.Get((CardId)e.Index).Wake != CardWake.WordPart);

        static List<CodexEntry> _all;

        // 和图鉴字谱同一个顺序：按关卡里第一次出现的先后。
        public static IReadOnlyList<CodexEntry> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new List<CodexEntry>();
                IReadOnlyList<CodexEntry> src = CodexCatalog.Base;
                for (int i = 0; i < src.Count; i++)
                    if (Studyable(src[i])) _all.Add(src[i]);
                return _all;
            }
        }

        public static bool Match(CodexEntry e, Filter f)
        {
            if (f == Filter.All) return true;
            if (e.Kind == CodexKind.Word) return f == Filter.Word;
            if (f == Filter.Word) return false;
            GlyphFamily fam = GlyphTable.Get((CardId)e.Index).Family;
            switch (f)
            {
                case Filter.Hurt: return fam == GlyphFamily.Hurt;
                case Filter.Status: return fam == GlyphFamily.Status;
                case Filter.Move: return fam == GlyphFamily.Move;
                case Filter.Ballistic: return fam == GlyphFamily.Ballistic;
                case Filter.Area: return fam == GlyphFamily.Area;
                default: return true;
            }
        }

        public static string FamilyName(CodexEntry e)
        {
            if (e.Kind == CodexKind.Word) return "词";
            switch (GlyphTable.Get((CardId)e.Index).Family)
            {
                case GlyphFamily.Hurt: return "伤害";
                case GlyphFamily.Status: return "状态";
                case GlyphFamily.Move: return "位移";
                case GlyphFamily.Ballistic: return "弹道";
                case GlyphFamily.Area: return "范围";
                default: return "";
            }
        }

        static int[] _firstChapter;

        // 第几章（从 0 数）的牌池里第一次出现。没进任何关的返回 -1。
        public static int FirstChapter(CodexEntry e)
        {
            if (_firstChapter == null)
            {
                _firstChapter = new int[CardCatalog.IdCount];
                for (int i = 0; i < _firstChapter.Length; i++) _firstChapter[i] = -1;
                for (int s = 0; s < GameConstants.StageCount; s++)
                {
                    StageDef st = StageCatalog.Get(s);
                    if (st.Pool == null) continue;
                    for (int i = 0; i < st.Pool.Length; i++)
                    {
                        int id = (int)st.Pool[i];
                        if (id > 0 && id < _firstChapter.Length && _firstChapter[id] < 0)
                            _firstChapter[id] = s / GameConstants.ChapterSize;
                    }
                }
            }
            if (e.Kind == CodexKind.Glyph) return e.Index < _firstChapter.Length ? _firstChapter[e.Index] : -1;
            int best = -1;
            CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
            for (int i = 0; i < parts.Length; i++)
            {
                int c = _firstChapter[(int)parts[i]];
                if (c >= 0 && (best < 0 || c > best)) best = c;
            }
            return best;
        }
    }
}
