using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 副本页的壳：上面每日副本、下面无尽副本。
    // 两张都是玩法底板，浅色收到框里，左边立绘，右边宝箱、结束时间和进入键。
    public static class DungeonPageBuilder
    {
        // 烘出来的预制里有这个空节点。换布局就改这个名字，编辑器编译后只重烘这一页。
        public const string Mark = "dungeon_v8";

        public const float CardW = 640f;
        public const float CardH = 240f;
        // 页顶已经让出货币条。运行时 HomeReskin.FitDungeon 还会按屏高再往下对齐。
        const float CardTop = 72f;
        public const float CardGap = 18f;
        // 立绘铺满左边那块圆，和底板的挖圆是同一组数：圆心 (73,171)，图宽盖住可见的中段。
        const float ArtW = 558f;
        const float ArtX = -141f;
        // 右边这一列以 464 为中线，和三个 60 的物品框、次数条、中号进入键对齐。
        const float CenterX = 464f;
        const float Slot = 60f;
        const float SlotGap = 10f;
        const float SlotY = 58f;
        const float Chest = 48f;
        const float BarY = 132f;
        const float BarH = 28f;
        const float BarW = 150f;
        const float Plus = 26f;
        const float GoW = 176f;
        const float GoH = 52f;
        const float GoY = 174f;
        // home_go.png 是 628×160，金面没有铺满。不按金面去铺，按钮会被上下空白压扁，底下空出一截。
        const float GoArtW = 628f;
        const float GoArtH = 160f;
        const float GoGoldX = 10f;
        const float GoGoldY = 4f;
        const float GoGoldW = 608f;
        const float GoGoldH = 128f;

        public static readonly Color Jade = InkTheme.Hex("2F8F6B");
        public static readonly Color JadeDeep = InkTheme.Hex("1E6149");
        public static readonly Color Grape = InkTheme.Hex("7B3FB8");
        public static readonly Color GrapeDeep = InkTheme.Hex("4E2380");
        public static readonly Color PlusGreen = InkTheme.Hex("8ACB29");

        // 烘预制时换成 AssetDatabase 里的图，存进预制才不会丢引用。
        public static Func<string, Sprite> SpriteOf;

        struct Tone
        {
            public string Name;
            public string Title;
            public string Head;
            public string Plate;
            public string Art;
            public Color Chip;
        }

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
            host.Daily = Card(page, CardTop, new Tone
            {
                Name = "daily",
                Title = "每日副本",
                Head = "获得每日宝箱",
                Plate = "Ui/mode_plate_green",
                Art = "Ui/dungeon_daily",
                Chip = InkTheme.Hex("A4D24F"),
            });
            host.Endless = Card(page, CardTop + CardH + CardGap, new Tone
            {
                Name = "endless",
                Title = "无尽副本",
                Head = "获得冲波宝箱",
                Plate = "Ui/mode_plate_purple",
                Art = "Ui/dungeon_endless",
                Chip = InkTheme.Hex("C6AEF8"),
            });
            var mark = new GameObject(Mark, typeof(RectTransform));
            mark.transform.SetParent(page, false);
            return host;
        }

        // 次数条和加号成一组，相对进入键左右居中。没有加号时次数条自己居中。
        public static void PlacePlays(DungeonCard card, bool plus)
        {
            if (card == null || card.PlaysBar == null) return;
            float barW = plus ? BarW : 188f;
            float group = barW + (plus ? Plus : 0f);
            float x = CenterX - group * 0.5f;
            var bar = card.PlaysBar.rectTransform;
            bar.anchoredPosition = new Vector2(x, -BarY);
            bar.sizeDelta = new Vector2(barW, BarH);
            if (card.Plays != null) card.Plays.rectTransform.sizeDelta = new Vector2(barW, BarH);
            if (card.Plus == null) return;
            card.Plus.gameObject.SetActive(plus);
            if (!plus) return;
            var pr = (RectTransform)card.Plus.transform;
            pr.anchoredPosition = new Vector2(x + barW, -(BarY + 1f));
        }

        static DungeonCard Card(RectTransform page, float top, Tone tone)
        {
            var frame = Pic(page, tone.Name, tone.Plate, new Vector2(0f, top), new Vector2(CardW, CardH), Pin.Top);
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

            card.Art = Pic(maskRt, "art", tone.Art, new Vector2(ArtX, 0f), new Vector2(ArtW, CardH), Pin.TopLeft);
            card.Art.preserveAspect = false;
            card.Art.raycastTarget = true;
            card.ArtButton = card.Art.gameObject.AddComponent<Button>();
            card.ArtButton.targetGraphic = card.Art;
            card.ArtButton.transition = Selectable.Transition.None;

            card.Lock = Pic(rt, "lock", "icon_lock", new Vector2(130f, 88f), new Vector2(72f, 72f), Pin.TopLeft);
            card.Lock.raycastTarget = false;
            card.Lock.gameObject.SetActive(false);

            var head = Label(rt, "head", tone.Head, 22, new Vector2(308f, 16f), new Vector2(190f, 32f), Pin.TopLeft, TextAnchor.MiddleLeft);
            head.color = InkTheme.Hex("291724");
            UiKit.Bold(head);
            var stroke = head.gameObject.AddComponent<Outline>();
            stroke.effectColor = Color.white;
            stroke.effectDistance = new Vector2(1f, -1f);

            card.Chip = Round(rt, "chip", new Vector2(504f, 14f), new Vector2(116f, 28f), tone.Chip, 8f);
            card.ChipLabel = Label(card.Chip.rectTransform, "t", "", 14, Vector2.zero, new Vector2(116f, 28f));
            card.ChipLabel.color = Color.white;

            string[] frames = { "Ui/mode_slot_blue", "Ui/mode_slot_green", "Ui/mode_slot_orange" };
            string[] chests = { "Ui/chest_wood", "Ui/chest_silver", "Ui/chest_gold" };
            float row = Slot * 3f + SlotGap * 2f;
            float x0 = CenterX - row * 0.5f;
            for (int i = 0; i < frames.Length; i++)
            {
                float x = x0 + i * (Slot + SlotGap);
                var slot = Pic(rt, "slot" + i, frames[i], new Vector2(x, SlotY), new Vector2(Slot, Slot), Pin.TopLeft);
                slot.raycastTarget = false;
                var chest = Pic(rt, "chest" + i, chests[i], new Vector2(x + (Slot - Chest) * 0.5f, SlotY + (Slot - Chest) * 0.5f),
                    new Vector2(Chest, Chest), Pin.TopLeft);
                chest.raycastTarget = false;
            }

            card.PlaysBar = Round(rt, "plays", new Vector2(0f, BarY), new Vector2(BarW, BarH), tone.Chip, 8f);
            card.Plays = Label(card.PlaysBar.rectTransform, "t", "", 15, Vector2.zero, new Vector2(BarW, BarH));
            card.Plays.color = Color.white;
            card.Plus = PlusBtn(rt);
            PlacePlays(card, true);

            card.Title = Label(rt, "title", tone.Title, 32, new Vector2(18f, 184f), new Vector2(220f, 48f), Pin.TopLeft, TextAnchor.MiddleLeft);
            card.Title.color = Color.white;
            UiKit.Bold(card.Title);
            var edge = card.Title.gameObject.AddComponent<Outline>();
            edge.effectColor = InkTheme.Ink;
            edge.effectDistance = new Vector2(1.5f, -1.5f);

            float goW = GoW * GoArtW / GoGoldW;
            float goH = GoH * GoArtH / GoGoldH;
            float goX = CenterX - GoW * 0.5f - GoGoldX / GoArtW * goW;
            float goY = GoY - GoGoldY / GoArtH * goH;
            var go = Pic(rt, "go", "Ui/home_go", new Vector2(goX, goY), new Vector2(goW, goH), Pin.TopLeft);
            go.type = Image.Type.Simple;
            go.preserveAspect = false;
            card.Go = go.gameObject.AddComponent<Button>();
            card.Go.targetGraphic = go;
            card.Go.transition = Selectable.Transition.None;
            float labelY = goH * (0.5f - (GoGoldY + GoGoldH * 0.5f) / GoArtH);
            card.GoLabel = Label(go.rectTransform, "t", "进入", 18, new Vector2(0f, labelY), new Vector2(GoW, GoH));
            card.GoLabel.color = InkTheme.Ink;
            UiKit.Bold(card.GoLabel);

            float rightPad = goW * (GoArtW - GoGoldX - GoGoldW) / GoArtW;
            float topPad = goH * GoGoldY / GoArtH;
            card.Dot = Pic(go.rectTransform, "dot", "Ui/home_dot", new Vector2(4f + rightPad, -(2f + topPad)), new Vector2(14f, 14f), Pin.TopRight);
            card.Dot.raycastTarget = false;
            card.Dot.gameObject.SetActive(false);
            return card;
        }

        static Image Round(Transform parent, string name, Vector2 pos, Vector2 size, Color color, float radius)
        {
            var rt = UiKit.Panel(parent, name, pos, size, color, Pin.TopLeft);
            var img = rt.GetComponent<Image>();
            img.sprite = UiSprites.FillExact(Mathf.RoundToInt(radius));
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        static Button PlusBtn(Transform parent)
        {
            var rt = UiKit.Panel(parent, "plus", Vector2.zero, new Vector2(Plus, Plus), PlusGreen, Pin.TopLeft);
            var img = rt.GetComponent<Image>();
            img.sprite = UiSprites.FillExact(6);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            var lineGo = new GameObject("line", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(rt, false);
            var lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.anchorMin = Vector2.zero;
            lineRt.anchorMax = Vector2.one;
            lineRt.offsetMin = Vector2.zero;
            lineRt.offsetMax = Vector2.zero;
            var line = lineGo.GetComponent<Image>();
            line.sprite = UiSprites.LineExact(6, 2);
            line.type = Image.Type.Sliced;
            line.color = InkTheme.Hex("0D050A");
            line.raycastTarget = false;
            var mark = Label(rt, "t", "+", 18, Vector2.zero, new Vector2(Plus, Plus));
            mark.color = Color.white;
            UiKit.Bold(mark);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            return btn;
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
