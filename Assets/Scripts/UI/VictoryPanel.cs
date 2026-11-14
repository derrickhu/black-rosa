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
        public Action OpenStarChest;      // 本章刚满星，当场打开炮台宝箱
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
        const float StarY = 158f;
        const float RewardY = -66f;
        const float TeaserY = -302f;
        const float NextY = -444f;

        static readonly Vector4 BorderTray = new Vector4(52f, 52f, 52f, 52f);

        static readonly Vector2[] StarPos = { new Vector2(-132f, 0f), new Vector2(0f, 16f), new Vector2(132f, 0f) };
        static readonly float[] StarSize = { 86f, 104f, 86f };
        static readonly float[] StarTilt = { 14f, 0f, -14f };

        UiAnim _anim;
        RectTransform _root;
        Text _inkText;
        RectTransform _inkCard;
        Button _double;
        RectTransform _home;
        float _homeParkY;
        int _ink;
        Text _foilText;
        int _foil;
        Text _chestNote;
        bool _starOpened;

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

            var banner = ResultKit.Banner(stage0, true, "通关！", new Vector2(0f, 396f), 600f);
            _anim.Move(banner, new Vector2(0f, 820f), new Vector2(0f, 396f), 0.05f, 0.42f, Ease.OutBack)
                 .Punch(banner, 0.47f, 0.10f, 0.3f)
                 .At(0.42f, AudioBus.Stamp);

            var sub = UiKit.Label(stage0, "sub", (info.Hard ? "精英 · " : "") + $"第 {a.Stage + 1} 关", 30,
                new Vector2(0f, 258f), new Vector2(560f, 44f));
            sub.color = InkTheme.Hex("FFE7B8");
            UiKit.Bold(sub);
            _anim.Fade(sub, 0.45f, 0.3f, 0f, 1f);

            BuildStars(stage0, info);
            BuildStarRule(stage0, a);
            float t = 0.95f + info.Stars * 0.3f;
            _anim.At(t - 0.1f, () =>
            {
                UiConfetti.Burst(_root, info.Stars >= 3 ? 110 : 80);
                AudioBus.Confetti();
            });

            t = BuildRewards(stage0, a, t + 0.1f);
            bool teased = BuildTeaser(stage0, a, t + 0.1f, out t);
            BuildButtons(stage0, a, t + 0.1f, teased ? NextY : -272f);
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

            var rib = UiKit.Stroke(g, "rib", new Vector2(0f, 340f), new Vector2(300f, 58f), Pin.Center, 4f,
                fill: InkTheme.Hex("E25B4A"), radius: 14f);
            var rt = UiKit.Label(rib, "t", ribbon, 26, Vector2.zero, new Vector2(280f, 58f));
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
                     AudioBus.UnlockSting();
                     UiConfetti.Sparks(_root, new Vector2(0f, 130f), InkTheme.GoldHi, 22, 640f);
                 });

            var name = ResultKit.LightTitle(g, "name", title, 48, new Vector2(0f, -30f),
                InkTheme.Hex("FFF3C8"));
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
            if (a.Guide)
            {
                var go = ResultKit.Group(g, "go", new Vector2(0f, -310f), new Vector2(314f, 80f));
                ModalKit.HexBtn(go, "wide_yellow", 0f, 0f, goText, () =>
                {
                    if (skin && a.Skin != null) a.Skin(skinId);
                    else if (a.Forge != null) a.Forge();
                    else Destroy(dim.gameObject);
                }, out _);
                _anim.Pop(go, 1.2f, 0.3f).At(1.55f, () => a.OnCard?.Invoke(go));
                return;
            }
            var goBtn = ResultKit.Group(g, "go", new Vector2(-98f, -310f), new Vector2(176f, 63f));
            ModalKit.HexBtn(goBtn, "md_gray", 0f, 0f, goText, () =>
            {
                if (skin && a.Skin != null) a.Skin(skinId);
                else if (a.Forge != null) a.Forge();
                else Destroy(dim.gameObject);
            }, out _);
            var ok = ResultKit.Group(g, "ok", new Vector2(98f, -310f), new Vector2(176f, 63f));
            ModalKit.HexBtn(ok, "md_yellow", 0f, 0f, last ? "知道了" : "下一个", () =>
            {
                Destroy(dim.gameObject);
                if (!last) Reveal(a, lines, k + 1);
            }, out _);
            _anim.Pop(goBtn, 1.2f, 0.3f).Pop(ok, 1.28f, 0.3f).Breathe(ok, 1.6f, 0.045f, 1.3f);
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
                Stamp(parent, "新纪录", new Vector2(200f, StarY + 58f), 16f, InkTheme.Rose, 0.8f + info.Stars * 0.3f);
            else if (info.FirstClear)
                Stamp(parent, "首通", new Vector2(200f, StarY + 58f), 16f, InkTheme.Rose, 0.8f + info.Stars * 0.3f);
        }

        // 星下面写清这一局为什么是这颗数，以及三星、两星的门槛。炮台宝箱在规则放完后当场开。
        void BuildStarRule(RectTransform parent, VictoryArgs a)
        {
            ResultInfo info = a.Info;
            var why = UiKit.Label(parent, "why", StarRules.Why(info), 18,
                new Vector2(0f, 104f), new Vector2(680f, 26f));
            why.color = InkTheme.Hex("FFE7B8");
            UiKit.Bold(why);
            var rule = UiKit.Label(parent, "rule", StarRules.Rule, 16,
                new Vector2(0f, 82f), new Vector2(680f, 24f));
            rule.color = InkTheme.Hex("CDBFA8");
            _anim.Fade(why, 1.65f, 0.25f, 0f, 1f).Fade(rule, 1.8f, 0.25f, 0f, 1f);
            if (a.OpenStarChest == null) return;
            _anim.At(2.05f, () =>
            {
                if (_starOpened) return;
                _starOpened = true;
                a.OpenStarChest();
            });
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
            var list = new List<(string icon, string name, int value, string tag)>();
            list.Add(("ink", "墨", info.Ink, info.DailyDouble ? "首胜×2" : null));
            if (info.Diamonds > 0) list.Add(("diamond", "钻石", info.Diamonds, "首通奖励"));
            if (info.Foil > 0) list.Add(("foil", "金箔", info.Foil, null));
            bool chest = info.Chest >= 0;

            SealDrop[] seals = info.Seals;
            bool seal = seals != null && seals.Length > 0;
            int totalSlots = list.Count + (seal ? 1 : 0) + (chest ? 1 : 0);

            int slotS = totalSlots >= 5 ? 86 : 96;
            float gap = totalSlots >= 5 ? 18f : 32f;
            float trayW = 624f;
            float trayH = chest ? 286f : 254f;

            var tray = ResultKit.Group(parent, "loot_tray", new Vector2(0f, RewardY), new Vector2(trayW, trayH));
            var trayBg = ModalKit.Sliced(tray, "bg", "Ui/victory_loot_tray", BorderTray, 0f, 0f, trayW, trayH, false);
            ModalKit.OutlinedText(trayBg, "rh", "本关收获", 22, 0f, 18f, trayW, 34f, ModalKit.CreamLight, TextAnchor.MiddleCenter, 1.5f);
            _anim.Pop(tray, at, 0.3f);

            float rowW = totalSlots * slotS + (totalSlots - 1) * gap;
            float x0 = Mathf.Round((trayW - rowW) * 0.5f);
            const float slotY = 82f;
            int col = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var (icon, label, value, tag) = list[i];
                float sx = x0 + col * (slotS + gap);
                col++;

                Sprite spr = icon == "foil" ? FoilArt.Icon : InkSprites.Ui(icon);
                var slot = ModalKit.ItemSlot(trayBg, ModalKit.CurrencyFrame(icon), slotS, sx, slotY, spr, "+0");
                slot.name = "rw" + i;
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(sx + slotS * 0.5f, -(slotY + slotS * 0.5f));

                var num = slot.Find("n")?.GetComponent<Text>();
                ModalKit.SolidText(trayBg, "nm" + i, label, 16, sx - 10f, slotY + slotS + 10f, slotS + 20f, 26f,
                    ModalKit.CreamLight, TextAnchor.MiddleCenter, true);

                float t0 = at + 0.08f + i * 0.12f;
                _anim.Pop(slot, t0, 0.32f);
                if (num != null)
                    _anim.CountUp(num, 0, value, t0 + 0.16f, 0.55f, "+{0}", AudioBus.CountTick);

                if (icon == "ink")
                {
                    _inkText = num;
                    _inkCard = slot;
                    _ink = info.Ink;
                }
                if (icon == "foil")
                {
                    _foilText = num;
                    _foil = info.Foil;
                }
                if (tag != null)
                {
                    var badge = ModalKit.Tag(trayBg, "Ui/modal_tag_coral", sx + slotS - 48f, slotY - 14f, 72f, 24f, tag, false, 12);
                    badge.pivot = new Vector2(0.5f, 0.5f);
                    badge.anchoredPosition = new Vector2(sx + slotS - 48f + 36f, -(slotY - 14f + 12f));
                    _anim.Pop(badge, t0 + 0.45f, 0.24f, 1.8f);
                }
            }

            if (seal)
            {
                float sx = x0 + col * (slotS + gap);
                col++;
                BuildSeals(trayBg, seals, sx, slotY, slotS, at + 0.08f + list.Count * 0.12f);
            }

            float end = at + 0.15f + totalSlots * 0.12f + 0.55f;
            if (chest)
            {
                float sx = x0 + col * (slotS + gap);
                end = Mathf.Max(end, BuildChest(trayBg, a, sx, slotY, slotS, trayW, at + 0.08f + col * 0.12f));
            }
            return end;
        }

        void BuildSeals(RectTransform trayBg, SealDrop[] seals, float sx, float sy, int slotS, float at)
        {
            SealQuality best = SealQuality.Green;
            for (int i = 0; i < seals.Length; i++)
                if (seals[i].Quality > best) best = seals[i].Quality;

            string tone = best == SealQuality.Gold ? "orange"
                : best == SealQuality.Purple ? "purple"
                : best == SealQuality.Blue ? "blue"
                : "green";
            var slot = ModalKit.ItemSlot(trayBg, tone, slotS, sx, sy, null, seals.Length > 1 ? "×" + seals.Length : "1");
            slot.name = "seal";
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = new Vector2(sx + slotS * 0.5f, -(sy + slotS * 0.5f));

            SealArt.Medal(slot, "m", seals[0].Type, seals[0].Quality, Vector2.zero, slotS * 0.68f);
            string text = seals.Length == 1 ? SealCatalog.DropName(seals[0]) : "印章";
            ModalKit.SolidText(trayBg, "seal_nm", text, 16, sx - 16f, sy + slotS + 10f, slotS + 32f, 26f,
                ModalKit.CreamLight, TextAnchor.MiddleCenter, true);

            _anim.Pop(slot, at, 0.32f).At(at + 0.25f, AudioBus.Stamp);
            if (best == SealQuality.Gold) _anim.At(at + 0.45f, AudioBus.StudyTier);
        }

        float BuildChest(RectTransform trayBg, VictoryArgs a, float sx, float sy, int slotS, float trayW, float at)
        {
            ResultInfo info = a.Info;
            ChestDef d = ChestCatalog.Get(info.Chest);
            var slot = ModalKit.ItemSlot(trayBg, ModalKit.ChestTone(d.Tier), slotS, sx, sy,
                InkSprites.Load("Ui/chest_" + d.Key), "1");
            slot.name = "chest";
            slot.pivot = new Vector2(0.5f, 0.5f);
            Vector2 home = new Vector2(sx + slotS * 0.5f, -(sy + slotS * 0.5f));
            slot.anchoredPosition = home;

            ModalKit.SolidText(trayBg, "chest_nm", d.Name, 16, sx - 12f, sy + slotS + 10f, slotS + 24f, 26f,
                ModalKit.CreamLight, TextAnchor.MiddleCenter, true);

            _anim.Move(slot, home + new Vector2(0f, 320f), home, at, 0.48f, Ease.OutBounce)
                 .At(at + 0.24f, AudioBus.ChestLand)
                 .Punch(slot, at + 0.5f, 0.16f, 0.28f);

            string note = d.Name + "：" + (info.ChestFull ? "位满了，离开后再选" : "不开就放进首页宝箱位");
            _chestNote = ModalKit.SolidText(trayBg, "note", note, 15, 0f, 234f, trayW, 28f,
                info.ChestFull ? InkTheme.Hex("FFE1A8") : InkTheme.Hex("CFC6B8"), TextAnchor.MiddleCenter, false);
            _anim.Fade(_chestNote, at + 0.55f, 0.25f, 0f, 1f);
            return at + 0.75f;
        }

        // 没有新字就不预告下一关。有新字才留一张字卡。
        // 重打已经通关的关时，下一关早就解锁过了，不再预告。
        bool BuildTeaser(RectTransform parent, VictoryArgs a, float at, out float end)
        {
            end = at;
            if (!a.Info.FirstClear || a.Info.Hard) return false;
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
            ModalKit.Tag(host, "Ui/modal_tag_coral", 195f, one ? 8f : 4f, 250f, 38f, "下一关解锁新字", false, 20);

            int n = fresh.Count;
            float icon = one ? 108f : Mathf.Min(96f, (520f - (n - 1) * 16f) / n);
            float step = icon + 16f;
            float x0 = -(n - 1) * step * 0.5f;
            float glyphY = one ? -16f : -18f;
            _anim.Pop(host, at, 0.34f).At(at + 0.3f, AudioBus.UnlockSting);
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
            float rowY = Mathf.Max(-496f, y - 40f);
            float homeY = Mathf.Max(-604f, rowY - 102f);
            _homeParkY = homeY;

            if (hasNext && ad)
            {
                // 并排：左青中号激励视频按钮 + 右黄宽主按钮「下一关 (-2⚡)」
                var adGroup = ResultKit.Group(parent, "double", new Vector2(-196f, rowY), new Vector2(176f, 63f));
                _double = ModalKit.HexBtn(adGroup, "md_cyan", 0f, 0f, "", a.DoubleInk, out _);
                string adTxt = a.DoubleText ?? "墨翻倍";
                ModalKit.SetAdBtnLabel(_double, "md_cyan", adTxt, true, adTxt.Length > 5 ? 16 : 20);
                _anim.Pop(adGroup, at, 0.32f);

                var nextGroup = ResultKit.Group(parent, "next", new Vector2(128f, rowY), new Vector2(314f, 80f));
                string nKind = a.NextAffordable ? "wide_yellow" : "wide_gray";
                var nextBtn = ModalKit.HexBtn(nextGroup, nKind, 0f, 0f, "", () =>
                {
                    if (a.NextAffordable) a.Next();
                    else AudioBus.Deny();
                }, out _);
                ModalKit.SetStaminaBtnLabel(nextBtn, nKind, "下一关", a.NextCost);
                _anim.Pop(nextGroup, at + 0.08f, 0.36f);
                if (a.NextAffordable) _anim.Breathe(nextGroup, at + 0.45f, 0.04f, 1.3f);
            }
            else if (hasNext)
            {
                var nextGroup = ResultKit.Group(parent, "next", new Vector2(0f, rowY), new Vector2(314f, 80f));
                string nKind = a.NextAffordable ? "wide_yellow" : "wide_gray";
                var nextBtn = ModalKit.HexBtn(nextGroup, nKind, 0f, 0f, "", () =>
                {
                    if (a.NextAffordable) a.Next();
                    else AudioBus.Deny();
                }, out _);
                ModalKit.SetStaminaBtnLabel(nextBtn, nKind, "下一关", a.NextCost);
                _anim.Pop(nextGroup, at, 0.36f);
                if (a.NextAffordable) _anim.Breathe(nextGroup, at + 0.45f, 0.04f, 1.3f);
            }
            else if (ad)
            {
                var adGroup = ResultKit.Group(parent, "double", new Vector2(0f, rowY), new Vector2(296f, 101f));
                _double = ModalKit.HexBtn(adGroup, "lg_yellow", 0f, 0f, "", a.DoubleInk, out _);
                ModalKit.SetAdBtnLabel(_double, "lg_yellow", a.DoubleText ?? "墨翻倍", true, 22);
                _anim.Pop(adGroup, at, 0.32f);
            }
            else
            {
                var homeOnly = ResultKit.Group(parent, "home", new Vector2(0f, rowY), new Vector2(314f, 80f));
                ModalKit.HexBtn(homeOnly, "wide_yellow", 0f, 0f, "返回首页", () => a.Home?.Invoke(), out _);
                _home = homeOnly;
                _anim.Pop(homeOnly, at, 0.32f);
                return;
            }

            // 下方轻文字入口「返回首页」
            _home = ResultKit.Group(parent, "home", new Vector2(0f, homeY), new Vector2(220f, 52f));
            var hit = _home.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.005f);
            hit.raycastTarget = true;
            var hBtn = _home.gameObject.AddComponent<Button>();
            hBtn.transition = Selectable.Transition.None;
            hBtn.targetGraphic = hit;
            hBtn.onClick.AddListener(() => a.Home?.Invoke());
            ModalKit.OutlinedText(_home, "t", "返回首页", 20, 0f, 8f, 220f, 36f,
                InkTheme.Hex("CFC6B8"), TextAnchor.MiddleCenter, 1.4f);
            _anim.Pop(_home, at + 0.18f, 0.28f);
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
            if (chestOpened && _chestNote != null)
            {
                _chestNote.text = "宝箱已当场打开";
                _chestNote.color = InkTheme.Hex("B8F0A8");
            }
            if (_foilText != null && _foil > 0)
            {
                _anim.CountUp(_foilText, _foil, _foil * 2, 0.05f, 0.7f, "+{0}", null);
                _foil *= 2;
            }
            if (_inkText == null || _ink <= 0) return;
            _anim.CountUp(_inkText, _ink, _ink * 2, 0.05f, 0.7f, "+{0}", AudioBus.CountTick);
            if (_inkCard != null)
            {
                _anim.Punch(_inkCard, 0.75f, 0.2f, 0.35f)
                     .At(0.75f, () => UiConfetti.Sparks(_root, Vector2.zero, InkTheme.GoldHi, 16, 600f));
            }
            _ink *= 2;
        }
    }
}
