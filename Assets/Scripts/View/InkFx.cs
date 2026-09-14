using UnityEngine;

namespace InkLine
{
    public static class InkFx
    {
        static Material _add;
        static Material _sprite;
        static Material _soft;
        static Texture2D _streak;
        static Sprite _dot;
        static Sprite _disc;
        static Sprite _ring;
        static Sprite _blade;
        static Sprite _pill;
        static Sprite _splat;

        public static Material AddMat()
        {
            if (_add != null) return _add;
            Shader sh = Shader.Find("InkLine/Add");
            if (sh == null) sh = Shader.Find("Particles/Additive");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return SpriteMat();
            _add = new Material(sh);
            return _add;
        }

        public static Material SpriteMat()
        {
            if (_sprite != null) return _sprite;
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) return null;
            _sprite = new Material(sh);
            return _sprite;
        }

        public static void PaintAdd(SpriteRenderer sr, Color color)
        {
            if (sr == null) return;
            Material mat = AddMat();
            if (mat != null) sr.sharedMaterial = mat;
            sr.color = color;
        }

        public static void PaintSprite(SpriteRenderer sr, Color color)
        {
            if (sr == null) return;
            sr.sharedMaterial = SpriteMat();
            sr.color = color;
        }

        public static Material SoftMat()
        {
            if (_soft != null) return _soft;
            Shader sh = Shader.Find("InkLine/Soft");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return SpriteMat();
            _soft = new Material(sh);
            return _soft;
        }

        public static void PaintSoft(SpriteRenderer sr, Color color)
        {
            if (sr == null) return;
            Material mat = SoftMat();
            if (mat != null) sr.sharedMaterial = mat;
            sr.color = color;
        }

