namespace InkLine
{
    // 一次通关入账的明细。MetaProgress.ApplyResult 算好，结算页只拿来演。
    public struct ResultInfo
    {
        public int Ink;             // 已入账的墨（含今日首胜翻倍）
        public bool DailyDouble;    // 今日首胜，墨翻了一倍
        public int FinaleShard;     // 章底首通送的技能碎片是哪个技能，-1 表示没有
        public int Shards;          // 局内拾到的碎片数（GameFlow 另行入账后填进来）
        public int Stars;
        public bool NewBest;
        public bool FirstClear;
        public int[] NewLines;      // 这次过关新亮相的词条下标，结算页逐张弹解锁卡
        public int[] NewSkins;      // 这次过关新开放（可以买了）的皮肤
    }
}
