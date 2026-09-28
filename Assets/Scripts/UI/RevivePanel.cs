using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 续命页。核心是「差一点」：大字百分比 + 快满的进度条。
    // 不倒计时，页面停着等玩家自己点「续命再战」或「放弃」。
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

        public static RevivePanel Show(RectTransform layer, float progress, int revivesLeft, Action revive, Action giveUp)
        {
            var dim = UiKit.Dimmer(layer);
            dim.GetComponent<Image>().color = new Color(0.18f, 0.04f, 0.03f, 0.80f);
            var p = dim.gameObject.AddComponent<RevivePanel>();
            p._root = dim;
            p._anim = UiAnim.On(p);
            p._giveUp = giveUp;
            p.Build(Mathf.Clamp01(progress), revivesLeft, revive);
            AudioBus.Sweep(dim);
            return p;
        }

        void Build(float progress, int revivesLeft, Action revive)
        {
            _heart = ResultKit.Group(_root, "heart", new Vector2(0f, 420f), new Vector2(120f, 120f));
            UiKit.Icon(_heart, InkArt.Icon(InkShape.Heart, 128), Vector2.zero, 120f);
            _anim.Pop(_heart, 0f, 0.36f);

            var title = ResultKit.Headline(_root, "title", "防线告急！", 68, new Vector2(0f, 308f),
                InkTheme.Hex("FFE2D6"), InkTheme.Hex("5A0E08"), 4f);
            _anim.Pop(title.transform, 0.08f, 0.4f, 1.8f).Fade(title, 0.08f, 0.2f, 0f, 1f)
                 .Shake(title.rectTransform, 0.4f, 10f, 0.3f);

            var card = ResultKit.Group(_root, "card", new Vector2(0f, 70f), new Vector2(580f, 320f));
            UiKit.Stroke(card, "bg", Vector2.zero, new Vector2(580f, 320f), Pin.Center, 6f,
                fill: InkTheme.Hex("FFF6E2"), radius: 36f);
            _anim.Pop(card, 0.22f, 0.36f);

            var done = UiKit.Label(card, "done", "本关已完成", 30, new Vector2(0f, 108f), new Vector2(400f, 40f));
            done.color = InkTheme.TextMid;
            int pct = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 1, 99);
            var big = ResultKit.Headline(card, "pct", "0%", 104, new Vector2(0f, 28f),
                InkTheme.Hex("F6BE3C"), InkTheme.Outline, 4f);
            _anim.CountUp(big, 0, pct, 0.5f, 0.8f, "{0}%", AudioBus.CountTick)
                 .Punch(big.transform, 1.3f, 0.14f, 0.3f);

            var fill = ResultKit.Bar(card, new Vector2(0f, -52f), 480f, 34f, InkTheme.Hex("F58A34"));
            _anim.Tween(0.5f, 0.8f, k => ResultKit.SetBar(fill, progress * Ease.OutCubic(k)));
            var flag = UiKit.Icon(card, InkSprites.Ui("star"), new Vector2(240f, -52f), 52f);
            _anim.Breathe(flag.transform, 1.2f, 0.12f, 1.6f);

            var near = UiKit.Label(card, "near", pct >= 70 ? "就差一点就守住了！" : "再撑一下，胜利就在前面", 28,
                new Vector2(0f, -112f), new Vector2(520f, 40f));
            UiKit.Bold(near);
            near.color = InkTheme.Rose;
            _anim.Fade(near, 1.2f, 0.3f, 0f, 1f);

            var rg = ResultKit.Group(_root, "revive", new Vector2(0f, -200f), new Vector2(460f, 116f));
            var btn = UiKit.Btn(rg, "b", "续命再战", Vector2.zero, new Vector2(460f, 116f), () =>
            {
                if (_busy) return;
                _busy = true;
                revive();
                _busy = false;
            });
            var label = btn.transform.Find("face/t")?.GetComponent<Text>();
            if (label != null) label.fontSize = 38;
            ResultKit.AdMark(btn, 52f);
            _anim.Pop(rg, 0.5f, 0.36f).Breathe(rg, 0.9f, 0.05f, 1.4f);

            var perk = UiKit.Label(_root, "perk", "防线回满 · 清掉逼近的敌人", 26, new Vector2(0f, -290f), new Vector2(560f, 36f));
            perk.color = InkTheme.Hex("FFE7B8");
            var left = UiKit.Label(_root, "left", $"本局还能续 {revivesLeft} 次", 22, new Vector2(0f, -328f), new Vector2(400f, 30f));
            left.color = InkTheme.Hex("D8B8A8");
            _anim.Fade(perk, 0.7f, 0.3f, 0f, 1f).Fade(left, 0.8f, 0.3f, 0f, 1f);

            var gg = ResultKit.Group(_root, "giveup", new Vector2(0f, -420f), new Vector2(200f, 66f));
            UiKit.Btn(gg, "b", "放弃", Vector2.zero, new Vector2(200f, 66f), GiveUp, false);
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
