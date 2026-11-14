using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 开箱演出：箱子砸下来抖两下、掀盖，墨先滚出来，卡背朝上从箱口飞出落位，
    // 再一张张蓄力、翻面。品质越高蓄力越久、光越亮，稀有卡翻开带光芒和震屏。
    // 印章不跟墨挤成小药丸：卡都翻完，它再占下一格牌位盖下来。章本身要够大，背后发光，名字写在下面。
    // 战利品在 MetaProgress.OpenChest 里已经入账了，这里只负责演，点一下就跳到结尾。
    public sealed class ChestOpenView : MonoBehaviour
    {
        const float ChestY = 350f;
        const float ChestSize = 260f;
        const float CardW = ItemPageBuilder.CardW;
        const float CardH = ItemPageBuilder.CardH;
        const float StepX = 168f;
        const float StepY = 244f;
        const float CardsTop = 40f;
        const float Deal = 0.38f;
        const float DealGap = 0.09f;
        const float Turn = 0.26f;

        // 蓄力时长、抖动力度、光晕峰值和翻开后留着的亮度，按 绿 / 蓝 / 紫。
        static readonly float[] Antic = { 0.14f, 0.4f, 0.85f };
        static readonly float[] Power = { 0.4f, 1f, 1.7f };
        static readonly float[] GlowMax = { 0.55f, 0.85f, 1f };
        static readonly float[] GlowRest = { 0f, 0.35f, 0.6f };

        sealed class Card
        {
            public RectTransform Root;
            public RectTransform Body;
            public GameObject Back;
            public GameObject Front;
            public Image Glow;
            public Image Flash;
            public Image Rays;
            public RectTransform Fill;
            public float FillW;
            public float FillH;
            public float From;
            public float To;
            public Vector2 Origin;
            public Vector2 Slot;
            public float Tilt;
            public float Wait;
            public int Tier;
            public int TurnFoil;
            public Image Art;
            public Text Name;
            public bool Turned;
        }

        MetaProgress _meta;
        ChestLoot _loot;
        Action _done;
        UiAnim _anim;
        RectTransform _root;
        bool _closing;
        bool _skipped;

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

        void Skip()
        {
            if (!_anim.Playing) return;
            _skipped = true;
            _anim.Finish();
        }

        // 跳过时补触发的 At 不再放声音和粒子，否则所有卡的火花会挤在同一帧炸开。
        void Live(float at, Action act) => _anim.At(at, () => { if (!_skipped) act(); });

        void Build()
        {
            ResultKit.SkipCatcher(_root, Skip);
            ChestDef d = ChestCatalog.Get(_loot.Tier);
            string artKey = string.IsNullOrEmpty(_loot.ArtKey) ? d.Key : _loot.ArtKey;
            if (InkSprites.Load("Ui/chest_" + artKey) == null) artKey = d.Key;
            string titleText = string.IsNullOrEmpty(_loot.Title) ? d.Name : _loot.Title;

            var rays = ResultKit.Rays(_root, new Vector2(0f, ChestY), 620f, new Color(1f, 0.84f, 0.45f, 0.7f));
            rays.transform.SetSiblingIndex(1);
            _anim.Spin(rays.transform, 18f).Fade(rays, 0.95f, 0.4f, 0f, 0.7f);

            var title = ResultKit.LightTitle(_root, "title", titleText, 42, new Vector2(0f, ChestY + 196f),
                InkTheme.Hex("FFF3C8"));
            _anim.Fade(title, 0.1f, 0.3f, 0f, 1f);

            var box = ResultKit.Group(_root, "box", new Vector2(0f, ChestY), new Vector2(ChestSize, ChestSize));
            var art = UiKit.Icon(box, InkSprites.Load("Ui/chest_" + artKey), Vector2.zero, ChestSize);
            Sprite open = InkSprites.Load("Ui/chest_" + artKey + "_open");
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
            int seals = _loot.Seals.Count;
            int lead = (_loot.Ink > 0 ? 1 : 0) + (_loot.Foil > 0 ? 1 : 0);
            const float slotStep = 96f + 36f;
            int chipAt = 0;
            Vector2 LeadPos() => new Vector2((chipAt++ - (lead - 1) * 0.5f) * slotStep, 152f);
            if (_loot.Ink > 0)
                CurrencySlot("ink", "灵墨", ModalKit.InkTone, ModalKit.Load("Ui/ico_ink"), _loot.Ink, LeadPos(), ref t);
            if (_loot.Foil > 0)
                CurrencySlot("foil", "金箔", ModalKit.FoilTone, FoilArt.Icon, _loot.Foil, LeadPos(), ref t);
            if (!string.IsNullOrEmpty(_loot.Note))
            {
                var note = UiKit.Label(_root, "note", _loot.Note, 22,
                    new Vector2(0f, 44f), new Vector2(560f, 32f));
                note.color = InkTheme.Hex("FFE7B8");
                _anim.Fade(note, t, 0.25f, 0f, 1f);
                t += 0.2f;
            }

            var cards = new List<Card>();
            if (_loot.Shards != null)
                for (int i = 0; i < _loot.Shards.Count; i++)
                    if (_loot.Shards[i].Count > 0) cards.Add(ShardCard(_loot.Shards[i], Slot(cards.Count)));
            for (int i = 0; i < _loot.Cards.Count; i++)
            {
                CardStack c = _loot.Cards[i];
                if (c.Item >= 0 && c.Count > 0) cards.Add(ItemCard(c, Slot(cards.Count)));
            }
            t = Play(cards, t);
            if (seals > 0) t = RevealSeals(t, cards.Count);

            var go = ResultKit.Group(_root, "go", new Vector2(0f, -542f), new Vector2(296f, 101f));
            ModalKit.HexBtn(go, "lg_yellow", 0f, 0f, "收下", Close, out _);
            _anim.Pop(go, t + 0.1f, 0.32f).Breathe(go, t + 0.5f, 0.04f, 1.2f);
        }

        void CurrencySlot(string id, string label, string tone, Sprite icon, int amount, Vector2 pos, ref float t)
        {
            const int s = 96;
            var holder = ResultKit.Group(_root, id, pos, new Vector2(s, s));
            var slot = ModalKit.ItemSlot(holder, tone, s, 0f, 0f, icon, "+0");
            CardText(holder, "nm", label, 16, -16f, s + 4f, s + 32f, 26f, Color.white, TextAnchor.MiddleCenter);
            var num = slot.Find("n")?.GetComponent<Text>();
            _anim.Pop(holder, t, 0.3f);
            if (num != null)
                _anim.CountUp(num, 0, amount, t + 0.1f, 0.5f, "+{0}", AudioBus.CountTick);
            t += 0.45f;
        }

        static Vector2 Slot(int i)
        {
            int col = i % 3, row = i / 3;
            return new Vector2((col - 1) * StepX, CardsTop - StepY * 0.5f - row * StepY);
        }

        // 卡翻完才盖章。章和道具牌一样大，落在下一格；背后的光落地才亮，名字写在章下面。
        float RevealSeals(float t, int cardsBefore)
        {
            int n = _loot.Seals.Count;
            float start = t + 0.12f;
            const float dur = 0.78f;
            for (int i = 0; i < n; i++)
            {
                SealDrop s = _loot.Seals[i];
                float at = start + i * 0.5f;
                Vector2 slot = cardsBefore == 0
                    ? new Vector2((i - (n - 1) * 0.5f) * StepX, CardsTop - StepY * 0.5f)
                    : Slot(cardsBefore + i);
                Vector2 from = new Vector2(0f, ChestY + 24f);
                var root = ResultKit.Group(_root, "seal" + i, from, new Vector2(172f, 250f));
                root.SetAsLastSibling();
                Color glow = SealGlow(s.Quality);
                var light = UiKit.Icon(root, InkSprites.Load("Ui/seal_burst"), new Vector2(0f, 36f), 250f);
                light.color = new Color(glow.r, glow.g, glow.b, 0f);
                SealArt.Medal(root, "m", s.Type, s.Quality, new Vector2(0f, 36f), 150f);
                var name = UiKit.Label(root, "n", SealCatalog.DropName(s), 26, new Vector2(0f, -92f), new Vector2(200f, 40f));
                name.color = SealCatalog.QualityDeep(s.Quality);
                UiKit.Bold(name);
                var nameFade = name.gameObject.AddComponent<CanvasGroup>();
                nameFade.alpha = 0f;
                _anim.Tween(at, dur, k => DriveSeal(root, light, nameFade, from, slot, k));
                float hit = at + dur * 0.7f;
                Live(hit, () =>
                {
                    AudioBus.Stamp();
                    UiConfetti.Sparks(_root, slot, glow, s.Quality == SealQuality.Gold ? 22 : 14, 560f);
                });
                if (s.Quality == SealQuality.Gold) Live(hit + 0.08f, AudioBus.StudyTier);
            }
            return start + (n - 1) * 0.5f + dur + 0.16f;
        }

        static Color SealGlow(SealQuality q)
        {
            switch (q)
            {
                case SealQuality.Gold: return InkTheme.Hex("F7C948");
                case SealQuality.Purple: return InkTheme.Hex("C084FC");
                case SealQuality.Blue: return InkTheme.Hex("7EB6FF");
                default: return InkTheme.Hex("8ED68A");
            }
        }

        // k 走完章停在牌位上，光留着，名字在下面。跳过时直接停在这一态。
        static void DriveSeal(RectTransform root, Image glow, CanvasGroup name, Vector2 from, Vector2 slot, float k)
        {
            if (root == null) return;
            const float emerge = 0.18f;
            const float travelEnd = 0.7f;
            Vector2 pos;
            float sc, sx = 1f, sy = 1f, rot = 0f, fade = 0f;
            if (k < emerge)
            {
                float u = k / emerge;
                pos = from;
                sc = Mathf.LerpUnclamped(0f, 1.35f, Ease.OutBack(u));
                rot = Mathf.Lerp(-16f, 8f, u);
            }
            else if (k < travelEnd)
            {
                float u = (k - emerge) / (travelEnd - emerge);
                pos = Vector2.LerpUnclamped(from, slot, Ease.InQuad(u));
                sc = Mathf.Lerp(1.35f, 1.08f, u);
                rot = Mathf.Lerp(8f, -4f, u);
            }
            else
            {
                float u = (k - travelEnd) / (1f - travelEnd);
                pos = slot;
                float w = Mathf.Sin(u * Mathf.PI) * (1f - u);
                sx = 1f + w * 0.22f;
                sy = 1f - w * 0.16f;
                sc = Mathf.Lerp(1.08f, 1f, Ease.OutCubic(u));
                rot = Mathf.Lerp(-4f, 0f, Ease.OutCubic(u));
                fade = Ease.OutCubic(Mathf.Clamp01((u - 0.08f) / 0.5f));
            }
            root.anchoredPosition = pos;
            root.localScale = new Vector3(sx * sc, sy * sc, 1f);
            root.localRotation = Quaternion.Euler(0f, 0f, rot);
            if (glow != null)
            {
                Color c = glow.color;
                c.a = fade * 0.9f;
                glow.color = c;
            }
            if (name != null) name.alpha = fade;
        }

        // 先一起飞出落位（卡背朝上），落齐了再按顺序一张张翻。
        float Play(List<Card> cards, float t)
        {
            if (cards.Count == 0) return t;
            float reveal = t + (cards.Count - 1) * DealGap + Deal + 0.2f;
            float tail = reveal;
            for (int i = 0; i < cards.Count; i++)
            {
                Card c = cards[i];
                float dealAt = t + i * DealGap;
                c.Wait = reveal - dealAt;
                float antic = Antic[c.Tier];
                float flipAt = reveal + antic;
                float total = c.Wait + antic + Turn + 0.7f;
                c.Tilt = (i % 3 - 1) * 24f + (i % 2 == 0 ? -10f : 10f);
                _anim.Tween(dealAt, total, k => Drive(c, k * total));
                float pitch = 0.9f + i * 0.05f;
                Live(dealAt, () => AudioBus.CardFlip(pitch));
                if (c.Tier == 2) Live(reveal, AudioBus.ChestUnlock);
                Live(flipAt, () => AudioBus.CardFlip(1.2f));
                Live(flipAt + Turn * 0.5f, () => Burst(c));
                if (c.Rays != null) _anim.Spin(c.Rays.transform, 40f);
                reveal = flipAt + Turn + (c.Tier == 2 ? 0.35f : 0.16f);
                tail = Mathf.Max(tail, reveal);
                if (c.TurnFoil > 0)
                {
                    float at = dealAt + total + 0.08f;
                    _anim.Tween(at, 0.48f, k => Morph(c, k));
                    Live(at + 0.2f, () =>
                    {
                        AudioBus.Coin();
                        UiConfetti.Sparks(_root, c.Slot, InkTheme.GoldHi, 14, 480f);
                    });
                    tail = Mathf.Max(tail, at + 0.6f);
                }
            }
            return tail + 0.3f;
        }

        // 满级的那叠：先按原卡翻开，停一下，再缩进去换成金箔。跳过时直接落在金箔上。
        void Morph(Card c, float k)
        {
            if (c.Body == null) return;
            float s = k < 0.4f
                ? Mathf.Lerp(1f, 0.22f, Ease.InQuad(k / 0.4f))
                : Mathf.Lerp(0.22f, 1f, Ease.OutBack((k - 0.4f) / 0.6f));
            c.Body.localScale = new Vector3(s, s, 1f);
            c.Body.localRotation = Quaternion.identity;
            if (k >= 0.4f) ShowFoil(c);
        }

        void ShowFoil(Card c)
        {
            if (c.Turned) return;
            c.Turned = true;
            Sprite foil = FoilArt.Icon;
            if (c.Art != null && foil != null) c.Art.sprite = foil;
            if (c.Name != null) c.Name.text = "金箔  +" + c.TurnFoil;
        }

        void Burst(Card c)
        {
            var q = (ItemQuality)c.Tier;
            AudioBus.CardReveal(q);
            Color tone = ItemCatalog.QualityColor(q);
            if (c.Tier == 0) return;
            UiConfetti.Sparks(_root, c.Slot, tone, c.Tier == 2 ? 26 : 14, c.Tier == 2 ? 680f : 480f);
            if (c.Tier == 2)
            {
                UiConfetti.Sparks(_root, c.Slot, InkTheme.GoldHi, 18, 560f);
                _anim.Shake(_root, 0f, 12f, 0.32f);
            }
        }

        // 整张卡的状态只由「离发牌过了几秒」决定。跳过时拨到末尾，直接落成翻开后的样子。
        void Drive(Card c, float s)
        {
            if (c.Root == null) return;
            if (s <= 0f)
            {
                c.Body.localScale = Vector3.zero;
                SetAlpha(c.Glow, 0f);
                SetAlpha(c.Rays, 0f);
                return;
            }
            float d = Mathf.Clamp01(s / Deal);
            c.Root.anchoredPosition = Vector2.LerpUnclamped(c.Origin, c.Slot, Ease.OutCubic(d));
            float sc = Mathf.LerpUnclamped(0.3f, 1f, Ease.OutBack(d));
            float rot = (1f - Ease.OutCubic(d)) * c.Tilt;
            float sx = 1f;
            bool up = false;
            float glow = 0f, rays = 0f, flash = 0f, bar = 0f;
            float antic = Antic[c.Tier];
            float r = s - c.Wait;
            if (r > 0f && r < antic)
            {
                float p = r / antic;
                rot += Mathf.Sin(r * 42f) * (1.5f + 5f * p) * Power[c.Tier];
                sc *= 1f + 0.07f * p;
                glow = p * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(r * 9f)));
                rays = p * 0.5f;
            }
            else if (r >= antic)
            {
                float u = r - antic;
                if (u < Turn)
                {
                    float h = u / Turn;
                    sc *= 1.1f;
                    if (h < 0.5f) sx = 1f - h * 2f;
                    else
                    {
                        up = true;
                        sx = Ease.OutBack((h - 0.5f) * 2f);
                    }
                    glow = 1f;
                    rays = 0.6f;
                }
                else
                {
                    up = true;
                    float v = Mathf.Clamp01((u - Turn) / 0.22f);
                    sc *= Mathf.Lerp(1.1f, 1f, Ease.OutCubic(v));
                    glow = Mathf.Lerp(1f, GlowRest[c.Tier] / GlowMax[c.Tier], v);
                    rays = 0.75f;
                    bar = Ease.OutCubic(Mathf.Clamp01((u - Turn - 0.1f) / 0.45f));
                }
                if (u >= Turn * 0.5f) flash = 1f - Mathf.Clamp01((u - Turn * 0.5f) / 0.34f);
            }
            c.Body.localScale = new Vector3(sx * sc, sc, 1f);
            c.Body.localRotation = Quaternion.Euler(0f, 0f, rot);
            if (c.Back.activeSelf == up) c.Back.SetActive(!up);
            if (c.Front.activeSelf != up) c.Front.SetActive(up);
            c.Glow.rectTransform.localScale = new Vector3(sc, sc, 1f);
            SetAlpha(c.Glow, glow * GlowMax[c.Tier]);
            SetAlpha(c.Flash, flash);
            SetAlpha(c.Rays, rays);
            if (c.Fill != null)
                c.Fill.sizeDelta = new Vector2(Mathf.Max(c.FillH, c.FillW * Mathf.Lerp(c.From, c.To, bar)), c.FillH);
        }

        static void SetAlpha(Graphic g, float a)
        {
            if (g == null) return;
            Color col = g.color;
            col.a = a;
            g.color = col;
        }

        Card ShardCard(ShardStack s, Vector2 slot)
        {
            SkinDef skin = SkinCatalog.Get(s.Skin);
            ItemQuality q = SkinCatalog.QualityOf(skin.Rarity);
            return MakeCard("shard_" + s.Skin, q, InkSprites.Ui("skin_" + skin.Key),
                skin.Name + "碎片", s.Count, slot);
        }

        Card ItemCard(CardStack c, Vector2 slot)
        {
            ItemDef d = ItemCatalog.Get(c.Item);
            Card card = MakeCard("card_" + c.Item, d.Quality, InkSprites.Ui((ItemId)c.Item),
                d.Name, c.Count, slot);
            card.TurnFoil = c.Foil;
            return card;
        }

        // 翻开就是道具页那张已装备卡，按品质上绿、蓝、紫。底栏只写「xx碎片」和这次的数量。
        Card MakeCard(string id, ItemQuality q, Sprite icon, string title, int count, Vector2 slot)
        {
            int tier = Mathf.Clamp((int)q, 0, 2);
            var size = new Vector2(CardW, CardH);
            Color tone = ItemCatalog.QualityColor(q);

            var c = new Card { Tier = tier, Slot = slot, Origin = new Vector2(0f, ChestY + 10f) };
            c.Root = ResultKit.Group(_root, id, c.Origin, size);
            if (tier == 2)
            {
                c.Rays = ResultKit.Rays(c.Root, Vector2.zero, CardH * 2.4f, Color.Lerp(tone, Color.white, 0.35f));
                c.Rays.color = new Color(c.Rays.color.r, c.Rays.color.g, c.Rays.color.b, 0f);
            }
            c.Glow = UiKit.Icon(c.Root, InkSprites.Load("Ui/loot_glow"), Vector2.zero, CardW);
            c.Glow.preserveAspect = false;
            c.Glow.rectTransform.sizeDelta = new Vector2(CardW * 1.35f, CardH * 1.22f);
            c.Glow.color = new Color(Mathf.Min(1f, tone.r * 1.25f), Mathf.Min(1f, tone.g * 1.25f), Mathf.Min(1f, tone.b * 1.25f), 0f);

            c.Body = ResultKit.Group(c.Root, "body", Vector2.zero, size);
            c.Back = ModalKit.Pic(c.Body, "back", "Ui/item_card_back", 0f, 0f, CardW, CardH).gameObject;

            var face = ModalKit.Pic(c.Body, "front", FrameKey(q), 0f, 0f, CardW, CardH);
            c.Front = face.gameObject;

            c.Art = ModalKit.Pic(face.transform, "art", null,
                ItemPageBuilder.IconX, ItemPageBuilder.IconY, ItemPageBuilder.IconS, ItemPageBuilder.IconS, true);
            if (icon != null) c.Art.sprite = icon;

            c.Name = ModalKit.SolidText(face.transform, "band", FragmentLine(title, count), 14,
                4f, ItemPageBuilder.BandY, CardW - 8f, ItemPageBuilder.BandH,
                ModalKit.TextDark, TextAnchor.MiddleCenter, true);

            c.Flash = ModalKit.Pic(c.Body, "flash", "Ui/loot_flash", 0f, 0f, CardW, CardH);
            SetAlpha(c.Flash, 0f);

            c.Front.SetActive(false);
            c.Body.localScale = Vector3.zero;
            return c;
        }

        static string FragmentLine(string title, int count)
        {
            string name = string.IsNullOrEmpty(title) ? "碎片" : title;
            if (!name.EndsWith("碎片")) name += "碎片";
            return name + "  ×" + count;
        }

        static string FrameKey(ItemQuality q)
        {
            if (q == ItemQuality.Purple) return "Ui/item_card_equip_purple";
            if (q == ItemQuality.Blue) return "Ui/item_card_equip_blue";
            return "Ui/item_card_equip_green";
        }

        static Text CardText(Transform parent, string name, string text, int size, float x, float y, float w, float h,
            Color color, TextAnchor align)
        {
            var t = UiKit.Label(parent, name, text, size, Vector2.zero, new Vector2(w, h), align, Pin.TopLeft);
            HomeReskin.TopLeft(t.rectTransform, x, y, w, h);
            t.color = color;
            t.alignByGeometry = true;
            UiKit.Bold(t);
            t.raycastTarget = false;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = HomeReskin.Ink;
            o.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        // UiAnim.Shake 在排轨那一刻记原位，那时箱子还在天上，所以这里按固定落点抖。
        static void Jiggle(RectTransform box, float k, float amp)
        {
            if (box == null) return;
            float w = (1f - k) * amp;
            box.anchoredPosition = new Vector2(Mathf.Sin(k * 60f) * w, ChestY + Mathf.Cos(k * 47f) * w * 0.6f);
        }

        void Close()
        {
            if (_closing) return;
            if (_anim.Playing)
            {
                Skip();
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
