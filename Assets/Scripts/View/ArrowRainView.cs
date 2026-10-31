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
        }

        const float ArrowHeight = 1.42f;

        static ArrowRainView _pool;
        readonly List<Bolt> _bolts = new List<Bolt>();
        Sprite[] _frames;

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
            int n = arrows != null ? arrows.Count : 0;
            while (_bolts.Count < n) _bolts.Add(Make());
            for (int i = 0; i < _bolts.Count; i++)
            {
                Bolt b = _bolts[i];
                bool on = i < n && _frames != null;
                b.Root.gameObject.SetActive(on);
                if (!on) continue;
                RainArrow a = arrows[i];
                int frame = _frames.Length;
                int at = (Mathf.FloorToInt(Time.unscaledTime * 8f) + a.Id) % frame;
                Sprite sprite = _frames[at] != null ? _frames[at] : _frames[0];
                b.Body.sprite = sprite;
                float h = sprite != null ? sprite.bounds.size.y : 1f;
                float tall = a.Cloud ? ArrowHeight * 1.85f : ArrowHeight;
                float s = tall / Mathf.Max(0.2f, h);
                float lean = ((a.Id % 5) - 2) * (a.Cloud ? 1.6f : 3.5f);
                b.Root.position = new Vector3(a.Pos.x, a.Pos.y, 0f);
                b.Root.localRotation = Quaternion.Euler(0f, 0f, 180f + lean);
                b.Root.localScale = new Vector3(a.Cloud ? s * 0.72f : s, s, 1f);
                b.Body.color = a.Cloud ? new Color(1f, 0.9f, 0.42f, 1f) : Color.white;
                b.Body.sortingOrder = a.Cloud ? 16 : 12;
            }
        }

        Bolt Make()
        {
            var root = new GameObject("arrow");
            root.transform.SetParent(transform, false);
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 12;
            InkFx.PaintSprite(sr, Color.white);
            return new Bolt { Root = root.transform, Body = sr };
        }
    }
}
