using System.Runtime.InteropServices;
using UnityEngine;

namespace InkLine
{
    // 微信「评价与推荐」里的推荐组件。官方不让用道具换推荐，所以只拉起，不发奖。
    // 基础库没有 createPageManager 时静默跳过。弹出过一次就不再弹。
    public sealed class WxRecommend : MonoBehaviour
    {
        const string ShownKey = "ink_wx_recommend_shown";

        static WxRecommend _it;
        static bool _busy;
        static bool _blocked;

        public static void Preload()
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (Application.isEditor || Seen()) return;
            Ensure();
            InkRecommendLoad();
#endif
        }

        public static void TryShow()
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (Application.isEditor || _busy || _blocked || Seen()) return;
            Ensure();
            _busy = true;
            InkRecommendShow();
#endif
        }

        static bool Seen() => PlayerPrefs.GetInt(ShownKey, 0) == 1;

        static void Ensure()
        {
            if (_it != null) return;
            var go = new GameObject("WxRecommend");
            DontDestroyOnLoad(go);
            _it = go.AddComponent<WxRecommend>();
        }

        public void OnJs(string msg)
        {
            _busy = false;
            if (msg == "show")
            {
                PlayerPrefs.SetInt(ShownKey, 1);
                PlayerPrefs.Save();
                return;
            }
            if (msg == "unsupported" || (msg != null && msg.StartsWith("error")))
            {
                _blocked = true;
                Debug.LogWarning("[WxRecommend] " + msg);
                return;
            }
            if (!string.IsNullOrEmpty(msg)) Debug.Log("[WxRecommend] " + msg);
        }

#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
        [DllImport("__Internal")]
        static extern void InkRecommendCall(int mode);

        static void InkRecommendLoad() => InkRecommendCall(0);

        static void InkRecommendShow() => InkRecommendCall(1);
#endif
    }
}
