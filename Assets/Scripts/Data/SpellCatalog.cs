using UnityEngine;

namespace InkLine
{
    public enum SpellId { Burst, Halt, Rage, Sweep, Splash, Mend }

    // 全局技能。和改装字牌完全分开：不进牌池、不占格子、不吃星。
    public struct SpellDef
    {
        public SpellId Id;
        public string Name;
        public string Desc;
        public int InkCost;    // 释放一次要多少局内墨
        public int Price;      // 解锁价，单位墨
        public int Gate;       // 解锁要求的总星
        public bool NeedClear; // 还要求通关本章
        public Color Tint;
    }

    public static class SpellCatalog
    {
        public const int Count = 6;

        // 共用一条墨，所以贵的技能天然和便宜的抢资源，这才有攒不攒的取舍。
        static readonly SpellDef[] All =
        {
            new SpellDef
            {
                Id = SpellId.Burst, Name = "墨爆", Desc = "最前排炸开一圈，6 点伤害",
                InkCost = 25, Price = 0, Gate = 0, Tint = InkTheme.Explode
            },
            new SpellDef
            {
                Id = SpellId.Halt, Name = "定身", Desc = "全场敌人定住 1.6 秒",
                InkCost = 35, Price = 40, Gate = 0, Tint = InkTheme.Word
            },
            new SpellDef
            {
                Id = SpellId.Rage, Name = "强攻", Desc = "5 秒内炮弹伤害翻倍",
                InkCost = 45, Price = 75, Gate = 0, Tint = InkTheme.Fire
            },
            new SpellDef
            {
                Id = SpellId.Sweep, Name = "横扫", Desc = "全屏 5 点伤害并击退",
                InkCost = 55, Price = 120, Gate = 14, Tint = InkTheme.Ink
            },
            new SpellDef
            {
                Id = SpellId.Splash, Name = "泼墨", Desc = "敌人最多那一列灼烧 3 秒",
                InkCost = 50, Price = 160, Gate = 18, Tint = InkTheme.Poison
            },
            new SpellDef
            {
                Id = SpellId.Mend, Name = "回血", Desc = "基地回 1 血，每局限一次",
                InkCost = 70, Price = 220, Gate = 0, NeedClear = true, Tint = InkTheme.Heart
            }
        };

        public static SpellDef Get(int i) => All[Mathf.Clamp(i, 0, Count - 1)];
        public static SpellDef Get(SpellId id) => All[(int)id];
    }

    public struct SkinDef
    {
        public string Name;
        public Color Tint;
        public int Price;
        public int Gate;
        public bool NeedClear;
    }

    // 皮肤只换炮位底下那团色washes，零数值。不卖强度。
    public static class SkinCatalog
    {
        public const int Count = 4;

        static readonly SkinDef[] All =
        {
            new SkinDef { Name = "素笔", Tint = Color.clear, Price = 0 },
            new SkinDef { Name = "朱砂", Tint = InkTheme.Hex("C0392B"), Price = 80 },
            new SkinDef { Name = "青瓷", Tint = InkTheme.Hex("4A7C8C"), Price = 150, Gate = 18 },
            new SkinDef { Name = "鎏金", Tint = InkTheme.Hex("E8B43A"), Price = 260, NeedClear = true }
        };

        public static SkinDef Get(int i) => All[Mathf.Clamp(i, 0, Count - 1)];
    }
}
