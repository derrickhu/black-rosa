using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InkLine
{
    // 首页按 720×1280 的稿重排。Figma 的 y 向下，这里锚在左上。
    public static class HomeReskin
    {
        public static bool Ready => InkSprites.Load("Ui/home_lawn") != null;

        public static void ApplyLawn(Image art, int chapter)
        {
            if (art == null) return;
            // 第一章沿用 home_lawn。第二章起是 home_lawn_2 到 home_lawn_14。
            int n = Mathf.Clamp(chapter, 0, 13) + 1;
            Sprite lawn = InkSprites.Load("Ui/home_lawn_" + n) ?? InkSprites.Load("Ui/home_lawn");
            if (lawn == null) return;
            art.sprite = lawn;
            art.color = Color.white;
            art.preserveAspect = true;
            art.type = Image.Type.Simple;
        }

        public const float NodeW = 50f;
        public const float NodeH = 50f * 204f / 336f;

        // 抠过的圆点：顶面中心在图高从下往上 132/204 的位置。
        public static readonly Vector2 NodePivot = new Vector2(0.5f, 132f / 204f);

        // 相对草地左上角的顶面中心。顺序是这一章的第 1 关到第 9 关，从下往上。
        // 左右交替，斜向间距一样，星星不会压到旁边的圆点。
        public static readonly Vector2[] Dots =
        {
            new Vector2(156f, 366f),
            new Vector2(216f, 330f),
            new Vector2(276f, 294f),
            new Vector2(216f, 258f),
            new Vector2(156f, 222f),
            new Vector2(216f, 186f),
            new Vector2(276f, 150f),
            new Vector2(216f, 114f),
            new Vector2(156f, 78f),
        };

        // 下标跟 HomeScreen.Side* 一致。行距 112，整列比原来下移约 48。
        // 左列：签到、游戏圈、邮箱、图鉴、排行榜。右列：招财进宝在上，新手礼包在下。
        static readonly float[] SideTop = { 332f, 332f, 220f, 556f, 668f, 220f, 444f };
        static bool SideOnRight(int index) => index == 0 || index == 5;

        public static readonly Color Ink = new Color(0.102f, 0.063f, 0.031f, 1f);
        public static readonly Color Cream = new Color(1f, 0.965f, 0.894f, 1f);

        // 三角的图和地图图左右都有透明边。贴到地图可见边缘外大约 6px。
        public const float ArrowXLeft = 96f;
        public const float ArrowXRight = 561f;

        // 图的下沿是阴影和透明边。字按整张图居中会沉到色块下面，这里按实心部分往上抬。
        const float TitleLift = 3f;
        const float TagLift = 5f;
        const float GoLift = 6f;
        const float OpenLift = 6f;

        public static void PlaceNode(RectTransform rt, int local)
        {
            if (rt == null || local < 0 || local >= Dots.Length) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = NodePivot;
            rt.sizeDelta = new Vector2(NodeW, NodeH);
            Vector2 dot = Dots[local];
            rt.anchoredPosition = new Vector2(dot.x, -dot.y);
            rt.localScale = Vector3.one;
        }

        // 序号写在圆点顶面中心。三位数略缩小，头目的紫色顶面用浅字。
        public static void PlaceNumber(Text num, int stageNo, bool boss)
        {
            if (num == null) return;
            num.gameObject.SetActive(true);
            num.text = stageNo.ToString();
            num.font = UiKit.FontBold;
            num.fontStyle = FontStyle.Normal;
            num.fontSize = UiKit.ReadSize(stageNo >= 100 ? 12 : 15);
            num.alignment = TextAnchor.MiddleCenter;
            num.alignByGeometry = true;
            num.color = boss ? Cream : Ink;
            num.horizontalOverflow = HorizontalWrapMode.Overflow;
            num.verticalOverflow = VerticalWrapMode.Overflow;
            num.raycastTarget = false;
            var rt = num.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, NodePivot.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48f, 28f);
            rt.anchoredPosition = new Vector2(0f, 1f);
        }

        public static float ScreenW = ScreenFit.DesignW;
        public static float ScreenH = ScreenFit.DesignH;
        public static bool LockDesign = true;

        const float DockH = 128f;
        const float DesignAboveDock = ScreenFit.DesignH - DockH;

        // 画布尺寸。锁在设计稿里时（矮屏整体缩小）坐标不再换算。
        public static void SetFrame(float width, float height, bool lockDesign)
        {
            ScreenW = Mathf.Max(1f, width);
            ScreenH = Mathf.Max(1f, height);
            LockDesign = lockDesign;
        }

        // 设计稿货币条从 y=100 起，这段已经让过顶。只把比它更低的胶囊多出来的部分往下推。
        static float TopExtra()
        {
            if (LockDesign || !WxBridge.IsMiniGame) return 0f;
            float cap = ScreenFit.CapsuleGuard > 0f ? ScreenFit.CapsuleGuard : 0f;
            return Mathf.Max(0f, cap - 100f);
        }

        public static float BottomExtra()
        {
            if (LockDesign || !WxBridge.IsMiniGame) return 0f;
            return ScreenFit.BottomPad;
        }

        // 宽仍按 720 居中。高屏把设计稿里底栏以上的间距拉开，相对位置不变。
        public static float PlaceX(float designX)
        {
            if (LockDesign) return designX;
            return designX + (ScreenW - ScreenFit.DesignW) * 0.5f;
        }

        public static float PlaceY(float designY)
        {
            if (LockDesign) return designY;
            float region = Mathf.Max(DesignAboveDock * 0.7f, ScreenH - TopExtra() - BottomExtra() - DockH);
            return TopExtra() + designY * (region / DesignAboveDock);
        }

        public static void TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        // 地图、标题、翻页和宝箱不在每次刷新里重摆，这里按当前屏高补一次。
        public static void FitStage(HomeView view)
        {
            if (!Ready || view == null || view.Chapter == null) return;
            TopLeft((RectTransform)view.Chapter.transform, PlaceX(144f), PlaceY(332f), 433f, 498f);
            if (view.Chapter.Ribbon != null)
                TopLeft(view.Chapter.Ribbon.rectTransform, PlaceX(212f), PlaceY(189f), 285f, 99f);
            PlaceArrow(view.Chapter.Prev, false);
            PlaceArrow(view.Chapter.Next, true);
            if (view.Chapter.StarChest != null)
            {
                var chest = (RectTransform)view.Chapter.StarChest.transform;
                float w = chest.sizeDelta.x > 1f ? chest.sizeDelta.x : 72f;
                float h = chest.sizeDelta.y > 1f ? chest.sizeDelta.y : 78f;
                TopLeft(chest, PlaceX(202f), PlaceY(344f), w, h);
            }
            if (view.GoButton != null)
                TopLeft((RectTransform)view.GoButton.transform, PlaceX(203f), PlaceY(848f), 314f, 80f);
            if (view.TabDock != null)
            {
                var dock = view.TabDock.rectTransform;
                dock.anchoredPosition = new Vector2(dock.anchoredPosition.x, BottomExtra());
            }
            if (view.SideActs != null)
            {
                for (int i = 0; i < view.SideActs.Length; i++)
                    if (view.SideActs[i] != null)
                        FitSide((RectTransform)view.SideActs[i].transform, i);
            }
            var page = view.Chapter.transform.parent as RectTransform;
            PlaceTag(page, "modetab0", 241.5f);
            PlaceTag(page, "modetab1", 241.5f + 112f);
        }

        static void PlaceTag(RectTransform page, string name, float designX)
        {
            if (page == null) return;
            var rt = page.Find(name) as RectTransform;
            if (rt == null) return;
            const float tagH = 45f;
            const float tagFoot = tagH * 76f / 90f;
            TopLeft(rt, PlaceX(designX), PlaceY(332f) - tagFoot + 4f, 126f, tagH);
        }

        static void PlaceArrow(Button btn, bool right)
        {
            if (btn == null) return;
            var rt = btn.transform as RectTransform;
            if (rt == null) return;
            TopLeft(rt, PlaceX(right ? ArrowXRight : ArrowXLeft), PlaceY(507f),
                rt.sizeDelta.x > 1f ? rt.sizeDelta.x : 62f,
                rt.sizeDelta.y > 1f ? rt.sizeDelta.y : 67f);
        }

        // 把背景放到首页根节点前面。已经在前面时不能再设成根节点的当前下标，
        // 那样背景会挪到根节点后面，把地图、底栏和两侧图标整页盖住。
        static void PlaceBehind(RectTransform back, RectTransform front)
        {
            if (back == null || front == null || back.parent != front.parent) return;
            int backAt = back.GetSiblingIndex();
            int frontAt = front.GetSiblingIndex();
            if (backAt < frontAt) return;
            back.SetSiblingIndex(frontAt);
        }

        public static void ApplyChrome(HomeView view)
        {
            if (!Ready || view == null) return;
            var root = view.transform as RectTransform;
            if (root == null) return;
            var host = root.parent as RectTransform;
            if (host == null) host = root;
            var bg = host.Find("home_bg") as RectTransform;
            if (bg == null) bg = root.Find("home_bg") as RectTransform;
            if (bg == null)
            {
                var go = new GameObject("home_bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(host, false);
                bg = go.GetComponent<RectTransform>();
                var img = go.GetComponent<Image>();
                img.sprite = InkSprites.Load("Ui/home_bg");
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
                img.color = Color.white;
            }
            else if (bg.parent != host)
                bg.SetParent(host, false);
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;
            bg.localScale = Vector3.one;
            if (host != root) PlaceBehind(bg, root);

            if (view.SortiePage != null)
            {
                var page = view.SortiePage.GetComponent<RectTransform>();
                if (page != null)
                {
                    page.anchorMin = Vector2.zero;
                    page.anchorMax = Vector2.one;
                    page.offsetMin = Vector2.zero;
                    page.offsetMax = Vector2.zero;
                    var pimg = page.GetComponent<Image>();
                    if (pimg != null)
                    {
                        pimg.color = new Color(1f, 1f, 1f, 0f);
                        pimg.raycastTarget = false;
                    }
                }
            }
            for (int i = 0; i < root.childCount; i++)
            {
                var rt = root.GetChild(i) as RectTransform;
                if (rt == null || rt == bg) continue;
                if (view.SortiePage != null && rt.gameObject == view.SortiePage) continue;
                if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.offsetMin.y > 140f)
                    rt.offsetMin = new Vector2(rt.offsetMin.x, 128f);
            }

            Raise(view.Stamina != null ? view.Stamina.transform.parent : null);
            Raise(view.Ink != null ? view.Ink.transform.parent : null);
            Transform inkChip = view.Ink != null ? view.Ink.transform.parent : null;
            Transform gem = inkChip != null && inkChip.parent != null ? inkChip.parent.Find("diamond") : null;
            Raise(gem);
            Raise(view.StamTip != null ? view.StamTip.transform : null);
            Raise(view.TabDock != null ? view.TabDock.transform : null);
            DropOld(view);
        }

        // 卷轴底栏、旧页签牌、旧路径和旧加号还会盖住新首页，进游戏就拆掉。
        static void DropOld(HomeView view)
        {
            if (view.Tabs != null)
            {
                foreach (Button tab in view.Tabs)
                {
                    if (tab == null) continue;
                    Drop(tab.transform.Find("plaque"));
                    Drop(tab.transform.Find("plaque_sh"));
                }
            }
            DropChipExtras(view.Stamina);
            DropChipExtras(view.Ink);
            Transform inkChip = view.Ink != null ? view.Ink.transform.parent : null;
            Transform gem = inkChip != null && inkChip.parent != null ? inkChip.parent.Find("diamond") : null;
            if (gem != null)
            {
                Drop(gem.Find("plus"));
                Drop(gem.Find("ln"));
                if (gem.parent != null) Drop(gem.parent.Find(gem.name + "_sh"));
            }
            if (view.GoButton != null)
            {
                Drop(view.GoButton.transform.Find("face"));
                Drop(view.GoButton.transform.Find("ln"));
                if (view.GoButton.transform.parent != null)
                    Drop(view.GoButton.transform.parent.Find(view.GoButton.name + "_sh"));
            }
            if (view.SortiePage != null)
            {
                Transform page = view.SortiePage.transform;
                Drop(page.Find("modetab0_old"));
                Drop(page.Find("modetab1_old"));
                Drop(page.Find("seals"));
                if (view.Logo != null) Drop(view.Logo.transform);
            }
            if (view.Chapter != null)
            {
                Drop(view.Chapter.transform.Find("hardtog"));
                Drop(view.Chapter.transform.Find("hardtog_sh"));
                if (view.Chapter.Route != null)
                {
                    Drop(view.Chapter.Route.transform);
                    view.Chapter.Route = null;
                }
                else Drop(view.Chapter.transform.Find("route"));
            }
        }

        static void DropChipExtras(Text label)
        {
            if (label == null) return;
            Transform chip = label.transform.parent;
            if (chip == null) return;
            Drop(chip.Find("plus"));
            Drop(chip.Find("ln"));
            if (chip.parent != null) Drop(chip.parent.Find(chip.name + "_sh"));
        }

        static void Drop(Transform t)
        {
            if (t == null) return;
            t.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(t.gameObject);
        }

        static void Raise(Transform t)
        {
            if (t != null) t.SetAsLastSibling();
        }

        // 货币条用 PlaceY(100) 跟着屏高下移。副本卡如果停在页顶固定处，标题会钻进货币里。
        // 卡片稿是 640 宽，720 的页左右各空 40。整张按页宽放大，内部比例不动，底栏上面留一截。
        public static void FitDungeon(HomeView view)
        {
            if (!Ready || view == null || view.DungeonPage == null) return;
            var root = view.transform as RectTransform;
            var page = view.DungeonPage.GetComponent<RectTransform>();
            var host = view.DungeonPage.GetComponent<DungeonPage>();
            if (root == null || page == null || host == null || host.Daily == null) return;
            var corners = new Vector3[4];
            page.GetWorldCorners(corners);
            float pageTop = root.InverseTransformPoint(corners[1]).y;
            float inset = root.rect.yMax - pageTop;
            float local = Mathf.Max(8f, PlaceY(168f) - inset);
            float span = page.rect.width > 64f ? page.rect.width : ScreenW;
            float scaleW = (span - 16f) / DungeonPageBuilder.CardW;
            float block = DungeonPageBuilder.CardH * 2f + DungeonPageBuilder.CardGap;
            float room = page.rect.height - local - 16f;
            float scaleH = room / Mathf.Max(1f, block);
            float scale = Mathf.Clamp(Mathf.Min(scaleW, scaleH), 1f, 1.12f);
            PlaceCard(host.Daily.transform as RectTransform, local, scale);
            float step = (DungeonPageBuilder.CardH + DungeonPageBuilder.CardGap) * scale;
            PlaceCard(host.Endless != null ? host.Endless.transform as RectTransform : null, local + step, scale);
        }

        static void PlaceCard(RectTransform rt, float y, float scale)
        {
            if (rt == null) return;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -y);
            rt.localScale = new Vector3(scale, scale, 1f);
        }

        // 道具页跟首页同一套：设计稿坐标锚在左上。高屏时上阵和字牌跟着拉开，图鉴底铺到 dock 上面。
        public static void FitItems(HomeView view)
        {
            if (view == null || view.SpellPage == null) return;
            var page = view.SpellPage.GetComponent<RectTransform>();
            if (page == null) return;
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = Vector2.zero;
            var items = page.GetComponentInChildren<HomeItemView>(true);
            if (items == null) return;
            float bottom = ScreenH - BottomExtra() - ItemPageBuilder.DockH;
            if (items.PlateOn != null)
                TopLeft(items.PlateOn, PlaceX(28f), PlaceY(ItemPageBuilder.TitleY),
                    ItemPageBuilder.PlateW, ItemPageBuilder.PlateH);
            if (items.Shelf != null)
                TopLeft(items.Shelf.rectTransform, PlaceX(ItemPageBuilder.GridX), PlaceY(ItemPageBuilder.LineupY),
                    ItemPageBuilder.LineupW, ItemPageBuilder.LineupH);
            float panelTop = PlaceY(ItemPageBuilder.PanelTop);
            if (items.Panel != null)
            {
                TopLeft(items.Panel, PlaceX(0f), panelTop, ScreenFit.DesignW, Mathf.Max(200f, bottom - panelTop));
                ItemPageBuilder.PaintPanel(items.Panel.GetComponent<Image>());
            }
            if (items.PlateCodex != null)
                TopLeft(items.PlateCodex, PlaceX(24f), PlaceY(ItemPageBuilder.PanelTop + 12f),
                    ItemPageBuilder.PlateW, ItemPageBuilder.PlateH);
            if (items.CountRt != null)
                TopLeft(items.CountRt, PlaceX(360f), PlaceY(ItemPageBuilder.PanelTop + 18f), 336f, 40f);
            if (items.Scroll != null)
            {
                float y = PlaceY(ItemPageBuilder.CodexY);
                TopLeft(items.Scroll.transform as RectTransform, PlaceX(ItemPageBuilder.GridX), y,
                    ItemPageBuilder.LineupW, Mathf.Max(160f, bottom - 8f - y));
            }
        }

        public static void FitBars(RectTransform stam, RectTransform ink, RectTransform gem, Text tip)
        {
            if (!Ready) return;
            Sprite bar = InkSprites.Load("Ui/home_bar");
            PlaceBar(stam, PlaceX(97f), bar, InkSprites.Ui("stamina"));
            PlaceBar(ink, PlaceX(271f), bar, InkSprites.Ui("ink"));
            PlaceBar(gem, PlaceX(445f), bar, InkSprites.Ui("diamond"));
            if (tip != null)
            {
                TopLeft(tip.rectTransform, PlaceX(97f), PlaceY(148f), 200f, 28f);
                tip.alignment = TextAnchor.MiddleCenter;
                tip.color = new Color(0.55f, 0.85f, 0.62f, 1f);
                tip.fontSize = UiKit.ReadSize(13);
                tip.horizontalOverflow = HorizontalWrapMode.Overflow;
                tip.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        static void PlaceBar(RectTransform chip, float x, Sprite bar, Sprite icon)
        {
            if (chip == null) return;
            TopLeft(chip, x, PlaceY(100f), 168f, 40f);
            var img = chip.GetComponent<Image>();
            if (img != null && bar != null)
            {
                img.sprite = bar;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
            }
            Transform plus = chip.Find("plus");
            if (plus != null) plus.gameObject.SetActive(false);
            Transform line = chip.Find("ln");
            if (line != null) line.gameObject.SetActive(false);
            if (chip.parent != null)
            {
                Transform shadow = chip.parent.Find(chip.name + "_sh");
                if (shadow != null) shadow.gameObject.SetActive(false);
            }

            Text value = null;
            foreach (Text t in chip.GetComponentsInChildren<Text>(true))
            {
                if (t.transform.parent == chip)
                {
                    value = t;
                    break;
                }
            }
            if (value != null)
            {
                TopLeft(value.rectTransform, 40f, 0f, 124f, 40f);
                value.color = Cream;
                value.alignment = TextAnchor.MiddleCenter;
                value.alignByGeometry = true;
                value.fontSize = UiKit.ReadSize(20);
                value.horizontalOverflow = HorizontalWrapMode.Overflow;
                value.verticalOverflow = VerticalWrapMode.Overflow;
            }

            Image mark = chip.Find("icon")?.GetComponent<Image>();
            if (mark == null)
            {
                foreach (Transform child in chip)
                {
                    if (child.name == "ln" || child.name == "plus" || child.name == "v") continue;
                    mark = child.GetComponent<Image>();
                    if (mark != null) break;
                }
            }
            if (mark != null)
            {
                TopLeft(mark.rectTransform, -7f, -8f, 47f, 47f);
                if (icon != null) mark.sprite = icon;
                mark.color = Color.white;
                mark.preserveAspect = true;
                mark.raycastTarget = false;
            }
        }

        public static void FitDock(HomeView view, Button[] tabs, int active, int[] slots)
        {
            if (!Ready || view == null || view.TabDock == null || tabs == null) return;
            var dock = view.TabDock.rectTransform;
            dock.anchorMin = new Vector2(0f, 0f);
            dock.anchorMax = new Vector2(1f, 0f);
            dock.pivot = new Vector2(0.5f, 0f);
            dock.anchoredPosition = new Vector2(0f, BottomExtra());
            dock.sizeDelta = new Vector2(0f, 128f);
            Sprite bar = InkSprites.Load("Ui/home_dock_bar");
            if (bar != null)
            {
                view.TabDock.sprite = bar;
                view.TabDock.type = Image.Type.Simple;
                view.TabDock.preserveAspect = false;
                view.TabDock.color = Color.white;
                view.TabDock.raycastTarget = false;
            }

            const float step = 144f;
            int n = tabs.Length;
            var sel = dock.Find("home_sel") as RectTransform;
            if (sel == null)
            {
                var go = new GameObject("home_sel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(dock, false);
                sel = go.GetComponent<RectTransform>();
            }
            var simg = sel.GetComponent<Image>();
            Sprite on = InkSprites.Load("Ui/home_dock_on");
            if (simg != null && on != null)
            {
                simg.sprite = on;
                simg.type = Image.Type.Simple;
                simg.preserveAspect = true;
                simg.raycastTarget = false;
                simg.color = Color.white;
            }

            string[] tabKeys = { "tab_forge", "tab_sortie", "tab_spell", "tab_dungeon", "tab_study" };
            float selX = 0f;
            for (int i = 0; i < n; i++)
            {
                if (tabs[i] == null) continue;
                bool isOn = i == active;
                int slot = slots != null && slots.Length == n ? slots[i] : i;
                float x = (slot - (n - 1) * 0.5f) * step;
                if (isOn) selX = x;
                var root = tabs[i].GetComponent<RectTransform>();
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = new Vector2(x, 0f);
                root.sizeDelta = new Vector2(step, 128f);
                Transform plaque = tabs[i].transform.Find("plaque");
                if (plaque != null) plaque.gameObject.SetActive(false);
                Transform plaqueSh = tabs[i].transform.Find("plaque_sh");
                if (plaqueSh != null) plaqueSh.gameObject.SetActive(false);
                Transform ic = tabs[i].transform.Find("icon");
                if (ic != null)
                {
                    var ir = ic.GetComponent<RectTransform>();
                    ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0.5f, 0.5f);
                    float icoS = isOn ? 84f : 74f;
                    ir.anchoredPosition = new Vector2(0f, isOn ? 20f : 18f);
                    ir.sizeDelta = new Vector2(icoS, icoS);
                    var ii = ic.GetComponent<Image>();
                    if (ii != null)
                    {
                        if (i < tabKeys.Length)
                        {
                            Sprite tabSpr = InkSprites.Ui(tabKeys[i]);
                            if (tabSpr != null) ii.sprite = tabSpr;
                        }
                        ii.preserveAspect = true;
                        Transform lk = ic.Find("lock");
                        bool locked = lk != null && lk.gameObject.activeSelf;
                        if (!locked) ii.color = Color.white;
                    }
                }
                Transform lb = tabs[i].transform.Find("t");
                if (lb != null)
                {
                    var lr = lb.GetComponent<RectTransform>();
                    lr.anchoredPosition = new Vector2(0f, -44f);
                    lr.sizeDelta = new Vector2(128f, 36f);
                    var t = lb.GetComponent<Text>();
                    if (t != null)
                    {
                        t.fontSize = UiKit.ReadSize(isOn ? 22 : 20);
                        t.fontStyle = FontStyle.Normal;
                        t.color = isOn ? Color.white : Ink;
                        t.font = UiKit.FontBold;
                        t.alignment = TextAnchor.MiddleCenter;
                        t.alignByGeometry = true;
                        var edge = t.GetComponent<Outline>();
                        if (isOn)
                        {
                            if (edge == null) edge = t.gameObject.AddComponent<Outline>();
                            edge.enabled = true;
                            edge.effectColor = Ink;
                            edge.effectDistance = new Vector2(1.5f, -1.5f);
                        }
                        else if (edge != null)
                        {
                            edge.enabled = false;
                        }
                    }
                }
            }

            sel.anchorMin = sel.anchorMax = new Vector2(0.5f, 0.5f);
            sel.pivot = new Vector2(0.5f, 0.5f);
            // 图是底栏两倍像素，含浅黄边。顶上 2px 墨线不在这张图里，所以贴着底边放。
            float selW = on != null ? on.rect.width * 0.5f : 122f;
            float selH = on != null ? on.rect.height * 0.5f : 126f;
            sel.sizeDelta = new Vector2(selW, selH);
            sel.anchoredPosition = new Vector2(selX, (selH - dock.sizeDelta.y) * 0.5f);
            sel.SetAsFirstSibling();
        }

        public static void FitGo(HomeView view, bool canGo)
        {
            if (!Ready || view == null || view.GoButton == null) return;
            var go = (RectTransform)view.GoButton.transform;
            TopLeft(go, PlaceX(203f), PlaceY(848f), 314f, 80f);
            var img = go.GetComponent<Image>();
            Sprite spr = InkSprites.Load("Ui/home_go");
            if (img != null && spr != null)
            {
                img.sprite = spr;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = canGo ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f);
            }
            if (view.GoLabel != null && view.GoLabel.transform.parent != go)
                view.GoLabel.transform.SetParent(go, false);
            Hide(go.Find("face"));
            Hide(go.Find("ln"));
            if (go.parent != null) Hide(go.parent.Find(go.name + "_sh"));
            if (view.GoLabel != null)
            {
                view.GoLabel.color = Ink;
                view.GoLabel.fontSize = UiKit.ReadSize(26);
                view.GoLabel.font = UiKit.FontBold;
                view.GoLabel.fontStyle = FontStyle.Normal;
                view.GoLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                view.GoLabel.verticalOverflow = VerticalWrapMode.Overflow;
                Seat(view.GoLabel, GoLift);
                view.GoLabel.gameObject.SetActive(true);
                view.GoLabel.transform.SetAsLastSibling();
            }
        }

        public static void FitChests(RectTransform root)
        {
            if (!Ready || root == null) return;
            TopLeft(root, PlaceX(97f), PlaceY(966f), 527f, 170f);
            for (int i = 0; i < 4; i++)
            {
                var box = root.Find("c" + i) as RectTransform;
                if (box == null) continue;
                TopLeft(box, i * 131f, 0f, 134f, 170f);
                var hit = box.GetComponent<Image>();
                if (hit != null)
                {
                    hit.color = new Color(1f, 1f, 1f, 0f);
                    hit.raycastTarget = true;
                }
                FitPiece(box.Find("tray") as RectTransform, 134f, 170f, true);
                FitPiece(box.Find("art") as RectTransform, 86f, 86f, false);
                if (box.Find("art") is RectTransform art) TopLeft(art, 24f, 16f, 86f, 86f);
                FitPiece(box.Find("ghost") as RectTransform, 86f, 86f, false);
                if (box.Find("ghost") is RectTransform ghost) TopLeft(ghost, 24f, 16f, 86f, 86f);
                Transform glow = box.Find("glow");
                if (glow != null) glow.gameObject.SetActive(false);
                var tag = box.Find("tag") as RectTransform;
                if (tag == null) continue;
                TopLeft(tag, 21f, 131f, 92f, 38f);
                var tagImg = tag.GetComponent<Image>();
                if (tagImg != null) tagImg.raycastTarget = false;
                var label = tag.Find("t") as RectTransform;
                if (label == null) continue;
                Seat(label.GetComponent<Text>(), OpenLift);
            }
        }

        static void FitPiece(RectTransform rt, float w, float h, bool center)
        {
            if (rt == null) return;
            if (center)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(w, h);
            }
            var img = rt.GetComponent<Image>();
            if (img != null)
            {
                img.preserveAspect = !center;
                img.raycastTarget = false;
            }
        }

        public static void FitSide(RectTransform rt, int index)
        {
            if (!Ready || rt == null || index < 0 || index >= SideTop.Length) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(96f, 82f);
            float x = SideOnRight(index) ? ScreenFit.DesignW - 48f : 48f;
            rt.anchoredPosition = new Vector2(PlaceX(x), -PlaceY(SideTop[index]));
            rt.localScale = Vector3.one;
            var icon = rt.Find("icon") as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1f);
                icon.pivot = new Vector2(0.5f, 1f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(62f, 62f);
            }
            var capRt = rt.Find("t") as RectTransform;
            if (capRt != null)
            {
                capRt.anchorMin = capRt.anchorMax = new Vector2(0.5f, 1f);
                capRt.pivot = new Vector2(0.5f, 1f);
                capRt.anchoredPosition = new Vector2(0f, -64f);
                capRt.sizeDelta = new Vector2(104f, 20f);
                var cap = capRt.GetComponent<Text>();
                if (cap != null)
                {
                    cap.font = UiKit.FontBold;
                    cap.fontStyle = FontStyle.Normal;
                    cap.fontSize = UiKit.ReadSize(16);
                    cap.color = Color.white;
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.alignByGeometry = true;
                    cap.horizontalOverflow = HorizontalWrapMode.Overflow;
                    cap.verticalOverflow = VerticalWrapMode.Overflow;
                    var line = cap.GetComponent<Outline>() ?? cap.gameObject.AddComponent<Outline>();
                    line.effectColor = new Color(0.10f, 0.06f, 0.03f, 1f);
                    line.effectDistance = new Vector2(1.3f, -1.3f);
                }
            }
            SeatDot(rt);
        }

        public static void FitStarChest(RectTransform chest, RectTransform art, Text label)
        {
            if (!Ready || chest == null) return;
            TopLeft(chest, PlaceX(202f), PlaceY(344f), 72f, 78f);
            if (art != null)
            {
                art.anchorMin = art.anchorMax = new Vector2(0.5f, 1f);
                art.pivot = new Vector2(0.5f, 1f);
                art.anchoredPosition = new Vector2(0f, 0f);
                art.sizeDelta = new Vector2(48f, 48f);
            }
            if (label != null) label.gameObject.SetActive(false);
        }

        public static void FitTitle(Text title)
        {
            if (!Ready || title == null) return;
            title.font = UiKit.FontBold;
            title.fontStyle = FontStyle.Normal;
            title.color = Cream;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 18;
            title.resizeTextMaxSize = 28;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            Seat(title, TitleLift, 96f, 14f, 22f, 22f);
        }

        // 按字形居中，再把字抬到色块中线。left/right/bottom/top 是相对按钮的边距。
        static void Seat(Text text, float lift, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            if (text == null) return;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom + lift);
            rt.offsetMax = new Vector2(-right, -top + lift);
        }

        public static Sprite NodeSprite(string key)
        {
            string file = "home_node_lock";
            if (key == "node_done") file = "home_node_done";
            else if (key == "node_now") file = "home_node_now";
            else if (key == "node_boss") file = "home_node_boss";
            return InkSprites.Load("Ui/" + file);
        }

        // 红点贴在当前图标的右上角。没有单独图标时（精英标签）贴在自身右上。
        public static void PlaceDot(Transform host, bool on, float size = 16f)
        {
            if (host == null) return;
            Transform dot = FindDot(host);
            if (dot == null)
            {
                if (!on) return;
                Transform icon = host.Find("icon");
                var made = UiKit.Icon(icon != null ? icon : host, DotSprite(), Vector2.zero, size);
                made.gameObject.name = "dot";
                made.color = Color.white;
                dot = made.transform;
            }
            else
            {
                Transform inner = dot.Find("icon");
                if (inner != null) UnityEngine.Object.Destroy(inner.gameObject);
                var img = dot.GetComponent<Image>();
                if (img != null)
                {
                    Sprite face = DotSprite();
                    if (face != null) img.sprite = face;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
            }
            SeatDot(host, size);
            dot = FindDot(host);
            if (dot == null) return;
            dot.gameObject.SetActive(on);
            if (on) dot.SetAsLastSibling();
        }

        public static void SeatDot(Transform host, float size = 16f)
        {
            if (host == null) return;
            Transform icon = host.Find("icon");
            Transform dot = FindDot(host);
            if (dot == null) return;
            if (icon != null && dot.parent != icon) dot.SetParent(icon, false);
            var rt = (RectTransform)dot;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            // 角上压进图标一截，不浮在图标外面。
            rt.anchoredPosition = icon != null ? new Vector2(-6f, -6f) : new Vector2(-14f, -10f);
        }

        static Transform FindDot(Transform host)
        {
            if (host == null) return null;
            Transform dot = host.Find("dot");
            if (dot != null) return dot;
            Transform icon = host.Find("icon");
            return icon != null ? icon.Find("dot") : null;
        }

        static Sprite DotSprite() => InkSprites.Load("Ui/home_dot");

        public static void PaintArrow(Button btn, bool enabled, bool right)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            string key = enabled
                ? (right ? "Ui/home_arrow_on" : "Ui/home_arrow_on_l")
                : (right ? "Ui/home_arrow_off_r" : "Ui/home_arrow_off");
            Sprite s = InkSprites.Load(key);
            if (s != null) img.sprite = s;
            var rt = btn.transform as RectTransform;
            if (rt != null)
            {
                TopLeft(rt, PlaceX(right ? ArrowXRight : ArrowXLeft), PlaceY(507f),
                    rt.sizeDelta.x > 1f ? rt.sizeDelta.x : 62f,
                    rt.sizeDelta.y > 1f ? rt.sizeDelta.y : 67f);
            }
            img.color = Color.white;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            btn.transform.localScale = Vector3.one;
        }

        public static void PaintTag(RectTransform page, string name, string label, bool elite, bool locked,
            bool dot, UnityAction click, bool selected = true)
        {
            Transform found = page.Find(name);
            Button btn = found != null ? found.GetComponent<Button>() : null;
            if (btn == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(page, false);
                btn = go.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = go.GetComponent<Image>();
                UiKit.Bold(UiKit.Label(go.transform, "t", label, 16, Vector2.zero, new Vector2(110f, 36f)));
            }
            const float tagW = 146f;
            const float tagH = 52f;
            float x = PlaceX(name == "modetab1" ? 241.5f + (tagW - 14f) : 241.5f);
            // 地图轴心在上，图贴着 chapter 的上沿。标签图 90 行里色块最低到第 76 行。
            // 底边只压住大约 4px 的描边，看起来仍贴着地图上沿，不再盖进草地。
            const float tagFoot = tagH * 76f / 90f;
            float mapTop = 332f;
            var lawn = page.Find("chapter") as RectTransform;
            if (lawn != null) mapTop = -lawn.anchoredPosition.y;
            TopLeft((RectTransform)btn.transform, x, mapTop - tagFoot + 4f, tagW, tagH);
            Hide(btn.transform.Find("face"));
            var plate = btn.GetComponent<Image>();
            // 选中才用本色：普通黄、精英粉。没选中和还锁着的都是灰。
            string file = selected && !locked
                ? (elite ? "Ui/home_tag_elite" : "Ui/home_tag_normal")
                : "Ui/home_tag_gray";
            Sprite spr = InkSprites.Load(file);
            if (plate != null && spr != null)
            {
                plate.sprite = spr;
                plate.type = Image.Type.Simple;
                plate.preserveAspect = false;
                plate.color = Color.white;
            }
            foreach (Text extra in btn.GetComponentsInChildren<Text>(true))
                if (extra.gameObject.name != "t") extra.gameObject.SetActive(false);
            var text = btn.transform.Find("t")?.GetComponent<Text>();
            if (text != null)
            {
                text.text = label;
                text.color = Ink;
                text.fontSize = UiKit.ReadSize(16);
                text.font = UiKit.FontBold;
                text.fontStyle = FontStyle.Normal;
                Seat(text, TagLift, locked ? 24f : 0f, locked ? 4f : 0f);
            }
            ShowTagLock(btn.transform, locked);
            Transform badge = btn.transform.Find("dot");
            if (badge != null || dot)
                PlaceDot(btn.transform, dot);
            btn.onClick.RemoveAllListeners();
            if (click != null) btn.onClick.AddListener(click);
        }

        public static bool LayoutStars(HomeSealCell slot, int stars)
        {
            Sprite on = InkSprites.Load("Ui/home_star");
            if (on == null || slot == null) return false;
            if (slot.Stars == null || slot.Stars.Length < 3 || slot.Stars[0] == null)
            {
                slot.Stars = new Image[3];
                for (int i = 0; i < 3; i++)
                    slot.Stars[i] = UiKit.Icon(slot.transform, on, Vector2.zero, 24f);
            }
            for (int i = 0; i < slot.Stars.Length; i++)
            {
                Image star = slot.Stars[i];
                if (star == null) continue;
                star.sprite = on;
                star.color = Color.white;
                star.preserveAspect = true;
                star.gameObject.SetActive(i < stars);
                var rt = star.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(24f, 24f);
                rt.anchoredPosition = new Vector2((i - 1) * 26f, -1f);
            }
            return true;
        }

        // 没开放的精英标签：灰底上靠左放一把锁，字让开锁。
        static void ShowTagLock(Transform host, bool on)
        {
            Transform mark = host.Find("lock");
            if (!on)
            {
                if (mark != null) mark.gameObject.SetActive(false);
                return;
            }
            var img = mark != null ? mark.GetComponent<Image>() : null;
            if (img == null)
            {
                img = UiKit.Icon(host, InkSprites.Load("icon_lock"), Vector2.zero, 22f);
                img.gameObject.name = "lock";
            }
            img.gameObject.SetActive(true);
            img.color = Color.white;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(22f, 22f);
            rt.anchoredPosition = new Vector2(30f, 2f);
        }

        static void Hide(Transform t)
        {
            if (t != null) t.gameObject.SetActive(false);
        }
    }
}
