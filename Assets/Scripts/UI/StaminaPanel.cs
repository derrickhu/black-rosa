using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 点顶栏体力或体力不够时弹：看广告补一点，或者花钻石补一大截。两种都按天限次。
    public sealed class StaminaPanel : MonoBehaviour
    {
        const float BoardW = 560f;
        const float BoardH = 560f;

        MetaProgress _meta;
        Action _changed;
        Text _now;
        Button _ad;
        Text _adNote;
        Button _gem;
        Text _gemCost;
        Text _gemNote;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "stamina_panel";
            var panel = dim.gameObject.AddComponent<StaminaPanel>();
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
            var board = PanelKit.Board(dim, "补充体力", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);
            UiKit.Icon(board, InkSprites.Ui("stamina"), new Vector2(0f, 120f), 110f);
            _now = UiKit.Label(board, "now", "", 30, new Vector2(0f, 42f), new Vector2(BoardW - 40f, 40f));
            UiKit.Bold(_now);

            _ad = UiKit.Btn(board, "ad", $"+{GameConstants.AdStaminaGain} 体力", new Vector2(-126f, 96f),
                new Vector2(232f, 96f), OnAd, true, Pin.Bottom);
            ResultKit.AdMark(_ad, 36f);
            _adNote = UiKit.Label(board, "adnote", "", 18, new Vector2(-126f, 40f), new Vector2(232f, 26f),
                TextAnchor.MiddleCenter, Pin.Bottom);
            _adNote.color = InkTheme.TextMid;

            _gem = UiKit.Btn(board, "gem", "", new Vector2(126f, 96f), new Vector2(232f, 96f), OnGem, false, Pin.Bottom);
            var face = _gem.transform.Find("face");
            var label = face.Find("t").GetComponent<Text>();
            label.text = $"+{GameConstants.DiamondStaminaGain}";
            label.rectTransform.anchoredPosition = new Vector2(-46f, 0f);
            UiKit.Icon(face, InkSprites.Ui("diamond"), new Vector2(20f, 0f), 40f);
            _gemCost = UiKit.Label(face, "cost", "", 26, new Vector2(66f, 0f), new Vector2(60f, 36f));
            UiKit.Bold(_gemCost);
            _gemNote = UiKit.Label(board, "gemnote", "", 18, new Vector2(126f, 40f), new Vector2(232f, 26f),
                TextAnchor.MiddleCenter, Pin.Bottom);
            _gemNote.color = InkTheme.TextMid;
            Refresh();
        }

        void Refresh()
        {
            _now.text = $"当前体力 {_meta.Stamina}/{GameConstants.StaminaMax}";
            int adLeft = Mathf.Max(0, GameConstants.AdStaminaPerDay - _meta.AdStaminaToday);
            _adNote.text = $"今日还剩 {adLeft} 次";
            PanelKit.Dim(_ad, !_meta.CanAdStamina);
            int price = _meta.StaminaDiamondPrice;
            int gemLeft = Mathf.Max(0, GameConstants.DiamondStaminaPerDay - _meta.DiamondStamCount);
            _gemCost.text = price > 0 ? price.ToString() : "-";
            _gemCost.color = price > 0 && _meta.Diamond >= price ? InkTheme.TextDark : InkTheme.Rose;
            _gemNote.text = $"今日还剩 {gemLeft} 次";
        }

        void OnAd()
        {
            AudioBus.Tap();
            if (!_meta.CanAdStamina)
            {
                InkToast.Show(transform.parent, "今天的广告次数用完了");
                return;
            }
            AdStub.Reward("stamina", () =>
            {
                if (this == null) return;
                _meta.GrantAdStamina();
                Refresh();
                _changed?.Invoke();
            });
        }

        void OnGem()
        {
            AudioBus.Tap();
            int price = _meta.StaminaDiamondPrice;
            string why = price < 0 ? "今天的钻石次数用完了"
                : _meta.Diamond < price ? $"钻石不够，还差 {price - _meta.Diamond}" : null;
            if (why != null || !_meta.BuyStamina())
            {
                InkToast.Show(transform.parent, why ?? "现在买不了");
                return;
            }
            AudioBus.CountTick();
            Refresh();
            _changed?.Invoke();
        }
    }
}
