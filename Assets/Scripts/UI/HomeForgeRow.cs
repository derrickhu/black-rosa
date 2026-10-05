using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 炮台页一条升级线：图标、名字和等级、生效中标记、「当前 → 下一级」、升级需要的通关数和墨，
    // 右边升级键一直在。已经露面的线永远是亮的，只有缺的那项标红、按钮灰掉。
    public sealed class HomeForgeRow
    {
        public const float Width = 620f;
        public const float Height = 140f;

        const string Green = "2E9E55";

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
            float w = card.rect.width;
            if (w < 300f) w = Width;
            var row = slot.Ui;
            if (row != null && row.Root != null && Mathf.Abs(row._builtW - w) < 1f) return row;
            var baked = card.Find("row") as RectTransform;
            if (baked != null)
            {
                var got = Capture(baked);
                if (got != null)
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
            var go = new GameObject("row", typeof(RectTransform));
            go.transform.SetParent(card, false);
            var root = go.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(w, Height);
            root.anchoredPosition = Vector2.zero;
            float left = -w * 0.5f;
            float right = w * 0.5f;
            var r = new HomeForgeRow { Root = root, _builtW = w };

            r._icon = UiKit.Icon(root, null, new Vector2(left + 58f, 10f), 76f);
            r._icon.gameObject.name = "ico";

            float x0 = left + 112f;
            r._name = UiKit.Label(root, "name", "", 26, new Vector2(x0 + 60f, 42f), new Vector2(120f, 34f), TextAnchor.MiddleLeft);
            UiKit.Bold(r._name);
            r._lv = UiKit.Label(root, "lv", "", 18, new Vector2(x0 + 160f, 40f), new Vector2(80f, 28f), TextAnchor.MiddleLeft);
            r._lv.color = InkTheme.TextMid;
            UiKit.Bold(r._lv);
            r._live = UiKit.Panel(root, "live", new Vector2(x0 + 246f, 41f), new Vector2(76f, 28f), InkTheme.Accel);
            var liveImg = r._live.GetComponent<Image>();
            liveImg.sprite = UiSprites.Fill(12);
            liveImg.type = Image.Type.Sliced;
            liveImg.raycastTarget = false;
            var liveText = UiKit.Label(r._live, "t", "生效中", 16, Vector2.zero, new Vector2(76f, 26f));
            liveText.color = InkTheme.CardFace;
            UiKit.Bold(liveText);

            float lineW = right - 170f - x0;
            r._value = UiKit.Label(root, "value", "", 20, new Vector2(x0 + lineW * 0.5f, 10f), new Vector2(lineW, 30f), TextAnchor.MiddleLeft);

            float needY = -22f;
            r._needTag = UiKit.Label(root, "needTag", "升级需要", 16, new Vector2(x0 + 35f, needY), new Vector2(70f, 26f), TextAnchor.MiddleLeft);
            r._needTag.color = InkTheme.TextMid;
            r._gate = UiKit.Label(root, "gate", "", 18, new Vector2(x0 + 136f, needY), new Vector2(128f, 28f), TextAnchor.MiddleLeft);
            UiKit.Bold(r._gate);
            r._inkIco = UiKit.Icon(root, InkSprites.Ui("ink"), new Vector2(x0 + 214f, needY), 28f);
            r._inkIco.gameObject.name = "inkIco";
            r._ink = UiKit.Label(root, "ink", "", 19, new Vector2(x0 + 262f, needY), new Vector2(64f, 28f), TextAnchor.MiddleLeft);
            UiKit.Bold(r._ink);

            r._act = UiKit.Btn(root, "act", "升级", new Vector2(right - 84f, 8f), new Vector2(136f, 62f), () => { }, true);
            var t = r._act.GetComponentInChildren<Text>();
            if (t != null) t.fontSize = 26;

            // 刚解锁的词条在图标左上角盖一枚「新」，炮台页看过一次就摘。
            r._fresh = Badge(root, "fresh", "新", new Vector2(left + 28f, 46f), new Vector2(48f, 32f), InkTheme.Rose);
            r._fresh.localRotation = Quaternion.Euler(0f, 0f, 12f);
            r._own = Badge(root, "own", "专属", new Vector2(left + 58f, -42f), new Vector2(64f, 28f), InkTheme.Seal);
            return r;
        }

        static RectTransform Badge(RectTransform root, string name, string text, Vector2 pos, Vector2 size, Color fill)
        {
            var b = UiKit.Panel(root, name, pos, size, fill);
            var img = b.GetComponent<Image>();
            img.sprite = UiSprites.Fill(12);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            var t = UiKit.Label(b, "t", text, 18, Vector2.zero, size);
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
            _name.color = InkTheme.TextDark;
            _lv.text = "Lv." + lv + "/" + d.MaxLevel;
            // 专属词条只在装着那款皮肤时算数，换下来就写「未装备」，免得以为还在生效。
            bool worn = !d.Exclusive || meta.Skin == d.Skin;
            _live.gameObject.SetActive(lv > 0);
            _live.GetComponent<Image>().color = worn ? InkTheme.Accel : InkTheme.LineDim;
            _live.GetComponentInChildren<Text>().text = worn ? "生效中" : "未装备";
            FlowTitle();

            string cur = ForgeCatalog.Value(line, lv);
            _value.color = InkTheme.TextDark;
            _value.text = max
                ? d.Stat + " " + cur + "  <color=#8E806E>已满级</color>"
                : d.Stat + " " + cur + "  <color=#" + Green + ">→ " + ForgeCatalog.Value(line, lv + 1) + "</color>";

            _needTag.text = "升级需要";
            _needTag.gameObject.SetActive(!max);
            _gate.gameObject.SetActive(!max && gate > 0);
            _inkIco.gameObject.SetActive(!max);
            _ink.gameObject.SetActive(!max);
            if (!max)
            {
                _gate.text = "通关 " + Mathf.Min(cleared, gate) + "/" + gate + " 关";
                _gate.color = gateOk ? InkTheme.TextDark : InkTheme.Rose;
                _ink.text = cost.ToString();
                _ink.color = inkOk ? InkTheme.TextDark : InkTheme.Rose;
                float inkX = gate > 0 ? 214f : 110f;
                float x0 = -Root.sizeDelta.x * 0.5f + 112f;
                _inkIco.rectTransform.anchoredPosition = new Vector2(x0 + inkX, -22f);
                _ink.rectTransform.anchoredPosition = new Vector2(x0 + inkX + 48f, -22f);
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
            _name.color = InkTheme.TextDark;
            _lv.text = "Lv.0/" + d.MaxLevel;
            _live.gameObject.SetActive(false);
            FlowTitle();
            _value.color = InkTheme.TextDark;
            _value.text = d.Stat + " " + ForgeCatalog.Value(line, 0)
                + "  <color=#" + Green + ">→ " + ForgeCatalog.Value(line, 1) + "</color>";
            _needTag.gameObject.SetActive(true);
            _needTag.text = "拥有" + SkinCatalog.Get(d.Skin).Name + "后可升级";
            _gate.gameObject.SetActive(false);
            _inkIco.gameObject.SetActive(false);
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
            _name.color = InkTheme.TextDim;
            _lv.text = "";
            _live.gameObject.SetActive(false);
            _value.color = InkTheme.TextDim;
            _value.text = d.Stat + " " + ForgeCatalog.Value(line, 1);
            _needTag.gameObject.SetActive(true);
            _needTag.text = "开放需要";
            _gate.gameObject.SetActive(true);
            _gate.text = "通关 " + Mathf.Min(cleared, d.Reveal) + "/" + d.Reveal + " 关";
            _gate.color = InkTheme.Rose;
            _inkIco.gameObject.SetActive(false);
            _ink.gameObject.SetActive(false);
            SetText("未开放");
            Paint(false);
            _act.onClick.RemoveAllListeners();
        }

        // 等级和「生效中」紧跟在名字后面，四个字和两个字的名字都不留大空当。
        void FlowTitle()
        {
            float x0 = -Root.sizeDelta.x * 0.5f + 112f;
            float nameW = Mathf.Clamp(_name.preferredWidth, 20f, 120f);
            float lvLeft = x0 + nameW + 10f;
            _lv.rectTransform.anchoredPosition = new Vector2(lvLeft + 40f, 40f);
            float lvW = Mathf.Clamp(_lv.preferredWidth, 0f, 80f);
            _live.anchoredPosition = new Vector2(lvLeft + lvW + 12f + 38f, 41f);
        }

        void SetText(string s)
        {
            var t = _act.GetComponentInChildren<Text>();
            if (t != null) t.text = s;
        }

        void Paint(bool can)
        {
            _act.interactable = can;
            if (can) UiKit.PaintBtn(_act, InkTheme.Cta, InkTheme.CtaDeep, InkTheme.CardFace);
            else UiKit.PaintBtn(_act, InkTheme.CardDim, InkTheme.LineDim, InkTheme.TextDark);
        }

        static HomeForgeRow Capture(RectTransform row)
        {
            if (row == null) return null;
            var r = new HomeForgeRow
            {
                Root = row,
                _builtW = row.sizeDelta.x > 300f ? row.sizeDelta.x : Width,
                _icon = row.Find("ico")?.GetComponent<Image>(),
                _name = row.Find("name")?.GetComponent<Text>(),
                _lv = row.Find("lv")?.GetComponent<Text>(),
                _live = row.Find("live") as RectTransform,
                _value = row.Find("value")?.GetComponent<Text>(),
                _needTag = row.Find("needTag")?.GetComponent<Text>(),
                _gate = row.Find("gate")?.GetComponent<Text>(),
                _inkIco = row.Find("inkIco")?.GetComponent<Image>(),
                _ink = row.Find("ink")?.GetComponent<Text>(),
                _act = row.Find("act")?.GetComponent<Button>(),
                _fresh = row.Find("fresh") as RectTransform,
                _own = row.Find("own") as RectTransform
            };
            if (r._icon == null || r._name == null || r._lv == null || r._live == null || r._value == null
                || r._needTag == null || r._gate == null || r._inkIco == null || r._ink == null
                || r._act == null || r._fresh == null || r._own == null)
                return null;
            return r;
        }

        void Repair()
        {
            UiKit.RepairSlice(_live.GetComponent<Image>(), UiSprites.Fill(12));
            UiKit.RepairSlice(_fresh.GetComponent<Image>(), UiSprites.Fill(12));
            UiKit.RepairSlice(_own.GetComponent<Image>(), UiSprites.Fill(12));
            if (_inkIco.sprite == null) _inkIco.sprite = InkSprites.Ui("ink");
            UiKit.RepairBtn(_act);
        }
    }
}
