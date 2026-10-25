using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 点开一张字卡：大卡、本事、伤害加成从几到几、十格等级、升级和看广告试用。
    // 左右箭头在当前分类里已有的字之间切换，不用退回列表。
    public sealed class GlyphDetailPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 1060f;
        const float CardY = 96f;
        static readonly Vector2 CardSize = new Vector2(210f, 270f);

        MetaProgress _meta;
        RectTransform _layer;
        Action _closed;
        RectTransform _board;
        RectTransform _body;
        List<CodexEntry> _list;
        int _at;
        UiAnim _anim;
        float _busyUntil;

        public static void Show(RectTransform layer, MetaProgress meta, List<CodexEntry> list, int at, Action closed)
        {
            if (list == null || list.Count == 0) return;
            var dim = UiKit.Dimmer(layer);
            dim.name = "study_detail";
            var p = dim.gameObject.AddComponent<GlyphDetailPanel>();
            p._meta = meta;
            p._layer = layer;
            p._closed = closed;
            p._list = list;
            p._at = Mathf.Clamp(at, 0, list.Count - 1);
            p._anim = UiAnim.On(p);
            p._board = PanelKit.Board(dim, CodexCatalog.Title(list[p._at]), Vector2.zero, new Vector2(BoardW, BoardH),
                Pin.Center, p.Close);
            p.Paint();
        }

        CodexEntry Cur => _list[_at];

        void Close()
        {
            AudioBus.Tap();
            _closed?.Invoke();
            Destroy(gameObject);
        }

        void Paint()
        {
            if (_body != null)
            {
                _body.gameObject.SetActive(false);
                Destroy(_body.gameObject);
            }
            _body = UiKit.Panel(_board, "body", Vector2.zero, Vector2.zero, Color.clear);
            _body.anchorMin = Vector2.zero;
            _body.anchorMax = Vector2.one;
            _body.offsetMin = Vector2.zero;
            _body.offsetMax = Vector2.zero;
            _body.GetComponent<Image>().raycastTarget = false;
            Transform ribbon = _board.Find("ribbon");
            Text title = ribbon != null ? ribbon.GetComponentInChildren<Text>() : null;
            if (title != null) title.text = CodexCatalog.Title(Cur);

            CodexEntry e = Cur;
            int lv = _meta.StudyLevel(e);
            var card = StudyCard.Build(_body, e, true, lv, new Vector2(0f, CardY), CardSize, Pin.Top, false);
            if (_meta.HasTrial(e)) StudyCard.Tag(card, "试用", InkTheme.Teal);

            if (_list.Count > 1)
            {
                UiKit.Btn(_body, "prev", "<", new Vector2(-220f, CardY + 100f), new Vector2(76f, 90f), () => Step(-1), false, Pin.Top);
                UiKit.Btn(_body, "next", ">", new Vector2(220f, CardY + 100f), new Vector2(76f, 90f), () => Step(1), false, Pin.Top);
            }

            string wake = e.Kind == CodexKind.Glyph ? CodexCatalog.WakeNote((CardId)e.Index) : CodexCatalog.WakeNote((WordId)e.Index);
            var note = UiKit.Label(_body, "note", StudyCatalog.FamilyName(e) + " · " + wake, 21,
                new Vector2(0f, 384f), new Vector2(560f, 30f), TextAnchor.MiddleCenter, Pin.Top);
            note.color = InkTheme.TextMid;
            string lore = e.Kind == CodexKind.Glyph ? CodexCatalog.Lore((CardId)e.Index) : CodexCatalog.WordLore((WordId)e.Index);
            var para = UiKit.Label(_body, "lore", lore, 21, new Vector2(0f, 418f), new Vector2(560f, 86f),
                TextAnchor.UpperCenter, Pin.Top);
            para.horizontalOverflow = HorizontalWrapMode.Wrap;
            para.verticalOverflow = VerticalWrapMode.Truncate;
            para.color = InkTheme.TextDark;

            BuildBonus(lv);
            BuildSoon();
            BuildActions(e, lv);
        }

        void BuildBonus(int lv)
        {
            var box = UiKit.Stroke(_body, "bonus", new Vector2(0f, 512f), new Vector2(580f, 164f), Pin.Top, 4f,
                fill: InkTheme.Hex("FFF6E2"), radius: 22f);
            StudyCatalog.Tier tier = StudyCatalog.TierOf(lv);
            var head = UiKit.Label(box, "h", (lv >= 1 ? StudyCatalog.TierName(tier) + "品质 · " : "") + "带着它的炮弹 · 伤害加成",
                22, new Vector2(0f, 56f), new Vector2(560f, 32f));
            head.color = lv >= 1 ? StudyCatalog.TierDeep(tier) : InkTheme.TextMid;
            if (lv >= 1) UiKit.Bold(head);
            int now = Mathf.RoundToInt(StudyCatalog.BonusOf(lv) * 100f);
            if (lv >= StudyCatalog.MaxLevel)
            {
                var full = UiKit.Label(box, "v", "+" + now + "%", 50, new Vector2(0f, 6f), new Vector2(300f, 60f));
                full.color = InkTheme.Seal;
                UiKit.Bold(full);
            }
            else
            {
                int next = Mathf.RoundToInt(StudyCatalog.BonusOf(lv + 1) * 100f);
                var a = UiKit.Label(box, "a", "+" + now + "%", 46, new Vector2(-110f, 6f), new Vector2(200f, 60f));
                UiKit.Bold(a);
                var arrow = UiKit.Label(box, "arrow", "→", 40, new Vector2(0f, 6f), new Vector2(60f, 60f));
                arrow.color = InkTheme.TextMid;
                var b = UiKit.Label(box, "b", "+" + next + "%", 46, new Vector2(110f, 6f), new Vector2(200f, 60f));
                b.color = InkTheme.Seal;
                UiKit.Bold(b);
            }

            int cap = _meta.StudyCap;
            const float step = 50f;
            for (int i = 0; i < StudyCatalog.MaxLevel; i++)
            {
                Vector2 at = new Vector2((i - (StudyCatalog.MaxLevel - 1) * 0.5f) * step, -50f);
                var ring = UiKit.Icon(box, UiSprites.Disc(), at, 34f);
                ring.name = "dot" + i;
                ring.color = i < cap ? InkTheme.Outline : InkTheme.LineDim;
                var fill = UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 26f);
                fill.color = i < lv ? StudyCatalog.TierColor(StudyCatalog.TierOf(i + 1))
                    : i < cap ? InkTheme.CardFace : InkTheme.CardDim;
                if (i == lv && i < cap) _anim.Breathe(ring.transform, 0f, 0.08f, 1.1f);
            }
        }

        // 第二版的插槽、第三版的特效先摆个位置，让人知道后面还有。
        void BuildSoon()
        {
            string[] names = { "插槽", "攻击特效" };
            for (int i = 0; i < names.Length; i++)
            {
                var chip = UiKit.Stroke(_body, "soon" + i, new Vector2((i - 0.5f) * 280f, 690f), new Vector2(260f, 50f),
                    Pin.Top, 3f, InkTheme.LineDim, InkTheme.CardDim, 25f);
                var t = UiKit.Label(chip, "t", names[i] + " · 即将开放", 21, Vector2.zero, new Vector2(260f, 50f));
                t.color = InkTheme.TextDim;
            }
        }

        void BuildActions(CodexEntry e, int lv)
        {
            MetaProgress.StudyBlock block = _meta.UpgradeBlock(e);
            int cost = StudyCatalog.CostOf(lv);
            if (lv < StudyCatalog.MaxLevel)
            {
                var line = UiKit.Panel(_body, "cost", new Vector2(0f, 758f), new Vector2(460f, 44f), Color.clear, Pin.Top);
                line.GetComponent<Image>().raycastTarget = false;
                Color ink = _meta.Foil >= cost ? InkTheme.TextDark : InkTheme.Seal;
                UiKit.Icon(line, FoilArt.Icon, new Vector2(-190f, 0f), 36f);
                var n = UiKit.Label(line, "n", cost.ToString(), 26, new Vector2(-118f, 0f),
                    new Vector2(76f, 40f), TextAnchor.MiddleLeft);
                n.color = ink;
                UiKit.Bold(n);
                var own = UiKit.Label(line, "own", "（拥有 " + _meta.Foil + "）", 22, new Vector2(48f, 0f),
                    new Vector2(210f, 40f), TextAnchor.MiddleLeft);
                own.color = ink;
            }

            string label = block == MetaProgress.StudyBlock.Max ? "已满级"
                : block == MetaProgress.StudyBlock.Cap ? "Lv." + _meta.StudyCap + " 已到上限"
                : "升级";
            var up = UiKit.Btn(_body, "up", label, new Vector2(0f, 804f), new Vector2(380f, 100f), Upgrade, true, Pin.Top);
            PanelKit.Dim(up, block != MetaProgress.StudyBlock.None);
            if (block == MetaProgress.StudyBlock.None) _anim.Breathe(up.transform, 0.2f, 0.035f, 1.2f);

            bool trial = _meta.HasTrial(e);
            string tl = trial ? "今日有效 · 用到才算"
                : "试用 +" + StudyCatalog.TrialBonus + " 级 · 当天有效（剩 " + _meta.TrialLeft + "）";
            var ad = UiKit.Btn(_body, "trial", tl, new Vector2(0f, 924f), new Vector2(440f, 84f), Trial, false, Pin.Top);
            var adText = ad.GetComponentInChildren<Text>();
            if (adText != null) adText.fontSize = 24;
            if (!trial) ResultKit.AdMark(ad, 34f);
            PanelKit.Dim(ad, !_meta.CanTrial(e), false);
        }

        void Step(int d)
        {
            if (Time.unscaledTime < _busyUntil) return;
            AudioBus.Tap();
            _at = (_at + d + _list.Count) % _list.Count;
            _anim.Clear();
            Paint();
        }

        void Upgrade()
        {
            // 蓄力那 0.3 秒里旧卡还在，连点或翻页会让爆点落到别的卡上。
            if (Time.unscaledTime < _busyUntil) return;
            CodexEntry e = Cur;
            MetaProgress.StudyBlock block = _meta.UpgradeBlock(e);
            if (block != MetaProgress.StudyBlock.None)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, BlockTip(block));
                return;
            }
            int was = _meta.StudyLevel(e);
            if (!_meta.UpgradeStudy(e)) return;
            int lv = _meta.StudyLevel(e);
            _busyUntil = Time.unscaledTime + StudyUpFx.Hit + 0.05f;
            AudioBus.StudyUp();
            _anim.Clear();
            var up = _body.Find("up");
            if (up != null) up.localScale = Vector3.one;
            bool tierUp = StudyCatalog.TierOf(lv) != StudyCatalog.TierOf(was);
            StudyUpFx.Play(_board, Vector2.zero, _body.Find("c") as RectTransform, _body.Find("cost/icon") as RectTransform,
                lv, tierUp, () => Reveal(lv));
        }

        // 爆点那一下换成新卡面，加成数字和新点亮的那颗等级点跟着跳。
        RectTransform Reveal(int lv)
        {
            Paint();
            // 重绘会把新卡面盖到演出上面，品质横幅要留在最上层。
            Transform fx = _board.Find("study_up_fx");
            if (fx != null) fx.SetAsLastSibling();
            Transform bonus = _body.Find("bonus");
            if (bonus != null)
            {
                Transform num = bonus.Find(lv >= StudyCatalog.MaxLevel ? "v" : "a");
                if (num != null) _anim.Punch(num, 0.05f, 0.3f, 0.4f);
                if (bonus.Find("dot" + (lv - 1)) is RectTransform dot)
                {
                    _anim.Pop(dot, 0.12f, 0.42f, 0.2f);
                    _anim.At(0.12f, () =>
                    {
                        if (dot == null) return;
                        UiConfetti.Sparks(_body, StudyUpFx.Local(_body, dot),
                            StudyCatalog.TierColor(StudyCatalog.TierOf(lv)), 8, 300f);
                    });
                }
            }
            return _body.Find("c") as RectTransform;
        }

        string BlockTip(MetaProgress.StudyBlock block)
        {
            switch (block)
            {
                case MetaProgress.StudyBlock.Max: return "已经满级了";
                case MetaProgress.StudyBlock.Cap:
                    int ch = _meta.ClearedCount() / GameConstants.ChapterSize + 1;
                    return "通关第" + ch + "章，等级上限 +1";
                case MetaProgress.StudyBlock.Foil: return "金箔不够，点上面的金箔能看到怎么拿";
                case MetaProgress.StudyBlock.NotOwned: return "还没在关卡里用过它";
                default: return "通关第四章开放字卡";
            }
        }

        void Trial()
        {
            CodexEntry e = Cur;
            if (_meta.HasTrial(e))
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "今天打出来才算用掉，不用就还在，过了今天会清掉");
                return;
            }
            if (!_meta.CanTrial(e))
            {
                AudioBus.Deny();
                string waiting = _meta.TrialWaiting ? "「" + _meta.TrialTitle() + "」还没打出来，" : "";
                InkToast.Show(_layer, waiting + "今天的试用次数用完了");
                return;
            }
            if (_meta.TrialWaiting)
            {
                string now = CodexCatalog.Title(e);
                string old = _meta.TrialTitle();
                ConfirmPanel.Ask(_layer,
                    "「" + old + "」的试用还没打出来。\n换成「" + now + "」会清掉它，并再扣一次。\n今天不用，过了今天也会清掉。",
                    "先去打", "还是要换", () => WatchTrial(e));
                return;
            }
            WatchTrial(e);
        }

        void WatchTrial(CodexEntry e)
        {
            AdStub.Reward("glyph_trial", () =>
            {
                _meta.GrantTrial(e);
                if (this == null) return;
                InkToast.Show(_layer, CodexCatalog.Title(e) + " 今日 +" + StudyCatalog.TrialBonus + " 级，用到才算");
                _anim.Clear();
                Paint();
            });
        }
    }
}
