using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 插槽只定种类，数值看镶进去的印章。存档里槽的类型写 (int)SealType，0 是还没刷出。
    public enum SealType { None, Luck, Edge, Fierce, Wide, Last, Move, Keep }

    public enum SealQuality { Green, Blue, Purple, Gold }

    // 一枚印章：掉落、结算、开箱演出都按这个传。
    public struct SealDrop
    {
        public SealType Type;
        public SealQuality Quality;
    }

    // 字卡第二版：插槽和印章。数值、刷出权重、掉落表都在这里，详见 docs/插槽.md。
    // 槽按「条目」编号：字是 CardId，词是 CardCatalog.IdCount + WordId。每个条目 3 个槽。
    public static class SealCatalog
    {
        public const int OpenCleared = 45;
        public const int SlotsPer = 3;
        public const int TypeCount = 7;
        public const int QualityCount = 4;
        // 一次刷新这个字上所有已开的槽。只花钻石，不限次数，不按槽数加价。
        public const int RerollDiamond = 10;

        public static int KeyCount => CardCatalog.IdCount + CardCatalog.WordCount;
        public static int SlotCount => KeyCount * SlotsPer;
        public static int BagSize => TypeCount * QualityCount;

        public static bool IsOpen(int cleared) => cleared >= OpenCleared;

        public static int KeyOf(CodexEntry e) =>
            e.Kind == CodexKind.Word ? CardCatalog.IdCount + e.Index : e.Index;

        public static int KeyOf(CardId id) => (int)id;
        public static int KeyOf(WordId w) => CardCatalog.IdCount + (int)w;

        public static int BagIndex(SealType t, SealQuality q) => ((int)t - 1) * QualityCount + (int)q;

        // 第 k 个槽在字卡几级时出现。
        static readonly int[] SlotLevel = { 1, 4, 7 };

        public static int SlotLevelOf(int k) => SlotLevel[Mathf.Clamp(k, 0, SlotLevel.Length - 1)];

        public static int SlotsAt(int level)
        {
            int n = 0;
            for (int k = 0; k < SlotLevel.Length; k++) if (level >= SlotLevel[k]) n++;
            return n;
        }

        // ---- 数值 ----

        static readonly float[] LuckMul = { 1.3f, 1.6f, 2.0f, 2.5f };
        static readonly float[] EdgePct = { 0.02f, 0.03f, 0.05f, 0.08f };
        static readonly float[] FiercePct = { 0.06f, 0.10f, 0.15f, 0.22f };
        static readonly float[] LastPct = { 0.08f, 0.12f, 0.18f, 0.25f };
        static readonly float[] WidePct = { 0.05f, 0.08f, 0.12f, 0.16f };
        static readonly float[] SplashCell = { 0.4f, 0.55f, 0.7f, 0.85f };
        static readonly float[] MovePct = { 0.08f, 0.12f, 0.18f, 0.25f };
        static readonly float[] KeepPt = { 0.03f, 0.05f, 0.07f, 0.10f };
        static readonly int[] Scrap = { 2, 5, 12, 30 };
        // 几枚溅射叠在一起也不超过这一圈，否则单体字比炸还炸。
        public const float SplashCap = 1.2f;
        // 溅射到旁边那只的伤害份额。
        public const float SplashShare = 0.5f;

        static float At(float[] t, SealQuality q) => t[Mathf.Clamp((int)q, 0, t.Length - 1)];

        public static int ScrapOf(SealQuality q) => Scrap[Mathf.Clamp((int)q, 0, Scrap.Length - 1)];

        public static float LuckOf(SealQuality q) => At(LuckMul, q);

        // 这枚印章在这一类上的数值。广域对没有半径的字是溅射格数，其余是比例或百分点。
        public static float Value(SealType t, SealQuality q, bool splash = false)
        {
            switch (t)
            {
                case SealType.Luck: return At(LuckMul, q);
                case SealType.Edge: return At(EdgePct, q);
                case SealType.Fierce: return At(FiercePct, q);
                case SealType.Wide: return splash ? At(SplashCell, q) : At(WidePct, q);
                case SealType.Last: return At(LastPct, q);
                case SealType.Move: return At(MovePct, q);
                case SealType.Keep: return At(KeepPt, q);
                default: return 0f;
            }
        }

        // ---- 谁能刷出什么 ----

        public static bool HasRadius(int key) =>
            key == (int)CardId.Water || key == (int)CardId.Thunder
            || key == (int)CardId.Gather || key == (int)CardId.Explode;

        static bool HasTime(int key) =>
            key == (int)CardId.Fire || key == (int)CardId.Poison || key == (int)CardId.Ice
            || key == (int)CardId.Water || key == (int)CardId.Thunder || key == (int)CardId.Stun
            || key == (int)CardId.Confuse || key == (int)CardId.Break || key == (int)CardId.Earth;

        static bool HasMove(int key) =>
            key == (int)CardId.Earth || key == (int)CardId.Wind || key == (int)CardId.Gather
            || key == KeyOf(WordId.Knockback);

        static bool HasKeep(int key) =>
            key == (int)CardId.Split || key == (int)CardId.Pierce || key == (int)CardId.Explode
            || key == KeyOf(WordId.Cleave);

        public static bool Allowed(int key, SealType t)
        {
            switch (t)
            {
                case SealType.Luck:
                case SealType.Edge:
                case SealType.Fierce:
                case SealType.Wide: return true;
                case SealType.Last: return HasTime(key);
                case SealType.Move: return HasMove(key);
                case SealType.Keep: return HasKeep(key);
                default: return false;
            }
        }

        static readonly List<SealType> _own = new List<SealType>();

        // 刷一个槽：字运 12、锋芒 / 烈度 / 广域各 22，专属合计 22 平分；没有专属，那 22 平分给三个通用。
        // noLuck：这个字别的槽已经有字运，抽中就重抽。
        public static SealType RollSlot(int key, bool noLuck)
        {
            _own.Clear();
            if (Allowed(key, SealType.Last)) _own.Add(SealType.Last);
            if (Allowed(key, SealType.Move)) _own.Add(SealType.Move);
            if (Allowed(key, SealType.Keep)) _own.Add(SealType.Keep);
            float common = _own.Count > 0 ? 22f : 22f + 22f / 3f;
            float own = _own.Count > 0 ? 22f / _own.Count : 0f;
            float luck = noLuck ? 0f : 12f;
            float sum = luck + common * 3f + own * _own.Count;
            float r = Random.value * sum;
            if ((r -= luck) < 0f) return SealType.Luck;
            if ((r -= common) < 0f) return SealType.Edge;
            if ((r -= common) < 0f) return SealType.Fierce;
            if ((r -= common) < 0f) return SealType.Wide;
            for (int i = 0; i < _own.Count; i++)
                if ((r -= own) < 0f) return _own[i];
            return _own.Count > 0 ? _own[_own.Count - 1] : SealType.Wide;
        }

        // ---- 掉落 ----

        public enum Source { Normal, Gold, Rare }

        static readonly float[][] Odds =
        {
            new[] { 0.60f, 0.28f, 0.10f, 0.02f },
            new[] { 0.45f, 0.35f, 0.16f, 0.04f },
            new[] { 0f, 0.60f, 0.32f, 0.08f },
        };

        public const float SilverChance = 0.15f;
        public const float GoldChance = 0.35f;

        public static SealDrop Roll(Source s)
        {
            float[] odds = Odds[(int)s];
            float r = Random.value;
            int q = odds.Length - 1;
            for (int i = 0; i < odds.Length; i++)
            {
                if ((r -= odds[i]) < 0f) { q = i; break; }
            }
            return new SealDrop { Type = (SealType)Random.Range(1, TypeCount + 1), Quality = (SealQuality)q };
        }

        // 开箱掉几枚。木箱、炮台宝箱不掉。
        public static void RollChest(ChestTier t, List<SealDrop> into)
        {
            switch (t)
            {
                case ChestTier.Silver:
                    if (Random.value < SilverChance) into.Add(Roll(Source.Normal));
                    break;
                case ChestTier.Gold:
                    if (Random.value < GoldChance) into.Add(Roll(Source.Gold));
                    break;
                case ChestTier.Royal:
                    into.Add(Roll(Source.Rare));
                    break;
            }
        }

        // 通关第五章后，每日残局 20 波全部守住才给一枚。品质走普通档：绿 60 / 蓝 28 / 紫 10 / 金 2。种类七种平均。
        public static SealDrop RollDaily() => Roll(Source.Normal);

        // ---- 文案和颜色 ----

        public static readonly SealType[] Types =
            { SealType.Luck, SealType.Edge, SealType.Fierce, SealType.Wide, SealType.Last, SealType.Move, SealType.Keep };

        public static string TypeName(SealType t)
        {
            switch (t)
            {
                case SealType.Luck: return "字运";
                case SealType.Edge: return "锋芒";
                case SealType.Fierce: return "烈度";
                case SealType.Wide: return "广域";
                case SealType.Last: return "持久";
                case SealType.Move: return "位移";
                case SealType.Keep: return "贯通";
                default: return "";
            }
        }

        // 背包里点一格的一句话。具体到哪个字加什么，在详情的镶嵌面板里写。
        public static string Blurb(SealType t)
        {
            switch (t)
            {
                case SealType.Luck: return "这个字在三选一里更常出现，每字只能刷出一个";
                case SealType.Edge: return "这个字的伤害加成再高一截";
                case SealType.Fierce: return "这个字本事里的那一项变强";
                case SealType.Wide: return "范围变大；单体的字改成溅射旁边";
                case SealType.Last: return "火毒冰等状态和定身持续更久";
                case SealType.Move: return "推、拉、吹得更远，之后两秒怪受伤更多";
                case SealType.Keep: return "穿透、分裂、爆开时伤害衰减更少";
                default: return "";
            }
        }

        public static string IconKey(SealType t)
        {
            switch (t)
            {
                case SealType.Luck: return "Ui/seal_ico_luck";
                case SealType.Edge: return "Ui/seal_ico_edge";
                case SealType.Fierce: return "Ui/seal_ico_fierce";
                case SealType.Wide: return "Ui/seal_ico_wide";
                case SealType.Last: return "Ui/seal_ico_last";
                case SealType.Move: return "Ui/seal_ico_move";
                case SealType.Keep: return "Ui/seal_ico_keep";
                default: return "Ui/seal_ico_lock";
            }
        }

        public static string QualityName(SealQuality q) =>
            q == SealQuality.Gold ? "金" : q == SealQuality.Purple ? "紫" : q == SealQuality.Blue ? "蓝" : "绿";

        public static Color QualityColor(SealQuality q) =>
            q == SealQuality.Gold ? InkTheme.Hex("E2662C")
            : q == SealQuality.Purple ? ItemCatalog.QualityColor(ItemQuality.Purple)
            : q == SealQuality.Blue ? ItemCatalog.QualityColor(ItemQuality.Blue)
            : ItemCatalog.QualityColor(ItemQuality.Green);

        public static Color QualityDeep(SealQuality q) =>
            q == SealQuality.Gold ? InkTheme.Hex("9A3A10")
            : q == SealQuality.Purple ? ItemCatalog.QualityDeep(ItemQuality.Purple)
            : q == SealQuality.Blue ? ItemCatalog.QualityDeep(ItemQuality.Blue)
            : ItemCatalog.QualityDeep(ItemQuality.Green);

        // 「烈度 · 绿」
        public static string DropName(SealDrop d) => TypeName(d.Type) + " · " + QualityName(d.Quality);

        // 槽边上和品质格里的短说明：提高的是什么（几个字）+ 这个品质加多少。
        // 「整发伤害 +5%」「裂弹衰减 -10%」「出现率 ×1.6」。what 不依赖品质，空槽也用它。
        public static void Line(int key, SealType t, SealQuality q, out string what, out string amount)
        {
            what = WhatOf(key, t);
            bool splash = t == SealType.Wide && !HasRadius(key);
            float v = Value(t, q, splash);
            int pct = Mathf.RoundToInt(v * 100f);
            if (t == SealType.Luck) amount = "×" + v.ToString("0.0#");
            else if (splash) amount = v.ToString("0.0#") + " 格";
            else if (t == SealType.Keep && key != (int)CardId.Explode) amount = "-" + pct + "%";
            else amount = "+" + pct + "%";
        }

        public static string WhatOf(int key, SealType t)
        {
            switch (t)
            {
                case SealType.Luck: return "出现率";
                case SealType.Edge: return "整发伤害";
                case SealType.Fierce: return FierceWhat(key);
                case SealType.Wide:
                    if (!HasRadius(key)) return "溅射范围";
                    switch ((CardId)key)
                    {
                        case CardId.Water: return "减速范围";
                        case CardId.Thunder: return "连带范围";
                        case CardId.Gather: return "拉怪范围";
                        default: return "爆炸范围";
                    }
                case SealType.Last: return key == (int)CardId.Earth ? "硬直时长" : "状态时长";
                case SealType.Move:
                    if (key == KeyOf(WordId.Knockback)) return "击退距离";
                    switch ((CardId)key)
                    {
                        case CardId.Earth: return "推开距离";
                        case CardId.Wind: return "多吹几率";
                        default: return "拉怪距离";
                    }
                case SealType.Keep:
                    if (key == KeyOf(WordId.Cleave)) return "每跳衰减";
                    switch ((CardId)key)
                    {
                        case CardId.Split: return "裂弹衰减";
                        case CardId.Pierce: return "穿透衰减";
                        default: return "溅射份额";
                    }
                default: return "";
            }
        }

        static string FierceWhat(int key)
        {
            if (key >= CardCatalog.IdCount)
            {
                switch ((WordId)(key - CardCatalog.IdCount))
                {
                    case WordId.InstantKill: return "斩杀血线";
                    case WordId.ArrowRain: return "每箭伤害";
                    case WordId.Knockback: return "退后易伤";
                    case WordId.Cleave: return "每跳伤害";
                    case WordId.Crush: return "追加伤害";
                    default: return "强度";
                }
            }
            switch ((CardId)key)
            {
                case CardId.Gold: return "加算伤害";
                case CardId.Heavy: return "重击倍率";
                case CardId.Wood: return "吸血比例";
                case CardId.Fire: return "灼烧伤害";
                case CardId.Poison: return "中毒伤害";
                case CardId.Ice: return "冻住易伤";
                case CardId.Water: return "减速幅度";
                case CardId.Thunder: return "雷击易伤";
                case CardId.Stun: return "晕住易伤";
                case CardId.Confuse: return "惑乱强度";
                case CardId.Break: return "易伤比例";
                case CardId.Earth: return "推后易伤";
                case CardId.Wind: return "吹后易伤";
                case CardId.Gather: return "拉后易伤";
                case CardId.Split: return "裂弹伤害";
                case CardId.Track: return "追踪弹伤害";
                case CardId.Accel: return "加速弹伤害";
                case CardId.Pierce: return "穿透伤害";
                case CardId.Rebound: return "折返伤害";
                case CardId.Explode: return "爆炸伤害";
                default: return "强度";
            }
        }

        // 这一类印章在这个字上提高的是什么。点开已镶的槽时用。
        public static string EffectOf(int key, SealType t)
        {
            switch (t)
            {
                case SealType.Luck: return LuckLine(key);
                case SealType.Edge: return key >= CardCatalog.IdCount
                    ? "成了这个词的炮弹，整发伤害提高"
                    : "带这个字的炮弹，整发伤害提高";
                case SealType.Fierce: return FierceLine(key);
                case SealType.Wide: return HasRadius(key) ? WideLine(key) : "命中时溅到身边的怪，溅射伤害是五成";
                case SealType.Last: return key == (int)CardId.Earth ? "落地硬直时长提高" : "状态持续时间提高";
                case SealType.Move: return MoveLine(key);
                case SealType.Keep: return KeepLine(key);
                default: return "";
            }
        }

        static string LuckLine(int key)
        {
            if (key < CardCatalog.IdCount) return "改装三选一里，" + CodexCatalog.Title(CodexKind.Glyph, key) + "更容易刷到";
            CardId[] parts = CodexCatalog.PartsOf((WordId)(key - CardCatalog.IdCount));
            if (parts.Length < 2) return "改装三选一里，这个词的字更容易刷到";
            return "改装三选一里，" + CardCatalog.Get(parts[0]).Name + "和" + CardCatalog.Get(parts[1]).Name + "都更容易刷到";
        }

        static string WideLine(int key)
        {
            switch ((CardId)key)
            {
                case CardId.Water: return "减速波及的半径变大";
                case CardId.Thunder: return "连带的半径变大";
                case CardId.Gather: return "拉怪的半径变大";
                default: return "爆炸半径变大";
            }
        }

        static string MoveLine(int key)
        {
            if (key == KeyOf(WordId.Knockback)) return "击退的距离变远";
            switch ((CardId)key)
            {
                case CardId.Earth: return "推开的距离变远";
                case CardId.Wind: return "有几率多吹开一列";
                default: return "拉怪的距离变远";
            }
        }

        static string KeepLine(int key)
        {
            if (key == KeyOf(WordId.Cleave)) return "每跳一次的伤害衰减变少";
            switch ((CardId)key)
            {
                case CardId.Split: return "裂弹的伤害衰减变少";
                case CardId.Pierce: return "每穿一只的伤害衰减变少";
                default: return "爆炸溅到周围的伤害份额变高";
            }
        }

        static string FierceLine(int key)
        {
            if (key >= CardCatalog.IdCount)
            {
                switch ((WordId)(key - CardCatalog.IdCount))
                {
                    case WordId.InstantKill: return "斩杀血线提高，头目也算";
                    case WordId.ArrowRain: return "每支箭的伤害提高";
                    case WordId.Knockback: return "被击退的怪之后两秒受到的伤害提高";
                    case WordId.Cleave: return "每一跳的伤害提高";
                    case WordId.Crush: return "按当前生命追加的那段伤害提高";
                    default: return "";
                }
            }
            switch ((CardId)key)
            {
                case CardId.Gold: return "加算伤害提高";
                case CardId.Heavy: return "重击的倍率提高";
                case CardId.Wood: return "吸血比例提高";
                case CardId.Fire: return "灼烧每秒伤害提高";
                case CardId.Poison: return "中毒每秒伤害提高";
                case CardId.Ice: return "冻住期间受到的伤害提高";
                case CardId.Water: return "减速幅度变大";
                case CardId.Thunder: return "被雷打中的怪晕住期间受伤提高，连带的也算";
                case CardId.Stun: return "晕住期间受到的伤害提高";
                case CardId.Confuse: return "被惑的怪咬同伴更狠，头目减速更多";
                case CardId.Break: return "易伤比例提高";
                case CardId.Earth: return "被推开的怪之后两秒受到的伤害提高";
                case CardId.Wind: return "被吹开的怪之后两秒受到的伤害提高";
                case CardId.Gather: return "被拉过来的怪之后两秒受到的伤害提高";
                case CardId.Split: return "每发裂弹的伤害提高";
                case CardId.Track: return "这发追踪弹自己的伤害提高";
                case CardId.Accel: return "这发加速弹自己的伤害提高";
                case CardId.Pierce: return "每一段穿透的伤害提高";
                case CardId.Rebound: return "折返那一程的伤害提高";
                case CardId.Explode: return "爆炸伤害提高";
                default: return "";
            }
        }
    }

    // 一局开打前，把每个条目的印章加成摊平成数组。每日残局、字卡没开都是 null。
    // 下标是 SealCatalog 的条目号。同类型的几枚相加。
    public sealed class SealPack
    {
        public readonly float[] Luck;
        public readonly float[] Edge;
        public readonly float[] Fierce;
        public readonly float[] Wide;
        public readonly float[] Splash;
        public readonly float[] Last;
        public readonly float[] Move;
        public readonly float[] Keep;
        // 镶着金印章的条目，落字飘字换成金色。
        public readonly bool[] Gold;

        public SealPack()
        {
            int n = SealCatalog.KeyCount;
            Luck = new float[n];
            Edge = new float[n];
            Fierce = new float[n];
            Wide = new float[n];
            Splash = new float[n];
            Last = new float[n];
            Move = new float[n];
            Keep = new float[n];
            Gold = new bool[n];
            for (int i = 0; i < n; i++) Luck[i] = 1f;
        }

        public void Add(int key, SealType t, SealQuality q)
        {
            if (key < 0 || key >= Edge.Length) return;
            if (q == SealQuality.Gold) Gold[key] = true;
            switch (t)
            {
                case SealType.Luck: Luck[key] = Mathf.Max(Luck[key], SealCatalog.LuckOf(q)); break;
                case SealType.Edge: Edge[key] += SealCatalog.Value(t, q); break;
                case SealType.Fierce: Fierce[key] += SealCatalog.Value(t, q); break;
                case SealType.Wide:
                    if (SealCatalog.HasRadius(key)) Wide[key] += SealCatalog.Value(t, q);
                    else Splash[key] = Mathf.Min(SealCatalog.SplashCap, Splash[key] + SealCatalog.Value(t, q, true));
                    break;
                case SealType.Last: Last[key] += SealCatalog.Value(t, q); break;
                case SealType.Move: Move[key] += SealCatalog.Value(t, q); break;
                case SealType.Keep: Keep[key] += SealCatalog.Value(t, q); break;
            }
        }
    }
}
