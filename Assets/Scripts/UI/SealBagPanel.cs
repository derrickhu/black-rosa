using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 字卡页的印章仓库（Figma Panel/其他弹窗/08_印章）。
    // 浅米色无描边圆格铺满，空位也留格子；印章本身带品质色。数量在格内右下角。
    // 底部只有一个主按钮：浏览时是「分解印章」，选中后变成「分解」。
    // 取消和选全部绿是摘要行两侧的文字，不是并列的大按钮。
    public sealed class SealBagPanel : MonoBehaviour
    {
        const float BoardW = 648f;
        const float BoardH = 964f;
        const int Cols = 5;
        const int MinRows = 5;
        const float Slot = 106f;
        const float Gap = 14f;
        const float GridX = 31f;
        const float GridY = 162f;
        const float GridW = Cols * Slot + (Cols - 1) * Gap;
        const float GridH = MinRows * Slot + (MinRows - 1) * Gap;

        const float BarX = 24f;
        const float BarY = 96f;
        const float BarW = 600f;
        const float BarH = 52f;
        const float TabInset = 6f;
        const float TabW = 71f;
        const float TabH = 40f;

        static readonly Color WellLink = InkTheme.Hex("1683C4");
        static readonly Vector4 CountBorder = new Vector4(16f, 16f, 16f, 16f);

        MetaProgress _meta;
        RectTransform _layer;
        Action _changed;
        RectTransform _content;
        ScrollRect _list;
        RectTransform _pill;
        readonly List<Text> _tabLabels = new List<Text>();
        Text _tip;
        Text _sum;
        Image _foilIcon;
        Text _foilNum;
        Button _cancel;
        Button _pickGreen;
        RectTransform _divider;
        Button _main;
        SealType _filter;
        bool _scrap;
        readonly Dictionary<int, int> _picks = new Dictionary<int, int>();

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = ModalKit.Stage(layer, "seal_bag", 0.78f, null, out RectTransform stage);
            var p = dim.gameObject.AddComponent<SealBagPanel>();
            p._meta = meta;
            p._layer = layer;
            p._changed = changed;
            meta.MarkSealsSeen();
            p.Build(stage);
        }

        void Close()
        {
            AudioBus.Tap();
            _changed?.Invoke();
            Destroy(gameObject);
        }

        void Build(RectTransform stage)
        {
            var board = ModalKit.Shell(stage, BoardW, BoardH, "印章", null, null, null, null, Close, out _, out _);

            var bar = ModalKit.Sliced(board, "tabs", "Ui/modal_sub_charcoal", ModalKit.BorderSub16,
                BarX, BarY, BarW, BarH, false);
            _pill = ModalKit.Sliced(bar, "on", "Ui/modal_tab_on", ModalKit.BorderPill12,
                TabInset, (BarH - TabH) * 0.5f, TabW, TabH, false);
            float step = (BarW - TabInset * 2f - TabW) / (SealCatalog.Types.Length);
            int tabs = SealCatalog.Types.Length + 1;
            for (int i = 0; i < tabs; i++)
            {
                SealType t = i == 0 ? SealType.None : SealCatalog.Types[i - 1];
                float x = TabInset + i * step;
                string text = i == 0 ? "全部" : SealCatalog.TypeName(t);
                var label = ModalKit.SolidText(bar, "lab" + i, text, 16, x, (BarH - 24f) * 0.5f, TabW, 24f,
                    ModalKit.CreamLight, TextAnchor.MiddleCenter, true);
                _tabLabels.Add(label);

                var hitGo = new GameObject("hit" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                hitGo.transform.SetParent(bar, false);
                ModalKit.TopLeft(hitGo.GetComponent<RectTransform>(), x, 0f, Mathf.Max(TabW, step), BarH);
                var hit = hitGo.GetComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0.01f);
                var btn = hitGo.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = hit;
                btn.onClick.AddListener(() => Pick(t));
            }

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(board, false);
            var view = vp.GetComponent<RectTransform>();
            ModalKit.TopLeft(view, GridX, GridY, GridW, GridH);
            vp.GetComponent<Image>().color = Color.clear;

            var rows = new GameObject("rows", typeof(RectTransform));
            rows.transform.SetParent(view, false);
            _content = rows.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, GridH);

            _list = vp.GetComponent<ScrollRect>();
            _list.content = _content;
            _list.viewport = view;
            _list.horizontal = false;
            _list.vertical = true;
            _list.movementType = ScrollRect.MovementType.Clamped;
            _list.scrollSensitivity = 28f;

            _tip = ModalKit.SolidText(board, "tip", "点击印章可查看词条说明", 16, 84f, 760f, 480f, 24f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, false);

            _cancel = TextLink(board, "cancel", "取消", 48f, InkTheme.Hex("735C47"), ExitScrap);
            _pickGreen = TextLink(board, "green", "选全部绿", 0f, WellLink, PickGreen);
            var greenRt = _pickGreen.GetComponent<RectTransform>();
            ModalKit.TopLeft(greenRt, BoardW - 48f - greenRt.sizeDelta.x, 754f, greenRt.sizeDelta.x, 40f);
            _sum = ModalKit.SolidText(board, "sum", "", 20, 0f, 760f, 220f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            _foilIcon = ModalKit.Pic(board, "foil", null, 0f, 760f, 28f, 28f, true);
            _foilIcon.sprite = FoilArt.Icon;
            _foilNum = ModalKit.SolidText(board, "foiln", "", 22, 0f, 760f, 48f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);

            _divider = ModalKit.Divider(board, "div", 36f, 792f, 576f);
            _main = ModalKit.HexBtn(board, "wide_yellow", (BoardW - 314f) * 0.5f, 806f, "分解印章", OnMain, out _);

            Paint();
        }

        static Button TextLink(Transform parent, string name, string text, float x, Color color, Action onClick)
        {
            float w = ModalKit.MeasureText(text, 16) + 8f;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            ModalKit.TopLeft(rt, x, 754f, w, 40f);
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.01f);
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            ModalKit.SolidText(rt, "t", text, 16, 0f, 6f, w, 28f, color, TextAnchor.MiddleCenter, true);
            return btn;
        }

        void Pick(SealType t)
        {
            if (_filter == t) return;
            AudioBus.Tap();
            _filter = t;
            if (_list != null) _list.verticalNormalizedPosition = 1f;
            Paint();
        }

        void Paint()
        {
            float step = (BarW - TabInset * 2f - TabW) / SealCatalog.Types.Length;
            for (int i = 0; i < _tabLabels.Count; i++)
            {
                SealType t = i == 0 ? SealType.None : SealCatalog.Types[i - 1];
                bool on = t == _filter;
                _tabLabels[i].color = on ? ModalKit.TextDark : ModalKit.CreamLight;
                if (on) ModalKit.TopLeft(_pill, TabInset + i * step, (BarH - TabH) * 0.5f, TabW, TabH);
            }

            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            int shown = 0;
            foreach (SealType t in SealCatalog.Types)
            {
                if (_filter != SealType.None && t != _filter) continue;
                for (int qi = SealCatalog.QualityCount - 1; qi >= 0; qi--)
                {
                    var q = (SealQuality)qi;
                    if (_meta.BagCount(t, q) <= 0) continue;
                    Cell(t, q, shown++);
                }
            }
            int rows = Mathf.Max(MinRows, (shown + Cols - 1) / Cols);
            int cells = rows * Cols;
            for (int i = shown; i < cells; i++) EmptyCell(i);
            _content.sizeDelta = new Vector2(0f, rows * Slot + (rows - 1) * Gap);

            ApplyFoot();
        }

        void Cell(SealType t, SealQuality q, int i)
        {
            int idx = SealCatalog.BagIndex(t, q);
            int have = _meta.BagCount(t, q);
            _picks.TryGetValue(idx, out int pick);
            bool sel = _scrap && pick > 0;
            var cell = MakeCell(i, true);
            SealArt.Medal(cell, "m", t, q, new Vector2(0f, 5f), 76f);

            string num = sel ? pick + "/" + have : "×" + have;
            float bw = Mathf.Max(44f, ModalKit.MeasureText(num, 14) + 16f);
            var chip = ModalKit.Sliced(cell, "count", "Ui/modal_seal_count", CountBorder,
                Slot - bw - 4f, Slot - 26f, bw, 22f, false);
            ModalKit.OutlinedText(chip, "t", num, 14, 0f, 0f, bw, 22f, Color.white, TextAnchor.MiddleCenter, 1.1f);

            if (sel)
            {
                string tag = "已选 " + pick;
                float tw = Mathf.Max(58f, ModalKit.MeasureText(tag, 13) + 16f);
                ModalKit.Tag(cell, "Ui/modal_tag_coral", Slot - tw - 4f, 4f, tw, 22f, tag, false, 13);
            }

            var hitGo = new GameObject("hit", typeof(RectTransform), typeof(Image), typeof(Button));
            hitGo.transform.SetParent(cell, false);
            ModalKit.TopLeft(hitGo.GetComponent<RectTransform>(), 0f, 0f, Slot, Slot);
            var hit = hitGo.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            var btn = hitGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(() => Tap(t, q));
        }

        void EmptyCell(int i)
        {
            MakeCell(i, false);
        }

        RectTransform MakeCell(int i, bool filled)
        {
            int col = i % Cols;
            int row = i / Cols;
            var go = new GameObject(filled ? "seal" : "empty", typeof(RectTransform));
            go.transform.SetParent(_content, false);
            var rt = go.GetComponent<RectTransform>();
            ModalKit.TopLeft(rt, col * (Slot + Gap), row * (Slot + Gap), Slot, Slot);
            var well = ModalKit.Pic(rt, "well", "Ui/modal_seal_well", 0f, 0f, Slot, Slot, false);
            well.raycastTarget = false;
            return rt;
        }

        void Tap(SealType t, SealQuality q)
        {
            if (!_scrap)
            {
                AudioBus.Tap();
                InkToast.Show(_layer, SealCatalog.TypeName(t) + "：" + SealCatalog.Blurb(t));
                return;
            }
            int idx = SealCatalog.BagIndex(t, q);
            _picks.TryGetValue(idx, out int pick);
            pick = pick >= _meta.BagCount(t, q) ? 0 : pick + 1;
            if (pick == 0) _picks.Remove(idx);
            else _picks[idx] = pick;
            AudioBus.CardFlip(0.9f + 0.05f * Mathf.Min(pick, 6));
            Paint();
        }

        int PickCount(out int foil)
        {
            int n = 0;
            foil = 0;
            foreach (var kv in _picks)
            {
                n += kv.Value;
                foil += kv.Value * SealCatalog.ScrapOf((SealQuality)(kv.Key % SealCatalog.QualityCount));
            }
            return n;
        }

        void ApplyFoot()
        {
            bool scrap = _scrap;
            _tip.gameObject.SetActive(!scrap);
            _cancel.gameObject.SetActive(scrap);
            _pickGreen.gameObject.SetActive(scrap);
            _sum.gameObject.SetActive(scrap);
            _foilIcon.gameObject.SetActive(scrap);
            _foilNum.gameObject.SetActive(scrap);

            int foil = 0;
            int n = scrap ? PickCount(out foil) : 0;
            if (scrap)
            {
                _sum.text = "已选 " + n + " 枚 · 分解可得";
                _foilNum.text = foil.ToString();
                float sumW = ModalKit.MeasureText(_sum.text, 20);
                float numW = ModalKit.MeasureText(_foilNum.text, 22);
                const float ico = 28f;
                const float gap = 8f;
                float total = sumW + gap + ico + gap + numW;
                float x = Mathf.Round((BoardW - total) * 0.5f);
                ModalKit.TopLeft(_sum.rectTransform, x, 760f, sumW + 4f, 28f);
                ModalKit.TopLeft(_foilIcon.rectTransform, x + sumW + gap, 760f, ico, ico);
                ModalKit.TopLeft(_foilNum.rectTransform, x + sumW + gap + ico + gap, 760f, numW + 8f, 28f);
                ModalKit.TopLeft(_divider, 36f, 804f, 576f, 2f);
                ModalKit.TopLeft(_main.GetComponent<RectTransform>(), (BoardW - 314f) * 0.5f, 820f, 314f, 80f);
            }
            else
            {
                ModalKit.TopLeft(_divider, 36f, 792f, 576f, 2f);
                ModalKit.TopLeft(_main.GetComponent<RectTransform>(), (BoardW - 314f) * 0.5f, 806f, 314f, 80f);
            }

            ModalKit.SetBtnLabel(_main, "wide_yellow", scrap ? "分解" : "分解印章");
            bool dimmed = scrap ? n == 0 : _meta.SealTotal == 0;
            ModalKit.SetHexBtnGray(_main, "wide_yellow", "wide_gray", dimmed);
        }

        void OnMain()
        {
            if (_scrap) DoScrap();
            else EnterScrap();
        }

        void EnterScrap()
        {
            if (_meta.SealTotal == 0)
            {
                InkToast.Show(_layer, "还没有印章");
                return;
            }
            AudioBus.Tap();
            _scrap = true;
            _picks.Clear();
            Paint();
        }

        void ExitScrap()
        {
            AudioBus.Tap();
            _scrap = false;
            _picks.Clear();
            Paint();
        }

        void PickGreen()
        {
            AudioBus.Tap();
            foreach (SealType t in SealCatalog.Types)
            {
                if (_filter != SealType.None && t != _filter) continue;
                int c = _meta.BagCount(t, SealQuality.Green);
                if (c > 0) _picks[SealCatalog.BagIndex(t, SealQuality.Green)] = c;
            }
            Paint();
        }

        void DoScrap()
        {
            int n = PickCount(out int foil);
            if (n == 0) return;
            bool rare = false;
            foreach (var kv in _picks)
                if (kv.Key % SealCatalog.QualityCount >= (int)SealQuality.Purple) rare = true;
            if (rare)
            {
                ConfirmPanel.Ask(_layer, "选中的有紫色或金色印章\n分解后拿不回来\n分解 " + n + " 枚，得 " + foil + " 金箔？",
                    "再想想", "分解", Scrap);
                return;
            }
            Scrap();
        }

        void Scrap()
        {
            Vector2 at = RewardFly.Local(_layer, _main.GetComponent<RectTransform>());
            int got = _meta.Scrap(_picks);
            _picks.Clear();
            _scrap = false;
            if (got <= 0)
            {
                Paint();
                return;
            }
            AudioBus.Stamp();
            InkToast.Show(_layer, "金箔 +" + got);
            Paint();
            _changed?.Invoke();
            RewardFly.Piece piece = RewardFly.Foil(_layer, got);
            RectTransform chip = RewardFly.Chip(_layer, "foil", "foil");
            if (chip != null) piece.Land = chip;
            RewardFly.Play(_layer, at, new[] { piece });
        }
    }
}
