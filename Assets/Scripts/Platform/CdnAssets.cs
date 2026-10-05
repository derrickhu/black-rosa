using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace InkLine
{
    // 大图和长音乐放在云存储，清单是 cdn_build.py 生成的 CdnManifest。
    // 图片：包里的 256 缩略图先顶上，高清下完再换；拉不到就一直用缩略图，不会空白。
    // 落盘缓存由微信 SDK 负责（URL 带 StreamingAssets），这里只管内存和并发。
    // 编辑器直接读 CdnArt/ 源文件，没上传也能看。
    public sealed class CdnAssets : MonoBehaviour
    {
        // 微信 WebGL1 上传贴图是同步的。一次解码三张大图，鸿蒙上单帧能到 600ms。
        const int MaxParallel = 1;
        const int TimeoutSec = 8;
        // 章节图 1024 宽、背景 720x1280，常驻 6 张大约 15MB；正在显示的不算在淘汰范围里。
        const int MaxTextures = 6;
        const float RetryAfter = 30f;

        static CdnAssets _runner;
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        static readonly List<string> Recent = new List<string>();
        static readonly Dictionary<string, List<Action>> Waiting = new Dictionary<string, List<Action>>();
        static readonly Dictionary<string, float> FailedAt = new Dictionary<string, float>();
        static readonly Queue<string> Pending = new Queue<string>();
        static readonly Dictionary<UnityEngine.Object, string> Bound = new Dictionary<UnityEngine.Object, string>();
        static int _running;

        public static bool Has(string name) => CdnManifest.Files.ContainsKey(name);

        public static string Url(string name)
        {
            if (!CdnManifest.Files.TryGetValue(name, out var f)) return null;
            if (Application.isEditor)
            {
                string local = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CdnArt", f.src));
                if (File.Exists(local)) return new Uri(local).AbsoluteUri;
            }
            return CdnManifest.BaseUrl + f.url;
        }

        public static void Bind(Image target, string name)
        {
            if (target == null) return;
            Bind(target, name, s => target.sprite = s);
        }

        public static void Bind(SpriteRenderer target, string name)
        {
            if (target == null) return;
            Bind(target, name, s => target.sprite = s);
        }

        // 先给能立刻拿到的（高清已在内存就直接用，否则缩略图），高清到了且目标还要这张时再换。
        static void Bind(UnityEngine.Object target, string name, Action<Sprite> set)
        {
            Bound[target] = name;
            Sprite now = Cached(name) ?? InkSprites.Load(name);
            if (now != null) set(now);
            if (!Has(name) || Cached(name) != null) return;
            Fetch(name, () =>
            {
                if (target == null || !Bound.TryGetValue(target, out string want) || want != name) return;
                Sprite hd = Cached(name);
                if (hd != null) set(hd);
            });
        }

        public static void Prefetch(string name)
        {
            if (Has(name) && Cached(name) == null && !name.StartsWith("Audio/", StringComparison.Ordinal))
                Fetch(name, null);
        }

        static Sprite Cached(string name)
        {
            if (!Sprites.TryGetValue(name, out Sprite s)) return null;
            Recent.Remove(name);
            Recent.Add(name);
            return s;
        }

        static void Fetch(string name, Action done)
        {
            if (FailedAt.TryGetValue(name, out float at) && Time.unscaledTime - at < RetryAfter) return;
            if (Waiting.TryGetValue(name, out var list))
            {
                if (done != null) list.Add(done);
                return;
            }
            Waiting[name] = new List<Action>();
            if (done != null) Waiting[name].Add(done);
            Pending.Enqueue(name);
            Pump();
        }

        static void Pump()
        {
            if (_runner == null)
            {
                var go = new GameObject("CdnAssets");
                DontDestroyOnLoad(go);
                _runner = go.AddComponent<CdnAssets>();
            }
            while (_running < MaxParallel && Pending.Count > 0)
            {
                _running++;
                _runner.StartCoroutine(Load(Pending.Dequeue()));
            }
        }

        static IEnumerator Load(string name)
        {
            // 微信播放器不带 UnityWebRequestAudioModule，长音乐不从这里下。
            string url = Url(name);
            UnityWebRequest req = UnityWebRequestTexture.GetTexture(url, true);
            req.timeout = TimeoutSec;
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            if (ok)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                ok = tex != null && tex.width > 8;
                if (ok)
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    Sprites[name] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 128f, 0, SpriteMeshType.FullRect);
                    Recent.Remove(name);
                    Recent.Add(name);
                    Trim();
                }
            }
            if (!ok)
            {
                FailedAt[name] = Time.unscaledTime;
                Debug.LogWarning($"[Cdn] {name} failed: {req.error}");
            }
            req.Dispose();

            _running--;
            if (Waiting.TryGetValue(name, out var list))
            {
                Waiting.Remove(name);
                if (ok)
                    for (int i = 0; i < list.Count; i++) list[i]();
            }
            Pump();
        }

        static void Trim()
        {
            for (int i = 0; i < Recent.Count && Sprites.Count > MaxTextures;)
            {
                string name = Recent[i];
                Sprite s = Sprites[name];
                if (InUse(s)) { i++; continue; }
                Recent.RemoveAt(i);
                Sprites.Remove(name);
                Destroy(s.texture);
                Destroy(s);
            }
        }

        static readonly List<UnityEngine.Object> Dead = new List<UnityEngine.Object>();

        static bool InUse(Sprite s)
        {
            bool used = false;
            Dead.Clear();
            foreach (var kv in Bound)
            {
                if (kv.Key == null) { Dead.Add(kv.Key); continue; }
                Sprite shown = kv.Key is Image img ? img.sprite
                    : kv.Key is SpriteRenderer sr ? sr.sprite : null;
                if (shown == s) used = true;
            }
            for (int i = 0; i < Dead.Count; i++) Bound.Remove(Dead[i]);
            return used;
        }
    }
}
