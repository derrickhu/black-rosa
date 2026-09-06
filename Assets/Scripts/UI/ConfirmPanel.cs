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
    }
}
