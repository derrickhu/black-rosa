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
        public Action DoubleInk;          // 看一次广告：墨翻倍并当场开这关的宝箱；null：两样都没得给
        public string DoubleText;
        public Action Home;
        public Action Forge;              // 解锁卡上「去炮台」
        public Action<int> Skin;          // 新炮台卡上「去看看」，炮台页停在那一款
        // 新手指引：只弹词条卡，卡上只留「去炮台」。卡摆好后把按钮交出去给遮罩镂空；没有卡就交 null。
        public bool Guide;
        public Action<RectTransform> OnCard;
    }

    // 通关页。一整段编排：光芒转起来 → 横幅砸下 → 三颗星依次砸进星座 → 彩带 →
    // 奖励卡弹出滚数 → 下一关有新字才预告 → 按钮。点屏幕任意处跳到最后。
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
        RectTransform _home;
        float _homeParkY;
        int _ink;
        Text _chestNote;

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
            var stage0 = ResultKit.Group(_root, "stage", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));

            // 光芒 + 横幅
            var rays = ResultKit.Rays(stage0, new Vector2(0f, 360f), 820f, new Color(1f, 0.86f, 0.52f, 0.85f));
            _anim.Fade(rays, 0f, 0.5f, 0f, 0.85f).Spin(rays.transform, 16f).Pop(rays.transform, 0f, 0.6f, 0.4f);

            var banner = ResultKit.Banner(stage0, true, "通关！", new Vector2(0f, 372f), 600f);
            _anim.Move(banner, new Vector2(0f, 820f), new Vector2(0f, 372f), 0.05f, 0.42f, Ease.OutBack)
                 .Punch(banner, 0.47f, 0.10f, 0.3f)
                 .At(0.42f, AudioBus.Stamp);

            var sub = UiKit.Label(stage0, "sub", $"第 {a.Stage + 1} 关", 30,
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

            t = BuildRewards(stage0, a, t + 0.1f);
            bool teased = BuildTeaser(stage0, a, t + 0.1f, out t);
            BuildButtons(stage0, a, t + 0.1f, teased ? NextY : -248f);
            int[] cards = RevealCards(info);
            if (a.Guide)
            {
                cards = System.Array.FindAll(cards, c => c >= 0);
                if (cards.Length > 0) cards = new[] { cards[0] };
                else
                {
                    _anim.At(t + 0.7f, () => a.OnCard?.Invoke(null));
                    return;
                }
            }
            if (cards.Length > 0)
                _anim.At(t + 0.7f, () => Reveal(a, cards, 0));
        }

        // 新炮台排在新词条前面：炮台是大件，先看。皮肤记成 -(下标+1)，词条照原下标。
        static int[] RevealCards(ResultInfo info)
        {
            var all = new List<int>();
            if (info.NewSkins != null)
                foreach (int s in info.NewSkins) all.Add(-(s + 1));
            if (info.NewLines != null) all.AddRange(info.NewLines);
            return all.ToArray();
        }

        // 解锁卡：结算演完再整屏盖上来，一样一张，点「知道了」看下一张。新炮台和新词条共用这套编排。
        void Reveal(VictoryArgs a, int[] lines, int k)
        {
            if (k >= lines.Length) return;
            int line = lines[k];
            bool skin = line < 0;
            int skinId = skin ? -line - 1 : -1;
            string ribbon, title, briefText, stepText, footText, goText;
            Sprite art;
            float artSize;
            if (skin)
            {
                SkinDef sd = SkinCatalog.Get(skinId);
                ribbon = "新炮台开放";
                title = sd.Name;
                briefText = sd.Note;
                stepText = SkinCatalog.PerkOf(skinId) + "  ·  " + sd.Shot;
                footText = sd.Way == SkinWay.Ink
                    ? sd.Price + " 墨就能买，买下自动换上"
                    : SkinCatalog.LockText(sd);
                goText = "去看看";
                art = InkSprites.Ui("skin_" + sd.Key);
                artSize = 270f;
            }
            else
            {
                ForgeDef d = ForgeCatalog.Get(line);
                ribbon = "新词条解锁";
                title = d.Name;
                briefText = d.Brief;
                stepText = "每升一级  " + ForgeCatalog.Step(line);
                footText = "去炮台页升级，下一局就生效";
                goText = "去炮台";
                art = InkSprites.Ui(d.Icon);
                artSize = 210f;
            }
            var dim = UiKit.Dimmer(_root);
            dim.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.03f, 0.82f);
            dim.SetAsLastSibling();
            ResultKit.SkipCatcher(dim, Skip);
            var g = ResultKit.Group(dim, "unlock", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));

            var rays = ResultKit.Rays(g, new Vector2(0f, 120f), 780f, new Color(1f, 0.82f, 0.42f, 0.9f));
            _anim.Fade(rays, 0f, 0.4f, 0f, 0.9f).Spin(rays.transform, 22f).Pop(rays.transform, 0f, 0.5f, 0.3f);

            var rib = UiKit.Stroke(g, "rib", new Vector2(0f, 340f), new Vector2(340f, 66f), Pin.Center, 5f,
                fill: InkTheme.Seal, radius: 30f);
            var rt = UiKit.Label(rib, "t", ribbon, 34, Vector2.zero, new Vector2(340f, 66f));
            rt.color = Color.white;
            UiKit.Bold(rt);
            _anim.Move(rib, new Vector2(0f, 700f), new Vector2(0f, 340f), 0.05f, 0.36f, Ease.OutBack)
                 .Punch(rib, 0.41f, 0.12f, 0.3f)
                 .At(0.38f, AudioBus.Stamp);
            if (lines.Length > 1)
            {
                var n = UiKit.Label(g, "count", (k + 1) + "/" + lines.Length, 24,
                    new Vector2(220f, 340f), new Vector2(80f, 36f));
                n.color = InkTheme.Hex("FFE7B8");
                UiKit.Bold(n);
            }

            var icon = UiKit.Icon(g, art, new Vector2(0f, 130f), artSize);
            _anim.Pop(icon.transform, 0.45f, 0.45f, 0f)
                 .Breathe(icon.transform, 1.0f, 0.05f, 1.2f)
                 .At(0.6f, () =>
                 {
                     AudioBus.Unlock();
                     UiConfetti.Sparks(_root, new Vector2(0f, 130f), InkTheme.GoldHi, 22, 640f);
                 });

            var name = ResultKit.Headline(g, "name", title, 60, new Vector2(0f, -30f),
                InkTheme.Hex("FFF3C8"), InkTheme.Hex("7A1E14"), 3f);
            var brief = UiKit.Label(g, "brief", briefText, 28, new Vector2(0f, -100f), new Vector2(600f, 40f));
            brief.color = InkTheme.Hex("FFE7B8");
            var step = UiKit.Label(g, "step", stepText, 26,
                new Vector2(0f, -150f), new Vector2(600f, 38f));
            step.color = InkTheme.Hex("8FE3A2");
            UiKit.Bold(step);
            var foot = UiKit.Label(g, "foot", footText, 22,
                new Vector2(0f, -196f), new Vector2(600f, 32f));
            foot.color = InkTheme.Hex("CDBFA8");
            _anim.Fade(name, 0.75f, 0.25f, 0f, 1f).Pop(name.transform, 0.75f, 0.3f, 1.6f)
                 .Fade(brief, 0.9f, 0.25f, 0f, 1f)
                 .Fade(step, 1.0f, 0.25f, 0f, 1f)
                 .Fade(foot, 1.1f, 0.25f, 0f, 1f);

            bool last = k + 1 >= lines.Length;
            var go = ResultKit.Group(g, "go", new Vector2(a.Guide ? 0f : -136f, -310f), new Vector2(232f, 88f));
            UiKit.Btn(go, "b", goText, Vector2.zero, new Vector2(232f, 88f), () =>
            {
                if (skin && a.Skin != null) a.Skin(skinId);
                else if (a.Forge != null) a.Forge();
                else Destroy(dim.gameObject);
            }, a.Guide);
            if (a.Guide)
            {
                _anim.Pop(go, 1.2f, 0.3f).At(1.55f, () => a.OnCard?.Invoke(go));
                return;
            }
            var ok = ResultKit.Group(g, "ok", new Vector2(136f, -310f), new Vector2(232f, 88f));
            UiKit.Btn(ok, "b", last ? "知道了" : "下一个", Vector2.zero, new Vector2(232f, 88f), () =>
            {
                Destroy(dim.gameObject);
                if (!last) Reveal(a, lines, k + 1);
            }, true);
            _anim.Pop(go, 1.2f, 0.3f).Pop(ok, 1.28f, 0.3f).Breathe(ok, 1.6f, 0.045f, 1.3f);
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

        float BuildRewards(RectTransform parent, VictoryArgs a, float at)
        {
            ResultInfo info = a.Info;
            var list = new List<(string icon, int value, string tag)>();
            list.Add(("ink", info.Ink, info.DailyDouble ? "首胜×2" : null));
            if (info.Diamonds > 0) list.Add(("diamond", info.Diamonds, "首通"));
            bool chest = info.Chest >= 0;

            var head = UiKit.Label(parent, "rh", "本关收获", 26, new Vector2(0f, RewardY + 106f), new Vector2(300f, 36f));
            head.color = InkTheme.Hex("FFE7B8");
            _anim.Fade(head, at, 0.25f, 0f, 1f);

            const float W = 168f, H = 150f, Gap = 18f, ChestW = 200f;
            int n = list.Count + (chest ? 1 : 0);
            float span = list.Count * W + (chest ? ChestW : 0f) + (n - 1) * Gap;
            float x = -span * 0.5f;
            for (int i = 0; i < list.Count; i++)
            {
                var (icon, value, tag) = list[i];
                float cx = x + W * 0.5f;
                x += W + Gap;
                var card = ResultKit.Group(parent, "rw" + i, new Vector2(cx, RewardY), new Vector2(W, H));
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
                    _ink = info.Ink;
                }
                if (tag != null)
                {
                    Stamp(card, tag, new Vector2(W * 0.40f, H * 0.52f), -12f, InkTheme.Seal, t0 + 0.7f);
                }
            }
            float end = at + list.Count * 0.14f + 0.7f;
            if (chest) end = Mathf.Max(end, BuildChest(parent, a, new Vector2(x + ChestW * 0.5f, RewardY), ChestW, H, at + list.Count * 0.14f));
            return end;
        }

        // 宝箱从天上砸下来、弹两下落进卡里。底下一行告诉玩家不开会怎样：进宝箱位，或位满了离开后再选。
        float BuildChest(RectTransform parent, VictoryArgs a, Vector2 pos, float w, float h, float at)
        {
            ResultInfo info = a.Info;
            ChestDef d = ChestCatalog.Get(info.Chest);
            var card = ResultKit.Group(parent, "chest", pos, new Vector2(w, h));
            UiKit.Stroke(card, "bg", Vector2.zero, new Vector2(w, h), Pin.Center, 5f,
                fill: InkTheme.Hex("FFF6E2"), radius: 26f);
            var glow = UiKit.Icon(card, InkSprites.Load("Ui/result_rays"), new Vector2(0f, 24f), 170f);
            glow.color = new Color(1f, 0.82f, 0.4f, 0.75f);
            _anim.Spin(glow.transform, 26f).Fade(glow, at + 0.45f, 0.3f, 0f, 0.75f);
            var box = ResultKit.Group(card, "box", new Vector2(0f, 24f), new Vector2(100f, 100f));
            UiKit.Icon(box, InkSprites.Load("Ui/chest_" + d.Key), Vector2.zero, 100f);
            var name = UiKit.Label(card, "n", d.Name, 22, new Vector2(0f, -32f), new Vector2(w, 30f));
            UiKit.Bold(name);
            name.color = InkTheme.TextDark;
            _anim.Pop(card, at, 0.3f)
                 .Move(box, new Vector2(0f, 420f), new Vector2(0f, 24f), at + 0.1f, 0.5f, Ease.OutBounce)
                 .At(at + 0.32f, AudioBus.ChestLand)
                 .Punch(box, at + 0.6f, 0.18f, 0.3f);

            string note = info.ChestFull ? "位满了，离开后再选" : "不开就放进宝箱位";
            _chestNote = UiKit.Label(card, "note", note, 18, new Vector2(0f, -h * 0.5f - 18f), new Vector2(w + 80f, 26f));
            _chestNote.color = info.ChestFull ? InkTheme.Hex("FFE1A8") : InkTheme.Hex("FFE7B8");
            _anim.Fade(_chestNote, at + 0.7f, 0.25f, 0f, 1f);
            return at + 0.9f;
        }

        // 没有新字就不预告下一关。有新字才留一张字卡。
        // 重打已经通关的关时，下一关早就解锁过了，不再预告。
        bool BuildTeaser(RectTransform parent, VictoryArgs a, float at, out float end)
        {
            end = at;
            if (!a.Info.FirstClear) return false;
            int next = a.Stage + 1;
            if (next >= GameConstants.StageCount) return false;
            List<CardId> fresh = StageCatalog.NewCards(next);
            if (fresh.Count == 0) return false;
            end = BuildFresh(parent, fresh, at);
            return true;
        }

        // 下一关新给的字。只亮字，效果进了关自己能看见。
        float BuildFresh(RectTransform parent, List<CardId> fresh, float at)
        {
            bool one = fresh.Count == 1;
            var host = ResultKit.Group(parent, "teaser", new Vector2(0f, TeaserY), new Vector2(640f, one ? 168f : 150f));
            var rib = UiKit.Stroke(host, "rib", new Vector2(0f, one ? 70f : 64f), new Vector2(250f, 44f), Pin.Center, 4f,
                fill: InkTheme.Seal, radius: 22f);
            var rt = UiKit.Label(rib, "t", "下一关解锁新字", 24, Vector2.zero, new Vector2(250f, 44f));
            rt.color = Color.white;
            UiKit.Bold(rt);

            int n = fresh.Count;
            float icon = one ? 108f : Mathf.Min(96f, (520f - (n - 1) * 16f) / n);
            float step = icon + 16f;
            float x0 = -(n - 1) * step * 0.5f;
            float glyphY = one ? -16f : -18f;
            _anim.Pop(host, at, 0.34f).At(at + 0.3f, AudioBus.Unlock);
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * step;
                var glow = UiKit.Icon(host, InkSprites.Load("Ui/result_rays"), new Vector2(x, glyphY), icon + 64f);
                glow.color = new Color(1f, 0.78f, 0.35f, 0.9f);
                _anim.Spin(glow.transform, -30f);
                var heap = UiKit.Icon(host, InkSprites.Heap(fresh[i]), new Vector2(x, glyphY), icon);
                _anim.Pop(heap.transform, at + 0.25f + i * 0.08f, 0.4f, 0f)
                     .Breathe(heap.transform, at + 0.7f, 0.06f, 1.1f);
            }
            return at + 0.5f;
        }

        void BuildButtons(RectTransform parent, VictoryArgs a, float at, float y)
        {
            bool hasNext = a.Next != null && a.NextCost >= 0;
            bool ad = a.DoubleInk != null;
            const float nextW = 400f, nextH = 90f;
            const float adW = 460f, adH = 80f;
            const float gap = 14f;
            const float floor = -618f;
            float homeH = !hasNext && !ad ? 100f : 76f;
            float homeW = !hasNext && !ad ? 360f : 280f;

            float yNext = y, yAd = y, yHome = y;
            if (hasNext)
                yHome = y - nextH * 0.5f - gap - (ad ? adH + gap : 0f) - homeH * 0.5f;
            if (ad)
            {
                yAd = hasNext ? y - nextH * 0.5f - gap - adH * 0.5f : y;
                yHome = yAd - adH * 0.5f - gap - homeH * 0.5f;
            }
            float bottom = yHome - homeH * 0.5f;
            if (bottom < floor)
            {
                float lift = floor - bottom;
                yNext += lift;
                yAd += lift;
                yHome += lift;
            }

            if (hasNext)
            {
                var g = ResultKit.Group(parent, "next", new Vector2(0f, yNext), new Vector2(nextW, nextH));
                var btn = UiKit.Btn(g, "b", "下一关", Vector2.zero, new Vector2(nextW, nextH), () =>
                {
                    if (a.NextAffordable) a.Next();
                    else AudioBus.Deny();
                });
                var label = btn.transform.Find("face/t")?.GetComponent<Text>();
                if (label != null) label.fontSize = 34;
                ResultKit.CostTag(btn, a.NextCost);
                if (!a.NextAffordable) UiKit.PaintBtn(btn, InkTheme.CtaOff, InkTheme.Hex("5E5A54"), InkTheme.CardFace);
                _anim.Pop(g, at, 0.36f);
                if (a.NextAffordable) _anim.Breathe(g, at + 0.4f, 0.045f, 1.3f);
            }

            if (ad)
            {
                var g = ResultKit.Group(parent, "double", new Vector2(0f, yAd), new Vector2(adW, adH));
                _double = UiKit.Btn(g, "b", a.DoubleText ?? "墨翻倍", Vector2.zero, new Vector2(adW, adH), a.DoubleInk, false);
                UiKit.PaintBtn(_double, InkTheme.CoinFace, InkTheme.CoinDeep, InkTheme.TextDark);
                ResultKit.AdMark(_double, 36f);
                var dl = _double.GetComponentInChildren<Text>();
                if (dl != null) dl.fontSize = 26;
                _anim.Pop(g, at + 0.12f, 0.32f);
                _homeParkY = yAd;
            }

            _home = ResultKit.Group(parent, "home", new Vector2(0f, yHome), new Vector2(homeW, homeH));
            UiKit.Btn(_home, "b", "回首页", Vector2.zero, _home.sizeDelta, () => a.Home?.Invoke(), !hasNext && !ad);
            _anim.Pop(_home, at + (ad ? 0.24f : 0.12f), 0.3f);
        }

        // 看完广告（开箱演出收下之后）：墨数字从现值滚到两倍，按钮收起来。
        public void Doubled(bool chestOpened)
        {
            if (_anim.Playing) _anim.Finish();
            if (_double != null)
            {
                _double.interactable = false;
                _double.transform.parent.gameObject.SetActive(false);
            }
            if (_home != null)
                _home.anchoredPosition = new Vector2(0f, _homeParkY);
            if (chestOpened && _chestNote != null)
            {
                _chestNote.text = "已打开";
                _chestNote.color = InkTheme.Hex("B8F0A8");
            }
            if (_inkText == null || _ink <= 0) return;
            _anim.CountUp(_inkText, _ink, _ink * 2, 0.05f, 0.7f, "+{0}", AudioBus.CountTick)
                 .Punch(_inkCard, 0.75f, 0.2f, 0.35f)
                 .At(0.75f, () => UiConfetti.Sparks(_root, _inkCard.anchoredPosition, InkTheme.GoldHi, 16, 600f));
            _ink *= 2;
        }
    }
}
