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
        // 八章，每章九关，第九关是章底。
        // MetaProgress.Normalize 里的 Fit() 会把旧存档的星数组补齐到新长度，
        // 改这个数字不会打坏已有存档。
        public const int ChapterSize = 9;
        public const int Chapters = 8;
        public const int StageCount = ChapterSize * Chapters;

        // 局外：体力。每次进关都收，失败不退。见 MetaProgress.StageCost。
        public const int StaminaMax = 12;
        public const int StaminaPerStage = 2;
        public const int StaminaFinale = 3;
        public const int FinaleStamina = 6;
        public const int StaminaRegenMinutes = 15;
        public const int AdStaminaGain = 6;
        public const int AdStaminaPerDay = 5;
        public const int DailyWinStamina = 4;
        public const int StarterInk = 85;

        // 新手礼包：看两次激励广告，领一次就没了。皮肤下标见 SkinCatalog（2 = 青瓷）。
        public const int GiftAds = 2;
        public const int GiftSkin = 2;
        public const int GiftShards = 10;
        public const int GiftInk = 100;
        // 七日签到：断一天从第 1 天重来；第一次连签满 7 天送鎏金（SkinCatalog 3）。
        public const int CheckDays = 7;
        public const int CheckStamina = 5;
        public const int CheckInk = 30;
        public const int CheckSkin = 3;
        // 第一轮没有。第二轮起，第 7 天在当天奖励之外再给碎片。
        public const int CheckShardDay7 = 5;
        // 游戏圈：每天发一条帖子领一次。
        public const int ClubInk = 50;
        public const int ClubShards = 1;

        // 局内墨是这一局拾到的墨，通关时整笔入账。击杀掉多少按关卡的 InkBudget 摊，
        // 紫宝箱另给一份。技能花的是本局金币，不花墨。
        public const int InkMax = 9999;
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
