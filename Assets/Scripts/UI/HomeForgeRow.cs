using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 炮台页一条升级线：图标、名字和等级、生效中标记、「当前 → 下一级」、升级需要的通关数和墨，
    // 右边升级键一直在。已经露面的线永远是亮的，只有缺的那项标红、按钮灰掉。
    public sealed class HomeForgeRow
    {
        public const float Width = 658f;
        public const float Height = 150f;

        const string Green = "2E9E55";
        static readonly Color Ink = new Color(0.051f, 0.020f, 0.039f, 1f);
        static readonly Color Cream = new Color(1f, 0.988f, 0.973f, 1f);
        static readonly Color Bar = new Color(1f, 0.914f, 0.659f, 1f);
        static readonly Color Gold = new Color(1f, 0.835f, 0.227f, 1f);

        public RectTransform Root;
        public Button Act => _act;
        float _builtW;
        Image _icon;
        Text _name;
        Text _lv;
        RectTransform _live;
        Text _value;
        Text _needTag;
        Text _gate;
        Image _inkIco;
        Text _ink;
        Button _act;
        RectTransform _fresh;
        RectTransform _own;

        // 烘进预制的那一行。运行时 Ensure 认到同名节点就接着用。
        public static void Install(RectTransform card)
        {
            if (card == null) return;
            float w = card.sizeDelta.x;
            if (w < 300f) w = Width;
            Build(card, w);
        }

        public static HomeForgeRow Ensure(HomeBoostRow slot, RectTransform card)
        {
            if (slot == null || card == null) return null;
            float w = Width;
            var row = slot.Ui;
            if (row != null && row.Root != null && Mathf.Abs(row._builtW - w) < 1f) return row;
            var baked = card.Find("row") as RectTransform;
            if (baked != null)
            {
                var got = Capture(baked);
                if (got != null && Mathf.Abs(got.Root.sizeDelta.y - Height) < 1f && Mathf.Abs(got._builtW - w) < 1f)
                {
                    for (int i = card.childCount - 1; i >= 0; i--)
                    {
                        Transform c = card.GetChild(i);
                        if (c != baked) c.gameObject.SetActive(false);
                    }
                    got.Repair();
                    slot.Ui = got;
                    return got;
                }
            }
            for (int i = card.childCount - 1; i >= 0; i--)
            {
                Transform c = card.GetChild(i);
                if (c.name == "row")
                {
                    c.name = "row_old";
                    UnityEngine.Object.Destroy(c.gameObject);
                }
                else c.gameObject.SetActive(false);
            }
            row = Build(card, w);
            slot.Ui = row;
            return row;
        }

        static HomeForgeRow Build(RectTransform card, float w)
        {
            if (w < Width - 1f) w = Width;
            var go = new GameObject("row", typeof(RectTransform));
            go.transform.SetParent(card, false);
            var root = go.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(w, Height);
            root.anchoredPosition = Vector2.zero;
            var r = new HomeForgeRow { Root = root, _builtW = w };

            var plate = UiKit.Panel(root, "plate", Vector2.zero, new Vector2(w, Height), Cream);
            var face = plate.GetComponent<Image>();
            face.sprite = UiSprites.FillExact(18);
            face.type = Image.Type.Sliced;
            face.raycastTarget = false;
            var mask = plate.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            StretchLine(plate, "ln");

            var bar = UiKit.Panel(plate, "bar", new Vector2(0f, 0f), new Vector2(w, 42f), Bar, Pin.TopLeft);
            var barImg = bar.GetComponent<Image>();
            barImg.sprite = UiSprites.FillTop(16);
            barImg.type = Image.Type.Sliced;
            barImg.raycastTarget = false;

            var ring = UiKit.Icon(plate, UiSprites.Disc(), Vector2.zero, 72f);
            ring.color = Ink;
            ring.raycastTarget = false;
            PinBox(ring.rectTransform, 34f, 48f, 72f);
            var disc = UiKit.Icon(plate, UiSprites.Disc(), Vector2.zero, 68f);
            disc.color = Gold;
            disc.raycastTarget = false;
            PinBox(disc.rectTransform, 36f, 50f, 68f);
            r._icon = UiKit.Icon(plate, null, Vector2.zero, 46f);
            r._icon.gameObject.name = "ico";
            PinBox(r._icon.rectTransform, 47f, 61f, 46f);

            r._name = UiKit.Label(plate, "name", "", 24, new Vector2(18f, 8f), new Vector2(160f, 32f), TextAnchor.MiddleLeft, Pin.TopLeft);
            r._name.color = Ink;
            UiKit.Bold(r._name);
            r._lv = UiKit.Label(plate, "lv", "", 20, new Vector2(76f, 13f), new Vector2(180f, 28f), TextAnchor.MiddleLeft, Pin.TopLeft);
            r._lv.color = Ink;
            UiKit.Bold(r._lv);
            r._lv.rectTransform.pivot = new Vector2(0f, 1f);

            r._live = UiKit.Panel(plate, "live", new Vector2(-400f, 0f), new Vector2(4f, 4f), Color.clear);
            r._live.GetComponent<Image>().raycastTarget = false;
            r._live.gameObject.SetActive(false);

            r._value = UiKit.Label(plate, "value", "", 18, new Vector2(136f, 50f), new Vector2(310f, 28f), TextAnchor.MiddleLeft, Pin.TopLeft);
            r._value.color = Ink;
            UiKit.Bold(r._value);
            r._value.supportRichText = true;

            var slot = UiKit.Panel(plate, "slot", new Vector2(136f, 82f), new Vector2(52f, 52f), Color.white, Pin.TopLeft);
            var slotImg = slot.GetComponent<Image>();
            slotImg.sprite = InkSprites.Load("Ui/item_frame_purple");
            slotImg.preserveAspect = true;
            slotImg.raycastTarget = false;
            r._inkIco = UiKit.Icon(slot, InkSprites.Ui("ink"), Vector2.zero, 34f);
            r._inkIco.gameObject.name = "inkIco";
            PinBox(r._inkIco.rectTransform, 9f, 9f, 34f);
            r._ink = UiKit.Label(plate, "ink", "", 16, new Vector2(196f, 90f), new Vector2(90f, 36f), TextAnchor.MiddleLeft, Pin.TopLeft);
            r._ink.color = Ink;
            UiKit.Bold(r._ink);

            r._needTag = UiKit.Label(plate, "needTag", "", 15, new Vector2(464f, 120f), new Vector2(176f, 24f), TextAnchor.MiddleCenter, Pin.TopLeft);
            r._needTag.color = Ink;
            UiKit.Bold(r._needTag);
            r._gate = UiKit.Label(plate, "gate", "", 14, new Vector2(-400f, 0f), new Vector2(4f, 4f));
            r._gate.gameObject.SetActive(false);

            r._act = UiKit.Btn(plate, "act", "升级", new Vector2(464f, 52f), new Vector2(176f, 63f), () => { }, true, Pin.TopLeft);
            var t = r._act.GetComponentInChildren<Text>();
            if (t != null) t.fontSize = 26;

            r._fresh = Badge(plate, "fresh", "新", new Vector2(22f, 52f), new Vector2(44f, 26f), InkTheme.Rose, Pin.TopLeft);
            r._fresh.localRotation = Quaternion.Euler(0f, 0f, -8f);
            var tag = UiKit.Panel(root, "own", new Vector2(w - 190f, -16f), new Vector2(196f, 69f), Color.white, Pin.TopLeft);
            var tagImg = tag.GetComponent<Image>();
            tagImg.sprite = InkSprites.Load("Ui/tag_exclusive");
            tagImg.preserveAspect = true;
            tagImg.raycastTarget = false;
            tag.gameObject.SetActive(false);
            r._own = tag;
            Transform stroke = plate.Find("ln");
            if (stroke != null) stroke.SetAsLastSibling();
            tag.SetAsLastSibling();
            return r;
        }

        static void PinBox(RectTransform rt, float x, float y, float size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(size, size);
        }

        static void StretchLine(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.LineExact(18, 2);
            img.type = Image.Type.Sliced;
            img.color = Ink;
            img.raycastTarget = false;
        }

        static RectTransform Badge(RectTransform root, string name, string text, Vector2 pos, Vector2 size, Color fill, Pin pin = Pin.Center)
        {
            var b = UiKit.Panel(root, name, pos, size, fill, pin);
            var img = b.GetComponent<Image>();
            img.sprite = UiSprites.Fill(12);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            var t = UiKit.Label(b, "t", text, 16, Vector2.zero, size);
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);
            b.gameObject.SetActive(false);
            return b;
        }

        public void Bind(MetaProgress meta, int line, Action buy, bool fresh = false)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            int lv = meta.ForgeLevel(line);
            bool max = lv >= d.MaxLevel;
            int cleared = meta.ClearedCount();
            int gate = max ? 0 : ForgeCatalog.Gate(line, lv);
            int cost = max ? 0 : ForgeCatalog.Cost(line, lv);
            bool gateOk = cleared >= gate;
            bool inkOk = meta.Wallet(d.Coin) >= cost;
            bool can = !max && gateOk && inkOk && gate <= GameConstants.StageCount;

            _icon.sprite = InkSprites.Ui(d.Icon);
            _icon.color = Color.white;
            _inkIco.sprite = InkSprites.Ui(ForgeCatalog.CoinIcon(d.Coin));
            _fresh.gameObject.SetActive(fresh);
            _own.gameObject.SetActive(d.Exclusive);
            _name.text = d.Name;
            _name.color = Ink;
            bool worn = !d.Exclusive || meta.Skin == d.Skin;
            _lv.text = "Lv." + lv + "/" + d.MaxLevel + (worn ? "" : " 未装备");
            _live.gameObject.SetActive(false);
            FlowTitle();

            string cur = ForgeCatalog.Value(line, lv);
            _value.color = Ink;
            _value.text = max
                ? d.Stat + " " + cur + "  <color=#8E806E>已满级</color>"
                : d.Stat + " " + cur + "  <color=#" + Green + ">→ " + ForgeCatalog.Value(line, lv + 1) + "</color>";

            _needTag.gameObject.SetActive(!max);
            _gate.gameObject.SetActive(false);
            _inkIco.transform.parent.gameObject.SetActive(!max);
            _ink.gameObject.SetActive(!max);
            if (!max)
            {
                _needTag.text = gate > 0
                    ? "升级需要 通关 " + Mathf.Min(cleared, gate) + "/" + gate + " 关"
                    : "升级需要";
                _needTag.color = gateOk ? Ink : InkTheme.Rose;
                _ink.text = cost.ToString();
                _ink.color = inkOk ? Ink : InkTheme.Rose;
            }

            SetText(max ? "已满级" : "升级");
            Paint(can);
            _act.onClick.RemoveAllListeners();
            _act.onClick.AddListener(() => buy());
        }

        // 展台上正看着一款还没买的炮：它的专属词条照常摆出来，只是不能升。开放条件写在展台上，这里不重复。
        public void BindPreview(MetaProgress meta, int line)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            _icon.sprite = InkSprites.Ui(d.Icon);
            _icon.color = Color.white;
            _fresh.gameObject.SetActive(false);
            _own.gameObject.SetActive(true);
            _name.text = d.Name;
            _name.color = Ink;
            _lv.text = "Lv.0/" + d.MaxLevel;
            _live.gameObject.SetActive(false);
            FlowTitle();
            _value.color = Ink;
            _value.text = d.Stat + " " + ForgeCatalog.Value(line, 0)
                + "  <color=#" + Green + ">→ " + ForgeCatalog.Value(line, 1) + "</color>";
            _needTag.gameObject.SetActive(true);
            _needTag.color = Ink;
            _needTag.text = "拥有" + SkinCatalog.Get(d.Skin).Name + "后可升级";
            _gate.gameObject.SetActive(false);
            _inkIco.transform.parent.gameObject.SetActive(false);
            _ink.gameObject.SetActive(false);
            SetText("未拥有");
            Paint(false);
            _act.onClick.RemoveAllListeners();
        }

        // 还没露面的下一条线：只做预告，写清楚通关多少关开放。
        public void BindTease(MetaProgress meta, int line)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            int cleared = meta.ClearedCount();
            _icon.sprite = InkSprites.Ui("lock");
            _icon.color = new Color(1f, 1f, 1f, 0.6f);
            _fresh.gameObject.SetActive(false);
            _own.gameObject.SetActive(false);
            _name.text = d.Name;
            _name.color = Ink;
            _lv.text = "";
            _live.gameObject.SetActive(false);
            _value.color = Ink;
            _value.text = d.Stat + " " + ForgeCatalog.Value(line, 1);
            _needTag.gameObject.SetActive(true);
            _needTag.color = InkTheme.Rose;
            _needTag.text = "通关 " + Mathf.Min(cleared, d.Reveal) + "/" + d.Reveal + " 关开放";
            _gate.gameObject.SetActive(false);
            _inkIco.transform.parent.gameObject.SetActive(false);
            _ink.gameObject.SetActive(false);
            SetText("未开放");
            Paint(false);
            _act.onClick.RemoveAllListeners();
        }

        // 等级和「生效中」紧跟在名字后面，四个字和两个字的名字都不留大空当。
        void FlowTitle()
        {
            if (_name.preferredWidth < 8f) Canvas.ForceUpdateCanvases();
            float nameW = Mathf.Max(24f, _name.preferredWidth);
            var lv = _lv.rectTransform;
            lv.anchorMin = lv.anchorMax = new Vector2(0f, 1f);
            lv.pivot = new Vector2(0f, 1f);
            lv.anchoredPosition = new Vector2(18f + nameW + 10f, -13f);
        }

        void SetText(string s)
        {
            var t = _act.GetComponentInChildren<Text>();
            if (t != null) t.text = s;
        }

        void Paint(bool can)
        {
            _act.interactable = can;
            var rt = _act.transform as RectTransform;
            if (rt != null) rt.sizeDelta = new Vector2(176f, 63f);
            UiKit.PaintHex(_act, can, Ink);
        }

        static HomeForgeRow Capture(RectTransform row)
        {
            if (row == null) return null;
            Transform p = row.Find("plate");
            if (p == null) return null;
            var r = new HomeForgeRow
            {
                Root = row,
                _builtW = row.sizeDelta.x > 300f ? row.sizeDelta.x : Width,
                _icon = p.Find("ico")?.GetComponent<Image>(),
                _name = p.Find("name")?.GetComponent<Text>(),
                _lv = p.Find("lv")?.GetComponent<Text>(),
                _live = p.Find("live") as RectTransform,
                _value = p.Find("value")?.GetComponent<Text>(),
                _needTag = p.Find("needTag")?.GetComponent<Text>(),
                _gate = p.Find("gate")?.GetComponent<Text>(),
                _inkIco = p.Find("slot/inkIco")?.GetComponent<Image>(),
                _ink = p.Find("ink")?.GetComponent<Text>(),
                _act = p.Find("act")?.GetComponent<Button>(),
                _fresh = p.Find("fresh") as RectTransform,
                _own = (row.Find("own") as RectTransform) ?? (p.Find("own") as RectTransform)
            };
            if (r._icon == null || r._name == null || r._lv == null || r._live == null || r._value == null
                || r._needTag == null || r._gate == null || r._inkIco == null || r._ink == null
                || r._act == null || r._fresh == null || r._own == null)
                return null;
            return r;
        }

        void Repair()
        {
            var plate = Root.Find("plate")?.GetComponent<Image>();
            if (plate != null && plate.sprite == null)
            {
                plate.sprite = UiSprites.FillExact(18);
                plate.type = Image.Type.Sliced;
            }
            var bar = Root.Find("plate/bar")?.GetComponent<Image>();
            if (bar != null && bar.sprite == null)
            {
                bar.sprite = UiSprites.FillTop(16);
                bar.type = Image.Type.Sliced;
            }
            var ln = Root.Find("plate/ln")?.GetComponent<Image>();
            if (ln != null && ln.sprite == null)
            {
                ln.sprite = UiSprites.LineExact(18, 2);
                ln.type = Image.Type.Sliced;
            }
            var own = _own != null ? _own.GetComponent<Image>() : null;
            if (own != null && own.sprite == null) own.sprite = InkSprites.Load("Ui/tag_exclusive");
            UiKit.RepairSlice(_fresh.GetComponent<Image>(), UiSprites.Fill(12));
            if (_inkIco.sprite == null) _inkIco.sprite = InkSprites.Ui("ink");
            var slot = Root.Find("plate/slot")?.GetComponent<Image>();
            if (slot != null && slot.sprite == null) slot.sprite = InkSprites.Load("Ui/item_frame_purple");
            UiKit.RepairBtn(_act);
        }
    }
}
