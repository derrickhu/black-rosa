using UnityEngine;

namespace InkLine
{
    // 奖励飞入：图标带「+N」从领取处弹出来，画一道弧落到顶栏、页签或宝箱位，落地冒火花、落点抖一下。
    // 新的领取一律走 Play。新手礼包、新手奖励仍用下面的 Flyer / Fly / Land 自己排时间（入账要等落地）。
    public static class RewardFly
    {
        public struct Piece
        {
            public Sprite Icon;
            public int Count;
            public Color Spark;
            public RectTransform Land;
        }

        static readonly Color GemSpark = InkTheme.Hex("F2A0C8");
        static readonly Color StamSpark = InkTheme.Hex("F5C542");

        public static Piece Ink(RectTransform layer, int n) =>
            Wallet(layer, "ink", "ci", InkSprites.Ui("ink"), n, InkTheme.Track);

        public static Piece Diamond(RectTransform layer, int n) =>
            Wallet(layer, "diamond", "cd", InkSprites.Ui("diamond"), n, GemSpark);

        public static Piece Stamina(RectTransform layer, int n) =>
            Wallet(layer, "stamina", "cs", InkSprites.Ui("stamina"), n, StamSpark);

        public static Piece Token(RectTransform layer, int n) =>
            new Piece { Icon = InkSprites.Ui("goldgain"), Count = n, Spark = InkTheme.Gold, Land = Tab(layer, HomeScreen.TabForge) };

        public static Piece Skin(RectTransform layer, int skin, RectTransform land = null) =>
            new Piece
            {
                Icon = InkSprites.Ui("skin_" + SkinCatalog.Get(skin).Key),
                Count = 1,
                Spark = InkTheme.Seal,
                Land = land != null ? land : Tab(layer, HomeScreen.TabForge)
            };

        public static Piece Chest(RectTransform layer, ChestTier tier, int slot) =>
            new Piece
            {
                Icon = InkSprites.Load("Ui/chest_" + ChestCatalog.Get(tier).Key),
                Count = 1,
                Spark = InkTheme.Gold,
                Land = ChestSlot(layer, slot) ?? Tab(layer, HomeScreen.TabSortie)
            };

        // 从 from 依次弹出，再飞向各自的落点。飞的东西挂在界面层上，面板刷新不会把它清掉。
        public static void Play(RectTransform layer, Vector2 from, Piece[] pieces)
        {
            if (layer == null || pieces == null || pieces.Length == 0) return;
            var go = new GameObject("reward_fly", typeof(RectTransform));
            var host = go.GetComponent<RectTransform>();
            host.SetParent(layer, false);
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
            host.sizeDelta = Vector2.zero;
            var anim = go.AddComponent<UiAnim>();
            int n = pieces.Length;
            var flyers = new RectTransform[n];
            var starts = new Vector2[n];
            anim.At(0.02f, () => AudioBus.Chime());
            for (int i = 0; i < n; i++)
            {
                int k = i;
                starts[k] = from + new Vector2((k - (n - 1) * 0.5f) * 36f, 0f);
                flyers[k] = Flyer(layer, pieces[k].Icon, pieces[k].Count, starts[k]);
                anim.Pop(flyers[k], 0.04f + k * 0.07f, 0.28f, 0f);
                anim.At(0.04f + k * 0.07f, () => UiConfetti.Sparks(layer, starts[k], pieces[k].Spark, 8, 360f));
                Vector2 dest = pieces[k].Land != null ? Local(layer, pieces[k].Land) : starts[k] + new Vector2(0f, 240f);
                float goAt = 0.4f + k * 0.09f;
                anim.At(goAt, () =>
                {
                    if (flyers[k] != null) flyers[k].SetAsLastSibling();
                    Fly(host, flyers[k], starts[k], dest);
                });
                anim.At(goAt + 0.55f, () => Land(layer, flyers[k], pieces[k].Land, pieces[k].Spark));
            }
            anim.At(0.4f + (n - 1) * 0.09f + 0.7f, () =>
            {
                if (go != null) Object.Destroy(go);
            });
        }

