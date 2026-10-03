using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 进关先挂一条「战斗开始」：用结算页同一张红丝带，从上面落下来晃一下站稳，
    // 底下压一枚奶油色签条写关名，有关卡规则就多一行。停一拍后一起往上飘走。
    // 这段时间战斗不走，玩家先看清棋盘；金币够了的改装框要等它放完才弹。
    public static class BattleBanner
    {
        public const float Length = 1.7f;

        public static void Show(RectTransform layer, StageDef stage, Action done)
        {
            var root = UiKit.Panel(layer, "battle_banner", new Vector2(0f, 150f), new Vector2(10f, 10f), Color.clear);
            root.GetComponent<Image>().raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var ribbon = ResultKit.Banner(root, true, "战斗开始", new Vector2(0f, 40f), 540f);

            string hint = stage != null ? StageCatalog.RuleHint(stage.Rules) : "";
            bool rules = !string.IsNullOrEmpty(hint);
            float tagH = rules ? 92f : 58f;
            var tag = UiKit.Stroke(root, "tag", new Vector2(0f, -72f - (tagH - 58f) * 0.5f), new Vector2(420f, tagH),
                Pin.Center, 4f, fill: InkTheme.Paper, radius: 22f);
            var title = UiKit.Label(tag, "name", stage != null ? stage.Name : "", 26,
                new Vector2(0f, rules ? 16f : 0f), new Vector2(400f, 40f));
            UiKit.Bold(title);
            title.color = InkTheme.TextDark;
            if (rules)
            {
                var rule = UiKit.Label(tag, "rule", hint, 19, new Vector2(0f, -20f), new Vector2(400f, 30f));
                rule.color = InkTheme.Hex("8A5A2C");
            }
            Transform tagShadow = root.Find("tag_sh");

            var anim = UiAnim.On(root);
            anim.Move(ribbon, new Vector2(0f, 420f), new Vector2(0f, 40f), 0f, 0.42f, Ease.OutBounce)
                .Rotate(ribbon, -7f, 0f, 0.20f, 0.5f, Ease.OutBack)
                .At(0.16f, AudioBus.BattleStart)
                .Pop(tag, 0.34f, 0.32f, 0f)
                .Tween(1.2f, 0.42f, k =>
                {
                    if (root == null) return;
                    float e = Ease.InQuad(k);
                    root.anchoredPosition = new Vector2(0f, 150f + 70f * e);
                    group.alpha = 1f - e;
                })
                .At(Length, () =>
                {
                    if (root != null) UnityEngine.Object.Destroy(root.gameObject);
                    done?.Invoke();
                });
            if (tagShadow != null)
                anim.Pop(tagShadow, 0.34f, 0.32f, 0f);
        }
    }
}
