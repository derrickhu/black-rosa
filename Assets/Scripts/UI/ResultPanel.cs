using System;
using UnityEngine;

namespace InkLine
{
    public static class ResultPanel
    {
        public static RectTransform Show(RectTransform layer, bool win, int stars, int ink,
            bool canRevive, Action revive, Action doubleInk, Action lobby, Action next)
        {
            var dim = UiKit.Dimmer(layer);
            var board = UiKit.Stroke(dim, "end", Vector2.zero, new Vector2(560, 480), Pin.Center, 7f);
            UiKit.Label(board, "t", win ? "通关" : "防线失守", 40, new Vector2(0, 176), new Vector2(500, 56));
            if (win)
            {
                float span = (stars - 1) * 36f;
                for (int i = 0; i < stars; i++)
                    UiKit.Icon(board, InkArt.Icon(InkShape.Diamond, 64), new Vector2(-span * 0.5f + i * 36f, 116f), 32f);
            }
            if (win && ink > 0)
            {
                var gain = UiKit.Label(board, "ink", $"墨  +{ink}", 30, new Vector2(0, 64), new Vector2(400, 40));
                UiKit.Bold(gain);
            }
            UiKit.Label(board, "d",
                win ? "棋盘已清空。墨可以在首页改造炮台。" : "体力不扣，可以看广告续命，或回首页。",
                24, new Vector2(0, win ? 20 : 56), new Vector2(480, 70));
            if (win && doubleInk != null)
                UiKit.Btn(board, "dbl", "看广告  墨翻倍", new Vector2(0, -30), new Vector2(300, 64), doubleInk);
            if (!win && canRevive)
                UiKit.Btn(board, "rev", "续命", new Vector2(-120, -100), new Vector2(200, 64), revive);
            float lobbyX = win ? (next != null ? -130f : 0f) : 120f;
            UiKit.Btn(board, "lobby", "首页", new Vector2(lobbyX, -100), new Vector2(200, 64), lobby, !win);
            if (win && next != null)
                UiKit.Btn(board, "next", "下一关", new Vector2(130, -100), new Vector2(200, 64), next);
            return dim;
        }
    }
}
