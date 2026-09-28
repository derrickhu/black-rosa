using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 首页左上角的 GM。调用方先确认 WxBridge.IsSimulator，真机不会走到这里。
    public static class GmBar
    {
        const int InkGrant = 200;
        const int ShardGrant = 20;

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

            var board = UiKit.Stroke(dim, "board", new Vector2(0f, -12f), new Vector2(480f, 780f), Pin.Center, 8f);
            var boardImg = board.GetComponent<Image>();
            if (boardImg != null) boardImg.raycastTarget = true;
            UiKit.Label(board, "title", "GM", 28, new Vector2(0f, 362f), new Vector2(200f, 40f));

            var rows = new (string label, Action act)[]
            {
                ("墨 +200", () => meta.AddInk(InkGrant)),
                ("体力补满", () => meta.FillStamina()),
                ("碎片 +20", () => meta.GrantShards(ShardGrant)),
                ("关卡全开", () => meta.UnlockStages()),
                ("皮肤全开", () => meta.UnlockSkins()),
                ("技能全开", () => meta.UnlockSpells()),
                ("改装满级", () => meta.MaxForge()),
                ("签到跨一天", () => meta.GmCheckNextDay()),
                ("图鉴全开", () => meta.UnlockCodex()),
                (GlowLabel(), () => ShotSparks.Bright = !ShotSparks.Bright),
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

            for (int i = 0; i < rows.Length; i++)
            {
                int idx = i;
                float y = 314f - i * 50f;
                Button row = null;
                row = UiKit.Btn(board, "g" + i, rows[i].label, new Vector2(0f, y), new Vector2(360f, 44f), () =>
                {
                    rows[idx].act();
                    if (idx == rows.Length - 1 || rows[idx].label.StartsWith("看")) return;
                    PanelKit.SetText(row, rows[idx].label.StartsWith("炮弹") ? GlowLabel() : rows[idx].label);
                    if (refresh != null) refresh();
                }, false);
                row.transform.SetAsLastSibling();
            }
            dim.SetAsLastSibling();
        }

        static void ShowPreview(RectTransform gm, int kind)
        {
            UnityEngine.Object.Destroy(gm.gameObject);
            Preview?.Invoke(kind);
        }

        static string GlowLabel() => ShotSparks.Bright ? "炮弹发光：炫（点切实）" : "炮弹发光：实（点切炫）";
    }
}
