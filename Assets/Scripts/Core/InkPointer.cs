using System;
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

        // 微信原生页（红包、转发）盖住时，松手事件经常丢。旧手指会一直报按住，
        // 新的点击全被当成同一次拖动，炮台就再也拖不动。盖住回来后只认新按下的一下。
        static bool _armFresh;

        public static void ArmFresh() => _armFresh = true;

        public static void Pump()
        {
            Down = Held = Up = false;
            if (TryFromEventSystem()) return;

            if (Input.touchCount > 0)
            {
                if (FreshTouch(Input.touchCount, i => Input.GetTouch(i))) return;
                Touch t = Input.GetTouch(0);
                ApplyTouch(t.position, t.phase);
                return;
            }

            ScreenPos = Input.mousePosition;
            if (_armFresh)
            {
                if (Input.GetMouseButtonUp(0)) _armFresh = false;
                else if (!Input.GetMouseButton(0)) _armFresh = false;
                else return;
            }
            Down = Input.GetMouseButtonDown(0);
            Held = Input.GetMouseButton(0);
            Up = Input.GetMouseButtonUp(0);
        }

        static bool FreshTouch(int count, System.Func<int, Touch> get)
        {
            if (!_armFresh) return false;
            for (int i = 0; i < count; i++)
            {
                Touch t = get(i);
                if (t.phase != TouchPhase.Began) continue;
                _armFresh = false;
                ApplyTouch(t.position, t.phase);
                return true;
            }
            return true;
        }

        static bool TryFromEventSystem()
        {
            var es = EventSystem.current;
            if (es == null || es.currentInputModule == null) return false;
            BaseInput input = es.currentInputModule.input;
            if (input == null || input.touchCount <= 0) return false;
            if (FreshTouch(input.touchCount, i => input.GetTouch(i))) return true;
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
