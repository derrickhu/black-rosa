using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 判官落下的朱砂劫印靶标：炮弹飞经击碎可反噬判官 8% 最大生命并爆出金币。
    public sealed class JudgeSealActor
    {
        public int Id;
        public int OwnerId;
        public int Col;
        public Vector2 Pos;
        public int Hp;
        public int MaxHp;
        public float Timer;
        public float MaxTimer;
        public float Flash;
    }

    // 第二幕终章（第十五章魔城、第十六章墨渊）专属机制：
    // 新词「横扫」、新诀「断岳横江」、新字「财」「护」、新怪「鼓吏」「污墨童」、终章首领「判官」「魔尊」。
    public sealed partial class BattleWorld
    {
        public readonly int[] GuardCharges = new int[GameConstants.Columns];
        public readonly int[] GuardStars = new int[GameConstants.Columns];
        public readonly float[] GuardFlash = new float[GameConstants.Columns];
        public readonly float[,] DustTimer = new float[GameConstants.Columns, GameConstants.Rows];
        public readonly List<JudgeSealActor> JudgeSeals = new List<JudgeSealActor>();

        float _bladeStormShoutCd;
        float _abyssWallFloatCd;
        bool _judgeTaught;
        bool _abyssTaught;

        void BeginFinale()
        {
            JudgeSeals.Clear();
            _bladeStormShoutCd = 0f;
            _abyssWallFloatCd = 0f;
            _judgeTaught = false;
            _abyssTaught = false;
            for (int c = 0; c < GameConstants.Columns; c++)
            {
                GuardCharges[c] = 0;
                GuardStars[c] = 0;
                GuardFlash[c] = 0f;
                for (int r = 0; r < GameConstants.Rows; r++)
                    DustTimer[c, r] = 0f;
            }
        }

        void RefreshFinaleCol(int col, bool heng, int hengS, bool sao, int saoS)
        {
            if (heng && sao && _word[col] == WordId.None)
            {
                _word[col] = WordId.Sweep;
                _wordStar[col] = Mathf.Max(hengS, saoS);
            }
            int prevGuard = GuardStars[col];
            int bestGuard = 0;
            for (int r = 0; r < OpenRows; r++)
            {
                if (Grid[col, r] == CardId.Guard)
                    bestGuard = Mathf.Max(bestGuard, EffectiveStar(col, r));
            }
            GuardStars[col] = bestGuard;
            if (bestGuard > prevGuard)
            {
                int prevCap = prevGuard > 0 ? GlyphTable.Get(CardId.Guard).Count.IntAt(prevGuard) : 0;
                int newCap = GlyphTable.Get(CardId.Guard).Count.IntAt(bestGuard);
                GuardCharges[col] = Mathf.Min(newCap, GuardCharges[col] + Mathf.Max(1, newCap - prevCap));
                GuardFlash[col] = 0.45f;
            }
            else if (bestGuard == 0)
            {
                GuardCharges[col] = 0;
            }
        }

        void RefillGuardsForNewWave()
        {
            for (int c = 0; c < GameConstants.Columns; c++)
            {
                if (GuardStars[c] <= 0) continue;
                int cap = GlyphTable.Get(CardId.Guard).Count.IntAt(GuardStars[c]);
                if (GuardCharges[c] < cap)
                {
                    GuardCharges[c] = cap;
                    GuardFlash[c] = 0.35f;
                }
            }
        }

        // 炮弹进列时顺手冲刷该列被污墨童蒙住的浮墨（连开三炮即净）。
        void WashColumnDust(int col)
        {
            for (int r = 0; r < OpenRows; r++)
            {
                if (DustTimer[col, r] <= 0f) continue;
                DustTimer[col, r] -= 1.45f;
                if (DustTimer[col, r] <= 0f)
                {
                    DustTimer[col, r] = 0f;
                    ShowFloat((Vector2)FieldLayout.CellPos(col, r) + Vector2.up * 0.28f, "净！", InkTheme.IceHi, 0.92f);
                }
            }
        }

        void TickFinale(float dt)
        {
            if (_bladeStormShoutCd > 0f) _bladeStormShoutCd -= dt;
            if (_abyssWallFloatCd > 0f) _abyssWallFloatCd -= dt;

            for (int c = 0; c < GameConstants.Columns; c++)
            {
                if (GuardFlash[c] > 0f) GuardFlash[c] = Mathf.Max(0f, GuardFlash[c] - dt);
                for (int r = 0; r < GameConstants.Rows; r++)
                    if (DustTimer[c, r] > 0f) DustTimer[c, r] = Mathf.Max(0f, DustTimer[c, r] - dt);
            }

            for (int i = JudgeSeals.Count - 1; i >= 0; i--)
            {
                JudgeSealActor s = JudgeSeals[i];
                if (s.Flash > 0f) s.Flash = Mathf.Max(0f, s.Flash - dt);
                s.Timer -= dt;
                if (s.Timer > 0f) continue;
                JudgeSeals.RemoveAt(i);
                // 劫印未被打碎而自然引爆：短蒙该列一格浮墨 3 秒。
                ApplyColumnDust(s.Col, 3.0f);
                Bursts.Add(new FxBurst { Pos = s.Pos, Kind = HitFx.Confuse, Tint = InkTheme.Explode, Scale = 1.15f });
            }
        }

        void ApplyColumnDust(int col, float duration)
        {
            if (col < 0 || col >= GameConstants.Columns || Grid == null) return;
            for (int r = OpenRows - 1; r >= 0; r--)
            {
                if (!Open[col, r] || !Grid[col, r].HasValue || DustTimer[col, r] > 0.2f) continue;
                DustTimer[col, r] = duration;
                ShowFloat((Vector2)FieldLayout.CellPos(col, r) + Vector2.up * 0.26f, "浮墨", InkTheme.GraphiteHi, 0.95f);
                return;
            }
        }

        // 鼓吏、判官、魔尊的主动机制轮询。
        void TickFinaleEnemy(EnemyActor e, float dt)
        {
            if (e.DrumBoost > 0f) e.DrumBoost = Mathf.Max(0f, e.DrumBoost - dt);
            if (e.AbyssVortex > 0f) e.AbyssVortex = Mathf.Max(0f, e.AbyssVortex - dt);

            if (e.SkillSeal > 0f || e.Held) return;

            if (e.Type == EnemyId.Drummer)
            {
                e.FinaleCd -= dt;
                if (e.FinaleCd > 0f) return;
                e.FinaleCd = 3.8f;
                int buffed = 0;
                const float r2 = 1.85f * 1.85f;
                for (int i = 0; i < Enemies.Count; i++)
                {
                    EnemyActor ally = Enemies[i];
                    if (ally.Dead || ally.Id == e.Id || ally.Doom > 0f) continue;
                    if ((ally.Pos - e.Pos).sqrMagnitude > r2) continue;
                    ally.DrumBoost = 2.2f;
                    buffed++;
                }
                if (buffed > 0)
                {
                    ShowFloat(e.Pos + Vector2.up * 0.32f, "擂鼓！", InkTheme.FireHi, 1.02f);
                    Bursts.Add(new FxBurst { Pos = e.Pos, Kind = HitFx.Fire, Tint = InkTheme.GoldHi, Scale = 0.95f });
                }
            }
            else if (e.Type == EnemyId.BossJudge)
            {
                e.FinaleCd -= dt;
                if (e.FinaleCd > 0f || JudgeSeals.Count >= 2) return;
                e.FinaleCd = 8.2f;
                int col = PickJudgeSealCol();
                var pos = new Vector2(FieldLayout.ColumnX(col), Mathf.Clamp(e.Pos.y - 1.6f, 1.35f, 3.6f));
                JudgeSeals.Add(new JudgeSealActor
                {
                    Id = NextActorId++,
                    OwnerId = e.Id,
                    Col = col,
                    Pos = pos,
                    Hp = 5,
                    MaxHp = 5,
                    Timer = 4.5f,
                    MaxTimer = 4.5f,
                    Flash = 0.25f
                });
                ShowFloat(pos + Vector2.up * 0.42f, "朱砂劫印！", InkTheme.Explode, 1.18f);
                if (!_judgeTaught)
                {
                    _judgeTaught = true;
                    ShowToast("滑炮击碎朱砂劫印 · 反噬判官爆金！", 2.4f);
                }
                AudioBus.BellShield();
            }
            else if (e.Type == EnemyId.BossAbyss)
            {
                if (e.Hp > e.MaxHp * 0.55f) return;
                e.FinaleCd -= dt;
                if (e.FinaleCd > 0f) return;
                e.FinaleCd = 11.5f;
                e.AbyssVortex = 6.0f;
                e.AbyssWallHits = 3;
                ShowFloat(e.Pos + Vector2.up * 0.55f, "渊墨护壁！", InkTheme.Violet, 1.38f);
                if (!_abyssTaught)
                {
                    _abyssTaught = true;
                    Shouts.Add(new Shout { Kind = ShoutKind.Horde, Text = "魔尊开渊壁！", Sub = "成词或秘卷连击三下破防" });
                }
                AudioBus.Horde();
                AddShake(0.24f);
            }
        }

        int PickJudgeSealCol()
        {
            int start = Random.Range(0, GameConstants.Columns);
            for (int k = 0; k < GameConstants.Columns; k++)
            {
                int c = (start + k) % GameConstants.Columns;
                bool busy = false;
                for (int i = 0; i < JudgeSeals.Count; i++)
                    if (JudgeSeals[i].Col == c) busy = true;
                if (!busy && ColumnCells(c) > 0) return c;
            }
            return start;
        }

        // 炮弹飞行途中可顺手打碎判官的朱砂劫印。
        bool HitJudgeSeals(BulletActor b)
        {
            if (JudgeSeals.Count == 0) return false;
            const float r = 0.56f;
            for (int i = JudgeSeals.Count - 1; i >= 0; i--)
            {
                JudgeSealActor s = JudgeSeals[i];
                if (b.HitIds.Contains(s.Id)) continue;
                float reach = r + b.Radius;
                if ((b.Pos - s.Pos).sqrMagnitude > reach * reach) continue;
                b.HitIds.Add(s.Id);
                int hitDmg = b.Mods.Has(CardId.Heavy) || b.Mods.Word != WordId.None ? 2 : 1;
                DamageJudgeSeal(i, hitDmg);
                if (b.Mods.Pierce > 0)
                {
                    b.Mods.Pierce--;
                    return false;
                }
                return true;
            }
            return false;
        }

        void DamageJudgeSeal(int idx, int dmg)
        {
            if (idx < 0 || idx >= JudgeSeals.Count) return;
            JudgeSealActor s = JudgeSeals[idx];
            s.Hp -= dmg;
            s.Flash = 0.18f;
            if (s.Hp > 0)
            {
                Bursts.Add(new FxBurst { Pos = s.Pos, Kind = HitFx.Heavy, Tint = InkTheme.GoldHi, Scale = 0.78f });
                AudioBus.Hit();
                return;
            }
            Vector2 pos = s.Pos;
            int ownerId = s.OwnerId;
            JudgeSeals.RemoveAt(idx);
            Bursts.Add(new FxBurst { Pos = pos, Kind = HitFx.Kill, Tint = InkTheme.GoldHi, Scale = 1.65f });
            ScatterAt(pos, 0.35f, false, DropKind.Gold, 12, 4);
            AudioBus.SlotJackpot();
            AddShake(0.30f);

            for (int k = 0; k < Enemies.Count; k++)
            {
                EnemyActor boss = Enemies[k];
                if (boss.Dead || (boss.Id != ownerId && boss.Type != EnemyId.BossJudge)) continue;
                float backlash = Mathf.Max(15f, boss.MaxHp * 0.08f);
                boss.Hp -= backlash;
                boss.HitFlash = 0.28f;
                Arcs.Add(new ArcFx { A = pos, B = boss.Pos, Tint = InkTheme.GoldHi });
                ShowFloat(boss.Pos + Vector2.up * 0.52f, "破印反噬！", InkTheme.GoldHi, 1.38f);
                ShowDamage(boss, backlash, InkTheme.Heart, 1.48f, true, 88);
                if (boss.Hp <= 0f) Kill(boss, null);
                break;
            }
        }

        // 魔尊半血渊墨护壁：需承受 3 记成词 / 秘卷 / 三字诀重击方可震碎护壁并打出 3.5 秒 1.5 倍易伤大硬直！
        float ResolveAbyssVortex(EnemyActor e, ShotMods m, HitSource from)
        {
            if (e.AbyssVortex <= 0f) return 1f;
            bool powerShot = m.Word != WordId.None
                || ShotLook.Signature(m) != ShotFx.None
                || m.FireWheel || m.Pin || m.BladeStorm || m.HeavySweep
                || (from == HitSource.Arrow && m.Has(CardId.Pierce));
            if (powerShot)
            {
                e.AbyssWallHits = Mathf.Max(0, e.AbyssWallHits - 1);
                if (e.AbyssWallHits > 0)
                {
                    e.HitFlash = 0.20f;
                    ShowFloat(e.Pos + Vector2.up * 0.44f, $"裂壁 {3 - e.AbyssWallHits}/3", InkTheme.GoldHi, 1.12f);
                    AudioBus.Hit();
                    return 0.75f;
                }
                e.AbyssVortex = 0f;
                e.FinaleCd = Mathf.Max(e.FinaleCd, 9.5f);
                e.Hard = StatusKind.Stun;
                e.HardTime = 3.5f;
                e.Vuln = Mathf.Max(e.Vuln, 0.50f);
                e.VulnTime = Mathf.Max(e.VulnTime, 4.0f);
                e.HitFlash = 0.32f;
                ScatterAt(e.Pos, e.Radius, true, DropKind.Gold, 15, 5);
                Bursts.Add(new FxBurst { Pos = e.Pos, Kind = HitFx.Kill, Tint = InkTheme.GoldHi, Scale = 2.1f });
                ShowFloat(e.Pos + Vector2.up * 0.62f, "破渊大硬直！", InkTheme.GoldHi, 1.5f);
                AudioBus.SlotJackpot();
                AddShake(0.36f);
                return 1.5f;
            }
            if (_abyssWallFloatCd <= 0f)
            {
                _abyssWallFloatCd = 0.9f;
                ShowFloat(e.Pos + Vector2.up * 0.28f, "渊壁减半", InkTheme.Violet, 0.92f);
            }
            return 0.5f;
        }

        // 横扫命中：劈出跨列弧月刀波，断岳横江时额外震退整排并定身。
        void StrikeSweep(BulletActor b, EnemyActor hit)
        {
            ShotMods m = b.Mods;
            int cols = Mathf.Max(3, m.SweepCols);
            WordHits.Add(new WordHit
            {
                Word = WordId.Sweep,
                Target = hit,
                From = b.Pos,
                Step = cols + (m.HeavySweep ? 1 : 0)
            });
            AudioBus.Spell(m.HeavySweep ? 0.92f : 1.12f);
            AddShake(m.HeavySweep ? 0.26f : 0.15f);
            ShowFloat(hit.Pos + Vector2.up * 0.36f, m.HeavySweep ? "断岳横江！" : "横扫！",
                InkTheme.GoldHi, m.HeavySweep ? 1.42f : 1.22f);

            float halfW = (cols * 0.5f + (m.HeavySweep ? 0.45f : 0.15f)) * GameConstants.CellWidth;
            float bandY = m.HeavySweep ? 0.95f : 0.72f;
            float waveDmg = m.BaseDamage * m.Decay * m.SweepShare;

            if (m.HeavySweep && !hit.Dead && !hit.NoKnock)
            {
                hit.KnockLeft = Mathf.Max(hit.KnockLeft, 0.90f);
                hit.HoldTime = Mathf.Max(hit.HoldTime, 0.50f);
            }

            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor other = Enemies[i];
                if (other.Dead || other.Doom > 0f || other.Hidden || other.Id == hit.Id) continue;
                if (Mathf.Abs(other.Pos.x - hit.Pos.x) > halfW + other.Radius) continue;
                if (Mathf.Abs(other.Pos.y - hit.Pos.y) > bandY + other.Radius) continue;
                b.HitIds.Add(other.Id);
                ResolveHit(other, m, b, HitSource.Area, waveDmg);
                if (m.HeavySweep && !other.Dead && !other.NoKnock)
                {
                    other.KnockLeft = Mathf.Max(other.KnockLeft, 0.90f);
                    other.HoldTime = Mathf.Max(other.HoldTime, 0.50f);
                }
            }

            for (int i = JudgeSeals.Count - 1; i >= 0; i--)
            {
                JudgeSealActor s = JudgeSeals[i];
                if (b.HitIds.Contains(s.Id)) continue;
                if (Mathf.Abs(s.Pos.x - hit.Pos.x) <= halfW && Mathf.Abs(s.Pos.y - hit.Pos.y) <= bandY + 0.4f)
                {
                    b.HitIds.Add(s.Id);
                    DamageJudgeSeal(i, 2);
                }
            }
        }

        // 万剑归宗：折返时索敌场上当前血量最高的目标。
        EnemyActor BestBladeTarget()
        {
            EnemyActor best = null;
            float bestHp = -1f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || e.Doom > 0f || e.Hidden) continue;
                if (e.Hp > bestHp)
                {
                    bestHp = e.Hp;
                    best = e;
                }
            }
            return best;
        }

        // 财：击倒敌人时按概率迸出金元宝，赏 2 金币并弹跳震晕近处一名同伴。
        void TryWealthBurst(EnemyActor dead, ShotMods m)
        {
            if (dead == null) return;
            if (dead.Type == EnemyId.Imp && dead.SkillSeal <= 0f)
                ApplyColumnDust(FieldLayout.ColumnAtX(dead.Pos.x), 4.0f);

            if (m == null || m.WealthChance <= 0f || PreviewFill) return;
            if (Random.value >= m.WealthChance) return;

            ScatterAt(dead.Pos, dead.Radius, false, DropKind.Gold, 2, 2);
            AudioBus.Coin();
            ShowFloat(dead.Pos + Vector2.up * 0.36f, "元宝 +2金", InkTheme.CoinFace, 1.08f);

            float reach = GlyphTable.Get(CardId.Wealth).Radius.At(1) * (1f + SealWide(CardId.Wealth));
            EnemyActor near = null;
            float bestD = reach * reach;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor other = Enemies[i];
                if (other.Dead || other.Doom > 0f || other.Hidden || other.Id == dead.Id) continue;
                float d = (other.Pos - dead.Pos).sqrMagnitude;
                if (d < bestD) { bestD = d; near = other; }
            }
            if (near == null) return;
            Arcs.Add(new ArcFx { A = dead.Pos, B = near.Pos, Tint = InkTheme.CoinFace });
            Bursts.Add(new FxBurst { Pos = near.Pos, Kind = HitFx.Gold, Tint = InkTheme.GoldHi, Scale = 0.95f });
            float bounceDmg = Mathf.Max(1f, m.BaseDamage * 0.85f * m.FinalMul);
            near.Hp -= bounceDmg;
            near.HitFlash = 0.16f;
            ApplyHard(near, StatusKind.Stun, 0.55f, 0f);
            ShowDamage(near, bounceDmg, InkTheme.CoinFace, 1.05f, false, 79);
            if (near.Hp <= 0f) Kill(near, null);
        }

        // 护：敌人触及防线底边时，消耗本列（或 ★3 邻列）金盾将其弹回中场并震晕。
        bool TryGuardBounce(EnemyActor e)
        {
            int col = FieldLayout.ColumnAtX(e.Pos.x);
            int useCol = -1;
            if (col >= 0 && col < GameConstants.Columns && GuardCharges[col] > 0)
            {
                useCol = col;
            }
            else
            {
                for (int d = -1; d <= 1; d += 2)
                {
                    int adj = col + d;
                    if (adj >= 0 && adj < GameConstants.Columns && GuardCharges[adj] > 0 && GuardStars[adj] >= 3)
                    {
                        useCol = adj;
                        break;
                    }
                }
            }
            if (useCol < 0) return false;

            GuardCharges[useCol]--;
            GuardFlash[useCol] = 0.55f;
            float bounce = (e.NoKnock ? 1.45f : 2.25f) * GameConstants.CellHeight * (1f + SealMove(CardId.Guard));
            e.Pos.y = Mathf.Max(e.Pos.y, GameConstants.LeakY + 0.25f);
            e.KnockLeft = Mathf.Max(e.KnockLeft, bounce);
            e.HoldTime = Mathf.Max(e.HoldTime, 1.0f);

            Vector2 fxPos = new Vector2(FieldLayout.ColumnX(useCol), GameConstants.LeakY + 0.18f);
            Bursts.Add(new FxBurst { Pos = fxPos, Kind = HitFx.Knock, Tint = InkTheme.GoldHi, Scale = 1.45f });
            ShowFloat(fxPos + Vector2.up * 0.35f, $"金盾御敌！（余{GuardCharges[useCol]}）", InkTheme.GoldHi, 1.15f);
            AudioBus.BellShield();
            AddShake(0.22f);

            if (GuardStars[useCol] >= 3)
            {
                const float shockR2 = 1.65f * 1.65f;
                for (int i = 0; i < Enemies.Count; i++)
                {
                    EnemyActor other = Enemies[i];
                    if (other.Dead || other.Id == e.Id || other.Doom > 0f) continue;
                    if ((other.Pos - fxPos).sqrMagnitude > shockR2) continue;
                    if (!other.NoKnock) other.KnockLeft = Mathf.Max(other.KnockLeft, 1.1f);
                    other.HoldTime = Mathf.Max(other.HoldTime, 0.65f);
                }
            }
            return true;
        }
    }
}
