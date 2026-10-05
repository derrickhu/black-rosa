using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 雷的连带电弧：从挨打的那只拉一道折线闪电到被传导的那只。
    // 和 hitv_thunder 同一套配色：外沿黑莓紫、身子葡萄紫、芯柠檬白，三层硬边叠出平涂感。
    // 折线每隔几帧重抖一次，电弧才是「滋滋」跳的，而不是一根死线。
    public sealed class InkArc : MonoBehaviour
    {
        const int Pool = 12;
        const int Points = 7;
        const float Life = 0.26f;
        const float Rejitter = 0.045f;

        static readonly Color Rim = InkTheme.Hex("2E1F66");
        static readonly Color Core = InkTheme.Hex("FFF6A8");

        static readonly List<InkArc> _all = new List<InkArc>();
        static Transform _root;
        static Material _mat;

        readonly Vector3[] _pts = new Vector3[Points];
        LineRenderer _rim, _body, _core;
        Vector2 _a, _b;
        Color _tint;
        float _t;
        float _shake;

        public bool Busy => enabled;

        public static void Play(ArcFx fx)
        {
            InkArc arc = Rent();
            arc._a = fx.A;
            arc._b = fx.B;
            arc._tint = fx.Tint.a > 0f ? fx.Tint : InkTheme.Thunder;
            arc._t = 0f;
            arc._shake = 0f;
            arc.Jitter();
            arc.Paint(1f);
            arc.enabled = true;
            arc.gameObject.SetActive(true);
        }

        static InkArc Rent()
        {
            if (_root == null)
            {
                _all.Clear();
                _root = new GameObject("InkArcs").transform;
            }
            for (int i = 0; i < _all.Count; i++)
                if (_all[i] != null && !_all[i].Busy) return _all[i];
            if (_all.Count >= Pool)
            {
                InkArc oldest = _all[0];
                _all.RemoveAt(0);
                _all.Add(oldest);
                return oldest;
            }
            var go = new GameObject("arc");
            go.transform.SetParent(_root, false);
            var arc = go.AddComponent<InkArc>();
            arc._rim = arc.Line("rim", 14);
            arc._body = arc.Line("body", 15);
            arc._core = arc.Line("core", 15);
            _all.Add(arc);
            return arc;
        }

        // 精灵默认材质：微信包体里一定在，Shader.Find 找的那份可能被裁掉。
        static Material Mat()
        {
            if (_mat != null) return _mat;
            var probe = new GameObject("arc_mat");
            var sr = probe.AddComponent<SpriteRenderer>();
            _mat = new Material(sr.sharedMaterial);
            Destroy(probe);
            return _mat;
        }

        LineRenderer Line(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = Points;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 2;
            lr.sortingOrder = order;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.material = Mat();
            return lr;
        }

        void Jitter()
        {
            Vector2 d = _b - _a;
            float len = d.magnitude;
            Vector2 side = len > 1e-4f ? new Vector2(-d.y, d.x) / len : Vector2.right;
            float amp = Mathf.Clamp(len * 0.14f, 0.06f, 0.2f);
            for (int i = 0; i < Points; i++)
            {
                float k = i / (float)(Points - 1);
                Vector2 p = _a + d * k;
                if (i > 0 && i < Points - 1)
                    p += side * (((i & 1) == 0 ? 1f : -1f) * amp * Random.Range(0.45f, 1f));
                _pts[i] = new Vector3(p.x, p.y, 0f);
            }
            _rim.SetPositions(_pts);
            _body.SetPositions(_pts);
            _core.SetPositions(_pts);
        }

        void Paint(float a)
        {
            float w = Mathf.Lerp(0.6f, 1f, a);
            Set(_rim, Rim, a, 0.17f * w);
            Set(_body, _tint, a, 0.11f * w);
            Set(_core, Core, a, 0.045f * w);
        }

        static void Set(LineRenderer lr, Color c, float a, float width)
        {
            c.a = a;
            lr.startColor = lr.endColor = c;
            lr.startWidth = lr.endWidth = width;
            lr.enabled = a > 0.01f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            if (_t >= Life)
            {
                enabled = false;
                _rim.enabled = _body.enabled = _core.enabled = false;
                return;
            }
            _shake += dt;
            if (_shake >= Rejitter)
            {
                _shake = 0f;
                Jitter();
            }
            // 前一半满亮，后一半收细变淡。
            float k = _t / Life;
            Paint(k < 0.5f ? 1f : 1f - (k - 0.5f) * 2f);
        }
    }
}
