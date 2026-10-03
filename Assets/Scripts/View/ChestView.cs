using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 地上的宝箱。落地弹一下、挨打闪白抖一下、半血换裂开的图、
    // 快过期时闪，过期缩小淡掉。打破那一下的碎片由世界投的 FxBurst 播。
    public static class ChestView
    {
        const float Width = 0.8f;
        const float DropTime = 0.28f;
        const float FadeTime = 0.25f;

        static readonly Dictionary<int, Piece> _live = new Dictionary<int, Piece>();
        static readonly List<int> _drop = new List<int>();
        static readonly HashSet<int> _seen = new HashSet<int>();
        static readonly Dictionary<Sprite, Sprite> _white = new Dictionary<Sprite, Sprite>();
        static Transform _root;

        sealed class Piece
        {
            public Transform Wrap;
            public SpriteRenderer Body;
            public SpriteRenderer Flash;
            public SpriteRenderer Shadow;
            public InkBar Bar;
        }

        public static void BindRoot(Transform root)
        {
            _root = root;
            _live.Clear();
        }

        public static void Sync(List<ChestActor> chests)
        {
            if (_root == null || chests == null) return;
            _seen.Clear();
            for (int i = 0; i < chests.Count; i++)
            {
                ChestActor c = chests[i];
                if (c.Dead) continue;
                _seen.Add(c.Id);
                if (!_live.TryGetValue(c.Id, out Piece p)) p = _live[c.Id] = Make();
                Paint(p, c);
            }
            _drop.Clear();
            foreach (var kv in _live)
                if (!_seen.Contains(kv.Key)) _drop.Add(kv.Key);
            for (int i = 0; i < _drop.Count; i++)
            {
                Object.Destroy(_live[_drop[i]].Wrap.gameObject);
                _live.Remove(_drop[i]);
            }
        }

        static void Paint(Piece p, ChestActor c)
        {
            bool gold = c.Kind == ChestKind.Gold;
            Sprite art = InkSprites.Load(gold ? (c.Cracked ? "bag_gold_torn" : "bag_gold")
                : (c.Cracked ? "bag_ink_torn" : "bag_ink"));
            p.Body.sprite = art;
            float native = art != null ? Mathf.Max(0.01f, art.bounds.size.x) : 1f;
            float size = Width / native;

            // 从半空砸下来，落地压扁一下再弹回。
            float land = Mathf.Clamp01(c.Age / DropTime);
            float drop = (1f - land * land) * 0.9f;
            float squash = c.Age < DropTime + 0.16f && c.Age > DropTime
                ? Mathf.Sin((c.Age - DropTime) / 0.16f * Mathf.PI) * 0.18f
                : 0f;
            float hit = Mathf.Clamp01(c.HitFlash / 0.16f);
            hit *= hit;
            float shake = hit * Mathf.Sin(Time.unscaledTime * 70f + c.Seed) * 0.06f;

            // 最后两秒闪，越到最后闪得越快；到点前缩小淡掉。
            float left = c.Left;
            float alpha = 1f;
            if (left < BattleWorld.ChestBlink)
            {
                float rate = Mathf.Lerp(14f, 5f, left / BattleWorld.ChestBlink);
                alpha = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * rate));
            }
            float fade = Mathf.Clamp01(left / FadeTime);
            float shrink = Mathf.Lerp(0.4f, 1f, fade);
            alpha *= fade;

            p.Wrap.position = new Vector3(c.Pos.x + shake, c.Pos.y + drop, 0f);
            float sx = size * shrink * (1f + squash + 0.14f * hit);
            float sy = size * shrink * (1f - squash - 0.1f * hit);
            p.Body.transform.localScale = new Vector3(sx, sy, 1f);
            p.Body.color = new Color(1f, 1f, 1f, alpha);

            p.Flash.sprite = WhiteOf(art);
            p.Flash.enabled = p.Flash.sprite != null && hit > 0.01f;
            p.Flash.color = new Color(1f, 1f, 1f, Mathf.Sqrt(hit) * 0.85f * alpha);

            float half = art != null ? art.bounds.size.y * size * 0.5f : Width * 0.5f;
            p.Shadow.transform.position = new Vector3(c.Pos.x, c.Pos.y - half * 0.82f, 0f);
            float sh = Width * shrink * Mathf.Lerp(0.6f, 1f, land);
            p.Shadow.transform.localScale = new Vector3(sh, sh * 0.3f, 1f);
            InkFx.PaintSoft(p.Shadow, new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, 0.24f * alpha));

            if (fade < 0.999f || land < 1f) p.Bar.Quiet();
            else p.Bar.SyncChest(c, half + 0.14f, 16);
        }

        // 挨打那一下的白罩。宝箱图是可读的，照着 alpha 刷一张纯白版。
        static Sprite WhiteOf(Sprite src)
        {
            if (src == null || src.texture == null) return null;
            if (_white.TryGetValue(src, out Sprite hit)) return hit;
            Sprite made = null;
            Texture2D tex = src.texture;
            if (tex.isReadable)
            {
                Color[] px = tex.GetPixels();
                for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, px[i].a);
                var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                copy.SetPixels(px);
                copy.Apply();
                Rect r = src.rect;
                made = Sprite.Create(copy, r, new Vector2(src.pivot.x / r.width, src.pivot.y / r.height), src.pixelsPerUnit);
            }
            _white[src] = made;
            return made;
        }

        static Piece Make()
        {
            var wrap = new GameObject("chest");
            wrap.transform.SetParent(_root, false);

            var shadow = new GameObject("shadow");
            shadow.transform.SetParent(wrap.transform, false);
            var sh = shadow.AddComponent<SpriteRenderer>();
            sh.sprite = InkFx.SoftDisc();
            sh.sortingOrder = 2;

            // 压在墨摊和格子底纹之上、走怪之下：怪从箱子上走过去，不会被箱子挡住。
            var body = new GameObject("body");
            body.transform.SetParent(wrap.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            InkFx.PaintSprite(sr, Color.white);

            var flash = new GameObject("hit");
            flash.transform.SetParent(body.transform, false);
            flash.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            var fr = flash.AddComponent<SpriteRenderer>();
            fr.sortingOrder = 3;
            fr.enabled = false;
            InkFx.PaintSprite(fr, Color.white);

            return new Piece
            {
                Wrap = wrap.transform,
                Body = sr,
                Flash = fr,
                Shadow = sh,
                Bar = body.AddComponent<InkBar>()
            };
        }
    }
}
