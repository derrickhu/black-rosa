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
        public int[] NewLines;      // 这次过关新亮相的词条下标，结算页逐张弹解锁卡
        public int[] NewSkins;      // 这次过关新开放（可以买了）的皮肤
    }
}
