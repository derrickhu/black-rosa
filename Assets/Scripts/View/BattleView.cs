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
        readonly Dictionary<int, SpriteRenderer> _enemies = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> _bullets = new Dictionary<int, SpriteRenderer>();
        readonly SpriteRenderer _leak;
        readonly HashSet<int> _seen = new HashSet<int>();

        public BattleView(Transform parent)
        {
            _root = new GameObject("BattleView").transform;
            _root.SetParent(parent, false);
            InkVfx.Ensure();
            InkVfx.BindRoot(_root);
            InkPops.BindRoot(_root);
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
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++, i++)
            {
                bool open = r < w.OpenRows;
                _grid[i].enabled = true;
                _grid[i].color = open ? Color.white : new Color(1f, 1f, 1f, 0.22f);
                if (open && w.Grid[c, r].HasValue)
                {
                    CardId id = w.Grid[c, r].Value;
                    _stamps[i].enabled = true;
                    _stamps[i].color = Color.white;
                    int star = Mathf.Max(1, w.Stars[c, r]);
                    PaintStamp(_stamps[i], id, star);
                    _stars[i].enabled = w.Stars[c, r] >= 2;
                    _stars[i].sprite = InkArt.Heap(InkShape.Diamond, InkTheme.Accent(id), 48);
                    _stars[i].transform.localScale = Vector3.one * (0.16f + 0.06f * w.Stars[c, r]);
                }
                else
                {
                    InkVfx.Stop(_stamps[i]);
                    _stamps[i].enabled = false;
                    _stars[i].enabled = false;
                }
            }

            for (int e = 0; e < GameConstants.MaxEmitters; e++)
            {
                bool on = e < w.EmitterCount;
                _emitters[e].enabled = on;
                if (!on) continue;
                _emitters[e].transform.position = new Vector3(w.RailX + e * GameConstants.CellWidth, GameConstants.EmitterY, 0f);
            }

            for (int n = 0; n < w.Enemies.Count; n++)
            {
                EnemyActor e = w.Enemies[n];
                if (e.Dead) continue;
                bool flash = e.HitFlash > 0f;
                Color tint = Color.white;
                if (!flash)
                {
                    if (e.Colored) tint = Color.Lerp(Color.white, InkTheme.Explode, 0.4f);
                    if (e.FreezeTime > 0f) tint = Color.Lerp(Color.white, InkTheme.Ice, 0.45f);
                    else if (e.BurnTime > 0f) tint = Color.Lerp(Color.white, InkTheme.Fire, 0.45f);
                }
                Sprite body = flash ? InkSprites.Flash(e.Type) : InkArt.Person(e.Type, Color.clear);
                if (body == null) body = InkArt.Person(e.Type, Color.clear);
                float baseScale = e.Radius * 2.6f;
                float wave = Time.unscaledTime * (e.FreezeTime > 0f ? 3.2f : 5.4f) + e.Id * 1.7f;
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
            }
            for (int n = 0; n < w.Bullets.Count; n++)
            {
                BulletActor b = w.Bullets[n];
                if (b.Dead) continue;
                if (InkVfx.TryBody(b.FireStar, b.IceStar, b.HeavyStar, b.Radius, out Sprite[] frames, out Sprite first, out float shotScale, out float fps))
                {
                    SpriteRenderer shot = Bind(_bullets, b.Id, first, b.Pos, shotScale, 6, Color.white);
                    var flip = shot.GetComponent<InkFlip>();
                    if (flip == null) flip = shot.gameObject.AddComponent<InkFlip>();
                    flip.Outline = PaintRim(shot, first, RimColor(b));
                    InkVfx.PlayLoop(shot, frames, fps, b.Id);
                    float ang = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg - 90f;
                    shot.transform.rotation = Quaternion.Euler(0f, 0f, ang);
                    PaintTrail(shot, b.TrackStar > 0, shotScale);
                }
                else
                {
                    Color slug = b.Color == Color.black ? InkTheme.Graphite : b.Color;
                    SpriteRenderer sr = Bind(_bullets, b.Id, InkArt.Heap(InkShape.Circle, slug, 48), b.Pos, b.Radius * 2.4f, 6, Color.white);
                    InkVfx.Stop(sr);
                    sr.transform.rotation = Quaternion.identity;
                    PaintTrail(sr, b.TrackStar > 0, b.Radius * 2.4f);
                    var flip = sr.GetComponent<InkFlip>();
                    if (flip != null) flip.Outline = PaintRim(sr, sr.sprite, RimColor(b));
                }
            }

            for (int n = 0; n < w.Bursts.Count; n++)
            {
                FxBurst fx = w.Bursts[n];
                if (fx.Kind == 1)
                    InkVfx.SpawnHit(fx.Pos, fx.Scale > 0.01f ? fx.Scale : 1.25f, fx.Tint);
            }
            w.Bursts.Clear();
            InkPops.Sync(w.Floats);

            _seen.Clear();
            for (int n = 0; n < w.Enemies.Count; n++) if (!w.Enemies[n].Dead) _seen.Add(w.Enemies[n].Id);
            Purge(_enemies);
            _seen.Clear();
            for (int n = 0; n < w.Bullets.Count; n++) if (!w.Bullets[n].Dead) _seen.Add(w.Bullets[n].Id);
            Purge(_bullets);
        }

        static void PaintStamp(SpriteRenderer stamp, CardId id, int star)
        {
            InkVfx.Stop(stamp);
            stamp.sprite = InkArt.Heap(id, star, 128);
            stamp.transform.localScale = Vector3.one * 0.88f;
        }

        static void PaintTrail(SpriteRenderer shot, bool on, float shotScale)
        {
            var trail = shot.GetComponent<InkTrail>();
            if (on)
            {
                if (trail == null) trail = shot.gameObject.AddComponent<InkTrail>();
                trail.Setup(new Color(0.96f, 0.78f, 1f), InkTheme.Track, 0.15f * shotScale);
                trail.Feed(shot.transform.position, Time.unscaledDeltaTime);
            }
            else if (trail != null) trail.Hide();
        }

        static Color RimColor(BulletActor b)
        {
            if (b.TrackStar > 0) return new Color(InkTheme.Track.r, InkTheme.Track.g, InkTheme.Track.b, 0.72f);
            if (b.ExplodeR > 0.01f) return new Color(InkTheme.Explode.r, InkTheme.Explode.g, InkTheme.Explode.b, 0.55f);
            return Color.clear;
        }

        static SpriteRenderer PaintRim(SpriteRenderer shot, Sprite sprite, Color color)
        {
            Transform t = shot.transform.Find("rim");
            SpriteRenderer rim = t != null ? t.GetComponent<SpriteRenderer>() : null;
            if (color.a < 0.02f)
            {
                if (rim != null) rim.enabled = false;
                return null;
            }
            if (rim == null)
            {
                var go = new GameObject("rim");
                go.transform.SetParent(shot.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * 1.16f;
                rim = go.AddComponent<SpriteRenderer>();
            }
            rim.enabled = true;
            rim.sprite = sprite;
            rim.color = color;
            rim.sortingOrder = shot.sortingOrder - 1;
            return rim;
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
