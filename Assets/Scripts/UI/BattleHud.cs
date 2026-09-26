using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class BattleHud
    {
        // 一个技能键：圆角牌 + 名字 + 金币花费 + 一条够不够放的细槽。
        public sealed class SpellKey
        {
            public RectTransform Root;
            public Button Btn;
            public Image Fill;
            public Image Mark;
            public Text Name;
            public Text Cost;
            public int Slot;
        }

        public readonly Text Gold;
        public readonly RectTransform GoldChip;
        public readonly Image[] Hearts;
        public readonly Text Wave;
        public readonly Text Toast;
        public readonly Text Ink;
        public readonly RectTransform InkChip;
        public readonly Button Draft;
        public readonly Button Retreat;
        public readonly Text DraftLabel;
        public readonly SpellKey[] Keys;
        readonly RectTransform _layer;
        readonly RectTransform _heartsWrap;
        readonly RectTransform _wavePlate;

        BattleHud(RectTransform layer, Text gold, RectTransform goldChip, Image[] hearts,
            RectTransform heartsWrap, RectTransform wavePlate, Text wave, Text toast, Text ink, RectTransform inkChip,
            Button draft, Button retreat, Text draftLabel, SpellKey[] keys)
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
            Retreat = retreat;
            DraftLabel = draftLabel;
            Keys = keys;
        }

        public static BattleHud Build(RectTransform layer, BattleWorld world,
            System.Action retreat, System.Action draft, System.Action<int> cast)
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

            // 撤退在左上角。底栏只留改装和技能，不再把撤退挤在改装左边。
            var draftBtn = UiKit.Btn(layer, "draft", "改装", Vector2.zero, new Vector2(280, 84), draft, true, Pin.Bottom);
            var draftLabel = draftBtn.GetComponentInChildren<Text>();
            var retreatBtn = UiKit.Btn(layer, "back", "撤退", Vector2.zero, new Vector2(RetreatW, RetreatH), retreat, false, Pin.TopLeft);

            var keys = new SpellKey[GameConstants.SpellSlots];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = MakeKey(layer, i, Vector2.zero, cast);

            int maxHp = world != null ? world.MaxBaseHp : GameConstants.BaseHp;
            var hearts = UiKit.Hearts(layer, Vector2.zero, 36f, maxHp, Pin.Center);
            var heartsWrap = hearts.Length > 0 ? hearts[0].transform.parent as RectTransform : null;

            var hud = new BattleHud(layer, gold, goldChip, hearts, heartsWrap, wavePlate, wave, toast,
                ink, inkChip, draftBtn, retreatBtn, draftLabel, keys);
            hud.Layout();
            return hud;
        }

        static SpellKey MakeKey(RectTransform layer, int slot, Vector2 pos, System.Action<int> cast)
        {
            var root = UiKit.Stroke(layer, "key" + slot, pos, new Vector2(KeyW, KeyH), Pin.BottomRight, 4f, radius: 16f);
            var name = UiKit.Label(root, "n", "", 26, new Vector2(0f, 16f), new Vector2(KeyW - 12f, 36f));
            UiKit.Bold(name);
            var mark = UiKit.Icon(root, InkSprites.Ui("gold"), new Vector2(-18f, -10f), 24f);
            var cost = UiKit.Label(root, "c", "", 20, new Vector2(16f, -10f), new Vector2(52f, 28f), TextAnchor.MiddleLeft);
            UiKit.Bold(cost);

            // 细槽贴在牌的下沿。底是淡金，实心按已有金币 / 花费从左往右填。
            var track = new GameObject("track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(root, false);
            var tr = track.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
            tr.pivot = new Vector2(0.5f, 0f);
            tr.anchoredPosition = new Vector2(0f, 8f);
            tr.sizeDelta = new Vector2(KeyW - 28f, 8f);
            var trackImg = track.GetComponent<Image>();
            trackImg.sprite = UiSprites.Fill(4);
            trackImg.type = Image.Type.Sliced;
            trackImg.color = InkTheme.GoldHi;
            trackImg.raycastTarget = false;

            var bar = new GameObject("bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(track.transform, false);
            var br = bar.GetComponent<RectTransform>();
            br.anchorMin = Vector2.zero;
            br.anchorMax = Vector2.one;
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;
            var fill = bar.GetComponent<Image>();
            fill.sprite = UiSprites.Fill(4);
            fill.type = Image.Type.Sliced;
            fill.color = InkTheme.CoinFace;
            fill.raycastTarget = false;

            var btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = root.GetComponent<Image>();
            int idx = slot;
            btn.onClick.AddListener(() => cast(idx));
            return new SpellKey
            {
                Root = root, Btn = btn, Fill = fill, Mark = mark, Name = name, Cost = cost, Slot = slot
            };
        }

        const float RowH = 84f;
        const float KeyW = 120f;
        const float KeyH = 84f;
        const float RetreatW = 124f;
        const float RetreatH = 52f;
        const float Side = 16f;
        const float Gap = 10f;

        public void Layout()
        {
            if (_layer == null) return;
            float bot = ScreenFit.BottomPad + 16f;
            float pad = ScreenFit.TopPad;
            float top = pad <= 36.1f ? 10f : pad;
            float row = top + RetreatH + 8f;
            float canvasW = Mathf.Max(720f, _layer.rect.width);
            int slots = GameConstants.SpellSlots;
            float keysW = slots * KeyW + Mathf.Max(0, slots - 1) * 8f;
            float half = canvasW * 0.5f;
            float left = -half + Side;
            float right = half - Side - keysW - Gap;
            float draftW = Mathf.Clamp(right - left, 200f, 520f);
            float draftX = (left + right) * 0.5f;

            PinTop(Retreat.transform as RectTransform, new Vector2(Side, top),
                new Vector2(RetreatW, RetreatH), 6f, Pin.TopLeft);
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
            for (int i = 0; i < Keys.Length; i++)
                PinBottom(Keys[i].Root, new Vector2(Side + i * (KeyW + 8f), bot),
                    new Vector2(KeyW, KeyH), 7f, Pin.BottomRight);

            if (_heartsWrap != null)
            {
                // 格子下沿居中。Pin.Center，世界坐标转画布中心偏移。
                _heartsWrap.anchorMin = _heartsWrap.anchorMax = _heartsWrap.pivot = new Vector2(0.5f, 0.5f);
                _heartsWrap.anchoredPosition = WorldToCanvas(_layer, new Vector3(0f, FieldLayout.GridBottom - 0.20f, 0f));
            }

            FitCamera(bot + RowH, Mathf.Max(1f, _layer.rect.height));
        }

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

        static Vector2 WorldToCanvas(RectTransform layer, Vector3 world)
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
            Wave.text = world.BossSpawned ? "关底" : $"波 {world.WaveIndex + 1}/{world.Stage.Waves.Length}";
            Ink.text = world.Ink.ToString();
            if (world.ToastTime > 0f) Toast.text = world.Toast;
            else if (!string.IsNullOrEmpty(tip)) Toast.text = tip;
            else if (world.RevealTime > 0f) Toast.text = $"显形 · {world.LastReveal}";
            else Toast.text = "";
            RefreshKeys(world, inBattle);
            bool can = world.CanDraft && inBattle;
            Draft.interactable = inBattle;
            DraftLabel.text = can ? $"改装  {world.DraftCost}" : $"差  {Mathf.Max(0, world.DraftCost - world.Gold)}";
            if (can) UiKit.PaintBtn(Draft, InkTheme.Cta, InkTheme.CtaDeep, InkTheme.CardFace);
            else UiKit.PaintBtn(Draft, InkTheme.CardDim, InkTheme.LineDim, InkTheme.TextDim);
            Draft.transform.localScale = can
                ? Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 8f))
                : Vector3.one;
        }

        // 收到一笔就把药丸顶一下、数字染一下色。飞过去的金币要在这儿落地有声，
        // 否则那段飞行只是装饰，玩家仍然不知道自己赚到了。
        static void Pop(RectTransform chip, Text value, float pulse, Color flash)
        {
            if (chip == null) return;
            chip.localScale = Vector3.one * (1f + 0.18f * pulse);
            if (value != null) value.color = Color.Lerp(InkTheme.TextDark, flash, pulse);
        }

        void RefreshKeys(BattleWorld world, bool inBattle)
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                SpellKey k = Keys[i];
                int id = world.SlotSpell(k.Slot);
                bool has = id >= 0;
                if (k.Root.gameObject.activeSelf != has) k.Root.gameObject.SetActive(has);
                Transform sh = k.Root.parent != null ? k.Root.parent.Find(k.Root.name + "_sh") : null;
                if (sh != null && sh.gameObject.activeSelf != has) sh.gameObject.SetActive(has);
                if (!has) continue;
                SpellDef d = SpellCatalog.Get(id);
                k.Name.text = d.Name;
                k.Cost.text = d.GoldCost.ToString();
                float need = Mathf.Max(1, d.GoldCost);
                float got = Mathf.Clamp01(world.Gold / need);
                var meter = k.Fill.rectTransform;
                meter.anchorMin = Vector2.zero;
                meter.anchorMax = new Vector2(got, 1f);
                meter.offsetMin = Vector2.zero;
                meter.offsetMax = Vector2.zero;
                k.Fill.enabled = got > 0.03f;
                bool ready = inBattle && world.CanCast(k.Slot);
                k.Btn.interactable = ready;
                k.Name.color = ready ? InkTheme.TextDark : InkTheme.TextDim;
                k.Cost.color = ready ? InkTheme.TextDark : InkTheme.TextDim;
                k.Mark.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                k.Fill.color = ready ? InkTheme.CoinFace : InkTheme.Gold;
                k.Root.GetComponent<Image>().color = ready ? InkTheme.CardFace : InkTheme.CardDim;
                k.Root.localScale = Vector3.one;
            }
        }
    }
}
