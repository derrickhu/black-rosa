using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class FrenzyResultArgs
    {
        public FrenzyGrant Grant;
        public Action Double;
        public Action Again;
        public Action Home;
    }

    // 百鬼狂潮全屏欢庆结算：金芒旋转、彩带喷涌、击破数滚表、金箔+灵墨+随机印章三格齐亮。
    public sealed class FrenzyResultPanel : MonoBehaviour
    {
        const float BannerY = 330f;
        const float SubY = 200f;
        const float KillsY = 96f;
        const float BestY = 8f;
        const float LootY = -172f;
        const float DoubleY = -396f;
        const float AgainY = -506f;

        static readonly Color Gold = InkTheme.Hex("F6BE3C");
        static readonly Color Amber = InkTheme.Hex("FFD86B");

        MetaProgress _meta;
        FrenzyResultArgs _a;
        UiAnim _anim;
        RectTransform _root;
        Text _foil;
        Text _ink;
        Button _double;

        public RectTransform Root => _root;

        public static FrenzyResultPanel Show(RectTransform layer, MetaProgress meta, FrenzyResultArgs a)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "frenzy_result";
            var p = dim.gameObject.AddComponent<FrenzyResultPanel>();
            p._meta = meta;
            p._a = a;
            p._root = dim;
            p._anim = UiAnim.On(p);
            ResultKit.SkipCatcher(dim, () =>
            {
                if (p._anim.Playing) p._anim.Finish();
            });
            p.Build();
            AudioBus.Sweep(dim);
            return p;
        }

        void Build()
        {
            var dim = _root.GetComponent<Image>();
            var veil = new Color(0.11f, 0.07f, 0.04f, 0.85f);
            dim.color = new Color(veil.r, veil.g, veil.b, 0f);
            _anim.Fade(dim, 0f, 0.35f, 0f, veil.a);

            var stage = ResultKit.Group(_root, "stage", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));
            var rays = ResultKit.Rays(stage, new Vector2(0f, BannerY - 20f), 780f, new Color(1f, 0.84f, 0.42f, 0.88f));
            _anim.Fade(rays, 0f, 0.45f, 0f, 0.88f).Spin(rays.transform, 16f).Pop(rays.transform, 0f, 0.55f, 0.35f);

            var banner = ResultKit.Banner(stage, true, "满载而归", new Vector2(0f, BannerY), 620f);
            _anim.Move(banner, new Vector2(0f, 860f), new Vector2(0f, BannerY), 0.05f, 0.4f, Ease.OutBack)
                 .Punch(banner, 0.45f, 0.1f, 0.28f)
                 .At(0.42f, AudioBus.Stamp);

            FrenzyGrant g = _a.Grant;
            var sub = UiKit.Label(stage, "sub", "百鬼狂潮 · 爽刷收官", 28,
                new Vector2(0f, SubY), new Vector2(620f, 40f));
            sub.color = Amber;
            UiKit.Bold(sub);
            _anim.Fade(sub, 0.48f, 0.28f, 0f, 1f);

            var big = ResultKit.LightTitle(stage, "kills", "击破 0", 62, new Vector2(g.NewBest ? -36f : 0f, KillsY), Gold);
            _anim.Pop(big.transform, 0.48f, 0.28f, 0f)
                 .CountUp(big, 0, g.Kills, 0.55f, 0.7f, "击破 {0}", AudioBus.CountTick)
                 .Punch(big.transform, 1.25f, 0.14f, 0.28f);

            string bestLine = g.NewBest
                ? "刷新最高击破纪录！"
                : $"历史最高 击破 {Mathf.Max(_meta.FrenzyBestKills, g.Kills)}";
            var best = UiKit.Label(stage, "best", bestLine, 25, new Vector2(0f, BestY), new Vector2(640f, 36f));
            UiKit.Bold(best);
            best.color = g.NewBest ? InkTheme.Hex("FF9670") : InkTheme.Hex("DFCFA8");
            _anim.Fade(best, 1.05f, 0.25f, 0f, 1f);
            if (g.NewBest)
            {
                _anim.Shake(stage, 1.22f, 9f, 0.24f);
                var st = ResultKit.Group(stage, "stamp", new Vector2(228f, KillsY), new Vector2(128f, 44f));
                st.localRotation = Quaternion.Euler(0f, 0f, -12f);
                ModalKit.Tag(st, "Ui/modal_tag_coral", 0f, 0f, 128f, 44f, "新纪录", false, 22);
                _anim.Pop(st, 1.18f, 0.24f, 2.2f).At(1.36f, AudioBus.Stamp);
            }

            BuildLoot(stage, 1.32f);
            BuildButtons(stage, 1.58f);
            _anim.At(1.45f, () =>
            {
                UiConfetti.Burst(_root, 110);
                AudioBus.Confetti();
            });
        }

        void BuildLoot(RectTransform stage, float at)
        {
            FrenzyGrant g = _a.Grant;
            bool showFoil = _meta.StudyOpen && g.Foil > 0;
            bool showSeal = g.HasSeal;
            int count = 1 + (showFoil ? 1 : 0) + (showSeal ? 1 : 0);

            const float trayW = 576f, trayH = 224f;
            var host = ResultKit.Group(stage, "loot", new Vector2(0f, LootY), new Vector2(trayW, trayH));
            var tray = ModalKit.Sliced(host, "card", "Ui/victory_loot_tray", new Vector4(52f, 52f, 52f, 52f),
                0f, 0f, trayW, trayH, false);
            ModalKit.OutlinedText(tray, "h", "本局收获", 22, 0f, 14f, trayW, 32f,
                ModalKit.CreamLight, TextAnchor.MiddleCenter, 1.4f);

            const int slotS = 86;
            const float gap = 54f;
            const float slotY = 58f;
            float totalW = count * slotS + (count - 1) * gap;
            float x0 = Mathf.Round((trayW - totalW) * 0.5f);
            int idx = 0;

            if (showFoil)
            {
                float fx = x0 + idx * (slotS + gap);
                var fSlot = ModalKit.ItemSlot(tray, ModalKit.FoilTone, slotS, fx, slotY, FoilArt.Icon, "+0");
                fSlot.pivot = new Vector2(0.5f, 0.5f);
                fSlot.anchoredPosition = new Vector2(fx + slotS * 0.5f, -(slotY + slotS * 0.5f));
                _foil = fSlot.Find("n")?.GetComponent<Text>();
                ModalKit.SolidText(tray, "foil_nm", "金箔", 16, fx - 12f, slotY + slotS + 6f, slotS + 24f, 24f,
                    ModalKit.CreamLight, TextAnchor.MiddleCenter, true);
                if (_foil != null)
                    _anim.CountUp(_foil, 0, g.Foil, at + 0.12f, 0.45f, "+{0}", AudioBus.CountTick);
                _anim.Pop(fSlot, at + 0.08f, 0.28f, 0.4f);
                idx++;
            }

            {
                float ix = x0 + idx * (slotS + gap);
                var iSlot = ModalKit.ItemSlot(tray, ModalKit.InkTone, slotS, ix, slotY, ModalKit.Load("Ui/ico_ink"), "+0");
                iSlot.pivot = new Vector2(0.5f, 0.5f);
                iSlot.anchoredPosition = new Vector2(ix + slotS * 0.5f, -(slotY + slotS * 0.5f));
                _ink = iSlot.Find("n")?.GetComponent<Text>();
                ModalKit.SolidText(tray, "ink_nm", "灵墨", 16, ix - 12f, slotY + slotY * 0f + slotS + 6f, slotS + 24f, 24f,
                    ModalKit.CreamLight, TextAnchor.MiddleCenter, true);
                if (_ink != null)
                    _anim.CountUp(_ink, 0, g.Ink, at + 0.18f, 0.45f, "+{0}", AudioBus.CountTick);
                _anim.Pop(iSlot, at + 0.14f, 0.28f, 0.4f);
                idx++;
            }

            if (showSeal)
            {
                float sx = x0 + idx * (slotS + gap);
                string sTone = g.Seal.Quality == SealQuality.Gold ? "orange"
                    : g.Seal.Quality == SealQuality.Purple ? "purple"
                    : g.Seal.Quality == SealQuality.Blue ? "blue" : "green";
                var sSlot = ModalKit.ItemSlot(tray, sTone, slotS, sx, slotY, ModalKit.Load(SealCatalog.IconKey(g.Seal.Type)), "1");
                sSlot.pivot = new Vector2(0.5f, 0.5f);
                sSlot.anchoredPosition = new Vector2(sx + slotS * 0.5f, -(slotY + slotS * 0.5f));
                ModalKit.SolidText(tray, "seal_nm", SealCatalog.DropName(g.Seal), 15, sx - 20f, slotY + slotS + 6f, slotS + 40f, 24f,
                    ModalKit.CreamLight, TextAnchor.MiddleCenter, true);
                _anim.Pop(sSlot, at + 0.24f, 0.32f, 0.3f).At(at + 0.38f, AudioBus.ChestLand);
            }
            else if (_meta.SealDropOpen)
            {
                int remain = Mathf.Max(1, StageCatalog.FrenzySealKillNeed - g.Kills);
                ModalKit.SolidText(tray, "seal_note", $"再多击破 {remain} 只即可额外获赠随机印章", 15, 0f, 184f, trayW, 24f,
                    InkTheme.Hex("D9CCB6"), TextAnchor.MiddleCenter, false);
            }

            _anim.Pop(host, at, 0.32f).At(at + 0.12f, AudioBus.Stamp);
        }

        void BuildButtons(RectTransform stage, float at)
        {
            if (_a.Double != null)
            {
                var dbl = ResultKit.Group(stage, "double", new Vector2(0f, DoubleY), new Vector2(296f, 101f));
                _double = ModalKit.HexBtn(dbl, "lg_yellow", 0f, 0f, "", () => _a.Double(), out _);
                string label = _meta.StudyOpen && _a.Grant.Foil > 0 ? "金箔与墨翻倍" : "灵墨翻倍";
                ModalKit.SetAdBtnLabel(_double, "lg_yellow", label, true, 22);
                _anim.Pop(dbl, at + 0.06f, 0.3f);
            }

            int left = _meta.FrenzyPlaysLeft;
            var row = ResultKit.Group(stage, "row", new Vector2(0f, AgainY), new Vector2(514f, 80f));
            string againKind = left > 0 ? "wide_yellow" : "wide_gray";
            ModalKit.HexBtn(row, againKind, 200f, 0f, $"再来一局（剩 {left}）",
                () => _a.Again?.Invoke(), out _);
            ModalKit.HexBtn(row, "md_gray", 0f, 8f, "返回副本", () => _a.Home?.Invoke(), out _);
            _anim.Pop(row, at + 0.14f, 0.3f);
        }

        public void Doubled()
        {
            if (_double != null) _double.gameObject.SetActive(false);
            FrenzyGrant g = _a.Grant;
            var anim = UiAnim.On(this);
            if (_foil != null && g.Foil > 0)
                anim.CountUp(_foil, g.Foil, g.Foil * 2, 0f, 0.45f, "+{0}", AudioBus.CountTick)
                    .Punch(_foil.transform.parent, 0.45f, 0.14f, 0.25f);
            if (_ink != null && g.Ink > 0)
                anim.CountUp(_ink, g.Ink, g.Ink * 2, 0f, 0.45f, "+{0}", AudioBus.CountTick)
                    .Punch(_ink.transform.parent, 0.45f, 0.14f, 0.25f);
            UiConfetti.Burst(_root, 70);
            AudioBus.Confetti();
        }
    }
}
