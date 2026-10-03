using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 点道具卡弹出的详情：大图、品质、等级、冷却、什么时候自己丢、这一级和下一级效果，
    // 底下「装备 / 卸下」和「解锁 / 升级」。栏满了点装备，交给道具页进入换栏。
    public sealed class ItemPanel : MonoBehaviour
    {
        const float BoardW = 620f;
        const float BoardH = 820f;
        const string Green = "2E9E55";
        const string Mid = "8E806E";

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
            Color q = ItemCatalog.QualityColor(d.Quality);
            var board = PanelKit.Board(dim, d.Name, Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);

            var stage = UiKit.Stroke(board, "stage", new Vector2(0f, 96f), new Vector2(BoardW - 60f, 230f), Pin.Top, 4f,
                ItemCatalog.QualityDeep(d.Quality), Color.Lerp(q, Color.white, 0.78f), 24f);
            stage.GetComponent<Image>().raycastTarget = false;
            var glow = UiKit.Icon(stage, InkFx.SoftDisc(), new Vector2(0f, 6f), 300f);
            glow.color = new Color(q.r, q.g, q.b, 0.45f);
            var art = UiKit.Icon(stage, InkSprites.Ui(d.Id), new Vector2(0f, 6f), 170f);
            if (!owned) art.color = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            UiAnim.On(this).Breathe(art.transform, 0f, 0.03f, 0.7f);

            var ql = Pill(stage, ItemCatalog.QualityName(d.Quality), q, new Vector2(-(BoardW - 60f) * 0.5f + 62f, 88f), 92f);
            ql.color = InkTheme.CardFace;
            var lv = Pill(stage, owned ? $"Lv.{rank}/{ItemCatalog.MaxLevel}" : "未解锁",
                owned ? InkTheme.Outline : InkTheme.BoneMid, new Vector2((BoardW - 60f) * 0.5f - 70f, 88f), 112f);
            lv.color = InkTheme.CardFace;
            if (_leveled)
                ItemCelebrate.Upgrade(UiAnim.On(this), (RectTransform)transform.parent, stage, art, glow,
                    (RectTransform)lv.transform.parent, q);

            float y = 346f;
            Line(board, Tag("冷却", Mid) + $"{ItemCatalog.CooldownAt(d, Mathf.Max(1, rank)):0.#} 秒", ref y);
            Line(board, Tag("触发", Mid) + d.When, ref y);
            string cur = ItemCatalog.Blurb(d, Mathf.Max(1, rank), _meta.ShotBase);
            Line(board, Tag(owned ? "当前" : "解锁后", owned ? Mid : Green) + cur, ref y);
            if (owned && !maxed)
                Line(board, Tag("下一级", Green) + Lift(cur, ItemCatalog.Blurb(d, rank + 1, _meta.ShotBase)), ref y);

            if (!maxed) BuildCost(board, d, rank, y + 14f);
            else
            {
                var done = UiKit.Label(board, "max", "已满级", 28, new Vector2(0f, y + 30f), new Vector2(300f, 40f),
                    TextAnchor.MiddleCenter, Pin.Top);
                done.color = InkTheme.Accel;
                UiKit.Bold(done);
            }
            BuildButtons(board, rank, maxed);
        }

        // 道具卡的小图：竖卡垫底，道具图盖住中间的星，边框还露着，才看得出是这件道具的卡。
        public const float CardInset = 0.6f;

        public static Image CardIcon(Transform parent, ItemId id, Vector2 pos, float size)
        {
            var card = UiKit.Icon(parent, InkSprites.Ui("card"), pos, size);
            UiKit.Icon(card.transform, InkSprites.Ui(id), Vector2.zero, size * 0.5f);
            return card;
        }

        static Text Pill(Transform parent, string text, Color fill, Vector2 pos, float w)
        {
            var p = UiKit.Panel(parent, "pill", pos, new Vector2(w, 34f), fill);
            var img = p.GetComponent<Image>();
            img.sprite = UiSprites.Fill(UiSprites.Tier(17f));
            img.color = fill;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            var t = UiKit.Label(p, "t", text, 20, Vector2.zero, new Vector2(w, 34f));
            UiKit.Bold(t);
            return t;
        }

        void Line(RectTransform board, string text, ref float y)
        {
            var t = UiKit.Label(board, "l", text, 24, new Vector2(0f, y), new Vector2(BoardW - 80f, 34f),
                TextAnchor.MiddleLeft, Pin.Top);
            t.supportRichText = true;
            t.color = InkTheme.TextDark;
            y += 42f;
        }

        void BuildCost(RectTransform board, ItemDef d, int rank, float y)
        {
            int have = _meta.ItemCardCount(_item);
            int need = ItemCatalog.NextCards(d, rank);
            int price = ItemCatalog.NextPrice(d, rank);
            var row = UiKit.Panel(board, "cost", new Vector2(0f, y), new Vector2(BoardW - 80f, 88f), Color.clear, Pin.Top);
            row.GetComponent<Image>().raycastTarget = false;
            var head = UiKit.Label(row, "need", rank <= 0 ? "解锁需要" : "升级需要", 22, new Vector2(-190f, 0f),
                new Vector2(140f, 34f), TextAnchor.MiddleLeft);
            head.color = InkTheme.TextMid;
            CardIcon(row, d.Id, new Vector2(-82f, 0f), 80f);
            var cards = UiKit.Label(row, "cards", have + "/" + need, 26, new Vector2(4f, 0f), new Vector2(90f, 34f),
                TextAnchor.MiddleLeft);
            cards.color = have >= need ? InkTheme.TextDark : InkTheme.Rose;
            UiKit.Bold(cards);
            if (price <= 0) return;
            UiKit.Icon(row, InkSprites.Ui("ink"), new Vector2(100f, 0f), 40f);
            var ink = UiKit.Label(row, "ink", price.ToString(), 26, new Vector2(164f, 0f), new Vector2(90f, 34f),
                TextAnchor.MiddleLeft);
            ink.color = _meta.Ink >= price ? InkTheme.TextDark : InkTheme.Rose;
            UiKit.Bold(ink);
        }

        void BuildButtons(RectTransform board, int rank, bool maxed)
        {
            bool owned = rank > 0;
            bool worn = _meta.EquippedSlot(_item) >= 0;
            var size = new Vector2(256f, 96f);
            if (owned)
            {
                var eq = UiKit.Btn(board, "equip", worn ? "卸下" : "装备", new Vector2(-138f, 48f), size, OnEquip, false, Pin.Bottom);
                if (worn) UiKit.PaintBtn(eq, InkTheme.Plain, InkTheme.PlainDeep, InkTheme.Seal);
                _equip = eq;
            }
            if (maxed) return;
            bool can = _meta.CanUpgradeItem(_item, out _);
            var up = UiKit.Btn(board, "up", owned ? "升级" : "解锁", new Vector2(owned ? 138f : 0f, 48f), size, OnUpgrade, true, Pin.Bottom);
            _up = up;
            if (!can) PanelKit.Dim(up, true);
            else UiAnim.On(this).Breathe(up.transform, 0.2f, 0.04f, 1.3f);
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
            if (_meta.Equip(_item))
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
