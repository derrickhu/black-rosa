using UnityEngine;

namespace InkLine
{
    public enum ItemId { Burst, Halt, Rage, Sweep, Splash, Mend, Frost, Slow, Snipe, Dart }

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
        public const int Count = 10;
        public const int MaxLevel = 8;

        // 1 级冷却按品质分档。每级大约短 5%，冷却短的至少再短 1 秒，所以每一级都变。
        // 档与档之间留了空：满级的普通仍短于 1 级高级，满级的高级仍短于 1 级稀有。
        static readonly ItemDef[] All =
        {
            new ItemDef
            {
                Id = ItemId.Burst, Name = "鞭炮", Desc = "最前排炸开一圈",
                When = "最前排有 2 个敌人", Quality = ItemQuality.Green, Cooldown = 25f, Tint = InkTheme.Explode
            },
            new ItemDef
            {
                Id = ItemId.Halt, Name = "闹钟", Desc = "全场敌人定住",
                When = "敌人逼近或场上 5 个以上", Quality = ItemQuality.Blue, Cooldown = 42f, Tint = InkTheme.Word
            },
            new ItemDef
            {
                Id = ItemId.Rage, Name = "能量饮料", Desc = "一阵子炮弹伤害翻倍",
                When = "场上 3 个以上或首领在场", Quality = ItemQuality.Blue, Cooldown = 48f, Tint = InkTheme.Fire
            },
            new ItemDef
            {
                Id = ItemId.Sweep, Name = "大扫把", Desc = "全屏伤害并击退",
                When = "场上 6 个以上或敌人逼近", Quality = ItemQuality.Purple, Cooldown = 78f, Tint = InkTheme.Ink
            },
            new ItemDef
            {
                Id = ItemId.Splash, Name = "辣椒酱", Desc = "最密的三列一起灼烧",
                When = "同一列有 3 个敌人", Quality = ItemQuality.Purple, Cooldown = 69f, Tint = InkTheme.Poison
            },
            new ItemDef
            {
                Id = ItemId.Mend, Name = "急救包", Desc = "基地回血",
                When = "基地掉血后", Quality = ItemQuality.Purple, Cooldown = 90f, Tint = InkTheme.Heart
            },
            new ItemDef
            {
                Id = ItemId.Frost, Name = "冰块", Desc = "全场冰伤并减速",
                When = "场上 4 个以上", Quality = ItemQuality.Blue, Cooldown = 42f, Tint = InkTheme.Ice
            },
            new ItemDef
            {
                Id = ItemId.Slow, Name = "胶水", Desc = "全场大幅减速",
                When = "有敌人过了半场", Quality = ItemQuality.Green, Cooldown = 24f, Tint = InkTheme.Water
            },
            new ItemDef
            {
                Id = ItemId.Snipe, Name = "弹弓", Desc = "最前一个吃一记重击",
                When = "场上有敌人", Quality = ItemQuality.Green, Cooldown = 18f, Tint = InkTheme.Thunder
            },
            new ItemDef
            {
                Id = ItemId.Dart, Name = "回旋镖", Desc = "一颗光球满场乱飞",
                When = "场上有敌人", Quality = ItemQuality.Purple, Cooldown = 75f, Tint = InkTheme.Hex("FF3EC8")
            }
        };

        public static ItemDef Get(int i) => All[Mathf.Clamp(i, 0, Count - 1)];
        public static ItemDef Get(ItemId id) => All[(int)id];

        // 下标是当前等级：[0] 是解锁要的卡，[1] 是 1→2 级，以此类推。
        // 同一品质越高要得越多。同一级越稀有要得越少，因为卡掉得少。
        static readonly int[][] CardNeed =
        {
            new[] { 6, 10, 18, 30, 50, 54, 58, 64 },
            new[] { 5, 8, 12, 18, 28, 32, 36, 42 },
            new[] { 5, 8, 12, 18, 26, 28, 32, 36 }
        };

        // 普通 / 高级 / 稀有。1～5 级同级大约 1 : 3 : 6，品质越高越贵。
        // 普通 1→5：280 / 530 / 785 / 1035。稀有：1680 / 3190 / 4705 / 6215。
        // 6～8 级按 4→5 那一档的 1.6 / 2.3 / 3.1 倍。后面的玩家没有早期那批余墨，不能按旧档的库存定价。
        // 普通 1660 / 2385 / 3210。稀有 9945 / 14295 / 19270。
        static readonly float[] InkMul = { 1f, 3f, 6f };
        const int InkBase = 280;
        const float InkGrow = 0.9f;
        const int InkCurve = 5;
        static readonly float[] InkTail = { 1.6f, 2.3f, 3.1f };

