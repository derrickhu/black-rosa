using UnityEngine;
using UnityEngine.EventSystems;

namespace InkLine
{
    public static class InkPointer
    {
        public static Vector2 ScreenPos { get; private set; }
        public static bool Down { get; private set; }
        public static bool Held { get; private set; }
        public static bool Up { get; private set; }

        public static void Pump()
        {
            Down = Held = Up = false;
            if (TryFromEventSystem()) return;

            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                ApplyTouch(t.position, t.phase);
                return;
            }

            ScreenPos = Input.mousePosition;
            Down = Input.GetMouseButtonDown(0);
            Held = Input.GetMouseButton(0);
            Up = Input.GetMouseButtonUp(0);
        }

        static bool TryFromEventSystem()
        {
            var es = EventSystem.current;
            if (es == null || es.currentInputModule == null) return false;
            BaseInput input = es.currentInputModule.input;
            if (input == null || input.touchCount <= 0) return false;
            Touch t = input.GetTouch(0);
            ApplyTouch(t.position, t.phase);
            return true;
        }

        static void ApplyTouch(Vector2 pos, TouchPhase phase)
        {
            ScreenPos = pos;
            Down = phase == TouchPhase.Began;
            Held = phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
            Up = phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
        }

        public static Vector3 WorldOnPlane()
        {
            if (Camera.main == null) return Vector3.zero;
            Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(ScreenPos.x, ScreenPos.y, 0f));
            w.z = 0f;
            return w;
        }
    }
}
