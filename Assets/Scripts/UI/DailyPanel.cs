using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 每日功课先下线，首页不再有入口。面板留着，MetaProgress.TasksLive 打开后还能用。
    // 每日副本（残局）在副本页，见 DailyPuzzlePanel。
    public sealed class DailyPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 570f;
        const float CardW = BoardW - 48f;

        static readonly Color Jade = DungeonPageBuilder.Jade;

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _body;
        float _top;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "daily_panel";
            var panel = dim.gameObject.AddComponent<DailyPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel._top = BoardH * 0.5f;
            var board = PanelKit.Board(dim, "每日任务", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, panel.Close);
            panel._body = ResultKit.Group(board, "body", Vector2.zero, new Vector2(BoardW, BoardH));
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
            BuildTasks();
        }

        // ---------- 每日功课 ----------

        const float RowH = 88f;

        void BuildTasks()
        {
            const float top = 112f;
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
    }
}
