using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    // 炮台页上半：整张露台底从屏幕顶铺到词条区，正中一门炮，两边是词条数值，底下横滑皮肤。
    // 坐标按 720×1280 的稿，从屏幕顶算。炮台页本身让开了顶栏，用 ScreenToPage 换到页内。
    // 正在用的不放按钮；没装上的，炮和皮肤之间留一颗装备 / 解锁。
    public sealed class SkinShowcase
    {
        public const float W = 720f;
        public const float H = 660f;
        // 词条列表从这行起，上面留出专属标伸出卡片的那一截。
        public const float ListScreenY = 712f;
        public const float PanelScreenY = 660f;

        const float SlotX = 31f;
        const float SlotY = 522f;
        const float SlotW = 96f;
        const float SlotH = 128f;
        const float SlotStep = 108f;
        const float GunW = 162f;
        const float GunH = 168f;
        // 根节点高 660，中心距顶 330。炮心在距顶 402，所以停在中心下方 72。
        const float GunRestY = -72f;

        // 页顶相对整屏下移了多少。设计稿从屏幕顶量，页内坐标要减掉这段。
        public static float ScreenToPage(RectTransform page, float screenY)
        {
            float origin = 96f;
            if (page != null) origin = Mathf.Max(0f, -page.offsetMax.y);
            return screenY - origin;
        }

        static readonly Color Ink = new Color(0.051f, 0.020f, 0.039f, 1f);
        static readonly Color Shadow = new Color(0.16f, 0.12f, 0.10f, 0.88f);
        static readonly Color GoldDeep = new Color(0.878f, 0.627f, 0.125f, 1f);

        static readonly float[] BarX = { 18f, 18f, 18f, 18f, 526f, 526f, 526f, 526f, 526f };
        static readonly float[] BarY = { 278f, 314f, 350f, 386f, 278f, 314f, 350f, 386f, 422f };

        readonly MetaProgress _meta;
        readonly Action<int> _focusChanged;
        readonly Action _changed;
        readonly RectTransform _root;
        Image _gun;
        Text _lockText;
        Button _act;
        Text _actText;
        Image[] _frames;
        Image[] _faces;
        RectTransform[] _locks;
        RectTransform[] _worn;
        Text[] _slotName;
        Text[] _barName;
        Text[] _barVal;
        RectTransform _skinRow;
        SkinStageMotion _motion;
        int _focus;

        public RectTransform Root => _root;
        public int Focus => _focus;

        SkinShowcase(MetaProgress meta, RectTransform root, Action<int> focusChanged, Action changed)
        {
            _meta = meta;
            _root = root;
            _focusChanged = focusChanged;
            _changed = changed;
        }

        public static void Create(RectTransform page, float top)
        {
            var root = MakeRoot(page, top);
            var s = new SkinShowcase(null, root, null, null);
            s.Build(false);
        }

        public static SkinShowcase Ensure(SkinShowcase have, RectTransform page, float top, MetaProgress meta,
            Action<int> focusChanged, Action changed)
        {
            if (have != null && have._root != null && have._root.parent == page) return have;
            var existing = page != null ? page.Find("showcase") as RectTransform : null;
            if (existing != null)
            {
                var baked = new SkinShowcase(meta, existing, focusChanged, changed);
                if (baked.Capture())
                {
                    baked.Repair();
                    baked.Rewire();
                    return baked;
                }
                existing.name = "showcase_old";
                UnityEngine.Object.Destroy(existing.gameObject);
            }
            var root = MakeRoot(page, top);
            var s = new SkinShowcase(meta, root, focusChanged, changed);
            s.Build(true);
            return s;
        }

        static RectTransform MakeRoot(RectTransform page, float top)
        {
            var root = UiKit.Panel(page, "showcase", new Vector2(0f, top), new Vector2(W, H), Color.clear, Pin.Top);
            root.GetComponent<Image>().raycastTarget = false;
            return root;
        }

        void Build(bool motion)
        {
            var upper = SpriteBox(_root, "upper", InkSprites.Load("Ui/turret_upper"),
                Vector2.zero, new Vector2(W, H), Pin.TopLeft);
            upper.preserveAspect = false;

            var swipe = UiKit.Panel(_root, "swipe", new Vector2(0f, 150f), new Vector2(W, 360f),
                new Color(1f, 1f, 1f, 0f), Pin.TopLeft);
            swipe.GetComponent<Image>().raycastTarget = true;
            if (motion) AttachMotion(swipe);

            _gun = Picture(_root, "gun", null, Vector2.zero, new Vector2(GunW, GunH));
            _gun.rectTransform.anchoredPosition = new Vector2(0f, GunRestY);
            if (_motion != null)
            {
                _motion.Gun = _gun.rectTransform;
                _motion.BaseY = GunRestY;
            }

            int lines = ForgeCatalog.LineCount;
            _barName = new Text[lines];
            _barVal = new Text[lines];
            var pill = InkSprites.Load("Ui/stat_pill");
            for (int i = 0; i < lines && i < BarX.Length; i++)
            {
                var box = SpriteBox(_root, "bar" + i, pill,
                    new Vector2(BarX[i], BarY[i]), new Vector2(176f, 28f), Pin.TopLeft);
                box.preserveAspect = false;
                _barName[i] = PillText(box.rectTransform, "n", "", 10f, 66f, TextAnchor.MiddleLeft);
                _barVal[i] = PillText(box.rectTransform, "v", "", 78f, 98f, TextAnchor.MiddleCenter);
            }

            _lockText = UiKit.Label(_root, "need", "", 16, new Vector2(0f, 448f), new Vector2(280f, 28f),
                TextAnchor.MiddleCenter, Pin.Top);
            _lockText.color = Ink;
            UiKit.Bold(_lockText);
            _lockText.gameObject.SetActive(false);

            _act = UiKit.Btn(_root, "act", "", new Vector2(0f, 456f), new Vector2(176f, 63f), OnAct, true, Pin.Top);
            _actText = _act.GetComponentInChildren<Text>();
            if (_actText != null) _actText.fontSize = 22;
            _act.gameObject.SetActive(false);

            BuildSkinBar();
        }

        void BuildSkinBar()
        {
            var bar = UiKit.Panel(_root, "skins", new Vector2(0f, SlotY), new Vector2(W, SlotH),
                new Color(1f, 1f, 1f, 0f), Pin.TopLeft);
            bar.GetComponent<Image>().raycastTarget = true;
            bar.gameObject.AddComponent<RectMask2D>();

            int n = SkinCatalog.Count;
            float contentW = SlotX + Mathf.Max(0, n - 1) * SlotStep + SlotW + 24f;
            var row = UiKit.Panel(bar, "row", Vector2.zero, new Vector2(contentW, SlotH), Color.clear, Pin.TopLeft);
            row.GetComponent<Image>().raycastTarget = false;
            _skinRow = row;

            var scroll = bar.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = bar;
            scroll.content = row;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;

            _frames = new Image[n];
            _faces = new Image[n];
            _locks = new RectTransform[n];
            _worn = new RectTransform[n];
            _slotName = new Text[n];
            for (int i = 0; i < n; i++) MakeSlot(row, i);
        }

        void MakeSlot(RectTransform row, int i)
        {
            SkinDef d = SkinCatalog.Get(i);
            var cell = UiKit.Panel(row, "av" + i, new Vector2(SlotX + i * SlotStep, 0f),
                new Vector2(SlotW, SlotH), Color.clear, Pin.TopLeft);
            cell.GetComponent<Image>().raycastTarget = false;

            var hex = SpriteBox(cell, "hex", HexSprite(d.Rarity),
                new Vector2(0f, 0f), new Vector2(SlotW, 110f), Pin.TopLeft, true);
            hex.preserveAspect = true;
            _frames[i] = hex;

            _faces[i] = Picture(cell, "face", InkSprites.Ui("skin_" + d.Key), new Vector2(0f, 8f), new Vector2(78f, 78f));
            _faces[i].rectTransform.anchorMin = _faces[i].rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _faces[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _faces[i].rectTransform.anchoredPosition = new Vector2(0f, -52f);

            var lk = SpriteBox(cell, "lock", InkSprites.Load("Ui/item_lock"),
                new Vector2(SlotW - 26f, 4f), new Vector2(22f, 22f), Pin.TopLeft);
            lk.preserveAspect = true;
            _locks[i] = lk.rectTransform;

            var worn = SpriteBox(cell, "worn", InkSprites.Load("Ui/item_badge"),
                new Vector2(55f, 2f), new Vector2(39f, 26f), Pin.TopLeft);
            worn.preserveAspect = true;
            var wornText = UiKit.Label(worn.rectTransform, "t", "使用", 12, Vector2.zero, new Vector2(39f, 26f));
            wornText.color = ItemPageBuilder.Paper;
            wornText.alignment = TextAnchor.MiddleCenter;
            UiKit.Bold(wornText);
            var edge = wornText.gameObject.AddComponent<Outline>();
            edge.effectColor = HomeReskin.Ink;
            edge.effectDistance = new Vector2(1.2f, -1.2f);
            worn.gameObject.SetActive(false);
            _worn[i] = worn.rectTransform;

            _slotName[i] = UiKit.Label(cell, "name", d.Name, 14, new Vector2(0f, 108f), new Vector2(SlotW, 20f),
                TextAnchor.MiddleCenter, Pin.TopLeft);
            _slotName[i].color = Ink;
            UiKit.Bold(_slotName[i]);

            var btn = cell.gameObject.AddComponent<Button>();
            btn.targetGraphic = hex;
            btn.transition = Selectable.Transition.None;
            int idx = i;
            btn.onClick.AddListener(() => Show(idx, 0, true));
        }

        static Image Picture(RectTransform parent, string name, Sprite spr, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = spr;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Image SpriteBox(RectTransform parent, string name, Sprite spr, Vector2 pos, Vector2 size, Pin pin, bool raycast = false)
        {
            var rt = UiKit.Panel(parent, name, pos, size, Color.white, pin);
            var img = rt.GetComponent<Image>();
            img.sprite = spr;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            if (spr == null) img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        static Text PillText(RectTransform pill, string name, string text, float x, float w, TextAnchor align)
        {
            var t = UiKit.Label(pill, name, text, 14, new Vector2(x, 0f), new Vector2(w, 28f), align, Pin.TopLeft);
            t.color = Ink;
            UiKit.Bold(t);
            return t;
        }

        static Sprite HexSprite(SkinRarity r)
        {
            string key = r == SkinRarity.Rare ? "Ui/skin_hex_purple"
                : r == SkinRarity.Advanced ? "Ui/skin_hex_blue"
                : "Ui/skin_hex_green";
            return InkSprites.Load(key);
        }

        bool Capture()
        {
            if (_root.Find("upper") == null || _root.Find("skins") == null) return false;
            var swipe = _root.Find("swipe") as RectTransform;
            _motion = swipe != null ? swipe.GetComponent<SkinStageMotion>() : null;
            _gun = _root.Find("gun")?.GetComponent<Image>();
            _lockText = _root.Find("need")?.GetComponent<Text>();
            _act = _root.Find("act")?.GetComponent<Button>();
            _actText = _act != null ? _act.GetComponentInChildren<Text>() : null;
            _skinRow = _root.Find("skins/row") as RectTransform;
            if (_gun == null || _lockText == null || _act == null || _actText == null || _skinRow == null)
                return false;

            int n = SkinCatalog.Count;
            _frames = new Image[n];
            _faces = new Image[n];
            _locks = new RectTransform[n];
            _worn = new RectTransform[n];
            _slotName = new Text[n];
            for (int i = 0; i < n; i++)
            {
                Transform cell = _skinRow.Find("av" + i);
                if (cell == null) return false;
                _frames[i] = cell.Find("hex")?.GetComponent<Image>();
                _faces[i] = cell.Find("face")?.GetComponent<Image>();
                _locks[i] = cell.Find("lock") as RectTransform;
                _worn[i] = cell.Find("worn") as RectTransform;
                _slotName[i] = cell.Find("name")?.GetComponent<Text>();
                if (_frames[i] == null || _faces[i] == null || _locks[i] == null || _worn[i] == null || _slotName[i] == null)
                    return false;
            }
            int lines = ForgeCatalog.LineCount;
            _barName = new Text[lines];
            _barVal = new Text[lines];
            for (int i = 0; i < lines; i++)
            {
                Transform bar = _root.Find("bar" + i);
                if (bar == null) return false;
                _barName[i] = bar.Find("n")?.GetComponent<Text>();
                _barVal[i] = bar.Find("v")?.GetComponent<Text>();
                if (_barName[i] == null || _barVal[i] == null) return false;
            }
            return true;
        }

        void Repair()
        {
            var upper = _root.Find("upper")?.GetComponent<Image>();
            if (upper != null && upper.sprite == null) upper.sprite = InkSprites.Load("Ui/turret_upper");
            UiKit.RepairBtn(_act);
        }

        void AttachMotion(RectTransform swipe)
        {
            if (swipe == null) return;
            _motion = swipe.GetComponent<SkinStageMotion>();
            if (_motion == null) _motion = swipe.gameObject.AddComponent<SkinStageMotion>();
            _motion.Swiped = dir => Step(dir);
            if (_gun != null)
            {
                _motion.Gun = _gun.rectTransform;
                _motion.BaseY = GunRestY;
            }
        }

        void Rewire()
        {
            AttachMotion(_root.Find("swipe") as RectTransform);
            _act.onClick.RemoveAllListeners();
            _act.onClick.AddListener(OnAct);
            for (int i = 0; i < _frames.Length; i++)
            {
                var btn = _frames[i].GetComponentInParent<Button>();
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                int idx = i;
                btn.onClick.AddListener(() => Show(idx, 0, true));
            }
        }

        void Step(int dir)
        {
            int n = SkinCatalog.Count;
            Show(((_focus + dir) % n + n) % n, dir, true);
        }

        public void Bind(int focus)
        {
            _focus = Mathf.Clamp(focus, 0, SkinCatalog.Count - 1);
            Show(_focus, 0, false);
        }

        void Show(int i, int dir, bool notify)
        {
            if (_meta == null) return;
            if (dir == 0 && i != _focus) dir = i > _focus ? 1 : -1;
            bool moved = i != _focus;
            _focus = i;

            SkinDef d = SkinCatalog.Get(i);
            bool owned = _meta.SkinOwned[i];
            bool shard = d.Way == SkinWay.Shard;
            int have = shard ? _meta.SkinShardCount(i) : 0;
            bool shown = owned || _meta.SkinOpened(i) || (shard && have > 0);

            _gun.sprite = InkSprites.CannonSkin(i);
            _gun.color = shown ? Color.white : Shadow;
            _lockText.gameObject.SetActive(false);
            if (_motion != null)
            {
                _motion.Alpha = _gun.color.a;
                _motion.SlideIn(dir);
                _motion.BaseY = GunRestY;
            }

            BindAct(i, d, owned, shown);
            for (int k = 0; k < _frames.Length; k++) BindSlot(k);
            PaintBars();
            ScrollTo(i);
            if (notify && moved) _focusChanged?.Invoke(i);
        }

        void PaintBars()
        {
            if (_barName == null || _meta == null) return;
            int n = Mathf.Min(_barName.Length, ForgeCatalog.LineCount);
            for (int i = 0; i < n; i++)
            {
                ForgeDef d = ForgeCatalog.Get(i);
                _barName[i].text = d.Stat;
                _barVal[i].text = ForgeCatalog.Value(i, _meta.ForgeLevel(i));
                _barVal[i].color = Ink;
            }
        }

        void ScrollTo(int i)
        {
            if (_skinRow == null) return;
            float contentW = _skinRow.sizeDelta.x;
            float max = Mathf.Max(0f, contentW - W);
            float x = Mathf.Clamp(SlotX + i * SlotStep - (W - SlotW) * 0.5f, 0f, max);
            var p = _skinRow.anchoredPosition;
            p.x = -x;
            _skinRow.anchoredPosition = p;
        }

        void BindAct(int i, SkinDef d, bool owned, bool ready)
        {
            bool on = _meta.Skin == i;
            string text;
            bool live;
            if (on) { text = "使用中"; live = false; }
            else if (owned) { text = "装备"; live = true; }
            else if (d.Way == SkinWay.Ad) { text = "看广告解锁"; live = true; }
            else if (d.Way == SkinWay.Check) { text = "签到获得"; live = false; }
            else if (d.Way == SkinWay.Event) { text = "活动获取"; live = false; }
            else if (d.Way == SkinWay.Shard)
            {
                int have = _meta.SkinShardCount(i);
                int need = Mathf.Max(1, d.Shards);
                text = have >= need ? "合成" : "碎片 " + have + "/" + need;
                live = have >= need;
            }
            else if (ready) { text = d.Price + "墨 购买"; live = _meta.CanBuySkin(i, out _); }
            else { text = "未开放"; live = false; }

            bool show = text != "使用中";
            _act.gameObject.SetActive(show);
            if (!show) return;
            _actText.text = text;
            _act.interactable = live;
            var rt = _act.transform as RectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(176f, 63f);
                rt.anchoredPosition = new Vector2(0f, -456f);
            }
            UiKit.PaintHex(_act, live, Ink);
        }

        void BindSlot(int k)
        {
            bool owned = _meta.SkinOwned[k];
            SkinDef dk = SkinCatalog.Get(k);
            _frames[k].sprite = HexSprite(dk.Rarity);
            _faces[k].sprite = InkSprites.Ui("skin_" + dk.Key);
            _faces[k].color = Color.white;
            _locks[k].gameObject.SetActive(!owned);
            if (_worn[k] != null) _worn[k].gameObject.SetActive(owned && _meta.Skin == k);
            _slotName[k].text = dk.Name;
            _slotName[k].color = k == _focus ? GoldDeep : Ink;
            _faces[k].rectTransform.localScale = Vector3.one * (k == _focus ? 1.06f : 1f);
        }

        void FlySkin(int i)
        {
            if (_root == null) return;
            RectTransform layer = RewardFly.LayerOf(_root);
            Vector2 from = RewardFly.Local(layer, _act != null ? _act.transform : _root);
            RectTransform gun = _gun != null ? _gun.rectTransform : null;
            RewardFly.Play(layer, from, new[] { RewardFly.Skin(layer, i, gun) });
        }

        void OnAct()
        {
            int i = _focus;
            if (_meta.SkinOwned[i])
            {
                _meta.EquipSkin(i);
                _changed?.Invoke();
                return;
            }
            if (SkinCatalog.Get(i).Way == SkinWay.Ad)
            {
                AdStub.Reward("skin", () =>
                {
                    if (!_meta.GrantSkin(i)) return;
                    FlySkin(i);
                    if (_root != null) _changed?.Invoke();
                });
                return;
            }
            if (SkinCatalog.Get(i).Way == SkinWay.Shard && _meta.TryCraftSkin(i))
            {
                FlySkin(i);
                if (_root != null) _changed?.Invoke();
                return;
            }
            if (_meta.BuySkin(i))
            {
                FlySkin(i);
                _changed?.Invoke();
            }
        }
    }

    // 展台上的炮：切换时从划来的方向滑进来，平时轻轻上下浮。
    public sealed class SkinStageMotion : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Gun;
        public float BaseY;
        public Action<int> Swiped;

        const float Slide = 0.22f;
        float _from;
        float _t = 1f;
        float _dragX;

        public void SlideIn(int dir)
        {
            if (dir == 0) return;
            _from = dir * 150f;
            _t = 0f;
        }

        void Update()
        {
            if (Gun == null) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Slide);
            float k = 1f - Mathf.Pow(1f - _t, 3f);
            float x = _from * (1f - k) + _dragX * 0.35f;
            float y = BaseY + Mathf.Sin(Time.unscaledTime * 2.2f) * 4f;
            Gun.anchoredPosition = new Vector2(x, y);
            if (_img == null) _img = Gun.GetComponent<Image>();
            if (_img != null)
            {
                Color c = _img.color;
                c.a = Alpha * Mathf.Clamp01(_t * 1.6f);
                _img.color = c;
            }
        }

        Image _img;
        public float Alpha = 1f;

        public void OnBeginDrag(PointerEventData e) => _dragX = 0f;

        public void OnDrag(PointerEventData e)
        {
            var canvas = GetComponentInParent<Canvas>();
            float s = canvas != null ? canvas.scaleFactor : 1f;
            _dragX += e.delta.x / Mathf.Max(0.01f, s);
        }

        public void OnEndDrag(PointerEventData e)
        {
            float dx = _dragX;
            _dragX = 0f;
            if (Mathf.Abs(dx) < 60f) return;
            Swiped?.Invoke(dx < 0f ? 1 : -1);
        }
    }
}
