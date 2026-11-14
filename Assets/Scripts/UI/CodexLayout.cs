using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴主页与图鉴详情弹窗的 Figma 切图壳（Panel/首页弹窗/06_图鉴 & 06b_图鉴详情）。
    // 可烘成 Resources/Prefabs/Codex、CodexDetail，运行时若检测到旧木框预制（缺 Mark）会自动重建新壳。
    public static class CodexLayout
    {
        public const string Mark = "codex_figma_v7";
        public const string MainPrefab = "Prefabs/Codex";
        public const string DetailPrefab = "Prefabs/CodexDetail";

        public const float BoardW = 648f;
        public const float BoardH = 988f;

        public const float TrackW = 324f;
        public const float TrackH = 30f;

        public const float MileY = 254f;
        public const float MileH = 68f;
        public const float MileStep = 80f;

        public const int Cols = 4;
        public const float CellW = 128f;
        public const float CellH = 172f;
        public const float GapX = 16f;
        public const float Gap = 16f;
        public const float GridPad = 20f;

        public const float FaceW = 128f;
        public const float FaceH = 172f;

        public const float DetailW = 648f;
        public const float DetailH = 720f;
        public const float RowW = 592f;
        public const float RowH = 64f;
        public const float ShotW = 448f;
        public const float ShotH = 172f;

        public static readonly CodexTab[] Tabs = { CodexTab.Glyph, CodexTab.Pair, CodexTab.Enemy };

        public static RectTransform Main(Transform layer) => Spawn(layer, MainPrefab, "codex", true) ?? BuildMain(layer);

        public static RectTransform Detail(Transform layer) =>
            Spawn(layer, DetailPrefab, "codex_detail", false) ?? BuildDetail(layer);

        public static void SetTitle(Transform board, string title)
        {
            Transform t = board.Find("head/title") ?? board.Find("ribbon/t");
            if (t != null) t.GetComponent<Text>().text = title;
        }

        static RectTransform Spawn(Transform layer, string path, string name, bool withClose)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            // 老预制没有最新 Mark 节点，跳过它改走新 Figma 切图壳
            if (prefab.transform.Find("stage/board/" + Mark) == null) return null;
            var go = Object.Instantiate(prefab, layer, false);
            go.name = name;
            UiKit.ApplyTo(go.transform);
            Transform st = go.transform.Find("stage");
            if (st != null) ModalKit.FitStage(st as RectTransform, layer as RectTransform);
            // 烘预制时弹出动画的第一帧（缩到 0.72 并往右上偏）会被存进去，这里拉回设计稿正中。
            ModalKit.SettleBoard(go.transform.Find("stage/board") as RectTransform);
            if (!withClose)
            {
                Transform old = go.transform.Find("stage/board/close");
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            return (RectTransform)go.transform;
        }

        public static RectTransform BuildMain(Transform parent)
        {
            var dim = ModalKit.Stage(parent, "codex", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "图鉴", null,
                "集齐可领灵墨", "yellow", "0 / 0", null, out _, out _, animate: parent != null);
            ClearMarks(board);
            var head = board.Find("head");
            if (head != null)
                ModalKit.Pic(head, "ico", "Ui/ico_act_codex", 56f, 18f, 44f, 44f, true);
            ModalKit.CloseBtn(board, 0f, 0f, null);

            ModalKit.AddMark(board, Mark);

            // 三栏分段切换条（字谱 / 秘卷 / 墨谱，统一 54 高外槽 + 42 高滑块）
            var tabBar = ModalKit.Sliced(board, "tabs", "Ui/modal_sub_charcoal", ModalKit.BorderSub16,
                24f, 152f, 600f, 54f, false);
            ModalKit.Sliced(tabBar, "tab_on", "Ui/modal_tab_on", ModalKit.BorderPill12,
                6f, 6f, 192f, 42f, false);
            for (int i = 0; i < Tabs.Length; i++)
            {
                float tx = 6f + i * 198f;
                var plate = new GameObject("tab" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                plate.transform.SetParent(tabBar, false);
                var prt = plate.GetComponent<RectTransform>();
                ModalKit.TopLeft(prt, tx, 6f, 192f, 42f);
                var hit = plate.GetComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0.01f);
                plate.GetComponent<Button>().targetGraphic = hit;

                ModalKit.SolidText(prt, "t", CodexCatalog.TabName(Tabs[i]), 18,
                    0f, 0f, 192f, 42f, ModalKit.TextDark, TextAnchor.MiddleCenter, true);
                var dot = ModalKit.Pic(prt, "dot", "Ui/home_dot", 168f, 2f, 18f, 18f, true);
                dot.gameObject.SetActive(false);
            }

            // 收集进度：标题一行，下面进度条和三档奖励同一行，数量标在各自物品框正下方。
            const float cardX = 24f, cardY = 210f, cardW = 600f, cardH = 146f;
            var mBar = ModalKit.Sliced(board, "mile_card", "Ui/modal_sub_cream", ModalKit.BorderSub18,
                cardX, cardY, cardW, cardH, false);
            ModalKit.SolidText(mBar, "cap", "收集进度", 20, 18f, 12f, 320f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);

            var track = ModalKit.ProgressBar(mBar, 18f, 64f, TrackW, TrackH, 0f, "", out _, out Text cntText);
            track.SetParent(board, true);
            track.name = "track";
            cntText.name = "count";

            for (int i = 0; i < 3; i++)
            {
                float sx = cardX + 356f + i * MileStep;
                float sy = cardY + 44f;
                var slot = ModalKit.ItemSlot(board, ModalKit.InkTone, 68, sx, sy, ModalKit.Load("Ui/ico_ink"), "0", forceNodes: true);
                slot.name = "mile" + i;
                var sImg = slot.GetComponent<Image>();
                sImg.raycastTarget = true;
                slot.gameObject.AddComponent<Button>().targetGraphic = sImg;

                Transform n = slot.Find("n");
                if (n != null) n.name = "ink";

                ModalKit.SolidText(slot, "need", "", 13, 0f, 70f, 68f, 20f,
                    ModalKit.TextBody, TextAnchor.MiddleCenter, true);
                var chk = ModalKit.Pic(slot, "done_chk", "Ui/checkin_check", 42f, -6f, 26f, 26f, true);
                chk.gameObject.SetActive(false);
                var rdy = ModalKit.Tag(slot, "Ui/modal_tag_coral", 8f, -8f, 52f, 22f, "可领", false, 12);
                rdy.name = "ready_tag";
                rdy.gameObject.SetActive(false);
            }

            // 4 列字卡滚动列表（复用 Page/字卡/统一 128×172 卡面）
            BuildList(board, 24f, 366f, 600f, 556f);
            BuildCell(board);

            var hint = ModalKit.SolidText(board, "hint", "", 16, 24f, 934f, 600f, 32f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, false);
            hint.name = "hint";

            return dim;
        }

        static void ClearMarks(RectTransform board)
        {
            for (int i = board.childCount - 1; i >= 0; i--)
            {
                Transform c = board.GetChild(i);
                if (!c.name.StartsWith("mark")) continue;
                if (Application.isPlaying) Object.Destroy(c.gameObject);
                else Object.DestroyImmediate(c.gameObject);
            }
        }

        static void BuildList(RectTransform board, float x, float y, float w, float h)
        {
            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(board, false);
            var viewport = vp.GetComponent<RectTransform>();
            ModalKit.TopLeft(viewport, x, y, w, h);
            vp.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            var go = new GameObject("cells", typeof(RectTransform));
            go.transform.SetParent(viewport, false);
            var content = go.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(w, 0f);

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
        }

        static void BuildCell(RectTransform board)
        {
            var cardImg = ModalKit.Pic(board, "cell", "Ui/study_card_blue", 0f, 0f, CellW, CellH, false);
            cardImg.raycastTarget = true;
            var card = cardImg.rectTransform;
            card.gameObject.AddComponent<Button>().targetGraphic = cardImg;

            // 和字卡页同一套：米字格 12,12 的 104 方格，名字落在 116 以下的底栏。
            const float face = 104f;
            const float g = 58f;
            const float gap = 2f;
            float left = 12f + (face - (g * 2f + gap)) * 0.5f;
            float top = 12f + (face - g) * 0.5f;
            Hidden(FaceIcon(card, "art", 12f, 12f, face), "art");
            Hidden(FaceIcon(card, "ga", left, top, g), "ga");
            Hidden(FaceIcon(card, "gb", left + g + gap, top, g), "gb");
            Hidden(ModalKit.OutlinedText(card, "q", "？", 44, 12f, 12f, face, face,
                InkTheme.Hex("8A7F74"), TextAnchor.MiddleCenter), "q");

            ModalKit.OutlinedText(card, "n", "", 20, 0f, 116f, CellW, 56f,
                Color.white, TextAnchor.MiddleCenter, 1.25f);

            Hidden(ModalKit.Pic(card, "lock", "Ui/item_lock", CellW - 32f, 8f, 24f, 24f, true), "lock");
            var newTag = ModalKit.Tag(card, "Ui/modal_tag_coral", CellW - 58f, 6f, 50f, 22f, "新", false, 13);
            newTag.name = "dot";
            newTag.gameObject.SetActive(false);

            card.gameObject.SetActive(false);
        }

        static Image FaceIcon(RectTransform parent, string name, float x, float y, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            ModalKit.TopLeft(go.GetComponent<RectTransform>(), x, y, size, size);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Image CenterIcon(RectTransform parent, string name, float cx, float cy, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -cy);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        // 06c_图鉴详情（定稿版）：黄顶奶油面板（无右上角关闭按钮，点空白处关闭）+ 说明暗条
        // + 左 128×172 字卡与右 448×172 弹形预览并排 + 单行简介 + 无外框大金星与浅黄文本框 + 底部加成说明。
        public static RectTransform BuildDetail(Transform parent)
        {
            var dim = ModalKit.Stage(parent, "codex_detail", 0.82f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, DetailW, DetailH, "", null, "", null, null, null,
                out Text stripLabel, out _, animate: false);
            if (stripLabel != null) stripLabel.gameObject.name = "note";
            ModalKit.AddMark(board, Mark);

            // 左侧标准 128×172 字卡展示（复用 Page/字卡/统一 卡面）
            var faceImg = ModalKit.Pic(board, "face", "Ui/study_card_blue", 28f, 160f, FaceW, FaceH, false);
            var face = faceImg.rectTransform;
            Hidden(CenterIcon(face, "glyph", 64f, 64f, 92f), "glyph");
            Hidden(CenterIcon(face, "ga", 64f, 44f, 50f), "ga");
            Hidden(CenterIcon(face, "gb", 64f, 88f, 50f), "gb");
            Hidden(CenterIcon(face, "person", 64f, 64f, 92f), "person");
            var plus = ModalKit.OutlinedText(face, "plus", "+", 22, 82f, 50f, 28f, 28f,
                Color.white, TextAnchor.MiddleCenter);
            plus.gameObject.SetActive(false);
            ModalKit.OutlinedText(face, "n", "", 18, 6f, 122f, FaceW - 12f, 42f,
                Color.white, TextAnchor.MiddleCenter, 1.25f);

            // 右侧 448×172 实时弹形预览暗窗
            var shot = ModalKit.Sliced(board, "shotBox", "Ui/modal_sub_dark", ModalKit.BorderSub16,
                172f, 160f, ShotW, ShotH, false);
            ModalKit.SolidText(shot, "cap", "弹形预览", 15, 16f, 10f, 160f, 24f,
                InkTheme.Hex("FFE455"), TextAnchor.MiddleLeft, true);
            Hidden(ModalKit.OutlinedText(shot, "q", "？", 56, 0f, 32f, ShotW, 118f,
                InkTheme.Hex("786E66"), TextAnchor.MiddleCenter), "q");
            var plain = ModalKit.SolidText(shot, "plain", "不改炮弹外形", 15, 16f, 138f, ShotW - 32f, 24f,
                InkTheme.Hex("CFC6B8"), TextAnchor.MiddleCenter, false);
            plain.gameObject.SetActive(false);

            // 直接居中排版的单行简介文字（无外围卡片框）
            var lore = ModalKit.SolidText(board, "lore", "", 18, 36f, 342f, 576f, 40f,
                ModalKit.TextBody, TextAnchor.MiddleCenter, true);
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

            const float starSize = 56f;
            const float starGap = 4f;
            const float starZoneW = 196f;

            for (int s = 1; s <= GameConstants.MaxStar; s++)
            {
                float ry = 388f + (s - 1) * 90f;
                var rowGo = new GameObject("s" + s, typeof(RectTransform), typeof(Image), typeof(Button));
                rowGo.transform.SetParent(g, false);
                var row = rowGo.GetComponent<RectTransform>();
                ModalKit.TopLeft(row, 28f, ry, RowW, 64f);
                var hitImg = rowGo.GetComponent<Image>();
                hitImg.color = new Color(1f, 1f, 1f, 0f);
                hitImg.raycastTarget = true;
                var btn = rowGo.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = hitImg;

                float groupW = s * starSize + (s - 1) * starGap;
                float ox0 = Mathf.Round((starZoneW - groupW) * 0.5f);
                for (int k = 0; k < s; k++)
                {
                    var st = ModalKit.Pic(row, "st" + k, "Ui/home_star", ox0 + k * (starSize + starGap), 4f, starSize, starSize, true);
                    st.raycastTarget = false;
                }

                var box = ModalKit.Sliced(row, "box", "Ui/modal_sub_honey", ModalKit.BorderSub18,
                    208f, 6f, 368f, 52f, false);
                var v = ModalKit.SolidText(box, "v", "", 18, 16f, 4f, 336f, 44f,
                    ModalKit.TextBody, TextAnchor.MiddleCenter, true);
                v.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            var note = ModalKit.SolidText(g, "tip", "说明：此处数值不含字卡等级和镶嵌加成", 16,
                28f, 672f, RowW, 26f, ModalKit.TextMuted, TextAnchor.MiddleCenter, false);
            note.name = "tip";
        }

        static void BuildPair(RectTransform board)
        {
            var g = Group(board, "pair");
            var box = ModalKit.Sliced(g, "effect", "Ui/modal_sub_honey", ModalKit.BorderSub18,
                28f, 404f, RowW, 168f, false);
            ModalKit.Tag(box, "Ui/modal_tag_coral", 18f, 16f, 72f, 28f, "效果", false, 13);
            var t = ModalKit.SolidText(box, "v", "", 20, 22f, 54f, RowW - 44f, 98f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;

            var how = ModalKit.SolidText(g, "how", "同发炮弹再叠第三个字也照常生效，炮弹外圈会多挂一颗对应色小珠。",
                16, 36f, 600f, RowW - 16f, 52f, ModalKit.TextMuted, TextAnchor.UpperCenter, false);
            how.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static void BuildEnemy(RectTransform board)
        {
            var g = Group(board, "enemy");
            var box = ModalKit.Sliced(g, "stat", "Ui/modal_sub_dark", ModalKit.BorderSub16,
                172f, 160f, ShotW, ShotH, false);
            Hidden(ModalKit.OutlinedText(box, "q", "？", 56, 0f, 30f, ShotW, 114f,
                InkTheme.Hex("786E66"), TextAnchor.MiddleCenter), "q");
            for (int i = 0; i < 4; i++)
            {
                float sx = 28f + (i % 2) * 204f;
                float sy = 32f + (i / 2) * 64f;
                ModalKit.SolidText(box, "s" + i, "", 20, sx, sy, 184f, 40f,
                    ModalKit.CreamLight, TextAnchor.MiddleLeft, true);
            }

            var trait = ModalKit.Sliced(g, "trait", "Ui/modal_sub_honey", ModalKit.BorderSub18,
                28f, 404f, RowW, 196f, false);
            ModalKit.Tag(trait, "Ui/modal_tag_coral", 18f, 16f, 72f, 28f, "特性", false, 13);
            var tv = ModalKit.SolidText(trait, "v", "", 20, 22f, 56f, RowW - 44f, 122f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, false);
            tv.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static RectTransform Group(RectTransform board, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(board, false);
            var rt = (RectTransform)go.transform;
            ModalKit.TopLeft(rt, 0f, 0f, DetailW, DetailH);
            return rt;
        }

        static void Hidden(Component c, string name)
        {
            c.gameObject.name = name;
            c.gameObject.SetActive(false);
        }
    }
}
