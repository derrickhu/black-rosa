using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 通关 / 续命 / 失败三张结算页共用的零件。都是 Pin.Center 坐标，原点在屏幕正中。
    public static class ResultKit
    {
        // 空容器。动效要整组一起弹，但 Btn / Stroke 会在同级插一张投影，
        // 直接缩放按钮本身会把投影留在原地，所以先包一层再动这一层。
        public static RectTransform Group(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var rt = UiKit.Panel(parent, name, pos, size, Color.clear);
            rt.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        // 带深描边和下投影的大字。横幅、百分比这类要从彩色底上跳出来的字用。
        public static Text Headline(Transform parent, string name, string text, int size, Vector2 pos,
            Color face, Color edge, float edgePx = 3f)
        {
            var t = UiKit.Label(parent, name, text, size, pos, new Vector2(640f, size * 1.5f));
            UiKit.Bold(t);
            t.color = face;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = edge;
            o.effectDistance = new Vector2(edgePx, -edgePx);
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(edge.r, edge.g, edge.b, 0.55f);
            s.effectDistance = new Vector2(0f, -edgePx * 2f);
            return t;
        }

        // 丝带横幅 + 压在上面的标题。原图 900x262 左右，按宽度等比缩。
        public static RectTransform Banner(Transform parent, bool win, string text, Vector2 pos, float width)
        {
            float h = width * 262f / 900f;
            var holder = Group(parent, win ? "banner" : "banner_lose", pos, new Vector2(width, h));
            UiKit.Art(holder, "art", win ? "Ui/result_banner_win" : "Ui/result_banner_lose",
                Vector2.zero, new Vector2(width, h));
            Color edge = win ? InkTheme.Hex("7A1E14") : InkTheme.Hex("23201E");
            Color face = win ? InkTheme.Hex("FFF3C8") : InkTheme.Hex("EDE8DF");
            Headline(holder, "t", text, Mathf.RoundToInt(h * 0.34f), new Vector2(0f, h * 0.06f), face, edge, 3f);
            return holder;
        }

        public static Image Rays(Transform parent, Vector2 pos, float size, Color tint)
        {
            var img = UiKit.Icon(parent, InkSprites.Load("Ui/result_rays"), pos, size);
            img.color = tint;
            img.transform.SetAsFirstSibling();
            return img;
        }

        // 全屏透明点击层，演出没放完时点一下直接跳到结尾。要放在所有按钮之前（下面）。
        public static void SkipCatcher(RectTransform parent, Action skip)
        {
            var go = new GameObject("skip", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = Color.clear;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => skip());
        }

        // 圆头进度条。返回填充条，用 SetBar 改进度。
        public static RectTransform Bar(Transform parent, Vector2 pos, float width, float height, Color fill)
        {
            var track = UiKit.Stroke(parent, "bar", pos, new Vector2(width, height), Pin.Center, 4f,
                fill: InkTheme.Hex("4A3A30"), radius: height * 0.5f);
            track.GetComponent<Image>().raycastTarget = false;
            var go = new GameObject("fill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(track, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(5f, 5f);
            rt.offsetMax = new Vector2(5f, -5f);
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.Fill(UiSprites.Tier((height - 10f) * 0.5f));
            img.type = Image.Type.Sliced;
            img.color = fill;
            img.raycastTarget = false;
            return rt;
        }

        public static void SetBar(RectTransform fill, float k)
        {
            if (fill == null) return;
            var track = fill.parent as RectTransform;
            float inner = track != null ? track.rect.width - 10f : 0f;
            float h = track != null ? track.rect.height - 10f : 0f;
            fill.sizeDelta = new Vector2(Mathf.Max(h, inner * Mathf.Clamp01(k)), fill.sizeDelta.y);
        }

        // 按钮左侧的小视频角标，一眼看出「这个要看广告」。
        public static void AdMark(Button btn, float size = 40f)
        {
            if (btn == null) return;
            Transform face = btn.transform.Find("face");
            var rt = btn.GetComponent<RectTransform>();
            float x = -rt.sizeDelta.x * 0.5f + size * 0.5f + 18f;
            UiKit.Icon(face != null ? face : btn.transform, AdSprite(), new Vector2(x, 0f), size);
            var t = btn.GetComponentInChildren<Text>();
            if (t != null) t.rectTransform.anchoredPosition += new Vector2(size * 0.45f, 0f);
        }

        // 按钮右侧的体力消耗「⚡ 5」。label 往左让一点。
        public static Text CostTag(Button btn, int cost)
        {
            if (btn == null) return null;
            Transform face = btn.transform.Find("face");
            Transform host = face != null ? face : btn.transform;
            var rt = btn.GetComponent<RectTransform>();
            float right = rt.sizeDelta.x * 0.5f;
            UiKit.Icon(host, InkSprites.Ui("stamina"), new Vector2(right - 92f, 0f), 40f);
            var n = UiKit.Label(host, "cost", cost.ToString(), 28, new Vector2(right - 48f, 0f),
                new Vector2(50f, 40f), TextAnchor.MiddleLeft);
            UiKit.Bold(n);
            n.color = InkTheme.CardFace;
            var t = btn.transform.Find("face/t");
            if (t != null) ((RectTransform)t).anchoredPosition += new Vector2(-40f, 0f);
            return n;
        }

        public static Sprite AdBadge() => AdSprite();

        static Sprite _ad;

        static Sprite AdSprite()
        {
            if (_ad != null) return _ad;
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            Color32 edge = InkTheme.Outline;
            Color32 face = InkTheme.Hex("E84E4E");
            Color32 white = new Color32(255, 255, 255, 255);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float fx = (x + 0.5f) / S * 2f - 1f, fy = (y + 0.5f) / S * 2f - 1f;
                float box = RoundBox(fx, fy, 0.86f, 0.70f, 0.28f);
                Color32 c = new Color32(0, 0, 0, 0);
                if (box < 0f) c = box > -0.12f ? edge : face;
                // 播放三角：左边竖直，尖朝右。
                float tx = fx + 0.10f;
                bool tri = tx > -0.26f && tx < 0.34f && Mathf.Abs(fy) < (0.34f - tx) * 0.62f;
                if (box < -0.12f && tri) c = white;
                float a = Mathf.Clamp01(-box * S * 0.5f);
                c.a = (byte)(c.a * a);
                px[y * S + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _ad = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            return _ad;
        }

        static float RoundBox(float x, float y, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x) - hw + r, qy = Mathf.Abs(y) - hh + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
    }
}
