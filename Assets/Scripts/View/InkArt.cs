using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public enum InkShape
    {
        Circle,
        Square,
        Triangle,
        Diamond,
        Flame,
        Arc,
        Bar,
        Burst,
        Arrow,
        Cannon,
        Ring,
        Heart,
        Star,
        Coin,
        Lock
    }

    public static class InkArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Shape(InkShape shape, Color color, int size = 64)
        {
            return Heap(shape, color, size);
        }

        public static Sprite Heap(CardId id, int size = 96)
        {
            return Heap(id, 1, size);
        }

        public static Sprite Heap(CardId id, int star, int size)
        {
            Sprite art = InkSprites.Heap(id, star);
            if (art != null) return NeedsCard(id) ? OnCard(art, size) : art;
            string key = "heap:" + id + ":" + star + ":" + size;
            if (Cache.TryGetValue(key, out Sprite s)) return s;
            s = Bake(size, (px, n) => DrawHeap(px, n, id));
            Cache[key] = s;
            return NeedsCard(id) ? OnCard(s, size) : s;
        }

        public static Sprite Card(int size = 128)
        {
            string key = "card:plate:" + size;
            if (Cache.TryGetValue(key, out Sprite s)) return s;
            s = Bake(size, DrawCardPlate);
            Cache[key] = s;
            return s;
        }

        static bool NeedsCard(CardId id)
        {
            switch (id)
            {
                case CardId.Fire:
                case CardId.Ice:
                case CardId.Split:
                case CardId.Track:
                case CardId.Pierce:
                case CardId.Explode:
                case CardId.Accel:
                case CardId.Heavy:
                    return true;
                default:
                    return false;
            }
        }

        static Sprite OnCard(Sprite art, int size)
        {
            if (art == null) return Card(size);
            string key = "carded:" + art.GetInstanceID() + ":" + size;
            if (Cache.TryGetValue(key, out Sprite s)) return s;
            s = Bake(size, (px, n) =>
            {
                DrawCardPlate(px, n);
                BlitSprite(px, n, art);
            });
            Cache[key] = s;
            return s;
        }

        static void DrawCardPlate(Color[] px, int n)
        {
            Color paper = InkTheme.PaperInner;
            Color ink = InkTheme.Graphite;
            float r = n * 0.08f;
            float outer = 1.5f;
            float inner = n * 0.055f;
            float t0 = Mathf.Max(1.6f, n * 0.018f);
            float t1 = Mathf.Max(1.2f, n * 0.014f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float pxv = x + 0.5f;
                float pyv = y + 0.5f;
                float d0 = RoundRect(pxv, pyv, outer, outer, n - 1f - outer, n - 1f - outer, r);
                if (d0 > 0.6f) continue;
                float d1 = RoundRect(pxv, pyv, inner, inner, n - 1f - inner, n - 1f - inner, Mathf.Max(2f, r - 4f));
                bool stroke = Mathf.Abs(d0) < t0 || Mathf.Abs(d1) < t1;
                px[y * n + x] = stroke ? ink : paper;
            }
        }

        static float RoundRect(float px, float py, float x0, float y0, float x1, float y1, float rad)
        {
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float hw = Mathf.Max(0.01f, (x1 - x0) * 0.5f - rad);
            float hh = Mathf.Max(0.01f, (y1 - y0) * 0.5f - rad);
            float dx = Mathf.Abs(px - cx) - hw;
            float dy = Mathf.Abs(py - cy) - hh;
            float ax = Mathf.Max(dx, 0f);
            float ay = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - rad;
        }

        static void BlitSprite(Color[] dst, int n, Sprite art)
        {
            Texture2D tex = art.texture;
            if (tex == null) return;
            Rect r = art.textureRect;
            Color[] src;
            try { src = tex.GetPixels(); }
            catch (UnityException) { return; }
            int tw = tex.width, th = tex.height;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n;
                float v = (y + 0.5f) / n;
                float sx = r.x + u * r.width - 0.5f;
                float sy = r.y + v * r.height - 0.5f;
                Color over = SampleBilinear(src, tw, th, sx, sy);
                if (over.a < 0.02f) continue;
                int i = y * n + x;
                dst[i] = BlendOver(dst[i], over);
            }
        }

        static Color SampleBilinear(Color[] src, int w, int h, float x, float y)
        {
            if (x < 0f || y < 0f || x >= w - 1f || y >= h - 1f) return new Color(0, 0, 0, 0);
            int x0 = (int)x, y0 = (int)y;
            float fx = x - x0, fy = y - y0;
            Color a = src[y0 * w + x0];
            Color b = src[y0 * w + x0 + 1];
            Color c = src[(y0 + 1) * w + x0];
            Color d = src[(y0 + 1) * w + x0 + 1];
            return Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
        }

        static Color BlendOver(Color under, Color over)
        {
            float a = over.a + under.a * (1f - over.a);
            if (a < 0.001f) return new Color(0, 0, 0, 0);
            Color rgb = (over * over.a + under * under.a * (1f - over.a)) / a;
            rgb.a = a;
            return rgb;
        }

        public static Sprite Heap(InkShape shape, Color color, int size = 64)
        {
            string key = "shp:" + shape + ":" + ColorUtility.ToHtmlStringRGBA(color) + ":" + size;
            if (Cache.TryGetValue(key, out Sprite s)) return s;
            s = Bake(size, (px, n) => DrawShaped(px, n, shape, color));
            Cache[key] = s;
            return s;
        }

        public static Sprite Person(EnemyId id, Color tint, int size = 96)
        {
            Sprite art = InkSprites.Person(id);
            if (art != null) return art;
            string key = "man:" + id + ":" + ColorUtility.ToHtmlStringRGBA(tint) + ":" + size;
            if (Cache.TryGetValue(key, out Sprite s)) return s;
            s = Bake(size, (px, n) => DrawPerson(px, n, id, tint));
            Cache[key] = s;
            return s;
        }

        public static Sprite Cannon(int size = 96)
        {
            Sprite art = InkSprites.Cannon();
            if (art != null) return art;
            const string key = "cannon:v3:96";
            if (size != 96) return Bake(size, DrawCannon);
            if (Cache.TryGetValue(key, out Sprite s)) return s;
            s = Bake(96, DrawCannon);
            Cache[key] = s;
            return s;
        }

        public static Sprite Cell(int size = 128)
        {
            const string key = "cell:dash:v1";
            if (Cache.TryGetValue(key, out Sprite s)) return s;
            s = Bake(size, DrawCell);
            Cache[key] = s;
            return s;
        }

        public static Sprite Icon(InkShape shape, int size = 64)
        {
            Sprite art = InkSprites.Icon(shape);
            if (art != null) return art;
            Color fill = shape == InkShape.Heart ? Color.white : InkTheme.GraphiteMid;
            return Heap(shape, fill, size);
        }

        static Sprite Bake(int size, System.Action<Color[], int> paint)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            paint(px, size);
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static void DrawHeap(Color[] px, int n, CardId id)
        {
            switch (id)
            {
                case CardId.Fire:
                    Mound(px, n, 0f, -0.08f, 0.72f, 0.55f, InkTheme.Fire, InkTheme.FireMid, InkTheme.FireHi);
                    FlameTongue(px, n, -0.18f, 0.18f, 0.22f, InkTheme.FireMid, InkTheme.FireHi);
                    FlameTongue(px, n, 0.16f, 0.22f, 0.18f, InkTheme.Fire, InkTheme.FireHi);
                    Rock(px, n, -0.28f, -0.42f, 0.16f);
                    Rock(px, n, 0.26f, -0.40f, 0.15f);
                    break;
                case CardId.Ice:
                    Mound(px, n, 0f, -0.06f, 0.70f, 0.50f, InkTheme.Ice, InkTheme.IceMid, InkTheme.IceHi);
                    Spike(px, n, -0.16f, 0.22f, 0.14f, 0.36f, InkTheme.IceMid, InkTheme.IceHi);
                    Spike(px, n, 0.14f, 0.18f, 0.12f, 0.30f, InkTheme.Ice, InkTheme.IceHi);
                    break;
                case CardId.Split:
                    Mound(px, n, -0.22f, -0.02f, 0.42f, 0.42f, InkTheme.Graphite, InkTheme.GraphiteMid, InkTheme.GraphiteHi);
                    Mound(px, n, 0.22f, -0.02f, 0.42f, 0.42f, InkTheme.Graphite, InkTheme.GraphiteMid, InkTheme.GraphiteHi);
                    break;
                case CardId.Track:
                    ArcHeap(px, n, InkTheme.Track);
                    break;
                case CardId.Pierce:
                    Spike(px, n, 0f, 0.05f, 0.16f, 0.78f, InkTheme.GraphiteMid, InkTheme.GraphiteHi);
                    Mound(px, n, 0f, -0.38f, 0.36f, 0.22f, InkTheme.Graphite, InkTheme.GraphiteMid, InkTheme.GraphiteHi);
                    break;
                case CardId.Explode:
                    BurstHeap(px, n, InkTheme.Explode);
                    break;
                case CardId.Accel:
                    DrawShaped(px, n, InkShape.Arrow, InkTheme.GraphiteMid);
                    break;
                case CardId.Heavy:
                    Mound(px, n, 0f, -0.04f, 0.62f, 0.50f, InkTheme.Graphite, InkTheme.GraphiteMid, InkTheme.GraphiteHi);
                    break;
                default:
                    DrawCannon(px, n);
                    return;
            }
            Outline(px, n, InkTheme.Ink);
        }

        static void DrawShaped(Color[] px, int n, InkShape shape, Color color)
        {
            Color mid = Color.Lerp(color, Color.white, 0.18f);
            Color hi = Color.Lerp(color, Color.white, 0.42f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                if (!Sample(shape, p)) continue;
                float t = 0.5f - p.y * 0.35f - p.x * 0.08f;
                Color c = t > 0.62f ? hi : t > 0.38f ? mid : color;
                Put(px, n, x, y, c);
            }
            Outline(px, n, InkTheme.Ink);
        }

        static void DrawPerson(Color[] px, int n, EnemyId id, Color tint)
        {
            Color body = tint.a < 0.1f || tint == Color.black ? InkTheme.Graphite : Color.Lerp(InkTheme.Graphite, tint, 0.55f);
            Color mid = Color.Lerp(body, InkTheme.GraphiteHi, 0.35f);
            float lean = id == EnemyId.Runner ? 0.10f : 0f;
            float wide = id == EnemyId.Strafer || id == EnemyId.Boss ? 1.18f : 1f;
            float scale = id == EnemyId.Swarm ? 0.55f : id == EnemyId.Boss ? 1.15f : 1f;
            if (id == EnemyId.Swarm)
            {
                StampPerson(px, n, -0.28f, -0.08f, 0.52f, 1f, body, mid);
                StampPerson(px, n, 0.26f, -0.12f, 0.50f, 1f, body, mid);
                StampPerson(px, n, 0.00f, 0.10f, 0.48f, 1f, body, mid);
                Outline(px, n, InkTheme.Ink);
                return;
            }
            StampPerson(px, n, lean, 0f, scale, wide, body, mid);
            if (id == EnemyId.Shield || id == EnemyId.Boss)
                Disc(px, n, 0.18f * wide, -0.02f, id == EnemyId.Boss ? 0.34f : 0.26f, InkTheme.GraphiteMid, InkTheme.GraphiteHi);
            if (id == EnemyId.Elite)
            {
                Disc(px, n, lean, 0.38f * scale, 0.16f, InkTheme.Graphite, InkTheme.GraphiteMid);
                StrokeBlob(px, n, 0.12f, 0.08f, 0.18f, 0.08f, InkTheme.Track);
            }
            Outline(px, n, InkTheme.Ink);
            Eye(px, n, -0.07f + lean, 0.22f * scale);
            Eye(px, n, 0.08f + lean, 0.22f * scale);
        }

        static void StampPerson(Color[] px, int n, float ox, float oy, float sc, float wide, Color body, Color mid)
        {
            Disc(px, n, ox, oy + 0.26f * sc, 0.20f * sc, body, mid);
            Disc(px, n, ox, oy - 0.02f * sc, 0.24f * sc * wide, body, mid);
            Disc(px, n, ox - 0.12f * sc * wide, oy - 0.32f * sc, 0.10f * sc, body, mid);
            Disc(px, n, ox + 0.12f * sc * wide, oy - 0.32f * sc, 0.10f * sc, body, mid);
            Disc(px, n, ox - 0.22f * sc * wide, oy - 0.02f * sc, 0.08f * sc, body, mid);
            Disc(px, n, ox + 0.22f * sc * wide, oy - 0.02f * sc, 0.08f * sc, body, mid);
        }

        static void DrawCannon(Color[] px, int n)
        {
            Box(px, n, 0f, -0.38f, 0.46f, 0.22f, InkTheme.Ink, InkTheme.Graphite);
            Box(px, n, 0f, 0.08f, 0.22f, 0.46f, InkTheme.Graphite, InkTheme.GraphiteHi);
            Disc(px, n, 0f, 0.48f, 0.16f, InkTheme.GraphiteMid, InkTheme.GraphiteHi);
            Outline(px, n, InkTheme.Ink);
        }

        static void DrawCell(Color[] px, int n)
        {
            Color ink = new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, 0.38f);
            int thick = Mathf.Max(2, n / 42);
            int dash = Mathf.Max(5, n / 14);
            int gap = Mathf.Max(4, n / 20);
            int period = dash + gap;
            int inset = 1;
            for (int i = inset; i < n - inset; i++)
            {
                if ((i - inset) % period >= dash) continue;
                for (int t = 0; t < thick; t++)
                {
                    Put(px, n, i, inset + t, ink);
                    Put(px, n, i, n - inset - 1 - t, ink);
                    Put(px, n, inset + t, i, ink);
                    Put(px, n, n - inset - 1 - t, i, ink);
                }
            }
        }

        static bool Sample(InkShape shape, Vector2 p)
        {
            switch (shape)
            {
                case InkShape.Circle: return p.sqrMagnitude <= 1f;
                case InkShape.Ring:
                    float d = p.magnitude;
                    return d <= 1f && d >= 0.62f;
                case InkShape.Square: return Mathf.Abs(p.x) <= 0.78f && Mathf.Abs(p.y) <= 0.78f;
                case InkShape.Triangle: return p.y > -0.72f && p.y < 0.82f - 1.55f * Mathf.Abs(p.x);
                case InkShape.Diamond: return Mathf.Abs(p.x) + Mathf.Abs(p.y) <= 1f;
                case InkShape.Flame: return Mathf.Abs(p.x) < 0.38f * (1.05f - p.y) && p.y > -0.75f && p.y < 0.92f;
                case InkShape.Arc: return Mathf.Abs(p.y - 0.15f * Mathf.Sin(p.x * 2.2f)) < 0.18f && Mathf.Abs(p.x) < 0.9f;
                case InkShape.Bar: return Mathf.Abs(p.x) < 0.16f && Mathf.Abs(p.y) < 0.9f;
                case InkShape.Burst: return p.sqrMagnitude <= 0.22f || (Mathf.Abs(p.x) < 0.1f && Mathf.Abs(p.y) < 0.95f) || (Mathf.Abs(p.y) < 0.1f && Mathf.Abs(p.x) < 0.95f);
                case InkShape.Arrow: return (p.y > 0.05f && p.y < 0.85f - 1.3f * Mathf.Abs(p.x)) || (Mathf.Abs(p.x) < 0.18f && p.y > -0.85f && p.y < 0.2f);
                case InkShape.Cannon: return (Mathf.Abs(p.x) < 0.28f && p.y > -0.2f && p.y < 0.85f) || (Mathf.Abs(p.x) < 0.55f && p.y > -0.75f && p.y < -0.1f);
                case InkShape.Heart:
                    {
                        float x = p.x * 1.15f;
                        float y = p.y * 1.15f + 0.1f;
                        return x * x + (y - Mathf.Sqrt(Mathf.Abs(x))) * (y - Mathf.Sqrt(Mathf.Abs(x))) < 0.55f && y > -0.55f;
                    }
                case InkShape.Star:
                    {
                        float ang = Mathf.Atan2(p.y, p.x);
                        float r = p.magnitude;
                        float spike = 0.42f + 0.38f * Mathf.Abs(Mathf.Cos(ang * 2.5f));
                        return r < spike;
                    }
                case InkShape.Coin: return p.sqrMagnitude <= 1f && p.sqrMagnitude >= 0.16f;
                case InkShape.Lock:
                    return (Mathf.Abs(p.x) < 0.42f && p.y > -0.55f && p.y < 0.08f)
                        || (Mathf.Abs(p.magnitude - 0.28f) < 0.10f && p.y > 0.0f);
                default: return p.sqrMagnitude <= 1f;
            }
        }

        public static InkShape EnemyShape(EnemyId id)
        {
            switch (id)
            {
                case EnemyId.Runner: return InkShape.Triangle;
                case EnemyId.Shield: return InkShape.Ring;
                case EnemyId.Swarm: return InkShape.Circle;
                case EnemyId.Strafer: return InkShape.Diamond;
                case EnemyId.Elite: return InkShape.Burst;
                case EnemyId.Boss: return InkShape.Square;
                default: return InkShape.Circle;
            }
        }

        static void Mound(Color[] px, int n, float ox, float oy, float w, float h, Color dark, Color mid, Color hi)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                float u = (p.x - ox) / w;
                float v = (p.y - oy) / h;
                if (u * u + (v + 0.15f) * (v + 0.15f) * 1.15f > 1f || v < -0.85f) continue;
                Color c = v > 0.18f ? hi : v > -0.15f ? mid : dark;
                Put(px, n, x, y, c);
            }
        }

        static void FlameTongue(Color[] px, int n, float ox, float oy, float w, Color mid, Color hi)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                float u = (p.x - ox) / w;
                float v = (p.y - oy) / (w * 1.6f);
                if (Mathf.Abs(u) < 1f - v && v > -0.2f && v < 1f)
                    Put(px, n, x, y, v > 0.45f ? hi : mid);
            }
        }

        static void Spike(Color[] px, int n, float ox, float oy, float w, float h, Color mid, Color hi)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                float u = (p.x - ox) / w;
                float v = (p.y - oy) / h;
                if (Mathf.Abs(u) < 1f - (v + 1f) * 0.45f && v > -1f && v < 1f)
                    Put(px, n, x, y, v > 0.2f ? hi : mid);
            }
        }

        static void ArcHeap(Color[] px, int n, Color col)
        {
            Color mid = Color.Lerp(col, Color.white, 0.25f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                float d = Mathf.Abs(p.magnitude - 0.48f);
                if (d < 0.16f && p.y > -0.15f) Put(px, n, x, y, p.y > 0.2f ? mid : col);
            }
        }

        static void BurstHeap(Color[] px, int n, Color col)
        {
            DrawShaped(px, n, InkShape.Burst, col);
        }

        static void RingHeap(Color[] px, int n, float r)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                float d = Mathf.Abs(p.magnitude - r);
                if (d < 0.07f) Put(px, n, x, y, InkTheme.GraphiteMid);
            }
        }

        static void Rock(Color[] px, int n, float ox, float oy, float r)
        {
            Disc(px, n, ox, oy, r, InkTheme.Graphite, InkTheme.GraphiteMid);
        }

        static void Disc(Color[] px, int n, float ox, float oy, float r, Color dark, Color hi)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                float u = (p.x - ox) / r;
                float v = (p.y - oy) / r;
                if (u * u + v * v > 1f) continue;
                Put(px, n, x, y, v > 0.15f ? hi : dark);
            }
        }

        static void Box(Color[] px, int n, float ox, float oy, float w, float h, Color dark, Color hi)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                if (Mathf.Abs(p.x - ox) > w || Mathf.Abs(p.y - oy) > h) continue;
                Put(px, n, x, y, p.y > oy + h * 0.25f ? hi : dark);
            }
        }

        static void StrokeBlob(Color[] px, int n, float ox, float oy, float w, float h, Color c)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = ToP(x, y, n);
                float u = (p.x - ox) / w;
                float v = (p.y - oy) / h;
                if (u * u + v * v < 1f) Put(px, n, x, y, c);
            }
        }

        static void Eye(Color[] px, int n, float ox, float oy)
        {
            Disc(px, n, ox, oy, 0.055f, Color.white, Color.white);
        }

        static void Outline(Color[] px, int n, Color ink)
        {
            var copy = (Color[])px.Clone();
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                if (copy[y * n + x].a < 0.08f) continue;
                bool edge = false;
                for (int oy = -1; oy <= 1 && !edge; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int xx = x + ox;
                    int yy = y + oy;
                    if (xx < 0 || yy < 0 || xx >= n || yy >= n || copy[yy * n + xx].a < 0.08f) edge = true;
                }
                if (edge) px[y * n + x] = ink;
            }
        }

        static Vector2 ToP(int x, int y, int n)
        {
            return new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
        }

        static void Put(Color[] px, int n, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= n || y >= n) return;
            if (c.a < 0.02f) return;
            px[y * n + x] = c;
        }
    }
}
