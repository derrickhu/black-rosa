using UnityEngine;

namespace InkLine
{
    // 词条的效果种类。存档下标不看这个，看词条在 ForgeCatalog.Lines 里的位置。
    // 通用词条的位置和枚举值一一对应；专属词条排在通用后面，效果可以和通用的重名。
    public enum ForgeLine { Damage, Emitters, FireRate, StartGold, BaseHp, Crit, GoldGain, DraftCut, ItemCd }

    // 升级用什么付。通用词条一律用墨；Token 留给皮肤专属词条，由活动发放。
    public enum ForgeCoin { Ink, Token }

    // 一条词条。Cost[i] 是从 i 级升到 i+1 级的价，Gate[i] 是那一级要求的已通关关数。
    public struct ForgeDef
    {
        public ForgeLine Line;
        public string Name;
        public string Stat;      // 加成项的名字，「当前 → 下一级」那行开头
        // 每级加多少，按展示口径写：百分比线写 16 表示每级 16%，其余写绝对值。
        // Stats()、Value() 和 ForgeCatalog.Step() 全读它，调一条线只改这一个数。
        public float Amount;
        public string Icon;      // Resources/Art/Ui/ico_<Icon>.png
        public int[] Cost;
        public int[] Gate;
        public int Reveal;       // 通关这么多关才在炮台页露面。0 = 开局就有
        public string LockNote;  // 门槛超出总关数时改显示这句
        public string Brief;     // 过关解锁卡上那句「这条词条干什么」
        // 专属词条只在拥有对应皮肤时露面，只在装着那款皮肤时生效。
        public bool Exclusive;
        public int Skin;
        public ForgeCoin Coin;

        public int MaxLevel => Cost.Length;
    }

    // 局外升级算出来的开局条件。BattleWorld 只认这个结构，不认等级。
    public struct ForgeStats
    {
        public float DamageMul;
        public float IntervalMul;
        public int Emitters;
        public int StartGold;
        public int BaseHp;
        public float CritChance;
        public float GoldMul;
        public float DraftMul;     // 改装价的倍率
        public float ItemCdMul;    // 道具冷却的倍率

        public static ForgeStats Default => new ForgeStats
        {
            DamageMul = 1f,
            IntervalMul = 1f,
            Emitters = 2,
            StartGold = 6,
            BaseHp = GameConstants.BaseHp,
            CritChance = 0f,
            GoldMul = 1f,
            DraftMul = 1f,
            ItemCdMul = 1f
        };
    }

    public static class ForgeCatalog
    {
        // 暴击打几倍。改这里要同步 check_stages 的战力裕度（它从这一行读）。
        public const float CritMul = 2f;

