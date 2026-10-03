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

        // 后台改过隐私协议之后，昵称和游戏圈数据都要先过这一关，才会弹出微信自己的隐私弹窗。
        // 同意过的这次会话里不再问。编辑器没有这套接口，直接当已同意。
        public static bool PrivacyAgreed { get; private set; }

        static bool _privacyBusy;
        static Action<bool> _privacyWait;

        public static void EnsurePrivacy(Action<bool> done)
        {
            if (!CanAskProfile || PrivacyAgreed)
            {
                if (!CanAskProfile) PrivacyAgreed = true;
                done?.Invoke(true);
                return;
            }
            _privacyWait += done;
            if (_privacyBusy) return;
            _privacyBusy = true;
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            try
            {
                WeChatWASM.WX.RequirePrivacyAuthorize(new WeChatWASM.RequirePrivacyAuthorizeOption
                {
                    success = _ => FinishPrivacy(true),
                    fail = _ => FinishPrivacy(false),
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Privacy] " + e.Message);
                FinishPrivacy(true);
            }
#else
            FinishPrivacy(true);
#endif
        }

        static void FinishPrivacy(bool ok)
        {
            _privacyBusy = false;
            if (ok) PrivacyAgreed = true;
            Action<bool> wait = _privacyWait;
            _privacyWait = null;
            wait?.Invoke(ok);
        }

#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
        static WeChatWASM.WXUserInfoButton _infoBtn;
#endif

        // 微信只允许用它自己画的透明按钮拿昵称头像，所以在 Unity 按钮正上方盖一个同样大小的。
        // SDK 里还会把坐标除一次 devicePixelRatio，所以这里传物理像素，不能传逻辑像素。
        // got(nick, avatarUrl, err)：没拿到时 nick 为空，err 是微信给的 errMsg。
        public static void ShowProfileButton(Rect screenRect, Action<string, string, string> got)
        {
            HideProfileButton();
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (Application.isEditor) return;
            RectInt r = ToPhysical(screenRect);
            _infoBtn = WeChatWASM.WX.CreateUserInfoButton(r.x, r.y, r.width, r.height, "zh_CN", false);
            _infoBtn.OnTap(res =>
            {
                // userInfo 是结构体，不能和 null 写在同一个三元表达式里。
                if (res == null || string.IsNullOrEmpty(res.userInfo.nickName))
                {
                    string err = res == null ? "no response" : (res.errCode + " " + res.errMsg);
                    Debug.LogWarning("[Profile] " + err);
                    got(null, null, err);
                    return;
                }
                got(res.userInfo.nickName, res.userInfo.avatarUrl ?? "", null);
            });
            _infoBtn.Show();
#endif
        }

        // 拒绝过一次之后，微信不会再弹授权框，原生按钮点了直接失败。只能引去设置页重新打开。
        // 弹窗的「去设置」也算用户点击，openSetting 才放行。done(true) 表示回来时已允许。
        public static void AskOpenSetting(string content, Action<bool> done)
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (!Application.isEditor)
            {
                WeChatWASM.WX.ShowModal(new WeChatWASM.ShowModalOption
                {
                    title = "需要你的授权",
                    content = content,
                    confirmText = "去设置",
                    cancelText = "取消",
                    showCancel = true,
                    success = m =>
                    {
                        if (!m.confirm) { done?.Invoke(false); return; }
                        WeChatWASM.WX.OpenSetting(new WeChatWASM.OpenSettingOption
                        {
                            success = s =>
                            {
                                bool ok = s.authSetting != null && s.authSetting.ContainsKey("scope.userInfo")
                                          && s.authSetting["scope.userInfo"];
                                done?.Invoke(ok);
                            },
                            fail = _ => done?.Invoke(false),
                        });
                    },
                    fail = _ => done?.Invoke(false),
                });
                return;
            }
#endif
            done?.Invoke(false);
        }

        public static void HideProfileButton()
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (_infoBtn == null) return;
            _infoBtn.Destroy();
            _infoBtn = null;
#endif
        }

        // 游戏圈入口也只能用微信原生按钮打开，同样透明地盖在 Unity 按钮上。
        public static bool CanUseClub => CanAskProfile;

#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
        static WeChatWASM.WXGameClubButton _clubBtn;
#endif

        public static void ShowClubButton(Rect screenRect)
        {
            HideClubButton();
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (Application.isEditor) return;
            RectInt r = ToWindow(screenRect);
            _clubBtn = WeChatWASM.WX.CreateGameClubButton(new WeChatWASM.WXCreateGameClubButtonParam
            {
                type = WeChatWASM.GameClubButtonType.text,
                text = " ",
                style = new WeChatWASM.GameClubButtonStyle
                {
                    left = r.x,
                    top = r.y,
                    width = r.width,
                    height = r.height,
                    backgroundColor = "rgba(0,0,0,0)",
                    borderColor = "rgba(0,0,0,0)",
                    borderWidth = 0,
                    borderRadius = 0,
                    color = "rgba(0,0,0,0)",
                    fontSize = 12,
                    lineHeight = r.height,
                },
            });
            _clubBtn?.Show();
#endif
        }

        public static void HideClubButton()
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (_clubBtn == null) return;
            _clubBtn.Destroy();
            _clubBtn = null;
#endif
        }

        // 游戏圈数据是加密的，用登录时的 session_key 在云函数里解。
        // done(encryptedData, iv, err)：err 为 null 表示拿到了。
        public static void GameClubData(int dataType, Action<string, string, string> done)
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (!Application.isEditor)
            {
                WeChatWASM.WX.GetGameClubData(new WeChatWASM.GetGameClubDataOption
                {
                    dataTypeList = new[] { new WeChatWASM.DataType { type = dataType } },
                    success = r => done(r.encryptedData, r.iv, string.IsNullOrEmpty(r.encryptedData) ? "empty" : null),
                    fail = e => done(null, null, e != null ? e.errMsg : "fail"),
                });
                return;
            }
#endif
            done(null, null, "not minigame");
        }

        // 从游戏圈切回来时刷新进度。编辑器里没有，返回 false。
        public static bool OnShow(Action show)
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (Application.isEditor) return false;
            WeChatWASM.WX.OnShow(_ => show());
            return true;
#else
            return false;
#endif
        }

        // 游戏圈按钮的 style 是逻辑像素，SDK 不再缩放。
        static RectInt ToWindow(Rect screenRect)
        {
            float winW = Num(SystemInfo(), "windowWidth");
            float k = winW > 0f ? winW / Mathf.Max(1, Screen.width) : 1f;
            return TopLeft(screenRect, k);
        }

        // 昵称按钮的 JS 会再除 devicePixelRatio。Unity 屏幕若已是逻辑像素，这里先乘回去。
        static RectInt ToPhysical(Rect screenRect)
        {
            float winW = Num(SystemInfo(), "windowWidth");
            float dpr = Num(SystemInfo(), "pixelRatio");
            if (dpr < 1f) dpr = 1f;
            float scale = winW > 0f && Screen.width < winW * 1.5f ? dpr : 1f;
            return TopLeft(screenRect, scale);
        }

        static RectInt TopLeft(Rect screenRect, float scale)
        {
            return new RectInt(
                Mathf.RoundToInt(screenRect.xMin * scale),
                Mathf.RoundToInt((Screen.height - screenRect.yMax) * scale),
                Mathf.Max(1, Mathf.RoundToInt(screenRect.width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(screenRect.height * scale)));
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
