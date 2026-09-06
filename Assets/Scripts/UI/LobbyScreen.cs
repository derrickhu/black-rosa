using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public static class LobbyScreen
    {
        public static void Build(RectTransform layer, MetaProgress meta, Action<int> startStage)
        {
            UiKit.PaperSheet(layer);
            float top = ScreenFit.TopPad + 28f;
            var plaque = UiKit.Stroke(layer, "title", new Vector2(0, top), new Vector2(580, 100), Pin.Top, 8f);
            UiKit.Stroke(plaque, "inner", Vector2.zero, new Vector2(548, 68), Pin.Center, 3f);
            UiKit.Label(plaque, "t", "墨弹防线", 44, Vector2.zero, new Vector2(520, 70));

            float chipsY = top + 132f;
            Chip(layer, InkArt.Icon(InkShape.Star), meta.TotalStars().ToString(), new Vector2(-168, chipsY));
            Chip(layer, InkArt.Icon(InkShape.Cannon), meta.StartEmitters.ToString(), new Vector2(0, chipsY));
            Chip(layer, InkArt.Icon(InkShape.Coin), meta.StartGold.ToString(), new Vector2(168, chipsY));

            for (int i = 0; i < GameConstants.ChapterStageCount; i++)
            {
                int idx = i;
                bool open = meta.Unlocked(i);
                float x = (i % 4 - 1.5f) * 150f;
                float y = 72f - (i / 4) * 170f;
                Seal(layer, idx, open, meta.Stars[i], new Vector2(x, y), () =>
                {
                    if (open) startStage(idx);
                });
            }

            UiKit.Label(layer, "help", "左右拖动底栏炮串。金币够了点改装。\n同牌可铺开，也可叠成 2、3 星。", 22, new Vector2(0, ScreenFit.BottomPad + 28), new Vector2(620, 80), TextAnchor.MiddleCenter, Pin.Bottom);
        }

        static void Chip(Transform parent, Sprite icon, string text, Vector2 pos)
        {
            var box = UiKit.Panel(parent, "chip", pos, new Vector2(150, 56), Color.clear, Pin.Top);
            box.GetComponent<Image>().raycastTarget = false;
            UiKit.Icon(box, icon, new Vector2(-30f, 0f), 44f);
            UiKit.Label(box, "n", text, 28, new Vector2(28f, 0f), new Vector2(72, 40), TextAnchor.MiddleLeft);
        }

        static void Seal(Transform parent, int index, bool open, int stars, Vector2 pos, Action click)
        {
            Color line = open ? InkTheme.Ink : InkTheme.Locked;
            var plate = UiKit.Stroke(parent, "st" + index, pos, new Vector2(128, 128), Pin.Center, open ? 7f : 5f, line);
            var img = plate.GetComponent<Image>();
            var btn = plate.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => click());
            if (!open)
            {
                UiKit.Icon(plate, InkArt.Icon(InkShape.Lock, 64), Vector2.zero, 52f);
                return;
            }
            if (stars <= 0)
            {
                UiKit.Label(plate, "n", (index + 1).ToString(), 36, Vector2.zero, new Vector2(80, 50));
                return;
            }
            float span = (stars - 1) * 26f;
            for (int s = 0; s < stars; s++)
                UiKit.Icon(plate, InkArt.Icon(InkShape.Diamond, 48), new Vector2(-span * 0.5f + s * 26f, 0f), 24f);
        }
    }
}
