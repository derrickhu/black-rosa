using UnityEngine;

namespace InkLine
{
    // 动态卡通特效的共享材质、无缝噪声贴图和程序灰度遮罩。
    // 所有遮罩都配合 InkLine/VfxFlow 使用：Shader 按能量值映射为「白热芯 -> 高饱和主色 -> 深色硬边轮廓」，
    // 在浅色宣纸战场上清晰锐利、60fps 流动、噪声硬边溶解，且零额外磁盘包体占用。
    public static class InkFlowArt
    {
        public static readonly int PropMain = Shader.PropertyToID("_MainTex");
        public static readonly int PropCore = Shader.PropertyToID("_ColorCore");
        public static readonly int PropMid = Shader.PropertyToID("_ColorMid");
        public static readonly int PropRim = Shader.PropertyToID("_ColorRim");
        public static readonly int PropFlow = Shader.PropertyToID("_Flow");
        public static readonly int PropDissolve = Shader.PropertyToID("_Dissolve");
        static readonly int PropNoise = Shader.PropertyToID("_NoiseTex");

        static Material _flowMat;
        static Texture2D _noise;
        static Sprite _vortex;
        static Sprite _ring;
        static Sprite _wisp;
        static Sprite _slash;
        static Sprite _crack;
        static Sprite _pillar;
        static Sprite _shard;

        public static Material FlowMat()
        {
            if (_flowMat != null) return _flowMat;
            Shader sh = Resources.Load<Shader>("Shaders/InkVfxFlow");
            if (sh == null) sh = Shader.Find("InkLine/VfxFlow");
            if (sh == null) return InkFx.SpriteMat();
            _flowMat = new Material(sh);
            _flowMat.SetTexture(PropNoise, NoiseTex());
            return _flowMat;
        }

        public static void Apply(SpriteRenderer sr, MaterialPropertyBlock mpb,
            Color core, Color mid, Color rim, Vector4 flow, Vector4 dissolve, float alpha = 1f)
        {
            if (sr == null) return;
            sr.sharedMaterial = FlowMat();
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            mpb.Clear();
            if (sr.sprite != null && sr.sprite.texture != null)
                mpb.SetTexture(PropMain, sr.sprite.texture);
            mpb.SetColor(PropCore, core);
            mpb.SetColor(PropMid, mid);
            mpb.SetColor(PropRim, rim);
            mpb.SetVector(PropFlow, flow);
            mpb.SetVector(PropDissolve, dissolve);
            sr.SetPropertyBlock(mpb);
        }