        // 通用词条：伤害打完第一关才亮，射速在第一章后段，打完第一章给多一门炮；
        // 第二章是开局金币和基地生命。专属词条跟着皮肤走。
        // 已经点过的线永远留着，免得存档玩家看见自己买过的东西消失。
        // 门槛铺满 72 关：每章都有几级可升，最后一级落在第七、八章。
        // 前两章的价不动，刚解锁买得动。第三章起按这一章的首通进账定价，略贵一截：
        // 正常打完，刚开的级点得动，手里剩不多，不够再点还没开的级。
        // 同一条线逐级只涨不降。多一门炮、加一滴血比同章的伤害贵。
        // 门槛过 45 关的级整体 ×1.6：线上存档显示双倍墨广告看全了，首通一遍就够买满，精英没人打。
        static readonly ForgeDef[] Lines =
        {
            // 伤害线 18 级，每级 +25%，满级 +450%。前 12 级门槛铺到第 69 关；
            // 第二幕再加 6 级：13、14 级在第九、十章，15 级在第十一章，16、17 级在第十二章，最后一级仍等以后的章节。
            // 第二章前期最多三级，+75% 才压得住最后一波；百分比乘在整发炮弹上。
            new ForgeDef
            {
                Line = ForgeLine.Damage, Name = "伤害", Stat = "炮弹伤害", Amount = 25f,
                Icon = "damage", Reveal = 1, Brief = "所有炮弹的伤害一起涨", LockNote = "后续章节开放",
                Cost = new[] { 120, 150, 180, 210, 350, 430, 550, 660, 1280, 1470, 1700, 1920, 2180, 2430, 2750, 3100, 3490, 3900 },
                Gate = new[] { 1, 2, 6, 12, 20, 28, 36, 44, 52, 58, 64, 69, 76, 84, 98, 104, 108, 140 }
            },
            new ForgeDef
            {
                Line = ForgeLine.Emitters, Name = "炮台数", Stat = "炮台", Amount = 1f,
                Icon = "guns", Reveal = 9, Brief = "开局多架一门炮",
                Cost = new[] { 270, 820 },
                Gate = new[] { 9, 40 }
            },
            new ForgeDef
            {
                Line = ForgeLine.FireRate, Name = "射速", Stat = "开火间隔", Amount = 8f,
                Icon = "rate", Reveal = 6, Brief = "每门炮开火更快",
                Cost = new[] { 140, 170, 310, 440, 990 },
                Gate = new[] { 6, 9, 24, 45, 62 }
            },
            // 开局金币 8 级，每级 +8，满级开局 70。门槛铺到第七、八章，
            // 后期一张改装要几十金币，开局要够先拿下一张。
            new ForgeDef
            {
                Line = ForgeLine.StartGold, Name = "开局金币", Stat = "开局金币", Amount = 8f,
                Icon = "gold", Reveal = 12, Brief = "开局多带金币，先手改装",
                Cost = new[] { 150, 170, 300, 400, 830, 1060, 1310, 1570 },
                Gate = new[] { 12, 18, 26, 36, 48, 56, 62, 68 }
            },
            new ForgeDef
            {
                Line = ForgeLine.BaseHp, Name = "基地生命", Stat = "基地生命", Amount = 1f,
                Icon = "hp", Reveal = 17, Brief = "基地多扛一次突破",
                Cost = new[] { 200, 480, 1730 },
                Gate = new[] { 17, 27, 50 }
            },
            // ---- 皮肤专属：拥有皮肤才露面，装着才生效。Reveal 写皮肤的开放关，和 Gate[0] 对齐 ----
            // 赤焰 · 暴击 5 级，每级 +4%，满级 20% 的炮弹打双倍。跳出来的大红字是最直接的爽点。
            new ForgeDef
            {
                Line = ForgeLine.Crit, Name = "暴击", Stat = "暴击率", Amount = 4f,
                Icon = "crit", Reveal = 12, Brief = "炮弹有几率打出双倍伤害", Exclusive = true, Skin = SkinCatalog.Flame,
                Cost = new[] { 160, 280, 400, 860, 1120 },
                Gate = new[] { 12, 24, 38, 52, 64 }
            },
            // 福袋 · 金币收益：只加局内金币，不碰墨 —— 墨是局外总账，check_stages 的墨价比按它算。
            new ForgeDef
            {
                Line = ForgeLine.GoldGain, Name = "金币收益", Stat = "局内金币", Amount = 6f,
                Icon = "goldgain", Reveal = 15, Brief = "打怪掉的金币更多", Exclusive = true, Skin = SkinCatalog.Lucky,
                Cost = new[] { 170, 340, 460, 990, 1280 },
                Gate = new[] { 15, 30, 44, 58, 68 }
            },
            // ---- 第二幕的通用词条。存档按位置记等级，只能接在最后 ----
            // 改装折扣 5 级，每级 -3%，满级改装便宜 15%。第二幕格子多，钱更紧。
            new ForgeDef
            {
                Line = ForgeLine.DraftCut, Name = "改装折扣", Stat = "改装价", Amount = 3f,
                Icon = "draftcut", Reveal = 72, Brief = "局内每次改装更便宜",
                Cost = new[] { 770, 900, 1060, 1250, 1470 },
                Gate = new[] { 72, 76, 80, 84, 88 }
            },
            // 道具冷却 5 级，每级 -4%，满级冷却短 20%。
            new ForgeDef
            {
                Line = ForgeLine.ItemCd, Name = "道具冷却", Stat = "道具冷却", Amount = 4f,
                Icon = "itemcd", Reveal = 74, Brief = "道具转好得更快",
                Cost = new[] { 670, 800, 930, 1090, 1280 },
                Gate = new[] { 74, 78, 82, 86, 90 }
            }
            // 以后的专属词条接在这里。活动货币升级的写 Coin = ForgeCoin.Token。
        };

        public static readonly int LineCount = Lines.Length;

        public static ForgeDef Get(int i) => Lines[Mathf.Clamp(i, 0, LineCount - 1)];

        // 通用词条：通关关数到了露面门槛，或者已经点过，就在炮台页出现。
        // 专属词条另由 MetaProgress.ForgeShown 按皮肤判断。
        public static bool Exposed(int line, int cleared, int level)
        {
            ForgeDef d = Get(line);
            if (d.Exclusive) return false;
            return level > 0 || cleared >= d.Reveal;
        }

        // 下一条要解锁的通用词条。炮台页只挂这一张预告，不把后面全摊开。
        public static int NextLocked(int cleared, int[] levels)
        {
            int best = -1, bestR = int.MaxValue;
            for (int i = 0; i < LineCount; i++)
            {
                if (Get(i).Exclusive) continue;
                int lv = levels != null && i < levels.Length ? levels[i] : 0;
                if (Exposed(i, cleared, lv)) continue;
                int r = Get(i).Reveal;
                if (r < bestR) { bestR = r; best = i; }
            }
            return best;
        }

        // 炮台页的排列：专属在最上，通用按解锁先后。
        public static int Rank(int line)
        {
            ForgeDef d = Get(line);
            return (d.Exclusive ? 0 : 1000) + d.Reveal * 10 + line;
        }

