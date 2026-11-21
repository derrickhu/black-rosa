using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 秘卷的现场表现：烈爆留下的火地，和其余十几对触发那一下的专属特效。
    // 底图是浅色宣纸，加色光在上面几乎看不见，所以主体一律用平涂，加色只做白热的芯。
    // 所有尺寸都按「世界单位里多大」给，换算时除掉贴图自己的 bounds，导入设置变了也不走样。
    public sealed class PairFxView : MonoBehaviour
    {
        enum ScaleCurve { EaseOut, PopBack, SquashLand, ShrinkIn }

        sealed class Bit
        {
            public SpriteRenderer Sr;
            public Sprite[] Frames;
            public Vector2 Pos, Vel, From, To;
            public float Age, Life, Delay, Spin, Gravity, Lift, A0;
            public Vector2 S0, S1;
            public Color C;
            public bool On, Travel, HoldAlpha, Orbit;
            public float OrbitAng, OrbitRate;

            // 动态增强：非线性缩放、空气阻力、速度拉伸、地面弹跳、Shader 流体溶解
            public ScaleCurve Curve;
            public float Drag, StretchByVel, FloorY, Bounce;
            public bool HasFloor;
            public bool Flow;
            public Color Core, Mid, Rim;
            public Vector4 FlowVec;
            public float Distort, Sharp, Phase, DissolveAt;
        }

        // 程序化动态弧线丝带（引力线、旋风风带、折返弧光），三层硬边：深色外沿 + 高饱和主色 + 亮色内芯。
        sealed class RibbonBit
        {
            public LineRenderer Outer, Body, Inner;
            public Vector2 From, Ctrl, To;
            public float Age, Life, Delay, Width;
            public Color Rim, Mid, Core;
            public bool On, Spiral;
            public Vector2 Center;
            public float R0, R1, Ang0, Turns;
        }

        sealed class Patch
        {
            public int Id;
            public bool Seen;
            public SpriteRenderer Scorch, Glow;
            public SpriteRenderer[] Flames;
            public Vector2[] Spots;
            public float[] Phase, Height;
            public float EmberAt, Age;
        }

        const int OrderScorch = 1;
        const int OrderGround = 2;
        const int OrderFlame = 3;
        const int OrderHit = 14;
        const int OrderTop = 15;
        const int OrderMote = 18;
        const int RibbonPts = 32;

        static PairFxView _pool;
        readonly List<Bit> _bits = new List<Bit>();
        readonly List<RibbonBit> _ribbons = new List<RibbonBit>();
        readonly List<Patch> _patches = new List<Patch>();
        readonly Vector3[] _rPts = new Vector3[RibbonPts];
        MaterialPropertyBlock _mpb;
        static readonly Dictionary<string, Sprite> Art = new Dictionary<string, Sprite>();

        static readonly Color ScorchInk = new Color(0.20f, 0.11f, 0.05f, 1f);
        static readonly Color Ember = new Color(1f, 0.52f, 0.12f, 1f);

        public static void Bind(Transform root)
        {
            if (_pool != null && _pool.transform.parent == root) return;
            var go = new GameObject("pairfx");
            go.transform.SetParent(root, false);
            _pool = go.AddComponent<PairFxView>();
        }

        static Sprite Pic(string name)
        {
            if (Art.TryGetValue(name, out Sprite s) && s != null) return s;
            s = InkSprites.Load("Vfx/" + name);
            Art[name] = s;
            return s;
        }

        // ---------- 触发特效 ----------

        public static void Play(PairFx fx)
        {
            AudioBus.PairTrigger(fx.Kind, fx.Size);
            if (_pool == null) return;
            switch (fx.Kind)
            {
                case PairFx.Scorch: _pool.Scorch(fx.A, fx.Size); break;
                case PairFx.Shatter: _pool.Shatter(fx.A, fx.Size); break;
                case PairFx.Conduct: _pool.Conduct(fx.A, fx.Size); break;
                case PairFx.WardGold: _pool.WardGold(fx.A, fx.Size); break;
                case PairFx.Leech: _pool.Leech(fx.A, fx.B); break;
                case PairFx.RamEarth: _pool.RamEarth(fx.A, fx.B, fx.Size); break;
                case PairFx.ColdWind: _pool.ColdWind(fx.A, fx.B, fx.Size); break;
                case PairFx.ThunderCut: _pool.ThunderCut(fx.A, fx.Size); break;
                case PairFx.Crumble: _pool.Crumble(fx.A, fx.Size); break;
                case PairFx.Collapse: _pool.Collapse(fx.A, fx.Size); break;
                case PairFx.Whirl: _pool.Whirl(fx.A, fx.Size); break;
                case PairFx.Riposte: _pool.Riposte(fx.A, fx.B); break;
                case PairFx.FireWheel: _pool.FireWheel(fx.A, fx.Size); break;
                case PairFx.Cloud: _pool.Cloud(fx.A, fx.Size); break;
                case PairFx.Needle: _pool.Needle(fx.A, fx.Size); break;
                case PairFx.Seal: _pool.SealMark(fx.A, fx.Size); break;
                case PairFx.Ghost: _pool.Ghost(fx.A, fx.Size); break;
                case PairFx.Soul: _pool.Soul(fx.A, fx.Size); break;
            }
        }

        // 焦雷：晕一结束，那一口火雷从天上劈进来。地上烧出一块焦印。
        void Scorch(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.6f);
            Mark(at + Vector2.down * 0.12f, s * 1.1f, s * 1.3f, 1.4f, ScorchInk, 0.5f);
            Anim("hitv_fire", at, s * 1.25f, s * 1.6f, 0.42f, OrderHit);
            Anim("hitv_thunder", at, s * 0.9f, s * 1.25f, 0.32f, OrderTop, 0.04f);
            Flash(at, s * 0.5f, s * 1.6f, 0.2f);
            Spray(at, 12, InkFx.Dot(), 0.09f, 2.2f, 4.2f, 0.55f, Ember, OrderTop, -4f);
            Spray(at, 6, InkFx.Star(), 0.2f, 1.6f, 3f, 0.4f, InkTheme.ThunderHi, OrderTop, 0f);
        }

        // 霰雷：冰壳整个崩掉，碎冰往四面掉。
        void Shatter(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.4f);
            Anim("hitv_ice", at, s * 0.9f, s * 1.45f, 0.36f, OrderHit);
            Flash(at, s * 0.4f, s * 1.4f, 0.16f);
            Sprite[] crust = InkVfx.Frames("ice_crust");
            Sprite chip = crust != null && crust[0] != null ? crust[0] : InkFx.Star();
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f + Random.Range(-0.05f, 0.05f)) * Mathf.PI * 2f;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.6f) * Random.Range(2.2f, 3.6f);
                Bit b = Add(chip, at, OrderTop, Color.white, false);
                b.Vel = v;
                b.Gravity = -9f;
                b.Life = Random.Range(0.42f, 0.55f);
                b.S0 = b.S1 = Size(chip, Random.Range(0.18f, 0.3f));
                b.Spin = Random.Range(-540f, 540f);
                b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }
            Spray(at, 8, InkFx.Star(), 0.16f, 1.8f, 3.4f, 0.4f, InkTheme.Ice, OrderTop, 0f);
        }

        // 导电：一圈水面亮起来，电光沿着圈跑，晕是一片一起吃的。
        void Conduct(Vector2 at, float r)
        {
            float d = r * 2f;
            Anim("hitv_water", at, d * 0.7f, d * 1.05f, 0.45f, OrderGround, 0f, 0.7f);
            Ring(at, d * 0.35f, d * 1.1f, 0.42f, InkTheme.Water, 0.85f, OrderGround);
            Sprite zap = Pic("stun_ring");
            if (zap != null)
            {
                Bit b = Add(zap, at, OrderHit, InkTheme.ThunderHi, false);
                b.S0 = Size(zap, d * 0.8f);
                b.S1 = Size(zap, d * 1.05f);
                b.Life = 0.5f;
                b.Spin = 260f;
                b.A0 = 0.95f;
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector2 p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * Random.Range(0.75f, 1f);
                Bit b = Add(InkFx.Star(), p, OrderTop, i % 2 == 0 ? Color.white : InkTheme.ThunderHi, true);
                b.S0 = Vector2.one * 0.34f;
                b.S1 = Vector2.one * 0.06f;
                b.Life = 0.3f;
                b.Delay = i * 0.03f;
                b.Spin = 400f;
            }
        }

        // 镇金：加算吃了乘算，那一下砸下去是一枚金印。
        void WardGold(Vector2 at, float r)
        {
            float s = Mathf.Max(1f, r * 2.4f);
            Anim("hitv_gold", at, s * 0.9f, s * 1.5f, 0.38f, OrderHit);
            Ring(at, s * 0.3f, s * 1.7f, 0.34f, InkTheme.CoinFace, 0.95f, OrderHit - 1);
            Sprite coin = InkFx.Coin();
            for (int i = 0; i < 5; i++)
            {
                Bit b = Add(coin, at, OrderTop, Color.white, false);
                b.Vel = new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(2.6f, 3.8f));
                b.Gravity = -10f;
                b.Life = 0.55f;
                b.S0 = b.S1 = Vector2.one * Random.Range(0.18f, 0.26f);
                b.Spin = Random.Range(-360f, 360f);
            }
        }

        // 蚀生：毒伤化成一粒绿光，弧线飞回城墙。
        void Leech(Vector2 from, Vector2 to)
        {
            Anim("hitv_wood", from, 0.45f, 0.75f, 0.32f, OrderHit, 0f, 0.8f);
            for (int i = 0; i < 3; i++)
            {
                Bit b = Add(InkFx.Mote(), from, OrderMote, i == 0 ? InkTheme.PoisonHi : InkTheme.Wood, false);
                b.Travel = true;
                b.From = from;
                b.To = to + new Vector2(Random.Range(-0.5f, 0.5f), 0f);
                b.Lift = Random.Range(-1.2f, 1.2f);
                b.Life = 0.7f;
                b.Delay = i * 0.06f;
                b.HoldAlpha = true;
                b.S0 = b.S1 = Vector2.one * (0.34f - 0.08f * i);
            }
        }

        // 夯土：怪被一路夯回去，地上拖一道土烟，落点砸开一圈裂地。
        void RamEarth(Vector2 from, Vector2 to, float r)
        {
            float s = Mathf.Max(1f, r * 2.4f);
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                Vector2 p = Vector2.Lerp(from, to, k) + new Vector2(Random.Range(-0.12f, 0.12f), 0f);
                Bit b = Add(InkFx.SoftDisc(), p, OrderGround, InkTheme.Earth, false);
                b.S0 = Vector2.one * 0.4f;
                b.S1 = Vector2.one * (0.95f + 0.2f * k);
                b.Life = 0.55f;
                b.Delay = k * 0.14f;
                b.A0 = 0.55f;
                b.Vel = new Vector2(Random.Range(-0.4f, 0.4f), 0.2f);
            }
            Ring(to, s * 0.3f, s * 1.9f, 0.4f, InkTheme.Earth, 0.8f, OrderGround, 0.12f);
            Anim("hitv_earth", to, s * 0.9f, s * 1.45f, 0.4f, OrderHit, 0.12f);
            Spray(to, 8, InkFx.Dot(), 0.12f, 1.8f, 3.2f, 0.5f, InkTheme.Earth, OrderTop, -8f, 0.12f);
        }

        // 寒风：横着吹走时拖一道霜，落脚的地方结一层冰。
        void ColdWind(Vector2 from, Vector2 to, float r)
        {
            float s = Mathf.Max(0.9f, r * 2.2f);
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                Vector2 p = Vector2.Lerp(from, to, k) + new Vector2(0f, Random.Range(-0.1f, 0.1f));
                Bit b = Add(InkFx.SoftDisc(), p, OrderGround, InkTheme.IceHi, false);
                b.S0 = Vector2.one * 0.55f;
                b.S1 = Vector2.one * 0.9f;
                b.Life = 0.5f;
                b.Delay = k * 0.08f;
                b.A0 = 0.85f;
                Bit f = Add(InkFx.Star(), p, OrderTop, InkTheme.Ice, false);
                f.Vel = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.3f, 0.9f));
                f.S0 = Vector2.one * 0.2f;
                f.S1 = Vector2.one * 0.05f;
                f.Life = 0.5f;
                f.Delay = k * 0.08f;
                f.Spin = 300f;
            }
            Sprite[] frost = InkVfx.Frames("dot_frost");
            if (frost != null && frost[0] != null)
            {
                Bit b = Add(frost[0], to + Vector2.down * r * 0.6f, OrderGround, Color.white, false);
                b.S0 = Size(frost[0], s * 0.6f);
                b.S1 = Size(frost[0], s * 1.05f);
                b.Life = 1.1f;
                b.Delay = 0.06f;
                b.A0 = 0.9f;
                b.HoldAlpha = true;
            }
            Anim("hitv_wind", to, s * 0.8f, s * 1.2f, 0.36f, OrderHit, 0.04f);
        }

        // 雷决：双道交叉靛紫电斩错峰弹出 + 中心白热爆核 + 急刹电火花。
        void ThunderCut(Vector2 at, float r)
        {
            float s = Mathf.Max(1.25f, r * 2.8f);
            Color core = InkTheme.Hex("FFF9D6");
            Color mid = InkTheme.Hex("7C5CFF");
            Color rim = InkTheme.Hex("23134F");
            float baseRot = Random.Range(-18f, 18f);
            // 第一道斜斩
            Bit s1 = FlowBit(InkFlowArt.SlashArc(), at, OrderTop, core, mid, rim,
                new Vector4(0f, -2.2f, 0f, 0f), 0.14f, 0.30f, 0.35f);
            s1.S0 = Size(s1.Sr.sprite, new Vector2(s * 0.65f, s * 1.15f));
            s1.S1 = Size(s1.Sr.sprite, new Vector2(s * 1.35f, s * 1.65f));
            s1.Curve = ScaleCurve.PopBack;
            s1.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, baseRot - 38f);
            // 第二道反手交叉斩，错峰 0.045s
            Bit s2 = FlowBit(InkFlowArt.SlashArc(), at, OrderTop, core, InkTheme.ThunderHi, rim,
                new Vector4(0f, -2.2f, 0f, 0f), 0.14f, 0.30f, 0.35f, 0.045f);
            s2.S0 = Size(s2.Sr.sprite, new Vector2(s * 0.6f, s * 1.05f));
            s2.S1 = Size(s2.Sr.sprite, new Vector2(s * 1.25f, s * 1.5f));
            s2.Curve = ScaleCurve.PopBack;
            s2.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, baseRot + 52f);
            // 中心电核 + 急刹高速电火花
            Flash(at, s * 0.25f, s * 1.1f, 0.14f);
            BurstSparks(at, 10, core, mid, 3.2f, 6.4f, 0.26f, 0.11f, OrderTop, 6f, 0.22f);
            InkShake.Punch(Camera.main, Vector2.down, 0.24f);
        }

        // 崩山：熔岩玄岩重锥轰砸压扁 -> 崩碎化作满地放射裂纹 + 金红冲击环 + 弹跳飞溅的棱角熔岩碎甲块。
        void Crumble(Vector2 at, float r)
        {
            float s = Mathf.Max(1.25f, r * 2.8f);
            Color core = InkTheme.Hex("FFF2B2");
            Color mid = InkTheme.Hex("FF6B2B");
            Color rim = InkTheme.Hex("3B160C");

            // 熔岩重锥本体砸落压扁崩裂闪现
            Sprite[] rockFrames = InkVfx.Frames("crumble_shot", 4);
            Sprite rock = rockFrames != null && rockFrames[0] != null ? rockFrames[0] : Pic("crumble_shot");
            if (rock != null)
            {
                Bit head = Add(rock, at + Vector2.up * 0.08f, OrderTop + 1, Color.white, false);
                head.Frames = rockFrames;
                head.S0 = Size(rock, s * 0.86f);
                head.S1 = Size(rock, s * 1.05f);
                head.Curve = ScaleCurve.SquashLand;
                head.Life = 0.24f;
                head.HoldAlpha = false;
            }

            // 地面硬边放射裂纹，留痕后噪声溶解
            Bit crack = FlowBit(InkFlowArt.CrackRing(), at + Vector2.down * 0.1f, OrderGround,
                InkTheme.Hex("FFE29A"), InkTheme.Hex("D96B27"), rim,
                new Vector4(0f, 0f, 0f, 0f), 0.05f, 0.62f, 0.55f);
            crack.S0 = Size(crack.Sr.sprite, s * 0.55f);
            crack.S1 = Size(crack.Sr.sprite, s * 1.48f);
            crack.Curve = ScaleCurve.PopBack;
            crack.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            // 破甲冲击环
            Bit ring = FlowBit(InkFlowArt.RingMask(), at, OrderHit, core, mid, rim,
                new Vector4(1.4f, -1.8f, 1f, 0.35f), 0.16f, 0.32f, 0.25f);
            ring.S0 = Size(ring.Sr.sprite, s * 0.35f);
            ring.S1 = Size(ring.Sr.sprite, s * 1.55f);
            Flash(at, s * 0.2f, s * 0.95f, 0.13f);
            // 棱角护甲/熔岩黑石块向上炸飞并弹跳落地
            BurstShards(at, 8, InkTheme.Hex("FFB84D"), InkTheme.Hex("4A3326"), 0.17f, 0.28f,
                2.5f, 4.8f, 0.54f, at.y - r * 0.55f, OrderTop + 2);
            BurstSparks(at, 9, core, mid, 2.8f, 5.6f, 0.28f, 0.10f, OrderTop + 2, 5.5f, 0.20f);
            InkShake.Punch(Camera.main, Vector2.down, 0.32f);
        }

        // 坍缩：三段式节奏 —— 0.18s 五刃黑紫奇点轮狂旋内缩 + 6 条引力丝带抽吸 -> 压成白热奇点 -> 轰然反冲炸开。
        void Collapse(Vector2 at, float r)
        {
            float d = Mathf.Max(1.3f, r * 2.1f);
            Color core = InkTheme.Hex("F6E8FF");
            Color mid = InkTheme.Hex("8A3FFC");
            Color rim = InkTheme.Hex("1C0933");

            Sprite[] colFrames = InkVfx.Frames("collapse_shot", 5);
            Sprite colOrb = colFrames != null && colFrames[0] != null ? colFrames[0] : Pic("collapse_shot");
            if (colOrb != null)
            {
                Bit wheel = Add(colOrb, at, OrderTop + 1, Color.white, false);
                wheel.Frames = colFrames;
                wheel.S0 = Size(colOrb, d * 1.15f);
                wheel.S1 = Size(colOrb, d * 0.18f);
                wheel.Curve = ScaleCurve.ShrinkIn;
                wheel.Spin = 520f;
                wheel.Life = 0.19f;
                wheel.HoldAlpha = true;
            }

            // 一段：极坐标旋涡底衬高速内吸
            Bit pull = FlowBit(InkFlowArt.VortexMask(), at, OrderHit, core, mid, rim,
                new Vector4(2.4f, 2.8f, 1f, 1.35f), 0.18f, 0.19f, 0.72f);
            pull.S0 = Size(pull.Sr.sprite, d * 1.35f);
            pull.S1 = Size(pull.Sr.sprite, d * 0.16f);
            pull.Curve = ScaleCurve.ShrinkIn;
            // 6 条弯曲引力丝带把四周能量抽进中心
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f + Random.Range(-0.06f, 0.06f)) * Mathf.PI * 2f;
                Vector2 start = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (d * 0.68f);
                FlowRibbon(start, at, (i % 2 == 0 ? 1f : -1f) * d * 0.24f, 0.18f, 0.10f, rim, mid, core, i * 0.01f);
            }
            // 二段：极短白热奇点蓄势
            Flash(at, d * 0.45f, d * 0.08f, 0.06f, 0.15f);
            // 三段：0.19s 后轰然反冲炸开（硬边黑紫流光爆团 + 外扩冲击环 + 急刹星屑）
            Bit boom = FlowBit(InkFlowArt.VortexMask(), at, OrderTop, core, InkTheme.Hex("B15CFF"), rim,
                new Vector4(-1.8f, -3.2f, 1f, 0.7f), 0.24f, 0.34f, 0.22f, 0.19f);
            boom.S0 = Size(boom.Sr.sprite, d * 0.18f);
            boom.S1 = Size(boom.Sr.sprite, d * 1.42f);
            boom.Curve = ScaleCurve.PopBack;
            Bit shock = FlowBit(InkFlowArt.RingMask(), at, OrderHit, core, mid, rim,
                new Vector4(-1.4f, -2.0f, 1f, 0.45f), 0.18f, 0.30f, 0.20f, 0.19f);
            shock.S0 = Size(shock.Sr.sprite, d * 0.2f);
            shock.S1 = Size(shock.Sr.sprite, d * 1.65f);
            Flash(at, d * 0.15f, d * 1.15f, 0.16f, 0.19f);
            BurstSparks(at, 12, core, InkTheme.Hex("C07BFF"), 3.4f, 6.8f, 0.30f, 0.11f, OrderTop + 2, 5.8f, 0.22f, 0.19f);
            InkShake.Punch(Camera.main, Vector2.up, 0.28f);
        }

        // 旋风：新绘五叶翡翠风轮核心急旋 + 外圈反向风环 + 螺旋风带丝带 + 翻飞月牙风刃。
        void Whirl(Vector2 at, float r)
        {
            float d = Mathf.Max(1.35f, r * 2.15f);
            Color core = InkTheme.Hex("E8FFF7");
            Color mid = InkTheme.Hex("2AD99B");
            Color rim = InkTheme.Hex("0D4A3B");
            // 外圈大风环（逆时针急旋，略压扁出透视感）
            Bit outer = FlowBit(InkFlowArt.RingMask(), at, OrderHit, core, mid, rim,
                new Vector4(2.6f, -1.6f, 1f, 1.45f), 0.22f, 0.48f, 0.42f);
            outer.S0 = Size(outer.Sr.sprite, new Vector2(d * 1.25f, d * 0.92f));
            outer.S1 = Size(outer.Sr.sprite, new Vector2(d * 0.42f, d * 0.34f));
            outer.Spin = 260f;

            // 内圈新绘五叶翡翠风轮（反向高速旋切）
            Sprite[] whirlFrames = InkVfx.Frames("whirl_shot", 5);
            Sprite whirlOrb = whirlFrames != null && whirlFrames[0] != null ? whirlFrames[0] : Pic("whirl_shot");
            if (whirlOrb != null)
            {
                Bit hub = Add(whirlOrb, at + Vector2.up * 0.05f, OrderTop + 1, Color.white, false);
                hub.Frames = whirlFrames;
                hub.S0 = Size(whirlOrb, d * 0.32f);
                hub.S1 = Size(whirlOrb, d * 0.88f);
                hub.Curve = ScaleCurve.PopBack;
                hub.Spin = -540f;
                hub.Life = 0.44f;
                hub.HoldAlpha = true;
            }

            // 3 条螺旋卷绕风带丝带
            for (int i = 0; i < 3; i++)
                FlowSpiral(at, d * 0.68f, d * 0.14f, i * Mathf.PI * 2f / 3f, 1.25f, 0.38f, 0.09f, rim, mid, core, i * 0.03f);
            // 4 道翻飞的硬边月牙风刃
            for (int i = 0; i < 4; i++)
            {
                float a = (i / 4f + Random.Range(-0.08f, 0.08f)) * Mathf.PI * 2f;
                Bit blade = FlowBit(InkFlowArt.SlashArc(), at, OrderTop + 2, core, mid, rim,
                    new Vector4(0f, -2f, 0f, 0f), 0.12f, 0.34f, 0.35f, i * 0.035f);
                blade.Orbit = true;
                blade.From = at;
                blade.OrbitAng = a;
                blade.OrbitRate = 11f;
                blade.Lift = d * 0.42f;
                blade.S0 = Size(blade.Sr.sprite, 0.48f);
                blade.S1 = Size(blade.Sr.sprite, 0.22f);
                blade.Spin = 620f;
            }
            BurstSparks(at, 8, core, mid, 2.0f, 4.2f, 0.32f, 0.09f, OrderTop + 2, 4.5f, 0.20f);
        }

        // 回锋：燕尾青金梭刃在折返点凌空翻转弹出 + 180° 青蓝弯月折返刀光 + 沿折返方向爆射的冷蓝速度针。
        void Riposte(Vector2 at, Vector2 toward)
        {
            Vector2 dir = (toward - at).sqrMagnitude > 1e-4f ? (toward - at).normalized : Vector2.down;
            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Color core = InkTheme.Hex("ECFAFF");
            Color mid = InkTheme.Hex("3CA8FF");
            Color rim = InkTheme.Hex("123066");

            Sprite bladeSpr = Pic("riposte_shot_00") ?? Pic("riposte_shot");
            if (bladeSpr != null)
            {
                Bit dagger = Add(bladeSpr, at + dir * 0.14f, OrderTop + 1, Color.white, false);
                dagger.S0 = Size(bladeSpr, 0.65f);
                dagger.S1 = Size(bladeSpr, 1.32f);
                dagger.Curve = ScaleCurve.PopBack;
                dagger.Life = 0.24f;
                dagger.HoldAlpha = false;
                dagger.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            }

            // 折返弯月刀光（弧背朝折返反方向，表现“刀锋急转调头”）
            Bit arc = FlowBit(InkFlowArt.SlashArc(), at - dir * 0.12f, OrderTop, core, mid, rim,
                new Vector4(0f, -2.6f, 0f, 0f), 0.14f, 0.28f, 0.28f);
            arc.S0 = Size(arc.Sr.sprite, new Vector2(0.85f, 0.65f));
            arc.S1 = Size(arc.Sr.sprite, new Vector2(1.55f, 1.25f));
            arc.Curve = ScaleCurve.PopBack;
            arc.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot + 180f);
            // 折返瞬间的尖锐穿刺光束丝带
            FlowRibbon(at - dir * 0.35f, at + dir * 1.15f, 0f, 0.20f, 0.12f, rim, mid, core, 0f);
            Flash(at, 0.18f, 0.95f, 0.12f);
            // 沿折返方向高速爆射的冷蓝与鎏金钢针火星
            Vector2 side = new Vector2(-dir.y, dir.x);
            for (int i = 0; i < 7; i++)
            {
                float spread = Random.Range(-0.45f, 0.45f);
                Vector2 v = (dir + side * spread).normalized * Random.Range(3.6f, 6.4f);
                Color sc = i % 3 == 0 ? InkTheme.Hex("FFD257") : i % 2 == 0 ? core : mid;
                Bit sp = Add(InkFx.Mote(), at, OrderTop + 2, sc, false);
                sp.Vel = v;
                sp.Drag = 5.5f;
                sp.StretchByVel = 0.24f;
                sp.Life = Random.Range(0.18f, 0.28f);
                sp.S0 = Vector2.one * Random.Range(0.10f, 0.15f);
                sp.S1 = sp.S0 * 0.2f;
            }
            InkShake.Punch(Camera.main, dir, 0.18f);
        }

        // ---------- 火地 ----------

        public static void SyncFields(List<FireField> fields)
        {
            if (_pool == null) return;
            _pool.Fields(fields);
        }

        void Fields(List<FireField> fields)
        {
            for (int i = 0; i < _patches.Count; i++) _patches[i].Seen = false;
            for (int i = 0; i < fields.Count; i++)
            {
                FireField f = fields[i];
                Patch p = FindPatch(f.Id);
                if (p == null)
                {
                    p = RentPatch(f);
                    Ignite(f);
                }
                p.Seen = true;
                DrawPatch(p, f);
            }
            for (int i = 0; i < _patches.Count; i++)
                if (!_patches[i].Seen && _patches[i].Id != 0) HidePatch(_patches[i]);
        }

        Patch FindPatch(int id)
        {
            for (int i = 0; i < _patches.Count; i++)
                if (_patches[i].Id == id) return _patches[i];
            return null;
        }

        const int FlameCap = 14;

        Patch RentPatch(FireField f)
        {
            Patch p = null;
            for (int i = 0; i < _patches.Count; i++)
                if (_patches[i].Id == 0) { p = _patches[i]; break; }
            if (p == null)
            {
                p = new Patch
                {
                    Scorch = Child("scorch", OrderScorch),
                    Glow = Child("glow", OrderScorch),
                    Flames = new SpriteRenderer[FlameCap],
                    Spots = new Vector2[FlameCap],
                    Phase = new float[FlameCap],
                    Height = new float[FlameCap]
                };
                for (int i = 0; i < FlameCap; i++) p.Flames[i] = Child("flame" + i, OrderFlame);
                _patches.Add(p);
            }
            p.Id = f.Id;
            p.Age = 0f;
            p.EmberAt = 0f;
            p.Scorch.sprite = InkFx.SoftDisc();
            InkFx.PaintSprite(p.Scorch, ScorchInk);
            p.Glow.sprite = InkFx.SoftDisc();
            InkFx.PaintSprite(p.Glow, BurnLooks.Number(f.Look));
            // 火苗沿圈摆一圈，圈里再撒几簇，前后错开节奏，读起来是一片在烧而不是一个贴图。
            int n = Mathf.Clamp(Mathf.RoundToInt(f.R * 8f), 7, FlameCap);
            int ring = Mathf.CeilToInt(n * 0.65f);
            for (int i = 0; i < FlameCap; i++)
            {
                bool on = i < n;
                p.Flames[i].enabled = on;
                if (!on) continue;
                Vector2 spot;
                if (i < ring)
                {
                    float a = (i + Random.Range(-0.25f, 0.25f)) / ring * Mathf.PI * 2f;
                    spot = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * f.R * Random.Range(0.68f, 0.85f);
                }
                else spot = Random.insideUnitCircle * f.R * 0.45f;
                p.Spots[i] = spot;
                p.Phase[i] = Random.Range(0f, 4f);
                p.Height[i] = Random.Range(0.42f, 0.62f);
                InkFx.PaintSprite(p.Flames[i], Color.white);
            }
            return p;
        }

        void Ignite(FireField f)
        {
            Ring(f.Pos, f.R * 0.4f, f.R * 2.3f, 0.35f, BurnLooks.Number(f.Look), 0.9f, OrderHit);
            Anim("hitv_fire", f.Pos, f.R * 1.2f, f.R * 1.9f, 0.4f, OrderHit - 1, 0f, 0.85f);
        }

        void DrawPatch(Patch p, FireField f)
        {
            float dt = Time.deltaTime;
            p.Age += dt;
            float born = Ease.OutBack(Mathf.Clamp01(p.Age / 0.22f));
            float fade = Mathf.Clamp01(f.Life / 0.45f);
            float t = Time.time;

            Place(p.Scorch, f.Pos, Vector2.one * f.R * 2.3f * Mathf.Min(1f, born), 0.42f * Mathf.Max(fade, 0.35f));
            float pulse = 0.85f + 0.15f * Mathf.Sin(t * 9f + p.Id);
            Place(p.Glow, f.Pos, Vector2.one * f.R * 1.9f * born * pulse, 0.38f * fade);

            Sprite[] frames = InkVfx.Frames(BurnLooks.Frames(f.Look));
            for (int i = 0; i < FlameCap; i++)
            {
                SpriteRenderer sr = p.Flames[i];
                if (!sr.enabled) continue;
                if (frames == null) { sr.enabled = false; continue; }
                int at = Mathf.FloorToInt(t * 12f + p.Phase[i] * 3f) % frames.Length;
                Sprite s = frames[at] ?? frames[0];
                sr.sprite = s;
                float h = p.Height[i] * born * Mathf.Lerp(0.35f, 1f, fade)
                          * (0.9f + 0.1f * Mathf.Sin(t * 14f + p.Phase[i] * 5f));
                float k = h / Mathf.Max(0.01f, s.bounds.size.y);
                Vector2 spot = f.Pos + p.Spots[i];
                // 火苗底边落在点位上，往上窜。
                sr.transform.position = new Vector3(spot.x, spot.y + h * 0.42f, 0f);
                sr.transform.localScale = new Vector3(k, k, 1f);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade * 1.2f));
            }

            if (f.Life > 0.2f && t >= p.EmberAt)
            {
                p.EmberAt = t + 0.07f;
                Vector2 at = f.Pos + Random.insideUnitCircle * f.R * 0.8f;
                Color c = Random.value < 0.5f ? BurnLooks.Number(f.Look) : InkTheme.FireHi;
                Bit b = Add(InkFx.Dot(), at, OrderMote - 1, c, false);
                b.Vel = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.9f, 1.5f));
                b.Life = Random.Range(0.5f, 0.75f);
                b.S0 = Vector2.one * Random.Range(0.08f, 0.13f);
                b.S1 = Vector2.one * 0.03f;
                b.HoldAlpha = true;
            }
        }

        void HidePatch(Patch p)
        {
            p.Id = 0;
            p.Scorch.enabled = false;
            p.Glow.enabled = false;
            for (int i = 0; i < FlameCap; i++) p.Flames[i].enabled = false;
        }

        static void Place(SpriteRenderer sr, Vector2 at, Vector2 size, float a)
        {
            sr.enabled = a > 0.005f;
            sr.transform.position = new Vector3(at.x, at.y, 0f);
            Vector2 k = Size(sr.sprite, size);
            sr.transform.localScale = new Vector3(k.x, k.y, 1f);
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }

        SpriteRenderer Child(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        // ---------- 小片 ----------

        static Vector2 Size(Sprite s, float world) => Size(s, Vector2.one * world);

        // 把「世界里要多大」换成缩放：按贴图最长边归一。
        static Vector2 Size(Sprite s, Vector2 world)
        {
            if (s == null) return world;
            Vector3 b = s.bounds.size;
            float span = Mathf.Max(0.001f, Mathf.Max(b.x, b.y));
            return world / span;
        }

        Bit Add(Sprite sprite, Vector2 at, int order, Color c, bool add)
        {
            Bit b = null;
            for (int i = 0; i < _bits.Count; i++)
                if (!_bits[i].On) { b = _bits[i]; break; }
            if (b == null)
            {
                if (_bits.Count >= 320) b = _bits[0];
                else
                {
                    var go = new GameObject("bit");
                    go.transform.SetParent(transform, false);
                    b = new Bit { Sr = go.AddComponent<SpriteRenderer>() };
                    _bits.Add(b);
                }
            }
            b.Sr.sprite = sprite;
            b.Sr.SetPropertyBlock(null);
            if (add) InkFx.PaintAdd(b.Sr, c);
            else InkFx.PaintSprite(b.Sr, c);
            b.Sr.sortingOrder = order;
            b.Sr.enabled = false;
            b.Sr.transform.localRotation = Quaternion.identity;
            b.Frames = null;
            b.Pos = b.From = b.To = at;
            b.Vel = Vector2.zero;
            b.Age = 0f;
            b.Life = 0.4f;
            b.Delay = 0f;
            b.Spin = 0f;
            b.Gravity = 0f;
            b.Lift = 0f;
            b.A0 = 1f;
            b.S0 = b.S1 = Vector2.one;
            b.C = c;
            b.Travel = false;
            b.HoldAlpha = false;
            b.Orbit = false;
            b.OrbitAng = 0f;
            b.OrbitRate = 0f;
            b.Curve = ScaleCurve.EaseOut;
            b.Drag = 3f;
            b.StretchByVel = 0f;
            b.FloorY = 0f;
            b.Bounce = 0f;
            b.HasFloor = false;
            b.Flow = false;
            b.Distort = 0.18f;
            b.Sharp = 1f;
            b.Phase = 0f;
            b.DissolveAt = 0.35f;
            b.On = true;
            return b;
        }

        // 平涂序列帧，播一遍。
        Bit Anim(string set, Vector2 at, float s0, float s1, float life, int order, float delay = 0f, float a0 = 1f)
        {
            Sprite[] frames = InkVfx.Frames(set);
            if (frames == null || frames[0] == null) return null;
            Bit b = Add(frames[0], at, order, Color.white, false);
            b.Frames = frames;
            b.S0 = Size(frames[0], s0);
            b.S1 = Size(frames[0], s1);
            b.Life = life;
            b.Delay = delay;
            b.A0 = a0;
            b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));
            return b;
        }

        // 第二幕的发光单张。
        Bit Glow(string name, Vector2 at, float s0, float s1, float life, int order, float delay)
        {
            Sprite s = Pic(name);
            if (s == null) return null;
            Bit b = Add(s, at, order, Color.white, false);
            b.S0 = Size(s, s0);
            b.S1 = Size(s, s1);
            b.Life = life;
            b.Delay = delay;
            return b;
        }

        void Ring(Vector2 at, float d0, float d1, float life, Color c, float a0, int order, float delay = 0f)
        {
            Sprite ring = InkFx.SoftRing();
            Bit b = Add(ring, at, order, c, false);
            b.S0 = Size(ring, d0);
            b.S1 = Size(ring, d1);
            b.Life = life;
            b.A0 = a0;
            b.Delay = delay;
        }

        void Mark(Vector2 at, float d0, float d1, float life, Color c, float a0)
        {
            Sprite disc = InkFx.SoftDisc();
            Bit b = Add(disc, at, OrderScorch, c, false);
            b.S0 = Size(disc, d0);
            b.S1 = Size(disc, d1);
            b.Life = life;
            b.A0 = a0;
            b.HoldAlpha = true;
        }

        void Flash(Vector2 at, float d0, float d1, float life, float delay = 0f)
        {
            Sprite disc = InkFx.SoftDisc();
            Bit b = Add(disc, at, OrderTop, Color.white, true);
            b.S0 = Size(disc, d0);
            b.S1 = Size(disc, d1);
            b.Life = life;
            b.Delay = delay;
            b.A0 = 0.9f;
        }

        // 三字诀的击中用素材包里的爆点，播一遍，不再放大成一圈圈程序环。
        static float ToonSize(float size)
        {
            if (size >= 3f) return 1.75f;
            if (size >= 2f) return 1.5f;
            return 1.28f;
        }

        void Cartoon(string set, Vector2 at, float size)
        {
            int n = InkVfx.CountFrames(set);
            if (n <= 0) return;
            Sprite[] frames = InkVfx.Frames(set, n);
            if (frames == null || frames[0] == null) return;
            Bit b = Add(frames[0], at, OrderTop, Color.white, false);
            b.Frames = frames;
            b.S0 = Size(frames[0], size);
            b.S1 = b.S0;
            b.Life = Mathf.Max(0.42f, n * 0.08f);
            b.A0 = 1f;
            b.HoldAlpha = true;
        }

        // 风火轮（三字诀）：青铜镂空法轮裹挟烈火与青风刀刃高速旋斩，折返时更爆开双色风火气浪与螺旋流带。
        void FireWheel(Vector2 at, float size)
        {
            bool big = size >= 2f;
            float s = big ? ToonSize(size) * 1.18f : 1.22f;
            Color fireCore = InkTheme.Hex("FFF6C2");
            Color fireMid = InkTheme.Hex("FF6A1A");
            Color fireRim = InkTheme.Hex("4D1206");
            Color windCore = InkTheme.Hex("E8FFF5");
            Color windMid = InkTheme.Hex("1DB992");
            Color windRim = InkTheme.Hex("0B3D30");

            // 核心主体：新绘国风卡通「风火轮」法宝高速旋转弹开
            Sprite[] wheelFrames = InkVfx.Frames("firewheel_shot", 5);
            Sprite wheel = wheelFrames != null && wheelFrames[0] != null ? wheelFrames[0] : Pic("firewheel_shot");
            if (wheel != null)
            {
                Bit hub = Add(wheel, at, OrderTop + 1, Color.white, false);
                hub.Frames = wheelFrames;
                hub.S0 = Size(wheel, s * (big ? 0.48f : 0.62f));
                hub.S1 = Size(wheel, s * (big ? 1.18f : 1.08f));
                hub.Curve = ScaleCurve.PopBack;
                hub.Spin = big ? -640f : -560f;
                hub.Life = big ? 0.46f : 0.32f;
                hub.HoldAlpha = true;
                hub.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }

            if (big)
            {
                // 折返大爆发：外圈青风气浪环 + 内圈烈火环反向高速交错
                Bit wind = FlowBit(InkFlowArt.RingMask(), at, OrderHit, windCore, windMid, windRim,
                    new Vector4(3.2f, -1.8f, 1f, 1.3f), 0.22f, 0.46f, 0.36f);
                wind.S0 = Size(wind.Sr.sprite, s * 0.45f);
                wind.S1 = Size(wind.Sr.sprite, s * 1.46f);
                wind.Curve = ScaleCurve.PopBack;
                wind.Spin = -420f;

                Bit fire = FlowBit(InkFlowArt.RingMask(), at, OrderTop, fireCore, fireMid, fireRim,
                    new Vector4(-3.6f, -2.4f, 1f, 1.5f), 0.24f, 0.40f, 0.34f, 0.02f);
                fire.S0 = Size(fire.Sr.sprite, s * 0.32f);
                fire.S1 = Size(fire.Sr.sprite, s * 1.14f);
                fire.Curve = ScaleCurve.PopBack;
                fire.Spin = 480f;

                FlowSpiral(at, s * 0.78f, s * 0.24f, 0f, 1.05f, 0.34f, 0.09f, fireRim, fireMid, fireCore, 0f);
                FlowSpiral(at, s * 0.78f, s * 0.24f, Mathf.PI, 1.05f, 0.34f, 0.09f, windRim, windMid, windCore, 0.02f);
                Flash(at, s * 0.22f, s * 1.15f, 0.15f);
                BurstSparks(at, 9, fireCore, fireMid, 3.2f, 6.5f, 0.35f, 0.12f, OrderTop + 2, 4.8f, 0.22f);
                BurstSparks(at, 6, windCore, windMid, 3.0f, 5.8f, 0.32f, 0.11f, OrderTop + 2, 4.8f, 0.22f);
                InkShake.Punch(Camera.main, Vector2.down, 0.28f);
            }
            else
            {
                // 穿敌旋斩：一火一风两道切向刀弧 + 飞溅风火流萤
                float baseAng = Random.Range(0f, 360f);
                for (int i = 0; i < 2; i++)
                {
                    bool isFire = i == 0;
                    Bit arc = FlowBit(InkFlowArt.SlashArc(), at, OrderTop + 2,
                        isFire ? fireCore : windCore,
                        isFire ? fireMid : windMid,
                        isFire ? fireRim : windRim,
                        new Vector4(isFire ? 2.6f : -2.6f, 0f, 0f, 0f), 0.14f, 0.24f, 0.32f);
                    arc.S0 = Size(arc.Sr.sprite, s * 0.65f);
                    arc.S1 = Size(arc.Sr.sprite, s * 1.22f);
                    arc.Curve = ScaleCurve.PopBack;
                    arc.Spin = isFire ? -420f : 420f;
                    arc.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, baseAng + i * 180f);
                }
                Flash(at, 0.2f, s * 0.85f, 0.12f);
                BurstSparks(at, 6, fireCore, fireMid, 2.6f, 5.2f, 0.28f, 0.10f, OrderTop + 2, 5.0f, 0.20f);
                BurstSparks(at, 4, windCore, windMid, 2.4f, 4.8f, 0.26f, 0.10f, OrderTop + 2, 5.0f, 0.20f);
            }
        }

        // 穿云箭（三字诀）：金色破云气浪锥 + 沿穿刺方向爆射的高速拉丝穿云光针。
        void Cloud(Vector2 at, float size)
        {
            float s = Mathf.Clamp(size, 0.55f, 1.05f);
            Color core = InkTheme.Hex("FFFBE0");
            Color mid = InkTheme.Hex("FFBD2E");
            Color rim = InkTheme.Hex("592E08");
            // 破云气浪弧（朝下穿刺方向）
            Bit cone = FlowBit(InkFlowArt.SlashArc(), at, OrderTop, core, mid, rim,
                new Vector4(0f, -2.4f, 0f, 0f), 0.12f, 0.24f, 0.30f);
            cone.S0 = Size(cone.Sr.sprite, new Vector2(s * 0.65f, s * 0.9f));
            cone.S1 = Size(cone.Sr.sprite, new Vector2(s * 1.35f, s * 1.45f));
            cone.Curve = ScaleCurve.PopBack;
            cone.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            // 尖锐金色穿云丝带
            FlowRibbon(at + Vector2.up * (s * 0.45f), at + Vector2.down * (s * 1.1f), 0f, 0.18f, 0.10f, rim, mid, core, 0f);
            // 6 根沿穿刺扇区高速爆射的速度拉伸金针
            for (int i = 0; i < 6; i++)
            {
                float ang = (-90f + Random.Range(-34f, 34f)) * Mathf.Deg2Rad;
                Vector2 v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(3.6f, 6.4f);
                Bit b = Add(InkFx.Mote(), at, OrderTop, i % 2 == 0 ? core : mid, false);
                b.Vel = v;
                b.Drag = 5.5f;
                b.StretchByVel = 0.26f;
                b.Life = Random.Range(0.18f, 0.28f);
                b.S0 = Vector2.one * Random.Range(0.10f, 0.15f);
                b.S1 = b.S0 * 0.2f;
            }
        }

        // 定海针（三字诀）：金箍玄岩冰晶神柱从天贯落 -> 砸地压扁回弹镇压全场 -> 两侧迸出斜刺冰棱 + 地面冰裂与弹跳岩冰碎块。
        void Needle(Vector2 at, float size)
        {
            float s = ToonSize(size);
            Color core = InkTheme.Hex("EBFBFF");
            Color mid = InkTheme.Hex("3BC4FF");
            Color rim = InkTheme.Hex("102642");
            Color goldMid = InkTheme.Hex("F2B632");
            Color goldRim = InkTheme.Hex("4A2E08");
            Vector2 ground = at + Vector2.down * 0.18f;

            Sprite[] needleFrames = InkVfx.Frames("needle_shot", 4);
            Sprite pillar = needleFrames != null && needleFrames[0] != null ? needleFrames[0] : Pic("needle_shot");
            float pinH = s * 1.72f;
            Vector2 landedCenter = ground + Vector2.up * (pinH * 0.44f);

            // 一段（0~0.095s）：地面先向内急缩聚怪引力环 + 定海神针拖着金青贯日流光从天砸落
            Bit gather = FlowBit(InkFlowArt.RingMask(), ground, OrderGround + 1, core, goldMid, goldRim,
                new Vector4(-2.4f, 1.8f, 1f, 0.7f), 0.14f, 0.11f, 0.85f);
            gather.S0 = Size(gather.Sr.sprite, new Vector2(s * 1.45f, s * 0.95f));
            gather.S1 = Size(gather.Sr.sprite, new Vector2(s * 0.36f, s * 0.24f));
            gather.Curve = ScaleCurve.ShrinkIn;

            FlowRibbon(ground + Vector2.up * 3.1f, ground, 0f, 0.14f, 0.14f, goldRim, goldMid, core, 0f);

            if (pillar != null)
            {
                Vector2 baseScale = Size(pillar, pinH);
                Bit drop = Add(pillar, ground + Vector2.up * 2.95f, OrderTop + 2, Color.white, false);
                drop.Travel = true;
                drop.From = ground + Vector2.up * 2.95f;
                drop.To = landedCenter;
                drop.Lift = 0f;
                drop.Life = 0.095f;
                drop.HoldAlpha = true;
                drop.S0 = Vector2.Scale(baseScale, new Vector2(0.82f, 1.28f));
                drop.S1 = baseScale;

                // 二段（0.095s 砸地）：神柱镇地压扁回弹，柱身寒脉闪烁
                Bit pin = Add(pillar, landedCenter, OrderTop + 2, Color.white, false);
                pin.Frames = needleFrames;
                pin.S0 = baseScale;
                pin.S1 = baseScale;
                pin.Curve = ScaleCurve.SquashLand;
                pin.Life = 0.50f;
                pin.Delay = 0.092f;
                pin.HoldAlpha = true;
            }
            else
            {
                Bit pin = FlowBit(InkFlowArt.PillarMask(), ground, OrderTop + 1, core, mid, rim,
                    new Vector4(0f, -1.2f, 0f, 0f), 0.14f, 0.46f, 0.55f, 0.092f);
                pin.S0 = Size(pin.Sr.sprite, new Vector2(s * 0.62f, s * 1.48f));
                pin.S1 = pin.S0;
                pin.Curve = ScaleCurve.SquashLand;
            }

            // 两侧对称迸出斜刺寒冰副棱，托住主神柱形成山字形镇海阵势
            for (int side = -1; side <= 1; side += 2)
            {
                float ang = -side * 34f;
                Vector2 offset = new Vector2(side * s * 0.36f, s * 0.22f);
                Bit spike = FlowBit(InkFlowArt.PillarMask(), ground + offset * 0.4f, OrderTop + 1, core, mid, rim,
                    new Vector4(0f, -1.8f, 0f, 0f), 0.12f, 0.38f, 0.45f, 0.10f);
                spike.Travel = true;
                spike.From = ground + new Vector2(side * s * 0.14f, 0.02f);
                spike.To = ground + offset;
                spike.Lift = 0f;
                spike.S0 = Size(spike.Sr.sprite, new Vector2(s * 0.22f, s * 0.35f));
                spike.S1 = Size(spike.Sr.sprite, new Vector2(s * 0.34f, s * 0.88f));
                spike.Curve = ScaleCurve.PopBack;
                spike.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            }

            // 地面爆开硬边冰霜裂纹 + 外扩冰金双层气浪环 + 弹跳岩冰碎块
            Bit crack = FlowBit(InkFlowArt.CrackRing(), ground, OrderGround, core, mid, rim,
                new Vector4(0f, 0f, 0f, 0f), 0.06f, 0.74f, 0.55f, 0.095f);
            crack.S0 = Size(crack.Sr.sprite, s * 0.48f);
            crack.S1 = Size(crack.Sr.sprite, s * 1.72f);
            crack.Curve = ScaleCurve.PopBack;
            crack.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Bit ring = FlowBit(InkFlowArt.RingMask(), ground, OrderHit, core, mid, rim,
                new Vector4(1.6f, -1.5f, 1f, 0.4f), 0.16f, 0.34f, 0.26f, 0.095f);
            ring.S0 = Size(ring.Sr.sprite, new Vector2(s * 0.38f, s * 0.26f));
            ring.S1 = Size(ring.Sr.sprite, new Vector2(s * 1.82f, s * 1.24f));

            Flash(ground, s * 0.24f, s * 1.25f, 0.14f, 0.095f);
            BurstShards(ground, 6, InkTheme.Hex("8CE8FF"), InkTheme.Hex("3898D8"), 0.17f, 0.27f,
                2.4f, 4.6f, 0.54f, ground.y - 0.15f, OrderTop + 2, 0.095f);
            BurstShards(ground, 4, goldMid, InkTheme.Hex("6B5033"), 0.15f, 0.24f,
                2.0f, 4.0f, 0.50f, ground.y - 0.15f, OrderTop + 2, 0.095f);
            BurstSparks(ground, 10, core, goldMid, 2.8f, 5.8f, 0.32f, 0.11f, OrderTop + 3, 5.2f, 0.22f, 0.095f);
            InkShake.Punch(Camera.main, Vector2.down, 0.38f);
        }

        // 封：黑紫硬边符咒锁环极速旋转收紧 + 四道向心封锁紫芒。
        void SealMark(Vector2 at, float size)
        {
            float s = Mathf.Max(0.95f, size * 2.3f);
            Color core = InkTheme.Hex("F2DEFF");
            Color mid = InkTheme.Hex("8F47FF");
            Color rim = InkTheme.Hex("210B3D");
            Bit lockRing = FlowBit(InkFlowArt.RingMask(), at, OrderTop, core, mid, rim,
                new Vector4(-2.2f, 1.5f, 1f, 0.65f), 0.14f, 0.42f, 0.52f);
            lockRing.S0 = Size(lockRing.Sr.sprite, s * 1.35f);
            lockRing.S1 = Size(lockRing.Sr.sprite, s * 0.58f);
            lockRing.Curve = ScaleCurve.ShrinkIn;
            lockRing.Spin = -260f;
            for (int i = 0; i < 4; i++)
            {
                float a = (i / 4f + 0.125f) * Mathf.PI * 2f;
                Vector2 p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (s * 0.72f);
                FlowRibbon(p, at, 0f, 0.24f, 0.07f, rim, mid, core, i * 0.02f);
            }
            Flash(at, s * 0.25f, s * 0.08f, 0.16f);
        }

        // 失魂（封+惑）：彻底告别违和 3D 爱心！黑紫硬边符咒锁环收紧（封）+ 两缕摇曳扭动的幽紫/绛粉卡通魂影从头顶抽离上飘（惑）。
        void Soul(Vector2 at, float size)
        {
            float s = Mathf.Clamp(size, 0.65f, 1.15f);
            Color sealCore = InkTheme.Hex("F6E4FF");
            Color sealMid = InkTheme.Hex("8B3DFF");
            Color sealRim = InkTheme.Hex("1F0A38");
            Color wispCore = InkTheme.Hex("FFE6F3");
            Color wispMid = InkTheme.Hex("E8489C");
            Color wispRim = InkTheme.Hex("380F47");
            // 1. 周身极速收紧的符咒锁环（封）
            Bit lockRing = FlowBit(InkFlowArt.RingMask(), at, OrderHit, sealCore, sealMid, sealRim,
                new Vector4(-2.6f, 1.8f, 1f, 0.85f), 0.16f, 0.36f, 0.48f);
            lockRing.S0 = Size(lockRing.Sr.sprite, s * 1.45f);
            lockRing.S1 = Size(lockRing.Sr.sprite, s * 0.42f);
            lockRing.Curve = ScaleCurve.ShrinkIn;
            lockRing.Spin = -300f;
            // 2. 两缕摇曳扭动的卡通魂影墨焰从怪身抽离打旋上飘（惑）
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Bit wisp = FlowBit(InkFlowArt.WispMask(), at, OrderTop,
                    i == 0 ? wispCore : sealCore,
                    i == 0 ? wispMid : sealMid,
                    i == 0 ? wispRim : sealRim,
                    new Vector4(side * 0.5f, -2.4f, 0f, 0f), 0.26f, 0.48f, 0.36f, i * 0.05f);
                wisp.Travel = true;
                wisp.From = at + new Vector2(side * s * 0.10f, -s * 0.05f);
                wisp.To = at + new Vector2(-side * s * 0.22f, s * (0.95f + i * 0.18f));
                wisp.Lift = side * s * 0.36f;
                wisp.S0 = Size(wisp.Sr.sprite, new Vector2(s * 0.52f, s * 0.72f));
                wisp.S1 = Size(wisp.Sr.sprite, new Vector2(s * 0.26f, s * 1.05f));
                wisp.Spin = side * 22f;
            }
            // 3. 轻盈游丝火星打旋上升
            Flash(at, s * 0.22f, s * 0.06f, 0.12f);
            for (int i = 0; i < 6; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Bit sp = Add(InkFx.Mote(), at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * 0.22f,
                    OrderTop, i % 2 == 0 ? wispMid : sealCore, false);
                sp.Vel = new Vector2(Random.Range(-1.1f, 1.1f), Random.Range(1.4f, 2.8f));
                sp.Drag = 3.2f;
                sp.StretchByVel = 0.18f;
                sp.Life = Random.Range(0.28f, 0.44f);
                sp.S0 = Vector2.one * Random.Range(0.08f, 0.13f);
                sp.S1 = sp.S0 * 0.2f;
                sp.Delay = i * 0.02f;
            }
        }

        // 影弹 / 鬼火（影+火）：单独影弹留一道极速溶解暗紫墨环；鬼火窜起 3 缕幽冥紫边橙芯摇曳墨焰 + 飘摇火星。
        void Ghost(Vector2 at, float size)
        {
            bool blaze = size >= 0.9f;
            float s = blaze ? 1.02f : 0.62f;
            Color shadeCore = InkTheme.Hex("E9DCFF");
            Color shadeMid = InkTheme.Hex("6B35D6");
            Color shadeRim = InkTheme.Hex("1A0930");
            if (!blaze)
            {
                Bit ring = FlowBit(InkFlowArt.RingMask(), at, OrderHit, shadeCore, shadeMid, shadeRim,
                    new Vector4(1.4f, -1.8f, 1f, 0.4f), 0.18f, 0.26f, 0.25f);
                ring.S0 = Size(ring.Sr.sprite, s * 0.25f);
                ring.S1 = Size(ring.Sr.sprite, s * 1.1f);
                Flash(at, s * 0.14f, s * 0.04f, 0.1f);
                return;
            }
            Color ghostCore = InkTheme.Hex("FFF3C4");
            Color ghostMid = InkTheme.Hex("FF6A14");
            Color ghostRim = InkTheme.Hex("3B0E5C");
            // 暗紫鬼火底环
            Bit baseRing = FlowBit(InkFlowArt.RingMask(), at, OrderHit, ghostCore, shadeMid, ghostRim,
                new Vector4(1.8f, -2.0f, 1f, 0.6f), 0.20f, 0.30f, 0.25f);
            baseRing.S0 = Size(baseRing.Sr.sprite, s * 0.28f);
            baseRing.S1 = Size(baseRing.Sr.sprite, s * 1.25f);
            Flash(at, s * 0.22f, s * 0.05f, 0.11f);
            // 3 缕错落摇曳窜起的幽冥墨焰（纵向强噪声扭曲，动态如真火舔舐）
            for (int i = 0; i < 3; i++)
            {
                float side = (i - 1) * s * 0.24f;
                Bit tongue = FlowBit(InkFlowArt.WispMask(), at, OrderTop, ghostCore, ghostMid, ghostRim,
                    new Vector4((i - 1) * 0.4f, -3.2f, 0f, 0f), 0.28f, 0.38f + i * 0.04f, 0.32f, i * 0.03f);
                tongue.Travel = true;
                tongue.From = at + new Vector2(side * 0.4f, -0.06f);
                tongue.To = at + new Vector2(side * 1.1f, s * (0.62f + (i == 1 ? 0.24f : 0.06f)));
                tongue.Lift = side * 0.45f;
                float w = i == 1 ? 0.56f : 0.40f;
                tongue.S0 = Size(tongue.Sr.sprite, new Vector2(s * w, s * (w * 1.25f)));
                tongue.S1 = Size(tongue.Sr.sprite, new Vector2(s * w * 0.65f, s * (w * 1.75f)));
            }
            // 10 颗向上飘摇的速度拉伸橙/紫火星
            for (int i = 0; i < 10; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Color c = i % 3 == 0 ? ghostCore : i % 2 == 0 ? ghostMid : InkTheme.Hex("A855FF");
                Bit b = Add(InkFx.Mote(), at, OrderTop, c, false);
                b.Vel = new Vector2(Mathf.Cos(a) * Random.Range(0.9f, 2.1f), Mathf.Sin(a) * Random.Range(0.6f, 1.5f) + 1.4f);
                b.Drag = 4.2f;
                b.StretchByVel = 0.20f;
                b.Life = Random.Range(0.24f, 0.40f);
                b.S0 = Vector2.one * Random.Range(0.08f, 0.13f);
                b.S1 = b.S0 * 0.2f;
                b.Delay = Random.Range(0f, 0.05f);
            }
        }

        void Spray(Vector2 at, int n, Sprite sprite, float size, float lo, float hi, float life, Color c, int order,
            float gravity, float delay = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = (i + Random.Range(-0.35f, 0.35f)) / n * Mathf.PI * 2f;
                Bit b = Add(sprite, at, order, c, false);
                b.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(lo, hi);
                b.Gravity = gravity;
                b.Life = life * Random.Range(0.8f, 1.15f);
                b.S0 = Vector2.one * size;
                b.S1 = Vector2.one * size * 0.3f;
                b.Delay = delay;
                b.Spin = Random.Range(-360f, 360f);
            }
        }

        // ---------- 动态流体 / 丝带 / 物理碎屑辅助 ----------

        Bit FlowBit(Sprite mask, Vector2 at, int order, Color core, Color mid, Color rim,
            Vector4 flow, float distort, float life, float dissolveAt = 0.35f, float delay = 0f)
        {
            Bit b = Add(mask, at, order, Color.white, false);
            b.Flow = true;
            b.Core = core;
            b.Mid = mid;
            b.Rim = rim;
            b.FlowVec = flow;
            b.Distort = distort;
            b.Sharp = 1f;
            b.Phase = Random.Range(0f, 10f);
            b.DissolveAt = dissolveAt;
            b.Life = life;
            b.Delay = delay;
            b.A0 = 1f;
            return b;
        }

        // 高速急刹 + 速度方向拉伸火花（打破均匀圆周分布，自然错落）。
        void BurstSparks(Vector2 at, int n, Color core, Color mid, float vLo, float vHi,
            float life, float size, int order, float drag, float stretch, float delay = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = (i + Random.Range(-0.42f, 0.42f)) / n * Mathf.PI * 2f;
                float spd = Random.Range(vLo, vHi) * (i % 3 == 0 ? 1.25f : 0.85f);
                Bit b = Add(InkFx.Mote(), at, order, i % 3 == 0 ? core : mid, false);
                b.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spd;
                b.Drag = drag;
                b.StretchByVel = stretch;
                b.Life = life * Random.Range(0.75f, 1.2f);
                b.S0 = Vector2.one * (size * Random.Range(0.75f, 1.3f));
                b.S1 = b.S0 * 0.18f;
                b.Delay = delay + Random.Range(0f, 0.03f);
            }
        }

        // 带深色描边的锐利多边形碎块：向上炸飞、重力下坠、落地弹跳。
        void BurstShards(Vector2 at, int n, Color hi, Color lo, float sLo, float sHi,
            float vLo, float vHi, float life, float floorY, int order, float delay = 0f)
        {
            Sprite shard = InkFlowArt.ShardPoly();
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(20f, 160f, (i + Random.Range(0.1f, 0.9f)) / n) * Mathf.Deg2Rad;
                Vector2 v = new Vector2(Mathf.Cos(a) * Random.Range(0.7f, 1.35f), Mathf.Sin(a)) * Random.Range(vLo, vHi);
                Bit b = Add(shard, at + Random.insideUnitCircle * 0.12f, order, i % 2 == 0 ? hi : lo, false);
                b.Vel = v;
                b.Gravity = -12.5f;
                b.Drag = 0.6f;
                b.HasFloor = true;
                b.FloorY = floorY + Random.Range(-0.08f, 0.08f);
                b.Bounce = Random.Range(0.35f, 0.55f);
                b.Life = life * Random.Range(0.85f, 1.15f);
                float sz = Random.Range(sLo, sHi);
                b.S0 = Size(shard, sz);
                b.S1 = Size(shard, sz * 0.72f);
                b.HoldAlpha = true;
                b.Delay = delay;
                b.Spin = Random.Range(-620f, 620f);
                b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }
        }

        void FlowRibbon(Vector2 from, Vector2 to, float lift, float life, float width, Color rim, Color mid, Color core, float delay = 0f)
        {
            RibbonBit r = RentRibbon();
            r.From = from;
            Vector2 d = to - from;
            Vector2 side = d.sqrMagnitude > 1e-4f ? new Vector2(-d.y, d.x).normalized : Vector2.right;
            r.Ctrl = (from + to) * 0.5f + side * lift;
            r.To = to;
            r.Spiral = false;
            r.Age = 0f;
            r.Life = Mathf.Max(0.06f, life);
            r.Delay = delay;
            r.Width = width * 1.15f;
            r.Rim = rim;
            r.Mid = mid;
            r.Core = core;
            r.On = true;
        }

        void FlowSpiral(Vector2 center, float r0, float r1, float ang0, float turns, float life, float width,
            Color rim, Color mid, Color core, float delay = 0f)
        {
            RibbonBit r = RentRibbon();
            r.Spiral = true;
            r.Center = center;
            r.R0 = r0;
            r.R1 = r1;
            r.Ang0 = ang0;
            r.Turns = turns;
            r.Age = 0f;
            r.Life = Mathf.Max(0.08f, life);
            r.Delay = delay;
            r.Width = width * 1.15f;
            r.Rim = rim;
            r.Mid = mid;
            r.Core = core;
            r.On = true;
        }

        RibbonBit RentRibbon()
        {
            for (int i = 0; i < _ribbons.Count; i++)
                if (!_ribbons[i].On) return _ribbons[i];
            if (_ribbons.Count >= 36) return _ribbons[0];
            var rb = new RibbonBit
            {
                Outer = MakeLine("ribOuter", OrderTop),
                Body = MakeLine("ribBody", OrderTop + 1),
                Inner = MakeLine("ribInner", OrderTop + 2)
            };
            _ribbons.Add(rb);
            return rb;
        }

        LineRenderer MakeLine(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = RibbonPts;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 3;
            lr.sortingOrder = order;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sharedMaterial = InkFx.SpriteMat();
            lr.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.55f, 1f),
                new Keyframe(1f, 0.10f));
            lr.enabled = false;
            return lr;
        }

        void StepRibbons(float dt)
        {
            for (int i = 0; i < _ribbons.Count; i++)
            {
                RibbonBit r = _ribbons[i];
                if (!r.On) continue;
                if (r.Delay > 0f) { r.Delay -= dt; continue; }
                r.Age += dt;
                float k = Mathf.Clamp01(r.Age / r.Life);
                if (k >= 1f)
                {
                    r.On = false;
                    r.Outer.enabled = false;
                    r.Body.enabled = false;
                    r.Inner.enabled = false;
                    continue;
                }
                // 丝带头部先冲出，尾部随后收拢，形成动态挥砍/抽吸轨迹
                float head = Ease.OutCubic(Mathf.Clamp01(k * 1.45f));
                float tail = InCubic(Mathf.Clamp01((k - 0.22f) / 0.78f));
                if (head - tail < 0.01f)
                {
                    r.Outer.enabled = false;
                    r.Body.enabled = false;
                    r.Inner.enabled = false;
                    continue;
                }
                for (int p = 0; p < RibbonPts; p++)
                {
                    float u = Mathf.Lerp(tail, head, p / (float)(RibbonPts - 1));
                    Vector2 pt;
                    if (r.Spiral)
                    {
                        float rad = Mathf.Lerp(r.R0, r.R1, u);
                        float ang = r.Ang0 + u * r.Turns * Mathf.PI * 2f;
                        pt = r.Center + new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad * 0.72f);
                    }
                    else
                    {
                        float inv = 1f - u;
                        pt = inv * inv * r.From + 2f * inv * u * r.Ctrl + u * u * r.To;
                    }
                    _rPts[p] = new Vector3(pt.x, pt.y, 0f);
                }
                float fade = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
                float w = r.Width * (0.45f + 0.55f * fade);
                r.Outer.enabled = true;
                r.Outer.SetPositions(_rPts);
                r.Outer.widthMultiplier = w;
                Color rim = r.Rim; rim.a = fade;
                r.Outer.startColor = r.Outer.endColor = rim;

                r.Body.enabled = true;
                r.Body.SetPositions(_rPts);
                r.Body.widthMultiplier = w * 0.66f;
                Color mid = r.Mid; mid.a = fade;
                r.Body.startColor = r.Body.endColor = mid;

                r.Inner.enabled = true;
                r.Inner.SetPositions(_rPts);
                r.Inner.widthMultiplier = w * 0.26f;
                Color core = r.Core; core.a = fade;
                r.Inner.startColor = r.Inner.endColor = core;
            }
        }

        static Vector2 EvalScale(Bit b, float k)
        {
            switch (b.Curve)
            {
                case ScaleCurve.PopBack:
                {
                    float p = Ease.OutBack(Mathf.Clamp01(k / 0.28f));
                    return Vector2.LerpUnclamped(b.S0, b.S1, p);
                }
                case ScaleCurve.SquashLand:
                {
                    // 砸地瞬间（前 0.22）先压扁变宽，再弹性回弹到原始比例
                    float u = Mathf.Clamp01(k / 0.26f);
                    float bump = Mathf.Sin(u * Mathf.PI) * (1f - u * 0.45f);
                    return new Vector2(b.S1.x * (1f + 0.36f * bump), b.S1.y * (1f - 0.24f * bump));
                }
                case ScaleCurve.ShrinkIn:
                    return Vector2.Lerp(b.S0, b.S1, InCubic(k));
                default:
                    return Vector2.Lerp(b.S0, b.S1, Ease.OutCubic(k));
            }
        }

        static float InCubic(float t) => t * t * t;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            for (int i = 0; i < _bits.Count; i++)
            {
                Bit b = _bits[i];
                if (!b.On) continue;
                if (b.Delay > 0f) { b.Delay -= dt; continue; }
                b.Age += dt;
                float k = Mathf.Clamp01(b.Age / b.Life);
                if (k >= 1f)
                {
                    b.On = false;
                    b.Sr.enabled = false;
                    continue;
                }
                if (b.Orbit)
                {
                    float rate = Mathf.Abs(b.OrbitRate) > 0.01f ? b.OrbitRate : 9f;
                    float ang = b.OrbitAng + b.Age * rate;
                    float rad = b.Lift * (0.72f + 0.28f * Mathf.Sin(b.Age * 10f));
                    b.Pos = b.From + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.62f) * rad;
                }
                else if (b.Travel)
                {
                    float e = k * k * (3f - 2f * k);
                    Vector2 mid = (b.From + b.To) * 0.5f;
                    Vector2 d = b.To - b.From;
                    Vector2 side = new Vector2(-d.y, d.x).normalized * b.Lift;
                    Vector2 c = mid + side;
                    b.Pos = (1 - e) * (1 - e) * b.From + 2f * (1 - e) * e * c + e * e * b.To;
                }
                else
                {
                    b.Vel.y += b.Gravity * dt;
                    if (b.Drag > 0f) b.Vel *= Mathf.Exp(-b.Drag * dt);
                    b.Pos += b.Vel * dt;
                    if (b.HasFloor && b.Pos.y < b.FloorY && b.Vel.y < 0f)
                    {
                        b.Pos.y = b.FloorY;
                        b.Vel.y = -b.Vel.y * b.Bounce;
                        b.Vel.x *= 0.72f;
                        b.Spin *= 0.6f;
                    }
                }
                if (b.Frames != null)
                {
                    int n = b.Frames.Length;
                    int f = Mathf.Min(n - 1, Mathf.FloorToInt(k * n));
                    if (b.Frames[f] != null) b.Sr.sprite = b.Frames[f];
                }
                b.Sr.enabled = true;
                b.Sr.transform.position = new Vector3(b.Pos.x, b.Pos.y, 0f);
                Vector2 s = EvalScale(b, k);
                if (b.StretchByVel > 0f && b.Vel.sqrMagnitude > 0.02f)
                {
                    float spd = b.Vel.magnitude;
                    float stretch = 1f + spd * b.StretchByVel;
                    float squash = 1f / Mathf.Sqrt(stretch);
                    s = new Vector2(s.x * squash, s.y * stretch);
                    float ang = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg - 90f;
                    b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                }
                else if (b.Spin != 0f)
                {
                    b.Sr.transform.Rotate(0f, 0f, b.Spin * dt);
                }
                b.Sr.transform.localScale = new Vector3(s.x, s.y, 1f);

                if (b.Flow)
                {
                    float cut = k < b.DissolveAt ? 0f : Mathf.SmoothStep(0f, 1f, (k - b.DissolveAt) / Mathf.Max(0.01f, 1f - b.DissolveAt));
                    float alpha = b.A0 * (k < 0.85f ? 1f : 1f - (k - 0.85f) / 0.15f);
                    InkFlowArt.Apply(b.Sr, _mpb, b.Core, b.Mid, b.Rim, b.FlowVec,
                        new Vector4(cut, b.Distort, b.Phase, b.Sharp), alpha);
                }
                else
                {
                    Color col = b.C;
                    float hold = b.HoldAlpha ? 0.6f : 0.3f;
                    col.a = b.A0 * (k < hold ? 1f : 1f - (k - hold) / (1f - hold));
                    b.Sr.color = col;
                }
            }
            StepRibbons(dt);
        }
    }
}
