using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 结算页这类「一段编排好的演出」用的轻量补间。挂在面板根节点上，面板销毁就一起停。
    // 全部走 unscaled time：结算时战场已暂停，timeScale 也可能被别处动过。
    // 每条轨都有起点和时长；Finish() 把钟拨到最后一条轨结束，玩家点一下就能跳过演出。
    public sealed class UiAnim : MonoBehaviour
    {
        sealed class Track
        {
            public float Start;
            public float Dur;
            public bool Loop;
            public bool Done;
            public Action<float> Apply;   // 0..1；循环轨传的是已过秒数
        }

        readonly List<Track> _tracks = new List<Track>();
        float _born = -1f;
        float _skip;

        public float Now => _born < 0f ? 0f : Time.unscaledTime - _born + _skip;

        public static UiAnim On(Component host)
        {
            var a = host.GetComponent<UiAnim>();
            return a != null ? a : host.gameObject.AddComponent<UiAnim>();
        }

        UiAnim Add(float delay, float dur, Action<float> apply, bool loop = false)
        {
            if (_born < 0f) _born = Time.unscaledTime;
            var t = new Track { Start = Now + delay, Dur = Mathf.Max(0.0001f, dur), Apply = apply, Loop = loop };
            _tracks.Add(t);
            if (!loop) apply(0f);
            return this;
        }

        public UiAnim At(float delay, Action act) => Add(delay, 0.0001f, k => { if (k >= 1f) act(); });

        // 自定义轨，k 走 0..1（未缓动）。
        public UiAnim Tween(float delay, float dur, Action<float> apply) => Add(delay, dur, apply);

        // 从 from 倍回弹到 1。from=0 时开演前就是看不见的。
        public UiAnim Pop(Transform t, float delay, float dur = 0.38f, float from = 0f)
        {
            return Add(delay, dur, k =>
            {
                if (t == null) return;
                float s = Mathf.LerpUnclamped(from, 1f, Ease.OutBack(k));
                t.localScale = new Vector3(s, s, 1f);
            });
        }

        public UiAnim Fade(CanvasGroup g, float delay, float dur, float from, float to)
        {
            return Add(delay, dur, k => { if (g != null) g.alpha = Mathf.Lerp(from, to, Ease.OutCubic(k)); });
        }

        public UiAnim Fade(Graphic g, float delay, float dur, float from, float to)
        {
            return Add(delay, dur, k =>
            {
                if (g == null) return;
                Color c = g.color;
                c.a = Mathf.Lerp(from, to, Ease.OutCubic(k));
                g.color = c;
            });
        }

        public UiAnim Move(RectTransform t, Vector2 from, Vector2 to, float delay, float dur, Func<float, float> ease = null)
        {
            ease = ease ?? Ease.OutCubic;
            return Add(delay, dur, k => { if (t != null) t.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(k)); });
        }

        public UiAnim Rotate(Transform t, float from, float to, float delay, float dur, Func<float, float> ease = null)
        {
            ease = ease ?? Ease.OutCubic;
            return Add(delay, dur, k => { if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(from, to, ease(k))); });
        }

        // 砸下来那一下：先压扁再弹回，幅度逐次减小。
        public UiAnim Punch(Transform t, float delay, float amp = 0.18f, float dur = 0.32f)
        {
            return Add(delay, dur, k =>
            {
                if (t == null) return;
                float w = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * amp;
                t.localScale = new Vector3(1f + w, 1f - w, 1f);
            });
        }

        // 整块左右抖。给震屏用，抖的是面板根而不是摄像机。
        public UiAnim Shake(RectTransform t, float delay, float amp = 14f, float dur = 0.3f)
        {
            Vector2 home = t.anchoredPosition;
            return Add(delay, dur, k =>
            {
                if (t == null) return;
                float w = (1f - k) * amp;
                t.anchoredPosition = home + new Vector2(Mathf.Sin(k * 60f) * w, Mathf.Cos(k * 47f) * w * 0.6f);
            });
        }

        public UiAnim Spin(Transform t, float degPerSec)
        {
            return Add(0f, 1f, s => { if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, s * degPerSec); }, true);
        }

        // 主按钮的呼吸：轻轻一涨一缩，勾着人去点。
        public UiAnim Breathe(Transform t, float delay, float amp = 0.05f, float hz = 1.4f)
        {
            float start = Now + delay;
            return Add(0f, 1f, s =>
            {
                if (t == null || s < start) return;
                float k = 1f + amp * Mathf.Sin((s - start) * hz * Mathf.PI * 2f);
                t.localScale = new Vector3(k, k, 1f);
            }, true);
        }

        // 数字滚动。每跳一个新值回调一次，外面拿去放「嗒」声。
        public UiAnim CountUp(Text t, int from, int to, float delay, float dur, string format, Action tick = null)
        {
            int last = int.MinValue;
            return Add(delay, dur, k =>
            {
                if (t == null) return;
                int v = Mathf.RoundToInt(Mathf.Lerp(from, to, Ease.OutCubic(k)));
                if (v == last) return;
                last = v;
                t.text = string.Format(format, v);
                if (k > 0f && tick != null) tick();
            });
        }

        // 跳过演出：所有还没结束的非循环轨直接落到终态，未触发的 At 按顺序补上。
        public void Finish()
        {
            // At 里还可能再排新轨，拨几轮直到全部落定。
            for (int round = 0; round < 6 && Playing; round++)
            {
                float end = Now;
                for (int i = 0; i < _tracks.Count; i++)
                    if (!_tracks[i].Loop) end = Mathf.Max(end, _tracks[i].Start + _tracks[i].Dur);
                _skip += end - Now + 0.001f;
                Update();
            }
        }

        // 整页重绑时用：循环轨也一起丢掉，缩放由调用方复位。
        public void Clear() => _tracks.Clear();

        public bool Playing
        {
            get
            {
                float now = Now;
                for (int i = 0; i < _tracks.Count; i++)
                    if (!_tracks[i].Loop && !_tracks[i].Done && now < _tracks[i].Start + _tracks[i].Dur) return true;
                return false;
            }
        }

        void Update()
        {
            float now = Now;
            // At 回调里可能再加轨，所以按下标走、不用 foreach。
            for (int i = 0; i < _tracks.Count; i++)
            {
                Track t = _tracks[i];
                if (t.Done) continue;
                if (t.Loop)
                {
                    t.Apply(now);
                    continue;
                }
                if (now < t.Start) continue;
                float k = Mathf.Clamp01((now - t.Start) / t.Dur);
                t.Apply(k);
                if (k >= 1f) t.Done = true;
            }
        }
    }

    public static class Ease
    {
        public static float OutCubic(float k)
        {
            k = 1f - k;
            return 1f - k * k * k;
        }

        public static float OutBack(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        public static float OutBounce(float k)
        {
            const float n = 7.5625f, d = 2.75f;
            if (k < 1f / d) return n * k * k;
            if (k < 2f / d) { k -= 1.5f / d; return n * k * k + 0.75f; }
            if (k < 2.5f / d) { k -= 2.25f / d; return n * k * k + 0.9375f; }
            k -= 2.625f / d;
            return n * k * k + 0.984375f;
        }

        public static float InQuad(float k) => k * k;
    }
}