        public static int MaxLevel(int i) => Get(i).MaxLevel;

        // 从 level 级升到 level+1 级要多少。已满级返回 0。币种看 Get(i).Coin。
        public static int Cost(int i, int level)
        {
            ForgeDef d = Get(i);
            return level < 0 || level >= d.Cost.Length ? 0 : d.Cost[level];
        }

        public static int Gate(int i, int level)
        {
            ForgeDef d = Get(i);
            return level < 0 || level >= d.Gate.Length ? 0 : d.Gate[level];
        }

        public static string CoinIcon(ForgeCoin coin) => coin == ForgeCoin.Token ? "token" : "ink";

        // 升到 level 级之后这条线一共加了多少，卡面「当前 → 下一级」用。
        public static string Value(int i, int level)
        {
            ForgeDef d = Get(i);
            int lv = Mathf.Max(0, level);
            string n = Num(d.Amount * lv);
            switch (d.Line)
            {
                case ForgeLine.Damage: return "+" + n + "%";
                case ForgeLine.FireRate: return "-" + n + "%";
                case ForgeLine.DraftCut: return "-" + n + "%";
                case ForgeLine.ItemCd: return "-" + n + "%";
                case ForgeLine.Crit: return n + "%";
                case ForgeLine.GoldGain: return "+" + n + "%";
                case ForgeLine.StartGold: return "+" + n;
                case ForgeLine.BaseHp: return "+" + n;
                case ForgeLine.Emitters: return (ForgeStats.Default.Emitters + lv) + " 门";
                default: return n;
            }
        }

        // 卡面和失败页建议里那句「每级加多少」。
        public static string Step(int i)
        {
            ForgeDef d = Get(i);
            string n = Num(d.Amount);
            switch (d.Line)
            {
                case ForgeLine.Damage: return d.Stat + " +" + n + "%";
                case ForgeLine.FireRate: return d.Stat + " -" + n + "%";
                case ForgeLine.DraftCut: return d.Stat + " -" + n + "%";
                case ForgeLine.ItemCd: return d.Stat + " -" + n + "%";
                case ForgeLine.Crit: return d.Stat + " +" + n + "%";
                case ForgeLine.GoldGain: return d.Stat + " +" + n + "%";
                case ForgeLine.Emitters: return "多一门炮";
                default: return d.Stat + " +" + n;
            }
        }

        // 整数就不拖小数点，方便以后把每级幅度调成 7.5 这种。
        static string Num(float v) =>
            Mathf.Approximately(v, Mathf.Round(v))
                ? Mathf.RoundToInt(v).ToString()
                : v.ToString("0.#");

        // skin 是当前装着的皮肤，专属词条只认它。
        public static ForgeStats Stats(int[] levels, int skin)
        {
            ForgeStats s = ForgeStats.Default;
            if (levels == null) return s;
            float dmg = 0f, rate = 0f, crit = 0f, gain = 0f, cut = 0f, itemCd = 0f;
            int gun = 0, gold = 0, hp = 0;
            for (int i = 0; i < LineCount && i < levels.Length; i++)
            {
                ForgeDef d = Lines[i];
                if (d.Exclusive && d.Skin != skin) continue;
                int lv = Mathf.Clamp(levels[i], 0, d.MaxLevel);
                if (lv <= 0) continue;
                switch (d.Line)
                {
                    case ForgeLine.Damage: dmg += d.Amount * lv; break;
                    case ForgeLine.FireRate: rate += d.Amount * lv; break;
                    case ForgeLine.Crit: crit += d.Amount * lv; break;
                    case ForgeLine.GoldGain: gain += d.Amount * lv; break;
                    case ForgeLine.DraftCut: cut += d.Amount * lv; break;
                    case ForgeLine.ItemCd: itemCd += d.Amount * lv; break;
                    case ForgeLine.Emitters: gun += Mathf.RoundToInt(d.Amount) * lv; break;
                    case ForgeLine.StartGold: gold += Mathf.RoundToInt(d.Amount) * lv; break;
                    case ForgeLine.BaseHp: hp += Mathf.RoundToInt(d.Amount) * lv; break;
                }
            }
            s.DamageMul = 1f + dmg * 0.01f;
            s.IntervalMul = Mathf.Max(0.2f, 1f - rate * 0.01f);
            s.Emitters = Mathf.Clamp(ForgeStats.Default.Emitters + gun, 1, GameConstants.MaxEmitters);
            s.StartGold = ForgeStats.Default.StartGold + gold;
            s.BaseHp = GameConstants.BaseHp + hp;
            s.CritChance = Mathf.Clamp01(crit * 0.01f);
            s.GoldMul = 1f + gain * 0.01f;
            s.DraftMul = Mathf.Clamp(1f - cut * 0.01f, 0.5f, 1f);
            s.ItemCdMul = Mathf.Clamp(1f - itemCd * 0.01f, 0.5f, 1f);
            return s;
        }
    }
}
