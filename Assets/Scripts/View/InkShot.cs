using UnityEngine;

namespace InkLine
{
    // 只读 ShotLook 给出的五个槽位，不认识任何具体的字。
    // 加字只要在 GlyphDef 里填槽位，这里不用动。
    public sealed class InkShot : MonoBehaviour
    {
        SpriteRenderer _core;
        SpriteRenderer _pellet;
        SpriteRenderer _glow;
        SpriteRenderer _hot;
        SpriteRenderer _form;
        SpriteRenderer _halo;
        SpriteRenderer _orbitA;
        SpriteRenderer _orbitB;
        SpriteRenderer _moteA;
        SpriteRenderer _moteB;
        TrailRenderer _body;
        TrailRenderer _trail;
        bool _bodyOn;
        bool _trailOn;
        SpriteRenderer _flare;
        SpriteRenderer _glint;
        readonly ShotTrail _sparks = new ShotTrail();
        readonly Ornament[] _orn = new Ornament[ShotTrail.MaxOrnaments];
        readonly System.Collections.Generic.List<SpriteRenderer> _ornSr = new System.Collections.Generic.List<SpriteRenderer>();
        int _bid = -1;
        Vector2 _last;

        public void Present(BulletActor b, int skin)
        {
            if (_core == null) _core = GetComponent<SpriteRenderer>();
            InkVfx.Ensure();

            ShotMods m = b.Mods;
            ShotView v = ShotLook.Resolve(m);

            float ang = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg - 90f;
            transform.SetPositionAndRotation(new Vector3(b.Pos.x, b.Pos.y, 0f), Quaternion.Euler(0f, 0f, ang));

            int heavyStar = m.Star(CardId.Heavy);
            bool pierce = m.Star(CardId.Pierce) > 0;

            // 视觉大小和碰撞半径解耦。重不改弹体大小，只加粗身后的拖尾。
            // ShotScale 把弹体、光晕、拖尾一起放大，命中判定仍用 BulletActor.Radius。
            const float ShotScale = 1.4f;
            float coreS = 0.50f * ShotScale;
            float breath = 1f + 0.018f * Mathf.Sin(Time.unscaledTime * 10f + b.Id * 1.7f);
            float sx = (pierce ? coreS * 0.68f : coreS) * breath;
            float sy = (pierce ? coreS * 1.32f : coreS) * (2f - breath);
            transform.localScale = new Vector3(sx, sy, 1f);

            InkVfx.Stop(_core);
            _core.enabled = false;

            // 体槽亮着就不要再露出底下那颗墨点，否则就是「彩色包黑心」。
            bool flat = InkVfx.Flat(v.Form.Fx, out InkVfx.FlatBody body);
            Sprite formSprite = flat ? InkVfx.FlatFrame(body, v.Form.Star, b.Id) : null;
            if (formSprite == null)
            {
                flat = false;
                formSprite = InkVfx.Shot(v.Form.Fx);
            }
            bool hasBody = formSprite != null;
            // 没吃字的弹按炮台皮肤走。钢珠是黑铁球，糖果是粉红糖球，机甲是青蓝能量球，黄金才用紫芯金边。
            bool naked = !hasBody && v.Elements == 0 && !v.Form.On && !v.Trail.On && !v.Halo.On && !v.Orbit.On && !v.Bloom.On;
            NakedShot look = naked ? NakedOf(skin) : default;
            if (naked && skin == 3)
                look.GlowScale = 1.05f + 0.08f * Mathf.Sin(Time.unscaledTime * 9f + b.Id);
            bool bright = ShotSparks.Bright;
            Color tone = ShotTrail.Tone(m, v);
            // 只挂了道族 / 伤族字（速、瞄、分、重……）的弹没有元素色，原来落成一颗黑墨点。
            // 「炫」档改用那个字自己的亮色，和它掉的粒子是一家。
            Color pelletTint = naked ? look.Pellet
                : (bright && v.Elements == 0 && tone.a > 0f ? Color.Lerp(tone, Color.white, 0.15f) : Color.white);
            Sprite pellet = InkArt.Heap(InkShape.Circle, Color.white, 64);
            Child(ref _pellet, "pellet", pellet, naked ? look.PelletScale : 0.20f, 6,
                hasBody ? Color.clear : pelletTint);
            if (hasBody && _pellet != null) _pellet.enabled = false;
            SkinArt(naked ? SkinShot(skin) : null, skin, b.Id);

            // 晕光槽：重和金只放一层柔光，不换形。
            Color glow = v.Elements > 0 ? Fade(v.Mixed, 0.26f) : Color.clear;
            float glowS = 0.88f;
            if (v.Bloom.On)
            {
                glow = Fade(v.Bloom.Tint, 0.30f);
                glowS = v.Bloom.Fx == ShotFx.BloomHeavy ? 1.05f : 0.95f;
            }
            else if (v.Form.On && v.Elements == 0)
                glow = Fade(v.Form.Tint, 0.24f);
            // 平涂体自己就是完整配色，再垫柔光只会把硬边泡软。「还带着别的字」
            // 现在由小卫星来说（数得清、且是硬边），所以平涂体一概不垫柔光，
            // 只有重 / 金的晕光槽还留着 —— 那是另一件事。
            if (flat && !v.Bloom.On) glow = Color.clear;
            // 「炫」档弹后已经有一团加法光，重 / 金那层柔光再垫就糊了。
            if (bright && !naked) glow = Color.clear;
            if (naked)
            {
                glow = look.Glow;
                glowS = look.GlowScale;
            }
            Child(ref _glow, "glow", InkFx.SoftDisc(), glowS, 5, glow);
            Child(ref _hot, "hot", InkFx.SoftDisc(), naked ? look.HotScale : 0.16f,
                7, naked ? look.Hot
                    : (v.Elements > 0 && !flat ? Fade(Color.white, 0.34f) : Color.clear));

            // 元素小卫星：体槽图没画到的元素各挂一颗，绕着弹体转。
            // 「炫」档每个字都有自己的粒子和绕身小件，卫星就不再挂，免得一颗弹身边挤满。
            Mote(ref _moteA, "moteA", !bright && v.Motes > 0, v.MoteA, 0f);
            Mote(ref _moteB, "moteB", !bright && v.Motes > 1, v.MoteB, Mathf.PI);
            if (naked && look.Orbit && v.Motes == 0)
                Mote(ref _moteA, "moteA", true, look.OrbitTint, 0f);

            if (flat)
                // 平涂体按原色画，不往混色偏。招牌两两的配色是照游戏调色板
                // 逐档定的（燎毒的绿火、雷决的靛紫电弧），染一层混色正好把它糊掉。
                Body(ref _form, formSprite, body, Color.white);
            else
                Child(ref _form, "form", formSprite, FormScale(v.Form.Fx), 7,
                    formSprite == null ? Color.clear : Tint(v.Form.Tint));

            // 环槽 / 绕槽原来是柔环、柔光团，在纸面上发灰。现在一律换成 ShotTrail.Ornaments
            // 给的硬边小件（电弧、金星、金闪、叶子、准星、漩涡），这两个槽只剩 ShotLook 里的记账。
            if (_halo != null) _halo.enabled = false;
            if (_orbitA != null) _orbitA.enabled = false;
            if (_orbitB != null) _orbitB.enabled = false;

            // 速原来还在后面挂两片柔光残影（`Ghost`），已撤：速现在有自己的绿尾，
            // 那两片是同一件事说两遍，而且是软的 —— 正是「子弹过后留了脏」的来源之一。

            // 主色带跟着混色走，尾槽再单独拉一条自己的色带。
            Color head = v.Elements > 0 ? Color.Lerp(v.Mixed, Color.white, 0.38f) : InkTheme.Bone;
            Color tail = v.Elements > 0 ? v.Mixed : InkTheme.BoneMid;
            float width = 0.12f + 0.02f * Mathf.Max(v.Form.Star, v.Elements);
            float ribbonTime = flat ? 0.07f : 0.10f;
            if (naked)
            {
                head = look.Head;
                tail = look.Tail;
                width = look.Width;
                ribbonTime = look.Time;
            }
            width *= ShotScale;
            // 只有这发弹没有别的字撑场面时，拖尾才染成铜色并按星级拉长。
            // 叠了火、冰这些，弹体和它们自己的色带保持原样，重只在粒子层另加一道铜尾。
            if (heavyStar > 0 && v.Elements == 0 && !v.Form.On && !v.Trail.On)
            {
                float mul = GlyphTable.Get(CardId.Heavy).Size.At(heavyStar);
                Color bronze = ShotTrail.GlowOf(CardId.Heavy);
                head = Color.Lerp(bronze, Color.white, 0.28f);
                tail = bronze;
                width *= mul;
                ribbonTime *= mul;
            }
            // 平涂体图里已经自带了尾，这里只补一条细速度线，宽了会盖住硬边。
            if (flat)
            {
                // 不带元素的道族 / 词组保持墨骨色 —— 它们本来就靠「不带色」
                // 和元素弹区分开，这里套 Mixed 会把那点区分度抹掉。
                if (v.Elements > 0)
                {
                    head = Color.Lerp(v.Mixed, Color.white, 0.10f);
                    tail = v.Mixed;
                }
                width *= 0.55f;
            }
            // 时长就是「拖尾留多长的历史」。弹速 7.2，0.07s 只有半个世界单位、
            // 约两三个弹身，读作速度线；原来 0.22 / 0.42 拉出两三个弹身开外，
            // 子弹早飞走了那截还亮着，看着就是残留。
            Ribbon(ref _body, ref _bodyOn, "body", true, head, tail, width, ribbonTime);
            // 尾槽是速 / 瞄的本体表现（绿 / 紫），给得比主色带长一点才看得出来，
            // 但仍然短到不会脱离子弹。
            Ribbon(ref _trail, ref _trailOn, "trail", v.Trail.On,
                Color.Lerp(v.Trail.Tint, Color.white, 0.2f), v.Trail.Tint,
                (v.Trail.Fx == ShotFx.TrailAccel ? 0.15f : 0.14f) * ShotScale,
                v.Trail.Fx == ShotFx.TrailAccel ? 0.17f : 0.15f);

            if (flat && v.Form.Fx == ShotFx.FormWind && _form != null)
                _form.transform.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 540f + b.Id * 37f);
            Ornaments(m, b.Id);
            BrightFx(ShotTrail.Aura(m, v), naked);
            Sparks(b, m);
        }

