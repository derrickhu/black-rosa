using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 第二幕的发光弹层。和 InkShot 一样按五槽拼，只是画法换成发光：
    //   体：千钧和四对新秘卷各一张发光弹体；体槽空着时，破 / 聚 / 返借自己的发光弹当体。
    //   环：破 —— 三片金粉晶绕着弹体抖。
    //   绕：聚 —— 一蓝一粉的光珠螺旋着往弹心收。
    //   尾：返 —— 身后甩出青金弯月。
    // 体槽被火、穿这类旧弹占着时只叠环 / 绕 / 尾，所以破 + 穿是一根穿针外面绕一圈晶片。
    // 弹体、晶片、光珠都是普通混合，只有外晕加色：整张加色叠在浅色战场上会冲成白。
    public sealed class GlowShot : MonoBehaviour
    {
        public struct Look
        {
            public string Key;
            public Color Halo;
            public bool On;
        }

        const int Ghosts = 3;
        const int Bits = 8;
        const int Shards = 3;
        const int Beads = 3;

        static readonly Color BreakHalo = InkTheme.Hex("FF2E88");
        static readonly Color GatherHalo = InkTheme.Hex("7A4DFF");
        static readonly Color ReboundHalo = InkTheme.Hex("2EE6C7");

        Transform _root;
        SpriteRenderer _halo, _body, _hot, _ringGlow, _orbitGlow, _tailGlow;
        readonly SpriteRenderer[] _ghost = new SpriteRenderer[Ghosts];
        readonly SpriteRenderer[] _shard = new SpriteRenderer[Shards];
        readonly SpriteRenderer[] _bead = new SpriteRenderer[Beads];
        readonly SpriteRenderer[] _bit = new SpriteRenderer[Bits];
        readonly float[] _bitAge = new float[Bits];
        readonly float[] _bitLife = new float[Bits];
        readonly float[] _bitRot = new float[Bits];
        readonly float[] _bitSpin = new float[Bits];
        readonly float[] _bitSize = new float[Bits];
        readonly Vector2[] _bitPos = new Vector2[Bits];
        readonly Vector2[] _bitVel = new Vector2[Bits];
        readonly Vector2[] _trail = new Vector2[Ghosts + 1];
        int _trailN, _bid = -1;
        float _age, _trailAt, _bodyBitAt, _tailBitAt, _emberAt;
        MaterialPropertyBlock _mpb;

        static readonly Dictionary<string, Sprite> ArtCache = new Dictionary<string, Sprite>();
        static readonly List<Hit> Hits = new List<Hit>();

        public static Sprite Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (ArtCache.TryGetValue(name, out Sprite s) && s != null) return s;
            if (name == "sweep_shot" || name == "heavysweep_shot")
                s = InkSprites.Load("Vfx/sweep_arc");
            else if (name == "bladestorm_shot")
                s = InkSprites.Load("Vfx/riposte_shot_00") ?? InkSprites.Load("Vfx/riposte_shot");
            else
                s = InkSprites.Load("Vfx/" + name);
            ArtCache[name] = s;
            return s;
        }

        public static bool ArtReady(string name) => Art(name) != null;

        // 有发光弹体图的体槽形。ShotLook 用它决定新秘卷能不能抢体槽。
        public static string FormKey(ShotFx fx)
        {
            switch (fx)
            {
                case ShotFx.FormCrush: return "crush";
                case ShotFx.FormCrumble: return "crumble";
                case ShotFx.FormCollapse: return "collapse";
                case ShotFx.FormWhirl: return "whirl";
                case ShotFx.FormRiposte: return "riposte";
                case ShotFx.FormGhostFire: return "ghostfire";
                case ShotFx.FormSoul: return "soul";
                case ShotFx.FormFireWheel: return "firewheel";
                case ShotFx.FormCloudArrow: return "cloudarrow";
                case ShotFx.FormNeedle: return "needle";
                case ShotFx.FormSweep: return "sweep";
                case ShotFx.FormBladeStorm: return "bladestorm";
                case ShotFx.FormHeavySweep: return "heavysweep";
                default: return null;
            }
        }

        public static bool HasBody(ShotFx fx)
        {
            string key = FormKey(fx);
            if (key == null) return false;
            return ArtReady(key + "_shot_00") || ArtReady(key + "_shot");
        }

        // 图鉴和飞行都播这一套帧。没有帧时退回单张。
        public static Sprite Poster(string key)
        {
            int n = InkVfx.CountFrames(key + "_shot");
            if (n > 0)
            {
                Sprite[] frames = InkVfx.Frames(key + "_shot", n);
                if (frames != null && frames[0] != null) return frames[0];
            }
            return Art(key + "_shot");
        }

        public static Sprite Loop(string key, float t, int id)
        {
            int n = InkVfx.CountFrames(key + "_shot");
            if (n > 0)
            {
                Sprite[] frames = InkVfx.Frames(key + "_shot", n);
                if (frames != null && frames[0] != null)
                {
                    float fps = key == "firewheel" ? 16f : key == "ghostfire" || key == "soul" ? 10f : 12f;
                    int step = Mathf.FloorToInt(t * fps + id * 0.37f);
                    for (int i = 0; i < n; i++)
                    {
                        Sprite s = frames[((step + i) % n + n) % n];
                        if (s != null) return s;
                    }
                }
            }
            return Art(key + "_shot");
        }

        // 卡通序列帧自己带颜色，不再套大光晕和残影。
        static bool Toon(string key) =>
            key == "firewheel" || key == "cloudarrow" || key == "needle";

        // 卡通硬边平涂序列帧：颜色与深色描边都在图里，普通混合，不再垫冲白的大柔光晕与白热点。
        static bool Painted(string key) =>
            key == "crumble" || key == "collapse" || key == "whirl" || key == "riposte"
            || key == "ghostfire" || key == "soul" || key == "crush"
            || key == "break" || key == "gather" || key == "rebound" || key == "shadow"
            || key == "sweep" || key == "heavysweep" || key == "bladestorm";

        // 是否沿飞行轨迹喷射对应色系的硬边卡通碎晶簇（*_shards.png）。
        static bool DropsShards(string key) =>
            key == "crumble" || key == "riposte" || key == "crush"
            || key == "break" || key == "gather" || key == "rebound";

        // 第二幕新字和秘卷比旧的发光弹更大、外晕更亮。
        static bool Premium(string key)
        {
            switch (key)
            {
                case "ghostfire":
                case "soul":
                case "firewheel":
                case "cloudarrow":
                case "needle":
                case "shadow":
                    return true;
                default:
                    return false;
            }
        }

        static float BodyLen(string key)
        {
            switch (key)
            {
                case "ghostfire": return 1.06f;
                case "soul": return 1.04f;
                case "crumble": return 1.22f;
                case "collapse":
                case "whirl": return 1.16f;
                case "riposte": return 1.36f;
                case "firewheel":
                    return 1.22f;
                case "cloudarrow":
                    return 1.3f;
                case "needle":
                    return 1.44f;
                case "crush":
                    return 1.24f;
                case "break":
                case "gather":
                case "rebound":
                    return 1.16f;
                case "shadow":
                    return 0.96f;
                default:
                    return 1f;
            }
        }

        static Color HaloOf(string key)
        {
            switch (key)
            {
                case "crush": return InkTheme.Hex("FF2A3C");
                case "crumble": return InkTheme.Hex("FF3B1F");
                case "collapse": return InkTheme.Hex("9B4DFF");
                case "whirl": return InkTheme.Hex("3EE0FF");
                case "riposte": return InkTheme.Hex("4D8CFF");
                case "ghostfire": return InkTheme.Hex("FF6A1A");
                case "soul": return InkTheme.Hex("C44DFF");
                case "firewheel": return InkTheme.Hex("FF8A1A");
                case "cloudarrow": return InkTheme.Hex("FFD24A");
                case "needle": return InkTheme.Hex("7EE7FF");
                case "shadow": return InkTheme.Hex("C9D4FF");
                case "break": return BreakHalo;
                case "gather": return GatherHalo;
                default: return ReboundHalo;
            }
        }

        static Look Make(string key) => new Look { Key = key, Halo = HaloOf(key), On = true };

        // 体槽：千钧 / 新秘卷赢了体槽就画它的发光体；体槽空着才轮到破、聚、返借自己的发光弹。
        public static bool BodyOf(ShotMods m, ShotView v, out Look look)
        {
            look = default;
            if (m == null) return false;
            if (v.Form.On)
            {
                string key = FormKey(v.Form.Fx);
                if (key != null && HasBody(v.Form.Fx)) look = Make(key);
                return look.On;
            }
            if (m.Has(CardId.Break) && ArtReady("break_shot")) look = Make("break");
            else if (m.Has(CardId.Gather) && ArtReady("gather_shot")) look = Make("gather");
            else if (m.Has(CardId.Rebound) && ArtReady("rebound_shot")) look = Make("rebound");
            return look.On;
        }

        public static bool BodyOf(ShotMods m, out Look look) => BodyOf(m, ShotLook.Resolve(m), out look);

        static bool RingOn(ShotMods m, in Look body) =>
            !Toon(body.Key) && m.Has(CardId.Break) && body.Key != "break" && ArtReady("break_bit0");

        static bool OrbitOn(ShotMods m, in Look body) =>
            !Toon(body.Key) && m.Has(CardId.Gather) && body.Key != "gather" && ArtReady("gather_bit0");

        static bool TailOn(ShotMods m, in Look body) =>
            !Toon(body.Key) && m.Has(CardId.Rebound) && body.Key != "rebound" && ArtReady("rebound_bit0");

        public static bool Wants(ShotMods m)
        {
            if (m == null) return false;
            BodyOf(m, out Look body);
            return body.On || RingOn(m, body) || OrbitOn(m, body) || TailOn(m, body);
        }

        // 命中：体是发光体，或者赢下命中层的就是破 / 聚，整张换发光爆点。
        // 命中层被火、冰这些别的字赢走时，原来的墨溅照播，发光爆点缩小叠在上面 —— 两个字都看得见。
        public static bool HitOf(ShotMods m, CardId winner, out Look look, out bool overlay)
        {
            overlay = false;
            if (BodyOf(m, out look) && look.Key != "break" && look.Key != "gather" && look.Key != "rebound")
                return true;
            look = default;
            if (m.Has(CardId.Break) && ArtReady("break_burst")) look = Make("break");
            else if (m.Has(CardId.Gather) && ArtReady("gather_burst")) look = Make("gather");
            else if (m.Has(CardId.Rebound) && ArtReady("rebound_burst")) look = Make("rebound");
            if (!look.On) return false;
            overlay = winner != CardId.None && winner != CardId.Break && winner != CardId.Gather;
            return true;
        }

        // InkShot 先摆好本节点（位置、朝向、为穿压扁的缩放），这里在它下面挂一层把缩放抵掉。
        public void Sync(BulletActor b)
        {
            Ensure();
            if (b.Id != _bid)
            {
                _bid = b.Id;
                _age = 0f;
                _trailN = 0;
                _trailAt = _bodyBitAt = _tailBitAt = 0f;
                for (int i = 0; i < Bits; i++) _bit[i].enabled = false;
            }
            ShotMods m = b.Mods;
            ShotView v = ShotLook.Resolve(m);
            BodyOf(m, v, out Look body);
            if (!body.On && b.Ghost && ArtReady("shadow_shot_00")) body = Make("shadow");
            _age += Time.deltaTime;

            Vector3 p = transform.localScale;
            _root.localPosition = Vector3.zero;
            _root.localRotation = Quaternion.identity;
            _root.localScale = new Vector3(
                Mathf.Abs(p.x) > 1e-4f ? 1f / p.x : 1f,
                Mathf.Abs(p.y) > 1e-4f ? 1f / p.y : 1f, 1f);

            Vector2 dir = b.Vel.sqrMagnitude > 1e-4f ? b.Vel.normalized : Vector2.up;
            Record(b.Pos);
            float born = Ease.OutBack(Mathf.Clamp01(_age / 0.12f));
            float t = _age;

            Body(body, born, t, b.Id);
            if (body.Key == "ghostfire" || body.Key == "soul" || body.Key == "shadow"
                || body.Key == "firewheel" || body.Key == "needle"
                || body.Key == "collapse" || body.Key == "whirl")
                Ember(body.Key, b.Pos, dir, t);
            Ring(RingOn(m, body), born, t);
            Orbit(OrbitOn(m, body), born, t);
            Tail(TailOn(m, body), b.Pos, dir, t);
            if (body.On && DropsShards(body.Key) && t >= _bodyBitAt)
            {
                _bodyBitAt = t + 0.055f;
                Sprite bit = Art(body.Key + "_shards");
                Vector2 side = new Vector2(-dir.y, dir.x) * Random.Range(-0.28f, 0.28f);
                Emit(bit, b.Pos - dir * 0.32f + side, -dir * Random.Range(0.9f, 1.7f) + side * 2.6f, 0.30f, 0.24f,
                    Random.Range(-360f, 360f));
            }
            StepBits();
        }

        public void Hide()
        {
            if (_root == null) return;
            _halo.enabled = _body.enabled = _hot.enabled = false;
            _ringGlow.enabled = _orbitGlow.enabled = _tailGlow.enabled = false;
            for (int i = 0; i < Ghosts; i++) _ghost[i].enabled = false;
            for (int i = 0; i < Shards; i++) _shard[i].enabled = false;
            for (int i = 0; i < Beads; i++) _bead[i].enabled = false;
            for (int i = 0; i < Bits; i++) _bit[i].enabled = false;
        }

        void Body(in Look look, float born, float t, int id)
        {
            _body.SetPropertyBlock(null);
            Sprite sprite = look.On ? Loop(look.Key, t, id) : null;
            if (sprite == null)
            {
                _body.enabled = _halo.enabled = _hot.enabled = false;
                for (int i = 0; i < Ghosts; i++) _ghost[i].enabled = false;
                return;
            }
            bool jewel = look.Key == "ghostfire" || look.Key == "soul";
            bool wisp = look.Key == "ghostfire" || look.Key == "shadow";
            bool toon = Toon(look.Key);
            bool painted = Painted(look.Key);
            float breathe = look.Key == "ghostfire" || look.Key == "soul" ? 1f + 0.05f * Mathf.Sin(t * 7f + id)
                : wisp ? 1f + 0.05f * Mathf.Sin(t * 15f + id)
                : look.Key == "crumble" || look.Key == "crush" ? 1f + 0.05f * Mathf.Sin(t * 14f + id)
                : look.Key == "needle" ? 1f + 0.05f * Mathf.Sin(t * 8f + id) : 1f;
            float len = BodyLen(look.Key) * born * breathe;
            float sink = toon || painted ? 0f : -len * 0.12f;
            bool premium = Premium(look.Key);
            // 全部采用硬边平涂普通混合，影分身用 0.82 半透明体现虚影，同时完整保留深色描边。
            InkFx.PaintSprite(_body, Color.white);
            Color tint = look.Key == "shadow" ? new Color(1f, 1f, 1f, 0.82f) : Color.white;
            float shine = look.Key == "ghostfire" ? 0.55f
                : look.Key == "shadow" ? 0.48f
                : jewel ? 0.62f : premium ? 1.25f : 1f;
            float haloA = look.Key == "ghostfire" ? 0.22f
                : look.Key == "shadow" ? 0.2f
                : jewel ? 0.26f : premium ? 0.55f : 0.40f;
            Fit(_body, sprite, len, tint, sink);
            if (look.Key == "shadow")
            {
                float wobble = Mathf.Sin(t * 10f + id);
                Vector3 at = _body.transform.localPosition;
                at.x += wobble * 0.04f;
                _body.transform.localPosition = at;
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, wobble * 5f);
            }
            else if (look.Key == "firewheel")
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 420f);
            else if (look.Key == "whirl")
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 340f);
            else if (look.Key == "collapse")
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, t * 240f);
            else if (look.Key == "ghostfire" || look.Key == "soul")
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 8f + id) * 4f);
            // 发光序列自己带着光。再垫大光晕、白热点和残影，浅地上会糊、会冲白。
            if (toon || painted)
            {
                _halo.enabled = false;
                _hot.enabled = false;
                for (int i = 0; i < Ghosts; i++) _ghost[i].enabled = false;
                return;
            }
            Soft(_halo, look.Halo, new Vector2(0.85f, 1.4f) * born * shine * (1f + 0.06f * Mathf.Sin(t * 16f)),
                haloA, sink);
            float hotA = look.Key == "ghostfire" ? 0f
                : look.Key == "shadow" ? 0.12f
                : jewel ? 0.22f : premium ? 0.32f : 0.5f;
            if (hotA <= 0f) _hot.enabled = false;
            else Soft(_hot, Color.white, new Vector2(0.16f, 0.38f) * born, hotA, len * 0.18f);
            Ghost(sprite, len, sink, false, 0.42f);
        }

        // 破：三片晶绕着弹体，半径和角度都按时间抖，读作「甲在碎」。
        void Ring(bool on, float born, float t)
        {
            Sprite shard = on ? Art("break_bit0") : null;
            _ringGlow.enabled = shard != null;
            for (int i = 0; i < Shards; i++)
            {
                SpriteRenderer sr = _shard[i];
                sr.enabled = shard != null;
                if (shard == null) continue;
                float a = t * 2.2f + i * Mathf.PI * 2f / Shards;
                float jit = Mathf.Sin(t * 23f + i * 3.1f);
                float r = (0.40f + 0.04f * jit) * born;
                Vector2 at = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                sr.sprite = shard;
                sr.transform.localPosition = new Vector3(at.x, at.y, 0f);
                // 晶尖朝外
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg - 90f + jit * 12f);
                sr.transform.localScale = Vector3.one * (0.38f * born / Size(shard));
                sr.color = Color.white;
            }
            if (shard != null)
                Soft(_ringGlow, BreakHalo, Vector2.one * 1.05f * born, 0.45f, 0f);
        }

        // 聚：光珠从外圈螺旋着收进弹心，收到底淡掉再从外圈出来。
        void Orbit(bool on, float born, float t)
        {
            Sprite blue = on ? Art("gather_bit0") : null;
            Sprite pink = on ? Art("gather_bit1") : null;
            if (pink == null) pink = blue;
            _orbitGlow.enabled = blue != null;
            for (int i = 0; i < Beads; i++)
            {
                SpriteRenderer sr = _bead[i];
                sr.enabled = blue != null;
                if (blue == null) continue;
                float u = Mathf.Repeat(t * 1.4f + i / (float)Beads, 1f);
                float r = Mathf.Lerp(0.58f, 0.14f, u) * born;
                float a = -t * 5f + i * Mathf.PI * 2f / Beads + u * 3f;
                sr.sprite = i % 2 == 0 ? blue : pink;
                sr.transform.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                sr.transform.localRotation = Quaternion.identity;
                sr.transform.localScale = Vector3.one * (Mathf.Lerp(0.27f, 0.14f, u) * born / Size(sr.sprite));
                float fade = u < 0.15f ? u / 0.15f : (u > 0.8f ? (1f - u) / 0.2f : 1f);
                sr.color = new Color(1f, 1f, 1f, fade);
            }
            if (blue != null)
                Soft(_orbitGlow, GatherHalo, Vector2.one * 0.95f * born, 0.38f, 0f);
        }

        // 返：身后甩出青金弯月，往两边翻着飞，读作「还会回来」。
        void Tail(bool on, Vector2 pos, Vector2 dir, float t)
        {
            Sprite moon = on ? Art("rebound_bit0") : null;
            _tailGlow.enabled = moon != null;
            if (moon == null) return;
            Soft(_tailGlow, ReboundHalo, new Vector2(0.36f, 1.0f), 0.42f, -0.5f);
            if (t < _tailBitAt) return;
            _tailBitAt = t + 0.07f;
            float side = (Mathf.FloorToInt(t / 0.07f) % 2 == 0) ? 1f : -1f;
            Vector2 n = new Vector2(-dir.y, dir.x) * side;
            Emit(moon, pos - dir * 0.35f + n * 0.1f, n * Random.Range(1.0f, 1.6f) - dir * 0.6f, 0.34f, 0.36f,
                side * 540f);
        }

        // 风火轮甩橙红/翡翠双色火星，定海针甩寒冰/金铜微晶，鬼火/失魂甩幽冥火星。
        void Ember(string key, Vector2 pos, Vector2 dir, float t)
        {
            if (t < _emberAt) return;
            if (key == "firewheel")
            {
                _emberAt = t + 0.032f;
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector2 rim = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 tang = new Vector2(rim.y, -rim.x);
                bool fire = Random.value < 0.6f;
                Color fwCol = fire ? InkTheme.Hex("FF7A1F") : InkTheme.Hex("2AD99B");
                Emit(InkFx.Mote(), pos + rim * 0.42f, tang * Random.Range(1.4f, 2.4f) - dir * 0.8f,
                    Random.Range(0.18f, 0.30f), fire ? 0.12f : 0.10f, Random.Range(-360f, 360f), fwCol);
                return;
            }
            if (key == "needle")
            {
                _emberAt = t + 0.045f;
                Vector2 sideN = new Vector2(-dir.y, dir.x);
                float s = Random.value < 0.5f ? -1f : 1f;
                bool ice = Random.value < 0.68f;
                Color ndCol = ice ? InkTheme.Hex("6EE2FF") : InkTheme.Hex("F0B43C");
                Emit(InkFlowArt.ShardPoly(), pos - dir * Random.Range(0.05f, 0.35f) + sideN * s * 0.14f,
                    -dir * Random.Range(0.8f, 1.5f) + sideN * s * Random.Range(0.5f, 1.1f),
                    Random.Range(0.22f, 0.34f), ice ? 0.13f : 0.10f, Random.Range(-420f, 420f), ndCol);
                return;
            }
            if (key == "collapse" || key == "whirl")
            {
                _emberAt = t + 0.04f;
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector2 rim = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                bool isWhirl = key == "whirl";
                Vector2 tang = isWhirl ? new Vector2(rim.y, -rim.x) : new Vector2(-rim.y, rim.x);
                Color vCol = isWhirl
                    ? (Random.value < 0.55f ? InkTheme.Hex("2AD99B") : InkTheme.Hex("5EE6FF"))
                    : (Random.value < 0.55f ? InkTheme.Hex("A855FF") : InkTheme.Hex("F2D6FF"));
                Emit(InkFx.Mote(), pos + rim * 0.36f, tang * Random.Range(1.2f, 2.1f) - dir * 0.9f,
                    Random.Range(0.18f, 0.28f), 0.10f, Random.Range(-320f, 320f), vCol);
                return;
            }
            bool shade = key == "shadow";
            _emberAt = t + (shade ? 0.07f : key == "soul" ? 0.055f : 0.04f);
            Sprite mote = InkFx.Mote();
            Vector2 side = new Vector2(-dir.y, dir.x);
            float flick = Random.Range(-1f, 1f);
            bool warm = key == "ghostfire";
            Color c = shade
                ? new Color(0.55f, 0.32f, 1f, 0.65f)
                : warm
                ? (Random.value < 0.45f ? new Color(1f, 0.42f, 0.12f, 1f) : new Color(0.72f, 0.32f, 1f, 1f))
                : (Random.value < 0.5f ? new Color(0.72f, 0.28f, 1f, 1f) : new Color(1f, 0.46f, 0.72f, 1f));
            Vector2 at = pos - dir * Random.Range(0.02f, 0.16f) + side * flick * (shade ? 0.06f : 0.1f);
            Vector2 vel = -dir * Random.Range(0.35f, 0.9f) + side * flick * Random.Range(0.4f, shade ? 0.8f : 1.3f);
            if (warm) vel.y += Random.Range(0.2f, 0.7f);
            Emit(mote, at, vel, Random.Range(0.18f, 0.32f), shade ? 0.07f : warm ? 0.11f : 0.09f, Random.Range(-240f, 240f), c);
        }

        void Emit(Sprite sprite, Vector2 at, Vector2 vel, float life, float size, float spin = 0f, Color? tint = null)
        {
            if (sprite == null) return;
            int slot = -1;
            float oldest = -1f;
            for (int i = 0; i < Bits; i++)
            {
                if (!_bit[i].enabled) { slot = i; break; }
                if (_bitAge[i] / _bitLife[i] > oldest)
                {
                    oldest = _bitAge[i] / _bitLife[i];
                    slot = i;
                }
            }
            SpriteRenderer sr = _bit[slot];
            sr.enabled = true;
            sr.sprite = sprite;
            sr.color = tint ?? Color.white;
            _bitPos[slot] = at;
            _bitVel[slot] = vel;
            _bitAge[slot] = 0f;
            _bitLife[slot] = life;
            _bitRot[slot] = spin != 0f ? Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg : Random.Range(0f, 360f);
            _bitSpin[slot] = spin != 0f ? spin : Random.Range(-240f, 240f);
            _bitSize[slot] = size;
            Place(slot, 1f);
        }

        void StepBits()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < Bits; i++)
            {
                if (!_bit[i].enabled) continue;
                _bitAge[i] += dt;
                float u = _bitAge[i] / _bitLife[i];
                if (u >= 1f)
                {
                    _bit[i].enabled = false;
                    continue;
                }
                _bitPos[i] += _bitVel[i] * dt;
                _bitVel[i] *= Mathf.Max(0f, 1f - 3f * dt);
                _bitRot[i] += _bitSpin[i] * dt;
                Place(i, 1f - u * 0.4f);
                Color bit = _bit[i].color;
                bit.a = 1f - u;
                _bit[i].color = bit;
            }
        }

        void Place(int i, float k)
        {
            Transform tr = _bit[i].transform;
            tr.SetPositionAndRotation(new Vector3(_bitPos[i].x, _bitPos[i].y, 0f), Quaternion.Euler(0f, 0f, _bitRot[i]));
            SetWorldSize(tr, _bitSize[i] * k / Size(_bit[i].sprite));
        }

        // 子节点挂在会被压扁的弹节点下面，世界大小要除掉父级的缩放。
        void SetWorldSize(Transform tr, float s)
        {
            Vector3 l = _root.lossyScale;
            float lx = Mathf.Abs(l.x) > 1e-4f ? l.x : 1f;
            float ly = Mathf.Abs(l.y) > 1e-4f ? l.y : 1f;
            tr.localScale = new Vector3(s / lx, s / ly, 1f);
        }

        void Record(Vector2 pos)
        {
            if (_age < _trailAt) return;
            _trailAt = _age + 0.03f;
            for (int i = Mathf.Min(_trailN, Ghosts); i > 0; i--) _trail[i] = _trail[i - 1];
            _trail[0] = pos;
            _trailN = Mathf.Min(_trailN + 1, Ghosts + 1);
        }

        void Ghost(Sprite sprite, float len, float sink, bool add, float alpha)
        {
            Quaternion rot = transform.rotation;
            Vector3 back = rot * new Vector3(0f, sink, 0f);
            for (int i = 0; i < Ghosts; i++)
            {
                SpriteRenderer sr = _ghost[i];
                bool on = i + 1 < _trailN;
                sr.enabled = on;
                if (!on) continue;
                if (add) InkFx.PaintAdd(sr, Color.white);
                else InkFx.PaintSprite(sr, Color.white);
                Vector2 at = _trail[i + 1];
                float k = 1f - 0.22f * (i + 1);
                sr.sprite = sprite;
                sr.transform.SetPositionAndRotation(new Vector3(at.x, at.y, 0f) + back, rot);
                SetWorldSize(sr.transform, len * k / Size(sprite));
                sr.color = new Color(1f, 1f, 1f, alpha * k);
            }
        }

        static void Fit(SpriteRenderer sr, Sprite sprite, float len, Color tint, float y)
        {
            sr.enabled = true;
            sr.sprite = sprite;
            sr.transform.localPosition = new Vector3(0f, y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one * (len / Size(sprite));
            sr.color = tint;
        }

        static void Soft(SpriteRenderer sr, Color c, Vector2 size, float a, float y)
        {
            sr.enabled = true;
            sr.transform.localPosition = new Vector3(0f, y, 0f);
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
            c.a = Mathf.Clamp01(a);
            sr.color = c;
        }

        static float Size(Sprite s) => s != null ? Mathf.Max(0.01f, Mathf.Max(s.bounds.size.x, s.bounds.size.y)) : 1f;

        void Ensure()
        {
            if (_root != null) return;
            _root = new GameObject("glow").transform;
            _root.SetParent(transform, false);
            // 排序：外晕 4 在色带下，残影 6，体 8 压过 InkShot 的墨点 / 体图（6~7），环绕小件 9~10。
            _halo = Layer("halo", InkFx.SoftDisc(), 4, true);
            _ringGlow = Layer("ringGlow", InkFx.SoftDisc(), 4, true);
            _orbitGlow = Layer("orbitGlow", InkFx.SoftDisc(), 4, true);
            _tailGlow = Layer("tailGlow", InkFx.SoftDisc(), 4, true);
            for (int i = 0; i < Ghosts; i++) _ghost[i] = Layer("ghost" + i, null, 6, false);
            _body = Layer("body", null, 8, false);
            _hot = Layer("hot", InkFx.SoftDisc(), 9, true);
            for (int i = 0; i < Shards; i++) _shard[i] = Layer("shard" + i, null, 10, false);
            for (int i = 0; i < Beads; i++) _bead[i] = Layer("bead" + i, null, 10, false);
            for (int i = 0; i < Bits; i++) _bit[i] = Layer("bit" + i, null, 9, false);
        }

        SpriteRenderer Layer(string name, Sprite sprite, int order, bool add)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (add) InkFx.PaintAdd(sr, Color.white);
            else InkFx.PaintSprite(sr, Color.white);
            sr.enabled = false;
            return sr;
        }

        // ---- 命中 ----

        public static void PlayHit(FxBurst fx)
        {
            Sprite burst = Art(fx.GlowArt);
            if (burst == null) return;
            Hit h = null;
            for (int i = 0; i < Hits.Count; i++)
                if (!Hits[i].On) { h = Hits[i]; break; }
            if (h == null)
            {
                h = new Hit();
                Hits.Add(h);
            }
            h.Play(fx, burst, Art(fx.GlowShards), fx.GlowOverlay ? 0.7f : 1f);
        }

        public static void TickHits()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < Hits.Count; i++)
                if (Hits[i].On) Hits[i].Tick(dt);
        }

        sealed class Hit
        {
            public bool On;
            SpriteRenderer Body, Ring;
            readonly SpriteRenderer[] Bit = new SpriteRenderer[6];
            readonly Vector2[] Vel = new Vector2[6];
            readonly float[] Sz = new float[6];
            MaterialPropertyBlock _mpb;
            Color _core, _mid, _rim;
            float Age, Life, Scale, Phase;

            public void Play(FxBurst fx, Sprite burst, Sprite shards, float scale)
            {
                Ensure();
                On = true;
                Age = 0f;
                Life = 0.30f;
                Scale = scale;
                Phase = Random.Range(0f, 10f);
                _mid = fx.GlowHalo.a > 0.01f ? fx.GlowHalo : InkTheme.Hex("FF5C8A");
                _core = Color.Lerp(_mid, Color.white, 0.82f);
                _rim = Color.Lerp(_mid, Color.black, 0.72f);

                Vector3 pos = new Vector3(fx.Pos.x, fx.Pos.y, 0f);
                Body.sprite = InkFlowArt.VortexMask();
                Body.enabled = true;
                Body.transform.position = pos;
                Body.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                Ring.sprite = InkFlowArt.RingMask();
                Ring.enabled = true;
                Ring.transform.position = pos;
                Ring.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                Sprite mote = InkFx.Mote();
                for (int i = 0; i < Bit.Length; i++)
                {
                    Bit[i].enabled = true;
                    Bit[i].sprite = mote;
                    Bit[i].color = i % 3 == 0 ? _core : _mid;
                    Bit[i].transform.position = pos;
                    float a = (i + Random.Range(-0.4f, 0.4f)) * Mathf.PI * 2f / Bit.Length;
                    Vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.4f, 5.0f) * scale;
                    Sz[i] = Random.Range(0.08f, 0.13f) * scale;
                }
            }

            public void Tick(float dt)
            {
                if (Body == null)
                {
                    On = false;
                    return;
                }
                Age += dt;
                float u = Mathf.Clamp01(Age / Life);
                if (u >= 1f)
                {
                    On = false;
                    Body.enabled = Ring.enabled = false;
                    for (int i = 0; i < Bit.Length; i++) Bit[i].enabled = false;
                    return;
                }
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                float pop = Ease.OutBack(Mathf.Clamp01(u / 0.28f));
                float cut = u < 0.22f ? 0f : Mathf.SmoothStep(0f, 1f, (u - 0.22f) / 0.78f);
                float alpha = u < 0.85f ? 1f : 1f - (u - 0.85f) / 0.15f;

                float bodyLen = Mathf.LerpUnclamped(0.28f, 1.05f, pop) * Scale;
                Body.transform.localScale = Vector3.one * (bodyLen / Size(Body.sprite));
                InkFlowArt.Apply(Body, _mpb, _core, _mid, _rim,
                    new Vector4(2.2f, -2.6f, 1f, 0.7f), new Vector4(cut, 0.22f, Phase, 1f), alpha);

                float ringLen = Mathf.Lerp(0.32f, 1.48f, Ease.OutCubic(u)) * Scale;
                Ring.transform.localScale = Vector3.one * (ringLen / Size(Ring.sprite));
                InkFlowArt.Apply(Ring, _mpb, _core, _mid, _rim,
                    new Vector4(-1.8f, -1.8f, 1f, 0.4f), new Vector4(cut, 0.16f, Phase + 2.3f, 1f), alpha);

                for (int i = 0; i < Bit.Length; i++)
                {
                    Bit[i].transform.position += (Vector3)(Vel[i] * dt);
                    Vel[i] *= Mathf.Exp(-5.5f * dt);
                    float spd = Vel[i].magnitude;
                    float stretch = 1f + spd * 0.22f;
                    float squash = 1f / Mathf.Sqrt(stretch);
                    float s = Sz[i] * (1f - u * 0.65f);
                    Bit[i].transform.localScale = new Vector3(s * squash, s * stretch, 1f);
                    if (spd > 0.05f)
                    {
                        float ang = Mathf.Atan2(Vel[i].y, Vel[i].x) * Mathf.Rad2Deg - 90f;
                        Bit[i].transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                    }
                    Color c = Bit[i].color;
                    c.a = u < 0.45f ? 1f : 1f - (u - 0.45f) / 0.55f;
                    Bit[i].color = c;
                }
            }

            void Ensure()
            {
                if (Body != null) return;
                var root = new GameObject("glowHit");
                // 叠在墨溅（9~15）上面
                Body = Make(root.transform, "body", 15, false);
                Ring = Make(root.transform, "ring", 14, false);
                for (int i = 0; i < Bit.Length; i++)
                    Bit[i] = Make(root.transform, "bit" + i, 16, false);
            }

            static SpriteRenderer Make(Transform parent, string name, int order, bool add)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = order;
                if (add) InkFx.PaintAdd(sr, Color.white);
                else InkFx.PaintSprite(sr, Color.white);
                sr.enabled = false;
                return sr;
            }
        }
    }
}
