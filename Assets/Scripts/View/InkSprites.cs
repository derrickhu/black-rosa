using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace InkLine
{
    public static class InkSprites
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Cannon() => Load("cannon");

        // 和首页皮肤卡是同一门炮，只是炮口朝上，好坐在战场底边。
        public static Sprite CannonSkin(int skin)
        {
            return Load("cannon_" + SkinCatalog.Get(skin).Key) ?? Load("cannon_plain") ?? Cannon();
        }

        // 炮口朝上的开炮帧，CannonFire/<皮肤>_0 是静止，后面是一发的动作。没出图的皮肤返回 null。
        public static Sprite[] CannonFire(int skin)
        {
            string key = "fire:" + skin;
            if (FireCache.TryGetValue(key, out Sprite[] cached)) return cached;
            string name = "CannonFire/" + SkinCatalog.Get(skin).Key + "_";
            var list = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                Sprite s = Load(name + i);
                if (s == null) break;
                list.Add(s);
            }
            cached = list.Count > 1 ? list.ToArray() : null;
            FireCache[key] = cached;
            return cached;
        }

        static readonly Dictionary<string, Sprite[]> FireCache = new Dictionary<string, Sprite[]>();

        // 战斗里的换皮前缀。活动关设成 EventTheme，敌人先找 evt_walker 这种，没有再退回原图。
        // 离开战斗要清掉，图鉴里的敌人一直用原图。
        public const string EventTheme = "evt_";
        public static string EnemyTheme;

        public static Sprite Person(EnemyId id)
        {
            string key = PersonKey(id);
            if (!string.IsNullOrEmpty(EnemyTheme))
            {
                Sprite themed = Load(EnemyTheme + key);
                if (themed != null) return themed;
            }
            return Load(key);
        }

        static string PersonKey(EnemyId id)
        {
            switch (id)
            {
                case EnemyId.Runner: return "runner";
                case EnemyId.Shield: return "shield";
                case EnemyId.Swarm: return "swarm";
                case EnemyId.Strafer: return "strafer";
                case EnemyId.Chubby: return "chubby";
                case EnemyId.Tall: return "tall";
                case EnemyId.Ball: return "ball";
                case EnemyId.BigHead: return "bighead";
                case EnemyId.Belt: return "belt";
                case EnemyId.Crawler: return "crawler";
                case EnemyId.Splitter: return "splitter";
                case EnemyId.Sprinter: return "sprinter";
                case EnemyId.Mender: return "mender";
                case EnemyId.Bulwark: return "bulwark";
                case EnemyId.Elite: return "elite";
                case EnemyId.Warden: return "warden";
                // 关底一关一张图。文件名和 EnemyId 一一对应，加 boss 时两边一起加。
                case EnemyId.BossDrum: return "boss_drum";
                case EnemyId.BossInkbag: return "boss_inkbag";
                case EnemyId.BossIron: return "boss_iron";
                case EnemyId.BossTwin: return "boss_twin";
                case EnemyId.BossWarden: return "boss_warden";
                case EnemyId.BossThunder: return "boss_thunder";
                case EnemyId.BossMedic: return "boss_medic";
                case EnemyId.BossKing: return "boss_king";
                default: return "walker";
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
                case CardId.Stun: return "stun";
                case CardId.Sec: return "sec";
                case CardId.Kill: return "kill";
                case CardId.Myriad: return "myriad";
                case CardId.Arrow: return "arrow";
                case CardId.Strike: return "strike";
                case CardId.Back: return "back";
                case CardId.Link: return "link";
                case CardId.Slash: return "slash";
                // 新元素还没出字图，Load 返回 null 会退到程序化底 + TextMesh 写字。
                case CardId.Gold: return "gold";
                case CardId.Wood: return "wood";
                case CardId.Water: return "water";
                case CardId.Earth: return "earth";
                case CardId.Wind: return "wind";
                case CardId.Thunder: return "thunder";
                case CardId.Poison: return "poison";
                case CardId.Confuse: return "confuse";
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

        // 界面手绘图标，Resources/Art/Ui/ico_*.png。
        // 这批图本身就有颜色和描边，别再用 Image.color 去 tint —— 相乘只会脏掉。
        public static Sprite Ui(string key) => Load("Ui/ico_" + key);

        public static Sprite Ui(ItemId id) => Ui("item_" + id.ToString().ToLowerInvariant()) ?? Ui(id.ToString().ToLowerInvariant());

        public static Sprite Flash(EnemyId id)
        {
            string key = "flash:" + EnemyTheme + id;
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
            // 先拿导入器切好的 Sprite。界面图 isReadable=false，Sprite.Create 会失败，
            // 那正是上次炮台页整屏退回白框的原因。
            Sprite sprite = Resources.Load<Sprite>("Art/" + name);
            if (sprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Art/" + name);
                if (tex != null && tex.isReadable)
                {
                    float ppu = Mathf.Max(tex.height, 64f);
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
                }
            }
#if UNITY_EDITOR
            // 团结还没把新图收进 Library 时，Resources.Load 是空的。
            // 编辑器里直接读 PNG，避免 Art() 退回白框。真机包走上面的 Resources。
            if (sprite == null) sprite = LoadFromDisk(name, default);
#endif
            Cache[name] = sprite;
            return sprite;
        }

        public static Sprite LoadSliced(string name, Vector4 border)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string key = name + ":slice:" + border;
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Sprite imported = Load(name);
            if (imported == null)
            {
                Cache[key] = null;
                return null;
            }
            // 导入器经常还是 0 边，不能信。调用方给了九宫格就按调用方切。
            if ((imported.border - border).sqrMagnitude < 1f && imported.border.sqrMagnitude > 1f)
            {
                Cache[key] = imported;
                return imported;
            }
            Texture2D tex = imported.texture;
            if (tex != null && tex.isReadable)
            {
                var sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    border);
                Cache[key] = sprite;
                return sprite;
            }
#if UNITY_EDITOR
            if (border.sqrMagnitude > 1f)
            {
                Sprite disk = LoadFromDisk(name, border);
                if (disk != null)
                {
                    Cache[key] = disk;
                    return disk;
                }
            }
#endif
            Cache[key] = imported;
            return imported;
        }

#if UNITY_EDITOR
        static Sprite LoadFromDisk(string name, Vector4 border)
        {
            string path = Path.Combine(Application.dataPath, "Resources/Art/" + name + ".png");
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                128f,
                0,
                SpriteMeshType.FullRect,
                border);
        }
#endif
    }
}
