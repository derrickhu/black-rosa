using UnityEngine;

namespace InkLine
{
    public enum ForgeLine { Damage, Emitters, FireRate, StartGold, BaseHp }

    // 一条升级线。Cost[i] 是从 i 级升到 i+1 级的价，Gate[i] 是那一级要求的总星。
    public struct ForgeDef
    {
        public ForgeLine Line;
        public string Name;
        public string Step;      // 每级加多少，卡面上直接显示
        public string Icon;      // Resources/Art/Ui/ico_<Icon>.png
        public int[] Cost;
        public int[] Gate;
        public string LockNote;  // 星门槛超出本章上限时改显示这句

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

        // 前三级刻意便宜，保证头几关就能点到 2、3 次。
        // 贵的那几级靠总星门槛卡住，让人想着往后打而不是原地刷。
        static readonly ForgeDef[] Lines =
        {
            new ForgeDef
            {
                Line = ForgeLine.Damage, Name = "伤害", Step = "炮弹伤害 +8%",
                Icon = "damage",
                Cost = new[] { 12, 28, 60, 120 },
                Gate = new[] { 0, 0, 12, 20 }
            },
            new ForgeDef
            {
                Line = ForgeLine.Emitters, Name = "炮台数", Step = "多一门炮",
                Icon = "guns",
                Cost = new[] { 60, 200 },
                Gate = new[] { 8, 99 },
                LockNote = "第二章开放"
            },
            new ForgeDef
            {
                Line = ForgeLine.FireRate, Name = "射速", Step = "开火间隔 -4%",
                Icon = "rate",
                Cost = new[] { 16, 40, 90 },
                Gate = new[] { 0, 0, 16 }
            },
            new ForgeDef
            {
                Line = ForgeLine.StartGold, Name = "开局金币", Step = "开局金币 +2",
                Icon = "gold",
                Cost = new[] { 14, 32, 70 },
                Gate = new[] { 0, 0, 16 }
            },
            new ForgeDef
            {
                Line = ForgeLine.BaseHp, Name = "基地生命", Step = "基地生命 +1",
                Icon = "hp",
                Cost = new[] { 50, 160 },
                Gate = new[] { 10, 22 }
            }
        };

        public static ForgeDef Get(int i) => Lines[Mathf.Clamp(i, 0, LineCount - 1)];
        public static ForgeDef Get(ForgeLine line) => Lines[(int)line];

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
            s.IntervalMul = 1f - 0.04f * rate;
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
