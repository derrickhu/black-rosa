using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 秘卷的现场表现：烈爆留下的火地，和其余十几对触发那一下的专属特效。
    // 底图是浅色宣纸，加色光在上面几乎看不见，所以主体一律用平涂，加色只做白热的芯。
    // 所有尺寸都按「世界单位里多大」给，换算时除掉贴图自己的 bounds，导入设置变了也不走样。
    public sealed class PairFxView : MonoBehaviour
    {
        sealed class Bit
        {
            public SpriteRenderer Sr;
            public Sprite[] Frames;
            public Vector2 Pos, Vel, From, To;
            public float Age, Life, Delay, Spin, Gravity, Lift, A0;
            public Vector2 S0, S1;
            public Color C;
            public bool On, Travel, HoldAlpha;
        }

        sealed class Patch
        {
            public int Id;
            public bool Seen;
            public SpriteRenderer Scorch, Glow;
            public SpriteRenderer[] Flames;
            public Vector2[] Spots;
            public float[] Phase, Height;
            public float EmberAt, Age;
        }

        const int OrderScorch = 1;
        const int OrderGround = 2;
        const int OrderFlame = 3;
        const int OrderHit = 14;
        const int OrderTop = 15;
        const int OrderMote = 18;

        static PairFxView _pool;
        readonly List<Bit> _bits = new List<Bit>();
        readonly List<Patch> _patches = new List<Patch>();
        static readonly Dictionary<string, Sprite> Art = new Dictionary<string, Sprite>();

        static readonly Color ScorchInk = new Color(0.20f, 0.11f, 0.05f, 1f);
        static readonly Color Ember = new Color(1f, 0.52f, 0.12f, 1f);

        public static void Bind(Transform root)
        {
            if (_pool != null && _pool.transform.parent == root) return;
            var go = new GameObject("pairfx");
            go.transform.SetParent(root, false);
            _pool = go.AddComponent<PairFxView>();
        }

        static Sprite Pic(string name)
        {
            if (Art.TryGetValue(name, out Sprite s) && s != null) return s;
            s = InkSprites.Load("Vfx/" + name);
            Art[name] = s;
            return s;
        }

        // ---------- 触发特效 ----------

        public static void Play(PairFx fx)
        {
            if (_pool == null) return;
            switch (fx.Kind)
            {
                case PairFx.Scorch: _pool.Scorch(fx.A, fx.Size); break;
                case PairFx.Shatter: _pool.Shatter(fx.A, fx.Size); break;
                case PairFx.Conduct: _pool.Conduct(fx.A, fx.Size); break;
                case PairFx.WardGold: _pool.WardGold(fx.A, fx.Size); break;
                case PairFx.Leech: _pool.Leech(fx.A, fx.B); break;
                case PairFx.RamEarth: _pool.RamEarth(fx.A, fx.B, fx.Size); break;
                case PairFx.ColdWind: _pool.ColdWind(fx.A, fx.B, fx.Size); break;
                case PairFx.ThunderCut: _pool.ThunderCut(fx.A, fx.Size); break;
                case PairFx.Crumble: _pool.Crumble(fx.A, fx.Size); break;
                case PairFx.Collapse: _pool.Collapse(fx.A, fx.Size); break;
                case PairFx.Whirl: _pool.Whirl(fx.A, fx.Size); break;
                case PairFx.Riposte: _pool.Riposte(fx.A, fx.B); break;
            }
        }

        // 焦雷：晕一结束，那一口火雷从天上劈进来。地上烧出一块焦印。
        void Scorch(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.6f);
            Mark(at + Vector2.down * 0.12f, s * 1.1f, s * 1.3f, 1.4f, ScorchInk, 0.5f);
            Anim("hitv_fire", at, s * 1.25f, s * 1.6f, 0.42f, OrderHit);
            Anim("hitv_thunder", at, s * 0.9f, s * 1.25f, 0.32f, OrderTop, 0.04f);
            Flash(at, s * 0.5f, s * 1.6f, 0.2f);
            Spray(at, 12, InkFx.Dot(), 0.09f, 2.2f, 4.2f, 0.55f, Ember, OrderTop, -4f);
            Spray(at, 6, InkFx.Star(), 0.2f, 1.6f, 3f, 0.4f, InkTheme.ThunderHi, OrderTop, 0f);
        }

        // 霰雷：冰壳整个崩掉，碎冰往四面掉。
        void Shatter(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.4f);
            Anim("hitv_ice", at, s * 0.9f, s * 1.45f, 0.36f, OrderHit);
            Flash(at, s * 0.4f, s * 1.4f, 0.16f);
            Sprite[] crust = InkVfx.Frames("ice_crust");
            Sprite chip = crust != null && crust[0] != null ? crust[0] : InkFx.Star();
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f + Random.Range(-0.05f, 0.05f)) * Mathf.PI * 2f;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.6f) * Random.Range(2.2f, 3.6f);
                Bit b = Add(chip, at, OrderTop, Color.white, false);
                b.Vel = v;
                b.Gravity = -9f;
                b.Life = Random.Range(0.42f, 0.55f);
                b.S0 = b.S1 = Size(chip, Random.Range(0.18f, 0.3f));
                b.Spin = Random.Range(-540f, 540f);
                b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }
            Spray(at, 8, InkFx.Star(), 0.16f, 1.8f, 3.4f, 0.4f, InkTheme.Ice, OrderTop, 0f);
        }

        // 导电：一圈水面亮起来，电光沿着圈跑，晕是一片一起吃的。
        void Conduct(Vector2 at, float r)
        {
            float d = r * 2f;
            Anim("hitv_water", at, d * 0.7f, d * 1.05f, 0.45f, OrderGround, 0f, 0.7f);
            Ring(at, d * 0.35f, d * 1.1f, 0.42f, InkTheme.Water, 0.85f, OrderGround);
            Sprite zap = Pic("stun_ring");
            if (zap != null)
            {
                Bit b = Add(zap, at, OrderHit, InkTheme.ThunderHi, false);
                b.S0 = Size(zap, d * 0.8f);
                b.S1 = Size(zap, d * 1.05f);
                b.Life = 0.5f;
                b.Spin = 260f;
                b.A0 = 0.95f;
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector2 p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * Random.Range(0.75f, 1f);
                Bit b = Add(InkFx.Star(), p, OrderTop, i % 2 == 0 ? Color.white : InkTheme.ThunderHi, true);
                b.S0 = Vector2.one * 0.34f;
                b.S1 = Vector2.one * 0.06f;
                b.Life = 0.3f;
                b.Delay = i * 0.03f;
                b.Spin = 400f;
            }
        }

        // 镇金：加算吃了乘算，那一下砸下去是一枚金印。
        void WardGold(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.4f);
            Anim("hitv_gold", at, s * 0.9f, s * 1.5f, 0.38f, OrderHit);
            Ring(at, s * 0.3f, s * 1.7f, 0.34f, InkTheme.CoinFace, 0.95f, OrderHit - 1);
            Sprite coin = InkFx.Coin();
            for (int i = 0; i < 5; i++)
            {
                Bit b = Add(coin, at, OrderTop, Color.white, false);
                b.Vel = new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(2.6f, 3.8f));
                b.Gravity = -10f;
                b.Life = 0.55f;
                b.S0 = b.S1 = Vector2.one * Random.Range(0.18f, 0.26f);
                b.Spin = Random.Range(-360f, 360f);
            }
        }

        // 蚀生：毒伤化成一粒绿光，弧线飞回城墙。
        void Leech(Vector2 from, Vector2 to)
        {
            Anim("hitv_wood", from, 0.45f, 0.75f, 0.32f, OrderHit, 0f, 0.8f);
            for (int i = 0; i < 3; i++)
            {
                Bit b = Add(InkFx.Mote(), from, OrderMote, i == 0 ? InkTheme.PoisonHi : InkTheme.Wood, false);
                b.Travel = true;
                b.From = from;
                b.To = to + new Vector2(Random.Range(-0.5f, 0.5f), 0f);
                b.Lift = Random.Range(-1.2f, 1.2f);
                b.Life = 0.7f;
                b.Delay = i * 0.06f;
                b.HoldAlpha = true;
                b.S0 = b.S1 = Vector2.one * (0.34f - 0.08f * i);
            }
        }

        // 夯土：怪被一路夯回去，地上拖一道土烟，落点砸开一圈裂地。
        void RamEarth(Vector2 from, Vector2 to, float r)
        {
            float s = Mathf.Max(1f, r * 2.4f);
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                Vector2 p = Vector2.Lerp(from, to, k) + new Vector2(Random.Range(-0.12f, 0.12f), 0f);
                Bit b = Add(InkFx.SoftDisc(), p, OrderGround, InkTheme.Earth, false);
                b.S0 = Vector2.one * 0.4f;
                b.S1 = Vector2.one * (0.95f + 0.2f * k);
                b.Life = 0.55f;
                b.Delay = k * 0.14f;
                b.A0 = 0.55f;
                b.Vel = new Vector2(Random.Range(-0.4f, 0.4f), 0.2f);
            }
            Ring(to, s * 0.3f, s * 1.9f, 0.4f, InkTheme.Earth, 0.8f, OrderGround, 0.12f);
            Anim("hitv_earth", to, s * 0.9f, s * 1.45f, 0.4f, OrderHit, 0.12f);
            Spray(to, 8, InkFx.Dot(), 0.12f, 1.8f, 3.2f, 0.5f, InkTheme.Earth, OrderTop, -8f, 0.12f);
        }

        // 寒风：横着吹走时拖一道霜，落脚的地方结一层冰。
        void ColdWind(Vector2 from, Vector2 to, float r)
        {
            float s = Mathf.Max(0.9f, r * 2.2f);
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                Vector2 p = Vector2.Lerp(from, to, k) + new Vector2(0f, Random.Range(-0.1f, 0.1f));
                Bit b = Add(InkFx.SoftDisc(), p, OrderGround, InkTheme.IceHi, false);
                b.S0 = Vector2.one * 0.55f;
                b.S1 = Vector2.one * 0.9f;
                b.Life = 0.5f;
                b.Delay = k * 0.08f;
                b.A0 = 0.85f;
                Bit f = Add(InkFx.Star(), p, OrderTop, InkTheme.Ice, false);
                f.Vel = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.3f, 0.9f));
                f.S0 = Vector2.one * 0.2f;
                f.S1 = Vector2.one * 0.05f;
                f.Life = 0.5f;
                f.Delay = k * 0.08f;
                f.Spin = 300f;
            }
            Sprite[] frost = InkVfx.Frames("dot_frost");
            if (frost != null && frost[0] != null)
            {
                Bit b = Add(frost[0], to + Vector2.down * r * 0.6f, OrderGround, Color.white, false);
                b.S0 = Size(frost[0], s * 0.6f);
                b.S1 = Size(frost[0], s * 1.05f);
                b.Life = 1.1f;
                b.Delay = 0.06f;
                b.A0 = 0.9f;
                b.HoldAlpha = true;
            }
            Anim("hitv_wind", to, s * 0.8f, s * 1.2f, 0.36f, OrderHit, 0.04f);
        }

        // 雷决：斩下去那一刀是一道从天而降的雷刃。
        void ThunderCut(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.6f);
            Sprite blade = Pic("word_kill_blade");
            if (blade != null)
            {
                Bit b = Add(blade, at + Vector2.up * 0.4f, OrderTop, InkTheme.ThunderHi, false);
                Vector2 full = Size(blade, s * 1.9f);
                b.S0 = new Vector2(full.x * 1.4f, full.y * 0.2f);
                b.S1 = full;
                b.Life = 0.34f;
                b.A0 = 1f;
            }
            Anim("hitv_thunder", at, s, s * 1.6f, 0.38f, OrderHit, 0.05f);
            Flash(at, s * 0.4f, s * 2f, 0.22f);
            Spray(at, 10, InkFx.Star(), 0.24f, 2.4f, 4.4f, 0.4f, InkTheme.Thunder, OrderTop, 0f, 0.05f);
        }

        // 崩山：甲当场崩成碎块。
        void Crumble(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.4f);
            Glow("crumble_burst", at, s * 0.3f, s * 1.5f, 0.42f, OrderTop, 0f);
            Anim("hitv_earth", at, s * 0.8f, s * 1.2f, 0.36f, OrderHit);
            Sprite shard = Pic("crumble_shards");
            if (shard == null) return;
            for (int i = 0; i < 6; i++)
            {
                Bit b = Add(shard, at, OrderTop, Color.white, false);
                float a = (i / 6f + Random.Range(-0.06f, 0.06f)) * Mathf.PI * 2f;
                b.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f + 0.7f) * Random.Range(2.4f, 3.6f);
                b.Gravity = -9f;
                b.Life = 0.5f;
                b.S0 = b.S1 = Size(shard, Random.Range(0.3f, 0.45f));
                b.Spin = Random.Range(-480f, 480f);
            }
        }

        // 坍缩：先往里一吸，一圈墨光收成一点，再炸开。
        void Collapse(Vector2 at, float r)
        {
            float d = r * 2f;
            Ring(at, d * 1.25f, d * 0.12f, 0.2f, InkTheme.Gather, 0.9f, OrderHit);
            for (int i = 0; i < 10; i++)
            {
                float a = (i / 10f) * Mathf.PI * 2f;
                Vector2 p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 1.2f;
                Bit b = Add(InkFx.Mote(), p, OrderTop, i % 2 == 0 ? InkTheme.GatherHi : InkTheme.Gather, false);
                b.Travel = true;
                b.From = p;
                b.To = at;
                b.Lift = 0f;
                b.Life = 0.2f;
                b.S0 = Vector2.one * 0.26f;
                b.S1 = Vector2.one * 0.08f;
                b.HoldAlpha = true;
            }
            Glow("collapse_burst", at, d * 0.2f, d * 1.05f, 0.42f, OrderTop, 0.16f);
            Flash(at, d * 0.1f, d * 0.8f, 0.2f, 0.16f);
        }

        // 旋风：一团怪被卷成一个旋。
        void Whirl(Vector2 at, float r)
        {
            float d = Mathf.Max(1.2f, r * 2f);
            Bit a = Glow("whirl_burst", at, d * 1.2f, d * 0.7f, 0.6f, OrderHit, 0f);
            if (a != null) a.Spin = 520f;
            Bit b = Glow("whirl_burst", at, d * 0.7f, d * 0.35f, 0.5f, OrderHit + 1, 0.06f);
            if (b != null) b.Spin = -680f;
            Ring(at, d * 1.3f, d * 0.4f, 0.45f, InkTheme.Wind, 0.75f, OrderGround);
        }

        // 回锋：折返那一下亮一道锋。
        void Riposte(Vector2 at, Vector2 toward)
        {
            Vector2 dir = toward - at;
            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Bit b = Glow("riposte_burst", at, 0.4f, 1.5f, 0.36f, OrderTop, 0f);
            if (b != null) b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            Flash(at, 0.2f, 1.1f, 0.16f);
            Sprite shard = Pic("riposte_shards");
            if (shard == null) return;
            for (int i = 0; i < 4; i++)
            {
                Bit s = Add(shard, at, OrderTop, Color.white, false);
                float a = Random.Range(0f, Mathf.PI * 2f);
                s.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.8f, 3f);
                s.Life = 0.35f;
                s.S0 = Size(shard, 0.4f);
                s.S1 = Size(shard, 0.12f);
                s.Spin = Random.Range(-360f, 360f);
            }
        }

        // ---------- 火地 ----------

        public static void SyncFields(List<FireField> fields)
        {
            if (_pool == null) return;
            _pool.Fields(fields);
        }

        void Fields(List<FireField> fields)
        {
            for (int i = 0; i < _patches.Count; i++) _patches[i].Seen = false;
            for (int i = 0; i < fields.Count; i++)
            {
                FireField f = fields[i];
                Patch p = FindPatch(f.Id);
                if (p == null)
                {
                    p = RentPatch(f);
                    Ignite(f);
                }
                p.Seen = true;
                DrawPatch(p, f);
            }
            for (int i = 0; i < _patches.Count; i++)
                if (!_patches[i].Seen && _patches[i].Id != 0) HidePatch(_patches[i]);
        }

        Patch FindPatch(int id)
        {
            for (int i = 0; i < _patches.Count; i++)
                if (_patches[i].Id == id) return _patches[i];
            return null;
        }

        const int FlameCap = 14;

        Patch RentPatch(FireField f)
        {
            Patch p = null;
            for (int i = 0; i < _patches.Count; i++)
                if (_patches[i].Id == 0) { p = _patches[i]; break; }
            if (p == null)
            {
                p = new Patch
                {
                    Scorch = Child("scorch", OrderScorch),
                    Glow = Child("glow", OrderScorch),
                    Flames = new SpriteRenderer[FlameCap],
                    Spots = new Vector2[FlameCap],
                    Phase = new float[FlameCap],
                    Height = new float[FlameCap]
                };
                for (int i = 0; i < FlameCap; i++) p.Flames[i] = Child("flame" + i, OrderFlame);
                _patches.Add(p);
            }
            p.Id = f.Id;
            p.Age = 0f;
            p.EmberAt = 0f;
            p.Scorch.sprite = InkFx.SoftDisc();
            InkFx.PaintSprite(p.Scorch, ScorchInk);
            p.Glow.sprite = InkFx.SoftDisc();
            InkFx.PaintSprite(p.Glow, BurnLooks.Number(f.Look));
            // 火苗沿圈摆一圈，圈里再撒几簇，前后错开节奏，读起来是一片在烧而不是一个贴图。
            int n = Mathf.Clamp(Mathf.RoundToInt(f.R * 8f), 7, FlameCap);
            int ring = Mathf.CeilToInt(n * 0.65f);
            for (int i = 0; i < FlameCap; i++)
            {
                bool on = i < n;
                p.Flames[i].enabled = on;
                if (!on) continue;
                Vector2 spot;
                if (i < ring)
                {
                    float a = (i + Random.Range(-0.25f, 0.25f)) / ring * Mathf.PI * 2f;
                    spot = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * f.R * Random.Range(0.68f, 0.85f);
                }
                else spot = Random.insideUnitCircle * f.R * 0.45f;
                p.Spots[i] = spot;
                p.Phase[i] = Random.Range(0f, 4f);
                p.Height[i] = Random.Range(0.42f, 0.62f);
                InkFx.PaintSprite(p.Flames[i], Color.white);
            }
            return p;
        }

        void Ignite(FireField f)
        {
            Ring(f.Pos, f.R * 0.4f, f.R * 2.3f, 0.35f, BurnLooks.Number(f.Look), 0.9f, OrderHit);
            Anim("hitv_fire", f.Pos, f.R * 1.2f, f.R * 1.9f, 0.4f, OrderHit - 1, 0f, 0.85f);
        }

        void DrawPatch(Patch p, FireField f)
        {
            float dt = Time.deltaTime;
            p.Age += dt;
            float born = Ease.OutBack(Mathf.Clamp01(p.Age / 0.22f));
            float fade = Mathf.Clamp01(f.Life / 0.45f);
            float t = Time.time;

            Place(p.Scorch, f.Pos, Vector2.one * f.R * 2.3f * Mathf.Min(1f, born), 0.42f * Mathf.Max(fade, 0.35f));
            float pulse = 0.85f + 0.15f * Mathf.Sin(t * 9f + p.Id);
            Place(p.Glow, f.Pos, Vector2.one * f.R * 1.9f * born * pulse, 0.38f * fade);

            Sprite[] frames = InkVfx.Frames(BurnLooks.Frames(f.Look));
            for (int i = 0; i < FlameCap; i++)
            {
                SpriteRenderer sr = p.Flames[i];
                if (!sr.enabled) continue;
                if (frames == null) { sr.enabled = false; continue; }
                int at = Mathf.FloorToInt(t * 12f + p.Phase[i] * 3f) % frames.Length;
                Sprite s = frames[at] ?? frames[0];
                sr.sprite = s;
                float h = p.Height[i] * born * Mathf.Lerp(0.35f, 1f, fade)
                          * (0.9f + 0.1f * Mathf.Sin(t * 14f + p.Phase[i] * 5f));
                float k = h / Mathf.Max(0.01f, s.bounds.size.y);
                Vector2 spot = f.Pos + p.Spots[i];
                // 火苗底边落在点位上，往上窜。
                sr.transform.position = new Vector3(spot.x, spot.y + h * 0.42f, 0f);
                sr.transform.localScale = new Vector3(k, k, 1f);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade * 1.2f));
            }

            if (f.Life > 0.2f && t >= p.EmberAt)
            {
                p.EmberAt = t + 0.07f;
                Vector2 at = f.Pos + Random.insideUnitCircle * f.R * 0.8f;
                Color c = Random.value < 0.5f ? BurnLooks.Number(f.Look) : InkTheme.FireHi;
                Bit b = Add(InkFx.Dot(), at, OrderMote - 1, c, false);
                b.Vel = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.9f, 1.5f));
                b.Life = Random.Range(0.5f, 0.75f);
                b.S0 = Vector2.one * Random.Range(0.08f, 0.13f);
                b.S1 = Vector2.one * 0.03f;
                b.HoldAlpha = true;
            }
        }

        void HidePatch(Patch p)
        {
            p.Id = 0;
            p.Scorch.enabled = false;
            p.Glow.enabled = false;
            for (int i = 0; i < FlameCap; i++) p.Flames[i].enabled = false;
        }

        static void Place(SpriteRenderer sr, Vector2 at, Vector2 size, float a)
        {
            sr.enabled = a > 0.005f;
            sr.transform.position = new Vector3(at.x, at.y, 0f);
            Vector2 k = Size(sr.sprite, size);
            sr.transform.localScale = new Vector3(k.x, k.y, 1f);
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }

        SpriteRenderer Child(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        // ---------- 小片 ----------

        static Vector2 Size(Sprite s, float world) => Size(s, Vector2.one * world);

        // 把「世界里要多大」换成缩放：按贴图最长边归一。
        static Vector2 Size(Sprite s, Vector2 world)
        {
            if (s == null) return world;
            Vector3 b = s.bounds.size;
            float span = Mathf.Max(0.001f, Mathf.Max(b.x, b.y));
            return world / span;
        }

        Bit Add(Sprite sprite, Vector2 at, int order, Color c, bool add)
        {
            Bit b = null;
            for (int i = 0; i < _bits.Count; i++)
                if (!_bits[i].On) { b = _bits[i]; break; }
            if (b == null)
            {
                if (_bits.Count >= 320) b = _bits[0];
                else
                {
                    var go = new GameObject("bit");
                    go.transform.SetParent(transform, false);
                    b = new Bit { Sr = go.AddComponent<SpriteRenderer>() };
                    _bits.Add(b);
                }
            }
            b.Sr.sprite = sprite;
            if (add) InkFx.PaintAdd(b.Sr, c);
            else InkFx.PaintSprite(b.Sr, c);
            b.Sr.sortingOrder = order;
            b.Sr.enabled = false;
            b.Sr.transform.localRotation = Quaternion.identity;
            b.Frames = null;
            b.Pos = b.From = b.To = at;
            b.Vel = Vector2.zero;
            b.Age = 0f;
            b.Life = 0.4f;
            b.Delay = 0f;
            b.Spin = 0f;
            b.Gravity = 0f;
            b.Lift = 0f;
            b.A0 = 1f;
            b.S0 = b.S1 = Vector2.one;
            b.C = c;
            b.Travel = false;
            b.HoldAlpha = false;
            b.On = true;
            return b;
        }

        // 平涂序列帧，播一遍。
        Bit Anim(string set, Vector2 at, float s0, float s1, float life, int order, float delay = 0f, float a0 = 1f)
        {
            Sprite[] frames = InkVfx.Frames(set);
            if (frames == null || frames[0] == null) return null;
            Bit b = Add(frames[0], at, order, Color.white, false);
            b.Frames = frames;
            b.S0 = Size(frames[0], s0);
            b.S1 = Size(frames[0], s1);
            b.Life = life;
            b.Delay = delay;
            b.A0 = a0;
            b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));
            return b;
        }

        // 第二幕的发光单张。
        Bit Glow(string name, Vector2 at, float s0, float s1, float life, int order, float delay)
        {
            Sprite s = Pic(name);
            if (s == null) return null;
            Bit b = Add(s, at, order, Color.white, false);
            b.S0 = Size(s, s0);
            b.S1 = Size(s, s1);
            b.Life = life;
            b.Delay = delay;
            return b;
        }

        void Ring(Vector2 at, float d0, float d1, float life, Color c, float a0, int order, float delay = 0f)
        {
            Sprite ring = InkFx.SoftRing();
            Bit b = Add(ring, at, order, c, false);
            b.S0 = Size(ring, d0);
            b.S1 = Size(ring, d1);
            b.Life = life;
            b.A0 = a0;
            b.Delay = delay;
        }

        void Mark(Vector2 at, float d0, float d1, float life, Color c, float a0)
        {
            Sprite disc = InkFx.SoftDisc();
            Bit b = Add(disc, at, OrderScorch, c, false);
            b.S0 = Size(disc, d0);
            b.S1 = Size(disc, d1);
            b.Life = life;
            b.A0 = a0;
            b.HoldAlpha = true;
        }

        void Flash(Vector2 at, float d0, float d1, float life, float delay = 0f)
        {
            Sprite disc = InkFx.SoftDisc();
            Bit b = Add(disc, at, OrderTop, Color.white, true);
            b.S0 = Size(disc, d0);
            b.S1 = Size(disc, d1);
            b.Life = life;
            b.Delay = delay;
            b.A0 = 0.9f;
        }

        void Spray(Vector2 at, int n, Sprite sprite, float size, float lo, float hi, float life, Color c, int order,
            float gravity, float delay = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = (i + Random.Range(-0.35f, 0.35f)) / n * Mathf.PI * 2f;
                Bit b = Add(sprite, at, order, c, false);
                b.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(lo, hi);
                b.Gravity = gravity;
                b.Life = life * Random.Range(0.8f, 1.15f);
                b.S0 = Vector2.one * size;
                b.S1 = Vector2.one * size * 0.3f;
                b.Delay = delay;
                b.Spin = Random.Range(-360f, 360f);
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _bits.Count; i++)
            {
                Bit b = _bits[i];
                if (!b.On) continue;
                if (b.Delay > 0f) { b.Delay -= dt; continue; }
                b.Age += dt;
                float k = Mathf.Clamp01(b.Age / b.Life);
                if (k >= 1f)
                {
                    b.On = false;
                    b.Sr.enabled = false;
                    continue;
                }
                if (b.Travel)
                {
                    float e = k * k * (3f - 2f * k);
                    Vector2 mid = (b.From + b.To) * 0.5f;
                    Vector2 d = b.To - b.From;
                    Vector2 side = new Vector2(-d.y, d.x).normalized * b.Lift;
                    Vector2 c = mid + side;
                    b.Pos = (1 - e) * (1 - e) * b.From + 2f * (1 - e) * e * c + e * e * b.To;
                }
                else
                {
                    b.Vel.y += b.Gravity * dt;
                    if (b.Gravity == 0f) b.Vel *= Mathf.Max(0f, 1f - 3f * dt);
                    b.Pos += b.Vel * dt;
                }
                if (b.Frames != null)
                {
                    int n = b.Frames.Length;
                    int f = Mathf.Min(n - 1, Mathf.FloorToInt(k * n));
                    if (b.Frames[f] != null) b.Sr.sprite = b.Frames[f];
                }
                b.Sr.enabled = true;
                b.Sr.transform.position = new Vector3(b.Pos.x, b.Pos.y, 0f);
                Vector2 s = Vector2.Lerp(b.S0, b.S1, Ease.OutCubic(k));
                b.Sr.transform.localScale = new Vector3(s.x, s.y, 1f);
                if (b.Spin != 0f) b.Sr.transform.Rotate(0f, 0f, b.Spin * dt);
                Color col = b.C;
                float hold = b.HoldAlpha ? 0.6f : 0.3f;
                col.a = b.A0 * (k < hold ? 1f : 1f - (k - hold) / (1f - hold));
                b.Sr.color = col;
            }
        }
    }
}
