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
        static Font _fontBold;

        public static Font Font => _font != null ? _font : (_font = LoadFont("Fonts/Ink"));

        // 真的 Bold 字重，不是 FontStyle.Bold。Unity 的合成粗体是把字形往四周抹一圈，
        // 汉字笔画本来就密，20px 上抹完就是一团黑。字体子集两个字重一共 198KB，
        // 值这个钱。重建见 docs/prompt/runtime/build_font.py。
        public static Font FontBold => _fontBold != null ? _fontBold : (_fontBold = LoadFont("Fonts/InkBold"));

        static Font LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f == null && !IsMiniGameRuntime())
                f = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Heiti SC", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" }, 28);
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        public static void SetFont(Font font)
        {
            if (font == null) return;
            _font = font;
        }

        // 把一段文字换成粗体字重。别写 t.fontStyle = FontStyle.Bold ——
        // 字体是按字表子集化的，合成粗体不在子集里，而且汉字会糊。
        public static Text Bold(Text t)
        {
            if (t != null) t.font = FontBold;
            return t;
        }

        public static void ApplyTo(Transform root)
        {
            if (root == null || Font == null) return;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
                if (texts[i].font == null) texts[i].font = Font;
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
            var go = new GameObject("bg", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.Gradient(InkTheme.BgTop, InkTheme.BgBot);
            img.color = Color.white;
            img.raycastTarget = false;
            // 纯渐变还是太平，压两团暖色晕染出层次
            Wash(rt, new Vector2(0.12f, 0.90f), 620f, InkTheme.Hex("FFBE78"), 0.40f);
            Wash(rt, new Vector2(0.86f, 0.18f), 700f, InkTheme.Hex("FFA894"), 0.32f);
            return rt;
        }

        static void Wash(RectTransform parent, Vector2 anchor, float size, Color color, float alpha)
        {
            var go = new GameObject("wash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = InkFx.SoftDisc();
            img.color = new Color(color.r, color.g, color.b, alpha);
            img.raycastTarget = false;
        }

        static Image DropShadow(Transform parent, string name, Vector2 pos, Vector2 size, Pin pin, int rad, float dy)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, pos, size, pin);
            // anchoredPosition 永远是 +y 朝上，所以减 dy 一定往下，不用管 pin 是哪个
            rt.anchoredPosition += new Vector2(0f, -dy);
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.Shadow(rad, 12);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.23f, 0.16f, 0.12f, 0.34f);
            img.raycastTarget = false;
            return img;
        }

        static Image AddLine(Transform root, int rad, int width, Color color)
        {
            var go = new GameObject("ln", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.Line(rad, width);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
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

        // 卡面 = 投影 + 圆角实底 + 等宽描边，三层九宫格叠出来。
        // 投影得是「同级、且先于卡面插入」的节点：Unity UI 里子节点一定画在父节点
        // 自己的图形之上，把投影挂成子节点会直接糊住卡面。
        // ink 沿用原来的语义位置，但现在表示描边宽度而不是内缩量。
        public static RectTransform Stroke(Transform parent, string name, Vector2 pos, Vector2 size,
            Pin pin = Pin.Center, float ink = 6f, Color? line = null, Color? fill = null, float radius = 0f)
        {
            int rad = radius > 0f ? UiSprites.Tier(radius) : UiSprites.TierFor(size);
            int width = Mathf.Clamp(Mathf.RoundToInt(ink), 3, 6);
            DropShadow(parent, name + "_sh", pos, size, pin, rad, 7f);
            var root = Panel(parent, name, pos, size, fill ?? InkTheme.CardFace, pin);
            var bg = root.GetComponent<Image>();
            bg.sprite = UiSprites.Fill(rad);
            bg.type = Image.Type.Sliced;
            AddLine(root, rad, width, line ?? InkTheme.Outline);
            return root;
        }

        // 顶栏药丸：图标压在左端略微出框，右边数值。返回数值 Text 供刷新。
        // 图标底下不再垫饱和色圆盘 —— 现在用的是本身带颜色和描边的手绘图标，
        // 再垫一层就是撞色，图标反而看不清。略微出框比缩在框里精神。
        public static Text Chip(Transform parent, string name, Sprite icon, string value,
            Vector2 pos, Vector2 size, Pin pin = Pin.Top)
        {
            var root = Stroke(parent, name, pos, size, pin, 5f, radius: size.y * 0.5f);
            root.GetComponent<Image>().raycastTarget = false;
            float r = size.y * 1.06f;
            if (icon != null) Icon(root, icon, new Vector2(-size.x * 0.5f + r * 0.42f, 0f), r);
            var t = Label(root, "v", value, 30, new Vector2(r * 0.40f, 0f),
                new Vector2(size.x - r - 16f, size.y), TextAnchor.MiddleLeft);
            t.color = InkTheme.TextDark;
            Bold(t);
            return t;
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
            go.GetComponent<Image>().color = InkTheme.Scrim;
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
            t.color = InkTheme.TextDark;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // 立体按钮：根节点是压深一档的「底唇」，子节点 face 抬起来盖在上面，
        // 露出的那道底边就是厚度。休闲小游戏的标准做法，比纯平的更想让人按。
        public static Button Btn(Transform parent, string name, string text, Vector2 pos, Vector2 size,
            Action click, bool primary = true, Pin pin = Pin.Center)
        {
            int rad = UiSprites.TierFor(size);
            float lift = Mathf.Clamp(size.y * 0.12f, 5f, 9f);
            Color faceCol = primary ? InkTheme.Cta : InkTheme.Plain;
            Color deepCol = primary ? InkTheme.CtaDeep : InkTheme.PlainDeep;

            DropShadow(parent, name + "_sh", pos, size, pin, rad, 8f);

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, pos, size, pin);
            var lip = go.GetComponent<Image>();
            lip.sprite = UiSprites.Fill(rad);
            lip.type = Image.Type.Sliced;
            lip.color = deepCol;
            AddLine(go.transform, rad, 5, InkTheme.Outline);

            var face = new GameObject("face", typeof(RectTransform), typeof(Image));
            face.transform.SetParent(go.transform, false);
            var fr = face.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(0f, lift);
            fr.offsetMax = Vector2.zero;
            var fi = face.GetComponent<Image>();
            fi.sprite = UiSprites.Fill(rad);
            fi.type = Image.Type.Sliced;
            fi.color = faceCol;
            fi.raycastTarget = false;
            AddLine(face.transform, rad, 5, InkTheme.Outline);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = fi;
            var cols = btn.colors;
            cols.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            cols.disabledColor = new Color(0.70f, 0.70f, 0.70f, 0.7f);
            btn.colors = cols;
            btn.onClick.AddListener(() => click());

            var label = Label(face.transform, "t", text, 29, Vector2.zero, size);
            label.color = primary ? InkTheme.CardFace : InkTheme.TextDark;
            Bold(label);
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

        // count 跟着局外「基地生命」升级走，不能写死 3 颗。
        public static Image[] Hearts(Transform parent, Vector2 pos, float size, int count, Pin pin = Pin.Top)
        {
            count = Mathf.Clamp(count, 1, 8);
            float step = size + 8f;
            var wrap = Panel(parent, "hp", pos, new Vector2(step * count + 8f, size + 8f), Color.clear, pin);
            wrap.GetComponent<Image>().raycastTarget = false;
            var hearts = new Image[count];
            float span = (count - 1) * step;
            for (int i = 0; i < count; i++)
            {
                hearts[i] = Icon(wrap, InkArt.Icon(InkShape.Heart, 64), new Vector2(-span * 0.5f + i * step, 0f), size);
                hearts[i].color = Color.white;
            }
            return hearts;
        }

        // 径向充能环。技能键和别的进度都用它，别再各自画一套。
        public static Image RadialFill(Transform parent, Vector2 pos, float size, Color color)
        {
            var img = Icon(parent, InkArt.Heap(InkShape.Circle, color, 96), pos, size);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.fillAmount = 0f;
            img.preserveAspect = false;
            return img;
        }

        // 等距网格里第 i 格的偏移。y 向下为正，和 Pin.Top 一致 ——
        // 用 Pin.Center 摆的话记得自己取反，否则第二行会跑到第一行上面。
        public static Vector2 GridPos(int i, int cols, float stepX, float stepY)
        {
            int cx = i % cols;
            int cy = i / cols;
            return new Vector2((cx - (cols - 1) * 0.5f) * stepX, cy * stepY);
        }

        // 底栏。整条悬浮的药丸，不是三个并排的按钮 —— 按钮的意思是「按一下发生一件事」，
        // 底栏的意思是「我现在在哪一页」。两者长得一样时，玩家分不清哪些能按出结果、
        // 哪些只是换个地方看，而且三个立体按钮并排在底部会把视线从内容上抢走。
        // 选中态靠三件事一起说：淡底 + 图标不透明放大 + 标签换粗体主色。
        // 不在图标底下垫饱和色块 —— 这批手绘图标自己带颜色和描边，垫一层就是撞色。
        public const float TabBarH = 128f;   // 底栏在 BottomPad 之上吃掉的高度
        const float TabBarW = 688f;
        const float TabBarInner = 104f;
        const float TabIconOn = 54f;
        const float TabIconOff = 48f;

        public static Button[] TabBar(Transform parent, string[] names, Sprite[] icons, Action<int> pick)
        {
            var bar = Stroke(parent, "tabbar", new Vector2(0f, ScreenFit.BottomPad + 12f),
                new Vector2(TabBarW, TabBarInner), Pin.Bottom, 5f, radius: TabBarInner * 0.34f);
            bar.GetComponent<Image>().raycastTarget = false;

            int n = names.Length;
            float step = TabBarW / n;
            var btns = new Button[n];
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var go = new GameObject("tab" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(bar, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2((i - (n - 1) * 0.5f) * step, 0f);
                rt.sizeDelta = new Vector2(step - 10f, TabBarInner - 14f);

                // 根节点这张图既是选中态的淡底，也是整格的点击区 ——
                // 未选中时 color 是全透明，但照样接射线，所以整格都点得到。
                var glow = go.GetComponent<Image>();
                glow.sprite = UiSprites.Fill(UiSprites.Tier(26f));
                glow.type = Image.Type.Sliced;
                glow.color = Color.clear;

                var btn = go.GetComponent<Button>();
                // 关掉 Button 自带的变色，否则它会和 PaintTab 抢着改同一张图的颜色。
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = glow;
                btn.onClick.AddListener(() => pick(idx));

                if (icons != null && idx < icons.Length && icons[idx] != null)
                    Icon(go.transform, icons[idx], new Vector2(0f, 14f), TabIconOff);
                Label(go.transform, "t", names[i], 22, new Vector2(0f, -28f), new Vector2(step - 16f, 28f));
                btns[i] = btn;
            }
            return btns;
        }

        // 给 Btn 换色。不要直接写 btn.image.color —— 根节点是压深的底唇，
        // 大面积可见的是 face 子节点，只改根节点等于没改。
        public static void PaintBtn(Button btn, Color face, Color deep, Color label)
        {
            if (btn == null) return;
            var lip = btn.GetComponent<Image>();
            if (lip != null) lip.color = deep;
            Transform f = btn.transform.Find("face");
            if (f != null) f.GetComponent<Image>().color = face;
            var t = btn.GetComponentInChildren<Text>();
            if (t != null) t.color = label;
        }

        public static void PaintTab(Button[] tabs, int active)
        {
            if (tabs == null) return;
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                bool on = i == active;
                var glow = tabs[i].GetComponent<Image>();
                if (glow != null) glow.color = on ? InkTheme.TabOn : Color.clear;

                Transform ic = tabs[i].transform.Find("icon");
                if (ic != null)
                {
                    // 压暗要用透明度，不能用 Image.color 相乘 —— 相乘只会把彩色图标弄脏。
                    ic.GetComponent<Image>().color = new Color(1f, 1f, 1f, on ? 1f : 0.42f);
                    float s = on ? TabIconOn : TabIconOff;
                    ic.GetComponent<RectTransform>().sizeDelta = new Vector2(s, s);
                }

                Transform lb = tabs[i].transform.Find("t");
                if (lb != null)
                {
                    var t = lb.GetComponent<Text>();
                    t.color = on ? InkTheme.CtaDeep : InkTheme.TextMid;
                    t.font = on ? FontBold : Font;
                }
            }
        }
    }
}
