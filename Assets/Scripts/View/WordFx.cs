using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 秒杀 / 连斩 / 击退那一发的弹体。照回旋镖光球的路子一层层叠光：
    // 外晕（加色、呼吸）→ 身后残影 → 光刃本体 → 白热芯 → 刃尖闪星。
    // 和单字炮弹的平涂体完全分开，词一出手就认得出是大招。万箭有自己的落箭，不走这里。
    public sealed class WordGlow : MonoBehaviour
    {
        const int Ghosts = 4;
        const float GhostGap = 0.028f;

        SpriteRenderer _halo, _body, _hot, _glint;
        readonly SpriteRenderer[] _ghost = new SpriteRenderer[Ghosts];
        readonly Vector2[] _trail = new Vector2[Ghosts + 1];
        int _trailN;
        float _trailAt, _age, _fxAt;
        int _bid = -1;

        public static bool Owns(ShotMods m) =>
            m.Word == WordId.InstantKill || m.Word == WordId.Cleave || m.Word == WordId.Knockback;

        public void Sync(BulletActor b)
        {
            Ensure();
            if (b.Id != _bid)
            {
                _bid = b.Id;
                _age = 0f;
                _trailN = 0;
                _trailAt = 0f;
                _fxAt = 0f;
            }
            _age += Time.deltaTime;
            Vector2 dir = b.Vel.sqrMagnitude > 1e-4f ? b.Vel.normalized : Vector2.up;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.SetPositionAndRotation(new Vector3(b.Pos.x, b.Pos.y, 0f), Quaternion.Euler(0f, 0f, ang));
            transform.localScale = Vector3.one;
            Record(b.Pos);
            float born = Ease.OutBack(Mathf.Clamp01(_age / 0.14f));
            switch (b.Mods.Word)
            {
                case WordId.InstantKill: Kill(b.Pos, dir, born); break;
                case WordId.Cleave: Cleave(b.Pos, dir, born); break;
                default: Knock(b.Pos, dir, ang, born); break;
            }
        }

        public void Hide()
        {
            if (_body == null) return;
            _halo.enabled = _body.enabled = _hot.enabled = _glint.enabled = false;
            for (int i = 0; i < Ghosts; i++) _ghost[i].enabled = false;
        }

        // 秒杀：一把竖着的红光刀，刃尖一颗闪星，身后拖三道淡下去的刀影，沿路掉火星。
        void Kill(Vector2 pos, Vector2 dir, float born)
        {
            float t = _age;
            float len = 1.35f * born;
            Fit(_body, WordFx.Art("word_kill_blade"), len, Color.white);
            Widen(_body, WordFx.BladeWide);
            _body.transform.localPosition = new Vector3(0f, -len * 0.2f, 0f);
            Soft(_halo, WordFx.Crimson, new Vector2(0.9f, 1.8f) * born * (1f + 0.08f * Mathf.Sin(t * 16f)), 0.5f, -0.22f);
            Soft(_hot, Color.white, new Vector2(0.2f, 0.95f) * born, 0.55f, -0.1f);
            Glint(len * 0.3f, 0.5f * (0.85f + 0.3f * Mathf.Abs(Mathf.Sin(t * 21f))), WordFx.CrimsonHi);
            Trail(WordFx.Art("word_kill_blade"), len, WordFx.Crimson, 0.55f, -len * 0.2f, 0f, 0f);
            if (t >= _fxAt)
            {
                _fxAt = t + 0.035f;
                Vector2 side = new Vector2(-dir.y, dir.x) * Random.Range(-0.18f, 0.18f);
                Color c = Random.value < 0.4f ? WordFx.Ember : WordFx.CrimsonHi;
                WordFx.Spark(pos - dir * 0.55f + side, -dir * Random.Range(0.6f, 1.8f) + side * 2f,
                    Random.Range(0.16f, 0.28f), c, 0.32f);
            }
        }

        // 连斩：一柄蓝色光剑，身后的残影左右晃开，飞着就是一串剑影。
        void Cleave(Vector2 pos, Vector2 dir, float born)
        {
            float t = _age;
            float len = 1.4f * born;
            Fit(_body, WordFx.Art("word_cleave_sword"), len, Color.white);
            Widen(_body, 1.2f);
            _body.transform.localPosition = new Vector3(0f, -len * 0.18f, 0f);
            Soft(_halo, WordFx.Azure, new Vector2(0.8f, 1.7f) * born * (1f + 0.07f * Mathf.Sin(t * 14f)), 0.45f, -0.2f);
            Soft(_hot, Color.white, new Vector2(0.16f, 0.85f) * born, 0.5f, -0.1f);
            Glint(len * 0.32f, 0.38f, WordFx.AzureHi);
            Trail(WordFx.Art("word_cleave_sword"), len, WordFx.AzureHi, 0.6f, -len * 0.18f, 0.2f, t * 22f);
            if (t >= _fxAt)
            {
                _fxAt = t + 0.04f;
                Vector2 side = new Vector2(-dir.y, dir.x) * Random.Range(-0.25f, 0.25f);
                WordFx.Spark(pos - dir * 0.45f + side, -dir * Random.Range(0.5f, 1.4f) + side,
                    Random.Range(0.14f, 0.24f), Random.value < 0.5f ? WordFx.AzureHi : Color.white, 0.28f);
            }
        }

        // 击退：一道比一列还宽的紫色冲击波，弓背朝前推出去，身后一圈圈甩出涟漪。
        void Knock(Vector2 pos, Vector2 dir, float ang, float born)
        {
            float t = _age;
            float wide = 1.7f * born * (1f + 0.05f * Mathf.Sin(t * 18f));
            Fit(_body, WordFx.Art("word_knock_wave"), wide, Color.white);
            _body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Soft(_halo, WordFx.Violet, new Vector2(2.1f, 1.0f) * born, 0.42f, -0.05f);
            Soft(_hot, Color.white, new Vector2(1.1f, 0.22f) * born, 0.45f, 0.28f);
            _glint.enabled = false;
            Trail(WordFx.Art("word_knock_wave"), wide * 0.86f, WordFx.Lilac, 0.4f, 0.12f, 0f, 0f);
            if (t >= _fxAt)
            {
                _fxAt = t + 0.085f;
                WordFx.Ring(pos - dir * 0.2f, ang, 0.45f, 1.0f, 0.3f, WordFx.Lilac);
                Vector2 side = new Vector2(-dir.y, dir.x) * Random.Range(-0.7f, 0.7f);
                WordFx.Spark(pos + side, -dir * Random.Range(1.2f, 2.4f), Random.Range(0.14f, 0.22f), WordFx.Lilac, 0.25f);
            }
        }

        void Record(Vector2 pos)
        {
            if (_age < _trailAt) return;
            _trailAt = _age + GhostGap;
            for (int i = Mathf.Min(_trailN, Ghosts); i > 0; i--) _trail[i] = _trail[i - 1];
            _trail[0] = pos;
            _trailN = Mathf.Min(_trailN + 1, Ghosts + 1);
        }

        // 残影按世界坐标摆在走过的点上；sway 给连斩用，残影左右晃开成一串剑影。
        void Trail(Sprite sprite, float len, Color tint, float alpha, float sink, float sway, float phase)
        {
            Quaternion rot = transform.rotation;
            Vector3 side = rot * Vector3.right;
            Vector3 back = rot * new Vector3(0f, sink, 0f);
            for (int i = 0; i < Ghosts; i++)
            {
                SpriteRenderer sr = _ghost[i];
                bool on = i + 1 < _trailN && i < 3;
                sr.enabled = on;
                if (!on) continue;
                Vector2 at = _trail[i + 1];
                float k = 1f - 0.2f * (i + 1);
                float wob = sway * Mathf.Sin(phase + i * 2.3f) * (i + 1) * 0.6f;
                sr.sprite = sprite;
                sr.transform.SetPositionAndRotation(new Vector3(at.x, at.y, 0f) + back + side * wob, rot);
                sr.transform.localScale = Vector3.one * (len * k / Size(sprite));
                tint.a = alpha * k;
                sr.color = tint;
            }
        }

        void Glint(float tip, float size, Color tint)
        {
            _glint.enabled = true;
            _glint.transform.localPosition = new Vector3(0f, tip + 0.18f, 0f);
            _glint.transform.localRotation = Quaternion.Euler(0f, 0f, _age * 240f);
            _glint.transform.localScale = Vector3.one * size;
            _glint.color = tint;
        }

        static void Fit(SpriteRenderer sr, Sprite sprite, float len, Color tint)
        {
            sr.enabled = sprite != null;
            if (sprite == null) return;
            sr.sprite = sprite;
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one * (len / Size(sprite));
            sr.color = tint;
        }

        static void Widen(SpriteRenderer sr, float k)
        {
            Vector3 s = sr.transform.localScale;
            sr.transform.localScale = new Vector3(s.x * k, s.y, 1f);
        }

        static void Soft(SpriteRenderer sr, Color c, Vector2 size, float a, float y)
        {
            sr.enabled = true;
            sr.transform.localPosition = new Vector3(0f, y, 0f);
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
            c.a = Mathf.Clamp01(a);
            sr.color = c;
        }

        static float Size(Sprite s) => s != null ? Mathf.Max(0.01f, Mathf.Max(s.bounds.size.x, s.bounds.size.y)) : 1f;

        void Ensure()
        {
            if (_body != null) return;
            int o = 11;
            _halo = Layer("wordHalo", InkFx.SoftDisc(), o - 1, true);
            _body = Layer("wordBody", null, o + 1, false);
            _hot = Layer("wordHot", InkFx.SoftDisc(), o + 2, true);
            _glint = Layer("wordGlint", WordFx.Art("dart_spark"), o + 3, true);
            for (int i = 0; i < Ghosts; i++) _ghost[i] = Layer("wordGhost" + i, null, o, false);
        }

        SpriteRenderer Layer(string name, Sprite sprite, int order, bool add)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (add) InkFx.PaintAdd(sr, Color.white);
            else InkFx.PaintSprite(sr, Color.white);
            sr.enabled = false;
            return sr;
        }
    }

    // 三个词的命中光效：
    // 秒杀 —— 光刀插在怪身上，红光往里收，怪死（或头目挨完这一刀）的那一刻炸开；
    // 连斩 —— 剑影从上一只闪到下一只，每只身上一阵乱斩；
    // 击退 —— 冲击波垫在怪身下一路顶回去，世界那边顶到位了才散。
    public sealed class WordFx : MonoBehaviour
    {
        public static readonly Color Crimson = InkTheme.Hex("FF2E4D");
        public static readonly Color CrimsonHi = InkTheme.Hex("FF9AA8");
        public static readonly Color Ember = InkTheme.Hex("FF8A3D");
        public static readonly Color Azure = InkTheme.Hex("3E9BFF");
        public static readonly Color AzureHi = InkTheme.Hex("BFE3FF");
        public static readonly Color Violet = InkTheme.Hex("B45CFF");
        public static readonly Color Lilac = InkTheme.Hex("E3C4FF");
        // 光刀那张图刃身偏细，横向撑开一点才压得住场。
        public const float BladeWide = 1.4f;

        sealed class Bit
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel, S0, S1;
            public float Age, Life, A0, Spin, Delay, Drag;
            public Color C;
            public EnemyActor Follow;
            public Vector2 Anchor;
            public bool On;
        }

        // 跟着怪、等世界那边收尾的那一件：秒杀插着的刀、击退垫着的波。
        sealed class Pin
        {
            public WordId Word;
            public EnemyActor Target;
            public Vector2 At, Dir;
            public float Age, Hold, Fade, EmitAt;
            public bool Lethal;
            public SpriteRenderer Body, Glow, Hot;
            public bool On;
        }

        static WordFx _pool;
        static readonly Dictionary<string, Sprite> _art = new Dictionary<string, Sprite>();
        readonly List<Bit> _bits = new List<Bit>();
        readonly List<Pin> _pins = new List<Pin>();

        public static Sprite Art(string name)
        {
            if (_art.TryGetValue(name, out Sprite s) && s != null) return s;
            s = InkSprites.Load("Vfx/" + name);
            if (s == null) s = InkFx.SoftDisc();
            _art[name] = s;
            return s;
        }

        public static void Bind(Transform root)
        {
            if (_pool != null && _pool.transform.parent == root) return;
            var go = new GameObject("wordfx");
            go.transform.SetParent(root, false);
            _pool = go.AddComponent<WordFx>();
        }

        public static void Hit(WordHit h)
        {
            if (_pool == null || h.Target == null) return;
            switch (h.Word)
            {
                case WordId.InstantKill: _pool.PinKill(h); break;
                case WordId.Cleave: _pool.Slash(h); break;
                case WordId.Knockback: _pool.PinKnock(h); break;
            }
        }

        public static void Spark(Vector2 at, Vector2 vel, float size, Color c, float life)
        {
            if (_pool == null) return;
            _pool.Add("dart_spark", at, vel, Vector2.one * size, Vector2.one * size * 0.2f, life, c, 0.95f,
                Random.Range(0f, 90f), Random.Range(-240f, 240f), true, 10);
        }

        public static void Ring(Vector2 at, float rot, float s0, float s1, float life, Color c)
        {
            if (_pool == null) return;
            _pool.Add("word_knock_ring", at, Vector2.zero, Vector2.one * s0, Vector2.one * s1, life, c, 0.7f,
                rot, 0f, true, 10);
        }

        static Vector2 Seen(EnemyActor e) => e.Pos + e.Recoil;

        static float Body(EnemyActor e) => Mathf.Clamp(e.Radius * 3f, 0.8f, 2.4f);

        // ---------- 秒杀 ----------

        void PinKill(WordHit h)
        {
            EnemyActor e = h.Target;
            Vector2 at = Seen(e);
            Vector2 dir = at - h.From;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector2.up;
            Pin p = Take(WordId.InstantKill, e, at, dir);
            p.Lethal = e.Doom > 0f;
            p.Hold = p.Lethal ? 1.2f : 0.3f;
            float size = Body(e);
            // 刀插进去那一下：一圈红光往里收，攒着劲等爆。
            Add("ring", at, Vector2.zero, Vector2.one * size * 1.9f, Vector2.one * size * 0.5f, 0.3f, Crimson, 0.9f,
                0f, 0f, true, 13, 0f, e);
            Add("disc", at, Vector2.zero, Vector2.one * 0.4f, Vector2.one * size * 1.2f, 0.12f, Color.white, 0.8f,
                0f, 0f, true, 14, 0f, e);
        }

        void TickKill(Pin p, float dt)
        {
            EnemyActor e = p.Target;
            bool alive = !e.Dead && (p.Lethal ? e.Doom > 0f : p.Age < p.Hold);
            if (!e.Dead) p.At = Seen(e);
            float size = Body(e);
            if (alive && p.Age < p.Hold)
            {
                float k = Mathf.Clamp01(p.Age / DoomLook);
                // 刀身插得越来越深、抖得越来越厉害，红光越聚越亮。
                Vector2 shake = Random.insideUnitCircle * 0.035f * (0.4f + k);
                float len = 1.35f * Mathf.Lerp(1.25f, 1f, Ease.OutCubic(Mathf.Clamp01(p.Age / 0.08f)));
                // 刀尖扎过怪心一点，越往后扎得越深。
                float depth = Mathf.Lerp(0.2f, 0.4f, k);
                Vector2 mid = p.At - p.Dir * (len * 0.5f - depth) + shake;
                float s = len / SizeOf(p.Body.sprite);
                Place(p.Body, mid, Rot(p.Dir), new Vector2(s * BladeWide, s), Color.white);
                float pulse = 0.85f + 0.3f * Mathf.Abs(Mathf.Sin(p.Age * 40f));
                Place(p.Glow, p.At, 0f, Vector2.one * size * Mathf.Lerp(0.7f, 1.7f, k), Fade(Crimson, 0.75f * pulse));
                Place(p.Hot, p.At, 0f, Vector2.one * size * Mathf.Lerp(0.15f, 0.6f, k), Fade(Color.white, 0.8f * pulse));
                if (p.Age >= p.EmitAt)
                {
                    p.EmitAt = p.Age + 0.03f;
                    Vector2 v = Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.6f);
                    Add("dart_spark", p.At, v, Vector2.one * 0.22f, Vector2.one * 0.04f, 0.22f,
                        Random.value < 0.5f ? Ember : CrimsonHi, 1f, 0f, 300f, true, 14);
                }
                return;
            }
            Boom(p.At, p.Dir, size * (e.IsBoss ? 1.3f : 1f) * (p.Lethal ? 1f : 0.75f));
            Drop(p);
        }

        // 光刀撑到这一刻炸开：交叉斩痕 + 白闪 + 红环 + 一圈火星。世界那边同一帧判死。
        const float DoomLook = 0.32f;

        void Boom(Vector2 at, Vector2 dir, float size)
        {
            float rot = Rot(dir) + Random.Range(-12f, 12f);
            Add("word_kill_burst", at, Vector2.zero, Vector2.one * size * 0.3f, Vector2.one * size * 1.25f, 0.34f,
                Color.white, 1f, rot, 0f, false, 15);
            Add("word_kill_burst", at, Vector2.zero, Vector2.one * size * 0.2f, Vector2.one * size * 0.95f, 0.3f,
                CrimsonHi, 0.9f, rot + 45f, 0f, true, 15, 0.04f);
            Add("disc", at, Vector2.zero, Vector2.one * size * 0.6f, Vector2.one * size * 2.6f, 0.2f, Color.white, 1f,
                0f, 0f, true, 15);
            Add("ring", at, Vector2.zero, Vector2.one * size * 0.4f, Vector2.one * size * 3f, 0.38f, Crimson, 1f,
                0f, 0f, true, 14);
            Add("word_kill_blade", at - dir * 0.15f, dir * 0.6f, Vector2.one * 0.7f, Vector2.one * 0.95f, 0.14f,
                Color.white, 0.9f, Rot(dir), 0f, false, 15);
            for (int i = 0; i < 14; i++)
            {
                float a = (i + Random.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / 14f;
                var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(3f, 6.5f) * size * 0.7f;
                Color c = i % 3 == 0 ? Ember : i % 3 == 1 ? CrimsonHi : Color.white;
                Add("dart_spark", at, v, Vector2.one * Random.Range(0.24f, 0.4f), Vector2.one * 0.05f,
                    Random.Range(0.25f, 0.42f), c, 1f, Random.Range(0f, 90f), Random.Range(-360f, 360f), true, 15);
            }
        }

        // ---------- 连斩 ----------

        const int SlashCount = 6;

        void Slash(WordHit h)
        {
            EnemyActor e = h.Target;
            Vector2 to = Seen(e);
            float start = h.Step * 0.07f;
            float size = Body(e);
            // 剑影从上一只闪过来：一道光剑沿连线扫过去。
            Vector2 d = to - h.From;
            if (d.sqrMagnitude > 0.04f)
            {
                const float dash = 0.08f;
                Add("word_cleave_sword", h.From, d / dash, Vector2.one * 0.6f, Vector2.one * 0.6f, dash,
                    AzureHi, 0.9f, Rot(d.normalized), 0f, true, 13, start, null, 0f);
                start += dash;
            }
            for (int i = 0; i < SlashCount; i++)
            {
                float delay = start + i * 0.045f;
                float rot = i * 67f + Random.Range(-20f, 20f);
                Vector2 off = Random.insideUnitCircle * e.Radius * 0.35f;
                float s = size * Random.Range(0.55f, 0.75f);
                Add("word_cleave_slash", off, Vector2.zero, new Vector2(s * 0.8f, s * 1.1f), new Vector2(s, s * 1.25f), 0.14f,
                    Color.white, 1f, rot, 0f, false, 14, delay, e);
                Add("word_cleave_slash", off, Vector2.zero, new Vector2(s * 0.6f, s), new Vector2(s * 0.8f, s * 1.2f), 0.12f,
                    AzureHi, 0.7f, rot, 0f, true, 15, delay, e);
                Vector2 v = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3f);
                Add("dart_spark", off, v, Vector2.one * 0.2f, Vector2.one * 0.04f, 0.2f,
                    i % 2 == 0 ? AzureHi : Color.white, 1f, 0f, 300f, true, 15, delay, e);
            }
            Add("disc", Vector2.zero, Vector2.zero, Vector2.one * size * 0.4f, Vector2.one * size * 1.5f, 0.2f,
                Azure, 0.75f, 0f, 0f, true, 13, start + SlashCount * 0.045f, e);
        }

        // ---------- 击退 ----------

        void PinKnock(WordHit h)
        {
            EnemyActor e = h.Target;
            Pin p = Take(WordId.Knockback, e, Seen(e), Vector2.up);
            p.Hold = 1.0f;
            Add("ring", Seen(e), Vector2.zero, Vector2.one * 0.4f, Vector2.one * Body(e) * 1.8f, 0.24f, Lilac, 0.9f,
                0f, 0f, true, 13);
        }

        void TickKnock(Pin p, float dt)
        {
            EnemyActor e = p.Target;
            bool pushing = !e.Dead && e.KnockLeft > 0f && p.Age < p.Hold && p.Fade <= 0f;
            if (!e.Dead) p.At = Seen(e);
            float wide = Mathf.Max(1.5f, e.Radius * 4.2f);
            Vector2 under = p.At + Vector2.down * (e.Radius + 0.12f);
            if (!pushing)
            {
                // 顶到位：波往前一冲、放大、淡掉。
                p.Fade += dt;
                float k = Mathf.Clamp01(p.Fade / 0.2f);
                if (k >= 1f)
                {
                    Drop(p);
                    return;
                }
                Vector2 at = under + Vector2.up * 0.25f * k;
                Place(p.Body, at, 0f, Vector2.one * wide * (1f + 0.2f * k) / SizeOf(p.Body.sprite), Fade(Color.white, 1f - k));
                Place(p.Glow, at, 0f, new Vector2(wide * 1.2f, wide * 0.5f) * (1f + 0.3f * k), Fade(Violet, 0.5f * (1f - k)));
                p.Hot.enabled = false;
                return;
            }
            float pulse = 1f + 0.05f * Mathf.Sin(p.Age * 30f);
            Place(p.Body, under, 0f, Vector2.one * wide * pulse / SizeOf(p.Body.sprite), Color.white);
            Place(p.Glow, under + Vector2.down * 0.1f, 0f, new Vector2(wide * 1.25f, wide * 0.55f), Fade(Violet, 0.55f));
            Place(p.Hot, under + Vector2.up * 0.18f, 0f, new Vector2(wide * 0.7f, 0.2f), Fade(Color.white, 0.6f));
            if (p.Age >= p.EmitAt)
            {
                p.EmitAt = p.Age + 0.03f;
                // 风往下甩：一道道气流从波面两侧往后刷，看得出是被顶着走。
                Vector2 at = under + Vector2.right * Random.Range(-wide * 0.45f, wide * 0.45f);
                Add("dart_spark", at, Vector2.down * Random.Range(2f, 3.6f), new Vector2(0.1f, 0.42f), new Vector2(0.04f, 0.2f),
                    0.2f, Random.value < 0.5f ? Lilac : Color.white, 0.9f, 0f, 0f, true, 12);
            }
        }

        // ---------- 池 ----------

        Pin Take(WordId word, EnemyActor e, Vector2 at, Vector2 dir)
        {
            Pin p = null;
            for (int i = 0; i < _pins.Count; i++)
                if (!_pins[i].On) { p = _pins[i]; break; }
            if (p == null)
            {
                p = new Pin
                {
                    Body = Spawn("pinBody", 14, false),
                    Glow = Spawn("pinGlow", 12, true),
                    Hot = Spawn("pinHot", 15, true)
                };
                _pins.Add(p);
            }
            p.Word = word;
            p.Target = e;
            p.At = at;
            p.Dir = dir;
            p.Age = 0f;
            p.Fade = 0f;
            p.EmitAt = 0f;
            p.Lethal = false;
            p.Body.sprite = Art(word == WordId.InstantKill ? "word_kill_blade" : "word_knock_wave");
            p.Glow.sprite = InkFx.SoftDisc();
            p.Hot.sprite = InkFx.SoftDisc();
            p.On = true;
            return p;
        }

        void Drop(Pin p)
        {
            p.On = false;
            p.Target = null;
            p.Body.enabled = p.Glow.enabled = p.Hot.enabled = false;
        }

        // Follow 不为空时 at 是相对怪身的偏移，怪走到哪光效跟到哪；怪死了就停在最后看见的位置。
        void Add(string art, Vector2 at, Vector2 vel, Vector2 s0, Vector2 s1, float life, Color c, float a0,
            float rot, float spin, bool add, int order, float delay = 0f, EnemyActor follow = null, float drag = 3.5f)
        {
            Bit b = null;
            for (int i = 0; i < _bits.Count; i++)
                if (!_bits[i].On) { b = _bits[i]; break; }
            if (b == null)
            {
                b = new Bit { Sr = Spawn("bit", order, add) };
                _bits.Add(b);
            }
            b.Sr.sprite = art == "disc" ? InkFx.SoftDisc() : art == "ring" ? InkFx.SoftRing() : Art(art);
            if (add) InkFx.PaintAdd(b.Sr, c);
            else InkFx.PaintSprite(b.Sr, c);
            b.Sr.sortingOrder = order;
            b.Sr.enabled = false;
            b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            b.Pos = at;
            b.Vel = vel;
            b.Age = 0f;
            b.Life = Mathf.Max(0.05f, life);
            b.S0 = s0;
            b.S1 = s1;
            b.A0 = a0;
            b.Spin = spin;
            b.Delay = delay;
            b.Drag = drag;
            b.C = c;
            b.Follow = follow;
            b.Anchor = follow != null ? Seen(follow) : Vector2.zero;
            b.On = true;
        }

        SpriteRenderer Spawn(string name, int order, bool add)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            if (add) InkFx.PaintAdd(sr, Color.white);
            else InkFx.PaintSprite(sr, Color.white);
            sr.enabled = false;
            return sr;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _pins.Count; i++)
            {
                Pin p = _pins[i];
                if (!p.On) continue;
                p.Age += dt;
                if (p.Word == WordId.InstantKill) TickKill(p, dt);
                else TickKnock(p, dt);
            }
            for (int i = 0; i < _bits.Count; i++)
            {
                Bit b = _bits[i];
                if (!b.On) continue;
                if (b.Follow != null && !b.Follow.Dead) b.Anchor = Seen(b.Follow);
                if (b.Delay > 0f) { b.Delay -= dt; continue; }
                b.Age += dt;
                float k = Mathf.Clamp01(b.Age / b.Life);
                if (k >= 1f)
                {
                    b.On = false;
                    b.Follow = null;
                    b.Sr.enabled = false;
                    continue;
                }
                if (b.Drag > 0f) b.Vel *= Mathf.Max(0f, 1f - b.Drag * dt);
                b.Pos += b.Vel * dt;
                Vector2 at = b.Follow != null ? b.Anchor + b.Pos : b.Pos;
                b.Sr.enabled = true;
                b.Sr.transform.position = new Vector3(at.x, at.y, 0f);
                Vector2 s = Vector2.Lerp(b.S0, b.S1, Ease.OutCubic(k));
                b.Sr.transform.localScale = new Vector3(s.x, s.y, 1f);
                if (b.Spin != 0f) b.Sr.transform.Rotate(0f, 0f, b.Spin * dt);
                Color c = b.C;
                c.a = b.A0 * (k < 0.25f ? 1f : 1f - (k - 0.25f) / 0.75f);
                b.Sr.color = c;
            }
        }

        static void Place(SpriteRenderer sr, Vector2 at, float rot, Vector2 scale, Color c)
        {
            sr.enabled = true;
            sr.transform.position = new Vector3(at.x, at.y, 0f);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            sr.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            sr.color = c;
        }

        static float Rot(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        static float SizeOf(Sprite s) => s != null ? Mathf.Max(0.01f, Mathf.Max(s.bounds.size.x, s.bounds.size.y)) : 1f;

        static Color Fade(Color c, float a)
        {
            c.a = Mathf.Clamp01(a);
            return c;
        }
    }
}
