using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    // 炮台页顶上的皮肤展台：一次只摆一门大炮，左右箭头或横划切换，底下木托架上一排头像。
    // 圆台下面那颗按钮就是装备 / 购买，没开放的炮在炮身上写开放条件。
    // 展台、头像框、箭头是生成图，坐标按展台原图里圆台和托条的位置量出来的。
    public sealed class SkinShowcase
    {
        public const float W = 640f;
        public const float H = 480f;

        // 以展台中心为原点。转台顶面中心约在 -44，炮的底边落在顶面中心略下。
        const float GunSize = 172f;
        const float GunFoot = -64f;
        const float GunPad = 18f / 480f;     // ico_skin 画布底边留白占比
        const float NameY = 186f;
        const float PerkY = 156f;
        const float ActY = -108f;
        const float ShelfY = -178f;
        const float AvatarStep = 72f;
        const float ArrowX = 262f;
        const float ArrowY = 24f;

        static readonly Color Shadow = new Color(0.16f, 0.12f, 0.10f, 0.88f);

        readonly MetaProgress _meta;
        readonly Action<int> _focusChanged;
        readonly Action _changed;
        readonly RectTransform _root;
        Image _gun;
        Image _lock;
        Text _lockText;
        Text _name;
        RectTransform _rankTag;
        Image _rankBg;
        Text _rank;
        Text _perk;
        Button _act;
        Text _actText;
        Image[] _frames;
        Image[] _faces;
        RectTransform[] _used;
        RectTransform[] _dots;
        SkinStageMotion _motion;
        int _focus;
        Text[] _fxName;
        Text[] _fxVal;

        public RectTransform Root => _root;
        public int Focus => _focus;

        SkinShowcase(MetaProgress meta, RectTransform root, Action<int> focusChanged, Action changed)
        {
            _meta = meta;
            _root = root;
            _focusChanged = focusChanged;
            _changed = changed;
        }

        // 烘预制时搭一份壳。圆角标签和圆章的图是运行时生成的，进游戏再补。
        public static void Create(RectTransform page, float top)
        {
            var root = MakeRoot(page, top);
            var s = new SkinShowcase(null, root, null, null);
            s.Build(false);
        }

        // 预制里已经有展台就接着用；没有才现搭。
        public static SkinShowcase Ensure(SkinShowcase have, RectTransform page, float top, MetaProgress meta,
            Action<int> focusChanged, Action changed)
        {
            if (have != null && have._root != null && have._root.parent == page) return have;
            var existing = page != null ? page.Find("showcase") as RectTransform : null;
            if (existing != null)
            {
                var baked = new SkinShowcase(meta, existing, focusChanged, changed);
                if (baked.Capture())
                {
                    baked.Repair();
                    baked.Rewire();
                    return baked;
                }
                UnityEngine.Object.Destroy(existing.gameObject);
            }
            var root = MakeRoot(page, top);
            var s = new SkinShowcase(meta, root, focusChanged, changed);
            s.Build(true);
            return s;
        }

        static RectTransform MakeRoot(RectTransform page, float top)
        {
            var root = UiKit.Art(page, "showcase", "Ui/skin_stage", new Vector2(0f, top), new Vector2(W, H), Pin.Top);
            root.GetComponent<Image>().raycastTarget = false;
            return root;
        }

        void Build(bool motion)
        {
            // 横划区垫在炮下面，箭头、按钮和头像在它上层，点它们不会被吞。
            // 滑动脚本进游戏再挂。这个脚本资源没有稳定的 guid，烘进预制会变成丢失脚本，整份预制就存不了。
            var swipe = UiKit.Panel(_root, "swipe", new Vector2(0f, 40f), new Vector2(460f, 320f),
                new Color(1f, 1f, 1f, 0f));
            if (motion) AttachMotion(swipe);

            _name = UiKit.Label(_root, "name", "", 34, new Vector2(0f, NameY), new Vector2(220f, 44f));
            _name.color = InkTheme.TextDark;
            UiKit.Bold(_name);
            _rankTag = UiKit.Panel(_root, "rank", new Vector2(0f, NameY), new Vector2(54f, 26f), InkTheme.Hex("3A8EE6"));
            _rankBg = _rankTag.GetComponent<Image>();
            _rankBg.sprite = UiSprites.Fill(8);
            _rankBg.type = Image.Type.Sliced;
            _rankBg.raycastTarget = false;
            _rank = UiKit.Label(_rankTag, "t", "", 16, Vector2.zero, new Vector2(54f, 26f));
            _rank.color = Color.white;
            UiKit.Bold(_rank);
            _perk = UiKit.Label(_root, "perk", "", 19, new Vector2(0f, PerkY), new Vector2(320f, 28f));
            _perk.color = InkTheme.Seal;
            UiKit.Bold(_perk);

            _gun = UiKit.Icon(_root, null, new Vector2(0f, GunCenter), GunSize);
            _gun.gameObject.name = "gun";
            if (_motion != null)
            {
                _motion.Gun = _gun.rectTransform;
                _motion.BaseY = GunCenter;
            }
            _lock = UiKit.Icon(_root, InkSprites.Ui("lock"), new Vector2(0f, GunCenter + 8f), 72f);
            _lock.gameObject.name = "lock";
            _lockText = UiKit.Label(_root, "need", "", 26, new Vector2(0f, GunCenter - 44f), new Vector2(460f, 36f));
            _lockText.color = InkTheme.CardFace;
            UiKit.Bold(_lockText);
            var lo = _lockText.gameObject.AddComponent<Outline>();
            lo.effectColor = new Color(0.12f, 0.08f, 0.06f, 0.95f);
            lo.effectDistance = new Vector2(2f, -2f);

            _act = UiKit.Btn(_root, "act", "", new Vector2(0f, ActY), new Vector2(200f, 58f), OnAct, true);
            _actText = _act.GetComponentInChildren<Text>();
            if (_actText != null) _actText.fontSize = 24;

            Arrow("prev", -1);
            Arrow("next", 1);

            int n = SkinCatalog.Count;
            _frames = new Image[n];
            _faces = new Image[n];
            _used = new RectTransform[n];
            _dots = new RectTransform[n];
            float span = (n - 1) * AvatarStep;
            for (int i = 0; i < n; i++)
            {
                var pos = new Vector2(-span * 0.5f + i * AvatarStep, ShelfY);
                var cell = UiKit.Art(_root, "av" + i, "Ui/skin_frame", pos, new Vector2(62f, 74f));
                _frames[i] = cell.GetComponent<Image>();
                _frames[i].preserveAspect = false;
                var btn = cell.gameObject.AddComponent<Button>();
                btn.targetGraphic = _frames[i];
                btn.transition = Selectable.Transition.None;
                int idx = i;
                btn.onClick.AddListener(() => Show(idx, 0, true));
                _faces[i] = UiKit.Icon(cell, null, new Vector2(0f, 5f), 54f);
                _faces[i].gameObject.name = "face";
                _used[i] = Seal(cell, "used", "用", new Vector2(22f, 28f), 26f, InkTheme.Seal);
                _dots[i] = Seal(cell, "dot", "", new Vector2(24f, 30f), 16f, InkTheme.Rose);
            }
            BuildFx();
        }

        // 炮右边列当前生效的词条，和进战斗的结算同一套：点过级、装着的专属才算。
        // 名字靠左，数值靠右一列，字号更大、各用各的颜色，扫一眼不用翻下面的列表。
        const int FxSlots = 7;
        const float FxTop = 138f;
        const float FxStep = 30f;
        const float FxNameX = 137f;
        const float FxValX = 214f;

        void BuildFx()
        {
            _fxName = new Text[FxSlots];
            _fxVal = new Text[FxSlots];
            for (int i = 0; i < FxSlots; i++)
            {
                float y = FxTop - i * FxStep;
                _fxName[i] = UiKit.Label(_root, "fxn" + i, "", 17,
                    new Vector2(FxNameX, y), new Vector2(74f, 30f), TextAnchor.MiddleLeft);
                _fxName[i].color = InkTheme.TextDark;
                UiKit.Bold(_fxName[i]);
                _fxVal[i] = UiKit.Label(_root, "fxv" + i, "", 22,
                    new Vector2(FxValX, y), new Vector2(84f, 32f), TextAnchor.MiddleLeft);
                UiKit.Bold(_fxVal[i]);
                var shade = _fxVal[i].gameObject.AddComponent<Shadow>();
                shade.effectColor = new Color(0.22f, 0.13f, 0.08f, 0.28f);
                shade.effectDistance = new Vector2(1f, -1f);
                _fxName[i].gameObject.SetActive(false);
                _fxVal[i].gameObject.SetActive(false);
            }
        }

        void PaintFx()
        {
            if (_fxName == null) return;
            int n = 0;
            int[] order = new int[ForgeCatalog.LineCount];
            int skin = _meta.Skin;
            for (int i = 0; i < ForgeCatalog.LineCount; i++)
            {
                ForgeDef d = ForgeCatalog.Get(i);
                if (_meta.ForgeLevel(i) <= 0) continue;
                if (d.Exclusive && d.Skin != skin) continue;
                order[n++] = i;
            }
            for (int a = 1; a < n; a++)
            {
                int v = order[a];
                int b = a;
                while (b > 0 && ForgeCatalog.Rank(order[b - 1]) > ForgeCatalog.Rank(v))
                {
                    order[b] = order[b - 1];
                    b--;
                }
                order[b] = v;
            }
            int shown = Mathf.Min(n, FxSlots);
            for (int s = 0; s < FxSlots; s++)
            {
                bool on = s < shown;
                _fxName[s].gameObject.SetActive(on);
                _fxVal[s].gameObject.SetActive(on);
                if (!on) continue;
                ForgeDef d = ForgeCatalog.Get(order[s]);
                _fxName[s].text = d.Stat;
                _fxVal[s].text = ForgeCatalog.Value(order[s], _meta.ForgeLevel(order[s]));
                _fxVal[s].color = FxColor(d.Line);
            }
        }

        static Color FxColor(ForgeLine line)
        {
            switch (line)
            {
                case ForgeLine.Damage: return InkTheme.Hex("E25B2A");
                case ForgeLine.FireRate: return InkTheme.Hex("1E8E4E");
                case ForgeLine.StartGold: return InkTheme.Hex("C98416");
                case ForgeLine.BaseHp: return InkTheme.Hex("D23B4A");
                case ForgeLine.Emitters: return InkTheme.Hex("1C8C8C");
                case ForgeLine.Crit: return InkTheme.Hex("7A3EC8");
                case ForgeLine.GoldGain: return InkTheme.Hex("B86E12");
                default: return InkTheme.TextDark;
            }
        }

        static float GunCenter => GunFoot + GunSize * 0.5f - GunSize * GunPad;

        void Arrow(string name, int dir)
        {
            var rt = UiKit.Art(_root, name, "Ui/skin_arrow", new Vector2(dir * ArrowX, ArrowY), new Vector2(66f, 66f));
            if (dir < 0) rt.localScale = new Vector3(-1f, 1f, 1f);
            var img = rt.GetComponent<Image>();
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Step(dir));
        }

        bool Capture()
        {
            var swipe = _root.Find("swipe") as RectTransform;
            _motion = swipe != null ? swipe.GetComponent<SkinStageMotion>() : null;
            _name = _root.Find("name")?.GetComponent<Text>();
            _rankTag = _root.Find("rank") as RectTransform;
            _rankBg = _rankTag != null ? _rankTag.GetComponent<Image>() : null;
            _rank = _rankTag != null ? _rankTag.Find("t")?.GetComponent<Text>() : null;
            _perk = _root.Find("perk")?.GetComponent<Text>();
            _gun = _root.Find("gun")?.GetComponent<Image>();
            _lock = _root.Find("lock")?.GetComponent<Image>();
            _lockText = _root.Find("need")?.GetComponent<Text>();
            _act = _root.Find("act")?.GetComponent<Button>();
            _actText = _act != null ? _act.GetComponentInChildren<Text>() : null;
            if (swipe == null || _name == null || _rankBg == null || _gun == null || _lock == null
                || _lockText == null || _act == null || _actText == null)
                return false;

            int n = SkinCatalog.Count;
            _frames = new Image[n];
            _faces = new Image[n];
            _used = new RectTransform[n];
            _dots = new RectTransform[n];
            for (int i = 0; i < n; i++)
            {
                Transform cell = _root.Find("av" + i);
                if (cell == null) return false;
                _frames[i] = cell.GetComponent<Image>();
                _faces[i] = cell.Find("face")?.GetComponent<Image>();
                _used[i] = cell.Find("used") as RectTransform;
                _dots[i] = cell.Find("dot") as RectTransform;
                if (_frames[i] == null || _faces[i] == null || _used[i] == null || _dots[i] == null) return false;
            }
            _fxName = new Text[FxSlots];
            _fxVal = new Text[FxSlots];
            for (int i = 0; i < FxSlots; i++)
            {
                _fxName[i] = _root.Find("fxn" + i)?.GetComponent<Text>();
                _fxVal[i] = _root.Find("fxv" + i)?.GetComponent<Text>();
                if (_fxName[i] == null || _fxVal[i] == null) return false;
            }
            return true;
        }

        void Repair()
        {
            if (_rankBg.sprite == null)
            {
                _rankBg.sprite = UiSprites.Fill(8);
                _rankBg.type = Image.Type.Sliced;
            }
            for (int i = 0; i < _used.Length; i++)
            {
                RepairDisc(_used[i]);
                RepairDisc(_dots[i]);
            }
            UiKit.RepairBtn(_act);
        }

        static void RepairDisc(RectTransform seal)
        {
            if (seal == null) return;
            var img = seal.GetComponent<Image>();
            if (img != null && img.sprite == null) img.sprite = UiSprites.Disc();
        }

        void AttachMotion(RectTransform swipe)
        {
            if (swipe == null) return;
            _motion = swipe.GetComponent<SkinStageMotion>();
            if (_motion == null) _motion = swipe.gameObject.AddComponent<SkinStageMotion>();
            _motion.Swiped = dir => Step(dir);
            if (_gun != null)
            {
                _motion.Gun = _gun.rectTransform;
                _motion.BaseY = GunCenter;
            }
        }

        void Rewire()
        {
            var swipe = _root.Find("swipe") as RectTransform;
            AttachMotion(swipe);
            WireArrow("prev", -1);
            WireArrow("next", 1);
            _act.onClick.RemoveAllListeners();
            _act.onClick.AddListener(OnAct);
            for (int i = 0; i < _frames.Length; i++)
            {
                var btn = _frames[i].GetComponent<Button>();
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                int idx = i;
                btn.onClick.AddListener(() => Show(idx, 0, true));
            }
        }

        void WireArrow(string name, int dir)
        {
            var btn = _root.Find(name)?.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => Step(dir));
        }

        static RectTransform Seal(RectTransform parent, string name, string text, Vector2 pos, float size, Color fill)
        {
            var b = UiKit.Panel(parent, name, pos, new Vector2(size, size), fill);
            var img = b.GetComponent<Image>();
            img.sprite = UiSprites.Disc();
            img.raycastTarget = false;
            if (text.Length > 0)
            {
                var t = UiKit.Label(b, "t", text, Mathf.RoundToInt(size * 0.62f), Vector2.zero, new Vector2(size, size));
                t.color = InkTheme.CardFace;
                UiKit.Bold(t);
            }
            b.gameObject.SetActive(false);
            return b;
        }

        void Step(int dir)
        {
            int n = SkinCatalog.Count;
            Show(((_focus + dir) % n + n) % n, dir, true);
        }

        // 外面重绑用这个，不回调 focusChanged，免得「换焦点 → 刷列表 → 重绑」绕圈。
        public void Bind(int focus)
        {
            _focus = Mathf.Clamp(focus, 0, SkinCatalog.Count - 1);
            Show(_focus, 0, false);
        }

        void Show(int i, int dir, bool notify)
        {
            if (dir == 0 && i != _focus) dir = i > _focus ? 1 : -1;
            bool moved = i != _focus;
            _focus = i;

            SkinDef d = SkinCatalog.Get(i);
            bool owned = _meta.SkinOwned[i];
            bool shard = d.Way == SkinWay.Shard;
            int have = shard ? _meta.SkinShardCount(i) : 0;
            int need = Mathf.Max(1, d.Shards);
            // 看广告的也先按没解锁画：暗影加锁。碎片还是 0 的同样锁成灰色，攒到了才亮出这门炮。
            bool shown = owned || _meta.SkinOpened(i) || (shard && have > 0);
            bool ad = !owned && d.Way == SkinWay.Ad;
            bool progress = !owned && shard;
            _gun.sprite = InkSprites.Ui("skin_" + d.Key);
            _gun.color = shown ? Color.white : Shadow;
            _lock.gameObject.SetActive(!shown);
            _lockText.gameObject.SetActive(progress || (!owned && !shown && !ad));
            _lockText.fontSize = progress ? 20 : 26;
            _lockText.text = progress
                ? "碎片 " + have + "/" + need + " 通过" + ChestCatalog.CannonName + "获取"
                : SkinCatalog.LockText(d);
            _name.text = d.Name;
            _name.color = SkinCatalog.RankDeep(d.Rarity);
            _rank.text = SkinCatalog.RankName(d.Rarity);
            _rankBg.color = SkinCatalog.RankColor(d.Rarity);
            PlaceTitle();
            _motion.Alpha = _gun.color.a;
            _motion.SlideIn(dir);

            int line = SkinCatalog.LineOf(i);
            bool plain = line < 0 && d.DamageAdd <= 0.01f && d.GoldAdd <= 0;
            _perk.gameObject.SetActive(!plain);
            _perk.text = SkinCatalog.PerkOf(i);

            BindAct(i, d, owned, shown);
            for (int k = 0; k < _frames.Length; k++) BindAvatar(k);
            PaintFx();
            if (notify && moved) _focusChanged?.Invoke(i);
        }

        // 名字始终钉在正中。标签贴在字的左侧，不参与居中。
        void PlaceTitle()
        {
            Canvas.ForceUpdateCanvases();
            float nameW = _name.preferredWidth;
            if (nameW < 8f) nameW = _name.text.Length * _name.fontSize;
            const float tagW = 54f;
            const float tagH = 26f;
            const float gap = 8f;
            _rankTag.sizeDelta = new Vector2(tagW, tagH);
            _rankTag.anchoredPosition = new Vector2(-nameW * 0.5f - gap - tagW * 0.5f, NameY);
        }

        void BindAct(int i, SkinDef d, bool owned, bool ready)
        {
            bool on = _meta.Skin == i;
            string text;
            bool live;
            if (on) { text = "使用中"; live = false; }
            else if (owned) { text = "装备"; live = true; }
            else if (d.Way == SkinWay.Ad) { text = "看广告解锁"; live = true; }
            else if (d.Way == SkinWay.Check) { text = "签到获得"; live = false; }
            else if (d.Way == SkinWay.Event) { text = "活动获取"; live = false; }
            else if (d.Way == SkinWay.Shard)
            {
                int have = _meta.SkinShardCount(i);
                int need = Mathf.Max(1, d.Shards);
                text = have >= need ? "合成" : "碎片 " + have + "/" + need;
                live = have >= need;
            }
            else if (ready) { text = d.Price + "墨 购买"; live = _meta.CanBuySkin(i, out _); }
            else { text = "未开放"; live = false; }
            _actText.text = text;
            _act.interactable = live;
            if (live) UiKit.PaintBtn(_act, InkTheme.Cta, InkTheme.CtaDeep, InkTheme.CardFace);
            else if (on) UiKit.PaintBtn(_act, InkTheme.Plain, InkTheme.PlainDeep, InkTheme.Seal);
            else UiKit.PaintBtn(_act, InkTheme.CardDim, InkTheme.LineDim, InkTheme.TextDark);
        }

        void BindAvatar(int k)
        {
            bool owned = _meta.SkinOwned[k];
            SkinDef dk0 = SkinCatalog.Get(k);
            bool ready = owned || _meta.SkinReady(k)
                || (dk0.Way == SkinWay.Shard && _meta.SkinShardCount(k) > 0);
            string key = k == _focus ? "Ui/skin_frame_on" : (ready ? "Ui/skin_frame" : "Ui/skin_frame_lock");
            Sprite spr = InkSprites.Load(key);
            if (spr != null) _frames[k].sprite = spr;
            _frames[k].rectTransform.localScale = Vector3.one * (k == _focus ? 1.12f : 1f);
            _faces[k].sprite = InkSprites.Ui("skin_" + dk0.Key);
            _faces[k].color = ready ? Color.white : Shadow;
            _used[k].gameObject.SetActive(_meta.Skin == k);
            bool ad = !owned && dk0.Way == SkinWay.Ad;
            bool craft = !owned && dk0.Way == SkinWay.Shard && _meta.SkinShardCount(k) >= dk0.Shards;
            _dots[k].gameObject.SetActive(ad || craft || (!owned && _meta.Skin != k && _meta.CanBuySkin(k, out _)));
        }

        void FlySkin(int i)
        {
            if (_root == null) return;
            RectTransform layer = RewardFly.LayerOf(_root);
            Vector2 from = RewardFly.Local(layer, _act != null ? _act.transform : _root);
            RectTransform gun = _gun != null ? _gun.rectTransform : null;
            RewardFly.Play(layer, from, new[] { RewardFly.Skin(layer, i, gun) });
        }

        void OnAct()
        {
            int i = _focus;
            if (_meta.SkinOwned[i])
            {
                _meta.EquipSkin(i);
                _changed?.Invoke();
                return;
            }
            if (SkinCatalog.Get(i).Way == SkinWay.Ad)
            {
                AdStub.Reward("skin", () =>
                {
                    if (!_meta.GrantSkin(i)) return;
                    FlySkin(i);
                    if (_root != null) _changed?.Invoke();
                });
                return;
            }
            if (SkinCatalog.Get(i).Way == SkinWay.Shard && _meta.TryCraftSkin(i))
            {
                FlySkin(i);
                if (_root != null) _changed?.Invoke();
                return;
            }
            if (_meta.BuySkin(i))
            {
                FlySkin(i);
                _changed?.Invoke();
            }
        }
    }

    // 展台上的炮：切换时从划来的方向滑进来，平时轻轻上下浮。
    public sealed class SkinStageMotion : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Gun;
        public float BaseY;
        public Action<int> Swiped;

        const float Slide = 0.22f;
        float _from;
        float _t = 1f;
        float _dragX;

        public void SlideIn(int dir)
        {
            if (dir == 0) return;
            _from = dir * 150f;
            _t = 0f;
        }

        void Update()
        {
            if (Gun == null) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Slide);
            float k = 1f - Mathf.Pow(1f - _t, 3f);
            float x = _from * (1f - k) + _dragX * 0.35f;
            float y = BaseY + Mathf.Sin(Time.unscaledTime * 2.2f) * 4f;
            Gun.anchoredPosition = new Vector2(x, y);
            if (_img == null) _img = Gun.GetComponent<Image>();
            if (_img != null)
            {
                Color c = _img.color;
                c.a = Alpha * Mathf.Clamp01(_t * 1.6f);
                _img.color = c;
            }
        }

        Image _img;
        public float Alpha = 1f;

        public void OnBeginDrag(PointerEventData e) => _dragX = 0f;

        public void OnDrag(PointerEventData e)
        {
            var canvas = GetComponentInParent<Canvas>();
            float s = canvas != null ? canvas.scaleFactor : 1f;
            _dragX += e.delta.x / Mathf.Max(0.01f, s);
        }

        public void OnEndDrag(PointerEventData e)
        {
            float dx = _dragX;
            _dragX = 0f;
            if (Mathf.Abs(dx) < 60f) return;
            Swiped?.Invoke(dx < 0f ? 1 : -1);
        }
    }
}
