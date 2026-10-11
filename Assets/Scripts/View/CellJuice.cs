using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 格子手感。世界往 Pulses 里丢「这一格发生了什么」，这里接住：
    // 字本身的弹跳、闪光、晃动由 BattleView 每帧来取；环、星、灰、残影、成词光柱是一次性的小件，自己养着。
    // 全走 unscaled：放字时战斗是暂停的，顿帧时世界也是停的，手感不能跟着停。
    public sealed class CellJuice
    {
        const int N = GameConstants.Columns * GameConstants.Rows;
        const float Cw = GameConstants.CellWidth;
        // 落下的字要等前一个动作（旧字甩走）让开一点再进来；成词要等两半都落稳。
        const float SwapLag = 0.07f;
        const float WordLag = 0.28f;

        struct Anim
        {
            public CellBeat Beat;
            public float Age;
            public float Delay;
            public bool On;
            public Color Tint;
        }

        struct Bit
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel;
            public Vector2 Size0, Size1;
            public float Life, Max, Delay;
            public float Rot, Spin, Drag, Grav;
            public Color Col;
            public bool Ease;
        }

        struct Later
        {
            public float At;
            public CellBeat Beat;
            public int Star;
        }

        readonly Transform _root;
        readonly Anim[] _anim = new Anim[N];
        readonly float[] _badge = new float[N];
        readonly float[] _nudge = new float[N];
        readonly List<Bit> _bits = new List<Bit>();
        readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();
        readonly List<Later> _later = new List<Later>();
        Sprite _spark;

        public CellJuice(Transform root)
        {
            _root = root;
        }

        static int Index(int col, int row) => col * GameConstants.Rows + row;

        public void Consume(BattleWorld w)
        {
            bool drop = false, swap = false, word = false, unword = false, deny = false;
            int upStar = 0;
            for (int n = 0; n < w.Pulses.Count; n++)
            {
                CellPulse p = w.Pulses[n];
                int i = Index(p.Col, p.Row);
                Vector2 at = FieldLayout.CellPos(p.Col, p.Row);
                switch (p.Beat)
                {
                    case CellBeat.Drop:
                        Start(i, CellBeat.Drop, 0f, Color.clear);
                        Land(at, p.Card, 0.06f);
                        drop = true;
                        break;
                    case CellBeat.Replace:
                        Start(i, CellBeat.Replace, SwapLag, Color.clear);
                        Ghost(at, p.Old);
                        Land(at, p.Card, SwapLag + 0.06f);
                        swap = true;
                        break;
                    case CellBeat.Upgrade:
                        Start(i, CellBeat.Upgrade, 0f, InkTheme.GoldHi);
                        _badge[i] = 1f;
                        Ring(at, InkTheme.Word, 0.5f, 1.55f, 0.42f, 0f);
                        Sparks(at, 6 + p.Star * 2, InkTheme.GoldHi, 0f);
                        upStar = Mathf.Max(upStar, p.Star);
                        break;
                    case CellBeat.Word:
                        Start(i, CellBeat.Word, WordLag, InkTheme.Word);
                        Ring(at, InkTheme.Word, 0.6f, 2.0f, 0.55f, WordLag);
                        Sparks(at, 10, InkTheme.GoldHi, WordLag);
                        // 两半各报一次，光柱和词名只在上面那一半画一次。
                        if (p.Mate > p.Row)
                        {
                            Vector2 mate = FieldLayout.CellPos(p.Col, p.Mate);
                            Beam(at, mate, WordLag);
                            w.ShowFloat((at + mate) * 0.5f + new Vector2(0f, 0.1f),
                                CardCatalog.WordName(p.Word) + "！", InkTheme.Word, 1.5f);
                        }
                        else if (p.Mate < 0)
                            w.ShowFloat(at + new Vector2(0f, Cw * 0.6f), CardCatalog.WordName(p.Word) + "！", InkTheme.Word, 1.5f);
                        word = true;
                        break;
                    case CellBeat.Unword:
                        Start(i, CellBeat.Unword, SwapLag, Color.clear);
                        Dust(at, 5, new Color(0.45f, 0.40f, 0.36f, 0.7f), SwapLag);
                        unword = true;
                        break;
                    case CellBeat.Fire:
                    {
                        bool isWord = p.Word != WordId.None;
                        Color c = isWord ? InkTheme.Word : InkTheme.Accent(p.Card);
                        Start(i, CellBeat.Fire, 0f, Color.Lerp(c, Color.white, 0.35f));
                        Ring(at, c, 0.45f, isWord ? 1.6f : 1.3f, 0.3f, 0f);
                        if (isWord) Sparks(at, 5, InkTheme.GoldHi, 0f);
                        break;
                    }
                    case CellBeat.Pass:
                        _nudge[i] = 1f;
                        break;
                    case CellBeat.Deny:
                        Start(i, CellBeat.Deny, 0f, InkTheme.Explode);
                        // 没开的格子连底板都不画，晃了也看不见，补一圈红。
                        if (!w.IsOpen(p.Col, p.Row)) Ring(at, InkTheme.Explode, 0.5f, 1.1f, 0.3f, 0f);
                        deny = true;
                        break;
                }
            }
            w.Pulses.Clear();

            float now = Time.unscaledTime;
            if (swap) AudioBus.CellSwap();
            else if (drop) AudioBus.CellDrop();
            if (upStar > 0) AudioBus.CellUpgrade(upStar);
            if (unword) _later.Add(new Later { At = now + SwapLag, Beat = CellBeat.Unword });
            if (word)
            {
                _later.Add(new Later { At = now + WordLag, Beat = CellBeat.Word });
                w.AddShake(0.12f);
            }
            if (deny) AudioBus.Deny();
        }

        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            for (int k = _later.Count - 1; k >= 0; k--)
            {
                if (now < _later[k].At) continue;
                if (_later[k].Beat == CellBeat.Word) AudioBus.WordForm();
                else if (_later[k].Beat == CellBeat.Unword) AudioBus.WordBreak();
                _later.RemoveAt(k);
            }
            for (int i = 0; i < N; i++)
            {
                if (_anim[i].On)
                {
                    if (_anim[i].Delay > 0f) _anim[i].Delay -= dt;
                    else
                    {
                        _anim[i].Age += dt;
                        if (_anim[i].Age >= Length(_anim[i].Beat)) _anim[i].On = false;
                    }
                }
                if (_badge[i] > 0f) _badge[i] = Mathf.Max(0f, _badge[i] - dt / 0.38f);
                if (_nudge[i] > 0f) _nudge[i] = Mathf.Max(0f, _nudge[i] - dt / 0.14f);
            }
            TickBits(dt);
        }

        // 字本身这一帧该怎么画：缩放（x、y 分开，落地要压扁）、位移、透明度、叠一层多亮的色。
        public void Shape(int col, int row, out Vector2 scale, out Vector2 offset, out float alpha, out Color glint)
        {
            int i = Index(col, row);
            scale = Vector2.one * (1f + 0.07f * _nudge[i]);
            offset = Vector2.zero;
            alpha = 1f;
            glint = Color.clear;
            Anim a = _anim[i];
            if (!a.On) return;
            if (a.Delay > 0f)
            {
                // 替换时新字还没进场，这一格先空着让旧字飞走。
                if (a.Beat == CellBeat.Replace) alpha = 0f;
                return;
            }
            float t = a.Age;
            float len = Length(a.Beat);
            float u = Mathf.Clamp01(t / len);
            switch (a.Beat)
            {
                case CellBeat.Drop:
                case CellBeat.Replace:
                {
                    // 从大一圈、半空里落下来，触地压扁，弹一下，站稳。
                    const float fall = 0.10f;
                    if (t < fall)
                    {
                        float k = t / fall;
                        k *= k;
                        float s = Mathf.Lerp(1.55f, 1f, k);
                        scale = new Vector2(s, s);
                        offset = new Vector2(0f, Mathf.Lerp(0.28f, 0f, k));
                        alpha = Mathf.Clamp01(k * 2.2f);
                    }
                    else
                    {
                        float k = (t - fall) / (len - fall);
                        float squash = Mathf.Sin(k * Mathf.PI * 2.2f) * Mathf.Pow(1f - k, 2f);
                        scale = new Vector2(1f + 0.20f * squash, 1f - 0.18f * squash);
                    }
                    break;
                }
                case CellBeat.Upgrade:
                {
                    float s = 1f + 0.34f * Spring(u, 2.4f);
                    scale = new Vector2(s, s);
                    offset = new Vector2(0f, 0.10f * Mathf.Sin(Mathf.Clamp01(u * 2f) * Mathf.PI));
                    glint = Fade(a.Tint, 0.75f * (1f - u));
                    break;
                }
                case CellBeat.Word:
                {
                    float s = 1f + 0.42f * Spring(u, 2.8f);
                    scale = new Vector2(s, s);
                    float g = u < 0.25f ? 1f : Mathf.Clamp01(1f - (u - 0.25f) / 0.75f);
                    glint = Fade(a.Tint, 0.9f * g);
                    break;
                }
                case CellBeat.Unword:
                {
                    offset = new Vector2(Mathf.Sin(t * 46f) * 0.06f * (1f - u), 0f);
                    float s = 1f - 0.12f * Mathf.Sin(u * Mathf.PI);
                    scale = new Vector2(s, s);
                    break;
                }
                case CellBeat.Fire:
                {
                    float s = 1f + 0.22f * Spring(u, 1.6f);
                    scale = new Vector2(s, s);
                    glint = Fade(a.Tint, 0.65f * (1f - u));
                    break;
                }
                case CellBeat.Deny:
                    offset = new Vector2(Mathf.Sin(t * 58f) * 0.08f * (1f - u), 0f);
                    break;
            }
        }

        // 格子底板：拒绝时晃一下、染一点红。
        public void Base(int col, int row, out Vector2 offset, out Color tint)
        {
            offset = Vector2.zero;
            tint = Color.white;
            Anim a = _anim[Index(col, row)];
            if (!a.On || a.Beat != CellBeat.Deny) return;
            float u = Mathf.Clamp01(a.Age / Length(a.Beat));
            offset = new Vector2(Mathf.Sin(a.Age * 58f) * 0.08f * (1f - u), 0f);
            tint = Color.Lerp(Color.white, new Color(1f, 0.62f, 0.58f, 1f), 1f - u);
        }

        // 星标那一下：升星时放大两倍再落回去。
        public float Badge(int col, int row)
        {
            float b = _badge[Index(col, row)];
            if (b <= 0f) return 1f;
            float u = 1f - b;
            return 1f + 1.1f * Spring(u, 2f);
        }

        void Start(int i, CellBeat beat, float delay, Color tint)
        {
            // 成词压在落字之上：同一格先落后亮，落地那一下不能被成词顶掉。
            if (beat == CellBeat.Word && _anim[i].On && (_anim[i].Beat == CellBeat.Drop || _anim[i].Beat == CellBeat.Replace))
                delay = Mathf.Max(delay, Length(_anim[i].Beat) - _anim[i].Age + _anim[i].Delay - 0.12f);
            _anim[i] = new Anim { Beat = beat, Age = 0f, Delay = delay, On = true, Tint = tint };
        }

        static float Length(CellBeat beat)
        {
            switch (beat)
            {
                case CellBeat.Drop:
                case CellBeat.Replace: return 0.42f;
                case CellBeat.Upgrade: return 0.46f;
                case CellBeat.Word: return 0.62f;
                case CellBeat.Unword: return 0.34f;
                case CellBeat.Fire: return 0.24f;
                case CellBeat.Deny: return 0.32f;
            }
            return 0.2f;
        }

        // 0 → 冲到 1 → 回弹过头一点 → 落回 0。
        static float Spring(float u, float wobble)
        {
            if (u <= 0f || u >= 1f) return 0f;
            float rise = 0.18f;
            if (u < rise) return Mathf.Sin(u / rise * Mathf.PI * 0.5f);
            float k = (u - rise) / (1f - rise);
            return Mathf.Cos(k * Mathf.PI * wobble) * Mathf.Pow(1f - k, 2f);
        }

        static Color Fade(Color c, float a)
        {
            c.a = Mathf.Clamp01(a);
            return c;
        }

        // ---------- 一次性小件 ----------

        void Land(Vector2 at, CardId id, float delay)
        {
            Color c = InkTheme.Accent(id);
            float sat = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            if (sat < 0.12f) c = InkTheme.GoldHi;
            Ring(at, c, 0.55f, 1.45f, 0.34f, delay);
            Dust(at + new Vector2(0f, -Cw * 0.3f), 7, new Color(1f, 0.96f, 0.86f, 0.95f), delay);
        }

        void Ring(Vector2 at, Color c, float from, float to, float life, float delay)
        {
            Spawn(InkFx.SoftRing(), at, Vector2.zero, Vector2.one * Cw * from, Vector2.one * Cw * to,
                life, delay, Fade(c, 0.95f), true, 0f, 0f, 0f, 0f, true);
        }

        void Dust(Vector2 at, int n, Color c, float delay)
        {
            for (int k = 0; k < n; k++)
            {
                float ang = Mathf.Lerp(-10f, 190f, (k + Random.value * 0.6f) / n) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.45f);
                float s = Cw * Random.Range(0.10f, 0.16f);
                Spawn(InkFx.Dot(), at + dir * Cw * 0.3f, dir * Random.Range(1.6f, 2.6f),
                    Vector2.one * s, Vector2.one * s * 0.2f, Random.Range(0.26f, 0.36f), delay,
                    c, false, 0f, 0f, 7f, 0f, false);
            }
        }

        void Sparks(Vector2 at, int n, Color c, float delay)
        {
            if (_spark == null) _spark = InkSprites.Load("Ui/ico_star") ?? InkFx.Star();
            for (int k = 0; k < n; k++)
            {
                float ang = (90f + Random.Range(-70f, 70f)) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                float s = Cw * Random.Range(0.16f, 0.26f);
                Spawn(_spark, at + dir * Cw * 0.18f, dir * Random.Range(2.2f, 3.6f),
                    Vector2.one * s, Vector2.one * s * 0.3f, Random.Range(0.45f, 0.65f), delay + k * 0.012f,
                    Color.Lerp(c, Color.white, Random.value * 0.4f), false,
                    Random.Range(0f, 360f), Random.Range(-260f, 260f), 2.2f, -5.5f, false);
            }
        }

        // 被替换掉的旧字：往斜上方甩出去，边转边缩边淡。
        void Ghost(Vector2 at, CardId old)
        {
            if (old == CardId.None) return;
            Sprite s = InkArt.Heap(old, 1, 128);
            if (s == null) return;
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector2 size = s.bounds.size * 0.88f;
            Spawn(s, at, new Vector2(side * 2.4f, 3.4f), size, size * 0.45f, 0.34f, 0f,
                Color.white, false, 0f, side * -320f, 1.2f, -9f, false);
            Dust(at, 5, new Color(0.55f, 0.50f, 0.46f, 0.7f), 0f);
        }

        // 成词：两半之间立起一道金光，先亮后散。
        void Beam(Vector2 a, Vector2 b, float delay)
        {
            Vector2 mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b) + Cw * 0.9f;
            Spawn(InkFx.SoftDisc(), mid, Vector2.zero, new Vector2(Cw * 0.5f, len * 0.9f),
                new Vector2(Cw * 1.1f, len * 1.15f), 0.6f, delay, Fade(InkTheme.Word, 0.85f), true,
                0f, 0f, 0f, 0f, true);
        }

        void Spawn(Sprite sprite, Vector2 pos, Vector2 vel, Vector2 size0, Vector2 size1, float life, float delay,
            Color col, bool add, float rot, float spin, float drag, float grav, bool ease)
        {
            if (sprite == null) return;
            SpriteRenderer sr = _pool.Count > 0 ? _pool.Pop() : MakeBit();
            sr.sprite = sprite;
            if (add) InkFx.PaintAdd(sr, col);
            else InkFx.PaintSprite(sr, col);
            sr.sortingOrder = add ? 7 : 8;
            sr.enabled = false;
            _bits.Add(new Bit
            {
                Sr = sr, Pos = pos, Vel = vel, Size0 = size0, Size1 = size1,
                Life = 0f, Max = life, Delay = delay, Rot = rot, Spin = spin,
                Drag = drag, Grav = grav, Col = col, Ease = ease
            });
        }

        SpriteRenderer MakeBit()
        {
            var go = new GameObject("cellFx");
            go.transform.SetParent(_root, false);
            return go.AddComponent<SpriteRenderer>();
        }

        void Recycle(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.enabled = false;
            _pool.Push(sr);
        }

        void TickBits(float dt)
        {
            for (int k = _bits.Count - 1; k >= 0; k--)
            {
                Bit b = _bits[k];
                if (b.Delay > 0f)
                {
                    b.Delay -= dt;
                    _bits[k] = b;
                    continue;
                }
                b.Life += dt;
                if (b.Life >= b.Max)
                {
                    Recycle(b.Sr);
                    _bits.RemoveAt(k);
                    continue;
                }
                float u = b.Life / b.Max;
                b.Vel *= Mathf.Max(0f, 1f - b.Drag * dt);
                b.Vel.y += b.Grav * dt;
                b.Pos += b.Vel * dt;
                b.Rot += b.Spin * dt;
                float e = b.Ease ? 1f - (1f - u) * (1f - u) : u;
                Vector2 size = Vector2.Lerp(b.Size0, b.Size1, e);
                Vector2 raw = b.Sr.sprite.bounds.size;
                Transform t = b.Sr.transform;
                t.position = new Vector3(b.Pos.x, b.Pos.y, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, b.Rot);
                t.localScale = new Vector3(size.x / Mathf.Max(0.001f, raw.x), size.y / Mathf.Max(0.001f, raw.y), 1f);
                Color c = b.Col;
                c.a *= u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f;
                b.Sr.color = c;
                b.Sr.enabled = true;
                _bits[k] = b;
            }
        }
    }
}
