using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 邮箱（Figma Panel/首页弹窗/03_邮箱，预制体 Prefabs/Mail）：
    // 未领邮件列表采用 Panel/信息面板 主壳、火漆信件子卡模板、68×68 通用方形物品框与尖角主按钮。
    public sealed class MailPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/Mail";
        public const string Mark = "mail_v1";

        const float BoardW = 648f;
        const float BoardH = 884f;
        const float RowW = 600f;
        const float RowH = 172f;
        const float RowStep = 186f;

        const string WechatTitle = "微信礼包";
        const string WechatBody = "礼包已送到，点击右侧领取";

        static readonly (string icon, string tone)[] GoodsSpec =
        {
            ("Ui/ico_ink",     ModalKit.InkTone),
            ("Ui/ico_stamina", ModalKit.CurrencyTone),
            ("Ui/ico_diamond", ModalKit.CurrencyTone),
            ("Ui/chest_wood",  "green"),
            ("Ui/chest_silver","blue"),
            ("Ui/chest_gold",  "purple"),
            ("Ui/chest_royal", "orange"),
            ("Ui/chest_cannon","orange"),
        };

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        RectTransform _content;
        RectTransform _rowTpl;
        Text _slantCount;
        Text _status;
        Button _retry;
        Button _all;
        bool _busy;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "mail", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<MailPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel.Bind(stage);
            panel.StartCoroutine(panel.Load());
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "mail", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "邮箱", "Ui/ico_act_mail",
                "查收防线补给与结算礼 · 保存30天", "yellow", "待领 0 封", () => { }, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(board, false);
            var view = vp.GetComponent<RectTransform>();
            ModalKit.TopLeft(view, 24f, 152f, RowW, 580f);
            vp.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            var rows = new GameObject("rows", typeof(RectTransform));
            rows.transform.SetParent(view, false);
            var content = rows.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            ModalKit.SolidText(view, "status", "请稍候…", 20, 0f, 220f, RowW, 40f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, true);
            var retry = ModalKit.HexBtn(view, "md_cyan", (RowW - 176f) * 0.5f, 280f, "重新加载", null, out _);
            retry.gameObject.name = "retry";
            retry.gameObject.SetActive(false);

            var all = ModalKit.HexBtn(board, "wide_yellow", (BoardW - 314f) * 0.5f, 756f, "一键全部领取", null, out _);
            all.gameObject.name = "claim_all";
            all.gameObject.SetActive(false);

            BuildRowTemplate(board);
            return dim;
        }

        static void BuildRowTemplate(RectTransform board)
        {
            var card = ModalKit.Sliced(board, "row_tpl", "Ui/modal_sub_bright", ModalKit.BorderSub18,
                0f, 0f, RowW, RowH, false);

            ModalKit.Pic(card, "wax", "Ui/modal_ico_waxmail", 18f, 16f, 30f, 30f, true);
            ModalKit.SolidText(card, "title", WechatTitle, 20, 56f, 14f, 404f, 32f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);

            var expTag = ModalKit.Tag(card, "Ui/modal_tag_slate", 472f, 15f, 110f, 26f, "剩 30 天", false, 13);
            expTag.name = "exp_tag";

            ModalKit.SolidText(card, "body", WechatBody, 15, 56f, 46f, 410f, 24f, ModalKit.TextMuted,
                TextAnchor.MiddleLeft, false);

            for (int i = 0; i < 5; i++)
            {
                float sx = 20f + i * 78f;
                var slot = ModalKit.ItemSlot(card, ModalKit.InkTone, 68, sx, 84f,
                    ModalKit.Load("Ui/ico_ink"), "1", forceNodes: true);
                slot.name = "s" + i;
            }

            var take = ModalKit.HexBtn(card, "sm_yellow", 450f, 94f, "领取", null, out _);
            take.gameObject.name = "take";
            card.gameObject.SetActive(false);
        }

        void Bind(RectTransform stage)
        {
            _board = stage.Find("board") as RectTransform;
            ModalKit.BindClose(_board, Close);

            _slantCount = _board.Find("slant_tag/t")?.GetComponent<Text>();
            _content = _board.Find("list/rows") as RectTransform;
            _status = _board.Find("list/status")?.GetComponent<Text>();
            _retry = ModalKit.BindBtn(_board, "list/retry", () =>
            {
                if (_busy) return;
                AudioBus.Tap();
                StartCoroutine(Load());
            });
            _all = ModalKit.BindBtn(_board, "claim_all", ClaimAll);
            _rowTpl = _board.Find("row_tpl") as RectTransform;

            PanelKit.Open(_board);
        }

        void Close()
        {
            if (_busy) return;
            AudioBus.Tap();
            Destroy(gameObject);
        }

        IEnumerator Load()
        {
            if (_status != null)
            {
                _status.gameObject.SetActive(true);
                _status.text = "请稍候…";
            }
            if (_retry != null) _retry.gameObject.SetActive(false);
            yield return Mailbox.Refresh(null);
            if (this == null) yield break;
            Paint();
        }

        void Paint()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            if (Mailbox.Failed)
            {
                if (_slantCount != null) _slantCount.text = "读取失败";
                _status.gameObject.SetActive(true);
                _status.text = "邮箱暂时打不开，请点下方重试";
                _retry.gameObject.SetActive(true);
                _all.gameObject.SetActive(false);
                _content.sizeDelta = Vector2.zero;
                return;
            }

            MailRow[] rows = Mailbox.Rows ?? new MailRow[0];
            if (_slantCount != null) _slantCount.text = $"待领 {rows.Length} 封";
            _retry.gameObject.SetActive(false);
            _status.gameObject.SetActive(rows.Length == 0);
            _status.text = "暂无待领取邮件";
            _all.gameObject.SetActive(rows.Length > 1);
            _content.sizeDelta = new Vector2(0f, rows.Length * RowStep);
            for (int i = 0; i < rows.Length; i++)
                BuildRow(rows[i], i * RowStep);
        }

        void BuildRow(MailRow row, float y)
        {
            if (row == null || _rowTpl == null) return;
            var card = Instantiate(_rowTpl, _content, false);
            card.name = "m";
            card.gameObject.SetActive(true);
            ModalKit.TopLeft(card, 0f, y, RowW, RowH);

            string title = string.IsNullOrEmpty(row.title) ? WechatTitle : row.title;
            string body = string.IsNullOrEmpty(row.body) ? WechatBody : row.body;
            var titleTxt = card.Find("title")?.GetComponent<Text>();
            if (titleTxt != null) titleTxt.text = title;
            var bodyTxt = card.Find("body")?.GetComponent<Text>();
            if (bodyTxt != null) bodyTxt.text = body;

            var expTag = card.Find("exp_tag") as RectTransform;
            string left = DaysLeft(row.expireAt, out bool urgent);
            if (expTag != null)
            {
                bool showExp = !string.IsNullOrEmpty(left);
                expTag.gameObject.SetActive(showExp);
                if (showExp)
                    ModalKit.SetTag(expTag, urgent ? "Ui/modal_tag_coral" : "Ui/modal_tag_slate", left, false);
            }

            int shown = 0;
            for (int i = 0; i < GoodsSpec.Length && shown < 5; i++)
            {
                int n = Count(row, i);
                if (n <= 0) continue;
                var slot = card.Find("s" + shown) as RectTransform;
                if (slot != null)
                {
                    slot.gameObject.SetActive(true);
                    ModalKit.SetItemSlot(slot, GoodsSpec[i].tone, 68, ModalKit.Load(GoodsSpec[i].icon), n.ToString());
                }
                shown++;
            }
            for (int k = shown; k < 5; k++)
            {
                var slot = card.Find("s" + k);
                if (slot != null) slot.gameObject.SetActive(false);
            }

            string id = row.id;
            bool goods = shown > 0;
            var take = card.Find("take")?.GetComponent<Button>();
            if (take != null)
            {
                string kind = goods ? "sm_yellow" : "sm_cyan";
                ModalKit.SetHexBtnGray(take, kind, "sm_gray", false);
                ModalKit.SetBtnLabel(take, kind, goods ? "领取" : "知道了");
                take.onClick.RemoveAllListeners();
                take.onClick.AddListener(() => StartCoroutine(Claim(new[] { id }, take.transform)));
            }
        }

        void ClaimAll()
        {
            if (_busy) return;
            AudioBus.Tap();
            MailRow[] rows = Mailbox.Rows ?? new MailRow[0];
            var ids = new List<string>();
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || string.IsNullOrEmpty(rows[i].id)) continue;
                ids.Add(rows[i].id);
            }
            if (ids.Count == 0) return;
            StartCoroutine(Claim(ids.ToArray(), _all != null ? _all.transform : transform));
        }

        IEnumerator Claim(string[] ids, Transform origin)
        {
            if (_busy) yield break;
            _busy = true;
            Vector2 from = RewardFly.Local(_layer, origin);
            bool ok = false;
            GiftGrant grant = null;
            yield return Mailbox.Claim(_meta, ids, (v, g) => { ok = v; grant = g; });
            _busy = false;
            if (this == null) yield break;
            if (!ok)
            {
                InkToast.Show(_layer, "打不开，请重试");
                yield break;
            }
            _changed?.Invoke();
            Paint();
            Reveal(_layer, _meta, grant, from, _changed);
        }

        static void Reveal(RectTransform layer, MetaProgress meta, GiftGrant grant, Vector2 from, Action changed)
        {
            if (layer == null || meta == null || grant == null) return;
            var pieces = new List<RewardFly.Piece>();
            if (grant.Ink > 0) pieces.Add(RewardFly.Ink(layer, grant.Ink));
            if (grant.Stamina > 0) pieces.Add(RewardFly.Stamina(layer, grant.Stamina));
            if (grant.Diamond > 0) pieces.Add(RewardFly.Diamond(layer, grant.Diamond));
            bool overflow = false;
            for (int i = 0; i < grant.Chests.Count; i++)
            {
                GrantedChest chest = grant.Chests[i];
                if (chest.Slot >= 0) pieces.Add(RewardFly.Chest(layer, chest.Tier, chest.Slot));
                else if (chest.Slot == -1) overflow = true;
            }
            Action next = () => AfterFly(layer, meta, grant, overflow, changed);
            if (pieces.Count > 0) RewardFly.Play(layer, from, pieces.ToArray(), next);
            else next();
        }

        static void AfterFly(RectTransform layer, MetaProgress meta, GiftGrant grant, bool overflow, Action changed)
        {
            if (overflow)
            {
                ChestOverflowView.Show(layer, meta, changed, () => OpenCannon(layer, meta, grant, 0));
                return;
            }
            OpenCannon(layer, meta, grant, 0);
        }

        static void OpenCannon(RectTransform layer, MetaProgress meta, GiftGrant grant, int index)
        {
            if (grant.Cannons == null || index >= grant.Cannons.Count) return;
            ChestLoot loot = grant.Cannons[index];
            if (loot == null)
            {
                OpenCannon(layer, meta, grant, index + 1);
                return;
            }
            ChestOpenView.Show(layer, meta, loot, () => OpenCannon(layer, meta, grant, index + 1));
        }

        static string DaysLeft(long expireAt, out bool urgent)
        {
            urgent = false;
            if (expireAt <= 0) return "";
            long left = expireAt - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (left <= 0)
            {
                urgent = true;
                return "今天到期";
            }
            int days = (int)((left + 86400000L - 1L) / 86400000L);
            urgent = days <= 2;
            return days <= 1 ? "剩 1 天" : "剩 " + days + " 天";
        }

        static int Count(MailRow row, int i)
        {
            switch (i)
            {
                case 0: return row.ink;
                case 1: return row.stamina;
                case 2: return row.diamond;
                case 3: return row.wood;
                case 4: return row.silver;
                case 5: return row.gold;
                case 6: return row.royal;
                default: return row.cannon;
            }
        }
    }
}
