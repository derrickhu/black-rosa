using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 万箭蓄满后从炮口往上射的满屏箭。帧动画的箭尖画在精灵底部，轴心也在箭尖，
    // 转 180 度后尖朝上，根节点仍是击中点，箭杆拖在下面。
    public sealed class ArrowRainView : MonoBehaviour
    {
        sealed class Bolt
        {
            public Transform Root;
            public SpriteRenderer Body;
            public SpriteRenderer[] Glow;
        }

        const float ArrowHeight = 1.42f;
        const float CloudHeight = 1.72f;
        const int GlowN = 3;

        static ArrowRainView _pool;
        readonly List<Bolt> _bolts = new List<Bolt>();
        Sprite[] _frames;
        Sprite[] _cloud;

        public static void Bind(Transform root)
        {
            if (_pool != null && _pool.transform.parent == root) return;
            var go = new GameObject("arrow-rain");
            go.transform.SetParent(root, false);
            _pool = go.AddComponent<ArrowRainView>();
        }

        public static void Sync(List<RainArrow> arrows)
        {
            if (_pool == null) return;
            _pool.Present(arrows);
        }

        void Present(List<RainArrow> arrows)
        {
            if (_frames == null) _frames = InkVfx.Frames("rain_arrow", 4);
            if (_cloud == null) _cloud = InkVfx.Frames("cloud_arrow", 4);
            int n = arrows != null ? arrows.Count : 0;
            while (_bolts.Count < n) _bolts.Add(Make());
            for (int i = 0; i < _bolts.Count; i++)
            {
                Bolt b = _bolts[i];
                bool on = i < n && _frames != null;
                b.Root.gameObject.SetActive(on);
                if (!on) continue;
                RainArrow a = arrows[i];
                Sprite[] set = a.Cloud && _cloud != null && _cloud[0] != null ? _cloud : _frames;
                int frame = set.Length;
                int at = (Mathf.FloorToInt(Time.unscaledTime * 8f) + a.Id) % frame;
                Sprite sprite = set[at] != null ? set[at] : set[0];
                b.Body.sprite = sprite;
                float h = sprite != null ? sprite.bounds.size.y : 1f;
                float s = (a.Cloud ? CloudHeight : ArrowHeight) / Mathf.Max(0.2f, h);
                float lean = ((a.Id % 5) - 2) * 3.5f;
                b.Root.position = new Vector3(a.Pos.x, a.Pos.y, 0f);
                b.Root.localRotation = Quaternion.Euler(0f, 0f, 180f + lean);
                b.Root.localScale = new Vector3(s, s, 1f);
                b.Body.color = Color.white;
                b.Body.sortingOrder = a.Cloud ? 16 : 12;
                // 穿云箭用更粗的那套箭，箭杆上再挂几颗光点。
                // 轴心在箭尖，转 180 后本地 +Y 就是拖在下面的箭杆。
                for (int g = 0; g < GlowN; g++)
                {
                    SpriteRenderer spark = b.Glow[g];
                    spark.enabled = a.Cloud;
                    if (!a.Cloud) continue;
                    float along = 0.28f + g * 0.22f;
                    float sway = Mathf.Sin(Time.unscaledTime * 16f + a.Id * 1.7f + g * 2.2f);
                    float localY = h * along;
                    spark.transform.localPosition = new Vector3(sway * 0.12f / s, localY, 0f);
                    float tw = 0.45f + 0.55f * (0.5f + 0.5f * sway);
                    float sz = (g == 1 ? 0.2f : 0.14f) * tw;
                    spark.transform.localScale = Vector3.one * (sz / s);
                    spark.color = g == 1
                        ? new Color(1f, 0.9f, 0.45f, 0.9f * tw)
                        : new Color(1f, 0.98f, 0.82f, 0.75f * tw);
                }
            }
        }

        Bolt Make()
        {
            var root = new GameObject("arrow");
            root.transform.SetParent(transform, false);
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 12;
            InkFx.PaintSprite(sr, Color.white);
            var glow = new SpriteRenderer[GlowN];
            for (int i = 0; i < GlowN; i++)
            {
                var go = new GameObject("glow");
                go.transform.SetParent(root.transform, false);
                var bit = go.AddComponent<SpriteRenderer>();
                bit.sprite = i == 1 ? InkFx.Star() : InkFx.Mote();
                bit.sortingOrder = 17;
                InkFx.PaintAdd(bit, Color.white);
                bit.enabled = false;
                glow[i] = bit;
            }
            return new Bolt { Root = root.transform, Body = sr, Glow = glow };
        }
    }
}
