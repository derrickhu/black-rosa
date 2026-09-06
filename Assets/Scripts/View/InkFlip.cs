using UnityEngine;

namespace InkLine
{
    public sealed class InkFlip : MonoBehaviour
    {
        public Sprite[] Frames;
        public float Fps = 10f;
        public bool Loop = true;
        public float Phase;
        public SpriteRenderer Outline;
        public System.Action OnDone;

        SpriteRenderer _sr;
        float _t;
        bool _done;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Restart()
        {
            _t = 0f;
            _done = false;
            enabled = true;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _sr.enabled = true;
            Apply(0);
        }

        void LateUpdate()
        {
            if (_done || Frames == null || Frames.Length == 0) return;
            _t += Time.unscaledDeltaTime * Fps;
            if (Loop)
            {
                Apply(((int)(_t + Phase)) % Frames.Length);
                return;
            }
            int i = Mathf.Min((int)_t, Frames.Length - 1);
            Apply(i);
            if (_t < Frames.Length) return;
            _done = true;
            enabled = false;
            if (_sr != null) _sr.enabled = false;
            OnDone?.Invoke();
        }

        void Apply(int i)
        {
            if (_sr == null || Frames == null || i < 0 || i >= Frames.Length) return;
            if (Frames[i] != null)
            {
                _sr.sprite = Frames[i];
                if (Outline != null) Outline.sprite = Frames[i];
            }
        }
    }
}
