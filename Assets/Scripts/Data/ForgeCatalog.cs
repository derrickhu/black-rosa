using UnityEngine;

namespace InkLine
{
    public enum ForgeLine { Damage, Emitters, FireRate, StartGold, BaseHp }

    // 一条升级线。Cost[i] 是从 i 级升到 i+1 级的价，Gate[i] 是那一级要求的已通关关数。
    public struct ForgeDef
    {
        public ForgeLine Line;
        public string Name;
        public string Stat;      // 加成项的名字，「当前 → 下一级」那行开头
        // 每级加多少，按展示口径写：百分比线写 8 表示 8%，其余写绝对值。
        // Stats()、Value() 和 ForgeCatalog.Step() 全读它，调一条线只改这一个数。
        public float Amount;
        public string Icon;      // Resources/Art/Ui/ico_<Icon>.png
        public int[] Cost;
        public int[] Gate;
        public int Reveal;       // 通关这么多关才在加成页露面。0 = 开局就有
        public string LockNote;  // 门槛超出总关数时改显示这句

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

        public static ForgeStats Default => new ForgeStats
        {
            DamageMul = 1f,
            IntervalMul = 1f,
            Emitters = 2,
            StartGold = 6,
            BaseHp = GameConstants.BaseHp
        };
    }

    public static class ForgeCatalog
    {
        public const int LineCount = 5;

        // 开局只亮伤害。别的线按通关关数陆续露面，不能五张一起铺开。
        // 已经点过的线永远留着，免得存档玩家看见自己买过的东西消失。
        // 门槛铺满 72 关：每章都有几级可升，最后一级落在第七、八章。
        // 价 = 门槛那一关的墨 ×4 再加 80，同一条线每级再贵一截。
        // ×4 是「打两关」再留一倍给签到、游戏圈和以后的渠道；+80 把早期垫到一天奖励买不满一级。
        // 多一门炮、加一滴血比加伤害贵，这两条单独上浮。
        static readonly ForgeDef[] Lines =
        {
            // 伤害线 12 级，门槛一路铺到第 69 关。八级封顶时玩家第六章就点满了，
            // 之后四章再没有任何战力成长，需求血量却还在涨。
            new ForgeDef
            {
                Line = ForgeLine.Damage, Name = "伤害", Stat = "炮弹伤害", Amount = 8f,
                Icon = "damage", Reveal = 0,
                Cost = new[] { 120, 140, 170, 200, 240, 300, 380, 470, 580, 700, 840, 1000 },
                Gate = new[] { 0, 2, 6, 12, 20, 28, 36, 44, 52, 58, 64, 69 }
            },
            new ForgeDef
            {
                Line = ForgeLine.Emitters, Name = "炮台数", Stat = "炮台", Amount = 1f,
                Icon = "guns", Reveal = 9,
                Cost = new[] { 260, 420 },
                Gate = new[] { 9, 40 }
            },
            new ForgeDef
            {
                Line = ForgeLine.FireRate, Name = "射速", Stat = "开火间隔", Amount = 8f,
                Icon = "rate", Reveal = 3,
                Cost = new[] { 130, 160, 220, 300, 400 },
                Gate = new[] { 3, 9, 24, 45, 62 }
            },
            new ForgeDef
            {
                Line = ForgeLine.StartGold, Name = "开局金币", Stat = "开局金币", Amount = 2f,
                Icon = "gold", Reveal = 4,
                Cost = new[] { 140, 160, 210, 270, 360 },
                Gate = new[] { 4, 10, 22, 36, 54 }
            },
            new ForgeDef
            {
                Line = ForgeLine.BaseHp, Name = "基地生命", Stat = "基地生命", Amount = 1f,
                Icon = "hp", Reveal = 7,
                Cost = new[] { 190, 280, 420 },
                Gate = new[] { 7, 27, 50 }
            }
        };

        public static ForgeDef Get(int i) => Lines[Mathf.Clamp(i, 0, LineCount - 1)];
        public static ForgeDef Get(ForgeLine line) => Lines[(int)line];

        // 点过，或通关关数到了露面门槛，才在加成页出现。
        public static bool Exposed(int line, int cleared, int level) =>
            level > 0 || cleared >= Get(line).Reveal;

        // 下一张要解锁的线。加成页只挂这一张「还没到」的预告，不把后面全摊开。
        public static int NextLocked(int cleared, int[] levels)
        {
            int best = -1, bestR = int.MaxValue;
            for (int i = 0; i < LineCount; i++)
            {
                int lv = levels != null && i < levels.Length ? levels[i] : 0;
                if (Exposed(i, cleared, lv)) continue;
                int r = Get(i).Reveal;
                if (r < bestR) { bestR = r; best = i; }
            }
            return best;
        }

        public static int MaxLevel(int i) => Get(i).MaxLevel;

        // 从 level 级升到 level+1 级要多少墨。已满级返回 0。
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
                case ForgeLine.Emitters: return "多一门炮";
                default: return d.Stat + " +" + n;
            }
        }

        // 整数就不拖小数点，方便以后把每级幅度调成 7.5 这种。
        static string Num(float v) =>
            Mathf.Approximately(v, Mathf.Round(v))
                ? Mathf.RoundToInt(v).ToString()
                : v.ToString("0.#");

        public static ForgeStats Stats(int[] levels)
        {
            ForgeStats s = ForgeStats.Default;
            if (levels == null) return s;
            int dmg = Lv(levels, ForgeLine.Damage);
            int gun = Lv(levels, ForgeLine.Emitters);
            int rate = Lv(levels, ForgeLine.FireRate);
            int gold = Lv(levels, ForgeLine.StartGold);
            int hp = Lv(levels, ForgeLine.BaseHp);
            s.DamageMul = 1f + Amount(ForgeLine.Damage) * 0.01f * dmg;
            s.IntervalMul = Mathf.Max(0.2f, 1f - Amount(ForgeLine.FireRate) * 0.01f * rate);
            s.Emitters = Mathf.Clamp(
                ForgeStats.Default.Emitters + Mathf.RoundToInt(Amount(ForgeLine.Emitters)) * gun,
                1, GameConstants.MaxEmitters);
            s.StartGold = ForgeStats.Default.StartGold + Mathf.RoundToInt(Amount(ForgeLine.StartGold)) * gold;
            s.BaseHp = GameConstants.BaseHp + Mathf.RoundToInt(Amount(ForgeLine.BaseHp)) * hp;
            return s;
        }

        static float Amount(ForgeLine line) => Get(line).Amount;

        static int Lv(int[] levels, ForgeLine line)
        {
            int i = (int)line;
            if (i < 0 || i >= levels.Length) return 0;
            return Mathf.Clamp(levels[i], 0, MaxLevel(i));
        }
    }
}
