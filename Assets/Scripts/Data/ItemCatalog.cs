using UnityEngine;

namespace InkLine
{
    public enum ItemId { Burst, Halt, Rage, Sweep, Splash, Mend, Frost, Slow, Snipe }

    public enum ItemQuality { Green, Blue, Purple }

    // 道具：带进关的实物。不花本局金币，冷却好了、场面对得上就自动丢出去。
    // 和改装字牌完全分开：不进牌池、不占格子、不吃星。
    public struct ItemDef
    {
        public ItemId Id;
        public string Name;
        public string Desc;
        public string When;      // 自动触发的条件，写给玩家看
        public ItemQuality Quality;
        public float Cooldown;   // 1 级冷却秒数，往上每级缩一点
        public Color Tint;
    }

    public static class ItemCatalog
    {
        public const int Count = 9;
        public const int MaxLevel = 5;

        // 1 级冷却按品质分档，升一级大约短 6%（见 CooldownAt）。
        // 档与档之间留了空：满级的普通仍短于 1 级高级，满级的高级仍短于 1 级稀有。
        static readonly ItemDef[] All =
        {
            new ItemDef
            {
                Id = ItemId.Burst, Name = "鞭炮", Desc = "最前排炸开一圈",
                When = "最前排有 2 个敌人", Quality = ItemQuality.Green, Cooldown = 14f, Tint = InkTheme.Explode
            },
            new ItemDef
            {
                Id = ItemId.Halt, Name = "闹钟", Desc = "全场敌人定住",
                When = "敌人逼近或场上 5 个以上", Quality = ItemQuality.Blue, Cooldown = 28f, Tint = InkTheme.Word
            },
            new ItemDef
            {
                Id = ItemId.Rage, Name = "能量饮料", Desc = "一阵子炮弹伤害翻倍",
                When = "场上 3 个以上或首领在场", Quality = ItemQuality.Blue, Cooldown = 32f, Tint = InkTheme.Fire
            },
            new ItemDef
            {
                Id = ItemId.Sweep, Name = "大扫把", Desc = "全屏伤害并击退",
                When = "场上 6 个以上或敌人逼近", Quality = ItemQuality.Purple, Cooldown = 52f, Tint = InkTheme.Ink
            },
            new ItemDef
            {
                Id = ItemId.Splash, Name = "辣椒酱", Desc = "敌人最多那一列灼烧",
                When = "同一列有 3 个敌人", Quality = ItemQuality.Purple, Cooldown = 46f, Tint = InkTheme.Poison
            },
            new ItemDef
            {
                Id = ItemId.Mend, Name = "急救包", Desc = "基地回血",
                When = "基地掉血后", Quality = ItemQuality.Purple, Cooldown = 60f, Tint = InkTheme.Heart
            },
            new ItemDef
            {
                Id = ItemId.Frost, Name = "冰块", Desc = "全场冰伤并减速",
                When = "场上 4 个以上", Quality = ItemQuality.Blue, Cooldown = 24f, Tint = InkTheme.Ice
            },
            new ItemDef
            {
                Id = ItemId.Slow, Name = "胶水", Desc = "全场减速一阵",
                When = "有敌人过了半场", Quality = ItemQuality.Green, Cooldown = 16f, Tint = InkTheme.Water
            },
            new ItemDef
            {
                Id = ItemId.Snipe, Name = "弹弓", Desc = "最前一个吃一记重击",
                When = "场上有敌人", Quality = ItemQuality.Green, Cooldown = 12f, Tint = InkTheme.Thunder
            }
        };

        public static ItemDef Get(int i) => All[Mathf.Clamp(i, 0, Count - 1)];
        public static ItemDef Get(ItemId id) => All[(int)id];

        // 下标是当前等级：[0] 是解锁要的卡，[1] 是 1→2 级，以此类推。
        // 紫卡要 5 张才解锁。金箱随时可能掉一张，凑满一件仍得多开几箱。
        static readonly int[][] CardNeed =
        {
            new[] { 6, 10, 18, 30, 50 },
            new[] { 4, 6, 10, 16, 26 },
            new[] { 5, 8, 12, 18, 26 }
        };

        static readonly float[] InkMul = { 1f, 1.6f, 2.6f };
        const int InkBase = 190;

        // 满级之后再开到的卡折成墨。
        static readonly int[] SpareInk = { 2, 6, 20 };

        public static int NextCards(ItemDef d, int level)
        {
            if (level >= MaxLevel) return 0;
            return CardNeed[(int)d.Quality][Mathf.Max(0, level)];
        }

