using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 百鬼狂潮（限时冲榜副本）：
    // 1. 开局 14 格 + 中宫 2 枚【灵砚四星格】+ 底排四角 4 枚【墨晶宝矿格】（射碎解锁满盘 18 格并赠 ★2 字）；
    // 2. 开局 3 门连射火炮，中段头目突袭与终幕各免费增援 1 门，普通怪撞底线照常扣心；
    // 3. 怪血对标玩家当前主线进度，重甲盾卫、精英头目逐波加压；
    // 4. 守住防线撑进终幕（压轴 23.5 秒）迎战「不倒金翁」，集火痛扁狂喷金箔，高击破加送随机印章！
    public static partial class StageCatalog
    {
        public const int FrenzyWaves = 6;
        public const string FrenzyBackdrop = "Bg/battle_bg_8";

        // 三档击破目标：给界面预览和结算用。
        public static readonly int[] FrenzyKillGoals = { 120, 220, 320 };
        public static readonly int[] FrenzyBaseFoil = { 6, 12, 18 };
        public const int FrenzySealKillNeed = 260;

        public static StageDef FrenzyStage(CardId[] accountPool, int clearedChapters)
        {
            int ch = Mathf.Clamp(clearedChapters - 1, 3, GameConstants.Chapters - 1);
            CardId[] pool = accountPool != null && accountPool.Length >= 6
                ? accountPool
                : Get(Mathf.Clamp(clearedChapters * GameConstants.ChapterSize - 1, 8, GameConstants.StageCount - 1)).Pool;

            // 对齐玩家当前主线章节的真实怪血曲线（含后期 ChapterLateHp 补强），逐波升温加压。
            float baseMul = HpOf(ch, 5, false) * ChapterLateHp[ch];
            float[] hpRamp = { 0.62f, 0.82f, 1.05f, 1.28f, 1.52f, 1.78f };
            int[] squadCounts = { 9, 11, 11, 13, 14, 16 };

            var waves = new WaveDef[FrenzyWaves];
            var bodies = new int[FrenzyWaves][];
            var waveHp = new float[FrenzyWaves];
            var waveGold = new float[FrenzyWaves];
            var waveSpeed = new float[FrenzyWaves];

            for (int w = 0; w < FrenzyWaves; w++)
            {
                bool midBoss = w == 2;
                bool climax = w == FrenzyWaves - 1;
                float dur = climax ? 23.5f : 14.5f;
                var spawns = new List<SpawnSpec>();
                var counts = new List<int>();

                if (midBoss)
                {
                    EnemyId vanguard = ch >= 10 ? EnemyId.BossBell
                        : ch >= 6 ? EnemyId.BossThunder : EnemyId.BossTwin;
                    spawns.Add(new SpawnSpec(0.6f, vanguard, 3, 1));
                    counts.Add(1);
                }
                else if (climax)
                {
                    // 压轴登场的黄金不倒翁！站正中慢步横晃，挨打狂掉金箔。
                    spawns.Add(new SpawnSpec(0.4f, EnemyId.Daruma, 2, 1));
                    counts.Add(1);
                }

                int groups = squadCounts[w];
                float step = (dur - 1.8f) / Mathf.Max(1, groups);
                for (int g = 0; g < groups; g++)
                {
                    float t = 0.25f + g * step;
                    EnemyId mob = PickFrenzyMob(w, g, ch);
                    int col = (w * 3 + g * 5) % GameConstants.Columns;
                    int n = mob == EnemyId.Swarm ? 6
                        : mob == EnemyId.Ball ? 6
                        : (mob == EnemyId.Elite || mob == EnemyId.Bulwark || mob == EnemyId.Warden || mob == EnemyId.Monk) ? 2 : 5;
                    spawns.Add(new SpawnSpec(t, mob, col, n));
                    counts.Add(n);
                }

                waves[w] = new WaveDef(dur, spawns.ToArray());
                bodies[w] = counts.ToArray();
                waveHp[w] = baseMul * hpRamp[w];
                waveGold[w] = Mathf.Pow(Mathf.Max(1f, hpRamp[w] * ChapterHp[ch]), 0.55f);
                waveSpeed[w] = 1.02f + w * 0.045f;
            }

            var terrain = new CellTerrain[GameConstants.Columns, GameConstants.Rows];
            // 中宫两枚【灵砚四星格】，底排四角四枚【墨晶宝矿】（打碎才能解锁底排四角满 18 格！）。
            terrain[2, 1] = CellTerrain.Inkwell;
            terrain[3, 1] = CellTerrain.Inkwell;
            terrain[0, 2] = CellTerrain.Crystal;
            terrain[1, 2] = CellTerrain.Crystal;
            terrain[4, 2] = CellTerrain.Crystal;
            terrain[5, 2] = CellTerrain.Crystal;

            var stage = new StageDef
            {
                Index = 0,
                Chapter = ch,
                Title = "百鬼狂潮",
                Name = "百鬼狂潮",
                Frenzy = true,
                Endless = true,
                Daily = true,
                Mask = new[] { "XXXXXX", "XXXXXX", "..XX.." },
                Terrain = terrain,
                Pool = pool,
                Waves = waves,
                Bodies = bodies,
                Hp = 1f,
                WaveHp = waveHp,
                WaveGold = waveGold,
                WaveSpeed = waveSpeed,
                StaminaCost = 0,
                Backdrop = FrenzyBackdrop,
                Rule = new StageRule
                {
                    Id = 99,
                    Name = "百鬼夜行",
                    Desc = "守住防线破晶开格，撑入终幕痛扁金翁爆金箔",
                    StartGold = 18,
                    GoldMul = 1.05f
                }
            };
            Price(stage, 0.90f);
            stage.DraftCurve = 0.14f;
            return stage;
        }

        static EnemyId PickFrenzyMob(int wave, int group, int ch)
        {
            int k = (wave * 5 + group * 3) % 10;
            switch (k)
            {
                case 0: return wave >= 2 ? EnemyId.Shield : EnemyId.Walker;
                case 1: return EnemyId.Swarm;
                case 2: return wave >= 3 && ch >= 8 ? EnemyId.Shade : EnemyId.Runner;
                case 3: return wave >= 1 ? EnemyId.Shield : EnemyId.Chubby;
                case 4: return wave >= 2 ? EnemyId.Elite : EnemyId.Tall;
                case 5: return wave >= 1 ? EnemyId.Splitter : EnemyId.BigHead;
                case 6: return wave >= 2 ? EnemyId.Bulwark : EnemyId.Strafer;
                case 7: return wave >= 3 ? (ch >= 14 ? EnemyId.Drummer : EnemyId.Warden) : EnemyId.Ball;
                case 8: return wave >= 2 ? (ch >= 9 ? EnemyId.Monk : EnemyId.Mender) : EnemyId.Walker;
                default: return wave >= 3 ? (ch >= 15 ? EnemyId.Imp : EnemyId.Sprinter) : EnemyId.Runner;
            }
        }
    }
}
