using UnityEngine;

namespace InkLine
{
    // 奖励飞进顶栏：图标带「+N」从卡上弹出来，画一道弧落到顶栏格子，落地冒火花、格子抖一下。
    // 新手礼包和新手奖励共用。
    public static class RewardFly
    {
        public static RectTransform Flyer(RectTransform layer, string icon, int n, Vector2 pos)
        {
            var g = ResultKit.Group(layer, "fly_" + icon, pos, new Vector2(140f, 150f));
            var glow = UiKit.Icon(g, InkFx.SoftDisc(), new Vector2(0f, 18f), 160f);
            glow.color = new Color(1f, 0.9f, 0.7f, 0.55f);
            UiKit.Icon(g, InkSprites.Ui(icon), new Vector2(0f, 18f), 104f);
            var t = UiKit.Label(g, "n", "+" + n, 34, new Vector2(0f, -52f), new Vector2(140f, 42f));
            t.color = InkTheme.Seal;
            t.raycastTarget = false;
            UiKit.Bold(t);
            g.SetAsLastSibling();
            g.localScale = Vector3.zero;
            return g;
        }

        public static void Fly(Component host, RectTransform flyer, Vector2 from, Vector2 to)
        {
            UiAnim.On(host).Tween(0f, 0.55f, k =>
            {
                if (flyer == null) return;
                float e = Ease.OutCubic(k);
                Vector2 p = Vector2.Lerp(from, to, e);
                p.y += Mathf.Sin(e * Mathf.PI) * 90f;
                flyer.anchoredPosition = p;
                float s = Mathf.Lerp(1.15f, 0.45f, e);
                flyer.localScale = new Vector3(s, s, 1f);
            });
        }

        public static void Land(RectTransform layer, RectTransform flyer, RectTransform chip, Color spark)
        {
            Vector2 at = flyer != null ? flyer.anchoredPosition : Vector2.zero;
            if (flyer != null) Object.Destroy(flyer.gameObject);
            AudioBus.Pickup();
            UiConfetti.Sparks(layer, at, spark, 10, 420f);
            if (chip == null) return;
            UiAnim.On(chip).Punch(chip, 0f, 0.18f, 0.32f);
        }

        public static RectTransform Chip(RectTransform layer, string a, string b)
        {
            Transform home = layer.Find("Home");
            Transform t = home != null ? home.Find(a) : null;
            if (t == null && home != null) t = home.Find(b);
            if (t == null) t = layer.Find(a);
            if (t == null) t = layer.Find(b);
            return t as RectTransform;
        }

        public static Vector2 Local(RectTransform layer, Transform target)
        {
            var rt = target as RectTransform;
            Vector3 world = rt != null ? rt.TransformPoint(rt.rect.center) : target.position;
            return layer.InverseTransformPoint(world);
        }
    }
}
