using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 掉在地上的金币和墨滴。图标直接用顶栏那两张手绘图 ——
    // 飞上去之后和药丸里的图标是同一个东西，收入这条线才连得起来。
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
            bool flying = d.Fly > 0f;
            bool grounded = !flying && d.Pos.y <= d.Ground + 0.01f;

            // 躺着的时候原地轻轻浮一下，告诉玩家这是可以被收走的东西，不是背景。
            float bob = grounded ? Mathf.Sin(Time.unscaledTime * 5.4f + d.Seed) * 0.045f : 0f;
            var at = new Vector3(d.Pos.x, d.Pos.y + bob, 0f);
            p.Body.transform.position = at;
            // 飞起来收小一点，一串收束进药丸里才好看
            float size = (d.Kind == DropKind.Gold ? 0.40f : 0.34f) * Mathf.Lerp(1f, 0.62f, d.Fly);
            p.Body.transform.localScale = Vector3.one * size;
            p.Body.transform.localRotation = Quaternion.Euler(0f, 0f,
                flying ? Mathf.Sin(Time.unscaledTime * 12f + d.Seed) * 16f : Mathf.Sin(d.Seed) * 8f);

            p.Shadow.enabled = grounded;
            if (grounded)
            {
                p.Shadow.transform.position = new Vector3(d.Pos.x, d.Ground - size * 0.42f, 0f);
                p.Shadow.transform.localScale = new Vector3(size * 0.8f, size * 0.26f, 1f);
                InkFx.PaintSoft(p.Shadow, new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, 0.22f));
            }
        }

        static Piece Make(DropKind kind)
        {
            var wrap = new GameObject(kind == DropKind.Gold ? "coin" : "drip");
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
            sr.sprite = InkSprites.Ui(kind == DropKind.Gold ? "gold" : "ink");
            // 手绘图标没载到时退回一枚程序化的形，别让掉落变成隐形的。
            if (sr.sprite == null)
                sr.sprite = InkArt.Heap(kind == DropKind.Gold ? InkShape.Coin : InkShape.Diamond,
                    kind == DropKind.Gold ? InkTheme.CoinFace : InkTheme.Track, 64);
            sr.sortingOrder = 9;
            InkFx.PaintSprite(sr, Color.white);
            return new Piece { Body = sr, Shadow = sh };
        }
    }
}
