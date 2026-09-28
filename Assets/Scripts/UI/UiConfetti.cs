using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 休闲游戏结算那阵彩带：从左右下角往上喷，升到顶后翻着面飘落，末段淡出。
    // 也顺带管星星落地那一圈小火花（Sparks）。都用 Image 池，播完自己销毁。
    public sealed class UiConfetti : MonoBehaviour
    {
        struct Bit
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Rot;
            public float Spin;
            public float Flip;
            public float Age;
            public float Life;
            public float Drag;
            public float Grav;
            public bool Flutter;
        }

        static readonly Color[] Palette =
        {
            InkTheme.Hex("E84E4E"), InkTheme.Hex("F6BE3C"), InkTheme.Hex("36B0B0"),
            InkTheme.Hex("7ED48A"), InkTheme.Hex("8C6EDC"), InkTheme.Hex("FF8A5B"), InkTheme.Hex("FFFFFF")
        };

        Bit[] _bits;
        int _n;

        // 两侧对喷。parent 应该是铺满屏的节点（Dimmer）。
        public static UiConfetti Burst(RectTransform parent, int count = 90)
        {
            var fx = Host(parent, "confetti", count);
            Vector2 size = parent.rect.size;
            if (size.x < 10f) size = new Vector2(ScreenFit.DesignW, ScreenFit.DesignH);
            float hw = size.x * 0.5f, hh = size.y * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var pos = new Vector2(side * (hw + 20f), -hh * Random.Range(0.05f, 0.35f));
                var vel = new Vector2(-side * Random.Range(260f, 820f), Random.Range(900f, 1650f));
                bool round = Random.value < 0.25f;
                Vector2 dim = round ? Vector2.one * Random.Range(12f, 18f) : new Vector2(Random.Range(10f, 16f), Random.Range(20f, 32f));
                fx.Spawn(round ? Dot() : null, pos, vel, dim, Palette[Random.Range(0, Palette.Length)],
                    Random.Range(2.4f, 3.4f), 1.1f, 1500f, !round);
            }
            return fx;
        }

        // 一圈往外迸的小亮点，星星、奖励卡落地时用。pos 是 parent 本地坐标。
        public static void Sparks(RectTransform parent, Vector2 pos, Color color, int count = 12, float speed = 520f)
        {
            var fx = Host(parent, "sparks", count);
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float s = Random.Range(12f, 22f);
                fx.Spawn(Star(), pos + dir * 30f, dir * speed * Random.Range(0.6f, 1.1f), Vector2.one * s,
                    Random.value < 0.5f ? color : Color.white, Random.Range(0.4f, 0.6f), 4f, 0f, false);
            }
        }

        static UiConfetti Host(RectTransform parent, string name, int count)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            var fx = go.AddComponent<UiConfetti>();
            fx._bits = new Bit[count];
            return fx;
        }

        static Sprite _dot;
        static Sprite Dot() => _dot != null ? _dot : (_dot = InkArt.Heap(InkShape.Circle, Color.white, 32));
        static Sprite Star() => ShotSparks.SpriteOf(SparkKind.Spark);

        void Spawn(Sprite sprite, Vector2 pos, Vector2 vel, Vector2 size, Color color, float life, float drag, float grav, bool flutter)
        {
            if (_n >= _bits.Length) return;
            var go = new GameObject("bit", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            _bits[_n++] = new Bit
            {
                Rt = rt, Img = img, Pos = pos, Vel = vel, Rot = Random.Range(0f, 360f),
                Spin = Random.Range(-540f, 540f), Flip = Random.Range(5f, 11f), Life = life,
                Drag = drag, Grav = grav, Flutter = flutter
            };
            Place(ref _bits[_n - 1]);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            int alive = 0;
            for (int i = 0; i < _n; i++)
            {
                ref Bit b = ref _bits[i];
                if (b.Rt == null) continue;
                b.Age += dt;
                if (b.Age >= b.Life)
                {
                    Destroy(b.Rt.gameObject);
                    b.Rt = null;
                    continue;
                }
                alive++;
                b.Vel *= Mathf.Max(0f, 1f - b.Drag * dt);
                b.Vel.y -= b.Grav * dt;
                // 落下时有空气阻力的终速，彩纸才飘得起来。
                if (b.Flutter && b.Vel.y < -260f) b.Vel.y = Mathf.Lerp(b.Vel.y, -260f, dt * 6f);
                if (b.Flutter) b.Vel.x += Mathf.Sin(b.Age * b.Flip) * 60f * dt;
                b.Pos += b.Vel * dt;
                b.Rot += b.Spin * dt;
                Place(ref b);
            }
            if (alive == 0 && _n > 0) Destroy(gameObject);
        }

        static void Place(ref Bit b)
        {
            b.Rt.anchoredPosition = b.Pos;
            b.Rt.localRotation = Quaternion.Euler(0f, 0f, b.Rot);
            float flip = b.Flutter ? Mathf.Cos(b.Age * b.Flip) : 1f;
            float pop = b.Flutter ? 1f : Mathf.Lerp(1.2f, 0.2f, b.Age / b.Life);
            b.Rt.localScale = new Vector3(flip * pop, pop, 1f);
            float fade = Mathf.Clamp01((b.Life - b.Age) / 0.5f);
            Color c = b.Img.color;
            c.a = fade;
            b.Img.color = c;
        }
    }
}
