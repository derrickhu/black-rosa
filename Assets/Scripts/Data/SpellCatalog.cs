using UnityEngine;

namespace InkLine
{
    public enum SpellId { Burst, Halt, Rage, Sweep, Splash, Mend, Frost, Slow, Snipe }

    // 全局技能。和改装字牌完全分开：不进牌池、不占格子、不吃星。
    public struct SpellDef
    {
        public SpellId Id;
        public string Name;
        public string Desc;
        public int GoldCost;   // 释放一次要多少本局金币
        public int Price;      // 碎片集满后的解锁价，单位墨
        public int Shards;     // 解锁前要攒的碎片数。关底 boss 随机掉
        public int Gate;       // 旧的通关关数门槛，解锁已改走碎片
        public bool NeedClear; // 旧的通关门槛，解锁已改走碎片
        public Color Tint;
    }

    public static class SpellCatalog
    {
        public const int Count = 9;
        public const int MaxLevel = 5;

        // 和改装抢同一笔本局金币，贵的技能放一次就少一次抽牌。
        // Price / Shards 是解锁的底数。升级见 NextPrice / NextShards，每一级都比上一级贵。
        // 新手礼包一次给 10 碎片、100 墨，定得比这个高，领完礼包也凑不齐一门。
        static readonly SpellDef[] All =
        {
            new SpellDef
            {
                Id = SpellId.Burst, Name = "墨爆", Desc = "最前排炸开一圈，6 点伤害",
                GoldCost = 25, Price = 150, Shards = 12, Gate = 0, Tint = InkTheme.Explode
            },
            new SpellDef
            {
                Id = SpellId.Halt, Name = "定身", Desc = "全场敌人定住 1.6 秒",
                GoldCost = 35, Price = 180, Shards = 14, Gate = 0, Tint = InkTheme.Word
            },
            new SpellDef
            {
                Id = SpellId.Rage, Name = "强攻", Desc = "5 秒内炮弹伤害翻倍",
                GoldCost = 45, Price = 320, Shards = 16, Gate = 0, Tint = InkTheme.Fire
            },
            new SpellDef
            {
                Id = SpellId.Sweep, Name = "横扫", Desc = "全屏 5 点伤害并击退",
                GoldCost = 55, Price = 420, Shards = 18, Gate = 5, Tint = InkTheme.Ink
            },
            new SpellDef
            {
                Id = SpellId.Splash, Name = "泼墨", Desc = "敌人最多那一列灼烧 3 秒",
                GoldCost = 50, Price = 560, Shards = 18, Gate = 6, Tint = InkTheme.Poison
            },
            new SpellDef
            {
                Id = SpellId.Mend, Name = "回血", Desc = "基地回 1 血，每局限一次",
                GoldCost = 70, Price = 760, Shards = 22, Gate = 0, NeedClear = true, Tint = InkTheme.Heart
            },
            new SpellDef
            {
                Id = SpellId.Frost, Name = "冰封", Desc = "全场冰伤并减速",
                GoldCost = 40, Price = 240, Shards = 14, Tint = InkTheme.Ice
            },
            new SpellDef
            {
                Id = SpellId.Slow, Name = "迟缓", Desc = "全场减速一阵",
                GoldCost = 30, Price = 200, Shards = 14, Tint = InkTheme.Water
            },
            new SpellDef
            {
                Id = SpellId.Snipe, Name = "贯击", Desc = "最前一个吃一记重击",
                GoldCost = 35, Price = 220, Shards = 14, Tint = InkTheme.Thunder
            }
        };

        public static SpellDef Get(int i) => All[Mathf.Clamp(i, 0, Count - 1)];
        public static SpellDef Get(SpellId id) => All[(int)id];

        // 当前等级再升一级要的碎片。0 级是解锁，之后每一级比上一级多半份底数。
        public static int NextShards(SpellDef d, int level)
        {
            if (level >= MaxLevel) return 0;
            int step = Mathf.Max(2, d.Shards / 2);
            return d.Shards + Mathf.Max(0, level) * step;
        }

        // 墨价同样从解锁价往上爬。每级再贵四分之一 —— 三分之一那档把技能推成了
        // 局外消耗的七成，练满一个技能比练满整条锻造线还贵，没人会去点第五级。
        public static int NextPrice(SpellDef d, int level)
        {
            if (level >= MaxLevel) return 0;
            int step = Mathf.Max(40, d.Price / 4);
            return d.Price + Mathf.Max(0, level) * step;
        }

