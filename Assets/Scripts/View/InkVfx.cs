using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public static class InkVfx
    {
        public static Sprite[] FireHeap;
        public static Sprite[] FireShot;
        public static Sprite[] IceShot;
        public static Sprite[] FireIceShot;
        public static Sprite[] HeavyShot;
        public static Sprite[] TrackShot;
        public static Sprite[] FireHit;

        const int HitPool = 16;
        static readonly List<SpriteRenderer> _hits = new List<SpriteRenderer>();
        static readonly Sprite[][] _heapTier = new Sprite[4][];
        static readonly Sprite[][] _shotTier = new Sprite[4][];
        static readonly Sprite[][] _iceShotTier = new Sprite[4][];
        static readonly Sprite[][] _fireIceShotTier = new Sprite[4][];
        static readonly Sprite[][] _heavyShotTier = new Sprite[4][];
        static readonly Sprite[][] _trackShotTier = new Sprite[4][];
        static readonly Sprite[][] _trackHeap = new Sprite[4][];
        static readonly Sprite[][] _pierceHeap = new Sprite[4][];
        static Transform _root;
        static bool _loaded;

        public static void BindRoot(Transform root)
        {
            _root = root;
            _hits.Clear();
        }

        public static void Ensure()
        {
            if (_loaded) return;
            _loaded = true;
            FireHeap = LoadSeq("Vfx/fire_heap", 8);
            FireShot = LoadSeq("Vfx/fire_shot", 8);
            IceShot = LoadSeq("Vfx/ice_shot", 8);
            FireIceShot = LoadSeq("Vfx/fireice_shot", 8);
            HeavyShot = LoadSeq("Vfx/heavy_shot", 8);
            TrackShot = LoadSeq("Vfx/track_shot", 8);
            FireHit = LoadSeq("Vfx/fire_hit", 4);
            BuildTiers(_heapTier, FireHeap);
            BuildTiers(_shotTier, FireShot);
            BuildTiers(_iceShotTier, IceShot);
            BuildTiers(_fireIceShotTier, FireIceShot);
            BuildTiers(_heavyShotTier, HeavyShot);
            BuildTiers(_trackShotTier, TrackShot);
            for (int s = 1; s <= 3; s++)
            {
                _trackHeap[s] = LoadPingPong(
                    "heap_track_s" + s + "_l",
                    "heap_track_s" + s + "_c",
                    "heap_track_s" + s + "_r");
                _pierceHeap[s] = LoadPingPong(
                    "heap_pierce_s" + s + "_in",
                    "heap_pierce_s" + s + "_mid",
                    "heap_pierce_s" + s + "_out");
            }
        }

        public static bool HasFireHeap { get { Ensure(); return Ready(FireHeap); } }
        public static bool HasFireShot { get { Ensure(); return Ready(FireShot); } }
        public static bool HasIceShot { get { Ensure(); return Ready(IceShot); } }
        public static bool HasFireIceShot { get { Ensure(); return Ready(FireIceShot); } }
        public static bool HasHeavyShot { get { Ensure(); return Ready(HeavyShot); } }
        public static bool HasTrackShot { get { Ensure(); return Ready(TrackShot); } }
        public static bool HasFireHit { get { Ensure(); return Ready(FireHit); } }
        public static bool HasTrackHeap { get { Ensure(); return Ready(_trackHeap[1]); } }
        public static bool HasPierceHeap { get { Ensure(); return Ready(_pierceHeap[1]); } }

        public static Sprite[] HeapTier(int star) => Tier(_heapTier, FireHeap, star);
        public static Sprite[] ShotTier(int star) => Tier(_shotTier, FireShot, star);
        public static Sprite[] IceShotTier(int star) => Tier(_iceShotTier, IceShot, star);
        public static Sprite[] FireIceShotTier(int star) => Tier(_fireIceShotTier, FireIceShot, star);
        public static Sprite[] HeavyShotTier(int star) => Tier(_heavyShotTier, HeavyShot, star);

        public static bool TryBody(int fireStar, int iceStar, int heavyStar, float radius, out Sprite[] frames, out Sprite first, out float scale, out float fps)
        {
            Ensure();
            frames = null;
            first = null;
            scale = 1.15f;
            fps = 14f;
            bool fire = fireStar > 0;
            bool ice = iceStar > 0;
            int star = 1;
            if (fire && ice && HasFireIceShot)
            {
                star = Mathf.Max(1, Mathf.Max(fireStar, iceStar));
                frames = FireIceShotTier(star);
                first = FireIceShot[0];
                fps = 12f;
            }
            else if (fire && HasFireShot)
            {
                star = Mathf.Max(1, fireStar);
                frames = ShotTier(star);
                first = FireShot[0];
            }
            else if (ice && HasIceShot)
            {
                star = Mathf.Max(1, iceStar);
                frames = IceShotTier(star);
                first = IceShot[0];
                fps = 11f;
            }
            else if (HasHeavyShot)
            {
                star = Mathf.Max(1, heavyStar);
                frames = HeavyShotTier(star);
                first = HeavyShot[0];
            }
            else return false;
            if (frames == null || first == null) return false;
            scale = star >= 3 ? 1.28f : star >= 2 ? 1.2f : 1.15f;
            scale *= Mathf.Clamp(radius / 0.11f, 0.7f, 2.8f);
            if (heavyStar > 0) scale *= 1.08f;
            return true;
        }
        public static Sprite[] TrackShotTier(int star) => Tier(_trackShotTier, TrackShot, star);
        public static Sprite[] TrackHeap(int star) => StarAnim(_trackHeap, star);
        public static Sprite[] PierceHeap(int star) => StarAnim(_pierceHeap, star);

        public static void PlayLoop(SpriteRenderer sr, Sprite[] frames, float fps, int phase)
        {
            if (sr == null || frames == null || frames.Length == 0) return;
            var flip = sr.GetComponent<InkFlip>();
            if (flip == null) flip = sr.gameObject.AddComponent<InkFlip>();
            if (flip.Frames != frames)
            {
                flip.Frames = frames;
                flip.Fps = fps;
                flip.Loop = true;
                flip.Phase = phase * 0.41f;
                flip.OnDone = null;
                flip.Restart();
            }
            sr.enabled = true;
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

        public static void SpawnHit(Vector2 pos, float scale, Color tint)
        {
            Ensure();
            if (!HasFireHit) return;
            SpriteRenderer sr = RentHit();
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.transform.localScale = Vector3.one * scale;
            sr.color = tint.a < 0.02f ? Color.white : tint;
            sr.enabled = true;
            var flip = sr.GetComponent<InkFlip>();
            if (flip == null) flip = sr.gameObject.AddComponent<InkFlip>();
            flip.Frames = FireHit;
            flip.Fps = 18f;
            flip.Loop = false;
            flip.Phase = 0f;
            flip.OnDone = () => { if (sr != null) sr.enabled = false; };
            flip.Restart();
        }

        static Sprite[] Tier(Sprite[][] cache, Sprite[] src, int star)
        {
            Ensure();
            int s = Mathf.Clamp(star, 1, 3);
            if (cache[s] != null && cache[s].Length > 0) return cache[s];
            return src;
        }

        static void BuildTiers(Sprite[][] cache, Sprite[] src)
        {
            if (src == null || src.Length == 0) return;
            int n = 0;
            for (int i = 0; i < src.Length; i++) if (src[i] != null) n = i + 1;
            if (n <= 4)
            {
                cache[1] = cache[2] = cache[3] = Slice(src, 0, n);
                return;
            }
            cache[1] = Slice(src, 0, 4);
            cache[2] = Slice(src, 2, 4);
            cache[3] = Slice(src, 4, Mathf.Min(4, n - 4));
        }

        static Sprite[] Slice(Sprite[] src, int start, int len)
        {
            var a = new Sprite[len];
            for (int i = 0; i < len; i++)
            {
                int idx = Mathf.Clamp(start + i, 0, src.Length - 1);
                a[i] = src[idx];
            }
            return a;
        }

        static SpriteRenderer RentHit()
        {
            if (_root == null)
            {
                var go = new GameObject("InkVfx");
                _root = go.transform;
            }
            for (int i = 0; i < _hits.Count; i++)
            {
                if (_hits[i] != null && !_hits[i].enabled) return _hits[i];
            }
            if (_hits.Count >= HitPool)
            {
                SpriteRenderer oldest = _hits[0];
                _hits.RemoveAt(0);
                _hits.Add(oldest);
                return oldest;
            }
            var spawned = new GameObject("hit");
            spawned.transform.SetParent(_root, false);
            var sr = spawned.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;
            spawned.AddComponent<InkFlip>();
            _hits.Add(sr);
            return sr;
        }

        static bool Ready(Sprite[] frames) => frames != null && frames.Length > 0 && frames[0] != null;

        static Sprite[] StarAnim(Sprite[][] cache, int star)
        {
            Ensure();
            int s = Mathf.Clamp(star, 1, 3);
            if (Ready(cache[s])) return cache[s];
            if (Ready(cache[1])) return cache[1];
            return null;
        }

        static Sprite[] LoadPingPong(string a, string b, string c)
        {
            Sprite sa = InkSprites.Load(a);
            Sprite sb = InkSprites.Load(b);
            Sprite sc = InkSprites.Load(c);
            if (sa == null || sb == null || sc == null) return null;
            return new[] { sa, sb, sc, sb };
        }

        static Sprite[] LoadSeq(string prefix, int n)
        {
            var frames = new Sprite[n];
            int got = 0;
            for (int i = 0; i < n; i++)
            {
                frames[i] = InkSprites.Load(prefix + "_" + i.ToString("00"));
                if (frames[i] != null) got++;
            }
            return got > 0 ? frames : null;
        }
    }
}
