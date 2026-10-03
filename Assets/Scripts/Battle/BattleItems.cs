using UnityEngine;

namespace InkLine
{
    // 道具的局内部分。和字牌结算完全分开：不进 ResolveHit 的十步，
    // 各自直接改血/状态，免得域里再套域。
    // 不花金币：冷却一好就进入「就绪」，等场面对得上才自己丢出去，不白放。
    public sealed partial class BattleWorld
    {
        // 局内墨：这一局拾到的墨，通关时整笔进 MetaProgress.Ink。道具不花它。
        public int Ink;
        public float RageTime;
        public float RageMul = 2f;
        public int MendCasts;
        public int[] ItemRanks;
        // 长度跟着常量走。内容一律由 BeginItems 写满，别依赖默认值。
        readonly int[] _slots = new int[GameConstants.ItemSlots];
        readonly float[] _itemCd = new float[GameConstants.ItemSlots];
        readonly float[] _itemCdMax = new float[GameConstants.ItemSlots];
        // 刚丢出去时 1，HUD 拿去把图标弹一下，自己衰减。
        public readonly float[] ItemFlash = new float[GameConstants.ItemSlots];
        // 刚冷却好时 1，HUD 拿去闪一下。
        public readonly float[] ItemReadyFlash = new float[GameConstants.ItemSlots];

        // 开局先给半个冷却，开战第一秒场上没怪，丢了也是白丢。
        const float OpeningCd = 0.5f;

        // 丢出去那一刻发给 HUD 演出，HUD 每帧取走清空。
        public struct ItemCast
        {
            public ItemId Id;
            public int Slot;
            public int Rank;
            public EnemyActor Target;   // 弹弓、鞭炮瞄的那只
            public int Column;          // 辣椒酱浇的那一列
            public float Impact;        // 多少秒后真正生效，演出照这个对时
        }

        public readonly System.Collections.Generic.List<ItemCast> ItemCasts =
            new System.Collections.Generic.List<ItemCast>();

        // 演出要先飞过去、先亮个相，伤害等它落地那一刻再结算。
        struct PendingCast
        {
            public ItemId Id;
            public float Left;
            public EnemyActor Target;
            public int Column;
        }

        readonly System.Collections.Generic.List<PendingCast> _pending =
            new System.Collections.Generic.List<PendingCast>();

        public static float ImpactDelay(ItemId id)
        {
            switch (id)
            {
                case ItemId.Snipe: return 0.42f;
                case ItemId.Burst: return 0.5f;
                case ItemId.Slow: return 0.42f;
                case ItemId.Halt: return 0.62f;
                case ItemId.Rage: return 0.55f;
                case ItemId.Frost: return 0.6f;
                case ItemId.Sweep: return 0.95f;
                case ItemId.Splash: return 0.95f;
                case ItemId.Mend: return 0.95f;
                default: return 0.3f;
            }
        }

        public int SlotItem(int slot) => slot >= 0 && slot < _slots.Length ? _slots[slot] : -1;

        public bool ItemsSealed => Stage != null && Stage.Has(StageRule.NoItem);

        // 0 刚丢完，1 冷却好了。
        public float SlotCharge(int slot)
        {
            if (slot < 0 || slot >= _slots.Length || _itemCdMax[slot] <= 0f) return 0f;
            return Mathf.Clamp01(1f - _itemCd[slot] / _itemCdMax[slot]);
        }

        public bool SlotSpent(int slot)
        {
            int id = SlotItem(slot);
            return id == (int)ItemId.Mend && !MendReady(id);
        }

        // 道具伤害的标尺：一发不带字的炮弹打多少，含皮肤加成和锻造伤害线。
        // 道具全写成它的倍数，否则「满级鞭炮 14 点」打第八章 140 血的墨尊毫无意义。
        // 「能量饮料」不进来 —— 它是临时增益，叠上道具会让一套连招直接抹掉半场。
        public float ShotBase => (ShotMods.DefaultBase + _skinDamage) * _damageMul;

