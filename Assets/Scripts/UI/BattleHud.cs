using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class BattleHud
    {
        // 一个技能键：底板 + 径向充能 + 名字 + 耗量。
        public sealed class SpellKey
        {
            public RectTransform Root;
            public Button Btn;
            public Image Fill;
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

        BattleHud(RectTransform layer, Text gold, RectTransform goldChip, Image[] hearts,
            RectTransform heartsWrap, Text wave, Text toast, Text ink, RectTransform inkChip,
            Button draft, Button retreat, Text draftLabel, SpellKey[] keys)
        {
            _layer = layer;
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

            // 底栏一行排开：撤退 | 改装 | 技能。技能不再往上叠 ——
            // 叠上去正好盖住炮，16:9 上躲不开。
            var draftBtn = UiKit.Btn(layer, "draft", "改装", Vector2.zero, new Vector2(280, 84), draft, true, Pin.Bottom);
            var draftLabel = draftBtn.GetComponentInChildren<Text>();
            var retreatBtn = UiKit.Btn(layer, "back", "撤退", Vector2.zero, new Vector2(132, 76), retreat, false, Pin.BottomLeft);

            var keys = new SpellKey[GameConstants.SpellSlots];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = MakeKey(layer, i, Vector2.zero, cast);

            int maxHp = world != null ? world.MaxBaseHp : GameConstants.BaseHp;
            var hearts = UiKit.Hearts(layer, Vector2.zero, 36f, maxHp, Pin.Center);
            var heartsWrap = hearts.Length > 0 ? hearts[0].transform.parent as RectTransform : null;

            var hud = new BattleHud(layer, gold, goldChip, hearts, heartsWrap, wave, toast,
                ink, inkChip, draftBtn, retreatBtn, draftLabel, keys);
            hud.Layout();
            return hud;
        }

        static SpellKey MakeKey(RectTransform layer, int slot, Vector2 pos, System.Action<int> cast)
        {
            var root = UiKit.Stroke(layer, "key" + slot, pos, new Vector2(84, 84), Pin.BottomRight, 5f, radius: 26f);
            // 充能盘内缩 6px，正好落在描边环里侧，不会盖住边
            var fill = UiKit.RadialFill(root, Vector2.zero, 72f, InkTheme.Violet);
            var name = UiKit.Label(root, "n", "", 30, new Vector2(0f, 9f), new Vector2(84, 40));
            UiKit.Bold(name);
            var cost = UiKit.Label(root, "c", "", 18, new Vector2(0f, -26f), new Vector2(84, 24));
            cost.color = InkTheme.TextMid;
            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = root.GetComponent<Image>();
            int idx = slot;
            btn.onClick.AddListener(() => cast(idx));
            return new SpellKey { Root = root, Btn = btn, Fill = fill, Name = name, Cost = cost, Slot = slot };
        }

        const float RowH = 84f;
        const float KeyS = 84f;
        const float RetreatW = 132f;
        const float Side = 16f;
        const float Gap = 10f;

        public void Layout()
        {
            if (_layer == null) return;
            float bot = ScreenFit.BottomPad + 16f;
            float canvasW = Mathf.Max(720f, _layer.rect.width);
            int slots = GameConstants.SpellSlots;
            float keysW = slots * KeyS + Mathf.Max(0, slots - 1) * 8f;
            float half = canvasW * 0.5f;
            float left = -half + Side + RetreatW + Gap;
            float right = half - Side - keysW - Gap;
            float draftW = Mathf.Clamp(right - left, 200f, 340f);
            float draftX = (left + right) * 0.5f;

            PinBottom(Retreat.transform as RectTransform, new Vector2(Side, bot),
                new Vector2(RetreatW, 76f), 8f, Pin.BottomLeft);
            PinBottom(Draft.transform as RectTransform, new Vector2(draftX, bot),
                new Vector2(draftW, RowH), 8f, Pin.Bottom);
            if (DraftLabel != null)
            {
                var box = DraftLabel.GetComponent<RectTransform>();
                if (box != null) box.sizeDelta = new Vector2(draftW, RowH);
            }
            for (int i = 0; i < Keys.Length; i++)
                PinBottom(Keys[i].Root, new Vector2(Side + i * (KeyS + 8f), bot),
                    new Vector2(KeyS, KeyS), 7f, Pin.BottomRight);

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
                k.Cost.text = d.InkCost.ToString();
                float need = Mathf.Max(1, d.InkCost);
                k.Fill.fillAmount = Mathf.Clamp01(world.Ink / need);
                bool ready = inBattle && world.CanCast(k.Slot);
                k.Btn.interactable = ready;
                Color tint = d.Tint;
                k.Fill.color = ready
                    ? new Color(tint.r, tint.g, tint.b, 0.55f)
                    : new Color(tint.r, tint.g, tint.b, 0.22f);
                k.Name.color = ready ? InkTheme.TextDark : InkTheme.TextDim;
                k.Root.GetComponent<Image>().color = ready ? InkTheme.CardFace : InkTheme.CardDim;
                k.Root.localScale = ready
                    ? Vector3.one * (1f + 0.03f * Mathf.Sin(Time.unscaledTime * 7f))
                    : Vector3.one;
            }
        }
    }
}
