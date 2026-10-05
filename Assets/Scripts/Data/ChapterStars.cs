namespace InkLine
{
    // 一章九关都是三星，送一只炮台宝箱。箱子不绑章节，别的活动也能发同一只。
    // 开出来什么由 ChestCatalog.RollCannon 现抽，这里只负责那句预告。
    public static class ChapterStars
    {
        public static string Preview(int chapter)
        {
            return chapter >= 0
                ? "九关都三星，送一只" + ChestCatalog.CannonName
                : ChestCatalog.CannonName;
        }
    }
}
