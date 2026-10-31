using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 灼烧的火色。普通火之外，三对和火有关的秘卷各有一种火，挂在怪身上一眼看得出是哪对在烧。
    public static class BurnLooks
    {
        public const int Plain = 0;
        public const int Frost = 1;    // 霜火：冰蓝冷焰
        public const int Blight = 2;   // 燎毒：毒紫描边、酸绿火身
        public const int Molten = 3;   // 熔金：金水火

        public static string Frames(int look)
        {
            switch (look)
            {
                case Frost: return "burn_frost";
                case Blight: return "burn_blight";
                case Molten: return "burn_molten";
                default: return "burn_body";
            }
        }

        public static Color Number(int look)
        {
            switch (look)
            {
                case Frost: return InkTheme.Ice;
                case Blight: return InkTheme.PoisonHi;
                case Molten: return InkTheme.CoinFace;
                default: return InkTheme.Fire;
            }
        }
    }

    // 秘卷触发那一下的表现投递单。结算已经在世界里做完，视图只管画。
    public struct PairFx
    {
        public const int Scorch = 1;      // 焦雷：晕一结束，天上劈下一道带火的雷
        public const int Shatter = 2;     // 霰雷：冻住的怪被打碎冰
        public const int Conduct = 3;     // 导电：命中点一圈水面通电
        public const int WardGold = 4;    // 镇金：金印砸下
        public const int Leech = 5;       // 蚀生：毒伤化成绿光飞回城墙
        public const int RamEarth = 6;    // 夯土：被夯回去，落点砸出裂地
        public const int ColdWind = 7;    // 寒风：横移拖一道霜，落点结霜
        public const int ThunderCut = 8;  // 雷决：斩杀时一道靛紫雷刃
        public const int Crumble = 9;     // 崩山：甲被崩碎
        public const int Collapse = 10;   // 坍缩：爆前往里一缩
        public const int Whirl = 11;      // 旋风：一团卷成漩涡
        public const int Riposte = 12;    // 回锋：折返那一下亮锋
        public const int FireWheel = 13;  // 风火轮
        public const int Cloud = 14;      // 穿云箭
        public const int Needle = 15;     // 定海针
        public const int Seal = 16;       // 封
        public const int Ghost = 17;      // 影弹
        public const int Soul = 18;       // 失魂

        public int Kind;
        public Vector2 A;
        public Vector2 B;
        public float Size;
    }

    // 烈爆留在地上的火。走进来的怪也会被点着。
    public sealed class FireField
    {
        public int Id;
        public Vector2 Pos;
        public float R;
        public float Life;
        public float Max;
        public float Dps;
        public int Look;
    }

    public sealed partial class BattleWorld
    {
        public const float BlazeTime = 2f;
        // 火地每隔一会儿给圈里的怪续一小段灼烧，离开火地后很快熄掉。
        const float FieldBurnHold = 0.4f;
        // 蚀生绿光飞回城墙的间隔，太密会糊成一条线。
        const float LeechFxGap = 0.45f;

        public readonly List<PairFx> PairFxs = new List<PairFx>();
        public readonly List<FireField> Fields = new List<FireField>();
        int _fieldSeq;

        void PairPop(int kind, Vector2 a, float size = 1f) => PairPop(kind, a, a, size);

        void PairPop(int kind, Vector2 a, Vector2 b, float size)
        {
            if (PairFxs.Count >= 48) return;
            PairFxs.Add(new PairFx { Kind = kind, A = a, B = b, Size = size });
        }

        void LightField(Vector2 pos, float r, float dps, int look)
        {
            if (dps <= 0f || r <= 0f) return;
            Fields.Add(new FireField
            {
                Id = ++_fieldSeq, Pos = pos, R = r, Life = BlazeTime, Max = BlazeTime, Dps = dps, Look = look
            });
        }

        void TickFields(float dt)
        {
            for (int i = Fields.Count - 1; i >= 0; i--)
            {
                FireField f = Fields[i];
                f.Life -= dt;
                if (f.Life <= 0f)
                {
                    Fields.RemoveAt(i);
                    continue;
                }
                for (int n = 0; n < Enemies.Count; n++)
                {
                    EnemyActor e = Enemies[n];
                    if (e.Dead || e.Doom > 0f || e.Hidden) continue;
                    float reach = f.R + e.Radius * 0.6f;
                    if ((e.Pos - f.Pos).sqrMagnitude > reach * reach) continue;
                    e.BurnDps = Mathf.Max(e.BurnDps, f.Dps);
                    e.BurnTime = Mathf.Max(e.BurnTime, FieldBurnHold);
                    e.BurnLook = Mathf.Max(e.BurnLook, f.Look);
                }
            }
        }

        void TickLeechFx(EnemyActor e, float dt)
        {
            e.LeechFxCd -= dt;
            if (e.LeechFxCd > 0f) return;
            e.LeechFxCd = LeechFxGap;
            PairPop(PairFx.Leech, e.Pos, new Vector2(0f, GameConstants.EmitterY + 0.7f), 1f);
        }

        // 焦雷：晕结束补伤害那一下。天上劈一道雷进火里。
        void ScorchStrike(EnemyActor e)
        {
            Arcs.Add(new ArcFx { A = e.Pos + new Vector2(0.18f, 3.2f), B = e.Pos, Tint = InkTheme.Thunder });
            Arcs.Add(new ArcFx { A = e.Pos + new Vector2(-0.3f, 2.4f), B = e.Pos, Tint = InkTheme.Fire });
            Bursts.Add(new FxBurst { Pos = e.Pos, Kind = HitFx.Fire, Tint = Color.white, Scale = 1.7f });
            PairPop(PairFx.Scorch, e.Pos, e.Radius);
            ShowFloat(e.Pos + Vector2.up * 0.36f, "焦", InkTheme.ThunderHi, 1.3f);
            AudioBus.Zap();
            AudioBus.HitFire();
            AddShake(0.14f);
        }

        // 导电：水面一圈通电，圈里随手再甩几道小电弧。
        void ConductRing(Vector2 at, float r)
        {
            PairPop(PairFx.Conduct, at, r);
            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f + Random.Range(-0.08f, 0.08f)) * Mathf.PI * 2f;
                Arcs.Add(new ArcFx { A = at, B = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, Tint = InkTheme.Water });
            }
        }

        // 雷决：真斩下去时，一道雷从天上劈到刀口。
        void ThunderCutStrike(EnemyActor e)
        {
            Arcs.Add(new ArcFx { A = e.Pos + new Vector2(0f, 3.4f), B = e.Pos, Tint = InkTheme.Thunder });
            PairPop(PairFx.ThunderCut, e.Pos, e.Radius);
            ShowFloat(e.Pos + Vector2.up * 0.5f, "雷决", InkTheme.ThunderHi, 1.2f);
            AudioBus.Zap();
        }
    }
}
