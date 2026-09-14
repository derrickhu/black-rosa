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
        SpriteRenderer _ghostA;
        SpriteRenderer _ghostB;
        TrailRenderer _body;
        TrailRenderer _trail;
        bool _bodyOn;
        bool _trailOn;

        public void Present(BulletActor b)
        {
            if (_core == null) _core = GetComponent<SpriteRenderer>();
            InkVfx.Ensure();

            ShotMods m = b.Mods;
            ShotView v = ShotLook.Resolve(m);

            float ang = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg - 90f;
            transform.SetPositionAndRotation(new Vector3(b.Pos.x, b.Pos.y, 0f), Quaternion.Euler(0f, 0f, ang));

            int heavyStar = m.Star(CardId.Heavy);
            bool pierce = m.Star(CardId.Pierce) > 0;

            // 视觉大小和碰撞半径解耦：重只是略大，不撑满格。
            float coreS = heavyStar > 0 ? 0.50f * GlyphTable.Get(CardId.Heavy).Size.At(heavyStar) : 0.50f;
            float breath = 1f + 0.018f * Mathf.Sin(Time.unscaledTime * 10f + b.Id * 1.7f);
            float sx = (pierce ? coreS * 0.68f : coreS) * breath;
            float sy = (pierce ? coreS * 1.32f : coreS) * (2f - breath);
            transform.localScale = new Vector3(sx, sy, 1f);

            InkVfx.Stop(_core);
            _core.enabled = false;

            // 体槽亮着就不要再露出底下那颗墨点，否则就是「彩色包黑心」。
            Sprite formSprite = InkVfx.Shot(v.Form.Fx);
            bool hasBody = formSprite != null;
            Sprite pellet = InkArt.Heap(InkShape.Circle, hasBody ? Color.white : InkTheme.Ink, 64);
            Child(ref _pellet, "pellet", pellet, heavyStar > 0 ? 0.24f : 0.20f, 6, Color.white);
            if (hasBody && _pellet != null) _pellet.enabled = false;

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
            Child(ref _glow, "glow", InkFx.SoftDisc(), glowS, 5, glow);
            Child(ref _hot, "hot", InkFx.SoftDisc(), 0.16f,
                7, v.Elements > 0 ? Fade(Color.white, 0.34f) : Color.clear);

            Child(ref _form, "form", formSprite, FormScale(v.Form.Fx), 7,
                formSprite == null ? Color.clear : Tint(v.Form.Tint));

            // 环槽：晕 / 雷 / 金，一圈转着的柔环。
            Sprite halo = v.Halo.On ? (InkVfx.Shot(v.Halo.Fx) ?? InkFx.SoftRing()) : null;
            Child(ref _halo, "halo", halo,
                1.44f + 0.05f * Mathf.Sin(Time.unscaledTime * 6f), 8,
                halo == null ? Color.clear : Fade(v.Halo.Tint, 0.52f));
            if (_halo != null && _halo.enabled)
                _halo.transform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 90f);

            // 绕槽：风 / 木，两团绕着弹体转的小影。分不占槽。
            Orbit(ref _orbitA, "orbitA", v.Orbit, 0f);
            Orbit(ref _orbitB, "orbitB", v.Orbit, Mathf.PI);

            bool fast = v.Trail.On && v.Trail.Fx == ShotFx.TrailAccel;
            Ghost(ref _ghostA, "ghostA", fast, -0.22f, 0.20f);
            Ghost(ref _ghostB, "ghostB", fast, -0.40f, 0.10f);

            // 主色带跟着混色走，尾槽再单独拉一条自己的色带。
            Color head = v.Elements > 0 ? Color.Lerp(v.Mixed, Color.white, 0.38f) : InkTheme.Bone;
            Color tail = v.Elements > 0 ? v.Mixed : InkTheme.BoneMid;
            float width = 0.12f + 0.02f * Mathf.Max(v.Form.Star, v.Elements);
            if (heavyStar > 0) width *= 1.08f;
            Ribbon(ref _body, ref _bodyOn, "body", true, head, tail, width, 0.22f);
            Ribbon(ref _trail, ref _trailOn, "trail", v.Trail.On,
                Color.Lerp(v.Trail.Tint, Color.white, 0.2f), v.Trail.Tint,
                v.Trail.Fx == ShotFx.TrailAccel ? 0.15f : 0.14f,
                v.Trail.Fx == ShotFx.TrailAccel ? 0.42f : 0.30f);
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

        void Orbit(ref SpriteRenderer sr, string name, SlotLook slot, float phase)
        {
            if (!slot.On)
            {
                if (sr != null) sr.enabled = false;
                return;
            }
            if (sr == null) sr = Spawn(name);
            float t = Time.unscaledTime * 3.4f + phase;
            sr.enabled = true;
            sr.sprite = InkFx.SoftDisc();
            sr.transform.localPosition = new Vector3(Mathf.Cos(t) * 0.42f, Mathf.Sin(t) * 0.24f, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one * 0.22f;
            sr.sortingOrder = 8;
            InkFx.PaintSoft(sr, Fade(slot.Tint, 0.46f));
        }

        void Ghost(ref SpriteRenderer sr, string name, bool on, float y, float alpha)
        {
            if (!on)
            {
                if (sr != null) sr.enabled = false;
                return;
            }
            if (sr == null) sr = Spawn(name);
            sr.enabled = true;
            sr.sprite = InkFx.SoftDisc();
            sr.transform.localPosition = new Vector3(0f, y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one * 0.30f;
            sr.sortingOrder = 4;
            InkFx.PaintSoft(sr, Fade(InkTheme.Accel, alpha));
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
                Material src = InkFx.SoftMat();
                if (src != null)
                {
                    var mat = new Material(src);
                    mat.mainTexture = InkFx.StreakTex();
                    tr.material = mat;
                }
            }
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                new[] { new GradientAlphaKey(0.92f, 0f), new GradientAlphaKey(0f, 1f) });
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
