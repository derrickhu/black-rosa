using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 九宫格烘图。一张小图配 border，Image.type = Sliced 之后拉到任意尺寸，
    // 圆角半径和描边宽度都保持烘好的像素值不变形。
    //
    // 这是「不再用系统矩形」的技术前提：InkArt 那套是按最终尺寸逐个烘，
    // 一个尺寸一张图，既浪费内存又没法让不同大小的卡片描边等宽。
    //
    // 卡面拆成 Fill + Line 两张叠着画，而不是把填充色和描边色一起烘死，
    // 这样任意填充色配任意描边色都只要这两张图。
    public static class UiSprites
    {
        const int Cell = 96;            // 烘图边长。border 上限 47，够放 radius 28 + blur 16
        const float Ppu = 100f;

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // 半径分三档。档位越少，运行时生成的贴图越少。
        public static int Tier(float radius)
        {
            if (radius >= 24f) return 28;
            return radius >= 16f ? 20 : 12;
        }

        public static int TierFor(Vector2 size)
        {
            float m = Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y));
            return Tier(m * 0.30f);
        }

        // 圆角矩形有符号距离场。内部为负，外部为正，正好用来做 1px 抗锯齿。
        static float Sd(float x, float y, float rad, float inset)
        {
            float half = Cell * 0.5f - inset;
            float ext = half - rad;
            float dx = Mathf.Abs(x - Cell * 0.5f) - ext;
            float dy = Mathf.Abs(y - Cell * 0.5f) - ext;
            float ax = Mathf.Max(dx, 0f);
            float ay = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - rad;
        }

        static Sprite Bake(string key, int border, System.Func<float, float, float> alpha)
        {
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;
            var tex = new Texture2D(Cell, Cell, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "ui_" + key
            };
            var px = new Color32[Cell * Cell];
            for (int y = 0; y < Cell; y++)
            for (int x = 0; x < Cell; x++)
            {
                float a = Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f));
                px[y * Cell + x] = new Color32(255, 255, 255, (byte)(a * 255f + 0.5f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            int b = Mathf.Clamp(border, 2, Cell / 2 - 1);
            s = Sprite.Create(tex, new Rect(0, 0, Cell, Cell), new Vector2(0.5f, 0.5f), Ppu,
                0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = key;
            Cache[key] = s;
            return s;
        }

        // 圆角实底。白色，靠 Image.color 上任意填充色。
        public static Sprite Fill(int radius)
        {
            radius = Tier(radius);
            return Bake("fill" + radius, radius + 1,
                (x, y) => 0.5f - Sd(x, y, radius, 0.5f));
        }

        // 只有描边的环，画在 Fill 上面。outline 是烘死的像素宽度，拉伸不变。
        public static Sprite Line(int radius, int outline)
        {
            radius = Tier(radius);
            outline = Mathf.Clamp(outline, 2, 10);
            return Bake("line" + radius + "_" + outline, radius + outline + 1, (x, y) =>
            {
                float d = Sd(x, y, radius, 0.5f);
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + outline));
                return outer - inner;
            });
        }

        // 柔和投影。UI 有了纵深才不像线框图。
        public static Sprite Shadow(int radius, int blur)
        {
            radius = Tier(radius);
            blur = Mathf.Clamp(blur, 4, 16);
            return Bake("sh" + radius + "_" + blur, radius + blur + 1, (x, y) =>
            {
                float d = Sd(x, y, radius, blur + 0.5f);
                float t = Mathf.Clamp01((d + blur) / (2f * blur));
                return 1f - t * t * (3f - 2f * t);
            });
        }

        // 竖向渐变背景。4px 宽就够，横向拉伸不会有可见的带状。
        public static Sprite Gradient(Color top, Color bottom)
        {
            string key = "grad" + ColorUtility.ToHtmlStringRGB(top) + ColorUtility.ToHtmlStringRGB(bottom);
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;
            const int h = 256, w = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "ui_" + key
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (h - 1f));
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu);
            s.name = key;
            Cache[key] = s;
            return s;
        }

        // 编辑器域重载会把贴图引用作废，留着空壳会画成洋红。
        public static void Clear()
        {
            Cache.Clear();
        }
    }
}
