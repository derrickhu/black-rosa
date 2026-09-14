using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 首页：常驻顶栏 + 三段底栏（炮台 / 出征 / 技能）。
    // 切页只 SetActive 子容器，买完东西只重建那一页，不动顶栏和底栏。
    public sealed class HomeScreen
    {
        const int TabForge = 0;
        const int TabSortie = 1;
        const int TabSpell = 2;

        // 三页共用的卡片尺寸。内容区高度 = 1280 - 顶栏 140 - 底栏 134 ≈ 1006。
        // 卡高和卡间距写死，富余的竖向空间由 Slot 上下均分 —— 各页顶部的常驻块
        // 高度不一样（炮台页有预览卡+皮肤条，技能页只有装备槽+一行提示），
        // 富余全堆在底下就会空出一大片，看着像没做完。
        const float CardW = 322f;
        const float CardH = 204f;
        const float ColStep = 338f;
        const float CardGap = 34f;

        static readonly Color[] LineTint =
        {
            InkTheme.Rose, InkTheme.Violet, InkTheme.Teal, InkTheme.CoinFace, InkTheme.Cta
        };

        readonly RectTransform _layer;
        readonly MetaProgress _meta;
        readonly Action<int> _start;
        readonly RectTransform[] _pages = new RectTransform[3];
        Button[] _tabs;
        Text _ink;
        Text _stamina;
        Text _stars;
        Text _stamTip;
        int _tab = TabSortie;

        HomeScreen(RectTransform layer, MetaProgress meta, Action<int> start)
        {
            _layer = layer;
            _meta = meta;
            _start = start;
        }

        public static HomeScreen Build(RectTransform layer, MetaProgress meta, Action<int> start)
        {
            var h = new HomeScreen(layer, meta, start);
            h.BuildShell();
            return h;
        }

        void BuildShell()
        {
            UiKit.PaperSheet(_layer);
            // 体力不能也用红心 —— 战斗里的红心是基地生命，两个混在一起看不懂。
            // 所以体力用青色水滴闪电，基地生命才是红心。
            float top = ScreenFit.TopPad + 18f;
            var size = new Vector2(190f, 58f);
            _stamina = UiKit.Chip(_layer, "cs", InkSprites.Ui("stamina"), "",
                new Vector2(-190f, top), size);
            _ink = UiKit.Chip(_layer, "ci", InkSprites.Ui("ink"), "",
                new Vector2(0f, top), size);
            _stars = UiKit.Chip(_layer, "cx", InkSprites.Ui("star"), "",
                new Vector2(190f, top), size);
            _stamTip = UiKit.Label(_layer, "stamtip", "", 19, new Vector2(-190f, top + 62f),
                new Vector2(200, 26), TextAnchor.MiddleCenter, Pin.Top);
            _stamTip.color = InkTheme.TextMid;

            for (int i = 0; i < _pages.Length; i++) _pages[i] = Page("page" + i);
            _tabs = UiKit.TabBar(_layer, new[] { "炮台", "出征", "技能" },
                new[] { InkSprites.Ui("tab_forge"), InkSprites.Ui("tab_sortie"), InkSprites.Ui("tab_spell") },
                Pick);
            Pick(TabSortie);
            RefreshTop();
        }

        // 手绘图标直接压在卡面上，不垫彩色方块 —— 图标自己有颜色和描边，
        // 底下再垫一块饱和色就是撞色。买不起的整张卡压灰，图标跟着淡下去，
        // 不能用 Image.color 去压成灰：相乘只会脏掉，降透明度才是干净的禁用态。
        static void CardIcon(Transform parent, Sprite icon, Vector2 pos, float size, bool live)
        {
            if (icon == null) return;
            UiKit.Icon(parent, icon, pos, size).color =
                live ? Color.white : new Color(1f, 1f, 1f, 0.50f);
        }

        RectTransform Page(string name)
        {
            var rt = UiKit.Panel(_layer, name, Vector2.zero, Vector2.zero, Color.clear);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, ScreenFit.BottomPad + UiKit.TabBarH);
            rt.offsetMax = new Vector2(0f, -(ScreenFit.TopPad + 104f));
            rt.GetComponent<Image>().raycastTarget = false;
            rt.gameObject.SetActive(false);
            return rt;
        }

        void Pick(int tab)
        {
            _tab = tab;
            for (int i = 0; i < _pages.Length; i++) _pages[i].gameObject.SetActive(i == tab);
            UiKit.PaintTab(_tabs, tab);
            Rebuild(tab);
        }

        void Rebuild(int tab)
        {
            RectTransform page = _pages[tab];
            for (int i = page.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(page.GetChild(i).gameObject);
            if (tab == TabForge) BuildForge(page);
            else if (tab == TabSortie) BuildSortie(page);
            else BuildSpells(page);
            RefreshTop();
        }

        public void Tick()
        {
            int before = _meta.Stamina;
            _meta.Refresh();
            RefreshTop();
            // 体力自然恢复到能出征了，出征页的按钮状态得跟上。
            if (_meta.Stamina != before && _tab == TabSortie) Rebuild(TabSortie);
        }

        int _shownSec = -1;

        void RefreshTop()
        {
            if (_stamina == null) return;
            _stamina.text = _meta.Stamina + "/" + GameConstants.StaminaMax;
            _ink.text = _meta.Ink.ToString();
            _stars.text = _meta.TotalStars().ToString();
            // 倒计时每帧都拼字符串没必要，秒数没变就不动。
            int sec = _meta.SecondsToNextStamina();
            if (sec == _shownSec) return;
            _shownSec = sec;
            _stamTip.text = sec <= 0 ? "体力已满" : $"{sec / 60:00}:{sec % 60:00} 后 +1";
        }

        void WatchStaminaAd()
        {
            AdStub.Reward("stamina", () =>
            {
                _meta.GrantAdStamina();
                Rebuild(_tab);
            });
        }

        // 卡面配色：能买/能打的是白底深描边，不能的整张压灰，
        // 只靠描边色区分会看不出来。
        static void Dim(RectTransform box, bool live)
        {
            if (live) return;
            box.GetComponent<Image>().color = InkTheme.CardDim;
            Transform ln = box.Find("ln");
            if (ln != null) ln.GetComponent<Image>().color = InkTheme.LineDim;
        }

        // 彩色圆角图标底。卡片左边那一块，白图标压在饱和色上。
        void IconPlate(Transform parent, Vector2 pos, float size, Color bg, Sprite icon, bool live)
        {
            var plate = UiKit.Stroke(parent, "ip", pos, new Vector2(size, size), Pin.Center, 4f,
                live ? InkTheme.Outline : InkTheme.LineDim, live ? bg : InkTheme.LineDim, size * 0.30f);
            plate.GetComponent<Image>().raycastTarget = false;
            if (icon != null) UiKit.Icon(plate, icon, Vector2.zero, size * 0.54f);
        }

        // 金币小药丸，放价格。不是按钮，整张卡才是按钮。
        void PricePill(Transform parent, Vector2 pos, string text, bool live)
        {
            var pill = UiKit.Stroke(parent, "pp", pos, new Vector2(118f, 42f), Pin.Center, 4f,
                live ? InkTheme.Outline : InkTheme.LineDim,
                live ? InkTheme.CoinFace : InkTheme.CardDim, 16f);
            pill.GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Label(pill, "t", text, 20, Vector2.zero, new Vector2(112f, 36f));
            t.color = live ? InkTheme.TextDark : InkTheme.TextDim;
            UiKit.Bold(t);
        }

        // 等级点：满的上色，空的压灰。
        void Pips(Transform parent, int lv, int max, Vector2 origin, Color col)
        {
            for (int i = 0; i < max; i++)
                UiKit.Icon(parent, InkArt.Heap(InkShape.Circle, i < lv ? col : InkTheme.CardDim, 32),
                    origin + new Vector2(i * 28f, 0f), 20f);
        }

        // 内容区可用高度，和 Page() 的上下留白保持一致。
        // 用 CanvasH 不是 DesignH —— Page 是靠 anchor 拉满的，真实高度跟着画布走，
        // 按 1280 算的话长条屏上底部会白留一大片（真机截图里炮台页下面那块空白就是这个）。
        static float PageH =>
            ScreenFit.CanvasH - (ScreenFit.TopPad + 104f) - (ScreenFit.BottomPad + UiKit.TabBarH);

        // topY 是卡片区最早能开始的位置（顶部常驻块的下沿）。
        // 排完还富余就把富余的一半推到上面，让上下留白对称；不够就先压行距。
        static Vector2 Slot(int i, float topY, int count)
        {
            int rows = Mathf.Max(1, (count + 1) / 2);
            float avail = PageH - topY - 20f;
            // 卡不缩，只压行距。卡里的图标、两行字、等级点、价格药丸全是按
            // CardH = 204 写死的偏移，缩卡等于把里面全挤乱。
            // 为什么要压：三行 204 的卡加 34 行距一共要 680，原来正好卡在
            // 720x1280 + TopPad 36 那个内容区（1006）上，一点余量都没有。
            // 真机上顶栏要给微信胶囊让位，内容区少一截，16:9 和平板上就顶到底栏下面了。
            float gap = Mathf.Clamp((avail - rows * CardH) / Mathf.Max(1, rows - 1), 0f, CardGap);
            float block = rows * CardH + (rows - 1) * gap;
            float slack = Mathf.Max(0f, avail - block);
            return UiKit.GridPos(i, 2, ColStep, CardH + gap)
                   + new Vector2(0f, topY + slack * 0.5f);
        }

        // ---------- 出征 ----------

        // 第一个没通关的已解锁关。顶部那个大按钮直接送过去，省得每次自己找。
        int NextStage()
        {
            int last = 0;
            for (int i = 0; i < GameConstants.ChapterStageCount; i++)
            {
                if (!_meta.Unlocked(i)) break;
                last = i;
                if (_meta.Stars[i] <= 0) return i;
            }
            return last;
        }

        void BuildSortie(RectTransform page)
        {
            // 字标自己带奶油岛和描边，再套一层 Stroke 白框会裁掉牌和墨滴。
            Sprite mark = InkSprites.Load("Ui/logo");
            float titleH;
            if (mark != null)
            {
                const float w = 340f;
                float h = w * mark.rect.height / Mathf.Max(1f, mark.rect.width);
                var slot = UiKit.Panel(page, "title", new Vector2(0f, 6f), new Vector2(w, h),
                    Color.clear, Pin.Top);
                slot.GetComponent<Image>().raycastTarget = false;
                var img = UiKit.Icon(slot, mark, Vector2.zero, w);
                img.rectTransform.sizeDelta = new Vector2(w, h);
                titleH = 6f + h;
            }
            else
            {
                var title = UiKit.Label(page, "t", "墨字防线", 44, new Vector2(0f, 16f),
                    new Vector2(480, 70), TextAnchor.MiddleCenter, Pin.Top);
                UiKit.Bold(title);
                titleH = 90f;
            }

            // 印章是这页的主体。二十关排 5 列 4 行 —— 原来 8 关用的是 4 列、
            // 164px 的大印章，那个尺寸排到 5 行会超出内容区。
            // 行距和印章尺寸都按内容区实际剩下多少算，不写死：真机顶栏要给微信胶囊
            // 让出一条，16:9 的机子上四行印章会压到底下的主按钮上。
            float sealTop = titleH + 16f;
            float room = PageH - sealTop - SortieFootH;
            float step = Mathf.Min(SealStepY, room / SealRows);
            float size = Mathf.Clamp(step - 22f, 96f, SealSize);
            for (int i = 0; i < GameConstants.ChapterStageCount; i++)
                Seal(page, i, size, UiKit.GridPos(i, SealCols, SealStepX, step)
                              + new Vector2(0f, sealTop));

            // 主按钮和说明贴着内容区下沿往上摞，不要按固定 y 悬在中间 ——
            // 悬在中间时底下会空出 270px，而出征页是打开游戏第一眼看到的页，
            // 那片空白就是「没做完」的观感来源。顺带按钮也落到更好按的位置。
            int next = NextStage();
            bool canGo = _meta.CanEnter(next);
            var go = UiKit.Btn(page, "go", (_meta.Stars[next] > 0 ? "重打" : "继续") + $"  第 {next + 1} 关",
                new Vector2(0f, 26f), new Vector2(460f, 106f), () => _start(next), true, Pin.Bottom);
            go.interactable = canGo;

            bool poor = _meta.Stamina < GameConstants.StaminaPerStage;
            float y = 150f;
            if (poor && _meta.CanAdStamina)
            {
                UiKit.Btn(page, "adstam", $"看广告  +{GameConstants.AdStaminaGain} 体力",
                    new Vector2(0f, y), new Vector2(400f, 84f), WatchStaminaAd, false, Pin.Bottom);
                y += 100f;
            }
            var help = UiKit.Label(page, "help",
                poor
                    ? "体力不够重刷了。没打过的关永远免费。"
                    : "左右拖动底栏炮串。金币够了点改装。\n重刷已通过的关要 2 点体力。",
                22, new Vector2(0f, y), new Vector2(620, 80), TextAnchor.LowerCenter, Pin.Bottom);
            help.color = InkTheme.TextMid;
        }

        // 出征页印章的排布。二十关排 5 列 4 行，尺寸和行距是上限，实际会按内容区收缩。
        const int SealCols = 5;
        const int SealRows = (GameConstants.ChapterStageCount + SealCols - 1) / SealCols;
        const float SealSize = 128f;
        const float SealStepX = 138f;
        const float SealStepY = 150f;
        const float SortieFootH = 236f; // 页脚那摞（主按钮 + 说明）从内容区下沿往上吃掉的高度

        void Seal(RectTransform page, int index, float size, Vector2 pos)
        {
            // 印章里的元素都按 128 那一档定的位，缩了要跟着等比缩，不然字会压到星上。
            float k = size / SealSize;
            bool open = _meta.Unlocked(index);
            int cost = _meta.StageCost(index);
            bool afford = _meta.Stamina >= cost;
            bool live = open && afford;
            var plate = UiKit.Stroke(page, "st" + index, pos, new Vector2(size, size), Pin.Top,
                live ? 5f : 4f,
                live ? InkTheme.Outline : InkTheme.LineDim, live ? InkTheme.CardFace : InkTheme.CardDim, 22f * k);
            var btn = plate.gameObject.AddComponent<Button>();
            btn.targetGraphic = plate.GetComponent<Image>();
            btn.interactable = live;
            int idx = index;
            btn.onClick.AddListener(() => { if (live) _start(idx); });
            if (!open)
            {
                UiKit.Icon(plate, InkSprites.Ui("lock"), Vector2.zero, 48f * k);
                return;
            }
            int stars = _meta.Stars[index];
            var num = UiKit.Label(plate, "n", (index + 1).ToString(), Mathf.RoundToInt(34f * k),
                new Vector2(0f, (stars > 0 ? 12f : 2f) * k), new Vector2(80f * k, 44f * k));
            UiKit.Bold(num);
            num.color = live ? InkTheme.TextDark : InkTheme.TextDim;
            if (stars <= 0)
            {
                var free = UiKit.Label(plate, "free", "免费", Mathf.RoundToInt(17f * k),
                    new Vector2(0f, -34f * k), new Vector2(90f * k, 24f * k));
                free.color = InkTheme.Teal;
                UiKit.Bold(free);
                return;
            }
            // 手绘星有描边，间距得比纯色圆星大一点才不糊在一起。
            float span = (stars - 1) * 26f * k;
            for (int s = 0; s < stars; s++)
                UiKit.Icon(plate, InkSprites.Ui("star"),
                    new Vector2(-span * 0.5f + s * 26f * k, -22f * k), 24f * k);
            // 体力价只在买不起的时候写出来 —— 那才是要玩家做决定的时刻。
            // 印章上再固定挂一行小字会糊成一团，平时的规则交给页脚那句说明。
            if (!afford)
            {
                var tag = UiKit.Label(plate, "cost", "体力 " + cost, Mathf.RoundToInt(16f * k),
                    new Vector2(0f, -46f * k), new Vector2(100f * k, 22f * k));
                tag.color = InkTheme.Rose;
            }
        }

        // ---------- 炮台 ----------

        void BuildForge(RectTransform page)
        {
            SkinDef skin = SkinCatalog.Get(_meta.Skin);
            var card = UiKit.Stroke(page, "stand", new Vector2(0f, 2f), new Vector2(316, 176), Pin.Top, 6f);
            card.GetComponent<Image>().raycastTarget = false;
            // 炮台是黑色墨稿，底下垫一张暖盘才不像一块黑疙瘩
            var disc = UiKit.Icon(card, InkArt.Heap(InkShape.Circle,
                skin.Tint.a > 0.01f ? skin.Tint : InkTheme.Hex("FFE7C4"), 96), new Vector2(0f, 22f), 132f);
            disc.color = new Color(1f, 1f, 1f, skin.Tint.a > 0.01f ? 0.75f : 1f);
            UiKit.Icon(card, InkArt.Cannon(), new Vector2(0f, 22f), 104f);
            var sn = UiKit.Label(card, "sn", skin.Name, 26, new Vector2(0f, -62f), new Vector2(280, 34));
            UiKit.Bold(sn);

            for (int i = 0; i < SkinCatalog.Count; i++) SkinChip(page, i);

            for (int i = 0; i < ForgeCatalog.LineCount; i++)
                ForgeCard(page, i, Slot(i, 306f, ForgeCatalog.LineCount));
        }

        void SkinChip(RectTransform page, int i)
        {
            SkinDef d = SkinCatalog.Get(i);
            bool owned = _meta.SkinOwned[i];
            bool on = _meta.Skin == i;
            bool buyable = _meta.CanBuySkin(i, out string why);
            var box = UiKit.Stroke(page, "sk" + i, new Vector2((i - 1.5f) * 168f, 186f), new Vector2(156, 108),
                Pin.Top, on ? 6f : 4f,
                on ? InkTheme.Cta : (owned ? InkTheme.Outline : InkTheme.LineDim),
                owned ? InkTheme.CardFace : InkTheme.CardDim, 20f);
            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box.GetComponent<Image>();
            btn.interactable = owned || buyable;
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                if (_meta.SkinOwned[idx]) _meta.EquipSkin(idx);
                else _meta.BuySkin(idx);
                Rebuild(TabForge);
            });
            if (d.Tint.a > 0.01f)
                IconPlate(box, new Vector2(0f, 26f), 48f, d.Tint, null, owned);
            else
            {
                var none = UiKit.Label(box, "d", "无", 24, new Vector2(0f, 26f), new Vector2(80, 30));
                none.color = owned ? InkTheme.TextDark : InkTheme.TextDim;
            }
            var n = UiKit.Label(box, "n", d.Name, 21, new Vector2(0f, -12f), new Vector2(150, 28));
            UiKit.Bold(n);
            n.color = owned ? InkTheme.TextDark : InkTheme.TextDim;
            // 两行字之前只隔 11px，压在一起了。名字和状态各占一行，间距拉到 22。
            string tail = owned ? (on ? "使用中" : "点击换上") : (buyable ? d.Price + " 墨" : why);
            var t = UiKit.Label(box, "s", tail, 16, new Vector2(0f, -36f), new Vector2(150, 24));
            t.color = on ? InkTheme.Cta : InkTheme.TextMid;
        }

        void ForgeCard(RectTransform page, int line, Vector2 pos)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            int lv = _meta.ForgeLevel(line);
            bool max = lv >= d.MaxLevel;
            bool buyable = _meta.CanBuyForge(line, out string why);
            Color tint = LineTint[Mathf.Clamp(line, 0, LineTint.Length - 1)];
            var box = UiKit.Stroke(page, "fg" + line, pos, new Vector2(CardW, CardH), Pin.Top, 5f);
            Dim(box, buyable);
            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box.GetComponent<Image>();
            btn.interactable = buyable;
            int idx = line;
            btn.onClick.AddListener(() =>
            {
                if (_meta.BuyForge(idx)) Rebuild(TabForge);
            });
            CardIcon(box, InkSprites.Ui(d.Icon), new Vector2(-114f, 34f), 84f, buyable);
            var n = UiKit.Label(box, "n", d.Name, 28, new Vector2(25f, 46f), new Vector2(180, 36), TextAnchor.MiddleLeft);
            UiKit.Bold(n);
            n.color = buyable ? InkTheme.TextDark : InkTheme.TextDim;
            var step = UiKit.Label(box, "s", d.Step, 18, new Vector2(25f, 10f), new Vector2(180, 26), TextAnchor.MiddleLeft);
            step.color = InkTheme.TextMid;
            Pips(box, lv, d.MaxLevel, new Vector2(-126f, -58f), buyable ? tint : InkTheme.LineDim);
            string tail = max ? "已满级" : (buyable ? ForgeCatalog.Cost(line, lv) + " 墨" : why);
            if (max || !buyable)
            {
                var t = UiKit.Label(box, "p", tail, 19, new Vector2(56f, -58f), new Vector2(170, 28),
                    TextAnchor.MiddleRight);
                t.color = InkTheme.TextDim;
            }
            else PricePill(box, new Vector2(84f, -58f), tail, true);
        }

        // ---------- 技能 ----------

        void BuildSpells(RectTransform page)
        {
            for (int s = 0; s < GameConstants.SpellSlots; s++)
            {
                int id = _meta.Equipped[s];
                bool has = id >= 0;
                var box = UiKit.Stroke(page, "slot" + s, new Vector2((s - 0.5f) * 236f, 4f),
                    new Vector2(232, 124), Pin.Top, 6f,
                    has ? InkTheme.Outline : InkTheme.LineDim,
                    has ? InkTheme.CardFace : InkTheme.CardDim, 24f);
                box.GetComponent<Image>().raycastTarget = false;
                if (!has)
                {
                    var e = UiKit.Label(box, "n", "空槽", 26, Vector2.zero, new Vector2(200, 40));
                    e.color = InkTheme.TextDim;
                    continue;
                }
                SpellDef d = SpellCatalog.Get(id);
                // 槽高 124，半高 62。图标 52 摆在 y 32 是 6..58，名字框顶沿在 0，
                // 差 6px 不打架；原来 58 摆在 28 会盖住名字 3px。
                CardIcon(box, InkSprites.Ui(d.Id), new Vector2(0f, 32f), 52f, true);
                var n = UiKit.Label(box, "n", d.Name, 28, new Vector2(0f, -18f), new Vector2(210, 36));
                UiKit.Bold(n);
                var c = UiKit.Label(box, "c", "耗 " + d.InkCost + " 墨", 19, new Vector2(0f, -44f), new Vector2(210, 26));
                c.color = InkTheme.TextMid;
            }
            var tip = UiKit.Label(page, "tip", "战斗中点右下角的键释放。局内打怪攒墨，两个键共用。", 20,
                new Vector2(0f, 142f), new Vector2(640, 30), TextAnchor.MiddleCenter, Pin.Top);
            tip.color = InkTheme.TextMid;

            for (int i = 0; i < SpellCatalog.Count; i++)
                SpellCard(page, i, Slot(i, 192f, SpellCatalog.Count));
        }

        void SpellCard(RectTransform page, int i, Vector2 pos)
        {
            SpellDef d = SpellCatalog.Get(i);
            bool owned = _meta.SpellOwned[i];
            int slot = _meta.EquippedSlot(i);
            bool buyable = _meta.CanBuySpell(i, out string why);
            bool live = owned || buyable;
            var box = UiKit.Stroke(page, "sp" + i, pos, new Vector2(CardW, CardH), Pin.Top,
                slot >= 0 ? 6f : 5f, slot >= 0 ? InkTheme.Cta : (live ? InkTheme.Outline : InkTheme.LineDim));
            Dim(box, live);
            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box.GetComponent<Image>();
            btn.interactable = live;
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                if (_meta.SpellOwned[idx]) _meta.Equip(idx);
                else _meta.BuySpell(idx);
                Rebuild(TabSpell);
            });
            CardIcon(box, InkSprites.Ui(d.Id), new Vector2(-114f, 34f), 84f, live);
            var n = UiKit.Label(box, "n", d.Name, 28, new Vector2(20f, 46f), new Vector2(170, 36), TextAnchor.MiddleLeft);
            UiKit.Bold(n);
            n.color = live ? InkTheme.TextDark : InkTheme.TextDim;
            // 最长的一条是墨爆的「最前排炸开一圈，6 点伤害」：17 号字实宽 200，
            // 原来框只有 190，会折成两行顶出 26 高的框压到底下那行能量上。
            // 框左沿对齐上面的名字（-66），右边离卡沿还留 27。
            var desc = UiKit.Label(box, "d", d.Desc, 16, new Vector2(34f, 10f), new Vector2(200, 26), TextAnchor.MiddleLeft);
            desc.color = InkTheme.TextMid;
            var cost = UiKit.Label(box, "e", "耗 " + d.InkCost + " 墨", 18, new Vector2(-78f, -58f), new Vector2(140, 26),
                TextAnchor.MiddleLeft);
            cost.color = InkTheme.TextMid;
            if (owned)
            {
                // 右对齐的框右沿要离卡边留出手指宽的余量，之前只剩 7px，顶到描边上了。
                var t = UiKit.Label(box, "s", slot >= 0 ? "已装备 " + (slot + 1) : "点击装备", 20,
                    new Vector2(56f, -58f), new Vector2(170, 28), TextAnchor.MiddleRight);
                t.color = slot >= 0 ? InkTheme.Cta : InkTheme.TextMid;
                UiKit.Bold(t);
            }
            else if (buyable) PricePill(box, new Vector2(84f, -58f), d.Price + " 墨", true);
            else
            {
                var t = UiKit.Label(box, "s", why, 18, new Vector2(56f, -58f), new Vector2(170, 28),
                    TextAnchor.MiddleRight);
                t.color = InkTheme.TextDim;
            }
        }
    }
}
