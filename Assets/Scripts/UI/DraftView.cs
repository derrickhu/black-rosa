using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 选改装是一只从底部拉起来的抽屉，上沿压在格子下沿之下：
    // 上面在走的怪、格子里已有的字都还看得见，玩家才能对着局面挑。
    // 卡面只放字图。字名就画在图上，说明玩家已经在图鉴和放置后的效果里见过；成词字补一枚小签。
    public sealed class DraftView : MonoBehaviour
    {
        public Text Title;
        public DraftCardView[] Cards;
        public Button Reroll;
        public Text RerollLabel;
        public Image Die;
        public Button Close;
        public float CardStep = 176f;

        static readonly Vector4 CardSlice = new Vector4(18f, 18f, 18f, 18f);

        void Awake()
        {
            UiKit.ApplyTo(transform);
            RescueCards();
        }

        public void Bind(string title, CardId[] offer, bool rerolled, Action<CardId> pick, Action reroll, Action close)
        {
            RescueCards();
            if (Title != null)
                Title.text = string.IsNullOrEmpty(title) ? "选一张改装" : title;

            int n = Cards != null ? Cards.Length : 0;
            int offerN = offer != null ? offer.Length : 0;

            // 牌池不足三个字的关卡（第一关只有「分」）只发一两张，重新居中。
            for (int i = 0; i < n && i < offerN; i++)
            {
                var rt = Cards[i] != null ? Cards[i].GetComponent<RectTransform>() : null;
                if (rt == null) continue;
                Vector2 p = rt.anchoredPosition;
                p.x = (i - (offerN - 1) * 0.5f) * CardStep;
                rt.anchoredPosition = p;
            }

            for (int i = 0; i < n; i++)
            {
                DraftCardView card = Cards[i];
                if (card == null) continue;
                if (i >= offerN)
                {
                    card.gameObject.SetActive(false);
                    continue;
                }
                card.gameObject.SetActive(true);
                CardDef def = CardCatalog.Get(offer[i]);
                if (card.Star != null) card.Star.enabled = false;
                if (card.Name != null) card.Name.text = "";
                if (card.Desc != null) card.Desc.text = "";
                if (card.Heap != null)
                {
                    card.Heap.sprite = InkArt.Glyph(def.Id, 160);
                    card.Heap.preserveAspect = true;
                    card.Heap.enabled = card.Heap.sprite != null;
                }
                if (card.WordTag != null) card.WordTag.SetActive(def.Wake == CardWake.WordPart);
                if (card.Button != null)
                {
                    CardId id = def.Id;
                    card.Button.onClick.RemoveAllListeners();
                    card.Button.onClick.AddListener(() => pick(id));
                }
            }

            if (Reroll != null)
            {
                Reroll.interactable = !rerolled;
                Reroll.onClick.RemoveAllListeners();
                if (!rerolled && reroll != null)
                    Reroll.onClick.AddListener(() => reroll());
            }
            if (RerollLabel != null)
                RerollLabel.text = rerolled ? "已重刷" : "重刷";
            if (Die != null) Die.enabled = !rerolled && Die.sprite != null;
            if (Close != null)
            {
                Close.onClick.RemoveAllListeners();
                if (close != null) Close.onClick.AddListener(() => close());
            }
        }

        void RescueCards()
        {
            if (Cards != null && Cards.Length > 0)
            {
                for (int i = 0; i < Cards.Length; i++)
                    if (Cards[i] == null) { Cards = null; break; }
            }
            if (Cards != null && Cards.Length > 0) return;
            Cards = GetComponentsInChildren<DraftCardView>(true);
        }

        public static DraftView BuildTemplate(Transform parent)
        {
            // 只压一层很淡的暗，挡住点击、但不挡视线。
            var dim = UiKit.Dimmer(parent);
            dim.GetComponent<Image>().color = new Color(0.16f, 0.11f, 0.08f, 0.16f);
            dim.gameObject.name = "DraftPanel";
            var view = dim.gameObject.AddComponent<DraftView>();

            var layer = parent as RectTransform;
            float canvasW = layer != null && layer.rect.width > 1f ? layer.rect.width : 750f;
            float canvasH = layer != null && layer.rect.height > 1f ? layer.rect.height : 1334f;
            // 抽屉上沿：格子下沿再往下一点。窄屏格子压得低，就退到能放下卡的最小高度。
            float gridFloor = BattleHud.WorldToCanvas(layer, new Vector3(0f, FieldLayout.GridBottom - 0.18f, 0f)).y + canvasH * 0.5f;
            float h = Mathf.Clamp(gridFloor, 228f, 300f);
            const float sink = 40f;   // 底边沉到屏幕外，只露圆角的上沿，像拉起来的抽屉

            var board = UiKit.Stroke(dim, "board", new Vector2(0f, -sink), new Vector2(canvasW + 24f, h + sink),
                Pin.Bottom, 6f, fill: InkTheme.Paper, radius: 30f);
            float top = (h + sink) * 0.5f;

            const float head = 58f;
            view.Title = UiKit.Label(board, "title", "选一张改装", 26, new Vector2(-canvasW * 0.5f + 150f, top - head * 0.5f - 4f),
                new Vector2(260f, 40f), TextAnchor.MiddleLeft);
            UiKit.Bold(view.Title);
            view.Close = CloseMark(board, new Vector2(canvasW * 0.5f - 52f, top - head * 0.5f - 4f));

            view.Reroll = UiKit.Btn(board, "reroll", "重刷", new Vector2(canvasW * 0.5f - 182f, top - head * 0.5f - 4f),
                new Vector2(150f, 50f), () => { }, false);
            view.RerollLabel = view.Reroll.GetComponentInChildren<Text>();
            if (view.RerollLabel != null)
            {
                view.RerollLabel.fontSize = 24;
                view.RerollLabel.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            }
            Transform face = view.Reroll.transform.Find("face");
            view.Die = UiKit.Icon(face != null ? face : view.Reroll.transform, ResultKit.AdBadge(), new Vector2(-46f, 0f), 30f);
            view.Die.gameObject.name = "ad";

            float cardH = Mathf.Clamp(h - head - 26f, 140f, 196f);
            float cardW = Mathf.Min(cardH * 0.95f, (canvasW - 80f) / 3f - 14f);
            view.CardStep = cardW + 22f;
            float cardY = top - head - 8f - cardH * 0.5f;
            view.Cards = new DraftCardView[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * view.CardStep;
                var card = UiKit.Art(board, "card" + i, "Ui/panel_card", new Vector2(x, cardY), new Vector2(cardW, cardH),
                    Pin.Center, CardSlice);
                var slot = card.gameObject.AddComponent<DraftCardView>();
                slot.Button = card.gameObject.AddComponent<Button>();
                slot.Button.targetGraphic = card.GetComponent<Image>();
                var cols = slot.Button.colors;
                cols.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
                slot.Button.colors = cols;
                slot.Heap = UiKit.Icon(card, InkArt.Glyph(CardId.Fire, 160), Vector2.zero, Mathf.Min(cardW, cardH) * 0.78f);
                slot.Heap.gameObject.name = "heap";
                slot.WordTag = WordTag(card, new Vector2(cardW * 0.5f - 30f, cardH * 0.5f - 18f)).gameObject;
                view.Cards[i] = slot;

                var anim = UiAnim.On(card);
                anim.Pop(card, 0.05f + i * 0.05f, 0.30f, 0.6f);
            }

            var boardAnim = UiAnim.On(board);
            Vector2 home = board.anchoredPosition;
            boardAnim.Move(board, home + new Vector2(0f, -h - 30f), home, 0f, 0.22f, Ease.OutCubic);
            return view;
        }

        // 成词字的小签：金底深边，压在卡的右上角。
        static RectTransform WordTag(Transform card, Vector2 pos)
        {
            var tag = UiKit.Stroke(card, "word", pos, new Vector2(56f, 28f), Pin.Center, 3f,
                fill: InkTheme.Word, radius: 14f);
            var t = UiKit.Label(tag, "t", "成词", 17, Vector2.zero, new Vector2(56f, 28f));
            t.color = InkTheme.TextDark;
            UiKit.Bold(t);
            Transform sh = card.Find("word_sh");
            if (sh != null) sh.gameObject.SetActive(false);
            return tag;
        }

        static Button CloseMark(Transform board, Vector2 pos)
        {
            var go = new GameObject("close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(board, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(48f, 48f);
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.Fill(24);
            img.color = InkTheme.Plain;
            var ringGo = new GameObject("ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(rt, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(48f, 48f);
            var ring = ringGo.GetComponent<Image>();
            ring.sprite = UiSprites.Line(18, 3);
            ring.color = InkTheme.Outline;
            ring.raycastTarget = false;
            var mark = UiKit.Label(rt, "t", "×", 30, Vector2.zero, new Vector2(48f, 48f));
            mark.color = InkTheme.TextDark;
            UiKit.Bold(mark);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            return btn;
        }
    }
}
