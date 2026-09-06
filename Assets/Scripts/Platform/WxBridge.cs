using System;
using UnityEngine;

namespace InkLine
{
    public static class WxBridge
    {
        static Type WxType()
        {
            return Type.GetType("WeChatWASM.WX, Wx") ?? Type.GetType("WeChatWASM.WX");
        }

        public static void InitSdk(Action ready)
        {
            var wx = WxType();
            var init = wx != null ? wx.GetMethod("InitSDK", new[] { typeof(Action<int>) }) : null;
            if (init == null)
            {
                ready();
                return;
            }
            Action<int> cb = _ => ready();
            init.Invoke(null, new object[] { cb });
        }

        public static void KeepRuntime()
        {
            var wx = WxType();
            wx?.GetMethod("GetSystemInfoSync", Type.EmptyTypes)?.Invoke(null, null);
        }

        public static void OverrideTouch(GameObject es)
        {
            var t = Type.GetType("WXTouchInputOverride, Wx") ?? Type.GetType("WXTouchInputOverride");
            if (t == null || es.GetComponent(t) != null) return;
            es.AddComponent(t);
        }
    }
}
