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

        // 每种命中都有一套 v4 分层平涂四帧（hitv_<名>，画法对标 hit_explode）；
        // 缺图时退回旧图：有专属 4 帧的元素用自己那套，其余拿白色的 hit_ink 染色。
        public static string Frames(int kind)
        {
            string v4 = V4(kind);
            if (v4 != null && InkVfx.Frames(v4) != null) return v4;
            return Legacy(kind);
        }

        public static bool Flat(string key) => key.StartsWith("hitv_") || key == "hit_explode";

        static string V4(int kind)
        {
            switch (kind)
            {
                case Ink: return "hitv_ink";
                case Fire: return "hitv_fire";
                case Ice: return "hitv_ice";
                case Heavy: return "hitv_heavy";
                case Stun: return "hitv_stun";
                case Kill: return "hitv_kill";
                case FireIce: return "hitv_fireice";
                case Cleave: return "hitv_cleave";
                case Knock: return "hitv_knock";
                case Arrow: return "hitv_arrow";
                case Water: return "hitv_water";
                case Earth: return "hitv_earth";
                case Wind: return "hitv_wind";
                case Thunder: return "hitv_thunder";
                case Poison: return "hitv_poison";
                case Gold: return "hitv_gold";
                case Wood: return "hitv_wood";
                case Confuse: return "hitv_confuse";
                default: return null;
            }
        }

        static string Legacy(int kind)
        {
            switch (kind)
            {
                case Fire: return "fire_hit";
                case Ice: return "hit_ice";
                case Explode: return "hit_explode";
                case Heavy: return "hit_heavy";
                case Ink: return "hit_gold";
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

        // 平涂体：自带完整配色的帧动画，按原色直接画，不染色也不叠柔光。
        // 米色宣纸底上柔光会糊成一团雾，硬边深色外沿才立得住，所以这类图
        // 走普通混合、独占体槽。火先登记，其余字等风格定了再逐行加。
        public struct FlatBody
        {
            public string Frames;   // Vfx/<Frames>_NN
            public int Count;       // 总帧数
            public int Window;      // 一个星级循环几帧
            public int MaxStar;
            public float Sink;      // 弹心在精灵中心之后几个精灵高（图里球心偏上）
            public float Scale;
        }

        // Sink / Scale 都是 docs/prompt/runtime/vfx_flat_slice.py 量出来回填的，不要手调：
        // Sink 来自切图时弹头在窗口里的位置，Scale 是按「★1 弹头 0.139 世界单位」归一算的
        // —— 各元素弹头占精灵的比例差一倍多，只按精灵高归一会大小不一。
        public static bool Flat(ShotFx fx, out FlatBody body)
        {
            switch (fx)
            {
                case ShotFx.FormFire:    body = Ramp("fire_shot", 0.238f, 1.00f); return true;
                case ShotFx.FormIce:     body = Ramp("ice_shot", 0.196f, 1.21f); return true;
                case ShotFx.FormWater:   body = Ramp("water_shot", 0.019f, 0.77f); return true;
                case ShotFx.FormPoison:  body = Ramp("poison_shot", 0.233f, 0.77f); return true;
                case ShotFx.FormEarth:   body = Ramp("earth_shot", 0.116f, 0.80f); return true;
                case ShotFx.FormExplode: body = Ramp("explode_shot", 0.007f, 0.90f); return true;
                case ShotFx.FormThunder: body = Ramp("thunder_shot", 0.193f, 0.61f); return true;
                case ShotFx.FormWind:    body = Ramp("wind_shot", -0.002f, 0.45f); return true;

                // 道族 / 词组：墨黑骨白，不吃星级渐变（星改的是威力和动词，不是热度），
                // 所以只有一帧。Scale 是按旧柔光图的实际可见高折算的 —— 新图是紧裁的，
                // 照旧的 FormScale 给会凭空变大一圈。
                case ShotFx.FormKill:   body = One("kill_shot", 1.04f); return true;
                case ShotFx.FormCleave: body = One("cleave_shot", 1.04f); return true;
                case ShotFx.FormKnock:  body = One("knock_shot", 0.75f); return true;
                case ShotFx.FormArrow:  body = One("arrow_shot", 1.04f); return true;
                case ShotFx.FormPierce: body = One("pierce_shot", 1.00f); return true;
                // 秤砣图还在，但重不再换弹体，只走拖尾。这帧留着，免得哪天又要挂回去。
                case ShotFx.FormHeavy:  body = One("heavy_shot", 0.85f); return true;
                // 分没有体槽图，也不该有：它分成两发子弹本身就说清了。
                // `FormRank` 里也没有 `CardId.Split`，所以 `FormSplit` 根本选不到。

                // 12 张招牌两两。每对 4 帧无级循环 —— 星级不改它的形，
                // 所以不走星级窗口。尺寸按「弹头 0.20 世界单位」归一，比单元素
                // ★3 的 0.253：叠出招牌绝不能反而变小。
                case ShotFx.FormFrostFire:  body = Pair("frostfire_shot", -0.030f, 0.70f); return true;
                case ShotFx.FormScorchBolt: body = Pair("scorchbolt_shot", 0.003f, 0.76f); return true;
                case ShotFx.FormHailBolt:   body = Pair("hailbolt_shot", -0.069f, 0.96f); return true;
                case ShotFx.FormBlightFire: body = Pair("blightfire_shot", -0.040f, 0.91f); return true;
                case ShotFx.FormConduct:    body = Pair("conduct_shot", -0.008f, 0.67f); return true;
                case ShotFx.FormMoltenGold: body = Pair("moltengold_shot", 0.108f, 0.79f); return true;
                case ShotFx.FormWardGold:   body = Pair("wardgold_shot", 0.076f, 0.77f); return true;
                case ShotFx.FormRotLife:    body = Pair("rotlife_shot", 0.099f, 0.79f); return true;
                case ShotFx.FormRamEarth:   body = Pair("ramearth_shot", 0.109f, 0.70f); return true;
                case ShotFx.FormColdWind:   body = Pair("coldwind_shot", 0.087f, 0.61f); return true;
                case ShotFx.FormBlaze:      body = Pair("blaze_shot", -0.113f, 0.90f); return true;
                case ShotFx.FormThunderCut: body = Pair("thundercut_shot", 0.103f, 1.06f); return true;

                default:
                    body = default;
                    return false;
            }
        }

        static FlatBody Ramp(string frames, float sink, float scale) => new FlatBody
        {
            Frames = frames, Count = 8, Window = 3, MaxStar = 3, Sink = sink, Scale = scale
        };

        // 单帧弹体：Sink 给 0，精灵按自己的包围盒正中压在子弹位置上。
        static FlatBody One(string frames, float scale) => new FlatBody
        {
            Frames = frames, Count = 1, Window = 1, MaxStar = 3, Sink = 0f, Scale = scale
        };

        // 招牌两两：Window == Count，星级不参与，四帧一直循环。
        static FlatBody Pair(string frames, float sink, float scale) => new FlatBody
        {
            Frames = frames, Count = 4, Window = 4, MaxStar = 3, Sink = sink, Scale = scale
        };

        // 八帧是一条「越往后越大越红」的单调渐变，星级决定窗口落在渐变的哪一段：
        // ★1 播 00–02，★2 播 03–05，★3 播 05–07。窗口内相邻帧只差一点，
        // 所以每个星级都能平滑循环；整段 0–7 一起播会让高星缩回低星那几帧。
        // id 拿来错开相位，同屏一片弹才不会齐刷刷地闪。
        public static Sprite FlatFrame(in FlatBody body, int star, int id)
        {
            Sprite[] frames = Frames(body.Frames, body.Count);
            if (frames == null) return null;
            int window = Mathf.Clamp(body.Window, 1, body.Count);
            int top = Mathf.Max(2, body.MaxStar);
            float k = Mathf.Clamp01((star - 1) / (float)(top - 1));
            int start = Mathf.FloorToInt(k * (body.Count - window) + 0.5f);
            int step = Mathf.FloorToInt(Time.unscaledTime * 13f + id * 0.41f);
            int at = start + ((step % window) + window) % window;
            for (int i = 0; i < body.Count; i++)
            {
                Sprite s = frames[(at + i) % body.Count];
                if (s != null) return s;
            }
            return null;
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

        // 默认 4 帧一组，缺帧就当这套图不存在，交给调用方回落。
        public static Sprite[] Frames(string baseName) => Frames(baseName, 4);

        public static Sprite[] Frames(string baseName, int count)
        {
            string key = count == 4 ? baseName : baseName + "#" + count;
            if (Sequences.TryGetValue(key, out Sprite[] cached)) return cached;
            var frames = new Sprite[count];
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                frames[i] = InkSprites.Load($"Vfx/{baseName}_{i:00}");
                any |= frames[i] != null;
            }
            if (!any) frames = null;
            Sequences[key] = frames;
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