        public static RectTransform Flyer(RectTransform layer, string icon, int n, Vector2 pos) =>
            Flyer(layer, InkSprites.Ui(icon), n, pos);

        public static RectTransform Flyer(RectTransform layer, Sprite icon, int n, Vector2 pos)
        {
            var g = ResultKit.Group(layer, "fly", pos, new Vector2(140f, 150f));
            var glow = UiKit.Icon(g, InkFx.SoftDisc(), new Vector2(0f, 18f), 160f);
            glow.color = new Color(1f, 0.9f, 0.7f, 0.55f);
            UiKit.Icon(g, icon, new Vector2(0f, 18f), 104f);
            var t = UiKit.Label(g, "n", "+" + n, 34, new Vector2(0f, -52f), new Vector2(140f, 42f));
            t.color = InkTheme.Seal;
            t.raycastTarget = false;
            UiKit.Bold(t);
            g.SetAsLastSibling();
            g.localScale = Vector3.zero;
            return g;
        }

        public static void Fly(Component host, RectTransform flyer, Vector2 from, Vector2 to)
        {
            UiAnim.On(host).Tween(0f, 0.55f, k =>
            {
                if (flyer == null) return;
                float e = Ease.OutCubic(k);
                Vector2 p = Vector2.Lerp(from, to, e);
                p.y += Mathf.Sin(e * Mathf.PI) * 90f;
                flyer.anchoredPosition = p;
                float s = Mathf.Lerp(1.15f, 0.45f, e);
                flyer.localScale = new Vector3(s, s, 1f);
            });
        }

        public static void Land(RectTransform layer, RectTransform flyer, RectTransform chip, Color spark)
        {
            Vector2 at = flyer != null ? flyer.anchoredPosition : Vector2.zero;
            if (flyer != null) Object.Destroy(flyer.gameObject);
            AudioBus.Pickup();
            UiConfetti.Sparks(layer, at, spark, 10, 420f);
            if (chip == null) return;
            UiAnim.On(chip).Punch(chip, 0f, 0.18f, 0.32f);
        }

        public static RectTransform Chip(RectTransform layer, string a, string b)
        {
            Transform home = layer != null ? layer.Find("Home") : null;
            Transform t = Direct(home, a) ?? Direct(home, b) ?? Direct(layer, a) ?? Direct(layer, b);
            if (t == null) t = Deep(home, a) ?? Deep(home, b);
            return t as RectTransform;
        }

        public static RectTransform Tab(RectTransform layer, int index)
        {
            Transform bar = Named(layer, "tabbar");
            return bar != null ? bar.Find("tab" + index) as RectTransform : null;
        }

        public static RectTransform ChestSlot(RectTransform layer, int slot)
        {
            if (slot < 0) return null;
            Transform row = Named(layer, "chests");
            return row != null ? row.Find("c" + slot) as RectTransform : null;
        }

        // 界面层就是挂着 Canvas 的那一层。面板只拿到自己的节点时用这个往上找。
        public static RectTransform LayerOf(Transform t)
        {
            if (t == null) return null;
            Canvas canvas = t.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : t as RectTransform;
        }

        public static Vector2 Local(RectTransform layer, Transform target)
        {
            if (layer == null || target == null) return Vector2.zero;
            var rt = target as RectTransform;
            Vector3 world = rt != null ? rt.TransformPoint(rt.rect.center) : target.position;
            return layer.InverseTransformPoint(world);
        }

        static Piece Wallet(RectTransform layer, string a, string b, Sprite icon, int n, Color spark) =>
            new Piece { Icon = icon, Count = n, Spark = spark, Land = Chip(layer, a, b) };

        static Transform Direct(Transform root, string name) =>
            root != null && !string.IsNullOrEmpty(name) ? root.Find(name) : null;

        static Transform Named(RectTransform layer, string name)
        {
            if (layer == null || string.IsNullOrEmpty(name)) return null;
            Transform home = layer.Find("Home");
            Transform hit = Deep(home, name);
            if (hit != null) return hit;
            return layer.name == name ? layer : Deep(layer, name);
        }

        static Transform Deep(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name == name) return c;
                Transform hit = Deep(c, name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
