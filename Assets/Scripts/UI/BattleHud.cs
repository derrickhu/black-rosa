using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class BattleHud
    {
        // 一个道具键就是道具本身：冷却中整只发灰发淡，亮色从底下往上灌满；
        // 灌满了背后起一圈品质色的光，等时机自己丢出去。
        public sealed class ItemKey
        {
            public RectTransform Root;
            public RectTransform Visual;
            public Image Glow;
            public Image Ring;
            public Image Dim;
            public RectTransform Fill;
            public Image Art;
            public int Slot;
            public int Shown = -1;
            public float Cast;
            public float SeenFlash;
        }

        public readonly Text Gold;
        public readonly RectTransform GoldChip;
        public readonly Image[] Hearts;
        public readonly Text Wave;
        public readonly Text Toast;
        public readonly Text Ink;
        public readonly RectTransform InkChip;
        public readonly Button Draft;
        public readonly Button Settings;
        public readonly Text DraftLabel;
        public readonly ItemKey[] Keys;
        readonly RectTransform _layer;
        readonly RectTransform _heartsWrap;
        readonly RectTransform _wavePlate;
        RectTransform _adGun;
        Image _adGhost;
        Image _adMark;

        BattleHud(RectTransform layer, Text gold, RectTransform goldChip, Image[] hearts,
            RectTransform heartsWrap, RectTransform wavePlate, Text wave, Text toast, Text ink, RectTransform inkChip,
            Button draft, Button retreat, Text draftLabel, ItemKey[] keys)
        {
            _layer = layer;
            _wavePlate = wavePlate;
            Gold = gold;
            GoldChip = goldChip;
            Hearts = hearts;
            _heartsWrap = heartsWrap;
            Wave = wave;
            Toast = toast;
            Ink = ink;
            InkChip = inkChip;
            Draft = draft;
            Settings = retreat;
            DraftLabel = draftLabel;
            Keys = keys;
        }

        public static BattleHud Build(RectTransform layer, BattleWorld world,
            System.Action retreat, System.Action draft, System.Action addEmitter)
        {
            // ScreenFit 在没有刘海时也保底 36，那是给首页顶栏的呼吸。
            // 战斗页再加 8，三个药丸就掉进走怪区，像浮在战场当中。
            // 真机安全区 / 胶囊下沿已经大于 36，原样贴着排，不再往下加。
            float pad = ScreenFit.TopPad;
            float top = pad <= 36.1f ? 10f : pad;

            // 读数做成药丸，和首页顶栏一套构件，图标也是同一批手绘图。
            var chip = new Vector2(172f, 54f);
            var gold = UiKit.Chip(layer, "gold", InkSprites.Ui("gold"), "0",
                new Vector2(18f, top), chip, Pin.TopLeft, out RectTransform goldChip);

            // 局内墨摆右上，和左上的金币对称。中间留给波次。
            // 图标用首页那滴墨，不用闪电 —— 闪电在首页是体力，两处撞图标，
            // 玩家会以为局内打怪在回体力。
            var ink = UiKit.Chip(layer, "ink", InkSprites.Ui("ink"), "0",
                new Vector2(18f, top), chip, Pin.TopRight, out RectTransform inkChip);

            var wavePlate = UiKit.Stroke(layer, "waveplate", new Vector2(0f, top), new Vector2(196f, 54f),
                Pin.Top, 5f, radius: 27f);
            wavePlate.GetComponent<Image>().raycastTarget = false;
            var wave = UiKit.Label(wavePlate, "n", "", 28, Vector2.zero, new Vector2(186f, 48f));
            UiKit.Bold(wave);

            var toast = UiKit.Label(layer, "toast", "", 24, new Vector2(0, top + 62f), new Vector2(640, 44),
                TextAnchor.MiddleCenter, Pin.Top);
            toast.color = InkTheme.TextDark;

            // 左上角是设置。底栏只留改装和技能。
            var draftBtn = UiKit.Btn(layer, "draft", "改装", Vector2.zero, new Vector2(280, 84), draft, true, Pin.Bottom);
            var draftLabel = draftBtn.GetComponentInChildren<Text>();
            var settingsBtn = GearButton(layer, retreat);

            var keys = new ItemKey[GameConstants.ItemSlots];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = MakeKey(layer, i, Vector2.zero);

            int maxHp = world != null ? world.MaxBaseHp : GameConstants.BaseHp;
            var hearts = UiKit.Hearts(layer, Vector2.zero, 36f, maxHp, Pin.Center);
            var heartsWrap = hearts.Length > 0 ? hearts[0].transform.parent as RectTransform : null;

            var hud = new BattleHud(layer, gold, goldChip, hearts, heartsWrap, wavePlate, wave, toast,
                ink, inkChip, draftBtn, settingsBtn, draftLabel, keys);
            hud._fx = ItemFx.Build(layer);
            hud._world = world;
            for (int i = 0; i < keys.Length; i++)
            {
                ItemKey k = keys[i];
                var hit = k.Root.GetComponent<Image>();
                hit.raycastTarget = true;
                var btn = k.Root.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = hit;
                btn.onClick.AddListener(() => hud.ToggleTip(k));
            }
            hud.BuildAdGun(addEmitter);
            hud.Layout();
            return hud;
        }

        static ItemKey MakeKey(RectTransform layer, int slot, Vector2 pos)
        {
            var root = UiKit.Panel(layer, "key" + slot, pos, new Vector2(KeyW, KeyH), Color.clear, Pin.BottomRight);
            root.GetComponent<Image>().raycastTarget = false;
            var visual = UiKit.Panel(root, "v", Vector2.zero, new Vector2(KeyW, KeyH), Color.clear);
            visual.GetComponent<Image>().raycastTarget = false;
            var glow = UiKit.Icon(visual, InkFx.SoftDisc(), Vector2.zero, KeyW * 2.2f);
            var ring = UiKit.Icon(visual, InkFx.SoftRing(), Vector2.zero, KeyW * 1.2f);
            ring.enabled = false;
            var dim = UiKit.Icon(visual, null, Vector2.zero, KeyW);

            // 裁切框钉在图标下沿，高度跟着冷却走。框里的彩色图标是满尺寸、底对齐，
            // 所以露出来的永远是图标的下半截，往上长。
            var fillGo = new GameObject("fill", typeof(RectTransform), typeof(RectMask2D));
            fillGo.transform.SetParent(visual, false);
            var fill = fillGo.GetComponent<RectTransform>();
            fill.anchorMin = fill.anchorMax = new Vector2(0.5f, 0f);
            fill.pivot = new Vector2(0.5f, 0f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(KeyW, 0f);
            var art = UiKit.Icon(fill, null, Vector2.zero, KeyW);
            var artRt = art.rectTransform;
            artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0f);
            artRt.pivot = new Vector2(0.5f, 0f);
            artRt.anchoredPosition = Vector2.zero;
            artRt.sizeDelta = new Vector2(KeyW, KeyH);
            return new ItemKey
            {
                Root = root, Visual = visual, Glow = glow, Ring = ring, Dim = dim, Fill = fill, Art = art, Slot = slot
            };
        }

        static Button GearButton(RectTransform layer, System.Action open)
        {
            var go = new GameObject("settings", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(layer, false);
            var img = go.GetComponent<Image>();
            img.sprite = InkSprites.Ui("settings");
            img.preserveAspect = true;
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => open());
            return btn;
        }

        const float RowH = 84f;
        const float KeyW = 84f;
        const float KeyH = 84f;
        const float KeySide = 8f;
        const float KeyGap = 14f;
        const float Gear = 76f;
        const float Side = 16f;
        const float Gap = 10f;

        public void Layout()
        {
            if (_layer == null) return;
            float bot = ScreenFit.BottomPad + 16f;
            float pad = ScreenFit.TopPad;
            float top = pad <= 36.1f ? 10f : pad;
            float row = top + Gear + 8f;
            float canvasW = Mathf.Max(720f, _layer.rect.width);
            float draftW = Mathf.Clamp(canvasW - 2f * (Side + Gap), 200f, 460f);
            float draftX = 0f;

            PinTop(Settings.transform as RectTransform, new Vector2(Side, top),
                new Vector2(Gear, Gear), 6f, Pin.TopLeft);
            PinTop(GoldChip, new Vector2(18f, row), new Vector2(172f, 54f), 7f, Pin.TopLeft);
            PinTop(_wavePlate, new Vector2(0f, row), new Vector2(196f, 54f), 7f, Pin.Top);
            PinTop(InkChip, new Vector2(18f, row), new Vector2(172f, 54f), 7f, Pin.TopRight);
            if (Toast != null)
            {
                var toastRt = Toast.rectTransform;
                toastRt.anchorMin = toastRt.anchorMax = new Vector2(0.5f, 1f);
                toastRt.pivot = new Vector2(0.5f, 1f);
                toastRt.anchoredPosition = new Vector2(0f, -(row + 58f));
            }
            PinBottom(Draft.transform as RectTransform, new Vector2(draftX, bot),
                new Vector2(draftW, RowH), 8f, Pin.Bottom);
            if (DraftLabel != null)
            {
                var box = DraftLabel.GetComponent<RectTransform>();
                if (box != null) box.sizeDelta = new Vector2(draftW, RowH);
            }
            if (_heartsWrap != null)
            {
                // 格子下沿居中。Pin.Center，世界坐标转画布中心偏移。
                _heartsWrap.anchorMin = _heartsWrap.anchorMax = _heartsWrap.pivot = new Vector2(0.5f, 0.5f);
                _heartsWrap.anchoredPosition = WorldToCanvas(_layer, new Vector3(0f, FieldLayout.GridBottom - 0.20f, 0f));
            }

            FitCamera(bot + RowH, Mathf.Max(1f, _layer.rect.height));
            LayoutKeys();
        }

        // 道具竖排贴右边，从格子上沿往上叠，第一格在最下面。炮弹从格子里往上走，
        // 贴边这一条几乎没有弹道。相机定好之后再算，格子上沿才对得上。
        void LayoutKeys()
        {
            float h = Mathf.Max(1f, _layer.rect.height);
            float gridTop = WorldToCanvas(_layer, new Vector3(0f, FieldLayout.GridTop, 0f)).y + h * 0.5f;
            _keyY0 = Mathf.Max(ScreenFit.BottomPad + 16f + RowH + 12f, gridTop + 18f);
            for (int i = 0; i < Keys.Length; i++)
            {
                RectTransform rt = Keys[i].Root;
                if (rt == null) continue;
                rt.sizeDelta = new Vector2(KeyW, KeyH);
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-KeySide, _keyY0 + i * (KeyH + KeyGap));
            }
        }

        float _keyY0;

        // 任何长宽比下，炮的下沿都要高过底栏上沿。短了就加视野、必要时下移相机，
        // 同时保住刷怪点还在画面里。
        static void FitCamera(float uiTopFromBottom, float canvasH)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            canvasH = Mathf.Max(1f, canvasH);
            float needV = Mathf.Clamp01((uiTopFromBottom + 14f) / canvasH);
            float spawnTop = GameConstants.SpawnY + 0.20f;
            float cannonBot = GameConstants.EmitterY - 0.42f;
            float ortho = cam.orthographicSize;
            float needOrtho = (spawnTop - cannonBot) / (2f * Mathf.Max(0.20f, 1f - needV));
            if (needOrtho > ortho)
            {
                cam.orthographicSize = needOrtho;
                ortho = needOrtho;
            }
            float camMax = cannonBot + ortho - needV * 2f * ortho;
            float camMin = spawnTop - ortho;
            float y = Mathf.Clamp(0f, camMin, camMax);
            Vector3 p = cam.transform.position;
            cam.transform.position = new Vector3(0f, y, p.z);
            InkShake.Pin(cam);
        }

        public static void ReleaseCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 p = cam.transform.position;
            cam.transform.position = new Vector3(0f, 0f, p.z);
            InkShake.Pin(cam);
        }

        public static Vector2 WorldToCanvas(RectTransform layer, Vector3 world)
        {
            Camera cam = Camera.main;
            if (cam == null || layer == null) return Vector2.zero;
            Vector3 vp = cam.WorldToViewportPoint(world);
            return new Vector2((vp.x - 0.5f) * layer.rect.width, (vp.y - 0.5f) * layer.rect.height);
        }

        static void PinTop(RectTransform rt, Vector2 pos, Vector2 size, float shadowDy, Pin pin)
        {
            if (rt == null) return;
            rt.sizeDelta = size;
            switch (pin)
            {
                case Pin.Top:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(pos.x, -pos.y);
                    break;
                case Pin.TopRight:
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-pos.x, -pos.y);
                    break;
                default:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(pos.x, -pos.y);
                    break;
            }
            var sh = rt.parent != null ? rt.parent.Find(rt.name + "_sh") as RectTransform : null;
            if (sh == null) return;
            sh.sizeDelta = size;
            sh.anchorMin = rt.anchorMin;
            sh.anchorMax = rt.anchorMax;
            sh.pivot = rt.pivot;
            sh.anchoredPosition = rt.anchoredPosition + new Vector2(0f, -shadowDy);
        }

        static void PinBottom(RectTransform rt, Vector2 pos, Vector2 size, float shadowDy, Pin pin)
        {
            if (rt == null) return;
            rt.sizeDelta = size;
            switch (pin)
            {
                case Pin.Bottom:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(pos.x, pos.y);
                    break;
                case Pin.BottomLeft:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = pos;
                    break;
                default:
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-pos.x, pos.y);
                    break;
            }
            var sh = rt.parent != null ? rt.parent.Find(rt.name + "_sh") as RectTransform : null;
            if (sh == null) return;
            sh.sizeDelta = size;
            sh.anchorMin = rt.anchorMin;
            sh.anchorMax = rt.anchorMax;
            sh.pivot = rt.pivot;
            sh.anchoredPosition = rt.anchoredPosition + new Vector2(0f, -shadowDy);
        }

        // 掉落物要飞到药丸上，得知道药丸在世界里的哪儿。画布是 ScreenSpaceOverlay，
        // RectTransform.position 本身就是屏幕像素，直接反投回去就行。
        public static Vector2 ChipInWorld(RectTransform chip, Vector2 fallback)
        {
            Camera cam = Camera.main;
            if (cam == null || chip == null) return fallback;
            Vector3 p = cam.ScreenToWorldPoint(new Vector3(chip.position.x, chip.position.y, 0f));
            return new Vector2(p.x, p.y);
        }

        public void Refresh(BattleWorld world, bool inBattle, string tip)
        {
            if (world == null || Gold == null) return;
            Layout();
            Gold.text = world.Gold.ToString();
            // 到账脉冲由这里衰减：世界在暂停和顿帧里都不走 Tick，
            // 而金币飞进来那一下恰恰常常压在顿帧上。
            float dt = Time.unscaledDeltaTime;
            world.GoldPop = Mathf.Max(0f, world.GoldPop - dt * 3.4f);
            world.InkPop = Mathf.Max(0f, world.InkPop - dt * 3.4f);
            Pop(GoldChip, Gold, world.GoldPop, InkTheme.CoinDeep);
            Pop(InkChip, Ink, world.InkPop, InkTheme.Track);
            int hp = Mathf.Clamp(world.BaseHp, 0, Hearts.Length);
            for (int i = 0; i < Hearts.Length; i++)
                Hearts[i].color = i < hp ? Color.white : new Color(1f, 1f, 1f, 0.28f);
            // 最后一波计时走完，下标会拨到总波数外面，用来等场上的怪清完再结算。
            // 显示停在最后一波，清场时才不会看成 3/2、6/5。
            int wave = Mathf.Min(world.WaveIndex + 1, world.Stage.Waves.Length);
            Wave.text = world.BossSpawned ? "关底" : $"波 {wave}/{world.Stage.Waves.Length}";
            Ink.text = world.Ink.ToString();
            if (world.ToastTime > 0f) Toast.text = world.Toast;
            else if (!string.IsNullOrEmpty(tip)) Toast.text = tip;
            else if (world.RevealTime > 0f) Toast.text = $"显形 · {world.LastReveal}";
            else Toast.text = "";
            RefreshKeys(world, inBattle);
            if (_fx != null) _fx.Hold(world.Paused);
            bool can = world.CanDraft && inBattle;
            Draft.interactable = inBattle;
            DraftLabel.text = can ? $"改装  {world.DraftCost}" : $"差  {Mathf.Max(0, world.DraftCost - world.Gold)}";
            if (can) UiKit.PaintBtn(Draft, InkTheme.Cta, InkTheme.CtaDeep, InkTheme.CardFace);
            else UiKit.PaintBtn(Draft, InkTheme.CardDim, InkTheme.LineDim, InkTheme.TextDark);
            Draft.transform.localScale = can
                ? Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 8f))
                : Vector3.one;
            PlaceAdGun(world, inBattle);
        }

        // 广告炮位：不再套白框。原地摆一门和真炮一样大的淡色炮，角上压一枚广告章，
        // 一眼读成「这里还能再来一门，看个广告就是你的」。整块透明区域都能点。
        void BuildAdGun(System.Action addEmitter)
        {
            _adGun = UiKit.Panel(_layer, "adgun", Vector2.zero, new Vector2(88f, 88f), Color.clear);
            var btn = _adGun.gameObject.AddComponent<Button>();
            btn.targetGraphic = _adGun.GetComponent<Image>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => addEmitter?.Invoke());
            _adGhost = UiKit.Icon(_adGun, InkSprites.CannonSkin(GunSkin), Vector2.zero, 88f);
            _adMark = UiKit.Icon(_adGun, ResultKit.AdBadge(), Vector2.zero, 40f);
            _adGun.gameObject.SetActive(false);
        }

        // 新手第一关：不给设置（里面有撤退）和看广告加炮，只留打仗要用的。
        public bool Guided
        {
            get => _guided;
            set
            {
                _guided = value;
                if (Settings != null) Settings.gameObject.SetActive(!value);
            }
        }
        bool _guided;

        // 和 BattleView 里的炮同一张图、同一个缩放，淡色版本才对得上真炮的大小。
        public int GunSkin;
        int _ghostSkin = -1;
        const float GunScale = 0.60f;

        void PlaceAdGun(BattleWorld world, bool inBattle)
        {
            if (_adGun == null) return;
            bool show = inBattle && !Guided && world.EmitterCount < GameConstants.AdEmitterCap;
            _adGun.gameObject.SetActive(show);
            if (!show) return;
            if (_ghostSkin != GunSkin && _adGhost != null)
            {
                _ghostSkin = GunSkin;
                _adGhost.sprite = InkSprites.CannonSkin(GunSkin);
            }
            Vector3 at = new Vector3(world.AdSlotPos.x, world.AdSlotPos.y, 0f);
            Vector2 c = WorldToCanvas(_layer, at);
            Vector2 c2 = WorldToCanvas(_layer, at + new Vector3(GameConstants.CellWidth, 0f, 0f));
            float perWorld = Mathf.Abs(c2.x - c.x) / GameConstants.CellWidth;
            float hit = Mathf.Clamp(perWorld * GameConstants.CellWidth * 0.92f, 52f, 140f);
            _adGun.anchorMin = _adGun.anchorMax = _adGun.pivot = new Vector2(0.5f, 0.5f);
            _adGun.sizeDelta = new Vector2(hit, hit);
            _adGun.anchoredPosition = c;

            float t = Time.unscaledTime;
            if (_adGhost != null)
            {
                Sprite spr = _adGhost.sprite;
                float gun = spr != null ? Mathf.Max(spr.bounds.size.x, spr.bounds.size.y) * GunScale * perWorld : hit;
                _adGhost.rectTransform.sizeDelta = new Vector2(gun, gun);
                // 淡，但会轻轻呼吸，像在等人来领。
                float a = 0.42f + 0.10f * Mathf.Sin(t * 3.2f);
                _adGhost.color = new Color(1f, 1f, 1f, a);
            }
            if (_adMark != null)
            {
                float m = hit * 0.40f;
                float bob = 1f + 0.08f * Mathf.Sin(t * 5.5f);
                var mr = _adMark.rectTransform;
                mr.sizeDelta = new Vector2(m, m);
                mr.anchoredPosition = new Vector2(hit * 0.30f, -hit * 0.24f);
                mr.localScale = new Vector3(bob, bob, 1f);
            }
        }

        // 收到一笔就把药丸顶一下、数字染一下色。飞过去的金币要在这儿落地有声，
        // 否则那段飞行只是装饰，玩家仍然不知道自己赚到了。
        static void Pop(RectTransform chip, Text value, float pulse, Color flash)
        {
            if (chip == null) return;
            chip.localScale = Vector3.one * (1f + 0.18f * pulse);
            if (value != null) value.color = Color.Lerp(InkTheme.TextDark, flash, pulse);
        }

        ItemFx _fx;
        BattleWorld _world;
        RectTransform _tip;
        ItemKey _tipKey;
        Text _tipCd;
        float _tipLeft;
        const float TipW = 370f;
        const float TipShow = 3.2f;

        void PlayCasts(BattleWorld world)
        {
            if (world.ItemCasts.Count == 0) return;
            for (int i = 0; i < world.ItemCasts.Count; i++)
            {
                BattleWorld.ItemCast c = world.ItemCasts[i];
                if (_fx == null || BattleWorld.PreviewFill) continue;
                Vector2 key = new Vector2(_layer.rect.width * 0.5f - KeySide - KeyW * 0.5f, 0f);
                foreach (ItemKey k in Keys)
                {
                    if (k.Slot != c.Slot || k.Root == null) continue;
                    Vector3 wp = k.Root.TransformPoint(k.Root.rect.center);
                    key = (Vector2)_fx.transform.InverseTransformPoint(wp);
                }
                Vector2 hearts = _heartsWrap != null
                    ? _heartsWrap.anchoredPosition
                    : WorldToCanvas(_layer, new Vector3(0f, FieldLayout.GridBottom, 0f));
                _fx.Play(c, world, key, hearts);
            }
            world.ItemCasts.Clear();
        }

        // 点道具键：键左边弹一张小卡，写这件道具干什么、什么时候自己丢。再点一下或过几秒收起。
        void ToggleTip(ItemKey k)
        {
            bool same = _tip != null && _tipKey == k;
            CloseTip();
            if (same || _world == null) return;
            int id = _world.SlotItem(k.Slot);
            if (id < 0) return;
            AudioBus.Tap();
            ItemDef d = ItemCatalog.Get(id);
            int lv = _world.ItemRanks != null && id < _world.ItemRanks.Length ? Mathf.Max(1, _world.ItemRanks[id]) : 1;
            Color q = ItemCatalog.QualityColor(d.Quality);

            const float h = 196f;
            var card = UiKit.Stroke(_layer, "itemtip", Vector2.zero, new Vector2(TipW, h), Pin.Center, 5f,
                null, InkTheme.Hex("FFF8EA"), 24f);
            card.GetComponent<Image>().raycastTarget = false;
            Vector3 wp = k.Root.TransformPoint(k.Root.rect.center);
            Vector2 at = (Vector2)_layer.InverseTransformPoint(wp);
            float halfH = _layer.rect.height * 0.5f;
            Vector2 pos = new Vector2(at.x - KeyW * 0.5f - 14f - TipW * 0.5f,
                Mathf.Clamp(at.y, -halfH + h * 0.5f + 20f, halfH - h * 0.5f - 20f));
            card.anchoredPosition = pos;
            var sh = _layer.Find("itemtip_sh") as RectTransform;
            if (sh != null) sh.anchoredPosition = pos + new Vector2(0f, -6f);

            var band = UiKit.Panel(card, "band", new Vector2(0f, -4f), new Vector2(TipW - 10f, 8f), q, Pin.Top);
            band.GetComponent<Image>().raycastTarget = false;
            UiKit.Icon(card, InkSprites.Ui(d.Id), new Vector2(-TipW * 0.5f + 52f, h * 0.5f - 58f), 72f);
            var name = UiKit.Label(card, "name", d.Name, 30, new Vector2(-TipW * 0.5f + 96f + 70f, h * 0.5f - 42f),
                new Vector2(140f, 40f), TextAnchor.MiddleLeft);
            UiKit.Bold(name);
            var pill = UiKit.Panel(card, "q", new Vector2(TipW * 0.5f - 96f, h * 0.5f - 42f), new Vector2(64f, 30f), q);
            pill.GetComponent<Image>().sprite = UiSprites.Fill(UiSprites.TierFor(new Vector2(64f, 30f)));
            pill.GetComponent<Image>().type = Image.Type.Sliced;
            var ql = UiKit.Label(pill, "t", ItemCatalog.QualityName(d.Quality), 18, Vector2.zero, new Vector2(64f, 30f));
            ql.color = Color.white;
            UiKit.Bold(ql);
            var lvl = UiKit.Label(card, "lv", "Lv." + lv, 20, new Vector2(TipW * 0.5f - 34f, h * 0.5f - 42f),
                new Vector2(56f, 30f));
            lvl.color = ItemCatalog.QualityDeep(d.Quality);
            UiKit.Bold(lvl);
            var cd = UiKit.Label(card, "cd", "", 18, new Vector2(-TipW * 0.5f + 96f + 110f, h * 0.5f - 76f),
                new Vector2(220f, 26f), TextAnchor.MiddleLeft);
            cd.color = InkTheme.TextMid;
            var blurb = UiKit.Label(card, "b", ItemCatalog.Blurb(d, lv, _world.ShotBase), 21,
                new Vector2(0f, -22f), new Vector2(TipW - 36f, 56f), TextAnchor.UpperLeft);
            blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
            var when = UiKit.Label(card, "w", "自动触发：" + d.When, 18, new Vector2(0f, -h * 0.5f + 26f),
                new Vector2(TipW - 36f, 26f), TextAnchor.MiddleLeft);
            when.color = q;
            UiKit.Bold(when);

            _tip = card;
            _tipKey = k;
            _tipCd = cd;
            _tipLeft = TipShow;
            card.localScale = Vector3.one * 0.6f;
        }

        void CloseTip()
        {
            if (_tip == null) return;
            var sh = _layer != null ? _layer.Find("itemtip_sh") : null;
            if (sh != null) Object.Destroy(sh.gameObject);
            Object.Destroy(_tip.gameObject);
            _tip = null;
            _tipKey = null;
            _tipCd = null;
        }

        void TickTip(BattleWorld world)
        {
            if (_tip == null) return;
            float dt = Time.unscaledDeltaTime;
            _tipLeft -= dt;
            if (_tipLeft <= 0f || _tipKey == null || world.SlotItem(_tipKey.Slot) < 0)
            {
                CloseTip();
                return;
            }
            float s = _tip.localScale.x;
            s = Mathf.MoveTowards(s, 1f, dt * 4f);
            _tip.localScale = new Vector3(s, s, 1f);
            if (_tipCd != null)
            {
                int slot = _tipKey.Slot;
                if (world.SlotSpent(slot)) _tipCd.text = "本局次数已用完";
                else
                {
                    float charge = world.SlotCharge(slot);
                    _tipCd.text = charge >= 1f ? "已就绪，等时机" : $"冷却 {Mathf.RoundToInt(charge * 100f)}%";
                }
            }
        }

        void RefreshKeys(BattleWorld world, bool inBattle)
        {
            float dt = Time.unscaledDeltaTime;
            PlayCasts(world);
            TickTip(world);
            int shown = 0;
            for (int i = 0; i < Keys.Length; i++)
            {
                ItemKey k = Keys[i];
                int id = world.SlotItem(k.Slot);
                bool has = id >= 0;
                if (k.Root.gameObject.activeSelf != has) k.Root.gameObject.SetActive(has);
                if (!has) continue;
                k.Root.anchoredPosition = new Vector2(-KeySide, _keyY0 + shown++ * (KeyH + KeyGap));
                ItemDef d = ItemCatalog.Get(id);
                Color q = ItemCatalog.QualityColor(d.Quality);
                if (k.Shown != id)
                {
                    k.Shown = id;
                    Sprite icon = InkSprites.Ui(d.Id);
                    k.Dim.sprite = icon;
                    k.Art.sprite = icon;
                }
                float charge = world.SlotCharge(k.Slot);
                bool off = world.SlotSpent(k.Slot);
                bool ready = !off && charge >= 1f;

                // 底下一层始终是淡影。彩色只从下沿露出冷却走完的那一截，灌满才是整只实心图标。
                k.Dim.color = new Color(0.55f, 0.55f, 0.55f, off ? 0.22f : 0.34f);
                k.Art.color = Color.white;
                k.Fill.sizeDelta = new Vector2(KeyW, off ? 0f : KeyH * Mathf.Clamp01(charge));

                float fire = world.ItemFlash[k.Slot];
                float pop = world.ItemReadyFlash[k.Slot];
                world.ItemFlash[k.Slot] = Mathf.Max(0f, fire - dt * 3f);
                world.ItemReadyFlash[k.Slot] = Mathf.Max(0f, pop - dt * 2.5f);
                if (fire > k.SeenFlash + 0.2f) k.Cast = 1f;
                k.SeenFlash = fire;
                k.Cast = Mathf.Max(0f, k.Cast - dt / 0.48f);

                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f);
                float glow = ready ? 0.72f + 0.28f * pulse : 0f;
                glow = Mathf.Max(glow, pop, k.Cast * 0.85f);
                k.Glow.color = new Color(q.r, q.g, q.b, glow);
                k.Glow.enabled = glow > 0.02f;
                k.Glow.transform.localScale = Vector3.one * (ready ? 1.08f + 0.16f * pulse : 1f);

                float c = k.Cast;
                float kick = Mathf.Sin(c * Mathf.PI);
                float sc = 1f + 0.32f * kick + (ready && c <= 0f ? 0.05f * pulse : 0f);
                k.Visual.anchoredPosition = new Vector2(0f, kick * 28f);
                k.Visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((1f - c) * Mathf.PI * 2f) * 18f * c);
                k.Visual.localScale = new Vector3(sc + kick * 0.1f, sc - kick * 0.06f, 1f);
                k.Ring.enabled = c > 0.02f;
                if (c > 0.02f)
                {
                    k.Ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 2.3f, 1f - c);
                    k.Ring.color = new Color(q.r, q.g, q.b, c * 0.95f);
                }
            }
        }
    }
}