        // 满级之后再开到的卡折成墨。
        static readonly int[] SpareInk = { 2, 6, 20 };

        public static int NextCards(ItemDef d, int level)
        {
            if (level >= MaxLevel) return 0;
            return CardNeed[(int)d.Quality][Mathf.Max(0, level)];
        }

        // 解锁只要卡，升级才花墨。1～5 级每级在底数上再加九成，再乘品质。
        // 6 级起改乘 5 级价的固定倍数，品质倍率不变。
        public static int NextPrice(ItemDef d, int level)
        {
            if (level <= 0 || level >= MaxLevel) return 0;
            float step = InkBase * InkMul[(int)d.Quality];
            float raw = level < InkCurve
                ? step * (1f + InkGrow * (level - 1))
                : step * (1f + InkGrow * 3f) * InkTail[level - InkCurve];
            return Mathf.RoundToInt(raw / 5f) * 5;
        }

        public static int SpareInkOf(ItemDef d) => SpareInk[(int)d.Quality];

        public static float CooldownAt(ItemDef d, int level)
        {
            int lv = Mathf.Clamp(level, 1, MaxLevel);
            int opened = Mathf.RoundToInt(d.Cooldown);
            int byPct = Mathf.RoundToInt(d.Cooldown * (1f - 0.05f * (lv - 1)));
            int byStep = opened - (lv - 1);
            return Mathf.Max(1, Mathf.Min(byPct, byStep));
        }

        // 持续时间一律整秒。文案、结算和演出都读这里。
        // 闹钟要盖住一整波往前涌；胶水减速比冰块狠，时间也更长。
        public static int HaltTime(int lv) => 6 + lv;
        public static int RageTime(int lv) => 10 + lv;
        public static int SplashTime(int lv) => 8 + lv;
        public static int FrostTime(int lv) => 5 + lv;
        // 胶水要够长，脚底下的水纹才看得出来。冷却 24 秒起，满级 15 秒仍盖过这阵减速。
        public static int SlowTime(int lv) => 7 + lv;
        // 光球穿过敌人，总伤靠多飞几个来回。冷却 75 秒起，满级 16 秒仍盖得过。
        public static int DartTime(int lv) => 8 + lv;
        // 每级加 0.5 倍。1 级 2 倍，5 级 4 倍，和原来的 5 级一样；满级 5.5 倍、18 秒。
        public static float RageMul(int lv) => 1.5f + 0.5f * Mathf.Clamp(lv, 1, MaxLevel);

        // 回血和次数错开涨，每一级文案都变。2 级单次停在 1，先多给一次。
        static readonly int[] MendHeals = { 1, 1, 2, 3, 3, 4, 5, 6 };
        static readonly int[] MendCaps = { 1, 2, 2, 2, 3, 3, 3, 3 };

        public static int MendHeal(int lv) => MendHeals[Mathf.Clamp(lv, 1, MaxLevel) - 1];

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

        // 伤害按默认弹伤 1.8 写成点数，再除回去得到倍数。锻造把弹伤抬高以后，点数跟着涨。
        // 前 5 级就是原来的点数。6 级起按同一档往上加，满级明显高于原来的 5 级。
        // 鞭炮 / 飞镖：4 5 6 8 9 10 11 13。扫把：6 9 11 14 16 19 21 24。弹弓低 1 点。
        const float Effect = 1.25f;
        public static float BurstMul(int lv) => (2f + Mathf.Clamp(lv, 1, MaxLevel)) * Effect / ShotMods.DefaultBase;
        public static float SweepMul(int lv) => (1.7f + 1.1f * Mathf.Clamp(lv, 1, MaxLevel)) * Effect;
        public static float FrostMul(int lv) => (1.46f + 0.47f * Mathf.Clamp(lv, 1, MaxLevel)) * Effect;
        // 冰块只是变慢。1 级 72%，每级少 4 个点，5 级 56%，满级 44%。
        public static float FrostFactor(int lv) => 0.72f - 0.04f * (Mathf.Clamp(lv, 1, MaxLevel) - 1);
        // 胶水比冰块狠。1 级 35%，每级少 4 个点，5 级 19%，满级 10%，仍走得动。
        public static float SlowFactor(int lv) =>
            Mathf.Max(0.10f, 0.35f - 0.04f * (Mathf.Clamp(lv, 1, MaxLevel) - 1));
        public static float SnipeMul(int lv) => (2f + 2f * Mathf.Clamp(lv, 1, MaxLevel)) * Effect / ShotMods.DefaultBase;
        // 浇在最密的一列，再盖住左右。半宽 1.5 格收到邻列外沿，再远的一列碰不到。
        public const float SplashHalf = 1.5f;
        // 每秒。默认弹伤下 8 / 10 / 12 / 14 / 16 / 18 / 20 / 22。
        public static float SplashMul(int lv) => (2.67f + 0.89f * Mathf.Clamp(lv, 1, MaxLevel)) * Effect;
        public static float DartMul(int lv) => BurstMul(lv);

