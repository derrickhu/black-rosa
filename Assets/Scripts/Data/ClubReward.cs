using UnityEngine;

namespace InkLine
{
    // 游戏圈每日发帖的奖励。打开面板时抽一份并存下来。
    // 转发刷新：八成只往上加（至少多一样，其余不降），两成整份重抽，可能变差。
    // 墨跟着通关数变宽，后期大约是半关首通，领了有感觉，替代不了打关。
    public static class ClubReward
    {
        public const int FoilChancePercent = 35;
        public const int FoilMin = 2;
        public const int FoilMax = 4;
        // 转发刷新里，完全重抽、可能变差的比例。其余只会比原来好。
        public const int WildPercent = 20;

        public static int RollInk(int cleared)
        {
            InkRange(cleared, out int min, out int max);
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

        // 当天这份已经抽好了。八成在它上面加一点，两成当没看过、整份重抽。
        public static void Reroll(int cleared, int ink, int chest, int foil,
            out int newInk, out int newChest, out int newFoil)
        {
            if (Random.Range(0, 100) < WildPercent)
            {
                newInk = RollInk(cleared);
                newChest = (int)RollChest();
                newFoil = RollFoil(cleared);
                return;
            }
            Improve(cleared, ink, chest, foil, out newInk, out newChest, out newFoil);
        }

        // 墨、箱子、金箔都不降，并且能涨的里面随机涨一样。三样都到顶时再给 5 墨，刷新仍看得到变化。
        static void Improve(int cleared, int ink, int chest, int foil,
            out int newInk, out int newChest, out int newFoil)
        {
            InkRange(cleared, out int inkMin, out int inkMax);
            newInk = Mathf.Max(0, ink);
            newChest = Mathf.Clamp(chest, (int)ChestTier.Wood, (int)ChestTier.Gold);
            newFoil = Mathf.Max(0, foil);
            int foilCap = cleared >= StudyCatalog.OpenCleared ? FoilMax : newFoil;
            if (newFoil > foilCap) foilCap = newFoil;

            int choices = 0;
            if (newInk < inkMax) choices++;
            if (newChest < (int)ChestTier.Gold) choices++;
            if (newFoil < foilCap) choices++;
            if (choices == 0)
            {
                newInk += 5;
                return;
            }

            int pick = Random.Range(0, choices);
            if (newInk < inkMax)
            {
                if (pick == 0)
                {
                    int span = Mathf.Max(5, inkMax - inkMin);
                    int step = Mathf.Max(5, Round5(span / 5));
                    newInk = Mathf.Min(inkMax, newInk + step * Random.Range(1, 3));
                    if (newInk <= ink) newInk = ink + 5;
                    return;
                }
                pick--;
            }
            if (newChest < (int)ChestTier.Gold)
            {
                if (pick == 0)
                {
                    newChest++;
                    return;
                }
                pick--;
            }
            newFoil = newFoil <= 0 ? Random.Range(FoilMin, FoilMax + 1) : Mathf.Min(foilCap, newFoil + 1);
        }

        static void InkRange(int cleared, out int min, out int max)
        {
            int c = Mathf.Clamp(cleared, 0, GameConstants.StageCount);
            min = Round5(40 + c);
            max = Round5(70 + c * 2);
            if (max < min) max = min;
        }

        static int Round5(int n) => (n + 2) / 5 * 5;
    }
}
