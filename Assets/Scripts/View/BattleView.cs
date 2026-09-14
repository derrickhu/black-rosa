using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public sealed class BattleView
    {
        readonly Transform _root;
        readonly List<SpriteRenderer> _grid = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _stamps = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _emitters = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _skins = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _washes = new List<SpriteRenderer>();

        // 皮肤。炮身是黑墨，乘色出不来，所以改成在炮位底下垫一团皮肤色。
        public Color EmitterTint = Color.clear;
        Color _skinPainted = new Color(-1f, -1f, -1f, -1f);
        readonly Dictionary<int, SpriteRenderer> _enemies = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> _bullets = new Dictionary<int, SpriteRenderer>();
        readonly SpriteRenderer _leak;
        readonly HashSet<int> _seen = new HashSet<int>();
        readonly Color[] _colWash = new Color[GameConstants.Columns];

        public BattleView(Transform parent)
        {
            _root = new GameObject("BattleView").transform;
            _root.SetParent(parent, false);
            InkVfx.Ensure();
            InkVfx.BindRoot(_root);
            InkPops.BindRoot(_root);
            var slab = Make("slab", InkFx.SoftDisc(), new Vector3(0f, 0.35f, 0f), 1f);
            slab.sortingOrder = -2;
            slab.transform.localScale = new Vector3(FieldLayout.FieldWidth * 1.35f, 13.5f, 1f);
            InkFx.PaintSprite(slab, InkTheme.StageLift);
            for (int c = 0; c < GameConstants.Columns; c++)
            {
                var wash = Make("wash", InkFx.SoftDisc(), new Vector3(FieldLayout.ColumnX(c), 0.8f, 0f), 1f);
                wash.sortingOrder = -1;
                wash.transform.localScale = new Vector3(1.28f, 5.4f, 1f);
                wash.enabled = false;
                InkFx.PaintAdd(wash, Color.clear);
                _washes.Add(wash);
            }
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++)
            {
                var cell = Make("cell", InkArt.Cell(), FieldLayout.CellPos(c, r), GameConstants.CellWidth);
                cell.sortingOrder = 0;
                _grid.Add(cell);
                var stamp = Make("stamp", InkArt.Heap(CardId.Fire, 128), FieldLayout.CellPos(c, r), 0.92f);
                stamp.sortingOrder = 2;
                stamp.enabled = false;
                _stamps.Add(stamp);
                var star = Make("star", InkArt.Heap(InkShape.Diamond, InkTheme.Ink, 48), FieldLayout.CellPos(c, r) + new Vector3(0.38f, 0.38f, 0), 0.22f);
                star.sortingOrder = 3;
                star.enabled = false;
                _stars.Add(star);
            }
            for (int i = 0; i < GameConstants.MaxEmitters; i++)
            {
                var skin = Make("skin", InkFx.SoftDisc(), new Vector3(0, GameConstants.EmitterY, 0), 1f);
                skin.sortingOrder = 4;
                skin.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                skin.enabled = false;
                InkFx.PaintAdd(skin, Color.clear);
                _skins.Add(skin);
                var gun = Make("gun", InkArt.Cannon(), new Vector3(0, GameConstants.EmitterY, 0), 0.82f);
                gun.sortingOrder = 5;
                _emitters.Add(gun);
            }
            _leak = Make("leak", InkArt.Heap(InkShape.Bar, new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, 0.35f), 64), new Vector3(0, GameConstants.LeakY, 0), 1f);
            _leak.sortingOrder = 1;
            _leak.transform.localScale = new Vector3(FieldLayout.FieldWidth, 0.05f, 1f);
        }

        public void Sync(BattleWorld w)
        {
            int i = 0;
            for (int c = 0; c < GameConstants.Columns; c++) _colWash[c] = Color.clear;
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++, i++)
            {
                bool open = w.IsOpen(c, r);
                _grid[i].enabled = true;
                _grid[i].color = open ? Color.white : new Color(1f, 1f, 1f, 0.18f);
                if (open && w.Grid[c, r].HasValue)
                {
                    CardId id = w.Grid[c, r].Value;
                    _stamps[i].enabled = true;
                    int star = Mathf.Max(1, w.Stars[c, r]);
                    PaintStamp(_stamps[i], id, star);
                    bool sleep = w.CellAsleep(c, r);
                    bool lit = w.CellWordLit(c, r);
                    _stamps[i].color = sleep ? new Color(1f, 1f, 1f, 0.38f) : Color.white;
                    PaintZi(_stamps[i], id, sleep);
                    if (!sleep && (id == CardId.Fire || id == CardId.Ice))
                        InkVfx.PlayGlow(_stamps[i], id == CardId.Fire ? InkTheme.Fire : InkTheme.Ice, 1.08f);
                    else
                        InkVfx.StopAura(_stamps[i]);
                    if (!sleep) AccrueWash(c, id);
                    w.CellCharge(c, r, out int charged, out int need);
                    if (need > 0)
                    {
                        _stars[i].enabled = true;
                        _stars[i].sprite = InkArt.Heap(InkShape.Diamond, lit || charged > 0 ? InkTheme.Accent(id) : InkTheme.Graphite, 48);
                        _stars[i].transform.localScale = Vector3.one * (0.10f + 0.07f * charged);
                    }
                    else
                    {
                        _stars[i].enabled = w.Stars[c, r] >= 2;
                        _stars[i].sprite = InkArt.Heap(InkShape.Diamond, InkTheme.Accent(id), 48);
                        _stars[i].transform.localScale = Vector3.one * (0.16f + 0.06f * w.Stars[c, r]);
                    }
                }
                else
                {
                    InkVfx.Stop(_stamps[i]);
                    InkVfx.StopAura(_stamps[i]);
                    _stamps[i].enabled = false;
                    _stars[i].enabled = false;
                    PaintZi(_stamps[i], CardId.None, true);
                }
            }
            PaintWashes();

            bool skinned = EmitterTint.a > 0.01f;
            // 皮肤色只在换皮肤时刷一次材质，不要每帧设。
            if (_skinPainted != EmitterTint)
            {
                _skinPainted = EmitterTint;
                var wash = new Color(EmitterTint.r, EmitterTint.g, EmitterTint.b, 0.5f);
                for (int e = 0; e < _skins.Count; e++)
                    InkFx.PaintAdd(_skins[e], skinned ? wash : Color.clear);
            }
            for (int e = 0; e < GameConstants.MaxEmitters; e++)
            {
                bool on = e < w.EmitterCount;
                _emitters[e].enabled = on;
                _skins[e].enabled = on && skinned;
                if (!on) continue;
                var at = new Vector3(w.RailX + e * GameConstants.CellWidth, GameConstants.EmitterY, 0f);
                _emitters[e].transform.position = at;
                _skins[e].transform.position = at;
            }

            for (int n = 0; n < w.Enemies.Count; n++)
            {
                EnemyActor e = w.Enemies[n];
                if (e.Dead) continue;
                bool flash = e.HitFlash > 0f;
                Color tint = Color.white;
                if (!flash)
                {
                    // 狂化染红。从 0.4 压到 0.25：关底现在是带金冠玉佩的手绘图，
                    // tint 是乘算的，压太狠会把配件的颜色一起糊掉，而配件颜色
                    // 正是玩家判断危险度的依据。0.25 足够看出「它红了」。
                    if (e.Colored) tint = Color.Lerp(Color.white, InkTheme.Explode, 0.25f);
                    // 冻是整体染青，其余持续状态交给 InkDot 的记号层，不再抢本体颜色
                    if (e.Frozen) tint = Color.Lerp(Color.white, InkTheme.Ice, 0.45f);
                    else if (e.BurnTime > 0f) tint = Color.Lerp(Color.white, InkTheme.Fire, 0.45f);
                    else if (e.PoisonTime > 0f) tint = Color.Lerp(Color.white, InkTheme.Poison, 0.32f);
                }
                Sprite body = flash ? InkSprites.Flash(e.Type) : InkArt.Person(e.Type, Color.clear);
                if (body == null) body = InkArt.Person(e.Type, Color.clear);
                float baseScale = e.Radius * 2.6f;
                float wave = Time.unscaledTime * (e.Held ? 3.2f : 5.4f) + e.Id * 1.7f;
                float breath = 1f + 0.075f * Mathf.Sin(wave);
                float sx = baseScale * breath;
                float sy = baseScale * (2f - breath);
                if (flash)
                {
                    float punch = Mathf.Clamp01(e.HitFlash / 0.14f);
                    sx *= 1.1f + 0.1f * punch;
                    sy *= 0.86f - 0.06f * punch;
                }
                SpriteRenderer sr = Bind(_enemies, e.Id, body, e.Pos + Vector2.up * (0.035f * Mathf.Sin(wave)), baseScale, 4, tint);
                sr.transform.localScale = new Vector3(sx, sy, 1f);
                if (!flash && (e.Frozen || e.BurnTime > 0f))
                    InkVfx.PlayGlow(sr, e.Frozen ? InkTheme.Ice : InkTheme.Fire, 1.45f);
                else
                    InkVfx.StopAura(sr);
                var dots = sr.GetComponent<InkDot>();
                if (dots == null) dots = sr.gameObject.AddComponent<InkDot>();
                dots.Sync(e, 5);
            }
            for (int n = 0; n < w.Bullets.Count; n++)
            {
                BulletActor b = w.Bullets[n];
                if (b.Dead) continue;
                Sprite body = InkArt.Heap(InkShape.Circle, InkTheme.Ink, 48);
                SpriteRenderer shot = Bind(_bullets, b.Id, body, b.Pos, 1f, 6, Color.white);
                var layer = shot.GetComponent<InkShot>();
                if (layer == null) layer = shot.gameObject.AddComponent<InkShot>();
                layer.Present(b);
            }

            for (int n = 0; n < w.Bursts.Count; n++) InkVfx.SpawnHit(w.Bursts[n]);
            w.Bursts.Clear();
            InkPops.Sync(w.Floats);

            _seen.Clear();
            for (int n = 0; n < w.Enemies.Count; n++) if (!w.Enemies[n].Dead) _seen.Add(w.Enemies[n].Id);
            Purge(_enemies);
            _seen.Clear();
            for (int n = 0; n < w.Bullets.Count; n++) if (!w.Bullets[n].Dead) _seen.Add(w.Bullets[n].Id);
            Purge(_bullets);
        }

        void AccrueWash(int col, CardId id)
        {
            Color a = InkTheme.Accent(id);
            float sat = Mathf.Max(a.r, Mathf.Max(a.g, a.b)) - Mathf.Min(a.r, Mathf.Min(a.g, a.b));
            if (sat < 0.12f) return;
            Color cur = _colWash[col];
            if (cur.a < 0.02f)
            {
                a.a = 0.20f;
                _colWash[col] = a;
                return;
            }
            Color mix = Color.Lerp(cur, a, 0.45f);
            mix.a = Mathf.Min(0.48f, cur.a + 0.12f);
            _colWash[col] = mix;
        }

        void PaintWashes()
        {
            for (int c = 0; c < _washes.Count; c++)
            {
                Color col = _colWash[c];
                bool on = col.a > 0.03f;
                _washes[c].enabled = on;
                if (on)
                {
                    col.a *= 0.55f;
                    InkFx.PaintSoft(_washes[c], col);
                }
            }
        }

        static void PaintStamp(SpriteRenderer stamp, CardId id, int star)
        {
            InkVfx.Stop(stamp);
            stamp.sprite = InkArt.Heap(id, star, 128);
            stamp.transform.localScale = Vector3.one * 0.88f;
        }

        static void PaintZi(SpriteRenderer stamp, CardId id, bool sleep)
        {
            Transform t = stamp.transform.Find("zi");
            TextMesh tm = t != null ? t.GetComponent<TextMesh>() : null;
            bool need = id != CardId.None && InkSprites.Heap(id) == null;
            if (!need)
            {
                if (tm != null) tm.gameObject.SetActive(false);
                return;
            }
            if (tm == null)
            {
                var go = new GameObject("zi");
                go.transform.SetParent(stamp.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                tm = go.AddComponent<TextMesh>();
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.characterSize = 0.08f;
                tm.fontSize = 64;
                tm.font = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Heiti SC", "STHeiti", "Songti SC" }, 64);
                if (tm.font != null) tm.GetComponent<MeshRenderer>().material = tm.font.material;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sortingOrder = 4;
            }
            tm.gameObject.SetActive(true);
            tm.text = CardCatalog.Get(id).Name;
            Color ink = CardCatalog.Get(id).Wake == CardWake.WordPart ? InkTheme.Word : InkTheme.Ink;
            tm.color = sleep ? new Color(ink.r, ink.g, ink.b, 0.5f) : ink;
        }

        SpriteRenderer Bind(Dictionary<int, SpriteRenderer> map, int id, Sprite sprite, Vector2 pos, float scale, int order, Color color)
        {
            if (!map.TryGetValue(id, out SpriteRenderer sr))
            {
                sr = Make("actor", sprite, Vector3.zero, scale);
                sr.sortingOrder = order;
                map[id] = sr;
            }
            sr.enabled = true;
            sr.sprite = sprite;
            sr.color = color;
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.transform.localScale = Vector3.one * scale;
            return sr;
        }

        void Purge(Dictionary<int, SpriteRenderer> map)
        {
            var drop = new List<int>();
            foreach (var kv in map)
            {
                if (_seen.Contains(kv.Key)) continue;
                Object.Destroy(kv.Value.gameObject);
                drop.Add(kv.Key);
            }
            for (int i = 0; i < drop.Count; i++) map.Remove(drop[i]);
        }

        SpriteRenderer Make(string name, Sprite sprite, Vector3 pos, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            return sr;
        }

        public void HighlightCell(int col, int row, Color color)
        {
            int i = col * GameConstants.Rows + row;
            if (i >= 0 && i < _grid.Count)
                _grid[i].color = color.a < 0.02f ? Color.white : Color.Lerp(Color.white, color, 0.85f);
        }

        public void Dispose()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
        }
    }
}
