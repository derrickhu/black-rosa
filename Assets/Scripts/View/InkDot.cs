using UnityEngine;

namespace InkLine
{
    // 挂在敌人身上的持续状态层。
    // 计划里写的是一张 512² 图集，实际这几个记号都是纯形状，
    // 直接烘出来比出图更省：没有新文件，每个记号 64²，全部走 tint 上色。
    public static class DotMarks
    {
        static Sprite _flame;
        static Sprite _swirl;

        public static Sprite Flame()
        {
            if (_flame != null) return _flame;
            _flame = Bake(64, (u, v) =>
            {
                // 上尖下圆的火舌
                float w = 0.52f * Mathf.Pow(Mathf.Clamp01(1f - v), 0.55f) + 0.06f;
                float a = 1f - Mathf.Abs(u) / Mathf.Max(0.02f, w);
                a = Mathf.Pow(Mathf.Clamp01(a), 1.2f);
                return a * Mathf.SmoothStep(0f, 0.16f, v) * Mathf.SmoothStep(1f, 0.82f, v);
            });
            return _flame;
        }

        public static Sprite Swirl()
        {
            if (_swirl != null) return _swirl;
            _swirl = Bake(64, (u, v) =>
            {
                // 一段转起来会像漩涡的开口环
                float y = v - 0.5f;
                float r = Mathf.Sqrt(u * u + y * y);
                float band = (r - 0.34f) / 0.09f;
                float ring = Mathf.Exp(-band * band);
                float ang = Mathf.Atan2(y, u);
                return ring * Mathf.SmoothStep(-2.6f, -1.4f, ang > -2.9f ? ang : -2.9f);
            });
            return _swirl;
        }

