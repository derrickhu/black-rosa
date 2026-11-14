using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 每日副本开局弹窗（Figma Panel/其他弹窗/02_每日副本，预制体 Prefabs/DailyPuzzle）：
    // 翠竹绿立体厚底专属外壳，顶部圆弧插画舞台突出今日最好波数；
    // 中段深色情报暗牌展示词缀、铲子机制与禁用字；下段守波宝箱（或第五章后随机印章）与统一主按钮。
    public sealed class DailyPuzzlePanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/DailyPuzzle";
        public const string Mark = "daily_puzzle_v1";

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
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "daily_puzzle_panel", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<DailyPuzzlePanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            panel._layer = layer;
            panel._stage = stage;
            panel.Bind(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "daily_puzzle_panel", 0.80f, null, out RectTransform stage);
            float mx = Mathf.Round((ModalKit.DesignW - BoardW) * 0.5f);
            float my = Mathf.Round((ModalKit.DesignH - BoardH) * 0.5f);

            var board = ModalKit.Sliced(stage, "board", "Ui/dungeon_shell_green", BorderShell, mx, my, BoardW, BoardH, true);
            board.pivot = new Vector2(0.5f, 0.5f);
            board.anchoredPosition = new Vector2(mx + BoardW * 0.5f, -(my + BoardH * 0.5f));
            ModalKit.AddMark(board, Mark);

            var wellGo = new GameObject("well", typeof(RectTransform));
            wellGo.transform.SetParent(board, false);
            var well = wellGo.GetComponent<RectTransform>();
            ModalKit.TopLeft(well, WellX, WellY, WellW, WellH);

            // 1. 顶部圆弧横幅
            var banner = ModalKit.Pic(well, "banner", "Ui/dungeon_banner_green", 0f, 0f, WellW, 196f, false).rectTransform;
            ModalKit.SolidText(banner, "sub", "今日最好", 16, 282f, 38f, 240f, 24f,
                InkTheme.Hex("C9EEAC"), TextAnchor.MiddleLeft, false);
            ModalKit.OutlinedText(banner, "best", "今天还没打", 36, 280f, 64f, 320f, 62f,
                InkTheme.Hex("FFE455"), TextAnchor.MiddleLeft, 1.8f);
            var pill = ModalKit.Sliced(banner, "pill", "Ui/dungeon_pill_green", BorderPill, 282f, 136f, 244f, 30f, false);
            ModalKit.SolidText(pill, "t", "全服同局 · 24小时后换新", 14, 0f, 0f, 244f, 30f,
                InkTheme.Hex("DDF7C6"), TextAnchor.MiddleCenter, true);

            // 2. 中段深色情报暗牌
            var intel = ModalKit.Sliced(well, "intel", "Ui/dungeon_intel_green", BorderIntel, 20f, 216f, 584f, 156f, false);
            var tag = ModalKit.Sliced(intel, "rule_tag", "Ui/glyph_tag_teal", BorderTicket, 18f, 16f, 104f, 36f, false);
            ModalKit.OutlinedText(tag, "t", "词缀", 18, 0f, 0f, 104f, 34f, Color.white, TextAnchor.MiddleCenter, 1.35f);
            ModalKit.SolidText(intel, "desc", "", 17, 134f, 16f, 432f, 36f,
                ModalKit.CreamLight, TextAnchor.MiddleLeft, true);

            ModalKit.Divider(intel, "div_h", 18f, 66f, 548f, 2f, true);
            ModalKit.Pic(intel, "shovel", null, 22f, 78f, 64f, 64f, true);
            ModalKit.SolidText(intel, "mech", "铲子可挖开棋盘 1 格", 17, 96f, 78f, 214f, 64f,
                InkTheme.Hex("DDF7C6"), TextAnchor.MiddleLeft, true);

            ModalKit.Divider(intel, "div_v", 322f, 80f, 2f, 60f, true);
            ModalKit.SolidText(intel, "ban_lbl", "禁用", 16, 338f, 78f, 48f, 64f,
                InkTheme.Hex("B7C6B5"), TextAnchor.MiddleLeft, false);

            for (int i = 0; i < 2; i++)
            {
                float bx = 396f + i * 74f;
                var tile = ModalKit.Pic(intel, "ban" + i, "Ui/dungeon_ban_tile", bx, 82f, 56f, 56f, false).rectTransform;
                ModalKit.SolidText(tile, "z", "", 26, 0f, 0f, 56f, 56f,
                    InkTheme.Hex("6E665E"), TextAnchor.MiddleCenter, true);
                ModalKit.Pic(tile, "sl", "Ui/dungeon_ban_slash", 0f, 0f, 56f, 56f, false);
            }

            // 3A. 守波宝箱组
            var chestsGrp = new GameObject("chests_grp", typeof(RectTransform)).GetComponent<RectTransform>();
            chestsGrp.SetParent(well, false);
            ModalKit.TopLeft(chestsGrp, 0f, 0f, WellW, WellH);
            ModalKit.SolidText(chestsGrp, "rw_h", "守波宝箱", 20, 26f, 392f, 160f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.SolidText(chestsGrp, "rw_tot", $"共 {StageCatalog.DailyWaves} 波", 15, 480f, 392f, 118f, 28f,
                ModalKit.TextMuted, TextAnchor.MiddleRight, false);

            var track = ModalKit.Sliced(chestsGrp, "axis", "Ui/dungeon_axis_track", BorderAxis, 186f, 474f, 252f, 18f, false);
            var fill = ModalKit.Sliced(track, "fill", "Ui/dungeon_axis_fill_green", BorderAxisFill, 3f, 3f, 14f, 12f, false);
            fill.gameObject.SetActive(false);

            ChestTier[] tiers = { ChestTier.Wood, ChestTier.Silver };
            int[] need = { StageCatalog.DailyBossEvery, StageCatalog.DailyWaves };
            float[] xs = { 118f, 410f };
            for (int i = 0; i < 2; i++)
            {
                ChestDef def = ChestCatalog.Get(tiers[i]);
                var slot = ModalKit.ItemSlot(chestsGrp, ModalKit.ChestTone(tiers[i]), 96, xs[i], 435f,
                    ModalKit.Load("Ui/chest_" + def.Key), "1");
                slot.name = "slot" + i;
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(xs[i] + 48f, -(435f + 48f));

                ModalKit.SolidText(chestsGrp, "w" + i, $"{need[i]} 波", 16, xs[i] - 12f, 538f, 120f, 24f,
                    ModalKit.TextBody, TextAnchor.MiddleCenter, true);

                var chk = ModalKit.Pic(chestsGrp, "chk" + i, "Ui/checkin_check", xs[i] + 68f, 427f, 32f, 32f, true);
                chk.gameObject.SetActive(false);
                var aimTag = ModalKit.Tag(chestsGrp, "Ui/modal_tag_coral", xs[i] + 10f, 419f, 76f, 24f, "下一目标", false, 12);
                aimTag.name = "aim" + i;
                aimTag.gameObject.SetActive(false);
            }

            // 3B. 第五章后随机印章奖励组
            var sealGrp = new GameObject("seal_grp", typeof(RectTransform)).GetComponent<RectTransform>();
            sealGrp.SetParent(well, false);
            ModalKit.TopLeft(sealGrp, 0f, 0f, WellW, WellH);
            ModalKit.SolidText(sealGrp, "rw_h", "结算奖励", 20, 26f, 392f, 160f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.SolidText(sealGrp, "rw_tot", $"守满 {StageCatalog.DailyWaves} 波发放", 15, 420f, 392f, 178f, 28f,
                ModalKit.TextMuted, TextAnchor.MiddleRight, false);

            var box = ModalKit.Sliced(sealGrp, "seal_box", "Ui/modal_sub_bright", ModalKit.BorderSub18,
                62f, 430f, 500f, 134f, false);
            ModalKit.Pic(box, "q_slot", "Ui/glyph_slot_empty_green", 24f, 21f, 88f, 92f, true);
            ModalKit.OutlinedText(box, "q_mark", "？", 36, 24f, 21f, 88f, 88f, ModalKit.TextMuted, TextAnchor.MiddleCenter);
            ModalKit.SolidText(box, "t", "全部守住 · 随机得一枚印章", 20, 128f, 34f, 344f, 32f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.SolidText(box, "s", "绿 / 蓝 / 紫 / 金四档印章，可镶嵌在字卡插槽上", 15, 128f, 72f, 352f, 26f,
                ModalKit.TextMuted, TextAnchor.MiddleLeft, false);
            sealGrp.gameObject.SetActive(false);

            // 4. 底部次数与主按钮
            ModalKit.SolidText(well, "plays", "今日剩余 1 / 1 次", 16, 26f, 586f, 176f, 26f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, true);
            var adBtn = ModalKit.HexBtn(well, "md_cyan", 26f, 626f, "", null, out _);
            adBtn.gameObject.name = "ad_btn";
            ModalKit.SetAdBtnLabel(adBtn, "md_cyan", $"+{GameConstants.DailyAdPlays} 次", true, 20);

            var go = ModalKit.HexBtn(well, "wide_yellow", 286f, 618f, "开始", null, out _);
            go.gameObject.name = "go_btn";

            var crest = ModalKit.Pic(board, "crest", "Ui/dungeon_crest_green", (BoardW - 248f) * 0.5f, -18f, 248f, 54f, false);
            ModalKit.OutlinedText(crest.rectTransform, "title", "每日副本", 28, 0f, 0f, 248f, 51f,
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

            var shovel = _well.Find("intel/shovel")?.GetComponent<Image>();
            if (shovel != null) shovel.sprite = InkArt.Shovel(160);

            Refresh();
            PanelKit.Open(_board);
        }

        void Refresh()
        {
            int best = _meta.DailyBestToday;
            var banner = _well.Find("banner");
            var bestTxt = banner.Find("best")?.GetComponent<Text>();
            if (bestTxt != null)
            {
                bestTxt.text = best > 0 ? $"第 {best} 波" : "今天还没打";
                bestTxt.fontSize = best > 0 ? 46 : 36;
            }
            int hours = Mathf.Max(1, Mathf.CeilToInt(DayClock.SecondsToTomorrow / 3600f));
            var pillTxt = banner.Find("pill/t")?.GetComponent<Text>();
            if (pillTxt != null) pillTxt.text = $"全服同局 · {hours}小时后换新";

            StageDef s = StageCatalog.DailyStage(DayClock.Today, DayClock.Week);
            var intel = _well.Find("intel");
            float tw = Mathf.Max(68f, ModalKit.MeasureText(s.Rule.Name, 18) + 34f);
            var ruleTag = intel.Find("rule_tag") as RectTransform;
            if (ruleTag != null)
            {
                ModalKit.TopLeft(ruleTag, 18f, 16f, tw, 36f);
                var rtTxt = ruleTag.Find("t")?.GetComponent<Text>();
                if (rtTxt != null)
                {
                    ModalKit.TopLeft(rtTxt.rectTransform, 0f, 0f, tw, 34f);
                    rtTxt.text = s.Rule.Name;
                }
            }
            var descTxt = intel.Find("desc")?.GetComponent<Text>();
            if (descTxt != null)
            {
                ModalKit.TopLeft(descTxt.rectTransform, 18f + tw + 12f, 16f, 548f - tw, 36f);
                descTxt.text = s.Rule.Desc;
            }

            int bCount = s.Banned != null ? s.Banned.Length : 0;
            for (int i = 0; i < 2; i++)
            {
                var tile = intel.Find("ban" + i);
                if (tile == null) continue;
                bool showBan = i < bCount;
                tile.gameObject.SetActive(showBan);
                if (showBan)
                {
                    var z = tile.Find("z")?.GetComponent<Text>();
                    if (z != null) z.text = CardCatalog.Get(s.Banned[i]).Name;
                }
            }

            bool sealMode = _meta.SealOpen;
            var chestsGrp = _well.Find("chests_grp");
            var sealGrp = _well.Find("seal_grp");
            if (chestsGrp != null) chestsGrp.gameObject.SetActive(!sealMode);
            if (sealGrp != null) sealGrp.gameObject.SetActive(sealMode);

            if (!sealMode && chestsGrp != null)
            {
                int[] need = { StageCatalog.DailyBossEvery, StageCatalog.DailyWaves };
                const float trackW = 252f;
                float ratio = Mathf.Clamp01((best - need[0] * 0.2f) / Mathf.Max(1f, need[1] - need[0] * 0.2f));
                var fill = chestsGrp.Find("axis/fill") as RectTransform;
                if (fill != null)
                {
                    bool showFill = best > 0 && ratio > 0.02f;
                    fill.gameObject.SetActive(showFill);
                    if (showFill)
                        ModalKit.TopLeft(fill, 3f, 3f, Mathf.Clamp((trackW - 6f) * ratio, 14f, trackW - 6f), 12f);
                }

                int next = best >= need[1] ? -1 : best >= need[0] ? 1 : 0;
                for (int i = 0; i < 2; i++)
                {
                    bool got = best >= need[i];
                    bool aim = i == next;
                    var slot = chestsGrp.Find("slot" + i) as RectTransform;
                    if (slot != null)
                    {
                        UiAnim.On(slot).Clear();
                        slot.localScale = Vector3.one;
                        if (aim) UiAnim.On(slot).Breathe(slot, 0f, 0.045f, 1.2f);
                    }
                    var wTxt = chestsGrp.Find("w" + i)?.GetComponent<Text>();
                    if (wTxt != null) wTxt.color = aim ? InkTheme.Hex("C84828") : ModalKit.TextBody;
                    var chk = chestsGrp.Find("chk" + i);
                    if (chk != null) chk.gameObject.SetActive(got);
                    var aimTag = chestsGrp.Find("aim" + i);
                    if (aimTag != null) aimTag.gameObject.SetActive(!got && aim);
                }
            }

            int cap = _meta.DailyPlaysCap;
            int left = _meta.DailyPlaysLeft;
            bool hasAd = !_meta.DailyAdDone;

            var playsTxt = _well.Find("plays")?.GetComponent<Text>();
            if (playsTxt != null)
            {
                playsTxt.text = $"今日剩余 {left} / {cap} 次";
                if (hasAd) ModalKit.TopLeft(playsTxt.rectTransform, 26f, 586f, 176f, 26f);
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
            AdStub.Reward("daily_plays", () =>
            {
                if (this == null || !_meta.GrantDailyAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }

        void OnStart()
        {
            if (_meta.DailyPlaysLeft <= 0)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.DailyAdDone
                    ? "今天的残局次数用完了，明天换一局"
                    : $"今天的次数用完了，看广告再开 {GameConstants.DailyAdPlays} 局");
                return;
            }
            AudioBus.Tap();
            Action start = _start;
            Destroy(gameObject);
            start?.Invoke();
        }
    }
}
