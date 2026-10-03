using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 道具页：壳由 ItemPageBuilder 烘进预制（缺了就运行时补一份），这里只绑数据。
    // 点卡看详情；栏满了点装备会进换栏模式，可用的格子发光跳动，点哪格换哪格。
    public sealed class HomeItemPage : MonoBehaviour
    {
        MetaProgress _meta;
        RectTransform _layer;
        Action _changed;
        HomeItemView _view;
        Button _cancel;
        int _swap = -1;

        public static HomeItemPage Ensure(RectTransform page, RectTransform layer, MetaProgress meta, Action changed)
        {
            var host = page.GetComponent<HomeItemPage>();
            if (host == null) host = page.gameObject.AddComponent<HomeItemPage>();
            if (host._view == null)
            {
                host._view = page.GetComponentInChildren<HomeItemView>(true);
                if (host._view == null) host._view = ItemPageBuilder.Build(page);
                for (int i = 0; i < page.childCount; i++)
                {
                    Transform c = page.GetChild(i);
                    c.gameObject.SetActive(c == host._view.transform);
                }
            }
            host._meta = meta;
            host._layer = layer;
            host._changed = changed;
            return host;
        }

        void OnDisable() => _swap = -1;

        public void Refresh()
        {
            var anim = UiAnim.On(this);
            anim.Finish();
            anim.Clear();
            BindSlots(anim);
            BindHead();
            BindCards(anim);
        }

        void Changed()
        {
            Refresh();
            _changed?.Invoke();
        }

        // ---------- 道具栏 ----------

        void BindSlots(UiAnim anim)
        {
            for (int s = 0; s < _view.Slots.Length; s++)
            {
                HomeItemSlot slot = _view.Slots[s];
                if (slot == null) continue;
                bool open = _meta.ItemSlotOpen(s);
                int id = open && s < _meta.Equipped.Length ? _meta.Equipped[s] : -1;
                bool picking = _swap >= 0 && open;
                slot.transform.localScale = Vector3.one;

                Soft(slot.Glow);
                slot.Glow.gameObject.SetActive(picking);
                if (picking)
                {
                    slot.Glow.color = new Color(1f, 0.82f, 0.3f, 0.8f);
                    anim.Breathe(slot.Glow.transform, s * 0.15f, 0.1f, 1.6f)
                        .Breathe(slot.transform, s * 0.15f, 0.05f, 1.6f);
                }
                slot.Frame.sprite = Spr(!open ? "item_slot_lock" : picking ? "item_slot_on" : "item_slot");
                slot.Lock.gameObject.SetActive(!open);
                slot.Note.gameObject.SetActive(!open);
                if (!open) slot.Note.text = "通关第" + (GameConstants.ItemSlotChapter[s] + 1) + "章";
                slot.Plus.gameObject.SetActive(open && id < 0);

                bool has = id >= 0;
                slot.Icon.gameObject.SetActive(has);
                slot.Halo.gameObject.SetActive(has);
                slot.Badge.gameObject.SetActive(has);
                if (has)
                {
                    ItemDef d = ItemCatalog.Get(id);
                    Color q = ItemCatalog.QualityColor(d.Quality);
                    slot.Icon.sprite = InkSprites.Ui(d.Id);
                    Soft(slot.Halo);
                    slot.Halo.color = new Color(q.r, q.g, q.b, 0.55f);
                    slot.Lv.text = "Lv." + _meta.ItemRank(id);
                }
                int idx = s;
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.onClick.AddListener(() => TapSlot(idx));
            }
        }

        void TapSlot(int s)
        {
            if (!_meta.ItemSlotOpen(s))
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "通关第" + (GameConstants.ItemSlotChapter[s] + 1) + "章解锁这一格");
                return;
            }
            if (_swap >= 0)
            {
                int item = _swap;
                _swap = -1;
                if (_meta.EquipAt(item, s))
                {
                    AudioBus.Stamp();
                    InkToast.Show(_layer, "换上 " + ItemCatalog.Get(item).Name);
                }
                Changed();
                return;
            }
            int id = _meta.Equipped[s];
            AudioBus.Tap();
            if (id >= 0) ItemPanel.Show(_layer, _meta, id, Changed, BeginSwap);
            else InkToast.Show(_layer, "点下面的道具卡装上");
        }

        void BeginSwap(int item)
        {
            _swap = item;
            Refresh();
        }

        void EndSwap()
        {
            AudioBus.Back();
            _swap = -1;
            Refresh();
        }

        // ---------- 图鉴 ----------

        void BindHead()
        {
            int owned = 0;
            for (int i = 0; i < ItemCatalog.Count; i++)
                if (_meta.ItemRank(i) > 0) owned++;
            bool swapping = _swap >= 0;
            _view.HeadTitle.gameObject.SetActive(!swapping);
            _view.HeadCount.gameObject.SetActive(!swapping);
            _view.HeadCount.text = "已拥有 " + owned + "/" + ItemCatalog.Count;
            _view.SwapHint.gameObject.SetActive(swapping);
            if (swapping) _view.SwapHint.text = "点上面一格，换上 " + ItemCatalog.Get(_swap).Name;
            if (_cancel == null)
            {
                _cancel = UiKit.Btn(_view.SwapCancel, "b", "取消", Vector2.zero, _view.SwapCancel.sizeDelta, EndSwap, false);
                _cancel.GetComponentInChildren<Text>().fontSize = 22;
            }
            _view.SwapCancel.gameObject.SetActive(swapping);
        }

        void BindCards(UiAnim anim)
        {
            int[] order = Order();
            for (int k = 0; k < order.Length && k < _view.Cards.Length; k++)
            {
                HomeItemCard card = _view.Cards[k];
                if (card == null) continue;
                card.transform.SetSiblingIndex(k);
                BindCard(card, order[k], anim);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.List);
        }

        // 已有的在前；同一档里能升级的往前提，再按品质从高到低。
        int[] Order()
        {
            var ids = new int[ItemCatalog.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = i;
            Array.Sort(ids, (a, b) =>
            {
                int ka = Key(a), kb = Key(b);
                return ka != kb ? kb.CompareTo(ka) : a.CompareTo(b);
            });
            return ids;
        }

        int Key(int i)
        {
            int k = (int)ItemCatalog.Get(i).Quality;
            if (_meta.CanUpgradeItem(i, out _)) k += 10;
            if (_meta.ItemRank(i) > 0) k += 100;
            return k;
        }

        static string FrameOf(ItemQuality q) =>
            q == ItemQuality.Purple ? "panel_item_card_purple" : q == ItemQuality.Blue ? "panel_item_card_blue" : "panel_item_card_green";

        void BindCard(HomeItemCard card, int i, UiAnim anim)
        {
            ItemDef d = ItemCatalog.Get(i);
            int rank = _meta.ItemRank(i);
            bool owned = rank > 0;
            bool maxed = rank >= ItemCatalog.MaxLevel;
            bool ready = _meta.CanUpgradeItem(i, out _);
            Color q = ItemCatalog.QualityColor(d.Quality);
            card.transform.localScale = Vector3.one;

            card.Frame.sprite = Spr(owned ? FrameOf(d.Quality) : "panel_item_card_lock");
            card.Quality.text = ItemCatalog.QualityName(d.Quality);
            card.Quality.color = owned ? InkTheme.CardFace : Color.Lerp(q, Color.white, 0.35f);
            Soft(card.Halo);
            card.Halo.gameObject.SetActive(owned);
            card.Halo.color = new Color(q.r, q.g, q.b, 0.4f);
            card.Icon.sprite = InkSprites.Ui(d.Id);
            if (card.CardItem != null) card.CardItem.sprite = InkSprites.Ui(d.Id);
            card.Icon.color = owned ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.8f);
            card.Badge.gameObject.SetActive(owned);
            card.Lv.text = "Lv." + rank;
            if (card.Worn.sprite == null) card.Worn.sprite = Pill(14f);
            card.Worn.type = Image.Type.Sliced;
            card.Worn.gameObject.SetActive(_meta.EquippedSlot(i) >= 0);
            card.Name.text = d.Name;
            card.Name.color = owned ? InkTheme.TextDark : InkTheme.TextMid;

            int have = _meta.ItemCardCount(i);
            int need = maxed ? 1 : ItemCatalog.NextCards(d, rank);
            float k = maxed ? 1f : Mathf.Clamp01(have / (float)Mathf.Max(1, need));
            if (card.Track.sprite == null) card.Track.sprite = Pill(13f);
            card.Track.type = Image.Type.Sliced;
            if (card.Fill.sprite == null) card.Fill.sprite = Pill(13f);
            card.Fill.type = Image.Type.Sliced;
            var fr = card.Fill.rectTransform;
            float w = card.Track.rectTransform.sizeDelta.x;
            float h = card.Track.rectTransform.sizeDelta.y;
            float fw = k > 0f ? Mathf.Max(h, w * k) : 0f;
            fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0f, 0.5f);
            fr.anchoredPosition = Vector2.zero;
            fr.sizeDelta = new Vector2(fw, h);
            card.Fill.gameObject.SetActive(k > 0f);
            card.Fill.color = ready ? InkTheme.Cta : maxed ? InkTheme.Gold : InkTheme.Accel;
            card.BarText.text = maxed ? "已满级" : ready ? (owned ? "可升级!" : "可解锁!") : have + "/" + need;

            if (ready)
                anim.Breathe(card.transform, 0.1f * (i % 3), 0.025f, 1.2f)
                    .Breathe(card.CardIcon.transform, 0f, 0.12f, 2.4f);
            else card.CardIcon.transform.localScale = Vector3.one;

            int idx = i;
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() => TapCard(idx));
        }

        void TapCard(int i)
        {
            AudioBus.Tap();
            if (_swap >= 0)
            {
                _swap = -1;
                Refresh();
            }
            ItemPanel.Show(_layer, _meta, i, Changed, BeginSwap);
        }

        static Sprite Spr(string file) => InkSprites.Load("Ui/" + file);

        static Sprite Pill(float radius) => UiSprites.Fill(UiSprites.Tier(radius));

        static void Soft(Image img)
        {
            if (img != null && img.sprite == null) img.sprite = InkFx.SoftDisc();
        }
    }
}
