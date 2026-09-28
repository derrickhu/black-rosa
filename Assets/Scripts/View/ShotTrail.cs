using UnityEngine;

namespace InkLine
{
    public struct SparkSpec
    {
        public SparkKind Kind;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Life;
        public float Size;
        public Color A;
        public Color B;
        public float Spin;
        public float Drag;
        public float Grav;      // 世界 -y 方向的加速度，负数往上飘
        public bool Face;
        public bool Glow;       // 「炫」档走加法发光；否则是带深边的实心块
    }

    public interface ISparkSink
    {
        void Emit(in SparkSpec s);
    }

    // 绕着弹体转的一组小件：雷的电弧、晕的金星、金的闪光、木的叶、瞄的准星、惑的漩涡。
    // 坐标在弹体本地空间（和 InkShot 子节点同一套单位），图鉴乘一个像素系数就能用。
    public struct Ornament
    {
        public SparkKind Kind;
        public int Count;
        public float Rx;
        public float Ry;
        public float Y;
        public float Speed;     // 弧度 / 秒
        public float Size;
        public float Depth;     // >0 时按 sin 分前后：后半圈缩小、压到弹体底下
        public float Spin;      // 自转，度 / 秒
        public float Twinkle;   // >0 时大小按这个频率一闪一闪
        public bool Tangent;    // 沿轨道切线摆（叶子）
        public bool Jitter;     // 每 0.06s 随机换位（电弧）
        public bool Glow;
        public Color Tint;
    }

    // 「炫」档弹身上那团光和那颗星芒。只有能量类的字才有；实心类的字什么都不垫，靠硬边本身。
    public struct ShotAura
    {
        public Color Glow;      // a = 0 表示不画光团
        public float Flare;     // 光团大小倍率
        public Color Glint;     // a = 0 表示不画星芒
        public float GlintScale;
    }

    // 一颗炮弹的粒子配方。战场（InkShot）和图鉴（CodexShot）读同一份，
    // 两边只是接收粒子的 sink 不同。按飞过的距离掉，停住就不掉。
    //
    // 字分两类，让一排炮弹看得出区别：
    //   能量类（火 雷 炸 金 晕 瞄 穿 词组）—— 粒子发光，弹身有光团；
    //   实心类（冰 水 毒 土 风 木 重 分 速 惑）—— 粒子是带深边的实心块，有的受重力往下掉、有的往上飘。
    public sealed class ShotTrail
    {
        public const int MaxOrnaments = 6;

        static readonly Color PierceHi = InkTheme.Hex("A8DDF5");
        static readonly Color PierceLo = InkTheme.Hex("4F8FC0");
        static readonly Color HeavyHi = InkTheme.Hex("E8A56A");
        static readonly Color HeavyLo = InkTheme.Hex("9C4A22");
        static readonly Color SplitHi = InkTheme.Hex("8FE0D8");
        static readonly Color TrackHi = InkTheme.Hex("E2C6F7");
        static readonly Color TrackLit = InkTheme.Hex("C98AF0");
        static readonly Color WordHi = InkTheme.Hex("FFE58A");
        static readonly Color ShardHi = InkTheme.Hex("F26A5A");
        static readonly Color ThunderCore = InkTheme.Hex("EDEBFF");

        enum Slot { Fire, Thunder, Wind, Stun, Explode, Ice, Water, Poison, Earth, Pierce,
            Gold, Heavy, Wood, Confuse, Split, Accel, Track, Word, Count }

        readonly float[] _acc = new float[(int)Slot.Count];
        int _n;
        ISparkSink _sink;
        Vector2 _pos, _dir, _side;
        float _rate;
        bool _glow;
        WordId _word;

        public void Reset()
        {
            for (int i = 0; i < _acc.Length; i++) _acc[i] = 0f;
        }

