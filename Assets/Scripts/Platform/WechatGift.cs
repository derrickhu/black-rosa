using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 微信后台把礼包发到云函数，进游戏或从微信切回来时领进存档。
    public static class WechatGift
    {
        const int MaxIds = 300;
        static bool _busy;

        [Serializable]
        sealed class IdBody
        {
            public string[] ids;
        }

        public static IEnumerator Sync(MetaProgress meta, Transform layer, Action refreshed)
        {
            if (_busy || meta == null || !WxBridge.IsMiniGame) yield break;
            _busy = true;
            CloudReply reply = null;
            string err = null;
            yield return Backend.Call(CloudConfig.WechatGiftQueryPath, "{}", (s, r, e) =>
            {
                reply = r;
                err = e;
            });
            if (err != null || reply == null || reply.data == null || reply.data.gifts == null)
            {
                _busy = false;
                yield break;
            }

            var known = LoadIds();
            var mark = new List<string>();
            int granted = 0;
            for (int i = 0; i < reply.data.gifts.Length; i++)
            {
                WechatGiftRow gift = reply.data.gifts[i];
                if (gift == null || string.IsNullOrEmpty(gift.id)) continue;
                if (known.Contains(gift.id))
                {
                    mark.Add(gift.id);
                    continue;
                }
                if (gift.ink <= 0 && gift.stamina <= 0 && gift.diamond <= 0
                    && gift.wood <= 0 && gift.silver <= 0 && gift.gold <= 0
                    && gift.royal <= 0 && gift.cannon <= 0) continue;
                meta.GrantWechatGift(gift.ink, gift.stamina, gift.diamond,
                    gift.wood, gift.silver, gift.gold, gift.royal, gift.cannon);
                known.Add(gift.id);
                mark.Add(gift.id);
                granted++;
            }
            if (granted > 0)
            {
                SaveIds(known);
                if (layer != null)
                    InkToast.Show(layer, granted > 1 ? "微信礼包已领 " + granted + " 个" : "微信礼包已领");
                if (refreshed != null) refreshed();
            }
            if (mark.Count > 0)
            {
                string json = JsonUtility.ToJson(new IdBody { ids = mark.ToArray() });
                yield return Backend.Call(CloudConfig.WechatGiftMarkPath, json, (s, r, e) => { });
            }
            _busy = false;
        }

        static List<string> LoadIds()
        {
            var list = new List<string>();
            string raw = PlayerPrefs.GetString(CloudConfig.WechatGiftGrantedKey, "");
            if (string.IsNullOrEmpty(raw)) return list;
            IdBody body;
            try { body = JsonUtility.FromJson<IdBody>(raw); }
            catch (Exception) { return list; }
            if (body == null || body.ids == null) return list;
            for (int i = 0; i < body.ids.Length; i++)
                if (!string.IsNullOrEmpty(body.ids[i])) list.Add(body.ids[i]);
            return list;
        }

        static void SaveIds(List<string> ids)
        {
            if (ids.Count > MaxIds) ids.RemoveRange(0, ids.Count - MaxIds);
            PlayerPrefs.SetString(CloudConfig.WechatGiftGrantedKey, JsonUtility.ToJson(new IdBody { ids = ids.ToArray() }));
            PlayerPrefs.Save();
        }
    }
}