        static Sprite Bake(int n, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n;
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(u, v)));
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }
    }

    public sealed class InkDot : MonoBehaviour
    {
        const int MaxStacks = 4;

        // 状态层的空间分区。灼烧 / 中毒 / 缓 / 一个硬控是四条独立的计时轨，
        // 随时可能同时挂在一只怪身上，所以每层必须占住一块不重叠的区域 ——
        // 都堆在中间就会互相盖住，玩家数不清身上到底有几个状态。
        // 坐标是怪的局部单位：脚底约 -0.45，头顶约 +0.45。
        //
        //   ┌── Halo  +0.56   晕环 / 惑漩涡（头顶之上）
        //   │   Crown  +0.48  冰锥从这里往下长 ┐
        //   │                 毒泡沿两侧上浮  │ 三者互斥或不同轴，互不遮挡
        //   │   火头到胸口 +0.15              ┘
        //   └── Foot  -0.45   火根 / 水纹（地面）
        const float Foot = -0.45f;    // 脚底：火焰的根、水纹所在
        const float Crown = 0.48f;    // 头顶：冰锥悬挂的上沿
        const float Halo = 0.56f;     // 头顶之上：晕 / 惑
        const float SideX = 0.30f;    // 身体两侧：毒泡的横向偏移
        const float BurnH = 0.62f;    // 火焰高度：从脚底到胸口，不盖过头
        const float CrustH = 0.58f;   // 冰锥高度：从头顶往下到腰，不盖住脚

        Transform _rig;
        SpriteRenderer _burn;
        SpriteRenderer _slow;
        SpriteRenderer _hard;
        SpriteRenderer[] _poison;

        public void Sync(EnemyActor e, int order, bool halted = false)
        {
            Ensure(order);
            // 敌人本体每帧在做呼吸缩放，记号要反着缩回去才不跟着变形
            Vector3 p = transform.localScale;
            _rig.localScale = new Vector3(
                p.x > 0.001f ? 1f / p.x : 1f,
                p.y > 0.001f ? 1f / p.y : 1f, 1f);

            float t = Time.unscaledTime;

            // 灼烧：一簇平涂火焰从脚下窜上来，火头到胸口。
            // 火头不再盖过头顶 —— 上半区要留给冰壳，霜火那对才能同时读出来。
            bool burn = e.BurnTime > 0f;
            Sprite blaze = Loop("burn_body", t * 12f + e.Id * 1.7f);
            if (blaze != null)
                Mark(_burn, burn, blaze, new Vector3(0f, Foot + 0.5f * BurnH * blaze.bounds.size.y, 0f),
                    BurnH, Fade(Color.white, 0.9f), 0f, true);
            else
                Mark(_burn, burn, DotMarks.Flame(),
                    new Vector3(0f, 0.42f, 0f),
                    0.24f * (1f + 0.12f * Mathf.Sin(t * 9f + e.Id)),
                    Fade(InkTheme.FireHi, 0.85f), 0f);

            // 毒：两侧外缘各飘一串气泡，左右交替。
            // 走身体两侧是为了让开中轴 —— 中轴下半归火、上半归冰。
            int stacks = e.PoisonTime > 0f ? Mathf.Clamp(e.PoisonStacks, 0, MaxStacks) : 0;
            Sprite bubble = InkSprites.Load("Vfx/dot_poison");
            for (int i = 0; i < MaxStacks; i++)
            {
                float rise = Mathf.Repeat(t * 0.7f + i * 0.31f, 1f);
                float side = (i % 2 == 0 ? -1f : 1f) * (SideX + 0.04f * (i / 2));
                Mark(_poison[i], i < stacks, bubble ?? InkFx.SoftDisc(),
                    new Vector3(side, -0.05f + rise * 0.46f, 0f),
                    (bubble != null ? 0.17f : 0.10f) * (1f - rise * 0.3f),
                    bubble != null
                        ? Fade(Color.white, 0.92f * (1f - rise * 0.7f))
                        : Fade(InkTheme.PoisonHi, 0.75f * (1f - rise)),
                    0f, bubble != null);
            }

            // 缓：脚下地面一圈水纹。冰的冻住走头顶冰壳，不要再用冰面表示减速。
            bool slow = e.SlowTime > 0f && e.Slow < 0.999f;
            Sprite frost = Loop("hitv_water", t * 8f + e.Id * 1.3f);
            float fs = frost != null ? 0.7f / Mathf.Max(0.01f, frost.bounds.size.x) : 1f;
            Mark(_slow, slow, frost, new Vector3(0f, Foot, 0f), fs,
                Fade(Color.white, 0.95f), 0f, true);

            // 硬控三个互斥，所以可以共用上半区：
            // 冻是从头肩往下长的冰锥（和火正好反方向），晕和惑在头顶之上转。
            // 闹钟是全场停住，优先画头顶的圈，免得被冰壳盖掉。
            bool hard = e.HardTime > 0f;
            if (halted || (hard && e.Hard == StatusKind.Stun))
            {
                Sprite ring = InkSprites.Load("Vfx/dot_stun");
                float bob = Mathf.Sin(t * 6f) * 0.04f;
                float pulse = 1f + 0.08f * Mathf.Sin(t * 8f);
                Mark(_hard, true, ring ?? InkFx.SoftRing(), new Vector3(0f, Halo + bob, 0f),
                    (ring != null ? 0.64f : 0.42f) * pulse,
                    ring != null ? Fade(Color.white, 0.95f) : Fade(InkTheme.Word, 0.80f),
                    t * 160f, ring != null);
            }
            else if (hard && e.Hard == StatusKind.Freeze)
            {
                Sprite crust = Loop("ice_crust", t * 6f + e.Id);
                if (crust != null)
                    Mark(_hard, true, crust,
                        new Vector3(0f, Crown - 0.5f * CrustH * crust.bounds.size.y, 0f),
                        CrustH, Fade(Color.white, 0.92f), 0f, true);
                else
                    Mark(_hard, true, InkFx.SoftRing(), Vector3.zero, 0.92f,
                        Fade(InkTheme.IceHi, 0.62f), 0f);
            }
            else if (hard && e.Hard == StatusKind.Confuse)
            {
                Sprite swirl = InkSprites.Load("Vfx/dot_confuse");
                Mark(_hard, true, swirl ?? DotMarks.Swirl(), new Vector3(0f, Halo, 0f),
                    swirl != null ? 0.34f : 0.34f,
                    swirl != null ? Fade(Color.white, 0.92f) : Fade(InkTheme.ConfuseHi, 0.85f),
                    t * -200f, swirl != null);
            }
            else
                Mark(_hard, false, null, Vector3.zero, 1f, Color.clear, 0f);
        }

        // 帧循环。负数取模在 C# 里是负的，所以要再拉回正区间。
        static Sprite Loop(string set, float phase)
        {
            Sprite[] frames = InkVfx.Frames(set);
            if (frames == null) return null;
            int at = ((Mathf.FloorToInt(phase) % frames.Length) + frames.Length) % frames.Length;
            return frames[at] ?? frames[0];
        }

        public void Quiet()
        {
            if (_rig == null) return;
            _rig.gameObject.SetActive(false);
        }

        static Color Fade(Color c, float a)
        {
            c.a = a;
            return c;
        }

        void Ensure(int order)
        {
            if (_rig == null)
            {
                var go = new GameObject("dots");
                go.transform.SetParent(transform, false);
                _rig = go.transform;
                _burn = Spawn("burn", order);
                // 水纹在地面，必须明确压到怪身体（order-1）后面去。
                // 原来给的是 order-1，正好和身体同层，谁盖谁由引擎决定。
                _slow = Spawn("slow", order - 2);
                _hard = Spawn("hard", order + 1);
                _poison = new SpriteRenderer[MaxStacks];
                for (int i = 0; i < MaxStacks; i++) _poison[i] = Spawn("poison" + i, order);
            }
            if (!_rig.gameObject.activeSelf) _rig.gameObject.SetActive(true);
        }

        SpriteRenderer Spawn(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rig, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        // flat：图自带配色，走普通混合按原色画；其余记号是白模，交给柔光染色。
        static void Mark(SpriteRenderer sr, bool on, Sprite sprite, Vector3 pos, float scale,
            Color color, float spin, bool flat = false)
        {
            if (sr == null) return;
            if (!on || sprite == null || color.a < 0.02f)
            {
                sr.enabled = false;
                return;
            }
            sr.enabled = true;
            sr.sprite = sprite;
            sr.transform.localPosition = pos;
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, spin);
            sr.transform.localScale = Vector3.one * scale;
            if (flat) InkFx.PaintSprite(sr, color);
            else InkFx.PaintSoft(sr, color);
        }
    }
}