        public void Step(ShotMods m, Vector2 pos, Vector2 dir, float d, ISparkSink sink)
        {
            if (d <= 0f) return;
            _sink = sink;
            _pos = pos;
            _dir = dir;
            _side = new Vector2(-dir.y, dir.x);
            _word = WordOf(m);

            // 一颗弹同时挂好几个字时每种都稀一点，总量按根号涨，不然三四个字叠起来就是一团。
            int k = 0;
            for (int i = 1; i <= (int)CardId.Confuse; i++)
                if (m.Star((CardId)i) > 0) k++;
            if (_word != WordId.None) k++;
            _rate = (ShotSparks.Bright ? 1.5f : 1f) / Mathf.Sqrt(Mathf.Max(1, k));

            Run(m.Star(CardId.Fire), Slot.Fire, 0.085f, d);
            Run(m.Star(CardId.Thunder), Slot.Thunder, 0.13f, d);
            Run(m.Star(CardId.Wind), Slot.Wind, 0.11f, d);
            Run(m.Star(CardId.Stun), Slot.Stun, 0.26f, d);
            Run(m.Star(CardId.Explode), Slot.Explode, 0.10f, d);
            Run(m.Star(CardId.Ice), Slot.Ice, 0.12f, d);
            Run(m.Star(CardId.Water), Slot.Water, 0.10f, d);
            Run(m.Star(CardId.Poison), Slot.Poison, 0.13f, d);
            Run(m.Star(CardId.Earth), Slot.Earth, 0.12f, d);
            Run(m.Star(CardId.Pierce), Slot.Pierce, 0.07f, d);
            Run(m.Star(CardId.Gold), Slot.Gold, 0.13f, d);
            Run(m.Star(CardId.Heavy), Slot.Heavy, 0.34f, d);
            Run(m.Star(CardId.Wood), Slot.Wood, 0.15f, d);
            Run(m.Star(CardId.Confuse), Slot.Confuse, 0.16f, d);
            Run(m.Star(CardId.Split), Slot.Split, 0.18f, d);
            Run(m.Star(CardId.Accel), Slot.Accel, 0.06f, d);
            Run(m.Star(CardId.Track), Slot.Track, 0.18f, d);
            Run(_word != WordId.None ? 1 : 0, Slot.Word, _word == WordId.Knockback ? 0.30f : 0.12f, d);
        }

        void Run(int star, Slot slot, float step, float d)
        {
            if (star <= 0) return;
            ref float acc = ref _acc[(int)slot];
            step /= _rate;
            acc += d;
            int guard = 0;
            while (acc >= step && guard++ < 8)
            {
                acc -= step;
                _glow = Glows(slot);
                Emit(slot, star);
                _n++;
            }
            if (acc > step) acc = 0f;
        }

        static bool Glows(Slot s)
        {
            switch (s)
            {
                case Slot.Fire:
                case Slot.Thunder:
                case Slot.Explode:
                case Slot.Gold:
                case Slot.Stun:
                case Slot.Track:
                case Slot.Pierce:
                case Slot.Word:
                    return true;
                default:
                    return false;
            }
        }

        void Emit(Slot slot, int s)
        {
            switch (slot)
            {
                case Slot.Fire: Fire(s); break;
                case Slot.Thunder: Thunder(s); break;
                case Slot.Wind: Wind(s); break;
                case Slot.Stun: Stun(s); break;
                case Slot.Explode: Explode(s); break;
                case Slot.Ice: Ice(s); break;
                case Slot.Water: Water(s); break;
                case Slot.Poison: Poison(s); break;
                case Slot.Earth: Earth(s); break;
                case Slot.Pierce: Pierce(s); break;
                case Slot.Gold: Gold(s); break;
                case Slot.Heavy: Heavy(s); break;
                case Slot.Wood: Wood(s); break;
                case Slot.Confuse: Confuse(s); break;
                case Slot.Split: Split(s); break;
                case Slot.Accel: Accel(s); break;
                case Slot.Track: Track(s); break;
                case Slot.Word: Word(_word); break;
            }
        }

        static float R(float a, float b) => Random.Range(a, b);
        Vector2 Back(float along, float spread) => _pos - _dir * along + _side * R(-spread, spread);

        // glow: -1 跟着这个字的类别走，0 / 1 强制实心 / 发光（一个配方里想两种都有时用）。
        void E(SparkKind kind, Vector2 at, Vector2 vel, float life, float size, Color a, Color b,
            float spin = 0f, float drag = 0f, bool face = false, float grav = 0f, int glow = -1)
        {
            _sink.Emit(new SparkSpec
            {
                Kind = kind, Pos = at, Vel = vel, Life = life, Size = size, A = a, B = b,
                Spin = spin, Drag = drag, Face = face, Grav = grav,
                Glow = glow < 0 ? _glow : glow == 1
            });
        }

        // ---- 能量类 ----

