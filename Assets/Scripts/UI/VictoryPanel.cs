using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class VictoryArgs
    {
        public int Stage;
        public ResultInfo Info;
        public int NextCost = -1;         // <0：没有下一关（最后一关或未解锁）
        public bool NextAffordable;
        public Action Next;
        public Action DoubleInk;          // null：这局不给翻倍（没捡到墨 / 已翻过）
        public Action Home;
    }

    // 通关页。一整段编排：光芒转起来 → 横幅砸下 → 三颗星依次砸进星座 → 彩带 →
    // 奖励卡弹出滚数 → 下一关预告 → 按钮。点屏幕任意处跳到最后。
    public sealed class VictoryPanel : MonoBehaviour
    {
        const float StarY = 150f;
        const float RewardY = -40f;
        const float TeaserY = -238f;
        const float NextY = -414f;

        static readonly Vector2[] StarPos = { new Vector2(-150f, 0f), new Vector2(0f, 26f), new Vector2(150f, 0f) };
        static readonly float[] StarSize = { 122f, 150f, 122f };
        static readonly float[] StarTilt = { 14f, 0f, -14f };

        UiAnim _anim;
        RectTransform _root;
        Text _inkText;
        RectTransform _inkCard;
        Button _double;
        int _ink;

        public RectTransform Root => _root;

        public static VictoryPanel Show(RectTransform layer, VictoryArgs a)
        {
            var dim = UiKit.Dimmer(layer);
            dim.GetComponent<Image>().color = new Color(0.10f, 0.07f, 0.05f, 0.78f);
            var p = dim.gameObject.AddComponent<VictoryPanel>();
            p._root = dim;
            p._anim = UiAnim.On(p);
            ResultKit.SkipCatcher(dim, p.Skip);
            p.Build(a);
            AudioBus.Sweep(dim);
            return p;
        }

        void Skip()
        {
            if (_anim.Playing) _anim.Finish();
        }

        void Build(VictoryArgs a)
        {
            ResultInfo info = a.Info;
            StageDef stage = StageCatalog.Get(a.Stage);
            var stage0 = ResultKit.Group(_root, "stage", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));

            // 光芒 + 横幅
            var rays = ResultKit.Rays(stage0, new Vector2(0f, 360f), 820f, new Color(1f, 0.86f, 0.52f, 0.85f));
            _anim.Fade(rays, 0f, 0.5f, 0f, 0.85f).Spin(rays.transform, 16f).Pop(rays.transform, 0f, 0.6f, 0.4f);

            var banner = ResultKit.Banner(stage0, true, "通关！", new Vector2(0f, 372f), 600f);
            _anim.Move(banner, new Vector2(0f, 820f), new Vector2(0f, 372f), 0.05f, 0.42f, Ease.OutBack)
                 .Punch(banner, 0.47f, 0.10f, 0.3f)
                 .At(0.42f, AudioBus.Stamp);

            var sub = UiKit.Label(stage0, "sub", $"第 {a.Stage + 1} 关 · {stage.Title}", 30,
                new Vector2(0f, 258f), new Vector2(560f, 44f));
            sub.color = InkTheme.Hex("FFE7B8");
            UiKit.Bold(sub);
            _anim.Fade(sub, 0.45f, 0.3f, 0f, 1f);

            BuildStars(stage0, info);
            float t = 0.95f + info.Stars * 0.3f;
            _anim.At(t - 0.1f, () =>
            {
                UiConfetti.Burst(_root, info.Stars >= 3 ? 110 : 80);
                AudioBus.Confetti();
            });

            t = BuildRewards(stage0, info, t + 0.1f);
            t = BuildTeaser(stage0, a, t + 0.1f);
            BuildButtons(stage0, a, t + 0.1f);
        }

        void BuildStars(RectTransform parent, ResultInfo info)
        {
            var row = ResultKit.Group(parent, "stars", new Vector2(0f, StarY), new Vector2(460f, 180f));
            for (int i = 0; i < 3; i++)
            {
                var slot = ResultKit.Group(row, "s" + i, StarPos[i], Vector2.one * StarSize[i]);
                slot.localRotation = Quaternion.Euler(0f, 0f, StarTilt[i]);
                var off = UiKit.Icon(slot, InkSprites.Load("Ui/result_star_off"), Vector2.zero, StarSize[i]);
                _anim.Pop(off.transform, 0.3f + i * 0.06f, 0.3f);
                if (i >= info.Stars) continue;

                var on = UiKit.Icon(slot, InkSprites.Load("Ui/result_star_on"), Vector2.zero, StarSize[i]);
                float at = 0.8f + i * 0.3f;
                int idx = i;
                Vector2 spark = StarPos[i] + new Vector2(0f, StarY);
                on.transform.localScale = Vector3.zero;
                _anim.Pop(on.transform, at, 0.26f, 2.6f)
                     .Fade(on, at, 0.12f, 0f, 1f)
                     .Punch(slot, at + 0.24f, 0.16f, 0.32f)
                     .At(at + 0.22f, () =>
                     {
                         AudioBus.StarLand(idx);
                         UiConfetti.Sparks(_root, spark, InkTheme.GoldHi, 14, 560f);
                     });
            }

            if (info.Stars >= 3)
                _anim.Shake(parent, 0.8f + 2 * 0.3f + 0.22f, 12f, 0.28f);

            if (info.NewBest && !info.FirstClear)
                Stamp(parent, "新纪录", new Vector2(218f, StarY + 86f), 16f, InkTheme.Rose, 0.8f + info.Stars * 0.3f);
            else if (info.FirstClear)
                Stamp(parent, "首通", new Vector2(218f, StarY + 86f), 16f, InkTheme.Rose, 0.8f + info.Stars * 0.3f);
        }

        // 斜着盖上去的小红章。
        RectTransform Stamp(RectTransform parent, string text, Vector2 pos, float tilt, Color col, float at)
        {
            var g = ResultKit.Group(parent, "stamp", pos, new Vector2(128f, 54f));
            g.localRotation = Quaternion.Euler(0f, 0f, tilt);
            UiKit.Stroke(g, "bg", Vector2.zero, new Vector2(128f, 54f), Pin.Center, 4f, fill: col, radius: 14f);
            var t = UiKit.Label(g, "t", text, 26, Vector2.zero, new Vector2(128f, 54f));
            t.color = Color.white;
            UiKit.Bold(t);
            _anim.Pop(g, at, 0.24f, 2.2f).At(at + 0.2f, AudioBus.Stamp);
            return g;
        }

        float BuildRewards(RectTransform parent, ResultInfo info, float at)
        {
            var list = new List<(string icon, int value, string tag)>();
            list.Add(("ink", info.Ink, info.DailyDouble ? "首胜×2" : null));
            int shards = info.Shards + (info.FinaleShard >= 0 ? 1 : 0);
            if (shards > 0) list.Add(("shard", shards, null));

            var head = UiKit.Label(parent, "rh", "本关收获", 26, new Vector2(0f, RewardY + 106f), new Vector2(300f, 36f));
            head.color = InkTheme.Hex("FFE7B8");
            _anim.Fade(head, at, 0.25f, 0f, 1f);

            const float W = 176f, H = 150f, Gap = 22f;
            float span = list.Count * W + (list.Count - 1) * Gap;
            for (int i = 0; i < list.Count; i++)
            {
                var (icon, value, tag) = list[i];
                float x = -span * 0.5f + W * 0.5f + i * (W + Gap);
                var card = ResultKit.Group(parent, "rw" + i, new Vector2(x, RewardY), new Vector2(W, H));
                UiKit.Stroke(card, "bg", Vector2.zero, new Vector2(W, H), Pin.Center, 5f, radius: 26f);
                UiKit.Icon(card, InkSprites.Ui(icon), new Vector2(0f, 22f), 72f);
                var num = UiKit.Label(card, "n", "+0", 36, new Vector2(0f, -42f), new Vector2(W, 48f));
                UiKit.Bold(num);
                num.color = InkTheme.TextDark;

                float t0 = at + i * 0.14f;
                _anim.Pop(card, t0, 0.32f)
                     .CountUp(num, 0, value, t0 + 0.2f, 0.6f, "+{0}", AudioBus.CountTick);
                if (icon == "ink")
                {
                    _inkText = num;
                    _inkCard = card;
                    _ink = value;
                }
                if (tag != null)
                {
                    Stamp(card, tag, new Vector2(W * 0.40f, H * 0.52f), -12f, InkTheme.Seal, t0 + 0.7f);
                }
            }
            return at + list.Count * 0.14f + 0.7f;
        }

        float BuildTeaser(RectTransform parent, VictoryArgs a, float at)
        {
            int next = a.Stage + 1;
            if (next >= GameConstants.StageCount) return at;
            StageDef nd = StageCatalog.Get(next);
            List<CardId> fresh = StageCatalog.NewCards(next);
            bool chapter = next % GameConstants.ChapterSize == 0;

            var box = ResultKit.Group(parent, "teaser", new Vector2(0f, TeaserY), new Vector2(600f, 156f));
            UiKit.Stroke(box, "bg", Vector2.zero, new Vector2(600f, 156f), Pin.Center, 5f,
                fill: InkTheme.Hex("FFF6E2"), radius: 28f);

            string ribbon = fresh.Count > 0 ? "下一关解锁新字" : chapter ? "新章节开启" : "下一关";
            var rib = UiKit.Stroke(box, "rib", new Vector2(0f, 78f), new Vector2(250f, 44f), Pin.Center, 4f,
                fill: InkTheme.Seal, radius: 22f);
            var rt = UiKit.Label(rib, "t", ribbon, 24, Vector2.zero, new Vector2(250f, 44f));
            rt.color = Color.white;
            UiKit.Bold(rt);

            if (fresh.Count > 0)
            {
                CardId id = fresh[0];
                CardDef def = CardCatalog.Get(id);
                var glow = UiKit.Icon(box, InkSprites.Load("Ui/result_rays"), new Vector2(-206f, -6f), 190f);
                glow.color = new Color(1f, 0.78f, 0.35f, 0.9f);
                _anim.Spin(glow.transform, -30f);
                var heap = UiKit.Icon(box, InkSprites.Heap(id), new Vector2(-206f, -6f), 118f);
                var name = UiKit.Label(box, "name", $"「{def.Name}」", 40, new Vector2(40f, 18f),
                    new Vector2(400f, 52f), TextAnchor.MiddleLeft);
                UiKit.Bold(name);
                name.color = InkTheme.Accent(id) == InkTheme.Graphite ? InkTheme.TextDark : InkTheme.Accent(id);
                string more = fresh.Count > 1 ? $"  等 {fresh.Count} 个字" : "";
                var desc = UiKit.Label(box, "desc", def.Desc + more, 22, new Vector2(40f, -34f),
                    new Vector2(400f, 60f), TextAnchor.UpperLeft);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.color = InkTheme.TextMid;
                _anim.Pop(box, at, 0.34f)
                     .Pop(heap.transform, at + 0.25f, 0.4f, 0f)
                     .Breathe(heap.transform, at + 0.7f, 0.06f, 1.1f)
                     .At(at + 0.3f, AudioBus.Unlock);
            }
            else
            {
                int chap = nd.Chapter;
                string title = chapter ? SortiePageBuilder.ChapterTitle(chap) : $"第 {next + 1} 关 · {nd.Title}";
                string line = nd.HasBoss ? "首领坐镇，打过去拿大奖" : $"{nd.Waves.Length} 波敌人，越打越强";
                var tt = UiKit.Label(box, "name", title, 36, new Vector2(0f, 10f), new Vector2(560f, 50f));
                UiKit.Bold(tt);
                var ln = UiKit.Label(box, "desc", line, 24, new Vector2(0f, -38f), new Vector2(560f, 40f));
                ln.color = InkTheme.TextMid;
                _anim.Pop(box, at, 0.34f).At(at + 0.1f, AudioBus.Chime);
            }
            return at + 0.5f;
        }

        void BuildButtons(RectTransform parent, VictoryArgs a, float at)
        {
            float y = NextY;
            bool hasNext = a.Next != null && a.NextCost >= 0;
            if (hasNext)
            {
                var g = ResultKit.Group(parent, "next", new Vector2(0f, y), new Vector2(440f, 112f));
                var btn = UiKit.Btn(g, "b", "下一关", Vector2.zero, new Vector2(440f, 112f), () =>
                {
                    if (a.NextAffordable) a.Next();
                    else AudioBus.Deny();
                });
                var label = btn.transform.Find("face/t")?.GetComponent<Text>();
                if (label != null) label.fontSize = 40;
                ResultKit.CostTag(btn, a.NextCost);
                if (!a.NextAffordable) UiKit.PaintBtn(btn, InkTheme.CtaOff, InkTheme.Hex("5E5A54"), InkTheme.CardFace);
                _anim.Pop(g, at, 0.36f);
                if (a.NextAffordable) _anim.Breathe(g, at + 0.4f, 0.045f, 1.3f);
                y -= 112f;
            }

            if (a.DoubleInk != null && _ink > 0)
            {
                var g = ResultKit.Group(parent, "double", new Vector2(0f, y), new Vector2(400f, 86f));
                _double = UiKit.Btn(g, "b", "墨翻倍", Vector2.zero, new Vector2(400f, 86f), a.DoubleInk, false);
                ResultKit.AdMark(_double, 46f);
                _anim.Pop(g, at + 0.12f, 0.32f);
                y -= 92f;
            }

            var hg = ResultKit.Group(parent, "home", new Vector2(0f, y - 4f), new Vector2(hasNext ? 220f : 360f, hasNext ? 70f : 100f));
            UiKit.Btn(hg, "b", "回首页", Vector2.zero, hg.sizeDelta, () => a.Home?.Invoke(), !hasNext);
            _anim.Pop(hg, at + 0.24f, 0.3f);
        }

        // 看完翻倍广告：墨数字从现值滚到两倍，按钮收起来。
        public void Doubled()
        {
            if (_anim.Playing) _anim.Finish();
            if (_double != null)
            {
                _double.interactable = false;
                _double.transform.parent.gameObject.SetActive(false);
            }
            if (_inkText == null) return;
            _anim.CountUp(_inkText, _ink, _ink * 2, 0.05f, 0.7f, "+{0}", AudioBus.CountTick)
                 .Punch(_inkCard, 0.75f, 0.2f, 0.35f)
                 .At(0.75f, () => UiConfetti.Sparks(_root, _inkCard.anchoredPosition, InkTheme.GoldHi, 16, 600f));
            _ink *= 2;
        }
    }
}