        // ---- 粒子层：按飞过的距离掉，停住（选牌暂停）就不再掉 ----

        void Sparks(BulletActor b, ShotMods m)
        {
            if (b.Id != _bid)
            {
                _bid = b.Id;
                _last = b.Pos;
                _sparks.Reset();
                return;
            }
            Vector2 delta = b.Pos - _last;
            _last = b.Pos;
            float d = delta.magnitude;
            if (d <= 1e-4f || d > 1.5f) return;
            Vector2 dir = b.Vel.sqrMagnitude > 1e-4f ? b.Vel.normalized : Vector2.up;
            _sparks.Step(m, b.Pos, dir, d, ShotSparks.World);
        }

        // 绕身小件：每件一个子节点，按需要长，多出来的关掉。
        void Ornaments(ShotMods m, int seed)
        {
            int sets = ShotTrail.Ornaments(m, _orn);
            int used = 0;
            float t = Time.time;
            Undistort(out float kx, out float ky);
            for (int s = 0; s < sets; s++)
            {
                Ornament o = _orn[s];
                Sprite sprite = ShotSparks.SpriteOf(o.Kind);
                Material mat = ShotSparks.MatOf(o.Glow);
                for (int i = 0; i < o.Count; i++)
                {
                    if (used == _ornSr.Count) _ornSr.Add(Spawn("orn" + used));
                    SpriteRenderer sr = _ornSr[used++];
                    bool on = ShotTrail.Place(o, i, t, seed, out Vector2 p, out float rot, out float sc, out bool behind);
                    sr.enabled = on;
                    if (!on) continue;
                    sr.sprite = sprite;
                    sr.sharedMaterial = mat;
                    sr.color = o.Tint;
                    sr.sortingOrder = behind ? 5 : 9;
                    sr.transform.localPosition = new Vector3(p.x * kx, p.y * ky, 0f);
                    sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
                    sr.transform.localScale = new Vector3(sc * kx, sc * ky, 1f);
                }
            }
            for (int i = used; i < _ornSr.Count; i++) _ornSr[i].enabled = false;
        }

