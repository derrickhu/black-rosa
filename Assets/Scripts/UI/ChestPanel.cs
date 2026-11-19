using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 点首页宝箱位弹出来的详情：箱里大概有什么、还要等多久，
    // 等不及就看广告或花钻石当场开。开好了的箱子点进来直接是「打开」。
    public sealed class ChestPanel : MonoBehaviour
    {
        const float BoardW = 600f;
        const float BoardH = 700f;

        MetaProgress _meta;
        int _slot;
        Action _changed;
        Text _state;
        Button _open;
        Button _ad;
        Button _gem;
        Text _gemCost;
        ChestState _last = (ChestState)(-1);
        int _lastSec = -1;
        bool _free;

        // 新手指引：加速不要钱也不看广告，只剩一颗「免费加速」，加速完不直接开，等玩家自己点「打开」。
        public RectTransform SpeedRect => _ad != null ? _ad.transform as RectTransform : null;
        public RectTransform OpenRect => _open != null ? _open.transform as RectTransform : null;
        public RectTransform StateRect => _state != null ? _state.rectTransform : null;
        public event Action Sped;
        public event Action Opening;
        public event Action Revealed;

        public static ChestPanel Show(RectTransform layer, MetaProgress meta, int slot, Action changed, bool free = false)
        {
            if (meta == null || meta.ChestStateOf(slot) == ChestState.Empty) return null;
            var dim = ModalKit.Stage(layer, "chest_panel", 0.8f, null, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<ChestPanel>();
            panel._meta = meta;
            panel._slot = slot;
            panel._changed = changed;
            panel._free = free;
            panel.Build(stage);
            return panel;
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        struct Drop
        {
            public string Tone, Caption, Count;
            public Sprite Icon;
            public bool Ask;
            public Drop(string tone, string caption, string count, Sprite icon, bool ask)
            {
                Tone = tone;
                Caption = caption;
                Count = count;
                Icon = icon;
                Ask = ask;
            }
        }

        void Build(RectTransform stage)
        {
            ChestDef d = ChestCatalog.Get(_meta.ChestTierOf(_slot));
            bool foil = _meta.StudyOpen;
            bool seal = _meta.SealDropOpen && d.Tier != ChestTier.Wood;
            string extra = d.PurpleChance >= 1f ? "必出稀有道具卡"
                : d.PurpleChance > 0f ? "有机会出稀有道具卡"
                : d.Tier == ChestTier.Silver ? "含高级道具卡" : "普通道具卡为主";

            var board = ModalKit.Shell(stage, BoardW, BoardH, d.Name, null, extra, null, null, Close, out _, out _);

            var art = ModalKit.Pic(board, "chest", "Ui/chest_" + d.Key, (BoardW - 156f) * 0.5f, 148f, 156f, 156f, true);
            art.raycastTarget = false;
            UiAnim.On(this).Breathe(art.transform, 0f, 0.03f, 0.8f);

            ModalKit.SolidText(board, "expect", "预计获得", 16, 40f, 314f, BoardW - 80f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleCenter, true);

            var drops = new List<Drop>
            {
                new Drop(ModalKit.InkTone, "灵墨", d.InkMin + "~" + d.InkMax, ModalKit.Load("Ui/ico_ink"), false),
                new Drop("rose", "道具卡", "×" + d.Cards, ModalKit.Load("Ui/item_card_back"), true),
            };
            if (foil) drops.Add(new Drop(ModalKit.FoilTone, "金箔", null, FoilArt.Icon, false));
            if (seal) drops.Add(new Drop("purple", d.Tier == ChestTier.Royal ? "必得印章" : "印章",
                null, ModalKit.Load("Ui/glyph_slot_empty_green"), true));
            LayDrops(board, drops);

            const float tw = 300f;
            var timer = ModalKit.Sliced(board, "timer", "Ui/modal_info_strip", ModalKit.BorderStrip,
                (BoardW - tw) * 0.5f, 492f, tw, 40f);
            _state = ModalKit.SolidText(timer, "t", "", 16, 10f, 0f, tw - 20f, 40f,
                ModalKit.CreamLight, TextAnchor.MiddleCenter, true);

            float openX = (BoardW - 296f) * 0.5f;
            _open = ModalKit.HexBtn(board, "lg_yellow", openX, 548f, "打开", OnOpen, out _);
            if (_free)
            {
                _ad = ModalKit.HexBtn(board, "lg_yellow", openX, 548f, "免费加速", OnFree, out _);
            }
            else
            {
                const float bw = 176f;
                const float gap = 16f;
                float x = (BoardW - bw * 2f - gap) * 0.5f;
                float by = 548f + (101f - 63f);
                _ad = ModalKit.HexBtn(board, "md_cyan", x, by, "看广告开", OnAd, out _);
                ModalKit.SetAdBtnLabel(_ad, "md_cyan", "看广告开", true);
                _gem = ModalKit.HexBtn(board, "md_yellow", x + bw + gap, by, "立即开", OnGem, out Text gemLabel);
                ModalKit.TopLeft(gemLabel.rectTransform, 6f, 8f, 74f, 46f);
                gemLabel.alignment = TextAnchor.MiddleRight;
                var dia = UiKit.Icon(_gem.transform, InkSprites.Ui("diamond"), Vector2.zero, 22f);
                dia.raycastTarget = false;
                ModalKit.TopLeft(dia.rectTransform, 84f, 20f, 22f, 22f);
                _gemCost = UiKit.Label(_gem.transform, "cost", "", 18, Vector2.zero, new Vector2(56f, 30f),
                    TextAnchor.MiddleLeft, Pin.TopLeft);
                ModalKit.TopLeft(_gemCost.rectTransform, 108f, 16f, 58f, 30f);
                _gemCost.color = ModalKit.TextDark;
                UiKit.Bold(_gemCost);
            }
            Refresh();
        }

        static void CenterIcon(RectTransform slot, float share)
        {
            var icon = slot.Find("icon") as RectTransform;
            if (icon == null) return;
            const float slotSize = 96f;
            float s = Mathf.Round(slotSize * share);
            float inset = (slotSize - s) * 0.5f;
            icon.anchorMin = icon.anchorMax = new Vector2(0f, 1f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.sizeDelta = new Vector2(s, s);
            icon.anchoredPosition = new Vector2(inset + s * 0.5f, -(inset + s * 0.5f));
            var img = icon.GetComponent<Image>();
            if (img != null) img.preserveAspect = true;
        }

        static void LayDrops(RectTransform board, List<Drop> drops)
        {
            const float slot = 96f;
            const float gap = 14f;
            int n = drops.Count;
            float row = n * slot + (n - 1) * gap;
            float x0 = (BoardW - row) * 0.5f;
            const float y = 348f;
            for (int i = 0; i < n; i++)
            {
                Drop d = drops[i];
                float x = x0 + i * (slot + gap);
                var rt = ModalKit.ItemSlot(board, d.Tone, 96, x, y, d.Icon, d.Count);
                // 竖卡和圆章在左上锚点的方框里会贴边。改成中心锚，图就落在物品框正中。
                CenterIcon(rt, d.Ask ? 0.78f : 0.60f);
                if (d.Ask)
                {
                    var q = ModalKit.OutlinedText(rt, "q", "?", 30, 0f, 0f, slot, slot,
                        Color.white, TextAnchor.MiddleCenter);
                    q.alignByGeometry = true;
                    q.transform.SetAsLastSibling();
                }
                ModalKit.SolidText(board, "cap" + i, d.Caption, 14, x - 10f, y + slot + 2f, slot + 20f, 24f,
                    ModalKit.TextDark, TextAnchor.MiddleCenter, true);
            }
        }

        void OnFree()
        {
            AudioBus.Tap();
            if (!_meta.AdRushChest(_slot)) return;
            AudioBus.UnlockSting();
            Refresh();
            Sped?.Invoke();
        }

        void Update()
        {
            if (_meta == null) return;
            ChestState st = _meta.ChestStateOf(_slot);
            int sec = _meta.ChestSecondsLeft(_slot);
            if (st != _last || sec != _lastSec) Refresh();
        }

        void Refresh()
        {
            ChestState st = _meta.ChestStateOf(_slot);
            if (st == ChestState.Empty)
            {
                Destroy(gameObject);
                return;
            }
            int sec = _meta.ChestSecondsLeft(_slot);
            _last = st;
            _lastSec = sec;
            bool ready = st == ChestState.Ready;
            SetShown(_open, ready);
            SetShown(_ad, !ready);
            SetShown(_gem, !ready && !_free);
            if (_state != null)
                _state.color = ready ? ModalKit.GoldLight : ModalKit.CreamLight;
            if (ready)
            {
                if (_state != null) _state.text = "已解锁，可以打开";
                return;
            }
            if (_state != null)
                _state.text = st == ChestState.Timing
                    ? "解锁中  剩余 " + ChestCatalog.Clock(sec)
                    : "排队中  需要 " + ChestCatalog.Span(sec);
            if (_gemCost != null)
            {
                int cost = _meta.ChestRushCost(_slot);
                _gemCost.text = cost.ToString();
                _gemCost.color = _meta.Diamond >= cost ? ModalKit.TextDark : InkTheme.Rose;
            }
        }

        static void SetShown(Button b, bool on)
        {
            if (b == null) return;
            b.gameObject.SetActive(on);
            b.transform.parent.Find(b.name + "_sh")?.gameObject.SetActive(on);
        }

        void OnOpen()
        {
            AudioBus.Tap();
            ChestLoot loot = _meta.OpenChest(_slot);
            if (loot != null) Reveal(loot);
        }

        void OnAd()
        {
            AudioBus.Tap();
            AdStub.Reward("chest_speed", () =>
            {
                if (this == null || !_meta.AdRushChest(_slot)) return;
                Reveal(_meta.OpenChest(_slot));
            });
        }

        void OnGem()
        {
            AudioBus.Tap();
            int cost = _meta.ChestRushCost(_slot);
            if (_meta.Diamond < cost)
            {
                InkToast.Show((RectTransform)transform.parent, $"钻石不够，还差 {cost - _meta.Diamond}");
                return;
            }
            if (!_meta.RushChest(_slot)) return;
            Reveal(_meta.OpenChest(_slot));
        }

        void Reveal(ChestLoot loot)
        {
            RectTransform layer = (RectTransform)transform.parent;
            Action changed = _changed;
            Action revealed = Revealed;
            Opening?.Invoke();
            Destroy(gameObject);
            _meta.DeferChestSlide = true;
            changed?.Invoke();
            ChestOpenView.Show(layer, _meta, loot, () =>
            {
                _meta.DeferChestSlide = false;
                changed?.Invoke();
                revealed?.Invoke();
            });
        }
    }
}