        // 火：往上蹿的火舌，金黄烧到橙红。
        void Fire(int s)
        {
            Vector2 at = Back(0.10f, 0.07f);
            E(SparkKind.Ember, at, -_dir * R(0.3f, 0.9f) + _side * R(-0.6f, 0.6f), R(0.22f, 0.38f), 0.10f + 0.02f * s,
                InkTheme.FireHi, InkTheme.Fire, 0f, 2.5f, true, -1.5f);
            if (_n % 3 == 0)
                E(SparkKind.Spark, at, _side * R(-1.4f, 1.4f) - _dir * 0.3f, 0.16f, 0.07f,
                    InkTheme.FireHi, InkTheme.FireMid, 400f, 3f);
        }

        // 雷：四散的电花，全场唯一发白的，夹一截小闪电。
        void Thunder(int s)
        {
            float ang = R(0f, Mathf.PI * 2f);
            var o = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            E(SparkKind.Spark, _pos + o * 0.16f, o * R(1.0f, 2.0f) - _dir * 0.4f, R(0.12f, 0.20f), 0.08f + 0.01f * s,
                ThunderCore, InkTheme.Thunder, 600f, 5f);
            if (_n % 2 == 0)
                E(SparkKind.Bolt, Back(0.18f, 0.1f), -_dir * 0.2f, 0.09f, 0.17f, InkTheme.ThunderHi, InkTheme.Thunder);
        }

        // 晕：一颗颗往下掉的金星。
        void Stun(int s)
        {
            E(SparkKind.Star, Back(0.1f, 0.15f), -_dir * 0.2f + _side * R(-0.5f, 0.5f), 0.40f, 0.10f,
                InkTheme.GoldHi, InkTheme.Word, 360f, 1.5f, false, 3f);
        }

        // 炸：发光的红火花往两边崩，夹带实心的暗红碎片。
        void Explode(int s)
        {
            Vector2 at = Back(0.12f, 0.08f);
            E(SparkKind.Spark, at, _side * R(-2.0f, 2.0f) - _dir * R(0.2f, 0.8f), R(0.14f, 0.22f), 0.09f + 0.015f * s,
                ShardHi, InkTheme.Explode, 500f, 4f);
            if (_n % 2 == 0)
                E(SparkKind.Shard, at, _side * R(-1.2f, 1.2f) - _dir * 0.6f, 0.34f, 0.10f + 0.01f * s,
                    InkTheme.Explode, InkTheme.Hex("8C1F2A"), R(-500f, 500f), 2f, false, 4f, 0);
        }

        // 金：实心的小铜钱翻着往下掉，偶尔一颗金色星芒。
        void Gold(int s)
        {
            E(SparkKind.Coin, Back(0.10f, 0.12f), -_dir * 0.3f + _side * R(-0.7f, 0.7f), 0.42f, 0.11f + 0.015f * s,
                InkTheme.CoinFace, InkTheme.CoinDeep, R(-360f, 360f), 2f, false, 3.5f, 0);
            if (_n % 3 == 0)
                E(SparkKind.Spark, Back(0.06f, 0.16f), _side * R(-0.4f, 0.4f), 0.20f, 0.08f, InkTheme.GoldHi, InkTheme.Gold, 400f, 2f);
        }

        // 穿：顺着弹道往后刷的冷蓝钢针，又细又快。
        void Pierce(int s)
        {
            E(SparkKind.Streak, Back(0.05f, 0.12f), -_dir * R(1.0f, 1.6f), 0.10f, 0.24f + 0.03f * s,
                PierceHi, PierceLo, 0f, 0f, true);
        }

        // 瞄：零星的紫色小闪，主体是身边那圈准星。
        void Track(int s)
        {
            E(SparkKind.Spark, Back(0.10f, 0.10f), -_dir * 0.3f, 0.25f, 0.07f, TrackHi, InkTheme.Track, 300f, 2f);
        }

        void Word(WordId w)
        {
            switch (w)
            {
                case WordId.InstantKill:
                    E(SparkKind.Star, Back(0.10f, 0.12f), _side * R(-1.0f, 1.0f) - _dir * 0.4f, 0.24f, 0.11f,
                        WordHi, InkTheme.Explode, 500f, 3f);
                    break;
                case WordId.ArrowRain:
                    E(SparkKind.Streak, Back(0.05f, 0.16f), -_dir * R(0.8f, 1.2f), 0.14f, 0.24f, WordHi, InkTheme.Word, 0f, 0f, true);
                    break;
                case WordId.Knockback:
                    E(SparkKind.Wave, _pos + _dir * 0.1f, _dir * 0.2f, 0.28f, 0.7f, WordHi, InkTheme.Word, 0f, 3f);
                    break;
                case WordId.Cleave:
                    E(SparkKind.Crescent, Back(0.08f, 0.10f), _side * R(-0.6f, 0.6f) - _dir * 0.4f, 0.26f, 0.18f,
                        WordHi, InkTheme.Word, (_n % 2 == 0 ? -1f : 1f) * 900f, 2f);
                    break;
            }
        }

