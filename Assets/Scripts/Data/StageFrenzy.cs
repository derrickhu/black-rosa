using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 百鬼狂潮（爽刷解压副本）：
    // 1. 18 格全开 + 正中 4 枚【灵砚四星格】+ 两侧 2 枚【墨晶宝矿格】；
    // 2. 开局 4 门高速连射炮，第 3 波、第 5 波免费自动增援到 5 门、6 门满编齐射；
    // 3. 96 秒海量脆皮墨潮不间断涌出，抽牌便宜且大概率直升二星；
    // 4. 最后一波（压轴 24 秒）「不倒金翁」登场，挨打狂喷金箔与金币，时间到稳拿金箔与随机印章！
    public static partial class StageCatalog
    {
        public const int FrenzyWaves = 6;
        public const string FrenzyBackdrop = "Bg/battle_bg_8";

        // 三档击破目标：给界面预览和结算用。
        public static readonly int[] FrenzyKillGoals = { 90, 180, 280 };
        public static readonly int[] FrenzyBaseFoil = { 6, 12, 18 };
        public const int FrenzySealKillNeed = 150;

        public static StageDef FrenzyStage(CardId[] accountPool, int clearedChapters)
        {
            CardId[] pool = accountPool != null && accountPool.Length >= 6
                ? accountPool
                : Get(Mathf.Clamp(clearedChapters * GameConstants.ChapterSize - 1, 8, GameConstants.StageCount - 1)).Pool;

            // 怪血特意按爽刷割草校准：脆皮成片爆，不倒金翁够耐揍喷金箔。
            float tierScale = 1.15f + Mathf.Clamp(clearedChapters - 3, 0, 12) * 0.22f;
            var waves = new WaveDef[FrenzyWaves];
            var bodies = new int[FrenzyWaves][];
            var waveHp = new float[FrenzyWaves];
            var waveGold = new float[FrenzyWaves];
            var waveSpeed = new float[FrenzyWaves];

            for (int w = 0; w < FrenzyWaves; w++)
            {
                bool climax = w == FrenzyWaves - 1;
                float dur = climax ? 24f : 14.4f;
                var spawns = new List<SpawnSpec>();
                var counts = new List<int>();

                if (climax)
                {
                    // 压轴登场的黄金不倒翁！站正中慢步横晃，挨打狂掉金箔。
                    spawns.Add(new SpawnSpec(0.4f, EnemyId.Daruma, 2, 1));
                    counts.Add(1);
                }

                int groups = climax ? 9 : 7 + w;
                float step = (dur - 1.8f) / Mathf.Max(1, groups);
                for (int g = 0; g < groups; g++)
                {
                    float t = 0.25f + g * step;
                    EnemyId mob = PickFrenzyMob(w, g);
                    int n = mob == EnemyId.Swarm ? 6 : mob == EnemyId.Ball ? 7 : 5;
                    spawns.Add(new SpawnSpec(t, mob, -1, n));
                    counts.Add(n);
                }

                waves[w] = new WaveDef(dur, spawns.ToArray());
                bodies[w] = counts.ToArray();
                waveHp[w] = tierScale * (0.65f + w * 0.16f);
                waveGold[w] = 1.15f + w * 0.15f;
                waveSpeed[w] = 1.06f + w * 0.04f;
            }

            var terrain = new CellTerrain[GameConstants.Columns, GameConstants.Rows];
            // 正中四格设为【灵砚四星格】，两侧两格埋【墨晶宝矿】开局爽敲！
            terrain[2, 1] = CellTerrain.Inkwell;
            terrain[3, 1] = CellTerrain.Inkwell;
            terrain[2, 2] = CellTerrain.Inkwell;
            terrain[3, 2] = CellTerrain.Inkwell;
            terrain[1, 0] = CellTerrain.Crystal;
            terrain[4, 0] = CellTerrain.Crystal;

            return new StageDef
            {
                Index = 0,
                Chapter = Mathf.Clamp(clearedChapters - 1, 3, GameConstants.Chapters - 1),
                Title = "百鬼狂潮",
                Name = "百鬼狂潮",
                Frenzy = true,
                Endless = true,
                Mask = new[] { "XXXXXX", "XXXXXX", "XXXXXX" },
                Terrain = terrain,
                Pool = pool,
                Waves = waves,
                Bodies = bodies,
                Hp = 1f,
                WaveHp = waveHp,
                WaveGold = waveGold,
                WaveSpeed = waveSpeed,
                StaminaCost = 0,
                DraftFirst = 6,
                DraftStep = 2.2f,
                DraftCurve = 0.06f,
                ChestGold = 12,
                ChestInk = 6,
                Backdrop = FrenzyBackdrop,
                Rule = new StageRule
                {
                    Id = 99,
                    Name = "火力全开",
                    Desc = "满盘灵砚四星格，射速暴涨，痛扁金翁爆金箔",
                    StartGold = 16,
                    GoldMul = 1.35f
                }
            };
        }

        static EnemyId PickFrenzyMob(int wave, int group)
        {
            int k = (wave * 5 + group) % 8;
            switch (k)
            {
                case 0: return EnemyId.Ball;
                case 1: return EnemyId.Swarm;
                case 2: return EnemyId.Walker;
                case 3: return EnemyId.Runner;
                case 4: return EnemyId.Tall;
                case 5: return wave >= 2 ? EnemyId.Splitter : EnemyId.BigHead;
                case 6: return wave >= 1 ? EnemyId.Strafer : EnemyId.Chubby;
                default: return wave >= 3 ? EnemyId.Sprinter : EnemyId.Ball;
            }
        }
    }
}
