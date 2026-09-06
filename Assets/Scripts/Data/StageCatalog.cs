using System.Collections.Generic;

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
        public readonly int OpenRows;
        public readonly CardId[] Pool;
        public readonly WaveDef[] Waves;
        public readonly bool TeachDraft;
        public readonly bool TeachStar;

        public StageDef(int index, string name, int openRows, CardId[] pool, WaveDef[] waves, bool teachDraft, bool teachStar)
        {
            Index = index;
            Name = name;
            OpenRows = openRows;
            Pool = pool;
            Waves = waves;
            TeachDraft = teachDraft;
            TeachStar = teachStar;
        }
    }

    public static class StageCatalog
    {
        static readonly CardId[] PoolEarly = { CardId.Split, CardId.Fire, CardId.Ice };
        static readonly CardId[] PoolMid = { CardId.Split, CardId.Fire, CardId.Ice, CardId.Track, CardId.Pierce, CardId.Explode, CardId.Accel };
        static readonly CardId[] PoolLate = { CardId.Split, CardId.Fire, CardId.Ice, CardId.Track, CardId.Pierce, CardId.Explode, CardId.Accel, CardId.Heavy };

        static StageDef[] _cache;

        public static IReadOnlyList<StageDef> Chapter1
        {
            get
            {
                if (_cache == null) _cache = Build();
                return _cache;
            }
        }

        public static StageDef Get(int index) => Chapter1[index];

        static StageDef[] Build()
        {
            return new[]
            {
                new StageDef(0, "第一章 · 墨点", 1, PoolEarly, new[]
                {
                    new WaveDef(7f, new SpawnSpec(0.15f, EnemyId.Walker, 1), new SpawnSpec(0.7f, EnemyId.Walker, 4), new SpawnSpec(1.3f, EnemyId.Walker, 0), new SpawnSpec(2f, EnemyId.Walker, 5), new SpawnSpec(2.8f, EnemyId.Walker, 2), new SpawnSpec(4f, EnemyId.Swarm, 3, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Walker, 0), new SpawnSpec(0.6f, EnemyId.Walker, 2), new SpawnSpec(1.2f, EnemyId.Walker, 5), new SpawnSpec(2f, EnemyId.Runner, 4), new SpawnSpec(3f, EnemyId.Walker, 1), new SpawnSpec(4.2f, EnemyId.Walker, -1, 2)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Walker, -1, 3), new SpawnSpec(2f, EnemyId.Runner, 0), new SpawnSpec(3f, EnemyId.Swarm, 4, 3), new SpawnSpec(4.5f, EnemyId.Walker, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Walker, 1), new SpawnSpec(0.6f, EnemyId.Walker, 4), new SpawnSpec(1.4f, EnemyId.Swarm, 3, 3), new SpawnSpec(3f, EnemyId.Runner, 5), new SpawnSpec(4.2f, EnemyId.Walker, 0)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Runner, 5), new SpawnSpec(0.8f, EnemyId.Walker, 0), new SpawnSpec(1.6f, EnemyId.Walker, 3), new SpawnSpec(2.8f, EnemyId.Walker, -1, 2), new SpawnSpec(4.2f, EnemyId.Swarm, 1, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Swarm, 1, 3), new SpawnSpec(1.2f, EnemyId.Walker, 3), new SpawnSpec(2.4f, EnemyId.Runner, 2), new SpawnSpec(3.6f, EnemyId.Walker, -1, 2)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Walker, -1, 3), new SpawnSpec(2f, EnemyId.Strafer, 3), new SpawnSpec(3.5f, EnemyId.Runner, 0), new SpawnSpec(5f, EnemyId.Walker, 5)),
                    new WaveDef(14f, new SpawnSpec(0.2f, EnemyId.Boss, 3), new SpawnSpec(3f, EnemyId.Walker, 0), new SpawnSpec(5f, EnemyId.Walker, 5))
                }, true, false),

                new StageDef(1, "第一章 · 第二张", 1, PoolEarly, new[]
                {
                    new WaveDef(7f, new SpawnSpec(0.1f, EnemyId.Walker, 0), new SpawnSpec(0.6f, EnemyId.Walker, 3), new SpawnSpec(1.2f, EnemyId.Walker, 5), new SpawnSpec(2f, EnemyId.Walker, 1), new SpawnSpec(2.8f, EnemyId.Walker, 4), new SpawnSpec(4f, EnemyId.Swarm, 3, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Runner, 1), new SpawnSpec(0.8f, EnemyId.Walker, 4), new SpawnSpec(1.6f, EnemyId.Walker, 0), new SpawnSpec(2.6f, EnemyId.Swarm, 5, 3), new SpawnSpec(4f, EnemyId.Walker, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Walker, -1, 3), new SpawnSpec(2f, EnemyId.Strafer, 1), new SpawnSpec(3.2f, EnemyId.Runner, 5), new SpawnSpec(4.4f, EnemyId.Walker, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Shield, 3), new SpawnSpec(1.2f, EnemyId.Walker, 0), new SpawnSpec(2f, EnemyId.Walker, 5), new SpawnSpec(3.2f, EnemyId.Walker, 1), new SpawnSpec(4.4f, EnemyId.Swarm, 4, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Swarm, 3, 3), new SpawnSpec(1.4f, EnemyId.Runner, 0), new SpawnSpec(2.6f, EnemyId.Walker, 2), new SpawnSpec(4f, EnemyId.Walker, -1, 2)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Walker, -1, 3), new SpawnSpec(2.2f, EnemyId.Shield, 1), new SpawnSpec(3.6f, EnemyId.Runner, 5), new SpawnSpec(5f, EnemyId.Walker, 3)),
                    new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Strafer, 4), new SpawnSpec(1.2f, EnemyId.Runner, 5), new SpawnSpec(2.4f, EnemyId.Walker, -1, 2), new SpawnSpec(4f, EnemyId.Swarm, 0, 3)),
                    new WaveDef(16f, new SpawnSpec(0.2f, EnemyId.Boss, 3), new SpawnSpec(3f, EnemyId.Walker, 0), new SpawnSpec(5f, EnemyId.Walker, 5), new SpawnSpec(7f, EnemyId.Runner, 1))
                }, true, true),

                new StageDef(2, "第一章 · 二层", 2, PoolMid, MidWaves(false), false, false),
                new StageDef(3, "第一章 · 盾列", 2, PoolMid, MidWaves(true), false, false),
                new StageDef(4, "第一章 · 横移", 2, PoolMid, MidWaves(true), false, false),
                new StageDef(5, "第一章 · 三层", 3, PoolLate, LateWaves(false), false, false),
                new StageDef(6, "第一章 · 重墨", 3, PoolLate, LateWaves(true), false, false),
                new StageDef(7, "第一章 · 关底", 3, PoolLate, LateWaves(true, true), false, false)
            };
        }

        static WaveDef[] MidWaves(bool heavier)
        {
            float m = heavier ? 1f : 0f;
            return new[]
            {
                new WaveDef(8f, new SpawnSpec(0.1f, EnemyId.Walker, 1), new SpawnSpec(0.7f, EnemyId.Walker, 4), new SpawnSpec(1.4f, EnemyId.Runner, 0), new SpawnSpec(2.2f, EnemyId.Walker, 3), new SpawnSpec(3.2f, EnemyId.Swarm, 5, 3)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Shield, 3), new SpawnSpec(2f, EnemyId.Walker, -1, 2), new SpawnSpec(5f, EnemyId.Strafer, 1)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Swarm, 0, 3), new SpawnSpec(2.2f, EnemyId.Swarm, 5, 3), new SpawnSpec(5f, EnemyId.Walker, 3)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Runner, 4), new SpawnSpec(1.4f, EnemyId.Runner, 1), new SpawnSpec(4f, EnemyId.Shield, 5)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Strafer, 3), new SpawnSpec(2.5f, EnemyId.Walker, -1, 3 + (int)m)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Shield, 0), new SpawnSpec(1.8f, EnemyId.Shield, 4), new SpawnSpec(5f, EnemyId.Swarm, 3, 4)),
                new WaveDef(13f, new SpawnSpec(0.2f, EnemyId.Walker, -1, 3), new SpawnSpec(3f, EnemyId.Elite, 3)),
                new WaveDef(20f, new SpawnSpec(0.3f, EnemyId.Boss, 3), new SpawnSpec(5f, EnemyId.Runner, 0), new SpawnSpec(7f, EnemyId.Runner, 5))
            };
        }

        static WaveDef[] LateWaves(bool heavier, bool finale = false)
        {
            return new[]
            {
                new WaveDef(11f, new SpawnSpec(0.2f, EnemyId.Walker, -1, 3), new SpawnSpec(3f, EnemyId.Strafer, 1), new SpawnSpec(5f, EnemyId.Runner, 5)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Shield, 1), new SpawnSpec(1.6f, EnemyId.Swarm, 4, 4), new SpawnSpec(4.5f, EnemyId.Elite, 3)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Runner, 0), new SpawnSpec(1.2f, EnemyId.Runner, 3), new SpawnSpec(2.4f, EnemyId.Runner, 5), new SpawnSpec(5f, EnemyId.Shield, 4)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Strafer, 0), new SpawnSpec(1.6f, EnemyId.Strafer, 5), new SpawnSpec(4f, EnemyId.Walker, -1, heavier ? 4 : 3)),
                new WaveDef(12f, new SpawnSpec(0.2f, EnemyId.Swarm, 3, 5), new SpawnSpec(3f, EnemyId.Elite, 1), new SpawnSpec(6f, EnemyId.Shield, 4)),
                new WaveDef(13f, new SpawnSpec(0.2f, EnemyId.Walker, -1, 4), new SpawnSpec(4f, EnemyId.Elite, 3)),
                new WaveDef(13f, new SpawnSpec(0.2f, EnemyId.Shield, -1, 2), new SpawnSpec(3.5f, EnemyId.Strafer, 3), new SpawnSpec(6f, EnemyId.Runner, 0)),
                new WaveDef(22f, new SpawnSpec(0.3f, EnemyId.Boss, 3), new SpawnSpec(4f, EnemyId.Elite, finale ? 1 : 5), new SpawnSpec(8f, EnemyId.Swarm, 4, 4))
            };
        }
    }
}