        public static Texture2D StreakTex()
        {
            if (_streak != null) return _streak;
            const int w = 64, h = 16;
            _streak = new Texture2D(w, h, TextureFormat.RGBA32, false);
            _streak.wrapMode = TextureWrapMode.Clamp;
            _streak.filterMode = FilterMode.Bilinear;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float along = 1f - x / (w - 1f);
                float across = Mathf.Exp(-((y - (h - 1) * 0.5f) / 3.4f) * ((y - (h - 1) * 0.5f) / 3.4f));
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * Mathf.Pow(along, 1.15f) * across), 0, 255);
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
            _streak.SetPixels32(px);
            _streak.Apply(false, false);
            return _streak;
        }

        public static Sprite SoftDisc()
        {
            if (_disc != null) return _disc;
            _disc = BakeRadial(96, (r, _) => Mathf.Pow(Mathf.Clamp01(1f - r), 2.15f));
            return _disc;
        }

        public static Sprite SoftRing()
        {
            if (_ring != null) return _ring;
            _ring = BakeRadial(96, (r, _) =>
            {
                float band = (r - 0.58f) / 0.10f;
                return Mathf.Exp(-band * band) * Mathf.SmoothStep(1.02f, 0.78f, r);
            });
            return _ring;
        }

        public static Sprite SoftBlade()
        {
            if (_blade != null) return _blade;
            const int w = 48, h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f;
                float v = (y + 0.5f) / h;
                float width = 0.36f * Mathf.Pow(Mathf.Clamp01(1f - v), 0.62f) + 0.045f;
                float across = 1f - Mathf.Abs(u) / Mathf.Max(0.02f, width);
                float a = Mathf.Pow(Mathf.Clamp01(across), 1.35f);
                a *= Mathf.SmoothStep(0f, 0.10f, v) * Mathf.SmoothStep(1f, 0.80f, v);
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _blade = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.18f), h);
            return _blade;
        }

        // Pill() 在 localScale 为 1 时的世界高度。贴图是 64x16、PPU 取 64，
        // 所以宽正好 1 个单位而高只有四分之一 —— 要多高得先除掉这个数。
        public const float PillH = 0.25f;

        // 血条用的圆角横条。轴心压在左端 —— 这样「按比例缩 x」就是从左往右填，
        // 不用再为对齐额外算一次位移。1 个世界单位 = 一整条。
        public static Sprite Pill()
        {
            if (_pill != null) return _pill;
            const int w = 64, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            float r = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dy = y - r;
                float dx = Mathf.Max(0f, Mathf.Max(r - x, x - (w - 1 - r)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r + 0.5f - d));
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _pill = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w);
            return _pill;
        }

        // 地上那摊墨。不是个圆 —— 圆看着像阴影，要有几片不规则的瓣和边缘的碎点，
        // 才读得出是「流出来的」。压扁交给缩放，这里只管形状。
        public static Sprite Splat()
        {
            if (_splat != null) return _splat;
            _splat = BakeRadial(128, (r, p) =>
            {
                float ang = Mathf.Atan2(p.y, p.x);
                float edge = 0.66f
                             + 0.15f * Mathf.Sin(ang * 3f + 0.7f)
                             + 0.09f * Mathf.Sin(ang * 5f - 1.9f)
                             + 0.05f * Mathf.Sin(ang * 9f + 2.6f);
                float body = Mathf.SmoothStep(edge, edge - 0.14f, r);
                // 主体外面甩几滴。角度上只在几个窄窗口里有，半径上锁在外圈一环 ——
                // 不锁半径的话这几滴会从中心一路连到边上，变成放射状的刺。
                float band = (r - 0.86f) / 0.075f;
                float fleck = Mathf.SmoothStep(0.04f, 0.16f, Mathf.Sin(ang * 7f + 1.2f) - 0.74f)
                              * Mathf.Exp(-band * band);
                return Mathf.Max(body, fleck);
            });
            return _splat;
        }

        static Sprite BakeRadial(int n, System.Func<float, Vector2, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            float mid = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x - mid) / mid, (y - mid) / mid);
                float a = Mathf.Clamp01(alpha(p.magnitude, p));
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        public static Sprite Dot()
        {
            if (_dot != null) return _dot;
            _dot = InkArt.Heap(InkShape.Circle, Color.white, 16);
            return _dot;
        }

    }

    public sealed class InkPulse : MonoBehaviour
    {
        public float Life = 0.2f;
        float _t;
        float _a0 = 1f;
        Vector3 _s0;
        SpriteRenderer _sr;

        public void Kick(float life)
        {
            Life = life;
            _t = 0f;
            _s0 = transform.localScale;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _a0 = _sr != null ? _sr.color.a : 1f;
            enabled = true;
            if (_sr != null) _sr.enabled = true;
        }

        void LateUpdate()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _t += Time.unscaledDeltaTime;
            float u = Life <= 0.01f ? 1f : Mathf.Clamp01(_t / Life);
            transform.localScale = _s0 * (1f + 0.85f * u);
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = _a0 * (1f - u);
                _sr.color = c;
                if (u >= 1f) _sr.enabled = false;
            }
            if (u >= 1f) enabled = false;
        }
    }

    public sealed class InkMotes : MonoBehaviour
    {
        const int N = 8;
        SpriteRenderer[] _rs;
        Vector3[] _vel;
        float[] _life;
        float _acc;

        public void Quiet()
        {
            if (_rs == null) return;
            for (int i = 0; i < N; i++)
            {
                _life[i] = 0f;
                if (_rs[i] != null) _rs[i].enabled = false;
            }
        }

        public void Emit(bool fire, bool ice)
        {
            Ensure();
            _acc += Time.unscaledDeltaTime;
            if (_acc < 0.075f) return;
            _acc = 0f;
            Color c = fire && ice ? Color.white : fire ? InkTheme.FireHi : InkTheme.IceHi;
            for (int i = 0; i < N; i++)
            {
                if (_life[i] > 0f) continue;
                _life[i] = 0.2f;
                _rs[i].enabled = true;
                _rs[i].color = c;
                _rs[i].transform.localPosition = new Vector3(Random.Range(-0.05f, 0.05f), -0.1f, 0f);
                _vel[i] = new Vector3(Random.Range(-0.35f, 0.35f), -0.85f, 0f);
                break;
            }
        }

        void LateUpdate()
        {
            if (_rs == null) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < N; i++)
            {
                if (_life[i] <= 0f) continue;
                _life[i] -= dt;
                _rs[i].transform.localPosition += _vel[i] * dt;
                float a = Mathf.Clamp01(_life[i] / 0.2f);
                Color c = _rs[i].color;
                c.a = a;
                _rs[i].color = c;
                _rs[i].transform.localScale = Vector3.one * (0.11f + 0.04f * a);
                if (_life[i] <= 0f) _rs[i].enabled = false;
            }
        }

        void Ensure()
        {
            if (_rs != null) return;
            _rs = new SpriteRenderer[N];
            _vel = new Vector3[N];
            _life = new float[N];
            Sprite dot = InkFx.Dot();
            for (int i = 0; i < N; i++)
            {
                var go = new GameObject("mote");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dot;
                sr.sortingOrder = 7;
                sr.enabled = false;
                InkFx.PaintSprite(sr, Color.white);
                _rs[i] = sr;
            }
        }
    }

    public sealed class InkBreath : MonoBehaviour
    {
        public float BaseA = 0.4f;
        public float Amp = 0.16f;
        public float Hertz = 5.2f;
        SpriteRenderer _sr;
        Vector3 _s0;
        bool _got;

        public void Kick(float baseA, float amp, float hertz)
        {
            BaseA = baseA;
            Amp = amp;
            Hertz = hertz;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (!_got)
            {
                _s0 = transform.localScale;
                _got = true;
            }
            enabled = true;
        }

        void LateUpdate()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            float w = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Hertz);
            transform.localScale = _s0 * (0.90f + 0.18f * w);
            if (_sr == null) return;
            Color c = _sr.color;
            c.a = BaseA * (0.52f + 0.48f * w);
            _sr.color = c;
        }
    }

    public sealed class InkBurst : MonoBehaviour
    {
        const int SlashN = 4;
        const int DotN = 2;
        SpriteRenderer _flash;
        SpriteRenderer _glow;
        SpriteRenderer _ring;
        SpriteRenderer _ink;
        SpriteRenderer _elem;
        SpriteRenderer _event;
        SpriteRenderer[] _slash;
        SpriteRenderer[] _dot;
        Sprite[] _inkFrames;
        Sprite[] _elemFrames;
        Sprite _eventSprite;
        Color _eventColor = Color.white;
        Color _dotColor0;
        Color _dotColor1;
        int _dots;
        bool _elemTinted;
        float _t;
        float _life = 0.16f;
        float _scale = 1f;
        Color _hi = Color.white;
        Color _mid = Color.white;
        Color _ringC = Color.white;
        int _slashN;
        bool _spin;
        bool _on;

        public bool Busy => _on;

        public void Play(FxBurst fx)
        {
            Ensure();
            transform.position = new Vector3(fx.Pos.x, fx.Pos.y, 0f);
            _t = 0f;
            _scale = Mathf.Max(0.4f, fx.Scale > 0.01f ? fx.Scale : 1.2f);
            _spin = false;
            _slashN = 4;
            Tune(fx.Kind, fx.Tint);

            // 底层：墨溅，永远在，负责「打到了」的手感
            _inkFrames = InkVfx.Frames("hit_ink");
            // 元素层：只有一个，没专属图就拿白墨溅染成元素色
            string key = HitFx.Frames(fx.Kind);
            _elemFrames = InkVfx.Frames(key);
            _elemTinted = key == "hit_ink";
            // 事件层：斩杀 / 晕 / 穿，压在最上面
            string art = HitEvent.Art(fx.Event);
            _eventSprite = art != null ? InkSprites.Load(art) : null;
            _eventColor = fx.Event == HitEvent.Kill ? InkTheme.Heart : InkTheme.Word;
            if (_event != null) _event.sprite = _eventSprite;

            // 元素层已经自带放射刺了，别再叠一份程序化的刀光
            if (_elemFrames != null) _slashN = 0;

            _dots = fx.Dots;
            _dotColor0 = fx.Dot0;
            _dotColor1 = fx.Dot1;

            _on = true;
            enabled = true;
            gameObject.SetActive(true);
            SetVis(true);
        }

        void Tune(int kind, Color tint)
        {
            switch (kind)
            {
                case HitFx.Fire:
                    _hi = Color.Lerp(InkTheme.FireHi, Color.white, 0.28f);
                    _mid = InkTheme.Fire;
                    _ringC = InkTheme.FireMid;
                    _life = 0.16f;
                    break;
                case HitFx.Ice:
                    _hi = Color.Lerp(InkTheme.IceHi, Color.white, 0.18f);
                    _mid = InkTheme.Ice;
                    _ringC = InkTheme.IceMid;
                    _life = 0.16f;
                    break;
                case HitFx.FireIce:
                    _hi = Color.white;
                    _mid = InkTheme.Fire;
                    _ringC = InkTheme.Ice;
                    _life = 0.18f;
                    break;
                case HitFx.Explode:
                    _hi = Color.Lerp(InkTheme.Explode, Color.white, 0.38f);
                    _mid = InkTheme.Explode;
                    _ringC = InkTheme.FireMid;
                    _life = 0.24f;
                    _scale *= 1.35f;
                    break;
                case HitFx.Heavy:
                    _hi = Color.white;
                    _mid = InkTheme.GraphiteMid;
                    _ringC = InkTheme.Graphite;
                    _life = 0.18f;
                    _scale *= 1.2f;
                    _slashN = 2;
                    break;
                case HitFx.Stun:
                    _hi = Color.Lerp(InkTheme.Word, Color.white, 0.32f);
                    _mid = InkTheme.Word;
                    _ringC = InkTheme.Word;
                    _life = 0.30f;
                    _slashN = 0;
                    _spin = true;
                    break;
                case HitFx.Kill:
                    _hi = Color.Lerp(InkTheme.Heart, Color.white, 0.36f);
                    _mid = InkTheme.Heart;
                    _ringC = InkTheme.Word;
                    _life = 0.22f;
                    break;
                case HitFx.Cleave:
                    _hi = Color.Lerp(InkTheme.Word, Color.white, 0.4f);
                    _mid = InkTheme.Word;
                    _ringC = InkTheme.Heart;
                    _life = 0.20f;
                    break;
                case HitFx.Knock:
                    _hi = Color.Lerp(InkTheme.Word, Color.white, 0.35f);
                    _mid = InkTheme.Word;
                    _ringC = InkTheme.Word;
                    _life = 0.22f;
                    _slashN = 0;
                    _spin = true;
                    break;
                case HitFx.Arrow:
                    _hi = Color.Lerp(InkTheme.Word, Color.white, 0.45f);
                    _mid = InkTheme.Word;
                    _ringC = InkTheme.FireHi;
                    _life = 0.16f;
                    _slashN = 2;
                    break;
                default:
                    _hi = Color.Lerp(InkTheme.GraphiteHi, Color.white, 0.35f);
                    _mid = InkTheme.Graphite;
                    _ringC = InkTheme.GraphiteMid;
                    _life = 0.14f;
                    _slashN = 3;
                    break;
            }
            if (tint.a > 0.4f && kind != HitFx.FireIce && kind != HitFx.Stun)
                _mid = Color.Lerp(_mid, tint, 0.35f);
        }

        void LateUpdate()
        {
            // 编辑器域重载会把私有引用清空，但池化对象还留在场上且 enabled 是 true，
            // 于是每帧撞空指针。没在播就直接关掉自己，等下次 Play 重建。
            if (!_on || _dot == null || _slash == null)
            {
                _on = false;
                enabled = false;
                return;
            }
            _t += Time.unscaledDeltaTime;
            float u = _life <= 0.01f ? 1f : Mathf.Clamp01(_t / _life);
            float e = 1f - (1f - u) * (1f - u);
            float fade = 1f - u;
            Paint(_flash, _hi, _scale * Mathf.Lerp(0.26f, 0.78f, e), fade * 0.95f);
            Paint(_glow, _mid, _scale * Mathf.Lerp(0.48f, 1.62f, e), fade * 0.52f);
            Paint(_ring, _ringC, _scale * Mathf.Lerp(0.20f, 1.78f, e), Mathf.Pow(fade, 1.15f) * 0.88f);
            if (_spin && _ring != null)
                _ring.transform.localRotation = Quaternion.Euler(0f, 0f, _t * 280f);

            // 三层图序：墨溅底在下，元素层在中，事件层在上
            Frame(_ink, _inkFrames, u, _scale * 0.92f, InkTheme.Ink, fade * 0.42f);
            Frame(_elem, _elemFrames, u, _scale * 1.12f,
                _elemTinted ? _mid : Color.white, fade * 0.92f);
            if (_eventSprite != null)
                Paint(_event, _eventColor, _scale * Mathf.Lerp(0.45f, 1.05f, e), Mathf.Pow(fade, 0.8f));

            // 被压掉的元素：墨溅边上两个小色点
            for (int i = 0; i < DotN; i++)
            {
                if (_dot[i] == null) continue;
                if (i >= _dots)
                {
                    _dot[i].enabled = false;
                    continue;
                }
                float ang = 0.9f + i * 2.3f;
                _dot[i].transform.localPosition = new Vector3(
                    Mathf.Cos(ang) * _scale * 0.44f * (0.6f + e),
                    Mathf.Sin(ang) * _scale * 0.44f * (0.6f + e), 0f);
                Paint(_dot[i], i == 0 ? _dotColor0 : _dotColor1, _scale * 0.16f, fade * 0.9f);
            }
            for (int i = 0; i < SlashN; i++)
            {
                bool on = i < _slashN;
                if (_slash[i] == null) continue;
                if (!on)
                {
                    _slash[i].enabled = false;
                    continue;
                }
                float ang = 90f * i + 18f;
                _slash[i].transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                _slash[i].transform.localScale = new Vector3(
                    _scale * Mathf.Lerp(0.22f, 0.38f, e),
                    _scale * Mathf.Lerp(0.55f, 1.15f, e),
                    1f);
                Paint(_slash[i], i % 2 == 0 ? _hi : _mid, -1f, fade * 0.78f);
            }
            if (u < 1f) return;
            SetVis(false);
            _on = false;
            enabled = false;
        }

        static void Paint(SpriteRenderer sr, Color rgb, float scale, float a)
        {
            if (sr == null) return;
            sr.enabled = a > 0.02f && sr.sprite != null;
            if (scale > 0f) sr.transform.localScale = Vector3.one * scale;
            rgb.a = Mathf.Clamp01(a);
            sr.color = rgb;
        }

        // 一次性播完 4 帧，不循环。
        static void Frame(SpriteRenderer sr, Sprite[] frames, float u, float scale, Color rgb, float a)
        {
            if (sr == null) return;
            if (frames == null || a <= 0.02f)
            {
                sr.enabled = false;
                return;
            }
            int i = Mathf.Clamp(Mathf.FloorToInt(u * frames.Length), 0, frames.Length - 1);
            sr.sprite = frames[i];
            sr.enabled = sr.sprite != null;
            sr.transform.localScale = Vector3.one * scale;
            rgb.a = Mathf.Clamp01(a);
            sr.color = rgb;
        }

        void SetVis(bool on)
        {
            if (_flash != null) _flash.enabled = on;
            if (_glow != null) _glow.enabled = on;
            if (_ring != null) _ring.enabled = on;
            if (_ink != null) _ink.enabled = on && _inkFrames != null;
            if (_elem != null) _elem.enabled = on && _elemFrames != null;
            if (_event != null) _event.enabled = on && _eventSprite != null;
            if (_dot != null)
                for (int i = 0; i < DotN; i++)
                    if (_dot[i] != null) _dot[i].enabled = on && i < _dots;
            if (_slash == null) return;
            for (int i = 0; i < SlashN; i++)
                if (_slash[i] != null) _slash[i].enabled = on && i < _slashN;
        }

        void Ensure()
        {
            if (_flash != null && _dot != null && _slash != null) return;
            // 域重载后引用丢了但子节点还挂着，先清掉再重建，免得越积越多
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            _flash = Make("flash", InkFx.SoftDisc(), 12);
            _glow = Make("glow", InkFx.SoftDisc(), 10);
            _ring = Make("ring", InkFx.SoftRing(), 11);
            _ink = Make("ink", null, 9);
            _elem = Make("elem", null, 13);
            _event = Make("event", null, 15);
            _slash = new SpriteRenderer[SlashN];
            for (int i = 0; i < SlashN; i++)
                _slash[i] = Make("slash" + i, InkFx.SoftBlade(), 11);
            _dot = new SpriteRenderer[DotN];
            for (int i = 0; i < DotN; i++)
                _dot[i] = Make("dot" + i, InkFx.SoftDisc(), 14);
        }

        SpriteRenderer Make(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            InkFx.PaintSoft(sr, Color.white);
            sr.enabled = false;
            return sr;
        }
    }
}
