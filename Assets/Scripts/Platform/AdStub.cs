using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 激励视频。编辑器里直接当看完，方便验循环；真机走微信广告位，看完才发奖。
    public static class AdStub
    {
        const string ChestSpeed = "adunit-39b1a921c9771f39";
        const string StarterGift = "adunit-173877ad925cc36d";
        const string Emitter = "adunit-e695ec449a36124f";
        const string Skin = "adunit-5350ff9cec6f29ee";
        const string Reroll = "adunit-11415a8a0688700b";
        const string Stamina = "adunit-9d8f7809fe38e610";
        const string CheckIn = "adunit-501d0517137c5114";
        const string Event = "adunit-b9540a99a3ceffcb";

        // 签到当天补领一份和当场翻倍是同一个广告位；招财进宝加次数、存钱翻倍、失败存回也共用一个。
        // 结算「墨翻倍 + 开宝箱」(double) 和复活 (revive) 还没给广告位，真机上不发奖。
        static string UnitOf(string slot)
        {
            switch (slot)
            {
                case "chest_speed": return ChestSpeed;
                case "starter_gift": return StarterGift;
                case "emitter": return Emitter;
                case "skin": return Skin;
                case "reroll": return Reroll;
                case "stamina": return Stamina;
                case "checkin_double":
                case "checkin_bonus": return CheckIn;
                case "event_plays":
                case "event_double":
                case "event_salvage": return Event;
                default: return null;
            }
        }

        // 返回 false 表示这次没播成（没广告位，或上一条还在播）。播成了才会在结束时调 onSettled，
        // 看完、中途关掉、加载失败都算结束。编辑器里没有广告，当场发奖并结束。
        public static bool Reward(string slot, Action onOk, Action onSettled = null)
        {
            if (!WxBridge.CanAskProfile)
            {
                onOk?.Invoke();
                onSettled?.Invoke();
                return true;
            }
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            string unit = UnitOf(slot);
            if (unit == null)
            {
                Debug.LogWarning("[Ad] 没有广告位 " + slot);
                Tell("这个广告还没配好");
                return false;
            }
            return Play(unit, slot, onOk, onSettled);
#else
            return false;
#endif
        }

#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
        sealed class Holder
        {
            public WeChatWASM.WXRewardedVideoAd Ad;
            public Action Pending;
            public Action Settled;
            public bool Busy;
            public int Gen;
            public string Scene;
            public string Unit;
            public bool Reported;
        }

        static readonly Dictionary<string, Holder> Holders = new Dictionary<string, Holder>();

        static Holder HolderOf(string unit)
        {
            if (Holders.TryGetValue(unit, out Holder got)) return got;
            // 开局把 8 个位一起建出来，微信会警告「实例过多」，鸿蒙上这一下能卡接近一秒。
            // 同时只留一个：换广告位之前先拆掉闲着的。multiton 还是要开，
            // 否则全局只认第一次的广告位，看完不发奖。
            DropIdle();
            var h = new Holder();
            h.Ad = WeChatWASM.WX.CreateRewardedVideoAd(new WeChatWASM.WXCreateRewardedVideoAdParam
            {
                adUnitId = unit,
                multiton = true,
            });
            h.Ad.OnClose(r => Closed(h, r));
            h.Ad.OnError(e => Errored(h, e));
            h.Ad.Load(_ => { }, e => Debug.LogWarning("[Ad] 预加载失败 " + e.errCode + " " + e.errMsg));
            Holders[unit] = h;
            return h;
        }

        static void DropIdle()
        {
            List<string> drop = null;
            foreach (var kv in Holders)
            {
                if (kv.Value.Busy || kv.Value.Ad == null) continue;
                try { kv.Value.Ad.Destroy(); }
                catch (Exception e) { Debug.LogWarning("[Ad] " + e.Message); }
                if (drop == null) drop = new List<string>();
                drop.Add(kv.Key);
            }
            if (drop == null) return;
            for (int i = 0; i < drop.Count; i++) Holders.Remove(drop[i]);
        }

        static bool Play(string unit, string slot, Action onOk, Action onSettled)
        {
            Holder h = HolderOf(unit);
            if (h.Busy)
            {
                Tell("广告还在播");
                return false;
            }
            h.Busy = true;
            h.Pending = onOk;
            h.Settled = onSettled;
            h.Scene = slot;
            h.Unit = unit;
            h.Reported = false;
            int gen = ++h.Gen;
            AudioBus.Duck(true);
            Analytics.AdRequest(slot, unit);
            h.Ad.Show(_ => Analytics.AdShow(slot, unit), _ => LoadThenShow(h, gen));
            return true;
        }

        // 还没加载好时 show 会失败，补拉一次再播。官方推荐的写法。
        static void LoadThenShow(Holder h, int gen)
        {
            if (!h.Busy || gen != h.Gen) return;
            h.Ad.Load(
                _ =>
                {
                    if (!h.Busy || gen != h.Gen) return;
                    h.Ad.Show(__ => Analytics.AdShow(h.Scene, h.Unit), ___ => Fail(h, gen, "广告加载失败，稍后再试"));
                },
                _ => Fail(h, gen, "广告加载失败，稍后再试"));
        }

        static void Closed(Holder h, WeChatWASM.WXRewardedVideoAdOnCloseResponse r)
        {
            if (!h.Busy)
            {
                h.Ad.Load(_ => { }, _ => { });
                return;
            }
            // 旧基础库关掉时不给结果，微信要求这种也发奖。中途关掉的 isEnded 是 false。
            bool ended = r == null || r.isEnded;
            Action ok = h.Pending;
            Analytics.AdClose(h.Scene, h.Unit, ended);
            Stop(h, h.Gen, ended ? null : "看完才能领取");
            if (ended) ok?.Invoke();
            h.Ad.Load(_ => { }, _ => { });
        }

        static void Fail(Holder h, int gen, string msg)
        {
            ReportAdError(h, msg);
            Stop(h, gen, msg);
        }

        static void ReportAdError(Holder h, string msg)
        {
            if (h.Reported) return;
            h.Reported = true;
            Analytics.AdError(h.Scene, h.Unit, msg);
        }

        static void Errored(Holder h, WeChatWASM.WXADErrorResponse e)
        {
            Debug.LogWarning("[Ad] " + e.errCode + " " + e.errMsg);
            if (!h.Busy) return;
            ReportAdError(h, e.errMsg);
            Stop(h, h.Gen, "广告加载失败，稍后再试");
        }

        static void Stop(Holder h, int gen, string msg)
        {
            if (!h.Busy || gen != h.Gen) return;
            h.Busy = false;
            Action settled = h.Settled;
            h.Pending = null;
            h.Settled = null;
            h.Gen++;
            AudioBus.Duck(false);
            settled?.Invoke();
            if (msg == null) return;
            AudioBus.Deny();
            Tell(msg);
        }

        static void Tell(string text)
        {
            Canvas best = null;
            Canvas[] all = UnityEngine.Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < all.Length; i++)
            {
                Canvas c = all[i];
                if (!c.isActiveAndEnabled || !c.isRootCanvas) continue;
                if (best == null || c.sortingOrder >= best.sortingOrder) best = c;
            }
            if (best != null) InkToast.Show(best.transform, text);
            else Debug.LogWarning("[Ad] " + text);
        }
#endif
    }
}
