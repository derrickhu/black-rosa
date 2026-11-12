using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 点道具卡弹出的详情。上半是品质色，圆点、圆圈、三条属性和斜名称条；
    // 下面是描述、升级奖励、共用物品框和尖角升级键。装备 / 卸下留在底上，教程还指得到。
    public sealed class ItemPanel : MonoBehaviour
    {
        const float DesignW = 544f;
        const float BoardW = 620f;
        const float BoardH = 900f;
        static float S => BoardW / DesignW;
        static float U(float v) => v * S;

        static readonly Color Cream = InkTheme.Hex("FFFDF1");
        static readonly Color Ink = InkTheme.Hex("0D050A");
        static readonly Color RoseGain = InkTheme.Hex("F1479E");

        MetaProgress _meta;
        int _item;
        Action _changed;
        Action<int> _swap;
        bool _leveled;
        Button _up;
        Button _equip;

        // 新手道具指引用：解锁后先别自动装上，让玩家自己点一次「装备」。
        public static bool HoldEquip;
        public static event Action<ItemPanel> Opened;
        public static event Action<int> UnlockShown;
        public static event Action<int> UnlockDone;
        public static event Action<int> EquipDone;

        public int Item => _item;
        public RectTransform UpRect => _up != null ? _up.transform as RectTransform : null;
        public RectTransform EquipRect => _equip != null ? _equip.transform as RectTransform : null;

        public static void Show(RectTransform layer, MetaProgress meta, int item, Action changed, Action<int> swap,
            bool leveled = false)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "item_panel";
            var panel = dim.gameObject.AddComponent<ItemPanel>();
            panel._meta = meta;
            panel._item = item;
            panel._changed = changed;
            panel._swap = swap;
            panel._leveled = leveled;
            panel.Build(dim);
            Opened?.Invoke(panel);
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Build(RectTransform dim)
        {
            ItemDef d = ItemCatalog.Get(_item);
            int rank = _meta.ItemRank(_item);
            bool owned = rank > 0;
            bool maxed = rank >= ItemCatalog.MaxLevel;
            int shown = Mathf.Max(1, rank);
            ItemCatalog.ItemPeek peek = ItemCatalog.Peek(d, rank, _meta.ShotBase);
            Tone tone = ToneOf(d.Quality);

            var board = UiKit.Stroke(dim, "board", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, 4f,
                Ink, Cream, 22f);
            Stars(board);
            var head = Header(board, tone);
            var art = UiKit.Icon(head, InkSprites.Ui(d.Id), Vector2.zero, U(100f));
            var iconRt = art.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(U(110f), -U(146f));
            if (!owned) art.color = new Color(0.55f, 0.55f, 0.55f, 0.85f);
            UiAnim.On(this).Breathe(art.transform, 0f, 0.03f, 0.7f);

            Stat(head, "冷却", ItemCatalog.CooldownAt(d, shown).ToString("0") + "秒", U(84f), tone.Bar);
            Stat(head, peek.Label, peek.Now, U(130f), tone.Bar);
            Stat(head, "触发", d.When, U(176f), tone.Bar, d.When.Length > 6 ? 16 : 20);

            var plate = UiKit.Art(board, "plate", tone.Plate, new Vector2(U(2f), U(244f)), new Vector2(U(268f), U(52f)), Pin.TopLeft);
            plate.GetComponent<Image>().raycastTarget = false;
            string level = owned ? "Lv." + rank : "未解锁";
            var lv = InkText(plate, "name", d.Name + "    " + level, 26, new Vector2(U(8f), U(4f)),
                new Vector2(U(204f), U(40f)), TextAnchor.MiddleCenter, Pin.TopLeft);
            if (_leveled)
            {
                var glow = UiKit.Icon(head, InkFx.SoftDisc(), Vector2.zero, U(160f));
                glow.color = new Color(tone.Face.r, tone.Face.g, tone.Face.b, 0.45f);
                var glowRt = glow.rectTransform;
                glowRt.anchorMin = glowRt.anchorMax = new Vector2(0f, 1f);
                glowRt.pivot = new Vector2(0.5f, 0.5f);
                glowRt.anchoredPosition = new Vector2(U(110f), -U(146f));
                glow.transform.SetSiblingIndex(art.transform.GetSiblingIndex());
                ItemCelebrate.Upgrade(UiAnim.On(this), (RectTransform)transform.parent, head, art, glow, lv.rectTransform,
                    ItemCatalog.QualityColor(d.Quality));
            }

            var desc = UiKit.Stroke(board, "desc", new Vector2(U(22f), U(326f)), new Vector2(U(500f), U(120f)),
                Pin.TopLeft, 3f, InkTheme.Hex("E4DDD0"), InkTheme.Hex("FFF9F0"), 16f);
            desc.GetComponent<Image>().raycastTarget = false;
            var body = UiKit.Label(desc, "t", d.Desc, 24, Vector2.zero, new Vector2(U(460f), U(96f)), TextAnchor.MiddleLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.color = InkTheme.TextDark;

            string reward = maxed ? "已满级"
                : rank <= 0 ? "解锁奖励：" + peek.Label + "  <color=#F1479E>" + peek.Now + "</color>"
                : "升级奖励：" + peek.Label + "  <color=#F1479E>" + (peek.Gain.Length > 0 ? peek.Gain : peek.Now) + "</color>";
            Reward(board, reward);

            if (!maxed) Cost(board, d, rank);
            if (!maxed)
            {
                bool can = _meta.CanUpgradeItem(_item, out _);
                _up = Hex(board, "up", owned ? "升级" : "解锁", can ? "Ui/btn_hex_green" : "Ui/btn_hex_gray",
                    new Vector2(U(344f), U(590f)), new Vector2(U(176f), U(63f)), Pin.TopLeft,
                    OnUpgrade, InkTheme.Hex("1A1008"), 26, can);
            }
            if (owned)
            {
                bool worn = _meta.EquippedSlot(_item) >= 0;
                const float equipW = 216f;
                float equipH = equipW * 63f / 176f;
                _equip = Hex(board, "equip", worn ? "卸下" : "装备", "Ui/btn_hex_red",
                    new Vector2(0f, U(22f)), new Vector2(U(equipW), U(equipH)), Pin.Bottom,
                    OnEquip, Color.white, 32, false);
            }

            var inset = UiKit.Panel(board, "inset", Vector2.zero, new Vector2(BoardW - U(16f), BoardH - U(16f)), Color.clear);
            var line = inset.GetComponent<Image>();
            line.sprite = UiSprites.Line(UiSprites.Tier(16f), 3);
            line.type = Image.Type.Sliced;
            line.color = InkTheme.Hex("E2DED2");
            line.raycastTarget = false;
            var edge = board.Find("ln");
            if (edge != null) edge.SetAsLastSibling();
            CloseMark(board);
            PanelKit.Open(board);
        }

        struct Tone
        {
            public Color Face;
            public Color Bar;
            public string Plate;
        }

        static Tone ToneOf(ItemQuality q)
        {
            if (q == ItemQuality.Purple)
                return new Tone { Face = InkTheme.Hex("C46EFA"), Bar = InkTheme.Hex("AC51E4"), Plate = "Ui/nameplate_purple" };
            if (q == ItemQuality.Blue)
                return new Tone { Face = InkTheme.Hex("6EBDFA"), Bar = InkTheme.Hex("51A4E4"), Plate = "Ui/nameplate_blue" };
            return new Tone { Face = InkTheme.Hex("C2FA6E"), Bar = InkTheme.Hex("A9E451"), Plate = "Ui/nameplate_green" };
        }

        RectTransform Header(RectTransform board, Tone tone)
        {
            var head = UiKit.Panel(board, "head", new Vector2(U(2f), U(2f)), new Vector2(U(540f), U(242f)), tone.Face, Pin.TopLeft);
            var img = head.GetComponent<Image>();
            img.sprite = UiSprites.FillTop(Mathf.RoundToInt(U(20f)));
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            Dots(head, Shade(tone.Face, 0.18f, 0.72f));
            var disc = UiKit.Icon(head, UiSprites.Disc(), Vector2.zero, U(136f));
            var rt = disc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(U(110f), -U(146f));
            disc.color = Color.Lerp(tone.Face, Color.white, 0.42f);
            return head;
        }

        static void Dots(RectTransform head, Color color)
        {
            float pitch = U(18f);
            float size = U(5.5f);
            for (int r = 0; r < 4; r++)
            for (int c = 0; c < 28; c++)
            {
                float x = U(14f) + c * pitch + (r % 2) * pitch * 0.5f;
                if (x > U(524f)) continue;
                var dot = UiKit.Icon(head, UiSprites.Disc(), Vector2.zero, size);
                var rt = dot.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, -(U(12f) + r * pitch * 0.7f));
                dot.color = color;
            }
        }

        void Stat(RectTransform head, string label, string value, float y, Color bar, int valueSize = 20)
        {
            var row = UiKit.Panel(head, "stat", new Vector2(U(268f), y), new Vector2(U(254f), U(36f)), bar, Pin.TopLeft);
            var img = row.GetComponent<Image>();
            img.sprite = UiSprites.FillExact(10);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            InkText(row, "k", label, 20, new Vector2(U(12f), 0f), new Vector2(U(72f), U(36f)), TextAnchor.MiddleLeft, Pin.TopLeft);
            InkText(row, "v", value, valueSize, new Vector2(U(84f), 0f), new Vector2(U(158f), U(36f)), TextAnchor.MiddleRight, Pin.TopLeft);
        }

        void Reward(RectTransform board, string text)
        {
            var bar = UiKit.Panel(board, "reward", new Vector2(U(22f), U(464f)), new Vector2(U(500f), U(50f)),
                InkTheme.Hex("2A2E34"), Pin.TopLeft);
            var img = bar.GetComponent<Image>();
            img.sprite = UiSprites.FillExact(16);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            for (int i = 0; i < 4; i++)
            {
                Dot(bar, U(22f + i * 16f));
                Dot(bar, U(500f - 26f - i * 16f));
            }
            var t = UiKit.Label(bar, "t", text, 22, Vector2.zero, new Vector2(U(280f), U(50f)));
            t.color = Color.white;
            t.supportRichText = true;
            UiKit.Bold(t);
        }

        static void Dot(RectTransform bar, float x)
        {
            var dot = UiKit.Icon(bar, UiSprites.Disc(), Vector2.zero, U(4f));
            var rt = dot.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            dot.color = new Color(1f, 1f, 1f, 0.22f);
        }

        void Cost(RectTransform board, ItemDef d, int rank)
        {
            int have = _meta.ItemCardCount(_item);
            int need = ItemCatalog.NextCards(d, rank);
            int price = ItemCatalog.NextPrice(d, rank);
            Frame(board, FrameKey(d.Quality), InkSprites.Ui(d.Id), U(28f), have + "/" + need, have >= need);
            if (price <= 0) return;
            Frame(board, "Ui/item_frame_purple", InkSprites.Ui("ink"), U(136f), price.ToString(), _meta.Ink >= price);
        }

        void Frame(RectTransform board, string key, Sprite icon, float x, string count, bool enough)
        {
            var frame = UiKit.Art(board, "frame", key, new Vector2(x, U(546f)), new Vector2(U(96f), U(96f)), Pin.TopLeft);
            frame.GetComponent<Image>().raycastTarget = false;
            UiKit.Icon(frame, icon, Vector2.zero, U(62f));
            var t = UiKit.Label(board, "n", count, 22, new Vector2(x, U(638f)), new Vector2(U(96f), U(36f)),
                TextAnchor.MiddleCenter, Pin.TopLeft);
            t.color = enough ? InkTheme.TextDark : InkTheme.Rose;
            UiKit.Bold(t);
        }

        Button Hex(RectTransform board, string name, string text, string key, Vector2 pos, Vector2 size, Pin pin,
            Action click, Color labelColor, int fontSize, bool breathe)
        {
            Sprite spr = InkSprites.Load(key);
            if (spr == null)
                return UiKit.Btn(board, name, text, pos, size, () => click(), false, pin);
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(board, false);
            var rt = go.GetComponent<RectTransform>();
            bool bottom = pin == Pin.Bottom;
            rt.anchorMin = rt.anchorMax = bottom ? new Vector2(0.5f, 0f) : new Vector2(0f, 1f);
            rt.pivot = bottom ? new Vector2(0.5f, 0f) : new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = bottom ? pos : new Vector2(pos.x, -pos.y);
            var img = go.GetComponent<Image>();
            img.sprite = spr;
            img.preserveAspect = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => click());
            float lift = size.y * (5f / 63f);
            var label = UiKit.Label(go.transform, "t", text, fontSize, new Vector2(0f, lift),
                new Vector2(size.x * 0.72f, size.y * 0.62f));
            label.color = labelColor;
            UiKit.Bold(label);
            if (breathe) UiAnim.On(this).Breathe(go.transform, 0.2f, 0.04f, 1.3f);
            return btn;
        }

        static void Stars(RectTransform board)
        {
            float[,] at =
            {
                { 70f, 400f, 22f }, { 250f, 430f, 16f }, { 470f, 560f, 20f },
                { 90f, 700f, 18f }, { 300f, 760f, 24f }, { 480f, 800f, 16f }, { 180f, 820f, 14f }
            };
            var star = InkArt.Shape(InkShape.Star, Color.white, 64);
            for (int i = 0; i < at.GetLength(0); i++)
            {
                var img = UiKit.Icon(board, star, Vector2.zero, U(at[i, 2]));
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(U(at[i, 0]), -U(at[i, 1]));
                img.color = new Color(0.78f, 0.74f, 0.66f, 0.55f);
            }
        }

        void CloseMark(RectTransform board)
        {
            float s = U(46f);
            var go = new GameObject("close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(board, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(s, s);
            rt.anchoredPosition = new Vector2(-s * 0.5f, -s * 0.5f);
            var ring = go.GetComponent<Image>();
            ring.sprite = UiSprites.Disc();
            ring.color = Ink;
            var face = UiKit.Icon(go.transform, UiSprites.Disc(), new Vector2(0f, U(1.5f)), s - U(4f));
            face.color = InkTheme.Hex("E23B3B");
            var x = UiKit.Label(go.transform, "x", "×", 32, new Vector2(0f, U(2f)), new Vector2(s, s));
            x.color = Color.white;
            UiKit.Bold(x);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = face;
            btn.onClick.AddListener(Close);
        }

        static Text InkText(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box,
            TextAnchor anchor, Pin pin)
        {
            var t = UiKit.Label(parent, name, text, size, pos, box, anchor, pin);
            t.color = Color.white;
            UiKit.Bold(t);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Ink;
            o.effectDistance = new Vector2(1.4f, -1.4f);
            return t;
        }

        static Color Shade(Color c, float satAdd, float valueMul)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(s + satAdd), Mathf.Clamp01(v * valueMul));
        }

        // 方形物品框是一套共用图。品质只决定取哪一张，不按页面再导一份。
        public static string FrameKey(ItemQuality q) =>
            q == ItemQuality.Purple ? "Ui/item_frame_purple"
            : q == ItemQuality.Blue ? "Ui/item_frame_blue"
            : "Ui/item_frame_green";

        public static Image CardIcon(Transform parent, ItemId id, Vector2 pos, float size)
        {
            ItemDef d = ItemCatalog.Get(id);
            var frame = UiKit.Icon(parent, InkSprites.Load(FrameKey(d.Quality)), pos, size);
            UiKit.Icon(frame.transform, InkSprites.Ui(id), Vector2.zero, size * 0.64f);
            return frame;
        }

        void OnEquip()
        {
            AudioBus.Tap();
            RectTransform layer = (RectTransform)transform.parent;
            if (_meta.EquippedSlot(_item) >= 0)
            {
                _meta.Unequip(_item);
                Done();
                return;
            }
            if (_meta.Equip(_item) || EquipOnlySlot(layer))
            {
                int item = _item;
                Done();
                EquipDone?.Invoke(item);
                return;
            }
            Destroy(gameObject);
            if (_swap != null) _swap(_item);
            else InkToast.Show(layer, "道具栏满了，先卸下一个");
        }

        // 只开着一格、里面又有东西：点装备就是换掉它，不用再点格子。
        bool EquipOnlySlot(RectTransform layer)
        {
            if (_meta.ItemSlotsOpen != 1) return false;
            for (int s = 0; s < GameConstants.ItemSlots; s++)
            {
                if (!_meta.ItemSlotOpen(s)) continue;
                bool occupied = _meta.Equipped[s] >= 0;
                if (!_meta.EquipAt(_item, s)) return false;
                if (occupied) InkToast.Show(layer, "换上 " + ItemCatalog.Get(_item).Name);
                return true;
            }
            return false;
        }

        void OnUpgrade()
        {
            RectTransform layer = (RectTransform)transform.parent;
            if (!_meta.CanUpgradeItem(_item, out string why))
            {
                AudioBus.Deny();
                InkToast.Show(layer, why.Contains("/") ? "道具卡不够，开宝箱拿卡" : why);
                return;
            }
            bool fresh = _meta.ItemRank(_item) <= 0;
            if (!_meta.UpgradeItem(_item, !(fresh && HoldEquip))) return;
            _changed?.Invoke();
            Destroy(gameObject);
            int item = _item;
            if (fresh)
            {
                UnlockShown?.Invoke(item);
                ItemCelebrate.Unlock(layer, _meta, item, () => UnlockDone?.Invoke(item));
            }
            else Show(layer, _meta, _item, _changed, _swap, true);
        }

        void Done()
        {
            _changed?.Invoke();
            Destroy(gameObject);
        }
    }
}
