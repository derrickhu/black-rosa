using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 字卡页左上的印章背包。同类型同品质叠成一格，按类型筛；镶嵌在字卡详情里点插槽做，这里只看和分解。
    // 分解模式下点一格多选一枚，再点到头归零；底部算好能换多少金箔。
    public sealed class SealBagPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 1040f;
        const float ChipY = 150f;
        const float ListTop = 186f;
        const float ListBottom = 150f;
        const int Cols = 4;
        const float CellW = 136f;
        const float CellH = 178f;
        const float StepX = 146f;
        const float StepY = 190f;

        MetaProgress _meta;
        RectTransform _layer;
        Action _changed;
        RectTransform _board;
        RectTransform _content;
        RectTransform _foot;
        Text _sum;
        SealType _filter;
        bool _scrap;
        readonly Dictionary<int, int> _picks = new Dictionary<int, int>();
        readonly List<Button> _chips = new List<Button>();

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "seal_bag";
            var p = dim.gameObject.AddComponent<SealBagPanel>();
            p._meta = meta;
            p._layer = layer;
            p._changed = changed;
            meta.MarkSealsSeen();
            p.Build(dim);
        }

        void Close()
        {
            AudioBus.Tap();
            _changed?.Invoke();
            Destroy(gameObject);
        }

        void Build(RectTransform dim)
        {
            _board = PanelKit.Board(dim, "印章", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);
            _sum = UiKit.Label(_board, "sum", "", 21, new Vector2(0f, 104f), new Vector2(580f, 30f),
                TextAnchor.MiddleCenter, Pin.Top);
            _sum.color = InkTheme.TextDim;

            int n = SealCatalog.Types.Length + 1;
            float step = (BoardW - 40f) / n;
            for (int i = 0; i < n; i++)
            {
                SealType t = i == 0 ? SealType.None : SealCatalog.Types[i - 1];
                string text = i == 0 ? "全部" : SealCatalog.TypeName(t);
                var size = new Vector2(step - 6f, 46f);
                var root = UiKit.Stroke(_board, "f" + i, new Vector2((i - (n - 1) * 0.5f) * step, ChipY), size, Pin.Top, 3f,
                    radius: size.y * 0.5f);
                var b = root.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.targetGraphic = root.GetComponent<Image>();
                b.onClick.AddListener(() => Pick(t));
                UiKit.Bold(UiKit.Label(root, "t", text, 20, Vector2.zero, size));
                _chips.Add(b);
            }

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(_board, false);
            var view = vp.GetComponent<RectTransform>();
            view.anchorMin = new Vector2(0f, 0f);
            view.anchorMax = new Vector2(1f, 1f);
            view.offsetMin = new Vector2(20f, ListBottom);
            view.offsetMax = new Vector2(-20f, -ListTop);
            vp.GetComponent<Image>().color = Color.clear;

            var rows = new GameObject("rows", typeof(RectTransform));
            rows.transform.SetParent(view, false);
            _content = rows.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            var list = vp.GetComponent<ScrollRect>();
            list.content = _content;
            list.viewport = view;
            list.horizontal = false;
            list.vertical = true;
            list.movementType = ScrollRect.MovementType.Clamped;
            list.scrollSensitivity = 28f;

            Paint();
        }

        void Pick(SealType t)
        {
            if (_filter == t) return;
            AudioBus.Tap();
            _filter = t;
            Paint();
        }

        void Paint()
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                SealType t = i == 0 ? SealType.None : SealCatalog.Types[i - 1];
                bool on = t == _filter;
                _chips[i].GetComponent<Image>().color = on ? InkTheme.Seal : InkTheme.CardFace;
                _chips[i].GetComponentInChildren<Text>().color = on ? InkTheme.CardFace : InkTheme.TextDark;
            }

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject old = _content.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }

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
            int rows = (shown + Cols - 1) / Cols;
            _content.sizeDelta = new Vector2(0f, Mathf.Max(1, rows) * StepY + 16f);
            if (shown == 0)
            {
                string tip = _meta.SealTotal == 0
                    ? "还没有印章\n首通第四章以后的关卡、精英首通\n开银、金、皇家宝箱会掉"
                    : "这一类还没有";
                var e = UiKit.Label(_content, "empty", tip, 22, new Vector2(0f, 180f), new Vector2(520f, 140f),
                    TextAnchor.MiddleCenter, Pin.Top);
                e.color = InkTheme.TextDim;
            }

            _sum.text = _scrap
                ? "点一格选一枚，再点继续加；选满再点清零"
                : "共 " + _meta.SealTotal + " 枚 · 镶嵌请到字卡详情点插槽";
            BuildFoot();
        }

        void Cell(SealType t, SealQuality q, int i)
        {
            int idx = SealCatalog.BagIndex(t, q);
            int have = _meta.BagCount(t, q);
            _picks.TryGetValue(idx, out int pick);
            bool sel = _scrap && pick > 0;
            Vector2 at = UiKit.GridPos(i, Cols, StepX, StepY) + new Vector2(0f, 8f);
            var cell = UiKit.Stroke(_content, "s" + idx, at, new Vector2(CellW, CellH), Pin.Top, sel ? 5f : 3f,
                sel ? InkTheme.Seal : SealCatalog.QualityDeep(q), InkTheme.CardFace, 22f);
            SealArt.Medal(cell, "m", t, q, new Vector2(0f, 26f), 88f);
            var name = UiKit.Label(cell, "n", SealCatalog.TypeName(t) + " · " + SealCatalog.QualityName(q), 21,
                new Vector2(0f, -40f), new Vector2(CellW, 28f));
            name.color = SealCatalog.QualityDeep(q);
            UiKit.Bold(name);
            var c = UiKit.Label(cell, "c", sel ? pick + " / " + have : "×" + have, 20, new Vector2(0f, -68f),
                new Vector2(CellW, 26f));
            c.color = sel ? InkTheme.Seal : InkTheme.TextDim;
            if (sel) UiKit.Bold(c);

            var btn = cell.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = cell.GetComponent<Image>();
            btn.onClick.AddListener(() => Tap(t, q));
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

        void BuildFoot()
        {
            if (_foot != null)
            {
                _foot.gameObject.SetActive(false);
                Destroy(_foot.gameObject);
            }
            _foot = UiKit.Panel(_board, "foot", new Vector2(0f, ListBottom * 0.5f - BoardH * 0.5f + 6f),
                new Vector2(BoardW, ListBottom), Color.clear);
            _foot.GetComponent<Image>().raycastTarget = false;

            if (!_scrap)
            {
                var go = UiKit.Btn(_foot, "scrap", "分解印章", new Vector2(0f, 0f), new Vector2(300f, 84f), EnterScrap, false);
                PanelKit.Dim(go, _meta.SealTotal == 0, false);
                return;
            }

            int n = PickCount(out int foil);
            var info = UiKit.Label(_foot, "info", n == 0 ? "还没选" : "已选 " + n + " 枚 · 得 " + foil + " 金箔", 22,
                new Vector2(0f, 44f), new Vector2(560f, 30f));
            info.color = n == 0 ? InkTheme.TextDim : InkTheme.TextDark;
            UiKit.Bold(info);
            UiKit.Btn(_foot, "cancel", "取消", new Vector2(-196f, -14f), new Vector2(160f, 78f), ExitScrap, false);
            UiKit.Btn(_foot, "green", "选全部绿", new Vector2(-12f, -14f), new Vector2(188f, 78f), PickGreen, false);
            var ok = UiKit.Btn(_foot, "ok", "分解", new Vector2(190f, -14f), new Vector2(170f, 78f), DoScrap, true);
            PanelKit.Dim(ok, n == 0);
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
            Vector2 at = RewardFly.Local(_layer, _foot);
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
            RewardFly.Piece foil = RewardFly.Foil(_layer, got);
            RectTransform chip = RewardFly.Chip(_layer, "foil", "foil");
            if (chip != null) foil.Land = chip;
            RewardFly.Play(_layer, at, new[] { foil });
        }
    }
}
