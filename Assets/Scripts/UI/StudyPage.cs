using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 金箔的图。正式图出来前先借星星。
    public static class FoilArt
    {
        public static Sprite Icon => InkSprites.Ui("foil");
        public static readonly Color Face = InkTheme.Hex("F7C948");
        public static readonly Color Deep = InkTheme.Hex("B7801E");
    }

    // 底栏「字卡」页。金箔贴在体力栏下面，右边是印章，兑换留在两者之间。
    // 底、说明条、分类条和卡面用确认稿导出的图，按稿上的位置摆。字和数字运行时填。
    public sealed class StudyPage : MonoBehaviour
    {
        const int Cols = 5;
        const float CellW = 136f;
        const float CellH = 183f;
        const float StepX = 142f;
        const float StepY = 196f;
        // 第二行金箔贴在体力栏下沿再隔一条缝。整页跟着上移，金箔仍挨着说明条。
        const float PanelY = 264f;
        const float HintY = 280f;
        const float FiltersY = 332f;
        const float GridY = 404f;
        const float FoilY = 180f;
        const float SealY = 160f;
        const float DockH = 128f;

        MetaProgress _meta;
        RectTransform _layer;
        Action _changed;
        RectTransform _page;
        RectTransform _content;
        ScrollRect _list;
        Text _foil;
        RectTransform _foilChip;
        RectTransform _foilPlus;
        Text _hint;
        Text _note;
        RectTransform _sealMark;
        RectTransform _panel;
        RectTransform _cap;
        RectTransform _filters;
        RectTransform _on;
        RectTransform _view;
        bool _laid;
        float _laidW;
        float _laidH;
        readonly List<Button> _chips = new List<Button>();
        StudyCatalog.Filter _filter = StudyCatalog.Filter.All;
        readonly List<CodexEntry> _shown = new List<CodexEntry>();
        float _scroll = -1f;

        public static StudyPage Build(RectTransform page)
        {
            var p = page.GetComponent<StudyPage>();
            if (p != null) return p;
            p = page.gameObject.AddComponent<StudyPage>();
            p._page = page;
            p.Shell();
            return p;
        }

        public void Bind(MetaProgress meta, RectTransform layer, Action changed)
        {
            _meta = meta;
            _layer = layer;
            _changed = changed;
            _meta.MarkStudySeen();
            Refresh();
        }

        void Shell()
        {
            var pageImg = _page.GetComponent<Image>();
            if (pageImg != null)
            {
                pageImg.color = Color.clear;
                pageImg.raycastTarget = false;
            }

            _panel = new GameObject("sheet", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            _panel.SetParent(_page, false);
            _cap = Piece(_page, "cap", "study_sheet_cap", new Vector2(720f, 88f));

            var wash = new GameObject("wash", typeof(RectTransform), typeof(Image));
            wash.transform.SetParent(_page, false);
            var washImg = wash.GetComponent<Image>();
            washImg.sprite = InkSprites.Load("Ui/study_wash");
            washImg.raycastTarget = false;
            _hint = UiKit.Label(_page, "hint", "升级字卡，带这个字的炮弹伤害更高", 17,
                Vector2.zero, new Vector2(420f, 40f), TextAnchor.MiddleLeft, Pin.Top);
            _hint.color = InkTheme.Hex("244B6B");
            _hint.raycastTarget = false;
            UiKit.Bold(_hint);
            _note = UiKit.Label(_page, "count", "", 16, Vector2.zero, new Vector2(148f, 40f),
                TextAnchor.MiddleRight, Pin.Top);
            _note.color = InkTheme.Hex("4F7593");
            _note.supportRichText = true;
            _note.raycastTarget = false;
            UiKit.Bold(_note);

            _foil = UiKit.Chip(_page, "foil", FoilArt.Icon, "0", Vector2.zero, new Vector2(162f, 32f), Pin.Top, out _foilChip);
            var foilHit = _foilChip.GetComponent<Image>();
            foilHit.raycastTarget = true;
            var foilBtn = _foilChip.gameObject.AddComponent<Button>();
            foilBtn.transition = Selectable.Transition.None;
            foilBtn.targetGraphic = foilHit;
            foilBtn.onClick.AddListener(ShowFoil);

            _filters = Piece(_page, "filters", "study_filter_bar", new Vector2(688f, 56f));
            _on = Piece(_filters, "on", "study_filter_on", new Vector2(93f, 44f));
            var names = StudyCatalog.Filters;
            const float inner = 6f;
            const float gap = 4f;
            const float cellW = 93f;
            const float cellH = 44f;
            for (int i = 0; i < names.Length; i++)
            {
                StudyCatalog.Filter f = names[i];
                var b = FilterChip(StudyCatalog.FilterName(f), new Vector2(inner + i * (cellW + gap), inner), new Vector2(cellW, cellH), () => Pick(f));
                _chips.Add(b);
            }

            FoilPlus();
            SealMark();

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(_page, false);
            _view = vp.GetComponent<RectTransform>();
            vp.GetComponent<Image>().color = Color.clear;

            var rows = new GameObject("rows", typeof(RectTransform));
            rows.transform.SetParent(_view, false);
            _content = rows.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            _list = vp.GetComponent<ScrollRect>();
            _list.content = _content;
            _list.viewport = _view;
            _list.horizontal = false;
            _list.vertical = true;
            _list.movementType = ScrollRect.MovementType.Clamped;
            _list.scrollSensitivity = 28f;
            _list.onValueChanged.AddListener(v => _scroll = v.y);
            Relayout();
        }

        public void Relayout()
        {
            if (_page == null) return;
            if (_laid && Mathf.Approximately(_laidW, HomeReskin.ScreenW) && Mathf.Approximately(_laidH, HomeReskin.ScreenH))
                return;
            _laid = true;
            _laidW = HomeReskin.ScreenW;
            _laidH = HomeReskin.ScreenH;

            float dock = HomeReskin.ScreenH - HomeReskin.BottomExtra() - DockH;
            float panelY = HomeReskin.PlaceY(PanelY);
            float sheetH = Mathf.Max(420f, dock - panelY + 16f);
            HomeReskin.TopLeft(_panel, HomeReskin.PlaceX(0f), panelY, ScreenFit.DesignW, sheetH);
            HomeReskin.TopLeft(_cap, HomeReskin.PlaceX(0f), panelY - 24f, 720f, 88f);
            TileSheet(sheetH);

            float hintY = HomeReskin.PlaceY(HintY);
            var wash = _page.Find("wash") as RectTransform;
            if (wash != null) HomeReskin.TopLeft(wash, HomeReskin.PlaceX(0f), hintY, ScreenFit.DesignW, 40f);
            HomeReskin.TopLeft(_hint.rectTransform, HomeReskin.PlaceX(72f), hintY, 420f, 40f);
            HomeReskin.TopLeft(_note.rectTransform, HomeReskin.PlaceX(500f), hintY, 148f, 40f);

            float filtersY = HomeReskin.PlaceY(FiltersY);
            HomeReskin.TopLeft(_filters, HomeReskin.PlaceX(16f), filtersY, 688f, 56f);

            float gridY = HomeReskin.PlaceY(GridY);
            HomeReskin.TopLeft(_view, HomeReskin.PlaceX(0f), gridY, ScreenFit.DesignW, Mathf.Max(160f, dock - gridY - 8f));

            RestyleFoil();
            HomeReskin.TopLeft(_sealMark, HomeReskin.PlaceX(624f), HomeReskin.PlaceY(SealY), 96f, 90f);
            _foilChip.SetAsLastSibling();
            _sealMark.SetAsLastSibling();
        }

        // 金箔货币栏右侧紧跟一颗绿色加号，点击打开每日兑换。
        void FoilPlus()
        {
            var go = new GameObject("plus", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_foilChip, false);
            var rt = go.GetComponent<RectTransform>();
            // 加号压在金箔条右缘的斜切上，不要悬在条外面。
            HomeReskin.TopLeft(rt, 137f, -9f, 48f, 48f);
            _foilPlus = rt;
            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.005f);
            hit.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(OpenSwap);

            var artGo = new GameObject("art", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(rt, false);
            HomeReskin.TopLeft(artGo.GetComponent<RectTransform>(), 9f, 10f, 30f, 30f);
            var art = artGo.GetComponent<Image>();
            art.sprite = InkSprites.Load("Ui/modal_btn_plus_green");
            art.type = Image.Type.Simple;
            art.preserveAspect = true;
            art.raycastTarget = false;
        }

        // 右上一枚印章标。开了插槽或攒到过印章才出现。
        void SealMark()
        {
            var go = new GameObject("seals", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_page, false);
            var rt = go.GetComponent<RectTransform>();
            var hit = go.GetComponent<Image>();
            hit.color = Color.clear;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(OpenSeals);
            SealArt.Medal(go.transform, "m", SealType.Fierce, SealQuality.Blue, new Vector2(0f, 16f), 54f);
            var t = UiKit.Label(go.transform, "t", "印章", 20, new Vector2(0f, -30f), new Vector2(96f, 32f));
            UiKit.Bold(t);
            var ring = UiKit.Icon(go.transform, UiSprites.Disc(), new Vector2(30f, 36f), 26f);
            ring.name = "dot";
            ring.color = InkTheme.CardFace;
            UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 20f).color = InkTheme.Seal;
            _sealMark = rt;
        }

        Button FilterChip(string text, Vector2 pos, Vector2 size, Action click)
        {
            var go = new GameObject("f_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_filters, false);
            HomeReskin.TopLeft(go.GetComponent<RectTransform>(), pos.x, pos.y, size.x, size.y);
            var face = go.GetComponent<Image>();
            face.color = new Color(1f, 1f, 1f, 0.01f);
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = face;
            btn.onClick.AddListener(() => click());
            var t = UiKit.Label(go.transform, "t", text, 20, Vector2.zero, size);
            UiKit.Bold(t);
            t.raycastTarget = false;
            InkEdge(t, 1.5f);
            return btn;
        }

        void Pick(StudyCatalog.Filter f)
        {
            if (_filter == f) return;
            AudioBus.Tap();
            _filter = f;
            _scroll = -1f;
            Refresh();
        }

        public void Refresh()
        {
            if (_meta == null) return;
            _foil.text = _meta.Foil.ToString();
            _sealMark.gameObject.SetActive(_meta.SealBagShown);
            Transform dot = _sealMark.Find("dot");
            if (dot != null) dot.gameObject.SetActive(_meta.SealNew > 0);
            for (int i = 0; i < _chips.Count; i++)
            {
                bool on = StudyCatalog.Filters[i] == _filter;
                if (on) HomeReskin.TopLeft(_on, 6f + i * 97f, 6f, 93f, 44f);
                _chips[i].GetComponentInChildren<Text>().color = Color.white;
            }
            Fill();
        }

        void Fill()
        {
            float keep = _scroll;
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject old = _content.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            _shown.Clear();
            IReadOnlyList<CodexEntry> all = StudyCatalog.All;
            int owned = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (!_meta.StudyOwned(all[i])) continue;
                owned++;
                if (StudyCatalog.Match(all[i], _filter)) _shown.Add(all[i]);
            }
            _shown.Sort((a, b) => Index(a).CompareTo(Index(b)));
            _note.text = "已收录 <color=#2C9FE2>" + owned + "</color>";
            int rows = (_shown.Count + Cols - 1) / Cols;
            _content.sizeDelta = new Vector2(0f, rows * StepY + 16f);
            for (int i = 0; i < _shown.Count; i++) Cell(_shown[i], i);
            Canvas.ForceUpdateCanvases();
            _list.verticalNormalizedPosition = keep >= 0f ? keep : 1f;
        }

        static int Index(CodexEntry e)
        {
            IReadOnlyList<CodexEntry> all = StudyCatalog.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Kind == e.Kind && all[i].Index == e.Index) return i;
            return 999;
        }

        void Cell(CodexEntry e, int i)
        {
            Vector2 at = UiKit.GridPos(i, Cols, StepX, StepY);
            bool owned = _meta.StudyOwned(e);
            int lv = _meta.StudyLevel(e);
            var card = StudyCard.Build(_content, e, owned, lv, at, new Vector2(CellW, CellH), Pin.Top);
            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = card.GetComponent<Image>();
            btn.onClick.AddListener(() => Open(e));
            if (_meta.HasTrial(e)) StudyCard.Tag(card, "试用", InkTheme.Teal);
            if (_meta.HasLuckSeal(e)) StudyCard.Luck(card);
            if (_meta.UpgradeBlock(e) == MetaProgress.StudyBlock.None || _meta.CanFillSlot(e)) StudyCard.Dot(card);
        }

        void Open(CodexEntry e)
        {
            AudioBus.Tap();
            GlyphDetailPanel.Show(_layer, _meta, _shown, _shown.IndexOf(e), AfterChange);
        }

        void OpenSeals()
        {
            AudioBus.Tap();
            SealBagPanel.Show(_layer, _meta, AfterChange);
        }

        void OpenSwap()
        {
            AudioBus.Tap();
            BagPanel.Show(_layer, _meta, AfterChange);
        }

        void ShowFoil()
        {
            AudioBus.Tap();
            BagPanel.ShowSources(_layer, _foilChip);
        }

        void AfterChange()
        {
            Refresh();
            _changed?.Invoke();
        }

        void RestyleFoil()
        {
            HomeReskin.TopLeft(_foilChip, HomeReskin.PlaceX(97f), HomeReskin.PlaceY(FoilY), 168f, 40f);
            var img = _foilChip.GetComponent<Image>();
            Sprite bar = InkSprites.Load("Ui/home_bar");
            if (bar != null)
            {
                img.sprite = bar;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
            }
            Transform shadow = _page.Find("foil_sh");
            if (shadow != null) shadow.gameObject.SetActive(false);
            Transform line = _foilChip.Find("ln");
            if (line != null) line.gameObject.SetActive(false);
            HomeReskin.TopLeft(_foil.rectTransform, 40f, 0f, 116f, 40f);
            _foil.color = HomeReskin.Cream;
            _foil.alignment = TextAnchor.MiddleCenter;
            _foil.fontSize = UiKit.ReadSize(20);
            var icon = _foilChip.Find("icon") as RectTransform;
            if (icon != null)
            {
                HomeReskin.TopLeft(icon, -7f, -8f, 47f, 47f);
                var mark = icon.GetComponent<Image>();
                if (mark != null) mark.preserveAspect = true;
            }
            if (_foilPlus != null) _foilPlus.SetAsLastSibling();
        }

        // 底沿是圆角那一截，下面用稿上裁出的 128 方块接着铺，星的大小不跟着屏高拉。
        void TileSheet(float height)
        {
            for (int i = _panel.childCount - 1; i >= 0; i--)
                Destroy(_panel.GetChild(i).gameObject);
            Sprite tile = InkSprites.Load("Ui/study_motif");
            const float tileS = 128f;
            const float body = 64f;
            int cols = Mathf.CeilToInt(ScreenFit.DesignW / tileS);
            int rows = Mathf.CeilToInt(Mathf.Max(0f, height - body) / tileS);
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var go = new GameObject("t", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_panel, false);
                HomeReskin.TopLeft(go.GetComponent<RectTransform>(), c * tileS, body + r * tileS, tileS, tileS);
                var img = go.GetComponent<Image>();
                img.sprite = tile;
                img.raycastTarget = false;
            }
        }

        static RectTransform Piece(Transform parent, string name, string file, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = InkSprites.Load("Ui/" + file);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return rt;
        }

        static void InkEdge(Text t, float dist)
        {
            var edge = t.gameObject.AddComponent<Outline>();
            edge.effectColor = InkTheme.Hex("0D050A");
            edge.effectDistance = new Vector2(dist, -dist);
        }
    }

    // 字卡页上的一格。五色卡面是确认稿导出的图。字用游戏里的字图，纸底抠掉后落在米字格上。「Lv.N」运行时填。
    // 详情页只用 Badge，不走这张卡面。
    public static class StudyCard
    {
        public static Color Fill(int lv) =>
            lv >= StudyCatalog.MaxLevel ? InkTheme.Hex("FFD970")
            : lv >= 7 ? InkTheme.Hex("FFE6AE")
            : lv >= 4 ? InkTheme.Hex("FFF3D6")
            : InkTheme.CardFace;

        // 和战斗里棋盘上的品质框同一套颜色。
        public static Color Line(int lv) =>
            lv >= 1 ? StudyCatalog.TierColor(StudyCatalog.TierOf(lv)) : InkTheme.Outline;

        public static Color Badge(int lv) =>
            lv >= 1 ? StudyCatalog.TierDeep(StudyCatalog.TierOf(lv)) : InkTheme.CtaOff;

        // caption 为假时不画底栏。字本身始终画在米字格里。
        public static RectTransform Build(Transform parent, CodexEntry e, bool owned, int lv, Vector2 pos, Vector2 size,
            Pin pin, bool caption = true)
        {
            float k = size.x / 128f;
            var go = new GameObject("c", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var card = go.GetComponent<RectTransform>();
            Place(card, pos, size, pin);
            var face = go.GetComponent<Image>();
            face.sprite = InkSprites.Load("Ui/" + Face(lv));
            face.raycastTarget = true;

            float side = 104f * k;
            if (owned && e.Kind == CodexKind.Glyph)
                PutGlyph(go.transform, (CardId)e.Index, 12f * k, 12f * k, side);
            else if (owned && e.Kind == CodexKind.Word)
                PutWord(go.transform, (WordId)e.Index, 12f * k, 12f * k, side);
            else
            {
                var word = UiKit.Label(go.transform, "zi", "？", Mathf.RoundToInt(64f * k), Vector2.zero, new Vector2(side, side));
                HomeReskin.TopLeft(word.rectTransform, 12f * k, 12f * k, side, side);
                word.color = Color.white;
                UiKit.Bold(word);
                word.raycastTarget = false;
                Edge(word, 2f * k);
            }

            if (owned && caption)
            {
                float top = 116f * k;
                float band = size.y - top;
                var num = UiKit.Label(go.transform, "lv", "Lv." + lv, Mathf.RoundToInt(20f * k),
                    Vector2.zero, new Vector2(size.x, band));
                HomeReskin.TopLeft(num.rectTransform, 0f, top, size.x, band);
                num.color = Color.white;
                UiKit.Bold(num);
                num.raycastTarget = false;
                Edge(num, 2f * k);
            }
            else if (!owned)
            {
                var lk = UiKit.Icon(go.transform, InkSprites.Load("icon_lock"), new Vector2(size.x * 0.5f - 24f * k, size.y * 0.5f - 24f * k), 34f * k);
                lk.color = new Color(1f, 1f, 1f, 0.85f);
            }
            return card;
        }

        public static string Face(int lv)
        {
            switch (StudyCatalog.TierOf(lv))
            {
                case StudyCatalog.Tier.Gold: return "study_card_gold";
                case StudyCatalog.Tier.Purple: return "study_card_purple";
                case StudyCatalog.Tier.Blue: return "study_card_blue";
                case StudyCatalog.Tier.Green: return "study_card_green";
                default: return "study_card_white";
            }
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size, Pin pin)
        {
            rt.sizeDelta = size;
            if (pin == Pin.Top)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(pos.x, -pos.y);
                return;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        static void PutGlyph(Transform parent, CardId id, float x, float y, float size, bool trimWord = false)
        {
            Sprite spr = trimWord ? InkArt.WordGlyph(id, true) : InkArt.CardGlyph(id, false);
            var img = UiKit.Icon(parent, spr, Vector2.zero, size);
            img.color = Color.white;
            img.raycastTarget = false;
            HomeReskin.TopLeft(img.rectTransform, x, y, size, size);
        }

        static void PutWord(Transform parent, WordId word, float x, float y, float side)
        {
            CardId[] parts = CodexCatalog.PartsOf(word);
            int n = Mathf.Min(parts.Length, 2);
            if (n <= 0) return;
            if (n == 1)
            {
                PutGlyph(parent, parts[0], x, y, side);
                return;
            }
            float g = side * 0.44f;
            float gap = side * 0.025f;
            float left = x + (side - (g * 2f + gap)) * 0.5f;
            float top = y + (side - g) * 0.5f;
            for (int i = 0; i < n; i++)
                PutGlyph(parent, parts[i], left + i * (g + gap), top, g, true);
        }

        static void Edge(Text t, float dist)
        {
            var edge = t.gameObject.AddComponent<Outline>();
            edge.effectColor = InkTheme.Hex("0D050A");
            edge.effectDistance = new Vector2(dist, -dist);
        }

        public static void Tag(RectTransform card, string text, Color col)
        {
            Vector2 size = new Vector2(64f, 30f);
            var tag = UiKit.Stroke(card, "tag", new Vector2(card.sizeDelta.x * 0.5f - 34f, -card.sizeDelta.y * 0.5f + 62f),
                size, Pin.Center, 3f, null, col, 15f);
            var t = UiKit.Label(tag, "t", text, 18, Vector2.zero, size);
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);
        }

        // 镶了字运：左上一枚小红印「运」，避开底栏。试用角标仍在右侧。
        public static void Luck(RectTransform card)
        {
            float k = card.sizeDelta.x / 128f;
            var ring = UiKit.Icon(card, UiSprites.Disc(),
                new Vector2(-card.sizeDelta.x * 0.5f + 20f * k, card.sizeDelta.y * 0.5f - 20f * k), 30f * k);
            ring.name = "luck";
            ring.color = InkTheme.Outline;
            UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 24f * k).color = InkTheme.Seal;
            var t = UiKit.Label(ring.transform, "t", "运", Mathf.RoundToInt(16 * k), Vector2.zero, new Vector2(30f, 30f) * k);
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);
        }

        public static void Dot(RectTransform card)
        {
            var ring = UiKit.Icon(card, UiSprites.Disc(), new Vector2(card.sizeDelta.x * 0.5f - 8f, card.sizeDelta.y * 0.5f - 8f), 28f);
            ring.color = InkTheme.CardFace;
            UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 22f).color = InkTheme.Seal;
        }
    }
}
