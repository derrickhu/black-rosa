using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public static class InkPops
    {
        const int Pool = 40;
        // 按 FloatText.Id 认人。原来是按下标铺，前面一个消失后面全体往前挪一格，
        // 一串数字会整体跳位 —— 割草关一屏十几个飘字，跳起来像在闪。
        static readonly Dictionary<int, TextMesh> _live = new Dictionary<int, TextMesh>();
        static readonly List<TextMesh> _free = new List<TextMesh>();
        static readonly List<int> _drop = new List<int>();
        static readonly HashSet<int> _seen = new HashSet<int>();
        static Transform _root;
        static int _made;

        public static void BindRoot(Transform root)
        {
            _root = root;
            _live.Clear();
            _free.Clear();
            _made = 0;
        }

        public static void Sync(List<FloatText> floats)
        {
            if (floats == null || _root == null) return;

            _seen.Clear();
            for (int i = 0; i < floats.Count; i++) _seen.Add(floats[i].Id);
            _drop.Clear();
            foreach (var kv in _live)
                if (!_seen.Contains(kv.Key)) _drop.Add(kv.Key);
            for (int i = 0; i < _drop.Count; i++)
            {
                TextMesh tm = _live[_drop[i]];
                _live.Remove(_drop[i]);
                if (tm == null) continue;
                tm.gameObject.SetActive(false);
                _free.Add(tm);
            }

            for (int i = 0; i < floats.Count; i++)
            {
                FloatText f = floats[i];
                if (!_live.TryGetValue(f.Id, out TextMesh tm))
                {
                    tm = Rent();
                    if (tm == null) continue;
                    _live[f.Id] = tm;
                }
                Paint(tm, f);
            }
        }

        static TextMesh Rent()
        {
            while (_free.Count > 0)
            {
                TextMesh tm = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
                if (tm == null) continue;
                tm.gameObject.SetActive(true);
                return tm;
            }
            if (_made >= Pool) return null;
            _made++;
            return Make();
        }

        static void Paint(TextMesh tm, FloatText f)
        {
            float u = f.MaxLife > 0.01f ? Mathf.Clamp01(f.Life / f.MaxLife) : 0f;
            // 只在最后三分之一淡出。整段都在淡的话，刚飘出来的字就已经是半透明了。
            float fade = u > 0.34f ? 1f : Mathf.Clamp01(u / 0.34f);
            // 每次并入新伤害都重新弹一次，弹的幅度比原来大 —— 那一下就是「又打中了」。
            float pop = f.Punch * f.Punch;
            float scale = Mathf.Min(2.1f, f.Scale * (1f + 0.22f * pop) * Body(f.Kind));
            // 弹出的瞬间横着撑开、竖着压扁，收回来才像被打出来的，而不是一个标签在变大。
            float sx = scale * (1f + 0.46f * pop);
            float sy = scale * (1f - 0.3f * pop);

            Color fill = Face(f);
            fill.a = fade;
            tm.text = f.Text;
            tm.color = fill;
            tm.transform.position = new Vector3(f.Pos.x, f.Pos.y, 0f);
            tm.transform.localScale = new Vector3(sx, sy, 1f);
            float lean = ((f.Id % 5) - 2) * 4f;
            if (f.Kind == PopKind.Crit) lean -= 8f;
            lean += pop * 7f * ((f.Id & 1) == 0 ? 1f : -1f);
            tm.transform.localRotation = Quaternion.Euler(0f, 0f, lean);

            Color edge = InkTheme.Outline;
            edge.a = fade;
            int n = tm.transform.childCount;
            for (int i = 0; i < n; i++)
            {
                var rim = tm.transform.GetChild(i).GetComponent<TextMesh>();
                if (rim == null) continue;
                rim.text = f.Text;
                rim.color = edge;
            }
        }

        static float Body(PopKind kind)
        {
            switch (kind)
            {
                case PopKind.Crit: return 1.3f;
                case PopKind.Word: return 1f;
                case PopKind.Heal: return 1.1f;
                default: return 1.02f;
            }
        }

        static Color Face(FloatText f)
        {
            switch (f.Kind)
            {
                // 奶油芯配酱油描边。黑字在墨怪身上会消失，白边放大一圈又把笔画吃成灰的。
                case PopKind.Damage:
                    return InkTheme.PaperInner;
                case PopKind.Crit:
                    Color c = f.Color.maxColorComponent > 0.35f ? f.Color : InkTheme.GoldHi;
                    return Color.Lerp(c, Color.white, 0.28f);
                case PopKind.Heal:
                    return InkTheme.WoodHi;
                default:
                    return f.Color.maxColorComponent > 0.35f ? f.Color : InkTheme.PaperInner;
            }
        }

        static readonly Vector2[] EdgeAt =
        {
            new Vector2(0.042f, 0f),
            new Vector2(-0.042f, 0f),
            new Vector2(0f, 0.042f),
            new Vector2(0f, -0.042f)
        };

        static TextMesh Make()
        {
            var go = new GameObject("pop");
            go.transform.SetParent(_root, false);
            var tm = Stamp(go, 20);
            for (int i = 0; i < EdgeAt.Length; i++)
            {
                var edge = new GameObject("edge");
                edge.transform.SetParent(go.transform, false);
                edge.transform.localPosition = new Vector3(EdgeAt[i].x, EdgeAt[i].y, 0.01f);
                Stamp(edge, 19);
            }
            return tm;
        }

        static TextMesh Stamp(GameObject go, int order)
        {
            var tm = go.AddComponent<TextMesh>();
            tm.font = UiKit.FontBold;
            tm.fontSize = 72;
            tm.characterSize = 0.064f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.richText = false;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (tm.font != null) mr.sharedMaterial = tm.font.material;
                mr.sortingOrder = order;
            }
            return tm;
        }
    }
}
