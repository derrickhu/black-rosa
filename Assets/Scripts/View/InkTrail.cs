using UnityEngine;

namespace InkLine
{
    public sealed class InkTrail : MonoBehaviour
    {
        const int Max = 16;
        readonly Vector3[] _pts = new Vector3[Max];
        LineRenderer _line;
        float _acc;
        int _count;

        public void Setup(Color head, Color tail, float startWidth)
        {
            if (_line == null)
            {
                _line = gameObject.AddComponent<LineRenderer>();
                _line.useWorldSpace = true;
                _line.textureMode = LineTextureMode.Stretch;
                _line.numCapVertices = 4;
                _line.numCornerVertices = 3;
                _line.sortingOrder = 5;
                _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _line.receiveShadows = false;
                var sr = GetComponent<SpriteRenderer>();
                _line.material = sr != null && sr.sharedMaterial != null
                    ? new Material(sr.sharedMaterial)
                    : new Material(Shader.Find("Sprites/Default"));
            }
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                new[] { new GradientAlphaKey(0.88f, 0f), new GradientAlphaKey(0f, 1f) });
            _line.colorGradient = g;
            _line.startWidth = startWidth;
            _line.endWidth = startWidth * 0.08f;
            _line.enabled = true;
        }

        public void Feed(Vector3 pos, float dt)
        {
            if (_line == null) return;
            if (_count == 0)
            {
                _pts[0] = pos;
                _count = 1;
            }
            else
                _pts[0] = pos;

            _acc += dt;
            if (_acc >= 0.016f)
            {
                _acc = 0f;
                if (_count < Max)
                {
                    for (int i = _count; i > 0; i--) _pts[i] = _pts[i - 1];
                    _count++;
                }
                else
                {
                    for (int i = Max - 1; i > 0; i--) _pts[i] = _pts[i - 1];
                }
                _pts[0] = pos;
            }

            _line.positionCount = _count;
            _line.SetPositions(_pts);
            _line.enabled = _count > 1;
        }

        public void Hide()
        {
            _count = 0;
            _acc = 0f;
            if (_line != null)
            {
                _line.positionCount = 0;
                _line.enabled = false;
            }
        }
    }
}
