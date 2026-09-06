namespace InkLine
{
    public static class GameConstants
    {
        public const int Columns = 6;
        public const int Rows = 3;
        public const int MaxStar = 3;
        public const int MaxEmitters = 4;
        public const int BaseHp = 3;
        public const int MaxRevives = 2;
        public const int FirstDraftCost = 4;
        public const int DraftCostStep = 2;
        public const int ChapterStageCount = 8;

        public const float WorldHalfHeight = 8f;
        public const float CellWidth = 1.16f;
        public const float CellHeight = 1.16f;
        // 3 行整块靠下，上面留给走怪。漏怪线贴在格底下方。
        public const float GridCenterY = -2.5f;
        public const float EmitterY = -5.85f;
        public static float LeakY =>
            GridCenterY - (Rows - 1) * 0.5f * CellHeight - CellHeight * 0.5f - 0.36f;
        public const float SpawnY = 7.15f;
        public const float BulletSpeed = 7.2f;
        public const float BaseFireInterval = 0.34f;
        public const float RailSnapSpeed = 14f;
    }
}
