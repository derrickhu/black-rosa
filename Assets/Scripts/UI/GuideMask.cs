using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 新手指引的遮罩：四块暗片围出一个镂空，镂空外的点击全被暗片吃掉，镂空里不放图形，点击落到下面。
    // GameFlow 判断「点在 UI 上」时会把暗片算进去，所以格子和滑轨在镂空外也点不动。
    // 镂空可以跟着一个界面元素走（抽屉滑入、按钮弹出都跟得上），也可以框住一块世界坐标。
    public sealed class GuideMask : MonoBehaviour
    {
        static readonly Color Shade = new Color(0.08f, 0.05f, 0.03f, 0.62f);
        const float HandH = 150f;
        const float BubbleW = 400f;
        const float BubbleH = 248f;

        RectTransform _layer;
        readonly Image[] _shade = new Image[4];
        RectTransform _hand;
        RectTransform _bubble;
        RectTransform _bubbleArt;
        Text _say;

        RectTransform _target;
        RectTransform _avoid;
        RectTransform _finger;
        Button _catcher;
        System.Action _onTap;
        float _pad;
        Rect _hole;
        bool _open;
        bool _clear;

        enum Hint { None, Tap, Swipe }
        Hint _hint;
        Vector2 _from;
        Vector2 _to;
        float _age;

        public static GuideMask Show(RectTransform layer)
        {
            var go = new GameObject("guide", typeof(RectTransform));
            go.transform.SetParent(layer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var m = go.AddComponent<GuideMask>();
            m._layer = layer;
            m.Build(rt);
            m.Block(false);
            return m;
        }

        void Build(RectTransform root)
        {
            for (int i = 0; i < 4; i++)
            {
                var p = UiKit.Panel(root, "shade" + i, Vector2.zero, Vector2.zero, Shade);
                _shade[i] = p.GetComponent<Image>();
                _shade[i].raycastTarget = true;
            }

            _bubble = UiKit.Panel(root, "bubble", Vector2.zero, new Vector2(BubbleW, BubbleH), Color.clear);
            _bubble.GetComponent<Image>().raycastTarget = false;
            var art = UiKit.Icon(_bubble, InkSprites.Load("Ui/guide_bubble"), Vector2.zero, BubbleW);
            _bubbleArt = art.rectTransform;
            _bubbleArt.sizeDelta = new Vector2(BubbleW, BubbleH);
            // 气泡尾巴占下沿约 18%，字摆在上面那块方框里。
            _say = UiKit.Label(_bubble, "say", "", 30, Vector2.zero, new Vector2(BubbleW - 64f, BubbleH * 0.56f));
            _say.horizontalOverflow = HorizontalWrapMode.Wrap;
            _say.raycastTarget = false;
            _say.color = InkTheme.TextDark;
            UiKit.Bold(_say);
            _bubble.gameObject.SetActive(false);

            var hand = UiKit.Icon(root, InkSprites.Load("Ui/guide_hand"), Vector2.zero, HandH);
            _hand = hand.rectTransform;
            _hand.sizeDelta = new Vector2(HandH * 0.6f, HandH);
            // 支点放在指尖，摆位置就是摆指尖。
            _hand.pivot = new Vector2(0.2f, 0.97f);
            _hand.gameObject.SetActive(false);
        }

        // 整屏挡住，不留镂空。dim=false 时是透明的，只吃点击不压暗，给结算页演出用。
        public void Block(bool dim = true)
        {
            _target = null;
            _avoid = null;
            ClearNarration();
            _open = false;
            _clear = !dim;
            _hint = Hint.None;
            _hand.gameObject.SetActive(false);
            _bubble.gameObject.SetActive(false);
            Paint();
        }

        public void HoleOn(RectTransform target, float pad = 14f)
        {
            _target = target;
            _avoid = null;
            ClearNarration();
            _pad = pad;
            _open = true;
            _clear = false;
            Track();
        }

        public void HoleWorld(Vector3 min, Vector3 max, float pad = 10f)
        {
            _target = null;
            _avoid = null;
            ClearNarration();
            _open = true;
            _clear = false;
            Vector2 a = BattleHud.WorldToCanvas(_layer, min);
            Vector2 b = BattleHud.WorldToCanvas(_layer, max);
            _hole = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - pad, Mathf.Min(a.y, b.y) - pad,
                Mathf.Max(a.x, b.x) + pad, Mathf.Max(a.y, b.y) + pad);
            Paint();
        }

        public Rect HoleRect => _hole;

        public void Tap(Vector2 at)
        {
            _hint = Hint.Tap;
            _followHole = false;
            _from = at;
            _age = 0f;
            _hand.gameObject.SetActive(true);
        }

        // 跟着镂空中心点按，目标在动也对得上。
        public void TapHole()
        {
            Tap(_hole.center);
            _followHole = true;
        }

        // 镂空框住一整块时，手指单独指到里面的某一格。
        public void FingerOn(RectTransform target)
        {
            _finger = target;
            _hint = Hint.Tap;
            _followHole = false;
            _age = 0f;
            _hand.gameObject.SetActive(target != null);
            PlaceFinger();
        }

        // 这次只讲解，点屏幕任意处继续。镂空里的按钮点不到。
        public void WhenTapped(System.Action next)
        {
            _onTap = next;
            if (_catcher == null)
            {
                var go = new GameObject("catch", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0f);
                img.raycastTarget = true;
                _catcher = go.GetComponent<Button>();
                _catcher.transition = Selectable.Transition.None;
                _catcher.onClick.AddListener(() =>
                {
                    System.Action n = _onTap;
                    _onTap = null;
                    _catcher.gameObject.SetActive(false);
                    AudioBus.Tap();
                    n?.Invoke();
                });
            }
            _catcher.gameObject.SetActive(true);
            _catcher.transform.SetAsLastSibling();
        }

        void ClearNarration()
        {
            _finger = null;
            _onTap = null;
            if (_catcher != null) _catcher.gameObject.SetActive(false);
        }

        bool _followHole;

        public void Swipe(Vector2 from, Vector2 to)
        {
            _hint = Hint.Swipe;
            _followHole = false;
            _from = from;
            _to = to;
            _age = 0f;
            _hand.gameObject.SetActive(true);
        }

        // 气泡别压住这块。先按镂空摆，重叠了就整段抬到它上方。
        public void ClearOf(RectTransform target)
        {
            _avoid = target;
        }

        // 气泡自动摆在镂空上方，镂空太靠上就摆下方并把尾巴翻上去。
        public void Say(string text)
        {
            _say.text = text;
            _bubble.gameObject.SetActive(!string.IsNullOrEmpty(text));
            PlaceBubble();
            _bubble.localScale = Vector3.zero;
            UiAnim.On(_bubble).Pop(_bubble, 0f, 0.3f, 0f);
        }

        public void Close()
        {
            if (this != null) Destroy(gameObject);
        }

        void LateUpdate()
        {
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1) transform.SetAsLastSibling();
            if (_target != null) Track();
            if (_finger != null) PlaceFinger();
            Animate(Time.unscaledDeltaTime);
        }

        void Track()
        {
            if (_target == null) return;
            var corners = new Vector3[4];
            _target.GetWorldCorners(corners);
            Vector2 a = _layer.InverseTransformPoint(corners[0]);
            Vector2 b = _layer.InverseTransformPoint(corners[2]);
            Vector2 off = _layer.rect.center;
            a -= off;
            b -= off;
            _hole = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - _pad, Mathf.Min(a.y, b.y) - _pad,
                Mathf.Max(a.x, b.x) + _pad, Mathf.Max(a.y, b.y) + _pad);
            Paint();
            if (_bubble.gameObject.activeSelf) PlaceBubble();
        }

        void PlaceFinger()
        {
            if (_finger == null) return;
            var corners = new Vector3[4];
            _finger.GetWorldCorners(corners);
            Vector2 a = _layer.InverseTransformPoint(corners[0]);
            Vector2 b = _layer.InverseTransformPoint(corners[2]);
            _from = (a + b) * 0.5f - _layer.rect.center;
        }

        void Paint()
        {
            float w = _layer.rect.width * 0.5f + 4f;
            float h = _layer.rect.height * 0.5f + 4f;
            Color c = _clear ? new Color(0f, 0f, 0f, 0.001f) : Shade;
            for (int i = 0; i < 4; i++) _shade[i].color = c;
            if (!_open)
            {
                Set(_shade[0], -w, -h, w, h);
                for (int i = 1; i < 4; i++) Set(_shade[i], 0f, 0f, 0f, 0f);
                return;
            }
            float x0 = Mathf.Clamp(_hole.xMin, -w, w);
            float x1 = Mathf.Clamp(_hole.xMax, -w, w);
            float y0 = Mathf.Clamp(_hole.yMin, -h, h);
            float y1 = Mathf.Clamp(_hole.yMax, -h, h);
            Set(_shade[0], -w, y1, w, h);
            Set(_shade[1], -w, -h, w, y0);
            Set(_shade[2], -w, y0, x0, y1);
            Set(_shade[3], x1, y0, w, y1);
        }

        static void Set(Image img, float x0, float y0, float x1, float y1)
        {
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        }

        void PlaceBubble()
        {
            float h = _layer.rect.height * 0.5f;
            float w = _layer.rect.width * 0.5f;
            bool above = !_open || _hole.center.y < h * 0.25f;
            float y = above
                ? (_open ? _hole.yMax : 0f) + 26f + BubbleH * 0.5f
                : _hole.yMin - 26f - BubbleH * 0.5f;
            LiftClear(ref y, ref above);
            // 手指压在镂空右下，气泡在上方时往左让一点，别和手叠在一起。
            float x = _open ? Mathf.Clamp(_hole.center.x - 40f, -w + BubbleW * 0.5f + 12f, w - BubbleW * 0.5f - 12f) : 0f;
            y = Mathf.Clamp(y, -h + BubbleH * 0.5f + 20f, h - BubbleH * 0.5f - 80f);
            _bubble.anchorMin = _bubble.anchorMax = _bubble.pivot = new Vector2(0.5f, 0.5f);
            _bubble.anchoredPosition = new Vector2(x, y);
            _bubbleArt.localScale = new Vector3(1f, above ? 1f : -1f, 1f);
            _say.rectTransform.anchoredPosition = new Vector2(0f, above ? BubbleH * 0.08f : -BubbleH * 0.08f);
        }

        // 气泡压住要露出来的那块时，整段抬到它上方，尾巴仍朝下。
        void LiftClear(ref float y, ref bool above)
        {
            if (_avoid == null) return;
            var corners = new Vector3[4];
            _avoid.GetWorldCorners(corners);
            Vector2 a = _layer.InverseTransformPoint(corners[0]);
            Vector2 b = _layer.InverseTransformPoint(corners[2]);
            Vector2 off = _layer.rect.center;
            float top = Mathf.Max(a.y, b.y) - off.y;
            float bot = Mathf.Min(a.y, b.y) - off.y;
            float gap = 16f;
            float bubbleBot = y - BubbleH * 0.5f;
            float bubbleTop = y + BubbleH * 0.5f;
            if (bubbleTop <= bot - gap || bubbleBot >= top + gap) return;
            y = top + gap + BubbleH * 0.5f;
            above = true;
        }

        void Animate(float dt)
        {
            if (_hint == Hint.None) return;
            _age += dt;
            if (_hint == Hint.Tap)
            {
                Vector2 at = _followHole ? _hole.center : _from;
                // 一秒按两下：抬起、按下，按下那一瞬手指缩一点。
                float k = Mathf.Repeat(_age * 1.6f, 1f);
                float lift = k < 0.55f ? Mathf.SmoothStep(0f, 1f, k / 0.55f) : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.55f) / 0.45f);
                _hand.anchoredPosition = at + new Vector2(10f, -14f) + new Vector2(18f, -24f) * lift;
                float s = 1f - 0.08f * (1f - lift);
                _hand.localScale = new Vector3(s, s, 1f);
                return;
            }
            float t = Mathf.PingPong(_age * 0.7f, 1f);
            float e = Mathf.SmoothStep(0f, 1f, t);
            _hand.anchoredPosition = Vector2.Lerp(_from, _to, e);
            _hand.localScale = Vector3.one;
        }
    }
}
