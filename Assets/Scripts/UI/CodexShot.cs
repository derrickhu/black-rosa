using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴里那颗会动的炮弹。拿一份假的 ShotMods 跑 ShotLook.Resolve，
    // 和战场上 InkShot 读的是同一套五槽，只是画在 UI 上。
    public sealed class CodexShot : MonoBehaviour
    {
        const float FireH = 150f;

        struct Unit
        {
            public RectTransform Root;
            public Vector2 Home;
            public Image Body;
            public Image Halo;
            public Image OrbA;
            public Image OrbB;
            public Image MoteA;
            public Image MoteB;
        }

        ShotView _v;
        bool _flat;
        InkVfx.FlatBody _fb;
        int _star;
        Unit[] _units;
        static float _k;

        public static CodexShot Build(RectTransform parent, Vector2 size)
        {
            var rt = UiKit.Panel(parent, "shot", Vector2.zero, size, Color.clear);
            rt.GetComponent<Image>().raycastTarget = false;
            rt.gameObject.AddComponent<RectMask2D>();
            return rt.gameObject.AddComponent<CodexShot>();
        }

        public void ShowGlyph(CardId id, int star)
        {
            var m = new ShotMods();
            m.Mark(id, star);
            int copies = id == CardId.Split ? GlyphTable.Get(CardId.Split).Count.IntAt(star) : 1;
            Show(m, star, copies);
        }

        public void ShowWord(WordId word)
        {
            var m = new ShotMods { WordLook = word };
            Show(m, 1, 1);
        }

        public void ShowPair(int pair)
        {
            SignaturePair p = SignaturePairs.All[pair];
            var m = new ShotMods();
            m.Mark(p.A, 1);
            m.Mark(p.B, 1);
            Show(m, 1, 1);
        }

        void Show(ShotMods m, int star, int copies)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _star = star;
            _v = ShotLook.Resolve(m);
            _flat = InkVfx.Flat(_v.Form.Fx, out _fb);
            copies = Mathf.Clamp(copies, 1, 4);
            _units = new Unit[copies];
            float scale = copies > 2 ? 0.72f : copies > 1 ? 0.85f : 1f;
            for (int i = 0; i < copies; i++)
            {
                float side = i - (copies - 1) * 0.5f;
                _units[i] = BuildUnit(m, i, new Vector2(side * 70f * scale, -6f), -side * 13f, scale);
            }
            Update();
        }

        Unit BuildUnit(ShotMods m, int index, Vector2 pos, float angle, float scale)
        {
            var u = new Unit();
            var root = new GameObject("u" + index, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            u.Root = root.GetComponent<RectTransform>();
            u.Root.sizeDelta = Vector2.zero;
            u.Root.anchoredPosition = pos;
            u.Root.localRotation = Quaternion.Euler(0f, 0f, angle);
            u.Root.localScale = Vector3.one * scale;
            u.Home = pos;

            int heavy = m.Star(CardId.Heavy);
            float grow = heavy > 0 ? GlyphTable.Get(CardId.Heavy).Size.At(heavy) : 1f;

            // 尾槽（速 / 瞄 / 风）一串渐小的圆点，平涂体自带尾巴就不再补墨尾。
            if (_v.Trail.On) Trail(u.Root, Color.Lerp(_v.Trail.Tint, Color.white, 0.2f), _v.Trail.Tint, 8, 34f * grow);
            else if (!_flat) Trail(u.Root, Head(), Tail(), 4, 22f * grow);

            Color glow = Color.clear;
            float glowS = 110f;
            if (_v.Bloom.On)
            {
                glow = Fade(_v.Bloom.Tint, 0.45f);
                glowS = _v.Bloom.Fx == ShotFx.BloomHeavy ? 150f : 130f;
            }
            else if (!_flat && _v.Elements > 0) glow = Fade(_v.Mixed, 0.36f);
            if (glow.a > 0f) Img(u.Root, InkFx.SoftDisc(), Vector2.zero, glowS * grow, glow);

            if (_v.Halo.On)
            {
                u.Halo = Img(u.Root, InkVfx.Shot(_v.Halo.Fx) ?? InkFx.SoftRing(), Vector2.zero, 150f, Fade(_v.Halo.Tint, 0.8f));
            }

            if (_flat)
            {
                Sprite first = InkVfx.FlatFrame(_fb, _star, index);
                u.Body = Img(u.Root, first, Vector2.zero, 10f, Color.white);
                if (first != null)
                {
                    float h = Mathf.Min(first.bounds.size.y * _fb.Scale * Kilo() * grow, 210f);
                    float w = h * first.rect.width / Mathf.Max(1f, first.rect.height);
                    u.Body.rectTransform.sizeDelta = new Vector2(w, h);
                    u.Body.rectTransform.anchoredPosition = new Vector2(0f, -_fb.Sink * h + h * 0.18f);
                }
            }
            else
            {
                Color pellet = _v.Elements > 0 ? _v.Mixed : InkTheme.Ink;
                u.Body = Img(u.Root, InkArt.Heap(InkShape.Circle, Color.white, 64), Vector2.zero, 40f * grow, pellet);
                if (_v.Elements > 0) Img(u.Root, InkFx.SoftDisc(), new Vector2(-4f, 5f), 20f * grow, Fade(Color.white, 0.6f));
            }

            if (_v.Orbit.On)
            {
                u.OrbA = Img(u.Root, InkFx.SoftDisc(), Vector2.zero, 38f, Fade(_v.Orbit.Tint, 0.8f));
                u.OrbB = Img(u.Root, InkFx.SoftDisc(), Vector2.zero, 38f, Fade(_v.Orbit.Tint, 0.8f));
            }
            if (_v.Motes > 0) u.MoteA = Img(u.Root, InkFx.Mote(), Vector2.zero, 26f, _v.MoteA);
            if (_v.Motes > 1) u.MoteB = Img(u.Root, InkFx.Mote(), Vector2.zero, 26f, _v.MoteB);
            return u;
        }

        // 以火 ★1 的弹头高为基准，各元素之间的大小和战场上是同一个比例。
        static float Kilo()
        {
            if (_k > 0f) return _k;
            InkVfx.Flat(ShotFx.FormFire, out InkVfx.FlatBody fire);
            Sprite s = InkVfx.FlatFrame(fire, 1, 0);
            _k = s != null && s.bounds.size.y > 1e-4f ? FireH / (s.bounds.size.y * fire.Scale) : 100f;
            return _k;
        }

        Color Head() => _v.Elements > 0 ? Color.Lerp(_v.Mixed, Color.white, 0.38f) : InkTheme.GraphiteMid;
        Color Tail() => _v.Elements > 0 ? _v.Mixed : InkTheme.Ink;

        static void Trail(RectTransform root, Color head, Color tail, int n, float size)
        {
            for (int i = n - 1; i >= 0; i--)
            {
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                Color c = Color.Lerp(head, tail, k);
                c.a = Mathf.Lerp(0.85f, 0.08f, k);
                Img(root, InkFx.SoftDisc(), new Vector2(0f, -18f - i * size * 0.42f), size * Mathf.Lerp(1f, 0.35f, k), c);
            }
        }

        static Image Img(RectTransform parent, Sprite sprite, Vector2 pos, float size, Color color)
        {
            var img = UiKit.Icon(parent, sprite, pos, size);
            img.color = color;
            img.enabled = sprite != null;
            return img;
        }

        static Color Fade(Color c, float a)
        {
            c.a = a;
            return c;
        }

        void Update()
        {
            if (_units == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < _units.Length; i++)
            {
                Unit u = _units[i];
                if (u.Root == null) continue;
                u.Root.anchoredPosition = u.Home + new Vector2(0f, Mathf.Sin(t * 3f + i * 0.9f) * 6f);
                if (_flat && u.Body != null)
                {
                    Sprite s = InkVfx.FlatFrame(_fb, _star, i);
                    if (s != null) u.Body.sprite = s;
                }
                if (u.Halo != null) u.Halo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                float o = t * 3.4f + i;
                if (u.OrbA != null) u.OrbA.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(o) * 64f, Mathf.Sin(o) * 36f);
                if (u.OrbB != null) u.OrbB.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(o + Mathf.PI) * 64f, Mathf.Sin(o + Mathf.PI) * 36f);
                float mo = t * 2.6f + i;
                if (u.MoteA != null) u.MoteA.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(mo) * 80f, Mathf.Sin(mo) * 80f);
                if (u.MoteB != null) u.MoteB.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(mo + Mathf.PI) * 80f, Mathf.Sin(mo + Mathf.PI) * 80f);
            }
        }
    }
}
