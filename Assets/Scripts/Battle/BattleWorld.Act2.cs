using UnityEngine;

namespace InkLine
{
    // 第二幕的新动词和新敌人：返（折返）、聚（向心）、墨影（隐身）、墨僧（挂盾）、
    // 钟魔（敲钟）、影姬（分幻影）。破和千钧只是 ResolveHit 里的两行，留在那边。
    public sealed partial class BattleWorld
    {
        // 返：飞过出怪线再往上一点就折回来；回程掉到漏怪线以下就算飞完。
        const float ReboundTop = GameConstants.SpawnY + 0.35f;
        static float ReboundFloor => GameConstants.LeakY - 0.5f;
        // 分裂弹每发都会折返，基础回程系数封顶，免得「分 + 返」翻倍。
        // 烈度在封顶之后再乘，金印是 0.5 × 1.22，不会把裂弹抬回满额折返。
        const float ReboundSplitCap = 0.5f;

        // 钟魔进场后多久敲第一下。
        const float BellFirst = 4f;
        // 墨僧一次最多给几只挂盾。
        const int AuraCap = 3;

        void Turn(BulletActor b, EnemyActor last)
        {
            ShotMods m = b.Mods;
            SignaturePairs.Tune(m);
            b.Returning = true;
            b.Mother = false;
            float k = m.Riposte ? 1f : m.Rebound;
            if (m.Has(CardId.Split)) k = Mathf.Min(k, ReboundSplitCap);
            if (!m.Riposte) k *= 1f + SealFierce(CardId.Rebound);
            m.Decay *= k;
            if (m.Riposte) m.Pierce += 1;
            float speed = b.Vel.magnitude;
            b.Vel = new Vector2(b.Vel.x * 0.3f, -Mathf.Max(0.5f, Mathf.Abs(b.Vel.y))).normalized * speed;
            b.HitIds.Clear();
            if (last != null) b.HitIds.Add(last.Id);
            if (m.Riposte)
            {
                PairPop(PairFx.Riposte, b.Pos, b.Pos + b.Vel.normalized, 1f);
                ShowFloat(b.Pos + Vector2.up * 0.2f, "回锋", InkTheme.GraphiteHi, 1.05f);
            }
            if (m.FireWheel)
            {
                // 回程再吹远一列。去程那一下在折返之前已经结算过了。
                m.PushLateral += 1;
                PairPop(PairFx.FireWheel, b.Pos, 3.4f);
                ShowFloat(b.Pos + Vector2.up * 0.28f, "轮", InkTheme.Fire, 1.45f);
                AddShake(0.22f);
            }
        }

