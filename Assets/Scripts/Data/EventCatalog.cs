using UnityEngine;

namespace InkLine
{
    public enum EventPrize { Ink, Token, Chest, Skin }

    public struct EventTierDef
    {
        public string Name;
        // 通关第几章（从 0 数）才开这一档。
        public int GateChapter;
        // 借主线哪一章哪一关的血量、速度、波长和出怪折扣。
        public int Chapter;
        public int Slot;
        public string Brief;
    }

    public struct EventMilestone
    {
        public int Need;
        public EventPrize Prize;
        public int Amount;
    }

    // 「招财进宝」：活动关不掉墨，打赢后钱袋里剩的金币存进聚宝盆，攒够换福袋炮台。
    // 常驻，不设起止日期。期号换了存档里的进度就整份清零。
    public static class EventCatalog
    {
        public const int Id = 1;
        public const string Title = "招财进宝";

        public const int PlaysPerDay = 5;
        public const int AdPlays = 2;
        // 每波结束按钱袋余额给利息：每 InterestStep 金给 1 金，单波最多 InterestCap。
        public const int InterestStep = 10;
        public const int InterestCap = 5;
        public const int SkinTarget = 6000;
        // 已经有福袋时，6000 那一档折成活动币。
        public const int SkinTokenRefund = 500;

        public static readonly EventTierDef[] Tiers =
        {
            new EventTierDef { Name = "小集", GateChapter = 0, Chapter = 1, Slot = 4, Brief = "四波，怪少钱少" },
            new EventTierDef { Name = "庙会", GateChapter = 1, Chapter = 2, Slot = 4, Brief = "五波，钱袋翻倍" },
            new EventTierDef { Name = "大集", GateChapter = 2, Chapter = 4, Slot = 4, Brief = "六波，怪硬钱多" }
        };

        public static readonly EventMilestone[] Milestones =
        {
            // 第一档给墨。够升一级前期伤害，活动不再发钻石。
            new EventMilestone { Need = 1000, Prize = EventPrize.Ink, Amount = 120 },
            new EventMilestone { Need = 2500, Prize = EventPrize.Token, Amount = 300 },
            new EventMilestone { Need = 4000, Prize = EventPrize.Chest, Amount = (int)ChestTier.Gold },
            new EventMilestone { Need = SkinTarget, Prize = EventPrize.Skin, Amount = SkinCatalog.Lucky },
            // 福袋之后活动币没处花了，后两档改发墨和皇家箱。
            new EventMilestone { Need = 8000, Prize = EventPrize.Ink, Amount = 400 },
            new EventMilestone { Need = 10000, Prize = EventPrize.Chest, Amount = (int)ChestTier.Royal }
        };

        public static int TierCount => Tiers.Length;
        public static EventTierDef Tier(int i) => Tiers[Mathf.Clamp(i, 0, Tiers.Length - 1)];

        public static int Interest(int gold, int cap) =>
            Mathf.Clamp(gold / InterestStep, 0, Mathf.Max(0, cap));

        public static string PrizeName(EventMilestone m)
        {
            switch (m.Prize)
            {
                case EventPrize.Ink: return "墨";
                case EventPrize.Token: return "活动币";
                case EventPrize.Chest: return ChestCatalog.Get(m.Amount).Name;
                default: return SkinCatalog.Get(m.Amount).Name + "炮台";
            }
        }

        public static string PrizeIcon(EventMilestone m)
        {
            switch (m.Prize)
            {
                case EventPrize.Ink: return "Ui/ico_ink";
                case EventPrize.Token: return "Ui/ico_goldgain";
                case EventPrize.Chest: return "Ui/chest_" + ChestCatalog.Get(m.Amount).Key;
                default: return "Ui/ico_skin_" + SkinCatalog.Get(m.Amount).Key;
            }
        }

        public static string PrizeCount(EventMilestone m) =>
            m.Prize == EventPrize.Ink || m.Prize == EventPrize.Token ? "×" + m.Amount : "×1";
    }
}
