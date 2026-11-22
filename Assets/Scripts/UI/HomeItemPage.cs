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
        static Material _grayIcon;

        static Material GrayIcon
        {
            get
            {
                if (_grayIcon != null) return _grayIcon;
                var shader = Resources.Load<Shader>("Shaders/UiGray");
                if (shader == null) return null;
                _grayIcon = new Material(shader);
                return _grayIcon;
            }
        }

        public static HomeItemPage Ensure(RectTransform page, RectTransform layer, MetaProgress meta, Action changed)
        {
            var host = page.GetComponent<HomeItemPage>();
            if (host == null) host = page.gameObject.AddComponent<HomeItemPage>();
            if (host._view == null)
            {
                host._view = page.GetComponentInChildren<HomeItemView>(true);
                if (host._view != null && host._view.transform.Find(ItemPageBuilder.Mark) == null)
                {
                    Destroy(host._view.gameObject);
                    host._view = null;
                }
                if (host._view == null) host._view = ItemPageBuilder.Build(page);
                ItemPageBuilder.Grow(host._view);
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
            if (_view != null) ItemPageBuilder.PaintPanel(_view.Panel != null ? _view.Panel.GetComponent<Image>() : null);
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
                bool has = id >= 0;
                slot.transform.localScale = Vector3.one;

                if (slot.Glow != null)
                {
                    Soft(slot.Glow);
                    slot.Glow.gameObject.SetActive(picking);
                    if (picking)
                    {
                        slot.Glow.color = new Color(1f, 0.82f, 0.3f, 0.55f);
                        anim.Breathe(slot.transform, s * 0.15f, 0.04f, 1.6f);
                    }
                }
                if (slot.Frame != null)
                    slot.Frame.sprite = Spr(has ? EquipFrame(ItemCatalog.Get(id).Quality) : "item_card_empty");
                if (slot.Lock != null) slot.Lock.gameObject.SetActive(!open);
                if (slot.Plus != null) slot.Plus.gameObject.SetActive(false);
                if (slot.Icon != null)
                {
                    slot.Icon.gameObject.SetActive(has);
                    slot.Icon.color = Color.white;
                    if (has) slot.Icon.sprite = InkSprites.Ui(ItemCatalog.Get(id).Id);
                }
                if (slot.Halo != null) slot.Halo.gameObject.SetActive(false);
                if (slot.Badge != null) slot.Badge.gameObject.SetActive(false);
                if (slot.Lv != null) slot.Lv.gameObject.SetActive(false);
                if (slot.Fill != null) slot.Fill.gameObject.SetActive(false);
                if (slot.BarText != null) slot.BarText.gameObject.SetActive(false);
                if (slot.Note != null)
                {
                    bool caption = !open || has;
                    slot.Note.gameObject.SetActive(caption);
                    if (!open)
                    {
                        slot.Note.fontSize = UiKit.ReadSize(16);
                        slot.Note.text = LockText(s);
                        HomeReskin.TopLeft(slot.Note.rectTransform, 8f, 124f, ItemPageBuilder.CardW - 16f, 36f);
                    }
                    else if (has)
                    {
                        ItemDef worn = ItemCatalog.Get(id);
                        slot.Note.fontSize = UiKit.ReadSize(20);
                        slot.Note.text = worn.Name + " " + _meta.ItemRank(id) + "级";
                        HomeReskin.TopLeft(slot.Note.rectTransform, 4f, ItemPageBuilder.BandY,
                            ItemPageBuilder.CardW - 8f, ItemPageBuilder.BandH);
                    }
                }
                int idx = s;
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.onClick.AddListener(() => TapSlot(idx));
            }
            if (_view.Title != null) _view.Title.text = "装备道具";
        }

        static string EquipFrame(ItemQuality q) =>
            q == ItemQuality.Purple ? "item_card_equip_purple"
            : q == ItemQuality.Blue ? "item_card_equip_blue" : "item_card_equip_green";

        static string LockText(int slot)
        {
            int need = GameConstants.ItemSlotChapter[slot];
            if (need == GameConstants.ItemSlotClosed) return "后续开放";
            return "通关" + (need + 1) + "章";
        }

        static string LockToast(int slot)
        {
            int need = GameConstants.ItemSlotChapter[slot];
            if (need == GameConstants.ItemSlotClosed) return "这一格后续开放";
            return "通关第" + (need + 1) + "章解锁这一格";
        }

        void TapSlot(int s)
        {
            if (!_meta.ItemSlotOpen(s))
            {
                AudioBus.Deny();
                InkToast.Show(_layer, LockToast(s));
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
            if (_view.HeadTitle != null) _view.HeadTitle.gameObject.SetActive(true);
            _view.HeadCount.gameObject.SetActive(!swapping);
            _view.HeadCount.text = "道具总数 <color=#7DD354>" + owned + "/" + ItemCatalog.Count + "</color>";
            _view.SwapHint.gameObject.SetActive(swapping);
            if (swapping) _view.SwapHint.text = "点上面一格，换上 " + ItemCatalog.Get(_swap).Name;
            if (_cancel == null)
            {
                _cancel = UiKit.Btn(_view.SwapCancel, "b", "取消", Vector2.zero, _view.SwapCancel.sizeDelta, EndSwap, false);
                _cancel.GetComponentInChildren<Text>().fontSize = UiKit.ReadSize(22);
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

        // 已解锁的排前面：等级高的在前，同等级按升下一级的进度。
        // 还没解锁的排后面，按攒卡进度，快解锁的在前。进度一样保持图鉴原序。
        int[] Order()
        {
            var ids = new int[ItemCatalog.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = i;
            Array.Sort(ids, (a, b) =>
            {
                int oa = _meta.ItemRank(a) > 0 ? 1 : 0;
                int ob = _meta.ItemRank(b) > 0 ? 1 : 0;
                if (oa != ob) return ob.CompareTo(oa);
                if (oa == 1)
                {
                    int la = _meta.ItemRank(a), lb = _meta.ItemRank(b);
                    if (la != lb) return lb.CompareTo(la);
                }
                int pa = Fill(a), pb = Fill(b);
                return pa != pb ? pb.CompareTo(pa) : a.CompareTo(b);
            });
            return ids;
        }

        // 千分比。卡攒够、或已经满级，都算走完这一档。
        int Fill(int i)
        {
            int rank = _meta.ItemRank(i);
            if (rank >= ItemCatalog.MaxLevel) return 1000;
            int need = ItemCatalog.NextCards(ItemCatalog.Get(i), rank);
            if (need <= 0) return 1000;
            return Mathf.Clamp(_meta.ItemCardCount(i) * 1000 / need, 0, 1000);
        }

        static string FrameOf(ItemQuality q) =>
            q == ItemQuality.Purple ? "item_card_purple" : q == ItemQuality.Blue ? "item_card_blue" : "item_card_green";

        void BindCard(HomeItemCard card, int i, UiAnim anim)
        {
            ItemDef d = ItemCatalog.Get(i);
            int rank = _meta.ItemRank(i);
            bool owned = rank > 0;
            bool ready = _meta.CanUpgradeItem(i, out _);
            card.transform.localScale = Vector3.one;

            if (card.Frame != null)
                card.Frame.sprite = Spr(owned ? FrameOf(d.Quality) : "item_card_gray");
            if (card.Quality != null) card.Quality.gameObject.SetActive(false);
            if (card.Halo != null) card.Halo.gameObject.SetActive(false);
            if (card.Badge != null) card.Badge.gameObject.SetActive(false);
            if (card.Name != null) card.Name.gameObject.SetActive(false);
            if (card.CardIcon != null) card.CardIcon.gameObject.SetActive(false);
            if (card.Icon != null)
            {
                card.Icon.sprite = InkSprites.Ui(d.Id);
                card.Icon.color = Color.white;
                card.Icon.material = owned ? null : GrayIcon;
            }
            if (card.Lv != null)
            {
                card.Lv.gameObject.SetActive(owned);
                card.Lv.text = rank.ToString();
            }
            if (card.Seal == null && card.Disc != null) card.Seal = ItemPageBuilder.LevelLock(card.Disc.rectTransform);
            if (card.Seal != null) card.Seal.gameObject.SetActive(!owned);
            int wornSlot = _meta.EquippedSlot(i);
            if (card.Worn != null)
            {
                if (card.Worn.sprite == null) card.Worn.sprite = Spr("item_badge");
                card.Worn.type = Image.Type.Simple;
                card.Worn.color = Color.white;
                card.Worn.gameObject.SetActive(wornSlot >= 0);
                if (card.WornText != null && wornSlot >= 0) card.WornText.text = "已装";
            }
            if (card.Arrow != null)
            {
                card.Arrow.sprite = Spr(ready ? "item_arrow_green" : "item_arrow_yellow");
                card.Arrow.color = Color.white;
                card.Arrow.preserveAspect = true;
            }
            FitFooter(card);
            PaintBar(card.Fill, card.BarText, i);

            if (ready) anim.Breathe(card.transform, 0.08f * (i % 4), 0.02f, 1.3f);

            int idx = i;
            card.Item = i;
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() => TapCard(idx));
        }

        // 盖住卡面里那条矮黑槽。等级圆、进度字和箭头一起放大，仍收在卡底边之内。
        static void FitFooter(HomeItemCard card)
        {
            if (card == null || card.Frame == null) return;
            var root = card.Frame.rectTransform;
            var track = card.Track;
            if (track == null)
            {
                var go = new GameObject("track", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                track = go.GetComponent<Image>();
                card.Track = track;
            }
            track.gameObject.SetActive(true);
            track.raycastTarget = false;
            track.sprite = Pill(ItemPageBuilder.FootH * 0.5f);
            track.type = Image.Type.Sliced;
            track.color = new Color(0.055f, 0.035f, 0.04f, 1f);
            HomeReskin.TopLeft(track.rectTransform, ItemPageBuilder.FootX, ItemPageBuilder.FootY,
                ItemPageBuilder.FootW, ItemPageBuilder.FootH);
            track.transform.SetAsFirstSibling();

            if (card.Disc != null)
            {
                card.Disc.preserveAspect = true;
                HomeReskin.TopLeft(card.Disc.rectTransform, ItemPageBuilder.FootDiscX, ItemPageBuilder.FootDiscY,
                    ItemPageBuilder.FootDisc, ItemPageBuilder.FootDisc);
            }
            if (card.Lv != null)
            {
                card.Lv.fontSize = ItemPageBuilder.FootLvSize;
                card.Lv.resizeTextForBestFit = false;
                card.Lv.horizontalOverflow = HorizontalWrapMode.Overflow;
                card.Lv.verticalOverflow = VerticalWrapMode.Overflow;
                HomeReskin.TopLeft(card.Lv.rectTransform, ItemPageBuilder.FootDiscX, ItemPageBuilder.FootDiscY,
                    ItemPageBuilder.FootDisc, ItemPageBuilder.FootDisc);
            }
            if (card.Arrow != null)
            {
                card.Arrow.preserveAspect = true;
                HomeReskin.TopLeft(card.Arrow.rectTransform, ItemPageBuilder.FootArrowX, ItemPageBuilder.FootArrowY,
                    ItemPageBuilder.FootArrowW, ItemPageBuilder.FootArrowH);
            }
            if (card.BarText != null)
            {
                card.BarText.fontSize = ItemPageBuilder.FootTextSize;
                card.BarText.resizeTextForBestFit = false;
                card.BarText.horizontalOverflow = HorizontalWrapMode.Overflow;
                card.BarText.verticalOverflow = VerticalWrapMode.Overflow;
                float textX = ItemPageBuilder.FootDiscX + ItemPageBuilder.FootDisc - 4f;
                float textR = ItemPageBuilder.FootArrowX + 6f;
                HomeReskin.TopLeft(card.BarText.rectTransform, textX, ItemPageBuilder.FootY,
                    Mathf.Max(40f, textR - textX), ItemPageBuilder.FootH);
            }
            if (card.Fill != null) card.Fill.transform.SetSiblingIndex(1);
            if (card.Disc != null) card.Disc.transform.SetAsLastSibling();
            if (card.Lv != null) card.Lv.transform.SetAsLastSibling();
            if (card.BarText != null) card.BarText.transform.SetAsLastSibling();
            if (card.Arrow != null) card.Arrow.transform.SetAsLastSibling();
        }

        void PaintBar(Image fill, Text label, int item)
        {
            if (fill == null && label == null) return;
            if (item < 0)
            {
                if (fill != null) fill.gameObject.SetActive(false);
                if (label != null) label.text = "";
                return;
            }
            ItemDef d = ItemCatalog.Get(item);
            int rank = _meta.ItemRank(item);
            bool owned = rank > 0;
            bool maxed = rank >= ItemCatalog.MaxLevel;
            bool ready = _meta.CanUpgradeItem(item, out _);
            int have = _meta.ItemCardCount(item);
            int need = maxed ? 1 : ItemCatalog.NextCards(d, rank);
            float k = maxed ? 1f : Mathf.Clamp01(have / (float)Mathf.Max(1, need));
            if (fill != null)
            {
                float fh = ItemPageBuilder.FootH - 10f;
                if (fill.sprite == null) fill.sprite = Pill(fh * 0.5f);
                fill.type = Image.Type.Sliced;
                fill.color = (maxed || have >= need) ? ItemPageBuilder.FillGreen : ItemPageBuilder.FillYellow;
                float maxW = ItemPageBuilder.FootW - 18f;
                float fw = k > 0f ? Mathf.Clamp(Mathf.Max(fh, maxW * k), 0f, maxW) : 0f;
                HomeReskin.TopLeft(fill.rectTransform, ItemPageBuilder.FootX + 6f, ItemPageBuilder.FootY + 5f, fw, fh);
                fill.gameObject.SetActive(k > 0f);
            }
            if (label != null)
                label.text = maxed ? "已满级" : ready ? (owned ? "可升级" : "可解锁") : have + "/" + Mathf.Max(1, need);
        }

        public RectTransform CardOf(int item)
        {
            if (_view == null || _view.Cards == null) return null;
            for (int k = 0; k < _view.Cards.Length; k++)
                if (_view.Cards[k] != null && _view.Cards[k].Item == item) return _view.Cards[k].transform as RectTransform;
            return null;
        }

        public RectTransform ShelfRect =>
            _view != null && _view.Shelf != null ? _view.Shelf.rectTransform : null;

        public RectTransform SlotRect(int s)
        {
            if (_view == null || _view.Slots == null || s < 0 || s >= _view.Slots.Length || _view.Slots[s] == null)
                return null;
            return _view.Slots[s].transform as RectTransform;
        }

        void TapCard(int i)
        {
            if (_meta.ItemRank(i) <= 0 && !_meta.CanUpgradeItem(i, out _))
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "还没解锁");
                return;
            }
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
