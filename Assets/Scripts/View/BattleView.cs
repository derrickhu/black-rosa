using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public sealed class BattleView
    {
        readonly Transform _root;
        readonly List<SpriteRenderer> _grid = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _stamps = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _plates = new List<SpriteRenderer>();
        readonly List<TextMesh> _ranks = new List<TextMesh>();
        readonly List<SpriteRenderer> _pips = new List<SpriteRenderer>();
        readonly float[] _pipFlash = new float[GameConstants.Columns * GameConstants.Rows];
        readonly int[] _pipWas = new int[GameConstants.Columns * GameConstants.Rows];
        const int PipN = 4;
        readonly List<SpriteRenderer> _emitters = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _muzzles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _skins = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _washes = new List<SpriteRenderer>();

        // 皮肤直接换炮身图，底下再垫一团同色的光。素笔的 tint 是透明，不能拿它判断换没换皮肤。
        public int EmitterSkin;
        public Color EmitterTint = Color.clear;
        int _skinPainted = -1;
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
            InkSpill.BindRoot(_root);
            InkDrops.BindRoot(_root);
            ChestView.BindRoot(_root);
            var slab = Make("slab", InkFx.SoftDisc(), new Vector3(0f, 0.35f, 0f), 1f);
            _slab = slab;
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
                var plate = Make("plate", LevelPlate(), FieldLayout.CellPos(c, r), 1f);
                plate.sortingOrder = 4;
                plate.enabled = false;
                InkFx.PaintSprite(plate, Color.white);
                _plates.Add(plate);
                _ranks.Add(MakeRank());
                for (int p = 0; p < PipN; p++)
                {
                    var pip = Make("pip", InkFx.Pill(), FieldLayout.CellPos(c, r), 1f);
                    pip.sortingOrder = 3;
                    pip.enabled = false;
                    InkFx.PaintSprite(pip, Color.white);
                    _pips.Add(pip);
                }
            }
            for (int i = 0; i < GameConstants.MaxEmitters; i++)
            {
                var skin = Make("skin", InkFx.SoftDisc(), new Vector3(0, GameConstants.EmitterY, 0), 1f);
                skin.sortingOrder = 4;
                skin.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                skin.enabled = false;
                InkFx.PaintAdd(skin, Color.clear);
                _skins.Add(skin);
                var gun = Make("gun", InkSprites.CannonSkin(0), new Vector3(0, GameConstants.EmitterY, 0), 0.64f);
                gun.sortingOrder = 5;
                _emitters.Add(gun);
                var muzzle = Make("muzzle", InkFx.SoftDisc(), new Vector3(0, GameConstants.EmitterY, 0), 0.22f);
                muzzle.sortingOrder = 6;
                muzzle.enabled = false;
                _muzzles.Add(muzzle);
            }
            _leak = Make("leak", InkArt.Heap(InkShape.Bar, new Color(InkTheme.Ink.r, InkTheme.Ink.g, InkTheme.Ink.b, 0.35f), 64), new Vector3(0, GameConstants.LeakY, 0), 1f);
            _leak.sortingOrder = 1;
            _leak.transform.localScale = new Vector3(FieldLayout.FieldWidth, 0.05f, 1f);
        }

        SpriteRenderer _backdrop;
        SpriteRenderer _slab;

        // 每章一张地面，铺满镜头（cover）。图没导进来就留原来的纯色底和中间那块提亮。
        public void SetBackdrop(int chapter)
        {
            Sprite s = InkSprites.Load("Bg/battle_bg_" + (chapter + 1));
            if (s == null) return;
            if (_slab != null) _slab.enabled = false;
            if (_backdrop == null)
            {
                _backdrop = Make("backdrop", s, Vector3.zero, 1f);
                _backdrop.sortingOrder = -10;
            }
            _backdrop.sprite = s;
            FitBackdrop();
        }

        void FitBackdrop()
        {
            Camera cam = Camera.main;
            if (_backdrop == null || _backdrop.sprite == null || cam == null) return;
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            Vector2 size = _backdrop.sprite.bounds.size;
            float k = Mathf.Max(w / Mathf.Max(0.01f, size.x), h / Mathf.Max(0.01f, size.y));
            Transform t = _backdrop.transform;
            t.localScale = new Vector3(k, k, 1f);
            t.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        }

        public void Sync(BattleWorld w)
        {
            FitBackdrop();
            int i = 0;
            for (int c = 0; c < GameConstants.Columns; c++) _colWash[c] = Color.clear;
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++, i++)
            {
                bool open = w.IsOpen(c, r);
                _grid[i].enabled = open;
                _grid[i].color = Color.white;
                if (open && w.Grid[c, r].HasValue)
                {
                    CardId id = w.Grid[c, r].Value;
                    _stamps[i].enabled = true;
                    int star = Mathf.Max(1, w.Stars[c, r]);
                    PaintStamp(_stamps[i], id, star);
                    bool sleep = w.CellAsleep(c, r);
                    _stamps[i].color = sleep ? new Color(1f, 1f, 1f, 0.38f) : Color.white;
                    PaintZi(_stamps[i], id, sleep);
                    if (!sleep && (id == CardId.Fire || id == CardId.Ice))
                        InkVfx.PlayGlow(_stamps[i], id == CardId.Fire ? InkTheme.Fire : InkTheme.Ice, 1.08f);
                    else
                        InkVfx.StopAura(_stamps[i]);
                    if (!sleep) AccrueWash(c, id);
                    w.CellCharge(c, r, out int charged, out int need);
                    PaintMark(i, _stamps[i], id, star, sleep, charged, need);
                }
                else
                {
                    InkVfx.Stop(_stamps[i]);
                    InkVfx.StopAura(_stamps[i]);
                    _stamps[i].enabled = false;
                    HideMark(i);
                    PaintZi(_stamps[i], CardId.None, true);
                }
            }
            PaintWashes();

            if (_skinPainted != EmitterSkin)
            {
                _skinPainted = EmitterSkin;
                Sprite body = InkSprites.CannonSkin(EmitterSkin);
                for (int e = 0; e < _emitters.Count; e++)
                {
                    _emitters[e].sprite = body;
                    _emitters[e].color = Color.white;
                }
            }
            for (int e = 0; e < GameConstants.MaxEmitters; e++)
            {
                bool on = e < w.EmitterCount;
                _emitters[e].enabled = on;
                if (_skins.Count > e) _skins[e].enabled = false;
                if (_muzzles.Count > e) _muzzles[e].enabled = false;
                if (!on) continue;
                var at = new Vector3(w.RailX + e * GameConstants.CellWidth, GameConstants.EmitterY, 0f);
                _emitters[e].transform.position = at;
            }

            for (int n = 0; n < w.Enemies.Count; n++)
            {
                EnemyActor e = w.Enemies[n];
                if (e.Dead) continue;
                bool flash = e.HitFlash > 0f;
                Color tint = Color.white;
                // 狂化染红。从 0.4 压到 0.25：关底现在是带金冠玉佩的手绘图，
                // tint 是乘算的，压太狠会把配件的颜色一起糊掉，而配件颜色
                // 正是玩家判断危险度的依据。0.25 足够看出「它红了」。
                if (e.Colored) tint = Color.Lerp(Color.white, InkTheme.Explode, 0.25f);
                // 冻是整体染青，其余持续状态交给 InkDot 的记号层，不再抢本体颜色
                // 持续状态现在都由 InkDot 的分区图层来说，本体染色只留一点点「它变了色」。
                // 原来冻 0.45 / 烧 0.45 / 毒 0.32 是在图层之外再喊一遍，
                // 压那么狠会把怪糊成一块色板，图层的硬边反而看不出来。
                // 同时挂多个状态时也只能染一种色，越浓越容易误导。
                if (e.Frozen) tint = Color.Lerp(Color.white, InkTheme.Ice, 0.20f);
                else if (e.BurnTime > 0f) tint = Color.Lerp(Color.white, InkTheme.Fire, 0.22f);
                else if (e.PoisonTime > 0f) tint = Color.Lerp(Color.white, InkTheme.Poison, 0.18f);
                Sprite body = InkArt.Person(e.Type, Color.clear);
                float baseScale = e.Radius * 2.6f;
                float wave = Time.unscaledTime * (e.Held ? 3.2f : 5.4f) + e.Id * 1.7f;
                float breath = 1f + 0.075f * Mathf.Sin(wave);
                float sx = baseScale * breath;
                float sy = baseScale * (2f - breath);
                if (flash)
                {
                    float punch = Mathf.Clamp01(e.HitFlash / 0.16f);
                    punch *= punch;
                    sx *= 1f + 0.32f * punch;
                    sy *= 1f - 0.26f * punch;
                }
                // 命中位移直接加在绘制位上：碰撞和走位还按 e.Pos 算，
                // 挨打顿一下只是看的人的事，不该影响谁先破防线。
                Vector2 at = e.Pos + e.Recoil + Vector2.up * (0.035f * Mathf.Sin(wave));
                SpriteRenderer sr = Bind(_enemies, e.Id, body, at, baseScale, 4, tint);
                sr.transform.localScale = new Vector3(sx, sy, 1f);
                // 持续状态一概不再叠柔光。冰锥 / 火焰 / 毒泡本身就是硬边图，
                // 外面再罩一圈雾正好把硬边泡软，而硬边是这套美术能立住的全部原因。
                InkVfx.StopAura(sr);
                var dots = sr.GetComponent<InkDot>();
                if (dots == null) dots = sr.gameObject.AddComponent<InkDot>();
                dots.Sync(e, 5);
                PaintHitFlash(sr, e);
                var bar = sr.GetComponent<InkBar>();
                if (bar == null) bar = sr.gameObject.AddComponent<InkBar>();
                // 16 起步：压在命中特效（9~15）之上、飘字（19/20）之下。
                // 血条被爆点盖住的话，最该看清的那一刻恰好看不清。
                bar.Sync(e, 16);
            }
            for (int n = 0; n < w.Bullets.Count; n++)
            {
                BulletActor b = w.Bullets[n];
                if (b.Dead) continue;
                Sprite body = InkArt.Heap(InkShape.Circle, InkTheme.Ink, 48);
                SpriteRenderer shot = Bind(_bullets, b.Id, body, b.Pos, 1f, 6, Color.white);
                var layer = shot.GetComponent<InkShot>();
                if (layer == null) layer = shot.gameObject.AddComponent<InkShot>();
                layer.Present(b, EmitterSkin);
            }

            for (int n = 0; n < w.Bursts.Count; n++) InkVfx.SpawnHit(w.Bursts[n]);
            w.Bursts.Clear();
            for (int n = 0; n < w.Deaths.Count; n++) InkSpill.Play(w.Deaths[n]);
            w.Deaths.Clear();
            if (w.ShakeWanted > 0f)
            {
                InkShake.Kick(Camera.main, w.ShakeWanted);
                w.ShakeWanted = 0f;
            }
            InkPops.Sync(w.Floats);
            InkDrops.Sync(w.Drops);
            ChestView.Sync(w.Chests);

            _seen.Clear();
            for (int n = 0; n < w.Enemies.Count; n++) if (!w.Enemies[n].Dead) _seen.Add(w.Enemies[n].Id);
            Purge(_enemies);
            _seen.Clear();
            for (int n = 0; n < w.Bullets.Count; n++) if (!w.Bullets[n].Dead) _seen.Add(w.Bullets[n].Id);
            Purge(_bullets);
        }

        // 命中白闪。原来是整只换成纯白剪影，射速快的时候敌人大半时间都是一团白，
        // 既看不出是什么怪，也看不出血条打到哪了 —— 白闪反而把最该看的东西盖了。
        // 现在本体照常画，白版只当一层会淡掉的罩子叠在上面。
        static void PaintHitFlash(SpriteRenderer host, EnemyActor e)
        {
            Transform t = host.transform.Find("hit");
            SpriteRenderer sr = t != null ? t.GetComponent<SpriteRenderer>() : null;
            float a = Mathf.Clamp01(e.HitFlash / 0.16f);
            a = Mathf.Sqrt(a);
            if (a <= 0.01f)
            {
                if (sr != null) sr.enabled = false;
                return;
            }
            if (sr == null)
            {
                var go = new GameObject("hit");
                go.transform.SetParent(host.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = host.sortingOrder + 1;
                InkFx.PaintSprite(sr, Color.white);
            }
            sr.sprite = InkSprites.Flash(e.Type);
            sr.enabled = sr.sprite != null;
            sr.color = new Color(1f, 1f, 1f, a * 0.92f);
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

        // 蜂蜜黄，生蜂蜜那种。方块色标，不做成圆章，免得读成金币。
        static readonly Color Honey = new Color(0.93f, 0.72f, 0.32f, 1f);

        // 右上角方块是星级。下面的槽：常驻字一整条茶叶绿，每发都生效；
        // 蓄力字按「过 N 发」分成 N 段，满格是茶叶绿，空格是淡青瓷。
        void PaintMark(int i, SpriteRenderer stamp, CardId id, int star, bool sleep, int charged, int need)
        {
            Bounds card = stamp.bounds;
            float half = Mathf.Min(card.extents.x, card.extents.y);
            half = Mathf.Min(half, GameConstants.CellWidth * 0.40f);
            Vector3 c = card.center;

            float badge = half * 2f * 0.18f;
            float margin = half * 2f * 0.08f;
            var bp = new Vector3(
                c.x + half - margin - badge * 0.5f,
                c.y + half - margin - badge * 0.5f,
                0f);
            SpriteRenderer plate = _plates[i];
            plate.enabled = true;
            Color honey = Honey;
            if (sleep) honey.a = 0.4f;
            plate.color = honey;
            plate.transform.position = bp;
            plate.transform.localScale = Vector3.one * badge;

            TextMesh rank = _ranks[i];
            rank.gameObject.SetActive(true);
            rank.text = star.ToString();
            rank.characterSize = badge * 0.13f;
            Color ink = InkTheme.Ink;
            if (sleep) ink.a = 0.45f;
            rank.color = ink;
            rank.transform.position = bp;

            int segs;
            int filled;
            bool metering;
            if (CardCatalog.Get(id).Wake == CardWake.Always)
            {
                segs = 1;
                filled = 1;
                metering = false;
            }
            else if (need > 0)
            {
                segs = Mathf.Clamp(need, 1, PipN);
                filled = Mathf.Clamp(charged, 0, segs);
                metering = true;
            }
            else
            {
                segs = Mathf.Clamp(CardCatalog.Get(id).ChargeNeed, 1, PipN);
                filled = 0;
                metering = false;
            }

            if (metering && _pipWas[i] > 0 && filled < _pipWas[i])
                _pipFlash[i] = 0.28f;
            _pipWas[i] = metering ? filled : 0;
            if (_pipFlash[i] > 0f) _pipFlash[i] -= Time.unscaledDeltaTime;
            bool burst = _pipFlash[i] > 0f;

            float width = half * 2f * 0.76f;
            float gap = segs > 1 ? width * 0.06f : 0f;
            float segW = (width - gap * (segs - 1)) / segs;
            float left = c.x - width * 0.5f;
            float h = half * 2f * (burst ? 0.09f : 0.07f);
            float y = c.y - half + margin + h * 0.5f;
            for (int p = 0; p < PipN; p++)
            {
                SpriteRenderer sr = _pips[i * PipN + p];
                if (p >= segs)
                {
                    sr.enabled = false;
                    continue;
                }
                bool on = burst || p < filled;
                Color col = on ? (burst ? InkTheme.AccelMid : InkTheme.Accel) : InkTheme.AccelHi;
                if (sleep) col.a = on ? 0.4f : 0.22f;
                sr.enabled = true;
                sr.color = col;
                sr.transform.position = new Vector3(left + p * (segW + gap), y, 0f);
                sr.transform.localScale = new Vector3(segW, h / InkFx.PillH, 1f);
            }
        }

        void HideMark(int i)
        {
            _plates[i].enabled = false;
            _ranks[i].gameObject.SetActive(false);
            _pipFlash[i] = 0f;
            _pipWas[i] = 0;
            for (int p = 0; p < PipN; p++)
                _pips[i * PipN + p].enabled = false;
        }

        TextMesh MakeRank()
        {
            var go = new GameObject("rank");
            go.transform.SetParent(_root, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = UiKit.FontBold;
            tm.fontSize = 72;
            tm.characterSize = 0.03f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = InkTheme.Ink;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sortingOrder = 5;
            if (tm.font != null) mr.material = tm.font.material;
            go.SetActive(false);
            return tm;
        }

        static Sprite _plate;
        static Sprite LevelPlate()
        {
            if (_plate != null) return _plate;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float rad = n * 0.22f;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = RoundBox(x + 0.5f, y + 0.5f, 1.2f, 1.2f, n - 2.2f, n - 2.2f, rad);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.9f - d));
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _plate = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return _plate;
        }

        static float RoundBox(float px, float py, float x0, float y0, float x1, float y1, float rad)
        {
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float hx = (x1 - x0) * 0.5f - rad;
            float hy = (y1 - y0) * 0.5f - rad;
            float dx = Mathf.Abs(px - cx) - hx;
            float dy = Mathf.Abs(py - cy) - hy;
            float ax = Mathf.Max(dx, 0f);
            float ay = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - rad;
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
            if (i < 0 || i >= _grid.Count || !_grid[i].enabled) return;
            // 能放的格子始终是黑框。这里不再把整格乘成绿色。
            _grid[i].color = Color.white;
        }

        public void Dispose()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
        }
    }
}
