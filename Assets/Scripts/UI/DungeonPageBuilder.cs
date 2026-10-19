using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 副本页的壳：上面每日副本、下面无尽副本两张卡。烘 Home.prefab 和运行时补页走同一份坐标。
    public static class DungeonPageBuilder
    {
        // 烘出来的预制里有这个空节点，编辑器编译后看到它就不再重烘。
        public const string Mark = "dungeon_v1";

        public const float CardW = 648f;
        public const float CardH = 452f;
        const float CardTop = 10f;
        const float CardGap = 16f;
        // 横幅图 1024x440，左右各让出卡框的描边。
        const float ArtW = 612f;
        const float ArtH = ArtW * 440f / 1024f;
        const float ArtTop = 14f;
        const float RibW = 300f;
        const float RibH = RibW * 240f / 900f;
        const float TextX = -110f;
        const float TextW = 390f;
        const float GoW = 200f;
        const float GoH = 84f;
        const float GoX = 206f;

        public static readonly Color Jade = InkTheme.Hex("2F8F6B");
        public static readonly Color JadeDeep = InkTheme.Hex("1E6149");
        public static readonly Color Grape = InkTheme.Hex("7B3FB8");
        public static readonly Color GrapeDeep = InkTheme.Hex("4E2380");

        static readonly Vector4 FrameSlice = new Vector4(30f, 34f, 30f, 30f);
        static readonly Vector4 PillSlice = new Vector4(44f, 8f, 44f, 8f);

        // 烘预制时换成 AssetDatabase 里的图，存进预制才不会丢引用。
        public static Func<string, Sprite> SpriteOf;

        static Sprite Spr(string key)
        {
            if (SpriteOf != null)
            {
                Sprite got = SpriteOf(key);
                if (got != null) return got;
            }
            return InkSprites.Load(key);
        }

        public static DungeonPage Build(RectTransform page)
        {
            var host = page.GetComponent<DungeonPage>();
            if (host == null) host = page.gameObject.AddComponent<DungeonPage>();
            host.Daily = Card(page, "daily", CardTop, "每日副本", "Ui/ribbon_daily", "Ui/dungeon_daily", Jade);
            host.Endless = Card(page, "endless", CardTop + CardH + CardGap, "无尽副本", "Ui/ribbon_endless",
                "Ui/dungeon_endless", Grape);
            var mark = new GameObject(Mark, typeof(RectTransform));
            mark.transform.SetParent(page, false);
            return host;
        }

        static DungeonCard Card(RectTransform page, string name, float top, string title, string ribbon, string art,
            Color accent)
        {
            var frame = Pic(page, name, "Ui/panel_skin", new Vector2(0f, top), new Vector2(CardW, CardH), Pin.Top, FrameSlice);
            frame.raycastTarget = false;
            var card = frame.gameObject.AddComponent<DungeonCard>();
            card.Frame = frame;
            var rt = frame.rectTransform;

            card.Art = Pic(rt, "art", art, new Vector2(0f, ArtTop), new Vector2(ArtW, ArtH), Pin.Top);
            card.Art.preserveAspect = false;
            card.Art.raycastTarget = true;
            card.ArtButton = card.Art.gameObject.AddComponent<Button>();
            card.ArtButton.targetGraphic = card.Art;
            card.ArtButton.transition = Selectable.Transition.None;

            card.Lock = Pic(card.Art.rectTransform, "lock", "icon_lock", new Vector2(0f, 0f), new Vector2(104f, 104f), Pin.Center);
            card.Lock.raycastTarget = false;
            card.Lock.gameObject.SetActive(false);

            card.Chip = Pic(rt, "chip", "Ui/panel_row", new Vector2(30f, ArtTop + 16f), new Vector2(236f, 48f), Pin.TopLeft, PillSlice);
            card.Chip.color = accent;
            card.Chip.raycastTarget = false;
            card.ChipLabel = Label(card.Chip.rectTransform, "t", "", 20, new Vector2(0f, 2f), new Vector2(220f, 34f));
            card.ChipLabel.color = InkTheme.CardFace;
            UiKit.Bold(card.ChipLabel);

            card.Ribbon = Pic(rt, "ribbon", ribbon, new Vector2(0f, ArtTop + ArtH - RibH * 0.52f), new Vector2(RibW, RibH), Pin.Top);
            card.Ribbon.raycastTarget = false;
            // 两种飘带正面那块都比整图中心高一点；无尽那条上沿还有一排尖角，再往下压一点。
            float lift = ribbon.EndsWith("endless") ? -0.02f : 0.10f;
            card.Title = Label(card.Ribbon.rectTransform, "t", title, 32, new Vector2(0f, RibH * lift), new Vector2(240f, 46f));
            card.Title.color = InkTheme.CardFace;
            UiKit.Bold(card.Title);

            float textTop = ArtTop + ArtH + RibH * 0.48f + 8f;
            card.Rule = Label(rt, "rule", "", 21, new Vector2(TextX, textTop), new Vector2(TextW, 30f), Pin.Top, TextAnchor.MiddleLeft);
            card.Rule.color = InkTheme.TextMid;
            card.Rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.Rule.verticalOverflow = VerticalWrapMode.Truncate;
            card.Rule.resizeTextForBestFit = true;
            card.Rule.resizeTextMinSize = 15;
            card.Rule.resizeTextMaxSize = 21;
            card.Best = Label(rt, "best", "", 30, new Vector2(TextX, textTop + 32f), new Vector2(TextW, 42f), Pin.Top, TextAnchor.MiddleLeft);
            card.Best.color = InkTheme.TextDark;
            UiKit.Bold(card.Best);
            card.Plays = Label(rt, "plays", "", 21, new Vector2(TextX, textTop + 72f), new Vector2(TextW, 30f), Pin.Top, TextAnchor.MiddleLeft);
            card.Plays.color = InkTheme.TextMid;

            var go = Pic(rt, "go", "Ui/panel_row", new Vector2(GoX, textTop + 4f), new Vector2(GoW, GoH), Pin.Top, PillSlice);
            go.color = accent;
            card.Go = go.gameObject.AddComponent<Button>();
            card.Go.targetGraphic = go;
            card.Go.transition = Selectable.Transition.None;
            card.GoLabel = Label(go.rectTransform, "t", "进入", 32, new Vector2(0f, 3f), new Vector2(GoW, 48f));
            card.GoLabel.color = InkTheme.CardFace;
            UiKit.Bold(card.GoLabel);

            card.Dot = Pic(go.rectTransform, "dot", "Ui/codex_dot", new Vector2(GoW * 0.5f - 12f, GoH * 0.5f - 6f),
                new Vector2(30f, 30f), Pin.Center);
            card.Dot.raycastTarget = false;
            card.Dot.gameObject.SetActive(false);
            return card;
        }

        static Image Pic(Transform parent, string name, string key, Vector2 pos, Vector2 size, Pin pin,
            Vector4 slice = default)
        {
            var rt = UiKit.Panel(parent, name, pos, size, Color.white, pin);
            var img = rt.GetComponent<Image>();
            Sprite spr = Spr(key);
            img.sprite = spr;
            bool sliced = spr != null && (spr.border.sqrMagnitude > 1f || slice.sqrMagnitude > 0.01f);
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = !sliced;
            return img;
        }

        static Text Label(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box, Pin pin = Pin.Center,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = UiKit.Label(parent, name, text, size, pos, box, anchor, pin);
            t.raycastTarget = false;
            return t;
        }
    }
}
