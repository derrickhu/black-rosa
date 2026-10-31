using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 首页左上角的 GM。调用方先确认 WxBridge.IsSimulator，真机不会走到这里。
    public static class GmBar
    {
        const int InkGrant = 200;
        const int CardGrant = 20;
        const int DiamondGrant = 100;
        const int FoilGrant = 100;
        const int EventBankGrant = 1000;

        // 预览结算页：0 通关，1 续命，2 失败。GameFlow 启动时挂上。
        public static Action<int> Preview;

        public static void Attach(RectTransform layer, MetaProgress meta, Action reload, Action refresh)
        {
            var btn = UiKit.Btn(layer, "gm", "GM", new Vector2(16f, HomeTop()), new Vector2(72f, 44f),
                () => Open(layer, meta, reload, refresh), false, Pin.TopLeft);
            btn.transform.SetAsLastSibling();
        }

        static float HomeTop()
        {
            return ScreenFit.CapsuleGuard > 0f
                ? ScreenFit.CapsuleGuard
                : Mathf.Clamp(ScreenFit.TopPad * 0.5f, 6f, 52f);
        }

        static void Open(RectTransform layer, MetaProgress meta, Action reload, Action refresh)
        {
            var old = layer.Find("gmPanel");
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);

            var dim = UiKit.Dimmer(layer);
            dim.gameObject.name = "gmPanel";
            dim.GetComponent<Image>().color = new Color(0.16f, 0.11f, 0.08f, 0.62f);
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.targetGraphic = dim.GetComponent<Image>();
            dimBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(dim.gameObject));

            var board = UiKit.Stroke(dim, "board", Vector2.zero, new Vector2(480f, 1240f), Pin.Center, 8f);
            var boardImg = board.GetComponent<Image>();
            if (boardImg != null) boardImg.raycastTarget = true;
            UiKit.Label(board, "title", "GM", 28, new Vector2(0f, 580f), new Vector2(200f, 40f));

            var rows = new (string label, Action act)[]
            {
                ("墨 +200", () => meta.AddInk(InkGrant)),
                ("体力补满", () => meta.FillStamina()),
                ("钻石 +100", () => meta.AddDiamond(DiamondGrant)),
                ("金箔 +100", () => meta.AddFoil(FoilGrant)),
                ("道具卡 +20", () => meta.GrantCards(CardGrant)),
                ("发金宝箱", () =>
                {
                    int slot = meta.GrantChest(ChestTier.Gold);
                    if (slot == -1) ChestOverflowView.Show(layer, meta, refresh);
                    else if (slot == -3)
                        InkToast.Show(layer, $"手里的宝箱太多，这只换成 {ChestCatalog.InkAvg(ChestTier.Gold)} 墨");
                }),
                ("宝箱解完", () => meta.GmFinishChests()),
                ("皮肤全开", () => meta.UnlockSkins()),
                ("道具全开", () => meta.UnlockItems()),
                ("改装满级", () => meta.MaxForge()),
                ("签到跨一天", () => meta.GmCheckNextDay()),
                ("图鉴全开", () => meta.UnlockCodex()),
                ("印章各来一套", () => meta.GmGrantSeals()),
                (GlowLabel(), () => ShotSparks.Bright = !ShotSparks.Bright),
                (GoldLabel(), NextGoldMul),
                ("聚宝盆 +1000", () => meta.GmEventBank(EventBankGrant)),
                ("活动重置", () => meta.GmEventReset()),
                ("看通关页", () => ShowPreview(dim, 0)),
                ("看续命页", () => ShowPreview(dim, 1)),
                ("看失败页", () => ShowPreview(dim, 2)),
                ("清档", () =>
                {
                    MetaProgress.Wipe();
                    if (reload != null) reload();
                    CloudSync.FlushNow("wipe");
                })
            };

            float y = 528f;
            for (int i = 0; i < rows.Length; i++)
            {
                if (i == 7) y = DrawChapters(board, meta, y, refresh);
                int idx = i;
                Button row = null;
                row = UiKit.Btn(board, "g" + i, rows[i].label, new Vector2(0f, y), new Vector2(360f, 44f), () =>
                {
                    rows[idx].act();
                    if (idx == rows.Length - 1 || rows[idx].label.StartsWith("看")) return;
                    string label = rows[idx].label;
                    if (label.StartsWith("炮弹")) label = GlowLabel();
                    else if (label.StartsWith("金币获取")) label = GoldLabel();
                    PanelKit.SetText(row, label);
                    if (refresh != null) refresh();
                }, false);
                row.transform.SetAsLastSibling();
                y -= 48f;
            }
            dim.SetAsLastSibling();
        }

        // 「没」和 1～12。点哪章，主线就通到那一章，再点「没」全部清掉。
        static float DrawChapters(RectTransform board, MetaProgress meta, float y, Action refresh)
        {
            var cap = UiKit.Label(board, "chcap", "", 22, new Vector2(0f, y), new Vector2(400f, 30f));
            UiKit.Bold(cap);
            cap.color = InkTheme.TextDark;
            y -= 34f;
            var chips = new Button[GameConstants.Chapters + 1];
            for (int n = 0; n <= GameConstants.Chapters; n++)
            {
                bool top = n <= 6;
                int col = top ? n : n - 7;
                int cols = top ? 7 : 6;
                float x = (col - (cols - 1) * 0.5f) * 54f;
                float cy = top ? y : y - 46f;
                int chapter = n;
                chips[n] = ChapterChip(board, "ch" + n, n == 0 ? "没" : n.ToString(), new Vector2(x, cy), () =>
                {
                    meta.GmClearThrough(chapter);
                    PaintChapters(chips, cap, meta.GmChapterThrough());
                    if (refresh != null) refresh();
                });
            }
            PaintChapters(chips, cap, meta.GmChapterThrough());
            return y - 46f - 48f;
        }

        static Button ChapterChip(Transform parent, string name, string text, Vector2 pos, Action click)
        {
            var rt = UiKit.Stroke(parent, name, pos, new Vector2(48f, 40f), Pin.Center, 3f, InkTheme.Outline, InkTheme.Plain, 12f);
            var img = rt.GetComponent<Image>();
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => click());
            var t = UiKit.Label(rt, "t", text, 22, Vector2.zero, new Vector2(48f, 40f));
            UiKit.Bold(t);
            return btn;
        }

        static void PaintChapters(Button[] chips, Text cap, int through)
        {
            cap.text = through <= 0 ? "主线还没通" : through >= GameConstants.Chapters ? "主线全通" : "主线通到第" + through + "章";
            for (int n = 0; n < chips.Length; n++)
            {
                bool on = n == through;
                var img = chips[n].GetComponent<Image>();
                if (img != null) img.color = on ? InkTheme.Cta : InkTheme.Plain;
                var t = chips[n].GetComponentInChildren<Text>();
                if (t != null) t.color = on ? InkTheme.CardFace : InkTheme.TextDark;
            }
        }

        static void ShowPreview(RectTransform gm, int kind)
        {
            UnityEngine.Object.Destroy(gm.gameObject);
            Preview?.Invoke(kind);
        }

        static string GlowLabel() => ShotSparks.Bright ? "炮弹发光：炫（点切实）" : "炮弹发光：实（点切炫）";

        static readonly int[] GoldMuls = { 1, 2, 5, 10 };

        static string GoldLabel() => $"金币获取 ×{BattleWorld.GmGoldMul}（点切换）";

        static void NextGoldMul()
        {
            int i = Array.IndexOf(GoldMuls, BattleWorld.GmGoldMul);
            BattleWorld.GmGoldMul = GoldMuls[(i + 1) % GoldMuls.Length];
        }
    }
}
