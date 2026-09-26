using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 弹窗共用的外壳：宣纸卡面 + 朱红飘带标题 + 右上角朱印关闭键，和排行榜一个样子。
    public static class PanelKit
    {
        public static readonly Color BoardFill = InkTheme.Hex("FBF4E6");

        public static RectTransform Board(RectTransform dim, string title, Vector2 pos, Vector2 size, Pin pin, Action close)
        {
            var board = UiKit.Stroke(dim, "board", pos, size, pin, 6f, null, BoardFill, 28f);

            Sprite rib = InkSprites.Load("Ui/ribbon_chapter");
            float ribW = 380f;
            float ribH = rib != null && rib.rect.width > 1f ? ribW * rib.rect.height / rib.rect.width : 100f;
            var ribbon = UiKit.Art(board, "ribbon", "Ui/ribbon_chapter", new Vector2(0f, -ribH * 0.46f),
                new Vector2(ribW, ribH), Pin.Top);
            ribbon.GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Label(ribbon, "t", title, 38, new Vector2(0f, ribH * 0.10f), new Vector2(260f, 56f));
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);

            CloseButton(board, close);
            return board;
        }

        public static void CloseButton(RectTransform board, Action close)
        {
            const float s = 68f;
            var go = new GameObject("close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(board, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.sizeDelta = new Vector2(s, s);
            var ring = go.GetComponent<Image>();
            ring.sprite = UiSprites.Disc();
            ring.color = InkTheme.Outline;
            var face = UiKit.Icon(go.transform, UiSprites.Disc(), Vector2.zero, s - 10f);
            face.color = InkTheme.Seal;
            var x = UiKit.Label(go.transform, "x", "×", 46, new Vector2(0f, 2f), new Vector2(s, s));
            x.color = InkTheme.CardFace;
            UiKit.Bold(x);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = face;
            btn.onClick.AddListener(() => close());
        }

        // 奖励小卡：图标 + 名字 + 数量。pos.y 是卡片上沿离 parent 上沿多远。
        public static RectTransform Reward(Transform parent, string icon, string name, string amount, Vector2 pos, Vector2 size)
        {
            var card = UiKit.Stroke(parent, "reward", pos, size, Pin.Top, 4f, null, InkTheme.CardFace, 20f);
            card.GetComponent<Image>().raycastTarget = false;
            float iconSize = Mathf.Min(size.x - 40f, size.y * 0.5f);
            UiKit.Icon(card, InkSprites.Load(icon), new Vector2(0f, size.y * 0.16f), iconSize);
            var n = UiKit.Label(card, "name", name, 22, new Vector2(0f, -size.y * 0.20f), new Vector2(size.x, 30f));
            n.color = InkTheme.TextMid;
            var a = UiKit.Label(card, "amount", amount, 28, new Vector2(0f, -size.y * 0.36f), new Vector2(size.x, 36f));
            a.color = InkTheme.Seal;
            UiKit.Bold(a);
            return card;
        }

        // 按钮换成灰的「不能按」样子，但仍可点 —— 点了给提示，比按不动更好懂。
        public static void Dim(Button btn, bool dim, bool primary = true)
        {
            if (dim) UiKit.PaintBtn(btn, InkTheme.CtaOff, InkTheme.GraphiteHi, InkTheme.CardFace);
            else if (primary) UiKit.PaintBtn(btn, InkTheme.Cta, InkTheme.CtaDeep, InkTheme.CardFace);
            else UiKit.PaintBtn(btn, InkTheme.Plain, InkTheme.PlainDeep, InkTheme.TextDark);
        }

        public static void SetText(Button btn, string text)
        {
            var t = btn != null ? btn.GetComponentInChildren<Text>() : null;
            if (t != null) t.text = text;
        }

        // Unity 屏幕像素矩形（左下原点），给微信原生按钮对位用。
        public static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }
    }
}
