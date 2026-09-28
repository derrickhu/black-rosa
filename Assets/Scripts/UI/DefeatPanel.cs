using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class DefeatArgs
    {
        public int Stage;
        public float Progress;
        public int RetryCost;
        public bool CanRetry;
        public List<Advice> Advice;
        public Action Retry;
        public Action<int> Improve;   // 去首页的哪个页签
        public Action Home;
    }

    // 失败页：先放 DefeatFx 那段倒下的动画，再把进度、提升建议、再来一次依次弹出来。
    // 口径是「差一点 + 有办法变强」，别让人看完只剩挫败感。
    public sealed class DefeatPanel : MonoBehaviour
    {
        UiAnim _anim;
        RectTransform _root;

        public RectTransform Root => _root;

        public static DefeatPanel Show(RectTransform layer, DefeatArgs a)
        {
            var dim = UiKit.Dimmer(layer);
            dim.GetComponent<Image>().color = new Color(0.07f, 0.06f, 0.06f, 0f);
            var p = dim.gameObject.AddComponent<DefeatPanel>();
            p._root = dim;
            p._anim = UiAnim.On(p);
            ResultKit.SkipCatcher(dim, () => { if (p._anim.Playing) p._anim.Finish(); });
            var stage = ResultKit.Group(dim, "stage", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));
            float t = DefeatFx.Play(dim, stage, p._anim);
            p.Build(stage, a, t);
            AudioBus.Sweep(dim);
            return p;
        }

        void Build(RectTransform stage, DefeatArgs a, float at)
        {
            StageDef def = StageCatalog.Get(a.Stage);
            int pct = Mathf.Clamp(Mathf.RoundToInt(a.Progress * 100f), 1, 99);

            var sub = UiKit.Label(stage, "sub", $"第 {a.Stage + 1} 关 · {def.Title}", 28,
                new Vector2(0f, 70f), new Vector2(560f, 40f));
            sub.color = InkTheme.Hex("D8CFC4");
            _anim.Fade(sub, at - 0.3f, 0.3f, 0f, 1f);

            var prog = ResultKit.Group(stage, "prog", new Vector2(0f, 16f), new Vector2(520f, 60f));
            var pl = UiKit.Label(prog, "l", "进度", 26, new Vector2(-222f, 0f), new Vector2(80f, 40f));
            pl.color = InkTheme.Hex("EDE8DF");
            var fill = ResultKit.Bar(prog, new Vector2(20f, 0f), 340f, 30f, InkTheme.Hex("F58A34"));
            var pv = UiKit.Label(prog, "v", "0%", 30, new Vector2(232f, 0f), new Vector2(90f, 40f));
            UiKit.Bold(pv);
            pv.color = InkTheme.Hex("F6BE3C");
            float k0 = a.Progress;
            _anim.Pop(prog, at, 0.3f)
                 .Tween(at + 0.2f, 0.7f, k => ResultKit.SetBar(fill, k0 * Ease.OutCubic(k)))
                 .CountUp(pv, 0, pct, at + 0.2f, 0.7f, "{0}%", AudioBus.CountTick);

            var head = UiKit.Label(stage, "tip", "提升以下能力，下次更稳", 30, new Vector2(0f, -46f), new Vector2(560f, 44f));
            UiKit.Bold(head);
            head.color = InkTheme.Hex("FFE7B8");
            _anim.Fade(head, at + 0.5f, 0.3f, 0f, 1f);

            float t = BuildAdvice(stage, a, at + 0.65f);
            BuildButtons(stage, a, t);
        }

        float BuildAdvice(RectTransform stage, DefeatArgs a, float at)
        {
            List<Advice> list = a.Advice ?? new List<Advice>();
            int n = list.Count;
            const float W = 196f, H = 236f, Gap = 16f;
            float span = n * W + (n - 1) * Gap;
            for (int i = 0; i < n; i++)
            {
                Advice ad = list[i];
                float x = -span * 0.5f + W * 0.5f + i * (W + Gap);
                var card = ResultKit.Group(stage, "adv" + i, new Vector2(x, -192f), new Vector2(W, H));
                UiKit.Stroke(card, "bg", Vector2.zero, new Vector2(W, H), Pin.Center, 5f,
                    fill: ad.Ready ? InkTheme.Hex("FFF6E2") : InkTheme.CardFace, radius: 26f);
                if (ad.Icon != null) UiKit.Icon(card, ad.Icon, new Vector2(0f, 70f), 76f);
                var title = UiKit.Label(card, "t", ad.Title, 26, new Vector2(0f, 16f), new Vector2(W - 12f, 36f));
                UiKit.Bold(title);
                var line = UiKit.Label(card, "l", ad.Line, 19, new Vector2(0f, -20f), new Vector2(W - 24f, 40f));
                line.horizontalOverflow = HorizontalWrapMode.Wrap;
                line.color = InkTheme.TextMid;
                var need = UiKit.Label(card, "n", ad.Need, 20, new Vector2(0f, -54f), new Vector2(W - 12f, 30f));
                need.color = ad.Ready ? InkTheme.Accel : InkTheme.Rose;
                UiKit.Bold(need);

                if (ad.Tab >= 0 && a.Improve != null)
                {
                    int tab = ad.Tab;
                    var go = UiKit.Btn(card, "go", "去提升", new Vector2(0f, -94f), new Vector2(150f, 52f),
                        () => a.Improve(tab), ad.Ready);
                    var gt = go.transform.Find("face/t")?.GetComponent<Text>();
                    if (gt != null) gt.fontSize = 24;
                }
                float t0 = at + i * 0.12f;
                _anim.Pop(card, t0, 0.34f).At(t0 + 0.05f, AudioBus.Tap);
                if (ad.Ready && ad.Tab >= 0) _anim.Breathe(card, t0 + 0.5f, 0.025f, 1.2f);
            }
            return at + n * 0.12f + 0.3f;
        }

        void BuildButtons(RectTransform stage, DefeatArgs a, float at)
        {
            var g = ResultKit.Group(stage, "retry", new Vector2(0f, -392f), new Vector2(440f, 108f));
            var btn = UiKit.Btn(g, "b", "再来一次", Vector2.zero, new Vector2(440f, 108f), () =>
            {
                if (a.CanRetry) a.Retry?.Invoke();
                else AudioBus.Deny();
            });
            var label = btn.transform.Find("face/t")?.GetComponent<Text>();
            if (label != null) label.fontSize = 36;
            ResultKit.CostTag(btn, a.RetryCost);
            if (!a.CanRetry) UiKit.PaintBtn(btn, InkTheme.CtaOff, InkTheme.Hex("5E5A54"), InkTheme.CardFace);
            _anim.Pop(g, at, 0.34f);
            if (a.CanRetry) _anim.Breathe(g, at + 0.4f, 0.04f, 1.3f);

            var hg = ResultKit.Group(stage, "home", new Vector2(0f, -500f), new Vector2(220f, 70f));
            UiKit.Btn(hg, "b", "回首页", Vector2.zero, new Vector2(220f, 70f), () => a.Home?.Invoke(), false);
            _anim.Pop(hg, at + 0.12f, 0.3f);
        }
    }
}
