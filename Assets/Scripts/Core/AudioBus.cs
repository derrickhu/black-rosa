using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 整套声音都是「湿墨落在宣纸上」。
    // 按钮是纸页轻响，落子是印章，命中是一滴墨。音乐若在 Resources/Audio 里就循环，没有就静音。
    public static class AudioBus
    {
        const float MusicHome = 0.26f;
        const float MusicBattle = 0.18f;

        static AudioSource _music;
        static AudioSource[] _voices;
        static int _next;
        static string _musicName = "";
        static bool _duck;
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> _readyAt = new Dictionary<string, float>();

        public static void Tap() => Cue("ui_tap", 0.72f, 0.03f, 0.035f);
        public static void Back() => Cue("ui_back", 0.7f, 0.05f, 0.02f);
        public static void Deny() => Cue("ui_deny", 0.62f, 0.08f, 0f);
        public static void Stamp() => Cue("stamp", 0.9f, 0.06f, 0.02f);
        public static void Chime() => Cue("chime", 0.82f, 0.12f, 0f);
        public static void Draft() => Cue("draft", 0.75f, 0.2f, 0f);
        // 发弹是全场最密的声音，必须比命中轻一截，否则命中被它淹掉。
        public static void Shot(float pitch) => Cue("shot", 0.3f, 0.07f, 0.05f, pitch);
        // 命中两层：上面是墨点的瞬态，下面垫一声低频的闷响，才有「砸进去」的身体。
        public static void Hit()
        {
            Cue("hit", 0.95f, 0.04f, 0.08f);
            Cue("hit_thud", 0.8f, 0.05f, 0.06f);
        }

        public static void HitFire()
        {
            Cue("hit_fire", 0.9f, 0.05f, 0.05f);
            Cue("hit_thud", 0.72f, 0.05f, 0.06f);
        }

        public static void HitIce()
        {
            Cue("hit_ice", 0.9f, 0.05f, 0.04f);
            Cue("hit_thud", 0.66f, 0.05f, 0.06f, 1.1f);
        }

        public static void Boom()
        {
            Cue("boom", 1f, 0.07f, 0.03f);
            Cue("hit_thud", 1f, 0.07f, 0.03f, 0.78f);
            Cue("hit", 0.6f, 0.07f, 0.03f, 0.7f);
        }

        // 连杀：0.7 秒内每多杀一只，爆破音按五声音阶往上走一格，最多一个八度。
        // 一波清屏会听成一串上行的「啵啵啵」，这是全局最解压的一下。
        static readonly int[] StreakSteps = { 0, 2, 4, 7, 9, 12 };
        const float StreakWindow = 0.7f;
        static int _streak;
        static float _lastKill = -10f;

        public static void Kill()
        {
            float now = Time.unscaledTime;
            _streak = now - _lastKill < StreakWindow ? Mathf.Min(_streak + 1, StreakSteps.Length - 1) : 0;
            _lastKill = now;
            float pitch = Mathf.Pow(2f, StreakSteps[_streak] / 12f);
            Cue("kill_pop", 1f, 0.035f, 0f, pitch);
            Cue("kill", 0.6f, 0.1f, 0.03f);
        }

        public static void Boss()
        {
            Cue("boss", 1f, 0.4f, 0f);
            Cue("hit_thud", 1f, 0.4f, 0f, 0.7f);
        }
        public static void Leak() => Cue("leak", 0.86f, 0.2f, 0f);
        public static void Pickup() => Cue("pickup", 0.58f, 0.07f, 0.05f);
        public static void Spell(float pitch) => Cue("spell", 0.86f, 0.18f, 0f, pitch);
        public static void Win() => Cue("win", 0.9f, 0.4f, 0f);
        public static void Lose() => Cue("lose", 0.85f, 0.4f, 0f);

        public static void Music(string name)
        {
            if (name == _musicName) return;
            _musicName = name;
            Ensure();
            AudioClip clip = Clip(name);
            if (clip == null)
            {
                _music.Stop();
                _music.clip = null;
                return;
            }
            _music.clip = clip;
            _music.loop = true;
            _music.volume = 0f;
            _music.Play();
        }

        public static void Duck(bool on) => _duck = on;

        public static void Tick()
        {
            if (_music == null || _music.clip == null) return;
            float bed = _musicName == "bgm_battle" ? MusicBattle : MusicHome;
            float target = bed * (_duck ? 0.4f : 1f);
            _music.volume = Mathf.MoveTowards(_music.volume, target, Time.unscaledDeltaTime * 0.7f);
        }

        public static void Sweep(Transform root)
        {
            if (root == null) return;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].GetComponent<AudioTap>() != null) continue;
                buttons[i].gameObject.AddComponent<AudioTap>();
            }
        }

        static void Cue(string name, float volume, float gap, float jitter, float pitch = 1f)
        {
            float now = Time.unscaledTime;
            if (_readyAt.TryGetValue(name, out float at) && now < at) return;
            _readyAt[name] = now + gap;
            AudioClip clip = Clip(name);
            if (clip == null) return;
            Ensure();
            AudioSource src = Voice();
            src.pitch = pitch + (jitter > 0f ? Random.Range(-jitter, jitter) : 0f);
            src.PlayOneShot(clip, volume);
        }

        static AudioClip Clip(string name)
        {
            if (_clips.TryGetValue(name, out AudioClip cached)) return cached;
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
            _clips[name] = clip;
            return clip;
        }

        static void Ensure()
        {
            if (_voices != null) return;
            var go = new GameObject("Audio");
            Object.DontDestroyOnLoad(go);
            _music = go.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _voices = new AudioSource[20];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = go.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
                _voices[i].spatialBlend = 0f;
            }
        }

        static AudioSource Voice()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                int idx = (_next + i) % _voices.Length;
                if (!_voices[idx].isPlaying)
                {
                    _next = (idx + 1) % _voices.Length;
                    return _voices[idx];
                }
            }
            AudioSource busy = _voices[_next];
            _next = (_next + 1) % _voices.Length;
            busy.Stop();
            return busy;
        }
    }
}
