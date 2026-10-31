using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 每日残局结算的三轴印章转轮。结果在 ApplyDaily 里已经定好、进了背包，这里只负责转到那三枚。
    // 拉杆 → 三轴依次停 → 前两轴同类时第三轴拖长减速憋一下 → 中奖的那几枚「开光」换成品质章面。
    // 时间全按自己的钟走，Skip 直接拨到结尾，和结算页的跳过一起用。
    public sealed class SealSlot : MonoBehaviour
    {
        const float BodyW = 580f, BodyH = 280f;
        const float WinW = 150f, WinH = 172f;
        const float ReelGap = 166f;
        const float ReelY = 18f;
        const float Cell = 92f;
        const float MedalSize = 78f;
        const float PlateY = -112f;

        const float Pull = 0.2f;          // 拉杆压到底，三轴起转
        const float Stagger = 0.08f;
        const float WindBack = 0.14f;     // 起转前先往回带一下
        const float Accel = 0.24f;
        const float Speed = 15f;          // 格 / 秒
        const float Over = 0.24f;         // 停的时候冲过头多少格，再弹回来
        const float Settle = 0.24f;
        static readonly float[] StopAt = { 1.25f, 1.8f, 2.35f };
        const float Decel = 0.55f;
        const float TenseStop = 4.3f;
        const float TenseDecel = 2.1f;
        const float RevealGap = 0.3f;
        const float LitStep = 0.16f;

        static readonly Color Body = InkTheme.Hex("B8322A");
        static readonly Color BodyDeep = InkTheme.Hex("7A1E14");
        static readonly Color Cream = InkTheme.Hex("FFF8EC");
        static readonly Color BulbOn = InkTheme.Hex("FFE9A0");
        static readonly Color BulbOff = InkTheme.Hex("6E2A1C");
        static readonly Color Rose = InkTheme.Hex("E84E4E");

        sealed class Reel
        {
            public SealType[] Seq;            // Seq[k + 1] 是第 k 格
            public RectTransform Window;
            public Image Bg;
            public RectTransform Strip;
            public readonly RectTransform[] Cells = new RectTransform[4];
            public readonly Image[] Faces = new Image[4];
            public readonly Image[] Marks = new Image[4];
            public Image Glow;
            public Image Dim;
            public float Start, Cruise, Td, V, End;
            public int D;
            public float P;
            public int Tick = int.MinValue;
            public bool Tense, Tensed, Stopped;
            public float Bump = -1f;
        }

        SealSpin _spin;
        RectTransform _root;
        RectTransform _body;
        RectTransform _fxRoot;
        readonly Reel[] _reels = new Reel[3];
        Image[] _bulbs;
        Image[] _bulbGlow;
        RectTransform _stick;
        RectTransform _knob;
        Vector2 _stickBase;
        float _stickLen;
        Text _prize;
        UiAnim _fx;
        float _t;
        float _revealAt;
        float _end;
        bool _pulled;
        bool _revealed;
        bool _quiet;
        Color _litColor;

        public Text Ink { get; private set; }

        // 从出现到演出放完的秒数（含 delay）。
        public float Duration { get; private set; }

        public bool Running => _t < _end;

        public static SealSlot Build(RectTransform parent, RectTransform fxRoot, Vector2 pos, SealSpin spin, float delay)
        {
            var root = ResultKit.Group(parent, "seal_slot", pos, new Vector2(BodyW + 120f, BodyH + 80f));
            var s = root.gameObject.AddComponent<SealSlot>();
            s._root = root;
            s._fxRoot = fxRoot;
            s._spin = spin;
            s._fx = UiAnim.On(s);
            s._t = -delay;
            s.Plan();
            s.Draw();
            s.Duration = delay + s._end;
            s.Apply();
            return s;
        }

        // ---- 排时间、排轴面 ----

        void Plan()
        {
            bool tense = _spin.A == _spin.B;
            for (int i = 0; i < 3; i++)
            {
                var r = new Reel { Tense = tense && i == 2 };
                r.Start = Pull + i * Stagger;
                float stop = r.Tense ? TenseStop : StopAt[i];
                r.Td = r.Tense ? TenseDecel : Decel;
                r.Cruise = stop - r.Start - WindBack - Accel - r.Td;
                float span = Accel * 0.5f + r.Cruise + r.Td * 0.5f;
                r.D = Mathf.Max(6, Mathf.RoundToInt(Speed * span - Over));
                r.V = (r.D + Over) / span;
                r.End = stop;
                r.Seq = Strip(r.D, _spin.Reel(i), Peek(i), r.Tense ? _spin.A : SealType.None);
                _reels[i] = r;
            }
            _revealAt = _reels[2].End + Settle + RevealGap;
            _end = _revealAt + LitStep * 3f + 0.9f;
        }

        // 停稳后上面露出半枚的那一格。前两轴同类、第三轴没中时，露出来的正好是差的那一枚。
        SealType Peek(int i)
        {
            if (i == 2 && _spin.A == _spin.B && _spin.C != _spin.A) return _spin.A;
            return SealCatalog.OtherType(_spin.Reel(i), SealType.None);
        }

        static SealType[] Strip(int d, SealType target, SealType peek, SealType tease)
        {
            var seq = new SealType[d + 4];
            SealType prev = SealType.None;
            for (int n = 0; n < seq.Length; n++)
            {
                prev = SealCatalog.OtherType(prev, SealType.None);
                seq[n] = prev;
            }
            seq[d + 1] = target;
            seq[d + 2] = peek;
            seq[d] = SealCatalog.OtherType(target, peek);
            if (seq[d + 3] == peek) seq[d + 3] = SealCatalog.OtherType(peek, target);
            // 憋悬念的那一轴，减速途中先让那一枚慢慢擦过去一次。
            if (tease != SealType.None && d >= 3)
            {
                seq[d - 2] = tease;
                if (seq[d - 1] == tease) seq[d - 1] = SealCatalog.OtherType(tease, target);
                if (seq[d - 3] == tease) seq[d - 3] = SealCatalog.OtherType(tease, target);
            }
            return seq;
        }

        static float Pos(Reel r, float t)
        {
            float u = t - r.Start;
            if (u <= 0f) return 0f;
            if (u < WindBack) return -0.22f * Mathf.Sin(Mathf.PI * u / WindBack);
            u -= WindBack;
            if (u < Accel) return r.V * u * u / (2f * Accel);
            float p = r.V * Accel * 0.5f;
            u -= Accel;
            if (u < r.Cruise) return p + r.V * u;
            p += r.V * r.Cruise;
            u -= r.Cruise;
            if (u < r.Td) return p + r.V * (u - u * u / (2f * r.Td));
            u -= r.Td;
            if (u < Settle) return r.D + Over * (1f - Ease.OutBack(u / Settle));
            return r.D;
        }

        // ---- 画面 ----

        void Draw()
        {
            var body = UiKit.Stroke(_root, "body", new Vector2(-14f, 0f), new Vector2(BodyW, BodyH), Pin.Center, 5f,
                InkTheme.Outline, Body, 30f);
            _body = body;
            body.GetComponent<Image>().raycastTarget = false;
            var sheen = UiKit.Panel(body, "sheen", new Vector2(0f, BodyH * 0.5f - 34f), new Vector2(BodyW - 28f, 40f),
                new Color(1f, 1f, 1f, 0.12f));
            sheen.GetComponent<Image>().sprite = UiSprites.Fill(20);
            sheen.GetComponent<Image>().type = Image.Type.Sliced;
            sheen.GetComponent<Image>().raycastTarget = false;

            BuildBulbs(body);

            var bezel = UiKit.Stroke(body, "bezel", new Vector2(0f, ReelY), new Vector2(ReelGap * 2f + WinW + 30f, WinH + 26f),
                Pin.Center, 4f, InkTheme.Outline, InkTheme.CoinFace, 22f);
            bezel.GetComponent<Image>().raycastTarget = false;
            for (int i = 0; i < 3; i++) BuildReel(body, i);

            var line = UiKit.Panel(body, "payline", new Vector2(0f, ReelY), new Vector2(ReelGap * 2f + WinW + 14f, 4f),
                new Color(Rose.r, Rose.g, Rose.b, 0.75f));
            line.GetComponent<Image>().raycastTarget = false;
            for (int side = -1; side <= 1; side += 2)
            {
                var tip = UiKit.Stroke(body, "tip", new Vector2(side * (ReelGap + WinW * 0.5f + 8f), ReelY), new Vector2(16f, 16f),
                    Pin.Center, 3f, InkTheme.Outline, Rose, 6f);
                tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
                tip.GetComponent<Image>().raycastTarget = false;
            }

            BuildMarquee(body);
            BuildPlate(body);
            BuildLever();
        }

        void BuildBulbs(RectTransform body)
        {
            const int perSide = 6;
            _bulbs = new Image[perSide * 2];
            _bulbGlow = new Image[perSide * 2];
            float top = BodyH * 0.5f - 46f, bottom = -BodyH * 0.5f + 46f;
            for (int i = 0; i < perSide * 2; i++)
            {
                int side = i < perSide ? -1 : 1;
                int k = i % perSide;
                // 左边从下往上、右边从上往下编号，跑马灯就是绕一圈。
                float y = side < 0 ? Mathf.Lerp(bottom, top, k / (perSide - 1f)) : Mathf.Lerp(top, bottom, k / (perSide - 1f));
                var p = new Vector2(side * (BodyW * 0.5f - 15f), y);
                var glow = UiKit.Icon(body, UiSprites.Disc(), p, 30f);
                glow.color = new Color(1f, 0.85f, 0.4f, 0f);
                _bulbGlow[i] = glow;
                var rim = UiKit.Icon(body, UiSprites.Disc(), p, 17f);
                rim.color = InkTheme.Outline;
                _bulbs[i] = UiKit.Icon(body, UiSprites.Disc(), p, 12f);
                _bulbs[i].color = BulbOff;
            }
        }

        void BuildReel(RectTransform body, int i)
        {
            Reel r = _reels[i];
            var at = new Vector2((i - 1) * ReelGap, ReelY);

            var glowRt = UiKit.Panel(body, "glow" + i, at, new Vector2(WinW + 26f, WinH + 26f), Color.clear);
            r.Glow = glowRt.GetComponent<Image>();
            r.Glow.sprite = UiSprites.Fill(28);
            r.Glow.type = Image.Type.Sliced;
            r.Glow.raycastTarget = false;

            r.Window = UiKit.Stroke(body, "reel" + i, at, new Vector2(WinW, WinH), Pin.Center, 4f, InkTheme.Outline, Cream, 16f);
            r.Bg = r.Window.GetComponent<Image>();
            r.Bg.raycastTarget = false;

            var maskGo = new GameObject("mask", typeof(RectTransform), typeof(RectMask2D));
            maskGo.transform.SetParent(r.Window, false);
            var mask = (RectTransform)maskGo.transform;
            mask.anchorMin = Vector2.zero;
            mask.anchorMax = Vector2.one;
            mask.offsetMin = new Vector2(4f, 4f);
            mask.offsetMax = new Vector2(-4f, -4f);
            r.Strip = mask;

            Sprite face = InkSprites.Load("Ui/seal_empty");
            for (int j = 0; j < 4; j++)
            {
                var cell = ResultKit.Group(mask, "c" + j, Vector2.zero, new Vector2(MedalSize, MedalSize));
                r.Cells[j] = cell;
                r.Faces[j] = UiKit.Icon(cell, face, Vector2.zero, MedalSize);
                r.Marks[j] = UiKit.Icon(cell, null, Vector2.zero, MedalSize * 0.52f);
            }

            // 上下压暗，转起来像个圆筒。
            Sprite shade = UiSprites.Gradient(new Color(0.23f, 0.12f, 0.08f, 0.5f), new Color(0.23f, 0.12f, 0.08f, 0f));
            for (int side = -1; side <= 1; side += 2)
            {
                var sh = UiKit.Icon(mask, shade, new Vector2(0f, side * (WinH * 0.5f - 26f)), 1f);
                sh.preserveAspect = false;
                sh.rectTransform.sizeDelta = new Vector2(WinW, 52f);
                if (side < 0) sh.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            }

            var dimRt = UiKit.Panel(r.Window, "dim", Vector2.zero, new Vector2(WinW - 8f, WinH - 8f), Color.clear);
            r.Dim = dimRt.GetComponent<Image>();
            r.Dim.sprite = UiSprites.Fill(12);
            r.Dim.type = Image.Type.Sliced;
            r.Dim.raycastTarget = false;
        }

        void BuildMarquee(RectTransform body)
        {
            var sign = UiKit.Stroke(body, "sign", new Vector2(0f, BodyH * 0.5f + 4f), new Vector2(250f, 54f), Pin.Center, 4f,
                InkTheme.Outline, InkTheme.CoinFace, 27f);
            sign.GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Label(sign, "t", "印章转轮", 30, new Vector2(0f, 1f), new Vector2(240f, 50f));
            t.color = BodyDeep;
            UiKit.Bold(t);
        }

        void BuildPlate(RectTransform body)
        {
            var plate = UiKit.Stroke(body, "plate", new Vector2(0f, PlateY), new Vector2(BodyW - 70f, 46f), Pin.Center, 3f,
                InkTheme.Outline, Cream, 23f);
            plate.GetComponent<Image>().raycastTarget = false;
            // 文字框的坐标是中心。两头是圆角，内容从圆角里侧起排。
            float w = BodyW - 70f;
            const float inset = 22f;
            const float icon = 28f;
            float iconX = -w * 0.5f + inset + icon * 0.5f;
            UiKit.Icon(plate, InkSprites.Ui("ink"), new Vector2(iconX, 0f), icon);
            const float inkW = 148f;
            float inkLeft = iconX + icon * 0.5f + 8f;
            Ink = UiKit.Label(plate, "ink", "墨 +0", 24, new Vector2(inkLeft + inkW * 0.5f, 0f), new Vector2(inkW, 40f), TextAnchor.MiddleLeft);
            Ink.color = InkTheme.TextDark;
            UiKit.Bold(Ink);
            const float prizeW = 230f;
            float prizeRight = w * 0.5f - inset;
            _prize = UiKit.Label(plate, "prize", "三个同类得金印", 22, new Vector2(prizeRight - prizeW * 0.5f, 0f), new Vector2(prizeW, 40f), TextAnchor.MiddleRight);
            _prize.color = InkTheme.TextMid;
            UiKit.Bold(_prize);
        }

        void BuildLever()
        {
            float x = BodyW * 0.5f + 14f;
            var socket = UiKit.Stroke(_root, "socket", new Vector2(x - 6f, 4f), new Vector2(26f, 70f), Pin.Center, 3f,
                InkTheme.Outline, InkTheme.Hex("4A3A30"), 10f);
            socket.GetComponent<Image>().raycastTarget = false;
            _stickLen = 104f;
            _stickBase = new Vector2(x + 2f, 4f);
            _stick = UiKit.Panel(_root, "stick", _stickBase, new Vector2(10f, _stickLen), InkTheme.Hex("C9C3BA"));
            _stick.pivot = new Vector2(0.5f, 0f);
            _stick.anchoredPosition = _stickBase;
            var si = _stick.GetComponent<Image>();
            si.sprite = UiSprites.Fill(12);
            si.type = Image.Type.Sliced;
            si.raycastTarget = false;
            _knob = ResultKit.Group(_root, "knob", _stickBase + new Vector2(0f, _stickLen), new Vector2(40f, 40f));
            var rim = UiKit.Icon(_knob, UiSprites.Disc(), Vector2.zero, 40f);
            rim.color = InkTheme.Outline;
            var ball = UiKit.Icon(_knob, UiSprites.Disc(), Vector2.zero, 32f);
            ball.color = Rose;
            var hi = UiKit.Icon(_knob, UiSprites.Disc(), new Vector2(-6f, 6f), 10f);
            hi.color = new Color(1f, 1f, 1f, 0.75f);
        }

        // ---- 走钟 ----

        void Update() => Advance(Time.unscaledDeltaTime);

        public void Skip()
        {
            if (!Running) return;
            _quiet = true;
            Advance(_end - _t + 0.001f);
            _fx.Finish();
            _quiet = false;
        }

        void Advance(float dt)
        {
            float before = _t;
            _t += dt;
            if (before < 0f && _t >= 0f && !_pulled)
            {
                _pulled = true;
                if (!_quiet) AudioBus.SlotLever();
            }
            for (int i = 0; i < 3; i++)
            {
                Reel r = _reels[i];
                if (r.Tense && !r.Tensed && _t >= r.End - r.Td)
                {
                    r.Tensed = true;
                    if (!_quiet) AudioBus.SlotTense();
                }
                if (!r.Stopped && _t >= r.End)
                {
                    r.Stopped = true;
                    r.Bump = 0f;
                    if (!_quiet) AudioBus.SlotStop(i);
                }
            }
            if (!_revealed && _t >= _revealAt) Reveal();
            Apply();
        }

        void Apply()
        {
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            for (int i = 0; i < 3; i++)
            {
                Reel r = _reels[i];
                float p = Pos(r, _t);
                float speed = Mathf.Abs(p - r.P) / dt;
                r.P = p;
                int tick = Mathf.FloorToInt(p + 0.5f);
                if (tick != r.Tick)
                {
                    if (r.Tick != int.MinValue && !_quiet && _t > r.Start && _t < r.End)
                        AudioBus.SlotTick(1f + 0.07f * i + (r.Tensed ? 0.15f : 0f));
                    r.Tick = tick;
                }
                float blur = Mathf.Clamp01((speed - 4f) / 10f);
                int baseK = Mathf.FloorToInt(p) - 1;
                for (int j = 0; j < 4; j++)
                {
                    int k = baseK + j;
                    int idx = Mathf.Clamp(k + 1, 0, r.Seq.Length - 1);
                    var cell = r.Cells[j];
                    cell.anchoredPosition = new Vector2(0f, (k - p) * Cell);
                    cell.localScale = new Vector3(1f - 0.08f * blur, 1f + 0.42f * blur, 1f);
                    r.Marks[j].sprite = SealArt.Icon(r.Seq[idx]);
                    float a = 1f - 0.45f * blur;
                    r.Faces[j].color = new Color(1f, 1f, 1f, a);
                    r.Marks[j].color = new Color(1f, 0.96f, 0.86f, a);
                }

                float wobble = 0f;
                if (r.Bump >= 0f)
                {
                    r.Bump += dt;
                    float k = Mathf.Clamp01(r.Bump / 0.3f);
                    wobble = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * 0.07f;
                    if (k >= 1f) r.Bump = -1f;
                }
                r.Window.localScale = new Vector3(1f + wobble, 1f - wobble, 1f);

                if (!_revealed)
                {
                    float glow = r.Tensed && !r.Stopped ? 0.55f + 0.4f * Mathf.Sin(_t * 18f) : 0f;
                    r.Glow.color = new Color(1f, 0.78f, 0.25f, glow);
                }
                else if (_spin.Lit(i))
                {
                    Color c = _litColor;
                    c.a = 0.6f + 0.35f * Mathf.Sin(_t * 6f);
                    r.Glow.color = c;
                }
            }
            Lever();
            Bulbs();
        }

        void Lever()
        {
            float u = _t;
            float k;
            if (u <= 0f || u >= 0.75f) k = 1f;
            else if (u < Pull) k = Mathf.Lerp(1f, -0.55f, Ease.InQuad(u / Pull));
            else k = Mathf.LerpUnclamped(-0.55f, 1f, Ease.OutBack((u - Pull) / (0.75f - Pull)));
            _stick.localScale = new Vector3(1f, k, 1f);
            _knob.anchoredPosition = _stickBase + new Vector2(0f, _stickLen * k);
            float s = 1f - 0.12f * Mathf.Clamp01(-k);
            _knob.localScale = new Vector3(s, s, 1f);
        }

        void Bulbs()
        {
            int n = _bulbs.Length;
            bool tense = _reels[2].Tensed && !_reels[2].Stopped;
            for (int i = 0; i < n; i++)
            {
                bool on;
                if (_t < 0f) on = (i + Mathf.FloorToInt(_t * 2f)) % 2 == 0;
                else if (!_revealed) on = tense ? Mathf.FloorToInt(_t * 9f) % 2 == 0 : (i + Mathf.FloorToInt(_t * 14f)) % 3 == 0;
                else if (_spin.Hit == SlotHit.Triple) on = (Mathf.FloorToInt(_t * 10f) + i) % 2 == 0;
                else if (_spin.Hit == SlotHit.Pair) on = (i + Mathf.FloorToInt(_t * 12f)) % 3 != 0;
                else on = true;
                _bulbs[i].color = on ? (tense ? InkTheme.Hex("FF8A6A") : BulbOn) : BulbOff;
                _bulbGlow[i].color = new Color(1f, 0.85f, 0.4f, on ? 0.55f : 0f);
            }
        }

        // ---- 开光 ----

        void Reveal()
        {
            _revealed = true;
            SealDrop prize = _spin.Prize;
            _litColor = SealCatalog.QualityColor(prize.Quality);
            var body = _body;
            int n = 0;
            for (int i = 0; i < 3; i++)
            {
                Reel r = _reels[i];
                if (!_spin.Lit(i))
                {
                    _fx.Tween(0f, 0.3f, k => { if (r.Dim != null) r.Dim.color = new Color(0.16f, 0.1f, 0.08f, 0.5f * k); });
                    continue;
                }
                float d = LitStep * n++;
                Vector2 at = r.Window.anchoredPosition;
                var medal = SealArt.Medal(body, "lit" + i, prize.Type, prize.Quality, at, MedalSize + 10f);
                medal.gameObject.SetActive(false);
                Color tint = Color.Lerp(Cream, _litColor, 0.28f);
                _fx.At(d, () =>
                    {
                        if (medal != null) medal.gameObject.SetActive(true);
                        if (r.Strip != null) r.Strip.gameObject.SetActive(false);
                        if (r.Bg != null) r.Bg.color = tint;
                        UiConfetti.Sparks(_root, (Vector2)body.anchoredPosition + at, _litColor, 14, 560f);
                        if (!_quiet) AudioBus.Stamp();
                    })
                   .Pop(medal, d, 0.42f, 1.7f);
            }

            float last = LitStep * Mathf.Max(0, n - 1) + 0.12f;
            _fx.At(last, () =>
            {
                if (_prize != null)
                {
                    _prize.text = "得到 " + SealCatalog.DropName(prize);
                    _prize.color = SealCatalog.QualityDeep(prize.Quality);
                }
                switch (_spin.Hit)
                {
                    case SlotHit.Triple: AudioBus.SlotJackpot(); break;
                    case SlotHit.Pair: AudioBus.SlotPair(); break;
                    default: AudioBus.SlotSmall(); break;
                }
            });
            if (_prize != null) _fx.Punch(_prize.transform, last, 0.22f, 0.36f);

            if (_spin.Hit == SlotHit.Triple) Jackpot(body, last);
            else if (_spin.Hit == SlotHit.Pair) PairWin(body, last);
        }

        void Jackpot(RectTransform body, float at)
        {
            var rays = ResultKit.Rays(_root, body.anchoredPosition + new Vector2(0f, ReelY), 860f,
                new Color(1f, 0.82f, 0.35f, 0.9f));
            rays.color = new Color(1f, 0.82f, 0.35f, 0f);
            _fx.Fade(rays, at, 0.4f, 0f, 0.9f).Spin(rays.transform, 22f);

            var flash = UiKit.Panel(_fxRoot, "slot_flash", Vector2.zero, Vector2.zero, new Color(1f, 0.96f, 0.82f, 0f));
            flash.anchorMin = Vector2.zero;
            flash.anchorMax = Vector2.one;
            flash.offsetMin = flash.offsetMax = Vector2.zero;
            var flashImg = flash.GetComponent<Image>();
            flashImg.raycastTarget = false;
            _fx.Tween(at, 0.5f, k =>
                {
                    if (flashImg != null) flashImg.color = new Color(1f, 0.96f, 0.82f, k > 0f ? 0.8f * (1f - Ease.OutCubic(k)) : 0f);
                })
               .At(at, () =>
               {
                   _fx.Shake(_root, 0f, 16f, 0.45f);
                   UiConfetti.Burst(_fxRoot, 120);
                   for (int i = 0; i < 3; i++)
                       UiConfetti.Sparks(_root, body.anchoredPosition + _reels[i].Window.anchoredPosition,
                           InkTheme.CoinFace, 18, 760f);
               });
            Callout(body, "三连", InkTheme.Hex("E2662C"), at);
        }

        void PairWin(RectTransform body, float at)
        {
            var rays = ResultKit.Rays(_root, body.anchoredPosition + new Vector2(0f, ReelY), 700f,
                new Color(0.78f, 0.62f, 1f, 0f));
            _fx.Fade(rays, at, 0.4f, 0f, 0.6f).Spin(rays.transform, 16f)
               .At(at, () =>
               {
                   _fx.Shake(_root, 0f, 8f, 0.3f);
                   UiConfetti.Burst(_fxRoot, 48);
               });
            Callout(body, "两连", SealCatalog.QualityColor(SealQuality.Purple), at);
        }

        void Callout(RectTransform body, string text, Color fill, float at)
        {
            var pos = body.anchoredPosition + new Vector2(BodyW * 0.5f - 46f, BodyH * 0.5f - 6f);
            var g = ResultKit.Group(_root, "callout", pos, new Vector2(120f, 56f));
            g.localRotation = Quaternion.Euler(0f, 0f, -12f);
            UiKit.Stroke(g, "bg", Vector2.zero, new Vector2(120f, 56f), Pin.Center, 4f, InkTheme.Outline, fill, 16f)
                .GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Label(g, "t", text, 30, Vector2.zero, new Vector2(120f, 56f));
            t.color = Color.white;
            UiKit.Bold(t);
            g.gameObject.SetActive(false);
            _fx.At(at, () => { if (g != null) g.gameObject.SetActive(true); })
               .Pop(g, at, 0.26f, 2.4f);
        }
    }
}
