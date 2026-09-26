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

        // wx.login 拿一次性 code，换 openid 在云函数里做。非微信构建直接报失败。
        public static void Login(Action<string> ok, Action<string> fail)
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            WeChatWASM.WX.Login(new WeChatWASM.LoginOption
            {
                success = r => ok(r.code),
                fail = e => fail(e.errMsg)
            });
#else
            fail("not minigame");
#endif
        }

        // 切后台（锁屏、回桌面、聊天顶部）。编辑器和其它平台由调用方走 OnApplicationPause。
        public static bool OnHide(Action hide)
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (Application.isEditor) return false;
            WeChatWASM.WX.OnHide(_ => hide());
            return true;
#else
            return false;
#endif
        }

        // 真机上才有微信原生的授权按钮；编辑器和开发者工具以外的平台一律没有。
        public static bool CanAskProfile
        {
            get
            {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
                return !Application.isEditor;
#else
                return false;
#endif
            }
        }

#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
        static WeChatWASM.WXUserInfoButton _infoBtn;
#endif

        // 微信只允许用它自己画的透明按钮拿昵称头像，所以在 Unity 按钮正上方盖一个同样大小的。
        // screenRect 是 Unity 屏幕像素（左下原点），这里换成微信窗口坐标（左上原点、逻辑像素）。
        // got(nick, avatarUrl)：拒绝授权时不回调。
        public static void ShowProfileButton(Rect screenRect, Action<string, string> got)
        {
            HideProfileButton();
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (Application.isEditor) return;
            float winW = Num(SystemInfo(), "windowWidth");
            float k = winW > 0f ? winW / Mathf.Max(1, Screen.width) : 1f;
            int x = Mathf.RoundToInt(screenRect.xMin * k);
            int y = Mathf.RoundToInt((Screen.height - screenRect.yMax) * k);
            int w = Mathf.RoundToInt(screenRect.width * k);
            int h = Mathf.RoundToInt(screenRect.height * k);
            _infoBtn = WeChatWASM.WX.CreateUserInfoButton(x, y, w, h, "zh_CN", false);
            _infoBtn.OnTap(res =>
            {
                // userInfo 是结构体，不能和 null 写在同一个三元表达式里。
                if (res == null || string.IsNullOrEmpty(res.userInfo.nickName)) return;
                got(res.userInfo.nickName, res.userInfo.avatarUrl ?? "");
            });
            _infoBtn.Show();
#endif
        }

        public static void HideProfileButton()
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (_infoBtn == null) return;
            _infoBtn.Destroy();
            _infoBtn = null;
#endif
        }

        // 玩家昵称什么字都可能有，游戏字体只切了用到的几百个字。
        // 真机拿微信的系统字体；编辑器里用本机字体。拿不到回调 null。
        static Font _sysFont;
        static bool _sysFontAsked;
        static Action<Font> _sysFontWait;

        public static void SystemFont(Action<Font> done)
        {
            if (_sysFont != null || (_sysFontAsked && _sysFontWait == null)) { done(_sysFont); return; }
            _sysFontWait += done;
            if (_sysFontAsked) return;
            _sysFontAsked = true;
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (!Application.isEditor)
            {
                WeChatWASM.WX.GetWXFont(null, f => FinishFont(f));
                return;
            }
#endif
            FinishFont(Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Heiti SC", "Noto Sans CJK SC", "Arial Unicode MS" }, 28));
        }

        static void FinishFont(Font f)
        {
            _sysFont = f;
            Action<Font> wait = _sysFontWait;
            _sysFontWait = null;
            wait?.Invoke(f);
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

        // 编辑器，或微信开发者工具的模拟器。真机（ios / android / ohos / windows）一律 false。
        // 问不到平台就当真机，GM 入口不会出现。
        public static bool IsSimulator
        {
            get
            {
                if (Application.isEditor) return true;
                try
                {
                    object raw = Member(SystemInfo(), "platform");
                    string name = raw as string;
                    return name != null && name.Equals("devtools", StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    return false;
                }
            }
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
