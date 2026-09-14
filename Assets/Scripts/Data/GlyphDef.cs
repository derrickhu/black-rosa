using UnityEngine;

namespace InkLine
{
    // 一个字属于哪一族，决定它落在 BattleWorld.ResolveHit 的第几步。
    public enum GlyphFamily
    {
        None,
        Hurt,       // 伤：只改数字
        Status,     // 状：产敌方可见状态
        Move,       // 位：改位置，不改血
        Ballistic,  // 道：改弹道
        Area        // 域：一次命中多出结算点
    }

    // 软状态（灼烧 / 中毒 / 缓）随便叠，同名取强；
    // 硬控（冻 / 晕 / 惑）同时只生效一个，优先级见 GlyphTable.HardRank。
    public enum StatusKind { None, Burn, Poison, Slow, Freeze, Stun, Confuse }

    // 位族的位移轴：同轴取最大，异轴都执行。
    public enum MoveAxis { None, Column, Lateral }

    // 炮弹五槽。同槽只亮优先级最高的一个形，颜色可以混；异槽全亮。
    // 招牌两两只替换「体」槽那一张，所以 12 张图就够覆盖组合。
    public enum ShotFx
    {
        None = 0,
        // 体
        FormFire, FormIce, FormWater, FormPoison, FormEarth, FormExplode,
        FormKill, FormCleave, FormKnock, FormArrow, FormPierce, FormSplit,
        // 体 · 招牌两两
        FormFrostFire, FormScorchBolt, FormHailBolt, FormBlightFire, FormConduct,
        FormMoltenGold, FormWardGold, FormRotLife, FormRamEarth, FormColdWind,
        FormBlaze, FormThunderCut,
        // 尾
        TrailAccel, TrailTrack, TrailWind,
        // 环
        HaloStun, HaloThunder, HaloGold,
        // 绕
        OrbitWind, OrbitSplit, OrbitWood,
        // 晕光
        BloomHeavy, BloomGold
    }

    // 三档数值。星只放大数字，★3 才允许加新动词。
    public readonly struct Tri
    {
        readonly float _s1;
        readonly float _s2;
        readonly float _s3;

        public Tri(float s1, float s2, float s3)
        {
            _s1 = s1;
            _s2 = s2;
            _s3 = s3;
        }

        public Tri(float flat) : this(flat, flat, flat) { }

        public float At(int star) => star <= 1 ? _s1 : star == 2 ? _s2 : _s3;
        public int IntAt(int star) => Mathf.RoundToInt(At(star));
        public bool Any => _s1 != 0f || _s2 != 0f || _s3 != 0f;
    }

    public sealed class GlyphDef
    {
        public CardId Id;
        public GlyphFamily Family = GlyphFamily.None;

        // 伤族
        public Tri AddDamage;   // 加算，最先算
        public Tri MulDamage;   // 乘算，最后算
        public Tri Leech;       // 结算总伤转吸血池的比例
        public Tri LeechCap;    // 每波回城上限

        // 状族
        public StatusKind Status = StatusKind.None;
        public Tri Power;       // 灼烧 / 中毒 DPS，或减速后的移速系数
        public Tri Time;        // 状态时长；位族借用为落地硬直
        public Tri Radius;      // 0 表示单体
        public Tri Stacks;      // 可叠层数，0 表示不可叠

        // 位族
        public MoveAxis Axis = MoveAxis.None;
        public Tri Move;        // 纵向格数，或横向列数

        // 道族
        public Tri Ballistic;   // 追踪转向速度 / 加速倍率
        public Tri Count;       // 分路数 / 穿透数 / 雷连带数
        public Tri Decay;       // 每多打一个目标的伤害衰减
        public Tri Size;        // 视觉放大，不动碰撞半径

        // 表现
        public ShotFx Form;
        public ShotFx Trail;
        public ShotFx Halo;
        public ShotFx Orbit;
        public ShotFx Bloom;
        public int Hit = HitFx.Ink;
    }

    public sealed class WordDef
    {
        public WordId Id;
        public int Charge;
        public Tri ExecuteHp;   // 非头目直接死的血线
        public Tri ExecuteBoss; // 头目按最大生命的比例
        public Tri Count;       // 箭数 / 连斩跳数
        public Tri Damage;      // 每箭伤害
        public Tri Decay;       // 连斩每跳衰减
        public Tri Move;        // 击退距离
        public ShotFx Form;
        public int Hit = HitFx.Ink;
    }

