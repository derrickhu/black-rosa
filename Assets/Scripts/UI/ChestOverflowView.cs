using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 四个宝箱位都满了，新箱子先不折成墨。把现有四只和新的一只摆出来，玩家自己选：
    // 有能开的就打开腾位；都在等，就看广告或花钻石打开一只，或者挑一只换成墨。
    public sealed class ChestOverflowView : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 1040f;
        const float CardW = 140f;
        const float CardH = 186f;
        const float NewW = 176f;
        const float NewH = 196f;
        const int Fresh = 4;

        sealed class SlotCard
        {
            public RectTransform Root;
            public Image Face;
            public Image Art;
            public Text Name;
            public Text State;
            public ChestState Shown = (ChestState)(-1);
        }

        MetaProgress _meta;
        Action _changed;
        Action _closed;
        RectTransform _root;
        RectTransform _board;
        RectTransform _actions;
        Text _hint;
        UiAnim _anim;
        readonly SlotCard[] _slots = new SlotCard[ChestCatalog.Slots];
        SlotCard _fresh;
        int _sel = Fresh;
        int _shownCost = -2;
        bool _busy;
        bool _intro = true;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed, Action closed = null)
        {
            if (layer == null || meta == null || !meta.HasOverflow)
            {
                closed?.Invoke();
                return;
            }
            if (layer.Find("chest_overflow") != null) return;
            var dim = UiKit.Dimmer(layer);
            dim.name = "chest_overflow";
            dim.SetAsLastSibling();
            var view = dim.gameObject.AddComponent<ChestOverflowView>();
            view._meta = meta;
            view._changed = changed;
            view._closed = closed;
            view._root = dim;
            view._anim = UiAnim.On(view);
            view.Build();
        }

        void Build()
        {
            ChestDef incoming = ChestCatalog.Get(_meta.OverflowTier);
            _board = PanelKit.Board(_root, "宝箱位满了", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Dismiss);

            var sub = UiKit.Label(_board, "sub", "新的" + incoming.Name + "还没处放", 28,
                new Vector2(0f, 392f), new Vector2(BoardW - 80f, 40f));
            sub.color = InkTheme.Seal;
            UiKit.Bold(sub);

            var own = UiKit.Label(_board, "own", "现在的四个", 20, new Vector2(0f, 348f), new Vector2(BoardW - 80f, 28f));
            own.color = InkTheme.TextMid;

            float span = CardW + 10f;
            float left = -span * 1.5f;
            for (int i = 0; i < _slots.Length; i++)
            {
                int slot = i;
                _slots[i] = MakeCard(_board, "s" + i, new Vector2(left + span * i, 214f), new Vector2(CardW, CardH), () => Select(slot));
                _anim.Pop(_slots[i].Root, 0.04f + i * 0.06f, 0.32f);
            }

            var tag = UiKit.Label(_board, "fresh", "新获得", 22, new Vector2(0f, 92f), new Vector2(200f, 30f));
            tag.color = InkTheme.Seal;
            UiKit.Bold(tag);

            _fresh = MakeCard(_board, "new", new Vector2(0f, -24f), new Vector2(NewW, NewH), () => Select(Fresh));
            PaintFresh();
            Vector2 home = _fresh.Root.anchoredPosition;
            _anim.Move(_fresh.Root, home + new Vector2(0f, 520f), home, 0.2f, 0.48f, Ease.OutBounce)
                 .At(0.46f, AudioBus.ChestLand)
                 .At(0.5f, () => UiConfetti.Sparks(_board, home, InkTheme.GoldHi, 14, 380f));

            _hint = UiKit.Label(_board, "hint", "", 22, new Vector2(0f, -186f), new Vector2(BoardW - 64f, 64f));
            _hint.color = InkTheme.TextMid;
            _hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            _hint.verticalOverflow = VerticalWrapMode.Overflow;

            _actions = ResultKit.Group(_board, "acts", new Vector2(0f, -382f), new Vector2(BoardW - 40f, 220f));

            _sel = FirstReady();
            RefreshSlots(true);
            ApplySelect(false);
            WriteHint();
            BuildActions();
            _anim.At(0.72f, () =>
            {
                _intro = false;
                ApplySelect(true);
            });
        }

        static SlotCard MakeCard(RectTransform board, string name, Vector2 pos, Vector2 size, Action tap)
        {
            var root = UiKit.Stroke(board, name, pos, size, Pin.Center, 5f, null, InkTheme.CardFace, 22f);
            var face = root.GetComponent<Image>();
            face.raycastTarget = true;
            var btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = face;
            btn.onClick.AddListener(() => tap());
            var card = new SlotCard
            {
                Root = root,
                Face = face,
                Art = UiKit.Icon(root, null, new Vector2(0f, 18f), size.x * 0.72f),
                Name = UiKit.Label(root, "name", "", 22, new Vector2(0f, -size.y * 0.28f), new Vector2(size.x, 28f)),
                State = UiKit.Label(root, "st", "", 20, new Vector2(0f, -size.y * 0.42f), new Vector2(size.x, 26f))
            };
            UiKit.Bold(card.Name);
            UiKit.Bold(card.State);
            card.Name.color = InkTheme.TextDark;
            return card;
        }

        int FirstReady()
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_meta.ChestStateOf(i) == ChestState.Ready) return i;
            return Fresh;
        }

        void Select(int sel)
        {
            if (_busy || sel == _sel) return;
            AudioBus.Tap();
            _sel = sel;
            ApplySelect(true);
            WriteHint();
            BuildActions();
        }

        void ApplySelect(bool scale)
        {
            for (int i = 0; i < _slots.Length; i++) Style(_slots[i], i == _sel, scale);
            Style(_fresh, _sel == Fresh, scale);
        }

        static void Style(SlotCard card, bool on, bool scale)
        {
            if (card == null || card.Root == null) return;
            card.Face.color = on ? InkTheme.Hex("FFE7B0") : InkTheme.CardFace;
            if (!scale) return;
            float s = on ? 1.06f : 1f;
            card.Root.localScale = new Vector3(s, s, 1f);
        }

        void WriteHint()
        {
            if (_sel == Fresh)
            {
                _hint.text = _meta.AnyChestReady
                    ? "点已经可以打开的，新箱子就放进去。这只也能看广告当场开"
                    : "四个都在等。看广告当场开这只，或换成墨";
                return;
            }
            _hint.text = _meta.ChestStateOf(_sel) == ChestState.Ready
                ? "打开后，后面的箱子往前排，新的放最后"
                : "打开后，后面的箱子往前排。换成墨只要墨，里面的卡不要了";
        }

        void BuildActions()
        {
            for (int i = _actions.childCount - 1; i >= 0; i--)
            {
                GameObject go = _actions.GetChild(i).gameObject;
                go.SetActive(false);
                Destroy(go);
            }
            if (_sel == Fresh) BuildFreshActions();
            else if (_meta.ChestStateOf(_sel) == ChestState.Ready) BuildOpenAction();
            else BuildWaitActions();
        }

        void BuildOpenAction()
        {
            UiKit.Btn(_actions, "open", "打开并放入", new Vector2(0f, 24f), new Vector2(460f, 96f), OpenSelected, true);
        }

        void BuildFreshActions()
        {
            ChestDef d = ChestCatalog.Get(_meta.OverflowTier);
            Button ad = UiKit.Btn(_actions, "ad", "看广告当场开", new Vector2(0f, 46f), new Vector2(460f, 92f), OpenFreshNow, true);
            ResultKit.AdMark(ad, 34f);
            UiKit.Btn(_actions, "ink", "换成" + ChestCatalog.InkAvg(d.Tier) + "墨",
                new Vector2(0f, -52f), new Vector2(460f, 84f), ScrapFresh, false);
        }

        void BuildWaitActions()
        {
            int slot = _sel;
            ChestDef d = ChestCatalog.Get(_meta.ChestTierOf(slot));
            Button ad = UiKit.Btn(_actions, "ad", "看广告打开", new Vector2(0f, 52f), new Vector2(460f, 88f), () => AdOpen(slot), true);
            ResultKit.AdMark(ad, 32f);
            int cost = _meta.ChestRushCost(slot);
            _shownCost = cost;
            Button gem = UiKit.Btn(_actions, "gem", cost + "钻打开", new Vector2(-118f, -48f), new Vector2(220f, 80f),
                () => GemOpen(slot), false);
            if (_meta.Diamond < cost) PanelKit.Dim(gem, true, false);
            UiKit.Btn(_actions, "ink", "换成" + ChestCatalog.InkAvg(d.Tier) + "墨",
                new Vector2(118f, -48f), new Vector2(220f, 80f), () => ScrapSlot(slot), false);
        }

        void Update()
        {
            if (_meta == null || _busy) return;
            bool actions = false;
            for (int i = 0; i < _slots.Length; i++)
            {
                ChestState st = _meta.ChestStateOf(i);
                if (st != _slots[i].Shown)
                {
                    PaintSlot(i, true);
                    if (i == _sel) actions = true;
                }
                else if (st == ChestState.Timing)
                {
                    int sec = _meta.ChestSecondsLeft(i);
                    _slots[i].State.text = ChestCatalog.Clock(sec);
                }
                else if (st == ChestState.Ready)
                {
                    float k = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 2.4f + i);
                    _slots[i].Art.rectTransform.localScale = new Vector3(k, k, 1f);
                }
            }
            if (_sel >= 0 && _sel < _slots.Length && _meta.ChestStateOf(_sel) == ChestState.Timing)
            {
                int cost = _meta.ChestRushCost(_sel);
                if (cost != _shownCost)
                {
                    _shownCost = cost;
                    var gem = _actions.Find("gem");
                    if (gem != null)
                    {
                        PanelKit.SetText(gem.GetComponent<Button>(), cost + "钻打开");
                        PanelKit.Dim(gem.GetComponent<Button>(), _meta.Diamond < cost, false);
                    }
                }
            }
            if (actions && !_intro)
            {
                WriteHint();
                BuildActions();
            }
        }

        void RefreshSlots(bool restyle)
        {
            for (int i = 0; i < _slots.Length; i++) PaintSlot(i, restyle);
            PaintFresh();
        }

        void PaintSlot(int i, bool restyle)
        {
            SlotCard card = _slots[i];
            ChestState st = _meta.ChestStateOf(i);
            card.Shown = st;
            if (st == ChestState.Empty)
            {
                card.Name.text = "空位";
                card.State.text = "";
                return;
            }
            ChestDef d = ChestCatalog.Get(_meta.ChestTierOf(i));
            card.Name.text = d.Name;
            if (restyle)
                card.Art.sprite = InkSprites.Load("Ui/chest_" + d.Key + (st == ChestState.Ready ? "_open" : ""));
            if (st == ChestState.Ready)
            {
                card.State.text = "可打开";
                card.State.color = InkTheme.Accel;
            }
            else if (st == ChestState.Timing)
            {
                card.State.text = ChestCatalog.Clock(_meta.ChestSecondsLeft(i));
                card.State.color = InkTheme.TextDark;
            }
            else
            {
                card.State.text = "排队";
                card.State.color = InkTheme.TextMid;
            }
        }

        void PaintFresh()
        {
            ChestDef d = ChestCatalog.Get(_meta.OverflowTier);
            _fresh.Name.text = d.Name;
            _fresh.State.text = "新获得";
            _fresh.State.color = InkTheme.Seal;
            _fresh.Art.sprite = InkSprites.Load("Ui/chest_" + d.Key);
        }

        void PaintSlotTier(int slot, ChestTier tier)
        {
            SlotCard card = _slots[slot];
            ChestDef d = ChestCatalog.Get(tier);
            card.Shown = _meta.ChestStateOf(slot);
            card.Name.text = d.Name;
            card.Art.sprite = InkSprites.Load("Ui/chest_" + d.Key);
            card.State.text = "已放入";
            card.State.color = InkTheme.Accel;
            card.Art.rectTransform.localScale = Vector3.one;
        }

        void OpenSelected()
        {
            if (_busy) return;
            AudioBus.Tap();
            int slot = _sel;
            ChestLoot loot = _meta.OpenChest(slot);
            if (loot == null) return;
            PlayOpen(loot, slot);
        }

        void AdOpen(int slot)
        {
            if (_busy) return;
            AudioBus.Tap();
            AdStub.Reward("chest_speed", () =>
            {
                if (this == null || _busy) return;
                if (!_meta.AdRushChest(slot)) return;
                ChestLoot loot = _meta.OpenChest(slot);
                if (loot != null) PlayOpen(loot, slot);
            });
        }

        void GemOpen(int slot)
        {
            if (_busy) return;
            AudioBus.Tap();
            int cost = _meta.ChestRushCost(slot);
            if (_meta.Diamond < cost)
            {
                InkToast.Show(_root, "钻石不够，还差 " + (cost - _meta.Diamond));
                return;
            }
            if (!_meta.RushChest(slot)) return;
            ChestLoot loot = _meta.OpenChest(slot);
            if (loot != null) PlayOpen(loot, slot);
        }

        void OpenFreshNow()
        {
            if (_busy) return;
            AudioBus.Tap();
            AdStub.Reward("chest_speed", () =>
            {
                if (this == null || _busy) return;
                ChestLoot loot = _meta.OpenOverflow();
                if (loot == null) return;
                _busy = true;
                _anim.Clear();
                _root.gameObject.SetActive(false);
                ChestOpenView.Show(_layerOf(), _meta, loot, () =>
                {
                    if (this == null) return;
                    _root.gameObject.SetActive(true);
                    Close(true);
                });
            });
        }

        void PlayOpen(ChestLoot loot, int slot)
        {
            _busy = true;
            _meta.DeferChestSlide = true;
            int[] from = _meta.ChestSlideFrom;
            _meta.ChestSlideFrom = null;
            _anim.Clear();
            _root.gameObject.SetActive(false);
            ChestOpenView.Show(_layerOf(), _meta, loot, () =>
            {
                if (this == null) return;
                _root.gameObject.SetActive(true);
                _meta.DeferChestSlide = false;
                ChestTier incoming = _meta.HasOverflow ? _meta.OverflowTier : loot.Tier;
                int placed = _meta.PlaceOverflow();
                Arrange(from, slot, placed, incoming, () => Close(true));
            });
        }

        void ScrapSlot(int slot)
        {
            if (_busy) return;
            AudioBus.Tap();
            _busy = true;
            ChestTier incoming = _meta.OverflowTier;
            Vector2 from = RewardFly.Local(_layerOf(), _slots[slot].Root);
            int ink = _meta.ScrapWaitingChest(slot);
            if (ink <= 0)
            {
                _busy = false;
                return;
            }
            int[] slide = _meta.ChestSlideFrom;
            _meta.ChestSlideFrom = null;
            int placed = _meta.PlaceOverflow();
            RewardFly.Play(_layerOf(), from, new[] { RewardFly.Ink(_layerOf(), ink) });
            Arrange(slide, slot, placed, incoming, () => Close(true));
        }

        void ScrapFresh()
        {
            if (_busy) return;
            AudioBus.Tap();
            _busy = true;
            Vector2 from = RewardFly.Local(_layerOf(), _fresh.Root);
            int ink = _meta.ScrapOverflow();
            if (ink <= 0)
            {
                _busy = false;
                return;
            }
            RewardFly.Play(_layerOf(), from, new[] { RewardFly.Ink(_layerOf(), ink) });
            Shrink(_fresh.Root, _fresh.Root.anchoredPosition, () => Close(true));
        }

        void Shrink(RectTransform target, Vector2 sparkAt, Action then)
        {
            _anim.Clear();
            Vector3 from = target.localScale;
            _anim.Tween(0f, 0.22f, k =>
            {
                if (target == null) return;
                float s = Mathf.Lerp(1f, 0.05f, Ease.OutCubic(k));
                target.localScale = from * s;
            });
            _anim.At(0.16f, () => UiConfetti.Sparks(_board, sparkAt, InkTheme.GoldHi, 12, 360f));
            _anim.At(0.24f, then);
        }

        Vector2 SlotPos(int i)
        {
            float span = CardW + 10f;
            return new Vector2(-span * 1.5f + span * i, 214f);
        }

        // 被打开的那只收掉，后面的按原顺序滑到前面，新箱子落到最后一格。
        void Arrange(int[] from, int opened, int placed, ChestTier tier, Action then)
        {
            _anim.Clear();
            if (opened >= 0 && opened < _slots.Length)
            {
                RectTransform gone = _slots[opened].Root;
                _anim.Tween(0f, 0.16f, k =>
                {
                    if (gone == null) return;
                    float s = Mathf.Lerp(1f, 0.05f, Ease.OutCubic(k));
                    gone.localScale = new Vector3(s, s, 1f);
                });
                _anim.At(0.16f, () => { if (gone != null) gone.gameObject.SetActive(false); });
            }
            if (from != null)
            {
                for (int i = 0; i < from.Length && i < _slots.Length; i++)
                {
                    int src = from[i];
                    if (src < 0 || src == i || src >= _slots.Length) continue;
                    _slots[src].Root.SetAsLastSibling();
                    _anim.Move(_slots[src].Root, SlotPos(src), SlotPos(i), 0.08f, 0.28f, Ease.OutCubic);
                }
            }
            if (_fresh != null && placed >= 0 && placed < _slots.Length)
            {
                RectTransform src = _fresh.Root;
                Vector2 to = SlotPos(placed);
                src.SetAsLastSibling();
                _anim.Move(src, src.anchoredPosition, to, 0.08f, 0.32f, Ease.OutCubic);
                _anim.Tween(0.08f, 0.32f, k =>
                {
                    if (src == null) return;
                    float s = Mathf.Lerp(1.06f, 0.78f, Ease.OutCubic(k));
                    src.localScale = new Vector3(s, s, 1f);
                });
            }
            _anim.At(0.46f, () =>
            {
                AudioBus.ChestLand();
                if (placed >= 0) UiConfetti.Sparks(_board, SlotPos(placed), InkTheme.GoldHi, 16, 420f);
                SnapSlots();
                if (_fresh != null) _fresh.Root.gameObject.SetActive(false);
                if (placed >= 0) PaintSlotTier(placed, tier);
            });
            _anim.At(0.62f, then);
        }

        void SnapSlots()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                SlotCard card = _slots[i];
                card.Root.gameObject.SetActive(true);
                card.Root.localScale = Vector3.one;
                card.Root.anchoredPosition = SlotPos(i);
                PaintSlot(i, true);
            }
        }

        RectTransform _layerOf() => _root.parent as RectTransform;

        void Dismiss()
        {
            if (_busy) return;
            AudioBus.Tap();
            InkToast.Show(_layerOf(), "新宝箱先留着，出征页上可以再放");
            Close(false);
        }

        void Close(bool resolved)
        {
            Action changed = _changed;
            Action closed = _closed;
            RectTransform layer = _layerOf();
            MetaProgress meta = _meta;
            bool more = resolved && meta != null && meta.HasOverflow;
            // 关掉是延后的。先改名，紧跟着的回家、开打才不会以为挑选页还在。
            gameObject.name = "chest_overflow_done";
            Destroy(gameObject);
            changed?.Invoke();
            if (more)
            {
                InkToast.Show(layer, "还有一只宝箱");
                Show(layer, meta, changed, closed);
                return;
            }
            closed?.Invoke();
        }
    }
}
