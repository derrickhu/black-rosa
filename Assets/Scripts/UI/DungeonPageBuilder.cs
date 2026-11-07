using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 副本页的壳：上面每日副本、下面无尽副本。两张都是玩法底板，左边立绘，右边现有文案。
    public static class DungeonPageBuilder
    {
        // 烘出来的预制里有这个空节点。换布局就改这个名字，编辑器编译后只重烘这一页。
        public const string Mark = "dungeon_v2";

        public const float CardW = 640f;
        public const float CardH = 240f;
        // 页本身已经从屏幕顶让出 96。再往下 48，卡就停在货币栏下面。
        const float CardTop = 48f;
        const float CardGap = 18f;
        // 立绘铺满左边那块圆，和底板的挖圆是同一组数：圆心 (73,171)，图宽盖住可见的中段。
        const float ArtW = 558f;
        const float ArtX = -141f;
        const float TextX = 286f;
        const float TextW = 338f;
        const float GoW = 216f;

        public static readonly Color Jade = InkTheme.Hex("2F8F6B");
        public static readonly Color JadeDeep = InkTheme.Hex("1E6149");
        public static readonly Color Grape = InkTheme.Hex("7B3FB8");
        public static readonly Color GrapeDeep = InkTheme.Hex("4E2380");

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
            host.Daily = Card(page, "daily", CardTop, "每日副本", "Ui/mode_plate_green", "Ui/dungeon_daily");
            host.Endless = Card(page, "endless", CardTop + CardH + CardGap, "无尽副本", "Ui/mode_plate_purple",
                "Ui/dungeon_endless");
            var mark = new GameObject(Mark, typeof(RectTransform));
            mark.transform.SetParent(page, false);
            return host;
        }

        static DungeonCard Card(RectTransform page, string name, float top, string title, string plate, string art)
        {
            var frame = Pic(page, name, plate, new Vector2(0f, top), new Vector2(CardW, CardH), Pin.Top);
            frame.raycastTarget = false;
            var card = frame.gameObject.AddComponent<DungeonCard>();
            card.Frame = frame;
            var rt = frame.rectTransform;

            var maskRt = UiKit.Panel(rt, "artmask", Vector2.zero, new Vector2(CardW, CardH), Color.white, Pin.TopLeft);
            var maskImg = maskRt.GetComponent<Image>();
            Sprite maskSpr = Spr("Ui/mode_plate_mask");
            maskImg.sprite = maskSpr;
            maskImg.type = Image.Type.Simple;
            maskImg.preserveAspect = false;
            maskImg.raycastTarget = false;
            var mask = maskRt.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            card.Art = Pic(maskRt, "art", art, new Vector2(ArtX, 0f), new Vector2(ArtW, CardH), Pin.TopLeft);
            card.Art.preserveAspect = false;
            card.Art.raycastTarget = true;
            card.ArtButton = card.Art.gameObject.AddComponent<Button>();
            card.ArtButton.targetGraphic = card.Art;
            card.ArtButton.transition = Selectable.Transition.None;

            card.Lock = Pic(rt, "lock", "icon_lock", new Vector2(130f, 88f), new Vector2(72f, 72f), Pin.TopLeft);
            card.Lock.raycastTarget = false;
            card.Lock.gameObject.SetActive(false);

            var chipRt = UiKit.Panel(rt, "chip", new Vector2(TextX, 14f), new Vector2(TextW, 28f),
                new Color(1f, 1f, 1f, 0f), Pin.TopLeft);
            card.Chip = chipRt.GetComponent<Image>();
            card.Chip.raycastTarget = false;
            card.ChipLabel = Label(chipRt, "t", "", 18, Vector2.zero, new Vector2(TextW, 28f), Pin.Center, TextAnchor.MiddleLeft);
            card.ChipLabel.color = InkTheme.TextMid;

            card.Title = Label(rt, "title", title, 26, new Vector2(18f, 186f), new Vector2(210f, 40f), Pin.TopLeft, TextAnchor.MiddleLeft);
            card.Title.color = Color.white;
            UiKit.Bold(card.Title);
            var edge = card.Title.gameObject.AddComponent<Outline>();
            edge.effectColor = InkTheme.Ink;
            edge.effectDistance = new Vector2(1.5f, -1.5f);

            card.Rule = Label(rt, "rule", "", 18, new Vector2(TextX, 44f), new Vector2(TextW, 56f), Pin.TopLeft, TextAnchor.UpperLeft);
            card.Rule.color = InkTheme.TextDark;
            card.Rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.Rule.verticalOverflow = VerticalWrapMode.Truncate;
            card.Best = Label(rt, "best", "", 22, new Vector2(TextX, 104f), new Vector2(TextW, 36f), Pin.TopLeft, TextAnchor.MiddleLeft);
            card.Best.color = InkTheme.TextDark;
            UiKit.Bold(card.Best);
            card.Plays = Label(rt, "plays", "", 16, new Vector2(TextX, 142f), new Vector2(TextW, 24f), Pin.TopLeft, TextAnchor.MiddleLeft);
            card.Plays.color = InkTheme.TextMid;

            Sprite goSpr = Spr("Ui/home_go");
            float goH = 55f;
            if (goSpr != null && goSpr.rect.width > 1f)
                goH = GoW * goSpr.rect.height / goSpr.rect.width;
            float goX = TextX + (TextW - GoW) * 0.5f;
            float goY = CardH - 14f - goH;
            var go = Pic(rt, "go", "Ui/home_go", new Vector2(goX, goY), new Vector2(GoW, goH), Pin.TopLeft);
            go.type = Image.Type.Simple;
            go.preserveAspect = false;
            card.Go = go.gameObject.AddComponent<Button>();
            card.Go.targetGraphic = go;
            card.Go.transition = Selectable.Transition.None;
            card.GoLabel = Label(go.rectTransform, "t", "进入", 22, new Vector2(0f, 4f), new Vector2(GoW, 40f));
            card.GoLabel.color = InkTheme.Ink;
            UiKit.Bold(card.GoLabel);

            card.Dot = Pic(go.rectTransform, "dot", "Ui/home_dot", new Vector2(8f, 2f), new Vector2(18f, 18f), Pin.TopRight);
            card.Dot.raycastTarget = false;
            card.Dot.gameObject.SetActive(false);
            return card;
        }

        static Image Pic(Transform parent, string name, string key, Vector2 pos, Vector2 size, Pin pin)
        {
            var rt = UiKit.Panel(parent, name, pos, size, Color.white, pin);
            var img = rt.GetComponent<Image>();
            Sprite spr = Spr(key);
            img.sprite = spr;
            bool sliced = spr != null && spr.border.sqrMagnitude > 1f;
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
