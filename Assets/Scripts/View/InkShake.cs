using UnityEngine;

namespace InkLine
{
    // 相机震动。挂在主相机上，只在 LateUpdate 里加一个衰减的偏移，
    // 每帧都先回到基准位再抖 —— 不记基准位的话，被别的脚本挪过相机之后
    // 震动会把它一路推走。
    public sealed class InkShake : MonoBehaviour
    {
        const float Decay = 4.2f;

        Vector3 _home;
        bool _got;
        float _power;
        float _seed;

        public static void Kick(Camera cam, float power)
        {
            if (cam == null || power <= 0.001f) return;
            var shake = cam.GetComponent<InkShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<InkShake>();
            shake._power = Mathf.Min(0.26f, Mathf.Max(shake._power, power * 0.34f));
            shake._seed = Random.value * 20f;
        }

        void LateUpdate()
        {
            if (!_got)
            {
                _home = transform.position;
                _got = true;
            }
            if (_power <= 0.0008f)
            {
                _power = 0f;
                transform.position = _home;
                return;
            }
            _power *= Mathf.Exp(-Decay * Time.unscaledDeltaTime);
            float t = Time.unscaledTime * 42f + _seed;
            // 两个不同频率的正弦，比纯随机更像「撞了一下」而不是信号噪点。
            transform.position = _home + new Vector3(
                Mathf.Sin(t) * _power,
                Mathf.Sin(t * 1.7f + 1.3f) * _power * 0.7f, 0f);
        }
    }
}