        // 解锁只要卡，升级才花墨。每级比上一级贵六成五的底数。
        public static int NextPrice(ItemDef d, int level)
        {
            if (level <= 0 || level >= MaxLevel) return 0;
            float raw = InkBase * InkMul[(int)d.Quality] * (1f + 0.65f * (level - 1));
            return Mathf.RoundToInt(raw / 5f) * 5;
        }

        public static int SpareInkOf(ItemDef d) => SpareInk[(int)d.Quality];

        public static float CooldownAt(ItemDef d, int level)
        {
            int lv = Mathf.Clamp(level, 1, MaxLevel);
            float raw = d.Cooldown * (1f - 0.06f * (lv - 1));
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        // 持续时间一律整秒。文案、结算和演出都读这里。
        // 闹钟定住要看得出停了一拍；饮料盖过十几发炮弹；冰块、辣椒酱要比胶水那阵减速更久。
        public static int HaltTime(int lv) => 3 + lv;
        public static int RageTime(int lv) => 9 + lv;
        public static int SplashTime(int lv) => 7 + lv;
        public static int FrostTime(int lv) => 5 + lv;
        // 胶水要够长，脚底下的减速标记才看得出来。冷却 12 到 16 秒，始终盖过这阵减速。
        public static int SlowTime(int lv) => 4 + lv;

        public static string QualityName(ItemQuality q) =>
            q == ItemQuality.Purple ? "稀有" : q == ItemQuality.Blue ? "高级" : "普通";

        public static Color QualityColor(ItemQuality q) =>
            q == ItemQuality.Purple ? InkTheme.Hex("9B5DE5")
            : q == ItemQuality.Blue ? InkTheme.Hex("3A8EE6")
            : InkTheme.Hex("3DAE5A");

        public static Color QualityDeep(ItemQuality q) =>
            q == ItemQuality.Purple ? InkTheme.Hex("6A3BA8")
            : q == ItemQuality.Blue ? InkTheme.Hex("2563A8")
            : InkTheme.Hex("2A7D40");

        // 伤害类道具一律写成「基础弹伤的几倍」。倍数就放在这里，
        // BattleItems 算伤害和道具页写文案读的是同一份，不会各写一遍再走岔。
        // 按默认弹伤 1.8：鞭炮 3/4/5/6/7，每下都低于同级蓝色冰块（4/5/6/7/8）。
        // 范围小，触发时通常能炸到两个，总伤仍比单发弹弓高一截。
        public static float BurstMul(int lv) => (2f + lv) / ShotMods.DefaultBase;
        public static float SweepMul(int lv) => 1.7f + 1.1f * lv;
        public static float FrostMul(int lv) => 1.7f + 0.55f * lv;
        // 按默认弹伤：1 级 4 点，之后每级 +2（6/8/10/12）。单目标，满级仍低于紫色大扫把的 13。
        public static float SnipeMul(int lv) => (2f + 2f * lv) / ShotMods.DefaultBase;
        public static float SplashMul(int lv) => 1.1f + 0.55f * lv;   // 每秒

        public static int MendCap(int lv) => lv >= MaxLevel ? 2 : 1;

        // shotBase 传玩家当前的基础弹伤（MetaProgress.ShotBase），
        // 文案上仍然显示点数 —— 「7.7 倍弹伤」玩家换算不过来。
        public static string Blurb(ItemDef d, int level, float shotBase)
        {
            int lv = Mathf.Clamp(level <= 0 ? 1 : level, 1, MaxLevel);
            float b = shotBase > 0f ? shotBase : ShotMods.DefaultBase;
            switch (d.Id)
            {
                case ItemId.Burst:
                    return "最前排炸开一圈，" + Pts(b * BurstMul(lv)) + " 点伤害";
                case ItemId.Halt:
                    return "全场敌人定住 " + HaltTime(lv) + " 秒";
                case ItemId.Rage:
                    return RageTime(lv) + " 秒内炮弹伤害 " + (2 + (lv - 1) / 2) + " 倍";
                case ItemId.Sweep:
                    return "全屏 " + Pts(b * SweepMul(lv)) + " 点伤害并击退";
                case ItemId.Splash:
                    return "那一列灼烧 " + SplashTime(lv) + " 秒，每秒 " + Pts(b * SplashMul(lv)) + " 点";
                case ItemId.Mend:
                    return "基地回 " + (1 + (lv - 1) / 2) + " 血" + (lv >= MaxLevel ? "，每局两次" : "，每局一次");
                case ItemId.Frost:
                    return "全场 " + Pts(b * FrostMul(lv)) + " 点冰伤，并减速 " + FrostTime(lv) + " 秒";
                case ItemId.Slow:
                    return "全场减速 " + SlowTime(lv) + " 秒";
                case ItemId.Snipe:
                    return "最前一个 " + Pts(b * SnipeMul(lv)) + " 点";
                default:
                    return d.Desc;
            }
        }

        static string Pts(float v) => Mathf.Max(1, Mathf.RoundToInt(v)).ToString();
    }

