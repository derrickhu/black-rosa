using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace InkLine
{
    // 经分上报。事件格式对齐 @gp/analytics-sdk，POST 到共用云函数 analytics-ingest。
    // 登录拿到 userId 之后才打 session_start，避免同一次启动被算成两个用户。
    public static class Analytics
    {
        const string Endpoint = CloudConfig.BaseUrl + "/analytics-ingest/track";
        const string AnonKey = "blackrosa_ga_anon";
        const string QueueKey = "blackrosa_ga_queue";
        const string SdkVersion = "0.1.0-cs";
        const int Bulk = 20;
        const int MaxQueue = 200;

        static bool _ready;
        static bool _sessionStarted;
        static bool _sending;
        static bool _hideHooked;
        static string _userId = "";
        static string _anonId;
        static string _sessionId;
        static int _seq;
        static int _levelId;
        static string _levelName = "";
        static float _levelAt;
        static bool _levelOpen;
        static float _lastEndAt = -10f;
        static readonly List<string> Queue = new List<string>();
        static Pump _pump;

        public static int LevelId => _levelOpen ? _levelId : 0;

        public static void Ensure()
        {
            if (_ready) return;
            _anonId = PlayerPrefs.GetString(AnonKey, "");
            if (string.IsNullOrEmpty(_anonId))
            {
                _anonId = NewId();
                PlayerPrefs.SetString(AnonKey, _anonId);
                PlayerPrefs.Save();
            }
            _sessionId = NewId();
            Restore();
            var go = new GameObject("Analytics");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _pump = go.AddComponent<Pump>();
            _hideHooked = WxBridge.OnHide(() => OnHide("app-hide"));
            _ready = true;
        }

        // 登录协程结束时调用。有 userId 会先打 login，再打 session_start。
        public static void BindUser(string userId)
        {
            Ensure();
            if (!string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(_userId))
            {
                _userId = userId;
                Track("login", new Param().Bool("from_anonymous", true));
                Flush();
            }
            if (_sessionStarted) return;
            _sessionStarted = true;
            Track("session_start", new Param().Str("entry", "main").Bool("with_user_id", !string.IsNullOrEmpty(_userId)));
            Flush();
        }

        public static void OnHide(string reason)
        {
            if (!_ready) return;
            if (Time.realtimeSinceStartup - _lastEndAt < 1f)
            {
                Flush();
                return;
            }
            _lastEndAt = Time.realtimeSinceStartup;
            if (_sessionStarted)
                Track("session_end", new Param().Str("reason", reason));
            Flush();
        }

        public static void LevelStart(int levelId, string name)
        {
            Ensure();
            if (_levelOpen) LevelFail("quit_to_home", 0);
            _levelOpen = true;
            _levelId = levelId;
            _levelName = name ?? "";
            _levelAt = Time.realtimeSinceStartup;
            Track("level_start", LevelParams(null, 0));
        }

        public static void LevelClear(int stars)
        {
            if (!_levelOpen) return;
            _levelOpen = false;
            Track("level_clear", LevelParams(null, 0).Num("stars", stars));
        }

        public static void LevelFail(string reason, int progressPct)
        {
            if (!_levelOpen) return;
            _levelOpen = false;
            Track("level_fail", LevelParams(reason, progressPct));
        }

        public static void AdRequest(string scene, string unit) => Ad("ad_request", scene, unit, null, null);
        public static void AdShow(string scene, string unit) => Ad("ad_show", scene, unit, null, null);

        public static void AdClose(string scene, string unit, bool ended)
        {
            Ad("ad_close", scene, unit, ended, null);
        }

        public static void AdError(string scene, string unit, string msg)
        {
            Ad("ad_error", scene, unit, null, string.IsNullOrEmpty(msg) ? "unknown" : msg);
        }

        static void Ad(string name, string scene, string unit, bool? ended, string err)
        {
            Ensure();
            Param p = new Param().Str("ad_unit_id", unit ?? "")
                .Str("ad_type", "reward")
                .Str("scene", string.IsNullOrEmpty(scene) ? "unknown" : scene);
            if (_levelOpen) p.Num("level_id", _levelId);
            if (ended.HasValue) p.Bool("is_ended", ended.Value);
            if (err != null) p.Str("err_msg", err.Length > 240 ? err.Substring(0, 240) : err);
            Track(name, p);
        }

        static Param LevelParams(string reason, int progressPct)
        {
            int duration = Mathf.Max(0, Mathf.RoundToInt((Time.realtimeSinceStartup - _levelAt) * 1000f));
            Param p = new Param().Num("level_id", _levelId).Str("level_name", _levelName).Str("mode", "stage");
            if (reason != null) p.Num("duration_ms", duration).Str("reason", reason).Num("progress_pct", progressPct);
            else if (!_levelOpen) p.Num("duration_ms", duration);
            return p;
        }

        static void Track(string name, Param param)
        {
            if (Application.isEditor || !_ready || string.IsNullOrEmpty(name)) return;
            _seq++;
            string json = Envelope(name, param);
            Queue.Add(json);
            while (Queue.Count > MaxQueue) Queue.RemoveAt(0);
            if (Queue.Count >= Bulk) Flush();
        }

        public static void Flush()
        {
            if (!_ready || _pump == null || _sending || Queue.Count == 0) return;
            Persist();
            _pump.StartCoroutine(Send());
        }

        static IEnumerator Send()
        {
            _sending = true;
            int n = Mathf.Min(Bulk, Queue.Count);
            var batch = new string[n];
            for (int i = 0; i < n; i++) batch[i] = Queue[i];
            var body = new StringBuilder(256 * n);
            body.Append("{\"batch\":[");
            for (int i = 0; i < n; i++)
            {
                if (i > 0) body.Append(',');
                body.Append(batch[i]);
            }
            body.Append("]}");

            long status = 0;
            using (var req = new UnityWebRequest(Endpoint, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString()));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = CloudConfig.RequestTimeoutSec;
                yield return req.SendWebRequest();
                status = req.responseCode;
            }

            bool clientErr = status >= 400 && status < 500;
            bool ok = status >= 200 && status < 300;
            if (ok || clientErr)
            {
                int drop = Mathf.Min(n, Queue.Count);
                Queue.RemoveRange(0, drop);
                if (clientErr) Debug.LogWarning("[Analytics] drop batch status=" + status);
            }
            Persist();
            _sending = false;
            if (ok && Queue.Count >= Bulk) Flush();
        }

        static string Envelope(string name, Param param)
        {
            string plat = WxBridge.IsMiniGame && !Application.isEditor ? "wechat" : "h5";
            string ver = Application.version;
            if (string.IsNullOrEmpty(ver)) ver = "1.0.0";
            var sb = new StringBuilder(512);
            sb.Append("{\"event_id\":").Append(Json(NewId()));
            sb.Append(",\"event_name\":").Append(Json(name));
            sb.Append(",\"event_ts\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"game_key\":").Append(Json(CloudConfig.GameKey));
            sb.Append(",\"app_version\":").Append(Json(ver));
            sb.Append(",\"sdk_version\":").Append(Json(SdkVersion));
            sb.Append(",\"platform\":").Append(Json(plat));
            sb.Append(",\"user_id\":").Append(Json(_userId ?? ""));
            sb.Append(",\"anonymous_id\":").Append(Json(_anonId));
            sb.Append(",\"session_id\":").Append(Json(_sessionId));
            sb.Append(",\"session_seq\":").Append(_seq.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"device\":{");
            sb.Append("\"brand\":\"\",\"model\":").Append(Json(SystemInfo.deviceModel));
            sb.Append(",\"system\":").Append(Json(SystemInfo.operatingSystem));
            sb.Append(",\"sdk_version\":\"\"");
            sb.Append(",\"screen_w\":").Append(Screen.width.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"screen_h\":").Append(Screen.height.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"network\":\"unknown\"}");
            sb.Append(",\"params\":").Append(param.Json());
            sb.Append('}');
            return sb.ToString();
        }

        static void Restore()
        {
            string raw = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(raw) || raw.Length < 2 || raw[0] != '[') return;
            int i = 1;
            while (i < raw.Length)
            {
                while (i < raw.Length && (raw[i] == ',' || raw[i] == ' ' || raw[i] == '\n')) i++;
                if (i >= raw.Length || raw[i] == ']') break;
                if (raw[i] != '{') break;
                int depth = 0;
                int start = i;
                bool str = false;
                bool esc = false;
                for (; i < raw.Length; i++)
                {
                    char c = raw[i];
                    if (str)
                    {
                        if (esc) esc = false;
                        else if (c == '\\') esc = true;
                        else if (c == '"') str = false;
                        continue;
                    }
                    if (c == '"') str = true;
                    else if (c == '{') depth++;
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            i++;
                            Queue.Add(raw.Substring(start, i - start));
                            break;
                        }
                    }
                }
            }
        }

        static void Persist()
        {
            int from = Mathf.Max(0, Queue.Count - 40);
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = from; i < Queue.Count; i++)
            {
                if (i > from) sb.Append(',');
                sb.Append(Queue[i]);
            }
            sb.Append(']');
            string raw = sb.ToString();
            if (raw.Length > 48000) raw = "[]";
            PlayerPrefs.SetString(QueueKey, raw);
            PlayerPrefs.Save();
        }

        static string NewId()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = Guid.NewGuid().ToByteArray();
            var sb = new StringBuilder(22);
            for (int i = 0; i < 22; i++) sb.Append(chars[bytes[i % bytes.Length] % chars.Length]);
            return sb.ToString();
        }

        static string Json(string v)
        {
            if (string.IsNullOrEmpty(v)) return "\"\"";
            var sb = new StringBuilder(v.Length + 2);
            sb.Append('"');
            for (int i = 0; i < v.Length && sb.Length < 500; i++)
            {
                char c = v[i];
                if (c == '\\' || c == '"') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c < 32) continue;
                else sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }

        sealed class Param
        {
            readonly StringBuilder _sb = new StringBuilder("{");
            bool _first = true;


            public Param Str(string key, string v) { Add(key); _sb.Append(Analytics.Json(v)); return this; }
            public Param Num(string key, int v) { Add(key); _sb.Append(v.ToString(CultureInfo.InvariantCulture)); return this; }
            public Param Bool(string key, bool v) { Add(key); _sb.Append(v ? "true" : "false"); return this; }

            void Add(string key)
            {
                if (!_first) _sb.Append(',');
                _first = false;
                _sb.Append(Analytics.Json(key)).Append(':');
            }

            public string Json()
            {
                _sb.Append('}');
                return _sb.ToString();
            }
        }

        sealed class Pump : MonoBehaviour
        {
            float _next;

            void Update()
            {
                if (Time.realtimeSinceStartup < _next) return;
                _next = Time.realtimeSinceStartup + 15f;
                Flush();
            }

            void OnApplicationPause(bool paused)
            {
                if (paused && !_hideHooked) OnHide("app-hide");
            }

            void OnApplicationQuit()
            {
                OnHide("quit");
            }
        }
    }
}