        // ---- 实心类 ----

        // 风：甩出去的青绿风刃，不发光，靠转和形状认。
        void Wind(int s)
        {
            float ang = Time.unscaledTime * 11f + _n * 2.1f;
            var o = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            E(SparkKind.Crescent, _pos + o * 0.14f, o * 1.0f - _dir * 0.5f, R(0.28f, 0.40f), 0.16f + 0.02f * s,
                InkTheme.WindHi, InkTheme.Wind, (_n % 2 == 0 ? -1f : 1f) * 720f, 2f);
        }

        // 冰：往下落的冰晶，慢慢打转。
        void Ice(int s)
        {
            E(SparkKind.Diamond, Back(0.10f, 0.12f), -_dir * R(0.2f, 0.4f) + _side * R(-0.4f, 0.4f), R(0.34f, 0.48f),
                0.13f + 0.02f * s, InkTheme.IceHi, InkTheme.IceMid, R(-120f, 120f), 2f, false, 2f);
        }

        // 水：往下滴的水珠，隔一个带一个小水泡。
        void Water(int s)
        {
            E(SparkKind.Ember, Back(0.1f, 0.08f), -_dir * R(0.3f, 0.7f) + _side * R(-0.6f, 0.6f), 0.32f, 0.10f + 0.01f * s,
                InkTheme.WaterHi, InkTheme.Water, 0f, 1.5f, true, 5f);
            if (_n % 2 == 0)
                E(SparkKind.Bubble, Back(0.08f, 0.12f), -_dir * 0.2f + _side * R(-0.4f, 0.4f), 0.30f,
                    R(0.07f, 0.10f), InkTheme.WaterHi, InkTheme.Water, 0f, 2f);
        }

        // 毒：往上冒的大毒泡，偶尔滴一滴往下掉。
        void Poison(int s)
        {
            E(SparkKind.Bubble, Back(0.10f, 0.14f), -_dir * 0.15f + _side * R(-0.5f, 0.5f), 0.50f,
                R(0.10f, 0.16f) + 0.01f * s, InkTheme.PoisonHi, InkTheme.Poison, 0f, 1.5f, false, -1.2f);
            if (_n % 3 == 0)
                E(SparkKind.Ember, Back(0.1f, 0.05f), -_dir * 0.4f, 0.30f, 0.09f, InkTheme.PoisonHi, InkTheme.Poison, 0f, 1f, true, 5f);
        }

        // 土：翻滚着往下砸的碎石，个头大、掉得快。
        void Earth(int s)
        {
            E(SparkKind.Shard, Back(0.10f, 0.10f), -_dir * R(0.2f, 0.6f) + _side * R(-0.9f, 0.9f), R(0.32f, 0.44f),
                0.13f + 0.02f * s, InkTheme.EarthHi, InkTheme.Earth, R(-500f, 500f), 2f, false, 6f);
        }

        // 重：一圈圈往外撑的深铜冲击波。
        void Heavy(int s)
        {
            E(SparkKind.Wave, _pos - _dir * 0.05f, -_dir * 0.3f, 0.32f, 0.55f + 0.10f * s, HeavyHi, HeavyLo, 0f, 3f);
        }

        // 木：打着旋慢慢飘落的叶子。
        void Wood(int s)
        {
            E(SparkKind.Leaf, Back(0.10f, 0.12f), -_dir * R(0.2f, 0.4f) + _side * R(-0.7f, 0.7f), 0.55f,
                0.14f + 0.02f * s, InkTheme.WoodHi, InkTheme.Wood, R(-260f, 260f), 2f, false, 1.2f);
        }

        // 惑：糖果一样的粉紫漩涡，实心、带深边。
        void Confuse(int s)
        {
            E(SparkKind.Swirl, Back(0.10f, 0.12f), -_dir * 0.3f + _side * R(-0.4f, 0.4f), 0.42f, 0.15f + 0.02f * s,
                InkTheme.ConfuseHi, InkTheme.Confuse, (_n % 2 == 0 ? -1f : 1f) * 400f, 2f);
        }

