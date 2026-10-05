using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴里那颗会动的炮弹。拿一份假的 ShotMods 跑 ShotLook.Resolve，
    // 和战场上 InkShot 读的是同一套五槽，只是画在 UI 上。
    // 粒子和绕身小件也读 ShotTrail 同一份配方：弹是停着的，所以粒子自带一段往下的漂移。
    public sealed class CodexShot : MonoBehaviour, ISparkSink
    {
        const float FireH = 150f;
        // 1 世界单位折成多少 UI 像素，只给粒子速度 / 大小用。
        const float Px = 150f;
        // 弹体本地单位折成像素，给绕身小件用（和 InkShot 子节点同一套单位）。
        const float OrnPx = 108f;
        // 战场上弹速 7.2，图鉴里只让粒子相对弹体往下漂这么快，不然一眨眼就飞出框。
        const float Drift = 3f;
        const float Travel = 7.2f;
        const int PoolSize = 72;

        struct Unit
        {
            public RectTransform Root;
            public Vector2 Home;
            public Image Body;
            public Image MoteA;
            public Image MoteB;
            public Image[] Orn;
            public Image Flare;
            public Image Glint;
        }

        struct Spark
        {
            public Image Img;
            public bool Alive;
            public SparkKind Kind;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Age;
            public float Life;
            public float Size;
            public float Rot;
            public float Spin;
            public float Drag;
            public float Grav;
            public Color A;
            public Color B;
            public bool Face;
        }

        ShotView _v;
        ShotMods _m;
        bool _flat;
        InkVfx.FlatBody _fb;
        int _star;
        Unit[] _units;
        static float _k;

        RectTransform _sparkRoot;
        readonly Spark[] _sparks = new Spark[PoolSize];
        readonly ShotTrail _trail = new ShotTrail();
        readonly Ornament[] _orn = new Ornament[ShotTrail.MaxOrnaments];
        int _ornSets;
        int _n;

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
            for (int i = 0; i < _sparks.Length; i++) _sparks[i] = default;
            _m = m;
            _star = star;
            _v = ShotLook.Resolve(m);
            _flat = InkVfx.Flat(_v.Form.Fx, out _fb);
            _ornSets = ShotTrail.Ornaments(m, _orn);
            _trail.Reset();

            var sr = new GameObject("sparks", typeof(RectTransform));
            sr.transform.SetParent(transform, false);
            _sparkRoot = sr.GetComponent<RectTransform>();
            _sparkRoot.sizeDelta = Vector2.zero;

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
            bool heavyTrail = heavy > 0 && _v.Elements == 0 && !_v.Form.On && !_v.Trail.On;
            float trailMul = heavyTrail ? GlyphTable.Get(CardId.Heavy).Size.At(heavy) : 1f;
            bool bright = ShotSparks.Bright;
            Color tone = ShotTrail.Tone(m, _v);
            ShotAura aura = ShotTrail.Aura(m, _v);
            Material add = bright ? InkFx.AddMat() : null;

            // 尾槽（速 / 瞄）一串渐小的圆点，平涂体自带尾巴就不再补墨尾。
            if (_v.Trail.On) Trail(u.Root, Color.Lerp(_v.Trail.Tint, Color.white, 0.2f), _v.Trail.Tint, 8, 34f);
            else if (!_flat)
            {
                Color head = heavyTrail ? Color.Lerp(ShotTrail.GlowOf(CardId.Heavy), Color.white, 0.28f) : Head();
                Color tail = heavyTrail ? ShotTrail.GlowOf(CardId.Heavy) : Tail();
                Trail(u.Root, head, tail, heavyTrail ? 7 : 4, 22f * trailMul);
            }

            if (!bright)
            {
                Color glow = Color.clear;
                float glowS = 110f;
                if (_v.Bloom.On)
                {
                    glow = Fade(_v.Bloom.Tint, 0.45f);
                    glowS = 130f;
                }
                else if (!_flat && _v.Elements > 0) glow = Fade(_v.Mixed, 0.36f);
                if (glow.a > 0f) Img(u.Root, InkFx.SoftDisc(), Vector2.zero, glowS, glow);
            }
            else if (aura.Glow.a > 0f)
            {
                u.Flare = Img(u.Root, InkFx.SoftDisc(), Vector2.zero, 90f * aura.Flare, aura.Glow);
                u.Flare.material = add;
            }

            // 小件先按「全在后面」建一遍，再建弹体，再按「全在前面」建一遍；
            // Update 里只开当下该亮的那一份，就不用每帧调层级。
            int total = 0;
            for (int s = 0; s < _ornSets; s++) total += _orn[s].Count;
            u.Orn = new Image[total * 2];
            BuildOrn(u, 0, bright);

            if (_flat)
            {
                Sprite first = InkVfx.FlatFrame(_fb, _star, index);
                u.Body = Img(u.Root, first, Vector2.zero, 10f, Color.white);
                if (first != null)
                {
                    float h = Mathf.Min(first.bounds.size.y * _fb.Scale * Kilo(), 210f);
                    float w = h * first.rect.width / Mathf.Max(1f, first.rect.height);
                    u.Body.rectTransform.sizeDelta = new Vector2(w, h);
                    u.Body.rectTransform.anchoredPosition = new Vector2(0f, -_fb.Sink * h + h * 0.18f);
                }
            }
            else
            {
                Color pellet = _v.Elements > 0 ? _v.Mixed
                    : (bright && tone.a > 0f ? Color.Lerp(tone, Color.white, 0.15f) : InkTheme.Ink);
                u.Body = Img(u.Root, InkArt.Heap(InkShape.Circle, Color.white, 64), Vector2.zero, 40f, pellet);
                if (_v.Elements > 0) Img(u.Root, InkFx.SoftDisc(), new Vector2(-4f, 5f), 20f, Fade(Color.white, 0.6f));
            }

            BuildOrn(u, total, bright);

            if (!bright)
            {
                if (_v.Motes > 0) u.MoteA = Img(u.Root, InkFx.Mote(), Vector2.zero, 26f, _v.MoteA);
                if (_v.Motes > 1) u.MoteB = Img(u.Root, InkFx.Mote(), Vector2.zero, 26f, _v.MoteB);
            }

            if (bright && aura.Glint.a > 0f)
            {
                u.Glint = Img(u.Root, ShotSparks.SpriteOf(SparkKind.Spark), Vector2.zero, 108f * aura.GlintScale, aura.Glint);
                u.Glint.material = add;
            }
            return u;
        }

        void BuildOrn(Unit u, int offset, bool bright)
        {
            int k = offset;
            for (int s = 0; s < _ornSets; s++)
            {
                Ornament o = _orn[s];
                for (int i = 0; i < o.Count; i++)
                {
                    Image img = Img(u.Root, ShotSparks.SpriteOf(o.Kind), Vector2.zero, 10f, o.Tint);
                    img.material = bright && o.Glow ? InkFx.AddMat() : null;
                    u.Orn[k++] = img;
                }
            }
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
                    if (_v.Form.Fx == ShotFx.FormWind)
                        u.Body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -t * 540f + i * 37f);
                }
                float mo = t * 2.6f + i;
                if (u.MoteA != null) u.MoteA.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(mo) * 80f, Mathf.Sin(mo) * 80f);
                if (u.MoteB != null) u.MoteB.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(mo + Mathf.PI) * 80f, Mathf.Sin(mo + Mathf.PI) * 80f);
                PlaceOrn(u, t, i);
                if (u.Glint != null) u.Glint.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 180f);
                if (u.Flare != null) u.Flare.rectTransform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t * 14f));
            }
            Emit(Time.unscaledDeltaTime);
            TickSparks(Time.unscaledDeltaTime);
        }

        void PlaceOrn(Unit u, float t, int seed)
        {
            if (u.Orn == null || u.Orn.Length == 0) return;
            int total = u.Orn.Length / 2;
            int k = 0;
            for (int s = 0; s < _ornSets; s++)
            {
                Ornament o = _orn[s];
                for (int i = 0; i < o.Count; i++, k++)
                {
                    bool on = ShotTrail.Place(o, i, t, seed, out Vector2 p, out float rot, out float sc, out bool behind);
                    Image back = u.Orn[k], front = u.Orn[total + k];
                    back.enabled = on && behind;
                    front.enabled = on && !behind;
                    Image img = behind ? back : front;
                    var rt = img.rectTransform;
                    rt.anchoredPosition = p * OrnPx;
                    rt.localRotation = Quaternion.Euler(0f, 0f, rot);
                    rt.sizeDelta = Vector2.one * sc * OrnPx;
                }
            }
        }

        // ---- 图鉴粒子：ShotTrail 以为自己在战场上，这里把世界单位换成像素 ----

        void Emit(float dt)
        {
            if (dt <= 0f || dt > 0.2f) return;
            Unit u = _units[_n++ % _units.Length];
            if (u.Root == null) return;
            Vector2 dir = u.Root.localRotation * Vector3.up;
            _trail.Step(_m, u.Root.anchoredPosition / Px, dir, Travel * dt, this);
        }

        public void Emit(in SparkSpec p)
        {
            for (int i = 0; i < _sparks.Length; i++)
            {
                if (_sparks[i].Alive) continue;
                ref Spark s = ref _sparks[i];
                if (s.Img == null)
                {
                    s.Img = UiKit.Icon(_sparkRoot, null, Vector2.zero, 1f);
                    s.Img.preserveAspect = false;
                }
                s.Img.sprite = ShotSparks.SpriteOf(p.Kind);
                s.Img.material = ShotSparks.Bright && p.Glow ? InkFx.AddMat() : null;
                s.Img.enabled = true;
                s.Alive = true;
                s.Kind = p.Kind;
                s.Pos = p.Pos * Px;
                s.Vel = (p.Vel + Vector2.down * Drift) * Px;
                s.Age = 0f;
                s.Life = Mathf.Max(0.02f, p.Life);
                s.Size = p.Size * Px;
                s.Rot = Random.Range(0f, 360f);
                s.Spin = p.Spin;
                s.Drag = p.Drag;
                s.Grav = p.Grav * Px;
                s.A = p.A;
                s.B = p.B;
                s.Face = p.Face;
                return;
            }
        }

        void TickSparks(float dt)
        {
            if (dt <= 0f) return;
            for (int i = 0; i < _sparks.Length; i++)
            {
                ref Spark s = ref _sparks[i];
                if (!s.Alive) continue;
                s.Age += dt;
                float t = s.Age / s.Life;
                if (t >= 1f)
                {
                    s.Alive = false;
                    s.Img.enabled = false;
                    continue;
                }
                s.Pos += s.Vel * dt;
                if (s.Drag > 0f) s.Vel *= Mathf.Max(0f, 1f - s.Drag * dt);
                s.Vel.y -= s.Grav * dt;
                s.Rot += s.Spin * dt;
                var rt = s.Img.rectTransform;
                rt.anchoredPosition = s.Pos;
                float rot = s.Face && s.Vel.sqrMagnitude > 1e-4f
                    ? Mathf.Atan2(s.Vel.y, s.Vel.x) * Mathf.Rad2Deg - 90f
                    : s.Rot;
                rt.localRotation = Quaternion.Euler(0f, 0f, rot);
                rt.sizeDelta = Vector2.one * s.Size * ShotSparks.Curve(s.Kind, t);
                Color c = Color.Lerp(s.A, s.B, t);
                c.a *= 1f - t * t;
                s.Img.color = c;
            }
        }
    }
}
