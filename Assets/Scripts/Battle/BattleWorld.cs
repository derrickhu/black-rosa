using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public sealed class EnemyActor
    {
        public int Id;
        public EnemyId Type;
        public Vector2 Pos;
        public float Hp;
        public float MaxHp;
        public float Speed;
        public float Radius;
        public int Gold;
        public bool Shield;
        public bool Strafe;
        public bool PreferEmpty;
        public bool IsBoss;
        public bool ColorPhase;
        public bool Dead;
        public float BurnDps;
        public float BurnTime;
        public float Slow = 1f;
        public float SlowTime;
        public float FreezeTime;
        public float StrafeDir = 1f;
        public float HitFlash;
        public bool Colored;
    }

    public sealed class BulletActor
    {
        public int Id;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Radius = 0.11f;
                public float Damage = 2.4f;
        public Color Color = Color.black;
        public int Pierce;
        public float ExplodeR;
        public float Homing;
        public float Burn;
        public bool BurnPop;
        public bool Freeze;
        public float Slow;
        public bool Crit;
        public int FireStar;
        public int IceStar;
        public int TrackStar;
        public int HeavyStar;
        public int NextRow;
        public bool Dead;
        public readonly HashSet<int> HitIds = new HashSet<int>();
    }

    public sealed class FloatText
    {
        public Vector2 Pos;
        public string Text;
        public Color Color;
        public float Scale = 1f;
        public float Life = 0.72f;
        public float MaxLife = 0.72f;
    }

        public struct FxBurst
        {
            public Vector2 Pos;
            public int Kind;
            public Color Tint;
            public float Scale;
        }

    public sealed class BattleWorld
    {
        public bool Paused;
        public float HitStop;
        public int Gold;
        public int BaseHp = GameConstants.BaseHp;
        public int RevivesUsed;
        public int DraftCount;
        public int OpenRows = 1;
        public int EmitterCount = 2;
        public float RailX;
        public bool Victory;
        public bool Defeat;
        public static bool PreviewFill;
        public bool BossSpawned;
        public bool BossKilled;
        public int WaveIndex;
        public float WaveTime;
        public float BattleTime;
        public int NextActorId = 1;
        public StageDef Stage;
        public CardId?[,] Grid;
        public int[,] Stars;
        public readonly List<EnemyActor> Enemies = new List<EnemyActor>();
        public readonly List<BulletActor> Bullets = new List<BulletActor>();
        public readonly List<FloatText> Floats = new List<FloatText>();
        public readonly List<FxBurst> Bursts = new List<FxBurst>();
        public string LastReveal;
        public float RevealTime;
        public string Toast;
        public float ToastTime;

        readonly float[] _fireCd = new float[GameConstants.MaxEmitters];
        readonly List<int> _deadBullets = new List<int>();

        public int DraftCost => GameConstants.FirstDraftCost + DraftCount * GameConstants.DraftCostStep;
        public bool CanDraft => Gold >= DraftCost && !Victory && !Defeat;
        public int AliveEnemies
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Enemies.Count; i++) if (!Enemies[i].Dead) n++;
                return n;
            }
        }

        public void Begin(StageDef stage, int startGold, int startEmitters)
        {
            Stage = stage;
            OpenRows = Mathf.Clamp(stage.OpenRows, 1, GameConstants.Rows);
            Grid = new CardId?[GameConstants.Columns, GameConstants.Rows];
            Stars = new int[GameConstants.Columns, GameConstants.Rows];
            Gold = startGold;
            EmitterCount = Mathf.Clamp(startEmitters, 1, GameConstants.MaxEmitters);
            RailX = FieldLayout.ColumnX(Mathf.Max(0, (GameConstants.Columns - EmitterCount) / 2));
            BaseHp = GameConstants.BaseHp;
            Enemies.Clear();
            Bullets.Clear();
            Floats.Clear();
            Bursts.Clear();
            if (PreviewFill) ApplyPreviewFill();
        }

        void ApplyPreviewFill()
        {
            OpenRows = GameConstants.Rows;
            PutPreview(0, 0, CardId.Fire, 1);
            PutPreview(0, 1, CardId.Fire, 2);
            PutPreview(0, 2, CardId.Fire, 3);
            PutPreview(1, 0, CardId.Ice, 1);
            PutPreview(1, 1, CardId.Ice, 2);
            PutPreview(1, 2, CardId.Ice, 3);
            PutPreview(2, 0, CardId.Track, 1);
            PutPreview(2, 1, CardId.Track, 2);
            PutPreview(2, 2, CardId.Track, 3);
            PutPreview(3, 0, CardId.Heavy, 1);
            PutPreview(3, 1, CardId.Heavy, 2);
            PutPreview(3, 2, CardId.Heavy, 3);
            PutPreview(4, 0, CardId.Split, 1);
            PutPreview(4, 1, CardId.Pierce, 1);
            PutPreview(4, 2, CardId.Pierce, 2);
            PutPreview(5, 0, CardId.Accel, 1);
            PutPreview(5, 1, CardId.Explode, 1);
            PutPreview(5, 2, CardId.Accel, 3);
        }

        void PutPreview(int col, int row, CardId id, int star)
        {
            Grid[col, row] = id;
            Stars[col, row] = Mathf.Clamp(star, 1, GameConstants.MaxStar);
        }

        public void Tick(float dt)
        {
            if (Paused || Victory || Defeat) return;
            if (HitStop > 0f)
            {
                HitStop -= dt;
                TickFloats(dt);
                return;
            }

            BattleTime += dt;
            TickWaves(dt);
            TickEmitters(dt);
            TickBullets(dt);
            TickEnemies(dt);
            TickFloats(dt);
            if (ToastTime > 0f) ToastTime -= dt;
            if (RevealTime > 0f) RevealTime -= dt;
            CheckEnd();
        }

        void TickWaves(float dt)
        {
            if (WaveIndex >= Stage.Waves.Length) return;
            WaveDef wave = Stage.Waves[WaveIndex];
            WaveTime += dt;
            for (int i = 0; i < wave.Spawns.Length; i++)
            {
                SpawnSpec s = wave.Spawns[i];
                if (WaveTime - dt < s.Time && WaveTime >= s.Time)
                    Spawn(s);
            }
            if (WaveTime >= wave.Duration)
            {
                WaveIndex++;
                WaveTime = 0f;
            }
        }

        void Spawn(SpawnSpec spec)
        {
            for (int i = 0; i < spec.Count; i++)
            {
                int col = spec.Column;
                if (col < 0) col = UnityEngine.Random.Range(0, GameConstants.Columns);
                col = Mathf.Clamp(col + (spec.Count > 1 ? i % 3 - 1 : 0), 0, GameConstants.Columns - 1);
                EnemyDef def = EnemyCatalog.Get(spec.Id, Stage.Index);
                var e = new EnemyActor
                {
                    Id = NextActorId++,
                    Type = spec.Id,
                    Pos = new Vector2(FieldLayout.ColumnX(col) + UnityEngine.Random.Range(-0.08f, 0.08f), GameConstants.SpawnY + i * 0.18f),
                    Hp = def.Hp,
                    MaxHp = def.Hp,
                    Speed = def.Speed,
                    Radius = def.Radius,
                    Gold = def.Gold,
                    Shield = def.HasShield,
                    Strafe = def.Strafe,
                    PreferEmpty = def.PreferEmpty,
                    IsBoss = def.IsBoss,
                    ColorPhase = def.ColorsInPhase2
                };
                if (e.IsBoss) BossSpawned = true;
                Enemies.Add(e);
            }
        }

        void TickEmitters(float dt)
        {
            float maxLeft = FieldLayout.ColumnX(0);
            float maxRight = FieldLayout.ColumnX(GameConstants.Columns - EmitterCount);
            RailX = Mathf.Clamp(RailX, maxLeft, maxRight);
            for (int i = 0; i < EmitterCount; i++)
            {
                int col = FieldLayout.ColumnAtX(RailX + i * GameConstants.CellWidth);
                float interval = GameConstants.BaseFireInterval;
                _fireCd[i] -= dt;
                if (_fireCd[i] > 0f) continue;
                _fireCd[i] = interval;
                float x = RailX + i * GameConstants.CellWidth;
                FireBullet(new Vector2(x, GameConstants.EmitterY + 0.35f), Vector2.up);
            }
        }

        BulletActor FireBullet(Vector2 pos, Vector2 dir)
        {
            var b = new BulletActor
            {
                Id = NextActorId++,
                Pos = pos,
                Vel = dir.normalized * GameConstants.BulletSpeed,
                NextRow = 0
            };
            Bullets.Add(b);
            return b;
        }

        void TickBullets(float dt)
        {
            _deadBullets.Clear();
            for (int i = 0; i < Bullets.Count; i++)
            {
                BulletActor b = Bullets[i];
                if (b.Dead) continue;
                if (b.Homing > 0f)
                    Steer(b, dt);
                b.Pos += b.Vel * dt;
                ApplyRows(b);
                if (b.Pos.y > GameConstants.WorldHalfHeight + 1f)
                    b.Dead = true;
                else
                    HitTest(b);
                if (b.Dead) _deadBullets.Add(i);
            }
            for (int i = _deadBullets.Count - 1; i >= 0; i--)
                Bullets.RemoveAt(_deadBullets[i]);
        }

        void Steer(BulletActor b, float dt)
        {
            EnemyActor best = null;
            float bestD = 999f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                float d = (e.Pos - b.Pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            if (best == null) return;
            Vector2 want = (best.Pos - b.Pos).normalized;
            b.Vel = Vector2.Lerp(b.Vel.normalized, want, b.Homing * dt).normalized * b.Vel.magnitude;
        }

        void ApplyRows(BulletActor b)
        {
            while (b.NextRow < OpenRows && b.Pos.y >= FieldLayout.RowApplyY(b.NextRow))
            {
                int col = FieldLayout.ColumnAtX(b.Pos.x);
                CardId? id = Grid[col, b.NextRow];
                if (id.HasValue)
                    ApplyMod(b, id.Value, Stars[col, b.NextRow]);
                b.NextRow++;
            }
        }

        void ApplyMod(BulletActor b, CardId id, int star)
        {
            switch (id)
            {
                case CardId.Split:
                    Split(b, star);
                    break;
                case CardId.Fire:
                    b.Burn = Mathf.Max(b.Burn, CardCatalog.BurnDps(star));
                    b.BurnPop = b.BurnPop || CardCatalog.BurnPop(star);
                    b.FireStar = Mathf.Max(b.FireStar, star);
                    if (star >= 3) b.ExplodeR = Mathf.Max(b.ExplodeR, 0.45f);
                    PaintBodyColor(b);
                    break;
                case CardId.Ice:
                    b.Slow = Mathf.Min(b.Slow <= 0f ? 1f : b.Slow, CardCatalog.SlowFactor(star));
                    b.Freeze = b.Freeze || CardCatalog.FreezeOnHit(star);
                    b.IceStar = Mathf.Max(b.IceStar, star);
                    PaintBodyColor(b);
                    break;
                case CardId.Track:
                    b.Homing = Mathf.Max(b.Homing, CardCatalog.Homing(star));
                    b.TrackStar = Mathf.Max(b.TrackStar, star);
                    break;
                case CardId.Pierce:
                    b.Pierce += CardCatalog.Pierce(star);
                    break;
                case CardId.Explode:
                    b.ExplodeR = Mathf.Max(b.ExplodeR, CardCatalog.ExplodeRadius(star));
                    break;
                case CardId.Accel:
                    b.Vel = b.Vel.normalized * (b.Vel.magnitude * CardCatalog.AccelMul(star));
                    break;
                case CardId.Heavy:
                    b.HeavyStar = Mathf.Max(b.HeavyStar, star);
                    b.Radius *= CardCatalog.SizeMul(star);
                    if (UnityEngine.Random.value < CardCatalog.CritChance(star))
                    {
                        b.Crit = true;
                        b.Damage *= 2f;
                        b.Radius *= 1.15f;
                    }
                    break;
            }
        }

        static void PaintBodyColor(BulletActor b)
        {
            if (b.FireStar > 0 && b.IceStar > 0)
                b.Color = Color.Lerp(CardCatalog.Accent(CardId.Fire), CardCatalog.Accent(CardId.Ice), 0.5f);
            else if (b.FireStar > 0)
                b.Color = CardCatalog.Accent(CardId.Fire);
            else if (b.IceStar > 0)
                b.Color = CardCatalog.Accent(CardId.Ice);
        }

        void Split(BulletActor source, int star)
        {
            int extra = CardCatalog.SplitCount(star) - 1;
            float spread = 13f;
            for (int i = 0; i < extra; i++)
            {
                float side = extra == 1 ? (i == 0 ? -1f : 1f) : (i - (extra - 1) * 0.5f);
                float ang = side * spread;
                Vector2 dir = Quaternion.Euler(0f, 0f, ang) * Vector2.up;
                BulletActor clone = FireBullet(source.Pos, dir);
                clone.Damage = source.Damage;
                clone.Color = source.Color;
                clone.Pierce = source.Pierce;
                clone.ExplodeR = source.ExplodeR;
                clone.Homing = source.Homing;
                clone.Burn = source.Burn;
                clone.BurnPop = source.BurnPop;
                clone.FireStar = source.FireStar;
                clone.IceStar = source.IceStar;
                clone.TrackStar = source.TrackStar;
                clone.HeavyStar = source.HeavyStar;
                clone.Freeze = source.Freeze;
                clone.Slow = source.Slow;
                clone.Crit = source.Crit;
                clone.Radius = source.Radius;
                clone.NextRow = source.NextRow + 1;
                clone.Vel = dir * source.Vel.magnitude;
            }
        }

        void HitTest(BulletActor b)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || b.HitIds.Contains(e.Id)) continue;
                if ((e.Pos - b.Pos).sqrMagnitude > (e.Radius + b.Radius) * (e.Radius + b.Radius)) continue;
                b.HitIds.Add(e.Id);
                DamageEnemy(e, b.Damage, b);
                if (b.ExplodeR > 0f) Explode(b.Pos, b.ExplodeR, b.Damage * 0.65f, b);
                if (b.Pierce > 0) b.Pierce--;
                else
                {
                    b.Dead = true;
                    break;
                }
            }
        }

        void Explode(Vector2 pos, float r, float dmg, BulletActor src)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || src.HitIds.Contains(e.Id)) continue;
                if ((e.Pos - pos).sqrMagnitude <= r * r)
                {
                    src.HitIds.Add(e.Id);
                    DamageEnemy(e, dmg, src);
                }
            }
        }

        void DamageEnemy(EnemyActor e, float dmg, BulletActor src)
        {
            if (e.Shield)
            {
                e.Shield = false;
                e.HitFlash = 0.16f;
                PulseHitStop(0.06f);
                ShowFloat(e.Pos + Vector2.up * 0.18f, "挡", InkTheme.Ink, 0.92f);
                return;
            }
            e.Hp -= dmg;
            e.HitFlash = src.Crit ? 0.2f : 0.14f;
            if (src.Burn > 0f)
            {
                e.BurnDps = Mathf.Max(e.BurnDps, src.Burn);
                e.BurnTime = 2.1f;
            }
            Bursts.Add(new FxBurst
            {
                Pos = e.Pos,
                Kind = 1,
                Tint = HitTint(src),
                Scale = src.ExplodeR > 0.01f ? 1.55f : 1.25f
            });
            if (src.Slow > 0f && src.Slow < 1f)
            {
                e.Slow = src.Slow;
                e.SlowTime = 1.4f;
            }
            if (src.Freeze) e.FreezeTime = 0.45f;
            PulseHitStop(src.Crit ? 0.1f : 0.07f);
            int n = Mathf.Max(1, Mathf.RoundToInt(dmg));
            Color ink = src.Color.r + src.Color.g + src.Color.b < 0.12f ? InkTheme.Ink : src.Color;
            ShowFloat(e.Pos + Vector2.up * 0.22f, n.ToString(), ink, src.Crit ? 1.42f : 1f);
            if (e.Hp <= 0f) Kill(e, src);
        }

        static Color HitTint(BulletActor src)
        {
            if (src.FireStar > 0 && src.IceStar > 0) return Color.white;
            if (src.FireStar > 0) return Color.white;
            if (src.IceStar > 0) return InkTheme.IceHi;
            if (src.ExplodeR > 0f) return InkTheme.Explode;
            return Color.white;
        }

        void PulseHitStop(float time)
        {
            if (HitStop <= 0.02f) HitStop = time;
        }

        void Kill(EnemyActor e, BulletActor src)
        {
            e.Dead = true;
            Gold += e.Gold;
            ShowFloat(e.Pos + Vector2.up * 0.08f, $"+{e.Gold}", InkTheme.Ink, 0.88f);
            if (e.IsBoss) BossKilled = true;
            if (src != null && src.BurnPop)
                Explode(e.Pos, 0.5f, 2.2f, src);
        }

        void TickEnemies(float dt)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if (e.HitFlash > 0f) e.HitFlash -= dt;
                if (e.ColorPhase && e.Hp <= e.MaxHp * 0.5f) e.Colored = true;
                if (e.BurnTime > 0f)
                {
                    e.BurnTime -= dt;
                    e.Hp -= e.BurnDps * dt;
                    if (e.Hp <= 0f) { Kill(e, null); continue; }
                }
                if (e.FreezeTime > 0f) { e.FreezeTime -= dt; continue; }
                if (e.SlowTime > 0f) e.SlowTime -= dt;
                else e.Slow = 1f;

                float speed = e.Speed * e.Slow;
                if (e.PreferEmpty)
                {
                    int col = FieldLayout.ColumnAtX(e.Pos.x);
                    if (!ColumnHasMod(col)) speed *= 1.28f;
                }
                e.Pos.y -= speed * dt;
                if (e.Strafe)
                {
                    e.Pos.x += e.StrafeDir * 0.55f * dt;
                    float min = FieldLayout.ColumnX(0);
                    float max = FieldLayout.ColumnX(GameConstants.Columns - 1);
                    if (e.Pos.x < min || e.Pos.x > max)
                    {
                        e.StrafeDir *= -1f;
                        e.Pos.x = Mathf.Clamp(e.Pos.x, min, max);
                    }
                }
                if (e.Pos.y <= GameConstants.LeakY)
                {
                    e.Dead = true;
                    BaseHp = Mathf.Max(0, BaseHp - 1);
                    ShowToast("防线被突破");
                }
            }
            Enemies.RemoveAll(e => e.Dead);
        }

        bool ColumnHasMod(int col)
        {
            for (int r = 0; r < OpenRows; r++)
                if (Grid[col, r].HasValue) return true;
            return false;
        }

        void TickFloats(float dt)
        {
            for (int i = Floats.Count - 1; i >= 0; i--)
            {
                Floats[i].Life -= dt;
                Floats[i].Pos += Vector2.up * dt * 0.8f;
                if (Floats[i].Life <= 0f) Floats.RemoveAt(i);
            }
        }

        void CheckEnd()
        {
            if (BaseHp <= 0) { Defeat = true; Paused = true; return; }
            if (BossKilled && AliveEnemies == 0 && WaveIndex >= Stage.Waves.Length)
            {
                Victory = true;
                Paused = true;
            }
        }

        public bool TryRevive()
        {
            if (RevivesUsed >= GameConstants.MaxRevives || (!Defeat && BaseHp > 0)) return false;
            RevivesUsed++;
            Defeat = false;
            Paused = false;
            BaseHp = Mathf.Max(1, BaseHp);
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i].Pos.y < GameConstants.GridCenterY)
                    Enemies.RemoveAt(i);
            }
            ShowToast("防线重整");
            return true;
        }

        public enum PlaceResult { Placed, Upgraded, NeedConfirm, RejectedMaxStar, LockedRow }

        public PlaceResult PeekPlace(CardId id, int col, int row)
        {
            if (row < 0 || row >= OpenRows) return PlaceResult.LockedRow;
            if (!Grid[col, row].HasValue) return PlaceResult.Placed;
            if (Grid[col, row] == id)
                return Stars[col, row] >= GameConstants.MaxStar ? PlaceResult.RejectedMaxStar : PlaceResult.Upgraded;
            return PlaceResult.NeedConfirm;
        }

        public PlaceResult Place(CardId id, int col, int row, bool overwrite)
        {
            PlaceResult peek = PeekPlace(id, col, row);
            if (peek == PlaceResult.LockedRow || peek == PlaceResult.RejectedMaxStar) return peek;
            if (peek == PlaceResult.NeedConfirm && !overwrite) return peek;
            if (peek == PlaceResult.Upgraded)
            {
                Stars[col, row]++;
                ShowToast($"{CardCatalog.Get(id).Name} {Stars[col, row]} 星");
            }
            else
            {
                Grid[col, row] = id;
                Stars[col, row] = 1;
            }
            TryReveal(col);
            return peek == PlaceResult.NeedConfirm ? PlaceResult.Placed : peek;
        }

        void TryReveal(int col)
        {
            if (OpenRows < 3) return;
            if (!Grid[col, 0].HasValue || !Grid[col, 1].HasValue || !Grid[col, 2].HasValue) return;
            var set = new HashSet<CardId> { Grid[col, 0].Value, Grid[col, 1].Value, Grid[col, 2].Value };
            string name = null;
            if (set.Contains(CardId.Split) && set.Contains(CardId.Explode) && set.Contains(CardId.Fire)) name = "焰雨";
            else if (set.Contains(CardId.Ice) && set.Contains(CardId.Track) && set.Contains(CardId.Pierce)) name = "霜矢";
            else if (set.Contains(CardId.Accel) && set.Contains(CardId.Heavy) && set.Contains(CardId.Fire)) name = "重焰";
            else if (set.Contains(CardId.Split) && set.Contains(CardId.Track) && set.Contains(CardId.Ice)) name = "散霜";
            if (name == null) return;
            LastReveal = name;
            RevealTime = 1.6f;
            ShowToast($"显形 · {name}");
        }

        public void ShowToast(string text)
        {
            Toast = text;
            ToastTime = 1.4f;
        }

        public void ShowFloat(Vector2 pos, string text, Color color, float scale = 1f)
        {
            Floats.Add(new FloatText
            {
                Pos = pos + new Vector2(UnityEngine.Random.Range(-0.16f, 0.16f), UnityEngine.Random.Range(0f, 0.08f)),
                Text = text,
                Color = color,
                Scale = scale,
                Life = 0.72f,
                MaxLife = 0.72f
            });
        }

        public void SetRailFromWorldX(float x, bool snap)
        {
            float left = x - (EmitterCount - 1) * GameConstants.CellWidth * 0.5f;
            float maxLeft = FieldLayout.ColumnX(0);
            float maxRight = FieldLayout.ColumnX(GameConstants.Columns - EmitterCount);
            left = Mathf.Clamp(left, maxLeft, maxRight);
            if (snap)
            {
                int col = FieldLayout.ColumnAtX(left);
                col = Mathf.Clamp(col, 0, GameConstants.Columns - EmitterCount);
                RailX = FieldLayout.ColumnX(col);
            }
            else RailX = left;
        }
    }
}