        // 「炫」档：能量类的字在弹体后面垫一团加法光，雷 / 金 / 穿再在弹心加一颗星芒。
        // 实心类的字和「实」档都不画。
        void BrightFx(ShotAura aura, bool naked)
        {
            bool on = ShotSparks.Bright && !naked;
            bool flare = on && aura.Glow.a > 0f;
            bool glint = on && aura.Glint.a > 0f;
            if (_flare != null) _flare.enabled = flare;
            if (_glint != null) _glint.enabled = glint;
            if (!flare && !glint) return;
            Undistort(out float kx, out float ky);
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 14f + _bid);
            if (flare)
            {
                if (_flare == null) _flare = Spawn("flare");
                _flare.enabled = true;
                _flare.sprite = InkFx.SoftDisc();
                _flare.sortingOrder = 5;
                InkFx.PaintAdd(_flare, aura.Glow);
                _flare.transform.localPosition = Vector3.zero;
                float f = aura.Flare * pulse;
                _flare.transform.localScale = new Vector3(f * kx, f * ky, 1f);
            }
            if (glint)
            {
                if (_glint == null) _glint = Spawn("glint");
                _glint.enabled = true;
                _glint.sprite = ShotSparks.SpriteOf(SparkKind.Spark);
                _glint.sortingOrder = 10;
                InkFx.PaintAdd(_glint, aura.Glint);
                _glint.transform.localPosition = Vector3.zero;
                _glint.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 180f);
                float g = aura.GlintScale * pulse;
                _glint.transform.localScale = new Vector3(g * kx, g * ky, 1f);
            }
        }

        struct NakedShot
        {
            public Color Pellet;
            public float PelletScale;
            public Color Glow;
            public float GlowScale;
            public Color Hot;
            public float HotScale;
            public bool Orbit;
            public Color OrbitTint;
            public Color Head;
            public Color Tail;
            public float Width;
            public float Time;
        }

        static Sprite SkinShot(int skin)
        {
            if (skin == SkinCatalog.Flame) return InkSprites.Load("shot_flame");
            if (skin == SkinCatalog.Lucky) return InkSprites.Load("shot_coin");
            return null;
        }

        // 赤焰、福袋的裸弹是一张画好的弹体图（火流星 / 铜钱），图是「弹头在上、尾巴在下」画的，
        // 本节点已经转到飞行方向，这里只把弹头沉回子弹位置。
        void SkinArt(Sprite sprite, int skin, int seed)
        {
            if (sprite == null)
            {
                if (_skinArt != null) _skinArt.enabled = false;
                return;
            }
            if (_skinArt == null) _skinArt = Spawn("skinShot");
            _skinArt.enabled = true;
            _skinArt.sprite = sprite;
            Undistort(out float kx, out float ky);
            bool flame = skin == SkinCatalog.Flame;
            float size = flame ? 0.95f : 0.62f;
            float sink = flame ? 0.20f : 0.10f;
            _skinArt.transform.localPosition = new Vector3(0f, -sink * size * ky * sprite.bounds.size.y, 0f);
            float wobble = flame ? 0f : Mathf.Sin(Time.time * 9f + seed * 1.3f) * 14f;
            _skinArt.transform.localRotation = Quaternion.Euler(0f, 0f, wobble);
            _skinArt.transform.localScale = new Vector3(size * kx, size * ky, 1f);
            _skinArt.sortingOrder = 7;
            InkFx.PaintSprite(_skinArt, Color.white);
        }

        SpriteRenderer _skinArt;

        // 0 钢珠黑球，1 糖果粉球，2 机甲能量球，3 黄金紫芯绕金，4 火流星，5 铜钱。
        static NakedShot NakedOf(int skin)
        {
            if (skin == SkinCatalog.Flame)
            {
                return new NakedShot
                {
                    Pellet = Color.clear,
                    PelletScale = 0.2f,
                    Glow = Fade(InkTheme.FireMid, 0.30f),
                    GlowScale = 0.78f,
                    Hot = Color.clear,
                    HotScale = 0.1f,
                    Head = InkTheme.FireHi,
                    Tail = InkTheme.Fire,
                    Width = 0.13f,
                    Time = 0.12f
                };
            }
            if (skin == SkinCatalog.Lucky)
            {
                return new NakedShot
                {
                    Pellet = Color.clear,
                    PelletScale = 0.2f,
                    Glow = Fade(InkTheme.GoldHi, 0.22f),
                    GlowScale = 0.62f,
                    Hot = Color.clear,
                    HotScale = 0.1f,
                    Orbit = true,
                    OrbitTint = InkTheme.GoldHi,
                    Head = InkTheme.GoldHi,
                    Tail = InkTheme.Gold,
                    Width = 0.07f,
                    Time = 0.07f
                };
            }
            if (skin == 3)
            {
                return new NakedShot
                {
                    Pellet = InkTheme.Track,
                    PelletScale = 0.28f,
                    Glow = Fade(InkTheme.Track, 0.42f),
                    GlowScale = 1.05f,
                    Hot = Fade(InkTheme.GoldHi, 0.85f),
                    HotScale = 0.22f,
                    Orbit = true,
                    OrbitTint = InkTheme.GoldHi,
                    Head = InkTheme.GoldHi,
                    Tail = InkTheme.Track,
                    Width = 0.16f,
                    Time = 0.16f
                };
            }
            if (skin == 1)
            {
                Color candy = InkTheme.Hex("FF5C8A");
                return new NakedShot
                {
                    Pellet = candy,
                    PelletScale = 0.26f,
                    Glow = Fade(candy, 0.35f),
                    GlowScale = 0.72f,
                    Hot = Fade(InkTheme.Hex("FFF0F5"), 0.9f),
                    HotScale = 0.12f,
                    Head = InkTheme.Hex("FFB3CC"),
                    Tail = candy,
                    Width = 0.08f,
                    Time = 0.06f
                };
            }
            if (skin == 2)
            {
                Color neon = InkTheme.Hex("3FD0FF");
                return new NakedShot
                {
                    Pellet = neon,
                    PelletScale = 0.24f,
                    Glow = Fade(neon, 0.28f),
                    GlowScale = 0.70f,
                    Hot = Fade(InkTheme.Hex("F4F7F2"), 0.95f),
                    HotScale = 0.12f,
                    Head = InkTheme.Hex("D8F6FF"),
                    Tail = neon,
                    Width = 0.09f,
                    Time = 0.08f
                };
            }
            return new NakedShot
            {
                Pellet = InkTheme.Ink,
                PelletScale = 0.20f,
                Glow = Color.clear,
                GlowScale = 0.2f,
                Hot = Color.clear,
                HotScale = 0.1f,
                Head = InkTheme.GraphiteMid,
                Tail = InkTheme.Ink,
                Width = 0.07f,
                Time = 0.05f
            };
        }

        static float FormScale(ShotFx fx)
        {
            switch (fx)
            {
                case ShotFx.FormKnock: return 1.20f;
                case ShotFx.FormExplode: return 1.10f;
                case ShotFx.FormPierce: return 1.02f;
                default: return 1.06f;
            }
        }

        static Color Tint(Color c) => Color.Lerp(Color.white, c, 0.45f);

        void OnDisable()
        {
            Quiet(ref _body, ref _bodyOn);
            Quiet(ref _trail, ref _trailOn);
        }

        static Color Fade(Color c, float a)
        {
            c.a = a;
            return c;
        }

        void Child(ref SpriteRenderer sr, string name, Sprite sprite, float scale, int order, Color color)
        {
            if (color.a < 0.02f || sprite == null)
            {
                if (sr != null) sr.enabled = false;
                return;
            }
            if (sr == null) sr = Spawn(name);
            sr.enabled = true;
            sr.sprite = sprite;
            sr.transform.localPosition = Vector3.zero;
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one * scale;
            sr.sortingOrder = order;
            InkFx.PaintSoft(sr, color);
        }

        // 平涂体单独一条路：普通混合、不染色、按图里的球心往尾巴方向沉一点，
        // 弹心才落在真正的子弹位置上（图是「球在上、尾在下」画的）。
        void Body(ref SpriteRenderer sr, Sprite sprite, in InkVfx.FlatBody body, Color tint)
        {
            if (sprite == null)
            {
                if (sr != null) sr.enabled = false;
                return;
            }
            if (sr == null) sr = Spawn("form");
            sr.enabled = true;
            sr.sprite = sprite;
            Undistort(out float kx, out float ky);
            sr.transform.localPosition =
                new Vector3(0f, -body.Sink * body.Scale * ky * sprite.bounds.size.y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = new Vector3(body.Scale * kx, body.Scale * ky, 1f);
            sr.sortingOrder = 7;
            InkFx.PaintSprite(sr, tint);
        }

        // 本节点的缩放是非等比的：穿把它拉成 0.68 x 1.32，呼吸再叠一层反向的挤压。
        // 那是给旧的柔光墨点设计的 —— 软球压扁看不出，硬边图压扁就是个蛋。
        // 平涂的子节点用这两个系数把长宽比除掉，只留总体大小，就永远不变形。
        // 穿的「细长」本来由 pierce_shot 那根长针自己说，不需要再挤弹体。
        void Undistort(out float kx, out float ky)
        {
            Vector3 p = transform.localScale;
            float u = Mathf.Sqrt(Mathf.Abs(p.x * p.y));
            kx = Mathf.Abs(p.x) > 1e-4f ? u / p.x : 1f;
            ky = Mathf.Abs(p.y) > 1e-4f ? u / p.y : 1f;
        }

        // 小卫星绕着弹体转。半径给得比弹体大一点，让它露在轮廓外面 ——
        // 压在弹身上就数不清有几颗了。
        void Mote(ref SpriteRenderer sr, string name, bool on, Color tint, float phase)
        {
            if (!on)
            {
                if (sr != null) sr.enabled = false;
                return;
            }
            if (sr == null) sr = Spawn(name);
            sr.enabled = true;
            sr.sprite = InkFx.Mote();
            Undistort(out float kx, out float ky);
            float t = Time.unscaledTime * 2.6f + phase;
            sr.transform.localPosition =
                new Vector3(Mathf.Cos(t) * 0.62f * kx, Mathf.Sin(t) * 0.62f * ky, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = new Vector3(0.22f * kx, 0.22f * ky, 1f);
            sr.sortingOrder = 9;
            InkFx.PaintSprite(sr, tint);
        }

        SpriteRenderer Spawn(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<SpriteRenderer>();
        }

        void Ribbon(ref TrailRenderer tr, ref bool was, string name, bool on, Color head, Color tail, float width, float time)
        {
            if (!on)
            {
                Quiet(ref tr, ref was);
                return;
            }
            if (tr == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                tr = go.AddComponent<TrailRenderer>();
                tr.numCapVertices = 5;
                tr.numCornerVertices = 4;
                tr.minVertexDistance = 0.02f;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                tr.sortingOrder = 4;
                tr.textureMode = LineTextureMode.Stretch;
                tr.alignment = LineAlignment.View;
                // 普通混合，不用 SoftMat 的柔光：柔光会让拖尾在纸上糊成一团亮雾，
                // 叠得越久越亮，看着就是「子弹过后留了脏」。
                Material src = InkFx.SpriteMat();
                if (src != null)
                {
                    var mat = new Material(src);
                    mat.mainTexture = InkFx.StreakTex();
                    tr.material = mat;
                }
            }
            var g = new Gradient();
            // 中间插一个 0.28 的键，让尾端提前掉透明。线性淡出会在末端留一截
            // 淡而可见的影子，就是那道「残留」。
            g.SetKeys(
                new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.92f, 0f),
                    new GradientAlphaKey(0.28f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            tr.colorGradient = g;
            tr.time = time;
            tr.widthMultiplier = width;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.55f, 0.42f), new Keyframe(1f, 0.04f));
            tr.enabled = true;
            tr.emitting = true;
            if (!was) tr.Clear();
            was = true;
        }

        static void Quiet(ref TrailRenderer tr, ref bool was)
        {
            if (tr != null)
            {
                tr.emitting = false;
                tr.Clear();
                tr.enabled = false;
            }
            was = false;
        }
    }
}
