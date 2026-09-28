using UnityEngine;

namespace InkLine
{
    public enum SparkKind
    {
        Ember, Spark, Bolt, Crescent, Star,
        Shard, Diamond, Bubble, Wave, Streak, Leaf, Coin, Swirl, Reticle
    }

    // 炮弹一路掉下来的小颗粒：火星、电花、风刃、金星……
    // 一律小尺寸、硬边、深色外沿，白色画、上色走 color，所以同一张图能给所有元素用。
    // 「实」走普通混合；「炫」走加法，外沿自动被 InkAdd 的亮度门限吃掉，只剩亮面发光。
    public sealed class ShotSparks : MonoBehaviour
    {
        const int Cap = 420;
        const string PrefKey = "shot_bright";
        const int KindCount = 14;

        struct P
        {
            public SpriteRenderer Sr;
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

        static ShotSparks _inst;
        static int _bright = -1;
        static readonly Sprite[] Sprites = new Sprite[KindCount];

        readonly P[] _p = new P[Cap];
        int _next;

        public static bool Bright
        {
            get
            {
                if (_bright < 0) _bright = PlayerPrefs.GetInt(PrefKey, 1);
                return _bright == 1;
            }
            set
            {
                _bright = value ? 1 : 0;
                PlayerPrefs.SetInt(PrefKey, _bright);
                PlayerPrefs.Save();
            }
        }

        // 发光的字（火、雷、金……）在「炫」档走加法；实心的字（土、木、水……）两档都是普通混合。
        public static Material MatOf(bool glow) => Bright && glow ? InkFx.AddMat() : InkFx.SpriteMat();

        public static readonly ISparkSink World = new WorldSink();

        sealed class WorldSink : ISparkSink
        {
            public void Emit(in SparkSpec s) => ShotSparks.Emit(s);
        }

        public static void Emit(in SparkSpec s)
        {
            if (_inst == null)
            {
                var go = new GameObject("ShotSparks");
                _inst = go.AddComponent<ShotSparks>();
            }
            _inst.Spawn(s);
        }

        public static void Clear()
        {
            if (_inst != null) Destroy(_inst.gameObject);
            _inst = null;
        }

        // 一生的大小曲线。冲击波由小撑大；其余先鼓一下再缩：出生那一帧最亮最大，读得出「迸」。
        public static float Curve(SparkKind kind, float t)
        {
            if (kind == SparkKind.Wave) return Mathf.Lerp(0.3f, 1.4f, 1f - (1f - t) * (1f - t));
            return t < 0.15f ? Mathf.Lerp(0.6f, 1.1f, t / 0.15f) : Mathf.Lerp(1.1f, 0.25f, (t - 0.15f) / 0.85f);
        }

        void Spawn(in SparkSpec s)
        {
            int slot = -1;
            for (int i = 0; i < Cap; i++)
            {
                int k = (_next + i) % Cap;
                if (_p[k].Alive) continue;
                slot = k;
                break;
            }
            if (slot < 0) return;
            _next = (slot + 1) % Cap;
            ref P p = ref _p[slot];
            if (p.Sr == null)
            {
                var go = new GameObject("spark");
                go.transform.SetParent(transform, false);
                p.Sr = go.AddComponent<SpriteRenderer>();
                p.Sr.sortingOrder = 5;
            }
            p.Sr.sprite = SpriteOf(s.Kind);
            p.Sr.sharedMaterial = MatOf(s.Glow);
            // 实心粒子压在发光粒子下面一层，发光的叠上去才不会被挡成一块块。
            p.Sr.sortingOrder = s.Glow ? 5 : 4;
            p.Sr.enabled = true;
            p.Alive = true;
            p.Kind = s.Kind;
            p.Pos = s.Pos;
            p.Vel = s.Vel;
            p.Age = 0f;
            p.Life = Mathf.Max(0.02f, s.Life);
            p.Size = s.Size;
            p.Rot = Random.Range(0f, 360f);
            p.Spin = s.Spin;
            p.Drag = s.Drag;
            p.Grav = s.Grav;
            p.A = s.A;
            p.B = s.B;
            p.Face = s.Face;
            Place(ref p, 0f);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            for (int i = 0; i < Cap; i++)
            {
                ref P p = ref _p[i];
                if (!p.Alive) continue;
                p.Age += dt;
                float t = p.Age / p.Life;
                if (t >= 1f)
                {
                    p.Alive = false;
                    p.Sr.enabled = false;
                    continue;
                }
                p.Pos += p.Vel * dt;
                if (p.Drag > 0f) p.Vel *= Mathf.Max(0f, 1f - p.Drag * dt);
                p.Vel.y -= p.Grav * dt;
                p.Rot += p.Spin * dt;
                Place(ref p, t);
            }
        }

