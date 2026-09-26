using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public enum ChestKind { Gold, Ink }

    // 地上的宝箱。不是敌人：不走、不打基地、不算进波次和胜负，
    // 只挨炮弹。打破才出东西，放着不管就过期消失 —— 值不值得挪炮过去打，是玩家自己的账。
    public sealed class ChestActor
    {
        public int Id;
        public ChestKind Kind;
        public Vector2 Pos;
        public float Hp;
        public float MaxHp;
        public int Amount;
        public float Age;
        public float Life;
        public float HitFlash;
        public float Seed;
        public bool Dead;

        public bool Cracked => Hp < MaxHp * 0.5f;
        public float Left => Life - Age;
    }

    public sealed partial class BattleWorld
    {
        public readonly List<ChestActor> Chests = new List<ChestActor>();

        public const float ChestLife = 7f;
        public const float ChestBlink = 2f;
        public const float ChestRadius = 0.34f;
        const int ChestCap = 2;
        const float ChestHpBase = 5f;

        float _inkCarry;

        void ResetChests()
        {
            Chests.Clear();
            _inkCarry = 0f;
        }

        void RollChest(EnemyActor e)
        {
            if (PreviewFill || Stage == null) return;
            if (!e.IsBoss && Random.value >= StageCatalog.ChestChance) return;
            if (Chests.Count >= ChestCap) return;
            bool gold = Random.value < StageCatalog.ChestGoldShare;
            int col = FieldLayout.ColumnAtX(e.Pos.x);
            float low = FieldLayout.GridTop + 0.9f;
            float high = GameConstants.SpawnY - 1.6f;
            Chests.Add(new ChestActor
            {
                Id = NextActorId++,
                Kind = gold ? ChestKind.Gold : ChestKind.Ink,
                Pos = new Vector2(FieldLayout.ColumnX(col), Mathf.Clamp(e.Pos.y, low, high)),
                Hp = ChestHpBase * Stage.Hp,
                MaxHp = ChestHpBase * Stage.Hp,
                Amount = gold ? Stage.ChestGold : Stage.ChestInk,
                Life = ChestLife,
                Seed = Random.value * 10f
            });
        }

        void TickChests(float dt)
        {
            for (int i = Chests.Count - 1; i >= 0; i--)
            {
                ChestActor c = Chests[i];
                c.Age += dt;
                if (c.HitFlash > 0f) c.HitFlash -= dt;
                if (c.Dead || c.Age >= c.Life) Chests.RemoveAt(i);
            }
        }

        // 直击：命中了就吃掉这发（穿透照常减一层），返回 true 表示炮弹该停。
        bool HitChests(BulletActor b)
        {
            for (int i = 0; i < Chests.Count; i++)
            {
                ChestActor c = Chests[i];
                if (c.Dead || b.HitIds.Contains(c.Id)) continue;
                float r = ChestRadius + b.Radius;
                if ((c.Pos - b.Pos).sqrMagnitude > r * r) continue;
                b.HitIds.Add(c.Id);
                DamageChest(c, ShotDamage(b.Mods, 1f));
                if (b.Mods.ExplodeR > 0f) Explode(b.Pos, b.Mods, b);
                if (b.Mods.Pierce > 0) { b.Mods.Pierce--; b.Mods.Decay *= b.Mods.PierceDecay; }
                else return true;
            }
            return false;
        }

        void BlastChests(Vector2 pos, float r, ShotMods m, BulletActor src)
        {
            for (int i = 0; i < Chests.Count; i++)
            {
                ChestActor c = Chests[i];
                if (c.Dead || src.HitIds.Contains(c.Id)) continue;
                if ((c.Pos - pos).sqrMagnitude > r * r) continue;
                src.HitIds.Add(c.Id);
                DamageChest(c, ShotDamage(m, m.ExplodeShare));
            }
        }

        // 和 ResolveHit 第 2~4 步同一套：基础 × 衰减，加算池、乘算池。状态和斩杀不打箱子。
        static float ShotDamage(ShotMods m, float share)
        {
            float dmg = m.BaseDamage * m.Decay * share;
            return m.GoldRidesMul ? (dmg + m.AddDamage) * m.MulDamage : dmg * m.MulDamage + m.AddDamage;
        }

        void DamageChest(ChestActor c, float dmg)
        {
            c.Hp -= Mathf.Max(1f, dmg);
            c.HitFlash = 0.16f;
            if (c.Hp > 0f) return;
            c.Dead = true;
            Color tint = c.Kind == ChestKind.Gold ? InkTheme.CoinFace : InkTheme.Poison;
            Bursts.Add(new FxBurst { Pos = c.Pos, Kind = HitFx.Explode, Tint = tint, Scale = 0.9f });
            AudioBus.Kill();
            AddShake(0.12f);
            if (c.Kind == ChestKind.Gold)
                ScatterAt(c.Pos, 0.4f, false, DropKind.Gold, c.Amount, Mathf.Clamp(c.Amount, 3, 8));
            else
                ScatterAt(c.Pos, 0.5f, true, DropKind.Ink, c.Amount, 1);
            Push(PopKind.Word, c.Id, c.Pos + Vector2.up * 0.5f, "+" + c.Amount,
                tint, 1.1f, 0.9f);
        }
    }
}
