using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class BattleHud
    {
        public readonly Text Gold;
        public readonly Image[] Hearts;
        public readonly Text Wave;
        public readonly Text Toast;
        public readonly Button Draft;
        public readonly Text DraftLabel;

        BattleHud(Text gold, Image[] hearts, Text wave, Text toast, Button draft, Text draftLabel)
        {
            Gold = gold;
            Hearts = hearts;
            Wave = wave;
            Toast = toast;
            Draft = draft;
            DraftLabel = draftLabel;
        }

        public static BattleHud Build(RectTransform layer, System.Action retreat, System.Action draft)
        {
            float top = ScreenFit.TopPad + 36f;
            float bot = ScreenFit.BottomPad + 18f;

            var goldWrap = UiKit.Panel(layer, "gold", new Vector2(20, top), new Vector2(168, 48), Color.clear, Pin.TopLeft);
            goldWrap.GetComponent<Image>().raycastTarget = false;
            UiKit.Icon(goldWrap, InkArt.Icon(InkShape.Coin, 48), new Vector2(-50f, 0f), 36f);
            var gold = InkLabel(goldWrap, "n", "0", 32, new Vector2(18f, 0f), new Vector2(88, 44), TextAnchor.MiddleLeft);

            var wave = InkLabel(layer, "wave", "", 30, new Vector2(0, top), new Vector2(240, 44), TextAnchor.MiddleCenter, Pin.Top);

            var toast = UiKit.Label(layer, "toast", "", 24, new Vector2(0, top + 50f), new Vector2(640, 44), TextAnchor.MiddleCenter, Pin.Top);

            var draftBtn = UiKit.Btn(layer, "draft", "改装", new Vector2(70, bot), new Vector2(360, 76), draft, true, Pin.Bottom);
            var draftLabel = draftBtn.GetComponentInChildren<Text>();
            UiKit.Btn(layer, "back", "撤退", new Vector2(22, bot), new Vector2(130, 68), retreat, false, Pin.BottomLeft);

            float heartSize = 36f;
            float wrapH = heartSize + 8f;
            float buttonTop = bot + 76f;
            float belowCannon = WorldToCanvasY(layer, GameConstants.EmitterY - 0.7f);
            float heartY = Mathf.Max(buttonTop + 10f, belowCannon - wrapH);
            var hearts = UiKit.Hearts(layer, new Vector2(0, heartY), heartSize, Pin.Bottom);

            return new BattleHud(gold, hearts, wave, toast, draftBtn, draftLabel);
        }

        static Text InkLabel(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box, TextAnchor anchor = TextAnchor.MiddleCenter, Pin pin = Pin.Center)
        {
            var t = UiKit.Label(parent, name, text, size, pos, box, anchor, pin);
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        static float WorldToCanvasY(RectTransform layer, float worldY)
        {
            Camera cam = Camera.main;
            if (cam == null || layer == null) return ScreenFit.BottomPad + 120f;
            float vy = cam.WorldToViewportPoint(new Vector3(0f, worldY, 0f)).y;
            return vy * layer.rect.height;
        }

        public void Refresh(BattleWorld world, bool inBattle, string tip)
        {
            if (world == null || Gold == null) return;
            Gold.text = world.Gold.ToString();
            int hp = Mathf.Clamp(world.BaseHp, 0, 3);
            for (int i = 0; i < Hearts.Length; i++)
                Hearts[i].color = i < hp ? Color.white : new Color(1f, 1f, 1f, 0.28f);
            Wave.text = world.BossSpawned ? "关底" : $"波 {world.WaveIndex + 1}/{world.Stage.Waves.Length}";
            if (world.ToastTime > 0f) Toast.text = world.Toast;
            else if (!string.IsNullOrEmpty(tip)) Toast.text = tip;
            else if (world.RevealTime > 0f) Toast.text = $"显形 · {world.LastReveal}";
            else Toast.text = "";
            bool can = world.CanDraft && inBattle;
            Draft.interactable = inBattle;
            DraftLabel.text = can ? $"改装  {world.DraftCost}" : $"差  {Mathf.Max(0, world.DraftCost - world.Gold)}";
            Draft.image.color = can ? InkTheme.Ink : InkTheme.CtaOff;
            Draft.transform.localScale = can
                ? Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 8f))
                : Vector3.one;
        }
    }
}
