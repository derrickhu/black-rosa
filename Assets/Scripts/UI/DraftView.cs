using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class DraftView : MonoBehaviour
    {
        public Text Title;
        public DraftCardView[] Cards;
        public Button Reroll;
        public Text RerollLabel;
        public Image Die;

        void Awake()
        {
            UiKit.ApplyTo(transform);
            RescueCards();
        }

        public void Bind(string title, CardId[] offer, bool rerolled, Action<CardId> pick, Action reroll)
        {
            RescueCards();
            if (Title != null)
                Title.text = string.IsNullOrEmpty(title) ? "选一张改装" : title;

            int n = Cards != null ? Cards.Length : 0;
            int offerN = offer != null ? offer.Length : 0;
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
                if (card.Star != null)
                {
                    card.Star.sprite = InkArt.Icon(InkShape.Star);
                    card.Star.enabled = card.Star.sprite != null;
                }
                if (card.Heap != null)
                {
                    card.Heap.sprite = InkArt.Heap(def.Id, 160);
                    card.Heap.preserveAspect = true;
                    card.Heap.enabled = card.Heap.sprite != null;
                }
                if (card.Name != null) card.Name.text = def.Name;
                if (card.Desc != null) card.Desc.text = def.Desc;
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
                RerollLabel.text = rerolled ? "已重刷" : "看广告重刷";
            if (Die != null)
            {
                Die.sprite = InkSprites.Die();
                Die.enabled = !rerolled && Die.sprite != null;
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
            var dim = UiKit.Dimmer(parent);
            dim.GetComponent<Image>().color = new Color(0.10f, 0.09f, 0.08f, 0.58f);
            dim.gameObject.name = "DraftPanel";
            var view = dim.gameObject.AddComponent<DraftView>();
            var board = UiKit.Stroke(dim, "board", new Vector2(0, -24), new Vector2(660, 620), Pin.Center, 8f);
            view.Title = UiKit.Label(board, "title", "选一张改装", 26, new Vector2(0, 256), new Vector2(600, 44));
            view.Cards = new DraftCardView[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 200f;
                var card = UiKit.Stroke(board, "card" + i, new Vector2(x, 28), new Vector2(184, 360), Pin.Center, 5f);
                var slot = card.gameObject.AddComponent<DraftCardView>();
                slot.Button = card.gameObject.AddComponent<Button>();
                slot.Button.targetGraphic = card.GetComponent<Image>();
                slot.Star = UiKit.Icon(card, InkArt.Icon(InkShape.Star), new Vector2(0, 140), 36f);
                slot.Star.gameObject.name = "star";
                slot.Heap = UiKit.Icon(card, InkArt.Heap(CardId.Fire, 160), new Vector2(0, 28), 128f);
                slot.Heap.gameObject.name = "heap";
                slot.Name = UiKit.Label(card, "name", "加火", 30, new Vector2(0, -92), new Vector2(168, 40));
                slot.Desc = UiKit.Label(card, "desc", "子弹变橙并灼烧", 18, new Vector2(0, -138), new Vector2(160, 56));
                view.Cards[i] = slot;
            }
            view.Reroll = UiKit.Btn(board, "reroll", "看广告重刷", new Vector2(0, -248), new Vector2(520, 64), () => { }, false);
            view.RerollLabel = view.Reroll.GetComponentInChildren<Text>();
            view.Die = UiKit.Icon(view.Reroll.transform, InkSprites.Die(), new Vector2(-190f, 0f), 36f);
            if (view.Die != null) view.Die.gameObject.name = "die";
            return view;
        }
    }
}
