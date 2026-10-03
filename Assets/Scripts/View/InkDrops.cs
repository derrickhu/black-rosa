using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 地上的收入。两种东西画法完全不同，因为它们是两件不同的事：
    //   金币 —— 一枚一枚弹出来、滚一下、躺平，然后成串飞进金币栏。数量就是收获感，
    //           所以画单枚硬币，不能拿顶栏那张「一沓钱」的图标当一份收入。
    //   墨   —— 怪化掉留在地上的那一摊。贴地摊开、慢慢淌，然后被抽成一道细流收走。
    public static class InkDrops
    {
        static readonly Dictionary<int, Piece> _live = new Dictionary<int, Piece>();
        static readonly List<int> _drop = new List<int>();
        static readonly HashSet<int> _seen = new HashSet<int>();
        static Transform _root;

        sealed class Piece
        {
            public SpriteRenderer Body;
            public SpriteRenderer Bead;     // 只有墨用：收拢后的那颗珠子
            public SpriteRenderer Shadow;
            public Vector2 Last;
            public bool Tracked;
        }

        public static void BindRoot(Transform root)
        {
            _root = root;
            _live.Clear();
        }

        public static void Sync(List<DropItem> drops)
        {
            if (_root == null || drops == null) return;
            for (int i = 0; i < drops.Count; i++) Paint(drops[i]);

            _seen.Clear();
            for (int i = 0; i < drops.Count; i++) _seen.Add(drops[i].Id);
            _drop.Clear();
            foreach (var kv in _live)
                if (!_seen.Contains(kv.Key)) _drop.Add(kv.Key);
            for (int i = 0; i < _drop.Count; i++)
            {
                Object.Destroy(_live[_drop[i]].Body.transform.parent.gameObject);
                _live.Remove(_drop[i]);
            }
        }

        static void Paint(DropItem d)
        {
            if (!_live.TryGetValue(d.Id, out Piece p)) p = _live[d.Id] = Make(d);
            if (d.Kind == DropKind.Ink) Puddle(p, d);
            else Coin(p, d);
            p.Last = d.Pos;
            p.Tracked = true;
        }

        static void Coin(Piece p, DropItem d)
        {
            bool flying = d.Fly > 0f;
            bool grounded = !flying && d.Pos.y <= d.Ground + 0.01f;

            // 躺着的时候原地轻轻浮一下，告诉玩家这是可以被收走的东西，不是背景。
            float bob = grounded ? Mathf.Sin(Time.unscaledTime * 5.4f + d.Seed) * 0.045f : 0f;
            p.Body.transform.position = new Vector3(d.Pos.x, d.Pos.y + bob, 0f);
            // 飞起来收小一点，一串收束进药丸里才好看
            // 0.27 太小：描边和戳印都糊没了，一枚金币看着只是个橙点。
            float size = 0.34f * Mathf.Lerp(1f, 0.66f, d.Fly);
            p.Body.transform.localScale = Vector3.one * size;
            // 在空中翻，躺下就停。一直转会像悬浮的道具，不像掉在地上的钱。
            p.Body.transform.localRotation = Quaternion.Euler(0f, 0f,
                grounded ? Mathf.Sin(d.Seed) * 10f : Mathf.Sin(Time.unscaledTime * 11f + d.Seed) * 24f);
            p.Body.color = Color.white;

            p.Shadow.enabled = grounded;
            if (grounded)
            {
                p.Shadow.transform.position = new Vector3(d.Pos.x, d.Ground - size * 0.44f, 0f);
                p.Shadow.transform.localScale = new Vector3(size * 0.9f, size * 0.3f, 1f);
                InkFx.PaintSoft(p.Shadow, new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, 0.22f));
            }
        }

        // 墨走三段：摊在地上 → 原地团成一颗珠 → 珠子飞进顶栏。
        // 中间那段是两层交叉淡：摊子缩着淡出，珠子涨着淡入。不换层直接把
        // Splat() 缩小的话，它外圈那五滴会跟着缩成一圈脏点。
        static void Puddle(Piece p, DropItem d)
        {
            p.Shadow.enabled = false;
            float wide = Mathf.Max(0.3f, d.Size);
            float gather = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d.Gather));
            float fly = Mathf.Clamp01(d.Fly);

            // 摊子：淌开 → 收拢时缩回去并淡掉
            float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d.Age / 0.3f));
            float breath = 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 3.1f + d.Seed);
            float w = wide * Mathf.Lerp(0.3f, 1f, grow) * breath * Mathf.Lerp(1f, 0.22f, gather);
            p.Body.enabled = gather < 1f;
            if (p.Body.enabled)
            {
                p.Body.transform.position = new Vector3(d.Pos.x, d.Ground, 0f);
                p.Body.transform.localScale = new Vector3(w, w * 0.4f, 1f);
                p.Body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(d.Seed) * 12f);
                p.Body.color = Ink(0.72f * grow * (1f - gather));
            }

            // 珠子：团起来时涨出来，飞的时候收小一点，顺着走向拖着尾巴
            p.Bead.enabled = gather > 0f;
            if (!p.Bead.enabled) return;
            float bead = wide * 0.34f * Mathf.Lerp(0.2f, 1f, gather) * Mathf.Lerp(1f, 0.66f, fly);
            p.Bead.transform.position = new Vector3(d.Pos.x, d.Pos.y, 0f);
            p.Bead.transform.localScale = new Vector3(bead, bead, 1f);
            // 刚团起来时还没位移，方向定不了，先让尖朝上；飞起来再跟着走向转。
            Vector2 step = p.Tracked ? d.Pos - p.Last : Vector2.zero;
            float ang = step.sqrMagnitude > 0.000004f
                ? Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg - 90f
                : 0f;
            p.Bead.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            p.Bead.color = Ink(0.86f * gather);
        }

        static Color Ink(float a) => new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, a);

        static Piece Make(DropItem d)
        {
            bool ink = d.Kind == DropKind.Ink;
            var wrap = new GameObject(ink ? "puddle" : "coin");
            wrap.transform.SetParent(_root, false);

            var shadow = new GameObject("shadow");
            shadow.transform.SetParent(wrap.transform, false);
            var sh = shadow.AddComponent<SpriteRenderer>();
            sh.sprite = InkFx.SoftDisc();
            sh.sortingOrder = 6;
            sh.enabled = false;

            var body = new GameObject("body");
            body.transform.SetParent(wrap.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = ink ? InkFx.Splat() : InkFx.Coin();
            // 墨摊贴在格子底纹之上、走怪之下 —— 盖住网格会让人以为格子锁了。
            sr.sortingOrder = ink ? 1 : 9;
            InkFx.PaintSprite(sr, Color.white);
            var piece = new Piece { Body = sr, Shadow = sh };
            if (!ink) return piece;

            // 珠子离地飞，得压在走怪之上，不然半路会钻到敌人后面去。
            var bead = new GameObject("bead");
            bead.transform.SetParent(wrap.transform, false);
            var br = bead.AddComponent<SpriteRenderer>();
            br.sprite = InkFx.Bead();
            br.sortingOrder = 9;
            br.enabled = false;
            InkFx.PaintSprite(br, Color.white);
            piece.Bead = br;
            return piece;
        }
    }
}
