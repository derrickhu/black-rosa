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
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Build(RectTransform dim)
        {
            var board = PanelKit.Board(dim, "新手礼包", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);

            var sub = UiKit.Label(board, "sub", "新人专享 · 看两个广告就能领", 24,
                new Vector2(0f, 72f), new Vector2(BoardW - 60f, 32f), TextAnchor.MiddleCenter, Pin.Top);
            sub.color = InkTheme.TextMid;

            var cardSize = new Vector2(172f, 214f);
            PanelKit.Reward(board, "Ui/ico_shard", "技能碎片", "×" + GameConstants.GiftShards,
                new Vector2(-110f, 124f), cardSize);
            PanelKit.Reward(board, "Ui/ico_ink", "墨", "×" + GameConstants.GiftInk,
                new Vector2(110f, 124f), cardSize);

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
            if (_meta.GiftReady)
            {
                if (!_meta.ClaimGift()) return;
                RectTransform layer = (RectTransform)transform.parent;
                InkToast.Show(layer, "新手礼包已领取");
                _changed?.Invoke();
                Destroy(gameObject);
                return;
            }
            AdStub.Reward("starter_gift", () =>
            {
                if (this == null) return;
                _meta.AddGiftAd();
                Refresh();
            });
        }
    }
}
