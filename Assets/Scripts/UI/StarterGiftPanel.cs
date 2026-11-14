using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 新手礼包（Figma Panel/首页弹窗/04_新手礼包，预制体 Prefabs/StarterGift）：
    // 一排通用物品框（银宝箱蓝框略大，灵墨、钻石在两侧），下方只留广告进度条，不再挂里程碑图标。
    public sealed class StarterGiftPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/StarterGift";
        public const string Mark = "starter_gift_v2";

        const float BoardW = 648f;
        const float BoardH = 624f;
        const float ProgW = 528f;
        const float ProgH = 30f;

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        RectTransform _gemSlot;
        RectTransform _inkSlot;
        RectTransform _progFill;
        Text _progTitle;
        Text _readyTagText;
        Button _btn;
        ChestLoot _loot;
        bool _wallet;
        bool _claiming;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "starter_gift", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<StarterGiftPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel.Bind(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "starter_gift", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "新手礼包", "Ui/ico_act_event",
                "新人专享福利 · 免费解锁整套补给", "magenta", "限领 1 次", () => { }, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            ChestDef chest = ChestCatalog.Get(GameConstants.GiftChest);

            var inkSlot = ModalKit.ItemSlot(board, ModalKit.InkTone, 96, 130f, 190f,
                ModalKit.Load("Ui/ico_ink"), GameConstants.GiftInk.ToString());
            inkSlot.name = "slot_ink";

            var chestSlot = ModalKit.ItemSlot(board, ModalKit.ChestTone(chest.Tier), 128, 260f, 174f,
                ModalKit.Load("Ui/chest_" + chest.Key), null);
            chestSlot.name = "slot_chest";

            var gemSlot = ModalKit.ItemSlot(board, ModalKit.CurrencyTone, 96, 422f, 190f,
                ModalKit.Load("Ui/ico_diamond"), GameConstants.GiftDiamond.ToString());
            gemSlot.name = "slot_gem";

            ModalKit.SolidText(board, "ink_name", "灵墨", 16, 138f, 292f, 80f, 24f,
                ModalKit.TextBody, TextAnchor.MiddleCenter, true);
            ModalKit.SolidText(board, "chest_name", chest.Name, 16, 276f, 308f, 96f, 24f,
                ModalKit.TextBody, TextAnchor.MiddleCenter, true);
            ModalKit.SolidText(board, "gem_name", "钻石", 16, 430f, 292f, 80f, 24f,
                ModalKit.TextBody, TextAnchor.MiddleCenter, true);

            var progCard = ModalKit.Sliced(board, "prog_card", "Ui/modal_sub_cream", ModalKit.BorderSub18,
                24f, 352f, 600f, 120f, false);
            ModalKit.SolidText(progCard, "t", "免费看广告解锁进度（0 / 2）", 20, 22f, 16f, 400f, 30f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            var readyTag = ModalKit.Tag(progCard, "Ui/modal_tag_green", 448f, 16f, 132f, 28f, "再看 2 次立得", true, 13);
            readyTag.name = "ready_tag";
            ModalKit.ProgressBar(progCard, 36f, 76f, ProgW, ProgH, 0f, "", out _, out _);

            var btn = ModalKit.HexBtn(board, "lg_yellow", 176f, 492f, "免费解锁 (0/2)", null, out _);
            btn.gameObject.name = "main_btn";
            ModalKit.SetAdBtnLabel(btn, "lg_yellow", $"免费解锁 (0/{GameConstants.GiftAds})", true);

            var freeTag = ModalKit.Tag(board, "Ui/modal_tag_coral", 350f, 480f, 86f, 26f, "免付费", false, 13);
            freeTag.name = "free_tag";

            return dim;
        }

        void Bind(RectTransform stage)
        {
            _board = stage.Find("board") as RectTransform;
            ModalKit.BindClose(_board, Close);
            _inkSlot = _board.Find("slot_ink") as RectTransform;
            _gemSlot = _board.Find("slot_gem") as RectTransform;
            // 预制烘出来时蓝框没挂上，打开时按银宝箱补上。
            var chestFrame = _board.Find("slot_chest")?.GetComponent<Image>();
            if (chestFrame != null)
            {
                ChestDef chest = ChestCatalog.Get(GameConstants.GiftChest);
                Sprite frame = ModalKit.Load(ModalKit.SlotSpriteKey(ModalKit.ChestTone(chest.Tier), 128));
                if (frame != null) chestFrame.sprite = frame;
            }
            _progTitle = _board.Find("prog_card/t")?.GetComponent<Text>();
            _readyTagText = _board.Find("prog_card/ready_tag/t")?.GetComponent<Text>();
            _progFill = _board.Find("prog_card/prog/fill") as RectTransform;
            _btn = ModalKit.BindBtn(_board, "main_btn", OnTap);

            Refresh();
            PanelKit.Open(_board);
        }

        void Close()
        {
            if (_claiming) return;
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_loot != null && !_wallet && _meta != null) _meta.GrantGiftWallet();
        }

        void Refresh()
        {
            int seen = Mathf.Min(_meta.GiftAds, GameConstants.GiftAds);
            int left = Mathf.Max(0, GameConstants.GiftAds - seen);
            if (_progTitle != null)
                _progTitle.text = $"免费看广告解锁进度（{seen} / {GameConstants.GiftAds}）";
            if (_readyTagText != null)
                _readyTagText.text = _meta.GiftReady ? "已解锁可领" : $"再看 {left} 次立得";
            ModalKit.SetProgress(_progFill, ProgW, ProgH, seen / (float)GameConstants.GiftAds);
            if (_btn != null)
            {
                if (_meta.GiftReady)
                    ModalKit.SetAdBtnLabel(_btn, "lg_yellow", "立即领取豪礼", false);
                else
                    ModalKit.SetAdBtnLabel(_btn, "lg_yellow", $"免费解锁 ({seen}/{GameConstants.GiftAds})", true);
            }
        }

        void OnTap()
        {
            AudioBus.Tap();
            if (_claiming) return;
            if (_meta.GiftReady)
            {
                ChestLoot loot = _meta.ClaimGift();
                if (loot == null) return;
                _loot = loot;
                _claiming = true;
                _btn.interactable = false;
                _changed?.Invoke();
                PlayWallet(_layer);
                return;
            }
            AdStub.Reward("starter_gift", () =>
            {
                if (this == null) return;
                _meta.AddGiftAd();
                Refresh();
            });
        }

        void PlayWallet(RectTransform layer)
        {
            var anim = UiAnim.On(this);
            Vector2 gemFrom = RewardFly.Local(layer, _gemSlot);
            Vector2 inkFrom = RewardFly.Local(layer, _inkSlot);
            RectTransform gemChip = RewardFly.Chip(layer, "diamond", "cd");
            RectTransform inkChip = RewardFly.Chip(layer, "ink", "ci");
            float top = layer.rect.height * 0.5f - 70f;
            Vector2 gemTo = gemChip != null ? RewardFly.Local(layer, gemChip) : new Vector2(232f, top);
            Vector2 inkTo = inkChip != null ? RewardFly.Local(layer, inkChip) : new Vector2(0f, top);

            anim.Punch(_gemSlot, 0f, 0.14f, 0.34f).Punch(_inkSlot, 0.08f, 0.14f, 0.34f)
                .At(0.05f, () =>
                {
                    AudioBus.Chime();
                    UiConfetti.Sparks(layer, gemFrom, InkTheme.Hex("F2A0C8"), 14, 480f);
                    UiConfetti.Sparks(layer, inkFrom, InkTheme.Track, 14, 480f);
                });

            RectTransform gem = RewardFly.Flyer(layer, "diamond", GameConstants.GiftDiamond, gemFrom);
            RectTransform ink = RewardFly.Flyer(layer, "ink", GameConstants.GiftInk, inkFrom);
            anim.Pop(gem, 0.08f, 0.36f, 0f).Pop(ink, 0.16f, 0.36f, 0f);

            var veil = gameObject.AddComponent<CanvasGroup>();
            anim.Fade(veil, 0.72f, 0.32f, 1f, 0f);
            anim.At(0.95f, () => RewardFly.Fly(this, gem, gemFrom, gemTo))
                .At(1.05f, () => RewardFly.Fly(this, ink, inkFrom, inkTo))
                .At(1.52f, () => RewardFly.Land(layer, gem, gemChip, InkTheme.Hex("F2A0C8")))
                .At(1.62f, () => RewardFly.Land(layer, ink, inkChip, InkTheme.Track))
                .At(1.9f, () => Finish(layer));
        }

        void Finish(RectTransform layer)
        {
            if (this == null) return;
            _wallet = true;
            _meta.GrantGiftWallet();
            _changed?.Invoke();
            ChestLoot loot = _loot;
            Destroy(gameObject);
            ChestOpenView.Show(layer, _meta, loot, _changed);
        }
    }
}