        static void Place(ref P p, float t)
        {
            Transform tr = p.Sr.transform;
            tr.position = new Vector3(p.Pos.x, p.Pos.y, 0f);
            float rot = p.Face && p.Vel.sqrMagnitude > 1e-4f
                ? Mathf.Atan2(p.Vel.y, p.Vel.x) * Mathf.Rad2Deg - 90f
                : p.Rot;
            tr.rotation = Quaternion.Euler(0f, 0f, rot);
            float s = p.Size * Curve(p.Kind, t);
            tr.localScale = new Vector3(s, s, 1f);
            Color c = Color.Lerp(p.A, p.B, t);
            c.a *= 1f - t * t;
            p.Sr.color = c;
        }

        public static Sprite SpriteOf(SparkKind kind)
        {
            int i = (int)kind;
            if (Sprites[i] != null) return Sprites[i];
            switch (kind)
            {
                case SparkKind.Ember: Sprites[i] = Bake(48, Ember); break;
                case SparkKind.Spark: Sprites[i] = Bake(48, Spark4); break;
                case SparkKind.Bolt: Sprites[i] = Bake(48, Bolt, 1.6f); break;
                case SparkKind.Crescent: Sprites[i] = Bake(48, Crescent, 1.8f); break;
                case SparkKind.Shard: Sprites[i] = Bake(48, Shard); break;
                case SparkKind.Diamond: Sprites[i] = Bake(48, Diamond); break;
                case SparkKind.Bubble: Sprites[i] = Bake(48, Bubble, 1.8f); break;
                case SparkKind.Wave: Sprites[i] = Bake(64, Wave, 1.4f); break;
                case SparkKind.Streak: Sprites[i] = Bake(48, Streak, 1.2f); break;
                case SparkKind.Leaf: Sprites[i] = Bake(48, Leaf); break;
                case SparkKind.Coin: Sprites[i] = Bake(48, Coin, 2.2f); break;
                case SparkKind.Swirl: Sprites[i] = Bake(48, Swirl, 1.5f); break;
                case SparkKind.Reticle: Sprites[i] = Bake(64, Reticle, 1.5f); break;
                default: Sprites[i] = Bake(48, Star5); break;
            }
            return Sprites[i];
        }

        // ---- 程序贴图：sdf 给「到边的像素距离」，正数在内。外沿 ~3px 画成深灰，上色后是同色系更深一圈 ----

        delegate float Sdf(Vector2 p, float n);

        static Sprite Bake(int n, Sdf sdf, float rimPx = 0f)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color[n * n];
            float mid = (n - 1) * 0.5f;
            float rim = rimPx > 0f ? rimPx : Mathf.Max(2.5f, n * 0.065f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x - mid) / mid, (y - mid) / mid);
                float d = sdf(p, mid);
                float a = Mathf.Clamp01(d + 0.5f);
                if (a <= 0f) { px[y * n + x] = new Color(1f, 1f, 1f, 0f); continue; }
                float shade = d < rim ? 0.30f : 1f;
                px[y * n + x] = new Color(shade, shade, shade, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        // 火星：上圆下尖的水滴，尖朝 +y（跟着速度转）。
        static float Ember(Vector2 p, float mid)
        {
            float r = 0.46f;
            var c = new Vector2(0f, -0.28f);
            float circle = r - (p - c).magnitude;
            float k = Mathf.Clamp01((p.y - c.y) / (0.95f - c.y));
            float cone = (r * (1f - k)) - Mathf.Abs(p.x);
            float body = p.y > c.y ? Mathf.Max(circle, Mathf.Min(cone, 0.95f - p.y)) : circle;
            return body * mid;
        }

        // 四角星：亮闪。
        static float Spark4(Vector2 p, float mid)
        {
            float ax = Mathf.Abs(p.x), ay = Mathf.Abs(p.y);
            float d = Mathf.Pow(Mathf.Pow(ax, 0.55f) + Mathf.Pow(ay, 0.55f), 1f / 0.55f);
            return (0.92f - d) * mid * 0.55f;
        }

        // 一截折线闪电。
        static readonly Vector2[] BoltPts =
        {
            new Vector2(-0.10f, 0.92f), new Vector2(0.22f, 0.28f), new Vector2(-0.18f, 0.08f),
            new Vector2(0.16f, -0.40f), new Vector2(-0.06f, -0.92f)
        };

        static float Bolt(Vector2 p, float mid)
        {
            float best = 9f;
            for (int i = 0; i < BoltPts.Length - 1; i++)
            {
                Vector2 a = BoltPts[i], b = BoltPts[i + 1];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float k = 1f - Mathf.Abs((i + t) / (BoltPts.Length - 1) - 0.5f) * 1.4f;
                best = Mathf.Min(best, (p - (a + ab * t)).magnitude - 0.21f * k);
            }
            return -best * mid;
        }

        // 风刃：一弯月牙，两头收尖。
        static float Crescent(Vector2 p, float mid)
        {
            float r = p.magnitude;
            float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            const float span = 210f;
            if (ang > span) return -9f * mid;
            float k = Mathf.Sin(ang / span * Mathf.PI);
            float half = 0.05f + 0.24f * k;
            float band = half - Mathf.Abs(r - 0.66f);
            return band * mid;
        }

        // 五角星：晕的金星。
        static float Star5(Vector2 p, float mid)
        {
            float ang = Mathf.Atan2(p.x, p.y);
            float seg = Mathf.PI * 2f / 5f;
            float a = Mathf.Repeat(ang + seg * 0.5f, seg) - seg * 0.5f;
            float t = Mathf.Abs(a) / (seg * 0.5f);
            float edge = Mathf.Lerp(0.95f, 0.42f, t);
            return (edge - p.magnitude) * mid * 0.8f;
        }

        // 凸多边形：到每条边的内侧距离取最小。点按逆时针给。
        static float Convex(Vector2 p, Vector2[] pts)
        {
            float d = 9f;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Length];
                Vector2 e = (b - a).normalized;
                var inward = new Vector2(-e.y, e.x);
                d = Mathf.Min(d, Vector2.Dot(p - a, inward));
            }
            return d;
        }

