using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 失守的那一小段：屏幕压暗 → 裂开的炮台砸下来、溅开几摊墨、往一边歪倒 →
    // 撕破的灰横幅晃着垂下来。约 1.8 秒，跟在后面的是失败页正文。
    public static class DefeatFx
    {
        public const float CannonY = 236f;
        public const float BannerY = 430f;

        // 返回演出结束的时刻，正文从这之后开始排。
        public static float Play(RectTransform root, RectTransform stage, UiAnim anim)
        {
            var dim = root.GetComponent<Image>();
            if (dim != null)
            {
                Color c = dim.color;
                anim.Tween(0f, 0.35f, k =>
                {
                    c.a = Mathf.Lerp(0f, 0.84f, Ease.OutCubic(k));
                    dim.color = c;
                });
            }

            // 墨迹先铺好、藏着，炮台落地那一下再炸开。
            Vector2[] splat = { new Vector2(-170f, -70f), new Vector2(160f, -84f), new Vector2(-40f, -128f), new Vector2(90f, 60f) };
            float[] size = { 150f, 170f, 120f, 96f };
            const float land = 0.72f;
            for (int i = 0; i < splat.Length; i++)
            {
                var s = UiKit.Icon(stage, InkSprites.Load("Ui/result_ink_splat"), new Vector2(0f, CannonY) + splat[i], size[i]);
                s.transform.localRotation = Quaternion.Euler(0f, 0f, i * 77f);
                s.color = new Color(1f, 1f, 1f, 0.92f);
                anim.Pop(s.transform, land + i * 0.04f, 0.26f, 0.2f);
            }

            var cannon = ResultKit.Group(stage, "cannon", new Vector2(0f, CannonY), new Vector2(290f, 258f));
            UiKit.Icon(cannon, InkSprites.Load("Ui/result_cannon_broken"), Vector2.zero, 290f);
            cannon.pivot = new Vector2(0.5f, 0.1f);
            anim.Move(cannon, new Vector2(0f, 900f), new Vector2(0f, CannonY - 110f), 0.3f, land - 0.3f, Ease.InQuad)
                .Punch(cannon, land, 0.22f, 0.36f)
                .Rotate(cannon, 0f, -17f, land + 0.3f, 0.45f, Ease.OutBounce)
                .Shake(stage, land, 18f, 0.36f)
                .At(land, () =>
                {
                    AudioBus.Crumble();
                    UiConfetti.Sparks(root, new Vector2(0f, CannonY - 110f), InkTheme.GraphiteHi, 16, 640f);
                });

            var banner = ResultKit.Banner(stage, false, "防线失守", new Vector2(0f, BannerY), 580f);
            banner.pivot = new Vector2(0.5f, 1f);
            float top = BannerY + banner.sizeDelta.y * 0.5f;
            anim.Move(banner, new Vector2(0f, 900f), new Vector2(0f, top), 1.05f, 0.36f, Ease.OutCubic)
                .Rotate(banner, -22f, 0f, 1.05f, 0.9f, SwingEase)
                .At(1.3f, AudioBus.Lose);
            return 1.8f;
        }

        // 悬挂物的摆：来回荡两下停住。
        static float SwingEase(float k) => 1f - Mathf.Cos(k * Mathf.PI * 3.2f) * Mathf.Pow(1f - k, 2.2f);
    }
}
