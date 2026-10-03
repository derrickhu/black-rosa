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

            // 运行时由 SkinShowcase 接管并藏掉，这里只留旧的四格占位，HomeScreen 靠它认出预制体。
            var skins = new HomeSkinCell[4];
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
            view.Equipped = null;
            view.Spells = null;
            view.SpellTip = null;
            ItemPageBuilder.Build(page);
        }

        [MenuItem("墨字防线/重排道具页")]
        public static void BakeItemsMenu()
        {
            AssetDatabase.ImportAsset("Assets/Resources/Art/Ui",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            SortiePageBuilder.SpriteOf = file =>
                AssetDatabase.LoadAssetAtPath<Sprite>(Ui + file + ".png");
            try { BakeItemsOnly(); }
            finally { SortiePageBuilder.SpriteOf = null; }
        }

        // 只换道具页。炮台和出征页留着，避免整份重烘盖掉微调。
        public static void BakeItemsOnly()
        {
            if (!System.IO.File.Exists(Out)) return;
            var root = PrefabUtility.LoadPrefabContents(Out);
            var view = root.GetComponent<HomeView>();
            var page = view.SpellPage.GetComponent<RectTransform>();
            for (int i = page.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(page.GetChild(i).gameObject);
            BakeSpell(page, view);
            PrefabUtility.SaveAsPrefabAsset(root, Out);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log("道具页已写入 " + Out + "。炮台和出征页没动。");
        }

        static Button[] BakeTabs(Transform root, HomeView view)
        {
            view.TabDock = Pic(root, "tabbar", "tab_dock", new Vector2(0f, 0f),
                new Vector2(720f, UiKit.DockH), Pin.Bottom);
            view.TabDock.raycastTarget = false;
            string[] names = { "炮台", "出征", "道具" };
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

        // 道具页换成道具栏 + 图鉴之后，旧预制里还是挂签版，编译后补一次。
        [InitializeOnLoad]
        static class ItemPrefabHook
        {
            static ItemPrefabHook()
            {
                EditorApplication.delayCall += Once;
            }

            static void Once()
            {
                if (!System.IO.File.Exists(Out)) return;
                string text = System.IO.File.ReadAllText(Out);
                if (text.Contains("m_Name: " + ItemPageBuilder.Mark + "\n") || text.Contains("m_Name: " + ItemPageBuilder.Mark + "\r\n"))
                    return;
                BakeItemsMenu();
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