        // 碎石 / 碎片：不规则的六边形。
        static readonly Vector2[] ShardPts =
        {
            new Vector2(0.05f, 0.92f), new Vector2(-0.52f, 0.58f), new Vector2(-0.86f, -0.08f),
            new Vector2(-0.34f, -0.84f), new Vector2(0.56f, -0.66f), new Vector2(0.78f, 0.30f)
        };

        static float Shard(Vector2 p, float mid) => Convex(p, ShardPts) * mid;

        // 冰晶：细长菱形。
        static readonly Vector2[] DiamondPts =
        {
            new Vector2(0f, 0.95f), new Vector2(-0.44f, 0f), new Vector2(0f, -0.95f), new Vector2(0.44f, 0f)
        };

        static float Diamond(Vector2 p, float mid) => Convex(p, DiamondPts) * mid;

        // 水泡：一圈厚环，内外两道深边，中间一道亮带。
        static float Bubble(Vector2 p, float mid) => (0.19f - Mathf.Abs(p.magnitude - 0.72f)) * mid;

        // 冲击波：细环，靠 Curve 撑大。
        static float Wave(Vector2 p, float mid) => (0.08f - Mathf.Abs(p.magnitude - 0.86f)) * mid;

        // 速度线：下粗上细的一根针。粒子面向速度（往后飞）时 +y 朝后，细尖拖在远端。
        static float Streak(Vector2 p, float mid)
        {
            float y = Mathf.Clamp(p.y, -0.95f, 0.95f);
            float k = (0.95f - y) / 1.9f;
            float w = Mathf.Lerp(0.05f, 0.26f, k);
            return (w - new Vector2(p.x, p.y - y).magnitude) * mid;
        }

        // 叶子：两个圆相交的柳叶形。
        static float Leaf(Vector2 p, float mid)
        {
            var c = new Vector2(0.46f, 0f);
            const float r = 0.98f;
            return Mathf.Min(r - (p - c).magnitude, r - (p + c).magnitude) * mid;
        }

        // 铜钱：外圆内方。
        static float Coin(Vector2 p, float mid)
        {
            float disc = 0.92f - p.magnitude;
            float hole = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - 0.26f;
            return Mathf.Min(disc, hole) * mid;
        }

        // 惑：一圈往外绕的漩涡线。
        static readonly Vector2[] SwirlPts = BuildSwirl();

        static Vector2[] BuildSwirl()
        {
            const int n = 36;
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)(n - 1) * Mathf.PI * 3.4f;
                float r = 0.08f + 0.78f * (i / (float)(n - 1));
                pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return pts;
        }

        static float Swirl(Vector2 p, float mid)
        {
            float best = 9f;
            for (int i = 0; i < SwirlPts.Length - 1; i++)
            {
                Vector2 a = SwirlPts[i], b = SwirlPts[i + 1];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float k = (i + t) / (SwirlPts.Length - 1);
                best = Mathf.Min(best, (p - (a + ab * t)).magnitude - Mathf.Lerp(0.07f, 0.13f, k));
            }
            return -best * mid;
        }

        // 瞄：准星，一圈细环加四根刻度。
        static float Reticle(Vector2 p, float mid)
        {
            float ring = 0.075f - Mathf.Abs(p.magnitude - 0.70f);
            float ax = Mathf.Abs(p.x), ay = Mathf.Abs(p.y);
            float tickV = Mathf.Min(0.075f - ax, Mathf.Min(ay - 0.42f, 0.98f - ay));
            float tickH = Mathf.Min(0.075f - ay, Mathf.Min(ax - 0.42f, 0.98f - ax));
            return Mathf.Max(ring, Mathf.Max(tickV, tickH)) * mid;
        }
    }
}