        // 分：一对碎晶朝两边劈开，读作「一分为二」。
        void Split(int s)
        {
            Vector2 at = Back(0.08f, 0f);
            float v = R(1.0f, 1.5f);
            E(SparkKind.Diamond, at, _side * v - _dir * 0.3f, 0.24f, 0.10f, SplitHi, InkTheme.Teal, 0f, 3f, true);
            E(SparkKind.Diamond, at, -_side * v - _dir * 0.3f, 0.24f, 0.10f, SplitHi, InkTheme.Teal, 0f, 3f, true);
        }

        // 速：两侧往后刷的绿色速度线。
        void Accel(int s)
        {
            float side = (_n % 2 == 0 ? 1f : -1f) * R(0.08f, 0.18f);
            E(SparkKind.Streak, _pos - _dir * 0.08f + _side * side, -_dir * R(0.6f, 1.0f), 0.12f, 0.20f + 0.02f * s,
                InkTheme.AccelHi, InkTheme.Accel, 0f, 0f, true);
        }

        public static WordId WordOf(ShotMods m)
        {
            if (m.Word != WordId.None) return m.Word;
            if (m.WordLook != WordId.None) return m.WordLook;
            if (m.Has(CardId.Sec) || m.Has(CardId.Kill)) return WordId.InstantKill;
            if (m.Has(CardId.Myriad) || m.Has(CardId.Arrow)) return WordId.ArrowRain;
            if (m.Has(CardId.Strike) || m.Has(CardId.Back)) return WordId.Knockback;
            if (m.Has(CardId.Link) || m.Has(CardId.Slash)) return WordId.Cleave;
            return WordId.None;
        }

        // ---- 绕身小件 ----

        public static int Ornaments(ShotMods m, Ornament[] into)
        {
            int n = 0;
            if (m.Has(CardId.Thunder) && n < into.Length)
                into[n++] = new Ornament
                {
                    Kind = SparkKind.Bolt, Count = 2, Rx = 0.36f, Ry = 0.36f, Size = 0.31f, Jitter = true, Glow = true,
                    Tint = ThunderCore
                };
            if (m.Has(CardId.Stun) && n < into.Length)
                into[n++] = new Ornament
                {
                    Kind = SparkKind.Star, Count = 3, Rx = 0.62f, Ry = 0.22f, Y = 0.10f, Speed = 5f, Size = 0.28f,
                    Depth = 0.3f, Spin = 200f, Glow = true, Tint = InkTheme.GoldHi
                };
            if (m.Has(CardId.Gold) && n < into.Length)
                into[n++] = new Ornament
                {
                    Kind = SparkKind.Spark, Count = 3, Rx = 0.56f, Ry = 0.56f, Speed = 2.4f, Size = 0.20f,
                    Spin = 250f, Twinkle = 7f, Glow = true, Tint = InkTheme.GoldHi
                };
            if (m.Has(CardId.Wood) && n < into.Length)
                into[n++] = new Ornament
                {
                    Kind = SparkKind.Leaf, Count = 2, Rx = 0.52f, Ry = 0.30f, Speed = 3.4f, Size = 0.26f,
                    Depth = 0.25f, Tangent = true, Tint = InkTheme.WoodHi
                };
            if (m.Has(CardId.Track) && n < into.Length)
                into[n++] = new Ornament
                {
                    Kind = SparkKind.Reticle, Count = 1, Size = 1.15f, Spin = 90f, Glow = true, Tint = TrackLit
                };
            if (m.Has(CardId.Confuse) && n < into.Length)
                into[n++] = new Ornament
                {
                    Kind = SparkKind.Swirl, Count = 2, Rx = 0.54f, Ry = 0.54f, Speed = -3f, Size = 0.24f,
                    Spin = 300f, Tint = InkTheme.ConfuseHi
                };
            return n;
        }