    public struct SkinDef
    {
        public string Name;
        public Color Tint;
        public int Price;
        public SkinWay Way;
        public int Chapter;      // Way 是 Ink 时，通关这章（从 0 数）才能买
        public string Note;
        public string Shot;      // 不带字时打出去的炮弹叫什么，展台上写给玩家看
        public string Key;       // 美术名：Ui/ico_skin_<Key>、cannon_<Key>
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

    public enum SkinWay
    {
        Free,     // 一开始就有
        Ink,      // 到了章节门槛，花墨买
        Ad,       // 看一次广告
        Check,    // 第一轮七日签到的第 7 天
        Event     // 活动送，活动还没接上，现在领不了
    }

    // 小钢炮白给。糖果炮看广告，机甲炮第一周签到，黄金炮通关第二章后花墨买。
    // 赤焰、福袋先挂「活动获取」，各带一条专属词条，只在装着时生效。
    public static class SkinCatalog
    {
        public const int Count = 6;
        public const int Flame = 4;
        public const int Lucky = 5;

        static readonly SkinDef[] All =
        {
            new SkinDef
            {
                Name = "小钢炮", Key = "plain", Tint = Color.clear, Price = 0,
                Note = "老木架扛着铁炮管，皮实耐打。", Shot = "铁球弹"
            },
            new SkinDef
            {
                Name = "糖果炮", Key = "cinnabar", Price = 0, Way = SkinWay.Ad,
                Tint = InkTheme.Hex("FF5C8A"), Note = "礼物盒上架着拐杖糖，轮子是棒棒糖。", Shot = "糖果弹"
            },
            new SkinDef
            {
                Name = "机甲炮", Key = "celadon", Price = 0, Way = SkinWay.Check,
                Tint = InkTheme.Hex("3FA9F5"), Note = "履带小车扛着机甲炮管，打得更狠。", Shot = "能量弹", DamageAdd = 1f
            },
            new SkinDef
            {
                Name = "黄金炮", Key = "gilt", Price = 365, Way = SkinWay.Ink, Chapter = 1,
                Tint = InkTheme.Hex("E8B43A"), Note = "坐在宝箱上，金币多到溢出来。", Shot = "金光弹", GoldAdd = 20
            },
            new SkinDef
            {
                Name = "赤焰", Key = "flame", Price = 0, Way = SkinWay.Event,
                Tint = InkTheme.Hex("E2552B"), Note = "龙口衔火，一炮一颗流星。", Shot = "火焰流星弹"
            },
            new SkinDef
            {
                Name = "福袋", Key = "lucky", Price = 0, Way = SkinWay.Event,
                Tint = InkTheme.Hex("D9A23A"), Note = "招财猫抱着炮，打出去全是铜钱。", Shot = "铜钱弹"
            }
        };

        public static SkinDef Get(int i) => All[Mathf.Clamp(i, 0, Count - 1)];

        // 这款皮肤自带的专属词条下标，没有返回 -1。
        public static int LineOf(int skin)
        {
            for (int i = 0; i < ForgeCatalog.LineCount; i++)
            {
                ForgeDef d = ForgeCatalog.Get(i);
                if (d.Exclusive && d.Skin == skin) return i;
            }
            return -1;
        }

        public static string PerkOf(int skin)
        {
            int line = LineOf(skin);
            return line >= 0 ? "专属词条 · " + ForgeCatalog.Get(line).Name : Get(skin).Perk;
        }

        // 展台上没拿到时，写在炮身上的那句。
        public static string LockText(SkinDef d)
        {
            switch (d.Way)
            {
                case SkinWay.Ad: return "看广告获取";
                case SkinWay.Check: return "七日签到获得";
                case SkinWay.Event: return "活动获取";
                case SkinWay.Ink: return "通关第" + ChapterNo(d.Chapter) + "章开放";
                default: return "";
            }
        }

        static string ChapterNo(int chapter)
        {
            const string n = "一二三四五六七八";
            if (chapter < 0 || chapter >= n.Length) return (chapter + 1).ToString();
            return n[chapter].ToString();
        }

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
