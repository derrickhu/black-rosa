using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 新手指引走完送的一份开局礼（Figma Panel/其他弹窗/01_新手奖励，预制体 Prefabs/GuideGift）：
    // 钻石和体力用黄框，灵墨用紫框，弹弓普通绿框；只有领取主按钮，无右上角关闭叉。
    public sealed class GuideGiftPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/GuideGift";
        public const string Mark = "guide_gift_v2";

        const float BoardW = 648f;
        const float BoardH = 496f;

        MetaProgress _meta;
        Action _done;
        RectTransform _layer;
        RectTransform _stage;
        RectTransform _land;
        Button _btn;
        RectTransform[] _cards;
        bool _claiming;

        static readonly string[] Icons = { "diamond", "ink", "stamina", "item_snipe" };
        static readonly string[] Names = { "钻石", "灵墨", "体力", "弹弓" };
        static readonly string[] Tones = { ModalKit.CurrencyTone, ModalKit.InkTone, ModalKit.CurrencyTone, "green" };
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
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "guide_gift", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<GuideGiftPanel>();
            panel._meta = meta;
            panel._layer = layer;
            panel._stage = stage;
            panel._land = cardLand;
            panel._done = done;
            panel.Bind(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "guide_gift", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "新手奖励", null,
                "教学完成，送你一份开局礼", null, null, null, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            var tray = ModalKit.Sliced(board, "tray", "Ui/modal_sub_warm", ModalKit.BorderSub18,
                24f, 154f, 600f, 198f, false);

            const float s = 96f, gap = 32f;
            float left = Mathf.Round((600f - (s * Icons.Length + gap * (Icons.Length - 1))) * 0.5f);
            for (int i = 0; i < Icons.Length; i++)
            {
                float sx = left + i * (s + gap);
                var slot = ModalKit.ItemSlot(tray, Tones[i], 96, sx, 30f,
                    ModalKit.Load("Ui/ico_" + Icons[i]), Amount(i).ToString());
                slot.name = "slot" + i;
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(sx + s * 0.5f, -(30f + s * 0.5f));

                ModalKit.SolidText(tray, "nm" + i, Names[i], 16, sx - 8f, 136f, 112f, 24f,
                    ModalKit.TextBody, TextAnchor.MiddleCenter, true);
            }

            var btn = ModalKit.HexBtn(board, "wide_yellow", (BoardW - 314f) * 0.5f, 378f, "领取", null, out _);
            btn.gameObject.name = "claim_btn";
            var brt = btn.GetComponent<RectTransform>();
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(BoardW * 0.5f, -(378f + 40f));

            return dim;
        }

        void OnRectTransformDimensionsChange()
        {
            if (_stage != null) ModalKit.FitStage(_stage, _layer);
        }

        void Bind(RectTransform stage)
        {
            var board = stage.Find("board") as RectTransform;
            var tray = board.Find("tray");
            _cards = new RectTransform[Icons.Length];
            for (int i = 0; i < Icons.Length; i++)
                _cards[i] = tray.Find("slot" + i) as RectTransform;

            _btn = ModalKit.BindBtn(board, "claim_btn", OnTap);
            PanelKit.Open(board);
            if (_btn != null) UiAnim.On(board).Breathe(_btn.transform, 0.5f, 0.04f, 1.3f);
        }

        void OnTap()
        {
            if (_claiming) return;
            _claiming = true;
            AudioBus.Tap();
            _btn.interactable = false;
            Play(_layer);
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
