using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace InkLine
{
    // 排行榜，逻辑照 hotpot 的 RankService：
    //   通关关数、无尽（历史 / 本周）、每日残局当天的成绩一起报，同样的一组数 + 昵称头像只报一次；
    //   打开榜单先把没报上去的补报，再拉某张榜的前 N 名和自己的名次。
    // 服务端只升不降（周榜、日榜在同一周、同一天里只升不降），所以本地清档、换设备都不会把榜上成绩拉下来。
    public sealed class RankService : MonoBehaviour
    {
        public const string BoardStage = "stage";
        public const string BoardEndless = "endless";
        public const string BoardWeekly = "weekly";
        public const string BoardDaily = "daily";

        [Serializable]
        sealed class SubmitBody
        {
            public int cleared;
            public int endless;
            public int weekly;
            public int week;
            public int daily;
            public int day;
            public string displayName;
            public string avatarUrl;
        }

        // 一次要报的成绩。周号、日期按本机算，服务端按北京时间核对。
        public struct Scores
        {
            public int Cleared;
            public int Endless;
            public int Weekly;
            public int Week;
            public int Daily;
            public int Day;

            public bool Any => Cleared > 0 || Endless > 0 || Weekly > 0 || Daily > 0;

            public string Key => Cleared + ":" + Endless + ":" + Week + "/" + Weekly + ":" + Day + "/" + Daily;
        }

        public static Scores ScoresOf(MetaProgress meta) => new Scores
        {
            Cleared = meta.ClearedCount(),
            Endless = meta.EndlessBest,
            Weekly = meta.EndlessWeek == DayClock.Week ? meta.EndlessWeekBest : 0,
            Week = DayClock.Week,
            Daily = meta.DailyBestToday,
            Day = DayClock.Today,
        };

        [Serializable]
        sealed class ListBody
        {
            public int limit;
            public string board;
            public int week;
            public int day;
        }

        [Serializable]
        sealed class Profile
        {
            public string nick;
            public string avatar;
        }

        public sealed class Board
        {
            public RankRow[] List;
            public RankRow Mine;
        }

        static RankService _it;
        static Profile _profile;
        readonly Dictionary<string, Sprite> _avatars = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Action<Sprite>> _avatarWait = new Dictionary<string, Action<Sprite>>();
        bool _sending;

        static RankService It
        {
            get
            {
                if (_it != null) return _it;
                var go = new GameObject("RankService");
                DontDestroyOnLoad(go);
                _it = go.AddComponent<RankService>();
                return _it;
            }
        }

        // 开发者工具里有 GM 一键全通关，报上去就挂榜首了。编辑器走 anon 身份，服务端不给真机玩家看。
        static bool Muted => WxBridge.IsSimulator && !Application.isEditor;

        public static bool HasProfile => !string.IsNullOrEmpty(LoadProfile().nick);
        public static string Nick => LoadProfile().nick ?? "";
        public static string Avatar => LoadProfile().avatar ?? "";

        static Profile LoadProfile()
        {
            if (_profile != null) return _profile;
            string raw = PlayerPrefs.GetString(CloudConfig.ProfileKey, "");
            try { _profile = string.IsNullOrEmpty(raw) ? null : JsonUtility.FromJson<Profile>(raw); }
            catch (Exception) { _profile = null; }
            if (_profile == null) _profile = new Profile();
            return _profile;
        }

        // 微信拒绝授权也会回一个「微信用户」+ 灰头像，当没给。
        public static bool SetProfile(string nick, string avatar)
        {
            nick = (nick ?? "").Trim();
            if (nick.Length == 0 || nick == "微信用户") return false;
            _profile = new Profile { nick = nick, avatar = avatar ?? "" };
            PlayerPrefs.SetString(CloudConfig.ProfileKey, JsonUtility.ToJson(_profile));
            PlayerPrefs.Save();
            return true;
        }

        // 老版本的 key 格式不同，升级后对不上会补报一次，正好把新字段带上去。
        static string SentKey(Scores s) => s.Key + ":" + Nick + ":" + Avatar;

        public static void Submit(MetaProgress meta)
        {
            Scores s = ScoresOf(meta);
            if (!s.Any || Muted) return;
            if (PlayerPrefs.GetString(CloudConfig.RankSentKey, "") == SentKey(s)) return;
            It.StartCoroutine(It.SubmitRoutine(s, null));
        }

        public static void Load(string board, MetaProgress meta, Action<Board, string> done)
        {
            It.StartCoroutine(It.LoadRoutine(board, ScoresOf(meta), done));
        }

        IEnumerator SubmitRoutine(Scores s, Action done)
        {
            while (_sending) yield return null;
            string key = SentKey(s);
            if (!s.Any || Muted || PlayerPrefs.GetString(CloudConfig.RankSentKey, "") == key)
            {
                done?.Invoke();
                yield break;
            }
            _sending = true;
            var body = new SubmitBody
            {
                cleared = s.Cleared, endless = s.Endless, weekly = s.Weekly, week = s.Week,
                daily = s.Daily, day = s.Day, displayName = Nick, avatarUrl = Avatar
            };
            string err = null;
            yield return Backend.Call(CloudConfig.RankSubmitPath, JsonUtility.ToJson(body), (s, r, e) => err = e);
            _sending = false;
            if (err == null)
            {
                PlayerPrefs.SetString(CloudConfig.RankSentKey, key);
                PlayerPrefs.Save();
            }
            else Debug.LogWarning("[Rank] submit failed: " + err);
            done?.Invoke();
        }

        IEnumerator LoadRoutine(string board, Scores s, Action<Board, string> done)
        {
            yield return SubmitRoutine(s, null);
            CloudReply reply = null;
            string err = null;
            var body = new ListBody { limit = CloudConfig.RankListLimit, board = board, week = s.Week, day = s.Day };
            yield return Backend.Call(CloudConfig.RankListPath, JsonUtility.ToJson(body), (s, r, e) => { reply = r; err = e; });
            if (err != null || reply == null || reply.data == null)
            {
                done(null, err ?? "empty reply");
                yield break;
            }
            // JsonUtility 不会把缺省的对象字段留成 null，没上榜时 mine 是个 rank 为 0 的空壳。
            RankRow mine = reply.data.mine != null && reply.data.mine.rank > 0 ? reply.data.mine : null;
            done(new Board { List = reply.data.list ?? new RankRow[0], Mine = mine }, null);
        }

        // 头像按 URL 缓存。微信头像域名要加进小程序后台的 request / downloadFile 合法域名。
        public static void AvatarOf(string url, Action<Sprite> done)
        {
            if (string.IsNullOrEmpty(url)) { done(null); return; }
            var s = It;
            if (s._avatars.TryGetValue(url, out Sprite hit)) { done(hit); return; }
            if (s._avatarWait.ContainsKey(url)) { s._avatarWait[url] += done; return; }
            s._avatarWait[url] = done;
            s.StartCoroutine(s.FetchAvatar(url));
        }

        IEnumerator FetchAvatar(string url)
        {
            Sprite sprite = null;
            using (var req = UnityWebRequestTexture.GetTexture(url, true))
            {
                req.timeout = CloudConfig.RequestTimeoutSec;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(req);
                    if (tex != null)
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
            }
            if (sprite != null) _avatars[url] = sprite;
            if (_avatarWait.TryGetValue(url, out Action<Sprite> wait))
            {
                _avatarWait.Remove(url);
                wait?.Invoke(sprite);
            }
        }
    }
}
