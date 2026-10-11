using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 新手指引走完送的一份：钻石、墨、体力，再加 3 张弹弓卡。只有领取，没有关闭。
    // 先在卡上跳一下，面板淡掉，前三样飞进顶栏，弹弓卡飞向道具页签；最后一样落地才入账。
    public sealed class GuideGiftPanel : MonoBehaviour
    {
        const float BoardW = 620f;
        const float BoardH = 560f;

        MetaProgress _meta;
        Action _done;
        RectTransform _land;
        Button _btn;
        RectTransform[] _cards;
        bool _claiming;

        static readonly string[] Icons = { "diamond", "ink", "stamina", "item_snipe" };
        static readonly string[] Names = { "钻石", "墨", "体力", "弹弓" };
        static readonly string[][] ChipNames = { new[] { "diamond", "cd" }, new[] { "ink", "ci" }, new[] { "stamina", "cs" } };
        static readonly Color[] Sparks =
        {
            InkTheme.Hex("F2A0C8"), InkTheme.Track, InkTheme.Hex("F5C542"), InkTheme.Hex("3DAE5A")
        };

        static int Amount(int i) =>
            i == 0 ? GameConstants.GuideDiamond
            : i == 1 ? GameConstants.GuideInk
            : i == 2 ? GameConstants.GuideStamina
            : GameConstants.GuideSlingshot;

        public static void Show(RectTransform layer, MetaProgress meta, RectTransform cardLand, Action done)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "guide_gift";
            var panel = dim.gameObject.AddComponent<GuideGiftPanel>();
            panel._meta = meta;
            panel._land = cardLand;
            panel._done = done;
            panel.Build(dim);
        }

        void Build(RectTransform dim)
        {
            var board = PanelKit.Board(dim, "新手奖励", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, null);
            Transform close = board.Find("close");
            if (close != null) Destroy(close.gameObject);

            var sub = UiKit.Label(board, "sub", "教学完成，送你一份开局礼", 24,
                new Vector2(0f, 72f), new Vector2(BoardW - 60f, 32f), TextAnchor.MiddleCenter, Pin.Top);
            sub.color = InkTheme.TextMid;

            var cardSize = new Vector2(132f, 196f);
            _cards = new RectTransform[Icons.Length];
            for (int i = 0; i < Icons.Length; i++)
                _cards[i] = PanelKit.Reward(board, "Ui/ico_" + Icons[i], Names[i], "×" + Amount(i),
                    new Vector2((i - 1.5f) * 150f, 124f), cardSize);

            _btn = UiKit.Btn(board, "claim", "领取", new Vector2(0f, 52f), new Vector2(430f, 104f), OnTap, true, Pin.Bottom);
            UiAnim.On(board).Breathe(_btn.transform, 0.5f, 0.045f, 1.3f);
        }

        void OnTap()
        {
            if (_claiming) return;
            _claiming = true;
            AudioBus.Tap();
            _btn.interactable = false;
            Play((RectTransform)transform.parent);
        }

        void Play(RectTransform layer)
        {
            var anim = UiAnim.On(this);
            int n = Icons.Length;
            var from = new Vector2[n];
            var to = new Vector2[n];
            var chips = new RectTransform[n];
            var flyers = new RectTransform[n];
            float top = layer.rect.height * 0.5f - 70f;
            for (int i = 0; i < n; i++)
            {
                Transform icon = _cards[i].Find("icon");
                from[i] = RewardFly.Local(layer, icon != null ? icon : _cards[i]);
                if (i < ChipNames.Length)
                    chips[i] = RewardFly.Chip(layer, ChipNames[i][0], ChipNames[i][1]);
                else
                    chips[i] = _land;
                to[i] = chips[i] != null ? RewardFly.Local(layer, chips[i]) : new Vector2((i - 1.5f) * 150f, top);
                anim.Punch(_cards[i], i * 0.08f, 0.14f, 0.34f);
            }
            anim.At(0.05f, () =>
            {
                AudioBus.Chime();
                for (int i = 0; i < n; i++) UiConfetti.Sparks(layer, from[i], Sparks[i], 14, 480f);
            });
            for (int i = 0; i < n; i++)
            {
                flyers[i] = RewardFly.Flyer(layer, Icons[i], Amount(i), from[i]);
                anim.Pop(flyers[i], 0.08f + i * 0.08f, 0.36f, 0f);
            }

            // 投影是面板的同级节点，整层一起淡，免得留一块影子。
            var veil = gameObject.AddComponent<CanvasGroup>();
            anim.Fade(veil, 0.72f, 0.32f, 1f, 0f);
            for (int i = 0; i < n; i++)
            {
                int k = i;
                anim.At(0.95f + k * 0.1f, () => RewardFly.Fly(this, flyers[k], from[k], to[k]))
                    .At(1.52f + k * 0.1f, () => RewardFly.Land(layer, flyers[k], chips[k], Sparks[k]));
            }
            anim.At(1.52f + (n - 1) * 0.1f + 0.05f, Finish);
        }

        void Finish()
        {
            if (this == null) return;
            _meta.GrantGuideGift();
            Action done = _done;
            Destroy(gameObject);
            done?.Invoke();
        }
    }
}
