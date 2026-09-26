using UnityEngine;

namespace InkLine
{
    // 全局技能的局内部分。和字牌结算完全分开：不进 ResolveHit 的十步，
    // 各自直接改血/状态，免得域里再套域。
    public sealed partial class BattleWorld
    {
        // 局内墨。和 MetaProgress.Ink 是两个池子：这一笔打完就清。技能不花它。
        public int Ink;
        public float RageTime;
        public float RageMul = 2f;
        public int MendCasts;
        public int[] SpellRanks;
        // 长度跟着常量走。内容一律由 BeginSpells 写满，别依赖默认值。
        readonly int[] _slots = new int[GameConstants.SpellSlots];

        public int SlotSpell(int slot) => slot >= 0 && slot < _slots.Length ? _slots[slot] : -1;

        public int SlotGoldCost(int slot)
        {
            int id = SlotSpell(slot);
            return id < 0 ? 0 : SpellCatalog.Get(id).GoldCost;
        }

        public bool CanCast(int slot)
        {
            int id = SlotSpell(slot);
            if (id < 0 || Paused || Victory || Defeat) return false;
            if (Gold < SpellCatalog.Get(id).GoldCost) return false;
            return MendReady(id);
        }

        int RankOf(int id)
        {
            if (id < 0) return 1;
            if (SpellRanks == null || id >= SpellRanks.Length) return 1;
            return Mathf.Max(1, SpellRanks[id]);
        }

        bool MendReady(int id)
        {
            if (id != (int)SpellId.Mend) return true;
            int cap = RankOf(id) >= SpellCatalog.MaxLevel ? 2 : 1;
            return MendCasts < cap;
        }

        void BeginSpells(int[] equipped)
        {
            Ink = 0;
            RageTime = 0f;
            RageMul = 2f;
            MendCasts = 0;
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
            Gold -= d.GoldCost;
            GoldPop = 1f;
            switch (d.Id)
            {
                case SpellId.Burst: CastBurst(); break;
                case SpellId.Halt: CastHalt(); break;
                case SpellId.Rage: CastRage(); break;
                case SpellId.Sweep: CastSweep(); break;
                case SpellId.Splash: CastSplash(); break;
                case SpellId.Mend: CastMend(); break;
                case SpellId.Frost: CastFrost(); break;
                case SpellId.Slow: CastSlow(); break;
                case SpellId.Snipe: CastSnipe(); break;
            }
            ShowToast(d.Name);
            AudioBus.Spell(SpellPitch(d.Id));
            return true;
        }

        static float SpellPitch(SpellId id)
        {
            switch (id)
            {
                case SpellId.Frost: return 1.14f;
                case SpellId.Halt: return 0.88f;
                case SpellId.Mend: return 1.08f;
                case SpellId.Snipe: return 0.92f;
                case SpellId.Sweep: return 0.96f;
                case SpellId.Slow: return 0.9f;
                default: return 1f;
            }
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
            int lv = RankOf((int)SpellId.Burst);
            float r = 1.05f + 0.12f * lv;
            float dmg = 4f + 2f * lv;
            Vector2 at = head.Pos;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if ((e.Pos - at).sqrMagnitude > r * r) continue;
                SpellHit(e, dmg, InkTheme.Explode);
            }
            Bursts.Add(new FxBurst { Pos = at, Kind = HitFx.Explode, Tint = InkTheme.Explode, Scale = 1.7f + 0.08f * lv });
            PulseHitStop(0.12f);
            AddShake(0.5f);
        }

        void CastHalt()
        {
            int lv = RankOf((int)SpellId.Halt);
            float time = 1.2f + 0.4f * lv;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                ApplyHard(e, StatusKind.Stun, time, 0f);
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
            int lv = RankOf((int)SpellId.Rage);
            RageTime = 4f + lv;
            RageMul = 2f + (lv - 1) / 2;
            ShowFloat(new Vector2(0f, GameConstants.EmitterY + 0.9f), "强攻", InkTheme.Fire, 1.3f);
        }

        void CastSweep()
        {
            int lv = RankOf((int)SpellId.Sweep);
            float dmg = 3f + 2f * lv;
            float knock = 0.7f + 0.2f * lv;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                e.Pos.y = Mathf.Min(GameConstants.SpawnY - 0.35f, e.Pos.y + knock);
                SpellHit(e, dmg, InkTheme.Ink);
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
            int lv = RankOf((int)SpellId.Splash);
            float dps = 2f + lv;
            float time = 2.2f + 0.6f * lv;
            float bx = FieldLayout.ColumnX(best);
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if (Mathf.Abs(e.Pos.x - bx) > GameConstants.CellWidth * 0.55f) continue;
                e.BurnDps = Mathf.Max(e.BurnDps, dps);
                e.BurnTime = Mathf.Max(e.BurnTime, time);
            }
            Bursts.Add(new FxBurst
            {
                Pos = new Vector2(bx, GameConstants.GridCenterY),
                Kind = HitFx.Poison, Tint = InkTheme.PoisonHi, Scale = 2f
            });
        }

        void CastMend()
        {
            MendCasts++;
            int heal = 1 + (RankOf((int)SpellId.Mend) - 1) / 2;
            if (BaseHp >= MaxBaseHp) return;
            BaseHp = Mathf.Min(MaxBaseHp, BaseHp + heal);
            Push(PopKind.Heal, 0, new Vector2(0f, GameConstants.EmitterY + 0.7f), "+" + heal, InkTheme.Heart, 1.3f, 1f);
        }

        void CastFrost()
        {
            int lv = RankOf((int)SpellId.Frost);
            float dmg = 3f + lv;
            float factor = Mathf.Max(0.4f, 0.58f - 0.04f * (lv - 1));
            float time = 1.6f + 0.3f * lv;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                SpellHit(e, dmg, InkTheme.IceHi);
                if (e.Dead) continue;
                e.Slow = Mathf.Min(e.Slow, factor);
                e.SlowTime = Mathf.Max(e.SlowTime, time);
            }
            Bursts.Add(new FxBurst
            {
                Pos = new Vector2(0f, GameConstants.GridCenterY),
                Kind = HitFx.Ice, Tint = InkTheme.IceHi, Scale = 2.4f
            });
            PulseHitStop(0.1f);
            AddShake(0.36f);
        }

        void CastSlow()
        {
            int lv = RankOf((int)SpellId.Slow);
            float factor = Mathf.Max(0.35f, 0.62f - 0.05f * (lv - 1));
            float time = 2.4f + 0.45f * lv;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                e.Slow = Mathf.Min(e.Slow, factor);
                e.SlowTime = Mathf.Max(e.SlowTime, time);
            }
            Bursts.Add(new FxBurst
            {
                Pos = new Vector2(0f, GameConstants.GridCenterY),
                Kind = HitFx.Knock, Tint = InkTheme.WaterHi, Scale = 2f
            });
        }

        void CastSnipe()
        {
            EnemyActor head = FrontMost();
            if (head == null) return;
            int lv = RankOf((int)SpellId.Snipe);
            SpellHit(head, 10f + 5f * lv, InkTheme.Thunder);
            Bursts.Add(new FxBurst { Pos = head.Pos, Kind = HitFx.Heavy, Tint = InkTheme.ThunderHi, Scale = 1.6f });
            PulseHitStop(0.08f);
            AddShake(0.32f);
        }
    }
}