        // 第 i 件此刻的位置 / 角度 / 大小。behind 为真时该画在弹体底下。
        public static bool Place(in Ornament o, int i, float t, int seed,
            out Vector2 pos, out float rot, out float scale, out bool behind)
        {
            behind = false;
            if (o.Jitter)
            {
                int slot = Mathf.FloorToInt(t / 0.06f);
                float h0 = Hash(slot, i, seed, 0), h1 = Hash(slot, i, seed, 1);
                float h2 = Hash(slot, i, seed, 2), h3 = Hash(slot, i, seed, 3);
                float ang = h1 * Mathf.PI * 2f;
                pos = new Vector2(Mathf.Cos(ang) * o.Rx, Mathf.Sin(ang) * o.Ry);
                rot = ang * Mathf.Rad2Deg - 90f + (h2 - 0.5f) * 50f;
                scale = o.Size * (0.85f + 0.35f * h3);
                return h0 > 0.2f;
            }
            float a = t * o.Speed + i * Mathf.PI * 2f / Mathf.Max(1, o.Count) + seed * 0.37f;
            float depth = Mathf.Sin(a);
            pos = new Vector2(Mathf.Cos(a) * o.Rx, o.Y + depth * o.Ry);
            rot = o.Tangent
                ? a * Mathf.Rad2Deg + (o.Speed >= 0f ? 0f : 180f)
                : t * o.Spin + i * 40f;
            scale = o.Size * (1f + o.Depth * depth);
            if (o.Twinkle > 0f) scale *= 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(t * o.Twinkle + i * 1.9f));
            behind = o.Depth > 0f && depth > 0f;
            return true;
        }

        static float Hash(int a, int b, int c, int d)
        {
            unchecked
            {
                uint h = (uint)a * 73856093u ^ (uint)b * 19349663u ^ (uint)c * 83492791u ^ (uint)d * 2654435761u;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        // ---- 弹身的主色和光团 ----

        // 主导这颗弹长相的字：先看赢下体槽的，其次按下面的顺序挑第一个亮着的 ——
        // 不用混色，几个颜色平均出来总是一团泥。
        static CardId Lead(ShotMods m, ShotView v, out bool word)
        {
            WordId w = WordOf(m);
            word = false;
            if (w != WordId.None && v.Form.On && v.Form.Card == CardId.None)
            {
                word = true;
                return CardId.None;
            }
            if (v.Form.On && v.Form.Card != CardId.None) return v.Form.Card;
            for (int i = 0; i < LeadOrder.Length; i++)
                if (m.Has(LeadOrder[i])) return LeadOrder[i];
            word = w != WordId.None;
            return CardId.None;
        }

        static readonly CardId[] LeadOrder =
        {
            CardId.Explode, CardId.Fire, CardId.Ice, CardId.Water, CardId.Poison, CardId.Earth,
            CardId.Thunder, CardId.Wind, CardId.Stun, CardId.Gold, CardId.Wood, CardId.Confuse,
            CardId.Heavy, CardId.Pierce, CardId.Accel, CardId.Track, CardId.Split
        };

        public static Color Tone(ShotMods m, ShotView v)
        {
            CardId id = Lead(m, v, out bool word);
            if (word) return InkTheme.Word;
            return id == CardId.None ? Color.clear : GlowOf(id);
        }

        public static Color GlowOf(CardId id)
        {
            switch (id)
            {
                case CardId.Stun: return InkTheme.GoldHi;
                case CardId.Pierce: return PierceHi;
                case CardId.Heavy: return HeavyHi;
                case CardId.Split: return InkTheme.Teal;
                default: return CardCatalog.Accent(id);
            }
        }

        // 光团只给能量类的字，而且各自大小、颜色不同；白色星芒只有雷和穿有，金是金色的。
        public static ShotAura Aura(ShotMods m, ShotView v)
        {
            CardId id = Lead(m, v, out bool word);
            if (word) return new ShotAura { Glow = Fade(InkTheme.Word, 0.55f), Flare = 1.7f };
            switch (id)
            {
                case CardId.Fire: return new ShotAura { Glow = Fade(InkTheme.Fire, 0.70f), Flare = 1.9f };
                case CardId.Explode: return new ShotAura { Glow = Fade(InkTheme.Explode, 0.70f), Flare = 2.1f };
                case CardId.Thunder:
                    return new ShotAura
                    {
                        Glow = Fade(InkTheme.Thunder, 0.85f), Flare = 2.0f, Glint = ThunderCore, GlintScale = 0.55f
                    };
                case CardId.Gold:
                    return new ShotAura
                    {
                        Glow = Fade(InkTheme.Gold, 0.50f), Flare = 1.6f, Glint = InkTheme.GoldHi, GlintScale = 0.50f
                    };
                case CardId.Stun: return new ShotAura { Glow = Fade(InkTheme.Word, 0.45f), Flare = 1.5f };
                case CardId.Track: return new ShotAura { Glow = Fade(InkTheme.Track, 0.45f), Flare = 1.5f };
                case CardId.Pierce: return new ShotAura { Glint = Color.white, GlintScale = 0.40f };
                default: return default;
            }
        }

        static Color Fade(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
