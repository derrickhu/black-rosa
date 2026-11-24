using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 字卡页的每日兑换（Figma Panel/其他弹窗/09_兑换）。
    // 两张暖色卡片。灵墨紫框，金箔橙框，钻石黄框。换不起时主按钮变灰；换过一次后墨那一栏多出「再领」。
    // 点金箔栏本身仍弹出获取说明。
    public sealed class BagPanel : MonoBehaviour
    {
        const float BoardW = 648f;
        const float BoardH = 636f;
        const float CardX = 24f;
        const float CardW = 600f;
        const float CardH = 245f;
        const float CardTop = 106f;
        const float CardGap = 16f;
        static readonly Vector4 CardBorder = new Vector4(44f, 44f, 44f, 44f);

        MetaProgress _meta;
        RectTransform _layer;
        Action _changed;
        RectTransform _board;
        bool _note;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = ModalKit.Stage(layer, "swap", 0.78f, null, out RectTransform stage);
            var p = dim.gameObject.AddComponent<BagPanel>();
            p._meta = meta;
            p._layer = layer;
            p._changed = changed;
            p.BuildSwap(stage);
        }

        public static void ShowSources(RectTransform layer, RectTransform anchor)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "foil_note";
            dim.GetComponent<Image>().color = Color.clear;
            var p = dim.gameObject.AddComponent<BagPanel>();
            p._note = true;
            var hit = dim.GetComponent<Image>();
            hit.raycastTarget = true;
            var btn = dim.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(p.Close);
            p.BuildNote(dim, anchor);
        }

        void Close()
        {
            AudioBus.Tap();
            if (!_note) _changed?.Invoke();
            Destroy(gameObject);
        }

        void BuildSwap(RectTransform stage)
        {
            _board = ModalKit.Shell(stage, BoardW, BoardH, "兑换", null, null, null, null, Close, out _, out _);
            PaintSwap();
        }

        void PaintSwap()
        {
            Drop("ink");
            Drop("gem");
            var ink = Card("ink", CardTop, "墨换金箔", "今日剩 " + _meta.SwapLeft + " 次");
            Formula(ink, ModalKit.InkTone, InkSprites.Ui("ink"), _meta.SwapCost.ToString(), StudyCatalog.SwapFoil.ToString());
            Buttons(ink, _meta.CanSwapAd, !_meta.CanSwap, DoSwap, DoSwapAd);

            var gem = Card("gem", CardTop + CardH + CardGap, "钻石换金箔",
                _meta.SwapGemDone ? "今日剩 0 次" : "今日剩 1 次");
            Formula(gem, ModalKit.CurrencyTone, InkSprites.Ui("diamond"), StudyCatalog.GemSwapCost.ToString(), StudyCatalog.GemSwapFoil.ToString());
            Buttons(gem, false, !_meta.CanSwapGem, DoSwapGem, null);
        }

        void Drop(string id)
        {
            var old = _board.Find(id);
            if (old == null) return;
            old.gameObject.SetActive(false);
            Destroy(old.gameObject);
        }

        RectTransform Card(string id, float y, string title, string remain)
        {
            var card = ModalKit.Sliced(_board, id, "Ui/modal_sub_warm", CardBorder, CardX, y, CardW, CardH, false);
            ModalKit.SolidText(card, "h", title, 22, 22f, 14f, 280f, 32f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.SolidText(card, "left", remain, 16, 418f, 16f, 160f, 28f,
                ModalKit.TextMuted, TextAnchor.MiddleRight, false);
            return card;
        }

        void Formula(RectTransform card, string payTone, Sprite pay, string cost, string gain)
        {
            const float slot = 96f;
            const float rowY = 56f;
            const float costW = 84f;
            const float arrowW = 28f;
            const float gainW = 48f;
            const float gap = 8f;
            float rowW = slot + gap + costW + gap + arrowW + gap + slot + gap + gainW;
            float x = Mathf.Round((CardW - rowW) * 0.5f);
            ModalKit.ItemSlot(card, payTone, 96, x, rowY, pay);
            x += slot + gap;
            ModalKit.SolidText(card, "cost", cost, 28, x, rowY, costW, slot,
                ModalKit.TextDark, TextAnchor.MiddleCenter, true);
            x += costW + gap;
            ModalKit.SolidText(card, "arrow", "→", 26, x, rowY, arrowW, slot,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, false);
            x += arrowW + gap;
            var got = ModalKit.ItemSlot(card, ModalKit.FoilTone, 96, x, rowY, FoilArt.Icon);
            got.name = "gain";
            x += slot + gap;
            ModalKit.SolidText(card, "got", gain, 28, x, rowY, gainW, slot,
                ModalKit.TextDark, TextAnchor.MiddleCenter, true);
        }

        void Buttons(RectTransform card, bool ad, bool gray, Action main, Action onAd)
        {
            const float bw = 176f;
            const float by = 166f;
            const float gap = 16f;
            int n = ad ? 2 : 1;
            float total = n * bw + (n - 1) * gap;
            float x = Mathf.Round((CardW - total) * 0.5f);
            var go = ModalKit.HexBtn(card, "md_yellow", x, by, "兑换", main, out _);
            ModalKit.SetHexBtnGray(go, "md_yellow", "md_gray", gray);
            if (!ad) return;
            string again = "再领 " + StudyCatalog.SwapFoil;
            var extra = ModalKit.HexBtn(card, "md_cyan", x + bw + gap, by, again, onAd, out _);
            ModalKit.SetAdBtnLabel(extra, "md_cyan", again, true);
        }

        void DoSwap()
        {
            if (!_meta.CanSwap)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.SwapLeft <= 0 ? "今天换完了，明天再来" : "墨不够 " + _meta.SwapCost);
                return;
            }
            _meta.SwapInk();
            Gained("ink", StudyCatalog.SwapFoil);
        }

        void DoSwapAd()
        {
            if (!_meta.CanSwapAd) return;
            AdStub.Reward("foil_double", () =>
            {
                _meta.GrantSwapAd();
                if (this == null) return;
                Gained("ink", StudyCatalog.SwapFoil);
            });
        }

        void DoSwapGem()
        {
            if (_meta.SwapGemDone)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "今天换过了，明天再来");
                return;
            }
            if (_meta.Diamond < StudyCatalog.GemSwapCost)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "钻石不够 " + StudyCatalog.GemSwapCost);
                return;
            }
            _meta.SwapGem();
            Gained("gem", StudyCatalog.GemSwapFoil);
        }

        void Gained(string cardId, int n)
        {
            Transform from = _board.Find(cardId + "/gain");
            Vector2 at = RewardFly.Local(_layer, from);
            InkToast.Show(_layer, "金箔 +" + n);
            PaintSwap();
            if (_changed != null) _changed();
            RewardFly.Piece foil = RewardFly.Foil(_layer, n);
            RectTransform chip = FoilChip();
            if (chip != null) foil.Land = chip;
            RewardFly.Play(_layer, at, new[] { foil });
        }

        // 金箔药丸在字卡页里，不在顶栏。飞入落到这颗上，数字会跟着弹一下。
        RectTransform FoilChip()
        {
            RectTransform hit = RewardFly.Chip(_layer, "foil", "foil");
            return hit != null ? hit : FindNamed(_layer, "foil");
        }

        static RectTransform FindNamed(Transform root, string name)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name == name) return c as RectTransform;
                RectTransform nested = FindNamed(c, name);
                if (nested != null) return nested;
            }
            return null;
        }

        void BuildNote(RectTransform dim, RectTransform anchor)
        {
            const float w = 456f;
            const float h = 216f;
            Vector3 under = anchor != null
                ? anchor.TransformPoint(new Vector3(0f, -anchor.rect.height - 8f, 0f))
                : dim.TransformPoint(new Vector3(0f, dim.rect.height * 0.5f - 80f, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dim, RectTransformUtility.WorldToScreenPoint(null, under), null, out Vector2 local);
            float limit = Mathf.Max(0f, dim.rect.width * 0.5f - w * 0.5f - 12f);
            Vector2 cardPos = new Vector2(Mathf.Clamp(local.x, -limit, limit), local.y - h * 0.5f);

            var card = UiKit.Stroke(dim, "tip", cardPos, new Vector2(w, h), Pin.Center, 3f, radius: 18f);
            foreach (var g in card.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            var body = UiKit.Label(card, "t",
                "· 首次通关\n· 百鬼狂潮\n· 每日签到\n· 开宝箱\n· 炮台及道具满级后，碎片自动转换\n· 每日兑换",
                19, Vector2.zero, new Vector2(w - 36f, h - 24f), TextAnchor.MiddleLeft);
            body.lineSpacing = 1.15f;
        }
    }
}
