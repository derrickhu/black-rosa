using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 首页：常驻顶栏 + 三段底栏（炮台 / 出征 / 技能）。
    // 有 Prefabs/Home 就只绑定；没有才走下面那套临时代码搭壳。
    public sealed class HomeScreen
    {
        const int TabForge = 0;
        const int TabSortie = 1;
        const int TabSpell = 2;

        const float CardW = 322f;
        const float CardH = 152f;
        const float ColStep = 338f;
        const float CardGap = 12f;
        const float SpellRodW = 640f;
        const float SpellSeal = 52f;
        const float SpellTagW = 190f;
        const float SpellCardW = 322f;

        readonly RectTransform _layer;
        readonly MetaProgress _meta;
        readonly Action<int> _start;
        readonly RectTransform[] _pages = new RectTransform[3];
        HomeView _view;
        Button[] _tabs;
        Text _ink;
        Text _stamina;
        Text _stamTip;
        int _tab = TabSortie;
        int _skinTip = -1;

        HomeScreen(RectTransform layer, MetaProgress meta, Action<int> start)
        {
            _layer = layer;
            _meta = meta;
            _start = start;
        }

        public static HomeScreen Build(RectTransform layer, MetaProgress meta, Action<int> start, Action reload)
        {
            var h = new HomeScreen(layer, meta, start);
            UiKit.PaperSheet(layer);
            if (!h.TryPrefab()) h.BuildShell();
            h.FitFrame();
            h.Pick(TabSortie);
            h.RefreshTop();
            if (WxBridge.IsSimulator)
                GmBar.Attach(layer, meta, reload, h.RefreshAfterGm);
            return h;
        }

        void RefreshAfterGm()
        {
            RefreshTop();
            Rebuild(_tab);
        }

        bool TryPrefab()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Home");
            if (prefab == null) return false;
            var go = UnityEngine.Object.Instantiate(prefab, _layer, false);
            go.name = "Home";
            _view = go.GetComponent<HomeView>();
            if (_view == null || _view.Skins == null || _view.Skins.Length == 0)
            {
                UnityEngine.Object.Destroy(go);
                _view = null;
                return false;
            }
            UiKit.ApplyTo(go.transform);
            _stamina = _view.Stamina;
            _ink = _view.Ink;
            _stamTip = _view.StamTip;
            _tabs = _view.Tabs;
            _pages[0] = _view.ForgePage != null ? _view.ForgePage.GetComponent<RectTransform>() : null;
            _pages[1] = _view.SortiePage != null ? _view.SortiePage.GetComponent<RectTransform>() : null;
            _pages[2] = _view.SpellPage != null ? _view.SpellPage.GetComponent<RectTransform>() : null;
            if (_tabs != null)
            {
                for (int i = 0; i < _tabs.Length; i++)
                {
                    if (_tabs[i] == null) continue;
                    int idx = i;
                    _tabs[i].onClick.RemoveAllListeners();
                    _tabs[i].onClick.AddListener(() => Pick(idx));
                }
            }
            if (_view.GoButton != null)
            {
                _view.GoButton.onClick.RemoveAllListeners();
                _view.GoButton.onClick.AddListener(() =>
                {
                    int next = NextStage();
                    if (_meta.CanEnter(next)) _start(next);
                });
            }
            if (_view.AdButton != null)
            {
                _view.AdButton.onClick.RemoveAllListeners();
                _view.AdButton.onClick.AddListener(WatchStaminaAd);
            }
            Paint(_view.TabDock, "Ui/tab_dock", new Vector4(88f, 70f, 88f, 70f));
            Paint(_view.Board, "Ui/panel_board");
            return true;
        }

        void BuildShell()
        {
            float top = TopBarY;
            var size = new Vector2(200f, ChipH);
            _stamina = UiKit.Chip(_layer, "cs", InkSprites.Ui("stamina"), "",
                new Vector2(-118f, top), size);
            _ink = UiKit.Chip(_layer, "ci", InkSprites.Ui("ink"), "",
                new Vector2(118f, top), size);
            _stamTip = UiKit.Label(_layer, "stamtip", "", 16, new Vector2(-118f, top + ChipH + 2f),
                new Vector2(200, 22), TextAnchor.MiddleCenter, Pin.Top);
            _stamTip.color = InkTheme.TextMid;

            for (int i = 0; i < _pages.Length; i++) _pages[i] = Page("page" + i);
            _tabs = UiKit.TabBar(_layer, new[] { "炮台", "出征", "技能" },
                new[] { InkSprites.Ui("tab_forge"), InkSprites.Ui("tab_sortie"), InkSprites.Ui("tab_spell") },
                Pick);
        }

        static void CardIcon(Transform parent, Sprite icon, Vector2 pos, float size, bool live)
        {
            if (icon == null) return;
            UiKit.Icon(parent, icon, pos, size).color =
                live ? Color.white : new Color(1f, 1f, 1f, 0.50f);
        }

        static void PaintCannon(Transform parent, Vector2 pos, int skin, bool owned, float size)
        {
            UiKit.Icon(parent, SkinGun(skin, owned), pos, size);
        }

        static Sprite SkinGun(int skin, bool owned)
        {
            string key;
            switch (skin)
            {
                case 1: key = "skin_cinnabar"; break;
                case 2: key = "skin_celadon"; break;
                case 3: key = "skin_gilt"; break;
                default: key = "skin_plain"; break;
            }
            Sprite spr = InkSprites.Ui(key);
            return spr != null ? spr : InkSprites.Ui(owned ? "skin_plain" : "skin_ghost");
        }

        RectTransform Page(string name)
        {
            var rt = UiKit.Panel(_layer, name, Vector2.zero, Vector2.zero, Color.clear);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, ScreenFit.BottomPad + UiKit.TabBarH);
            rt.offsetMax = new Vector2(0f, -PageTop);
            rt.GetComponent<Image>().raycastTarget = false;
            rt.gameObject.SetActive(false);
            return rt;
        }

        const float ChipH = 54f;
        const float ChromeGap = 6f;

        // 货币条只让开胶囊。纯刘海的安全区让一半就够 —— 整段让完，
        // 顶上会空出一条比货币条还高的白带，三页看着都像没排满。
        static float TopBarY => ScreenFit.CapsuleGuard > 0f
            ? ScreenFit.CapsuleGuard
            : Mathf.Clamp(ScreenFit.TopPad * 0.5f, 6f, 52f);

        static float PageTop => TopBarY + ChipH + ChromeGap;

        void Pick(int tab)
        {
            _tab = tab;
            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i] != null) _pages[i].gameObject.SetActive(i == tab);
            UiKit.PaintTab(_tabs, tab);
            Rebuild(tab);
        }

        void Rebuild(int tab)
        {
            if (_view != null)
            {
                if (tab == TabForge) BindForge();
                else if (tab == TabSortie) BindSortie();
                else BindSpells();
                RefreshTop();
                return;
            }
            RectTransform page = _pages[tab];
            for (int i = page.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(page.GetChild(i).gameObject);
            if (tab == TabForge) BuildForge(page);
            else if (tab == TabSortie) BuildSortie(page);
            else BuildSpells(page);
            RefreshTop();
        }

        float _fitW = -1f;
        float _fitH = -1f;
        float _fitTop = -1f;
        float _fitBot = -1f;

        // 预制体是按 720×1280 排的。画布一拉高，页会跟着变高，
        // 顶上的牌子还钉在屏幕最上，多出来的高度就空在格子和底栏之间。
        // 整页保持设计比例，能放下就按原大贴着安全区下沿，放不下再整体缩小。
        void FitFrame()
        {
            if (_view == null) return;
            var rt = _view.transform as RectTransform;
            if (rt == null) return;
            var parent = rt.parent as RectTransform;
            float cw = parent != null ? parent.rect.width : 0f;
            float ch = parent != null ? parent.rect.height : 0f;
            if (cw < 2f || ch < 2f)
            {
                cw = ScreenFit.CanvasW;
                ch = ScreenFit.CanvasH;
            }
            if (cw < 2f || ch < 2f) return;

            float top = 0f;
            float bot = 0f;
            if (WxBridge.IsMiniGame)
            {
                top = ScreenFit.TopPad;
                bot = ScreenFit.BottomPad;
            }
            if (Mathf.Approximately(cw, _fitW) && Mathf.Approximately(ch, _fitH)
                && Mathf.Approximately(top, _fitTop) && Mathf.Approximately(bot, _fitBot))
                return;

            float availH = Mathf.Max(1f, ch - top - bot);
            float scale = Mathf.Min(cw / ScreenFit.DesignW, availH / ScreenFit.DesignH);
            if (scale < 0.01f) return;
            _fitW = cw;
            _fitH = ch;
            _fitTop = top;
            _fitBot = bot;

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ScreenFit.DesignW, ScreenFit.DesignH);
            rt.localScale = new Vector3(scale, scale, 1f);
            float half = ScreenFit.DesignH * scale * 0.5f;
            bool laidOut = parent != null && parent.rect.height > 2f;
            Rect prect = laidOut ? parent.rect : new Rect(-cw * 0.5f, -ch * 0.5f, cw, ch);
            float centerY = (prect.yMin + prect.yMax) * 0.5f;
            float pivotY = prect.yMin + bot + half;
            rt.anchoredPosition = new Vector2(0f, pivotY - centerY);
            Canvas.ForceUpdateCanvases();
        }

        public void Tick()
        {
            FitFrame();
            int before = _meta.Stamina;
            _meta.Refresh();
            RefreshTop();
            if (_meta.Stamina != before && _tab == TabSortie) Rebuild(TabSortie);
        }

        int _shownSec = -1;

        void RefreshTop()
        {
            if (_stamina == null) return;
            _stamina.text = _meta.Stamina + "/" + GameConstants.StaminaMax;
            _ink.text = _meta.Ink.ToString();
            int sec = _meta.SecondsToNextStamina();
            if (sec == _shownSec) return;
            _shownSec = sec;
            if (_stamTip != null)
            {
                bool full = sec <= 0;
                _stamTip.gameObject.SetActive(!full);
                if (!full) _stamTip.text = $"{sec / 60:00}:{sec % 60:00} 后 +1";
            }
        }

        void WatchStaminaAd()
        {
            AdStub.Reward("stamina", () =>
            {
                _meta.GrantAdStamina();
                Rebuild(_tab);
            });
        }

        static void Dim(RectTransform box, bool live)
        {
            if (live) return;
            box.GetComponent<Image>().color = InkTheme.CardDim;
            Transform ln = box.Find("ln");
            if (ln != null) ln.GetComponent<Image>().color = InkTheme.LineDim;
        }

        // 买得起是铜钱那种黄签，买不起 / 满级 / 锁定是灰签。
        // 原型里这两种都是签，不是「有签」和「一行灰字」。
        void PricePill(Transform parent, Vector2 pos, string text, bool live)
        {
            var pill = UiKit.Art(parent, "pp", live ? "Ui/panel_price" : "Ui/panel_price_off",
                pos, new Vector2(146f, 44f), Pin.Center, PriceSlice);
            pill.GetComponent<Image>().raycastTarget = false;
            bool ink = live && text.EndsWith(" 墨");
            string label = ink ? text.Substring(0, text.Length - 2).Trim() : text;
            Sprite coin = ink ? InkSprites.Ui("ink") : null;
            const float icon = 28f;
            float w = Mathf.Max(24f, label.Length * 12f);
            float total = coin != null ? w + 4f + icon : w;
            float textX = coin != null ? -total * 0.5f + w * 0.5f : 0f;
            if (coin != null)
                UiKit.Icon(pill, coin, new Vector2(-total * 0.5f + w + 4f + icon * 0.5f, 0f), icon);
            var t = UiKit.Label(pill, "t", label, 20, new Vector2(textX, 0f), new Vector2(w + 4f, 34f));
            t.alignment = TextAnchor.MiddleCenter;
            t.color = live ? InkTheme.TextDark : InkTheme.TextMid;
            UiKit.Bold(t);
        }

        static float PageH =>
            ScreenFit.CanvasH - PageTop - (ScreenFit.BottomPad + UiKit.TabBarH);

        static Vector2 Slot(int i, float topY, int count)
        {
            int rows = Mathf.Max(1, (count + 1) / 2);
            float avail = PageH - topY - 20f;
            float gap = Mathf.Clamp((avail - rows * CardH) / Mathf.Max(1, rows - 1), 0f, CardGap);
            float block = rows * CardH + (rows - 1) * gap;
            float slack = Mathf.Max(0f, avail - block);
            return UiKit.GridPos(i, 2, ColStep, CardH + gap)
                   + new Vector2(0f, topY + slack * 0.5f);
        }

        static void Paint(Image img, string key, Vector4 border = default)
        {
            if (img == null) return;
            Sprite spr = border.sqrMagnitude > 0.01f
                ? InkSprites.LoadSliced(key, border)
                : InkSprites.Load(key);
            if (spr == null) return;
            img.sprite = spr;
            img.type = spr.border.sqrMagnitude > 1f || border.sqrMagnitude > 0.01f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            img.preserveAspect = img.type == Image.Type.Simple;
        }

        static void Write(Text t, string s, Color c, bool bold = false)
        {
            if (t == null) return;
            t.font = bold ? UiKit.FontBold : UiKit.Font;
            t.text = s;
            t.color = c;
        }

        static void Fade(Image img, bool live)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = live ? 1f : 0.50f;
            img.color = c;
        }

        // ---------- 预制体绑定 ----------

        void BindForge()
        {
            LayoutForgeTray();
            HideSkinTip();
            if (_view.Skins != null)
                for (int i = 0; i < _view.Skins.Length && i < SkinCatalog.Count; i++)
                    BindSkin(_view.Skins[i], i);
            int stars = _meta.ClearedCount();
            int locked = ForgeCatalog.NextLocked(stars, _meta.Forge);
            if (_view.Boosts == null) return;
            for (int i = 0; i < _view.Boosts.Length; i++)
            {
                var row = _view.Boosts[i];
                if (row == null) continue;
                bool shown = i < ForgeCatalog.LineCount &&
                             ForgeCatalog.Exposed(i, stars, _meta.ForgeLevel(i));
                bool tease = i == locked;
                row.gameObject.SetActive(shown || tease);
                if (shown) BindBoost(row, i);
                else if (tease) BindLocked(row, i, stars);
            }
        }

        void BindSkin(HomeSkinCell slot, int i)
        {
            if (slot == null) return;
            SkinDef d = SkinCatalog.Get(i);
            bool owned = _meta.SkinOwned[i];
            bool on = _meta.Skin == i;
            bool buyable = _meta.CanBuySkin(i, out _);
            bool live = owned || buyable;
            Paint(slot.Card, on ? "Ui/panel_skin_on" : (owned ? "Ui/panel_skin" : "Ui/panel_skin_dim"), SkinSlice);
            if (slot.Gun != null)
            {
                slot.Gun.sprite = SkinGun(i, owned || on);
                slot.Gun.color = owned || on ? Color.white : new Color(1f, 1f, 1f, 0.72f);
            }
            Write(slot.Name, d.Name, owned ? InkTheme.TextDark : InkTheme.TextDim, true);
            Write(slot.Tail,
                owned ? (on ? "使用中" : "") : (buyable ? d.Price + " 墨" : (d.NeedClear ? "通关三章" : $"通关 {d.Gate} 关")),
                on ? InkTheme.Seal : InkTheme.TextDim);
            if (slot.Button != null)
            {
                slot.Button.interactable = live;
                slot.Button.onClick.RemoveAllListeners();
                int idx = i;
                slot.Button.onClick.AddListener(() =>
                {
                    if (_meta.SkinOwned[idx]) _meta.EquipSkin(idx);
                    else _meta.BuySkin(idx);
                    Rebuild(TabForge);
                });
            }
            var card = slot.Card != null ? slot.Card.rectTransform : slot.GetComponent<RectTransform>();
            EnsureSkinMark(card, i);
        }

        void EnsureSkinMark(RectTransform card, int i)
        {
            if (card == null) return;
            var mark = card.Find("mark") as RectTransform;
            if (mark == null)
            {
                var go = new GameObject("mark", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(card, false);
                mark = go.GetComponent<RectTransform>();
                mark.anchorMin = mark.anchorMax = new Vector2(0.5f, 0.5f);
                mark.pivot = new Vector2(0.5f, 0.5f);
                float x = card.sizeDelta.x * 0.5f - 26f;
                float y = card.sizeDelta.y * 0.5f - 24f;
                mark.anchoredPosition = new Vector2(x, y);
                mark.sizeDelta = new Vector2(34f, 34f);
                var img = go.GetComponent<Image>();
                img.sprite = UiSprites.Fill(17);
                img.color = InkTheme.Seal;
                img.raycastTarget = true;
                var ringGo = new GameObject("ring", typeof(RectTransform), typeof(Image));
                ringGo.transform.SetParent(mark, false);
                var ringRt = ringGo.GetComponent<RectTransform>();
                ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
                ringRt.sizeDelta = new Vector2(34f, 34f);
                var ring = ringGo.GetComponent<Image>();
                ring.sprite = UiSprites.Line(14, 3);
                ring.color = InkTheme.TextDark;
                ring.raycastTarget = false;
                var bang = UiKit.Label(mark, "t", "!", 22, Vector2.zero, new Vector2(34f, 34f));
                bang.color = Color.white;
                UiKit.Bold(bang);
                var btn = go.GetComponent<Button>();
                btn.targetGraphic = img;
                btn.transition = Selectable.Transition.None;
            }
            mark.SetAsLastSibling();
            var button = mark.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            int idx = i;
            button.onClick.AddListener(() => ToggleSkinTip(idx, card));
        }

        void HideSkinTip()
        {
            _skinTip = -1;
            if (_view == null || _view.Board == null) return;
            var tip = _view.Board.rectTransform.Find("skinTip");
            if (tip != null) tip.gameObject.SetActive(false);
        }

        void ToggleSkinTip(int i, RectTransform card)
        {
            var board = card.parent as RectTransform;
            if (board == null) return;
            var tip = board.Find("skinTip") as RectTransform;
            if (_skinTip == i && tip != null && tip.gameObject.activeSelf)
            {
                tip.gameObject.SetActive(false);
                _skinTip = -1;
                return;
            }
            _skinTip = i;
            if (tip == null)
            {
                tip = UiKit.Art(board, "skinTip", "Ui/panel_card", Vector2.zero,
                    new Vector2(208f, 84f), Pin.Center, CardSlice);
                var plate = tip.GetComponent<Image>();
                var closer = tip.gameObject.AddComponent<Button>();
                closer.transition = Selectable.Transition.None;
                closer.targetGraphic = plate;
                closer.onClick.AddListener(() =>
                {
                    tip.gameObject.SetActive(false);
                    _skinTip = -1;
                });
                var note = UiKit.Label(tip, "note", "", 15, new Vector2(0f, 14f), new Vector2(188f, 40f));
                note.horizontalOverflow = HorizontalWrapMode.Wrap;
                note.color = InkTheme.TextDark;
                var perk = UiKit.Label(tip, "perk", "", 16, new Vector2(0f, -22f), new Vector2(188f, 28f));
                UiKit.Bold(perk);
            }
            tip.gameObject.SetActive(true);
            tip.SetAsLastSibling();
            tip.anchoredPosition = card.anchoredPosition + new Vector2(0f, -18f);
            SkinDef d = SkinCatalog.Get(i);
            var noteT = tip.Find("note").GetComponent<Text>();
            var perkT = tip.Find("perk").GetComponent<Text>();
            noteT.text = d.Note;
            perkT.text = d.Perk;
            perkT.color = d.DamageAdd > 0.01f || d.GoldAdd > 0 ? InkTheme.Seal : InkTheme.TextDim;
        }

        void BindBoost(HomeBoostRow slot, int line)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            int lv = _meta.ForgeLevel(line);
            bool max = lv >= d.MaxLevel;
            bool buyable = _meta.CanBuyForge(line, out string why);
            Paint(slot.Row, "Ui/panel_row", PillSlice);
            if (slot.Row != null) slot.Row.color = Color.white;
            if (slot.Icon != null)
            {
                slot.Icon.sprite = InkSprites.Ui(d.Icon);
                Fade(slot.Icon, buyable || max);
            }
            Write(slot.Title, d.Name + "  Lv." + lv, (buyable || max) ? InkTheme.TextDark : InkTheme.TextDim, true);
            Write(slot.Step, d.Step, InkTheme.TextMid);
            string tail = max ? "已满级" : (buyable ? ForgeCatalog.Cost(line, lv) + " 墨" : why);
            FitPrice(slot, tail, buyable && !max);
            if (slot.Button != null)
            {
                slot.Button.interactable = buyable;
                slot.Button.onClick.RemoveAllListeners();
                int idx = line;
                slot.Button.onClick.AddListener(() =>
                {
                    if (_meta.BuyForge(idx)) Rebuild(TabForge);
                });
            }
        }

        void BindLocked(HomeBoostRow slot, int line, int stars)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            Paint(slot.Row, "Ui/panel_row_lock", PillSlice);
            if (slot.Icon != null)
            {
                slot.Icon.sprite = InkSprites.Ui("lock");
                Fade(slot.Icon, false);
            }
            Write(slot.Title, d.Name, InkTheme.TextDim, true);
            Write(slot.Step, d.Step, InkTheme.TextDim);
            int need = Mathf.Max(0, d.Reveal - stars);
            FitPrice(slot, need > 0 ? $"通关 {d.Reveal} 关" : d.LockNote, false);
            if (slot.Button != null)
            {
                slot.Button.interactable = false;
                slot.Button.onClick.RemoveAllListeners();
            }
        }

        int _chapter = -1;

        void BindSortie()
        {
            if (_pages[1] != null) SortiePageBuilder.Ensure(_view, _pages[1]);
            int next = NextStage();
            bool canGo = _meta.CanEnter(next);
            if (_view.GoLabel != null)
                _view.GoLabel.text = (_meta.Stars[next] > 0 ? "重打" : "继续") + $"  第 {next + 1} 关";
            if (_view.GoButton != null) _view.GoButton.interactable = canGo;
            bool poor = _meta.Stamina < _meta.StageCost(next);
            bool ad = poor && _meta.CanAdStamina;
            if (_view.AdButton != null)
            {
                _view.AdButton.gameObject.SetActive(ad);
                if (_view.AdLabel != null)
                    _view.AdLabel.text = $"看广告  +{GameConstants.AdStaminaGain} 体力";
            }
            if (_view.Help != null) _view.Help.gameObject.SetActive(false);
            if (_view.Chapter != null) BindChapter(next);
            else if (_view.Seals != null)
                for (int i = 0; i < _view.Seals.Length && i < GameConstants.StageCount; i++)
                    BindSeal(_view.Seals[i], i);
            WireSides();
        }

        void BindChapter(int frontier)
        {
            HomeChapterBoard board = _view.Chapter;
            if (_chapter < 0) _chapter = SortiePageBuilder.ChapterOf(frontier);
            _chapter = Mathf.Clamp(_chapter, 0, SortiePageBuilder.ChapterCount - 1);
            if (board.Art != null)
            {
                Sprite art = InkSprites.Load("Ui/chapter_" + (_chapter + 1));
                if (art == null) art = InkSprites.Load("Ui/chapter_1");
                if (art != null) board.Art.sprite = art;
            }
            if (board.Title != null) board.Title.text = SortiePageBuilder.ChapterTitle(_chapter);
            int count = Mathf.Min(SortiePageBuilder.PerChapter,
                GameConstants.StageCount - _chapter * SortiePageBuilder.PerChapter);
            if (board.Route != null)
            {
                board.Route.color = InkTheme.Outline;
                board.Route.SetPoints(SortiePageBuilder.RoutePoints(count));
            }
            if (board.Nodes != null)
            {
                for (int i = 0; i < board.Nodes.Length; i++)
                {
                    HomeSealCell slot = board.Nodes[i];
                    if (slot == null) continue;
                    bool show = i < count;
                    slot.gameObject.SetActive(show);
                    if (show) BindNode(slot, _chapter * SortiePageBuilder.PerChapter + i, frontier);
                }
            }
            if (board.Dots != null)
            {
                for (int i = 0; i < board.Dots.Length; i++)
                {
                    if (board.Dots[i] == null) continue;
                    if (board.Dots[i].sprite == null)
                    {
                        board.Dots[i].sprite = UiSprites.Fill(8);
                        board.Dots[i].type = Image.Type.Sliced;
                    }
                    board.Dots[i].color = i == _chapter ? InkTheme.Cta : InkTheme.LineDim;
                }
            }
            bool prevOk = _chapter > 0;
            bool nextOk = _chapter + 1 < SortiePageBuilder.ChapterCount && ChapterOpen(_chapter + 1);
            if (board.Prev != null) board.Prev.gameObject.SetActive(prevOk);
            if (board.PrevLabel != null) board.PrevLabel.color = InkTheme.TextMid;
            if (board.NextLabel != null)
                board.NextLabel.color = nextOk ? InkTheme.TextDark : InkTheme.TextDim;
            var swipe = board.GetComponent<ChapterSwipe>();
            if (swipe != null) swipe.Moved = ShiftChapter;
            if (board.Prev != null)
            {
                board.Prev.onClick.RemoveAllListeners();
                board.Prev.onClick.AddListener(() => ShiftChapter(-1));
            }
            if (board.Next != null)
            {
                board.Next.onClick.RemoveAllListeners();
                board.Next.onClick.AddListener(() => ShiftChapter(1));
            }
        }

        void ShiftChapter(int dir)
        {
            int to = _chapter + dir;
            if (to < 0 || to >= SortiePageBuilder.ChapterCount) return;
            if (dir > 0 && !ChapterOpen(to))
            {
                AudioBus.Deny();
                return;
            }
            AudioBus.Tap();
            _chapter = to;
            BindChapter(NextStage());
        }

        bool ChapterOpen(int chapter)
        {
            if (chapter <= 0) return true;
            int first = chapter * SortiePageBuilder.PerChapter;
            return first < GameConstants.StageCount && _meta.Unlocked(first);
        }

        void BindNode(HomeSealCell slot, int index, int frontier)
        {
            bool open = _meta.Unlocked(index);
            bool cleared = open && _meta.Stars[index] > 0;
            bool current = open && index == frontier && !cleared;
            bool last = (index % SortiePageBuilder.PerChapter) == SortiePageBuilder.PerChapter - 1
                || index == GameConstants.StageCount - 1;
            string key = "node_lock";
            if (cleared) key = "node_done";
            else if (current) key = "node_now";
            else if (last) key = "node_boss";
            Sprite face = InkSprites.Ui(key);
            if (slot.Plate != null && face != null) slot.Plate.sprite = face;
            if (slot.Lock != null) slot.Lock.gameObject.SetActive(false);
            if (slot.Stars != null)
                for (int s = 0; s < slot.Stars.Length; s++)
                    if (slot.Stars[s] != null) slot.Stars[s].gameObject.SetActive(false);
            if (slot.Number != null)
            {
                slot.Number.gameObject.SetActive(current);
                slot.Number.text = (index + 1).ToString();
                slot.Number.color = InkTheme.CardFace;
            }
            if (slot.Tag != null) slot.Tag.text = "";
            int cost = _meta.StageCost(index);
            bool live = open && _meta.Stamina >= cost;
            if (slot.Button != null)
            {
                slot.Button.interactable = live;
                slot.Button.onClick.RemoveAllListeners();
                int idx = index;
                slot.Button.onClick.AddListener(() =>
                {
                    if (_meta.Unlocked(idx) && _meta.Stamina >= _meta.StageCost(idx)) _start(idx);
                });
            }
        }

        void WireSides()
        {
            if (_view.SideActs == null) return;
            for (int i = 0; i < _view.SideActs.Length; i++)
            {
                Button btn = _view.SideActs[i];
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => AudioBus.Tap());
            }
        }

        void BindSeal(HomeSealCell slot, int index)
        {
            if (slot == null) return;
            bool open = _meta.Unlocked(index);
            int cost = _meta.StageCost(index);
            bool afford = _meta.Stamina >= cost;
            bool live = open && afford;
            Paint(slot.Plate, live ? "Ui/panel_card_on" : (open ? "Ui/panel_card" : "Ui/panel_card_dim"), CardSlice);
            if (slot.Lock != null) slot.Lock.gameObject.SetActive(!open);
            if (slot.Number != null)
            {
                slot.Number.gameObject.SetActive(open);
                slot.Number.text = (index + 1).ToString();
                slot.Number.color = live ? InkTheme.TextDark : InkTheme.TextDim;
            }
            if (slot.Stars != null)
                for (int s = 0; s < slot.Stars.Length; s++)
                    if (slot.Stars[s] != null) slot.Stars[s].gameObject.SetActive(false);
            if (slot.Tag != null)
            {
                if (!open || afford) slot.Tag.text = "";
                else
                {
                    slot.Tag.text = "体力 " + cost;
                    slot.Tag.color = InkTheme.Rose;
                }
            }
            if (slot.Button != null)
            {
                slot.Button.interactable = live;
                slot.Button.onClick.RemoveAllListeners();
                int idx = index;
                slot.Button.onClick.AddListener(() => { if (live) _start(idx); });
            }
        }

        void BindSpells()
        {
            EnsureSpellSeal();
            EnsureSpellRoster();
            if (_view.SpellTip != null) _view.SpellTip.gameObject.SetActive(false);
            if (_view.Equipped != null)
                for (int s = 0; s < _view.Equipped.Length; s++)
                    BindSpellSlot(_view.Equipped[s], s);
            if (_view.Spells != null)
                for (int i = 0; i < _view.Spells.Length && i < SpellCatalog.Count; i++)
                    BindSpellCard(_view.Spells[i], i);
        }

        // 预制体里的格子是烘出来的固定张数。技能变多时按第一张复制，并让整列可以往下翻。
        void EnsureSpellRoster()
        {
            if (_view.Spells == null || _view.Spells.Length == 0 || _view.Spells[0] == null) return;
            var sample = _view.Spells[0];
            var grid = sample.transform.parent as RectTransform;
            if (grid == null) return;
            var list = new System.Collections.Generic.List<HomeSpellCard>();
            for (int i = 0; i < _view.Spells.Length; i++)
                if (_view.Spells[i] != null) list.Add(_view.Spells[i]);
            while (list.Count < SpellCatalog.Count)
            {
                var go = UnityEngine.Object.Instantiate(sample.gameObject, grid);
                go.name = "sp" + list.Count;
                list.Add(go.GetComponent<HomeSpellCard>());
            }
            _view.Spells = list.ToArray();
            EnsureSpellScroll(grid);
        }

        void EnsureSpellScroll(RectTransform grid)
        {
            RectTransform viewport;
            if (grid.parent != null && grid.parent.name == "spellView")
            {
                viewport = grid.parent as RectTransform;
                if (viewport.anchorMax.x - viewport.anchorMin.x < 0.9f)
                {
                    float top = Mathf.Max(160f, -viewport.offsetMax.y);
                    viewport.anchorMin = Vector2.zero;
                    viewport.anchorMax = Vector2.one;
                    viewport.offsetMin = new Vector2(4f, 52f);
                    viewport.offsetMax = new Vector2(-4f, -top);
                }
            }
            else
            {
                var page = grid.parent as RectTransform;
                if (page == null) return;
                float topInset = 330f;
                if (grid.anchorMax.y - grid.anchorMin.y > 0.5f)
                    topInset = Mathf.Max(160f, -grid.offsetMax.y);
                var view = new GameObject("spellView", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewport = view.GetComponent<RectTransform>();
                viewport.SetParent(page, false);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                // 底栏卷轴会盖住页脚，列表在它上面停住，整张卡才不会被切掉。
                viewport.offsetMin = new Vector2(4f, 52f);
                viewport.offsetMax = new Vector2(-4f, -topInset);
                var img = view.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
                grid.SetParent(viewport, false);
                var scroll = view.AddComponent<ScrollRect>();
                scroll.content = grid;
                scroll.viewport = viewport;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 40f;
                scroll.inertia = true;
            }
            FitSpellGrid(grid, viewport);
        }

        // 两列卡片本身就要 660 宽。视口若比这窄，格子会从左右被切掉。
        static void FitSpellGrid(RectTransform grid, RectTransform viewport)
        {
            if (grid == null || viewport == null) return;
            var layout = grid.GetComponent<GridLayoutGroup>();
            Canvas.ForceUpdateCanvases();
            float viewW = viewport.rect.width;
            float viewH = viewport.rect.height;
            if (viewW < 80f) viewW = 700f;
            float gapX = layout != null ? layout.spacing.x : 16f;
            float cellW = 322f;
            float cellH = 176f;
            if (cellW * 2f + gapX > viewW - 8f)
                cellW = Mathf.Floor((viewW - gapX - 8f) * 0.5f);
            if (layout != null)
            {
                layout.spacing = new Vector2(gapX, 10f);
                layout.padding = new RectOffset(0, 0, 4, 24);
                layout.childAlignment = TextAnchor.UpperCenter;
                if (viewH > 120f)
                {
                    float inner = viewH - layout.padding.top - layout.padding.bottom - layout.spacing.y * 2f;
                    cellH = Mathf.Clamp(inner / 3f, 156f, 176f);
                }
                layout.cellSize = new Vector2(cellW, cellH);
            }
            grid.anchorMin = new Vector2(0.5f, 1f);
            grid.anchorMax = new Vector2(0.5f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = Vector2.zero;
            float width = cellW * 2f + gapX;
            grid.sizeDelta = new Vector2(width, 40f);
            var fit = grid.GetComponent<ContentSizeFitter>();
            if (fit == null) fit = grid.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
        }

        void BindSpellSlot(HomeSpellSlot slot, int s)
        {
            if (slot == null) return;
            int id = s < _meta.Equipped.Length ? _meta.Equipped[s] : -1;
            bool has = id >= 0;
            Paint(slot.Card, has ? "Ui/panel_spell_tag_on" : "Ui/panel_spell_tag");
            if (slot.Icon != null) slot.Icon.gameObject.SetActive(has);
            if (!has)
            {
                if (slot.Name != null)
                {
                    slot.Name.text = "空槽";
                    slot.Name.color = InkTheme.TextDim;
                }
                if (slot.Cost != null) slot.Cost.text = "";
                return;
            }
            SpellDef d = SpellCatalog.Get(id);
            int rank = _meta.SpellRank(id);
            if (slot.Icon != null) slot.Icon.sprite = InkSprites.Ui(d.Id);
            if (slot.Name != null)
            {
                slot.Name.text = rank > 0 ? d.Name + " Lv." + rank : d.Name;
                slot.Name.color = InkTheme.TextDark;
            }
            if (slot.Cost != null) slot.Cost.text = "耗 " + d.GoldCost + " 金";
        }

        void BindSpellCard(HomeSpellCard slot, int i)
        {
            if (slot == null) return;
            SpellDef d = SpellCatalog.Get(i);
            int rank = _meta.SpellRank(i);
            bool owned = rank > 0;
            bool maxed = rank >= SpellCatalog.MaxLevel;
            int eq = _meta.EquippedSlot(i);
            int have = _meta.SpellShardCount(i);
            int need = maxed ? 0 : SpellCatalog.NextShards(d, rank);
            bool gathering = !maxed && have < need;
            bool buyable = _meta.CanBuySpell(i, out string why);
            bool ready = !maxed && need > 0 && have >= need;
            Paint(slot.Card, eq >= 0 ? "Ui/panel_skin_on"
                : (rank <= 0 && have <= 0 ? "Ui/panel_skin_dim" : "Ui/panel_skin"), SkinSlice);
            LiftSpellFoot(slot);
            ShowShardBar(slot.Card != null ? slot.Card.transform : slot.transform, have, need, d.Tint, gathering);
            if (slot.Icon != null)
            {
                slot.Icon.sprite = InkSprites.Ui(d.Id);
                Fade(slot.Icon, true);
            }
            if (slot.Name != null)
            {
                slot.Name.text = owned ? d.Name + " Lv." + rank : d.Name;
                slot.Name.color = eq >= 0 ? InkTheme.Seal
                    : (owned || buyable || have > 0 ? InkTheme.TextDark : InkTheme.TextDim);
            }
            if (slot.Desc != null)
            {
                slot.Desc.text = SpellCatalog.Blurb(d, rank);
                slot.Desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                slot.Desc.verticalOverflow = VerticalWrapMode.Overflow;
            }
            if (slot.Cost != null) slot.Cost.text = "耗 " + d.GoldCost + " 金";
            if (slot.State != null) slot.State.raycastTarget = false;
            if (slot.PriceBack != null) slot.PriceBack.gameObject.SetActive(buyable);
            BindUpgradePill(slot, i, owned && buyable);
            if (slot.State != null)
            {
                if (maxed)
                {
                    slot.State.text = "已满级";
                    slot.State.color = InkTheme.TextMid;
                }
                else if (buyable)
                {
                    slot.State.text = SpellCatalog.NextPrice(d, rank) + " 墨";
                    slot.State.color = InkTheme.TextDark;
                }
                else if (ready)
                {
                    slot.State.text = why;
                    slot.State.color = InkTheme.TextDim;
                }
                else if (gathering)
                    slot.State.text = "";
                else if (owned)
                {
                    slot.State.text = eq >= 0 ? "已装备 " + (eq + 1) : "点击装备";
                    slot.State.color = eq >= 0 ? InkTheme.Cta : InkTheme.TextMid;
                }
                else
                {
                    slot.State.text = why;
                    slot.State.color = InkTheme.TextDim;
                }
            }
            if (slot.Button != null)
            {
                slot.Button.interactable = owned || buyable;
                slot.Button.onClick.RemoveAllListeners();
                int idx = i;
                slot.Button.onClick.AddListener(() =>
                {
                    if (_meta.SpellRank(idx) > 0) _meta.Equip(idx);
                    else _meta.BuySpell(idx);
                    Rebuild(TabSpell);
                });
            }
        }

        void BindUpgradePill(HomeSpellCard slot, int i, bool upgrade)
        {
            if (slot.PriceBack == null) return;
            var btn = slot.PriceBack.GetComponent<Button>();
            if (!upgrade)
            {
                slot.PriceBack.raycastTarget = false;
                if (btn != null) btn.onClick.RemoveAllListeners();
                return;
            }
            if (btn == null)
            {
                btn = slot.PriceBack.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = slot.PriceBack;
            }
            slot.PriceBack.raycastTarget = true;
            slot.PriceBack.transform.SetAsLastSibling();
            btn.onClick.RemoveAllListeners();
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                if (_meta.BuySpell(idx)) Rebuild(TabSpell);
            });
        }

        static void ShowShardBar(Transform card, int have, int need, Color tint, bool show)
        {
            if (card == null) return;
            Transform bar = card.Find("shard");
            if (!show)
            {
                if (bar != null) bar.gameObject.SetActive(false);
                return;
            }
            if (bar == null)
            {
                var track = UiKit.Panel(card, "shard", new Vector2(56f, -36f), new Vector2(150f, 18f), InkTheme.Bone);
                var bg = track.GetComponent<Image>();
                bg.sprite = UiSprites.Fill(8);
                bg.type = Image.Type.Sliced;
                bg.raycastTarget = false;
                var fill = UiKit.Panel(track, "fill", new Vector2(2f, 0f), new Vector2(8f, 12f), tint);
                var fr = fill.GetComponent<RectTransform>();
                fr.anchorMin = fr.anchorMax = new Vector2(0f, 0.5f);
                fr.pivot = new Vector2(0f, 0.5f);
                var fi = fill.GetComponent<Image>();
                fi.sprite = UiSprites.Fill(6);
                fi.type = Image.Type.Sliced;
                fi.raycastTarget = false;
                var label = UiKit.Label(track, "n", "", 14, Vector2.zero, new Vector2(146f, 18f));
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
                UiKit.Bold(label);
                bar = track;
            }
            bar.gameObject.SetActive(true);
            var barRt = bar as RectTransform;
            if (barRt != null)
            {
                var bp = barRt.anchoredPosition;
                barRt.anchoredPosition = new Vector2(bp.x, FootY(card));
            }
            float u = need <= 0 ? 0f : Mathf.Clamp01(have / (float)need);
            Transform fillT = bar.Find("fill");
            if (fillT != null)
            {
                var fr = fillT.GetComponent<RectTransform>();
                fr.sizeDelta = new Vector2(Mathf.Max(u > 0.01f ? 10f : 0f, 146f * u), 12f);
                var fi = fillT.GetComponent<Image>();
                if (fi != null) fi.color = tint;
            }
            Transform num = bar.Find("n");
            if (num != null)
            {
                var t = num.GetComponent<Text>();
                t.text = have + "/" + need;
                t.color = InkTheme.TextDark;
            }
        }

        // 瓷面底唇大约 34 像素，底行贴在唇上会压进灰边。按卡片实际高度抬到唇上面。
        static void LiftSpellFoot(HomeSpellCard slot)
        {
            float y = FootY(slot.Card != null ? slot.Card.transform : slot.transform);
            SetY(slot.Cost, y);
            SetY(slot.State, y);
            SetY(slot.PriceBack, y);
        }

        static float FootY(Transform card)
        {
            var rt = card as RectTransform;
            float h = rt != null ? rt.rect.height : 0f;
            if (h < 40f) h = 176f;
            return -h * 0.5f + 52f;
        }

        static void SetY(Component c, float y)
        {
            if (c == null) return;
            var rt = c.transform as RectTransform;
            if (rt == null) return;
            var p = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(p.x, y);
        }

        // ---------- 出征（无预制体时） ----------

        int NextStage()
        {
            int last = 0;
            for (int i = 0; i < GameConstants.StageCount; i++)
            {
                if (!_meta.Unlocked(i)) break;
                last = i;
                if (_meta.Stars[i] <= 0) return i;
            }
            return last;
        }

        void BuildSortie(RectTransform page)
        {
            var host = page.GetComponent<HomeView>();
            if (host == null) host = page.gameObject.AddComponent<HomeView>();
            SortiePageBuilder.Build(page, host);
            int next = NextStage();
            bool canGo = _meta.CanEnter(next);
            var go = UiKit.Btn(page, "go", (_meta.Stars[next] > 0 ? "重打" : "继续") + $"  第 {next + 1} 关",
                new Vector2(0f, 26f), new Vector2(460f, 106f), () => _start(next), true, Pin.Bottom);
            go.interactable = canGo;
            bool poor = _meta.Stamina < _meta.StageCost(next);
            if (poor && _meta.CanAdStamina)
                UiKit.Btn(page, "adstam", $"看广告  +{GameConstants.AdStaminaGain} 体力",
                    new Vector2(0f, 150f), new Vector2(400f, 84f), WatchStaminaAd, false, Pin.Bottom);
        }

        // ---------- 炮台（无预制体时） ----------

        // 棕盘原图 640×415，字牌在图里。方框必须同比例，卡才落在深色盘底里。
        const float BoardW = 640f;
        const float BoardH = 415f;
        const float SkinCardW = 224f;
        const float SkinCardH = 124f;
        const float RowH = 88f;
        const float RowGap = 8f;

        static readonly Vector4 CardSlice = new Vector4(18f, 18f, 18f, 18f);
        // 皮肤卡自带白边、描边和底唇，边要比普通票面宽，拉到 224×124 时唇才不会被拉扁。
        static readonly Vector4 SkinSlice = new Vector4(30f, 34f, 30f, 30f);
        static readonly Vector4 PillSlice = new Vector4(44f, 8f, 44f, 8f);
        static readonly Vector4 PriceSlice = new Vector4(22f, 8f, 22f, 8f);

        void BuildForge(RectTransform page)
        {
            var board = UiKit.Art(page, "board", "Ui/panel_board", new Vector2(0f, 2f),
                new Vector2(BoardW, BoardH), Pin.Top);
            board.GetComponent<Image>().raycastTarget = false;

            var card = new Vector2(SkinCardW, SkinCardH);
            for (int i = 0; i < SkinCatalog.Count; i++)
                SkinCell(board, i, SkinSlot(i), card);

            BuildBoosts(page, 2f + BoardH + 10f);
        }

        static Vector2 SkinSlot(int i)
        {
            return new Vector2(i % 2 == 0 ? -119f : 119f, i / 2 == 0 ? 46f : -86f);
        }

        void LayoutForgeTray()
        {
            if (_view.Board != null)
            {
                var board = _view.Board.rectTransform;
                board.sizeDelta = new Vector2(BoardW, BoardH);
                var tag = board.Find("tag");
                if (tag != null) tag.gameObject.SetActive(false);
            }
            if (_view.GunSummary != null)
                _view.GunSummary.transform.parent.gameObject.SetActive(false);
            if (_view.Skins != null)
            {
                var size = new Vector2(SkinCardW, SkinCardH);
                for (int i = 0; i < _view.Skins.Length && i < SkinCatalog.Count; i++)
                {
                    var slot = _view.Skins[i];
                    if (slot == null) continue;
                    var rt = slot.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = SkinSlot(i);
                    rt.sizeDelta = size;
                    if (slot.Gun != null)
                    {
                        slot.Gun.rectTransform.anchoredPosition = new Vector2(0f, 14f);
                        slot.Gun.rectTransform.sizeDelta = new Vector2(64f, 64f);
                    }
                    if (slot.Name != null)
                    {
                        slot.Name.rectTransform.anchoredPosition = new Vector2(0f, -28f);
                        slot.Name.rectTransform.sizeDelta = new Vector2(200f, 24f);
                        slot.Name.alignment = TextAnchor.MiddleCenter;
                    }
                    if (slot.Tail != null)
                    {
                        slot.Tail.rectTransform.anchoredPosition = new Vector2(0f, -48f);
                        slot.Tail.rectTransform.sizeDelta = new Vector2(200f, 20f);
                        slot.Tail.alignment = TextAnchor.MiddleCenter;
                    }
                }
            }
            if (_view.Boosts != null && _view.Boosts.Length > 0 && _view.Boosts[0] != null)
            {
                var box = _view.Boosts[0].transform.parent as RectTransform;
                if (box != null && box.name == "boosts")
                    box.anchoredPosition = new Vector2(0f, -(8f + BoardH + 12f));
            }
        }

        void FitPrice(HomeBoostRow slot, string tail, bool cta)
        {
            bool ink = cta && tail.EndsWith(" 墨");
            string label = ink ? tail.Substring(0, tail.Length - 2).Trim() : tail;
            if (slot.PriceBack != null)
            {
                slot.PriceBack.gameObject.SetActive(true);
                Paint(slot.PriceBack, cta ? "Ui/panel_price" : "Ui/panel_price_off", PriceSlice);
                var back = slot.PriceBack.rectTransform;
                back.anchorMin = back.anchorMax = new Vector2(0.5f, 0.5f);
                back.pivot = new Vector2(0.5f, 0.5f);
                back.anchoredPosition = new Vector2(200f, 0f);
                back.sizeDelta = new Vector2(156f, 44f);
            }
            if (slot.Price != null)
            {
                var rt = slot.Price.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                slot.Price.alignment = TextAnchor.MiddleCenter;
                slot.Price.horizontalOverflow = HorizontalWrapMode.Overflow;
                slot.Price.verticalOverflow = VerticalWrapMode.Overflow;
            }
            Write(slot.Price, label, cta ? InkTheme.TextDark : InkTheme.TextDim, cta);
            Image coin = PriceInk(slot);
            if (coin != null) coin.gameObject.SetActive(ink && slot.PriceBack != null);
            if (slot.Price == null) return;
            var textRt = slot.Price.rectTransform;
            if (!ink || coin == null || slot.PriceBack == null)
            {
                textRt.anchoredPosition = new Vector2(200f, 0f);
                textRt.sizeDelta = new Vector2(144f, 36f);
                return;
            }
            const float icon = 30f;
            const float gap = 4f;
            float w = slot.Price.preferredWidth;
            if (w < 8f) w = Mathf.Max(18f, slot.Price.fontSize * 0.62f * label.Length);
            float total = w + gap + icon;
            float left = 200f - total * 0.5f;
            textRt.sizeDelta = new Vector2(w + 6f, 36f);
            textRt.anchoredPosition = new Vector2(left + w * 0.5f, 0f);
            var crt = coin.rectTransform;
            crt.SetParent(slot.PriceBack.rectTransform, false);
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(icon, icon);
            crt.anchoredPosition = new Vector2(left + w + gap + icon * 0.5f - 200f, 0f);
            crt.SetAsLastSibling();
        }

        Image PriceInk(HomeBoostRow slot)
        {
            if (slot.PriceBack == null) return null;
            Transform host = slot.PriceBack.transform;
            Transform found = host.Find("ink");
            Image img;
            if (found == null)
            {
                var go = new GameObject("ink", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(host, false);
                img = go.GetComponent<Image>();
                img.raycastTarget = false;
            }
            else img = found.GetComponent<Image>();
            Sprite spr = InkSprites.Ui("ink");
            if (spr != null) img.sprite = spr;
            img.preserveAspect = true;
            img.color = Color.white;
            return img;
        }

        void SkinCell(RectTransform board, int i, Vector2 pos, Vector2 size)
        {
            SkinDef d = SkinCatalog.Get(i);
            bool owned = _meta.SkinOwned[i];
            bool on = _meta.Skin == i;
            bool buyable = _meta.CanBuySkin(i, out string why);
            bool live = owned || buyable;
            string key = on ? "Ui/panel_skin_on" : (owned ? "Ui/panel_skin" : "Ui/panel_skin_dim");
            var box = UiKit.Art(board, "sk" + i, key, pos, size, Pin.Center, SkinSlice);
            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box.GetComponent<Image>();
            btn.interactable = live;
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                if (_meta.SkinOwned[idx]) _meta.EquipSkin(idx);
                else _meta.BuySkin(idx);
                Rebuild(TabForge);
            });
            PaintCannon(box, new Vector2(0f, 10f), i, owned || on, 78f);
            var n = UiKit.Label(box, "n", d.Name, 18, new Vector2(-22f, -46f), new Vector2(80, 28),
                TextAnchor.MiddleRight);
            UiKit.Bold(n);
            n.color = owned ? InkTheme.TextDark : InkTheme.TextDim;
            string tail = owned
                ? (on ? "使用中" : "")
                : (buyable ? d.Price + " 墨" : (d.NeedClear ? "通关三章" : $"通关 {d.Gate} 关"));
            if (tail.Length > 0)
            {
                var s = UiKit.Label(box, "s", tail, 14, new Vector2(40f, -46f), new Vector2(72, 26),
                    TextAnchor.MiddleLeft);
                s.color = on ? InkTheme.Seal : InkTheme.TextDim;
            }
            EnsureSkinMark(box, i);
        }

        void BuildBoosts(RectTransform page, float topY)
        {
            int stars = _meta.ClearedCount();
            var shown = new System.Collections.Generic.List<int>();
            for (int i = 0; i < ForgeCatalog.LineCount; i++)
                if (ForgeCatalog.Exposed(i, stars, _meta.ForgeLevel(i)))
                    shown.Add(i);
            int locked = ForgeCatalog.NextLocked(stars, _meta.Forge);
            int n = shown.Count + (locked >= 0 ? 1 : 0);
            float viewH = Mathf.Max(140f, PageH - topY - 6f);
            float gap = RowGap;
            float contentH = n * RowH + Mathf.Max(0, n - 1) * gap + 8f;
            var view = UiKit.Panel(page, "boosts", new Vector2(0f, topY), new Vector2(720f, viewH), Color.clear, Pin.Top);
            view.GetComponent<Image>().raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            var list = UiKit.Panel(view, "list", Vector2.zero, new Vector2(720f, Mathf.Max(viewH, contentH)), Color.clear, Pin.Top);
            list.GetComponent<Image>().raycastTarget = false;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = list;
            scroll.horizontal = false;
            scroll.vertical = contentH > viewH + 1f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48f;
            for (int i = 0; i < shown.Count; i++)
                ForgeRow(list, shown[i], i * (RowH + gap));
            if (locked >= 0)
                LockedRow(list, locked, stars, shown.Count * (RowH + gap));
        }

        void ForgeRow(RectTransform page, int line, float y)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            int lv = _meta.ForgeLevel(line);
            bool max = lv >= d.MaxLevel;
            bool buyable = _meta.CanBuyForge(line, out string why);
            var box = UiKit.Art(page, "fg" + line, "Ui/panel_row", new Vector2(0f, y),
                new Vector2(668f, RowH), Pin.Top, PillSlice);
            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box.GetComponent<Image>();
            btn.interactable = buyable;
            int idx = line;
            btn.onClick.AddListener(() =>
            {
                if (_meta.BuyForge(idx)) Rebuild(TabForge);
            });
            CardIcon(box, InkSprites.Ui(d.Icon), new Vector2(-262f, 0f), 62f, true);
            var n = UiKit.Label(box, "n", d.Name + " Lv." + lv, 26,
                new Vector2(-50f, 14f), new Vector2(340, 36), TextAnchor.MiddleLeft);
            UiKit.Bold(n);
            n.color = InkTheme.TextDark;
            var step = UiKit.Label(box, "s", d.Step, 18, new Vector2(-50f, -16f), new Vector2(340, 28),
                TextAnchor.MiddleLeft);
            step.color = InkTheme.TextMid;
            string tail = max ? "已满级" : (buyable ? ForgeCatalog.Cost(line, lv) + " 墨" : why);
            PricePill(box, new Vector2(238f, 0f), tail, buyable && !max);
        }

        void LockedRow(RectTransform page, int line, int stars, float y)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            var box = UiKit.Art(page, "lk" + line, "Ui/panel_row_lock", new Vector2(0f, y),
                new Vector2(668f, RowH), Pin.Top, PillSlice);
            box.GetComponent<Image>().raycastTarget = false;
            CardIcon(box, InkSprites.Ui("lock"), new Vector2(-262f, 0f), 56f, false);
            var n = UiKit.Label(box, "n", d.Name, 26, new Vector2(-50f, 14f), new Vector2(340, 36),
                TextAnchor.MiddleLeft);
            UiKit.Bold(n);
            n.color = InkTheme.TextDim;
            var step = UiKit.Label(box, "s", d.Step, 18, new Vector2(-50f, -16f), new Vector2(340, 28),
                TextAnchor.MiddleLeft);
            step.color = InkTheme.TextDim;
            int need = Mathf.Max(0, d.Reveal - stars);
            string tail = need > 0 ? $"通关 {d.Reveal} 关" : d.LockNote;
            PricePill(box, new Vector2(238f, 0f), tail, false);
        }

        // ---------- 技能（无预制体时） ----------

        static Vector2 ArtSize(string key, float width, float fallbackH)
        {
            Sprite spr = InkSprites.Load(key);
            if (spr == null) return new Vector2(width, fallbackH);
            return new Vector2(width, width * spr.rect.height / Mathf.Max(1f, spr.rect.width));
        }

        void BuildSpells(RectTransform page)
        {
            Vector2 rodSize = ArtSize("Ui/panel_spell_rod", SpellRodW, 28f);
            float hangTop = 8f;
            var rod = UiKit.Art(page, "rod", "Ui/panel_spell_rod",
                new Vector2(0f, hangTop + (SpellSeal - rodSize.y) * 0.5f), rodSize, Pin.Top);
            rod.GetComponent<Image>().raycastTarget = false;
            var seal = UiKit.Art(page, "seal", "Ui/panel_spell_seal",
                new Vector2(-SpellRodW * 0.5f + SpellSeal * 0.38f, hangTop),
                new Vector2(SpellSeal, SpellSeal), Pin.Top);
            seal.GetComponent<Image>().raycastTarget = false;

            Vector2 tagSize = ArtSize("Ui/panel_spell_tag", SpellTagW, 300f);
            // 红绳顶上的结要压在细竹篾正中。
            float tagTop = hangTop + SpellSeal * 0.5f - tagSize.y * 0.08f;
            for (int s = 0; s < GameConstants.SpellSlots; s++)
                HangSpell(page, s, new Vector2((s - 0.5f) * 300f + 20f, tagTop), tagSize);

            float catalogY = tagTop + tagSize.y + 12f;
            float avail = Mathf.Max(360f, PageH - catalogY - 8f);
            float cardH = Mathf.Clamp((avail - 24f) / 3f, 148f, 188f);
            float gap = Mathf.Clamp((avail - 3f * cardH) / 2f, 8f, 16f);
            var cardSize = new Vector2(SpellCardW, cardH);
            for (int i = 0; i < SpellCatalog.Count; i++)
                SpellCard(page, i, UiKit.GridPos(i, 2, 338f, cardH + gap) + new Vector2(0f, catalogY), cardSize);
        }

        void EnsureSpellSeal()
        {
            if (_view == null || _view.SpellPage == null) return;
            var page = _view.SpellPage.GetComponent<RectTransform>();
            if (page.Find("seal") != null) return;
            var seal = UiKit.Art(page, "seal", "Ui/panel_spell_seal",
                new Vector2(-SpellRodW * 0.5f + SpellSeal * 0.38f, 8f),
                new Vector2(SpellSeal, SpellSeal), Pin.Top);
            seal.GetComponent<Image>().raycastTarget = false;
        }

        void HangSpell(RectTransform page, int s, Vector2 pos, Vector2 size)
        {
            int id = s < _meta.Equipped.Length ? _meta.Equipped[s] : -1;
            bool has = id >= 0;
            var box = UiKit.Art(page, "slot" + s, has ? "Ui/panel_spell_tag_on" : "Ui/panel_spell_tag",
                pos, size, Pin.Top);
            box.GetComponent<Image>().raycastTarget = false;
            if (!has)
            {
                var e = UiKit.Label(box, "n", "空槽", 28, new Vector2(0f, -size.y * 0.08f), new Vector2(140, 40));
                e.color = InkTheme.TextDim;
                return;
            }
            SpellDef d = SpellCatalog.Get(id);
            int rank = _meta.SpellRank(id);
            CardIcon(box, InkSprites.Ui(d.Id), new Vector2(0f, size.y * 0.04f), 56f, true);
            var n = UiKit.Label(box, "n", rank > 0 ? d.Name + " Lv." + rank : d.Name, 26,
                new Vector2(0f, -size.y * 0.16f), new Vector2(140, 34));
            UiKit.Bold(n);
            var c = UiKit.Label(box, "c", "耗 " + d.GoldCost + " 金", 18,
                new Vector2(0f, -size.y * 0.28f), new Vector2(140, 26));
            c.color = InkTheme.TextMid;
        }

        void SpellCard(RectTransform page, int i, Vector2 pos, Vector2 size)
        {
            SpellDef d = SpellCatalog.Get(i);
            int rank = _meta.SpellRank(i);
            bool owned = rank > 0;
            bool maxed = rank >= SpellCatalog.MaxLevel;
            int slot = _meta.EquippedSlot(i);
            int have = _meta.SpellShardCount(i);
            int need = maxed ? 0 : SpellCatalog.NextShards(d, rank);
            bool gathering = !maxed && have < need;
            bool buyable = _meta.CanBuySpell(i, out string why);
            bool ready = !maxed && need > 0 && have >= need;
            string key = slot >= 0 ? "Ui/panel_skin_on"
                : (rank <= 0 && have <= 0 ? "Ui/panel_skin_dim" : "Ui/panel_skin");
            var box = UiKit.Art(page, "sp" + i, key, pos, size, Pin.Top, SkinSlice);
            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box.GetComponent<Image>();
            btn.interactable = owned || buyable;
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                if (_meta.SpellRank(idx) > 0) _meta.Equip(idx);
                else _meta.BuySpell(idx);
                Rebuild(TabSpell);
            });

            CardIcon(box, InkSprites.Ui(d.Id), new Vector2(-118f, 10f), 68f, true);
            Color title = rank <= 0 && have <= 0 ? InkTheme.TextDim : InkTheme.TextDark;
            Color body = rank <= 0 && have <= 0 ? InkTheme.TextDim : InkTheme.TextMid;
            var n = UiKit.Label(box, "n", owned ? d.Name + " Lv." + rank : d.Name, 26,
                new Vector2(24f, 36f), new Vector2(168, 34), TextAnchor.MiddleLeft);
            UiKit.Bold(n);
            n.color = slot >= 0 ? InkTheme.Seal : title;
            var desc = UiKit.Label(box, "d", SpellCatalog.Blurb(d, rank), 16, new Vector2(36f, 4f),
                new Vector2(200, 28), TextAnchor.MiddleLeft);
            desc.color = body;
            var cost = UiKit.Label(box, "e", "耗 " + d.GoldCost + " 金", 17,
                new Vector2(-86f, -36f), new Vector2(130, 26), TextAnchor.MiddleLeft);
            cost.color = body;
            ShowShardBar(box, have, need, d.Tint, gathering);

            if (buyable)
            {
                var pill = UiKit.Art(box, "pp", "Ui/panel_price", new Vector2(88f, -36f),
                    new Vector2(124f, 38f), Pin.Center, PriceSlice);
                var pillImg = pill.GetComponent<Image>();
                pillImg.raycastTarget = owned;
                UiKit.Icon(pill, InkSprites.Ui("ink"), new Vector2(-40f, 0f), 22f);
                var t = UiKit.Label(pill, "t", SpellCatalog.NextPrice(d, rank) + " 墨", 18,
                    new Vector2(10f, 0f), new Vector2(86, 30));
                UiKit.Bold(t);
                if (owned)
                {
                    var up = pill.gameObject.AddComponent<Button>();
                    up.targetGraphic = pillImg;
                    up.onClick.AddListener(() => { if (_meta.BuySpell(idx)) Rebuild(TabSpell); });
                }
            }
            else if (maxed || ready || (owned && !gathering))
            {
                string tail = maxed ? "已满级"
                    : (ready ? why : (slot >= 0 ? "已装备 " + (slot + 1) : "点击装备"));
                var t = UiKit.Label(box, "s", tail, 18, new Vector2(78f, -36f),
                    new Vector2(140, 26), TextAnchor.MiddleRight);
                t.color = maxed || ready ? InkTheme.TextDim : (slot >= 0 ? InkTheme.Seal : InkTheme.TextMid);
                UiKit.Bold(t);
            }
        }
    }
}
