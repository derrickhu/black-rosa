using UnityEngine;

namespace InkLine
{
    public enum EventPrize { Ink, Token, Chest, Skin }

    public struct EventTierDef
    {
        public string Name;
        // 借主线哪一章哪一关的血量、速度、波长和出怪折扣。下一处要打赢上一处才开，不看章节。
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
        // 小集硬存大约 200、正常打完抽牌大约 60；大集硬存大约 420。门槛按这个大约砍半。
        public const int SkinTarget = 3000;
        // 已经有福袋时，这一档折成活动币。
        public const int SkinTokenRefund = 500;

        public static readonly EventTierDef[] Tiers =
        {
            new EventTierDef { Name = "小集", Chapter = 1, Slot = 4, Brief = "六波，怪少钱少" },
            new EventTierDef { Name = "庙会", Chapter = 2, Slot = 4, Brief = "八波，钱袋翻倍" },
            new EventTierDef { Name = "大集", Chapter = 4, Slot = 4, Brief = "十波，怪硬钱多" }
        };

        public static readonly EventMilestone[] Milestones =
        {
            // 第一档给墨。够升一级前期伤害，活动不再发钻石。
            new EventMilestone { Need = 500, Prize = EventPrize.Ink, Amount = 120 },
            new EventMilestone { Need = 1200, Prize = EventPrize.Token, Amount = 300 },
            new EventMilestone { Need = 2000, Prize = EventPrize.Chest, Amount = (int)ChestTier.Gold },
            new EventMilestone { Need = SkinTarget, Prize = EventPrize.Skin, Amount = SkinCatalog.Lucky },
            // 福袋之后活动币没处花了，后两档改发墨和皇家箱。
            new EventMilestone { Need = 4000, Prize = EventPrize.Ink, Amount = 400 },
            new EventMilestone { Need = 5000, Prize = EventPrize.Chest, Amount = (int)ChestTier.Royal }
        };

        public static int TierCount => Tiers.Length;
        public static EventTierDef Tier(int i) => Tiers[Mathf.Clamp(i, 0, Tiers.Length - 1)];

        public static int Interest(int gold, int cap) =>
            Mathf.Clamp(gold / InterestStep, 0, Mathf.Max(0, cap));

        // 输了看广告能存回的金币：钱袋里的一半，至少留 1 枚。
        public static int Salvage(int purse) => purse <= 0 ? 0 : Mathf.Max(1, purse / 2);

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
