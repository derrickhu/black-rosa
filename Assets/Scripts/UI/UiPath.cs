using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 章节路线。点是本地坐标，拐弯处补一颗圆，避免折角露缝。
    public sealed class UiPath : MaskableGraphic
    {
        public float Thickness = 14f;
        Vector2[] _pts = System.Array.Empty<Vector2>();

        public void SetPoints(List<Vector2> pts)
        {
            _pts = pts == null || pts.Count == 0 ? System.Array.Empty<Vector2>() : pts.ToArray();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_pts.Length < 2) return;
            Color32 col = color;
            float r = Thickness * 0.5f;
            for (int i = 0; i < _pts.Length - 1; i++)
                AddSegment(vh, _pts[i], _pts[i + 1], r, col);
            for (int i = 0; i < _pts.Length; i++)
                AddDisc(vh, _pts[i], r, col);
        }

        static void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float r, Color32 col)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.5f) return;
            d /= len;
            Vector2 n = new Vector2(-d.y, d.x) * r;
            int i = vh.currentVertCount;
            vh.AddVert(a + n, col, Vector2.zero);
            vh.AddVert(b + n, col, Vector2.zero);
            vh.AddVert(b - n, col, Vector2.zero);
            vh.AddVert(a - n, col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        static void AddDisc(VertexHelper vh, Vector2 c, float r, Color32 col)
        {
            const int Seg = 10;
            int center = vh.currentVertCount;
            vh.AddVert(c, col, Vector2.zero);
            for (int i = 0; i <= Seg; i++)
            {
                float a = i * Mathf.PI * 2f / Seg;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col, Vector2.zero);
            }
            for (int i = 0; i < Seg; i++)
                vh.AddTriangle(center, center + 1 + i, center + 2 + i);
        }
    }
}
