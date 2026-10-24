using System;
using UnityEngine;

namespace InkLine
{
    public static class ConfirmPanel
    {
        public static RectTransform Show(RectTransform layer, Action yes, Action no)
        {
            var dim = UiKit.Dimmer(layer);
            var board = UiKit.Stroke(dim, "yesno", Vector2.zero, new Vector2(520, 280), Pin.Center, 6f);
            UiKit.Label(board, "t", "覆盖这格？星级会清零。", 28, new Vector2(0, 60), new Vector2(460, 50));
            UiKit.Btn(board, "y", "覆盖", new Vector2(-110, -60), new Vector2(180, 64), yes);
            UiKit.Btn(board, "n", "取消", new Vector2(110, -60), new Vector2(180, 64), no, false);
            return dim;
        }

        // 点完自己关掉。keep 是建议的那一边，放左边金色；drop 是会清掉东西的那一边。
        public static void Ask(RectTransform layer, string text, string keep, string drop, Action onDrop)
        {
            var dim = UiKit.Dimmer(layer);
            var board = UiKit.Stroke(dim, "yesno", Vector2.zero, new Vector2(560f, 400f), Pin.Center, 6f);
            var t = UiKit.Label(board, "t", text, 26, new Vector2(0f, 58f), new Vector2(500f, 160f));
            t.lineSpacing = 1.15f;
            void Shut()
            {
                if (dim != null) UnityEngine.Object.Destroy(dim.gameObject);
            }
            UiKit.Btn(board, "y", keep, new Vector2(-120f, -120f), new Vector2(200f, 68f), Shut);
            UiKit.Btn(board, "n", drop, new Vector2(120f, -120f), new Vector2(200f, 68f), () =>
            {
                Shut();
                onDrop?.Invoke();
            }, false);
        }
    }
}
