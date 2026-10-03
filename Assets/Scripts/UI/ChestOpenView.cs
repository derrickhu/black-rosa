using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 开箱演出：箱子砸下来抖两下、掀盖，墨先滚出来，卡一叠一叠翻开。
    // 战利品在 MetaProgress.OpenChest 里已经入账了，这里只负责演，点一下就跳到结尾。
    public sealed class ChestOpenView : MonoBehaviour
    {
        const float ChestY = 330f;
        const float ChestSize = 260f;
        const float CardW = 180f;
        const float CardH = 236f;
        const float StepX = 200f;
        const float StepY = 262f;
        const float CardsTop = 40f;

        MetaProgress _meta;
        ChestLoot _loot;
        Action _done;
        UiAnim _anim;
        RectTransform _root;
        bool _closing;

        public static void Show(RectTransform layer, MetaProgress meta, ChestLoot loot, Action done)
        {
            if (layer == null || loot == null)
            {
                done?.Invoke();
                return;
            }
            var dim = UiKit.Dimmer(layer);
            dim.name = "chest_open";
            dim.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.04f, 0.88f);
            var view = dim.gameObject.AddComponent<ChestOpenView>();
            view._meta = meta;
            view._loot = loot;
            view._done = done;
            view._root = dim;
            view._anim = UiAnim.On(view);
            view.Build();
        }

        void Build()
        {
            ResultKit.SkipCatcher(_root, () => { if (_anim.Playing) _anim.Finish(); });
            ChestDef d = ChestCatalog.Get(_loot.Tier);

            var rays = ResultKit.Rays(_root, new Vector2(0f, ChestY), 620f, new Color(1f, 0.84f, 0.45f, 0.7f));
            rays.transform.SetSiblingIndex(1);
            _anim.Spin(rays.transform, 18f).Fade(rays, 0.95f, 0.4f, 0f, 0.7f);

            var title = ResultKit.Headline(_root, "title", d.Name, 46, new Vector2(0f, ChestY + 200f),
                InkTheme.Hex("FFF3C8"), InkTheme.Hex("5A2A10"));
            _anim.Fade(title, 0.1f, 0.3f, 0f, 1f);

            var box = ResultKit.Group(_root, "box", new Vector2(0f, ChestY), new Vector2(ChestSize, ChestSize));
            var art = UiKit.Icon(box, InkSprites.Load("Ui/chest_" + d.Key), Vector2.zero, ChestSize);
            Sprite open = InkSprites.Load("Ui/chest_" + d.Key + "_open");
            _anim.Move(box, new Vector2(0f, ChestY + 700f), new Vector2(0f, ChestY), 0f, 0.5f, Ease.OutBounce)
                 .At(0.22f, AudioBus.ChestLand)
                 .Tween(0.55f, 0.22f, k => Jiggle(box, k, 10f))
                 .Tween(0.8f, 0.24f, k => Jiggle(box, k, 16f))
                 .At(1.05f, () =>
                 {
                     if (open != null) art.sprite = open;
                     AudioBus.ChestOpen();
                     UiConfetti.Sparks(_root, new Vector2(0f, ChestY + 40f), InkTheme.GoldHi, 22, 700f);
                 })
                 .Punch(box, 1.05f, 0.22f, 0.36f);

            float t = 1.35f;
            var chip = ResultKit.Group(_root, "ink", new Vector2(0f, ChestY - 170f), new Vector2(220f, 64f));
            var ink = UiKit.Chip(chip, "v", InkSprites.Ui("ink"), "+0",
                Vector2.zero, new Vector2(220f, 64f), Pin.Center);
            _anim.Pop(chip, t, 0.3f)
                 .CountUp(ink, 0, _loot.Ink, t + 0.1f, 0.5f, "+{0}", AudioBus.CountTick);
            t += 0.45f;

            int shown = 0;
            for (int i = 0; i < _loot.Cards.Count; i++)
            {
                CardStack c = _loot.Cards[i];
                if (c.Item < 0 || c.Count <= 0) continue;
                int col = shown % 3, row = shown / 3;
                float x = (col - 1) * StepX;
                float y = CardsTop - CardH * 0.5f - row * StepY;
                t = BuildCard(c, new Vector2(x, y), t);
                shown++;
            }

            var go = ResultKit.Group(_root, "go", new Vector2(0f, -540f), new Vector2(360f, 100f));
            UiKit.Btn(go, "b", "收下", Vector2.zero, new Vector2(360f, 100f), Close, true);
            _anim.Pop(go, t + 0.1f, 0.32f).Breathe(go, t + 0.5f, 0.04f, 1.2f);
        }

        // UiAnim.Shake 在排轨那一刻记原位，那时箱子还在天上，所以这里按固定落点抖。
        static void Jiggle(RectTransform box, float k, float amp)
        {
            if (box == null) return;
            float w = (1f - k) * amp;
            box.anchoredPosition = new Vector2(Mathf.Sin(k * 60f) * w, ChestY + Mathf.Cos(k * 47f) * w * 0.6f);
        }

        float BuildCard(CardStack c, Vector2 pos, float at)
        {
            ItemDef d = ItemCatalog.Get(c.Item);
            Color q = ItemCatalog.QualityColor(d.Quality);
            int rank = _meta != null ? _meta.ItemRank(c.Item) : 0;
            int have = _meta != null ? _meta.ItemCardCount(c.Item) : c.Count;
            int need = Mathf.Max(1, ItemCatalog.NextCards(d, rank));
            bool max = rank >= ItemCatalog.MaxLevel;

            var card = ResultKit.Group(_root, "card_" + c.Item, pos, new Vector2(CardW, CardH));
            UiKit.Stroke(card, "bg", Vector2.zero, new Vector2(CardW, CardH), Pin.Center, 6f,
                line: ItemCatalog.QualityDeep(d.Quality), fill: InkTheme.CardFace, radius: 24f);
            var band = UiKit.Panel(card, "band", new Vector2(0f, CardH * 0.5f - 22f), new Vector2(CardW - 12f, 32f), q);
            band.GetComponent<Image>().raycastTarget = false;
            var qn = UiKit.Label(band, "q", ItemCatalog.QualityName(d.Quality), 20, Vector2.zero, new Vector2(CardW, 30f));
            qn.color = InkTheme.CardFace;
            UiKit.Bold(qn);
            UiKit.Icon(card, InkSprites.Ui((ItemId)c.Item), new Vector2(0f, 22f), 96f);
            var name = UiKit.Label(card, "n", d.Name, 22, new Vector2(0f, -46f), new Vector2(CardW, 30f));
            name.color = InkTheme.TextDark;
            UiKit.Bold(name);
            var count = ResultKit.Headline(card, "c", "×" + c.Count, 34, new Vector2(CardW * 0.30f, 46f),
                InkTheme.CardFace, InkTheme.Outline, 2f);
            count.rectTransform.sizeDelta = new Vector2(120f, 48f);

            var fill = ResultKit.Bar(card, new Vector2(0f, -CardH * 0.5f + 30f), CardW - 30f, 26f, q);
            ResultKit.SetBar(fill, max ? 1f : Mathf.Clamp01((have - c.Count) / (float)need));
            string done = max ? "已满级" : (have >= need ? (rank <= 0 ? "可解锁!" : "可升级!") : have + "/" + need);
            var prog = UiKit.Label(fill.parent, "p", done, 18, Vector2.zero, new Vector2(CardW, 26f));
            prog.color = InkTheme.CardFace;
            UiKit.Bold(prog);
            prog.gameObject.AddComponent<Outline>().effectColor = InkTheme.Outline;

            bool rare = d.Quality == ItemQuality.Purple;
            _anim.Tween(at, 0.28f, k =>
                 {
                     if (card == null) return;
                     card.localScale = new Vector3(Ease.OutBack(k), 1f, 1f);
                 })
                 .At(at, () => AudioBus.CardReveal(d.Quality))
                 .Tween(at + 0.3f, 0.4f, k =>
                 {
                     if (fill == null) return;
                     float from = (have - c.Count) / (float)need;
                     ResultKit.SetBar(fill, max ? 1f : Mathf.Clamp01(Mathf.Lerp(from, have / (float)need, Ease.OutCubic(k))));
                 })
                 .Punch(card, at + 0.3f, 0.1f, 0.25f);
            if (rare)
                _anim.At(at + 0.12f, () => UiConfetti.Sparks(_root, pos, q, 18, 560f));
            if (have >= need && !max) _anim.Breathe(prog.transform, at + 0.7f, 0.08f, 1.6f);
            // 排轨时每条轨都会先按 k=0 落一次，Punch 的 k=0 是原大，得排完再藏起来。
            card.localScale = new Vector3(0f, 1f, 1f);
            float gap = rare ? 0.9f : d.Quality == ItemQuality.Blue ? 0.72f : 0.42f;
            return at + gap;
        }

        void Close()
        {
            if (_closing) return;
            if (_anim.Playing)
            {
                _anim.Finish();
                return;
            }
            _closing = true;
            AudioBus.Tap();
            Action done = _done;
            Destroy(gameObject);
            done?.Invoke();
        }
    }
}
