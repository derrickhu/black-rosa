using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴两张弹窗的壳，只挂工程里的图。烘成 Resources/Prefabs/Codex、CodexDetail，
    // 预制缺了就按这里当场搭一份。CodexPanel / CodexDetail 只按节点名找，两边要对得上。
    public static class CodexLayout
    {
        public const string MainPrefab = "Prefabs/Codex";
        public const string DetailPrefab = "Prefabs/CodexDetail";

        public const float BoardW = 700f;
        // 木框原图边厚约 65px，按这个倍率压到 40px 左右，让开页签和格子。
        const float FrameMul = 1.3f;

        // 板子顶上那条「图鉴」飘带往下压到 80 左右，页签得排在它下面，红点才露得出来。
        public const float TabY = 96f;
        public const float TabW = 186f;
        public const float TabH = 96f;
        public const float TabStep = 200f;
        public const float TrackY = 202f;
        public const float TrackW = 600f;
        public const float TrackH = 64f;
        public const float MileY = 276f;
        public const float MileW = 190f;
        public const float MileH = 110f;
        public const float MileStep = 204f;
        const float HintY = 396f;
        const float ListTop = 428f;
        const float ListBottom = 46f;
        const float ListSide = 38f;
        public const int Cols = 3;
        public const float CellW = 190f;
        // 卡图宽高比 0.772，格子照着比例开，整张贴不拉伸。
        public const float CellH = 246f;
        public const float Gap = 12f;
        // 卡图里字面中心比卡心高 7.5%，名字条中心在卡心下 36.5%。
        // 字面底色就是字图的宣纸色 FCFCF6，字图那块方底贴上去看不出边。
        public const float FaceUp = 0.075f;
        public const float NameDown = 0.365f;
        public const float FaceY = CellH * FaceUp;
        const float FaceW = 196f;
        const float FaceH = 254f;

        public const float DetailW = 680f;
        public const float DetailH = 940f;
        public const float RowW = 560f;
        public const float RowH = 90f;
        public const float StarTop = 554f;
        public const float ShotW = 330f;
        public const float ShotH = 250f;

        public static readonly CodexTab[] Tabs = { CodexTab.Glyph, CodexTab.Pair, CodexTab.Enemy };

        public static RectTransform Main(Transform layer) => Spawn(layer, MainPrefab, "codex") ?? BuildMain(layer);

        public static RectTransform Detail(Transform layer) =>
            Spawn(layer, DetailPrefab, "codex_detail") ?? BuildDetail(layer);

        public static void SetTitle(Transform board, string title)
        {
            Transform t = board.Find("ribbon/t");
            if (t != null) t.GetComponent<Text>().text = title;
        }

        static RectTransform Spawn(Transform layer, string path, string name)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, layer, false);
            go.name = name;
            UiKit.ApplyTo(go.transform);
            return (RectTransform)go.transform;
        }

        public static RectTransform BuildMain(Transform parent)
        {
            var dim = Shell(parent, "codex", "图鉴", new Vector2(0f, 120f), new Vector2(BoardW, 1000f), Pin.Top);
            var board = (RectTransform)dim.Find("board");

            for (int i = 0; i < Tabs.Length; i++)
            {
                var plate = UiKit.Art(board, "tab" + i, "Ui/codex_tab_off",
                    new Vector2((i - 1) * TabStep, TabY), new Vector2(TabW, TabH), Pin.Top);
                plate.gameObject.AddComponent<Button>().targetGraphic = plate.GetComponent<Image>();
                Named(UiKit.Icon(plate, InkSprites.Load(CodexCatalog.TabIcon(Tabs[i])),
                    new Vector2(-TabW * 0.5f + 44f, -6f), 40f), "ico");
                UiKit.Bold(UiKit.Label(plate, "t", CodexCatalog.TabName(Tabs[i]), 28,
                    new Vector2(18f, -6f), new Vector2(TabW - 76f, 40f)));
                // 云头两侧的肩比中间低一截，红点放在右肩上。
                Badge(plate, new Vector2(TabW * 0.5f - 22f, TabH * 0.5f - 26f));
            }

            var track = CodexPanel.Slab(board, "track", "Ui/codex_track", new Vector2(0f, TrackY),
                new Vector2(TrackW, TrackH));
            CodexPanel.Slab(track, "fill", "Ui/codex_track_fill", Vector2.zero,
                new Vector2(TrackH, TrackH * 0.44f - 2f), Pin.Center);
            var count = UiKit.Bold(UiKit.Label(track, "count", "", 22,
                new Vector2(0f, TrackH * 0.04f), new Vector2(TrackW - 80f, 30f)));
            count.color = InkTheme.CardFace;

            for (int i = 0; i < 3; i++)
            {
                var host = UiKit.Art(board, "mile" + i, "Ui/codex_mile_off",
                    new Vector2((i - 1) * MileStep, MileY), new Vector2(MileW, MileH), Pin.Top);
                host.gameObject.AddComponent<Button>().targetGraphic = host.GetComponent<Image>();
                // 票右边那块奶油底才是写字的地方，约占票宽 60%、中心偏右 12%。
                UiKit.Bold(UiKit.Label(host, "need", "", 18, new Vector2(MileW * 0.12f, -15f), new Vector2(104f, 26f)));
                UiKit.Bold(UiKit.Label(host, "ink", "", 22, new Vector2(MileW * 0.12f, 13f), new Vector2(104f, 30f)));
            }

            var hint = UiKit.Label(board, "hint", "", 20, new Vector2(0f, HintY),
                new Vector2(BoardW - 100f, 28f), TextAnchor.MiddleCenter, Pin.Top);
            hint.color = InkTheme.TextMid;

            BuildList(board);
            BuildCell(board);
            return dim;
        }

        static void BuildList(RectTransform board)
        {
            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(board, false);
            var viewport = vp.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(ListSide, ListBottom);
            viewport.offsetMax = new Vector2(-ListSide, -ListTop);
            vp.GetComponent<Image>().color = Color.clear;

            var go = new GameObject("cells", typeof(RectTransform));
            go.transform.SetParent(viewport, false);
            var content = go.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
        }

        // 格子模板，藏在板子下面。CodexPanel 每格复制一份再填图。
        static void BuildCell(RectTransform board)
        {
            var card = UiKit.Art(board, "cell", "Ui/codex_face", Vector2.zero, new Vector2(CellW, CellH), Pin.Top);
            var img = card.GetComponent<Image>();
            img.raycastTarget = true;
            card.gameObject.AddComponent<Button>().targetGraphic = img;

            UiKit.Bold(UiKit.Label(card, "n", "", 20, new Vector2(0f, -CellH * NameDown), new Vector2(CellW - 60f, 28f)));
            Hidden(UiKit.Icon(card, null, new Vector2(0f, FaceY), 144f), "art");
            Hidden(UiKit.Icon(card, null, new Vector2(-37f, FaceY), 74f), "ga");
            Hidden(UiKit.Icon(card, null, new Vector2(37f, FaceY), 74f), "gb");
            Hidden(UiKit.Icon(card, InkSprites.Load("Ui/codex_lock"),
                new Vector2(CellW * 0.5f - 30f, CellH * 0.5f - 30f), 36f), "lock");
            Badge(card, new Vector2(CellW * 0.5f - 16f, CellH * 0.5f - 16f));
            card.gameObject.SetActive(false);
        }

        public static RectTransform BuildDetail(Transform parent)
        {
            var dim = Shell(parent, "codex_detail", "", new Vector2(0f, -10f), new Vector2(DetailW, DetailH), Pin.Center);
            var board = (RectTransform)dim.Find("board");

            var face = UiKit.Art(board, "face", "Ui/codex_face", new Vector2(-170f, 108f),
                new Vector2(FaceW, FaceH), Pin.Top);
            face.GetComponent<Image>().raycastTarget = false;
            float faceY = FaceH * FaceUp;
            Hidden(UiKit.Icon(face, null, new Vector2(0f, faceY), 150f), "glyph");
            Hidden(UiKit.Icon(face, null, new Vector2(0f, faceY + 43f), 84f), "ga");
            Hidden(UiKit.Icon(face, null, new Vector2(0f, faceY - 43f), 84f), "gb");
            Hidden(UiKit.Icon(face, null, new Vector2(0f, faceY), 140f), "person");
            var plus = UiKit.Bold(UiKit.Label(face, "plus", "+", 30, new Vector2(58f, faceY), new Vector2(34f, 36f)));
            plus.color = InkTheme.TextMid;
            plus.gameObject.SetActive(false);
            UiKit.Bold(UiKit.Label(face, "n", "", 20, new Vector2(0f, -FaceH * NameDown), new Vector2(FaceW - 60f, 28f)));

            var shot = Box(board, "shotBox", new Vector2(120f, 110f), new Vector2(ShotW, ShotH));
            var cap = UiKit.Label(shot, "cap", "炮弹", 20, new Vector2(-ShotW * 0.5f + 46f, ShotH * 0.5f - 30f),
                new Vector2(60f, 26f));
            cap.color = InkTheme.TextMid;
            Hidden(BigQ(shot, 110), "q");
            var plain = UiKit.Label(shot, "plain", "不改炮弹外形", 20, new Vector2(0f, -ShotH * 0.5f + 48f),
                new Vector2(ShotW - 60f, 26f));
            plain.color = InkTheme.TextMid;
            plain.gameObject.SetActive(false);

            var note = UiKit.Label(board, "note", "", 22, new Vector2(0f, 376f), new Vector2(RowW, 30f),
                TextAnchor.MiddleCenter, Pin.Top);
            note.color = InkTheme.TextMid;
            var lore = UiKit.Label(board, "lore", "", 26, new Vector2(0f, 418f), new Vector2(RowW, 120f),
                TextAnchor.UpperLeft, Pin.Top);
            lore.horizontalOverflow = HorizontalWrapMode.Wrap;
            lore.lineSpacing = 1.15f;

            BuildStars(board);
            BuildPair(board);
            BuildEnemy(board);
            return dim;
        }

        static void BuildStars(RectTransform board)
        {
            var g = Group(board, "stars");
            Caption(g, "星级数值");
            var tip = UiKit.Label(g, "tip", "点一行看对应星级的炮弹", 20, new Vector2(RowW * 0.5f - 130f, StarTop - 8f),
                new Vector2(260f, 30f), TextAnchor.MiddleRight, Pin.Top);
            tip.color = InkTheme.TextDim;
            for (int s = 1; s <= GameConstants.MaxStar; s++)
            {
                var row = CodexPanel.Slab(g, "s" + s, "Ui/codex_row",
                    new Vector2(0f, StarTop + 28f + (s - 1) * (RowH + 8f)), new Vector2(RowW, RowH));
                var img = row.GetComponent<Image>();
                img.raycastTarget = true;
                row.gameObject.AddComponent<Button>().targetGraphic = img;
                for (int k = 0; k < s; k++)
                    UiKit.Icon(row, InkSprites.Ui("star"), new Vector2(-RowW * 0.5f + 44f + k * 30f, 0f), 30f);
                var t = UiKit.Label(row, "v", "", 23, new Vector2(66f, 0f), new Vector2(RowW - 170f, RowH - 8f),
                    TextAnchor.MiddleLeft);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        static void BuildPair(RectTransform board)
        {
            var g = Group(board, "pair");
            Caption(g, "额外效果");
            var box = CodexPanel.Slab(g, "effect", "Ui/codex_row_on", new Vector2(0f, StarTop + 28f),
                new Vector2(RowW, 120f));
            var t = UiKit.Bold(UiKit.Label(box, "v", "", 28, Vector2.zero, new Vector2(RowW - 80f, 100f)));
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var how = UiKit.Label(g, "how", "再叠第三个字也照样生效，炮弹会多挂一颗对应颜色的小珠。", 21,
                new Vector2(0f, StarTop + 176f), new Vector2(RowW + 40f, 60f), TextAnchor.UpperCenter, Pin.Top);
            how.horizontalOverflow = HorizontalWrapMode.Wrap;
            how.color = InkTheme.TextMid;
        }

        static void BuildEnemy(RectTransform board)
        {
            var g = Group(board, "enemy");
            var box = Box(g, "stat", new Vector2(120f, 110f), new Vector2(ShotW, ShotH));
            Hidden(BigQ(box, 90), "q");
            for (int i = 0; i < 4; i++)
                UiKit.Bold(UiKit.Label(box, "s" + i, "", 26, new Vector2(16f, 60f - i * 40f),
                    new Vector2(ShotW - 120f, 36f), TextAnchor.MiddleLeft));
            var cap = UiKit.Bold(UiKit.Label(g, "cap", "本事", 24, new Vector2(-RowW * 0.5f + 40f, 530f),
                new Vector2(80f, 30f), TextAnchor.MiddleCenter, Pin.Top));
            cap.color = InkTheme.TextDark;
            var trait = CodexPanel.Slab(g, "trait", "Ui/codex_row", new Vector2(0f, 566f), new Vector2(RowW, 130f));
            var tv = UiKit.Label(trait, "v", "", 26, Vector2.zero, new Vector2(RowW - 90f, 80f));
            tv.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static RectTransform Shell(Transform parent, string name, string title, Vector2 pos, Vector2 size, Pin pin)
        {
            var dim = UiKit.Dimmer(parent);
            dim.name = name;
            var board = CodexPanel.Slab(dim, "board", "Ui/codex_board", pos, size, pin);
            var img = board.GetComponent<Image>();
            img.raycastTarget = true;
            if (img.type == Image.Type.Sliced) img.pixelsPerUnitMultiplier = FrameMul;

            Sprite rib = InkSprites.Load("Ui/ribbon_chapter");
            float ribW = 380f;
            float ribH = rib != null && rib.rect.width > 1f ? ribW * rib.rect.height / rib.rect.width : 100f;
            var ribbon = UiKit.Art(board, "ribbon", "Ui/ribbon_chapter", new Vector2(0f, -ribH * 0.46f),
                new Vector2(ribW, ribH), Pin.Top);
            ribbon.GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Bold(UiKit.Label(ribbon, "t", title, 38, new Vector2(0f, ribH * 0.10f), new Vector2(260f, 56f)));
            t.color = InkTheme.CardFace;

            // 板子 700 宽几乎贴满 720 的屏，关闭键得收进木框角里，不然右半边出屏。
            var close = UiKit.Art(board, "close", "Ui/codex_close", Vector2.zero, new Vector2(80f, 80f));
            close.anchorMin = close.anchorMax = Vector2.one;
            close.anchoredPosition = new Vector2(-34f, -26f);
            close.gameObject.AddComponent<Button>().targetGraphic = close.GetComponent<Image>();
            return dim;
        }

        // 纯白圆角框，原图 160 见方、圆角 40，按原尺寸切九宫格。
        static RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size) =>
            CodexPanel.Slab(parent, name, "Ui/codex_box", pos, size, Pin.Top, 160f);

        static RectTransform Group(RectTransform board, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(board, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static void Caption(RectTransform g, string text)
        {
            var cap = UiKit.Bold(UiKit.Label(g, "cap", text, 24, new Vector2(-RowW * 0.5f + 60f, StarTop - 8f),
                new Vector2(140f, 30f), TextAnchor.MiddleCenter, Pin.Top));
            cap.color = InkTheme.TextDark;
        }

        static Text BigQ(Transform box, int size)
        {
            var q = UiKit.Bold(UiKit.Label(box, "q", "？", size, new Vector2(0f, 4f), new Vector2(ShotW, ShotH)));
            q.color = InkTheme.LineDim;
            return q;
        }

        static void Badge(Transform parent, Vector2 pos)
        {
            Hidden(UiKit.Icon(parent, InkSprites.Load("Ui/codex_dot"), pos, 32f), "dot");
        }

        static void Named(Component c, string name) => c.gameObject.name = name;

        static void Hidden(Component c, string name)
        {
            c.gameObject.name = name;
            c.gameObject.SetActive(false);
        }
    }
}
