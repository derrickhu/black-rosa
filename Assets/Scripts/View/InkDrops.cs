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
            if (!_live.TryGetValue(d.Id, out Piece p)) p = _live[d.Id] = Make(d.Kind);
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

        static void Puddle(Piece p, DropItem d)
        {
            p.Shadow.enabled = false;
            float wide = Mathf.Max(0.3f, d.Size);

            if (d.Fly <= 0f)
            {
                // 摊开：头 0.3 秒淌到最大，之后边缘小幅呼吸，像还在流。
                float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d.Age / 0.3f));
                float breath = 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 3.1f + d.Seed);
                float w = wide * Mathf.Lerp(0.3f, 1f, grow) * breath;
                p.Body.transform.position = new Vector3(d.Pos.x, d.Ground, 0f);
                p.Body.transform.localScale = new Vector3(w, w * 0.4f, 1f);
                p.Body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(d.Seed) * 12f);
                p.Body.color = Ink(0.72f * grow);
                return;
            }

            // 被收走：横向收拢、纵向抽起来，最后成一道顺着飞行方向的细流。
            float u = Mathf.Clamp01(d.Fly);
            float pull = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.42f));
            float w2 = wide * Mathf.Lerp(1f, 0.16f, pull);
            float h2 = wide * Mathf.Lerp(0.4f, 0.5f, pull) * Mathf.Lerp(1f, 0.42f, u);
            p.Body.transform.position = new Vector3(d.Pos.x, d.Pos.y, 0f);
            p.Body.transform.localScale = new Vector3(w2, h2, 1f);
            // 细流要顺着走向躺。抬起来那一下方向还没定，先竖着，之后跟着位移转。
            Vector2 step = p.Tracked ? d.Pos - p.Last : Vector2.up;
            float ang = step.sqrMagnitude > 0.000004f
                ? Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg - 90f
                : 0f;
            p.Body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, ang, pull));
            p.Body.color = Ink(0.72f);
        }

        static Color Ink(float a) => new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, a);

        static Piece Make(DropKind kind)
        {
            bool gold = kind == DropKind.Gold;
            var wrap = new GameObject(gold ? "coin" : "puddle");
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
            sr.sprite = gold ? InkFx.Coin() : InkFx.Splat();
            // 墨摊贴在格子底纹之上、走怪之下 —— 盖住网格会让人以为格子锁了。
            sr.sortingOrder = gold ? 9 : 1;
            InkFx.PaintSprite(sr, Color.white);
            return new Piece { Body = sr, Shadow = sh };
        }
    }
}