    public static class GlyphTable
    {
        static readonly GlyphDef[] All =
        {
            // ---- 伤族 ----
            new GlyphDef
            {
                Id = CardId.Gold, Family = GlyphFamily.Hurt,
                AddDamage = new Tri(1.2f, 2.2f, 3.6f),
                Halo = ShotFx.HaloGold, Bloom = ShotFx.BloomGold, Hit = HitFx.Gold
            },
            new GlyphDef
            {
                Id = CardId.Heavy, Family = GlyphFamily.Hurt,
                MulDamage = new Tri(1.6f, 1.9f, 2.3f),
                Size = new Tri(1.15f, 1.35f, 1.6f),
                Bloom = ShotFx.BloomHeavy, Hit = HitFx.Heavy
            },
            new GlyphDef
            {
                Id = CardId.Wood, Family = GlyphFamily.Hurt,
                Leech = new Tri(0.12f, 0.20f, 0.30f),
                LeechCap = new Tri(1f, 2f, 3f),
                Orbit = ShotFx.OrbitWood, Hit = HitFx.Wood
            },

            // ---- 状族 ----
            // 火高 DPS、短、不可叠；毒低 DPS、长、可叠层。
            new GlyphDef
            {
                Id = CardId.Fire, Family = GlyphFamily.Status, Status = StatusKind.Burn,
                Power = new Tri(1.6f, 2.8f, 4.4f), Time = new Tri(3f),
                Form = ShotFx.FormFire, Hit = HitFx.Fire
            },
            new GlyphDef
            {
                Id = CardId.Poison, Family = GlyphFamily.Status, Status = StatusKind.Poison,
                Power = new Tri(1.0f, 1.7f, 2.6f), Time = new Tri(6f),
                Stacks = new Tri(2f, 3f, 4f),
                Form = ShotFx.FormPoison, Hit = HitFx.Poison
            },
            // 冰单体强控（★3 可升冻结），水范围弱控。
            new GlyphDef
            {
                Id = CardId.Ice, Family = GlyphFamily.Status, Status = StatusKind.Slow,
                Power = new Tri(0.72f, 0.55f, 0.38f), Time = new Tri(2f),
                Form = ShotFx.FormIce, Hit = HitFx.Ice
            },
            new GlyphDef
            {
                Id = CardId.Water, Family = GlyphFamily.Status, Status = StatusKind.Slow,
                Power = new Tri(0.85f, 0.75f, 0.62f), Time = new Tri(1.5f),
                Radius = new Tri(0.6f),
                Form = ShotFx.FormWater, Hit = HitFx.Water
            },
            // 雷常驻、范围、短控；晕蓄力、单体、长控。雷不额外加伤。
            new GlyphDef
            {
                Id = CardId.Thunder, Family = GlyphFamily.Status, Status = StatusKind.Stun,
                Time = new Tri(0.5f, 0.75f, 1.0f),
                Radius = new Tri(0.5f, 0.7f, 0.9f),
                Count = new Tri(1f, 2f, 3f),
                Halo = ShotFx.HaloThunder, Hit = HitFx.Thunder
            },
            new GlyphDef
            {
                Id = CardId.Stun, Family = GlyphFamily.Status, Status = StatusKind.Stun,
                Time = new Tri(0.5f, 0.75f, 1.0f),
                Halo = ShotFx.HaloStun, Hit = HitFx.Stun
            },
            new GlyphDef
            {
                Id = CardId.Confuse, Family = GlyphFamily.Status, Status = StatusKind.Confuse,
                Time = new Tri(3f, 4f, 5f),
                Power = new Tri(0.30f), // 对头目无效，只降这么多输出
                Hit = HitFx.Confuse
            },

            // ---- 位族 ----
            new GlyphDef
            {
                Id = CardId.Earth, Family = GlyphFamily.Move, Axis = MoveAxis.Column,
                Move = new Tri(0.5f, 0.8f, 1.1f), Time = new Tri(0.4f),
                Form = ShotFx.FormEarth, Hit = HitFx.Earth
            },
            new GlyphDef
            {
                Id = CardId.Wind, Family = GlyphFamily.Move, Axis = MoveAxis.Lateral,
                Move = new Tri(1f, 2f, 2f), Time = new Tri(0f, 0f, 0.3f),
                Trail = ShotFx.TrailWind, Orbit = ShotFx.OrbitWind, Hit = HitFx.Wind
            },

            // ---- 道族 ----
            // 分只裂弹，不换弹形。裂开之后每发还是原来那颗墨点。
            new GlyphDef
            {
                Id = CardId.Split, Family = GlyphFamily.Ballistic,
                Count = new Tri(2f, 3f, 4f), Decay = new Tri(0.6f)
            },
            new GlyphDef
            {
                Id = CardId.Track, Family = GlyphFamily.Ballistic,
                Ballistic = new Tri(2.4f, 4.2f, 6.4f),
                Trail = ShotFx.TrailTrack
            },
            new GlyphDef
            {
                Id = CardId.Accel, Family = GlyphFamily.Ballistic,
                Ballistic = new Tri(1.18f, 1.34f, 1.5f),
                Trail = ShotFx.TrailAccel
            },
            new GlyphDef
            {
                Id = CardId.Pierce, Family = GlyphFamily.Ballistic,
                Count = new Tri(1f, 2f, 3f), Decay = new Tri(0.85f),
                Form = ShotFx.FormPierce
            },

            // ---- 域族 ----
            new GlyphDef
            {
                Id = CardId.Explode, Family = GlyphFamily.Area,
                Radius = new Tri(0.55f, 0.82f, 1.12f), Decay = new Tri(0.6f),
                Form = ShotFx.FormExplode, Hit = HitFx.Explode
            },

            // ---- 词组字：数值在 WordTable，单字只管长相 ----
            new GlyphDef { Id = CardId.Sec, Form = ShotFx.FormKill, Hit = HitFx.Kill },
            new GlyphDef { Id = CardId.Kill, Form = ShotFx.FormKill, Hit = HitFx.Kill },
            new GlyphDef { Id = CardId.Myriad, Form = ShotFx.FormArrow, Hit = HitFx.Arrow },
            new GlyphDef { Id = CardId.Arrow, Form = ShotFx.FormArrow, Hit = HitFx.Arrow },
            new GlyphDef { Id = CardId.Strike, Form = ShotFx.FormKnock, Hit = HitFx.Knock },
            new GlyphDef { Id = CardId.Back, Form = ShotFx.FormKnock, Hit = HitFx.Knock },
            new GlyphDef { Id = CardId.Link, Form = ShotFx.FormCleave, Hit = HitFx.Cleave },
            new GlyphDef { Id = CardId.Slash, Form = ShotFx.FormCleave, Hit = HitFx.Cleave }
        };