        // 128x128 周期无缝 3-octave FBM 噪声，uv Repeat 无接缝。
        public static Texture2D NoiseTex()
        {
            if (_noise != null) return _noise;
            const int n = 128;
            _noise = new Texture2D(n, n, TextureFormat.RGBA32, false);
            _noise.wrapMode = TextureWrapMode.Repeat;
            _noise.filterMode = FilterMode.Bilinear;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n;
                float v = y / (float)n;
                float f = TileNoise(u, v, 6, 11) * 0.55f
                        + TileNoise(u, v, 12, 37) * 0.30f
                        + TileNoise(u, v, 24, 73) * 0.15f;
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(f * 255f), 0, 255);
                px[y * n + x] = new Color32(b, b, b, 255);
            }
            _noise.SetPixels32(px);
            _noise.Apply(false, false);
            return _noise;
        }

        static float TileNoise(float u, float v, int period, int seed)
        {
            float fx = u * period;
            float fy = v * period;
            int x0 = Mathf.FloorToInt(fx) % period;
            int y0 = Mathf.FloorToInt(fy) % period;
            int x1 = (x0 + 1) % period;
            int y1 = (y0 + 1) % period;
            float tx = fx - Mathf.Floor(fx);
            float ty = fy - Mathf.Floor(fy);
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash(x0, y0, seed);
            float b = Hash(x1, y0, seed);
            float c = Hash(x0, y1, seed);
            float d = Hash(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        static float Hash(int x, int y, int seed)
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0x7fffffff) / 2147483647f;
        }

        // 径向实心旋涡遮罩：外留安全边距供 UV 扭曲舒展，中心能量最满。
        public static Sprite VortexMask()
        {
            if (_vortex != null) return _vortex;
            _vortex = Bake(128, 128, new Vector2(0.5f, 0.5f), (u, v) =>
            {
                float dx = u * 2f - 1f;
                float dy = v * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                return Mathf.Pow(Mathf.Clamp01(1f - r / 0.80f), 0.72f);
            });
            return _vortex;
        }

        // 粗厚能量环遮罩：适合旋风风环、坍缩冲击环、符咒锁环、风火轮。
        public static Sprite RingMask()
        {
            if (_ring != null) return _ring;
            _ring = Bake(128, 128, new Vector2(0.5f, 0.5f), (u, v) =>
            {
                float dx = u * 2f - 1f;
                float dy = v * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float inner = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 0.50f, r));
                float outer = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 0.60f, r));
                return inner * outer;
            });
            return _ring;
        }

        // 上尖下圆的火焰/魂影遮罩：轴心偏下（y=0.25），向上窜起摇曳。
        public static Sprite WispMask()
        {
            if (_wisp != null) return _wisp;
            _wisp = Bake(96, 128, new Vector2(0.5f, 0.25f), (u, v) =>
            {
                float x = u * 2f - 1f;
                float y = v;
                // 底部饱满，往上收尖，两侧留足扭曲边距
                float halfW = Mathf.Lerp(0.58f, 0.04f, Mathf.Pow(y, 0.85f)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f, 0.24f, y));
                if (halfW <= 0.01f) return 0f;
                float across = 1f - Mathf.Abs(x) / halfW;
                float topFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.92f, 0.62f, y));
                return Mathf.Pow(Mathf.Clamp01(across), 0.72f) * topFade;
            });
            return _wisp;
        }

        // 锐利弯月刀光遮罩：上弓背饱满、两端尖收，适合回锋刀光、风刃、雷决斩痕。
        public static Sprite SlashArc()
        {
            if (_slash != null) return _slash;
            _slash = Bake(128, 128, new Vector2(0.5f, 0.5f), (u, v) =>
            {
                float x = u * 2f - 1f;
                float y = v * 2f - 1f;
                float r = Mathf.Sqrt(x * x + y * y);
                float ang = Mathf.Atan2(y, x); // 0=+x, pi/2=+y
                // 上半弧：以 +y 为刀背中心，向左右展开约 130°
                float dAng = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, 90f)) / 72f;
                if (dAng >= 1f) return 0f;
                float taper = Mathf.Pow(1f - dAng * dAng, 0.7f);
                float midR = 0.66f;
                float thick = 0.24f * taper;
                if (thick <= 0.01f) return 0f;
                float dist = Mathf.Abs(r - midR) / thick;
                // 外弧更锐利，内弧略柔和
                float bias = r > midR ? 1.15f : 0.9f;
                return Mathf.Pow(Mathf.Clamp01(1f - dist * bias), 0.8f) * taper;
            });
            return _slash;
        }

        // 放射状硬边裂地/冰裂纹遮罩：9 条长短不一、带折角的裂纹 + 中心爆心。
        public static Sprite CrackRing()
        {
            if (_crack != null) return _crack;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];

            // 预生成 9 条不均匀分布的放射裂纹（固定种子，形状自然不机械）
            const int rays = 9;
            var angs = new float[rays];
            var lens = new float[rays];
            var wids = new float[rays];
            for (int i = 0; i < rays; i++)
            {
                angs[i] = (i / (float)rays + (Hash(i, 3, 19) - 0.5f) * 0.06f) * Mathf.PI * 2f;
                lens[i] = Mathf.Lerp(0.55f, 0.94f, Hash(i, 7, 41));
                wids[i] = Mathf.Lerp(0.045f, 0.085f, Hash(i, 11, 59));
            }

            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float px0 = (x + 0.5f) / n * 2f - 1f;
                float py0 = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(px0 * px0 + py0 * py0);
                // 中心爆点 + 内环裂口
                float val = Mathf.Clamp01(1f - r / 0.34f) * 0.95f;
                float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.36f) / 0.09f) * 0.55f;
                val = Mathf.Max(val, ring);

                for (int i = 0; i < rays; i++)
                {
                    float ca = Mathf.Cos(angs[i]);
                    float sa = Mathf.Sin(angs[i]);
                    float along = px0 * ca + py0 * sa;
                    if (along < 0.05f || along > lens[i]) continue;
                    float perp = -px0 * sa + py0 * ca;
                    // 锯齿折角偏移
                    float zig = Mathf.Sin(along * 15f + i * 2.1f) * 0.045f;
                    float t = along / lens[i];
                    float halfW = wids[i] * (1f - t * 0.85f);
                    float d = Mathf.Abs(perp - zig) / Mathf.Max(0.005f, halfW);
                    float rayVal = Mathf.Clamp01(1f - d) * Mathf.Lerp(1f, 0.45f, t);
                    if (rayVal > val) val = rayVal;
                }
                px[y * n + x] = new Color(val, val, val, val > 0.01f ? 1f : 0f);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _crack = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
            return _crack;
        }

        // 上粗下尖带棱面的巨型定海针/冰土石柱罩（轴心在底部尖端 y=0.08，方便精确钉地）。
        public static Sprite PillarMask()
        {
            if (_pillar != null) return _pillar;
            _pillar = Bake(64, 160, new Vector2(0.5f, 0.08f), (u, v) =>
            {
                float x = u * 2f - 1f;
                // 底部尖刺 -> 中段微粗 -> 顶部略收带冠
                float halfW;
                if (v < 0.16f) halfW = Mathf.Lerp(0.04f, 0.44f, v / 0.16f);
                else if (v < 0.86f) halfW = Mathf.Lerp(0.44f, 0.56f, (v - 0.16f) / 0.70f);
                else halfW = Mathf.Lerp(0.56f, 0.18f, (v - 0.86f) / 0.14f);
                // 顶部横向装饰环
                if (v > 0.74f && v < 0.83f) halfW = Mathf.Max(halfW, 0.80f);
                float across = 1f - Mathf.Abs(x) / Mathf.Max(0.02f, halfW);
                if (across <= 0f) return 0f;
                // 中央棱线高光更亮
                float core = Mathf.Pow(Mathf.Clamp01(across), 0.55f);
                float endFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.01f, 0.05f, v))
                              * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.99f, 0.94f, v));
                return core * endFade;
            });
            return _pillar;
        }

        // 带深色描边的锐利多边形碎块：可直接走普通混合 + 染色，用作碎甲、碎冰、飞石。
        public static Sprite ShardPoly()
        {
            if (_shard != null) return _shard;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];
            // 5 边形不规则菱片
            Vector2[] poly =
            {
                new Vector2(0.0f, 0.88f),
                new Vector2(0.62f, 0.20f),
                new Vector2(0.40f, -0.78f),
                new Vector2(-0.48f, -0.62f),
                new Vector2(-0.68f, 0.28f)
            };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
                float minEdge = 10f;
                for (int i = 0; i < poly.Length; i++)
                {
                    Vector2 a = poly[i];
                    Vector2 b = poly[(i + 1) % poly.Length];
                    Vector2 e = b - a;
                    Vector2 inward = new Vector2(-e.y, e.x).normalized; // 顺时针多边形的内法线取右法线
                    float d = Vector2.Dot(p - a, -inward);
                    if (d < minEdge) minEdge = d;
                }
                float a0 = Mathf.Clamp01(minEdge * 18f);
                if (a0 <= 0.001f) { px[y * n + x] = Color.clear; continue; }
                // 外缘深色描边，内部亮面（左上更亮）
                float rim = Mathf.Clamp01((minEdge - 0.11f) * 14f);
                float lit = Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(0.5f + (-p.x * 0.4f + p.y * 0.6f)));
                float c = Mathf.Lerp(0.22f, lit, rim);
                px[y * n + x] = new Color(c, c, c, a0);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _shard = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return _shard;
        }

        static Sprite Bake(int w, int h, Vector2 pivot, System.Func<float, float, float> fn)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = Mathf.Clamp01(fn((x + 0.5f) / w, (y + 0.5f) / h));
                px[y * w + x] = new Color(v, v, v, v > 0.004f ? 1f : 0f);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, Mathf.Max(w, h), 0, SpriteMeshType.FullRect);
        }
    }
}
