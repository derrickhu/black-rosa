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
        // 皮肤名字在 630–650，「炮台词条」标题牌到 724。列表从这行起，滑上去停在标题下面。
        public const float ListScreenY = 748f;
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
        static Material _gray;

        // 没拿到的炮台：皮肤和六边形框一起去色。锁本身留着原色。
        static Material GrayMat
        {
            get
            {
                if (_gray != null) return _gray;
                var shader = Resources.Load<Shader>("Shaders/UiGray");
                if (shader == null) return null;
                _gray = new Material(shader);
                return _gray;
            }
        }

        static void PaintOwned(Image img, bool owned)
        {
            if (img == null) return;
            img.color = Color.white;
            img.material = owned ? null : GrayMat;
        }

        // 黄签写名字，奶油底写数值。数值用粗体和金币深色，比名字大一档。
        const float BarW = 204f;
        const float BarH = 42f;
        const float BarStep = 48f;
        const float BarTop = 270f;
        const float BarLeft = 16f;
        const float BarRight = 500f;
        const int BarNameSize = 18;
        const int BarValSize = 22;
        static readonly Color ValueColor = new Color(0.745f, 0.463f, 0.118f, 1f);
        static readonly Color OwnValue = new Color(0.420f, 0.157f, 0.620f, 1f);

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
        Text _rankText;
        Text _featText;
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
            for (int i = 0; i < lines; i++)
            {
                var box = SpriteBox(_root, "bar" + i, pill,
                    new Vector2(BarLeft, BarTop), new Vector2(BarW, BarH), Pin.TopLeft);
                box.preserveAspect = false;
                _barName[i] = PillText(box.rectTransform, "n", "");
                _barVal[i] = PillText(box.rectTransform, "v", "");
            }
            FitBars();

            _lockText = UiKit.Label(_root, "need", "", 16, new Vector2(0f, 448f), new Vector2(280f, 28f),
                TextAnchor.MiddleCenter, Pin.Top);
            _lockText.color = Ink;
            UiKit.Bold(_lockText);
            _lockText.gameObject.SetActive(false);

            _act = UiKit.Btn(_root, "act", "", new Vector2(0f, 456f), new Vector2(176f, 63f), OnAct, true, Pin.Top);
            _actText = _act.GetComponentInChildren<Text>();
            if (_actText != null) _actText.fontSize = UiKit.ReadSize(22);
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

            _slotName[i] = UiKit.Label(cell, "name", d.Name, 20, new Vector2(-4f, 100f), new Vector2(SlotW + 8f, 28f),
                TextAnchor.MiddleCenter, Pin.TopLeft);
            StyleSlotName(_slotName[i]);

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

        static Text PillText(RectTransform pill, string name, string text)
        {
            var t = UiKit.Label(pill, name, text, BarNameSize, Vector2.zero, new Vector2(BarW * 0.5f, BarH),
                TextAnchor.MiddleCenter, Pin.TopLeft);
            t.color = Ink;
            return t;
        }

        void FitBars()
        {
            if (_barName == null) return;
            for (int i = 0; i < _barName.Length; i++)
            {
                if (_barName[i] == null) continue;
                var bar = _barName[i].transform.parent as RectTransform;
                if (bar == null) continue;
                bool left = i < 4;
                int row = left ? i : i - 4;
                bar.anchorMin = bar.anchorMax = new Vector2(0f, 1f);
                bar.pivot = new Vector2(0f, 1f);
                bar.sizeDelta = new Vector2(BarW, BarH);
                bar.anchoredPosition = new Vector2(left ? BarLeft : BarRight, -(BarTop + row * BarStep));
                var face = bar.GetComponent<Image>();
                if (face != null) face.preserveAspect = false;
                float half = BarW * 0.5f;
                StylePill(_barName[i], 10f, half - 18f, BarNameSize, Ink, false);
                StylePill(_barVal[i], half + 8f, half - 16f, BarValSize, ValueColor, true);
            }
        }

        static void StylePill(Text t, float x, float w, int size, Color color, bool bold)
        {
            if (t == null) return;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(w, BarH);
            t.font = bold ? UiKit.FontBold : UiKit.Font;
            t.fontStyle = FontStyle.Normal;
            t.resizeTextForBestFit = false;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
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
            FitBars();
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
            PaintOwned(_gun, owned);
            _lockText.gameObject.SetActive(false);
            if (_motion != null)
            {
                _motion.Alpha = _gun.color.a;
                _motion.SlideIn(dir);
                _motion.BaseY = GunRestY;
            }

            BindAct(i, d, owned, shown);
            for (int k = 0; k < _frames.Length; k++) BindSlot(k);
            PaintBlurb(d);
            PaintBars();
            ScrollTo(i);
            if (notify && moved) _focusChanged?.Invoke(i);
        }

        // 等级 0 不占位。专属词条只在装着对应皮肤时出现，胶囊用紫色。
        void PaintBars()
        {
            if (_barName == null || _meta == null) return;
            int n = Mathf.Min(_barName.Length, ForgeCatalog.LineCount);
            int[] live = new int[n];
            int count = 0;
            Sprite own = InkSprites.Load("Ui/stat_pill_own");
            Sprite plain = InkSprites.Load("Ui/stat_pill");
            for (int i = 0; i < _barName.Length; i++)
            {
                var bar = BarOf(i);
                if (i >= n)
                {
                    if (bar != null) bar.gameObject.SetActive(false);
                    continue;
                }
                ForgeDef d = ForgeCatalog.Get(i);
                int lv = _meta.ForgeLevel(i);
                bool on = lv > 0 && (!d.Exclusive || d.Skin == _meta.Skin);
                if (bar != null) bar.gameObject.SetActive(on);
                if (!on || _barName[i] == null || _barVal[i] == null) continue;
                var face = bar.GetComponent<Image>();
                if (face != null)
                {
                    Sprite s = d.Exclusive ? own : plain;
                    if (s != null) face.sprite = s;
                    face.color = Color.white;
                    face.preserveAspect = false;
                }
                _barName[i].text = d.Stat;
                _barName[i].color = Ink;
                _barVal[i].text = ForgeCatalog.Value(i, lv);
                _barVal[i].color = d.Exclusive ? OwnValue : ValueColor;
                live[count++] = i;
            }
            PlaceLive(live, count);
        }

        RectTransform BarOf(int i)
        {
            if (_barName == null || i < 0 || i >= _barName.Length || _barName[i] == null) return null;
            return _barName[i].transform.parent as RectTransform;
        }

        // 生效的词条左右分开，整列对着炮心。底下给皮肤条留空。
        void PlaceLive(int[] live, int count)
        {
            if (count <= 0) return;
            int leftN = count - count / 2;
            int rightN = count - leftN;
            const float gunMid = 402f;
            const float floor = 512f;
            float gap = BarStep - BarH;
            int li = 0, ri = 0;
            for (int k = 0; k < count; k++)
            {
                bool left = k < leftN;
                int row = left ? li++ : ri++;
                int rows = left ? leftN : rightN;
                float h = rows * BarH + (rows - 1) * gap;
                float top = gunMid - h * 0.5f;
                float bottom = top + h;
                if (bottom > floor) top -= bottom - floor;
                var bar = BarOf(live[k]);
                if (bar == null) continue;
                bar.anchorMin = bar.anchorMax = new Vector2(0f, 1f);
                bar.pivot = new Vector2(0f, 1f);
                bar.sizeDelta = new Vector2(BarW, BarH);
                bar.anchoredPosition = new Vector2(left ? BarLeft : BarRight, -(top + row * BarStep));
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
            bool isAdUnlock = !owned && d.Way == SkinWay.Ad;
            if (on) { text = "使用中"; live = false; }
            else if (owned) { text = "装备"; live = true; }
            else if (d.Way == SkinWay.Ad) { text = "免费解锁"; live = true; }
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

            Transform adTr = _act.transform.Find("ad_ico");
            if (isAdUnlock)
            {
                Image adImg = adTr != null ? adTr.GetComponent<Image>()
                    : UiKit.Icon(_act.transform, ResultKit.AdBadge(), Vector2.zero, 28f);
                adImg.gameObject.name = "ad_ico";
                adImg.gameObject.SetActive(true);
                adImg.sprite = ResultKit.AdBadge();
                adImg.rectTransform.anchoredPosition = new Vector2(-46f, 4f);
                _actText.rectTransform.anchoredPosition += new Vector2(14f, 0f);
            }
            else if (adTr != null)
            {
                adTr.gameObject.SetActive(false);
            }
        }

        void BindSlot(int k)
        {
            bool owned = _meta.SkinOwned[k];
            SkinDef dk = SkinCatalog.Get(k);
            _frames[k].sprite = HexSprite(dk.Rarity);
            _faces[k].sprite = InkSprites.Ui("skin_" + dk.Key);
            PaintOwned(_frames[k], owned);
            PaintOwned(_faces[k], owned);
            _locks[k].gameObject.SetActive(!owned);
            if (_worn[k] != null) _worn[k].gameObject.SetActive(owned && _meta.Skin == k);
            _slotName[k].text = dk.Name;
            StyleSlotName(_slotName[k]);
            _faces[k].rectTransform.localScale = Vector3.one * (k == _focus ? 1.04f : 1f);
            var glow = EnsurePick(_frames[k].transform.parent as RectTransform, _frames[k].sprite);
            if (glow != null) glow.gameObject.SetActive(k == _focus);
        }

        static void StyleSlotName(Text t)
        {
            if (t == null) return;
            t.font = UiKit.FontBold;
            t.fontStyle = FontStyle.Normal;
            t.fontSize = 20;
            // 地板是暖桃色。金色在这上面对比大约 1.2，几乎看不见。名字一律用深色。
            t.color = Ink;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(-4f, -100f);
            rt.sizeDelta = new Vector2(SlotW + 8f, 28f);
            var edge = t.GetComponent<Outline>();
            if (edge != null) edge.enabled = false;
        }

        // 选中只露一圈和六边形同形的亮边，不铺大光斑。
        static Image EnsurePick(RectTransform cell, Sprite hex)
        {
            if (cell == null) return null;
            var img = cell.Find("pick")?.GetComponent<Image>();
            if (img == null)
            {
                var go = new GameObject("pick", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(cell, false);
                img = go.GetComponent<Image>();
            }
            img.transform.SetAsFirstSibling();
            var frame = cell.Find("hex") as RectTransform;
            float w = frame != null ? frame.sizeDelta.x : SlotW;
            float h = frame != null ? frame.sizeDelta.y : 110f;
            Vector2 origin = frame != null ? frame.anchoredPosition : Vector2.zero;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w + 12f, h + 14f);
            rt.anchoredPosition = origin + new Vector2(w * 0.5f, -h * 0.5f);
            img.sprite = hex;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 0.92f, 0.42f, 1f);
            if (img.GetComponent<SkinPickGlow>() == null) img.gameObject.AddComponent<SkinPickGlow>();
            return img;
        }

        static readonly Vector4 BlurbTagBorder = new Vector4(32f, 0f, 32f, 0f);

        // 炮上方一行：品质签靠左，黑条只写效果。没有加成就写「无特殊效果」。
        void PaintBlurb(SkinDef d)
        {
            EnsureBlurb();
            if (_featText == null || _rankText == null) return;
            string perk = SkinCatalog.PerkOf(_focus);
            if (string.IsNullOrEmpty(perk)) perk = "无特殊效果";
            _featText.text = perk;
            _rankText.text = SkinCatalog.RankName(d.Rarity);
            var tag = _rankText.transform.parent != null ? _rankText.transform.parent.GetComponent<Image>() : null;
            if (tag != null)
            {
                string tone = d.Rarity == SkinRarity.Rare ? "purple"
                    : d.Rarity == SkinRarity.Advanced ? "blue" : "green";
                Sprite s = ModalKit.Load2x("Ui/glyph_tag_" + tone, BlurbTagBorder);
                if (s != null)
                {
                    tag.sprite = s;
                    tag.type = Image.Type.Sliced;
                }
            }
            LayoutBlurb(_root.Find("blurb") as RectTransform);
        }

        void EnsureBlurb()
        {
            var host = _root.Find("blurb") as RectTransform;
            if (host == null)
            {
                host = UiKit.Panel(_root, "blurb", new Vector2(0f, 198f), new Vector2(720f, 44f), Color.clear, Pin.Top);
                host.GetComponent<Image>().raycastTarget = false;
                host.SetAsLastSibling();

                var tag = ModalKit.Sliced(host, "rank", "Ui/glyph_tag_green", BlurbTagBorder,
                    0f, 3f, 96f, 36f, false);
                _rankText = ModalKit.OutlinedText(tag, "t", "普通", 16, 0f, 0f, 96f, 36f,
                    Color.white, TextAnchor.MiddleCenter, 1.15f);

                var strip = ModalKit.Sliced(host, "strip", "Ui/modal_info_strip", ModalKit.BorderStrip,
                    0f, 0f, 500f, 42f, false);
                _featText = ModalKit.OutlinedText(strip, "t", "", 16, 24f, 0f, 452f, 42f,
                    Color.white, TextAnchor.MiddleCenter, 1.15f);
            }
            else
            {
                _rankText = host.Find("rank/t")?.GetComponent<Text>();
                _featText = host.Find("strip/t")?.GetComponent<Text>();
                Transform note = host.Find("note");
                if (note != null) note.gameObject.SetActive(false);
            }
        }

        // 标签和黑条合成一组，按字数收短，整组对准炮。
        void LayoutBlurb(RectTransform host)
        {
            if (host == null || _featText == null) return;
            host.sizeDelta = new Vector2(720f, 44f);
            // 货币条在高屏上跟着顶栏下移，说明若停在 210 就会贴上去。贴在货币下沿，并停在炮顶之前。
            float y = Mathf.Max(210f, HomeReskin.PlaceY(100f) + 70f);
            host.anchoredPosition = new Vector2(0f, -Mathf.Min(y, 260f));
            const float tagW = 96f, tagH = 36f, stripH = 42f, gap = 10f;
            int size = _featText.fontSize > 0 ? _featText.fontSize : UiKit.ReadSize(16);
            float textW = ModalKit.MeasureText(_featText.text, size);
            float stripW = Mathf.Clamp(textW + 108f, 188f, 440f);
            float x = Mathf.Round((720f - (tagW + gap + stripW)) * 0.5f);
            var tag = host.Find("rank") as RectTransform;
            var strip = host.Find("strip") as RectTransform;
            if (tag != null) ModalKit.TopLeft(tag, x, (stripH - tagH) * 0.5f, tagW, tagH);
            if (strip != null) ModalKit.TopLeft(strip, x + tagW + gap, 0f, stripW, stripH);
            float inner = Mathf.Max(40f, stripW - 96f);
            float inset = Mathf.Round((stripW - inner) * 0.5f);
            if (_featText != null) ModalKit.TopLeft(_featText.rectTransform, inset, 0f, inner, stripH);
            var rank = tag != null ? tag.Find("t") as RectTransform : null;
            if (rank != null) ModalKit.TopLeft(rank, 0f, 0f, tagW, tagH);
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

    // 选中六边形外圈的亮边，只轻轻闪一下，不放大。
    public sealed class SkinPickGlow : MonoBehaviour
    {
        Image _img;
        Color _color;

        void Awake()
        {
            _img = GetComponent<Image>();
            if (_img != null) _color = _img.color;
        }

        void Update()
        {
            transform.localScale = Vector3.one;
            if (_img == null) return;
            float u = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
            Color c = _color;
            c.a = Mathf.Lerp(0.72f, 1f, u);
            _img.color = c;
        }
    }
}
