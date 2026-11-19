using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴三栏（Figma Panel/首页弹窗/06_图鉴 1:1 切图版）：
    // 字谱、秘卷、墨谱三卷切换，上方通用凹槽进度条 + 3 枚通用方形物品框里程碑，下方 4 列字卡网格。
    public sealed class CodexPanel : MonoBehaviour
    {
        public static RectTransform Slab(Transform parent, string name, string key, Vector2 pos, Vector2 size,
            Pin pin = Pin.Top, float refH = 0f)
        {
            var rt = UiKit.Art(parent, name, key, pos, size, pin);
            var img = rt.GetComponent<Image>();
            img.raycastTarget = false;
            if (img.sprite != null && img.type == Image.Type.Sliced)
                img.pixelsPerUnitMultiplier = img.sprite.rect.height / Mathf.Max(1f, refH > 0f ? refH : size.y);
            return rt;
        }

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        ScrollRect _list;
        RectTransform _cell;
        CodexTab _tab;
        bool[] _seenCards;
        bool[] _seenEnemies;
        float _scroll = -1f;
        UiAnim _anim;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = CodexLayout.Main(layer);
            var panel = dim.gameObject.AddComponent<CodexPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel._seenCards = CodexCatalog.SeenCards(meta);
            panel._seenEnemies = CodexCatalog.SeenEnemies(meta);
            panel._anim = UiAnim.On(panel);
            panel._board = (RectTransform)(dim.Find("stage/board") ?? dim.Find("board"));
            var title = panel._board.Find("head/title")?.GetComponent<Text>();
            if (title != null) title.text = "图鉴";
            panel.Wire();
            panel.Refresh();
            PanelKit.Enlarge(panel._board);
            PanelKit.Open(panel._board);
        }

        void Wire()
        {
            var closeBtn = _board.Find("close")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Close);
            }
            Transform tabsHost = _board.Find("tabs") ?? _board;
            for (int i = 0; i < CodexLayout.Tabs.Length; i++)
            {
                CodexTab tab = CodexLayout.Tabs[i];
                var btn = tabsHost.Find("tab" + i)?.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => Switch(tab));
                }
            }
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var mBtn = _board.Find("mile" + i)?.GetComponent<Button>();
                if (mBtn != null)
                {
                    mBtn.onClick.RemoveAllListeners();
                    mBtn.onClick.AddListener(() => Claim(idx));
                }
            }
            _list = _board.Find("list").GetComponent<ScrollRect>();
            _cell = (RectTransform)_board.Find("cell");
            _list.onValueChanged.AddListener(v => _scroll = v.y);
        }

        void Close()
        {
            AudioBus.Tap();
            _changed?.Invoke();
            Destroy(gameObject);
        }

        void Refresh()
        {
            float keep = _scroll;
            PaintTabs();
            PaintProgress();
            var hint = _board.Find("hint")?.GetComponent<Text>();
            if (hint != null) hint.text = CodexCatalog.TabHint(_tab);
            FillGrid(CodexCatalog.Of(_tab), keep);
        }

        void Switch(CodexTab tab)
        {
            if (_tab == tab) return;
            AudioBus.Tap();
            _tab = tab;
            _scroll = -1f;
            Refresh();
        }

        void PaintTabs()
        {
            Transform tabsHost = _board.Find("tabs") ?? _board;
            RectTransform tabOn = tabsHost.Find("tab_on") as RectTransform;
            for (int i = 0; i < CodexLayout.Tabs.Length; i++)
            {
                CodexTab tab = CodexLayout.Tabs[i];
                bool on = _tab == tab;
                if (on && tabOn != null)
                    ModalKit.TopLeft(tabOn, 6f + i * 198f, 6f, 192f, 42f);

                Transform plate = tabsHost.Find("tab" + i);
                if (plate == null) continue;
                var txt = plate.Find("t")?.GetComponent<Text>();
                if (txt != null)
                    txt.color = on ? ModalKit.TextDark : InkTheme.Hex("CFC6B8");
                plate.Find("dot")?.gameObject.SetActive(_meta.CodexTabNew(tab));
            }
        }

        void PaintProgress()
        {
            IReadOnlyList<CodexEntry> list = CodexCatalog.Of(_tab);
            int known = CodexCatalog.CountKnown(_meta, list);
            CodexMile[] miles = CodexCatalog.Miles(_tab);
            float ratio = list.Count > 0 ? (float)known / list.Count : 0f;

            var slantTxt = _board.Find("slant_tag/t")?.GetComponent<Text>();
            if (slantTxt != null) slantTxt.text = $"{known} / {list.Count}";

            Transform track = _board.Find("track");
            if (track != null)
            {
                var fill = track.Find("fill") as RectTransform;
                ModalKit.SetProgress(fill, CodexLayout.TrackW, CodexLayout.TrackH, ratio);
                var cnt = track.Find("count")?.GetComponent<Text>();
                if (cnt != null) cnt.text = $"已收录 {known} / {list.Count}";
            }

            for (int i = 0; i < miles.Length && i < 3; i++)
            {
                bool reached = known >= miles[i].Need;
                bool claimed = _meta.MileClaimed(_tab, i);
                bool ready = reached && !claimed;
                Transform host = _board.Find("mile" + i);
                if (host == null) continue;

                var img = host.GetComponent<Image>();
                if (img != null) img.color = claimed ? new Color(0.84f, 0.82f, 0.78f, 1f) : Color.white;

                var needLab = host.Find("need")?.GetComponent<Text>();
                var inkLab = host.Find("ink")?.GetComponent<Text>();
                if (needLab != null)
                {
                    needLab.text = claimed ? "已领取" : $"{miles[i].Need}个";
                    needLab.color = ready ? InkTheme.Hex("C84828") : ModalKit.TextBody;
                }
                if (inkLab != null)
                    inkLab.text = miles[i].Ink.ToString();

                host.Find("done_chk")?.gameObject.SetActive(claimed);
                host.Find("ready_tag")?.gameObject.SetActive(ready);

                var pulse = UiAnim.On(host);
                pulse.Clear();
                host.localScale = Vector3.one;
                if (ready) pulse.Breathe(host, 0f, 0.045f, 1.15f);
            }
        }

        void Claim(int i)
        {
            int ink = _meta.ClaimMile(_tab, i);
            if (ink <= 0)
            {
                AudioBus.Tap();
                CodexMile mile = CodexCatalog.Miles(_tab)[i];
                if (_meta.MileClaimed(_tab, i))
                    InkToast.Show(_layer, "这档奖励已经领过了");
                else
                    InkToast.Show(_layer, "再收录 " + (mile.Need - CodexCatalog.CountKnown(_meta, CodexCatalog.Of(_tab))) + " 个可领");
                return;
            }
            AudioBus.UnlockSting();
            Transform ticket = _board.Find("mile" + i);
            Vector2 from = RewardFly.Local(_layer, ticket);
            RewardFly.Play(_layer, from, new[] { RewardFly.Ink(_layer, ink) });
            InkToast.Show(_layer, $"领到收集奖励 墨×{ink}");
            Refresh();
        }

        void FillGrid(IReadOnlyList<CodexEntry> list, float keep)
        {
            RectTransform content = _list.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject old = content.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            int rows = (list.Count + CodexLayout.Cols - 1) / CodexLayout.Cols;
            content.sizeDelta = new Vector2(600f, rows * (CodexLayout.CellH + CodexLayout.Gap) + 20f);
            for (int i = 0; i < list.Count; i++) Cell(content, list[i], i);
            Canvas.ForceUpdateCanvases();
            _list.verticalNormalizedPosition = keep >= 0f ? keep : 1f;
        }

        void Cell(RectTransform content, CodexEntry e, int i)
        {
            const float cellW = CodexLayout.CellW;
            const float cellH = CodexLayout.CellH;
            const int cols = CodexLayout.Cols;
            float x = CodexLayout.GridPad + (i % cols) * (cellW + CodexLayout.GapX);
            float y = 8f + (i / cols) * (cellH + CodexLayout.Gap);
            bool[] seen = e.Kind == CodexKind.Enemy ? _seenEnemies : _seenCards;
            CodexState st = CodexCatalog.StateOf(_meta, e, seen);
            bool known = st == CodexState.Known;

            var card = Instantiate(_cell, content, false);
            card.name = "c" + i;
            ModalKit.TopLeft(card, x, y, cellW, cellH);
            card.gameObject.SetActive(true);

            var cardImg = card.GetComponent<Image>();
            cardImg.sprite = InkSprites.Load(CardFaceOf(e, st));
            card.GetComponent<Button>().onClick.AddListener(() => Open(e, st));

            var name = card.Find("n").GetComponent<Text>();
            Transform qMark = card.Find("q");
            if (st == CodexState.Unknown)
            {
                if (qMark != null) qMark.gameObject.SetActive(true);
                bool hinted = e.Kind == CodexKind.Pair && CodexCatalog.PairHinted(_meta, e.Index)
                    || e.Kind == CodexKind.Trio && CodexCatalog.TrioHinted(_meta, e.Index);
                name.text = hinted ? "似曾相识" : "未收录";
                name.color = hinted ? InkTheme.Hex("FFE455") : InkTheme.Hex("AEA296");
                return;
            }
            if (qMark != null) qMark.gameObject.SetActive(false);

            Color tint = known ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            if (e.Kind == CodexKind.Glyph)
                Put(card, "art", InkArt.Bare(InkArt.Glyph((CardId)e.Index, 256)), 104f, tint);
            else if (e.Kind == CodexKind.Word)
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int k = 0; k < parts.Length && k < 2; k++)
                    Put(card, k == 0 ? "ga" : "gb", InkArt.Bare(InkArt.Glyph(parts[k], 256)), 58f, tint);
            }
            else if (e.Kind == CodexKind.Enemy)
                Put(card, "art", InkSprites.Person((EnemyId)e.Index), 104f,
                    known ? Color.white : new Color(0.3f, 0.26f, 0.22f, 0.6f));
            else if (e.Kind == CodexKind.Trio && e.Index == 1)
                ShowArt(card, CloudArrowFrames(), 104f, tint, 180f);
            else if (e.Kind == CodexKind.Trio)
                ShowArt(card, TrioFrames(e.Index), 104f, tint, 0f, e.Index == 0 ? -160f : 0f);
            else
            {
                ShotFx pForm = SignaturePairs.All[e.Index].Form;
                float pSpin = pForm == ShotFx.FormWhirl ? -150f : pForm == ShotFx.FormCollapse ? 105f : 0f;
                ShowArt(card, PairFrames(e.Index), 104f, tint, 0f, pSpin);
            }

            name.text = CodexCatalog.Title(e);
            name.color = known ? Color.white : InkTheme.Hex("D8D0C6");
            card.Find("lock")?.gameObject.SetActive(!known);
            card.Find("dot")?.gameObject.SetActive(known && _meta.CodexIsNew(e.Kind, e.Index));
        }

        public static string CardFaceOf(CodexEntry e, CodexState st)
        {
            if (st == CodexState.Unknown) return "Ui/codex_card_locked";
            if (st == CodexState.Seen) return "Ui/study_card_gray";
            switch (e.Kind)
            {
                case CodexKind.Trio: return "Ui/study_card_gold";
                case CodexKind.Pair: return "Ui/study_card_purple";
                case CodexKind.Word: return "Ui/study_card_purple";
                case CodexKind.Enemy:
                    return e.Index >= 8 ? "Ui/study_card_purple"
                        : e.Index >= 4 ? "Ui/study_card_blue"
                        : "Ui/study_card_green";
                default:
                    int idx = e.Index;
                    if (idx >= 13) return "Ui/study_card_gold";
                    if (idx % 3 == 0) return "Ui/study_card_purple";
                    if (idx % 3 == 1) return "Ui/study_card_blue";
                    return "Ui/study_card_green";
            }
        }

        public static void Put(Transform host, string path, Sprite sprite, float size, Color tint)
        {
            Transform t = host.Find(path);
            if (t == null || sprite == null) return;
            var img = t.GetComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.preserveAspect = true;
            RectTransform rt = img.rectTransform;
            // 把轴心归到格子正中：既让窄长形（定海针）水平居中，又避免穿云箭旋转 180° 甩出米字格。
            Vector2 center = rt.anchoredPosition + new Vector2(
                rt.sizeDelta.x * (0.5f - rt.pivot.x),
                rt.sizeDelta.y * (0.5f - rt.pivot.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = center;
            rt.localEulerAngles = Vector3.zero;
            CodexReel reel = img.GetComponent<CodexReel>();
            if (reel != null) reel.enabled = false;
            t.gameObject.SetActive(true);
        }

        static void ShowArt(Transform card, Sprite[] frames, float size, Color tint, float spin = 0f, float spinSpeed = 0f)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            Put(card, "art", frames[0], size, tint);
            Image img = card.Find("art").GetComponent<Image>();
            img.preserveAspect = true;
            img.rectTransform.localEulerAngles = new Vector3(0f, 0f, spin);
            if (frames.Length < 2 && Mathf.Abs(spinSpeed) < 0.01f) return;
            CodexReel reel = img.GetComponent<CodexReel>();
            if (reel == null) reel = img.gameObject.AddComponent<CodexReel>();
            reel.Frames = frames;
            reel.Tint = tint;
            reel.BaseAngle = spin;
            reel.SpinSpeed = spinSpeed;
            reel.enabled = true;
        }

        public static Sprite[] PairFrames(int pair)
        {
            ShotFx form = SignaturePairs.All[pair].Form;
            if (InkVfx.Flat(form, out InkVfx.FlatBody body))
            {
                Sprite[] frames = InkVfx.Frames(body.Frames, body.Count);
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames;
            }
            string key = GlowShot.FormKey(form);
            if (key == null) return null;
            int n = InkVfx.CountFrames(key + "_shot");
            if (n > 1)
            {
                Sprite[] frames = InkVfx.Frames(key + "_shot", n);
                if (frames != null && frames[0] != null) return frames;
            }
            Sprite one = GlowShot.Poster(key);
            return one != null ? new[] { one } : null;
        }

        void Open(CodexEntry e, CodexState st)
        {
            AudioBus.Tap();
            if (st == CodexState.Unknown)
            {
                string tip = e.Kind == CodexKind.Trio ? "三字同列，方见真形"
                    : e.Kind == CodexKind.Pair ? "两字同弹，方见真形" : "还没在关卡里遇见过";
                InkToast.Show(_layer, tip);
                return;
            }
            if (st == CodexState.Known) _meta.CodexSeen(e.Kind, e.Index);
            CodexDetail.Show(_layer, e, st == CodexState.Known, Refresh);
        }

        public static Sprite PairFrame(int pair)
        {
            ShotFx form = SignaturePairs.All[pair].Form;
            if (InkVfx.Flat(form, out InkVfx.FlatBody body))
            {
                Sprite[] frames = InkVfx.Frames(body.Frames, body.Count);
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            string key = GlowShot.FormKey(form);
            return key != null ? GlowShot.Poster(key) : null;
        }

        public static Sprite[] CloudArrowFrames()
        {
            int n = InkVfx.CountFrames("cloud_arrow");
            if (n > 0)
            {
                Sprite[] frames = InkVfx.Frames("cloud_arrow", n);
                if (frames != null && frames[0] != null) return frames;
            }
            Sprite one = GlowShot.Poster("cloudarrow");
            return one != null ? new[] { one } : null;
        }

        public static Sprite TrioFrame(int trio)
        {
            ShotFx form = trio == 0 ? ShotFx.FormFireWheel
                : trio == 1 ? ShotFx.FormCloudArrow
                : ShotFx.FormNeedle;
            string key = GlowShot.FormKey(form);
            return key != null ? GlowShot.Poster(key) : null;
        }

        public static Sprite[] TrioFrames(int trio)
        {
            ShotFx form = trio == 0 ? ShotFx.FormFireWheel
                : trio == 1 ? ShotFx.FormCloudArrow
                : ShotFx.FormNeedle;
            string key = GlowShot.FormKey(form);
            if (key == null) return null;
            int n = InkVfx.CountFrames(key + "_shot");
            if (n > 1)
            {
                Sprite[] frames = InkVfx.Frames(key + "_shot", n);
                if (frames != null && frames[0] != null) return frames;
            }
            Sprite one = GlowShot.Poster(key);
            return one != null ? new[] { one } : null;
        }
    }

    // 图鉴详情弹窗（Figma Panel/首页弹窗/06c_图鉴详情 定稿版）：
    // 黄顶奶油面板（无右上角关闭按钮，点空白处关闭）+ 说明暗条 + 左 128×172 字卡与右 448×172 弹形预览并排
    // + 单行简介 + 无外框大金星与浅黄文本框 + 底部加成说明。
    public sealed class CodexDetail : MonoBehaviour
    {
        CodexEntry _e;
        bool _known;
        Action _closed;
        RectTransform _board;
        CodexShot _shot;
        Image _glyph;
        readonly List<RectTransform> _rows = new List<RectTransform>();

        public static void Show(RectTransform layer, CodexEntry e, bool known, Action closed)
        {
            var dim = CodexLayout.Detail(layer);
            var d = dim.gameObject.AddComponent<CodexDetail>();
            d._e = e;
            d._known = known;
            d._closed = closed;
            d._board = (RectTransform)(dim.Find("stage/board") ?? dim.Find("board"));
            CodexLayout.SetTitle(d._board, CodexCatalog.Title(e));

            // 无右上角关闭按钮，点击面板外侧暗场空白处关闭（面板自身拦截点击防穿透）。
            var scrim = dim.GetComponent<Image>();
            if (scrim != null) scrim.raycastTarget = true;
            var dimBtn = dim.GetComponent<Button>() ?? dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.targetGraphic = scrim;
            dimBtn.onClick.RemoveAllListeners();
            dimBtn.onClick.AddListener(d.Close);
            var boardImg = d._board.GetComponent<Image>();
            if (boardImg != null) boardImg.raycastTarget = true;

            d.Build();
            PanelKit.Enlarge(d._board);
            PanelKit.Open(d._board);
        }

        void Close()
        {
            AudioBus.Tap();
            _closed?.Invoke();
            Destroy(gameObject);
        }

        Text Lab(string path) => _board.Find(path)?.GetComponent<Text>();

        void Build()
        {
            bool enemy = _e.Kind == CodexKind.Enemy;
            bool pair = _e.Kind == CodexKind.Pair || _e.Kind == CodexKind.Trio;

            _board.Find("shotBox")?.gameObject.SetActive(!enemy);
            _board.Find("enemy")?.gameObject.SetActive(enemy);
            _board.Find("pair")?.gameObject.SetActive(pair);
            _board.Find("stars")?.gameObject.SetActive(!enemy && !pair);

            BuildFace();
            if (enemy)
            {
                Text ent = Lab("info_strip/note") ?? Lab("info_strip/t") ?? Lab("note");
                if (ent != null) ent.text = _known ? "墨谱记录 · 战场首领与妖物" : "？？？";
                BuildEnemy();
                return;
            }
            BuildShotBox();

            string note;
            string lore;
            if (_e.Kind == CodexKind.Glyph)
            {
                var id = (CardId)_e.Index;
                note = CodexCatalog.WakeNote(id);
                lore = CodexCatalog.Lore(id);
            }
            else if (_e.Kind == CodexKind.Word)
            {
                var w = (WordId)_e.Index;
                note = CodexCatalog.WakeNote(w);
                lore = CodexCatalog.WordLore(w);
            }
            else if (_e.Kind == CodexKind.Trio)
            {
                SignatureTrio t = SignatureTrios.All[_e.Index];
                note = $"同列配方：【{CardCatalog.Get(t.A).Name}】+【{CardCatalog.Get(t.B).Name}】+【{CardCatalog.Get(t.C).Name}】";
                lore = "三个字放进同一列，一起打出去。字本身照常生效，另外多出这一诀。";
            }
            else
            {
                SignaturePair p = SignaturePairs.All[_e.Index];
                note = $"合击配方：【{CardCatalog.Get(p.A).Name}】 + 【{CardCatalog.Get(p.B).Name}】";
                lore = $"「{CardCatalog.Get(p.A).Name}」和「{CardCatalog.Get(p.B).Name}」叠在同一发炮弹上，炮弹化成这副模样，还多出一点本事。";
            }
            if (!_known)
            {
                note = "？？？";
                lore = "在关卡里实战触发一次，即可完整记录进图鉴。";
            }
            Text nt = Lab("info_strip/note") ?? Lab("info_strip/t") ?? Lab("note");
            if (nt != null) nt.text = string.IsNullOrEmpty(note) ? "常驻被动生效" : note;
            Text para = Lab("lore");
            if (para != null)
            {
                para.text = lore;
                para.color = _known ? ModalKit.TextBody : ModalKit.TextMuted;
            }

            if (pair) BuildPairEffect();
            else BuildStars();
        }

        void BuildEnemy()
        {
            var id = (EnemyId)_e.Index;
            EnemyDef d = EnemyCatalog.Base(id);
            Transform box = _board.Find("enemy/stat");
            box.Find("q")?.gameObject.SetActive(!_known);
            string[] rows =
            {
                "生命  " + Mathf.RoundToInt(d.Hp),
                "移速  " + d.Speed.ToString("0.00"),
                "赏金  " + d.Gold,
                "掉落  " + (d.Ink < 0.05f ? "少许墨" : d.Ink.ToString(d.Ink >= 1f ? "0.#" : "0.00") + "墨")
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var t = box.Find("s" + i)?.GetComponent<Text>();
                if (t == null) continue;
                t.gameObject.SetActive(_known);
                t.text = rows[i];
            }

            var para = Lab("lore");
            if (para != null)
            {
                para.text = _known ? EnemyCatalog.Lore(id) : "在关卡中击败它一次，即可记录进墨谱。";
                para.color = _known ? ModalKit.TextBody : ModalKit.TextMuted;
            }

            var tv = Lab("enemy/trait/v");
            if (tv != null)
            {
                tv.text = _known ? EnemyCatalog.TraitLine(d) : "？？？";
                tv.color = _known ? ModalKit.Ink : ModalKit.TextMuted;
            }
        }

        void BuildFace()
        {
            var card = _board.Find("face");
            var faceImg = card.GetComponent<Image>();
            faceImg.sprite = InkSprites.Load(CodexPanel.CardFaceOf(_e, _known ? CodexState.Known : CodexState.Seen));

            var name = card.Find("n").GetComponent<Text>();
            name.text = CodexCatalog.Title(_e);
            name.color = _known ? Color.white : InkTheme.Hex("D8D0C6");

            Color tint = _known ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            if (_e.Kind == CodexKind.Enemy)
            {
                CodexPanel.Put(card, "person", InkSprites.Person((EnemyId)_e.Index), 88f,
                    _known ? Color.white : new Color(0.3f, 0.26f, 0.22f, 0.6f));
                return;
            }
            if (_e.Kind == CodexKind.Glyph)
            {
                CodexPanel.Put(card, "glyph", InkArt.Bare(InkArt.Glyph((CardId)_e.Index, 256)), 88f, tint);
                _glyph = card.Find("glyph").GetComponent<Image>();
                return;
            }

            CardId[] parts;
            if (_e.Kind == CodexKind.Word) parts = CodexCatalog.PartsOf((WordId)_e.Index);
            else if (_e.Kind == CodexKind.Trio)
            {
                SignatureTrio t = SignatureTrios.All[_e.Index];
                parts = new[] { t.A, t.B, t.C };
            }
            else
            {
                SignaturePair p = SignaturePairs.All[_e.Index];
                parts = new[] { p.A, p.B };
            }
            bool trio = parts.Length > 2;
            CodexPanel.Put(card, "ga", InkArt.Bare(InkArt.Glyph(parts[0], 256)), trio ? 40f : 48f, tint);
            CodexPanel.Put(card, "gb", InkArt.Bare(InkArt.Glyph(parts[1], 256)), trio ? 36f : 48f, tint);
            if (trio)
            {
                card.Find("ga").GetComponent<RectTransform>().anchoredPosition = new Vector2(64f, -40f);
                card.Find("gb").GetComponent<RectTransform>().anchoredPosition = new Vector2(40f, -82f);
                Transform gc = card.Find("gc");
                if (gc == null)
                {
                    gc = Instantiate(card.Find("gb"), card);
                    gc.name = "gc";
                }
                CodexPanel.Put(card, "gc", InkArt.Bare(InkArt.Glyph(parts[2], 256)), 36f, tint);
                gc.GetComponent<RectTransform>().anchoredPosition = new Vector2(88f, -82f);
            }
            card.Find("plus")?.gameObject.SetActive(_e.Kind == CodexKind.Pair);
        }

        void BuildShotBox()
        {
            var box = (RectTransform)_board.Find("shotBox");
            box.Find("q")?.gameObject.SetActive(!_known);
            if (!_known) return;
            _shot = CodexShot.Build(box, new Vector2(360f, 128f));
            _shot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -8f);
            if (_e.Kind == CodexKind.Glyph) _shot.ShowGlyph((CardId)_e.Index, 1);
            else if (_e.Kind == CodexKind.Word) _shot.ShowWord((WordId)_e.Index);
            else if (_e.Kind == CodexKind.Trio) _shot.ShowTrio(_e.Index);
            else _shot.ShowPair(_e.Index);
            box.Find("plain")?.gameObject.SetActive(_e.Kind == CodexKind.Glyph && !ChangesLook((CardId)_e.Index));
        }

        static bool ChangesLook(CardId id)
        {
            if (id == CardId.Split) return true;
            GlyphDef g = GlyphTable.Get(id);
            return g.Form != ShotFx.None || g.Trail != ShotFx.None || g.Halo != ShotFx.None
                   || g.Orbit != ShotFx.None || g.Bloom != ShotFx.None;
        }

        void BuildStars()
        {
            bool pickable = _known && _e.Kind == CodexKind.Glyph;
            Transform g = _board.Find("stars");
            for (int s = 1; s <= GameConstants.MaxStar; s++)
            {
                int star = s;
                var row = g.Find("s" + s) as RectTransform;
                if (row == null) continue;
                _rows.Add(row);
                var t = (row.Find("box/v") ?? row.Find("v"))?.GetComponent<Text>();
                if (t != null)
                {
                    t.text = !_known ? "？？？"
                        : _e.Kind == CodexKind.Glyph ? CodexCatalog.StarLine((CardId)_e.Index, s)
                        : CodexCatalog.WordStarLine((WordId)_e.Index, s);
                    t.color = _known ? ModalKit.TextBody : ModalKit.TextMuted;
                }
                var img = row.GetComponent<Image>();
                if (img != null) img.raycastTarget = pickable;
                if (pickable)
                {
                    var btn = row.GetComponent<Button>();
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => PickStar(star));
                }
            }
            if (pickable) Highlight(1);
        }

        void PickStar(int star)
        {
            AudioBus.Tap();
            var id = (CardId)_e.Index;
            if (_shot != null) _shot.ShowGlyph(id, star);
            if (_glyph != null)
            {
                Sprite s = InkSprites.Heap(id, star);
                if (s != null) _glyph.sprite = s;
            }
            Highlight(star);
        }

        void Highlight(int star)
        {
            Sprite honey = ModalKit.Load2x("Ui/modal_sub_honey", ModalKit.BorderSub18);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i + 1 == star;
                var boxImg = _rows[i].Find("box")?.GetComponent<Image>();
                if (boxImg != null)
                {
                    if (honey != null) boxImg.sprite = honey;
                    boxImg.color = active ? Color.white : new Color(1f, 0.985f, 0.95f, 0.88f);
                }
            }
        }

        void BuildPairEffect()
        {
            var v = Lab("pair/effect/v");
            if (v != null)
            {
                v.text = _e.Kind == CodexKind.Trio
                    ? SignatureTrios.All[_e.Index].Note
                    : SignaturePairs.All[_e.Index].Note;
            }
        }
    }

    public sealed class CodexReel : MonoBehaviour
    {
        public Sprite[] Frames;
        public Color Tint = Color.white;
        public float BaseAngle;
        public float SpinSpeed;
        Image _img;

        void OnEnable()
        {
            _img = GetComponent<Image>();
        }

        void Update()
        {
            if (!enabled || _img == null) return;
            if (Frames != null && Frames.Length >= 2)
            {
                int i = Mathf.FloorToInt(Time.unscaledTime * 8f) % Frames.Length;
                Sprite s = Frames[i];
                if (s != null) _img.sprite = s;
            }
            if (Mathf.Abs(SpinSpeed) > 0.01f)
                _img.rectTransform.localEulerAngles = new Vector3(0f, 0f, BaseAngle + Time.unscaledTime * SpinSpeed);
            _img.color = Tint;
        }
    }
}
