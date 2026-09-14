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

        // 局外：体力。只卡重刷，没通过的关不收费，见 MetaProgress.StageCost。
        public const int StaminaMax = 12;
        public const int StaminaPerStage = 2;
        public const int StaminaRegenMinutes = 15;
        public const int AdStaminaGain = 6;
        public const int AdStaminaPerDay = 5;
        public const int DailyWinStamina = 4;
        public const int StarterInk = 60;

        // 局内：技能能量。击杀按敌人金币值 ×2 累积。
        public const int EnergyMax = 100;
        public const int EnergyPerGold = 2;
        public const int SpellSlots = 2;

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
