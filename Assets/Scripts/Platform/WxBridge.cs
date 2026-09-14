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

        public static bool IsMiniGame
        {
            get
            {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
                return true;
#else
                string p = Application.platform.ToString();
                return p.IndexOf("MiniGame", StringComparison.OrdinalIgnoreCase) >= 0
                    || p.IndexOf("Weixin", StringComparison.OrdinalIgnoreCase) >= 0;
#endif
            }
        }

        // 右上角那个胶囊按钮（「···」+「⊙」）是微信画在游戏画布之上的，
        // 既不在 Screen.safeArea 里，Unity 也看不见它 —— 顶栏那排数值就这么被压住了。
        // 返回胶囊下沿占屏幕高度的比例（0~1），拿不到返回 -1。
        // 每帧都会问（ScreenFit.Apply 在 Update 里），所以只算一次；胶囊不会动。
        static float _capsule = float.NaN;

        public static float CapsuleBottomFrac()
        {
            if (!float.IsNaN(_capsule)) return _capsule;
            // 拿不到接口时的兜底：状态栏 + 胶囊在常见竖屏机型上大约占屏高的 9.5%。
            // 只在真机上兜底 —— 编辑器里没有胶囊，白让出一条会看不出真实排版。
            _capsule = IsMiniGame ? 0.095f : -1f;
            try
            {
                Type wx = WxType();
                object rect = wx?.GetMethod("GetMenuButtonBoundingClientRect", Type.EmptyTypes)
                                 ?.Invoke(null, null);
                float bottom = Num(rect, "bottom");
                float screenH = Num(SystemInfo(), "screenHeight");
                if (bottom > 0f && screenH > 0f) _capsule = bottom / screenH;
            }
            catch (Exception)
            {
                // 老版本基础库没有这个接口，兜底值已经设好了
            }
            return _capsule;
        }

        // 微信自己报的安全区，单位和 screenHeight 一致。团结导出的包里
        // Screen.safeArea 不一定填得对（刘海机上会退化成全屏），所以两边取大的那个。
        // 返回上下内缩占屏高的比例；拿不到就都是 -1。
        static Vector2 _inset = new Vector2(float.NaN, float.NaN);

        public static Vector2 SafeInsetFrac()
        {
            if (!float.IsNaN(_inset.x)) return _inset;
            _inset = new Vector2(-1f, -1f);
            try
            {
                object sys = SystemInfo();
                float screenH = Num(sys, "screenHeight");
                object area = Member(sys, "safeArea");
                float top = Num(area, "top");
                float bottom = Num(area, "bottom");
                if (screenH > 0f && bottom > top && bottom <= screenH)
                    _inset = new Vector2(top / screenH, (screenH - bottom) / screenH);
            }
            catch (Exception)
            {
            }
            return _inset;
        }

        static object SystemInfo()
        {
            Type wx = WxType();
            return wx?.GetMethod("GetSystemInfoSync", Type.EmptyTypes)?.Invoke(null, null);
        }

        // 微信 SDK 里这些结构体有时是字段有时是属性，数值类型也不统一（float / double / int），
        // 所以统一按名字取再 Convert，别按具体类型强转。
        static object Member(object o, string name)
        {
            if (o == null) return null;
            Type t = o.GetType();
            var f = t.GetField(name);
            if (f != null) return f.GetValue(o);
            var p = t.GetProperty(name);
            return p?.GetValue(o);
        }

        static float Num(object o, string name)
        {
            object v = Member(o, name);
            return v == null ? 0f : Convert.ToSingle(v);
        }
    }
}