        int RankOf(int id)
        {
            if (id < 0) return 1;
            if (ItemRanks == null || id >= ItemRanks.Length) return 1;
            return Mathf.Max(1, ItemRanks[id]);
        }

        bool MendReady(int id)
        {
            if (id != (int)ItemId.Mend) return true;
            return MendCasts < ItemCatalog.MendCap(RankOf(id));
        }

        void BeginItems(int[] equipped)
        {
            Ink = 0;
            RageTime = 0f;
            RageMul = 2f;
            MendCasts = 0;
            ItemCasts.Clear();
            _pending.Clear();
            for (int i = 0; i < _slots.Length; i++)
            {
                int id = equipped != null && i < equipped.Length ? equipped[i] : -1;
                _slots[i] = id >= 0 && id < ItemCatalog.Count ? id : -1;
                float cd = _slots[i] >= 0 ? ItemCatalog.CooldownAt(ItemCatalog.Get(_slots[i]), RankOf(_slots[i])) : 0f;
                _itemCdMax[i] = cd;
                _itemCd[i] = cd * OpeningCd;
                ItemFlash[i] = 0f;
                ItemReadyFlash[i] = 0f;
            }
        }

        void TickItems(float dt)
        {
            if (ItemsSealed || PreviewFill) return;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingCast p = _pending[i];
                p.Left -= dt;
                if (p.Left > 0f) { _pending[i] = p; continue; }
                _pending.RemoveAt(i);
                Land(p);
            }
            for (int i = 0; i < _slots.Length; i++)
            {
                int id = _slots[i];
                if (id < 0 || SlotSpent(i)) continue;
                if (_itemCd[i] > 0f)
                {
                    _itemCd[i] -= dt;
                    if (_itemCd[i] > 0f) continue;
                    _itemCd[i] = 0f;
                    ItemReadyFlash[i] = 1f;
                    AudioBus.ItemReady();
                }
                if (!Wants((ItemId)id)) continue;
                FireItem(i);
            }
        }

