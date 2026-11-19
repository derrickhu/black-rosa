using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 游戏圈每日任务（Figma Panel/首页弹窗/02_游戏圈 定稿版，预制体 Prefabs/GameClub）：
    // 上方说明条，正中统一 96×96 物品框奖励展示台 + 右下小号「转发刷新」，
    // 下方发帖进度条，底部正中单主按钮位（未发帖青色「去游戏圈」/ 已发帖黄色「领取奖励」/ 已领灰色「今日已领」）。
    public sealed class GameClubPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/GameClub";
        public const string Mark = "game_club_v2";

        const float BoardW = 648f;
        const float BoardH = 612f;
        const float StageW = 600f;
        const float StageH = 218f;
        const float TrackW = 600f;
        const float TrackH = 30f;
        static readonly float[] ShowPolls = { 0f, 1.5f, 4f, 8f };

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        RectTransform _rewardsHost;
        RectTransform _progFill;
        Text _progTitle;
        Button _refresh;
        Text _refreshLabel;
        Button _main;
        Text _mainLabel;
        bool _clubOverMain;
        string _block;
        int _posts = -1;
        bool _asking;
        Coroutine _poll;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "game_club", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<GameClubPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            WxBridge.EnsurePrivacy(null);
            panel.Bind(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "game_club", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "游戏圈", "Ui/ico_act_circle",
                "每日在游戏圈发 1 条动态 · 领随机补给", null, null, () => { }, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            var rewardStage = ModalKit.Sliced(board, "reward_stage", "Ui/modal_sub_warm", ModalKit.BorderSub18,
                24f, 152f, StageW, StageH, false);

            var hostGo = new GameObject("rewards", typeof(RectTransform));
            hostGo.transform.SetParent(rewardStage, false);
            var rewardsHost = hostGo.GetComponent<RectTransform>();
            ModalKit.TopLeft(rewardsHost, 0f, 20f, StageW, 134f);

            const float s = 96f, gap = 44f;
            float x0 = Mathf.Round((StageW - (3f * s + 2f * gap)) * 0.5f);
            string[] defTones = { ModalKit.InkTone, "blue", ModalKit.FoilTone };
            string[] defIcons = { "Ui/ico_ink", "Ui/chest_silver", "Ui/ico_foil" };
            string[] defNames = { "灵墨", "银宝箱", "金箔" };
            string[] defCnts = { "200", null, "3" };
            for (int i = 0; i < 3; i++)
            {
                float sx = x0 + i * (s + gap);
                var slot = ModalKit.ItemSlot(rewardsHost, defTones[i], 96, sx, 2f,
                    ModalKit.Load(defIcons[i]), defCnts[i], forceNodes: true);
                slot.name = "slot" + i;
                ModalKit.SolidText(rewardsHost, "nm" + i, defNames[i], 16, sx - 12f, 104f, 120f, 24f,
                    ModalKit.TextBody, TextAnchor.MiddleCenter, true);
            }

            ModalKit.SolidText(rewardStage, "tip", "每日可免费转发刷新 1 次，大概率升阶奖励", 16,
                24f, 162f, 400f, 48f, ModalKit.TextMuted, TextAnchor.MiddleLeft, false);
            var refresh = ModalKit.HexBtn(rewardStage, "sm_cyan", 444f, 156f, "转发刷新", null, out _);
            refresh.name = "refresh";

            ModalKit.SolidText(board, "prog_title", "今日发帖进度（0 / 1）", 20, 28f, 386f, 420f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.ProgressBar(board, 24f, 422f, TrackW, TrackH, 0f, "", out _, out _);

            var main = ModalKit.HexBtn(board, "lg_cyan", 176f, 476f, "去游戏圈", null, out _);
            main.name = "main_action";

            return dim;
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

        void Bind(RectTransform stage)
        {
            _meta.EnsureClubOffer();
            _board = stage.Find("board") as RectTransform;
            ModalKit.BindClose(_board, Close);

            _rewardsHost = _board.Find("reward_stage/rewards") as RectTransform;
            _refresh = ModalKit.BindBtn(_board, "reward_stage/refresh", OnRefresh);
            _refreshLabel = _refresh != null ? _refresh.transform.Find("t")?.GetComponent<Text>() : null;

            _progTitle = _board.Find("prog_title")?.GetComponent<Text>();
            _progFill = _board.Find("prog/fill") as RectTransform;

            _main = ModalKit.BindBtn(_board, "main_action", OnMainTap);
            _mainLabel = _main != null ? _main.transform.Find("t")?.GetComponent<Text>() : null;

            PaintRewards();
            Refresh();
            PanelKit.Enlarge(_board);
            FitRefreshRow();
            PanelKit.Open(_board);
            Ask();
        }

        // 说明和「转发刷新」同一行。字号加大后会溢出原来的窄框，盖到按钮上。
        void FitRefreshRow()
        {
            var stage = _board != null ? _board.Find("reward_stage") as RectTransform : null;
            var tip = _board != null ? _board.Find("reward_stage/tip")?.GetComponent<Text>() : null;
            var btnRt = _refresh != null ? _refresh.GetComponent<RectTransform>() : null;
            if (stage == null || tip == null || btnRt == null) return;

            float stageW = stage.sizeDelta.x > 2f ? stage.sizeDelta.x : StageW;
            float btnW = btnRt.sizeDelta.x;
            float btnH = btnRt.sizeDelta.y;
            const float pad = 16f;
            const float gap = 14f;
            const float rowY = 156f;
            float textW = ModalKit.MeasureText(tip.text, tip.fontSize) + 10f;
            float avail = stageW - pad * 2f - gap - btnW;

            tip.horizontalOverflow = HorizontalWrapMode.Overflow;
            tip.verticalOverflow = VerticalWrapMode.Overflow;
            if (textW <= avail)
            {
                float left = Mathf.Round((stageW - (textW + gap + btnW)) * 0.5f);
                ModalKit.TopLeft(tip.rectTransform, left, rowY, textW, btnH);
                tip.alignment = TextAnchor.MiddleLeft;
                ModalKit.TopLeft(btnRt, left + textW + gap, rowY, btnW, btnH);
                return;
            }

            float btnX = stageW - pad - btnW;
            ModalKit.TopLeft(btnRt, btnX, rowY, btnW, btnH);
            ModalKit.TopLeft(tip.rectTransform, pad, rowY - 8f, btnX - gap - pad, btnH + 16f);
            tip.alignment = TextAnchor.MiddleLeft;
            tip.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        void SyncClubOverlay(bool needClub)
        {
            _clubOverMain = needClub;
            if (!WxBridge.CanUseClub) return;
            StopCoroutine(nameof(PlaceClubButton));
            if (needClub && _main != null)
                StartCoroutine(nameof(PlaceClubButton));
            else
                WxBridge.HideClubButton();
        }

        IEnumerator PlaceClubButton()
        {
            RectTransform rt = _main.GetComponent<RectTransform>();
            float still = 0f;
            Rect last = default;
            while (_main != null && _clubOverMain && still < 0.12f)
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
                        _block = "需同意隐私协议同步";
                }
                else _posts = Mathf.Max(_posts, count);
                Refresh();
            });
        }

        void PaintRewards()
        {
            if (_rewardsHost == null || !_meta.HasClubOffer) return;

            var items = new List<(string tone, string icon, string name, string cnt)>(3)
            {
                (ModalKit.InkTone, "Ui/ico_ink", "灵墨", _meta.ClubInk.ToString()),
            };
            ChestDef chest = ChestCatalog.Get(_meta.ClubChestTier);
            items.Add((ModalKit.ChestTone(_meta.ClubChestTier), "Ui/chest_" + chest.Key, chest.Name, null));
            if (_meta.ClubFoil > 0)
                items.Add((ModalKit.FoilTone, "Ui/ico_foil", "金箔", _meta.ClubFoil.ToString()));

            const float s = 96f, gap = 44f;
            float totalW = items.Count * s + (items.Count - 1) * gap;
            float x0 = Mathf.Round((StageW - totalW) * 0.5f);

            for (int i = 0; i < 3; i++)
            {
                var slot = _rewardsHost.Find("slot" + i) as RectTransform;
                var nmTr = _rewardsHost.Find("nm" + i) as RectTransform;
                bool active = i < items.Count;
                if (slot != null) slot.gameObject.SetActive(active);
                if (nmTr != null) nmTr.gameObject.SetActive(active);
                if (!active) continue;

                float sx = x0 + i * (s + gap);
                if (slot != null)
                {
                    ModalKit.TopLeft(slot, sx, 2f, s, s);
                    ModalKit.SetItemSlot(slot, items[i].tone, 96, ModalKit.Load(items[i].icon), items[i].cnt);
                }
                if (nmTr != null)
                {
                    ModalKit.TopLeft(nmTr, sx - 12f, 104f, 120f, 24f);
                    var nmTxt = nmTr.GetComponent<Text>();
                    if (nmTxt != null) nmTxt.text = items[i].name;
                }
            }
        }

        void Refresh()
        {
            bool claimed = _meta.ClubClaimedToday;
            bool used = _meta.ClubRefreshedToday;

            if (_refresh != null)
            {
                _refresh.gameObject.SetActive(!claimed);
                if (!claimed)
                {
                    if (_refreshLabel != null) _refreshLabel.text = used ? "今日已刷" : "转发刷新";
                    ModalKit.SetHexBtnGray(_refresh, "sm_cyan", "sm_gray", used);
                }
            }

            int done = claimed ? 1 : Mathf.Max(0, Mathf.Min(1, _posts));
            ModalKit.SetProgress(_progFill, TrackW, TrackH, done);

            if (!claimed && _posts <= 0 && !string.IsNullOrEmpty(_block))
            {
                if (_progTitle != null) _progTitle.text = _block;
                if (_mainLabel != null) _mainLabel.text = "去游戏圈";
                ModalKit.SetHexBtnGray(_main, "lg_cyan", "lg_gray", false);
                SyncClubOverlay(true);
                return;
            }

            if (_progTitle != null)
                _progTitle.text = claimed ? "今日发帖进度（1 / 1）· 已领取" : $"今日发帖进度（{done} / 1）";

            if (claimed)
            {
                if (_mainLabel != null) _mainLabel.text = "今日已领";
                ModalKit.SetHexBtnGray(_main, "lg_yellow", "lg_gray", true);
                SyncClubOverlay(false);
            }
            else if (done > 0)
            {
                if (_mainLabel != null) _mainLabel.text = "领取奖励";
                ModalKit.SetHexBtnGray(_main, "lg_yellow", "lg_gray", false);
                SyncClubOverlay(false);
            }
            else
            {
                if (_mainLabel != null) _mainLabel.text = "去游戏圈";
                ModalKit.SetHexBtnGray(_main, "lg_cyan", "lg_gray", false);
                SyncClubOverlay(true);
            }
        }

        void OnMainTap()
        {
            if (_meta.ClubClaimedToday)
            {
                AudioBus.Tap();
                InkToast.Show(_layer, "今天已经领过了");
                return;
            }
            if (_posts <= 0)
            {
                AudioBus.Tap();
                InkToast.Show(_layer, "游戏圈要在微信里打开");
                return;
            }
            OnClaim();
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
            GameFlow.ArmShareReturn();
            WxBridge.HideClubButton();
            WxBridge.ShareForReward(ShareCatalog.PickClub(), "gameclub_refresh", () =>
            {
                GameFlow.DisarmShareReturn();
                RestoreClubButton();
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
                GameFlow.DisarmShareReturn();
                RestoreClubButton();
                if (this != null) InkToast.Show(_layer, "转发没发出去");
            });
        }

        void RestoreClubButton()
        {
            if (this == null || _main == null || !_clubOverMain || !WxBridge.CanUseClub) return;
            WxBridge.ShowClubButton(PanelKit.ScreenRect(_main.GetComponent<RectTransform>()));
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
                where = chest + "换成灵墨";
            }
            else where = "宝箱位满了，选个位置放下";
            Vector2 from = RewardFly.Local(_layer, _main != null ? _main.transform : transform);
            var pieces = new List<RewardFly.Piece>(3) { RewardFly.Ink(_layer, shown) };
            if (foil > 0) pieces.Add(RewardFly.Foil(_layer, foil));
            if (slot >= 0) pieces.Add(RewardFly.Chest(_layer, tier, slot));
            InkToast.Show(_layer, "已领取 灵墨×" + ink + extra + "，" + where);
            RewardFly.Play(_layer, from, pieces.ToArray());
            _changed?.Invoke();
            Refresh();
            if (slot == -1) ChestOverflowView.Show(_layer, _meta, _changed);
        }
    }
}
