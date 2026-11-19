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
        Vector2 _punch;

        public static void Kick(Camera cam, float power)
        {
            if (cam == null || power <= 0.001f) return;
            var shake = cam.GetComponent<InkShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<InkShake>();
            shake._power = Mathf.Min(0.26f, Mathf.Max(shake._power, power * 0.34f));
            shake._seed = Random.value * 20f;
        }

        // 方向性冲击（砸地向下顿、斩击沿刀向顿），叠在微震之上更有重量感。
        public static void Punch(Camera cam, Vector2 dir, float power)
        {
            if (cam == null || power <= 0.001f) return;
            var shake = cam.GetComponent<InkShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<InkShake>();
            Vector2 d = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector2.down;
            float mag = Mathf.Min(0.22f, power * 0.32f);
            if (mag > shake._punch.magnitude) shake._punch = d * mag;
            shake._power = Mathf.Min(0.26f, Mathf.Max(shake._power, power * 0.22f));
            shake._seed = Random.value * 20f;
        }

        // 战斗页会把相机基准位挪开，好给底栏腾出炮的位置。
        // 不更新 _home 的话，LateUpdate 会把相机拽回第一次记下的原点。
        public static void Pin(Camera cam)
        {
            if (cam == null) return;
            var shake = cam.GetComponent<InkShake>();
            if (shake == null) return;
            shake._home = cam.transform.position;
            shake._got = true;
        }

        void LateUpdate()
        {
            if (!_got)
            {
                _home = transform.position;
                _got = true;
            }
            if (_power <= 0.0008f && _punch.sqrMagnitude <= 1e-6f)
            {
                _power = 0f;
                _punch = Vector2.zero;
                transform.position = _home;
                return;
            }
            float dt = Time.unscaledDeltaTime;
            _power *= Mathf.Exp(-Decay * dt);
            _punch *= Mathf.Exp(-14f * dt);
            float t = Time.unscaledTime * 42f + _seed;
            // 两个不同频率的正弦，比纯随机更像「撞了一下」而不是信号噪点。
            transform.position = _home + new Vector3(
                Mathf.Sin(t) * _power + _punch.x,
                Mathf.Sin(t * 1.7f + 1.3f) * _power * 0.7f + _punch.y, 0f);
        }
    }
}
