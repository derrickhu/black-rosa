using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 字帖弹窗：一屏点选最多 2 个「偏爱」（三选一出现率 ×1.65）与最多 2 个「封存」（本局不进三选一池）。
    // 成词按整词生效，两个半边字同步加权或剔除，绝不会留下孤立半边字。每日残局不受字帖影响。
    public sealed class CopybookPanel : MonoBehaviour
    {
        const float BoardW = 648f;
        const float BoardH = 964f;
        const int Cols = 4;
        const float CellW = 128f;
        const float CellH = 172f;
        const float StepX = 140f;
        const float StepY = 186f;
        const float GridX = 36f;
        const float GridY = 224f;
        const float GridW = 576f;
        const float GridH = 592f;

        const float BarX = 24f;
        const float BarY = 92f;
        const float BarW = 600f;
        const float BarH = 54f;

        static readonly Color FavBadge = InkTheme.Hex("E28A18");
        static readonly Color BanBadge = InkTheme.Hex("C94848");

        MetaProgress _meta;
        RectTransform _layer;
        RectTransform _stage;
        Action _changed;
        RectTransform _board;
        RectTransform _pill;
        Text _favTab;
        Text _banTab;
        Text _tip;
        RectTransform _content;
        ScrollRect _list;
        Button _clearBtn;
        bool _banMode;
        float _scroll = -1f;
        readonly List<CodexEntry> _owned = new List<CodexEntry>();

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = ModalKit.Stage(layer, "copybook_panel", 0.78f, null, out RectTransform stage);
            var p = dim.gameObject.AddComponent<CopybookPanel>();
            p._meta = meta;
            p._layer = layer;
            p._stage = stage;
            p._changed = changed;
            p.Build(stage);
        }

        void OnRectTransformDimensionsChange()
        {
            if (_stage != null) ModalKit.FitStage(_stage, _layer);
        }

        void Close()
        {
            AudioBus.Tap();
            _changed?.Invoke();
            Destroy(gameObject);
        }

        void Build(RectTransform stage)
        {
            _board = ModalKit.Shell(stage, BoardW, BoardH, "出战字帖", null, null, null, null, Close, out _, out _, animate: false);

            var bar = ModalKit.Sliced(_board, "tabs", "Ui/modal_sub_charcoal", ModalKit.BorderSub16,
                BarX, BarY, BarW, BarH, false);
            const float tabW = 290f;
            const float tabH = 42f;
            _pill = ModalKit.Sliced(bar, "on", "Ui/modal_tab_on", ModalKit.BorderPill12,
                6f, (BarH - tabH) * 0.5f, tabW, tabH, false);

            _favTab = ModalKit.SolidText(bar, "t_fav", "偏爱 (0/2)", 18, 6f, 0f, tabW, BarH,
                ModalKit.CreamLight, TextAnchor.MiddleCenter, true);
            _banTab = ModalKit.SolidText(bar, "t_ban", "封存 (0/2)", 18, BarW - 6f - tabW, 0f, tabW, BarH,
                ModalKit.CreamLight, TextAnchor.MiddleCenter, true);

            MakeTabHit(bar, "hit_fav", 6f, tabW, () => SetMode(false));
            MakeTabHit(bar, "hit_ban", BarW - 6f - tabW, tabW, () => SetMode(true));

            var noteBar = ModalKit.Sliced(_board, "note", "Ui/modal_sub_cream", ModalKit.BorderSub16,
                24f, 158f, 600f, 52f, false);
            _tip = ModalKit.SolidText(noteBar, "tip", "", 15, 16f, 0f, 568f, 52f,
                ModalKit.TextBody, TextAnchor.MiddleCenter, true);

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(_board, false);
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
            _list.onValueChanged.AddListener(v => _scroll = v.y);

            ModalKit.Divider(_board, "div", 32f, 830f, BoardW - 64f);
            _clearBtn = ModalKit.HexBtn(_board, "md_gray", 68f, 854f, "一键清空", OnClear, out _);
            ModalKit.HexBtn(_board, "wide_yellow", 268f, 846f, "选好了", Close, out _);

            ModalKit.FitStage(stage, _layer);
            Refresh();
            PanelKit.Enlarge(_board, 1.04f);
            PanelKit.Open(_board);
        }

        static void MakeTabHit(Transform parent, string name, float x, float w, Action click)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            ModalKit.TopLeft(go.GetComponent<RectTransform>(), x, 0f, w, BarH);
            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(() => click());
        }

        void SetMode(bool ban)
        {
            if (_banMode == ban) return;
            AudioBus.Tap();
            _banMode = ban;
            RefreshHeader();
        }

        void RefreshHeader()
        {
            const float tabW = 290f;
            const float tabH = 42f;
            float x = _banMode ? BarW - 6f - tabW : 6f;
            ModalKit.TopLeft(_pill, x, (BarH - tabH) * 0.5f, tabW, tabH);
            _favTab.text = $"偏爱 ({_meta.CopyFavCount}/{GameConstants.CopybookFavMax})";
            _banTab.text = $"封存 ({_meta.CopyBanCount}/{GameConstants.CopybookBanMax})";
            _favTab.color = !_banMode ? ModalKit.Ink : ModalKit.CreamLight;
            _banTab.color = _banMode ? ModalKit.Ink : ModalKit.CreamLight;
            _tip.text = _banMode
                ? "点选字卡设为「封存」：本局不出（每日副本除外）"
                : "点选字卡设为「偏爱」：三选一出现率提升 ×1.65";
            if (_clearBtn != null)
                ModalKit.SetHexBtnGray(_clearBtn, "md_cyan", "md_gray", _meta.CopyTotalCount == 0);
        }

        void Refresh()
        {
            RefreshHeader();
            float keep = _scroll;
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject old = _content.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            _owned.Clear();
            IReadOnlyList<CodexEntry> all = StudyCatalog.All;
            for (int i = 0; i < all.Count; i++)
                if (_meta.StudyOwned(all[i])) _owned.Add(all[i]);

            int rows = Mathf.Max(1, (_owned.Count + Cols - 1) / Cols);
            _content.sizeDelta = new Vector2(0f, rows * StepY + 16f);
            for (int i = 0; i < _owned.Count; i++)
                BuildCell(_owned[i], i);
            Canvas.ForceUpdateCanvases();
            _list.verticalNormalizedPosition = keep >= 0f ? keep : 1f;
        }

        void BuildCell(CodexEntry e, int i)
        {
            Vector2 at = UiKit.GridPos(i, Cols, StepX, StepY);
            int lv = _meta.StudyLevel(e);
            var card = StudyCard.Build(_content, e, true, lv, at, new Vector2(CellW, CellH), Pin.Top);
            bool fav = _meta.IsCopyFav(e);
            bool ban = _meta.IsCopyBan(e);
            if (fav) StudyCard.Tag(card, "偏爱", FavBadge);
            else if (ban) StudyCard.Tag(card, "封存", BanBadge);
            if (ban)
            {
                var img = card.GetComponent<Image>();
                if (img != null) img.color = new Color(0.82f, 0.78f, 0.78f, 1f);
            }
            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = card.GetComponent<Image>();
            btn.onClick.AddListener(() => OnTapCard(e));
        }

        void OnTapCard(CodexEntry e)
        {
            if (_banMode)
            {
                if (!_meta.IsCopyBan(e) && _meta.CopyBanCount >= GameConstants.CopybookBanMax)
                {
                    AudioBus.Deny();
                    InkToast.Show(_layer, $"最多封存 {GameConstants.CopybookBanMax} 张，先点已封存的取消");
                    return;
                }
                AudioBus.Tap();
                _meta.ToggleCopyBan(e);
            }
            else
            {
                if (!_meta.IsCopyFav(e) && _meta.CopyFavCount >= GameConstants.CopybookFavMax)
                {
                    AudioBus.Deny();
                    InkToast.Show(_layer, $"最多偏爱 {GameConstants.CopybookFavMax} 张，先点已偏爱的取消");
                    return;
                }
                AudioBus.Tap();
                _meta.ToggleCopyFav(e);
            }
            Refresh();
            _changed?.Invoke();
        }

        void OnClear()
        {
            if (_meta.CopyTotalCount == 0)
            {
                AudioBus.Deny();
                return;
            }
            AudioBus.Tap();
            _meta.ClearCopybook();
            Refresh();
            _changed?.Invoke();
        }
    }
}
