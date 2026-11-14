using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 续命页。核心是「差一点」：大字百分比 + 快满的进度条。
    // 当天还没转发复活过时，主按钮是转发，字下写当日 0/1；用过之后才是看广告。
    // 不倒计时，页面停着等玩家自己点或放弃。
    public sealed class RevivePanel : MonoBehaviour
    {
        const float Beat = 0.9f;

        UiAnim _anim;
        RectTransform _root;
        RectTransform _heart;
        Action _giveUp;
        float _beat = Beat;
        bool _busy;

        public RectTransform Root => _root;

        // doneText 换掉「本关已完成」那一行，无尽模式写已撑过几波。
        public static RevivePanel Show(RectTransform layer, float progress, int revivesLeft, bool grantGun,
            bool shareRevive, Action revive, Action giveUp, string doneText = null)
        {
            var dim = UiKit.Dimmer(layer);
            dim.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.05f, 0.82f);
            var p = dim.gameObject.AddComponent<RevivePanel>();
            p._root = dim;
            p._anim = UiAnim.On(p);
            p._giveUp = giveUp;
            p.Build(Mathf.Clamp01(progress), revivesLeft, grantGun, shareRevive, revive, doneText);
            AudioBus.Sweep(dim);
            return p;
        }

        void Build(float progress, int revivesLeft, bool grantGun, bool shareRevive, Action revive, string doneText)
        {
            // 字重跟全游戏一致：暗底标题是浅色细墨线，奶油卡上的百分比是实心深色，
            // 说明走 Ink 常规，按钮走尖角按钮档位（大 24 / 中 20，InkBold）。
            _heart = ResultKit.Group(_root, "heart", new Vector2(0f, 448f), new Vector2(112f, 112f));
            UiKit.Icon(_heart, InkArt.Icon(InkShape.Heart, 128), Vector2.zero, 112f);
            _anim.Pop(_heart, 0f, 0.36f);

            var title = ResultKit.LightTitle(_root, "title", "防线告急！", 52, new Vector2(0f, 324f),
                InkTheme.Hex("FFF6E4"));
            var starL = UiKit.Icon(_root, InkSprites.Ui("star"), new Vector2(-236f, 324f), 30f);
            var starR = UiKit.Icon(_root, InkSprites.Ui("star"), new Vector2(236f, 324f), 30f);
            _anim.Pop(title.transform, 0.08f, 0.4f, 1.8f).Fade(title, 0.08f, 0.2f, 0f, 1f)
                 .Shake(title.rectTransform, 0.4f, 8f, 0.28f)
                 .Fade(starL, 0.16f, 0.2f, 0f, 1f).Fade(starR, 0.16f, 0.2f, 0f, 1f);

            const float CardW = 620f;
            const float CardH = 328f;
            var card = ResultKit.Group(_root, "card", new Vector2(0f, 80f), new Vector2(CardW, CardH));
            UiKit.Stroke(card, "bg", Vector2.zero, new Vector2(CardW, CardH), Pin.Center, 5f,
                fill: InkTheme.Hex("FFFDF1"), radius: 28f);
            _anim.Pop(card, 0.22f, 0.36f);

            var done = UiKit.Label(card, "done", doneText ?? "本关已完成", 22, new Vector2(0f, 118f), new Vector2(420f, 32f));
            done.color = InkTheme.TextMid;
            int pct = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 1, 99);
            var big = ResultKit.PlaqueTitle(card, "pct", "0%", 68, new Vector2(0f, 36f), InkTheme.TextDark);
            _anim.CountUp(big, 0, pct, 0.5f, 0.8f, "{0}%", AudioBus.CountTick)
                 .Punch(big.transform, 1.3f, 0.14f, 0.3f);

            var fill = ResultKit.Bar(card, new Vector2(0f, -28f), 480f, 28f, InkTheme.CoinFace);
            _anim.Tween(0.5f, 0.8f, k => ResultKit.SetBar(fill, progress * Ease.OutCubic(k)));
            var flag = UiKit.Icon(card, InkSprites.Ui("star"), new Vector2(240f, -28f), 44f);
            _anim.Breathe(flag.transform, 1.2f, 0.12f, 1.6f);

            var near = UiKit.Label(card, "near", pct >= 70 ? "就差一点就守住了！" : "再撑一下，胜利就在前面", 24,
                new Vector2(0f, -78f), new Vector2(540f, 34f));
            UiKit.Bold(near);
            near.color = InkTheme.Rose;

            string offer = grantGun
                ? "回一半血 · 清掉防线上的敌人 · 多一门炮"
                : "回一半血 · 清掉防线上的敌人";
            var perk = UiKit.Label(card, "perk", offer, 18, new Vector2(0f, -122f), new Vector2(560f, 28f));
            perk.color = InkTheme.TextMid;
            _anim.Fade(near, 1.2f, 0.3f, 0f, 1f).Fade(perk, 1.25f, 0.3f, 0f, 1f);

            var rg = ResultKit.Group(_root, "revive", new Vector2(0f, -162f), new Vector2(296f, 101f));
            var btn = ModalKit.HexBtn(rg, "lg_yellow", 0f, 0f, shareRevive ? "转发复活" : "续命再战", () =>
            {
                if (_busy) return;
                _busy = true;
                revive();
                _busy = false;
            }, out _);
            if (shareRevive)
            {
                var label = btn.transform.Find("t");
                if (label != null) ModalKit.TopLeft(label.GetComponent<RectTransform>(), 16f, 8f, 264f, 44f);
                var sub = ModalKit.SolidText(btn.transform, "sub", "当日 0/1", 16, 16f, 50f, 264f, 26f,
                    InkTheme.TextDark, TextAnchor.MiddleCenter, false);
                sub.color = InkTheme.Hex("5C4636");
            }
            else ModalKit.SetAdBtnLabel(btn, "lg_yellow", "续命再战", true, 24);
            _anim.Pop(rg, 0.5f, 0.36f).Breathe(rg, 0.9f, 0.04f, 1.4f);

            var gg = ResultKit.Group(_root, "giveup", new Vector2(0f, -276f), new Vector2(176f, 63f));
            ModalKit.HexBtn(gg, "md_gray", 0f, 0f, "放弃", GiveUp, out _);
            _anim.Pop(gg, 0.7f, 0.3f);
        }

        void GiveUp()
        {
            if (_busy || _giveUp == null) return;
            Action a = _giveUp;
            _giveUp = null;
            a();
        }

        void Update()
        {
            if (_heart == null) return;
            _beat -= Time.unscaledDeltaTime;
            if (_beat > 0f) return;
            _beat = Beat;
            AudioBus.Heartbeat();
            _anim.Punch(_heart, 0f, 0.16f, 0.28f);
        }
    }
}
