using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 字卡页右上角的兑换，和点金箔栏弹出的获取说明。货币本身在顶栏和金箔药丸上已经有了。
    public sealed class BagPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float CardH = 196f;
        const float CardTop = 156f;
        const float CardGap = 16f;
        const float SwapBoardH = CardTop + CardH + CardGap + CardH + 28f;

        MetaProgress _meta;
        RectTransform _layer;
        Action _changed;
        RectTransform _board;
        bool _note;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "swap";
            var p = dim.gameObject.AddComponent<BagPanel>();
            p._meta = meta;
            p._layer = layer;
            p._changed = changed;
            p.BuildSwap(dim);
        }

        public static void ShowSources(RectTransform layer, RectTransform anchor)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "foil_note";
            dim.GetComponent<Image>().color = Color.clear;
            var p = dim.gameObject.AddComponent<BagPanel>();
            p._note = true;
            var hit = dim.GetComponent<Image>();
            hit.raycastTarget = true;
            var btn = dim.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(p.Close);
            p.BuildNote(dim, anchor);
        }

        void Close()
        {
            AudioBus.Tap();
            if (!_note) _changed?.Invoke();
            Destroy(gameObject);
        }

        void BuildSwap(RectTransform dim)
        {
            _board = PanelKit.Board(dim, "兑换", Vector2.zero, new Vector2(BoardW, SwapBoardH), Pin.Center, Close);
            PaintSwap();
        }

        void PaintSwap()
        {
            Drop("ink");
            Drop("gem");
            var ink = Offer("ink", CardTop, "墨换金箔", "今日剩 " + _meta.SwapLeft + " 次",
                InkSprites.Ui("ink"), _meta.SwapCost.ToString(), StudyCatalog.SwapFoil.ToString());
            bool canAd = _meta.CanSwapAd;
            var go = UiKit.Btn(ink, "go", "兑换", new Vector2(canAd ? -130f : 0f, -56f), new Vector2(canAd ? 240f : 300f, 70f),
                DoSwap, true);
            PanelKit.Dim(go, !_meta.CanSwap);
            if (canAd)
            {
                var ad = UiKit.Btn(ink, "ad", "再领 " + StudyCatalog.SwapFoil, new Vector2(130f, -56f), new Vector2(240f, 70f),
                    DoSwapAd, false);
                UiKit.PaintBtn(ad, FoilArt.Face, FoilArt.Deep, InkTheme.TextDark);
                ResultKit.AdMark(ad, 32f);
            }

            var gem = Offer("gem", CardTop + CardH + CardGap, "钻石换金箔",
                _meta.SwapGemDone ? "今日剩 0 次" : "今日剩 1 次",
                InkSprites.Ui("diamond"), StudyCatalog.GemSwapCost.ToString(), StudyCatalog.GemSwapFoil.ToString());
            var buy = UiKit.Btn(gem, "go", "兑换", new Vector2(0f, -56f), new Vector2(300f, 70f), DoSwapGem, true);
            PanelKit.Dim(buy, !_meta.CanSwapGem);
        }

        void Drop(string id)
        {
            var old = _board.Find(id);
            if (old != null)
            {
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            var shade = _board.Find(id + "_sh");
            if (shade != null)
            {
                shade.gameObject.SetActive(false);
                Destroy(shade.gameObject);
            }
        }

        RectTransform Offer(string id, float y, string title, string remain, Sprite pay, string cost, string gain)
        {
            var card = UiKit.Stroke(_board, id, new Vector2(0f, y), new Vector2(580f, CardH), Pin.Top, 5f,
                FoilArt.Deep, InkTheme.Hex("FFF3D6"), 22f);
            var head = UiKit.Label(card, "h", title, 28, new Vector2(-150f, 64f), new Vector2(240f, 40f),
                TextAnchor.MiddleLeft);
            UiKit.Bold(head);
            var left = UiKit.Label(card, "left", remain, 22, new Vector2(170f, 64f), new Vector2(200f, 36f),
                TextAnchor.MiddleRight);
            left.color = InkTheme.TextMid;
            UiKit.Icon(card, pay, new Vector2(-200f, 6f), 54f);
            var a = UiKit.Label(card, "a", cost, 32, new Vector2(-120f, 6f), new Vector2(100f, 44f));
            UiKit.Bold(a);
            var arrow = UiKit.Label(card, "arrow", "→", 34, new Vector2(-40f, 6f), new Vector2(60f, 44f));
            arrow.color = InkTheme.TextMid;
            Image gainIcon = UiKit.Icon(card, FoilArt.Icon, new Vector2(30f, 6f), 54f);
            gainIcon.gameObject.name = "gain";
            var b = UiKit.Label(card, "b", gain, 32, new Vector2(96f, 6f), new Vector2(80f, 44f));
            b.color = FoilArt.Deep;
            UiKit.Bold(b);
            return card;
        }

        void DoSwap()
        {
            if (!_meta.CanSwap)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.SwapLeft <= 0 ? "今天换完了，明天再来" : "墨不够 " + _meta.SwapCost);
                return;
            }
            _meta.SwapInk();
            Gained("ink", StudyCatalog.SwapFoil);
        }

        void DoSwapAd()
        {
            if (!_meta.CanSwapAd) return;
            AdStub.Reward("foil_double", () =>
            {
                _meta.GrantSwapAd();
                if (this == null) return;
                Gained("ink", StudyCatalog.SwapFoil);
            });
        }

        void DoSwapGem()
        {
            if (_meta.SwapGemDone)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "今天换过了，明天再来");
                return;
            }
            if (_meta.Diamond < StudyCatalog.GemSwapCost)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "钻石不够 " + StudyCatalog.GemSwapCost);
                return;
            }
            _meta.SwapGem();
            Gained("gem", StudyCatalog.GemSwapFoil);
        }

        void Gained(string cardId, int n)
        {
            Transform from = _board.Find(cardId + "/gain");
            Vector2 at = RewardFly.Local(_layer, from);
            InkToast.Show(_layer, "金箔 +" + n);
            PaintSwap();
            if (_changed != null) _changed();
            RewardFly.Piece foil = RewardFly.Foil(_layer, n);
            RectTransform chip = FoilChip();
            if (chip != null) foil.Land = chip;
            RewardFly.Play(_layer, at, new[] { foil });
        }

        // 金箔药丸在字卡页里，不在顶栏。飞入落到这颗上，数字会跟着弹一下。
        RectTransform FoilChip()
        {
            RectTransform hit = RewardFly.Chip(_layer, "foil", "foil");
            return hit != null ? hit : FindNamed(_layer, "foil");
        }

        static RectTransform FindNamed(Transform root, string name)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name == name) return c as RectTransform;
                RectTransform nested = FindNamed(c, name);
                if (nested != null) return nested;
            }
            return null;
        }

        void BuildNote(RectTransform dim, RectTransform anchor)
        {
            const float w = 420f;
            const float h = 172f;
            var card = UiKit.Stroke(dim, "tip", Vector2.zero, new Vector2(w, h), Pin.Center, 3f, radius: 18f);
            foreach (var g in card.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            var body = UiKit.Label(card, "t",
                "· 首次通关\n· 每日签到\n· 开宝箱\n· 炮台及道具满级后，碎片自动转换\n· 每日兑换",
                20, Vector2.zero, new Vector2(w - 36f, h - 16f), TextAnchor.MiddleLeft);
            body.lineSpacing = 1.12f;

            Vector3 under = anchor != null
                ? anchor.TransformPoint(new Vector3(0f, -anchor.rect.height - 8f, 0f))
                : dim.TransformPoint(new Vector3(0f, dim.rect.height * 0.5f - 80f, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dim, RectTransformUtility.WorldToScreenPoint(null, under), null, out Vector2 local);
            float limit = Mathf.Max(0f, dim.rect.width * 0.5f - w * 0.5f - 12f);
            card.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -limit, limit), local.y - h * 0.5f);
        }
    }
}
