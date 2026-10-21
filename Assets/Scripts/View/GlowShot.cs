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
        float _age, _trailAt, _bodyBitAt, _tailBitAt;

        static readonly Dictionary<string, Sprite> ArtCache = new Dictionary<string, Sprite>();
        static readonly List<Hit> Hits = new List<Hit>();

        public static Sprite Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (ArtCache.TryGetValue(name, out Sprite s) && s != null) return s;
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
                default: return null;
            }
        }

        public static bool HasBody(ShotFx fx)
        {
            string key = FormKey(fx);
            return key != null && ArtReady(key + "_shot");
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
                if (key != null && ArtReady(key + "_shot")) look = Make(key);
                return look.On;
            }
            if (m.Has(CardId.Break) && ArtReady("break_shot")) look = Make("break");
            else if (m.Has(CardId.Gather) && ArtReady("gather_shot")) look = Make("gather");
            else if (m.Has(CardId.Rebound) && ArtReady("rebound_shot")) look = Make("rebound");
            return look.On;
        }

        public static bool BodyOf(ShotMods m, out Look look) => BodyOf(m, ShotLook.Resolve(m), out look);

        static bool RingOn(ShotMods m, in Look body) =>
            m.Has(CardId.Break) && body.Key != "break" && ArtReady("break_bit0");

        static bool OrbitOn(ShotMods m, in Look body) =>
            m.Has(CardId.Gather) && body.Key != "gather" && ArtReady("gather_bit0");

        static bool TailOn(ShotMods m, in Look body) =>
            m.Has(CardId.Rebound) && body.Key != "rebound" && ArtReady("rebound_bit0");

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

            Body(body, born, t);
            Ring(RingOn(m, body), born, t);
            Orbit(OrbitOn(m, body), born, t);
            Tail(TailOn(m, body), b.Pos, dir, t);
            if (body.On && t >= _bodyBitAt)
            {
                _bodyBitAt = t + 0.05f;
                Sprite bit = Art(body.Key + "_shards");
                Vector2 side = new Vector2(-dir.y, dir.x) * Random.Range(-0.3f, 0.3f);
                Emit(bit, b.Pos - dir * 0.3f + side, -dir * Random.Range(0.8f, 1.6f) + side * 3f, 0.32f, 0.26f);
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

        void Body(in Look look, float born, float t)
        {
            Sprite sprite = look.On ? Art(look.Key + "_shot") : null;
            if (sprite == null)
            {
                _body.enabled = _halo.enabled = _hot.enabled = false;
                for (int i = 0; i < Ghosts; i++) _ghost[i].enabled = false;
                return;
            }
            float len = 1.0f * born;
            float sink = -len * 0.12f;
            Fit(_body, sprite, len, Color.white, sink);
            Soft(_halo, look.Halo, new Vector2(0.8f, 1.3f) * born * (1f + 0.06f * Mathf.Sin(t * 16f)), 0.40f, sink);
            Soft(_hot, Color.white, new Vector2(0.2f, 0.5f) * born, 0.5f, len * 0.1f);
            Ghost(sprite, len, sink);
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

        void Emit(Sprite sprite, Vector2 at, Vector2 vel, float life, float size, float spin = 0f)
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
            sr.color = Color.white;
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
                _bit[i].color = new Color(1f, 1f, 1f, 1f - u);
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

        void Ghost(Sprite sprite, float len, float sink)
        {
            Quaternion rot = transform.rotation;
            Vector3 back = rot * new Vector3(0f, sink, 0f);
            for (int i = 0; i < Ghosts; i++)
            {
                SpriteRenderer sr = _ghost[i];
                bool on = i + 1 < _trailN;
                sr.enabled = on;
                if (!on) continue;
                Vector2 at = _trail[i + 1];
                float k = 1f - 0.22f * (i + 1);
                sr.sprite = sprite;
                sr.transform.SetPositionAndRotation(new Vector3(at.x, at.y, 0f) + back, rot);
                SetWorldSize(sr.transform, len * k / Size(sprite));
                sr.color = new Color(1f, 1f, 1f, 0.42f * k);
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
            SpriteRenderer Body, Halo;
            readonly SpriteRenderer[] Bit = new SpriteRenderer[4];
            readonly Vector2[] Vel = new Vector2[4];
            float Age, Life, Scale;
            int N;

            public void Play(FxBurst fx, Sprite burst, Sprite shards, float scale)
            {
                Ensure();
                On = true;
                Age = 0f;
                Life = 0.32f;
                Scale = scale;
                Body.sprite = burst;
                Body.enabled = true;
                Body.color = Color.white;
                Body.transform.position = new Vector3(fx.Pos.x, fx.Pos.y, 0f);
                Body.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
                Body.transform.localScale = Vector3.one * 0.35f;
                Halo.enabled = true;
                Halo.color = fx.GlowHalo;
                Halo.transform.position = Body.transform.position;
                Halo.transform.localScale = Vector3.one * 0.4f;
                N = shards != null ? Bit.Length : 0;
                for (int i = 0; i < Bit.Length; i++)
                {
                    bool use = i < N;
                    Bit[i].enabled = use;
                    if (!use) continue;
                    Bit[i].sprite = shards;
                    Bit[i].color = Color.white;
                    Bit[i].transform.position = Body.transform.position;
                    Bit[i].transform.localScale = Vector3.one * (0.42f * scale / Size(shards));
                    float a = (i + Random.Range(-0.2f, 0.2f)) * Mathf.PI * 2f / Bit.Length;
                    Vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.6f, 3.2f) * scale;
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
                    Body.enabled = Halo.enabled = false;
                    for (int i = 0; i < Bit.Length; i++) Bit[i].enabled = false;
                    return;
                }
                float e = 1f - (1f - u) * (1f - u);
                float len = Mathf.Lerp(0.45f, 1.35f, e) * Scale;
                Body.transform.localScale = Vector3.one * (len / Size(Body.sprite));
                Body.color = new Color(1f, 1f, 1f, u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f);
                Color h = Halo.color;
                h.a = 0.55f * (1f - u);
                Halo.color = h;
                Halo.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.6f, e) * Scale;
                for (int i = 0; i < N; i++)
                {
                    Bit[i].transform.position += (Vector3)(Vel[i] * dt);
                    Vel[i] *= Mathf.Max(0f, 1f - 2.5f * dt);
                    Bit[i].color = new Color(1f, 1f, 1f, 1f - u);
                }
            }

            void Ensure()
            {
                if (Body != null) return;
                var root = new GameObject("glowHit");
                // 叠在墨溅（9~15）上面
                Body = Make(root.transform, "body", 15, false);
                Halo = Make(root.transform, "halo", 13, true);
                Halo.sprite = InkFx.SoftDisc();
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
