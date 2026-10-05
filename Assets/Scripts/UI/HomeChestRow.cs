using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 出征页「继续」下面那排四个宝箱托盘。箱子坐在托盘里，状态写在压着托盘下沿的小签上：
    // 排队写要等多久，在解的签子从左往右灌绿，开好了托盘换金边、箱子掀盖一涨一缩。
    // 每帧只改倒计时，状态变了才换样子。
    public sealed class HomeChestRow
    {
        public const float Slot = 116f;
        const float Gap = 14f;
        const float TagW = 96f;
        const float TagH = 30f;

        static readonly Color TagBack = InkTheme.Hex("4A3A30");
        static readonly Color TagCream = InkTheme.Hex("FFF3DC");

        sealed class Cell
        {
            public Image Tray;
            public Image Glow;
            public Image Ghost;
            public Image Art;
            public RectTransform Tag;
            public Image TagFill;
            public Text Label;
            public ChestState Shown = (ChestState)(-1);
            public int Tier = -1;
            public int Sec = -1;
        }

        readonly Cell[] _cells = new Cell[ChestCatalog.Slots];
        public RectTransform Root { get; private set; }

        public static float Width => ChestCatalog.Slots * Slot + (ChestCatalog.Slots - 1) * Gap;

        public static HomeChestRow Build(RectTransform page, Vector2 pos, Action<int> tap)
        {
            var row = new HomeChestRow();
            float w = Width;
            row.Root = ResultKit.Group(page, "chests", pos, new Vector2(w, Slot));
            row.Root.anchorMin = row.Root.anchorMax = new Vector2(0.5f, 0f);
            row.Root.anchoredPosition = pos;
            for (int i = 0; i < ChestCatalog.Slots; i++)
            {
                float x = -w * 0.5f + Slot * 0.5f + i * (Slot + Gap);
                var box = ResultKit.Group(row.Root, "c" + i, new Vector2(x, 0f), new Vector2(Slot, Slot));
                var hit = box.GetComponent<Image>();
                hit.raycastTarget = true;
                var btn = box.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = hit;
                int idx = i;
                btn.onClick.AddListener(() => tap(idx));

                var cell = new Cell
                {
                    Glow = UiKit.Icon(box, InkFx.SoftDisc(), new Vector2(0f, 6f), Slot * 1.5f),
                    Tray = UiKit.Icon(box, InkSprites.Load("Ui/chest_slot"), Vector2.zero, Slot),
                    Ghost = UiKit.Icon(box, InkSprites.Load("Ui/chest_wood"), new Vector2(0f, 6f), Slot * 0.5f),
                    Art = UiKit.Icon(box, null, new Vector2(0f, 8f), Slot * 0.74f)
                };
                cell.Glow.color = new Color(1f, 0.82f, 0.3f, 0.75f);
                cell.Ghost.color = new Color(0.36f, 0.25f, 0.16f, 0.16f);

                cell.Tag = UiKit.Panel(box, "tag", new Vector2(0f, -Slot * 0.5f + 8f), new Vector2(TagW, TagH), TagBack);
                var tagImg = cell.Tag.GetComponent<Image>();
                tagImg.sprite = UiSprites.Fill(UiSprites.Tier(TagH * 0.5f));
                tagImg.type = Image.Type.Sliced;
                tagImg.raycastTarget = false;
                var fill = UiKit.Panel(cell.Tag, "fill", Vector2.zero, Vector2.zero, InkTheme.Accel);
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(0f, 1f);
                fill.pivot = new Vector2(0f, 0.5f);
                fill.offsetMin = new Vector2(3f, 3f);
                fill.offsetMax = new Vector2(0f, -3f);
                cell.TagFill = fill.GetComponent<Image>();
                cell.TagFill.sprite = tagImg.sprite;
                cell.TagFill.type = Image.Type.Sliced;
                cell.TagFill.raycastTarget = false;
                cell.Label = UiKit.Label(cell.Tag, "t", "", 19, new Vector2(0f, 1f), new Vector2(TagW, TagH));
                UiKit.Bold(cell.Label);
                row._cells[i] = cell;
            }
            return row;
        }

        public void Refresh(MetaProgress meta)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                Cell c = _cells[i];
                ChestState st = meta.ChestStateOf(i);
                if (st == ChestState.Ready)
                {
                    float s = Mathf.Sin(Time.unscaledTime * 1.3f * Mathf.PI * 2f + i);
                    float k = 1f + 0.06f * s;
                    c.Art.transform.localScale = new Vector3(k, k, 1f);
                    c.Glow.color = new Color(1f, 0.82f, 0.3f, 0.55f + 0.25f * s);
                }
                int tier = st == ChestState.Empty ? -1 : (int)meta.ChestTierOf(i);
                int sec = st == ChestState.Timing ? meta.ChestSecondsLeft(i) : -1;
                if (st == c.Shown && tier == c.Tier && sec == c.Sec) continue;
                bool restyle = st != c.Shown || tier != c.Tier;
                if (st == ChestState.Ready && c.Shown == ChestState.Timing) AudioBus.ChestUnlock();
                c.Shown = st;
                c.Tier = tier;
                c.Sec = sec;
                Paint(c, st, restyle);
            }
            MaybeSlide(meta);
        }

        // 打开之后，后面的箱子从原来的格子滑到新格子。托盘不动，箱子和下面的签一起滑。
        void MaybeSlide(MetaProgress meta)
        {
            if (meta.DeferChestSlide || meta.ChestSlideFrom == null) return;
            int[] from = meta.ChestSlideFrom;
            meta.ChestSlideFrom = null;
            var anim = UiAnim.On(Root);
            float step = Slot + Gap;
            for (int i = 0; i < _cells.Length; i++)
            {
                int src = i < from.Length ? from[i] : -1;
                if (src < 0 || src == i) continue;
                float dx = (src - i) * step;
                Nudge(anim, _cells[i].Art.rectTransform, dx);
                Nudge(anim, _cells[i].Tag, dx);
                if (_cells[i].Glow.gameObject.activeSelf) Nudge(anim, _cells[i].Glow.rectTransform, dx);
            }
        }

        void Nudge(UiAnim anim, RectTransform piece, float dx)
        {
            if (piece == null) return;
            Vector2 home = piece.anchoredPosition;
            Transform parent = piece.parent;
            piece.SetParent(Root, true);
            piece.SetAsLastSibling();
            Vector2 end = piece.anchoredPosition;
            Vector2 start = end + new Vector2(dx, 0f);
            anim.Move(piece, start, end, 0.02f, 0.28f, Ease.OutCubic);
            anim.At(0.32f, () =>
            {
                if (piece == null || parent == null) return;
                piece.SetParent(parent, false);
                piece.anchoredPosition = home;
            });
        }

        static void Paint(Cell c, ChestState st, bool restyle)
        {
            bool empty = st == ChestState.Empty;
            bool ready = st == ChestState.Ready;
            c.Ghost.gameObject.SetActive(empty);
            c.Art.gameObject.SetActive(!empty);
            c.Tag.gameObject.SetActive(!empty);
            c.Glow.gameObject.SetActive(ready);
            if (restyle)
            {
                c.Tray.sprite = InkSprites.Load(ready ? "Ui/chest_slot_ready" : "Ui/chest_slot");
                c.Art.transform.localScale = Vector3.one;
            }
            if (empty) return;

            ChestDef d = ChestCatalog.Get(c.Tier);
            if (restyle) c.Art.sprite = InkSprites.Load("Ui/chest_" + d.Key + (ready ? "_open" : ""));
            var fill = c.TagFill.rectTransform;
            switch (st)
            {
                case ChestState.Ready:
                    c.Tag.GetComponent<Image>().color = InkTheme.CtaDeep;
                    c.TagFill.color = InkTheme.Cta;
                    fill.anchorMax = new Vector2(1f, 1f);
                    fill.offsetMax = new Vector2(-3f, -3f);
                    c.Label.text = "打开";
                    c.Label.color = InkTheme.CardFace;
                    break;
                case ChestState.Timing:
                    c.Tag.GetComponent<Image>().color = TagBack;
                    c.TagFill.color = InkTheme.Accel;
                    float k = Mathf.Clamp01(1f - c.Sec / (float)Mathf.Max(1, d.Seconds));
                    fill.anchorMax = new Vector2(k, 1f);
                    fill.offsetMax = new Vector2(k >= 0.99f ? -3f : 0f, -3f);
                    c.Label.text = ChestCatalog.Clock(c.Sec);
                    c.Label.color = TagCream;
                    break;
                default:
                    c.Tag.GetComponent<Image>().color = TagBack;
                    fill.anchorMax = new Vector2(0f, 1f);
                    c.Label.text = ChestCatalog.Span(d.Seconds);
                    c.Label.color = TagCream;
                    break;
            }
        }
    }
}
