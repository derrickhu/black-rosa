using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 每日页：上半张是今日残局（全服同一局：词缀、开局摆好的字、禁用的字、今日最好），
    // 下半张是三条每日功课和全领奖励。领奖、看广告加次数之后整页重画。
    public sealed class DailyPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 1020f;
        // 残局没开时上半张只剩一张小锁卡，整页矮一截。
        const float LockedH = 790f;
        const float CardW = BoardW - 48f;
        const float Tile = 66f;

        static readonly Color Jade = InkTheme.Hex("2F8F6B");
        static readonly Color JadeDeep = InkTheme.Hex("1E6149");
        static readonly Color JadeFace = InkTheme.Hex("EAF6EF");
        static readonly Color Gold = InkTheme.Hex("F6BE3C");

        MetaProgress _meta;
        Action _start;
        Action _changed;
        RectTransform _layer;
        RectTransform _body;
        float _top;

        public static void Show(RectTransform layer, MetaProgress meta, Action start, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "daily_panel";
            var panel = dim.gameObject.AddComponent<DailyPanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            panel._layer = layer;
            float h = meta.DailyOpen ? BoardH : LockedH;
            panel._top = h * 0.5f;
            var board = PanelKit.Board(dim, "每日", Vector2.zero, new Vector2(BoardW, h), Pin.Center, panel.Close);
            panel._body = ResultKit.Group(board, "body", Vector2.zero, new Vector2(BoardW, h));
            panel.Refresh();
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        float Y(float fromTop) => _top - fromTop;

        void Refresh()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);
            int hours = Mathf.Max(1, Mathf.CeilToInt(DayClock.SecondsToTomorrow / 3600f));
            var reset = UiKit.Label(_body, "reset", $"{hours} 小时后换新", 20, new Vector2(0f, Y(96f)), new Vector2(400f, 28f));
            reset.color = InkTheme.TextMid;
            if (_meta.DailyOpen) BuildPuzzle();
            else BuildLocked();
            BuildTasks();
        }

        // ---------- 今日残局 ----------

        const float PuzzleTop = 124f;
        const float PuzzleH = 470f;

        void BuildLocked()
        {
            var card = Card("puzzle", PuzzleTop, 200f, JadeFace);
            var t = UiKit.Label(card, "t", "每日残局", 30, new Vector2(0f, 40f), new Vector2(CardW, 40f));
            UiKit.Bold(t);
            t.color = JadeDeep;
            var n = UiKit.Label(card, "n", $"通关第{ChapterName(GameConstants.DailyOpenChapter)}章开放", 24,
                new Vector2(0f, -14f), new Vector2(CardW, 34f));
            n.color = InkTheme.TextMid;
            var m = UiKit.Label(card, "m", "全服每天同一局，开局摆好字、禁掉两个字，比谁撑得久", 20,
                new Vector2(0f, -54f), new Vector2(CardW - 40f, 30f));
            m.color = InkTheme.TextMid;
        }

        void BuildPuzzle()
        {
            StageDef s = StageCatalog.DailyStage(DayClock.Today, DayClock.Week);
            var card = Card("puzzle", PuzzleTop, PuzzleH, JadeFace);
            float top = PuzzleH * 0.5f;

            // 词缀小牌 + 效果
            var tag = UiKit.Stroke(card, "tag", new Vector2(-CardW * 0.5f + 100f, top - 46f), new Vector2(160f, 50f),
                Pin.Center, 3f, null, Jade, 14f);
            tag.GetComponent<Image>().raycastTarget = false;
            var tn = UiKit.Label(tag, "t", "今日 · " + s.Rule.Name, 22, Vector2.zero, new Vector2(160f, 50f));
            tn.color = InkTheme.CardFace;
            UiKit.Bold(tn);
            var desc = UiKit.Label(card, "desc", s.Rule.Desc, 21, new Vector2(80f, top - 46f), new Vector2(380f, 40f),
                TextAnchor.MiddleLeft);
            desc.color = JadeDeep;

            // 开局字
            Caption(card, "开局已摆好", new Vector2(-CardW * 0.5f + 28f, top - 98f));
            int n = s.Preset != null ? s.Preset.Length : 0;
            for (int i = 0; i < n; i++)
                Glyph(card, s.Preset[i].Id, s.Preset[i].Star, new Vector2(-CardW * 0.5f + 64f + i * (Tile + 18f), top - 148f), false);

            // 禁用字
            Caption(card, "今日禁用", new Vector2(CardW * 0.5f - 200f, top - 98f));
            int b = s.Banned != null ? s.Banned.Length : 0;
            for (int i = 0; i < b; i++)
                Glyph(card, s.Banned[i], 0, new Vector2(CardW * 0.5f - 164f + i * (Tile + 18f), top - 148f), true);

            // 成绩
            int best = _meta.DailyBestToday;
            var info = UiKit.Label(card, "info", $"共 {StageCatalog.DailyWaves} 波 · 第 {StageCatalog.DailyBossEvery} / {StageCatalog.DailyWaves} 波各有首领",
                20, new Vector2(0f, top - 212f), new Vector2(CardW - 40f, 28f));
            info.color = InkTheme.TextMid;
            var bestT = ResultKit.Headline(card, "best", best > 0 ? $"今日最好 第 {best} 波" : "今天还没打",
                40, new Vector2(0f, top - 258f), Gold, InkTheme.Outline, 3f);
            bestT.rectTransform.sizeDelta = new Vector2(CardW - 40f, 56f);
            var chest = UiKit.Label(card, "chest", $"撑过 {StageCatalog.DailyBossEvery} 波得木箱，{StageCatalog.DailyWaves} 波全守住得银箱", 20,
                new Vector2(0f, top - 302f), new Vector2(CardW - 40f, 28f));
            chest.color = InkTheme.TextMid;

            // 次数 + 广告 + 排行
            int left = _meta.DailyPlaysLeft;
            bool ad = !_meta.DailyAdDone;
            var plays = UiKit.Label(card, "plays", $"今日次数 {left}/{_meta.DailyPlaysCap}", 24,
                new Vector2(ad ? -150f : 0f, top - 350f), new Vector2(240f, 34f));
            UiKit.Bold(plays);
            if (ad)
            {
                var adBtn = UiKit.Btn(card, "ad", $"+{GameConstants.DailyAdPlays} 次", new Vector2(130f, top - 350f),
                    new Vector2(200f, 64f), OnAd, false);
                ResultKit.AdMark(adBtn, 30f);
            }

            UiKit.Btn(card, "rank", "排行", new Vector2(-180f, top - 420f), new Vector2(150f, 72f), OnRank, false);
            var go = UiKit.Btn(card, "go", "开始", new Vector2(70f, top - 420f), new Vector2(280f, 84f), OnStart, true);
            if (left <= 0) PanelKit.Dim(go, true);
            else
            {
                UiKit.PaintBtn(go, Jade, JadeDeep, InkTheme.CardFace);
                UiAnim.On(go).Breathe(go.transform, 0.3f, 0.035f, 1.1f);
            }
        }

        static void Caption(RectTransform card, string text, Vector2 pos)
        {
            var c = UiKit.Label(card, "cap", text, 20, pos + new Vector2(90f, 0f), new Vector2(180f, 28f), TextAnchor.MiddleLeft);
            c.color = InkTheme.TextMid;
            UiKit.Bold(c);
        }

        // 一枚字方块：字色描边、宣纸底，星级挂在下沿。禁用的压灰、斜一道朱线。
        static void Glyph(RectTransform card, CardId id, int star, Vector2 pos, bool banned)
        {
            Color accent = banned ? InkTheme.Hex("9A928A") : CardCatalog.Accent(id);
            var tile = UiKit.Stroke(card, "g" + (int)id, pos, new Vector2(Tile, Tile), Pin.Center, 4f, accent,
                banned ? InkTheme.Hex("E9E4DD") : InkTheme.CardFace, 14f);
            tile.GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Label(tile, "t", CardCatalog.Get(id).Name, 38, new Vector2(0f, 2f), new Vector2(Tile, Tile));
            t.color = accent;
            UiKit.Bold(t);
            if (banned)
            {
                var slash = UiKit.Panel(tile, "x", Vector2.zero, new Vector2(Tile * 1.1f, 5f), InkTheme.Seal);
                slash.localRotation = Quaternion.Euler(0f, 0f, -38f);
                slash.GetComponent<Image>().raycastTarget = false;
                return;
            }
            for (int i = 0; i < star; i++)
            {
                float x = (i - (star - 1) * 0.5f) * 20f;
                UiKit.Icon(tile, InkSprites.Load("Ui/result_star_on"), new Vector2(x, -Tile * 0.5f - 2f), 22f);
            }
        }

        void OnAd()
        {
            AudioBus.Tap();
            AdStub.Reward("daily_plays", () =>
            {
                if (this == null || !_meta.GrantDailyAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }

        void OnRank()
        {
            AudioBus.Tap();
            RankPanel.Show(_layer, _meta, RankService.BoardDaily);
        }

        void OnStart()
        {
            if (_meta.DailyPlaysLeft <= 0)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.DailyAdDone
                    ? "今天的残局次数用完了，明天换一局"
                    : $"今天的次数用完了，看广告再开 {GameConstants.DailyAdPlays} 局");
                return;
            }
            AudioBus.Tap();
            Action start = _start;
            Destroy(gameObject);
            start?.Invoke();
        }

        // ---------- 每日功课 ----------

        const float RowH = 88f;

        void BuildTasks()
        {
            float top = _meta.DailyOpen ? PuzzleTop + PuzzleH + 18f : PuzzleTop + 226f;
            var head = UiKit.Label(_body, "taskhead", "每 日 功 课", 24, new Vector2(0f, Y(top + 18f)), new Vector2(300f, 32f));
            UiKit.Bold(head);
            head.color = InkTheme.TextDark;
            for (int side = -1; side <= 1; side += 2)
            {
                var line = UiKit.Panel(_body, "rule" + side, new Vector2(side * 160f, Y(top + 18f)), new Vector2(120f, 3f),
                    InkTheme.Hex("E6D8C2"));
                line.GetComponent<Image>().raycastTarget = false;
            }

            float y = top + 44f;
            for (int i = 0; i < GameConstants.TaskCount; i++)
            {
                TaskRow(i, y + RowH * 0.5f);
                y += RowH + 8f;
            }
            BonusRow(y + 34f);
        }

        void TaskRow(int i, float fromTop)
        {
            int code = _meta.TaskCode(i);
            int need = TaskCatalog.Need(code);
            int prog = _meta.TaskProgress(i);
            bool done = prog >= need;
            bool claimed = _meta.TaskClaimed(i);

            var row = UiKit.Stroke(_body, "task" + i, new Vector2(0f, Y(fromTop)), new Vector2(CardW, RowH), Pin.Center, 3f,
                done && !claimed ? InkTheme.Seal : (Color?)null, claimed ? InkTheme.Hex("F1ECE4") : InkTheme.CardFace, 18f);
            row.GetComponent<Image>().raycastTarget = false;
            float left = -CardW * 0.5f;

            var text = UiKit.Label(row, "t", TaskCatalog.Text(code), 24, new Vector2(left + 24f + 170f, 16f),
                new Vector2(340f, 32f), TextAnchor.MiddleLeft);
            text.color = claimed ? InkTheme.TextMid : InkTheme.TextDark;
            UiKit.Bold(text);

            var bar = ResultKit.Bar(row, new Vector2(left + 24f + 120f, -18f), 240f, 22f, done ? Jade : InkTheme.Hex("F58A34"));
            ResultKit.SetBar(bar, need > 0 ? prog / (float)need : 1f);
            var pv = UiKit.Label(row, "p", $"{prog}/{need}", 20, new Vector2(left + 24f + 290f, -18f), new Vector2(80f, 28f),
                TextAnchor.MiddleLeft);
            pv.color = InkTheme.TextMid;

            float bx = CardW * 0.5f - 90f;
            if (claimed)
            {
                var got = UiKit.Label(row, "got", "已领", 24, new Vector2(bx, 0f), new Vector2(140f, 40f));
                got.color = InkTheme.TextMid;
                UiKit.Bold(got);
                return;
            }
            int ix = i;
            var btn = UiKit.Btn(row, "claim", "墨 +" + _meta.TaskInk, new Vector2(bx, 0f), new Vector2(150f, 64f),
                () => OnClaim(ix), done);
            if (!done) PanelKit.Dim(btn, true, false);
        }

        void BonusRow(float fromTop)
        {
            bool claimed = _meta.TaskBonusClaimed;
            bool ready = _meta.TaskBonusReady;
            float x = -CardW * 0.5f + 40f;
            UiKit.Icon(_body, InkSprites.Ui("diamond"), new Vector2(x + 20f, Y(fromTop)), 52f);
            var t = UiKit.Label(_body, "bonus", claimed ? "今天的功课全部完成" : $"三项全领，再得钻石 {GameConstants.TaskBonusDiamond}", 22,
                new Vector2(x + 60f + 160f, Y(fromTop)), new Vector2(320f, 32f), TextAnchor.MiddleLeft);
            t.color = InkTheme.TextDark;
            UiKit.Bold(t);
            if (claimed) return;
            var btn = UiKit.Btn(_body, "bonusbtn", "领取", new Vector2(CardW * 0.5f - 90f, Y(fromTop)), new Vector2(150f, 64f),
                OnBonus, ready);
            if (!ready) PanelKit.Dim(btn, true, false);
        }

        void OnClaim(int i)
        {
            if (!_meta.TaskDone(i) || _meta.TaskClaimed(i))
            {
                AudioBus.Deny();
                if (!_meta.TaskDone(i)) InkToast.Show(_layer, "还没做完，打一局就能涨进度");
                return;
            }
            int ink = _meta.ClaimTask(i);
            if (ink <= 0) return;
            AudioBus.Stamp();
            InkToast.Show(_layer, "墨 +" + ink);
            _changed?.Invoke();
            Refresh();
        }

        void OnBonus()
        {
            if (!_meta.TaskBonusReady)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "三项功课都领完才能拿");
                return;
            }
            int gem = _meta.ClaimTaskBonus();
            if (gem <= 0) return;
            AudioBus.Stamp();
            InkToast.Show(_layer, "钻石 +" + gem);
            _changed?.Invoke();
            Refresh();
        }

        RectTransform Card(string name, float fromTop, float h, Color fill)
        {
            var card = UiKit.Stroke(_body, name, new Vector2(0f, Y(fromTop + h * 0.5f)), new Vector2(CardW, h), Pin.Center, 4f,
                null, fill, 22f);
            card.GetComponent<Image>().raycastTarget = false;
            return card;
        }

        static string ChapterName(int chapter)
        {
            string[] cn = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            return chapter >= 0 && chapter < cn.Length ? cn[chapter] : (chapter + 1).ToString();
        }
    }
}
