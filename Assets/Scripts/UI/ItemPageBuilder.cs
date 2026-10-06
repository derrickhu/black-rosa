using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 道具页的壳。烘「重排道具页」和运行时兜底走同一份坐标。
    // 只挂工程里的图；光晕、进度条这类程序生成的图在 HomeItemPage 绑定时补上，存不进预制。
    public static class ItemPageBuilder
    {
        public const string Mark = "items_v3";

        public const float ShelfTop = 4f;
        public const float ShelfW = 680f;
        public const float ShelfH = 254f;
        // 木架原图 900 宽，九宫格四角按这个倍率缩，和整张图缩到 680 宽时一样粗。
        const float ShelfSrcW = 900f;
        // 红牌子单独一张图，骑在木架上沿；标题挂在牌子下面，跟着牌子走。
        const float PlaqueW = 168f;
        const float PlaqueH = 59f;
        const float ShelfDrop = 18f;
        const float SlotSize = 128f;
        const float SlotStep = 204f;
        const float SlotY = -4f;

        public const float HeadY = ShelfTop + ShelfH + 24f;
        public const float ListTop = HeadY + 26f;

        public const float CardW = 208f;
        public const float CardH = 268f;
        const float CardGap = 14f;

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
            var go = new GameObject("items", typeof(RectTransform));
            go.transform.SetParent(page, false);
            var root = go.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var view = go.AddComponent<HomeItemView>();

            var shelf = Pic(root, "shelf", "panel_item_shelf", new Vector2(0f, ShelfTop + ShelfDrop),
                new Vector2(ShelfW, ShelfH - ShelfDrop), Pin.Top);
            shelf.type = Image.Type.Sliced;
            shelf.pixelsPerUnitMultiplier = ShelfSrcW / ShelfW;
            shelf.preserveAspect = false;
            view.Shelf = shelf;
            var plaque = Pic(root, "plaque", "panel_item_plaque", new Vector2(0f, ShelfTop), new Vector2(PlaqueW, PlaqueH), Pin.Top);
            plaque.preserveAspect = false;
            var title = UiKit.Label(plaque.rectTransform, "title", "道具栏", 26, new Vector2(0f, 1f), new Vector2(PlaqueW, PlaqueH));
            title.color = InkTheme.CardFace;
            Edge(title, InkTheme.Hex("7A1E14"));
            view.Title = title;

            view.Slots = new HomeItemSlot[GameConstants.ItemSlots];
            for (int s = 0; s < view.Slots.Length; s++)
                view.Slots[s] = Slot(shelf.rectTransform, s, new Vector2((s - 1) * SlotStep, SlotY));

            var head = Box(root, "head", new Vector2(0f, HeadY), new Vector2(ShelfW, 44f), Pin.Top);
            view.Head = head;
            view.HeadTitle = UiKit.Label(head, "t", "道具图鉴", 26, new Vector2(-ShelfW * 0.5f + 80f, 0f),
                new Vector2(160f, 40f), TextAnchor.MiddleLeft);
            UiKit.Bold(view.HeadTitle);
            view.HeadCount = UiKit.Label(head, "n", "", 20, new Vector2(ShelfW * 0.5f - 90f, 0f),
                new Vector2(180f, 30f), TextAnchor.MiddleRight);
            view.HeadCount.color = InkTheme.TextMid;
            view.SwapHint = UiKit.Label(head, "swap", "", 24, new Vector2(-60f, 0f), new Vector2(420f, 40f),
                TextAnchor.MiddleLeft);
            view.SwapHint.color = InkTheme.Seal;
            UiKit.Bold(view.SwapHint);
            view.SwapHint.gameObject.SetActive(false);
            view.SwapCancel = Box(head, "cancel", new Vector2(ShelfW * 0.5f - 60f, 0f), new Vector2(110f, 48f), Pin.Center);

            var port = new GameObject("view", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            port.transform.SetParent(root, false);
            var pr = port.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(0f, 6f);
            pr.offsetMax = new Vector2(0f, -ListTop);
            port.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            var listGo = new GameObject("list", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(pr, false);
            var list = listGo.GetComponent<RectTransform>();
            list.anchorMin = list.anchorMax = list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = new Vector2(3f * CardW + 2f * CardGap, 100f);
            var grid = listGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardW, CardH);
            grid.spacing = new Vector2(CardGap, CardGap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(0, 0, 8, 24);
            var fit = listGo.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = port.GetComponent<ScrollRect>();
            scroll.viewport = pr;
            scroll.content = list;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48f;
            view.Scroll = scroll;
            view.List = list;

            view.Cards = new HomeItemCard[ItemCatalog.Count];
            for (int i = 0; i < view.Cards.Length; i++) view.Cards[i] = Card(list, i);

            var mark = new GameObject(Mark, typeof(RectTransform));
            mark.transform.SetParent(root, false);
            return view;
        }

        // 预制里烘的是当时的张数。图鉴变多时把缺的卡补进同一条列表，格子布局会自己排开。
        public static void Grow(HomeItemView view)
        {
            if (view == null || view.List == null || view.Cards == null) return;
            if (view.Cards.Length >= ItemCatalog.Count) return;
            var next = new HomeItemCard[ItemCatalog.Count];
            for (int i = 0; i < view.Cards.Length; i++) next[i] = view.Cards[i];
            for (int i = view.Cards.Length; i < next.Length; i++) next[i] = Card(view.List, i);
            view.Cards = next;
        }

        static HomeItemSlot Slot(RectTransform shelf, int s, Vector2 pos)
        {
            var box = Box(shelf, "slot" + s, pos, new Vector2(SlotSize, SlotSize), Pin.Center);
            var slot = box.gameObject.AddComponent<HomeItemSlot>();
            slot.Glow = Pic(box, "glow", null, Vector2.zero, Vector2.one * SlotSize * 1.7f, Pin.Center);
            slot.Glow.gameObject.SetActive(false);
            slot.Frame = Pic(box, "frame", "item_slot", Vector2.zero, Vector2.one * SlotSize, Pin.Center);
            slot.Frame.raycastTarget = true;
            slot.Button = box.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = slot.Frame;
            slot.Halo = Pic(box, "halo", null, new Vector2(0f, 4f), Vector2.one * SlotSize * 0.9f, Pin.Center);
            slot.Icon = Pic(box, "icon", "ico_item_burst", new Vector2(0f, 6f), Vector2.one * SlotSize * 0.7f, Pin.Center);
            slot.Lock = Pic(box, "lock", "ico_lock", new Vector2(0f, 6f), Vector2.one * 56f, Pin.Center);
            slot.Plus = UiKit.Label(box, "plus", "+", 60, new Vector2(0f, 6f), new Vector2(SlotSize, 70f));
            slot.Plus.color = InkTheme.Hex("C9A57E");
            UiKit.Bold(slot.Plus);
            slot.Badge = Pic(box, "badge", "item_lv_badge", new Vector2(0f, -SlotSize * 0.5f + 4f), new Vector2(78f, 44f), Pin.Center);
            slot.Badge.preserveAspect = false;
            slot.Lv = UiKit.Label(slot.Badge.rectTransform, "t", "Lv.1", 20, new Vector2(0f, 1f), new Vector2(78f, 40f));
            slot.Lv.color = InkTheme.CardFace;
            Edge(slot.Lv, InkTheme.Hex("1F4A2A"));
            slot.Note = UiKit.Label(box, "note", "", 19, new Vector2(0f, -SlotSize * 0.5f + 4f), new Vector2(SlotSize + 30f, 30f));
            slot.Note.color = InkTheme.CardFace;
            Edge(slot.Note, InkTheme.Outline);
            return slot;
        }

        static HomeItemCard Card(RectTransform list, int i)
        {
            var frame = Pic(list, "it" + i, "panel_item_card_green", Vector2.zero, new Vector2(CardW, CardH), Pin.Center);
            frame.preserveAspect = false;
            frame.raycastTarget = true;
            var box = frame.rectTransform;
            var card = frame.gameObject.AddComponent<HomeItemCard>();
            card.Frame = frame;
            card.Button = frame.gameObject.AddComponent<Button>();
            card.Button.targetGraphic = frame;
            card.Button.transition = Selectable.Transition.None;

            card.Quality = UiKit.Label(box, "q", "普通", 19, new Vector2(0f, 96f), new Vector2(CardW, 30f));
            card.Quality.color = InkTheme.CardFace;
            Edge(card.Quality, InkTheme.Outline);
            card.Halo = Pic(box, "halo", null, new Vector2(0f, 28f), Vector2.one * 150f, Pin.Center);
            card.Icon = Pic(box, "icon", "ico_item_burst", new Vector2(0f, 28f), Vector2.one * 104f, Pin.Center);
            card.Badge = Pic(box, "badge", "item_lv_badge", new Vector2(-58f, 64f), new Vector2(62f, 38f), Pin.Center);
            card.Badge.preserveAspect = false;
            card.Lv = UiKit.Label(card.Badge.rectTransform, "t", "Lv.1", 17, new Vector2(0f, 1f), new Vector2(62f, 34f));
            card.Lv.color = InkTheme.CardFace;
            Edge(card.Lv, InkTheme.Hex("1F4A2A"));
            card.Worn = Pic(box, "worn", null, new Vector2(58f, 64f), new Vector2(60f, 28f), Pin.Center);
            card.Worn.color = InkTheme.Seal;
            card.WornText = UiKit.Label(card.Worn.rectTransform, "t", "已装", 17, Vector2.zero, new Vector2(60f, 28f));
            card.WornText.color = InkTheme.CardFace;
            UiKit.Bold(card.WornText);
            card.Name = UiKit.Label(box, "name", "", 24, new Vector2(0f, -44f), new Vector2(CardW, 32f));
            UiKit.Bold(card.Name);

            card.Track = Pic(box, "bar", null, new Vector2(10f, -80f), new Vector2(148f, 26f), Pin.Center);
            card.Track.color = InkTheme.Hex("4A3A30");
            card.Fill = Pic(card.Track.rectTransform, "fill", null, Vector2.zero, new Vector2(148f, 26f), Pin.Center);
            card.Fill.color = InkTheme.Accel;
            card.BarText = UiKit.Label(card.Track.rectTransform, "t", "", 18, Vector2.zero, new Vector2(148f, 26f));
            card.BarText.color = InkTheme.CardFace;
            Edge(card.BarText, InkTheme.Outline);
            card.CardIcon = Pic(box, "cardico", "ico_card_blank", new Vector2(-72f, -78f), Vector2.one * 48f, Pin.Center);
            card.CardItem = Pic(card.CardIcon.rectTransform, "item", "ico_item_burst", Vector2.zero, Vector2.one * 48f * ItemPanel.CardInset, Pin.Center);
            return card;
        }

        static void Edge(Text t, Color c)
        {
            UiKit.Bold(t);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(1.6f, -1.6f);
        }

        static RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Pin pin)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, pos, size, pin);
            return rt;
        }

        static Image Pic(Transform parent, string name, string file, Vector2 pos, Vector2 size, Pin pin)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), pos, size, pin);
            var img = go.GetComponent<Image>();
            if (file != null) img.sprite = Spr(file);
            img.preserveAspect = file != null;
            img.raycastTarget = false;
            return img;
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size, Pin pin)
        {
            rt.sizeDelta = size;
            if (pin == Pin.Top)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(pos.x, -pos.y);
                return;
            }
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }
    }
}
