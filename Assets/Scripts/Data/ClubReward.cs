using UnityEngine;

namespace InkLine
{
    // 游戏圈每日发帖的奖励。打开面板时抽一份并存下来，转发可以整份重抽一次。
    // 墨跟着通关数变宽，后期大约是半关首通，领了有感觉，替代不了打关。
    public static class ClubReward
    {
        public const int FoilChancePercent = 35;
        public const int FoilMin = 2;
        public const int FoilMax = 4;

        public static int RollInk(int cleared)
        {
            int c = Mathf.Clamp(cleared, 0, GameConstants.StageCount);
            int min = Round5(40 + c);
            int max = Round5(70 + c * 2);
            return Random.Range(min / 5, max / 5 + 1) * 5;
        }

        // 每次一个箱子。木 72%，银 22%，金 6%。皇家不进这个池。
        public static ChestTier RollChest()
        {
            int roll = Random.Range(0, 100);
            if (roll < 6) return ChestTier.Gold;
            if (roll < 28) return ChestTier.Silver;
            return ChestTier.Wood;
        }

        // 通关第四章之后才有，2～4 片，不再随关卡涨。
        public static int RollFoil(int cleared)
        {
            if (cleared < StudyCatalog.OpenCleared) return 0;
            if (Random.Range(0, 100) >= FoilChancePercent) return 0;
            return Random.Range(FoilMin, FoilMax + 1);
        }

        static int Round5(int n) => (n + 2) / 5 * 5;
    }
}
