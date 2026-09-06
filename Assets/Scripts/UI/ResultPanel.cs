using System;
using UnityEngine;

namespace InkLine
{
    public static class ResultPanel
    {
        public static RectTransform Show(RectTransform layer, bool win, int stars, bool canRevive, Action revive, Action lobby, Action next)
        {
            var dim = UiKit.Dimmer(layer);
            var board = UiKit.Stroke(dim, "end", Vector2.zero, new Vector2(560, 440), Pin.Center, 7f);
            UiKit.Label(board, "t", win ? "通关" : "防线失守", 40, new Vector2(0, 150), new Vector2(500, 56));
            if (win)
            {
                float span = (stars - 1) * 36f;
                for (int i = 0; i < stars; i++)
                    UiKit.Icon(board, InkArt.Icon(InkShape.Diamond, 64), new Vector2(-span * 0.5f + i * 36f, 88f), 32f);
            }
            UiKit.Label(board, "d", win ? "棋盘已清空。下一关重新构筑。" : "可以看广告续命，或回大厅。", 24, new Vector2(0, win ? 30 : 70), new Vector2(480, 70));
            if (!win && canRevive)
                UiKit.Btn(board, "rev", "续命", new Vector2(-120, -80), new Vector2(200, 64), revive);
            UiKit.Btn(board, "lobby", "大厅", new Vector2(win ? 0 : 120, -80), new Vector2(200, 64), lobby, !win);
            if (win && next != null)
                UiKit.Btn(board, "next", "下一关", new Vector2(0, -168), new Vector2(260, 64), next);
            return dim;
        }
    }
}
