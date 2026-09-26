using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 出征页的章节卡。预制里没有这张卡时，运行时补在 page_sortie 上；
    // 烘「重排出征页」时走同一份，避免两套坐标。
    public static class SortiePageBuilder
    {
        public const int PerChapter = 9;
        public const int Cols = 3;

        static readonly string[] CnDigit =
        {
            "一", "二", "三", "四", "五", "六", "七", "八", "九", "十"
        };

        public static int ChapterCount =>
            (GameConstants.ChapterStageCount + PerChapter - 1) / PerChapter;

        public static int ChapterOf(int stage) => Mathf.Clamp(stage / PerChapter, 0, ChapterCount - 1);

        // 烘预制时改成 AssetDatabase 里的图，存进预制才不会丢引用。
        public static Func<string, Sprite> SpriteOf;

        static Sprite Spr(string file)
        {
            if (SpriteOf != null)
            {
                Sprite got = SpriteOf(file);
                if (got != null) return got;
            }
            return InkSprites.Load("Ui/" + file);
        }

        static Sprite Ico(string key) => Spr("ico_" + key);

        static void Use(Image img, string file)
        {
            if (img == null) return;
            Sprite s = Spr(file);
            if (s != null) img.sprite = s;
        }

        public static string ChapterTitle(int chapter)
        {
            int n = chapter + 1;
            if (n >= 1 && n <= CnDigit.Length) return "第" + CnDigit[n - 1] + "章";
            return "第" + n + "章";
        }

        public static void Ensure(HomeView view, RectTransform page)
        {
            if (view == null || page == null || view.Chapter != null) return;
            if (view.Logo != null) view.Logo.gameObject.SetActive(false);
            if (view.Seals != null && view.Seals.Length > 0 && view.Seals[0] != null)
            {
                Transform box = view.Seals[0].transform.parent;
                if (box != null && box.name == "seals") box.gameObject.SetActive(false);
            }
            Build(page, view);
        }

        // 卡片比合稿那张窄条宽一截、矮一截。底图是直角底边，尺寸跟这张卡对齐。
        const float CardTop = 80f;
        const float CardW = 464f;
        const float CardH = 640f;
        const float NodeSize = 68f;
        const float PitchX = 136f;
        const float PitchY = 104f;

        public static void Build(RectTransform page, HomeView view)
        {
            float cardW = CardW;

            var boardRt = UiKit.Art(page, "chapter", "Ui/panel_chapter",
                new Vector2(0f, CardTop), new Vector2(cardW, CardH), Pin.Top);
            var boardImg = boardRt.GetComponent<Image>();
            Use(boardImg, "panel_chapter");
            boardImg.preserveAspect = true;
            boardImg.raycastTarget = true;
            var swipe = boardRt.gameObject.AddComponent<ChapterSwipe>();
            var chapter = boardRt.gameObject.AddComponent<HomeChapterBoard>();
            chapter.Board = boardImg;

            Sprite artSpr = Spr("chapter_1");
            float artW = cardW - 28f;
            float artH = artW * 682f / 1498f;
            if (artSpr != null && artSpr.rect.width > 1f)
                artH = artW * artSpr.rect.height / artSpr.rect.width;
            const float artTop = 16f;
            var art = UiKit.Art(boardRt, "art", "Ui/chapter_1",
                new Vector2(0f, artTop), new Vector2(artW, artH), Pin.Top);
            chapter.Art = art.GetComponent<Image>();
            Use(chapter.Art, "chapter_1");
            chapter.Art.preserveAspect = true;
            chapter.Art.raycastTarget = false;

            Sprite ribSpr = Spr("ribbon_chapter");
            float ribW = Mathf.Min(252f, cardW * 0.64f);
            float ribH = ribSpr != null && ribSpr.rect.width > 1f
                ? ribW * ribSpr.rect.height / ribSpr.rect.width
                : 64f;
            float ribTop = artTop + artH + 4f;
            var ribbon = UiKit.Art(boardRt, "ribbon", "Ui/ribbon_chapter",
                new Vector2(0f, ribTop), new Vector2(ribW, ribH), Pin.Top);
            chapter.Ribbon = ribbon.GetComponent<Image>();
            Use(chapter.Ribbon, "ribbon_chapter");
            chapter.Ribbon.preserveAspect = true;
            chapter.Ribbon.raycastTarget = false;
            chapter.Title = UiKit.Label(ribbon, "t", "第一章", 28, Vector2.zero, new Vector2(200f, 44f));
            chapter.Title.color = InkTheme.CardFace;
            UiKit.Bold(chapter.Title);

            float row0 = ribTop + ribH + 32f + NodeSize * 0.5f;
            float routeCenter = row0 + PitchY;
            var routeGo = new GameObject("route", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiPath));
            routeGo.transform.SetParent(boardRt, false);
            var routeRt = routeGo.GetComponent<RectTransform>();
            routeRt.anchorMin = routeRt.anchorMax = new Vector2(0.5f, 1f);
            routeRt.pivot = new Vector2(0.5f, 0.5f);
            routeRt.sizeDelta = new Vector2(cardW - 24f, PitchY * 2f + NodeSize + 12f);
            routeRt.anchoredPosition = new Vector2(0f, -routeCenter);
            var path = routeGo.GetComponent<UiPath>();
            path.raycastTarget = false;
            path.Thickness = 13f;
            path.color = InkTheme.Outline;
            chapter.Route = path;

            var nodes = new HomeSealCell[PerChapter];
            for (int i = 0; i < PerChapter; i++)
            {
                Vector2 lp = NodeLocal(i);
                var plate = new GameObject("nd" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                plate.transform.SetParent(routeRt, false);
                var pr = plate.GetComponent<RectTransform>();
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.pivot = new Vector2(0.5f, 0.5f);
                pr.anchoredPosition = lp;
                pr.sizeDelta = new Vector2(NodeSize, NodeSize);
                var img = plate.GetComponent<Image>();
                img.sprite = Ico("node_lock");
                img.preserveAspect = true;
                var btn = plate.GetComponent<Button>();
                btn.targetGraphic = img;
                btn.transition = Selectable.Transition.None;
                var num = UiKit.Label(plate.transform, "n", "", 28, Vector2.zero, new Vector2(56f, 42f));
                UiKit.Bold(num);
                num.color = InkTheme.CardFace;
                var slot = plate.AddComponent<HomeSealCell>();
                slot.Plate = img;
                slot.Button = btn;
                slot.Number = num;
                nodes[i] = slot;
            }
            chapter.Nodes = nodes;

            int chapters = ChapterCount;
            var dots = new Image[chapters];
            float span = (chapters - 1) * 22f;
            for (int i = 0; i < chapters; i++)
            {
                var dot = new GameObject("dot" + i, typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(boardRt, false);
                var dr = dot.GetComponent<RectTransform>();
                dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0f);
                dr.pivot = new Vector2(0.5f, 0f);
                dr.anchoredPosition = new Vector2(-span * 0.5f + i * 22f, 22f);
                dr.sizeDelta = new Vector2(12f, 12f);
                var di = dot.GetComponent<Image>();
                di.sprite = UiSprites.Fill(8);
                di.type = Image.Type.Sliced;
                di.raycastTarget = false;
                dots[i] = di;
            }
            chapter.Dots = dots;

            chapter.Prev = MakeTab(boardRt, "prev", "上一章", false, cardW, out chapter.PrevLabel);
            chapter.Next = MakeTab(boardRt, "next", "下一章", true, cardW, out chapter.NextLabel);

            view.Chapter = chapter;
            view.SideActs = BuildSides(page);
            var mark = new GameObject("sortie_v6", typeof(RectTransform));
            mark.transform.SetParent(page, false);
            swipe.Moved = null;
        }

        // 合稿右下角那张小票：奶油底、一圈细描边，挂在卡片下角。
        static Button MakeTab(RectTransform board, string name, string text, bool right, float cardW, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(board, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(right ? 1f : 0f, 0f);
            rt.anchoredPosition = new Vector2(right ? cardW * 0.5f - 14f : -cardW * 0.5f + 14f, -4f);
            rt.sizeDelta = new Vector2(108f, 36f);
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.Fill(8);
            img.type = Image.Type.Sliced;
            img.color = InkTheme.Outline;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            var face = new GameObject("face", typeof(RectTransform), typeof(Image));
            face.transform.SetParent(go.transform, false);
            var fr = face.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(3f, 3f);
            fr.offsetMax = new Vector2(-3f, -3f);
            var fi = face.GetComponent<Image>();
            fi.sprite = UiSprites.Fill(8);
            fi.type = Image.Type.Sliced;
            fi.color = new Color(1f, 0.96f, 0.90f, 1f);
            fi.raycastTarget = false;

            label = UiKit.Label(face.transform, "t", text, 20, Vector2.zero, new Vector2(96f, 28f));
            label.color = InkTheme.TextDark;
            return btn;
        }

        static Button[] BuildSides(RectTransform page)
        {
            // 合稿上的贴纸尺寸，含一圈白边。锚在页面左右边，外沿贴屏幕。
            string[] labels = { "游戏圈", "签到", "活动", "排行榜" };
            string[] icons = { "act_circle", "act_checkin", "act_event", "act_rank" };
            Vector2[] iconSize =
            {
                new Vector2(112f, 94f),
                new Vector2(96f, 100f),
                new Vector2(96f, 96f),
                new Vector2(112f, 102f),
            };
            float y1 = CardTop + CardH * 0.40f;
            float y2 = CardTop + CardH * 0.68f;
            float[] ys = { y1, y2, y1, y2 };
            var buttons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                bool left = i < 2;
                float iw = iconSize[i].x;
                float ih = iconSize[i].y;
                var go = new GameObject("act" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(page, false);
                var rt = go.GetComponent<RectTransform>();
                float edge = left ? 0f : 1f;
                rt.anchorMin = rt.anchorMax = new Vector2(edge, 1f);
                rt.pivot = new Vector2(edge, 0.5f);
                rt.sizeDelta = new Vector2(iw, ih + 72f);
                rt.anchoredPosition = new Vector2(0f, -ys[i]);
                var hit = go.GetComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                var btn = go.GetComponent<Button>();
                btn.targetGraphic = hit;
                btn.transition = Selectable.Transition.None;
                var icon = UiKit.Icon(go.transform, Ico(icons[i]), Vector2.zero, Mathf.Max(iw, ih));
                icon.rectTransform.sizeDelta = new Vector2(iw, ih);
                var cap = UiKit.Label(go.transform, "t", labels[i], 22,
                    new Vector2(0f, -(ih * 0.5f + 22f)), new Vector2(iw, 32f));
                cap.color = InkTheme.TextDark;
                buttons[i] = btn;
            }
            return buttons;
        }

        public static Vector2 NodeLocal(int index)
        {
            int row = index / Cols;
            int col = index % Cols;
            return new Vector2((col - 1) * PitchX, (1 - row) * PitchY);
        }

        public static List<Vector2> RoutePoints(int count)
        {
            var pts = new List<Vector2>();
            count = Mathf.Clamp(count, 0, PerChapter);
            int rows = (count + Cols - 1) / Cols;
            for (int row = 0; row < rows; row++)
            {
                int n = Mathf.Min(Cols, count - row * Cols);
                for (int c = 0; c < n; c++)
                {
                    Vector2 p = NodeLocal(row * Cols + c);
                    if (c == 0 && pts.Count > 0) AppendS(pts, pts[pts.Count - 1], p);
                    else pts.Add(p);
                }
            }
            return pts;
        }

        // 从上一行右端绕出去，扫到下一行左端再接上。弯的形状对着参考里那条 S。
        static void AppendS(List<Vector2> pts, Vector2 a, Vector2 b)
        {
            float midY = (a.y + b.y) * 0.5f;
            Vector2 right = new Vector2(a.x + 38f, midY + 4f);
            Vector2 left = new Vector2(b.x - 38f, midY - 4f);
            Cubic(pts, a, a + new Vector2(36f, 2f), right + new Vector2(6f, 18f), right);
            Cubic(pts, right, right + new Vector2(-24f, -2f), left + new Vector2(24f, 2f), left);
            Cubic(pts, left, left + new Vector2(6f, -14f), b + new Vector2(-30f, 2f), b);
        }

        static void Cubic(List<Vector2> pts, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            const int N = 12;
            for (int i = 1; i <= N; i++)
            {
                float t = i / (float)N;
                float u = 1f - t;
                pts.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
            }
        }
    }
}
