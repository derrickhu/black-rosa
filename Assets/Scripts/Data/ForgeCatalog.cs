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
        public string Step;      // 每级加多少
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
        // 价按门槛那几关的墨收入定，一级大约是那一段打三到五关的收入。
        static readonly ForgeDef[] Lines =
        {
            new ForgeDef
            {
                Line = ForgeLine.Damage, Name = "伤害", Stat = "炮弹伤害", Step = "炮弹伤害 +8%",
                Icon = "damage", Reveal = 0,
                Cost = new[] { 10, 20, 30, 55, 90, 130, 180, 260 },
                Gate = new[] { 0, 2, 6, 12, 20, 30, 42, 56 }
            },
            new ForgeDef
            {
                Line = ForgeLine.Emitters, Name = "炮台数", Stat = "炮台", Step = "多一门炮",
                Icon = "guns", Reveal = 9,
                Cost = new[] { 50, 160 },
                Gate = new[] { 9, 40 }
            },
            new ForgeDef
            {
                Line = ForgeLine.FireRate, Name = "射速", Stat = "开火间隔", Step = "开火间隔 -8%",
                Icon = "rate", Reveal = 3,
                Cost = new[] { 15, 35, 90, 180 },
                Gate = new[] { 3, 9, 24, 45 }
            },
            new ForgeDef
            {
                Line = ForgeLine.StartGold, Name = "开局金币", Stat = "开局金币", Step = "开局金币 +2",
                Icon = "gold", Reveal = 4,
                Cost = new[] { 10, 25, 55, 100, 170 },
                Gate = new[] { 4, 10, 22, 36, 54 }
            },
            new ForgeDef
            {
                Line = ForgeLine.BaseHp, Name = "基地生命", Stat = "基地生命", Step = "基地生命 +1",
                Icon = "hp", Reveal = 7,
                Cost = new[] { 30, 100, 210 },
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
            int lv = Mathf.Max(0, level);
            switch (Get(i).Line)
            {
                case ForgeLine.Damage: return "+" + 8 * lv + "%";
                case ForgeLine.FireRate: return "-" + 8 * lv + "%";
                case ForgeLine.StartGold: return "+" + 2 * lv;
                case ForgeLine.BaseHp: return "+" + lv;
                case ForgeLine.Emitters: return (2 + lv) + " 门";
                default: return lv.ToString();
            }
        }

        public static ForgeStats Stats(int[] levels)
        {
            ForgeStats s = ForgeStats.Default;
            if (levels == null) return s;
            int dmg = Lv(levels, ForgeLine.Damage);
            int gun = Lv(levels, ForgeLine.Emitters);
            int rate = Lv(levels, ForgeLine.FireRate);
            int gold = Lv(levels, ForgeLine.StartGold);
            int hp = Lv(levels, ForgeLine.BaseHp);
            s.DamageMul = 1f + 0.08f * dmg;
            s.IntervalMul = 1f - 0.08f * rate;
            s.Emitters = Mathf.Clamp(2 + gun, 1, GameConstants.MaxEmitters);
            s.StartGold = 6 + 2 * gold;
            s.BaseHp = GameConstants.BaseHp + hp;
            return s;
        }

        static int Lv(int[] levels, ForgeLine line)
        {
            int i = (int)line;
            if (i < 0 || i >= levels.Length) return 0;
            return Mathf.Clamp(levels[i], 0, MaxLevel(i));
        }
    }
}
