namespace InkLine
{
    // 开局就摆在盘上的字。每日残局用，全服同一盘。
    public readonly struct PresetGlyph
    {
        public readonly int Col;
        public readonly int Row;
        public readonly CardId Id;
        public readonly int Star;

        public PresetGlyph(int col, int row, CardId id, int star)
        {
            Col = col;
            Row = row;
            Id = id;
            Star = star;
        }
    }

    // 一局的魔气词缀：只动怪和钱，不动字牌。无尽每周换一条，每日残局每天换一条。
    public sealed class StageRule
    {
        public int Id;
        public string Name;
        public string Desc;
        public float HpMul = 1f;
        public float SpeedMul = 1f;
        public float GoldMul = 1f;
        // 每波的怪量预算和只数上限一起乘。
        public float CountMul = 1f;
        // 改装起价和步长一起乘。
        public float DraftMul = 1f;
        public int StartGold;
        public int BaseHp;
        // 在侵染的减伤之外再加的百分点。
        public int Ward;
        public float DemonChance;
    }

    public static class StageRules
    {
        public static readonly StageRule[] All =
        {
            new StageRule { Id = 0, Name = "疾风", Desc = "怪的移速 +15%，掉落金币 +10%", SpeedMul = 1.15f, GoldMul = 1.1f },
            new StageRule { Id = 1, Name = "厚墨", Desc = "怪的血量 +20%，掉落金币 +25%", HpMul = 1.2f, GoldMul = 1.25f },
            new StageRule { Id = 2, Name = "蜂群", Desc = "怪多三成，每只的血少四分之一", CountMul = 1.3f, HpMul = 0.75f },
            new StageRule { Id = 3, Name = "魔染", Desc = "怪自带 6% 减伤，掉落金币 +15%", Ward = 6, GoldMul = 1.15f },
            new StageRule { Id = 4, Name = "拮据", Desc = "开局多 30 金币，改装贵两成", StartGold = 30, DraftMul = 1.2f },
            new StageRule { Id = 5, Name = "魔化", Desc = "三成杂兵带魔化词缀，掉落金币 +20%", DemonChance = 0.3f, GoldMul = 1.2f },
            new StageRule { Id = 6, Name = "背水", Desc = "基地少 1 点血，掉落金币 +40%", BaseHp = -1, GoldMul = 1.4f },
        };

        public static StageRule Get(int id) => All[((id % All.Length) + All.Length) % All.Length];

        // 相邻两周错开，七周轮完一圈。
        public static StageRule ForWeek(int week) => Get(week * 3 + 1);

        public static StageRule ThisWeek => ForWeek(DayClock.Week);

        // 每日残局和本周无尽错开，同一天不撞同一条。
        public static StageRule ForDay(int day, int week)
        {
            StageRule pick = Get(DayClock.Hash(day, 17));
            return pick.Id == ForWeek(week).Id ? Get(pick.Id + 1) : pick;
        }
    }
}
