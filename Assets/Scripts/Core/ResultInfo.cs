using UnityEngine;

namespace InkLine
{
    // 一次通关入账的明细。MetaProgress.ApplyResult 算好，结算页只拿来演。
    public struct ResultInfo
    {
        public int Ink;             // 已入账的墨（含今日首胜翻倍）
        public bool DailyDouble;    // 今日首胜，墨翻了一倍
        public int Diamonds;        // 首通给的钻石，已入账
        public int Chest;           // 这关掉的宝箱品阶（ChestTier），-1 表示没有
        public bool ChestFull;      // 结算时四个宝箱位都满了，离开时要玩家挑选，不直接折墨
        public int ChestInk;        // 若玩家把这只换成墨，能换多少。还没入账
        public int Stars;
        public bool NewBest;
        public bool FirstClear;
        public int HpLeft;          // 结算时防线还剩几格
        public int HpMax;
        public bool Revived;        // 这局看过广告续命
        public bool HasStarChest;   // 这一章刚凑满三星，结算页当场开炮台宝箱
        public int StarChest;       // 章下标
        public int[] NewLines;      // 这次过关新亮相的词条下标，结算页逐张弹解锁卡
        public int[] NewSkins;      // 这次过关新开放（可以买了）的皮肤
    }

    // 通关评星。三星要自己守住七成血；看过广告续命，最多两星。
    public static class StarRules
    {
        public const float Three = 0.7f;
        public const float Two = 0.35f;
        public const string Rule = "三星要剩七成血且没续命，两星要剩三成半";

        public static int Earn(int hp, int max, int revives)
        {
            float ratio = hp / (float)Mathf.Max(1, max);
            int s = ratio >= Three ? 3 : ratio >= Two ? 2 : 1;
            if (revives > 0) s = Mathf.Min(s, 2);
            return s;
        }

        public static string Why(ResultInfo info)
        {
            int max = Mathf.Max(1, info.HpMax);
            int hp = Mathf.Clamp(info.HpLeft, 0, max);
            string left = "防线还剩 " + hp + "/" + max;
            float ratio = hp / (float)max;
            if (info.Revived && info.Stars < 3 && ratio >= Three)
                return left + "，血够三星，续过命所以是两星";
            if (info.Revived && info.Stars >= 2)
                return left + "，续过命，最多两星";
            if (info.Stars >= 3)
                return left + "，没续命，三星";
            if (info.Stars >= 2)
                return left + "，够两星，七成血才是三星";
            return left + "，不到三成半，一星";
        }
    }
}
