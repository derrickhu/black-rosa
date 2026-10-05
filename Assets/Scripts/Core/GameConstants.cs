namespace InkLine
{
    public static class GameConstants
    {
        public const int Columns = 6;
        public const int Rows = 3;
        public const int MaxStar = 3;
        public const int MaxEmitters = 4;
        // 局里看广告加炮的上限，和列数对齐。局外锻造仍然停在 MaxEmitters。
        public const int AdEmitterCap = 6;
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

        // 局外：体力。每关进关都收 2 点，章末关也一样，失败不退。见 MetaProgress.StageCost。
        // 上限只管自动回复：到了就不再回。奖励和购买来的体力不受它限制。
        public const int StaminaMax = 20;
        public const int StaminaPerStage = 2;
        public const int StaminaRegenMinutes = 15;
        public const int AdStaminaGain = 10;
        public const int AdStaminaPerDay = 5;
        // 开局墨加上第一关掉的墨，正好够新手指引里升一级伤害（ForgeCatalog 伤害 Cost[0]）。
        public const int StarterInk = 100;
        // 新手指引走完送一份。弹弓卡分两笔：第一关木箱 3 张，这份奖励再 3 张，正好够解锁。
        public const int GuideDiamond = 10;
        public const int GuideInk = 100;
        public const int GuideStamina = 10;
        public const int GuideSlingshot = 3;

        // 新手礼包：看两次激励广告，领一次就没了。附带一个当场打开的银宝箱。
        public const int GiftAds = 2;
        public const int GiftInk = 100;
        public const int GiftDiamond = 30;
        public const ChestTier GiftChest = ChestTier.Silver;
        // 七日签到：断一天从第 1 天重来；第一次连签满 7 天送机甲炮（SkinCatalog 2）。
        // 每天给钻石，第 7 天另给一个金宝箱。
        public const int CheckDays = 7;
        public const int CheckStamina = 5;
        public const int CheckInk = 30;
        public const int CheckSkin = 2;
        public static readonly int[] CheckDiamonds = { 3, 3, 4, 4, 5, 5, 20 };
        // 游戏圈：每天发一条帖子领一次，墨加一个木宝箱。
        public const int ClubInk = 50;

        // 首通给钻石。重复通关不给。
        public const int DiamondStage = 2;
        public const int DiamondBoss = 5;
        public const int DiamondFinale = 15;
        // 钻石补体力：10 钻换 10 体力，每天限次。
        public const int DiamondStaminaGain = 10;
        public const int DiamondStaminaPrice = 10;
        public const int DiamondStaminaPerDay = 3;

        // 局内墨是这一局拾到的墨，通关时整笔入账。每只怪掉多少写在 EnemyCatalog 里，
        // 紫钱袋另给一份。道具不花金币也不花墨。
        public const int InkMax = 9999;
        // 道具栏：开局一格，通关第三章开第二格，通关第六章开第三格。值是要通关的章下标。
        public const int ItemSlots = 3;
        public static readonly int[] ItemSlotChapter = { -1, 2, 5 };

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