        public static int MendCap(int lv) => MendCaps[Mathf.Clamp(lv, 1, MaxLevel) - 1];

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
                    return RageTime(lv) + " 秒内炮弹伤害 " + MulText(RageMul(lv)) + " 倍";
                case ItemId.Sweep:
                    return "全屏 " + Pts(b * SweepMul(lv)) + " 点伤害并击退";
                case ItemId.Splash:
                    return "最密的三列灼烧 " + SplashTime(lv) + " 秒，每秒 " + Pts(b * SplashMul(lv)) + " 点";
                case ItemId.Mend:
                    return "基地回 " + MendHeal(lv) + " 血，每局" + Times(MendCap(lv)) + "次";
                case ItemId.Frost:
                    return "全场 " + Pts(b * FrostMul(lv)) + " 点冰伤，移速降到 " + Pct(FrostFactor(lv))
                           + "，持续 " + FrostTime(lv) + " 秒";
                case ItemId.Slow:
                    return "全场移速降到 " + Pct(SlowFactor(lv)) + "，持续 " + SlowTime(lv) + " 秒";
                case ItemId.Snipe:
                    return "最前一个 " + Pts(b * SnipeMul(lv)) + " 点";
                case ItemId.Dart:
                    return "光球飞 " + DartTime(lv) + " 秒，碰到的敌人 " + Pts(b * DartMul(lv)) + " 点";
                default:
                    return d.Desc;
            }
        }

        static string Pts(float v) => Mathf.Max(1, Mathf.RoundToInt(v)).ToString();
        static string Pct(float factor) => Mathf.RoundToInt(factor * 100f) + "%";
        static string MulText(float m)
        {
            int n = Mathf.RoundToInt(m * 2f);
            return n % 2 == 0 ? (n / 2).ToString() : (n / 2) + ".5";
        }

        static string Times(int n) => n <= 1 ? "一" : n == 2 ? "两" : "三";
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
        public SkinRarity Rarity;
        public int Shards;       // Way 是 Shard 时，集满这么多碎片就到手
        // 加在单发默认伤害上，之后炮台伤害、强攻照旧乘上去。
        public float DamageAdd;
        // 加在开局金币上，之后金币改装照旧再加。
        public int GoldAdd;
        // 开火更快的比例。0.06 是间隔缩短 6%，锻射击速线照旧再乘。
        public float RateAdd;
        // 装着就生效的炮弹加成。有这句时，和上面的数值加成写在一起。
        public string Bonus;

        public string Perk
        {
            get
            {
                string stat = "";
                if (DamageAdd > 0.01f) stat = "炮弹伤害 +" + Mathf.RoundToInt(DamageAdd);
                else if (GoldAdd > 0) stat = "开局金币 +" + GoldAdd;
                else if (RateAdd > 0.001f) stat = "射速 +" + Mathf.RoundToInt(RateAdd * 100f) + "%";
                if (stat.Length > 0 && !string.IsNullOrEmpty(Bonus)) return stat + "，" + Bonus;
                if (stat.Length > 0) return stat;
                if (!string.IsNullOrEmpty(Bonus)) return Bonus;
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
        Event,    // 活动送
        Shard     // 炮台宝箱掉碎片，集满到手
    }

    public enum SkinRarity { Common, Advanced, Rare }

    // 钢珠、糖果是普通。机甲、黄金、霜晶、海浪、闪电是高级。赤焰、福袋是稀有。
    // 黄金、霜晶、赤焰、海浪、闪电靠炮台宝箱的碎片集齐。机甲第一周签到，福袋活动送。
    // 赤焰、福袋各带一条专属词条，只在装着时生效。
    // 海浪打中的怪会慢一小会儿，并溅到旁边。闪电射速快一截，命中后再电一只。
    // 每加一门皮肤，没吃字时的裸弹都要有自己的样子：色球写在 InkShot.NakedOf，
    // 专属弹体贴图写在 InkShot.SkinShot（Resources/Art/shot_<key>.png）。
    public static class SkinCatalog
    {
        public const int Count = 9;
        public const int Gilt = 3;
        public const int Flame = 4;
        public const int Lucky = 5;
        public const int Frost = 6;
        public const int Tide = 7;
        public const int Bolt = 8;
        // 海浪：打中的那只移速降到这个比例，持续这么久。比一星「水」更短。
        public const float TideSlow = 0.85f;
        public const float TideSlowTime = 0.8f;
        // 闪电：开火间隔缩短的比例。锻射击速是每级 8%，这里只给一点点。
        public const float BoltRate = 0.06f;

        static readonly SkinDef[] All =
        {
            new SkinDef
            {
                Name = "钢珠", Key = "plain", Tint = Color.clear, Price = 0,
                Note = "老木架扛着铁炮管，皮实耐打。", Shot = "铁球弹"
            },
            new SkinDef
            {
                Name = "糖果", Key = "cinnabar", Price = 0, Way = SkinWay.Ad,
                Tint = InkTheme.Hex("FF5C8A"), Note = "礼物盒上架着拐杖糖，轮子是棒棒糖。", Shot = "糖果弹"
            },
            new SkinDef
            {
                Name = "机甲", Key = "celadon", Price = 0, Way = SkinWay.Check,
                Rarity = SkinRarity.Advanced,
                Tint = InkTheme.Hex("3FA9F5"), Note = "履带小车扛着机甲炮管，打得更狠。", Shot = "能量弹", DamageAdd = 1f
            },
            new SkinDef
            {
                Name = "黄金", Key = "gilt", Price = 0, Way = SkinWay.Shard,
                Rarity = SkinRarity.Advanced, Shards = 6,
                Tint = InkTheme.Hex("E8B43A"), Note = "坐在宝箱上，金币多到溢出来。", Shot = "金光弹", GoldAdd = 20
            },
            new SkinDef
            {
                Name = "赤焰", Key = "flame", Price = 0, Way = SkinWay.Shard,
                Rarity = SkinRarity.Rare, Shards = 8,
                Tint = InkTheme.Hex("E2552B"), Note = "龙口衔火，一炮一颗流星。", Shot = "火焰流星弹"
            },
            new SkinDef
            {
                Name = "福袋", Key = "lucky", Price = 0, Way = SkinWay.Event,
                Rarity = SkinRarity.Rare,
                Tint = InkTheme.Hex("D9A23A"), Note = "招财猫抱着炮，打出去全是铜钱。", Shot = "铜钱弹"
            },
            new SkinDef
            {
                Name = "霜晶", Key = "frost", Price = 0, Way = SkinWay.Shard,
                Rarity = SkinRarity.Advanced, Shards = 6,
                Tint = InkTheme.Hex("7ED0E8"), Note = "炮口结着冰晶，打出去带着霜。", Shot = "冰晶弹", DamageAdd = 2f
            },
            new SkinDef
            {
                Name = "海浪", Key = "tide", Price = 0, Way = SkinWay.Shard,
                Rarity = SkinRarity.Advanced, Shards = 6,
                Tint = InkTheme.Hex("3CB4F5"), Note = "炮管是泳池那种蓝，坐在一朵卷起来的浪上。",
                Shot = "水滴弹", Bonus = "打中的怪慢 0.8 秒，并溅到旁边"
            },
            new SkinDef
            {
                Name = "闪电", Key = "bolt", Price = 0, Way = SkinWay.Shard,
                Rarity = SkinRarity.Advanced, Shards = 6,
                Tint = InkTheme.Hex("4C6BFF"), Note = "炮管是蓝宝石那种蓝，身上一道闪电，坐在一小团雷云上。",
                Shot = "电光弹", RateAdd = BoltRate, Bonus = "命中后再电最近一只"
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
                case SkinWay.Shard: return "集满碎片获得";
                case SkinWay.Ink: return "通关第" + ChapterNo(d.Chapter) + "章开放";
                default: return "";
            }
        }

        static string ChapterNo(int chapter)
        {
            const string n = "一二三四五六七八九十";
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

        public static string RankName(SkinRarity r)
        {
            if (r == SkinRarity.Rare) return "稀有";
            if (r == SkinRarity.Advanced) return "高级";
            return "普通";
        }

        // 跟道具卡同一套：普通绿，高级蓝，稀有紫。
        public static Color RankColor(SkinRarity r) =>
            ItemCatalog.QualityColor(QualityOf(r));

        public static Color RankDeep(SkinRarity r) =>
            ItemCatalog.QualityDeep(QualityOf(r));

        public static ItemQuality QualityOf(SkinRarity r) =>
            r == SkinRarity.Rare ? ItemQuality.Purple
            : r == SkinRarity.Advanced ? ItemQuality.Blue
            : ItemQuality.Green;

        // 已经有这门炮时，多出来的碎片按这个价换成墨。
        public static int ShardInk(SkinRarity r) => r == SkinRarity.Rare ? 40 : 25;
    }
}