        // 聚：命中点一圈里的敌人往中心拉。头目、推不动的、还隐着身的不拉。
        void Gather(BulletActor b, EnemyActor hit)
        {
            ShotMods m = b.Mods;
            Vector2 c = hit.Pos;
            int n = 0;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || e.Doom > 0f || e.Hidden || e.Id == hit.Id || e.IsBoss || e.NoKnock) continue;
                Vector2 d = e.Pos - c;
                float reach = m.GatherR + e.Radius;
                if (d.sqrMagnitude > reach * reach) continue;
                float dist = d.magnitude;
                // 拉到和中心那只身子挨着就停，不叠进去。
                float room = Mathf.Max(0f, dist - (e.Radius + hit.Radius) * 0.7f);
                float pull = m.Whirl ? room : Mathf.Min(room, m.GatherPull);
                if (dist > 0.001f) e.Pos -= d / dist * pull;
                if (m.Whirl) e.Pos.x = Mathf.Lerp(e.Pos.x, c.x, 0.85f);
                e.Pos.y = Mathf.Clamp(e.Pos.y, GameConstants.LeakY + 0.4f, GameConstants.SpawnY - 0.35f);
                if (m.GatherHold > 0f) e.HoldTime = Mathf.Max(e.HoldTime, m.GatherHold);
                if (m.Pin) PinDown(e);
                MarkHurt(e, m.MarkGather);
                n++;
            }
            if (m.Pin) PinDown(hit);
            if (n == 0 && !m.Pin) return;
            Bursts.Add(new FxBurst
            {
                Pos = c, Kind = HitFx.Wind, Tint = InkTheme.GatherHi, Scale = 0.9f + m.GatherR
            });
            if (m.Whirl) PairPop(PairFx.Whirl, c, m.GatherR);
            if (m.Pin)
            {
                PairPop(PairFx.Needle, c, Mathf.Max(0.8f, m.GatherR));
                AddShake(0.18f);
            }
            ShowFloat(c + Vector2.up * 0.22f, m.Pin ? "针" : m.Whirl ? "旋" : "聚",
                m.Pin ? InkTheme.Ice : InkTheme.Gather, m.Pin ? 1.4f : m.Whirl ? 1.2f : 0.95f);
        }

        // 定海针：缓到四成，不占冻的硬控位。头目钉一半时间。
        void PinDown(EnemyActor e)
        {
            if (e == null || e.Dead) return;
            e.Slow = Mathf.Min(e.Slow, 0.4f);
            e.SlowTime = Mathf.Max(e.SlowTime, e.IsBoss ? 1f : 2f);
        }

        void Reveal(EnemyActor e)
        {
            e.Hidden = false;
            ShowFloat(e.Pos + Vector2.up * 0.26f, "显", InkTheme.Ink, 1.0f);
            Bursts.Add(new FxBurst { Pos = e.Pos, Kind = HitFx.Ink, Tint = Color.white, Scale = 1.1f });
        }

        void TickAct2(EnemyActor e, float dt)
        {
            if (e.VulnTime > 0f)
            {
                e.VulnTime -= dt;
                if (e.VulnTime <= 0f) e.Vuln = 0f;
            }
            if (e.MarkTime > 0f)
            {
                e.MarkTime -= dt;
                if (e.MarkTime <= 0f) e.MarkAmp = 0f;
            }
            if (e.SkillSeal > 0f)
            {
                e.SkillSeal -= dt;
                if (e.Hidden) Reveal(e);
            }
            // 封着的时候不分裂幻影、不挂盾、不敲钟。
            if (e.Phantoms > 0 && e.SkillSeal <= 0f && e.Hp <= e.MaxHp * 0.5f) SpawnPhantoms(e);
            if (e.SkillSeal > 0f) return;
            if (e.ShieldAura <= 0f && e.Bell <= 0f) return;
            // 被定住（冻 / 晕）的施法者不施法。
            if (e.Held) return;
            e.AuraCd -= dt;
            if (e.AuraCd > 0f) return;
            if (e.Bell > 0f)
            {
                e.AuraCd = e.Bell;
                RingBell(e);
            }
            else
            {
                e.AuraCd = e.ShieldAura;
                ShieldNearby(e);
            }
        }

        void ShieldNearby(EnemyActor src)
        {
            float r2 = EnemyCatalog.HealRange * EnemyCatalog.HealRange;
            int n = 0;
            for (int i = 0; i < Enemies.Count && n < AuraCap; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || e.Id == src.Id || e.Shield || e.Doom > 0f) continue;
                if ((e.Pos - src.Pos).sqrMagnitude > r2) continue;
                e.Shield = true;
                e.ShieldGiven = true;
                n++;
            }
            if (n > 0) ShowFloat(src.Pos + Vector2.up * 0.3f, "护", InkTheme.Gather, 0.95f);
        }

        void RingBell(EnemyActor src)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || e.Shield || e.Doom > 0f) continue;
                e.Shield = true;
                e.ShieldGiven = true;
            }
            ShowFloat(src.Pos + Vector2.up * 0.5f, "钟", InkTheme.Word, 1.5f);
            Bursts.Add(new FxBurst
            {
                Pos = src.Pos, Kind = HitFx.Knock, Tint = InkTheme.GoldHi, Scale = 2.2f
            });
            AudioBus.Spell(0.78f);
            AddShake(0.26f);
        }

        // 影姬半血：左右各分一只幻影，跟着本体这一波算。
        void SpawnPhantoms(EnemyActor from)
        {
            int count = from.Phantoms;
            from.Phantoms = 0;
            float min = FieldLayout.ColumnX(0);
            float max = FieldLayout.ColumnX(GameConstants.Columns - 1);
            for (int i = 0; i < count; i++)
            {
                float side = count == 1 ? 1f : (i % 2 == 0 ? -1f : 1f) * (1 + i / 2);
                var at = new Vector2(Mathf.Clamp(from.Pos.x + side * 0.9f, min, max), from.Pos.y + 0.2f);
                EnemyActor p = Make(EnemyId.Phantom, at, from.HpShare, from.Wave);
                p.StrafeDir = side;
                Enemies.Add(p);
            }
            AddShake(0.2f);
        }
    }
}