        // 按已经练到的等级写效果。0 级先展示 1 级会是什么样。
        // 伤害类技能一律写成「基础弹伤的几倍」。倍数就放在这里，
        // BattleSpells 算伤害和加成页写文案读的是同一份，不会各写一遍再走岔。
        public static float BurstMul(int lv) => 2.2f + 1.1f * lv;
        public static float SweepMul(int lv) => 1.7f + 1.1f * lv;
        public static float FrostMul(int lv) => 1.7f + 0.55f * lv;
        public static float SnipeMul(int lv) => 5.6f + 2.8f * lv;
        public static float SplashMul(int lv) => 1.1f + 0.55f * lv;   // 每秒

        // shotBase 传玩家当前的基础弹伤（MetaProgress.ShotBase），
        // 文案上仍然显示点数 —— 「7.7 倍弹伤」玩家换算不过来。
        public static string Blurb(SpellDef d, int level, float shotBase)
        {
            int lv = Mathf.Clamp(level <= 0 ? 1 : level, 1, MaxLevel);
            float b = shotBase > 0f ? shotBase : ShotMods.DefaultBase;
            switch (d.Id)
            {
                case SpellId.Burst:
                    return "最前排炸开一圈，" + Pts(b * BurstMul(lv)) + " 点伤害";
                case SpellId.Halt:
                    return "全场敌人定住 " + Sec(1.2f + 0.4f * lv) + " 秒";
                case SpellId.Rage:
                    return (4 + lv) + " 秒内炮弹伤害 " + (2 + (lv - 1) / 2) + " 倍";
                case SpellId.Sweep:
                    return "全屏 " + Pts(b * SweepMul(lv)) + " 点伤害并击退";
                case SpellId.Splash:
                    return "那一列灼烧 " + Sec(2.2f + 0.6f * lv) + " 秒，每秒 " + Pts(b * SplashMul(lv)) + " 点";
                case SpellId.Mend:
                    return "基地回 " + (1 + (lv - 1) / 2) + " 血" + (lv >= MaxLevel ? "，可放两次" : "，每局限一次");
                case SpellId.Frost:
                    return "全场 " + Pts(b * FrostMul(lv)) + " 点冰伤，并减速";
                case SpellId.Slow:
                    return "全场减速 " + Sec(2.4f + 0.45f * lv) + " 秒";
                case SpellId.Snipe:
                    return "最前一个 " + Pts(b * SnipeMul(lv)) + " 点";
                default:
                    return d.Desc;
            }
        }

        static string Sec(float t) => t.ToString("0.0");

        static string Pts(float v) => Mathf.Max(1, Mathf.RoundToInt(v)).ToString();
    }

    public struct SkinDef
    {
        public string Name;
        public Color Tint;
        public int Price;
        public int Gate;
        public bool NeedClear;
        public string Note;
        // 加在单发默认伤害上，之后炮台伤害、强攻照旧乘上去。
        public float DamageAdd;
        // 加在开局金币上，之后金币改装照旧再加。
        public int GoldAdd;

        public string Perk
        {
            get
            {
                if (DamageAdd > 0.01f) return "炮弹伤害 +" + Mathf.RoundToInt(DamageAdd);
                if (GoldAdd > 0) return "开局金币 +" + GoldAdd;
                return "无特殊效果";
            }
        }
    }

    // 素笔、朱砂只换样子。青瓷、鎏金的加成写在说明里，并真的进战斗。
    public static class SkinCatalog
    {
        public const int Count = 4;

        static readonly SkinDef[] All =
        {
            new SkinDef { Name = "素笔", Tint = Color.clear, Price = 0, Note = "素木炮架，墨还没染。" },
            new SkinDef
            {
                Name = "朱砂", Tint = InkTheme.Hex("C0392B"), Price = 110, Gate = 2,
                Note = "红得像一盒印泥。"
            },
            new SkinDef
            {
                Name = "青瓷", Tint = InkTheme.Hex("4A7C8C"), Price = 210, Gate = 6,
                Note = "窑火里拉出的细管。", DamageAdd = 1f
            },
            new SkinDef
            {
                Name = "鎏金", Tint = InkTheme.Hex("E8B43A"), Price = 365, NeedClear = true,
                Note = "炮座是一叠金币。", GoldAdd = 20
            }
        };

        public static SkinDef Get(int i) => All[Mathf.Clamp(i, 0, Count - 1)];

        // 已有的全亮，后面只挂下一档 —— 四张一起摊开就没有「攒星解锁」的过程。
        public static bool Listed(int i, bool[] owned)
        {
            if (i < 0 || i >= Count) return false;
            if (owned != null && i < owned.Length && owned[i]) return true;
            for (int k = 0; k < i; k++)
                if (owned == null || k >= owned.Length || !owned[k]) return false;
            return true;
        }

        public static Color Pad(SkinDef d) =>
            d.Tint.a > 0.01f ? d.Tint : InkTheme.Hex("FFE7C4");
    }
}
