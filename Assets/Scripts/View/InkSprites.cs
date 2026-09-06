using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public static class InkSprites
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Cannon() => Load("cannon");

        public static Sprite Person(EnemyId id)
        {
            switch (id)
            {
                case EnemyId.Runner: return Load("runner");
                case EnemyId.Shield: return Load("shield");
                case EnemyId.Swarm: return Load("swarm");
                case EnemyId.Strafer: return Load("strafer");
                case EnemyId.Elite: return Load("elite");
                case EnemyId.Boss: return Load("boss");
                default: return Load("walker");
            }
        }

        public static string HeapKey(CardId id)
        {
            switch (id)
            {
                case CardId.Split: return "split";
                case CardId.Fire: return "fire";
                case CardId.Ice: return "ice";
                case CardId.Track: return "track";
                case CardId.Pierce: return "pierce";
                case CardId.Explode: return "explode";
                case CardId.Accel: return "accel";
                case CardId.Heavy: return "heavy";
                default: return "fire";
            }
        }

        public static Sprite Heap(CardId id, int star = 1)
        {
            string key = HeapKey(id);
            Sprite art = Load("heap_" + key);
            if (art == null)
            {
                int s = Mathf.Clamp(star, 1, 3);
                art = Load("heap_" + key + "_s" + s);
                if (art == null && id == CardId.Track) art = Load("heap_track_s" + s + "_c");
                if (art == null && id == CardId.Pierce) art = Load("heap_pierce_s" + s + "_mid");
            }
            return art;
        }

        public static Sprite Icon(InkShape shape)
        {
            switch (shape)
            {
                case InkShape.Star: return Load("icon_star");
                case InkShape.Coin: return Load("icon_coin");
                case InkShape.Lock: return Load("icon_lock");
                case InkShape.Heart: return Load("icon_heart");
                case InkShape.Diamond: return Load("icon_diamond");
                case InkShape.Cannon: return Load("icon_cannon") ?? Load("cannon");
                default: return null;
            }
        }

        public static Sprite Die() => Load("icon_die");

        public static Sprite Flash(EnemyId id)
        {
            string key = "flash:" + id;
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Sprite src = Person(id);
            if (src == null || src.texture == null) return src;
            Texture2D tex = src.texture;
            Color[] px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color(1f, 1f, 1f, px[i].a);
            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            copy.filterMode = tex.filterMode;
            copy.wrapMode = TextureWrapMode.Clamp;
            copy.SetPixels(px);
            copy.Apply(false, false);
            cached = Sprite.Create(copy, new Rect(0, 0, copy.width, copy.height), new Vector2(0.5f, 0.5f), Mathf.Max(copy.height, 64f));
            Cache[key] = cached;
            return cached;
        }

        public static Sprite Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Cache.TryGetValue(name, out Sprite cached) && cached != null) return cached;
            Texture2D tex = Resources.Load<Texture2D>("Art/" + name);
            Sprite sprite = null;
            if (tex != null)
            {
                float ppu = Mathf.Max(tex.height, 64f);
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            }
            if (sprite == null) sprite = Resources.Load<Sprite>("Art/" + name);
            Cache[name] = sprite;
            return sprite;
        }
    }
}
