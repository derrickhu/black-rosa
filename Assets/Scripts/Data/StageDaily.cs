using System.Collections.Generic;
using UnityEngine;
using static InkLine.EnemyId;

namespace InkLine
{
    // 每日残局：全服同一天打同一局。20 波，看谁撑得久，成绩上「每日」榜。
    // 每天由日期定死：开局盘上摆好四张字（一张二星）、停用两张字、一条魔气词缀、
    // 兵种和出场顺序、战场底图。字池固定用第四章章底那一份，通关第四章才开。
    // 难度按无尽的曲线走得更快：第 n 波相当于无尽第 1.6n 波，第 20 波约 9 倍血。
    // 第 10、20 波各压一只关底。不加格、不渗魔气。
    public static partial class StageCatalog
    {
        public const int DailyWaves = 20;
        const float DailyDepthStep = 1.6f;
        // 字池取这一关（第四章章底）的。
        const int DailyPoolStage = GameConstants.ChapterSize * (GameConstants.DailyOpenChapter + 1) - 1;
        const int DailyPresetCount = 4;
        const int DailyBanCount = 2;
        const int DailyPriceWaves = 8;
        public const int DailyBossEvery = 10;

        static readonly EnemyId[] DailyMidBosses = { BossInkbag, BossIron, BossTwin, BossDrum };
        static readonly EnemyId[] DailyEndBosses = { BossThunder, BossMedic, BossWarden, BossKing };

        static StageDef _daily;

        // 今天的残局。同一天只排一次。
        public static StageDef DailyStage(int day, int week)
        {
            if (_daily != null && _daily.DailyDay == day) return _daily;
            _daily = BuildDaily(day, week);
            return _daily;
        }

        static StageDef BuildDaily(int day, int week)
        {
            var rng = new System.Random(DayClock.Hash(day, 7919));
            StageRule rule = StageRules.ForDay(day, week);
            float countMul = rule.CountMul;

            var waves = new WaveDef[DailyWaves];
            var bodies = new int[DailyWaves][];
            var hp = new float[DailyWaves];
            var gold = new float[DailyWaves];
            var speed = new float[DailyWaves];
            var ward = new int[DailyWaves];
            EnemyId mid = DailyMidBosses[rng.Next(DailyMidBosses.Length)];
            EnemyId end = DailyEndBosses[rng.Next(DailyEndBosses.Length)];
            int shift = rng.Next(GameConstants.Columns);
            for (int w = 0; w < DailyWaves; w++)
            {
                int depth = Mathf.RoundToInt(w * DailyDepthStep);
                EnemyId? boss = null;
                if ((w + 1) % DailyBossEvery == 0) boss = w + 1 >= DailyWaves ? end : mid;
                SurvivalWave(depth, boss, new System.Random(rng.Next()), countMul, w + shift, out waves[w], out bodies[w]);
                hp[w] = EndlessHp(depth);
                gold[w] = EndlessDrop(depth);
                speed[w] = EndlessSpeed(depth);
                ward[w] = 0;
            }

            CardId[] full = Get(DailyPoolStage).Pool;
            var singles = new List<CardId>();
            for (int i = 0; i < full.Length; i++)
                if (CardCatalog.Get(full[i]).Wake != CardWake.WordPart) singles.Add(full[i]);
            Shuffle(singles, rng);
            var preset = new List<CardId>();
            for (int i = 0; i < DailyPresetCount && i < singles.Count; i++) preset.Add(singles[i]);
            var banned = new List<CardId>();
            for (int i = DailyPresetCount; i < singles.Count && banned.Count < DailyBanCount; i++) banned.Add(singles[i]);
            var pool = new List<CardId>();
            for (int i = 0; i < full.Length; i++)
                if (!banned.Contains(full[i])) pool.Add(full[i]);

            var cells = new List<int>();
            for (int r = 0; r < 2; r++)
            for (int c = 0; c < GameConstants.Columns; c++)
                cells.Add(r * GameConstants.Columns + c);
            Shuffle(cells, rng);
            var placed = new PresetGlyph[preset.Count];
            int twoStar = rng.Next(preset.Count);
            for (int i = 0; i < preset.Count; i++)
            {
                int cell = cells[i];
                placed[i] = new PresetGlyph(cell % GameConstants.Columns, cell / GameConstants.Columns, preset[i],
                    i == twoStar ? 2 : 1);
            }

            var s = new StageDef
            {
                Index = -1,
                Chapter = GameConstants.DailyOpenChapter,
                Slot = 0,
                Title = "每日残局",
                Name = "每日残局 · " + DailyWaves + " 波",
                Mask = G("XXXXXX", "XXXXXX"),
                Pool = pool.ToArray(),
                Fresh = new CardId[0],
                Waves = waves,
                Bodies = bodies,
                Hp = 1f,
                WaveHp = hp,
                WaveGold = gold,
                WaveSpeed = speed,
                WaveWard = ward,
                StaminaCost = 0,
                Endless = true,
                Daily = true,
                DailyDay = day,
                Preset = placed,
                Banned = banned.ToArray(),
                Backdrop = "Bg/battle_bg_" + (1 + DayClock.Hash(day, 31) % GameConstants.Chapters),
            };
            ApplyRule(s, rule);
            PriceSurvival(s, DailyPriceWaves);
            return s;
        }

        public static bool DailyBossWave(int wave) => (wave + 1) % DailyBossEvery == 0;

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }
    }
}
