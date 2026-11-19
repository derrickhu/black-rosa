using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 排行榜（Figma Panel/首页弹窗/05_排行榜，预制体 Prefabs/Rank）：
    // 通关榜与无尽周榜双页签，前三名金蓝橙底卡与奖章、68×68 通用方形物品框奖励、底部常驻深色「我」排名栏。
    public sealed class RankPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/Rank";
        public const string Mark = "rank_v1";

        const float BoardW = 648f;
        const float BoardH = 988f;
        const float RowW = 600f;
        const float RowH = 100f;
        const float RowStep = 110f;

        static readonly string[] BoardIds = { RankService.BoardStage, RankService.BoardWeekly };
        static readonly string[] BoardNames = { "通关榜", "无尽周榜" };
        static readonly string[] BoardInfos =
        {
            "按通关关数排名 · 同关数先到者居前",
            "每周一 00:00 结算 · 前三档奖励发邮箱"
        };

        MetaProgress _meta;
        RectTransform _board;
        RectTransform _tabOn;
        Text[] _tabTexts;
        Text _infoLabel;
        RectTransform _viewport;
        RectTransform _content;
        RectTransform _footer;
        RectTransform _rowTpl;
        Button _auth;
        Text _status;
        Button _retry;
        Font _sysFont;
        RankService.Board _data;
        bool _fontReady;
        bool _failed;
        bool _authWait;
        int _boardIx;
        int _ask;

        string BoardId => BoardIds[_boardIx];
        bool Waves => BoardId != RankService.BoardStage;

        int ScoreOf(RankRow r)
        {
            switch (BoardId)
            {
                case RankService.BoardWeekly: return r.weekly;
                case RankService.BoardEndless: return r.endless;
                default: return r.cleared;
            }
        }

        public static RectTransform Show(RectTransform layer, MetaProgress meta, string board = RankService.BoardStage)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "rank", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<RankPanel>();
            panel._meta = meta;
            if (board == RankService.BoardEndless) board = RankService.BoardWeekly;
            panel._boardIx = Mathf.Max(0, System.Array.IndexOf(BoardIds, board));
            panel.Bind(stage);
            return dim;
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "rank", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "排行榜", "Ui/ico_act_rank",
                BoardInfos[0], null, null, () => { }, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            var tabBar = ModalKit.Sliced(board, "tabs", "Ui/modal_sub_charcoal", ModalKit.BorderSub16,
                24f, 152f, RowW, 54f, false);
            ModalKit.Sliced(tabBar, "on", "Ui/modal_tab_on", ModalKit.BorderPill12,
                6f, 6f, 292f, 42f, false);
            for (int i = 0; i < BoardIds.Length; i++)
            {
                float tx = 6f + i * 296f;
                var hitGo = new GameObject("tab" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                hitGo.transform.SetParent(tabBar, false);
                ModalKit.TopLeft(hitGo.GetComponent<RectTransform>(), tx, 6f, 292f, 42f);
                var hitImg = hitGo.GetComponent<Image>();
                hitImg.color = new Color(1f, 1f, 1f, 0.01f);
                var btn = hitGo.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = hitImg;
                ModalKit.SolidText(hitGo.transform, "t", BoardNames[i], 18,
                    0f, 0f, 292f, 42f, ModalKit.TextDark, TextAnchor.MiddleCenter, true);
            }

            const float listY = 220f, footerH = 124f;
            float listH = BoardH - listY - footerH - 18f;

            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(board, false);
            var viewport = vp.GetComponent<RectTransform>();
            ModalKit.TopLeft(viewport, 24f, listY, RowW, listH);
            vp.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            var contentGo = new GameObject("rows", typeof(RectTransform));
            contentGo.transform.SetParent(viewport, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            ModalKit.SolidText(viewport, "status", "排行榜加载中…", 20, 0f, listH * 0.36f, RowW, 40f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, true);
            var retry = ModalKit.HexBtn(viewport, "md_cyan", (RowW - 176f) * 0.5f, listH * 0.36f + 52f,
                "重新加载", null, out _);
            retry.gameObject.name = "retry";
            retry.gameObject.SetActive(false);

            var footerGo = new GameObject("footer", typeof(RectTransform));
            footerGo.transform.SetParent(board, false);
            var footer = footerGo.GetComponent<RectTransform>();
            ModalKit.TopLeft(footer, 24f, BoardH - footerH - 12f, RowW, footerH);

            var auth = ModalKit.HexBtn(footer, "wide_yellow", (RowW - 314f) * 0.5f, 122f,
                "使用微信昵称头像上榜", null, out _);
            auth.name = "auth";
            auth.gameObject.SetActive(false);

            BuildRowTemplate(board);
            return dim;
        }

        static void BuildRowTemplate(RectTransform board)
        {
            var root = ModalKit.Sliced(board, "row_tpl", "Ui/modal_rank_row_n", ModalKit.BorderSub16,
                0f, 0f, RowW, RowH, false);

            ModalKit.Pic(root, "medal", "Ui/ico_rank_1", 8f, (RowH - 76f) * 0.5f - 4f, 62f, 76f, true);
            var plate = ModalKit.Pic(root, "plate", "Ui/ico_rank_plate", 16f, (RowH - 48f) * 0.5f, 48f, 48f, true).rectTransform;
            ModalKit.SolidText(plate, "t", "4", 20, 0f, 0f, 48f, 48f,
                ModalKit.TextDark, TextAnchor.MiddleCenter, true);

            var box = ModalKit.Pic(root, "av_box", "Ui/modal_avatar_box", 78f, (RowH - 56f) * 0.5f, 56f, 56f, false).rectTransform;
            var maskGo = new GameObject("mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(box, false);
            ModalKit.TopLeft(maskGo.GetComponent<RectTransform>(), 4f, 4f, 48f, 48f);
            var maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = ModalKit.Load("Ui/modal_avatar_mask");
            maskImg.color = InkTheme.Seal;
            maskImg.raycastTarget = false;
            maskGo.GetComponent<Mask>().showMaskGraphic = true;
            ModalKit.SolidText(maskGo.transform, "seal", "墨", 24, 0f, 0f, 48f, 48f, Color.white, TextAnchor.MiddleCenter, true);
            var pic = ModalKit.Pic(maskGo.transform, "pic", null, 0f, 0f, 48f, 48f, false);
            pic.gameObject.SetActive(false);

            var meTag = ModalKit.Tag(root, "Ui/modal_tag_coral", 86f, (RowH - 56f) * 0.5f - 12f, 40f, 22f, "我", false, 12);
            meTag.name = "me_tag";
            meTag.gameObject.SetActive(false);

            ModalKit.OutlinedText(root, "name", "墨守书生", 20, 150f, 20f, 380f, 30f, ModalKit.TextDark, TextAnchor.MiddleLeft);
            ModalKit.SolidText(root, "sub", "", 15, 150f, 54f, 380f, 26f, ModalKit.TextMuted, TextAnchor.MiddleLeft, false);

            var chestSlot = ModalKit.ItemSlot(root, "orange", 68, 512f, (RowH - 68f) * 0.5f, ModalKit.Load("Ui/chest_cannon"), "1");
            chestSlot.name = "chest_slot";
            chestSlot.gameObject.SetActive(false);

            ModalKit.SolidText(root, "sc", "0", 30, 444f, (RowH - 42f) * 0.5f, 102f, 42f,
                ModalKit.TextDark, TextAnchor.MiddleRight, true);
            ModalKit.SolidText(root, "u", "关", 16, 550f, (RowH - 28f) * 0.5f + 3f, 34f, 28f,
                ModalKit.TextMuted, TextAnchor.MiddleLeft, false);

            root.gameObject.SetActive(false);
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

        void Bind(RectTransform stage)
        {
            _board = stage.Find("board") as RectTransform;
            ModalKit.BindClose(_board, Close);

            _infoLabel = _board.Find("info_strip/t")?.GetComponent<Text>();
            _tabOn = _board.Find("tabs/on") as RectTransform;
            _tabTexts = new Text[BoardIds.Length];
            for (int i = 0; i < BoardIds.Length; i++)
            {
                int ix = i;
                ModalKit.BindBtn(_board, "tabs/tab" + i, () => Pick(ix));
                _tabTexts[i] = _board.Find("tabs/tab" + i + "/t")?.GetComponent<Text>();
            }

            _viewport = _board.Find("list") as RectTransform;
            _content = _board.Find("list/rows") as RectTransform;
            _status = _board.Find("list/status")?.GetComponent<Text>();
            _retry = ModalKit.BindBtn(_board, "list/retry", Reload);
            _footer = _board.Find("footer") as RectTransform;
            _auth = ModalKit.BindBtn(_board, "footer/auth", OnAuthTap);
            _rowTpl = _board.Find("row_tpl") as RectTransform;

            bool askProfile = WxBridge.CanAskProfile && !RankService.HasProfile;
            float footerH = askProfile ? 196f : 124f;
            float listY = 220f;
            float listH = BoardH - listY - footerH - 18f;
            ModalKit.TopLeft(_viewport, 24f, listY, RowW, listH);
            ModalKit.TopLeft(_footer, 24f, BoardH - footerH - 12f, RowW, footerH);
            if (_status != null) ModalKit.TopLeft(_status.rectTransform, 0f, listH * 0.36f, RowW, 40f);
            if (_retry != null) ModalKit.TopLeft(_retry.GetComponent<RectTransform>(), (RowW - 176f) * 0.5f, listH * 0.36f + 52f, 176f, 63f);

            if (_auth != null) _auth.gameObject.SetActive(askProfile);
            if (askProfile)
            {
                WxBridge.EnsurePrivacy(null);
                StartCoroutine(PlaceWhenReady());
            }

            PaintTabs();
            PanelKit.Enlarge(_board);
            SetStatus("排行榜加载中…", false);

            _fontReady = true;
            WxBridge.SystemFont(f =>
            {
                if (this == null || f == null || f == _sysFont) return;
                _sysFont = f;
                TryFill();
            });
            PanelKit.Open(_board);
            Reload();
        }

        void OnAuthTap()
        {
            if (WxBridge.PrivacyAgreed) return;
            AudioBus.Tap();
            WxBridge.EnsurePrivacy(null);
            StartCoroutine(PlaceWhenReady());
        }

        IEnumerator PlaceWhenReady()
        {
            if (_authWait) yield break;
            _authWait = true;
            if (!WxBridge.PrivacyAgreed)
            {
                bool done = false, ok = false;
                WxBridge.EnsurePrivacy(agreed => { ok = agreed; done = true; });
                float until = Time.realtimeSinceStartup + 20f;
                while (!done && Time.realtimeSinceStartup < until) yield return null;
                if (!ok || this == null)
                {
                    _authWait = false;
                    if (this != null)
                        InkToast.Show((RectTransform)transform.parent, "需同意隐私协议后才能用微信昵称上榜");
                    yield break;
                }
            }
            _authWait = false;
            yield return PlaceProfileButton();
        }

        void SetStatus(string text, bool retry)
        {
            if (_status != null)
            {
                _status.gameObject.SetActive(!string.IsNullOrEmpty(text));
                _status.text = text;
            }
            if (_retry != null) _retry.gameObject.SetActive(retry);
        }

        void Pick(int ix)
        {
            if (ix == _boardIx) return;
            AudioBus.Tap();
            _boardIx = ix;
            PaintTabs();
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);
            _content.sizeDelta = Vector2.zero;
            _content.anchoredPosition = Vector2.zero;
            Reload();
        }

        void PaintTabs()
        {
            if (_tabOn != null) ModalKit.TopLeft(_tabOn, 6f + _boardIx * 296f, 6f, 292f, 42f);
            for (int i = 0; i < _tabTexts.Length; i++)
                if (_tabTexts[i] != null)
                    _tabTexts[i].color = i == _boardIx ? ModalKit.TextDark : InkTheme.Hex("CFC6B8");
            if (_infoLabel != null) _infoLabel.text = BoardInfos[_boardIx];
        }

        void Reload()
        {
            _data = null;
            _failed = false;
            SetStatus("排行榜加载中…", false);
            int ask = ++_ask;
            RankService.Load(BoardId, _meta, (board, err) =>
            {
                if (this == null || ask != _ask) return;
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
            _content.sizeDelta = new Vector2(0f, list.Length * RowStep + 8f);
            for (int i = 0; i < list.Length; i++)
                BuildRow(_content, list[i], false, i * RowStep);
            SetStatus(list.Length == 0 ? "还没有人上榜，快去抢第一" : "", false);
            FillMine(_data.Mine);
        }

        void FillMine(RankRow mine)
        {
            Transform old = _footer.Find("mine");
            if (old != null) Destroy(old.gameObject);
            if (!_fontReady) return;
            if (mine == null)
            {
                RankService.Scores s = RankService.ScoresOf(_meta);
                mine = new RankRow
                {
                    rank = 0,
                    cleared = s.Cleared,
                    endless = s.Endless,
                    weekly = s.Weekly,
                    daily = s.Daily,
                    displayName = RankService.HasProfile ? RankService.Nick : "墨守书生",
                    avatarUrl = RankService.Avatar,
                    isMe = true,
                };
            }
            BuildRow(_footer, mine, true, 0f);
        }

        Font ResolveNickFont(string text, bool bold)
        {
            Font baseFont = bold ? UiKit.FontBold : UiKit.Font;
            if (_sysFont == null || string.IsNullOrEmpty(text) || baseFont == null) return baseFont;
            for (int i = 0; i < text.Length; i++)
            {
                if (!baseFont.HasCharacter(text[i])) return _sysFont;
            }
            return baseFont;
        }

        RectTransform BuildRow(Transform parent, RankRow r, bool pinned, float y)
        {
            if (_rowTpl == null) return null;
            float rowH = pinned ? 114f : RowH;
            string rowKey = pinned ? "Ui/modal_rank_row_me"
                : r.rank == 1 ? "Ui/modal_rank_row_1"
                : r.rank == 2 ? "Ui/modal_rank_row_2"
                : r.rank == 3 ? "Ui/modal_rank_row_3"
                : "Ui/modal_rank_row_n";
            Vector4 rowBorder = pinned ? ModalKit.BorderSub18 : ModalKit.BorderSub16;

            var root = Instantiate(_rowTpl, parent, false);
            root.name = pinned ? "mine" : "row" + r.rank;
            root.gameObject.SetActive(true);
            ModalKit.TopLeft(root, 0f, y, RowW, rowH);
            var bgImg = root.GetComponent<Image>();
            if (bgImg != null) bgImg.sprite = ModalKit.Load2x(rowKey, rowBorder);

            bool top3 = r.rank >= 1 && r.rank <= 3;
            var medalTr = root.Find("medal") as RectTransform;
            var plateTr = root.Find("plate") as RectTransform;
            if (medalTr != null)
            {
                medalTr.gameObject.SetActive(top3);
                if (top3)
                {
                    ModalKit.TopLeft(medalTr, 8f, (rowH - 76f) * 0.5f - 4f, 62f, 76f);
                    var mImg = medalTr.GetComponent<Image>();
                    if (mImg != null) mImg.sprite = ModalKit.Load("Ui/ico_rank_" + r.rank);
                }
            }
            if (plateTr != null)
            {
                plateTr.gameObject.SetActive(!top3);
                if (!top3)
                {
                    ModalKit.TopLeft(plateTr, 16f, (rowH - 48f) * 0.5f, 48f, 48f);
                    var pt = plateTr.Find("t")?.GetComponent<Text>();
                    if (pt != null)
                    {
                        pt.text = r.rank <= 0 ? "-" : r.rank.ToString();
                        pt.fontSize = UiKit.ReadSize(r.rank >= 100 ? 16 : 20);
                    }
                }
            }

            float avX = pinned ? 86f : 78f;
            float avY = (rowH - 56f) * 0.5f;
            var avBox = root.Find("av_box") as RectTransform;
            if (avBox != null)
            {
                ModalKit.TopLeft(avBox, avX, avY, 56f, 56f);
                BindAvatar(avBox, r);
            }

            var meTag = root.Find("me_tag") as RectTransform;
            if (meTag != null)
            {
                bool showMe = pinned || r.isMe;
                meTag.gameObject.SetActive(showMe);
                if (showMe) ModalKit.TopLeft(meTag, avX + 8f, avY - 12f, 40f, 22f);
            }

            float textX = pinned ? 158f : 150f;
            int score = ScoreOf(r);
            string chestKey = WeeklyChest(r.rank, out string chestTone);
            // 有奖励时宝箱在波数左边，名字让开这一截。
            const float ChestX = 384f;
            float textW = chestKey != null ? Mathf.Max(160f, ChestX - textX - 12f) : 380f;

            string display = Clip(r.displayName, 8);
            var nameText = root.Find("name")?.GetComponent<Text>();
            if (nameText != null)
            {
                ModalKit.TopLeft(nameText.rectTransform, textX, pinned ? 22f : 20f, textW, 30f);
                nameText.text = display;
                nameText.color = pinned ? Color.white : ModalKit.TextDark;
                nameText.font = ResolveNickFont(display, true);
                var outline = nameText.GetComponent<Outline>();
                if (outline != null) outline.enabled = pinned;
            }

            string subLine = pinned && r.rank <= 0
                ? (score > 0 ? $"当前成绩：{score}{(Waves ? " 波" : " 关")}（上榜同步中）"
                    : Waves ? "本周无尽打完第 1 波即可上榜" : "通关第 1 关即可上榜")
                : (Waves ? $"最高坚守 第 {score} 波" : $"主线已通关 第 {score} 关");
            var subText = root.Find("sub")?.GetComponent<Text>();
            if (subText != null)
            {
                ModalKit.TopLeft(subText.rectTransform, textX, pinned ? 60f : 54f, textW, 26f);
                subText.text = subLine;
                subText.color = pinned ? ModalKit.GoldLight : ModalKit.TextMuted;
            }

            var chestSlot = root.Find("chest_slot") as RectTransform;
            var scText = root.Find("sc")?.GetComponent<Text>();
            var uText = root.Find("u")?.GetComponent<Text>();

            if (chestSlot != null)
            {
                chestSlot.gameObject.SetActive(chestKey != null);
                if (chestKey != null)
                {
                    ModalKit.TopLeft(chestSlot, ChestX, (rowH - 68f) * 0.5f, 68f, 68f);
                    ModalKit.SetItemSlot(chestSlot, chestTone, 68, ModalKit.Load(chestKey), "1");
                }
            }
            if (scText != null)
            {
                scText.gameObject.SetActive(true);
                ModalKit.TopLeft(scText.rectTransform, chestKey != null ? 462f : 444f, (rowH - 42f) * 0.5f,
                    chestKey != null ? 86f : 102f, 42f);
                scText.text = score.ToString();
                scText.color = pinned ? ModalKit.GoldLight : (top3 ? InkTheme.Hex("C84828") : ModalKit.TextDark);
            }
            if (uText != null)
            {
                uText.gameObject.SetActive(true);
                ModalKit.TopLeft(uText.rectTransform, chestKey != null ? 552f : 550f, (rowH - 28f) * 0.5f + 3f, 34f, 28f);
                uText.text = Waves ? "波" : "关";
                uText.color = pinned ? ModalKit.CreamLight : ModalKit.TextMuted;
            }

            return root;
        }

        string WeeklyChest(int rank, out string tone)
        {
            tone = "orange";
            if (BoardId != RankService.BoardWeekly) return null;
            if (rank == 1) { tone = "orange"; return "Ui/chest_cannon"; }
            if (rank == 2) { tone = "orange"; return "Ui/chest_royal"; }
            if (rank == 3) { tone = "purple"; return "Ui/chest_gold"; }
            return null;
        }

        void BindAvatar(RectTransform box, RankRow r)
        {
            var maskTr = box.Find("mask");
            if (maskTr == null) return;
            var maskImg = maskTr.GetComponent<Image>();
            var seal = maskTr.Find("seal")?.GetComponent<Text>();
            var pic = maskTr.Find("pic")?.GetComponent<Image>();

            string ch = FirstChar(r.displayName);
            if (seal != null)
            {
                seal.gameObject.SetActive(true);
                seal.text = ch;
                seal.font = ResolveNickFont(ch, true);
            }
            if (pic != null) pic.gameObject.SetActive(false);
            if (maskImg != null) maskImg.color = InkTheme.Seal;

            RankService.AvatarOf(r.avatarUrl, sprite =>
            {
                if (sprite == null || maskImg == null || pic == null) return;
                pic.sprite = sprite;
                pic.gameObject.SetActive(true);
                maskImg.color = Color.white;
                if (seal != null) seal.gameObject.SetActive(false);
            });
        }

        IEnumerator PlaceProfileButton()
        {
            RectTransform rt = _auth.GetComponent<RectTransform>();
            float still = 0f;
            Rect last = default;
            while (_auth != null && still < 0.12f)
            {
                Rect now = PanelKit.ScreenRect(rt);
                if (Mathf.Abs(now.x - last.x) > 0.5f || Mathf.Abs(now.y - last.y) > 0.5f
                    || Mathf.Abs(now.width - last.width) > 0.5f || Mathf.Abs(now.height - last.height) > 0.5f)
                {
                    last = now;
                    still = 0f;
                }
                else still += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_auth == null) yield break;
            WxBridge.ShowProfileButton(last, (nick, avatar, err) =>
            {
                if (this == null) return;
                if (string.IsNullOrEmpty(nick))
                {
                    AuthFailed(err ?? "");
                    return;
                }
                if (!RankService.SetProfile(nick, avatar))
                {
                    InkToast.Show((RectTransform)transform.parent, "昵称保存失败，请稍后再试");
                    return;
                }
                WxBridge.HideProfileButton();
                AudioBus.Tap();
                RectTransform dim = (RectTransform)transform;
                RectTransform layer = (RectTransform)dim.parent;
                Destroy(gameObject);
                Show(layer, _meta, BoardIds[_boardIx]);
            });
        }

        void AuthFailed(string err)
        {
            var layer = (RectTransform)transform.parent;
            string e = err.ToLowerInvariant();
            if (e.Contains("privacy") || e.Contains("not declared") || e.StartsWith("112 "))
            {
                InkToast.Show(layer, "隐私指引未声明「用户信息」，暂时不能用昵称上榜");
                return;
            }
            if (e.Contains("deny") || e.Contains("auth"))
            {
                WxBridge.AskOpenSetting("之前拒绝过使用微信昵称和头像，去设置里打开「用户信息」后就能上榜。", ok =>
                {
                    if (this == null) return;
                    InkToast.Show(layer, ok ? "已允许，再点一次就能上榜" : "没有打开「用户信息」，排行榜里会显示默认名字");
                });
                return;
            }
            InkToast.Show(layer, "没有拿到微信昵称（" + (string.IsNullOrEmpty(err) ? "未知原因" : Clip(err, 24)) + "）");
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
