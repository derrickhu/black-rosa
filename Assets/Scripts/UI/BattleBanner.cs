using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 进关先挂一条「战斗开始」：用结算页同一张红丝带，从上面落下来晃一下站稳。
    // 停一拍后往上飘走。这段时间战斗不走，玩家先看清棋盘；金币够了的改装框要等它放完才弹。
    public static class BattleBanner
    {
        public const float Length = 1.7f;

        public static void Show(RectTransform layer, Action done)
        {
            var root = UiKit.Panel(layer, "battle_banner", new Vector2(0f, 150f), new Vector2(10f, 10f), Color.clear);
            root.GetComponent<Image>().raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var ribbon = ResultKit.Banner(root, true, "战斗开始", Vector2.zero, 540f);

            var anim = UiAnim.On(root);
            anim.Move(ribbon, new Vector2(0f, 420f), Vector2.zero, 0f, 0.42f, Ease.OutBounce)
                .Rotate(ribbon, -7f, 0f, 0.20f, 0.5f, Ease.OutBack)
                .At(0.16f, AudioBus.BattleStart)
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
        }
    }
}
