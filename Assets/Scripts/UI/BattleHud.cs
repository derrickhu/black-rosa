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
        public readonly Text DraftLabel;
        public readonly SpellKey[] Keys;

        BattleHud(Text gold, RectTransform goldChip, Image[] hearts, Text wave, Text toast,
            Text ink, RectTransform inkChip, Button draft, Text draftLabel, SpellKey[] keys)
        {
            Gold = gold;
            GoldChip = goldChip;
            Hearts = hearts;
            Wave = wave;
            Toast = toast;
            Ink = ink;
            InkChip = inkChip;
            Draft = draft;
            DraftLabel = draftLabel;
            Keys = keys;
        }

        public static BattleHud Build(RectTransform layer, BattleWorld world,
            System.Action retreat, System.Action draft, System.Action<int> cast)
        {
            // 只加 8 不是 30：TopPad 里已经含了「让到微信胶囊下沿再留 12」那一段，
            // 再加 30 就把整排药丸推进战场里去了 —— 右上那个墨药丸本来就压在
            // 走怪区上沿，而顶栏每往下一格，玩家能提前看见敌人的时间就少一点。
            float top = ScreenFit.TopPad + 8f;
            float bot = ScreenFit.BottomPad + 18f;

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

            var draftBtn = UiKit.Btn(layer, "draft", "改装", new Vector2(70, bot), new Vector2(360, 84), draft, true, Pin.Bottom);
            var draftLabel = draftBtn.GetComponentInChildren<Text>();
            UiKit.Btn(layer, "back", "撤退", new Vector2(22, bot), new Vector2(140, 76), retreat, false, Pin.BottomLeft);

            // 技能键竖排在右下角：改装行顶到 bot+84，心形居中，这一块是空的。
            var keys = new SpellKey[GameConstants.SpellSlots];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = MakeKey(layer, i, new Vector2(20f, bot + 110f + i * 108f), cast);

            float heartSize = 36f;
            float wrapH = heartSize + 8f;
            float buttonTop = bot + 84f;
            float belowCannon = WorldToCanvasY(layer, GameConstants.EmitterY - 0.7f);
            float heartY = Mathf.Max(buttonTop + 10f, belowCannon - wrapH);
            int maxHp = world != null ? world.MaxBaseHp : GameConstants.BaseHp;
            var hearts = UiKit.Hearts(layer, new Vector2(-70f, heartY), heartSize, maxHp, Pin.Bottom);

            return new BattleHud(gold, goldChip, hearts, wave, toast, ink, inkChip, draftBtn, draftLabel, keys);
        }

        static SpellKey MakeKey(RectTransform layer, int slot, Vector2 pos, System.Action<int> cast)
        {
            var root = UiKit.Stroke(layer, "key" + slot, pos, new Vector2(96, 96), Pin.BottomRight, 5f, radius: 26f);
            // 充能盘内缩 6px，正好落在描边环里侧，不会盖住边
            var fill = UiKit.RadialFill(root, Vector2.zero, 84f, InkTheme.Violet);
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

        static float WorldToCanvasY(RectTransform layer, float worldY)
        {
            Camera cam = Camera.main;
            if (cam == null || layer == null) return ScreenFit.BottomPad + 120f;
            float vy = cam.WorldToViewportPoint(new Vector3(0f, worldY, 0f)).y;
            return vy * layer.rect.height;
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
