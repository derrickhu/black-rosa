using UnityEngine;

namespace InkLine
{
    // 挂在敌人身上的持续状态层。
    // 计划里写的是一张 512² 图集，实际这几个记号都是纯形状，
    // 直接烘出来比出图更省：没有新文件，每个记号 64²，全部走 tint 上色。
    public static class DotMarks
    {
        static Sprite _flame;
        static Sprite _ripple;
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

        public static Sprite Ripple()
        {
            if (_ripple != null) return _ripple;
            _ripple = Bake(64, (u, v) =>
            {
                // 脚下一道压扁的水纹
                float d = Mathf.Sqrt(u * u + (v - 0.5f) * (v - 0.5f) * 9f);
                float band = (d - 0.62f) / 0.14f;
                return Mathf.Exp(-band * band);
            });
            return _ripple;
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
        Transform _rig;
        SpriteRenderer _burn;
        SpriteRenderer _slow;
        SpriteRenderer _hard;
        SpriteRenderer[] _poison;

        public void Sync(EnemyActor e, int order)
        {
            Ensure(order);
            // 敌人本体每帧在做呼吸缩放，记号要反着缩回去才不跟着变形
            Vector3 p = transform.localScale;
            _rig.localScale = new Vector3(
                p.x > 0.001f ? 1f / p.x : 1f,
                p.y > 0.001f ? 1f / p.y : 1f, 1f);

            float t = Time.unscaledTime;

            // 灼烧：头顶一簇橙火苗，呼吸
            bool burn = e.BurnTime > 0f;
            Mark(_burn, burn, DotMarks.Flame(),
                new Vector3(0f, 0.42f, 0f),
                0.24f * (1f + 0.12f * Mathf.Sin(t * 9f + e.Id)),
                Fade(InkTheme.FireHi, 0.85f), 0f);

            // 毒：紫绿气泡，几层就几个
            int stacks = e.PoisonTime > 0f ? Mathf.Clamp(e.PoisonStacks, 0, MaxStacks) : 0;
            for (int i = 0; i < MaxStacks; i++)
            {
                float rise = Mathf.Repeat(t * 0.8f + i * 0.27f, 1f);
                Mark(_poison[i], i < stacks, InkFx.SoftDisc(),
                    new Vector3(-0.16f + i * 0.11f, 0.18f + rise * 0.34f, 0f),
                    0.10f * (1f - rise * 0.35f),
                    Fade(InkTheme.PoisonHi, 0.75f * (1f - rise)), 0f);
            }

            // 缓：脚下一道青色水纹；冻的时候让位给冰壳
            bool slow = e.SlowTime > 0f && e.Slow < 0.999f && !e.Frozen;
            Mark(_slow, slow, DotMarks.Ripple(),
                new Vector3(0f, -0.34f, 0f), 0.52f,
                Fade(InkTheme.WaterHi, 0.55f), 0f);

            // 硬控：冻是一层冰壳罩住整体，晕和惑是头顶转圈
            bool hard = e.HardTime > 0f;
            if (hard && e.Hard == StatusKind.Freeze)
                Mark(_hard, true, InkFx.SoftRing(), Vector3.zero, 0.92f,
                    Fade(InkTheme.IceHi, 0.62f), 0f);
            else if (hard && e.Hard == StatusKind.Stun)
                Mark(_hard, true, InkFx.SoftRing(), new Vector3(0f, 0.44f, 0f), 0.34f,
                    Fade(InkTheme.Word, 0.80f), t * 260f);
            else if (hard && e.Hard == StatusKind.Confuse)
                Mark(_hard, true, DotMarks.Swirl(), new Vector3(0f, 0.44f, 0f), 0.34f,
                    Fade(InkTheme.ConfuseHi, 0.85f), t * -200f);
            else
                Mark(_hard, false, null, Vector3.zero, 1f, Color.clear, 0f);
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
                _slow = Spawn("slow", order - 1);
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

        static void Mark(SpriteRenderer sr, bool on, Sprite sprite, Vector3 pos, float scale, Color color, float spin)
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
            InkFx.PaintSoft(sr, color);
        }
    }
}
