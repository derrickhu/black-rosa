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
        const float AvatarStep = 84f;
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
        Text _perk;
        Button _act;
        Text _actText;
        Image[] _frames;
        Image[] _faces;
        RectTransform[] _used;
        RectTransform[] _dots;
        SkinStageMotion _motion;
        int _focus;

        public RectTransform Root => _root;
        public int Focus => _focus;

        SkinShowcase(MetaProgress meta, RectTransform root, Action<int> focusChanged, Action changed)
        {
            _meta = meta;
            _root = root;
            _focusChanged = focusChanged;
            _changed = changed;
        }

        // 预制体路径每次 Rebuild 只重新绑定，不重建；无预制体路径页面整个清掉，跟着重建。
        public static SkinShowcase Ensure(SkinShowcase have, RectTransform page, float top, MetaProgress meta,
            Action<int> focusChanged, Action changed)
        {
            if (have != null && have._root != null && have._root.parent == page) return have;
            var root = UiKit.Art(page, "showcase", "Ui/skin_stage", new Vector2(0f, top), new Vector2(W, H), Pin.Top);
            root.GetComponent<Image>().raycastTarget = false;
            var s = new SkinShowcase(meta, root, focusChanged, changed);
            s.Build();
            return s;
        }

        void Build()
        {
            // 横划区垫在炮下面，箭头、按钮和头像在它上层，点它们不会被吞。
            var swipe = UiKit.Panel(_root, "swipe", new Vector2(0f, 40f), new Vector2(460f, 320f),
                new Color(1f, 1f, 1f, 0f));
            _motion = swipe.gameObject.AddComponent<SkinStageMotion>();
            _motion.Swiped = dir => Step(dir);

            _name = UiKit.Label(_root, "name", "", 34, new Vector2(0f, NameY), new Vector2(300f, 44f));
            _name.color = InkTheme.TextDark;
            UiKit.Bold(_name);
            _perk = UiKit.Label(_root, "perk", "", 19, new Vector2(0f, PerkY), new Vector2(320f, 28f));
            _perk.color = InkTheme.Seal;
            UiKit.Bold(_perk);

            _gun = UiKit.Icon(_root, null, new Vector2(0f, GunCenter), GunSize);
            _motion.Gun = _gun.rectTransform;
            _motion.BaseY = GunCenter;
            _lock = UiKit.Icon(_root, InkSprites.Ui("lock"), new Vector2(0f, GunCenter + 8f), 72f);
            _lockText = UiKit.Label(_root, "need", "", 26, new Vector2(0f, GunCenter - 44f), new Vector2(260f, 36f));
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
                _used[i] = Seal(cell, "用", new Vector2(22f, 28f), 26f, InkTheme.Seal);
                _dots[i] = Seal(cell, "", new Vector2(24f, 30f), 16f, InkTheme.Rose);
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

        static RectTransform Seal(RectTransform parent, string text, Vector2 pos, float size, Color fill)
        {
            var b = UiKit.Panel(parent, "seal", pos, new Vector2(size, size), fill);
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
            // 看广告的也先按没解锁画：暗影加锁。到手之前不给看彩色大图。
            bool shown = owned || _meta.SkinOpened(i);
            bool ad = !owned && d.Way == SkinWay.Ad;
            _gun.sprite = InkSprites.Ui("skin_" + d.Key);
            _gun.color = shown ? Color.white : Shadow;
            _lock.gameObject.SetActive(!shown);
            _lockText.gameObject.SetActive(!shown && !ad);
            _lockText.text = SkinCatalog.LockText(d);
            _name.text = d.Name;
            _motion.Alpha = _gun.color.a;
            _motion.SlideIn(dir);

            int line = SkinCatalog.LineOf(i);
            bool plain = line < 0 && d.DamageAdd <= 0.01f && d.GoldAdd <= 0;
            _perk.gameObject.SetActive(!plain);
            _perk.text = SkinCatalog.PerkOf(i);

            BindAct(i, d, owned, shown);
            for (int k = 0; k < _frames.Length; k++) BindAvatar(k);
            if (notify && moved) _focusChanged?.Invoke(i);
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
            bool ready = owned || _meta.SkinReady(k);
            string key = k == _focus ? "Ui/skin_frame_on" : (ready ? "Ui/skin_frame" : "Ui/skin_frame_lock");
            Sprite spr = InkSprites.Load(key);
            if (spr != null) _frames[k].sprite = spr;
            _frames[k].rectTransform.localScale = Vector3.one * (k == _focus ? 1.12f : 1f);
            _faces[k].sprite = InkSprites.Ui("skin_" + SkinCatalog.Get(k).Key);
            _faces[k].color = ready ? Color.white : Shadow;
            _used[k].gameObject.SetActive(_meta.Skin == k);
            bool ad = !owned && SkinCatalog.Get(k).Way == SkinWay.Ad;
            _dots[k].gameObject.SetActive(ad || (!owned && _meta.Skin != k && _meta.CanBuySkin(k, out _)));
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
                    if (_root != null) _changed?.Invoke();
                });
                return;
            }
            if (_meta.BuySkin(i)) _changed?.Invoke();
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
