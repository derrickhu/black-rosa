using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 通关排行榜。宣纸卡面 + 朱红飘带标题，前三名挂金银铜牌，
    // 自己那一行钉在底部，列表滚到哪都看得见。
    public sealed class RankPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float RowW = 592f;
        const float RowH = 92f;
        const float RowStep = 102f;
        const float ListTop = 104f;
        const float AuthH = 76f;

        static readonly Color[] Podium = { InkTheme.Hex("FFF1C9"), InkTheme.Hex("EEF0F4"), InkTheme.Hex("FBE3CE") };
        static readonly Color MineFill = InkTheme.Hex("FFF6E6");

        MetaProgress _meta;
        RectTransform _board;
        RectTransform _viewport;
        RectTransform _content;
        RectTransform _footer;
        Button _auth;
        Text _status;
        Button _retry;
        Font _nameFont;
        RankService.Board _data;
        bool _fontReady;
        bool _failed;

        public static RectTransform Show(RectTransform layer, MetaProgress meta)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "rank";
            var panel = dim.gameObject.AddComponent<RankPanel>();
            panel._meta = meta;
            panel.Build(dim);
            return dim;
        }

        void OnDestroy()
        {
            WxBridge.HideProfileButton();
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Build(RectTransform dim)
        {
            float top = ScreenFit.TopPad + 64f;
            float bottom = ScreenFit.BottomPad + 36f;
            float h = Mathf.Max(760f, ScreenFit.CanvasH - top - bottom);
            _board = UiKit.Stroke(dim, "board", new Vector2(0f, top), new Vector2(BoardW, h), Pin.Top,
                6f, null, InkTheme.Hex("FBF4E6"), 28f);

            Sprite rib = InkSprites.Load("Ui/ribbon_chapter");
            float ribW = 380f;
            float ribH = rib != null && rib.rect.width > 1f ? ribW * rib.rect.height / rib.rect.width : 100f;
            var ribbon = UiKit.Art(_board, "ribbon", "Ui/ribbon_chapter", new Vector2(0f, -ribH * 0.46f),
                new Vector2(ribW, ribH), Pin.Top);
            ribbon.GetComponent<Image>().raycastTarget = false;
            var title = UiKit.Label(ribbon, "t", "排行榜", 38, new Vector2(0f, ribH * 0.10f), new Vector2(260f, 56f));
            title.color = InkTheme.CardFace;
            UiKit.Bold(title);

            var sub = UiKit.Label(_board, "sub", "按通关关数排名 · 同关数先到者居前", 22,
                new Vector2(0f, 64f), new Vector2(BoardW - 60f, 30f), TextAnchor.MiddleCenter, Pin.Top);
            sub.color = InkTheme.TextMid;

            BuildClose();

            bool askProfile = WxBridge.CanAskProfile && !RankService.HasProfile;
            float footerH = RowH + 36f + (askProfile ? AuthH + 22f : 0f);
            _footer = UiKit.Panel(_board, "footer", new Vector2(0f, 0f), new Vector2(BoardW, footerH),
                Color.clear, Pin.Bottom);
            _footer.GetComponent<Image>().raycastTarget = false;
            var rule = UiKit.Panel(_footer, "rule", new Vector2(0f, 0f), new Vector2(RowW, 3f),
                InkTheme.Hex("E6D8C2"), Pin.Top);
            rule.GetComponent<Image>().raycastTarget = false;
            if (askProfile)
            {
                _auth = UiKit.Btn(_footer, "auth", "使用微信昵称头像上榜", new Vector2(0f, 20f),
                    new Vector2(470f, AuthH), () => { }, true, Pin.Bottom);
                StartCoroutine(PlaceProfileButton());
            }

            BuildList(footerH);
            SetStatus("排行榜加载中…", false);
            WxBridge.SystemFont(f =>
            {
                if (this == null) return;
                _nameFont = f != null ? f : UiKit.Font;
                _fontReady = true;
                TryFill();
            });
            Reload();
        }

        void BuildClose()
        {
            const float s = 68f;
            var go = new GameObject("close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_board, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.sizeDelta = new Vector2(s, s);
            var ring = go.GetComponent<Image>();
            ring.sprite = UiSprites.Disc();
            ring.color = InkTheme.Outline;
            var face = UiKit.Icon(go.transform, UiSprites.Disc(), Vector2.zero, s - 10f);
            face.color = InkTheme.Seal;
            var x = UiKit.Label(go.transform, "x", "×", 46, new Vector2(0f, 2f), new Vector2(s, s));
            x.color = InkTheme.CardFace;
            UiKit.Bold(x);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = face;
            btn.onClick.AddListener(Close);
        }

        void BuildList(float footerH)
        {
            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(_board, false);
            _viewport = vp.GetComponent<RectTransform>();
            _viewport.anchorMin = new Vector2(0f, 0f);
            _viewport.anchorMax = new Vector2(1f, 1f);
            _viewport.offsetMin = new Vector2(16f, footerH + 4f);
            _viewport.offsetMax = new Vector2(-16f, -ListTop);
            vp.GetComponent<Image>().color = Color.clear;

            var content = new GameObject("rows", typeof(RectTransform));
            content.transform.SetParent(_viewport, false);
            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 0f);

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = _content;
            scroll.viewport = _viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            _status = UiKit.Label(_viewport, "status", "", 26, new Vector2(0f, 20f), new Vector2(RowW, 40f));
            _status.color = InkTheme.TextMid;
            _retry = UiKit.Btn(_viewport, "retry", "重试", new Vector2(0f, -60f), new Vector2(200f, 68f), Reload, false);
        }

        void SetStatus(string text, bool retry)
        {
            _status.gameObject.SetActive(!string.IsNullOrEmpty(text));
            _status.text = text;
            _retry.gameObject.SetActive(retry);
            Transform sh = _viewport.Find("retry_sh");
            if (sh != null) sh.gameObject.SetActive(retry);
        }

        void Reload()
        {
            _data = null;
            _failed = false;
            SetStatus("排行榜加载中…", false);
            RankService.Load(_meta.ClearedCount(), (board, err) =>
            {
                if (this == null) return;
                if (board == null)
                {
                    _failed = true;
                    SetStatus("网络不太好，排行榜没拉下来", true);
                }
                _data = board;
                TryFill();
            });
        }

        void TryFill()
        {
            if (!_fontReady) return;
            if (_data == null)
            {
                if (_failed) FillMine(null);
                return;
            }
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);
            RankRow[] list = _data.List;
            _content.sizeDelta = new Vector2(0f, list.Length * RowStep + 12f);
            for (int i = 0; i < list.Length; i++)
            {
                var row = Row(_content, list[i], false);
                row.anchoredPosition = new Vector2(0f, -(8f + i * RowStep));
            }
            SetStatus(list.Length == 0 ? "还没有人上榜，快去抢第一" : "", false);
            FillMine(_data.Mine);
        }

        void FillMine(RankRow mine)
        {
            Transform old = _footer.Find("mine");
            if (old != null) Destroy(old.gameObject);
            Transform oldSh = _footer.Find("mine_sh");
            if (oldSh != null) Destroy(oldSh.gameObject);
            if (!_fontReady) return;
            if (mine == null)
            {
                mine = new RankRow
                {
                    rank = 0,
                    cleared = _meta.ClearedCount(),
                    displayName = RankService.HasProfile ? RankService.Nick : "我",
                    avatarUrl = RankService.Avatar,
                    isMe = true,
                };
            }
            float y = 18f;
            var row = Row(_footer, mine, true);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -y);
        }

        RectTransform Row(Transform parent, RankRow r, bool pinned)
        {
            bool podium = r.rank >= 1 && r.rank <= 3;
            Color fill = pinned ? MineFill : (podium ? Podium[r.rank - 1] : InkTheme.CardFace);
            Color line = pinned || r.isMe ? InkTheme.Seal : InkTheme.Outline;

            if (pinned)
            {
                var sh = UiKit.Panel(parent, "mine_sh", new Vector2(0f, 0f), new Vector2(RowW, RowH), Color.white, Pin.Top);
                sh.anchoredPosition = new Vector2(0f, -18f - 6f);
                var shImg = sh.GetComponent<Image>();
                shImg.sprite = UiSprites.Shadow(20, 12);
                shImg.type = Image.Type.Sliced;
                shImg.color = new Color(0.23f, 0.16f, 0.12f, 0.30f);
                shImg.raycastTarget = false;
            }

            var root = UiKit.Panel(parent, pinned ? "mine" : "row" + r.rank, Vector2.zero, new Vector2(RowW, RowH), fill, Pin.Top);
            var bg = root.GetComponent<Image>();
            bg.sprite = UiSprites.Fill(20);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;
            var ln = UiKit.Panel(root, "ln", Vector2.zero, Vector2.zero, line);
            ln.anchorMin = Vector2.zero;
            ln.anchorMax = Vector2.one;
            ln.offsetMin = ln.offsetMax = Vector2.zero;
            var lnImg = ln.GetComponent<Image>();
            lnImg.sprite = UiSprites.Line(20, pinned || r.isMe ? 5 : 3);
            lnImg.type = Image.Type.Sliced;
            lnImg.raycastTarget = false;

            float left = -RowW * 0.5f;
            Badge(root, r.rank, new Vector2(left + 52f, 0f));
            Avatar(root, r, new Vector2(left + 140f, 0f));

            var name = UiKit.Label(root, "name", Clip(r.displayName, 8), 28, new Vector2(left + 188f + 130f, 0f),
                new Vector2(260f, 40f), TextAnchor.MiddleLeft);
            name.font = _nameFont;
            name.color = InkTheme.TextDark;
            if (pinned && r.rank <= 0)
            {
                name.rectTransform.anchoredPosition = new Vector2(left + 188f + 130f, 14f);
                var tip = UiKit.Label(root, "tip", r.cleared > 0 ? "上榜中，稍后再看" : "通关第一关即可上榜", 20,
                    new Vector2(left + 188f + 130f, -20f), new Vector2(260f, 28f), TextAnchor.MiddleLeft);
                tip.color = InkTheme.TextMid;
            }

            var num = UiKit.Label(root, "n", r.cleared.ToString(), 40, new Vector2(RowW * 0.5f - 64f - 60f, 2f),
                new Vector2(120f, 50f), TextAnchor.MiddleRight);
            num.color = podium ? InkTheme.Seal : InkTheme.TextDark;
            UiKit.Bold(num);
            var unit = UiKit.Label(root, "u", "关", 24, new Vector2(RowW * 0.5f - 38f, -2f), new Vector2(40f, 36f));
            unit.color = InkTheme.TextMid;

            if (r.isMe) Stamp(root);
            return root;
        }

        static void Badge(Transform row, int rank, Vector2 pos)
        {
            if (rank >= 1 && rank <= 3)
            {
                UiKit.Icon(row, InkSprites.Load("Ui/ico_rank_" + rank), pos + new Vector2(0f, 4f), 84f);
                return;
            }
            UiKit.Icon(row, InkSprites.Load("Ui/ico_rank_plate"), pos, 64f);
            string text = rank <= 0 ? "-" : rank.ToString();
            var t = UiKit.Label(row, "rank", text, rank >= 100 ? 20 : 26, pos, new Vector2(60f, 40f));
            t.color = InkTheme.TextDark;
            UiKit.Bold(t);
        }

        void Avatar(Transform row, RankRow r, Vector2 pos)
        {
            const float s = 66f;
            UiKit.Icon(row, UiSprites.Disc(), pos, s + 6f).color = InkTheme.Outline;
            var maskImg = UiKit.Icon(row, UiSprites.Disc(), pos, s);
            maskImg.color = InkTheme.Seal;
            maskImg.gameObject.name = "avatar";
            maskImg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var seal = UiKit.Label(maskImg.transform, "seal", FirstChar(r.displayName), 32, new Vector2(0f, 1f),
                new Vector2(s, s));
            seal.font = _nameFont;
            seal.color = InkTheme.CardFace;

            RankService.AvatarOf(r.avatarUrl, sprite =>
            {
                if (sprite == null || maskImg == null) return;
                var pic = UiKit.Icon(maskImg.transform, sprite, Vector2.zero, s);
                pic.preserveAspect = false;
                maskImg.color = Color.white;
                seal.gameObject.SetActive(false);
            });
        }

        // 「我」字朱印，斜压在行的左上角。
        static void Stamp(Transform row)
        {
            // 列表第一行上方只留了 8，印章再往上探就会被 RectMask2D 裁掉。
            var box = UiKit.Panel(row, "me", new Vector2(-6f, -6f), new Vector2(40f, 32f), InkTheme.Seal, Pin.TopLeft);
            var img = box.GetComponent<Image>();
            img.sprite = UiSprites.Fill(12);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            box.localRotation = Quaternion.Euler(0f, 0f, 10f);
            var t = UiKit.Label(box, "t", "我", 20, new Vector2(0f, 1f), new Vector2(40f, 32f));
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);
        }

        IEnumerator PlaceProfileButton()
        {
            yield return null;
            if (_auth == null) yield break;
            var corners = new Vector3[4];
            _auth.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            var rect = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
            WxBridge.ShowProfileButton(rect, (nick, avatar) =>
            {
                if (this == null) return;
                if (!RankService.SetProfile(nick, avatar)) return;
                WxBridge.HideProfileButton();
                AudioBus.Tap();
                RectTransform dim = (RectTransform)transform;
                RectTransform layer = (RectTransform)dim.parent;
                Destroy(gameObject);
                Show(layer, _meta);
            });
        }

        static string Clip(string s, int max)
        {
            s = string.IsNullOrEmpty(s) ? "墨客" : s;
            var info = new StringInfo(s);
            if (info.LengthInTextElements <= max) return s;
            return info.SubstringByTextElements(0, max - 1) + "…";
        }

        static string FirstChar(string s)
        {
            if (string.IsNullOrEmpty(s)) return "墨";
            return new StringInfo(s).SubstringByTextElements(0, 1);
        }
    }
}
