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
            float punch = 1f + 0.55f * f.Punch * f.Punch;
            float scale = f.Scale * punch * Body(f.Kind);

            Color fill = Face(f);
            fill.a = fade;
            tm.text = f.Text;
            tm.color = fill;
            tm.transform.position = new Vector3(f.Pos.x, f.Pos.y, 0f);
            tm.transform.localScale = Vector3.one * scale;
            // 暴击歪一点。整屏数字都正着写，大数字混在里面根本挑不出来。
            tm.transform.localRotation = f.Kind == PopKind.Crit
                ? Quaternion.Euler(0f, 0f, -8f)
                : Quaternion.identity;

            if (tm.transform.childCount == 0) return;
            var halo = tm.transform.GetChild(0).GetComponent<TextMesh>();
            if (halo == null) return;
            halo.text = f.Text;
            Color paper = InkTheme.PaperInner;
            paper.a = fade;
            halo.color = paper;
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
                // 普通伤害就是墨色。底子是宣纸，深字最好读，也让暴击的彩色真成重点 ——
                // 满屏彩色数字等于没有重点。
                case PopKind.Damage:
                    return InkTheme.Ink;
                case PopKind.Crit:
                    return f.Color.maxColorComponent > 0.3f ? f.Color : InkTheme.Word;
                default:
                    return f.Color.maxColorComponent > 0.3f ? f.Color : InkTheme.Ink;
            }
        }

        static TextMesh Make()
        {
            var go = new GameObject("pop");
            go.transform.SetParent(_root, false);
            var tm = Stamp(go, 20);
            // 描边做法：同一串字放大一圈、纸色、垫在后面。单向投影只有一边有对比，
            // 数字飘到深色墨迹或敌人身上时另外三边就化进背景了。
            var halo = new GameObject("halo");
            halo.transform.SetParent(go.transform, false);
            halo.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            // 1.16 试过了，太粗：字的笔画本来就细，纸色放大一圈直接把深色的芯吃掉，
            // 远看整个数字是灰的。1.09 刚好只在外沿留一道边。
            halo.transform.localScale = Vector3.one * 1.09f;
            Stamp(halo, 19);
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
