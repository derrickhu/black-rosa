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

            var board = UiKit.Stroke(dim, "board", new Vector2(0f, -12f), new Vector2(480f, 640f), Pin.Center, 8f);
            var boardImg = board.GetComponent<Image>();
            if (boardImg != null) boardImg.raycastTarget = true;
            UiKit.Label(board, "title", "GM", 28, new Vector2(0f, 276f), new Vector2(200f, 40f));

            var rows = new (string label, Action act)[]
            {
                ("墨 +200", () => meta.AddInk(InkGrant)),
                ("体力补满", () => meta.FillStamina()),
                ("碎片 +20", () => meta.GrantShards(ShardGrant)),
                ("关卡全开", () => meta.UnlockStages()),
                ("皮肤全开", () => meta.UnlockSkins()),
                ("技能全开", () => meta.UnlockSpells()),
                ("改装满级", () => meta.MaxForge()),
                ("清档", () =>
                {
                    MetaProgress.Wipe();
                    if (reload != null) reload();
                })
            };

            for (int i = 0; i < rows.Length; i++)
            {
                int idx = i;
                float y = 214f - i * 64f;
                var row = UiKit.Btn(board, "g" + i, rows[i].label, new Vector2(0f, y), new Vector2(360f, 52f), () =>
                {
                    rows[idx].act();
                    if (idx == rows.Length - 1) return;
                    if (refresh != null) refresh();
                }, false);
                row.transform.SetAsLastSibling();
            }
            dim.SetAsLastSibling();
        }
    }
}
