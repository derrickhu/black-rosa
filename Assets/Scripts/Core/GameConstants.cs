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
        // 二十关。原来是 8 关，一路满速解锁，没有过渡就打完了。
        // MetaProgress.Normalize 里的 Fit() 会把旧存档的星数组补齐到新长度，
        // 改这个数字不会打坏已有存档。
        public const int ChapterStageCount = 20;

        // 局外：体力。每次进关都收，失败不退。见 MetaProgress.StageCost。
        public const int StaminaMax = 12;
        public const int StaminaPerStage = 2;
        public const int StaminaRegenMinutes = 15;
        public const int AdStaminaGain = 6;
        public const int AdStaminaPerDay = 5;
        public const int DailyWinStamina = 4;
        public const int StarterInk = 60;

        // 局内墨和局外墨是两个池子，打完就清，不进存档。
        // 技能花的是本局金币。击杀仍按赏金 ×2 往这只瓶子里攒墨。
        public const int InkMax = 100;
        public const int InkPerGold = 2;
        public const int SpellSlots = 2;

        public const float WorldHalfHeight = 8f;
        public const float CellWidth = 1.16f;
        public const float CellHeight = 1.16f;
        // 3 行整块靠下，上面留给走怪。漏怪线贴在格底下方。
        public const float GridCenterY = -2.5f;
        // 炮在格底和底栏之间。太靠下会在 16:9 上被改装键挡住，
        // 太靠上又会顶到漏怪线。-5.20 给底栏一行按钮留出约一指空隙。
        public const float EmitterY = -5.20f;
        public static float LeakY =>
            GridCenterY - (Rows - 1) * 0.5f * CellHeight - CellHeight * 0.5f - 0.36f;
        public const float SpawnY = 7.15f;
        public const float BulletSpeed = 7.2f;
        // 开局刻意打得慢：0.34 几乎是机关枪，两门炮对着一条线就没有瞄准压力。
        // 往后变快只走局外「射速」线，不在局里叠。
        public const float BaseFireInterval = 0.62f;
        public const float RailSnapSpeed = 14f;
    }
}