        static readonly WordDef[] Words =
        {
            new WordDef
            {
                Id = WordId.InstantKill, Charge = 3,
                ExecuteHp = new Tri(0.35f, 0.45f, 0.60f),
                ExecuteBoss = new Tri(0.22f, 0.30f, 0.40f),
                Form = ShotFx.FormKill, Hit = HitFx.Kill
            },
            new WordDef
            {
                Id = WordId.ArrowRain, Charge = 4,
                Count = new Tri(3f, 4f, 6f),
                Damage = new Tri(2.2f, 3.2f, 4.4f),
                Form = ShotFx.FormArrow, Hit = HitFx.Arrow
            },
            new WordDef
            {
                Id = WordId.Knockback, Charge = 2,
                Move = new Tri(0.55f, 0.85f, 1.2f),
                Form = ShotFx.FormKnock, Hit = HitFx.Knock
            },
            new WordDef
            {
                Id = WordId.Cleave, Charge = 3,
                Count = new Tri(1f, 2f, 3f),
                Decay = new Tri(0.75f),
                Form = ShotFx.FormCleave, Hit = HitFx.Cleave
            }
        };

        static readonly GlyphDef Empty = new GlyphDef { Id = CardId.None };
        static readonly WordDef NoWord = new WordDef { Id = WordId.None };

        public static GlyphDef Get(CardId id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return Empty;
        }

        public static WordDef Word(WordId id)
        {
            for (int i = 0; i < Words.Length; i++)
                if (Words[i].Id == id) return Words[i];
            return NoWord;
        }

        // 硬控同时只生效一个，数大的赢；输的那个降级成等时长的缓。
        public static int HardRank(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Freeze: return 3;
                case StatusKind.Stun: return 2;
                case StatusKind.Confuse: return 1;
                default: return 0;
            }
        }

        public static bool IsHard(StatusKind kind) => HardRank(kind) > 0;

        // 冰 ★3 有几率把「缓」升级成「冻」。
        public const float IceFreezeChance = 0.35f;
        public const float IceFreezeTime = 0.45f;

        public static bool FreezeOnHit(int star) => star >= 3;
        public static bool BurnPop(int star) => star >= 2;
    }
}
