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

    // 底栏「字卡」页。金箔贴在体力栏下面，右边是兑换图标，再是分类和四列字卡。
    public sealed class StudyPage : MonoBehaviour
    {
        const int Cols = 4;
        const float CellW = 152f;
        const float CellH = 196f;
        const float StepX = 164f;
        const float StepY = 208f;
        const float BarY = 2f;
        const float ChipW = 210f;
        const float ChipH = 54f;
        const float NoteY = BarY + ChipH + 4f;
        const float ChipsY = NoteY + 40f;
        const float ListTop = ChipsY + ChipH + 10f;

        MetaProgress _meta;
        RectTransform _layer;
        Action _changed;
        RectTransform _page;
        RectTransform _content;
        ScrollRect _list;
        Text _foil;
        RectTransform _foilChip;
        Text _note;
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
            // 和顶上三颗货币同一套药丸，贴在这一页的最上沿，也就是体力栏正下方。
            _foil = UiKit.Chip(_page, "foil", FoilArt.Icon, "0", new Vector2(0f, BarY),
                new Vector2(ChipW, ChipH), Pin.Top, out _foilChip);
            var foilHit = _foilChip.GetComponent<Image>();
            foilHit.raycastTarget = true;
            var foilBtn = _foilChip.gameObject.AddComponent<Button>();
            foilBtn.transition = Selectable.Transition.None;
            foilBtn.targetGraphic = foilHit;
            foilBtn.onClick.AddListener(ShowFoil);
            _note = UiKit.Label(_page, "note", "", 20, new Vector2(0f, NoteY), new Vector2(420f, 28f),
                TextAnchor.MiddleCenter, Pin.Top);
            _note.color = InkTheme.TextMid;
            SwapMark(new Vector2(292f, BarY));

            var names = StudyCatalog.Filters;
            float step = 96f;
            for (int i = 0; i < names.Length; i++)
            {
                StudyCatalog.Filter f = names[i];
                var b = FilterChip(StudyCatalog.FilterName(f), new Vector2((i - (names.Length - 1) * 0.5f) * step, ChipsY),
                    new Vector2(step - 8f, ChipH), () => Pick(f));
                _chips.Add(b);
            }

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(_page, false);
            var view = vp.GetComponent<RectTransform>();
            view.anchorMin = new Vector2(0f, 0f);
            view.anchorMax = new Vector2(1f, 1f);
            view.offsetMin = new Vector2(10f, 6f);
            view.offsetMax = new Vector2(-10f, -ListTop);
            vp.GetComponent<Image>().color = Color.clear;

            var rows = new GameObject("rows", typeof(RectTransform));
            rows.transform.SetParent(view, false);
            _content = rows.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            _list = vp.GetComponent<ScrollRect>();
            _list.content = _content;
            _list.viewport = view;
            _list.horizontal = false;
            _list.vertical = true;
            _list.movementType = ScrollRect.MovementType.Clamped;
            _list.scrollSensitivity = 28f;
            _list.onValueChanged.AddListener(v => _scroll = v.y);
        }

        // 右侧一枚兑换标，字在图下面。点开只做每日墨换金箔。
        void SwapMark(Vector2 pos)
        {
            var go = new GameObject("swap", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_page, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(96f, 90f);
            rt.anchoredPosition = new Vector2(pos.x, -pos.y);
            var hit = go.GetComponent<Image>();
            hit.color = Color.clear;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(OpenSwap);
            UiKit.Icon(go.transform, InkSprites.Ui("swap"), new Vector2(0f, 16f), 56f);
            var t = UiKit.Label(go.transform, "t", "兑换", 20, new Vector2(0f, -28f), new Vector2(96f, 26f));
            UiKit.Bold(t);
        }

        Button FilterChip(string text, Vector2 pos, Vector2 size, Action click)
        {
            var root = UiKit.Stroke(_page, "f_" + text, pos, size, Pin.Top, 4f, radius: size.y * 0.5f);
            var btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = root.GetComponent<Image>();
            btn.onClick.AddListener(() => click());
            var t = UiKit.Label(root, "t", text, 24, Vector2.zero, size);
            UiKit.Bold(t);
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
            for (int i = 0; i < _chips.Count; i++)
            {
                bool on = StudyCatalog.Filters[i] == _filter;
                _chips[i].GetComponent<Image>().color = on ? InkTheme.Seal : InkTheme.CardFace;
                _chips[i].GetComponentInChildren<Text>().color = on ? InkTheme.CardFace : InkTheme.TextDark;
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
            _note.text = "已有 " + owned + "/" + all.Count + " · 上限 Lv." + _meta.StudyCap;
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
            Vector2 at = UiKit.GridPos(i, Cols, StepX, StepY) + new Vector2(0f, 8f);
            bool owned = _meta.StudyOwned(e);
            int lv = _meta.StudyLevel(e);
            var card = StudyCard.Build(_content, e, owned, lv, at, new Vector2(CellW, CellH), Pin.Top);
            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = card.GetComponent<Image>();
            btn.onClick.AddListener(() => Open(e));
            if (_meta.HasTrial(e)) StudyCard.Tag(card, "试用", InkTheme.Teal);
            if (_meta.UpgradeBlock(e) == MetaProgress.StudyBlock.None) StudyCard.Dot(card);
        }

        void Open(CodexEntry e)
        {
            AudioBus.Tap();
            GlyphDetailPanel.Show(_layer, _meta, _shown, _shown.IndexOf(e), AfterChange);
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
    }

    // 一张字卡的卡面。字卡页的格子和详情页的大卡共用。
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

        // caption 为假时不在图下再写字名。详情大卡的图里已经有字。
        public static RectTransform Build(Transform parent, CodexEntry e, bool owned, int lv, Vector2 pos, Vector2 size,
            Pin pin, bool caption = true)
        {
            float k = size.x / 152f;
            Transform host = parent;
            if (owned && lv >= StudyCatalog.MaxLevel)
            {
                var rays = UiKit.Icon(parent, InkSprites.Load("Ui/result_rays"), Vector2.zero, size.y * 1.15f);
                var rr = rays.rectTransform;
                rr.anchorMin = rr.anchorMax = pin == Pin.Top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
                rr.anchoredPosition = pin == Pin.Top ? new Vector2(pos.x, -pos.y - size.y * 0.5f) : pos;
                rays.color = new Color(1f, 0.82f, 0.4f, 0.55f);
                UiAnim.On(rays).Spin(rays.transform, 18f);
            }
            var card = UiKit.Stroke(host, "c", pos, size, pin, owned && lv >= 7 ? 6f : 5f,
                owned ? Line(lv) : InkTheme.LineDim, owned ? Fill(lv) : InkTheme.CardDim, 24f * k);
            Color tint = owned ? Color.white : new Color(0.25f, 0.2f, 0.16f, 0.32f);
            float y = caption ? 18f * k : 0f;
            if (e.Kind == CodexKind.Glyph)
                Glyph(card, InkArt.Glyph((CardId)e.Index, 160), new Vector2(0f, y), 104f * k, tint);
            else
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int i = 0; i < parts.Length && i < 2; i++)
                    Glyph(card, InkArt.Glyph(parts[i], 160), new Vector2((i == 0 ? -30f : 30f) * k, y), 62f * k, tint);
            }
            if (caption)
            {
                var name = UiKit.Label(card, "name", owned ? CodexCatalog.Title(e) : "？？", Mathf.RoundToInt(26 * k),
                    new Vector2(0f, -size.y * 0.5f + 30f * k), new Vector2(size.x, 36f * k));
                name.color = owned ? InkTheme.TextDark : InkTheme.TextDim;
                UiKit.Bold(name);
            }
            if (owned)
            {
                Vector2 pillSize = new Vector2(70f, 32f) * k;
                var pill = UiKit.Stroke(card, "lv", new Vector2(-size.x * 0.5f + pillSize.x * 0.5f + 8f * k,
                    size.y * 0.5f - pillSize.y * 0.5f - 8f * k), pillSize, Pin.Center, 3f, null, Badge(lv), pillSize.y * 0.5f);
                var t = UiKit.Label(pill, "t", lv >= StudyCatalog.MaxLevel ? "MAX" : "Lv." + lv,
                    Mathf.RoundToInt(20 * k), Vector2.zero, pillSize);
                t.color = InkTheme.CardFace;
                UiKit.Bold(t);
            }
            else
            {
                var lk = UiKit.Icon(card, InkSprites.Load("icon_lock"), new Vector2(size.x * 0.5f - 24f * k, size.y * 0.5f - 24f * k), 34f * k);
                lk.color = new Color(1f, 1f, 1f, 0.85f);
            }
            return card;
        }

        static void Glyph(Transform card, Sprite s, Vector2 pos, float size, Color tint)
        {
            if (s == null) return;
            UiKit.Icon(card, s, pos, size).color = tint;
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

        public static void Dot(RectTransform card)
        {
            var ring = UiKit.Icon(card, UiSprites.Disc(), new Vector2(card.sizeDelta.x * 0.5f - 8f, card.sizeDelta.y * 0.5f - 8f), 28f);
            ring.color = InkTheme.CardFace;
            UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 22f).color = InkTheme.Seal;
        }
    }
}
