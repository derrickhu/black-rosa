using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 回旋镖光球。照着「一串发光球」那种样子一层层叠：
    // 品红外晕（加色、呼吸）→ 身后甩的月牙光弧 → 紫粉球壳 → 橙黄风车芯（自转）→ 白热芯（闪）。
    // 身后拖一串越来越小的残球，沿路撒闪星。宿主 SpriteRenderer 就是球壳本身。
    public sealed class DartOrb : MonoBehaviour
    {
        const int Ghosts = 5;
        const float SparkGap = 0.045f;

        SpriteRenderer _host;
        SpriteRenderer _glow, _arc, _swirl, _core, _hot;
        SpriteRenderer[] _ghost, _ghostCore;
        float _sparkAt;

        public static Sprite Shell => DartFx.Art("dart_shell");

        public void Sync(BattleWorld.DartActor d)
        {
            Ensure();
            float t = d.Age;
            float born = Ease.OutBack(Mathf.Clamp01(t / 0.2f));
            float fade = Mathf.Clamp01(d.Left / 0.35f);
            float s = Mathf.Max(0.05f, born * Mathf.Lerp(0.55f, 1f, fade));
            transform.localScale *= s;
            // 最后一秒快闪，提醒它要散了。
            float warn = d.Left < 1f ? 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(t * 18f)) : 1f;

            _host.color = new Color(1f, 1f, 1f, warn);
            Paint(_glow, InkTheme.Hex("FF3EC8"), 3.1f * (1f + 0.08f * Mathf.Sin(t * 13f)), 0.62f * warn);
            Paint(_hot, Color.white, 0.55f * (0.85f + 0.3f * Mathf.Abs(Mathf.Sin(t * 23f))), 0.85f * warn);
            Paint(_core, Color.white, 0.6f, warn);
            _core.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 900f);

            // 月牙的实心那边在贴图右下，转到走向的正后方。
            float heading = Mathf.Atan2(d.Vel.y, d.Vel.x) * Mathf.Rad2Deg;
            Paint(_arc, Color.white, 1.2f, 0.9f * warn);
            _arc.transform.localRotation = Quaternion.Euler(0f, 0f, heading + 225f);
            Paint(_swirl, Color.white, 1.36f, 0.42f * warn);
            _swirl.transform.localRotation = Quaternion.Euler(0f, 0f, t * 520f);

            for (int i = 0; i < Ghosts; i++)
            {
                bool on = i + 1 < d.TrailN;
                _ghost[i].enabled = on;
                _ghostCore[i].enabled = on;
                if (!on) continue;
                Vector2 at = d.Trail[i + 1];
                float gs = (0.84f - 0.12f * i) * s * BattleView.DartScale;
                float ga = (0.62f - 0.11f * i) * warn;
                Place(_ghost[i], at, gs, ga);
                Place(_ghostCore[i], at, gs * 0.6f, ga);
                _ghostCore[i].transform.rotation = Quaternion.Euler(0f, 0f, -t * 900f + 30f * (i + 1));
            }

            if (t >= _sparkAt)
            {
                _sparkAt = t + SparkGap;
                Vector2 back = d.Vel.sqrMagnitude > 0.01f ? -d.Vel.normalized : Vector2.down;
                Vector2 side = new Vector2(-back.y, back.x) * Random.Range(-0.3f, 0.3f);
                Color c = Random.value < 0.35f ? InkTheme.Hex("5FF2FF") : InkTheme.Hex("FF7AD9");
                DartFx.Spark(d.Pos + back * 0.35f + side, back * Random.Range(0.6f, 1.6f) + side,
                    Random.Range(0.18f, 0.32f), c, 0.4f);
            }
        }

        static void Paint(SpriteRenderer sr, Color c, float scale, float a)
        {
            sr.transform.localScale = Vector3.one * scale;
            c.a = Mathf.Clamp01(a);
            sr.color = c;
        }

        static void Place(SpriteRenderer sr, Vector2 at, float scale, float a)
        {
            sr.transform.position = new Vector3(at.x, at.y, 0f);
            sr.transform.localScale = Vector3.one * scale;
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        }

        void Ensure()
        {
            if (_host != null && _glow != null) return;
            _host = GetComponent<SpriteRenderer>();
            int o = _host.sortingOrder;
            _glow = Layer("glow", InkFx.SoftDisc(), o - 3, true);
            _arc = Layer("arc", DartFx.Art("dart_arc"), o - 1, false);
            _swirl = Layer("swirl", DartFx.Art("dart_arc"), o + 1, true);
            _core = Layer("core", DartFx.Art("dart_core"), o + 1, false);
            _hot = Layer("hot", InkFx.SoftDisc(), o + 2, true);
            _ghost = new SpriteRenderer[Ghosts];
            _ghostCore = new SpriteRenderer[Ghosts];
            for (int i = 0; i < Ghosts; i++)
            {
                // 残球挂在宿主的父节点上：它们按世界坐标摆，不跟着宿主缩放。
                _ghost[i] = Loose("ghost" + i, Shell, o - 2);
                _ghostCore[i] = Loose("ghostCore" + i, DartFx.Art("dart_core"), o - 2);
            }
        }

        SpriteRenderer Layer(string name, Sprite sprite, int order, bool add)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (add) InkFx.PaintAdd(sr, Color.white);
            return sr;
        }

        SpriteRenderer Loose(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform.parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        void OnDestroy()
        {
            if (_ghost == null) return;
            for (int i = 0; i < Ghosts; i++)
            {
                if (_ghost[i] != null) Destroy(_ghost[i].gameObject);
                if (_ghostCore[i] != null) Destroy(_ghostCore[i].gameObject);
            }
        }
    }

    // 光球的爆点和沿路闪星。全是一次性的小片，放在一个池里统一走时间。
    public sealed class DartFx : MonoBehaviour
    {
        sealed class Bit
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel;
            public float Age, Life, S0, S1, A0, Spin, Delay;
            public Color C;
            public bool On;
        }

        static DartFx _pool;
        static readonly Dictionary<string, Sprite> _art = new Dictionary<string, Sprite>();
        readonly List<Bit> _bits = new List<Bit>();

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
            var go = new GameObject("dartfx");
            go.transform.SetParent(root, false);
            _pool = go.AddComponent<DartFx>();
        }

        static readonly Color Pink = new Color(1f, 0.24f, 0.78f, 1f);
        static readonly Color Cyan = new Color(0.37f, 0.95f, 1f, 1f);

        public static void Pop(BattleWorld.DartPop p)
        {
            if (_pool == null) return;
            Vector2 at = p.Pos;
            switch (p.Kind)
            {
                case BattleWorld.DartPop.Hit:
                    _pool.Add("disc", at, Vector2.zero, 0.6f, 2.6f, 0.16f, Pink, 0.8f, 0f, 0f, true, 16);
                    _pool.Add("dart_spark", at, Vector2.zero, 0.4f, 1.7f, 0.2f, Color.white, 1f, Random.Range(0f, 90f), 0f, true, 18);
                    _pool.Add("dart_ring", at, Vector2.zero, 0.15f, 0.8f, 0.3f, Color.white, 1f, Random.Range(0f, 360f), 0f, false, 17);
                    Burst(at, 7, 2.6f, 4.6f);
                    break;
                case BattleWorld.DartPop.Wall:
                    _pool.Add("dart_ring", at, Vector2.zero, 0.1f, 0.42f, 0.2f, Cyan, 0.85f, 0f, 0f, true, 17);
                    Burst(at, 3, 1.6f, 2.8f);
                    break;
                case BattleWorld.DartPop.Launch:
                    _pool.Add("disc", at, Vector2.zero, 0.8f, 3.2f, 0.22f, Pink, 0.9f, 0f, 0f, true, 16);
                    _pool.Add("dart_ring", at, Vector2.zero, 0.15f, 1f, 0.32f, Color.white, 1f, 0f, 0f, false, 17);
                    _pool.Add("dart_spark", at, Vector2.zero, 0.5f, 2f, 0.24f, Color.white, 1f, 0f, 0f, true, 18);
                    Burst(at, 9, 2.4f, 5f);
                    break;
                default:
                    _pool.Add("disc", at, Vector2.zero, 1f, 4.2f, 0.3f, Pink, 1f, 0f, 0f, true, 16);
                    _pool.Add("dart_ring", at, Vector2.zero, 0.2f, 1.4f, 0.42f, Color.white, 1f, 0f, 0f, false, 17);
                    _pool.Add("dart_ring", at, Vector2.zero, 0.1f, 0.95f, 0.36f, Cyan, 0.8f, 45f, 0f, true, 17, 0.08f);
                    _pool.Add("dart_spark", at, Vector2.zero, 0.6f, 2.8f, 0.3f, Color.white, 1f, 0f, 120f, true, 18);
                    Burst(at, 14, 3f, 6.5f);
                    break;
            }
        }

        public static void Spark(Vector2 at, Vector2 vel, float size, Color c, float life)
        {
            if (_pool == null) return;
            _pool.Add("dart_spark", at, vel, size, size * 0.2f, life, c, 0.95f, Random.Range(0f, 90f),
                Random.Range(-240f, 240f), true, 11);
        }

        static void Burst(Vector2 at, int n, float lo, float hi)
        {
            for (int i = 0; i < n; i++)
            {
                float ang = (i + Random.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / n;
                var v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(lo, hi);
                Color c = i % 3 == 0 ? Cyan : i % 3 == 1 ? Pink : Color.white;
                _pool.Add("dart_spark", at, v, Random.Range(0.22f, 0.4f), 0.05f, Random.Range(0.25f, 0.4f), c, 1f,
                    Random.Range(0f, 90f), Random.Range(-360f, 360f), true, 18);
            }
        }

        void Add(string art, Vector2 at, Vector2 vel, float s0, float s1, float life, Color c, float a0,
            float rot, float spin, bool add, int order, float delay = 0f)
        {
            Bit b = null;
            for (int i = 0; i < _bits.Count; i++)
                if (!_bits[i].On) { b = _bits[i]; break; }
            if (b == null)
            {
                var go = new GameObject("bit");
                go.transform.SetParent(transform, false);
                b = new Bit { Sr = go.AddComponent<SpriteRenderer>() };
                _bits.Add(b);
            }
            b.Sr.sprite = art == "disc" ? InkFx.SoftDisc() : Art(art);
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
            b.C = c;
            b.On = true;
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
                b.Vel *= Mathf.Max(0f, 1f - 3.5f * dt);
                b.Pos += b.Vel * dt;
                b.Sr.enabled = true;
                b.Sr.transform.position = new Vector3(b.Pos.x, b.Pos.y, 0f);
                b.Sr.transform.localScale = Vector3.one * Mathf.Lerp(b.S0, b.S1, Ease.OutCubic(k));
                if (b.Spin != 0f) b.Sr.transform.Rotate(0f, 0f, b.Spin * dt);
                Color c = b.C;
                c.a = b.A0 * (k < 0.25f ? 1f : 1f - (k - 0.25f) / 0.75f);
                b.Sr.color = c;
            }
        }
    }
}
