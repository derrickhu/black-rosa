using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    public enum Pin { Center, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }

    public static class UiKit
    {
        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("Fonts/Ink");
                if (_font == null && !IsMiniGameRuntime())
                    _font = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Heiti SC", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" }, 28);
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static void SetFont(Font font)
        {
            if (font == null) return;
            _font = font;
        }

        public static void ApplyTo(Transform root)
        {
            if (root == null || Font == null) return;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].font = Font;
        }

        static bool IsMiniGameRuntime()
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            return true;
#else
            string p = Application.platform.ToString();
            return p.IndexOf("MiniGame", StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Weixin", StringComparison.OrdinalIgnoreCase) >= 0;
#endif
        }

        public static Canvas CreateCanvas(string name)
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                UnityEngine.Object.DontDestroyOnLoad(es);
                WxBridge.OverrideTouch(es);
            }
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ScreenFit.DesignW, ScreenFit.DesignH);
            scaler.matchWidthOrHeight = 0f;
            return canvas;
        }

        public static RectTransform PaperSheet(Transform parent)
        {
            var go = new GameObject("paper", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = InkTheme.Paper;
            img.raycastTarget = false;
            return rt;
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size, Pin pin)
        {
            rt.sizeDelta = size;
            switch (pin)
            {
                case Pin.Top:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(pos.x, -pos.y);
                    break;
                case Pin.Bottom:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(pos.x, pos.y);
                    break;
                case Pin.TopLeft:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(pos.x, -pos.y);
                    break;
                case Pin.TopRight:
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-pos.x, -pos.y);
                    break;
                case Pin.BottomLeft:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = pos;
                    break;
                case Pin.BottomRight:
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-pos.x, pos.y);
                    break;
                default:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = pos;
                    break;
            }
        }

        public static bool TryClick(Canvas canvas)
        {
            if (EventSystem.current == null || canvas == null) return false;
            var ped = new PointerEventData(EventSystem.current) { position = InkPointer.ScreenPos };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                var btn = hits[i].gameObject.GetComponentInParent<Button>();
                if (btn == null || !btn.interactable) continue;
                btn.onClick.Invoke();
                return true;
            }
            return false;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Pin pin = Pin.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, pos, size, pin);
            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static RectTransform Stroke(Transform parent, string name, Vector2 pos, Vector2 size, Pin pin = Pin.Center, float ink = 6f, Color? line = null)
        {
            var inkRt = Panel(parent, name, pos, size, line ?? InkTheme.Ink, pin);
            var inner = new GameObject("in", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(inkRt, false);
            var ir = inner.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(ink, ink);
            ir.offsetMax = new Vector2(-ink, -ink);
            inner.GetComponent<Image>().color = InkTheme.PaperInner;
            inner.GetComponent<Image>().raycastTarget = false;
            return inkRt;
        }

        public static RectTransform Dimmer(Transform parent)
        {
            var go = new GameObject("dim", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = InkTheme.Dim;
            return rt;
        }

        public static Text Label(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box, TextAnchor anchor = TextAnchor.MiddleCenter, Pin pin = Pin.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, pos, box, pin);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = InkTheme.Ink;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string name, string text, Vector2 pos, Vector2 size, Action click, bool primary = true, Pin pin = Pin.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, pos, size, pin);
            var img = go.GetComponent<Image>();
            img.color = InkTheme.Ink;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => click());
            if (!primary)
            {
                var fill = new GameObject("fill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(go.transform, false);
                var fr = fill.GetComponent<RectTransform>();
                fr.anchorMin = Vector2.zero;
                fr.anchorMax = Vector2.one;
                fr.offsetMin = new Vector2(4f, 4f);
                fr.offsetMax = new Vector2(-4f, -4f);
                fill.GetComponent<Image>().color = InkTheme.Ghost;
                fill.GetComponent<Image>().raycastTarget = false;
            }
            var label = Label(go.transform, "t", text, 28, Vector2.zero, size);
            label.color = primary ? InkTheme.PaperInner : InkTheme.Ink;
            label.raycastTarget = false;
            return btn;
        }

        public static Image Icon(Transform parent, Sprite sprite, Vector2 pos, float size)
        {
            var go = new GameObject("icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Image[] Hearts(Transform parent, Vector2 pos, float size, Pin pin = Pin.Top)
        {
            var wrap = Panel(parent, "hp", pos, new Vector2(size * 3.4f, size + 8f), Color.clear, pin);
            wrap.GetComponent<Image>().raycastTarget = false;
            var hearts = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                hearts[i] = Icon(wrap, InkArt.Icon(InkShape.Heart, 64), new Vector2((i - 1) * (size + 8f), 0f), size);
                hearts[i].color = Color.white;
            }
            return hearts;
        }
    }
}
