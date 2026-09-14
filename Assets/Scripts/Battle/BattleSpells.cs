using UnityEngine;

namespace InkLine
{
    // 全局技能的局内部分。和字牌结算完全分开：不进 ResolveHit 的十步，
    // 各自直接改血/状态，免得域里再套域。
    public sealed partial class BattleWorld
    {
        // 局内墨。和 MetaProgress.Ink 是两个池子：这一笔打完就清。
        public int Ink;
        public float RageTime;
        public bool MendUsed;
        // 长度跟着常量走。内容一律由 BeginSpells 写满，别依赖默认值。
        readonly int[] _slots = new int[GameConstants.SpellSlots];

        public int SlotSpell(int slot) => slot >= 0 && slot < _slots.Length ? _slots[slot] : -1;

        public int SlotInkCost(int slot)
        {
            int id = SlotSpell(slot);
            return id < 0 ? 0 : SpellCatalog.Get(id).InkCost;
        }

        public bool CanCast(int slot)
        {
            int id = SlotSpell(slot);
            if (id < 0 || Paused || Victory || Defeat) return false;
            if (Ink < SpellCatalog.Get(id).InkCost) return false;
            return id != (int)SpellId.Mend || !MendUsed;
        }

        void BeginSpells(int[] equipped)
        {
            Ink = 0;
            RageTime = 0f;
            MendUsed = false;
            for (int i = 0; i < _slots.Length; i++)
            {
                int id = equipped != null && i < equipped.Length ? equipped[i] : -1;
                _slots[i] = id >= 0 && id < SpellCatalog.Count ? id : -1;
            }
        }

        public bool CastSpell(int slot)
        {
            if (!CanCast(slot)) return false;
            SpellDef d = SpellCatalog.Get(SlotSpell(slot));
            Ink -= d.InkCost;
            switch (d.Id)
            {
                case SpellId.Burst: CastBurst(); break;
                case SpellId.Halt: CastHalt(); break;
                case SpellId.Rage: CastRage(); break;
                case SpellId.Sweep: CastSweep(); break;
                case SpellId.Splash: CastSplash(); break;
                case SpellId.Mend: CastMend(); break;
            }
            ShowToast(d.Name);
            return true;
        }

        EnemyActor FrontMost()
        {
            EnemyActor best = null;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if (best == null || e.Pos.y < best.Pos.y) best = e;
            }
            return best;
        }

        void SpellHit(EnemyActor e, float dmg, Color tint)
        {
            e.Hp -= dmg;
            e.HitFlash = 0.18f;
            ShowDamage(e, dmg, tint, 1.1f, true);
            if (e.Hp <= 0f) Kill(e, null);
        }

        void CastBurst()
        {
            EnemyActor head = FrontMost();
            if (head == null) return;
            Vector2 at = head.Pos;
            const float r = 1.2f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if ((e.Pos - at).sqrMagnitude > r * r) continue;
                SpellHit(e, 6f, InkTheme.Explode);
            }
            Bursts.Add(new FxBurst { Pos = at, Kind = HitFx.Explode, Tint = InkTheme.Explode, Scale = 1.9f });
            PulseHitStop(0.12f);
            AddShake(0.5f);
        }

        void CastHalt()
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                ApplyHard(e, StatusKind.Stun, 1.6f, 0f);
            }
            Bursts.Add(new FxBurst
            {
                Pos = new Vector2(0f, GameConstants.GridCenterY),
                Kind = HitFx.Stun, Tint = InkTheme.Word, Scale = 2.2f
            });
            PulseHitStop(0.1f);
            AddShake(0.34f);
        }

        void CastRage()
        {
            RageTime = 5f;
            ShowFloat(new Vector2(0f, GameConstants.EmitterY + 0.9f), "强攻", InkTheme.Fire, 1.3f);
        }

        void CastSweep()
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                e.Pos.y = Mathf.Min(GameConstants.SpawnY - 0.35f, e.Pos.y + 0.9f);
                SpellHit(e, 5f, InkTheme.Ink);
            }
            Bursts.Add(new FxBurst
            {
                Pos = new Vector2(0f, GameConstants.GridCenterY),
                Kind = HitFx.Knock, Tint = InkTheme.Ink, Scale = 2.4f
            });
            PulseHitStop(0.14f);
            AddShake(0.55f);
        }

        void CastSplash()
        {
            int best = -1;
            int most = 0;
            for (int c = 0; c < GameConstants.Columns; c++)
            {
                int n = 0;
                float x = FieldLayout.ColumnX(c);
                for (int i = 0; i < Enemies.Count; i++)
                {
                    EnemyActor e = Enemies[i];
                    if (e.Dead) continue;
                    if (Mathf.Abs(e.Pos.x - x) <= GameConstants.CellWidth * 0.55f) n++;
                }
                if (n > most) { most = n; best = c; }
            }
            if (best < 0) return;
            float bx = FieldLayout.ColumnX(best);
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if (Mathf.Abs(e.Pos.x - bx) > GameConstants.CellWidth * 0.55f) continue;
                e.BurnDps = Mathf.Max(e.BurnDps, 3f);
                e.BurnTime = Mathf.Max(e.BurnTime, 3f);
            }
            Bursts.Add(new FxBurst
            {
                Pos = new Vector2(bx, GameConstants.GridCenterY),
                Kind = HitFx.Poison, Tint = InkTheme.PoisonHi, Scale = 2f
            });
        }

        void CastMend()
        {
            MendUsed = true;
            if (BaseHp >= MaxBaseHp) return;
            BaseHp++;
            Push(PopKind.Heal, 0, new Vector2(0f, GameConstants.EmitterY + 0.7f), "+1", InkTheme.Heart, 1.3f, 1f);
        }
    }
}
