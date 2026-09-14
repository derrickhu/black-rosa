using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public static class HitFx
    {
        public const int Ink = 0;
        public const int Fire = 1;
        public const int Ice = 2;
        public const int Explode = 3;
        public const int Heavy = 4;
        public const int Stun = 5;
        public const int Kill = 6;
        public const int FireIce = 7;
        public const int Cleave = 8;
        public const int Knock = 9;
        public const int Arrow = 10;
        public const int Water = 11;
        public const int Earth = 12;
        public const int Wind = 13;
        public const int Thunder = 14;
        public const int Poison = 15;
        public const int Gold = 16;
        public const int Wood = 17;
        public const int Confuse = 18;

        // 有专属 4 帧的元素用自己那套，其余拿白色的 hit_ink 染色。
        public static string Frames(int kind)
        {
            switch (kind)
            {
                case Fire: return "fire_hit";
                case Ice: return "hit_ice";
                case Explode: return "hit_explode";
                case Heavy: return "hit_heavy";
                default: return "hit_ink";
            }
        }
    }

    // 命中的事件层，压在元素层上面。
    public static class HitEvent
    {
        public const int None = 0;
        public const int Kill = 1;
        public const int Stun = 2;
        public const int Push = 3;
        public const int Pierce = 4;

        public static string Art(int id)
        {
            switch (id)
            {
                case Kill: return "Vfx/kill_mark";
                case Stun: return "Vfx/stun_ring";
                case Pierce: return "Vfx/pierce_tip";
                default: return null;
            }
        }
    }

    public static class InkVfx
    {
        const int BurstPool = 16;
        static readonly List<InkBurst> _bursts = new List<InkBurst>();
        static readonly Dictionary<ShotFx, Sprite> Shots = new Dictionary<ShotFx, Sprite>();
        static Transform _root;

        public static void BindRoot(Transform root)
        {
            _root = root;
            _bursts.Clear();
        }

        public static void Ensure()
        {
            // 贴图按槽位按需载入，这里只保证根节点在。
            if (_root == null) RentBurst();
        }

        // 槽位换图就是换一行文件名；招牌两两没出图时回落到主元素那张。
        static string FileOf(ShotFx fx)
        {
            switch (fx)
            {
                case ShotFx.FormFire: return "Vfx/shot_fire";
                case ShotFx.FormIce: return "Vfx/shot_ice";
                case ShotFx.FormWater: return "Vfx/shot_water";
                case ShotFx.FormPoison: return "Vfx/shot_poison";
                case ShotFx.FormEarth: return "Vfx/shot_earth";
                case ShotFx.FormExplode: return "Vfx/shot_explode";
                case ShotFx.FormKill: return "Vfx/shot_kill";
                case ShotFx.FormCleave: return "Vfx/shot_cleave";
                case ShotFx.FormKnock: return "Vfx/shot_knock";
                case ShotFx.FormArrow: return "Vfx/shot_arrow";
                case ShotFx.FormPierce: return "Vfx/shot_pierce";
                case ShotFx.FormSplit: return "Vfx/shot_split";
                case ShotFx.FormFrostFire: return "Vfx/shot_frostfire";
                case ShotFx.FormScorchBolt: return "Vfx/shot_scorchbolt";
                case ShotFx.FormHailBolt: return "Vfx/shot_hailbolt";
                case ShotFx.FormBlightFire: return "Vfx/shot_blightfire";
                case ShotFx.FormConduct: return "Vfx/shot_conduct";
                case ShotFx.FormMoltenGold: return "Vfx/shot_moltengold";
                case ShotFx.FormWardGold: return "Vfx/shot_wardgold";
                case ShotFx.FormRotLife: return "Vfx/shot_rotlife";
                case ShotFx.FormRamEarth: return "Vfx/shot_ramearth";
                case ShotFx.FormColdWind: return "Vfx/shot_coldwind";
                case ShotFx.FormBlaze: return "Vfx/shot_blaze";
                case ShotFx.FormThunderCut: return "Vfx/shot_thundercut";
                case ShotFx.TrailTrack: return "Vfx/shot_track";
                case ShotFx.TrailAccel: return "Vfx/shot_accel";
                case ShotFx.HaloStun: return "Vfx/shot_stun";
                case ShotFx.BloomHeavy: return "Vfx/shot_heavy";
                default: return null;
            }
        }

        public static Sprite Shot(ShotFx fx)
        {
            if (fx == ShotFx.None) return null;
            if (Shots.TryGetValue(fx, out Sprite cached)) return cached;
            string file = FileOf(fx);
            // 黑底已由 docs/prompt/runtime/vfx_crush.py 离线压透明，直接用。
            Sprite s = file != null ? InkSprites.Load(file) : null;
            if (s == null)
            {
                ShotFx back = ShotLook.Fallback(fx);
                s = back != ShotFx.None && back != fx ? Shot(back) : null;
            }
            Shots[fx] = s;
            return s;
        }

        public static void Stop(SpriteRenderer sr)
        {
            if (sr == null) return;
            var flip = sr.GetComponent<InkFlip>();
            if (flip != null)
            {
                flip.Frames = null;
                flip.enabled = false;
            }
        }

        public static void PlayGlow(SpriteRenderer host, Color color, float scale)
        {
            if (host == null) return;
            Transform t = host.transform.Find("aura");
            SpriteRenderer sr = t != null ? t.GetComponent<SpriteRenderer>() : null;
            if (sr == null)
            {
                var go = new GameObject("aura");
                go.transform.SetParent(host.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one * scale;
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = host.sortingOrder - 1;
            }
            sr.sprite = InkFx.SoftDisc();
            Color c = color;
            c.a = 0.62f;
            InkFx.PaintSoft(sr, c);
            sr.enabled = true;
            var breath = sr.GetComponent<InkBreath>();
            if (breath == null) breath = sr.gameObject.AddComponent<InkBreath>();
            if (!breath.enabled) sr.transform.localScale = Vector3.one * scale;
            breath.Kick(0.62f, 0.16f, 4.8f);
        }

        public static void StopAura(SpriteRenderer host)
        {
            if (host == null) return;
            Transform t = host.transform.Find("aura");
            if (t == null) return;
            var sr = t.GetComponent<SpriteRenderer>();
            Stop(sr);
            var breath = t.GetComponent<InkBreath>();
            if (breath != null) breath.enabled = false;
            if (sr != null) sr.enabled = false;
        }

        static readonly Dictionary<string, Sprite[]> Sequences = new Dictionary<string, Sprite[]>();

        // 4 帧一组，缺帧就当这套图不存在，交给调用方回落。
        public static Sprite[] Frames(string baseName)
        {
            if (Sequences.TryGetValue(baseName, out Sprite[] cached)) return cached;
            var frames = new Sprite[4];
            bool any = false;
            for (int i = 0; i < 4; i++)
            {
                frames[i] = InkSprites.Load($"Vfx/{baseName}_{i:00}");
                any |= frames[i] != null;
            }
            if (!any) frames = null;
            Sequences[baseName] = frames;
            return frames;
        }

        public static void SpawnHit(FxBurst fx)
        {
            Ensure();
            RentBurst().Play(fx);
        }

        static InkBurst RentBurst()
        {
            if (_root == null)
            {
                var go = new GameObject("InkVfx");
                _root = go.transform;
            }
            for (int i = 0; i < _bursts.Count; i++)
            {
                if (_bursts[i] != null && !_bursts[i].Busy) return _bursts[i];
            }
            if (_bursts.Count >= BurstPool)
            {
                InkBurst oldest = _bursts[0];
                _bursts.RemoveAt(0);
                _bursts.Add(oldest);
                return oldest;
            }
            var spawned = new GameObject("burst");
            spawned.transform.SetParent(_root, false);
            var burst = spawned.AddComponent<InkBurst>();
            _bursts.Add(burst);
            return burst;
        }
    }
}
