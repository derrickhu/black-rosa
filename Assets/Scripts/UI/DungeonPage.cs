using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 副本页：每日副本（全服同一局残局）和无尽副本（冲层）两张卡。
    // 卡上只放一眼要看的：换新倒计时、词缀、最好成绩、次数。点「进入」才打开详情弹窗开打。
    public sealed class DungeonPage : MonoBehaviour
    {
        public DungeonCard Daily;
        public DungeonCard Endless;

        static readonly Color Dimmed = new Color(0.58f, 0.58f, 0.60f, 1f);
        static readonly Color GoOff = InkTheme.Hex("B9B2A8");

        public void Bind(MetaProgress meta, Action openDaily, Action openEndless)
        {
            BindDaily(meta, openDaily);
            BindEndless(meta, openEndless);
        }

        public static bool HasPlay(MetaProgress meta) =>
            (meta.DailyOpen && meta.DailyPlaysLeft > 0) || (meta.EndlessOpen && meta.EndlessPlaysLeft > 0);

        void BindDaily(MetaProgress meta, Action open)
        {
            DungeonCard c = Daily;
            if (c == null) return;
            CdnAssets.Bind(c.Art, "Ui/dungeon_daily");
            if (!meta.DailyOpen)
            {
                Locked(c, "全服每天同一局，开局只留几格，禁掉两个字",
                    "通关第" + GameConstants.ChapterName(GameConstants.DailyOpenChapter) + "章开放");
            }
            else
            {
                StageDef s = StageCatalog.DailyStage(DayClock.Today, DayClock.Week);
                int hours = Mathf.Max(1, Mathf.CeilToInt(DayClock.SecondsToTomorrow / 3600f));
                int best = meta.DailyBestToday;
                Open(c, DungeonPageBuilder.Jade, $"{hours} 小时后换新",
                    "今日 · " + s.Rule.Name + "：" + s.Rule.Desc,
                    best > 0 ? $"今日最好 第 {best} 波" : "今天还没打",
                    $"今日次数 {meta.DailyPlaysLeft}/{meta.DailyPlaysCap}",
                    meta.DailyPlaysLeft > 0);
            }
            Wire(c, open);
        }

        void BindEndless(MetaProgress meta, Action open)
        {
            DungeonCard c = Endless;
            if (c == null) return;
            CdnAssets.Bind(c.Art, "Ui/dungeon_endless");
            if (!meta.EndlessOpen)
            {
                Locked(c, $"{StageCatalog.EndlessWaves} 波冲层，每周换词缀、全服冲榜",
                    "通关第" + GameConstants.ChapterName(GameConstants.EndlessOpenChapter) + "章开放");
            }
            else
            {
                CdnAssets.Prefetch(StageCatalog.EndlessBackdrop);
                CdnAssets.Prefetch(StageCatalog.EndlessDarkBackdrop);
                StageRule rule = StageRules.ThisWeek;
                int days = DayClock.DaysLeftInWeek;
                int week = meta.EndlessWeek == DayClock.Week ? meta.EndlessWeekBest : 0;
                string ever = meta.EndlessBest > 0 ? $" · 历史最好 {meta.EndlessBest} 波" : "";
                Open(c, DungeonPageBuilder.Grape, days <= 1 ? "今晚零点换词缀" : $"{days} 天后换词缀",
                    "本周 · " + rule.Name + "：" + rule.Desc,
                    week > 0 ? $"本周最好 第 {week} 波" : "本周还没打",
                    $"今日次数 {meta.EndlessPlaysLeft}/{meta.EndlessPlaysCap}{ever}",
                    meta.EndlessPlaysLeft > 0);
            }
            Wire(c, open);
        }

        static void Open(DungeonCard c, Color accent, string chip, string rule, string best, string plays, bool ready)
        {
            if (c.Art != null) c.Art.color = Color.white;
            if (c.Lock != null) c.Lock.gameObject.SetActive(false);
            if (c.Chip != null)
            {
                c.Chip.gameObject.SetActive(true);
                c.Chip.color = accent;
            }
            Write(c.ChipLabel, chip);
            Write(c.Rule, rule);
            Write(c.Best, best);
            Write(c.Plays, plays);
            if (c.Plays != null) c.Plays.gameObject.SetActive(true);
            if (c.Go != null && c.Go.targetGraphic != null) c.Go.targetGraphic.color = accent;
            Write(c.GoLabel, "进入");
            if (c.Dot != null) c.Dot.gameObject.SetActive(ready);
            Breathe(c, ready);
        }

        static void Locked(DungeonCard c, string rule, string when)
        {
            if (c.Art != null) c.Art.color = Dimmed;
            if (c.Lock != null) c.Lock.gameObject.SetActive(true);
            if (c.Chip != null) c.Chip.gameObject.SetActive(false);
            Write(c.Rule, rule);
            Write(c.Best, when);
            if (c.Plays != null) c.Plays.gameObject.SetActive(false);
            if (c.Go != null && c.Go.targetGraphic != null) c.Go.targetGraphic.color = GoOff;
            Write(c.GoLabel, "未开放");
            if (c.Dot != null) c.Dot.gameObject.SetActive(false);
            Breathe(c, false);
        }

        // 重绑时先丢掉上一次的呼吸，免得叠成好几条轨。
        static void Breathe(DungeonCard c, bool on)
        {
            if (c.Go == null) return;
            UiAnim anim = c.Go.GetComponent<UiAnim>();
            if (anim != null) anim.Clear();
            c.Go.transform.localScale = Vector3.one;
            if (on) UiAnim.On(c.Go).Breathe(c.Go.transform, 0.3f, 0.035f, 1.1f);
        }

        // 没开也照样能点：弹窗入口自己会提示开放条件。
        static void Wire(DungeonCard c, Action open)
        {
            if (c.Go != null)
            {
                c.Go.onClick.RemoveAllListeners();
                c.Go.onClick.AddListener(() => open?.Invoke());
            }
            if (c.ArtButton != null)
            {
                c.ArtButton.onClick.RemoveAllListeners();
                c.ArtButton.onClick.AddListener(() => open?.Invoke());
            }
        }

        static void Write(Text t, string s)
        {
            if (t != null) t.text = s;
        }
    }
}
