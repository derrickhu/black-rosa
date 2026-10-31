using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 印章的样子：奶油描边的金边珐琅章，按品质换釉色。中间压一枚立体小饰物，饰物按类型各画一枚。
    // 详情槽、背包、结算、开箱共用。
    public static class SealArt
    {
        public static Sprite Icon(SealType t) => InkSprites.Load(SealCatalog.IconKey(t));

        // 饰物占整章的比例。章外还有一圈奶油边，饰物要落在釉面里面，又要够大才看得清。
        const float IconShare = 0.56f;

        // 一枚镶好的印章。金印后面多一圈慢转的光。
        public static RectTransform Medal(Transform parent, string name, SealType t, SealQuality q, Vector2 pos, float size)
        {
            var root = Holder(parent, name, pos, size);
            if (q == SealQuality.Gold)
            {
                var rays = UiKit.Icon(root, InkSprites.Load("Ui/seal_burst"), Vector2.zero, size * 1.9f);
                rays.color = Color.white;
                UiAnim.On(rays).Spin(rays.transform, 22f);
            }
            UiKit.Icon(root, InkSprites.Load(FaceKey(q)), Vector2.zero, size);
            UiKit.Icon(root, Icon(t), Vector2.zero, size * IconShare);
            return root;
        }

        // 刷出了类型、还空着：一圈金边围着凹下去的深棕槽，纹样淡淡地刻在里面。
        public static RectTransform Empty(Transform parent, string name, SealType t, Vector2 pos, float size)
        {
            var root = Holder(parent, name, pos, size);
            UiKit.Icon(root, InkSprites.Load("Ui/seal_empty"), Vector2.zero, size);
            var mark = UiKit.Icon(root, Icon(t), Vector2.zero, size * IconShare);
            mark.color = new Color(1f, 0.92f, 0.78f, 0.42f);
            return root;
        }

        static string FaceKey(SealQuality q)
        {
            switch (q)
            {
                case SealQuality.Blue: return "Ui/seal_q_blue";
                case SealQuality.Purple: return "Ui/seal_q_purple";
                case SealQuality.Gold: return "Ui/seal_q_gold";
                default: return "Ui/seal_q_green";
            }
        }

        // 还不知道会开出哪一枚：灰铁章，中间一个问号。
        public static RectTransform Unknown(Transform parent, string name, Vector2 pos, float size)
        {
            var root = Holder(parent, name, pos, size);
            UiKit.Icon(root, InkSprites.Load("Ui/seal_stud"), Vector2.zero, size);
            // 全角问号的笔画靠在字框左边，放进圆章会偏。半角问号的笔画在字框正中。
            int px = Mathf.RoundToInt(size * 0.42f);
            var q = UiKit.Label(root, "q", "?", px, new Vector2(0f, px * 0.06f), new Vector2(size, size));
            q.color = new Color(0.45f, 0.43f, 0.40f, 1f);
            UiKit.Bold(q);
            return root;
        }

        // 还没到等级的槽：灰铁边的锁槽，中间一把小锁。
        public static RectTransform Stud(Transform parent, string name, Vector2 pos, float size)
        {
            var root = Holder(parent, name, pos, size);
            UiKit.Icon(root, InkSprites.Load("Ui/seal_stud"), Vector2.zero, size);
            var lockIcon = UiKit.Icon(root, InkSprites.Load("Ui/seal_ico_lock"), Vector2.zero, size * 0.44f);
            lockIcon.color = new Color(0.62f, 0.60f, 0.58f, 1f);
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
