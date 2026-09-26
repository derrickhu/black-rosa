using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 屏幕中间的一句提示，一会儿就淡掉。同一时间只留一条。
    public sealed class InkToast : MonoBehaviour
    {
        const float Hold = 1.4f;
        const float Fade = 0.4f;

        static InkToast _cur;
        CanvasGroup _group;
        float _t;

        public static void Show(Transform layer, string text)
        {
            if (layer == null || string.IsNullOrEmpty(text)) return;
            if (_cur != null) Destroy(_cur.gameObject);
            float w = Mathf.Clamp(text.Length * 30f + 80f, 260f, 640f);
            var root = UiKit.Panel(layer, "toast", new Vector2(0f, 120f), new Vector2(w, 76f),
                new Color(0.10f, 0.08f, 0.06f, 0.86f));
            var img = root.GetComponent<Image>();
            img.sprite = UiSprites.Fill(20);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            var t = UiKit.Label(root, "t", text, 28, Vector2.zero, new Vector2(w - 40f, 60f));
            t.color = InkTheme.CardFace;
            _cur = root.gameObject.AddComponent<InkToast>();
            _cur._group = root.gameObject.AddComponent<CanvasGroup>();
            _cur._group.blocksRaycasts = false;
            _cur._group.interactable = false;
            root.SetAsLastSibling();
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_t > Hold) _group.alpha = Mathf.Clamp01(1f - (_t - Hold) / Fade);
            if (_t >= Hold + Fade) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_cur == this) _cur = null;
        }
    }
}
