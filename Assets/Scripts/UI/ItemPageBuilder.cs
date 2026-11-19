using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 道具页的壳。坐标跟 720×1280 的稿一致，锚在左上。烘「重排道具页」和运行时兜底走同一份。
    // 卡面、字牌、挂锁是工程里的图。图鉴底和进度填充是运行时画的圆角，存不进预制。
    public static class ItemPageBuilder
    {
        public const string Mark = "items_v9";

        public const float CardW = 148f;
        public const float CardH = 199f;
        public const float GapX = 22f;
        public const float GapY = 18f;
        public const int Cols = 4;
        public const float GridX = 31f;
        public const float TitleY = 160f;
        public const float PlateW = 260f;
        public const float PlateH = 52f;
        public const float LineupY = 222f;
        public const float PanelGap = 22f;
        public const float CodexDrop = 72f;
        public const float DockH = 128f;

        public static float LineupW => CardW * Cols + GapX * (Cols - 1);
        public static float LineupH => CardH * 2f + GapY;
        public static float PanelTop => LineupY + LineupH + PanelGap;
        public static float CodexY => PanelTop + CodexDrop;

        // 相对一张 148×199 的卡。底栏按 180 的物品卡底缩过来：等级圆、黑槽、窄箭头。
        public const float FooterScale = 148f / 180f;
        public const float DiscX = 10f * FooterScale;
        public const float DiscY = 184f * FooterScale;
        public const float Disc = 48f * FooterScale;
        public const float FillX = 20f * FooterScale;
        public const float FillY = 194f * FooterScale;
        public const float FillH = 28f * FooterScale;
        public const float FillMax = 135f * FooterScale;
        public const float ArrowX = 140f * FooterScale;
        public const float ArrowY = 184f * FooterScale;
        public const float ArrowW = 32f * FooterScale;
        public const float ArrowH = 40f * FooterScale;
        public const float BarTextX = 60f * FooterScale;
        public const float BarTextY = 192f * FooterScale;
        public const float BarTextW = 80f * FooterScale;
        public const float BarTextH = 32f * FooterScale;
        // 图鉴卡底槽在 148 宽的卡上只有约 26 高。手机上槽和字都偏小，运行时盖一条更高的。
        public const float FootX = 8f;
        public const float FootY = 149f;
        public const float FootW = 132f;
        public const float FootH = 40f;
        public const float FootDisc = 48f;
        public const float FootDiscX = 4f;
        public const float FootDiscY = 145f;
        public const float FootArrowX = 112f;
        public const float FootArrowY = 147f;
        public const float FootArrowW = 30f;
        public const float FootArrowH = 40f;
        public const int FootLvSize = 26;
        public const int FootTextSize = 22;
        public const float IconS = 108f;
        public const float IconX = (34f + 56f) * FooterScale - IconS * 0.5f;
        public const float IconY = (60f + 56f) * FooterScale - IconS * 0.5f;
        public const float BandY = 159f;
        public const float BandH = 40f;
        public const float BadgeX = 101f;
        public const float BadgeY = 12f;
        public const float BadgeW = 39f;
        public const float BadgeH = 26f;

        public static readonly Color FillGreen = new Color(0.490f, 0.827f, 0.329f, 1f);
        public static readonly Color FillYellow = new Color(252f / 255f, 197f / 255f, 74f / 255f, 1f);
        public static readonly Color Paper = new Color(1f, 0.973f, 0.933f, 1f);
        public static readonly Color PanelInk = new Color(42f / 255f, 36f / 255f, 38f / 255f, 1f);
        public const int PanelRadius = 22;

        static Sprite Spr(string file)
        {
            if (SortiePageBuilder.SpriteOf != null)
            {
                Sprite got = SortiePageBuilder.SpriteOf(file);
                if (got != null) return got;
            }
            return InkSprites.Load("Ui/" + file);
        }

        public static HomeItemView Build(RectTransform page)
        {
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = page.offsetMax = Vector2.zero;
            var pageImg = page.GetComponent<Image>();
            if (pageImg != null)
            {
                pageImg.color = new Color(1f, 1f, 1f, 0f);
                pageImg.raycastTarget = false;
            }

            var go = new GameObject("items", typeof(RectTransform));
            go.transform.SetParent(page, false);
            var root = go.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var view = go.AddComponent<HomeItemView>();

            view.Panel = StretchImage(root, "panel", null, 0f, PanelTop, 720f,
                ScreenFit.DesignH - DockH - PanelTop);
            PaintPanel(view.Panel.GetComponent<Image>());

            view.PlateOn = StretchImage(root, "plate_on", "item_plate_on", 28f, TitleY, PlateW, PlateH);
            view.Title = PlateText(view.PlateOn, "title", "装备道具");

            view.PlateCodex = StretchImage(root, "plate_codex", "item_plate_codex", 24f, PanelTop + 12f, PlateW, PlateH);
            view.HeadTitle = PlateText(view.PlateCodex, "title", "道具图鉴");

            view.CountRt = new GameObject("count", typeof(RectTransform)).GetComponent<RectTransform>();
            view.CountRt.SetParent(root, false);
            HomeReskin.TopLeft(view.CountRt, 360f, PanelTop + 18f, 336f, 40f);
            view.HeadCount = UiKit.Label(view.CountRt, "n", "", 18, Vector2.zero, new Vector2(220f, 40f),
                TextAnchor.MiddleRight, Pin.Center);
            HomeReskin.TopLeft(view.HeadCount.rectTransform, 0f, 0f, 220f, 40f);
            view.HeadCount.color = Paper;
            view.HeadCount.supportRichText = true;
            view.HeadCount.alignment = TextAnchor.MiddleRight;
            view.HeadCount.alignByGeometry = true;
            UiKit.Bold(view.HeadCount);
            Edge(view.HeadCount, HomeReskin.Ink);

            view.SwapHint = UiKit.Label(view.CountRt, "swap", "", 18, Vector2.zero, new Vector2(220f, 40f),
                TextAnchor.MiddleRight, Pin.Center);
            HomeReskin.TopLeft(view.SwapHint.rectTransform, 0f, 0f, 220f, 40f);
            view.SwapHint.color = Paper;
            view.SwapHint.alignment = TextAnchor.MiddleRight;
            view.SwapHint.alignByGeometry = true;
            UiKit.Bold(view.SwapHint);
            Edge(view.SwapHint, HomeReskin.Ink);
            view.SwapHint.gameObject.SetActive(false);

            view.SwapCancel = new GameObject("cancel", typeof(RectTransform)).GetComponent<RectTransform>();
            view.SwapCancel.SetParent(view.CountRt, false);
            HomeReskin.TopLeft(view.SwapCancel, 228f, 2f, 100f, 36f);
            view.SwapCancel.gameObject.SetActive(false);

            var lineup = new GameObject("lineup", typeof(RectTransform), typeof(Image));
            lineup.transform.SetParent(root, false);
            view.Shelf = lineup.GetComponent<Image>();
            view.Shelf.color = new Color(1f, 1f, 1f, 0f);
            view.Shelf.raycastTarget = false;
            HomeReskin.TopLeft(view.Shelf.rectTransform, GridX, LineupY, LineupW, LineupH);
            view.Slots = new HomeItemSlot[GameConstants.ItemSlots];
            for (int s = 0; s < view.Slots.Length; s++)
                view.Slots[s] = Slot(view.Shelf.rectTransform, s);

            var port = new GameObject("view", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            port.transform.SetParent(root, false);
            var pr = port.GetComponent<RectTransform>();
            float listW = LineupW;
            float listH = ScreenFit.DesignH - DockH - CodexY - 8f;
            HomeReskin.TopLeft(pr, GridX, CodexY, listW, listH);
            var hit = port.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            var listGo = new GameObject("list", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(pr, false);
            var list = listGo.GetComponent<RectTransform>();
            list.anchorMin = list.anchorMax = new Vector2(0.5f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = new Vector2(listW, 100f);
            var grid = listGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardW, CardH);
            grid.spacing = new Vector2(GapX, GapY);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fit = listGo.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = port.GetComponent<ScrollRect>();
            scroll.viewport = pr;
            scroll.content = list;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            view.Scroll = scroll;
            view.List = list;

            view.Cards = new HomeItemCard[ItemCatalog.Count];
            for (int i = 0; i < view.Cards.Length; i++) view.Cards[i] = Card(list);

            var mark = new GameObject(Mark, typeof(RectTransform));
            mark.transform.SetParent(root, false);
            return view;
        }

        public static void Grow(HomeItemView view)
        {
            if (view == null || view.List == null || view.Cards == null) return;
            if (view.Cards.Length >= ItemCatalog.Count) return;
            var next = new HomeItemCard[ItemCatalog.Count];
            for (int i = 0; i < view.Cards.Length; i++) next[i] = view.Cards[i];
            for (int i = view.Cards.Length; i < next.Length; i++) next[i] = Card(view.List);
            view.Cards = next;
        }

        static HomeItemSlot Slot(RectTransform lineup, int s)
        {
            int col = s % Cols;
            int row = s / Cols;
            var frame = Pic(lineup, "slot" + s, "item_card_empty",
                new Vector2(col * (CardW + GapX), row * (CardH + GapY)),
                new Vector2(CardW, CardH));
            frame.raycastTarget = true;
            var slot = frame.gameObject.AddComponent<HomeItemSlot>();
            slot.Frame = frame;
            slot.Button = frame.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = frame;
            slot.Button.transition = Selectable.Transition.None;
            slot.Glow = Pic(frame.rectTransform, "glow", null, new Vector2(-6f, -6f),
                new Vector2(CardW + 12f, CardH + 12f));
            slot.Glow.transform.SetAsFirstSibling();
            slot.Glow.gameObject.SetActive(false);
            FillChrome(frame.rectTransform, out slot.Icon, out slot.Lv, out slot.Fill, out slot.BarText);
            slot.Plus = UiKit.Label(frame.rectTransform, "plus", "", 28, Vector2.zero, new Vector2(IconS, IconS));
            HomeReskin.TopLeft(slot.Plus.rectTransform, IconX, IconY, IconS, IconS);
            slot.Plus.gameObject.SetActive(false);
            slot.Lock = Pic(frame.rectTransform, "lock", "item_lock",
                new Vector2((CardW - 68f) * 0.5f, 36f), new Vector2(68f, 68f));
            slot.Note = UiKit.Label(frame.rectTransform, "note", "", 16, Vector2.zero, new Vector2(CardW - 16f, BandH));
            HomeReskin.TopLeft(slot.Note.rectTransform, 8f, BandY, CardW - 16f, BandH);
            slot.Note.color = Paper;
            slot.Note.alignment = TextAnchor.MiddleCenter;
            slot.Note.alignByGeometry = true;
            UiKit.Bold(slot.Note);
            Edge(slot.Note, HomeReskin.Ink);
            return slot;
        }

        static HomeItemCard Card(RectTransform list)
        {
            var frame = Pic(list, "it", "item_card_green", Vector2.zero, new Vector2(CardW, CardH));
            frame.raycastTarget = true;
            var card = frame.gameObject.AddComponent<HomeItemCard>();
            card.Frame = frame;
            card.Button = frame.gameObject.AddComponent<Button>();
            card.Button.targetGraphic = frame;
            card.Button.transition = Selectable.Transition.None;
            FillChrome(frame.rectTransform, out card.Icon, out card.Lv, out card.Fill, out card.BarText);
            card.Disc = Pic(frame.rectTransform, "disc", "item_disc", new Vector2(DiscX, DiscY), new Vector2(Disc, Disc));
            card.Disc.preserveAspect = true;
            card.Seal = LevelLock(card.Disc.rectTransform);
            card.Arrow = Pic(frame.rectTransform, "arrow", "item_arrow_yellow", new Vector2(ArrowX, ArrowY), new Vector2(ArrowW, ArrowH));
            card.Arrow.preserveAspect = true;
            card.Lv.transform.SetAsLastSibling();
            card.BarText.transform.SetAsLastSibling();
            card.Worn = Pic(frame.rectTransform, "worn", "item_badge", new Vector2(BadgeX, BadgeY), new Vector2(BadgeW, BadgeH));
            card.WornText = UiKit.Label(card.Worn.rectTransform, "t", "", 12, Vector2.zero, new Vector2(BadgeW, BadgeH));
            card.WornText.color = Paper;
            card.WornText.alignment = TextAnchor.MiddleCenter;
            card.WornText.alignByGeometry = true;
            UiKit.Bold(card.WornText);
            Edge(card.WornText, HomeReskin.Ink);
            var wornRt = card.WornText.rectTransform;
            wornRt.anchorMin = Vector2.zero;
            wornRt.anchorMax = Vector2.one;
            wornRt.offsetMin = Vector2.zero;
            wornRt.offsetMax = Vector2.zero;
            card.Worn.gameObject.SetActive(false);
            return card;
        }

        public static Image LevelLock(RectTransform disc)
        {
            float s = 26f;
            var img = Pic(disc, "seal", "item_lock", new Vector2((Disc - s) * 0.5f, (Disc - s) * 0.5f), new Vector2(s, s));
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.gameObject.SetActive(false);
            return img;
        }

        static void FillChrome(RectTransform card, out Image icon, out Text lv, out Image fill, out Text bar)
        {
            fill = Pic(card, "fill", null, new Vector2(FillX, FillY), new Vector2(FillH, FillH));
            fill.color = FillGreen;
            fill.gameObject.SetActive(false);
            bar = UiKit.Label(card, "bar", "", 15, Vector2.zero, new Vector2(BarTextW, BarTextH));
            HomeReskin.TopLeft(bar.rectTransform, BarTextX, BarTextY, BarTextW, BarTextH);
            bar.color = Paper;
            bar.alignment = TextAnchor.MiddleCenter;
            bar.alignByGeometry = true;
            UiKit.Bold(bar);
            Edge(bar, HomeReskin.Ink);
            icon = Pic(card, "icon", null, new Vector2(IconX, IconY), new Vector2(IconS, IconS));
            icon.preserveAspect = true;
            lv = UiKit.Label(card, "lv", "", 20, Vector2.zero, new Vector2(Disc, Disc));
            HomeReskin.TopLeft(lv.rectTransform, DiscX, DiscY, Disc, Disc);
            lv.color = Paper;
            lv.alignment = TextAnchor.MiddleCenter;
            lv.alignByGeometry = true;
            UiKit.Bold(lv);
            Edge(lv, HomeReskin.Ink);
        }

        static Text PlateText(RectTransform plate, string name, string text)
        {
            var t = UiKit.Label(plate, name, text, 28, Vector2.zero, plate.sizeDelta);
            t.color = Paper;
            t.alignment = TextAnchor.MiddleCenter;
            t.alignByGeometry = true;
            UiKit.Bold(t);
            Edge(t, HomeReskin.Ink);
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-8f, 0f);
            return t;
        }

        static void Edge(Text t, Color c)
        {
            var o = t.gameObject.GetComponent<Outline>();
            if (o == null) o = t.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(1.2f, -1.2f);
        }

        // 图鉴底是纯色，只圆上沿。运行时画，生成的图存不进预制，进页面时再补一次。
        public static void PaintPanel(Image panel)
        {
            if (panel == null) return;
            if (panel.sprite == null) panel.sprite = UiSprites.FillTop(PanelRadius);
            panel.type = Image.Type.Sliced;
            panel.color = PanelInk;
            panel.pixelsPerUnitMultiplier = 1f;
        }

        static RectTransform StretchImage(Transform parent, string name, string file, float x, float y, float w, float h)
        {
            var img = Pic(parent, name, file, new Vector2(x, y), new Vector2(w, h));
            img.raycastTarget = false;
            return img.rectTransform;
        }

        static Image Pic(Transform parent, string name, string file, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            HomeReskin.TopLeft(go.GetComponent<RectTransform>(), pos.x, pos.y, size.x, size.y);
            var img = go.GetComponent<Image>();
            if (file != null) img.sprite = Spr(file);
            img.preserveAspect = file == "item_lock";
            img.raycastTarget = false;
            return img;
        }
    }
}
