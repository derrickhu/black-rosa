using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 邮箱。未领的信列在这里，领取后进存档。没有默认信。
    public sealed class MailPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 900f;
        const float RowW = 580f;
        const float RowH = 156f;
        const float RowStep = 168f;
        const float ListTop = 128f;
        const float FooterH = 132f;

        const string WechatTitle = "微信礼包";
        const string WechatBody = "礼包已送到，打开领取";

        static readonly string[] Icons =
        {
            "Ui/ico_ink", "Ui/ico_stamina", "Ui/ico_diamond",
            "Ui/chest_wood", "Ui/chest_silver", "Ui/chest_gold", "Ui/chest_royal", "Ui/chest_cannon"
        };

        MetaProgress _meta;
        Action _changed;
        RectTransform _board;
        RectTransform _content;
        Text _status;
        Button _retry;
        Button _all;
        bool _busy;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "mail";
            var panel = dim.gameObject.AddComponent<MailPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel.Build(dim);
            panel.StartCoroutine(panel.Load());
        }

        void Close()
        {
            if (_busy) return;
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Build(RectTransform dim)
        {
            _board = PanelKit.Board(dim, "邮箱", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, Close);

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(_board, false);
            var view = vp.GetComponent<RectTransform>();
            view.anchorMin = new Vector2(0f, 0f);
            view.anchorMax = new Vector2(1f, 1f);
            view.offsetMin = new Vector2(16f, FooterH);
            view.offsetMax = new Vector2(-16f, -ListTop);
            vp.GetComponent<Image>().color = Color.clear;

            var rows = new GameObject("rows", typeof(RectTransform));
            rows.transform.SetParent(view, false);
            _content = rows.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = _content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            _status = UiKit.Label(view, "status", "请稍候", 28, Vector2.zero, new Vector2(RowW, 48f));
            _status.color = InkTheme.TextMid;
            _retry = UiKit.Btn(view, "retry", "重试", new Vector2(0f, -70f), new Vector2(200f, 72f), () =>
            {
                if (_busy) return;
                AudioBus.Tap();
                StartCoroutine(Load());
            }, false);
            Show(_retry, false);

            _all = UiKit.Btn(_board, "all", "全部领取", new Vector2(0f, 36f), new Vector2(430f, 88f), ClaimAll, true, Pin.Bottom);
            Show(_all, false);
        }

        IEnumerator Load()
        {
            _status.gameObject.SetActive(true);
            _status.text = "请稍候";
            Show(_retry, false);
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
                _status.gameObject.SetActive(true);
                _status.text = "打不开，点重试";
                Show(_retry, true);
                Show(_all, false);
                _content.sizeDelta = new Vector2(0f, 0f);
                return;
            }

            MailRow[] rows = Mailbox.Rows ?? new MailRow[0];
            Show(_retry, false);
            _status.gameObject.SetActive(rows.Length == 0);
            _status.text = "暂无邮件";
            Show(_all, rows.Length > 1);
            _content.sizeDelta = new Vector2(0f, rows.Length * RowStep);
            for (int i = 0; i < rows.Length; i++)
                BuildRow(rows[i], i * RowStep);
        }

        void BuildRow(MailRow row, float y)
        {
            if (row == null) return;
            var card = UiKit.Stroke(_content, "m", new Vector2(0f, y), new Vector2(RowW, RowH), Pin.Top, 4f, null, InkTheme.CardFace, 18f);
            string title = string.IsNullOrEmpty(row.title) ? WechatTitle : row.title;
            string body = string.IsNullOrEmpty(row.body) ? WechatBody : row.body;
            var head = UiKit.Label(card, "title", title, 28, new Vector2(-70f, 16f), new Vector2(400f, 40f), TextAnchor.MiddleLeft, Pin.Top);
            head.color = InkTheme.TextDark;
            UiKit.Bold(head);
            var sub = UiKit.Label(card, "body", body, 20, new Vector2(-70f, 52f), new Vector2(400f, 30f), TextAnchor.MiddleLeft, Pin.Top);
            sub.color = InkTheme.TextMid;

            int shown = 0;
            for (int i = 0; i < Icons.Length; i++)
            {
                int n = Count(row, i);
                if (n <= 0) continue;
                float x = -RowW * 0.5f + 36f + shown * 48f;
                UiKit.Icon(card, InkSprites.Load(Icons[i]), new Vector2(x, -18f), 40f);
                var num = UiKit.Label(card, "n" + i, "×" + n, 16, new Vector2(x, -46f), new Vector2(48f, 22f));
                num.color = InkTheme.Seal;
                shown++;
            }

            string id = row.id;
            bool goods = shown > 0;
            UiKit.Btn(card, "take", goods ? "领取" : "知道了", new Vector2(RowW * 0.5f - 78f, 0f), new Vector2(132f, 64f),
                () => StartCoroutine(Claim(new[] { id }, goods)), goods);
        }

        void ClaimAll()
        {
            if (_busy) return;
            AudioBus.Tap();
            MailRow[] rows = Mailbox.Rows ?? new MailRow[0];
            var ids = new List<string>();
            bool goods = false;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || string.IsNullOrEmpty(rows[i].id)) continue;
                ids.Add(rows[i].id);
                if (HasGoods(rows[i])) goods = true;
            }
            if (ids.Count == 0) return;
            StartCoroutine(Claim(ids.ToArray(), goods));
        }

        IEnumerator Claim(string[] ids, bool goods)
        {
            if (_busy) yield break;
            _busy = true;
            bool ok = false;
            yield return Mailbox.Claim(_meta, ids, v => ok = v);
            _busy = false;
            if (this == null) yield break;
            if (!ok)
            {
                InkToast.Show((RectTransform)transform.parent, "打不开，点重试");
                yield break;
            }
            if (goods) InkToast.Show((RectTransform)transform.parent, "邮件已领");
            if (_changed != null) _changed();
            Paint();
        }

        static void Show(Button btn, bool on)
        {
            if (btn == null) return;
            btn.gameObject.SetActive(on);
            Transform sh = btn.transform.parent != null ? btn.transform.parent.Find(btn.name + "_sh") : null;
            if (sh != null) sh.gameObject.SetActive(on);
        }

        static bool HasGoods(MailRow row)
        {
            for (int i = 0; i < Icons.Length; i++)
                if (Count(row, i) > 0) return true;
            return false;
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