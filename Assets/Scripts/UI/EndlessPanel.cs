using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 无尽副本开局弹窗（Figma Panel/其他弹窗/03_无尽模式，预制体 Prefabs/Endless）：
    // 夜魅紫立体厚底专属外壳，顶部圆弧妖王横幅聚焦本周最高波数与历史纪录；
    // 中段单行深紫词缀暗条；下段冲波宝箱轴串起木(绿)→银(蓝)→金(紫)→皇家(橙)四枚统一物品框。
    public sealed class EndlessPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/Endless";
        public const string Mark = "endless_v1";

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
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "endless_panel", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<EndlessPanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            panel._layer = layer;
            panel._stage = stage;
            panel.Bind(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "endless_panel", 0.80f, null, out RectTransform stage);
            float mx = Mathf.Round((ModalKit.DesignW - BoardW) * 0.5f);
            float my = Mathf.Round((ModalKit.DesignH - BoardH) * 0.5f);

            var board = ModalKit.Sliced(stage, "board", "Ui/dungeon_shell_purple", BorderShell, mx, my, BoardW, BoardH, true);
            board.pivot = new Vector2(0.5f, 0.5f);
            board.anchoredPosition = new Vector2(mx + BoardW * 0.5f, -(my + BoardH * 0.5f));
            ModalKit.AddMark(board, Mark);

            var wellGo = new GameObject("well", typeof(RectTransform));
            wellGo.transform.SetParent(board, false);
            var well = wellGo.GetComponent<RectTransform>();
            ModalKit.TopLeft(well, WellX, WellY, WellW, WellH);

            // 1. 顶部圆弧横幅
            var banner = ModalKit.Pic(well, "banner", "Ui/dungeon_banner_purple", 0f, 0f, WellW, 196f, false).rectTransform;
            ModalKit.SolidText(banner, "sub", "本周最好", 15, 282f, 36f, 320f, 24f,
                InkTheme.Hex("DEC7FF"), TextAnchor.MiddleLeft, false);
            ModalKit.OutlinedText(banner, "best", "本周还没打", 36, 280f, 62f, 320f, 64f,
                InkTheme.Hex("FFE455"), TextAnchor.MiddleLeft, 1.8f);
            var pill = ModalKit.Sliced(banner, "pill", "Ui/dungeon_pill_purple", BorderPill, 282f, 136f, 256f, 30f, false);
            ModalKit.SolidText(pill, "t", $"共 {StageCatalog.EndlessWaves} 波 · 7天后换词缀清榜", 14, 0f, 0f, 256f, 30f,
                InkTheme.Hex("E6D4FF"), TextAnchor.MiddleCenter, true);

            // 2. 中段深紫词缀暗条
            var bar = ModalKit.Sliced(well, "rule", "Ui/dungeon_intel_purple", BorderIntel, 20f, 216f, 584f, 74f, false);
            var tag = ModalKit.Sliced(bar, "tag", "Ui/glyph_tag_purple", BorderTicket, 18f, 19f, 104f, 36f, false);
            ModalKit.OutlinedText(tag, "t", "词缀", 18, 0f, 0f, 104f, 34f, Color.white, TextAnchor.MiddleCenter, 1.35f);
            ModalKit.SolidText(bar, "desc", "", 18, 136f, 0f, 432f, 74f,
                ModalKit.CreamLight, TextAnchor.MiddleLeft, true);

            // 3. 冲波宝箱轴
            ModalKit.SolidText(well, "rw_h", "冲波宝箱", 20, 26f, 316f, 140f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.SolidText(well, "rw_sub", $"每守住 {StageCatalog.EndlessBossEvery} 波，结算宝箱升一档", 15,
                320f, 316f, 278f, 28f, ModalKit.TextMuted, TextAnchor.MiddleRight, false);

            const float trackW = 472f;
            var track = ModalKit.Sliced(well, "axis", "Ui/dungeon_axis_track", BorderAxis, 76f, 414f, trackW, 18f, false);
            var fill = ModalKit.Sliced(track, "fill", "Ui/dungeon_axis_fill_purple", BorderAxisFill, 3f, 3f, 14f, 12f, false);
            fill.gameObject.SetActive(false);

            int n = (int)ChestTier.Royal + 1;
            const float s = 96f, gap = 46f;
            float left = Mathf.Round((WellW - (s * n + gap * (n - 1))) * 0.5f);
            for (int i = 0; i < n; i++)
            {
                int need = (i + 1) * StageCatalog.EndlessBossEvery;
                float sx = left + i * (s + gap);
                var tier = (ChestTier)i;
                ChestDef def = ChestCatalog.Get(tier);

                var slot = ModalKit.ItemSlot(well, ModalKit.ChestTone(tier), 96, sx, 375f,
                    ModalKit.Load("Ui/chest_" + def.Key), "1");
                slot.name = "slot" + i;
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(sx + 48f, -(375f + 48f));

                string waveStr = i == n - 1 ? $"{need} 波起" : $"{need} 波";
                ModalKit.SolidText(well, "w" + i, waveStr, 16, sx - 12f, 480f, 120f, 24f,
                    ModalKit.TextBody, TextAnchor.MiddleCenter, true);
                ModalKit.SolidText(well, "nm" + i, def.Name, 14, sx - 12f, 506f, 120f, 22f,
                    ModalKit.TextMuted, TextAnchor.MiddleCenter, false);

                var chk = ModalKit.Pic(well, "chk" + i, "Ui/checkin_check", sx + 68f, 367f, 30f, 30f, true);
                chk.gameObject.SetActive(false);
                var aimTag = ModalKit.Tag(well, "Ui/modal_tag_coral", sx + 10f, 358f, 76f, 24f, "下一目标", false, 12);
                aimTag.name = "aim" + i;
                aimTag.gameObject.SetActive(false);
            }

            // 4. 底部次数与主按钮
            ModalKit.SolidText(well, "plays", "今日剩余 1 / 1 次", 16, 26f, 586f, 176f, 26f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, true);
            var adBtn = ModalKit.HexBtn(well, "md_cyan", 26f, 626f, "", null, out _);
            adBtn.gameObject.name = "ad_btn";
            ModalKit.SetAdBtnLabel(adBtn, "md_cyan", $"+{GameConstants.EndlessAdPlays} 次", true, 20);

            var go = ModalKit.HexBtn(well, "wide_yellow", 286f, 618f, "开始", null, out _);
            go.gameObject.name = "go_btn";

            var crest = ModalKit.Pic(board, "crest", "Ui/dungeon_crest_purple", (BoardW - 248f) * 0.5f, -18f, 248f, 54f, false);
            ModalKit.OutlinedText(crest.rectTransform, "title", "无尽副本", 28, 0f, 0f, 248f, 51f,
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

            Refresh();
            PanelKit.Open(_board);
        }

        void Refresh()
        {
            var banner = _well.Find("banner");
            var subTxt = banner.Find("sub")?.GetComponent<Text>();
            if (subTxt != null)
                subTxt.text = _meta.EndlessBest > 0 ? $"本周最好  ·  历史 {_meta.EndlessBest} 波" : "本周最好";

            int week = _meta.EndlessWeek == DayClock.Week ? _meta.EndlessWeekBest : 0;
            var bestTxt = banner.Find("best")?.GetComponent<Text>();
            if (bestTxt != null)
            {
                bestTxt.text = week > 0 ? $"第 {week} 波" : "本周还没打";
                bestTxt.fontSize = week > 0 ? 48 : 36;
            }

            int days = DayClock.DaysLeftInWeek;
            var pillTxt = banner.Find("pill/t")?.GetComponent<Text>();
            if (pillTxt != null)
            {
                pillTxt.text = days <= 1
                    ? $"共 {StageCatalog.EndlessWaves} 波 · 今晚零点换词缀清榜"
                    : $"共 {StageCatalog.EndlessWaves} 波 · {days}天后换词缀清榜";
            }

            StageRule rule = StageRules.ThisWeek;
            var bar = _well.Find("rule");
            float tw = Mathf.Max(68f, ModalKit.MeasureText(rule.Name, 18) + 34f);
            var ruleTag = bar.Find("tag") as RectTransform;
            if (ruleTag != null)
            {
                ModalKit.TopLeft(ruleTag, 18f, 19f, tw, 36f);
                var rtTxt = ruleTag.Find("t")?.GetComponent<Text>();
                if (rtTxt != null)
                {
                    ModalKit.TopLeft(rtTxt.rectTransform, 0f, 0f, tw, 34f);
                    rtTxt.text = rule.Name;
                }
            }
            var descTxt = bar.Find("desc")?.GetComponent<Text>();
            if (descTxt != null)
            {
                ModalKit.TopLeft(descTxt.rectTransform, 18f + tw + 14f, 0f, 540f - tw, 74f);
                descTxt.text = rule.Desc;
            }

            const float trackW = 472f;
            int n = (int)ChestTier.Royal + 1;
            int maxNeed = n * StageCatalog.EndlessBossEvery;
            float ratio = Mathf.Clamp01((_meta.EndlessBest - 4f) / Mathf.Max(1f, maxNeed - 4f));
            var fill = _well.Find("axis/fill") as RectTransform;
            if (fill != null)
            {
                bool showFill = _meta.EndlessBest > 0 && ratio > 0.02f;
                fill.gameObject.SetActive(showFill);
                if (showFill)
                    ModalKit.TopLeft(fill, 3f, 3f, Mathf.Clamp((trackW - 6f) * ratio, 14f, trackW - 6f), 12f);
            }

            int next = -1;
            for (int i = 0; i < n; i++)
                if (_meta.EndlessBest < (i + 1) * StageCatalog.EndlessBossEvery) { next = i; break; }

            for (int i = 0; i < n; i++)
            {
                int need = (i + 1) * StageCatalog.EndlessBossEvery;
                bool got = _meta.EndlessBest >= need;
                bool aim = i == next;
                var slot = _well.Find("slot" + i) as RectTransform;
                if (slot != null)
                {
                    UiAnim.On(slot).Clear();
                    slot.localScale = Vector3.one;
                    if (aim) UiAnim.On(slot).Breathe(slot, 0f, 0.045f, 1.2f);
                }
                var wTxt = _well.Find("w" + i)?.GetComponent<Text>();
                if (wTxt != null) wTxt.color = aim ? InkTheme.Hex("C84828") : ModalKit.TextBody;
                var chk = _well.Find("chk" + i);
                if (chk != null) chk.gameObject.SetActive(got);
                var aimTag = _well.Find("aim" + i);
                if (aimTag != null) aimTag.gameObject.SetActive(!got && aim);
            }

            int cap = _meta.EndlessPlaysCap;
            int left = _meta.EndlessPlaysLeft;
            bool hasAd = !_meta.EndlessAdDone;

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
            AdStub.Reward("endless_plays", () =>
            {
                if (this == null || !_meta.GrantEndlessAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }

        void OnStart()
        {
            if (_meta.EndlessPlaysLeft <= 0)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.EndlessAdDone
                    ? "今天的次数用完了，明天再来"
                    : $"今天的次数用完了，看广告再开 {GameConstants.EndlessAdPlays} 局");
                return;
            }
            AudioBus.Tap();
            Action start = _start;
            Destroy(gameObject);
            start?.Invoke();
        }
    }
}
