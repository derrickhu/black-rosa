using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 新手礼包，照 hotpot：看两个激励广告解锁，领一次就从出征页撤掉。
    public sealed class StarterGiftPanel : MonoBehaviour
    {
        const float BoardW = 620f;
        const float BoardH = 660f;

        MetaProgress _meta;
        Action _changed;
        Button _btn;
        Text _progress;
        Image[] _pips;
        RectTransform _board;
        RectTransform _gemCard;
        RectTransform _inkCard;
        ChestLoot _loot;
        bool _wallet;
        bool _claiming;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "starter_gift";
            var panel = dim.gameObject.AddComponent<StarterGiftPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel.Build(dim);
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

        void Build(RectTransform dim)
        {
            var board = PanelKit.Board(dim, "新手礼包", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);

            var sub = UiKit.Label(board, "sub", "新人专享 · 看两个广告就能领", 24,
                new Vector2(0f, 72f), new Vector2(BoardW - 60f, 32f), TextAnchor.MiddleCenter, Pin.Top);
            sub.color = InkTheme.TextMid;

            _board = board;
            var cardSize = new Vector2(172f, 214f);
            ChestDef chest = ChestCatalog.Get(GameConstants.GiftChest);
            PanelKit.Reward(board, "Ui/chest_" + chest.Key, chest.Name, "当场开",
                new Vector2(-186f, 124f), cardSize);
            _gemCard = PanelKit.Reward(board, "Ui/ico_diamond", "钻石", "×" + GameConstants.GiftDiamond,
                new Vector2(0f, 124f), cardSize);
            _inkCard = PanelKit.Reward(board, "Ui/ico_ink", "墨", "×" + GameConstants.GiftInk,
                new Vector2(186f, 124f), cardSize);

            _progress = UiKit.Label(board, "progress", "", 26, new Vector2(-40f, 386f), new Vector2(240f, 40f),
                TextAnchor.MiddleCenter, Pin.Top);
            _progress.color = InkTheme.TextDark;
            _pips = new Image[GameConstants.GiftAds];
            for (int i = 0; i < _pips.Length; i++)
            {
                var ring = UiKit.Icon(board, UiSprites.Disc(), new Vector2(92f + i * 46f, BoardH * 0.5f - 406f), 36f);
                ring.color = InkTheme.Outline;
                _pips[i] = UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 28f);
            }

            _btn = UiKit.Btn(board, "claim", "", new Vector2(0f, 52f), new Vector2(430f, 104f), OnTap, true, Pin.Bottom);
            Refresh();
        }

        void Refresh()
        {
            int seen = Mathf.Min(_meta.GiftAds, GameConstants.GiftAds);
            _progress.text = $"已看广告 {seen}/{GameConstants.GiftAds}";
            for (int i = 0; i < _pips.Length; i++)
                _pips[i].color = i < seen ? InkTheme.Seal : InkTheme.CardFace;
            PanelKit.SetText(_btn, _meta.GiftReady ? "领取" : $"看广告领取  {seen}/{GameConstants.GiftAds}");
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
                PlayWallet((RectTransform)transform.parent);
                return;
            }
            AdStub.Reward("starter_gift", () =>
            {
                if (this == null) return;
                _meta.AddGiftAd();
                Refresh();
            });
        }

        // 钻石和墨先在原卡上跳出来，面板褪掉后飞进顶栏，到账再开宝箱。
        void PlayWallet(RectTransform layer)
        {
            var anim = UiAnim.On(this);
            Transform gemIcon = _gemCard.Find("icon");
            Transform inkIcon = _inkCard.Find("icon");
            Vector2 gemFrom = Local(layer, gemIcon != null ? gemIcon : _gemCard);
            Vector2 inkFrom = Local(layer, inkIcon != null ? inkIcon : _inkCard);
            RectTransform gemChip = Chip(layer, "diamond", "cd");
            RectTransform inkChip = Chip(layer, "ink", "ci");
            float top = layer.rect.height * 0.5f - 70f;
            Vector2 gemTo = gemChip != null ? Local(layer, gemChip) : new Vector2(232f, top);
            Vector2 inkTo = inkChip != null ? Local(layer, inkChip) : new Vector2(0f, top);

            anim.Punch(_gemCard, 0f, 0.14f, 0.34f).Punch(_inkCard, 0.08f, 0.14f, 0.34f)
                .At(0.05f, () =>
                {
                    AudioBus.Chime();
                    UiConfetti.Sparks(layer, gemFrom, InkTheme.Hex("F2A0C8"), 14, 480f);
                    UiConfetti.Sparks(layer, inkFrom, InkTheme.Track, 14, 480f);
                });

            RectTransform gem = Flyer(layer, "diamond", GameConstants.GiftDiamond, gemFrom);
            RectTransform ink = Flyer(layer, "ink", GameConstants.GiftInk, inkFrom);
            anim.Pop(gem, 0.08f, 0.36f, 0f).Pop(ink, 0.16f, 0.36f, 0f);

            // 投影是面板的同级节点，只淡卡面会把那块深色影子留在章节卡上。整层一起淡。
            var veil = gameObject.AddComponent<CanvasGroup>();
            anim.Fade(veil, 0.72f, 0.32f, 1f, 0f);
            // 飞行动画等到出发再排。提前排的话第一帧就会把图标缩放改掉，弹出还没演完。
            anim.At(0.95f, () => Fly(gem, gemFrom, gemTo))
                .At(1.05f, () => Fly(ink, inkFrom, inkTo))
                .At(1.52f, () => Land(layer, gem, gemChip, InkTheme.Hex("F2A0C8")))
                .At(1.62f, () => Land(layer, ink, inkChip, InkTheme.Track))
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

        static void Land(RectTransform layer, RectTransform flyer, RectTransform chip, Color spark) =>
            RewardFly.Land(layer, flyer, chip, spark);

        void Fly(RectTransform flyer, Vector2 from, Vector2 to) => RewardFly.Fly(this, flyer, from, to);

        static RectTransform Flyer(RectTransform layer, string icon, int n, Vector2 pos) =>
            RewardFly.Flyer(layer, icon, n, pos);

        static RectTransform Chip(RectTransform layer, string a, string b) => RewardFly.Chip(layer, a, b);

        static Vector2 Local(RectTransform layer, Transform target) => RewardFly.Local(layer, target);
    }
}
