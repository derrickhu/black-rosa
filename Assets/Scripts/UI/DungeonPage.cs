using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 副本页：每日副本（全服同一局残局）和无尽副本（冲层）两张卡。
    // 卡上只放一眼要看的：结束时间、奖励框、剩余次数。点「进入」才打开详情弹窗开打。
    public sealed class DungeonPage : MonoBehaviour
    {
        public DungeonCard Daily;
        public DungeonCard Endless;

        static readonly Color Dimmed = new Color(0.58f, 0.58f, 0.60f, 1f);
        static readonly Color GoOff = new Color(0.72f, 0.72f, 0.72f, 1f);

        MetaProgress _meta;
        Action _openDaily;
        Action _openEndless;
        Action _changed;

        public void Bind(MetaProgress meta, Action openDaily, Action openEndless, Action changed = null)
        {
            _meta = meta;
            _openDaily = openDaily;
            _openEndless = openEndless;
            _changed = changed;
            BindDaily();
            BindEndless();
        }

        public static bool HasPlay(MetaProgress meta) =>
            (meta.DailyOpen && meta.DailyPlaysLeft > 0) || (meta.EndlessOpen && meta.EndlessPlaysLeft > 0);

        void BindDaily()
        {
            DungeonCard c = Daily;
            if (c == null || _meta == null) return;
            CdnAssets.Bind(c.Art, "Ui/dungeon_daily");
            if (!_meta.DailyOpen)
            {
                Locked(c, "通关第" + GameConstants.ChapterName(GameConstants.DailyOpenChapter) + "章开放");
            }
            else
            {
                int hours = Mathf.Max(1, Mathf.CeilToInt(DayClock.SecondsToTomorrow / 3600f));
                Open(c, hours + "小时后结束",
                    "剩余次数：" + _meta.DailyPlaysLeft + "/" + _meta.DailyPlaysCap,
                    _meta.DailyPlaysLeft > 0,
                    !_meta.DailyAdDone);
            }
            Wire(c, _openDaily, OnDailyAd);
        }

        void BindEndless()
        {
            DungeonCard c = Endless;
            if (c == null || _meta == null) return;
            CdnAssets.Bind(c.Art, "Ui/dungeon_endless");
            if (!_meta.EndlessOpen)
            {
                Locked(c, "通关第" + GameConstants.ChapterName(GameConstants.EndlessOpenChapter) + "章开放");
            }
            else
            {
                CdnAssets.Prefetch(StageCatalog.EndlessBackdrop);
                CdnAssets.Prefetch(StageCatalog.EndlessDarkBackdrop);
                int days = DayClock.DaysLeftInWeek;
                Open(c, days <= 1 ? "今晚结束" : days + "天后结束",
                    "剩余次数：" + _meta.EndlessPlaysLeft + "/" + _meta.EndlessPlaysCap,
                    _meta.EndlessPlaysLeft > 0,
                    !_meta.EndlessAdDone);
            }
            Wire(c, _openEndless, OnEndlessAd);
        }

        static void Open(DungeonCard c, string chip, string plays, bool ready, bool plus)
        {
            Repair(c);
            if (c.Art != null) c.Art.color = Color.white;
            if (c.Lock != null) c.Lock.gameObject.SetActive(false);
            if (c.Chip != null) c.Chip.gameObject.SetActive(true);
            Write(c.ChipLabel, chip);
            Write(c.Plays, plays);
            if (c.PlaysBar != null) c.PlaysBar.gameObject.SetActive(true);
            DungeonPageBuilder.PlacePlays(c, plus);
            if (c.Go != null && c.Go.targetGraphic != null)
                c.Go.targetGraphic.color = ready ? Color.white : GoOff;
            Write(c.GoLabel, "进入");
            if (c.Dot != null) c.Dot.gameObject.SetActive(ready);
            Still(c);
        }

        static void Locked(DungeonCard c, string when)
        {
            Repair(c);
            if (c.Art != null) c.Art.color = Dimmed;
            if (c.Lock != null) c.Lock.gameObject.SetActive(true);
            if (c.Chip != null) c.Chip.gameObject.SetActive(false);
            if (c.PlaysBar != null) c.PlaysBar.gameObject.SetActive(true);
            Write(c.Plays, when);
            DungeonPageBuilder.PlacePlays(c, false);
            if (c.Go != null && c.Go.targetGraphic != null) c.Go.targetGraphic.color = GoOff;
            Write(c.GoLabel, "未开放");
            if (c.Dot != null) c.Dot.gameObject.SetActive(false);
            Still(c);
        }

        // 圆角色块是运行时烘的，预制里存不住。每次都盖上，避免旧的半圆档还留在图上。
        static void Paint(Image img, Sprite sprite)
        {
            if (img == null || sprite == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }

        static void Repair(DungeonCard c)
        {
            if (c == null) return;
            if (c.Frame != null)
            {
                string[] slotKeys = { "Ui/modal_slot_60_green", "Ui/modal_slot_60_blue", "Ui/modal_slot_60_purple" };
                for (int i = 0; i < slotKeys.Length; i++)
                {
                    var si = c.Frame.transform.Find("slot" + i)?.GetComponent<Image>();
                    Sprite s = InkSprites.Load(slotKeys[i]);
                    if (si != null && s != null) si.sprite = s;
                }
            }
            Paint(c.Chip, UiSprites.FillExact(8));
            Paint(c.PlaysBar, UiSprites.FillExact(8));
            if (c.Plus == null) return;
            Paint(c.Plus.GetComponent<Image>(), UiSprites.FillExact(6));
            Paint(c.Plus.transform.Find("line")?.GetComponent<Image>(), UiSprites.LineExact(6, 2));
        }

        // 进入键不呼吸，只把旧的缩放轨清掉。
        static void Still(DungeonCard c)
        {
            if (c.Go == null) return;
            UiAnim anim = c.Go.GetComponent<UiAnim>();
            if (anim != null) anim.Clear();
            c.Go.transform.localScale = Vector3.one;
        }

        // 没开也照样能点：弹窗入口自己会提示开放条件。
        static void Wire(DungeonCard c, Action open, Action ad)
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
            if (c.Plus != null)
            {
                c.Plus.onClick.RemoveAllListeners();
                c.Plus.onClick.AddListener(() => ad?.Invoke());
            }
        }

        void OnDailyAd()
        {
            AudioBus.Tap();
            AdStub.Reward("daily_plays", () =>
            {
                if (this == null || _meta == null || !_meta.GrantDailyAdPlays()) return;
                if (_changed != null) _changed();
                else BindDaily();
            });
        }

        void OnEndlessAd()
        {
            AudioBus.Tap();
            AdStub.Reward("endless_plays", () =>
            {
                if (this == null || _meta == null || !_meta.GrantEndlessAdPlays()) return;
                if (_changed != null) _changed();
                else BindEndless();
            });
        }

        static void Write(Text t, string s)
        {
            if (t != null) t.text = s;
        }
    }
}
