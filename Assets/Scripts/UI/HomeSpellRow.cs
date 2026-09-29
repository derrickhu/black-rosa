using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 技能页一张横卡：左图标和等级，中间名字、释放消耗、当前效果、下一级效果、所需材料，
    // 右边「装备」和「解锁 / 升级」。解锁升级键一直在，缺材料时灰掉，缺的那项标红。
    public sealed class HomeSpellRow
    {
        public const float Width = 660f;
        public const float Height = 212f;

        const string Green = "2E9E55";
        const string Mid = "8E806E";

        public RectTransform Root;
        Vector2 _built;
        Image _icon;
        Image _lvBack;
        Text _lv;
        Text _name;
        Text _cast;
        Image _castGold;
        Text _castTag;
        Text _now;
        Text _next;
        Text _needTag;
        Image _shardIco;
        Text _shard;
        Image _inkIco;
        Text _ink;
        Button _act;
        Button _equip;

        public static HomeSpellRow Ensure(HomeSpellCard slot, RectTransform card)
        {
            if (slot == null || card == null) return null;
            Vector2 size = card.rect.size;
            if (size.x < 200f || size.y < 120f) size = new Vector2(Width, Height);
            var row = slot.Row;
            if (row != null && row.Root != null && (row._built - size).sqrMagnitude < 1f) return row;
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
            row = Build(card, size);
            slot.Row = row;
            return row;
        }

        static HomeSpellRow Build(RectTransform card, Vector2 size)
        {
            var go = new GameObject("row", typeof(RectTransform));
            go.transform.SetParent(card, false);
            var root = go.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = size;
            root.anchoredPosition = Vector2.zero;

            float w = size.x;
            float h = size.y;
            float left = -w * 0.5f;
            float right = w * 0.5f;
            float top = h * 0.5f;
            var r = new HomeSpellRow { Root = root, _built = size };

            float iconX = left + 74f;
            r._icon = UiKit.Icon(root, null, new Vector2(iconX, 24f), 104f);
            var lv = UiKit.Panel(root, "lv", new Vector2(iconX, -48f), new Vector2(96f, 32f), InkTheme.Outline);
            r._lvBack = lv.GetComponent<Image>();
            r._lvBack.sprite = UiSprites.Fill(12);
            r._lvBack.type = Image.Type.Sliced;
            r._lvBack.raycastTarget = false;
            r._lv = UiKit.Label(lv, "t", "", 18, Vector2.zero, new Vector2(92f, 30f));
            r._lv.color = InkTheme.CardFace;
            UiKit.Bold(r._lv);

            float x0 = left + 144f;
            float nameY = top - 42f;
            r._name = UiKit.Label(root, "name", "", 30, new Vector2(x0 + 50f, nameY), new Vector2(100f, 38f), TextAnchor.MiddleLeft);
            UiKit.Bold(r._name);
            r._castTag = UiKit.Label(root, "castTag", "释放消耗", 16, new Vector2(x0 + 142f, nameY - 2f), new Vector2(70f, 26f), TextAnchor.MiddleLeft);
            r._castTag.color = InkTheme.TextMid;
            r._castGold = UiKit.Icon(root, InkSprites.Ui("gold"), new Vector2(x0 + 194f, nameY - 2f), 26f);
            r._cast = UiKit.Label(root, "cast", "", 20, new Vector2(x0 + 236f, nameY - 2f), new Vector2(56f, 28f), TextAnchor.MiddleLeft);
            UiKit.Bold(r._cast);

            float lineW = right - 176f - x0;
            r._now = UiKit.Label(root, "now", "", 19, new Vector2(x0 + lineW * 0.5f, 22f), new Vector2(lineW, 28f), TextAnchor.MiddleLeft);
            r._next = UiKit.Label(root, "next", "", 19, new Vector2(x0 + lineW * 0.5f, -10f), new Vector2(lineW, 28f), TextAnchor.MiddleLeft);

            float needY = -50f;
            r._needTag = UiKit.Label(root, "needTag", "", 16, new Vector2(x0 + 35f, needY), new Vector2(70f, 26f), TextAnchor.MiddleLeft);
            r._needTag.color = InkTheme.TextMid;
            r._shardIco = UiKit.Icon(root, InkSprites.Ui("shard"), new Vector2(x0 + 92f, needY), 32f);
            r._shard = UiKit.Label(root, "shard", "", 20, new Vector2(x0 + 140f, needY), new Vector2(60f, 28f), TextAnchor.MiddleLeft);
            UiKit.Bold(r._shard);
            r._inkIco = UiKit.Icon(root, InkSprites.Ui("ink"), new Vector2(x0 + 188f, needY), 30f);
            r._ink = UiKit.Label(root, "ink", "", 20, new Vector2(x0 + 240f, needY), new Vector2(72f, 28f), TextAnchor.MiddleLeft);
            UiKit.Bold(r._ink);

            float btnX = right - 94f;
            r._equip = UiKit.Btn(root, "equip", "装备", new Vector2(btnX, top - 50f), new Vector2(148f, 46f), () => { }, false);
            SetLabel(r._equip, 22);
            r._act = UiKit.Btn(root, "act", "升级", new Vector2(btnX, -38f), new Vector2(148f, 64f), () => { }, true);
            SetLabel(r._act, 26);
            return r;
        }

        static void SetLabel(Button b, int size)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.fontSize = size;
        }

        public void Bind(MetaProgress meta, int i, Action act, Action equip)
        {
            SpellDef d = SpellCatalog.Get(i);
            int rank = meta.SpellRank(i);
            bool owned = rank > 0;
            bool maxed = rank >= SpellCatalog.MaxLevel;
            bool worn = meta.EquippedSlot(i) >= 0;
            int have = meta.SpellShardCount(i);
            int need = maxed ? 0 : SpellCatalog.NextShards(d, rank);
            int price = maxed ? 0 : SpellCatalog.NextPrice(d, rank);
            bool shardOk = have >= need;
            bool inkOk = meta.Ink >= price;
            bool can = !maxed && shardOk && inkOk;

            _icon.sprite = InkSprites.Ui(d.Id);
            _icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            _lv.text = owned ? "Lv." + rank + "/" + SpellCatalog.MaxLevel : "未解锁";
            _lvBack.color = owned ? InkTheme.Outline : InkTheme.BoneMid;

            _name.text = d.Name;
            _name.color = worn ? InkTheme.Seal : InkTheme.TextDark;
            _cast.text = d.GoldCost.ToString();
            _cast.color = InkTheme.TextDark;

            float shotBase = meta.ShotBase;
            string cur = SpellCatalog.Blurb(d, rank, shotBase);
            _now.text = owned ? Tag("当前", Mid) + cur : Tag("当前", Mid) + "<color=#" + Mid + ">未解锁</color>";
            _now.color = InkTheme.TextDark;
            if (maxed) _next.text = Tag("下一级", Mid) + "<color=#" + Mid + ">已满级</color>";
            else if (!owned) _next.text = Tag("解锁后", Green) + cur;
            else _next.text = Tag("下一级", Green) + Lift(cur, SpellCatalog.Blurb(d, rank + 1, shotBase));
            _next.color = InkTheme.TextDark;

            bool showNeed = !maxed;
            _needTag.text = maxed ? "" : (owned ? "升级需要" : "解锁需要");
            _shardIco.gameObject.SetActive(showNeed);
            _shard.gameObject.SetActive(showNeed);
            _inkIco.gameObject.SetActive(showNeed);
            _ink.gameObject.SetActive(showNeed);
            if (showNeed)
            {
                _shard.text = have + "/" + need;
                _shard.color = shardOk ? InkTheme.TextDark : InkTheme.Rose;
                _ink.text = price.ToString();
                _ink.color = inkOk ? InkTheme.TextDark : InkTheme.Rose;
            }

            SetText(_act, maxed ? "已满级" : (owned ? "升级" : "解锁"));
            _act.interactable = can;
            if (can) UiKit.PaintBtn(_act, InkTheme.Cta, InkTheme.CtaDeep, InkTheme.CardFace);
            else UiKit.PaintBtn(_act, InkTheme.CardDim, InkTheme.LineDim, InkTheme.TextDim);
            _act.onClick.RemoveAllListeners();
            _act.onClick.AddListener(() => act());

            _equip.gameObject.SetActive(owned);
            Transform sh = _equip.transform.parent.Find("equip_sh");
            if (sh != null) sh.gameObject.SetActive(owned);
            SetText(_equip, worn ? "卸下" : "装备");
            if (worn) UiKit.PaintBtn(_equip, InkTheme.Plain, InkTheme.PlainDeep, InkTheme.Seal);
            else UiKit.PaintBtn(_equip, InkTheme.Plain, InkTheme.PlainDeep, InkTheme.TextDark);
            _equip.onClick.RemoveAllListeners();
            _equip.onClick.AddListener(() => equip());
        }

        static void SetText(Button b, string s)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = s;
        }

        static string Tag(string label, string hex) => "<color=#" + hex + ">" + label + "</color>  ";

        static readonly Regex Num = new Regex(@"\d+(\.\d+)?");

        // 下一级里和当前不一样的数字染绿，一眼看出升了什么。
        static string Lift(string cur, string next)
        {
            MatchCollection a = Num.Matches(cur);
            MatchCollection b = Num.Matches(next);
            var sb = new StringBuilder();
            int last = 0;
            for (int k = 0; k < b.Count; k++)
            {
                Match m = b[k];
                sb.Append(next, last, m.Index - last);
                bool same = k < a.Count && a[k].Value == m.Value;
                if (same) sb.Append(m.Value);
                else sb.Append("<color=#").Append(Green).Append(">").Append(m.Value).Append("</color>");
                last = m.Index + m.Length;
            }
            sb.Append(next, last, next.Length - last);
            return sb.ToString();
        }
    }
}
