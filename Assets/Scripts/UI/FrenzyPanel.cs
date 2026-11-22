using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 百鬼狂潮开局弹窗：暖琥珀鎏金立体外壳，顶部圆弧狂潮横幅聚焦最高击破与单局金箔纪录；
    // 中段单行爽刷法则暗条；下段展示三大掉落（金箔雨 · 随机印章 · 灵墨）与击破里程碑轴。
    public sealed class FrenzyPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/Frenzy";
        public const string Mark = "frenzy_v2";

        const float BoardW = 648f;
        const float BoardH = 764f;
        const float WellX = 12f;
        const float WellY = 12f;
        const float WellW = 624f;
        const float WellH = 728f;

        static readonly Vector4 BorderShell = new Vector4(84f, 108f, 84f, 84f);
        static readonly Vector4 BorderIntel = new Vector4(40f, 40f, 40f, 40f);
        static readonly Vector4 BorderPill = new Vector4(24f, 0f, 24f, 0f);
        static readonly Vector4 BorderTicket = new Vector4(32f, 0f, 32f, 0f);
        static readonly Vector4 BorderAxis = new Vector4(22f, 0f, 22f, 0f);
        static readonly Vector4 BorderAxisFill = new Vector4(14f, 0f, 14f, 0f);

        MetaProgress _meta;
        Action _start;
        Action _changed;
        RectTransform _layer;
        RectTransform _stage;
        RectTransform _board;
        RectTransform _well;

        public static void Show(RectTransform layer, MetaProgress meta, Action start, Action changed)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "frenzy_panel", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<FrenzyPanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            panel._layer = layer;
            panel._stage = stage;
            panel.Bind(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "frenzy_panel", 0.80f, null, out RectTransform stage);
            float mx = Mathf.Round((ModalKit.DesignW - BoardW) * 0.5f);
            float my = Mathf.Round((ModalKit.DesignH - BoardH) * 0.5f);

            var board = ModalKit.Sliced(stage, "board", "Ui/dungeon_shell_gold", BorderShell, mx, my, BoardW, BoardH, true);
            board.pivot = new Vector2(0.5f, 0.5f);
            board.anchoredPosition = new Vector2(mx + BoardW * 0.5f, -(my + BoardH * 0.5f));
            ModalKit.AddMark(board, Mark);

            var wellGo = new GameObject("well", typeof(RectTransform));
            wellGo.transform.SetParent(board, false);
            var well = wellGo.GetComponent<RectTransform>();
            ModalKit.TopLeft(well, WellX, WellY, WellW, WellH);

            // 1. 顶部圆弧横幅
            var banner = ModalKit.Pic(well, "banner", "Ui/dungeon_banner_gold", 0f, 0f, WellW, 196f, false).rectTransform;
            ModalKit.SolidText(banner, "sub", "最高击破", 15, 282f, 32f, 320f, 28f,
                InkTheme.Hex("FFF0CC"), TextAnchor.MiddleLeft, false);
            ModalKit.OutlinedText(banner, "best", "今日待战", 36, 280f, 62f, 320f, 64f,
                InkTheme.Hex("FFE866"), TextAnchor.MiddleLeft, 1.8f);
            var pill = ModalKit.Sliced(banner, "pill", "Ui/dungeon_pill_gold", BorderPill, 294f, 136f, 256f, 30f, false);
            ModalKit.SolidText(pill, "t", "96秒爽刷 · 零耗体 · 漏怪不败", 13, 0f, 0f, 256f, 30f,
                InkTheme.Hex("FFF3D6"), TextAnchor.MiddleCenter, true);

            // 2. 中段爽刷法则暗条
            var bar = ModalKit.Sliced(well, "rule", "Ui/dungeon_intel_gold", BorderIntel, 20f, 216f, 584f, 74f, false);
            var tag = ModalKit.Sliced(bar, "tag", "Ui/modal_tag_coral", BorderTicket, 18f, 19f, 112f, 36f, false);
            ModalKit.OutlinedText(tag, "t", "狂潮特权", 18, 0f, 0f, 112f, 34f, Color.white, TextAnchor.MiddleCenter, 1.35f);
            ModalKit.SolidText(bar, "desc", "中宫四格升 ★4 极意 · 终幕扁金翁爆金箔", 15,
                142f, 0f, 426f, 74f, ModalKit.CreamLight, TextAnchor.MiddleLeft, true);

            // 3. 狂潮掉落轴
            ModalKit.SolidText(well, "rw_h", "狂潮战利品", 20, 26f, 316f, 160f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.SolidText(well, "rw_sub", $"击破满 {StageCatalog.FrenzySealKillNeed} 只加送随机印章", 15,
                300f, 316f, 298f, 28f, ModalKit.TextMuted, TextAnchor.MiddleRight, false);

            const float trackW = 420f;
            var track = ModalKit.Sliced(well, "axis", "Ui/dungeon_axis_track", BorderAxis, 102f, 414f, trackW, 18f, false);
            var fill = ModalKit.Sliced(track, "fill", "Ui/dungeon_axis_fill_gold", BorderAxisFill, 3f, 3f, 14f, 12f, false);
            fill.gameObject.SetActive(false);

            const int n = 3;
            const float s = 96f, gap = 68f;
            float left = Mathf.Round((WellW - (s * n + gap * (n - 1))) * 0.5f);
            string[] tones = { ModalKit.InkTone, ModalKit.FoilTone, "green" };
            string[] icons = { "Ui/ico_ink", "Ui/ico_foil", "Ui/seal_ico_fierce" };
            string[] counts = { "海量", "狂掉", "1" };
            string[] gates = { "保底必得", "痛扁金翁", $"击破 {StageCatalog.FrenzySealKillNeed}" };
            string[] names = { "灵墨", "金箔雨", "随机印章" };
            for (int i = 0; i < n; i++)
            {
                float sx = left + i * (s + gap);
                var slot = ModalKit.ItemSlot(well, tones[i], 96, sx, 375f, ModalKit.Load(icons[i]), counts[i]);
                slot.name = "slot" + i;
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(sx + 48f, -(375f + 48f));

                ModalKit.SolidText(well, "w" + i, gates[i], 16, sx - 16f, 476f, 128f, 28f,
                    ModalKit.TextBody, TextAnchor.MiddleCenter, true);
                ModalKit.SolidText(well, "nm" + i, names[i], 14, sx - 16f, 506f, 128f, 26f,
                    ModalKit.TextMuted, TextAnchor.MiddleCenter, false);

                var chk = ModalKit.Pic(well, "chk" + i, "Ui/checkin_check", sx + 68f, 367f, 30f, 30f, true);
                chk.gameObject.SetActive(false);
                var aimTag = ModalKit.Tag(well, "Ui/modal_tag_coral", sx + 10f, 358f, 76f, 24f, "大奖", false, 12);
                aimTag.name = "aim" + i;
                aimTag.gameObject.SetActive(i == 2);
            }

            // 4. 底部次数与主按钮
            ModalKit.SolidText(well, "plays", "今日剩余 1 / 1 次", 16, 26f, 586f, 184f, 26f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, true);
            var adBtn = ModalKit.HexBtn(well, "md_cyan", 26f, 626f, "", null, out _);
            adBtn.gameObject.name = "ad_btn";
            ModalKit.SetAdBtnLabel(adBtn, "md_cyan", $"+{GameConstants.FrenzyAdPlays} 次", true, 20);

            var go = ModalKit.HexBtn(well, "wide_yellow", 286f, 618f, "爽刷开战", null, out _);
            go.gameObject.name = "go_btn";

            var crest = ModalKit.Pic(board, "crest", "Ui/dungeon_crest_gold", (BoardW - 248f) * 0.5f, -18f, 248f, 54f, false);
            ModalKit.OutlinedText(crest.rectTransform, "title", "百鬼狂潮", 28, 0f, 0f, 248f, 51f,
                Color.white, TextAnchor.MiddleCenter);
            ModalKit.CloseBtn(board, 0f, 0f, () => { });

            return dim;
        }

        void OnRectTransformDimensionsChange()
        {
            if (_stage != null) ModalKit.FitStage(_stage, _layer);
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Bind(RectTransform stage)
        {
            _board = stage.Find("board") as RectTransform;
            _well = _board.Find("well") as RectTransform;
            ModalKit.BindClose(_board, Close);
            ModalKit.BindBtn(_well, "ad_btn", OnAd);
            ModalKit.BindBtn(_well, "go_btn", OnStart);
            RepairArt();
            Refresh();
            PanelKit.Enlarge(_board);
            PanelKit.Open(_board);
        }

        void RepairArt()
        {
            ModalKit.FillSprite(_board, "", "Ui/dungeon_shell_gold", BorderShell);
            ModalKit.FillSprite(_board, "crest", "Ui/dungeon_crest_gold");
            ModalKit.FillSprite(_board, "well/banner", "Ui/dungeon_banner_gold");
            ModalKit.FillSprite(_board, "well/banner/pill", "Ui/dungeon_pill_gold", BorderPill);
            ModalKit.FillSprite(_board, "well/rule", "Ui/dungeon_intel_gold", BorderIntel);
            ModalKit.FillSprite(_board, "well/rule/tag", "Ui/modal_tag_coral", BorderTicket);
            ModalKit.FillSprite(_board, "well/axis", "Ui/dungeon_axis_track", BorderAxis);
            ModalKit.FillSprite(_board, "well/axis/fill", "Ui/dungeon_axis_fill_gold", BorderAxisFill);
        }

        void Refresh()
        {
            var banner = _well.Find("banner");
            var subTxt = banner.Find("sub")?.GetComponent<Text>();
            if (subTxt != null)
                subTxt.text = _meta.FrenzyBestFoil > 0
                    ? $"最高击破  ·  单局金箔 {_meta.FrenzyBestFoil}"
                    : "最高击破";

            var bestTxt = banner.Find("best")?.GetComponent<Text>();
            if (bestTxt != null)
            {
                bestTxt.text = _meta.FrenzyBestKills > 0 ? $"击破 {_meta.FrenzyBestKills}" : "今日待战";
                bestTxt.fontSize = _meta.FrenzyBestKills > 0 ? 44 : 36;
            }

            const float trackW = 420f;
            float ratio = Mathf.Clamp01(_meta.FrenzyBestKills / (float)StageCatalog.FrenzySealKillNeed);
            var fill = _well.Find("axis/fill") as RectTransform;
            if (fill != null)
            {
                bool showFill = _meta.FrenzyBestKills > 0 && ratio > 0.02f;
                fill.gameObject.SetActive(showFill);
                if (showFill)
                    ModalKit.TopLeft(fill, 3f, 3f, Mathf.Clamp((trackW - 6f) * ratio, 14f, trackW - 6f), 12f);
            }

            bool sealReached = _meta.FrenzyBestKills >= StageCatalog.FrenzySealKillNeed;
            var chk2 = _well.Find("chk2");
            if (chk2 != null) chk2.gameObject.SetActive(sealReached);
            var aim2 = _well.Find("aim2");
            if (aim2 != null) aim2.gameObject.SetActive(!sealReached);
            var slot2 = _well.Find("slot2") as RectTransform;
            if (slot2 != null)
            {
                UiAnim.On(slot2).Clear();
                slot2.localScale = Vector3.one;
                if (!sealReached) UiAnim.On(slot2).Breathe(slot2, 0f, 0.045f, 1.2f);
            }

            int cap = _meta.FrenzyPlaysCap;
            int left = _meta.FrenzyPlaysLeft;
            bool hasAd = _meta.CanFrenzyAd;

            var playsTxt = _well.Find("plays")?.GetComponent<Text>();
            if (playsTxt != null)
            {
                playsTxt.text = $"今日剩余 {left} / {cap} 次";
                if (hasAd) ModalKit.TopLeft(playsTxt.rectTransform, 26f, 586f, 184f, 26f);
                else ModalKit.TopLeft(playsTxt.rectTransform, 0f, 584f, WellW, 26f);
            }

            var adTr = _well.Find("ad_btn");
            if (adTr != null) adTr.gameObject.SetActive(hasAd);

            var goBtn = _well.Find("go_btn")?.GetComponent<Button>();
            if (goBtn != null)
            {
                float goX = hasAd ? 286f : (WellW - 314f) * 0.5f;
                ModalKit.TopLeft(goBtn.GetComponent<RectTransform>(), goX, 618f, 314f, 80f);
                ModalKit.SetHexBtnGray(goBtn, "wide_yellow", "wide_gray", left <= 0);
            }
        }

        void OnAd()
        {
            AudioBus.Tap();
            AdStub.Reward("frenzy_plays", () =>
            {
                if (this == null || !_meta.GrantFrenzyAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }

        void OnStart()
        {
            if (_meta.FrenzyPlaysLeft <= 0)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.FrenzyAdDone
                    ? "今日狂潮次数已用完，明天再来爽刷"
                    : "次数用完了，点左边看广告加一次");
                return;
            }
            AudioBus.Tap();
            Action start = _start;
            Destroy(gameObject);
            start?.Invoke();
        }
    }
}
