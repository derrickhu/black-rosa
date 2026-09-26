using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 游戏圈每日任务，照 hotpot：去游戏圈发一条帖子，回来领墨和碎片，每天一次。
    // 「去游戏圈」上盖着微信原生的透明按钮，点它直接进游戏圈；切回来时重查发帖数。
    public sealed class GameClubPanel : MonoBehaviour
    {
        const float BoardW = 620f;
        const float BoardH = 640f;
        static readonly float[] ShowPolls = { 0f, 1.5f, 4f, 8f };

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        Button _go;
        Button _claim;
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
            var board = PanelKit.Board(dim, "游戏圈", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);

            var sub = UiKit.Label(board, "sub", "每天在游戏圈发一条帖子就能领", 24,
                new Vector2(0f, 72f), new Vector2(BoardW - 60f, 32f), TextAnchor.MiddleCenter, Pin.Top);
            sub.color = InkTheme.TextMid;

            var cardSize = new Vector2(190f, 214f);
            PanelKit.Reward(board, "Ui/ico_ink", "墨", "×" + GameConstants.ClubInk, new Vector2(-110f, 124f), cardSize);
            PanelKit.Reward(board, "Ui/ico_shard", "技能碎片", "×" + GameConstants.ClubShards, new Vector2(110f, 124f), cardSize);

            _task = UiKit.Label(board, "task", "", 26, new Vector2(0f, 382f), new Vector2(BoardW - 60f, 40f),
                TextAnchor.MiddleCenter, Pin.Top);
            _task.color = InkTheme.TextDark;

            _go = UiKit.Btn(board, "go", "去游戏圈", new Vector2(-138f, 52f), new Vector2(256f, 104f), OnGo, false, Pin.Bottom);
            _claim = UiKit.Btn(board, "claim", "领取", new Vector2(138f, 52f), new Vector2(256f, 104f), OnClaim, true, Pin.Bottom);

            Refresh();
            Ask();
            if (WxBridge.CanUseClub) StartCoroutine(PlaceClubButton());
        }

        IEnumerator PlaceClubButton()
        {
            yield return null;
            if (_go == null) yield break;
            WxBridge.ShowClubButton(PanelKit.ScreenRect(_go.GetComponent<RectTransform>()));
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

        void Refresh()
        {
            bool claimed = _meta.ClubClaimedToday;
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

        void OnGo()
        {
            AudioBus.Tap();
            // 真机上点到的是盖在上面的原生按钮，走不到这里。
            InkToast.Show(_layer, "游戏圈要在微信里打开");
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
            if (!_meta.ClaimClub(out int shards)) return;
            string msg = shards > 0
                ? $"已领取 墨×{GameConstants.ClubInk}、碎片×{shards}"
                : $"已领取 墨×{GameConstants.ClubInk}，技能碎片已满";
            InkToast.Show(_layer, msg);
            _changed?.Invoke();
            Refresh();
        }
    }
}