        // 场面对不对得上。对不上就一直挂着「就绪」，等下一次机会。
        bool Wants(ItemId id)
        {
            int alive = 0;
            int near = 0;
            int mid = 0;
            bool boss = false;
            float danger = FieldLayout.GridTop + 1.8f;
            float half = (FieldLayout.GridTop + GameConstants.SpawnY) * 0.5f;
            EnemyActor head = null;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                alive++;
                if (e.IsBoss) boss = true;
                if (e.Pos.y < danger) near++;
                if (e.Pos.y < half) mid++;
                if (head == null || e.Pos.y < head.Pos.y) head = e;
            }
            switch (id)
            {
                case ItemId.Snipe: return alive > 0;
                case ItemId.Slow: return mid > 0;
                case ItemId.Burst: return head != null && CountAround(head.Pos, 1.05f + 0.12f * RankOf((int)id)) >= 2;
                case ItemId.Halt: return near > 0 || alive >= 5;
                case ItemId.Frost: return alive >= 4;
                case ItemId.Rage: return alive >= 3 || boss;
                case ItemId.Sweep: return alive >= 6 || near > 0;
                case ItemId.Splash: return BusiestColumn(out int n) >= 0 && n >= 3;
                case ItemId.Mend: return BaseHp < MaxBaseHp;
                default: return alive > 0;
            }
        }

        int CountAround(Vector2 at, float r)
        {
            int n = 0;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (!e.Dead && (e.Pos - at).sqrMagnitude <= r * r) n++;
            }
            return n;
        }

        int BusiestColumn(out int most)
        {
            int best = -1;
            most = 0;
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
            return best;
        }

        void FireItem(int slot)
        {
            ItemDef d = ItemCatalog.Get(SlotItem(slot));
            var p = new PendingCast { Id = d.Id, Left = ImpactDelay(d.Id), Column = -1 };
            if (d.Id == ItemId.Burst || d.Id == ItemId.Snipe) p.Target = FrontMost();
            if (d.Id == ItemId.Splash) p.Column = BusiestColumn(out _);
            // 急救包的次数要在丢出去时就记上，否则落地前 SlotSpent 还是 false。
            if (d.Id == ItemId.Mend) MendCasts++;
            _pending.Add(p);
            ItemCasts.Add(new ItemCast
            {
                Id = d.Id, Slot = slot, Rank = RankOf((int)d.Id),
                Target = p.Target, Column = p.Column, Impact = p.Left
            });
            _itemCd[slot] = _itemCdMax[slot];
            ItemFlash[slot] = 1f;
            AudioBus.Item(d.Id);
        }

        void Land(PendingCast p)
        {
            switch (p.Id)
            {
                case ItemId.Burst: CastBurst(p.Target); break;
                case ItemId.Halt: CastHalt(); break;
                case ItemId.Rage: CastRage(); break;
                case ItemId.Sweep: CastSweep(); break;
                case ItemId.Splash: CastSplash(p.Column); break;
                case ItemId.Mend: CastMend(); break;
                case ItemId.Frost: CastFrost(); break;
                case ItemId.Slow: CastSlow(); break;
                case ItemId.Snipe: CastSnipe(p.Target); break;
            }
        }

        // 瞄的那只在飞行途中被打死了，就换当时最前面的。
        EnemyActor Aim(EnemyActor target) => target != null && !target.Dead ? target : FrontMost();

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

        void CastBurst(EnemyActor target)
        {
            EnemyActor head = Aim(target);
            if (head == null) return;
            int lv = RankOf((int)ItemId.Burst);
            float r = 1.05f + 0.12f * lv;
            float dmg = ShotBase * ItemCatalog.BurstMul(lv);
            Vector2 at = head.Pos;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if ((e.Pos - at).sqrMagnitude > r * r) continue;
                SpellHit(e, dmg, InkTheme.Explode);
            }
            Bursts.Add(new FxBurst { Pos = at, Kind = HitFx.Explode, Tint = InkTheme.Explode, Scale = r * BlastScale });
            PulseHitStop(0.12f);
            AddShake(0.5f);
        }

        void CastHalt()
        {
            int lv = RankOf((int)ItemId.Halt);
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
            int lv = RankOf((int)ItemId.Rage);
            RageTime = 4f + lv;
            RageMul = 2f + (lv - 1) / 2;
        }

        void CastSweep()
        {
            int lv = RankOf((int)ItemId.Sweep);
            float dmg = ShotBase * ItemCatalog.SweepMul(lv);
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

        void CastSplash(int column)
        {
            int best = column >= 0 ? column : BusiestColumn(out _);
            if (best < 0) return;
            int lv = RankOf((int)ItemId.Splash);
            float dps = ShotBase * ItemCatalog.SplashMul(lv);
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
            int heal = 1 + (RankOf((int)ItemId.Mend) - 1) / 2;
            if (BaseHp >= MaxBaseHp) return;
            BaseHp = Mathf.Min(MaxBaseHp, BaseHp + heal);
            Push(PopKind.Heal, 0, new Vector2(0f, GameConstants.EmitterY + 0.7f), "+" + heal, InkTheme.Heart, 1.3f, 1f);
        }

        void CastFrost()
        {
            int lv = RankOf((int)ItemId.Frost);
            float dmg = ShotBase * ItemCatalog.FrostMul(lv);
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
            int lv = RankOf((int)ItemId.Slow);
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

        void CastSnipe(EnemyActor target)
        {
            EnemyActor head = Aim(target);
            if (head == null) return;
            int lv = RankOf((int)ItemId.Snipe);
            SpellHit(head, ShotBase * ItemCatalog.SnipeMul(lv), InkTheme.Thunder);
            Bursts.Add(new FxBurst { Pos = head.Pos, Kind = HitFx.Heavy, Tint = InkTheme.ThunderHi, Scale = 1.6f });
            PulseHitStop(0.08f);
            AddShake(0.32f);
        }
    }
}
