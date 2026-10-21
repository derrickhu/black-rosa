using System.Collections.Generic;
using UnityEngine;
using static InkLine.EnemyId;

namespace InkLine
{
    // 每日残局：全服同一天打同一局。20 波，看谁撑得久，成绩上「每日」榜。
    // 每天由日期定死：开局第一排 0～2 个空格、不摆字、停用两张字、一条魔气词缀、
    // 兵种和出场顺序、战场底图。字池固定用第四章章底那一份，通关第四章才开。
    // 格子靠改装里随机刷出的铲子挖。难度按无尽的曲线走得更快：
    // 第 n 波相当于无尽第 1.6n 波，第 20 波约 9 倍血。第 10、20 波各压一只关底。不渗魔气。
    public static partial class StageCatalog
    {
        public const int DailyWaves = 20;
        const float DailyDepthStep = 1.6f;
        // 字池取这一关（第四章章底）的。
        const int DailyPoolStage = GameConstants.ChapterSize * (GameConstants.DailyOpenChapter + 1) - 1;
        const int DailyBanCount = 2;
        // 开局最多留这么多空格，当天在 0～这个数里取。不摆字。
        public const int DailyStartCells = 2;
        // 改装三选一里，有这么大的机会把其中一张换成铲子。一个格子都没有时必出。
        public const float DailyShovelChance = 0.5f;
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
            var banned = new List<CardId>();
            for (int i = 0; i < singles.Count && banned.Count < DailyBanCount; i++) banned.Add(singles[i]);
            var pool = new List<CardId>();
            for (int i = 0; i < full.Length; i++)
                if (!banned.Contains(full[i])) pool.Add(full[i]);

            int holes = rng.Next(DailyStartCells + 1);
            var cols = new List<int>();
            for (int c = 0; c < GameConstants.Columns; c++) cols.Add(c);
            Shuffle(cols, rng);
            var row = new char[GameConstants.Columns];
            for (int c = 0; c < row.Length; c++) row[c] = '.';
            for (int i = 0; i < holes; i++) row[cols[i]] = 'X';

            var s = new StageDef
            {
                Index = -1,
                Chapter = GameConstants.DailyOpenChapter,
                Slot = 0,
                Title = "每日残局",
                Name = "每日残局 · " + DailyWaves + " 波",
                Mask = new[] { new string(row) },
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
                Banned = banned.ToArray(),
                Backdrop = "Bg/battle_bg_" + (1 + DayClock.Hash(day, 31) % GameConstants.Chapters),
            };
            ApplyRule(s, rule);
            PriceSurvival(s, DailyPriceWaves);
            // 开局只有一两格。按格子数反推的话，一局的钱会摊进两三次改装，起价能到几百。
            // 这里按十几次来摊，铲子和字都买得起，后面仍一次比一次贵。
            PriceDailyDraft(s);
            return s;
        }

        public static bool DailyBossWave(int wave) => (wave + 1) % DailyBossEvery == 0;

        const int DailyDraftPicks = 12;

        static void PriceDailyDraft(StageDef s)
        {
            float mul = s.Rule != null ? s.Rule.DraftMul : 1f;
            float budget = (s.GoldPurse + ForgeStats.Default.StartGold) * 0.5f;
            float avg = budget / DailyDraftPicks;
            int first = Mathf.Max(16, Mathf.RoundToInt(avg * 0.4f));
            float step = 2f * Mathf.Max(0f, budget - DailyDraftPicks * first) / (DailyDraftPicks * (DailyDraftPicks - 1f));
            s.DraftFirst = Mathf.RoundToInt(first * mul);
            s.DraftStep = Mathf.Clamp(step, 3f, 7f) * mul;
            s.DraftCurve = 0.08f * mul;
        }

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
