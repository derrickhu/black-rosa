using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 印章的样子：品质色章面 + 金边 + 类型纹样，三层叠出来。详情槽、背包、结算、开箱共用。
    public static class SealArt
    {
        public static Sprite Icon(SealType t) => InkSprites.Load(SealCatalog.IconKey(t));

        // 一枚镶好的印章。返回根节点，金印外面多一圈慢转的光。
        public static RectTransform Medal(Transform parent, string name, SealType t, SealQuality q, Vector2 pos, float size)
        {
            var root = Holder(parent, name, pos, size);
            if (q == SealQuality.Gold)
            {
                var rays = UiKit.Icon(root, InkSprites.Load("Ui/result_rays"), Vector2.zero, size * 1.9f);
                rays.color = new Color(1f, 0.8f, 0.35f, 0.75f);
                UiAnim.On(rays).Spin(rays.transform, 30f);
            }
            var face = UiKit.Icon(root, InkSprites.Load("Ui/seal_face"), Vector2.zero, size);
            face.color = SealCatalog.QualityColor(q);
            UiKit.Icon(root, InkSprites.Load("Ui/seal_rim"), Vector2.zero, size);
            UiKit.Icon(root, Icon(t), Vector2.zero, size * 0.5f);
            return root;
        }

        // 刷出了类型、还空着的槽：一枚金币，中间压着淡淡的类型纹样。
        public static RectTransform Empty(Transform parent, string name, SealType t, Vector2 pos, float size)
        {
            var root = Holder(parent, name, pos, size);
            UiKit.Icon(root, InkSprites.Load("Ui/seal_empty"), Vector2.zero, size);
            var mark = UiKit.Icon(root, Icon(t), Vector2.zero, size * 0.46f);
            mark.color = new Color(0.62f, 0.4f, 0.1f, 0.85f);
            return root;
        }

        // 还没到等级的槽：一颗灰石钉。
        public static RectTransform Stud(Transform parent, string name, Vector2 pos, float size)
        {
            var root = Holder(parent, name, pos, size);
            UiKit.Icon(root, InkSprites.Load("Ui/seal_stud"), Vector2.zero, size);
            return root;
        }

        // 结算、开箱用的一张小卡：印章 + 「烈度 · 绿」。
        public static RectTransform Card(Transform parent, string name, SealDrop d, Vector2 pos, Vector2 size)
        {
            var card = UiKit.Stroke(parent, name, pos, size, Pin.Center, 5f, SealCatalog.QualityDeep(d.Quality),
                InkTheme.CardFace, 26f);
            card.GetComponent<Image>().raycastTarget = false;
            Medal(card, "m", d.Type, d.Quality, new Vector2(0f, size.y * 0.14f), Mathf.Min(size.x, size.y) * 0.52f);
            var t = UiKit.Label(card, "n", SealCatalog.DropName(d), 22, new Vector2(0f, -size.y * 0.5f + 26f),
                new Vector2(size.x, 30f));
            t.color = SealCatalog.QualityDeep(d.Quality);
            UiKit.Bold(t);
            return card;
        }

        static RectTransform Holder(Transform parent, string name, Vector2 pos, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }
    }
}
