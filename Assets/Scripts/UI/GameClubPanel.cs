using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 游戏圈每日任务：发一条帖子领当天抽好的墨和宝箱，通关第四章后有机会带一点金箔。
    // 领取前可以转发一次，整份奖励重抽。转发和发帖不分先后。
    // 「去游戏圈」上盖着微信原生的透明按钮，点它直接进游戏圈；切回来时重查发帖数。
    public sealed class GameClubPanel : MonoBehaviour
    {
        const float BoardW = 620f;
        const float BoardH = 720f;
        static readonly float[] ShowPolls = { 0f, 1.5f, 4f, 8f };
        static readonly Vector2 CardSize = new Vector2(176f, 200f);

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        Button _go;
        Button _claim;
        Button _refresh;
        Text _task;
        string _block;
        int _posts = -1;
        bool _asking;
        Coroutine _poll;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "game_club";
            var panel = dim.gameObject.AddComponent<GameClubPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            WxBridge.EnsurePrivacy(null);
            panel.Build(dim);
        }

        void OnEnable()
        {
            GameClubService.Hook();
            GameClubService.Shown += OnBack;
        }

        void OnDisable()
        {
            GameClubService.Shown -= OnBack;
        }

        void OnDestroy()
        {
            WxBridge.HideClubButton();
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Build(RectTransform dim)
        {
            _meta.EnsureClubOffer();
            _board = PanelKit.Board(dim, "游戏圈", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);

            var sub = UiKit.Label(_board, "sub", "每天在游戏圈发一条帖子就能领", 24,
                new Vector2(0f, 72f), new Vector2(BoardW - 60f, 32f), TextAnchor.MiddleCenter, Pin.Top);
            sub.color = InkTheme.TextMid;

            _task = UiKit.Label(_board, "task", "", 26, new Vector2(0f, 430f), new Vector2(BoardW - 60f, 40f),
                TextAnchor.MiddleCenter, Pin.Top);
            _task.color = InkTheme.TextDark;

            _refresh = UiKit.Btn(_board, "refresh", "转发刷新", new Vector2(0f, 176f), new Vector2(520f, 80f),
                OnRefresh, false, Pin.Bottom);
            _go = UiKit.Btn(_board, "go", "去游戏圈", new Vector2(-138f, 52f), new Vector2(256f, 104f), OnGo, false, Pin.Bottom);
            _claim = UiKit.Btn(_board, "claim", "领取", new Vector2(138f, 52f), new Vector2(256f, 104f), OnClaim, true, Pin.Bottom);

            PaintRewards();
            Refresh();
            Ask();
            if (WxBridge.CanUseClub) StartCoroutine(PlaceClubButton());
        }

        IEnumerator PlaceClubButton()
        {
            // 卡面打开的 0.32 秒里从 0.72 涨到 1。只在第一帧量一次的话，
            // 微信那个透明按钮会留在还没涨开的位置，之后点「去游戏圈」点到的是底下的 Unity 按钮。
            RectTransform rt = _go.GetComponent<RectTransform>();
            float still = 0f;
            Rect last = default;
            while (_go != null && still < 0.12f)
            {
                Rect now = PanelKit.ScreenRect(rt);
                if (Mathf.Abs(now.x - last.x) > 0.5f || Mathf.Abs(now.y - last.y) > 0.5f
                    || Mathf.Abs(now.width - last.width) > 0.5f || Mathf.Abs(now.height - last.height) > 0.5f)
                {
                    WxBridge.ShowClubButton(now);
                    last = now;
                    still = 0f;
                }
                else still += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 发帖后游戏圈那边的统计有延迟，切回来多问几次。
        void OnBack()
        {
            if (this == null) return;
            if (_poll != null) StopCoroutine(_poll);
            _poll = StartCoroutine(PollAfterShow());
        }

        IEnumerator PollAfterShow()
        {
            float last = 0f;
            foreach (float at in ShowPolls)
            {
                if (_meta.ClubClaimedToday || _posts > 0) yield break;
                if (at > last) yield return new WaitForSecondsRealtime(at - last);
                last = at;
                Ask();
            }
        }

        void Ask()
        {
            if (_asking || _meta.ClubClaimedToday) return;
            _asking = true;
            GameClubService.DailyPosts((count, err) =>
            {
                _asking = false;
                if (this == null) return;
                _block = null;
                if (err != null)
                {
                    Debug.LogWarning("[GameClub] " + err);
                    if (err.IndexOf("隐私", StringComparison.Ordinal) >= 0)
                        _block = "需同意隐私协议后才能同步发帖";
                }
                else _posts = Mathf.Max(_posts, count);
                Refresh();
            });
        }

        void PaintRewards()
        {
            Drop("ink");
            Drop("chest");
            Drop("foil");
            if (!_meta.HasClubOffer) return;
            var cards = new List<(string id, string icon, string name, string amount)>(3)
            {
                ("ink", "Ui/ico_ink", "墨", "×" + _meta.ClubInk),
            };
            ChestDef chest = ChestCatalog.Get(_meta.ClubChestTier);
            cards.Add(("chest", "Ui/chest_" + chest.Key, chest.Name, "×1"));
            if (_meta.ClubFoil > 0)
                cards.Add(("foil", "Ui/ico_foil", "金箔", "×" + _meta.ClubFoil));
            float span = (cards.Count - 1) * (CardSize.x + 16f);
            for (int i = 0; i < cards.Count; i++)
            {
                float x = -span * 0.5f + i * (CardSize.x + 16f);
                var card = PanelKit.Reward(_board, cards[i].icon, cards[i].name, cards[i].amount,
                    new Vector2(x, 150f), CardSize);
                card.name = cards[i].id;
                Transform shade = _board.Find("reward_sh");
                if (shade != null) shade.name = cards[i].id + "_sh";
            }
        }

        void Drop(string id)
        {
            Transform old = _board.Find(id);
            if (old != null)
            {
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            Transform shade = _board.Find(id + "_sh");
            if (shade != null)
            {
                shade.gameObject.SetActive(false);
                Destroy(shade.gameObject);
            }
        }

        void Refresh()
        {
            bool claimed = _meta.ClubClaimedToday;
            bool used = _meta.ClubRefreshedToday;
            SetShown(_refresh, !claimed);
            if (!claimed)
            {
                PanelKit.SetText(_refresh, used ? "今日已刷新" : "转发刷新");
                PanelKit.Dim(_refresh, used, false);
            }
            if (!claimed && _posts <= 0 && !string.IsNullOrEmpty(_block))
            {
                _task.text = _block;
                PanelKit.SetText(_claim, "领取");
                PanelKit.Dim(_claim, true);
                return;
            }
            int done = claimed ? 1 : Mathf.Max(0, Mathf.Min(1, _posts));
            _task.text = claimed ? "今日奖励已领取，明天再来" : $"今日发帖  {done}/1";
            PanelKit.SetText(_claim, claimed ? "已领取" : "领取");
            PanelKit.Dim(_claim, claimed || _posts <= 0);
        }

        static void SetShown(Button btn, bool on)
        {
            if (btn == null) return;
            btn.gameObject.SetActive(on);
            Transform shade = btn.transform.parent.Find(btn.name + "_sh");
            if (shade != null) shade.gameObject.SetActive(on);
        }

        void OnGo()
        {
            AudioBus.Tap();
            // 真机上点到的是盖在上面的原生按钮，走不到这里。
            InkToast.Show(_layer, "游戏圈要在微信里打开");
        }

        void OnRefresh()
        {
            AudioBus.Tap();
            if (_meta.ClubClaimedToday) return;
            if (_meta.ClubRefreshedToday)
            {
                InkToast.Show(_layer, "今天已经刷新过了");
                return;
            }
            WxBridge.ShareForReward(ShareCatalog.PickClub(), "gameclub_refresh", () =>
            {
                if (!_meta.RerollClubOffer()) return;
                if (this == null)
                {
                    _changed?.Invoke();
                    return;
                }
                PaintRewards();
                Refresh();
                InkToast.Show(_layer, "奖励已刷新");
                _changed?.Invoke();
            }, () =>
            {
                if (this != null) InkToast.Show(_layer, "转发没发出去");
            });
        }

        void OnClaim()
        {
            AudioBus.Tap();
            if (_meta.ClubClaimedToday) { InkToast.Show(_layer, "今天已经领过了"); return; }
            if (_posts <= 0)
            {
                InkToast.Show(_layer, !string.IsNullOrEmpty(_block) ? _block
                    : (_asking ? "正在查询发帖记录…" : "先去游戏圈发一条帖子"));
                if (!_asking) Ask();
                return;
            }
            if (!_meta.ClaimClub(out int slot)) return;
            int ink = _meta.ClubInk;
            int foil = _meta.ClubFoil;
            ChestTier tier = _meta.ClubChestTier;
            string chest = ChestCatalog.Get(tier).Name;
            string extra = foil > 0 ? "，金箔×" + foil : "";
            int shown = ink;
            string where;
            if (slot >= 0) where = chest + "已放进宝箱位";
            else if (slot == -3)
            {
                shown += ChestCatalog.InkAvg(tier);
                where = chest + "换成墨";
            }
            else where = "宝箱位满了，选个位置放下";
            Vector2 from = RewardFly.Local(_layer, _claim != null ? _claim.transform : transform);
            var pieces = new List<RewardFly.Piece>(3) { RewardFly.Ink(_layer, shown) };
            if (foil > 0) pieces.Add(RewardFly.Foil(_layer, foil));
            if (slot >= 0) pieces.Add(RewardFly.Chest(_layer, tier, slot));
            InkToast.Show(_layer, "已领取 墨×" + ink + extra + "，" + where);
            RewardFly.Play(_layer, from, pieces.ToArray());
            _changed?.Invoke();
            Refresh();
            if (slot == -1) ChestOverflowView.Show(_layer, _meta, _changed);
        }
    }
}
