using System;
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

        public static void Show(RectTransform layer, MetaProgress meta, int slot, Action changed)
        {
            if (meta == null || meta.ChestStateOf(slot) == ChestState.Empty) return;
            var dim = UiKit.Dimmer(layer);
            dim.name = "chest_panel";
            var panel = dim.gameObject.AddComponent<ChestPanel>();
            panel._meta = meta;
            panel._slot = slot;
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
            ChestDef d = ChestCatalog.Get(_meta.ChestTierOf(_slot));
            var board = PanelKit.Board(dim, d.Name, Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);

            var art = UiKit.Icon(board, InkSprites.Load("Ui/chest_" + d.Key), new Vector2(0f, 150f), 200f);
            UiAnim.On(this).Breathe(art.transform, 0f, 0.03f, 0.8f);

            var row = new Vector2(0f, 10f);
            ContentChip(board, "ink", $"{d.InkMin}~{d.InkMax}", row + new Vector2(-130f, 0f));
            ContentChip(board, "card", "×" + d.Cards, row + new Vector2(130f, 0f));
            string extra = d.PurpleChance >= 1f ? "必出稀有道具卡"
                : d.PurpleChance > 0f ? "有机会出稀有道具卡"
                : d.Tier == ChestTier.Silver ? "含高级道具卡" : "普通道具卡为主";
            var hint = UiKit.Label(board, "hint", extra, 22, new Vector2(0f, -48f), new Vector2(BoardW - 60f, 30f));
            hint.color = InkTheme.TextMid;

            _state = UiKit.Label(board, "state", "", 30, new Vector2(0f, -100f), new Vector2(BoardW - 60f, 40f));
            _state.color = InkTheme.TextDark;
            UiKit.Bold(_state);

            _open = UiKit.Btn(board, "open", "打开", new Vector2(0f, 70f), new Vector2(400f, 100f), OnOpen, true, Pin.Bottom);

            _ad = UiKit.Btn(board, "ad", "看广告开", new Vector2(-130f, 70f), new Vector2(236f, 96f), OnAd, true, Pin.Bottom);
            ResultKit.AdMark(_ad, 36f);

            _gem = UiKit.Btn(board, "gem", "", new Vector2(130f, 70f), new Vector2(236f, 96f), OnGem, false, Pin.Bottom);
            var face = _gem.transform.Find("face");
            var label = face.Find("t").GetComponent<Text>();
            label.text = "立即开";
            label.rectTransform.anchoredPosition = new Vector2(-30f, 0f);
            UiKit.Icon(face, InkSprites.Ui("diamond"), new Vector2(42f, 0f), 40f);
            _gemCost = UiKit.Label(face, "cost", "", 26, new Vector2(84f, 0f), new Vector2(60f, 36f));
            _gemCost.color = InkTheme.TextDark;
            UiKit.Bold(_gemCost);
            Refresh();
        }

        static void ContentChip(RectTransform board, string icon, string text, Vector2 pos)
        {
            var t = UiKit.Chip(board, "c_" + icon, InkSprites.Ui(icon), text, pos, new Vector2(210f, 60f), Pin.Center);
            t.fontSize = 28;
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
            SetShown(_gem, !ready);
            if (ready)
            {
                _state.text = "已解锁，可以打开";
                _state.color = InkTheme.Accel;
                return;
            }
            _state.color = InkTheme.TextDark;
            _state.text = st == ChestState.Timing
                ? "解锁中  剩余 " + ChestCatalog.Clock(sec)
                : "排队中  需要 " + ChestCatalog.Span(sec);
            int cost = _meta.ChestRushCost(_slot);
            _gemCost.text = cost.ToString();
            _gemCost.color = _meta.Diamond >= cost ? InkTheme.TextDark : InkTheme.Rose;
        }

        static void SetShown(Button b, bool on)
        {
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
            Destroy(gameObject);
            changed?.Invoke();
            ChestOpenView.Show(layer, _meta, loot, changed);
        }
    }
}
