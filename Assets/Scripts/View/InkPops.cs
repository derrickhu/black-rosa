using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public static class InkPops
    {
        const int Pool = 28;
        static readonly List<TextMesh> _pool = new List<TextMesh>();
        static Transform _root;

        public static void BindRoot(Transform root)
        {
            _root = root;
            _pool.Clear();
        }

        public static void Sync(List<FloatText> floats)
        {
            if (floats == null || _root == null) return;
            while (_pool.Count < floats.Count && _pool.Count < Pool)
                _pool.Add(Make());
            for (int i = 0; i < _pool.Count; i++)
            {
                TextMesh tm = _pool[i];
                if (tm == null) continue;
                bool on = i < floats.Count;
                tm.gameObject.SetActive(on);
                if (!on) continue;
                Paint(tm, floats[i]);
            }
        }

        static void Paint(TextMesh tm, FloatText f)
        {
            float u = f.MaxLife > 0.01f ? Mathf.Clamp01(f.Life / f.MaxLife) : 0f;
            float age = 1f - u;
            float fade = u > 0.32f ? 1f : Mathf.Clamp01(u / 0.32f);
            float punch = 1f + 0.38f * Mathf.Exp(-age * 9f);
            bool tinted = f.Color.r + f.Color.g + f.Color.b > 0.55f;
            Color fill = tinted ? f.Color : InkTheme.PaperInner;
            fill.a = fade;
            tm.text = f.Text;
            tm.color = fill;
            tm.transform.position = new Vector3(f.Pos.x, f.Pos.y, 0f);
            tm.transform.localScale = Vector3.one * (f.Scale * punch);
            if (tm.transform.childCount == 0) return;
            var shadow = tm.transform.GetChild(0).GetComponent<TextMesh>();
            if (shadow == null) return;
            shadow.text = f.Text;
            Color ink = InkTheme.Ink;
            ink.a = fade * 0.92f;
            shadow.color = ink;
        }

        static TextMesh Make()
        {
            var go = new GameObject("pop");
            go.transform.SetParent(_root, false);
            var tm = Stamp(go, 20);
            var shadow = new GameObject("ink");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0.03f, -0.035f, 0.01f);
            Stamp(shadow, 19);
            return tm;
        }

        static TextMesh Stamp(GameObject go, int order)
        {
            var tm = go.AddComponent<TextMesh>();
            tm.font = UiKit.FontBold;
            tm.fontSize = 72;
            tm.characterSize = 0.052f;
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
