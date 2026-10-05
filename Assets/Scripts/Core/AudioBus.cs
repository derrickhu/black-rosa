using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 整套声音都是「湿墨落在宣纸上」。
    // 按钮是纸页轻响，落子是印章，命中是一滴墨。音乐若在 Resources/Audio 里就循环，没有就静音。
    // 在 CdnManifest 里的音乐（如 bgm_battle）从云上拉，没拉到之前也是静音。
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
        public static void BattleStart() => CueOr("battle_start", "draft", 1f, 0.5f, 1f);

        // 格子上的变化。一套软胶玩具的音色：落是闷弹一下，升星往上走，成词是一串铃。
        public static void CellDrop() => CueOr("cell_drop", "stamp", 0.95f, 0.05f, 1f);
        public static void CellUpgrade(int star) => CueOr("cell_upgrade", "chime", 0.9f, 0.08f, 1f + 0.12f * Mathf.Max(0, star - 2));
        public static void CellSwap() => CueOr("cell_swap", "stamp", 0.95f, 0.06f, 1f);
        public static void WordForm() => CueOr("word_form", "chime", 1f, 0.3f, 1f);
        public static void WordBreak() => CueOr("word_break", "ui_deny", 0.8f, 0.15f, 1f);
        // 蓄满一发就响一下，射速快的时候很密，压低音量、拉开间隔。
        public static void ChargeFire(bool word) => Cue("charge_fire", word ? 0.7f : 0.42f, word ? 0.12f : 0.09f, 0.04f, word ? 0.86f : 1.08f);
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

        // 雷的连带传导。没有专门的电声时拿冰的脆响拔高顶上。
        public static void Zap() => CueOr("hit_zap", "hit_ice", 0.55f, 0.1f, 1.35f);

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
        public static void ItemFire(float pitch) => CueOr("item_fire", "spell", 0.8f, 0.12f, pitch);
        // 每个道具一段专属音，从起手一路响到生效。没入库时退回通用的丢出声。
        public static void Item(ItemId id)
        {
            string name = "item_" + id.ToString().ToLowerInvariant();
            if (Clip(name) != null) Cue(name, 1f, 0.2f, 0f, 1f);
            else ItemFire(1f);
        }
        public static void ItemReady() => CueOr("item_ready", "chime", 0.55f, 0.3f, 1f);
        public static void ChestLand() => CueOr("chest_land", "stamp", 0.9f, 0.2f, 1f);
        public static void ChestUnlock() => CueOr("chest_unlock", "chime", 0.85f, 0.2f, 1f);
        public static void ChestOpen() => CueOr("chest_open", "res_unlock", 0.72f, 0.4f, 1f);
        public static void CardFlip(float pitch) => CueOr("card_flip", "ui_tap", 0.8f, 0.05f, pitch);
        public static void CardReveal(ItemQuality q)
        {
            if (q == ItemQuality.Purple) CueOr("card_rare", "chime", 1f, 0.2f, 1f);
            else if (q == ItemQuality.Blue) CueOr("card_blue", "card_flip", 0.95f, 0.08f, 1.05f);
            else CueOr("card_green", "card_flip", 0.9f, 0.06f, 1f);
        }
        public static void CardRare() => CardReveal(ItemQuality.Purple);
        public static void Win() => Cue("win", 0.9f, 0.4f, 0f);
        public static void Lose() => Cue("lose", 0.85f, 0.4f, 0f);

        // 结算页一套。新音效还没入库时退回相近的旧音效，别让演出变哑。
        public static void StarLand(int i) => CueOr("res_star", "stamp", 0.95f, 0.05f, 1f + i * 0.14f);
        public static void Confetti() => CueOr("res_confetti", "win", 0.85f, 0.4f, 1f);
        public static void CountTick() => CueOr("res_tick", "pickup", 0.42f, 0.055f, 1.15f);
        public static void Heartbeat() => CueOr("res_heart", "hit_thud", 0.8f, 0.35f, 0.8f);
        public static void Crumble() => CueOr("res_crumble", "boom", 0.9f, 0.3f, 0.85f);
        public static void UnlockSting() => CueOr("res_unlock", "chime", 0.9f, 0.3f, 1f);

        static void CueOr(string name, string fallback, float volume, float gap, float pitch)
        {
            Cue(Clip(name) != null ? name : fallback, volume, gap, 0f, pitch);
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(SfxKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        const string MusicKey = "inkline.audio.music";
        const string SfxKey = "inkline.audio.sfx";

        public static void Music(string name)
        {
            if (name == _musicName) return;
            _musicName = name;
            Ensure();
            StopStream();
            _music.Stop();
            _music.clip = null;
            if (CdnAssets.Has("Audio/" + name))
            {
                PlayRemote(name);
                return;
            }
            AudioClip clip = Clip(name);
            if (clip != null) PlayClip(clip);
        }

        static void PlayClip(AudioClip clip)
        {
            _music.clip = clip;
            _music.loop = true;
            _music.volume = 0f;
            _music.Play();
        }

        // 编辑器里由 CdnAudioHook 填上。微信包走 InnerAudioContext，不经过这里。
        public static System.Func<string, AudioClip> EditorMusic;

        // 云上的长音乐提前拉好（大厅里调），进战斗时就不用等。
        public static void Warm(string name)
        {
            if (!CdnAssets.Has("Audio/" + name)) return;
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (!Application.isEditor)
            {
                Stream(name);
                return;
            }
#endif
            if (EditorMusic != null) EditorMusic(name);
        }

        public static void Duck(bool on) => _duck = on;

        public static void Tick()
        {
            float bed = _musicName == "bgm_battle" ? MusicBattle : MusicHome;
            float target = MusicOn ? bed * (_duck ? 0.4f : 1f) : 0f;
            float step = Time.unscaledDeltaTime * 0.7f;
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (_stream != null && _streamOn)
            {
                _streamVol = Mathf.MoveTowards(_streamVol, target, step);
                _stream.volume = _streamVol;
            }
#endif
            if (_music == null || _music.clip == null) return;
            _music.volume = Mathf.MoveTowards(_music.volume, target, step);
        }

        static void PlayRemote(string name)
        {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
            if (!Application.isEditor)
            {
                _stream = Stream(name);
                _streamOn = true;
                _streamVol = 0f;
                _stream.volume = 0f;
                if (Ready.Contains(name)) _stream.Play();
                return;
            }
#endif
            AudioClip local = EditorMusic != null ? EditorMusic(name) : null;
            if (local != null && _musicName == name) PlayClip(local);
        }

#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
        // 微信里长音乐交给 InnerAudioContext：needDownload 让 SDK 整首下完落盘再播，下次秒开；
        // 切后台 / 来电打断后的续播 SDK 自己会做。每首只建一个，切走时暂停不销毁。
        static readonly Dictionary<string, WeChatWASM.WXInnerAudioContext> Streams =
            new Dictionary<string, WeChatWASM.WXInnerAudioContext>();
        static readonly HashSet<string> Ready = new HashSet<string>();
        static WeChatWASM.WXInnerAudioContext _stream;
        static bool _streamOn;
        static float _streamVol;

        static WeChatWASM.WXInnerAudioContext Stream(string name)
        {
            if (Streams.TryGetValue(name, out var ctx)) return ctx;
            ctx = WeChatWASM.WX.CreateInnerAudioContext(new WeChatWASM.InnerAudioContextParam
            {
                src = CdnAssets.Url("Audio/" + name),
                loop = true,
                volume = 0f,
                needDownload = true,
            });
            ctx.OnCanplay(() =>
            {
                Ready.Add(name);
                if (_musicName == name && _stream == ctx && _streamOn) ctx.Play();
            });
            ctx.OnError(e => Debug.LogWarning($"[Audio] {name} stream error {e.errCode}"));
            Streams[name] = ctx;
            return ctx;
        }

        static void StopStream()
        {
            if (_stream == null) return;
            _stream.Pause();
            _stream.volume = 0f;
            _stream = null;
            _streamOn = false;
        }
#else
        static void StopStream() { }
#endif

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

        static bool _unlocked;

        // 第一次按下去时把音效数据拉起来。微信里要等用户点过，WebAudio 才真正开，
        // 在那之前解码会失败，这一局就一直没声音。
        public static void Unlock()
        {
            if (_unlocked) return;
            _unlocked = true;
            AudioClip[] all = Resources.LoadAll<AudioClip>("Audio");
            for (int i = 0; i < all.Length; i++)
            {
                AudioClip clip = all[i];
                if (clip == null) continue;
                _clips[clip.name] = clip;
                if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            }
        }

        static void Cue(string name, float volume, float gap, float jitter, float pitch = 1f)
        {
            if (!SfxOn) return;
            float now = Time.unscaledTime;
            if (_readyAt.TryGetValue(name, out float at) && now < at) return;
            AudioClip clip = Clip(name);
            if (clip == null || clip.loadState != AudioDataLoadState.Loaded) return;
            _readyAt[name] = now + gap;
            Ensure();
            AudioSource src = Voice();
            src.pitch = pitch + (jitter > 0f ? Random.Range(-jitter, jitter) : 0f);
            src.PlayOneShot(clip, volume);
        }

        static AudioClip Clip(string name)
        {
            if (_clips.TryGetValue(name, out AudioClip cached) && cached != null) return cached;
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) return null;
            _clips[name] = clip;
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
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
