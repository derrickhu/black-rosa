using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 首页：常驻顶栏 + 三段底栏（炮台 / 出征 / 技能）。
    // 有 Prefabs/Home 就只绑定；没有才走下面那套临时代码搭壳。
    public sealed class HomeScreen
    {
        public const int TabForge = 0;
        public const int TabSortie = 1;
        public const int TabSpell = 2;

        const float CardW = 322f;
        const float CardH = 152f;
        const float ColStep = 338f;
        const float CardGap = 12f;
        const float SpellRodW = 640f;
        const float SpellSeal = 52f;
        const float SpellTagW = 190f;

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

        HomeScreen(RectTransform layer, MetaProgress meta, Action<int> start)
        {
            _layer = layer;
            _meta = meta;
            _start = start;
        }

        public static HomeScreen Build(RectTransform layer, MetaProgress meta, Action<int> start, Action reload,
            int tab = TabSortie)
        {
            var h = new HomeScreen(layer, meta, start);
            UiKit.PaperSheet(layer);
            if (!h.TryPrefab()) h.BuildShell();
            h.FitFrame();
            h.Pick(Mathf.Clamp(tab, TabForge, TabSpell));
            h.RefreshTop();
            // 排行榜上线前就有进度的老玩家，进大厅补报一次；报过的同样关数不会重发。
            RankService.Submit(meta.ClearedCount());
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
                ForgeTabDot();
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
            var page = _view.Board != null ? _view.Board.transform.parent as RectTransform : _pages[TabForge];
            if (page != null) BindShowcase(page, ShowcaseTop);
            BindBoostList();
        }

        // 专属词条不论买没买那款炮，都只跟着展台上正在看的那一款出现，排在列表最上面。
        int SkinInView => _skinFocus >= 0 ? _skinFocus : _meta.Skin;

        bool ListShown(int line)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            return d.Exclusive ? d.Skin == SkinInView : _meta.ForgeShown(line);
        }

        // 展台换了一款炮，只刷词条列表，展台自己的滑入动画不打断。
        void RefreshBoosts()
        {
            if (_view != null)
            {
                BindBoostList();
                return;
            }
            RectTransform page = _pages[TabForge];
            Transform old = page != null ? page.Find("boosts") : null;
            if (old == null) return;
            old.name = "boosts_old";
            UnityEngine.Object.Destroy(old.gameObject);
            BuildBoosts(page, 2f + SkinShowcase.H + 10f);
        }

        void BindBoostList()
        {
            int stars = _meta.ClearedCount();
            int locked = ForgeCatalog.NextLocked(stars, _meta.Forge);
            _forgeFresh |= _meta.TakeForgeNew();
            if (_view.Boosts == null || _view.Boosts.Length == 0 || _view.Boosts[0] == null) return;
            EnsureBoostRows();
            EnsureBoostScroll();
            for (int i = 0; i < _view.Boosts.Length; i++)
            {
                var row = _view.Boosts[i];
                if (row == null) continue;
                bool shown = i < ForgeCatalog.LineCount && ListShown(i);
                bool tease = i == locked;
                row.gameObject.SetActive(shown || tease);
                var le = row.GetComponent<LayoutElement>();
                if (le != null) le.minHeight = le.preferredHeight = HomeForgeRow.Height;
            }
            // 列表按解锁先后排，专属在顶，预告垫底。预制体里是按词条下标烘的。
            int[] order = ForgeOrder();
            for (int k = 0; k < order.Length; k++)
                if (_view.Boosts[order[k]] != null) _view.Boosts[order[k]].transform.SetSiblingIndex(k);
            if (locked >= 0 && _view.Boosts[locked] != null) _view.Boosts[locked].transform.SetAsLastSibling();
            var list = _view.Boosts[0].transform.parent as RectTransform;
            if (list != null) LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            for (int i = 0; i < _view.Boosts.Length && i < ForgeCatalog.LineCount; i++)
            {
                var row = _view.Boosts[i];
                if (row == null || !row.gameObject.activeSelf) continue;
                if (i == locked) BindBoostTease(row, i);
                else BindBoost(row, i);
            }
        }

        long _forgeFresh;

        int[] ForgeOrder()
        {
            var ids = new System.Collections.Generic.List<int>();
            for (int i = 0; i < ForgeCatalog.LineCount; i++) ids.Add(i);
            ids.Sort((a, b) => ForgeCatalog.Rank(a).CompareTo(ForgeCatalog.Rank(b)));
            return ids.ToArray();
        }

        // 预制体只烘了当时那几条。新加的词条照第一行复制，不用回去重烘整页。
        void EnsureBoostRows()
        {
            int have = _view.Boosts.Length;
            if (have >= ForgeCatalog.LineCount) return;
            var src = _view.Boosts[0];
            var rows = new HomeBoostRow[ForgeCatalog.LineCount];
            for (int i = 0; i < have; i++) rows[i] = _view.Boosts[i];
            for (int i = have; i < rows.Length; i++)
            {
                var go = UnityEngine.Object.Instantiate(src.gameObject, src.transform.parent, false);
                go.name = "fg" + i;
                var slot = go.GetComponent<HomeBoostRow>();
                slot.Ui = null;
                rows[i] = slot;
            }
            _view.Boosts = rows;
        }

        // 五条线加高之后超出页面，挂一层竖向滚动。底栏卷轴会盖住页脚，视口在它上面停住。
        void EnsureBoostScroll()
        {
            if (_view.Boosts.Length == 0 || _view.Boosts[0] == null) return;
            var box = _view.Boosts[0].transform.parent as RectTransform;
            if (box == null) return;
            var fit = box.GetComponent<ContentSizeFitter>();
            if (fit == null) fit = box.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = box.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 10f;
                layout.padding = new RectOffset(0, 0, 4, 20);
            }
            float top = ShowcaseTop + SkinShowcase.H + 12f;
            RectTransform view = box.parent != null && box.parent.name == "boostView" ? box.parent as RectTransform : null;
            if (view == null)
            {
                var page = box.parent as RectTransform;
                if (page == null) return;
                var go = new GameObject("boostView", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                view = go.GetComponent<RectTransform>();
                view.SetParent(page, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
                box.SetParent(view, false);
                var scroll = go.AddComponent<ScrollRect>();
                scroll.content = box;
                scroll.viewport = view;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 40f;
                scroll.inertia = true;
            }
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(0f, 52f);
            view.offsetMax = new Vector2(0f, -top);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 1f);
            box.pivot = new Vector2(0.5f, 1f);
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = new Vector2(HomeForgeRow.Width, box.sizeDelta.y);
        }

        SkinShowcase _showcase;
        int _skinFocus = -1;
        const float ShowcaseTop = 8f;

        // 展台记着上次看到哪一款；第一次进来、或者刚买完换上，停在正在用的那款。
        void BindShowcase(RectTransform page, float top)
        {
            _showcase = SkinShowcase.Ensure(_showcase, page, top, _meta,
                f => { _skinFocus = f; RefreshBoosts(); },
                () => { _skinFocus = _meta.Skin; Rebuild(TabForge); });
            _showcase.Root.SetAsFirstSibling();
            _showcase.Bind(_skinFocus >= 0 ? _skinFocus : _meta.Skin);
            if (_meta.SkinNew != 0)
            {
                _meta.SkinNew = 0;
                _meta.Save();
            }
        }

        // 结算页「去看看」新开放的炮台时，炮台页直接停在那一款上。
        public void FocusSkin(int skin)
        {
            _skinFocus = Mathf.Clamp(skin, 0, SkinCatalog.Count - 1);
            if (_tab == TabForge) Rebuild(TabForge);
        }

        void BindBoost(HomeBoostRow slot, int line)
        {
            Paint(slot.Row, "Ui/panel_skin", SkinSlice);
            if (slot.Row != null) slot.Row.color = Color.white;
            var ui = HomeForgeRow.Ensure(slot, slot.transform as RectTransform);
            if (ui == null) return;
            ForgeDef d = ForgeCatalog.Get(line);
            if (d.Exclusive && !_meta.ForgeShown(line))
            {
                ui.BindPreview(_meta, line);
                MuteRow(slot);
                return;
            }
            int idx = line;
            ui.Bind(_meta, line, () => { if (_meta.BuyForge(idx)) Rebuild(TabForge); },
                (_forgeFresh & (1L << line)) != 0);
            MuteRow(slot);
        }

        void BindBoostTease(HomeBoostRow slot, int line)
        {
            Paint(slot.Row, "Ui/panel_skin_dim", SkinSlice);
            var ui = HomeForgeRow.Ensure(slot, slot.transform as RectTransform);
            if (ui == null) return;
            ui.BindTease(_meta, line);
            MuteRow(slot);
        }

        // 整行不再是按钮，只有右边的升级键能点，免得滑动列表时误买。
        static void MuteRow(HomeBoostRow slot)
        {
            if (slot.Button == null) return;
            slot.Button.onClick.RemoveAllListeners();
            slot.Button.interactable = false;
        }

        int _chapter = -1;

        void BindSortie()
        {
            if (_pages[1] != null)
            {
                SortiePageBuilder.Ensure(_view, _pages[1]);
                SortiePageBuilder.EnsureSides(_view, _pages[1]);
            }
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
                string art = "Ui/chapter_" + (_chapter + 1);
                if (InkSprites.Load(art) == null) art = "Ui/chapter_1";
                CdnAssets.Bind(board.Art, art);
            }
            // 左右翻页和点「出征」都不用等图：相邻两章的章节图和本章战场先拉。
            CdnAssets.Prefetch("Ui/chapter_" + _chapter);
            CdnAssets.Prefetch("Ui/chapter_" + (_chapter + 2));
            CdnAssets.Prefetch("Bg/battle_bg_" + (_chapter + 1));
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

        // 过了的关在节点底下挂三颗小星，亮几颗看最高评星。预制体里没烘星星，
        // 第一次用到时现挂到节点上，之后复用。
        static void NodeStars(HomeSealCell slot, int stars)
        {
            if (slot.Stars == null || slot.Stars.Length < 3 || slot.Stars[0] == null)
            {
                if (stars <= 0) return;
                var rt = slot.GetComponent<RectTransform>();
                float size = rt != null ? rt.rect.width : 68f;
                float s = Mathf.Max(18f, size * 0.34f);
                slot.Stars = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    float x = (i - 1) * s * 0.92f;
                    float y = -size * 0.5f - s * 0.1f + (i == 1 ? -s * 0.18f : 0f);
                    slot.Stars[i] = UiKit.Icon(slot.transform, InkSprites.Load("Ui/result_star_on"), new Vector2(x, y), s);
                }
            }
            Sprite on = InkSprites.Load("Ui/result_star_on");
            Sprite off = InkSprites.Load("Ui/result_star_off");
            for (int i = 0; i < slot.Stars.Length; i++)
            {
                if (slot.Stars[i] == null) continue;
                slot.Stars[i].gameObject.SetActive(stars > 0);
                Sprite spr = i < stars ? on : off;
                if (spr != null) slot.Stars[i].sprite = spr;
            }
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
            NodeStars(slot, cleared ? _meta.Stars[index] : 0);
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
                if (i == SideGift) btn.onClick.AddListener(() => StarterGiftPanel.Show(_layer, _meta, AfterReward));
                else if (i == SideClub) btn.onClick.AddListener(() => GameClubPanel.Show(_layer, _meta, AfterReward));
                else if (i == SideCheckIn) btn.onClick.AddListener(() => CheckInPanel.Show(_layer, _meta, AfterReward));
                else if (i == SideCodex) btn.onClick.AddListener(() => CodexPanel.Show(_layer, _meta, AfterReward));
                else if (i == SideRank) btn.onClick.AddListener(() => RankPanel.Show(_layer, _meta));
            }
            if (_view.SideActs.Length != SortiePageBuilder.SideCount) return;
            Button gift = _view.SideActs[SideGift];
            if (gift != null) gift.gameObject.SetActive(!_meta.GiftClaimed);
            RedDot(gift, !_meta.GiftClaimed);
            RedDot(_view.SideActs[SideClub], !_meta.ClubClaimedToday);
            RedDot(_view.SideActs[SideCheckIn], !_meta.CheckedToday);
            RedDot(_view.SideActs[SideCodex], _meta.CodexHasNew);
        }

        // 领了礼包 / 游戏圈奖励：顶栏数值、礼包贴纸、皮肤页都要跟着变。
        void AfterReward()
        {
            _shownSec = -1;
            Rebuild(_tab);
        }

        // 有刚解锁、还没看过的词条时，底栏「炮台」的牌匾右上角挂红点。
        void ForgeTabDot()
        {
            if (_tabs == null || _tabs.Length <= TabForge || _tabs[TabForge] == null) return;
            Transform btn = _tabs[TabForge].transform;
            bool on = _meta.ForgeNew != 0 || _meta.SkinNew != 0;
            Transform dot = btn.Find("dot");
            if (dot == null)
            {
                if (!on) return;
                var ring = UiKit.Icon(btn, UiSprites.Disc(), new Vector2(42f, 88f), 28f);
                ring.gameObject.name = "dot";
                ring.color = InkTheme.CardFace;
                UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 22f).color = InkTheme.Seal;
                dot = ring.transform;
            }
            dot.gameObject.SetActive(on);
        }

        static void RedDot(Button btn, bool on)
        {
            if (btn == null) return;
            Transform dot = btn.transform.Find("dot");
            if (dot == null)
            {
                if (!on) return;
                var rt = (RectTransform)btn.transform;
                var ring = UiKit.Icon(btn.transform, UiSprites.Disc(), new Vector2(rt.sizeDelta.x * 0.5f - 14f, 52f), 26f);
                ring.gameObject.name = "dot";
                ring.color = InkTheme.CardFace;
                UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 20f).color = InkTheme.Seal;
                dot = ring.transform;
            }
            dot.gameObject.SetActive(on);
        }

        // 出征页侧边贴纸，顺序同 SortiePageBuilder.BuildSides。
        const int SideGift = 0;
        const int SideClub = 1;
        const int SideCheckIn = 2;
        const int SideCodex = 3;
        const int SideRank = 4;

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

        // 一行一张横卡。当前、下一级、所需材料和按钮要同时摆开，两列放不下。
        static void FitSpellGrid(RectTransform grid, RectTransform viewport)
        {
            if (grid == null || viewport == null) return;
            var layout = grid.GetComponent<GridLayoutGroup>();
            Canvas.ForceUpdateCanvases();
            float viewW = viewport.rect.width;
            if (viewW < 80f) viewW = 700f;
            float cellW = Mathf.Min(HomeSpellRow.Width, viewW - 12f);
            float cellH = HomeSpellRow.Height;
            if (layout != null)
            {
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 1;
                layout.spacing = new Vector2(0f, 12f);
                layout.padding = new RectOffset(0, 0, 6, 24);
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.cellSize = new Vector2(cellW, cellH);
            }
            grid.anchorMin = new Vector2(0.5f, 1f);
            grid.anchorMax = new Vector2(0.5f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = Vector2.zero;
            grid.sizeDelta = new Vector2(cellW, 40f);
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
            if (slot.Cost != null) slot.Cost.text = "释放 " + d.GoldCost + " 金";
        }

        void BindSpellCard(HomeSpellCard slot, int i)
        {
            if (slot == null) return;
            bool worn = _meta.EquippedSlot(i) >= 0;
            Paint(slot.Card, worn ? "Ui/panel_skin_on" : "Ui/panel_skin", SkinSlice);
            var card = (slot.Card != null ? slot.Card.transform : slot.transform) as RectTransform;
            var row = HomeSpellRow.Ensure(slot, card);
            if (row == null) return;
            int idx = i;
            row.Bind(_meta, i,
                () => { if (_meta.BuySpell(idx)) Rebuild(TabSpell); },
                () => { _meta.Equip(idx); Rebuild(TabSpell); });
            if (slot.Button != null)
            {
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.interactable = false;
            }
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

        const float RowGap = 8f;

        static readonly Vector4 CardSlice = new Vector4(18f, 18f, 18f, 18f);
        // 词条行底图自带白边、描边和底唇，边要比普通票面宽，拉长时唇才不会被拉扁。
        static readonly Vector4 SkinSlice = new Vector4(30f, 34f, 30f, 30f);
        static readonly Vector4 PillSlice = new Vector4(44f, 8f, 44f, 8f);

        void BuildForge(RectTransform page)
        {
            _showcase = null;
            BindShowcase(page, 2f);
            BuildBoosts(page, 2f + SkinShowcase.H + 10f);
        }

        // 预制体里还烘着旧的棕盘和 2×2 皮肤卡，展台接管之后全部藏掉。
        void LayoutForgeTray()
        {
            if (_view.Board != null) _view.Board.gameObject.SetActive(false);
            if (_view.GunSummary != null)
                _view.GunSummary.transform.parent.gameObject.SetActive(false);
            if (_view.Skins != null)
                foreach (var slot in _view.Skins)
                    if (slot != null) slot.gameObject.SetActive(false);
        }

        void BuildBoosts(RectTransform page, float topY)
        {
            int stars = _meta.ClearedCount();
            _forgeFresh |= _meta.TakeForgeNew();
            var shown = new System.Collections.Generic.List<int>();
            foreach (int i in ForgeOrder())
                if (ListShown(i))
                    shown.Add(i);
            int locked = ForgeCatalog.NextLocked(stars, _meta.Forge);
            int n = shown.Count + (locked >= 0 ? 1 : 0);
            float viewH = Mathf.Max(140f, PageH - topY - 6f);
            float gap = RowGap;
            float contentH = n * HomeForgeRow.Height + Mathf.Max(0, n - 1) * gap + 8f;
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
                BindBoost(ForgeRow(list, "fg" + shown[i], i * (HomeForgeRow.Height + gap)), shown[i]);
            if (locked >= 0)
                BindBoostTease(ForgeRow(list, "lk" + locked, shown.Count * (HomeForgeRow.Height + gap)), locked);
        }

        static HomeBoostRow ForgeRow(RectTransform page, string name, float y)
        {
            var box = UiKit.Art(page, name, "Ui/panel_skin", new Vector2(0f, y),
                new Vector2(HomeForgeRow.Width, HomeForgeRow.Height), Pin.Top, SkinSlice);
            var slot = box.gameObject.AddComponent<HomeBoostRow>();
            slot.Row = box.GetComponent<Image>();
            return slot;
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
            var cardSize = new Vector2(HomeSpellRow.Width, HomeSpellRow.Height);
            for (int i = 0; i < SpellCatalog.Count; i++)
                SpellCard(page, i, new Vector2(0f, catalogY + i * (HomeSpellRow.Height + 12f)), cardSize);
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
            var c = UiKit.Label(box, "c", "释放 " + d.GoldCost + " 金", 18,
                new Vector2(0f, -size.y * 0.28f), new Vector2(140, 26));
            c.color = InkTheme.TextMid;
        }

        void SpellCard(RectTransform page, int i, Vector2 pos, Vector2 size)
        {
            var box = UiKit.Art(page, "sp" + i, "Ui/panel_skin", pos, size, Pin.Top, SkinSlice);
            var slot = box.gameObject.AddComponent<HomeSpellCard>();
            slot.Card = box.GetComponent<Image>();
            BindSpellCard(slot, i);
        }
    }
}
