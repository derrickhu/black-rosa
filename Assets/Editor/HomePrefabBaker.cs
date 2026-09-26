using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 首页三页的壳。只挂工程里的 Sprite。
    // 预制已在就以编辑器里拖的为准，不要当日常保存。
    public static class HomePrefabBaker
    {
        const string Out = "Assets/Resources/Prefabs/Home.prefab";
        const string Ui = "Assets/Resources/Art/Ui/";

        [MenuItem("墨字防线/烘首页预制")]
        public static void BakeMenu()
        {
            if (System.IO.File.Exists(Out) &&
                !EditorUtility.DisplayDialog(
                    "重烘会覆盖微调",
                    "Home.prefab 已经在。重烘会按代码重新生成三页，你刚拖过的位置和尺寸都会被盖掉。\n\n日常改首页：打开预制 → 微调 → Save。\n只有预制丢了、或要整页重来时才重烘。",
                    "仍然重烘",
                    "取消"))
                return;
            Bake();
        }

        public static void BakeFromBatch()
        {
            AssetDatabase.ImportAsset("Assets/Resources/Art/Ui",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Bake();
        }

        [MenuItem("墨字防线/重排出征页")]
        public static void BakeSortieMenu()
        {
            AssetDatabase.ImportAsset("Assets/Resources/Art/Ui",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            SortiePageBuilder.SpriteOf = file =>
                AssetDatabase.LoadAssetAtPath<Sprite>(Ui + file + ".png");
            try { BakeSortieOnly(); }
            finally { SortiePageBuilder.SpriteOf = null; }
        }

        // 只换出征页。炮台和技能页留着，避免整份重烘盖掉微调。
        public static void BakeSortieFromBatch()
        {
            AssetDatabase.ImportAsset("Assets/Resources/Art/Ui",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            SortiePageBuilder.SpriteOf = file =>
                AssetDatabase.LoadAssetAtPath<Sprite>(Ui + file + ".png");
            try { BakeSortieOnly(); }
            finally { SortiePageBuilder.SpriteOf = null; }
        }

        public static void BakeSortieOnly()
        {
            if (!System.IO.File.Exists(Out))
            {
                Bake();
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(Out);
            var view = root.GetComponent<HomeView>();
            var page = view.SortiePage.GetComponent<RectTransform>();
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                Transform child = page.GetChild(i);
                if (child.name == "go" || child.name == "adstam" || child.name == "help") continue;
                Object.DestroyImmediate(child.gameObject);
            }
            view.Logo = null;
            view.Seals = null;
            view.Chapter = null;
            view.SideActs = null;
            SortiePageBuilder.Build(page, view);
            PrefabUtility.SaveAsPrefabAsset(root, Out);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log("出征页已写入 " + Out + "。炮台和技能页没动。");
        }

        public static void Bake()
        {
            var root = new GameObject("Home", typeof(RectTransform), typeof(HomeView));
            var view = root.GetComponent<HomeView>();
            Stretch(root.GetComponent<RectTransform>());

            BakeChips(root.transform, view);
            view.ForgePage = Page(root.transform, "page_forge").gameObject;
            view.SortiePage = Page(root.transform, "page_sortie").gameObject;
            view.SpellPage = Page(root.transform, "page_spell").gameObject;
            BakeForge(view.ForgePage.GetComponent<RectTransform>(), view);
            BakeSortie(view.SortiePage.GetComponent<RectTransform>(), view);
            BakeSpell(view.SpellPage.GetComponent<RectTransform>(), view);
            view.Tabs = BakeTabs(root.transform, view);

            System.IO.Directory.CreateDirectory("Assets/Resources/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(root, Out);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("首页预制已写入 " + Out + "。炮台 / 出征 / 技能可在 Project 里双开后直接拖。");
        }

        static void BakeChips(Transform root, HomeView view)
        {
            var stam = Pic(root, "stamina", "panel_strip", new Vector2(-128f, 10f), new Vector2(220f, 58f), Pin.Top);
            stam.raycastTarget = false;
            Icon(stam.rectTransform, "ico_stamina", new Vector2(-72f, 0f), 52f);
            view.Stamina = Label(stam.rectTransform, "v", "", 30, new Vector2(18f, 0f), new Vector2(120, 40), Pin.Center);
            view.Stamina.alignment = TextAnchor.MiddleLeft;
            UiKit.Bold(view.Stamina);

            var ink = Pic(root, "ink", "panel_strip", new Vector2(128f, 10f), new Vector2(220f, 58f), Pin.Top);
            ink.raycastTarget = false;
            Icon(ink.rectTransform, "ico_ink", new Vector2(-72f, 0f), 52f);
            view.Ink = Label(ink.rectTransform, "v", "", 30, new Vector2(18f, 0f), new Vector2(120, 40), Pin.Center);
            view.Ink.alignment = TextAnchor.MiddleLeft;
            UiKit.Bold(view.Ink);

            view.StamTip = Label(root, "stamtip", "", 19, new Vector2(-128f, 70f), new Vector2(220, 26), Pin.Top);
            view.StamTip.color = InkTheme.TextMid;
        }

        static void BakeForge(RectTransform page, HomeView view)
        {
            view.Board = Pic(page, "board", "panel_board", new Vector2(0f, 8f), new Vector2(640f, 415f), Pin.Top);
            view.Board.raycastTarget = false;

            var skins = new HomeSkinCell[SkinCatalog.Count];
            float[] xs = { -119f, 119f };
            float[] ys = { 46f, -86f };
            string[] guns = { "ico_skin_plain", "ico_skin_cinnabar", "ico_skin_ghost", "ico_skin_ghost" };
            for (int i = 0; i < skins.Length; i++)
            {
                var cell = Pic(view.Board.rectTransform, "sk" + i, i == 0 ? "panel_skin_on" : "panel_skin",
                    new Vector2(xs[i % 2], ys[i / 2]), new Vector2(224f, 124f), Pin.Center);
                var slot = cell.gameObject.AddComponent<HomeSkinCell>();
                slot.Card = cell;
                slot.Button = cell.gameObject.AddComponent<Button>();
                slot.Button.targetGraphic = cell;
                slot.Button.transition = Selectable.Transition.None;
                slot.Gun = Icon(cell.rectTransform, guns[i], new Vector2(0f, 14f), 64f);
                slot.Name = Label(cell.rectTransform, "n", "", 22, new Vector2(0f, -28f), new Vector2(200, 24), Pin.Center);
                slot.Tail = Label(cell.rectTransform, "s", "", 16, new Vector2(0f, -48f), new Vector2(200, 20), Pin.Center);
                skins[i] = slot;
            }
            view.Skins = skins;

            view.GunSummary = null;

            var box = Panel(page, "boosts");
            box.anchorMin = new Vector2(0.5f, 1f);
            box.anchorMax = new Vector2(0.5f, 1f);
            box.pivot = new Vector2(0.5f, 1f);
            box.anchoredPosition = new Vector2(0f, -435f);
            box.sizeDelta = new Vector2(620f, 520f);
            box.GetComponent<Image>().color = Color.clear;
            box.GetComponent<Image>().raycastTarget = false;
            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var rows = new HomeBoostRow[ForgeCatalog.LineCount];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = Pic(box, "fg" + i, "panel_row", Vector2.zero, new Vector2(620f, 92f), Pin.Top);
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 92f;
                var slot = row.gameObject.AddComponent<HomeBoostRow>();
                slot.Row = row;
                slot.Button = row.gameObject.AddComponent<Button>();
                slot.Button.targetGraphic = row;
                slot.Button.transition = Selectable.Transition.None;
                slot.Icon = Icon(row.rectTransform, "ico_damage", new Vector2(-250f, 0f), 56f);
                slot.Title = Label(row.rectTransform, "n", "", 24, new Vector2(-70f, 14f), new Vector2(280, 30), Pin.Center);
                slot.Title.alignment = TextAnchor.MiddleLeft;
                slot.Step = Label(row.rectTransform, "s", "", 18, new Vector2(-70f, -16f), new Vector2(280, 24), Pin.Center);
                slot.Step.alignment = TextAnchor.MiddleLeft;
                slot.PriceBack = Pic(row.rectTransform, "pill", "panel_price", new Vector2(200f, 0f),
                    new Vector2(156f, 44f), Pin.Center);
                slot.PriceBack.raycastTarget = false;
                slot.Price = Label(row.rectTransform, "p", "", 20, new Vector2(200f, 0f), new Vector2(144, 36), Pin.Center);
                slot.Price.alignment = TextAnchor.MiddleCenter;
                UiKit.Bold(slot.Price);
                rows[i] = slot;
            }
            view.Boosts = rows;
        }

        static void BakeSortie(RectTransform page, HomeView view)
        {
            SortiePageBuilder.Build(page, view);
            if (view.GoButton == null)
            {
                view.GoButton = PillBtn(page, "go", "继续  第 1 关", new Vector2(0f, 26f), new Vector2(460f, 106f), true);
                view.GoLabel = view.GoButton.GetComponentInChildren<Text>();
                view.AdButton = PillBtn(page, "adstam", "看广告  +6 体力", new Vector2(0f, 150f), new Vector2(400f, 84f), false);
                view.AdLabel = view.AdButton.GetComponentInChildren<Text>();
                view.AdButton.gameObject.SetActive(false);
            }
        }

        static void BakeSpell(RectTransform page, HomeView view)
        {
            Pic(page, "rod", "panel_spell_rod", new Vector2(0f, 20f), new Vector2(640f, 28f), Pin.Top)
                .raycastTarget = false;
            Pic(page, "seal", "panel_spell_seal", new Vector2(-300f, 8f), new Vector2(52f, 52f), Pin.Top)
                .raycastTarget = false;
            var equipped = new HomeSpellSlot[GameConstants.SpellSlots];
            for (int s = 0; s < equipped.Length; s++)
            {
                var box = Pic(page, "slot" + s, "panel_spell_tag", new Vector2((s - 0.5f) * 300f + 20f, 52f),
                    new Vector2(168f, 254f), Pin.Top);
                box.raycastTarget = false;
                var slot = box.gameObject.AddComponent<HomeSpellSlot>();
                slot.Card = box;
                slot.Icon = Icon(box.rectTransform, "ico_burst", new Vector2(0f, 12f), 56f);
                slot.Name = Label(box.rectTransform, "n", "空槽", 26, new Vector2(0f, -48f), new Vector2(140, 34), Pin.Center);
                slot.Cost = Label(box.rectTransform, "c", "", 18, new Vector2(0f, -84f), new Vector2(140, 26), Pin.Center);
                slot.Cost.color = InkTheme.TextMid;
                equipped[s] = slot;
            }
            view.Equipped = equipped;
            view.SpellTip = null;

            var boxCards = Panel(page, "spells");
            boxCards.anchorMin = new Vector2(0.5f, 0f);
            boxCards.anchorMax = new Vector2(0.5f, 1f);
            boxCards.offsetMin = new Vector2(-290f, 8f);
            boxCards.offsetMax = new Vector2(290f, -330f);
            boxCards.GetComponent<Image>().color = Color.clear;
            boxCards.GetComponent<Image>().raycastTarget = false;
            var grid = boxCards.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(322f, 176f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            var cards = new HomeSpellCard[SpellCatalog.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                var card = Pic(boxCards, "sp" + i, "panel_skin", Vector2.zero, new Vector2(322f, 176f), Pin.Center);
                var slot = card.gameObject.AddComponent<HomeSpellCard>();
                slot.Card = card;
                slot.Button = card.gameObject.AddComponent<Button>();
                slot.Button.targetGraphic = card;
                slot.Button.transition = Selectable.Transition.None;
                slot.Icon = Icon(card.rectTransform, "ico_burst", new Vector2(-114f, 34f), 84f);
                slot.Name = Label(card.rectTransform, "n", "", 28, new Vector2(20f, 46f), new Vector2(170, 36), Pin.Center);
                slot.Name.alignment = TextAnchor.MiddleLeft;
                slot.Desc = Label(card.rectTransform, "d", "", 16, new Vector2(34f, 10f), new Vector2(200, 26), Pin.Center);
                slot.Desc.alignment = TextAnchor.MiddleLeft;
                slot.Desc.color = InkTheme.TextMid;
                slot.Cost = Label(card.rectTransform, "e", "", 18, new Vector2(-78f, -58f), new Vector2(140, 26), Pin.Center);
                slot.Cost.alignment = TextAnchor.MiddleLeft;
                slot.Cost.color = InkTheme.TextMid;
                slot.PriceBack = Pic(card.rectTransform, "pill", "panel_price", new Vector2(84f, -58f),
                    new Vector2(118f, 42f), Pin.Center);
                slot.PriceBack.raycastTarget = false;
                slot.State = Label(card.rectTransform, "s", "", 20, new Vector2(56f, -58f), new Vector2(170, 28), Pin.Center);
                slot.State.alignment = TextAnchor.MiddleRight;
                cards[i] = slot;
            }
            view.Spells = cards;
        }

        static Button[] BakeTabs(Transform root, HomeView view)
        {
            view.TabDock = Pic(root, "tabbar", "tab_dock", new Vector2(0f, 0f),
                new Vector2(720f, UiKit.DockH), Pin.Bottom);
            view.TabDock.raycastTarget = false;
            string[] names = { "炮台", "出征", "技能" };
            string[] icons = { "ico_tab_forge", "ico_tab_sortie", "ico_tab_spell" };
            var tabs = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("tab" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(view.TabDock.transform, false);
                var t = go.GetComponent<RectTransform>();
                t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
                t.anchoredPosition = new Vector2((i - 1) * 180f, 4f);
                t.sizeDelta = new Vector2(172f, 152f);
                go.GetComponent<Image>().color = Color.clear;
                var btn = go.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                Pic(t, "plaque", "tab_plaque", new Vector2(0f, 42f), new Vector2(112f, 112f), Pin.Center);
                Icon(t, icons[i], new Vector2(0f, 42f), 80f);
                Label(t, "t", names[i], 28, new Vector2(0f, -36f), new Vector2(160, 36), Pin.Center);
                tabs[i] = btn;
            }
            return tabs;
        }

        static Button PillBtn(Transform parent, string name, string text, Vector2 pos, Vector2 size, bool primary)
        {
            var img = Pic(parent, name, "panel_row", pos, size, Pin.Bottom);
            if (primary) img.color = InkTheme.Cta;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            var t = Label(img.rectTransform, "t", text, 29, Vector2.zero, size, Pin.Center);
            t.color = primary ? InkTheme.CardFace : InkTheme.TextDark;
            UiKit.Bold(t);
            return btn;
        }

        static RectTransform Page(Transform parent, string name)
        {
            var rt = Panel(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, UiKit.TabBarH);
            rt.offsetMax = new Vector2(0f, -96f);
            rt.GetComponent<Image>().color = Color.clear;
            rt.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        static RectTransform Panel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static Image Pic(Transform parent, string name, string file, Vector2 pos, Vector2 size, Pin pin)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), pos, size, pin);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteAt(file);
            img.type = img.sprite != null && img.sprite.border.sqrMagnitude > 1f ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = img.type == Image.Type.Simple;
            return img;
        }

        static Image Icon(Transform parent, string file, Vector2 pos, float size)
        {
            var go = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteAt(file);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Text Label(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box, Pin pin)
        {
            var t = UiKit.Label(parent, name, text, size, pos, box, TextAnchor.MiddleCenter, pin);
            t.raycastTarget = false;
            return t;
        }

        static Sprite SpriteAt(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Ui + file + ".png");
        }

        // 编辑器重新编译后，出征页还是旧格子时补一次。已经有章节卡就不再动。
        [InitializeOnLoad]
        static class SortiePrefabHook
        {
            static SortiePrefabHook()
            {
                EditorApplication.delayCall += Once;
            }

            static void Once()
            {
                if (!System.IO.File.Exists(Out)) return;
                string text = System.IO.File.ReadAllText(Out);
                if (text.Contains("\n  m_Name: sortie_v6\n") || text.Contains("\r\n  m_Name: sortie_v6\r\n"))
                    return;
                BakeSortieMenu();
            }
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size, Pin pin)
        {
            rt.sizeDelta = size;
            switch (pin)
            {
                case Pin.Top:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(pos.x, -pos.y);
                    break;
                case Pin.Bottom:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(pos.x, pos.y);
                    break;
                default:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = pos;
                    break;
            }
        }
    }
}
