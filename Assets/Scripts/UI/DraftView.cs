using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 选改装是一只从底部拉起来的抽屉，上沿压在格子下沿之下：
    // 上面在走的怪、格子里已有的字都还看得见，玩家才能对着局面挑。
    // 卡面用字卡金底。米字格里只放字，底栏写「单字」或「成词」。
    public sealed class DraftView : MonoBehaviour
    {
        public Text Title;
        public DraftCardView[] Cards;
        public Button Reroll;
        public Text RerollLabel;
        public Image Die;
        public Button Close;
        public float CardStep = 144f;

        void Awake()
        {
            UiKit.ApplyTo(transform);
            RescueCards();
        }

        public void Bind(string title, CardId[] offer, bool[] shovel, bool rerolled, Action<int> pick, Action reroll, Action close)
        {
            RescueCards();
            if (Title != null)
            {
                // 单张流程会传来「点下面的字」。抽屉标题保持「选一张改装」，点哪张交给指引气泡。
                bool point = title == "点下面的字";
                Title.text = string.IsNullOrEmpty(title) || point ? "选一张改装" : title;
            }

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
                bool dig = shovel != null && i < shovel.Length && shovel[i];
                CardDef def = CardCatalog.Get(offer[i]);
                if (card.Star != null) card.Star.enabled = false;
                if (card.Desc != null) card.Desc.text = "";
                if (card.Heap != null)
                {
                    card.Heap.sprite = dig ? InkArt.Shovel(160) : InkArt.Bare(InkArt.Glyph(def.Id, 160));
                    card.Heap.preserveAspect = true;
                    card.Heap.color = Color.white;
                    card.Heap.enabled = card.Heap.sprite != null;
                }
                if (card.Name != null)
                    card.Name.text = dig ? "铲子" : def.Wake == CardWake.WordPart ? "成词" : "单字";
                if (card.WordTag != null) card.WordTag.SetActive(false);
                if (card.Button != null)
                {
                    int slot = i;
                    card.Button.onClick.RemoveAllListeners();
                    card.Button.onClick.AddListener(() => pick(slot));
                }
            }

            // 新手关不给关、不给重刷：传 null 就整颗藏掉。
            Show(Reroll, reroll != null);
            Show(Close, close != null);
            if (Reroll != null)
            {
                Reroll.interactable = !rerolled;
                Reroll.onClick.RemoveAllListeners();
                if (!rerolled && reroll != null)
                    Reroll.onClick.AddListener(() => reroll());
                ModalKit.SetAdBtnLabel(Reroll, "sm_cyan", rerolled ? "已重刷" : "重刷", !rerolled);
            }
            if (Close != null)
            {
                Close.onClick.RemoveAllListeners();
                if (close != null) Close.onClick.AddListener(() => close());
            }
        }

        static void Show(Button btn, bool on)
        {
            if (btn == null) return;
            btn.gameObject.SetActive(on);
            Transform sh = btn.transform.parent != null ? btn.transform.parent.Find(btn.name + "_sh") : null;
            if (sh != null) sh.gameObject.SetActive(on);
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
            // 抽屉上沿：格子下沿再往下一点。窄屏格子压得低，就退到能放下字卡的最小高度。
            float gridFloor = BattleHud.WorldToCanvas(layer, new Vector3(0f, FieldLayout.GridBottom - 0.18f, 0f)).y + canvasH * 0.5f;
            const float head = 64f;
            const float cardW = 128f;
            const float cardH = 172f;
            const float bottom = 18f;
            float content = head + cardH + bottom;
            float h = Mathf.Clamp(gridFloor, content, Mathf.Max(content, 320f));
            const float sink = 40f;

            var board = UiKit.Stroke(dim, "board", new Vector2(0f, -sink), new Vector2(canvasW + 24f, h + sink),
                Pin.Bottom, 3f, fill: InkTheme.Paper, radius: 28f);
            float top = (h + sink) * 0.5f;
            float boardW = canvasW + 24f;

            view.Title = UiKit.Label(board, "title", "选一张改装", 26, new Vector2(0f, top - head * 0.5f),
                new Vector2(Mathf.Max(180f, canvasW - 240f), 40f));
            view.Title.color = InkTheme.TextDark;
            UiKit.Bold(view.Title);

            view.Close = ModalKit.CloseBtn(board, 0f, 0f, () => { });
            var closeRt = (RectTransform)view.Close.transform;
            closeRt.anchoredPosition = new Vector2(-41f, -25f);
            view.Reroll = ModalKit.HexBtn(board, "sm_cyan", boardW - 132f - 58f, 8f, "重刷", null, out view.RerollLabel);
            ModalKit.SetAdBtnLabel(view.Reroll, "sm_cyan", "重刷", true);
            view.Die = view.Reroll.transform.Find("ad_ico")?.GetComponent<Image>();

            view.CardStep = cardW + 16f;
            float cardY = top - head - cardH * 0.5f;
            view.Cards = new DraftCardView[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * view.CardStep;
                var go = new GameObject("card" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(board, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, cardY);
                rt.sizeDelta = new Vector2(cardW, cardH);
                var img = go.GetComponent<Image>();
                img.sprite = InkSprites.Load("Ui/study_card_gold");
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = true;

                var slot = go.AddComponent<DraftCardView>();
                slot.Button = go.GetComponent<Button>();
                slot.Button.targetGraphic = img;
                var cols = slot.Button.colors;
                cols.pressedColor = new Color(0.92f, 0.86f, 0.7f, 1f);
                slot.Button.colors = cols;

                var heapGo = new GameObject("heap", typeof(RectTransform), typeof(Image));
                heapGo.transform.SetParent(go.transform, false);
                var hrt = heapGo.GetComponent<RectTransform>();
                hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 1f);
                hrt.pivot = new Vector2(0f, 1f);
                hrt.anchoredPosition = new Vector2(12f, -12f);
                hrt.sizeDelta = new Vector2(104f, 104f);
                slot.Heap = heapGo.GetComponent<Image>();
                slot.Heap.preserveAspect = true;
                slot.Heap.raycastTarget = false;

                slot.Name = UiKit.Label(go.transform, "cap", "单字", 18, Vector2.zero, new Vector2(cardW, 56f),
                    TextAnchor.MiddleCenter, Pin.TopLeft);
                HomeReskin.TopLeft(slot.Name.rectTransform, 0f, 116f, cardW, 56f);
                slot.Name.color = InkTheme.TextDark;
                UiKit.Bold(slot.Name);
                slot.Name.raycastTarget = false;

                view.Cards[i] = slot;
                UiAnim.On(rt).Pop(rt, 0.05f + i * 0.05f, 0.30f, 0.6f);
            }

            var boardAnim = UiAnim.On(board);
            Vector2 home = board.anchoredPosition;
            boardAnim.Move(board, home + new Vector2(0f, -h - 30f), home, 0f, 0.22f, Ease.OutCubic);
            return view;
        }
    }
}
