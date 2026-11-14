using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    // 招财进宝活动页（Figma Panel/首页弹窗/07_招财进宝，预制体 Prefabs/Event）：
    // 聚宝盆进度与 6 档里程碑（前 5 档通用 68×68 物品框，福袋炮台复用六边形皮肤框），下接庙会三档集市地图与尖角主按钮。
    public sealed class EventPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/Event";
        public const string Mark = "event_v2";

        const float BoardW = 648f;
        const float BoardH = 988f;
        const float ProgW = 556f;
        const float ProgH = 26f;

        static readonly string[] SpotArt = { "Ui/event_stall", "Ui/event_temple", "Ui/event_fair" };

        MetaProgress _meta;
        Action<int> _start;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        RectTransform _body;
        Text _infoLabel;
        Text _slantLabel;
        bool _leaving;
        int _selTier;

        public static void Show(RectTransform layer, MetaProgress meta, Action<int> start, Action changed)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "event_panel", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<EventPanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            panel._layer = layer;
            panel._selTier = panel.HighestOpenTier();
            panel.Bind(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "event_panel", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, EventCatalog.Title, "Ui/ico_act_event",
                "活动关不掉墨 · 赢局钱袋全入聚宝盆", "magenta", $"每日 {EventCatalog.PlaysPerDay} 次",
                () => { }, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            var bodyGo = new GameObject("body", typeof(RectTransform));
            bodyGo.transform.SetParent(board, false);
            var body = bodyGo.GetComponent<RectTransform>();
            ModalKit.TopLeft(body, 0f, 0f, BoardW, BoardH);

            // 1. 聚宝盆进度与 6 档里程碑
            var basin = ModalKit.Sliced(body, "basin", "Ui/modal_sub_warm", ModalKit.BorderSub18,
                24f, 152f, 600f, 246f, false);
            ModalKit.Pic(basin, "pouch", "Ui/modal_ico_pouch", 18f, 14f, 36f, 36f, true);

            int max = EventCatalog.Milestones[EventCatalog.Milestones.Length - 1].Need;
            ModalKit.SolidText(basin, "bank", $"聚宝盆已攒：0 / {max} 金", 20,
                62f, 16f, 290f, 30f, ModalKit.TextDark, TextAnchor.MiddleLeft, true);
            ModalKit.Tag(basin, "Ui/modal_tag_slate", 356f, 16f, 226f, 28f, "赢局全入盆 · 输局看广告存半", false, 13);
            ModalKit.ProgressBar(basin, 22f, 62f, ProgW, ProgH, 0f, $"0 / {max}", out _, out _);

            int n = EventCatalog.Milestones.Length;
            for (int i = 0; i < n; i++)
            {
                EventMilestone m = EventCatalog.Milestones[i];
                bool isSkin = m.Prize == EventPrize.Skin;
                float nx = 18f + i * 96f;

                RectTransform hitRt;
                if (isSkin)
                {
                    var hexImg = ModalKit.Pic(basin, "ms" + i, "Ui/skin_hex_purple", nx - 6f, 100f, 80f, 90f, true);
                    hexImg.raycastTarget = true;
                    hitRt = hexImg.rectTransform;
                    ModalKit.Pic(hitRt, "art", EventCatalog.PrizeIcon(m), 11f, 13f, 58f, 58f, true);
                    var ult = ModalKit.Tag(basin, "Ui/modal_tag_coral", nx - 2f, 92f, 72f, 22f, "终极炮台", false, 12);
                    ult.name = "ult" + i;
                }
                else
                {
                    string tone = ToneOf(m);
                    string cnt = m.Prize == EventPrize.Ink || m.Prize == EventPrize.Token ? m.Amount.ToString() : "1";
                    hitRt = ModalKit.ItemSlot(basin, tone, 68, nx, 112f, ModalKit.Load(EventCatalog.PrizeIcon(m)), cnt);
                    hitRt.name = "ms" + i;
                    hitRt.GetComponent<Image>().raycastTarget = true;
                }

                var btn = hitRt.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = hitRt.GetComponent<Image>();

                var chk = ModalKit.Pic(basin, "chk" + i, "Ui/checkin_check", nx + 44f, 102f, 28f, 28f, true);
                chk.gameObject.SetActive(false);

                var rdy = ModalKit.Tag(basin, "Ui/modal_tag_coral", nx + 16f, 100f, 54f, 22f, "可领", false, 13);
                rdy.name = "rdy" + i;
                rdy.gameObject.SetActive(false);

                ModalKit.SolidText(basin, "req" + i, m.Need + "金", 14, nx - 6f, 192f, 80f, 24f,
                    isSkin ? InkTheme.Hex("B85C14") : ModalKit.TextBody, TextAnchor.MiddleCenter, true);
            }

            // 2. 庙会三档集市地图
            var card = ModalKit.Sliced(body, "stage_card", "Ui/modal_sub_charcoal", ModalKit.BorderSub16,
                24f, 414f, 600f, 398f, false);

            var maskGo = new GameObject("map_mask", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            maskGo.transform.SetParent(card, false);
            var mapRt = maskGo.GetComponent<RectTransform>();
            ModalKit.TopLeft(mapRt, 4f, 4f, 592f, 334f);
            maskGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            ModalKit.Pic(mapRt, "map", "Ui/event_map", 0f, -40f, 592f, 414f, false);

            BuildSpotNode(mapRt, 0, 108f, 232f);
            BuildSpotNode(mapRt, 1, 296f, 162f);
            BuildSpotNode(mapRt, 2, 476f, 84f);

            ModalKit.Tag(card, "Ui/modal_tag_dark", 16f, 16f, 256f, 34f, "点击集市开打 · 福袋炮台拾金+10%", false, 13);
            ModalKit.SlantTag(card, "yellow", 458f, 14f, EventCatalog.Tier(0).Name + "·可进", out _);

            var bot = ModalKit.Sliced(card, "bot_bar", "Ui/modal_sub_dark", ModalKit.BorderSub16,
                4f, 338f, 592f, 56f, false);
            ModalKit.OutlinedText(bot, "plays", $"今日免费挑战次数：{EventCatalog.PlaysPerDay} / {EventCatalog.PlaysPerDay}", 19,
                0f, 0f, 592f, 56f, ModalKit.GoldLight, TextAnchor.MiddleCenter);

            // 3. 底部按钮
            var adBtn = ModalKit.HexBtn(body, "md_cyan", 46f, 846f, "", null, out _);
            adBtn.gameObject.name = "ad_btn";
            ModalKit.SetAdBtnLabel(adBtn, "md_cyan", $"+{EventCatalog.AdPlays} 次", true, 20);

            var goBtn = ModalKit.HexBtn(body, "wide_yellow", 288f, 838f,
                "开始打金 · " + EventCatalog.Tier(0).Name, null, out _);
            goBtn.gameObject.name = "go_btn";

            board.Find("close")?.SetAsLastSibling();
            return dim;
        }

        static void BuildSpotNode(RectTransform mapRt, int t, float cx, float cy)
        {
            EventTierDef d = EventCatalog.Tier(t);
            const float iconS = 104f;

            var hitGo = new GameObject("spot" + t, typeof(RectTransform), typeof(Image), typeof(Button));
            hitGo.transform.SetParent(mapRt, false);
            var hit = hitGo.GetComponent<RectTransform>();
            hit.anchorMin = hit.anchorMax = new Vector2(0f, 1f);
            hit.pivot = new Vector2(0.5f, 0.5f);
            hit.anchoredPosition = new Vector2(cx, -cy);
            hit.sizeDelta = new Vector2(132f, 138f);
            var hitImg = hitGo.GetComponent<Image>();
            hitImg.color = new Color(1f, 1f, 1f, 0.01f);
            var btn = hitGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hitImg;

            ModalKit.Pic(hit, "art", SpotArt[t], (132f - iconS) * 0.5f, 4f, iconS, iconS, true);
            var lk = ModalKit.Pic(hit, "lock", "Ui/item_lock", 46f, 36f, 40f, 40f, true);
            lk.gameObject.SetActive(false);

            var tag = ModalKit.Tag(hit, "Ui/modal_tag_dark", 16f, 106f, 100f, 26f, d.Name, false, 14);
            tag.name = "spot_tag";
        }

        void Bind(RectTransform stage)
        {
            _board = stage.Find("board") as RectTransform;
            _body = _board.Find("body") as RectTransform;
            ModalKit.BindClose(_board, Close);

            _infoLabel = _board.Find("info_strip/t")?.GetComponent<Text>();
            _slantLabel = _board.Find("slant_tag/t")?.GetComponent<Text>();

            var basin = _body.Find("basin");
            for (int i = 0; i < EventCatalog.Milestones.Length; i++)
            {
                int idx = i;
                ModalKit.BindBtn(basin, "ms" + i, () => Claim(idx));
            }

            var mapRt = _body.Find("stage_card/map_mask");
            for (int t = 0; t < EventCatalog.TierCount; t++)
            {
                int tier = t;
                var spot = mapRt.Find("spot" + t) as RectTransform;
                if (spot == null) continue;
                ModalKit.BindBtn(spot, "", () =>
                {
                    if (_meta.EventTierOpen(tier)) _selTier = tier;
                    PressGo(spot, tier);
                });

                var trigger = spot.GetComponent<EventTrigger>() ?? spot.gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();
                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener(_ => { if (_meta.EventTierOpen(tier)) spot.localScale = new Vector3(0.92f, 0.92f, 1f); });
                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener(_ => { if (!_leaving) spot.localScale = Vector3.one; });
                trigger.triggers.Add(down);
                trigger.triggers.Add(up);
            }

            ModalKit.BindBtn(_body, "ad_btn", OnAd);
            ModalKit.BindBtn(_body, "go_btn", () => PressGo(null, _selTier));

            Refresh();
            PanelKit.Open(_board);
        }

        int HighestOpenTier()
        {
            int best = 0;
            for (int i = 0; i < EventCatalog.TierCount; i++)
                if (_meta.EventTierOpen(i)) best = i;
            return best;
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Refresh()
        {
            UiAnim.On(this).Clear();

            bool done = _meta.EventDone;
            if (_infoLabel != null)
            {
                _infoLabel.text = done
                    ? "聚宝盆奖励已全部领完，再打不再产出奖励"
                    : "活动关不掉墨 · 积满聚宝盆抱走福袋炮台";
            }
            if (_slantLabel != null)
                _slantLabel.text = done ? "全部已领" : $"每日 {EventCatalog.PlaysPerDay} 次";

            var basin = _body.Find("basin");
            int max = EventCatalog.Milestones[EventCatalog.Milestones.Length - 1].Need;
            var bankTxt = basin.Find("bank")?.GetComponent<Text>();
            if (bankTxt != null) bankTxt.text = $"聚宝盆已攒：{_meta.EventBank} / {max} 金";

            float ratio = max > 0 ? Mathf.Min(_meta.EventBank, max) / (float)max : 0f;
            ModalKit.SetProgress(basin.Find("prog/fill") as RectTransform, ProgW, ProgH, ratio);
            var progTxt = basin.Find("prog/t")?.GetComponent<Text>();
            if (progTxt != null) progTxt.text = $"{_meta.EventBank} / {max}";

            int n = EventCatalog.Milestones.Length;
            for (int i = 0; i < n; i++)
            {
                bool can = _meta.EventClaimable(i);
                bool got = _meta.EventClaimed(i);
                var ms = basin.Find("ms" + i) as RectTransform;
                if (ms != null)
                {
                    ms.localScale = Vector3.one;
                    var img = ms.GetComponent<Image>();
                    if (img != null) img.color = got ? new Color(0.84f, 0.82f, 0.78f, 1f) : Color.white;
                    if (can) UiAnim.On(this).Breathe(ms, 0.08f * i, 0.045f, 1.2f);
                }
                var chk = basin.Find("chk" + i);
                if (chk != null) chk.gameObject.SetActive(got);
                var rdy = basin.Find("rdy" + i);
                if (rdy != null) rdy.gameObject.SetActive(!got && can);
                var ult = basin.Find("ult" + i);
                if (ult != null) ult.gameObject.SetActive(!got && !can);
            }

            var stageCard = _body.Find("stage_card");
            var cardSlant = stageCard.Find("slant_tag/t")?.GetComponent<Text>();
            if (cardSlant != null) cardSlant.text = EventCatalog.Tier(_selTier).Name + "·可进";

            int cap = EventCatalog.PlaysPerDay + (_meta.EventAdDone ? EventCatalog.AdPlays : 0);
            var playsTxt = stageCard.Find("bot_bar/plays")?.GetComponent<Text>();
            if (playsTxt != null) playsTxt.text = $"今日免费挑战次数：{_meta.EventPlaysLeft} / {cap}";

            var mapRt = stageCard.Find("map_mask");
            for (int t = 0; t < EventCatalog.TierCount; t++)
            {
                EventTierDef d = EventCatalog.Tier(t);
                bool open = _meta.EventTierOpen(t);
                bool won = _meta.EventTierWon(t);
                var spot = mapRt.Find("spot" + t);
                if (spot == null) continue;

                var art = spot.Find("art")?.GetComponent<Image>();
                if (art != null)
                {
                    art.transform.localScale = Vector3.one;
                    art.color = open ? Color.white : new Color(0.58f, 0.58f, 0.60f, 1f);
                    if (open) UiAnim.On(this).Breathe(art.transform, 0.15f * t, 0.04f, 1.15f);
                }
                var lk = spot.Find("lock");
                if (lk != null) lk.gameObject.SetActive(!open);

                var tag = spot.Find("spot_tag") as RectTransform;
                string tagKey = !open ? "Ui/modal_tag_gray" : (_selTier == t ? "Ui/modal_tag_gold" : "Ui/modal_tag_dark");
                ModalKit.SetTag(tag, tagKey, won ? d.Name + " ★" : d.Name, open && _selTier == t);
            }

            bool hasAd = !_meta.EventAdDone;
            var adTr = _body.Find("ad_btn");
            if (adTr != null) adTr.gameObject.SetActive(hasAd);

            var goBtn = _body.Find("go_btn")?.GetComponent<Button>();
            if (goBtn != null)
            {
                float goX = hasAd ? 288f : (BoardW - 314f) * 0.5f;
                ModalKit.TopLeft(goBtn.GetComponent<RectTransform>(), goX, 838f, 314f, 80f);
                string kind = _meta.EventPlaysLeft <= 0 ? "wide_gray" : "wide_yellow";
                ModalKit.SetHexBtnGray(goBtn, "wide_yellow", "wide_gray", _meta.EventPlaysLeft <= 0);
                ModalKit.SetBtnLabel(goBtn, kind, "开始打金 · " + EventCatalog.Tier(_selTier).Name);
            }
        }

        static string ToneOf(EventMilestone m)
        {
            switch (m.Prize)
            {
                case EventPrize.Ink: return ModalKit.InkTone;
                case EventPrize.Token: return ModalKit.CurrencyTone;
                case EventPrize.Chest: return ModalKit.ChestTone((ChestTier)m.Amount);
                default: return ModalKit.CurrencyTone;
            }
        }

        static RewardFly.Piece[] PrizePieces(RectTransform layer, EventMilestone m, bool refunded, int chest)
        {
            if (refunded) return new[] { RewardFly.Token(layer, EventCatalog.SkinTokenRefund) };
            switch (m.Prize)
            {
                case EventPrize.Ink: return new[] { RewardFly.Ink(layer, m.Amount) };
                case EventPrize.Token: return new[] { RewardFly.Token(layer, m.Amount) };
                case EventPrize.Chest:
                    if (chest == -3) return new[] { RewardFly.Ink(layer, ChestCatalog.InkAvg((ChestTier)m.Amount)) };
                    if (chest < 0) return new RewardFly.Piece[0];
                    return new[] { RewardFly.Chest(layer, (ChestTier)m.Amount, chest) };
                default: return new[] { RewardFly.Skin(layer, m.Amount) };
            }
        }

        void Claim(int i)
        {
            EventMilestone m = EventCatalog.Milestones[i];
            if (!_meta.EventClaimable(i))
            {
                AudioBus.Tap();
                string what = EventCatalog.PrizeName(m);
                InkToast.Show(_layer, _meta.EventClaimed(i) ? $"{what}已经领过了" : $"聚宝盆攒到 {m.Need} 金币可领{what}");
                return;
            }
            Transform basin = _body != null ? _body.Find("basin") : null;
            Transform card = basin != null ? basin.Find("ms" + i) : null;
            Vector2 from = RewardFly.Local(_layer, card);
            if (!_meta.ClaimEvent(i, out bool refunded, out int chest)) return;
            AudioBus.CountTick();
            string got = refunded ? $"已有福袋炮台，折成活动币 ×{EventCatalog.SkinTokenRefund}"
                : chest == -1 ? "宝箱位满了，选个位置放下"
                : chest == -3 ? $"宝箱位满了，换成 {ChestCatalog.InkAvg((ChestTier)m.Amount)} 灵墨"
                : $"领到 {EventCatalog.PrizeName(m)} {EventCatalog.PrizeCount(m)}";
            InkToast.Show(_layer, got);
            if (m.Prize != EventPrize.Token && !refunded)
                RewardFly.Play(_layer, from, PrizePieces(_layer, m, refunded, chest));
            _changed?.Invoke();
            Refresh();
            if (chest == -1) ChestOverflowView.Show(_layer, _meta, _changed);
        }

        void PressGo(RectTransform spot, int tier)
        {
            if (_leaving) return;
            if (!_meta.EventTierOpen(tier))
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "先打下上一处集市");
                return;
            }
            AudioBus.Tap();
            Refresh();
            if (_meta.EventPlaysLeft <= 0)
            {
                InkToast.Show(_layer, _meta.EventAdDone
                    ? "今天的次数用完了，明天再来"
                    : $"今天的次数用完了，看广告再开 {EventCatalog.AdPlays} 局");
                return;
            }
            if (_meta.EventDone)
            {
                ConfirmDone(tier);
                return;
            }
            if (spot != null) StartCoroutine(PopGo(spot, tier));
            else Launch(tier);
        }

        IEnumerator PopGo(RectTransform spot, int tier)
        {
            _leaving = true;
            float t = 0f;
            while (t < 0.12f && spot != null)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + 0.14f * Mathf.Sin(Mathf.Clamp01(t / 0.12f) * Mathf.PI);
                spot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (this != null) Launch(tier);
        }

        void Launch(int tier)
        {
            Destroy(gameObject);
            _start?.Invoke(tier);
        }

        void ConfirmDone(int tier)
        {
            ConfirmPanel.Ask(_layer, "聚宝盆的奖励已全部领取\n再打不会获得任何奖励", "不打了", "照样开打", () =>
            {
                if (this != null) Launch(tier);
            });
        }

        void OnAd()
        {
            AudioBus.Tap();
            AdStub.Reward("event_plays", () =>
            {
                if (this == null || !_meta.GrantEventAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }
    }
}
