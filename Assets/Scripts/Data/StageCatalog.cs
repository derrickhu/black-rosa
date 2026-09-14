using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public readonly struct SpawnSpec
    {
        public readonly float Time;
        public readonly EnemyId Id;
        public readonly int Column;
        public readonly int Count;

        public SpawnSpec(float time, EnemyId id, int column = -1, int count = 1)
        {
            Time = time;
            Id = id;
            Column = column;
            Count = count;
        }
    }

    public sealed class WaveDef
    {
        public readonly float Duration;
        public readonly SpawnSpec[] Spawns;

        public WaveDef(float duration, params SpawnSpec[] spawns)
        {
            Duration = duration;
            Spawns = spawns;
        }
    }

    public sealed class StageDef
    {
        public readonly int Index;
        public readonly string Name;

        // 每行开放几格，长度就是开放的行数。{2} 是「只有第一排中间两格能放字」，
        // {6,3} 是「第一排全开、第二排开中间三格」。
        // 原来这里是一个 OpenRows 整数，粒度只到整行，做不出新手关那种
        // 「一排里先开两格」的开局。
        public readonly int[] OpenCells;

        public readonly CardId[] Pool;
        public readonly WaveDef[] Waves;
        public readonly bool TeachDraft;
        public readonly bool TeachStar;

        public StageDef(int index, string name, int[] openCells, CardId[] pool, WaveDef[] waves,
            bool teachDraft, bool teachStar)
        {
            Index = index;
            Name = name;
            OpenCells = openCells;
            Pool = pool;
            Waves = waves;
            TeachDraft = teachDraft;
            TeachStar = teachStar;
        }

        public int OpenRows => OpenCells.Length;

        public int OpenCount
        {
            get
            {
                int n = 0;
                for (int r = 0; r < OpenCells.Length; r++) n += OpenCells[r];
                return n;
            }
        }

        // 关底 boss 是最后一波里的那只。没有关底的关（前两关）返回 false。
        public bool HasBoss
        {
            get
            {
                if (Waves == null || Waves.Length == 0) return false;
                SpawnSpec[] last = Waves[Waves.Length - 1].Spawns;
                for (int i = 0; i < last.Length; i++)
                    if (EnemyIds.IsBoss(last[i].Id)) return true;
                return false;
            }
        }
    }

    public static class StageCatalog
    {
        // 一行里格子按中间往两边开：2、3 先开，最外侧两列最后开。
        // 中间两列是敌人最密的地方，新手关只开这两格也立刻能感到「放字有用」；
        // 要是从第 0 列开起，第一关的字会摆在几乎没人走的边上。
        static readonly int[] ColOpenOrder = { 2, 3, 1, 4, 0, 5 };

        public static bool CellOpen(int[] openCells, int col, int row)
        {
            if (openCells == null || row < 0 || row >= openCells.Length) return false;
            int n = Mathf.Min(openCells[row], ColOpenOrder.Length);
            for (int i = 0; i < n; i++)
                if (ColOpenOrder[i] == col) return true;
            return false;
        }

        // 字的解锁顺序，取前 n 个当这一关的牌池。
        // 两条规则：一关最多多一个新字（多了认不过来）；成词的四对必须同关一起给 ——
        // 只发半边的那一关里，那张牌就是张永远醒不了的废牌。
        static readonly CardId[] UnlockOrder =
        {
            CardId.Split,                       // 1  分：一发变多发，最直观，开局就给
            CardId.Fire,                         // 2  火：持续掉血
            CardId.Ice,                          // 3  冰：减速
            CardId.Heavy,                        // 4  重：单发变粗必暴
            CardId.Accel,                        // 5  疾：弹速
            CardId.Pierce,                       // 6  穿：穿人
            CardId.Explode,                      // 7  炸：范围
            CardId.Track,                        // 8  追：自动瞄
            CardId.Stun,                         // 9  晕：硬控
            CardId.Strike, CardId.Back,          // 10 击退：最好懂的词，正好有爬子当反例
            CardId.Sec, CardId.Kill,             // 12 秒杀
            CardId.Link, CardId.Slash,           // 14 连斩
            CardId.Myriad, CardId.Arrow          // 16 万箭
        };

        public static int CardCount => UnlockOrder.Length;

        // 章与章之间：17 个字是底盘，每章只解锁 1~2 个新元素，牌池逐章累加。
        // 一次全放会让抽卡变成抽奖，玩家也认不过来。
        static readonly CardId[][] ChapterUnlocks =
        {
            new CardId[0],                                // 第一章：只有底盘 17 字
            new[] { CardId.Gold, CardId.Thunder },        // 第二章：加伤 + 范围控
            new[] { CardId.Wood, CardId.Poison },         // 第三章：吸血 + 可叠层 DoT
            new[] { CardId.Water, CardId.Earth },         // 第四章：范围缓 + 上推
            new[] { CardId.Wind, CardId.Confuse }         // 第五章：横移 + 反打
        };

        public static int ChapterCount => ChapterUnlocks.Length;

        // 到第 chapter 章为止解锁的全部新元素（chapter 从 1 数）。
        public static CardId[] Unlocked(int chapter)
        {
            var list = new List<CardId>();
            int cap = Mathf.Clamp(chapter, 1, ChapterUnlocks.Length);
            for (int c = 0; c < cap; c++) list.AddRange(ChapterUnlocks[c]);
            return list.ToArray();
        }

        // 前 n 个底盘字 + 这一章解锁的新元素。
        static CardId[] Pool(int n, int chapter)
        {
            n = Mathf.Clamp(n, 1, UnlockOrder.Length);
            CardId[] extra = Unlocked(chapter);
            var all = new CardId[n + extra.Length];
            for (int i = 0; i < n; i++) all[i] = UnlockOrder[i];
            extra.CopyTo(all, n);
            return all;
        }

        static StageDef[] _cache;

        public static IReadOnlyList<StageDef> Chapter1 => Chapter(1);

        public static IReadOnlyList<StageDef> Chapter(int chapter)
        {
            // 注意：缓存忽略 chapter，Chapter(2) 会拿到第一章。第二章开工时要连
            // 这里一起改成按章缓存。现在全工程只走 Chapter1，先不动。
            if (_cache == null) _cache = Build(chapter);
            return _cache;
        }

        public static StageDef Get(int index) => Chapter1[Mathf.Clamp(index, 0, GameConstants.ChapterStageCount - 1)];

        // 二十关的三条递进线，互相错开、不在同一关同时跳：
        //   格子：一排开 2→3→4→5→6 格，再开第二排 2→3→4→5→6，再开第三排。
        //   字：  一关多一个，成词的四对同关给，到关 16 满 17 个。
        //   敌人：前六关全是纯墨杂兵，第七关才出现第一处颜色，之后每关多一只，
        //         色阶一路往上到 T3。「颜色越多越强」这条规则要有对照物才立得住。
        // 关底从第三关开始，一只 boss 管两关：第一关单刷（看清它的机制），
        // 第二关带护卫（真考）。八只按血量顺序排，算上关卡系数后关底波总血量
        // 一路单调上升：81/87/112/119/178/187/246/258/315/329/392/408/456/473/593/944/1159/1386。
        static StageDef[] Build(int ch)
        {
            var s = new StageDef[GameConstants.ChapterStageCount];

            // --- 关 1~2：教学。没有关底，清完波次就算过 ---
            // 第一关只开中间两格、只有「分」一个字、两波杂兵，目标是三十秒内通关，
            // 让人学会「拖底下那串炮 → 选字 → 点格子」这三下，别的一概不教。
            s[0] = new StageDef(0, "第一章 · 起笔", new[] { 2 }, Pool(1, ch), new[]
            {
                new WaveDef(8f, new SpawnSpec(0.3f, EnemyId.Walker, 2), new SpawnSpec(2.6f, EnemyId.Walker, 3), new SpawnSpec(5f, EnemyId.Walker, 2)),
                // 第一关的怪全走开放的那两列（2、3）。走到没开的列上虽然也打得死，
                // 但玩家会得到「我放了字却没反应」的错觉，第一关不该出现这种噪音。
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Walker, 3), new SpawnSpec(2f, EnemyId.Walker, 2), new SpawnSpec(4f, EnemyId.Walker, 3), new SpawnSpec(6f, EnemyId.Walker, 2))
            }, true, false);

            s[1] = new StageDef(1, "第一章 · 两点", new[] { 3 }, Pool(2, ch), new[]
            {
                new WaveDef(9f, new SpawnSpec(0.3f, EnemyId.Walker, 2), new SpawnSpec(2f, EnemyId.Walker, 3), new SpawnSpec(4.5f, EnemyId.Walker, 1)),
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Swarm, 2, 3), new SpawnSpec(3.5f, EnemyId.Swarm, 3, 3)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Walker, 1), new SpawnSpec(1.8f, EnemyId.Swarm, 3, 3), new SpawnSpec(4.5f, EnemyId.Walker, 2), new SpawnSpec(6.5f, EnemyId.Walker, 3))
            }, true, true);

            // --- 关 3~6：纯墨杂兵四只轮番登场，一排格子从 4 格开到满 ---
            s[2] = new StageDef(2, "第一章 · 团墨", new[] { 4 }, Pool(3, ch), new[]
            {
                new WaveDef(9f, new SpawnSpec(0.3f, EnemyId.Walker, 1), new SpawnSpec(2f, EnemyId.Walker, 4), new SpawnSpec(4.5f, EnemyId.Swarm, 2, 3)),
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Chubby, 2), new SpawnSpec(3.5f, EnemyId.Chubby, 3)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Chubby, 1), new SpawnSpec(2f, EnemyId.Walker, 3), new SpawnSpec(4f, EnemyId.Walker, 4), new SpawnSpec(6.5f, EnemyId.Swarm, 2, 3)),
                FinalWave(EnemyId.BossDrum, 16f, new SpawnSpec(4f, EnemyId.Walker, 1), new SpawnSpec(7f, EnemyId.Walker, 4))
            }, false, false);

            s[3] = new StageDef(3, "第一章 · 长墨", new[] { 5 }, Pool(4, ch), new[]
            {
                new WaveDef(9f, new SpawnSpec(0.3f, EnemyId.Chubby, 2), new SpawnSpec(2f, EnemyId.Walker, -1, 2), new SpawnSpec(5f, EnemyId.Swarm, 4, 3)),
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Tall, 2), new SpawnSpec(3f, EnemyId.Tall, 4)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Tall, 1), new SpawnSpec(1.8f, EnemyId.Chubby, 3), new SpawnSpec(4.5f, EnemyId.Walker, -1, 2), new SpawnSpec(7f, EnemyId.Swarm, 0, 3)),
                FinalWave(EnemyId.BossDrum, 18f, new SpawnSpec(4f, EnemyId.Tall, 1), new SpawnSpec(7f, EnemyId.Tall, 4), new SpawnSpec(10.5f, EnemyId.Chubby, 3))
            }, false, false);

            s[4] = new StageDef(4, "第一章 · 一行", new[] { 6 }, Pool(5, ch), new[]
            {
                new WaveDef(9f, new SpawnSpec(0.3f, EnemyId.Walker, -1, 3), new SpawnSpec(3f, EnemyId.Tall, 0), new SpawnSpec(5.5f, EnemyId.Tall, 5)),
                new WaveDef(9f, new SpawnSpec(0.3f, EnemyId.Ball, 2), new SpawnSpec(2f, EnemyId.Ball, 3), new SpawnSpec(4f, EnemyId.Ball, 1)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Chubby, 2), new SpawnSpec(1.8f, EnemyId.Chubby, 3), new SpawnSpec(4.5f, EnemyId.Ball, -1, 2), new SpawnSpec(7.5f, EnemyId.Swarm, 5, 3)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Ball, -1, 3), new SpawnSpec(3f, EnemyId.Tall, -1, 2), new SpawnSpec(6.5f, EnemyId.Walker, -1, 2)),
                FinalWave(EnemyId.BossInkbag, 18f, new SpawnSpec(4f, EnemyId.Ball, 0), new SpawnSpec(6f, EnemyId.Ball, 5), new SpawnSpec(9.5f, EnemyId.Chubby, 3))
            }, false, false);

            s[5] = new StageDef(5, "第一章 · 大头", new[] { 6 }, Pool(6, ch), new[]
            {
                new WaveDef(9f, new SpawnSpec(0.3f, EnemyId.Ball, -1, 3), new SpawnSpec(3f, EnemyId.Chubby, 2), new SpawnSpec(5.5f, EnemyId.Tall, 4)),
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.BigHead, 2), new SpawnSpec(3f, EnemyId.BigHead, 3)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.BigHead, 1), new SpawnSpec(1.8f, EnemyId.BigHead, 4), new SpawnSpec(4.5f, EnemyId.Swarm, 2, 4), new SpawnSpec(7.5f, EnemyId.Tall, 0)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Chubby, -1, 2), new SpawnSpec(3f, EnemyId.Ball, -1, 3), new SpawnSpec(6.5f, EnemyId.BigHead, 3), new SpawnSpec(9f, EnemyId.Walker, -1, 2)),
                FinalWave(EnemyId.BossInkbag, 20f, new SpawnSpec(4f, EnemyId.BigHead, 0), new SpawnSpec(6f, EnemyId.BigHead, 5), new SpawnSpec(9.5f, EnemyId.Chubby, 2), new SpawnSpec(13f, EnemyId.Ball, 3))
            }, false, false);

            // --- 关 7~10：第二排从中间两格逐步开到五格；第一处颜色出现 ---
            // 爬子是整局第一个带颜色的兵（黄护膝），配的关底铁桶也免击退 + 减伤，
            // 这一关从头到尾都在讲同一件事：有些东西推不动，得正面打穿。
            s[6] = new StageDef(6, "第一章 · 二层", new[] { 6, 2 }, Pool(7, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Walker, -1, 3), new SpawnSpec(3f, EnemyId.BigHead, 2), new SpawnSpec(6f, EnemyId.Ball, -1, 2)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Crawler, 2), new SpawnSpec(3.5f, EnemyId.Crawler, 3)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Chubby, 1), new SpawnSpec(2f, EnemyId.Crawler, 4), new SpawnSpec(5.5f, EnemyId.Tall, -1, 2)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Crawler, 0), new SpawnSpec(1.6f, EnemyId.Crawler, 5), new SpawnSpec(4.5f, EnemyId.Swarm, 3, 4), new SpawnSpec(8f, EnemyId.BigHead, 2)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Ball, -1, 4), new SpawnSpec(3f, EnemyId.Crawler, -1, 2), new SpawnSpec(7.5f, EnemyId.Chubby, 3)),
                FinalWave(EnemyId.BossIron, 22f, new SpawnSpec(5f, EnemyId.Crawler, 0), new SpawnSpec(7f, EnemyId.Crawler, 5), new SpawnSpec(12f, EnemyId.Chubby, 3))
            }, false, false);

            // 束墨走空列。它登场这一关第二排只开中间三格，外侧两列天然没字 ——
            // 「中间堆满挡不住两边」这一课由关卡形状和敌人特性一起上。
            s[7] = new StageDef(7, "第一章 · 束带", new[] { 6, 3 }, Pool(8, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Crawler, 1), new SpawnSpec(2f, EnemyId.Crawler, 4), new SpawnSpec(5.5f, EnemyId.Ball, -1, 3)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Belt, 2), new SpawnSpec(3.5f, EnemyId.Belt, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Belt, 0), new SpawnSpec(1.8f, EnemyId.Belt, 5), new SpawnSpec(5f, EnemyId.BigHead, -1, 2), new SpawnSpec(8.5f, EnemyId.Tall, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Chubby, -1, 2), new SpawnSpec(3f, EnemyId.Crawler, -1, 2), new SpawnSpec(7f, EnemyId.Swarm, 2, 4)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Belt, -1, 2), new SpawnSpec(3.5f, EnemyId.Ball, -1, 4), new SpawnSpec(7.5f, EnemyId.BigHead, 3), new SpawnSpec(10f, EnemyId.Crawler, 2)),
                FinalWave(EnemyId.BossIron, 24f, new SpawnSpec(5f, EnemyId.Belt, 0), new SpawnSpec(7f, EnemyId.Belt, 5), new SpawnSpec(12f, EnemyId.Crawler, 2), new SpawnSpec(15f, EnemyId.Crawler, 3))
            }, false, false);

            s[8] = new StageDef(8, "第一章 · 快脚", new[] { 6, 4 }, Pool(9, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Belt, -1, 2), new SpawnSpec(3f, EnemyId.Crawler, 3), new SpawnSpec(6.5f, EnemyId.Tall, -1, 2)),
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Runner, 2), new SpawnSpec(2.5f, EnemyId.Runner, 3), new SpawnSpec(5f, EnemyId.Runner, 1)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Runner, 0), new SpawnSpec(1.5f, EnemyId.Runner, 5), new SpawnSpec(4.5f, EnemyId.Belt, 3), new SpawnSpec(8.5f, EnemyId.Chubby, 2)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Crawler, -1, 2), new SpawnSpec(3f, EnemyId.Swarm, 4, 5), new SpawnSpec(7.5f, EnemyId.BigHead, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Runner, -1, 3), new SpawnSpec(3.5f, EnemyId.Belt, -1, 2), new SpawnSpec(8.5f, EnemyId.Ball, -1, 4)),
                FinalWave(EnemyId.BossTwin, 24f, new SpawnSpec(5f, EnemyId.Runner, 0), new SpawnSpec(7f, EnemyId.Runner, 5), new SpawnSpec(12f, EnemyId.Belt, 3))
            }, false, false);

            s[9] = new StageDef(9, "第一章 · 游墨", new[] { 6, 5 }, Pool(11, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Runner, -1, 2), new SpawnSpec(3f, EnemyId.Belt, 2), new SpawnSpec(6.5f, EnemyId.Crawler, 4)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Strafer, 2), new SpawnSpec(3.5f, EnemyId.Strafer, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Strafer, 0), new SpawnSpec(1.8f, EnemyId.Strafer, 5), new SpawnSpec(5f, EnemyId.Runner, -1, 2), new SpawnSpec(9f, EnemyId.Chubby, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Belt, -1, 2), new SpawnSpec(3.5f, EnemyId.Crawler, -1, 2), new SpawnSpec(8f, EnemyId.Swarm, 3, 5)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Strafer, -1, 2), new SpawnSpec(3.5f, EnemyId.Runner, -1, 3), new SpawnSpec(8.5f, EnemyId.BigHead, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Belt, 1), new SpawnSpec(1.8f, EnemyId.Belt, 4), new SpawnSpec(5.5f, EnemyId.Strafer, 3), new SpawnSpec(9.5f, EnemyId.Ball, -1, 4)),
                FinalWave(EnemyId.BossTwin, 26f, new SpawnSpec(5f, EnemyId.Strafer, 0), new SpawnSpec(7f, EnemyId.Strafer, 5), new SpawnSpec(12f, EnemyId.Runner, -1, 2), new SpawnSpec(16f, EnemyId.Belt, 3))
            }, false, false);

            // --- 关 11~12：第二排满开，T2 登场 ---
            s[10] = new StageDef(10, "第一章 · 双排", new[] { 6, 6 }, Pool(11, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Strafer, -1, 2), new SpawnSpec(3f, EnemyId.Runner, -1, 2), new SpawnSpec(7f, EnemyId.Belt, 3)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Splitter, 2), new SpawnSpec(3.5f, EnemyId.Splitter, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Splitter, 0), new SpawnSpec(1.8f, EnemyId.Splitter, 5), new SpawnSpec(5f, EnemyId.Crawler, -1, 2), new SpawnSpec(9f, EnemyId.Strafer, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Belt, -1, 2), new SpawnSpec(3.5f, EnemyId.Runner, -1, 3), new SpawnSpec(8.5f, EnemyId.Chubby, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 3), new SpawnSpec(4.5f, EnemyId.Swarm, 2, 5), new SpawnSpec(9f, EnemyId.BigHead, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Strafer, 1), new SpawnSpec(2f, EnemyId.Strafer, 4), new SpawnSpec(5.5f, EnemyId.Splitter, 3), new SpawnSpec(10f, EnemyId.Ball, -1, 4)),
                FinalWave(EnemyId.BossWarden, 26f, new SpawnSpec(5f, EnemyId.Splitter, 0), new SpawnSpec(7f, EnemyId.Splitter, 5), new SpawnSpec(13f, EnemyId.Belt, 3))
            }, false, false);

            s[11] = new StageDef(11, "第一章 · 厚甲", new[] { 6, 6 }, Pool(13, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 2), new SpawnSpec(3.5f, EnemyId.Strafer, 3), new SpawnSpec(7.5f, EnemyId.Runner, -1, 2)),
                // 厚甲很硬，一只就够让人发现「我这套小伤害打不动它」
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Bulwark, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Bulwark, 1), new SpawnSpec(2.5f, EnemyId.Bulwark, 4), new SpawnSpec(6.5f, EnemyId.Belt, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Crawler, -1, 3), new SpawnSpec(4f, EnemyId.Splitter, -1, 2), new SpawnSpec(9f, EnemyId.Swarm, 3, 5)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Bulwark, 2), new SpawnSpec(2.5f, EnemyId.Strafer, -1, 2), new SpawnSpec(7.5f, EnemyId.Runner, -1, 3)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Bulwark, 0), new SpawnSpec(2f, EnemyId.Bulwark, 5), new SpawnSpec(6.5f, EnemyId.Splitter, 3), new SpawnSpec(10.5f, EnemyId.Belt, -1, 2)),
                FinalWave(EnemyId.BossWarden, 28f, new SpawnSpec(5f, EnemyId.Bulwark, 1), new SpawnSpec(8f, EnemyId.Bulwark, 4), new SpawnSpec(14f, EnemyId.Splitter, -1, 2), new SpawnSpec(18f, EnemyId.Strafer, 3))
            }, false, false);

            // --- 关 13~16：第三排从中间两格逐步开到五格 ---
            s[12] = new StageDef(12, "第一章 · 三层", new[] { 6, 6, 2 }, Pool(13, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Bulwark, 3), new SpawnSpec(3f, EnemyId.Belt, -1, 2), new SpawnSpec(7.5f, EnemyId.Runner, -1, 2)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Sprinter, 2), new SpawnSpec(3f, EnemyId.Sprinter, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Sprinter, 0), new SpawnSpec(1.6f, EnemyId.Sprinter, 5), new SpawnSpec(5f, EnemyId.Splitter, -1, 2), new SpawnSpec(9.5f, EnemyId.Chubby, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Strafer, -1, 2), new SpawnSpec(3.5f, EnemyId.Crawler, -1, 3), new SpawnSpec(8.5f, EnemyId.Swarm, 2, 5)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Bulwark, 1), new SpawnSpec(2.5f, EnemyId.Bulwark, 4), new SpawnSpec(7.5f, EnemyId.Sprinter, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Sprinter, -1, 3), new SpawnSpec(4f, EnemyId.Runner, -1, 3), new SpawnSpec(9.5f, EnemyId.Belt, -1, 2)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 2), new SpawnSpec(3f, EnemyId.Bulwark, 2), new SpawnSpec(5.5f, EnemyId.Bulwark, 3), new SpawnSpec(9.5f, EnemyId.Sprinter, -1, 2)),
                FinalWave(EnemyId.BossThunder, 28f, new SpawnSpec(5f, EnemyId.Sprinter, 0), new SpawnSpec(7f, EnemyId.Sprinter, 5), new SpawnSpec(13f, EnemyId.Bulwark, 3))
            }, false, false);

            s[13] = new StageDef(13, "第一章 · 补墨", new[] { 6, 6, 3 }, Pool(15, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Sprinter, -1, 2), new SpawnSpec(3.5f, EnemyId.Bulwark, 3), new SpawnSpec(7.5f, EnemyId.Belt, -1, 2)),
                // 补墨只奶别人，落单毫无威胁。第一次登场必须配两个肉，
                // 否则玩家根本看不出它在做什么，这个机制就白教了。
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Mender, 3), new SpawnSpec(0.8f, EnemyId.Chubby, 2), new SpawnSpec(1.3f, EnemyId.Chubby, 4)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Mender, 0), new SpawnSpec(0.9f, EnemyId.Bulwark, 0), new SpawnSpec(4.5f, EnemyId.Mender, 5), new SpawnSpec(5.1f, EnemyId.Bulwark, 5)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Sprinter, -1, 3), new SpawnSpec(4f, EnemyId.Splitter, -1, 2), new SpawnSpec(9f, EnemyId.Swarm, 3, 5)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Mender, 2), new SpawnSpec(2f, EnemyId.Strafer, -1, 2), new SpawnSpec(6.5f, EnemyId.Crawler, -1, 3)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Bulwark, 1), new SpawnSpec(2.5f, EnemyId.Bulwark, 4), new SpawnSpec(7f, EnemyId.Mender, 3), new SpawnSpec(10.5f, EnemyId.Runner, -1, 3)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 3), new SpawnSpec(4f, EnemyId.Sprinter, -1, 2), new SpawnSpec(8.5f, EnemyId.Bulwark, 3)),
                FinalWave(EnemyId.BossThunder, 30f, new SpawnSpec(5f, EnemyId.Mender, 0), new SpawnSpec(7f, EnemyId.Mender, 5), new SpawnSpec(13f, EnemyId.Sprinter, -1, 2), new SpawnSpec(19f, EnemyId.Bulwark, 3))
            }, false, false);

            s[14] = new StageDef(14, "第一章 · 铜盾", new[] { 6, 6, 4 }, Pool(15, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Mender, 3), new SpawnSpec(0.9f, EnemyId.Bulwark, 3), new SpawnSpec(5.5f, EnemyId.Sprinter, -1, 2)),
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Shield, 2), new SpawnSpec(3f, EnemyId.Shield, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Shield, 0), new SpawnSpec(1.8f, EnemyId.Shield, 5), new SpawnSpec(5.5f, EnemyId.Mender, 3), new SpawnSpec(9.5f, EnemyId.Crawler, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 3), new SpawnSpec(4f, EnemyId.Strafer, -1, 2), new SpawnSpec(9f, EnemyId.Swarm, 2, 6)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Shield, -1, 3), new SpawnSpec(4f, EnemyId.Bulwark, 3), new SpawnSpec(8.5f, EnemyId.Runner, -1, 3)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Mender, 1), new SpawnSpec(0.9f, EnemyId.Shield, 1), new SpawnSpec(4.5f, EnemyId.Mender, 4), new SpawnSpec(5.4f, EnemyId.Shield, 4), new SpawnSpec(9.5f, EnemyId.Sprinter, -1, 2)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Bulwark, -1, 2), new SpawnSpec(4f, EnemyId.Splitter, -1, 3), new SpawnSpec(9.5f, EnemyId.Belt, -1, 2)),
                FinalWave(EnemyId.BossMedic, 30f, new SpawnSpec(5f, EnemyId.Shield, 0), new SpawnSpec(7f, EnemyId.Shield, 5), new SpawnSpec(13f, EnemyId.Mender, 3), new SpawnSpec(19f, EnemyId.Bulwark, 2))
            }, false, false);

            s[15] = new StageDef(15, "第一章 · 墨尊", new[] { 6, 6, 5 }, Pool(17, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Shield, -1, 2), new SpawnSpec(3.5f, EnemyId.Mender, 3), new SpawnSpec(7.5f, EnemyId.Sprinter, -1, 2)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Elite, 3)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Elite, 1), new SpawnSpec(2.5f, EnemyId.Elite, 4), new SpawnSpec(7.5f, EnemyId.Shield, -1, 2)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 3), new SpawnSpec(4f, EnemyId.Crawler, -1, 3), new SpawnSpec(9.5f, EnemyId.Swarm, 3, 6)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Elite, 2), new SpawnSpec(2.5f, EnemyId.Mender, 3), new SpawnSpec(6.5f, EnemyId.Bulwark, -1, 2)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Sprinter, -1, 3), new SpawnSpec(4f, EnemyId.Runner, -1, 3), new SpawnSpec(9.5f, EnemyId.Strafer, -1, 2)),
                new WaveDef(15f, new SpawnSpec(0.3f, EnemyId.Elite, 0), new SpawnSpec(2.5f, EnemyId.Elite, 5), new SpawnSpec(7.5f, EnemyId.Shield, 3), new SpawnSpec(11.5f, EnemyId.Mender, 2)),
                FinalWave(EnemyId.BossMedic, 32f, new SpawnSpec(5f, EnemyId.Elite, 1), new SpawnSpec(8f, EnemyId.Mender, 0), new SpawnSpec(10f, EnemyId.Mender, 5), new SpawnSpec(17f, EnemyId.Bulwark, 3), new SpawnSpec(23f, EnemyId.Shield, -1, 2))
            }, false, false);

            // --- 关 17~20：全盘开放，墨王四连 ---
            s[16] = new StageDef(16, "第一章 · 镇守", new[] { 6, 6, 6 }, Pool(17, ch), new[]
            {
                new WaveDef(10f, new SpawnSpec(0.3f, EnemyId.Elite, 3), new SpawnSpec(3.5f, EnemyId.Shield, -1, 2), new SpawnSpec(8f, EnemyId.Sprinter, -1, 2)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Warden, 3)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Warden, 1), new SpawnSpec(2.5f, EnemyId.Warden, 4), new SpawnSpec(7.5f, EnemyId.Mender, 3)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 3), new SpawnSpec(4f, EnemyId.Sprinter, -1, 3), new SpawnSpec(9.5f, EnemyId.Swarm, 2, 6)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Warden, 2), new SpawnSpec(2.5f, EnemyId.Elite, 3), new SpawnSpec(7f, EnemyId.Bulwark, -1, 2)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Mender, 0), new SpawnSpec(0.9f, EnemyId.Shield, 0), new SpawnSpec(4.5f, EnemyId.Mender, 5), new SpawnSpec(5.4f, EnemyId.Shield, 5), new SpawnSpec(9.5f, EnemyId.Runner, -1, 3)),
                new WaveDef(15f, new SpawnSpec(0.3f, EnemyId.Warden, 0), new SpawnSpec(2.5f, EnemyId.Warden, 5), new SpawnSpec(7.5f, EnemyId.Elite, 3), new SpawnSpec(11.5f, EnemyId.Strafer, -1, 2)),
                FinalWave(EnemyId.BossKing, 34f, new SpawnSpec(6f, EnemyId.Warden, 1), new SpawnSpec(9f, EnemyId.Warden, 4), new SpawnSpec(16f, EnemyId.Elite, 3), new SpawnSpec(23f, EnemyId.Mender, -1, 2))
            }, false, false);

            s[17] = new StageDef(17, "第一章 · 满盘", new[] { 6, 6, 6 }, Pool(17, ch), new[]
            {
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Warden, 3), new SpawnSpec(3f, EnemyId.Elite, -1, 2), new SpawnSpec(8f, EnemyId.Shield, -1, 2)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Crawler, -1, 3), new SpawnSpec(3.5f, EnemyId.Bulwark, -1, 2), new SpawnSpec(8.5f, EnemyId.Belt, -1, 3)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Sprinter, -1, 4), new SpawnSpec(4f, EnemyId.Runner, -1, 4), new SpawnSpec(9.5f, EnemyId.Ball, -1, 5)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Splitter, -1, 4), new SpawnSpec(4.5f, EnemyId.Swarm, 2, 6), new SpawnSpec(9.5f, EnemyId.Strafer, -1, 3)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Mender, 1), new SpawnSpec(0.9f, EnemyId.Warden, 1), new SpawnSpec(4.5f, EnemyId.Mender, 4), new SpawnSpec(5.4f, EnemyId.Warden, 4)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Elite, 0), new SpawnSpec(2.5f, EnemyId.Elite, 5), new SpawnSpec(7.5f, EnemyId.Bulwark, 3), new SpawnSpec(11.5f, EnemyId.Shield, -1, 2)),
                new WaveDef(15f, new SpawnSpec(0.3f, EnemyId.Warden, 2), new SpawnSpec(2.5f, EnemyId.Warden, 3), new SpawnSpec(7f, EnemyId.Elite, -1, 2), new SpawnSpec(11.5f, EnemyId.Mender, -1, 2)),
                FinalWave(EnemyId.BossKing, 36f, new SpawnSpec(3f, EnemyId.BossDrum, 0), new SpawnSpec(3f, EnemyId.BossDrum, 5), new SpawnSpec(13f, EnemyId.Warden, 3), new SpawnSpec(21f, EnemyId.Elite, -1, 2))
            }, false, false);

            s[18] = new StageDef(18, "第一章 · 逼宫", new[] { 6, 6, 6 }, Pool(17, ch), new[]
            {
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Elite, -1, 2), new SpawnSpec(3.5f, EnemyId.Warden, -1, 2), new SpawnSpec(8.5f, EnemyId.Mender, -1, 2)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Bulwark, -1, 3), new SpawnSpec(4f, EnemyId.Shield, -1, 3), new SpawnSpec(9.5f, EnemyId.Crawler, -1, 3)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Sprinter, -1, 4), new SpawnSpec(4f, EnemyId.Splitter, -1, 4), new SpawnSpec(10f, EnemyId.Swarm, 3, 6)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Warden, 0), new SpawnSpec(2f, EnemyId.Warden, 5), new SpawnSpec(6.5f, EnemyId.Mender, 2), new SpawnSpec(7.4f, EnemyId.Mender, 3)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Elite, 1), new SpawnSpec(2f, EnemyId.Elite, 4), new SpawnSpec(7f, EnemyId.Bulwark, -1, 2), new SpawnSpec(11.5f, EnemyId.Runner, -1, 4)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Strafer, -1, 3), new SpawnSpec(4f, EnemyId.Belt, -1, 3), new SpawnSpec(9.5f, EnemyId.Tall, -1, 4)),
                new WaveDef(15f, new SpawnSpec(0.3f, EnemyId.Warden, 2), new SpawnSpec(2f, EnemyId.Elite, 3), new SpawnSpec(6.5f, EnemyId.Mender, -1, 2), new SpawnSpec(11.5f, EnemyId.Shield, -1, 3)),
                FinalWave(EnemyId.BossKing, 38f, new SpawnSpec(4f, EnemyId.BossMedic, 1), new SpawnSpec(15f, EnemyId.Warden, -1, 2), new SpawnSpec(25f, EnemyId.Elite, -1, 2))
            }, false, false);

            s[19] = new StageDef(19, "第一章 · 墨王", new[] { 6, 6, 6 }, Pool(17, ch), new[]
            {
                new WaveDef(11f, new SpawnSpec(0.3f, EnemyId.Warden, -1, 2), new SpawnSpec(3.5f, EnemyId.Elite, -1, 2), new SpawnSpec(8.5f, EnemyId.Shield, -1, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Mender, -1, 2), new SpawnSpec(3f, EnemyId.Bulwark, -1, 3), new SpawnSpec(9f, EnemyId.Splitter, -1, 3)),
                new WaveDef(12f, new SpawnSpec(0.3f, EnemyId.Ball, -1, 6), new SpawnSpec(3.5f, EnemyId.Runner, -1, 4), new SpawnSpec(8.5f, EnemyId.Sprinter, -1, 4)),
                new WaveDef(13f, new SpawnSpec(0.3f, EnemyId.Crawler, -1, 4), new SpawnSpec(4f, EnemyId.Belt, -1, 3), new SpawnSpec(9.5f, EnemyId.Swarm, 2, 6)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Warden, 0), new SpawnSpec(2f, EnemyId.Warden, 5), new SpawnSpec(6.5f, EnemyId.Mender, 1), new SpawnSpec(7.4f, EnemyId.Mender, 4), new SpawnSpec(11.5f, EnemyId.Elite, 3)),
                new WaveDef(14f, new SpawnSpec(0.3f, EnemyId.Bulwark, -1, 3), new SpawnSpec(4f, EnemyId.Shield, -1, 3), new SpawnSpec(9.5f, EnemyId.Strafer, -1, 3)),
                new WaveDef(15f, new SpawnSpec(0.3f, EnemyId.Elite, 1), new SpawnSpec(2f, EnemyId.Elite, 4), new SpawnSpec(6.5f, EnemyId.Warden, 2), new SpawnSpec(8.5f, EnemyId.Warden, 3), new SpawnSpec(12.5f, EnemyId.Splitter, -1, 3)),
                new WaveDef(16f, new SpawnSpec(0.3f, EnemyId.Warden, 2), new SpawnSpec(2f, EnemyId.Warden, 3), new SpawnSpec(6.5f, EnemyId.Elite, -1, 2), new SpawnSpec(11.5f, EnemyId.Mender, -1, 2)),
                FinalWave(EnemyId.BossKing, 42f, new SpawnSpec(4f, EnemyId.BossTwin, 0), new SpawnSpec(4f, EnemyId.BossTwin, 5), new SpawnSpec(15f, EnemyId.BossDrum, 1), new SpawnSpec(15f, EnemyId.BossDrum, 4), new SpawnSpec(27f, EnemyId.Warden, -1, 2))
            }, false, false);

            return s;
        }

        // 关底波。「一个或多个」都从这里长出来：双首天生成对，其余护卫写在调用处 ——
        // 那是各关自己的节奏；成组规则写在这里，因为那是 boss 自己的设定。
        static WaveDef FinalWave(EnemyId boss, float duration, params SpawnSpec[] escort)
        {
            var list = new List<SpawnSpec>();
            if (boss == EnemyId.BossTwin)
            {
                // 分两列进场，逼玩家分火；挤在一列就退化成打一个大血包了。
                list.Add(new SpawnSpec(0.3f, boss, 1));
                list.Add(new SpawnSpec(0.3f, boss, 4));
            }
            else
            {
                list.Add(new SpawnSpec(0.3f, boss, 3));
            }
            list.AddRange(escort);
            return new WaveDef(duration, list.ToArray());
        }
    }
}
