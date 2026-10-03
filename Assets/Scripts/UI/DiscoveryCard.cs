using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 第一次打出新字、新词、隐藏组合时弹一下。只留横幅、这个字、一行效果说明。
    // 炮弹长什么样，战场上自己看得到，这里不再画字图和预览。
    public static class DiscoveryCard
    {
        const float BannerW = 560f;

        public static void Show(RectTransform layer, CodexKind kind, int index, Action done)
        {
            bool pair = kind == CodexKind.Pair;
            var dim = UiKit.Dimmer(layer);
            dim.name = "discovery";
            dim.GetComponent<Image>().color = new Color(0.10f, 0.07f, 0.05f, 0.78f);
            var anim = UiAnim.On(dim);
            ResultKit.SkipCatcher(dim, anim.Finish);
            var g = ResultKit.Group(dim, "g", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));

            Sprite spr = InkSprites.Load("Ui/discover_banner");
            float bannerH = spr != null && spr.rect.width > 1f
                ? BannerW * spr.rect.height / spr.rect.width
                : 180f;
            const float bannerY = 150f;
            var rib = UiKit.Art(g, "rib", "Ui/discover_banner", new Vector2(0f, bannerY), new Vector2(BannerW, bannerH));
            var title = UiKit.Label(rib, "t", Ribbon(kind), 36, new Vector2(0f, -10f), new Vector2(BannerW * 0.58f, 52f));
            title.color = InkTheme.TextDark;
            UiKit.Bold(title);
            anim.Move(rib, new Vector2(0f, 720f), new Vector2(0f, bannerY), 0.02f, 0.32f, Ease.OutBack)
                .At(0.26f, AudioBus.Stamp);

            float nameY = bannerY - bannerH * 0.5f - 86f;
            var name = ResultKit.Headline(g, "name", CodexCatalog.Title(kind, index), 96,
                new Vector2(0f, nameY), InkTheme.Hex("FFF6E4"), InkTheme.Outline, 4f);
            anim.Fade(name, 0.24f, 0.18f, 0f, 1f).Pop(name.transform, 0.24f, 0.3f, 1.35f);

            var desc = UiKit.Label(g, "desc", EffectLine(kind, index), 32,
                new Vector2(0f, nameY - 92f), new Vector2(640f, 96f));
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.lineSpacing = 1.05f;
            desc.color = pair ? InkTheme.GoldHi : InkTheme.FireHi;
            UiKit.Bold(desc);
            anim.Fade(desc, 0.4f, 0.22f, 0f, 1f)
                .At(0.36f, () =>
                {
                    AudioBus.Unlock();
                    UiConfetti.Sparks(dim, new Vector2(0f, nameY), desc.color, 14, 380f);
                });

            var ok = ResultKit.Group(g, "ok", new Vector2(0f, nameY - 210f), new Vector2(260f, 84f));
            UiKit.Btn(ok, "b", "知道了", Vector2.zero, new Vector2(260f, 84f), () =>
            {
                AudioBus.Tap();
                UnityEngine.Object.Destroy(dim.gameObject);
                done?.Invoke();
            }, true);
            anim.Pop(ok, 0.52f, 0.26f);
        }

        static string Ribbon(CodexKind kind) =>
            kind == CodexKind.Pair ? "发现隐藏组合" : kind == CodexKind.Word ? "成词 · 新词" : "解锁新字";

        static string EffectLine(CodexKind kind, int index)
        {
            if (kind == CodexKind.Glyph) return CodexCatalog.StarLine((CardId)index, 1);
            if (kind == CodexKind.Word) return CodexCatalog.WordStarLine((WordId)index, 1);
            return SignaturePairs.All[index].Note;
        }
    }
}
