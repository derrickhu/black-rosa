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
        // 无尽和每日残局里看广告加的炮单独封顶，续命送的那门也算。主线仍走到 AdEmitterCap。
        public const int EndlessAdEmitters = 2;
        public const int BaseHp = 3;
        public const int MaxRevives = 2;
        // 无尽模式只给续一次命。
        public const int EndlessRevives = 1;
        // 章下标从 0 数。招财进宝通关第一章开，无尽通关第二章开。
        public const int EventOpenChapter = 0;
        public const int EndlessOpenChapter = 1;
        // 通关这一章后拉起一次微信推荐组件。第二章，下标从 0 数。
        public const int RecommendChapter = 1;
        public const int EndlessPlaysPerDay = 1;
        public const int EndlessAdPlays = 1;
        public const int EndlessInkPerWave = 5;
        // 每日残局通关第三章开，每天一局，看广告再加一局，续一次命。
        // 残局的字池、移速和掉墨仍按第四章，见 StageDaily。
        public const int DailyOpenChapter = 2;

        public static string ChapterName(int chapter)
        {
            string[] n =
            {
                "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "十一", "十二"
            };
            if (chapter < 0 || chapter >= n.Length) return (chapter + 1).ToString();
            return n[chapter];
        }
        public const int DailyPlaysPerDay = 1;
        public const int DailyAdPlays = 1;
        public const int DailyInkPerWave = 4;
        // 精英首通的钻石：普通关、有关底、章底。
        public const int HardDiamondStage = 2;
        public const int HardDiamondBoss = 6;
        public const int HardDiamondFinale = 15;
        // 每日功课：三条，每条给墨；三条全领再给钻石。
        public const int TaskCount = 3;
        public const int TaskBonusDiamond = 10;
        // 改装起价的下限。开局有 6 金币，低于这个数按钮一进关就亮。
        public const int FirstDraftCost = 10;
        public const int DraftCostStep = 2;
        // 十二章，每章九关，第九关是章底。前八章是第一幕，九、十章是第二幕。
        // 十一、十二章不新增字，开局格子少，靠铲子挖开。
        // MetaProgress.Normalize 里的 Fit() 会把旧存档的星数组补齐到新长度，
        // 改这个数字不会打坏已有存档。
        public const int ChapterSize = 9;
        public const int Chapters = 12;
        public const int StageCount = ChapterSize * Chapters;

        // 局外：体力。每关进关都收 2 点，章末关也一样，失败不退。见 MetaProgress.StageCost。
        // 上限只管自动回复：到了就不再回。奖励和购买来的体力不受它限制。
        public const int StaminaMax = 10;
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
        // 七日签到：断一天从第 1 天重来；第一次连签满 7 天送机甲（SkinCatalog 2）。
        // 每天给钻石，第 7 天另给一个金宝箱。
        public const int CheckDays = 7;
        public const int CheckStamina = 5;
        public const int CheckInk = 30;
        public const int CheckSkin = 2;
        public static readonly int[] CheckDiamonds = { 3, 3, 4, 4, 5, 5, 20 };
        // 游戏圈：每天发一条帖子领一次。墨、宝箱、金箔的范围在 ClubReward。

        // 首通给钻石，重复通关不给。普通关、最后一波有头目、章底。
        public const int DiamondStage = 1;
        public const int DiamondBoss = 4;
        public const int DiamondFinale = 12;
        // 钻石补体力：20 钻换 10 体力，每天限次。
        public const int DiamondStaminaGain = 10;
        public const int DiamondStaminaPrice = 20;
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
        // 炮在格底和底栏之间。心贴在格子下沿，炮口再抬就会压到心上。
        // -5.70 让炮身顶离开心的下沿；底栏那一行仍由 BattleHud.FitCamera 让开。
        public const float EmitterY = -5.70f;
        public static float LeakY =>
            GridCenterY - (Rows - 1) * 0.5f * CellHeight - CellHeight * 0.5f - 0.36f;
        public const float SpawnY = 7.15f;
        public const float BulletSpeed = 7.2f;
        // 开局刻意打得慢：0.34 几乎是机关枪，两门炮对着一条线就没有瞄准压力。
        // 往后变快只走局外「射速」线，不在局里叠。
        public const float BaseFireInterval = 0.62f;
        public const float RailSnapSpeed = 14f;
        // 六门炮铺满六列时，炮口卡死在列心，两门中间的怪炮弹够不着。
        // 整排允许左右挪这一点，不到半格，字还是落在原来那一列上。
        public const float FullRailNudge = 0.42f;
    }
}
