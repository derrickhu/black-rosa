using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 道具丢出去的演出。道具不走炮弹，画在战场上面的界面层上，可以铺满全屏。
    // 三档排场跟品质走：普通只在目标附近热闹一下；高级加一层全屏氛围和名字横幅；
    // 稀有先压暗全场、亮出大图标和名字，再出手。等级越高粒子越多、范围越大。
    // 伤害由 BattleWorld 在 ItemCast.Impact 秒后结算，这里的「命中」点要和它对齐。
    public sealed class ItemFx : MonoBehaviour
    {
        RectTransform _root;
        RectTransform _layer;

        public static ItemFx Build(RectTransform layer)
        {
            var go = new GameObject("itemfx", typeof(RectTransform));
            go.transform.SetParent(layer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            // 压在 HUD 底下：道具键、血条、按钮始终露在演出上面。
            rt.SetAsFirstSibling();
            var fx = go.AddComponent<ItemFx>();
            fx._root = rt;
            fx._layer = layer;
            return fx;
        }

        // 一次演出的舞台：一个铺满的组 + 一条时间轴，播完整组销毁。
        sealed class Stage
        {
            public RectTransform G;
            public UiAnim A;
            public Vector2 Half;
            public float Pow;
            public int Lv;
        }

        public void Play(BattleWorld.ItemCast c, BattleWorld w, Vector2 key, Vector2 hearts)
        {
            if (w == null) return;
            var g = ResultKit.Group(_root, "cast_" + c.Id, Vector2.zero, Vector2.zero);
            g.anchorMin = Vector2.zero;
            g.anchorMax = Vector2.one;
            g.offsetMin = g.offsetMax = Vector2.zero;
            Vector2 size = _root.rect.size;
            if (size.x < 10f) size = new Vector2(ScreenFit.DesignW, ScreenFit.DesignH);
            var s = new Stage
            {
                G = g, A = UiAnim.On(g), Half = size * 0.5f,
                Lv = Mathf.Clamp(c.Rank, 1, ItemCatalog.MaxLevel),
            };
            s.Pow = 1f + 0.14f * (s.Lv - 1);
            ItemDef d = ItemCatalog.Get(c.Id);
            float end;
            switch (c.Id)
            {
                case ItemId.Snipe: end = Snipe(s, d, c, w, key); break;
                case ItemId.Burst: end = Burst(s, d, c, w, key); break;
                case ItemId.Slow: end = Slow(s, d, c, w, key); break;
                case ItemId.Halt: end = Halt(s, d, c, w, key); break;
                case ItemId.Rage: end = Rage(s, d, c, w, key); break;
                case ItemId.Frost: end = Frost(s, d, c, w, key); break;
                case ItemId.Sweep: end = Sweep(s, d, c, w, key); break;
                case ItemId.Splash: end = Splash(s, d, c, w, key); break;
                case ItemId.Mend: end = Mend(s, d, c, w, key, hearts); break;
                default: end = 1f; break;
            }
            s.A.At(end, () => { if (g != null) Destroy(g.gameObject); });
        }

        // ---------- 普通：弹弓、鞭炮、胶水 ----------

        // 准星从大缩到目标身上，弹弓在键旁拉满一弹，石子划一道线砸过去。
        float Snipe(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            Func<Vector2> aim = Track(w, c.Target);
            Tag(s, d, key);

            var sight = ResultKit.Group(s.G, "sight", aim(), new Vector2(10f, 10f));
            var ring = Pic(sight, InkFx.SoftRing(), Vector2.zero, 240f, InkTheme.Explode);
            var cross = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                cross[i] = Bar(sight, new Vector2(6f, 34f), InkTheme.Explode);
                cross[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            }
            s.A.Tween(0f, T + 0.12f, k =>
            {
                sight.anchoredPosition = aim();
                float r = Mathf.Lerp(250f, 96f, Ease.OutCubic(Mathf.Clamp01(k * 1.25f)));
                ring.rectTransform.sizeDelta = new Vector2(r, r);
                float a = k < 0.85f ? Mathf.Clamp01(k * 4f) : (1f - k) / 0.15f;
                ring.color = Alpha(InkTheme.Explode, a);
                for (int i = 0; i < 4; i++)
                {
                    float ang = i * Mathf.PI * 0.5f;
                    cross[i].rectTransform.anchoredPosition = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * (r * 0.5f + 8f);
                    cross[i].color = Alpha(InkTheme.Explode, a);
                }
                sight.localRotation = Quaternion.Euler(0f, 0f, 90f * Ease.OutCubic(k));
            });

            Vector2 grip = key + new Vector2(-110f, 30f);
            var sling = Pic(s.G, InkSprites.Ui(d.Id), key, 96f, Color.white);
            s.A.Move(sling.rectTransform, key, grip, 0f, 0.16f)
                .Pop(sling.transform, 0f, 0.2f, 0.4f)
                .Tween(0.12f, 0.12f, k => Squash(sling.transform, 0.25f * Mathf.Sin(k * Mathf.PI)))
                .Fade(sling, T, 0.25f, 1f, 0f);

            var stone = Pic(s.G, Dot(), grip, 30f * s.Pow, InkTheme.Outline);
            var trail = new Image[4];
            for (int i = 0; i < trail.Length; i++)
                trail[i] = Pic(s.G, Dot(), grip, (24f - i * 4f) * s.Pow, Alpha(InkTheme.Word, 0f));
            float t0 = 0.22f;
            s.A.Tween(0f, T + 0.01f, k =>
            {
                float now = k * (T + 0.01f);
                float f = Mathf.Clamp01((now - t0) / (T - t0));
                Vector2 to = aim();
                stone.enabled = now >= t0 && f < 1f;
                stone.rectTransform.anchoredPosition = Vector2.Lerp(grip, to, Ease.InQuad(f));
                for (int i = 0; i < trail.Length; i++)
                {
                    float fi = Mathf.Clamp01(f - 0.06f * (i + 1));
                    trail[i].rectTransform.anchoredPosition = Vector2.Lerp(grip, to, Ease.InQuad(fi));
                    trail[i].color = Alpha(InkTheme.Word, stone.enabled ? 0.7f - 0.15f * i : 0f);
                }
            });

            s.A.At(T, () =>
            {
                Vector2 at = aim();
                AudioBus.Hit();
                Boom(s, "Vfx/hit_heavy_", 4, at, 230f * s.Pow, InkTheme.ThunderHi, T, 0.32f);
                Boom(s, "Vfx/fire_hit_", 4, at, 120f * s.Pow, Color.white, T, 0.24f);
                UiConfetti.Sparks(s.G, at, InkTheme.Word, 10 + s.Lv * 2, 560f);
                if (s.Lv >= 3) Shock(s, at, InkTheme.Word, 60f, 300f * s.Pow, T, 0.35f);
            });
            return T + 0.7f;
        }

        // 一挂鞭炮抛物线甩过去，引信一路冒火星；落地主爆一下，再噼里啪啦串几响。
        float Burst(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            Func<Vector2> aim = Track(w, c.Target);
            Tag(s, d, key);

            var bomb = Pic(s.G, InkSprites.Ui(d.Id), key, 92f, Color.white);
            var fuse = Pic(s.G, Star(), key, 30f, InkTheme.Word);
            Vector2 from = key;
            s.A.Tween(0f, T, k =>
            {
                Vector2 to = aim();
                float h = 260f + 0.25f * Mathf.Abs(to.x - from.x);
                Vector2 p = Vector2.Lerp(from, to, k) + new Vector2(0f, 4f * h * k * (1f - k));
                bomb.rectTransform.anchoredPosition = p;
                bomb.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -540f * k);
                bomb.enabled = k < 1f;
                float ang = (-540f * k + 60f) * Mathf.Deg2Rad;
                fuse.rectTransform.anchoredPosition = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 40f;
                float flick = 0.7f + 0.5f * Mathf.Abs(Mathf.Sin(k * 60f));
                fuse.rectTransform.localScale = Vector3.one * flick;
                fuse.enabled = k < 1f;
            });

            s.A.At(T, () =>
            {
                Vector2 at = aim();
                AudioBus.Boom();
                float big = 300f * s.Pow;
                Boom(s, "Vfx/hit_explode_", 4, at, big, Color.white, T, 0.4f);
                Flash(s, InkTheme.Hex("FFD9A0"), T, 0.14f, 0.28f);
                UiConfetti.Sparks(s.G, at, InkTheme.Explode, 14 + s.Lv * 2, 640f);
                UiConfetti.Sparks(s.G, at, InkTheme.Word, 10 + s.Lv * 2, 480f);
                int pops = 2 + s.Lv;
                for (int i = 0; i < pops; i++)
                {
                    float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    Vector2 off = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * UnityEngine.Random.Range(60f, 130f) * s.Pow;
                    float at2 = T + 0.08f + i * 0.07f;
                    Boom(s, "Vfx/fire_hit_", 4, at + off, UnityEngine.Random.Range(90f, 130f), Color.white, at2, 0.22f);
                }
                // 红纸屑：鞭炮炸完满地红。
                Debris(s, at, InkTheme.Explode, 10 + s.Lv * 3, T);
            });
            return T + 1.4f;
        }

        // 胶水瓶飞到上空倒过来一挤，一滴一滴甩到每只怪脚下，摊成一滩黏糊糊的。
        float Slow(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            Tag(s, d, key);
            Color glue = InkTheme.Hex("CFE9F5");
            Vector2 top = new Vector2(0f, s.Half.y * 0.48f);
            var bottle = Pic(s.G, InkSprites.Ui(d.Id), key, 100f, Color.white);
            s.A.Move(bottle.rectTransform, key, top, 0f, 0.2f)
                .Rotate(bottle.transform, 0f, 160f, 0.08f, 0.16f)
                .Tween(0.2f, 0.18f, k => Squash(bottle.transform, 0.22f * Mathf.Sin(k * Mathf.PI * 2f)))
                .Fade(bottle, T + 0.2f, 0.25f, 1f, 0f);

            var targets = Alive(w, 6 + s.Lv);
            float hold = 2.4f + 0.45f * s.Lv;
            for (int i = 0; i < targets.Count; i++)
            {
                EnemyActor e = targets[i];
                Func<Vector2> at = Track(w, e);
                float t0 = 0.2f + i * 0.025f;
                var drop = Pic(s.G, InkSprites.Load("Vfx/water_shot_03"), top, 46f, glue);
                Vector2 start = top + new Vector2(0f, -40f);
                s.A.Tween(t0, T - t0, k =>
                {
                    Vector2 to = at();
                    Vector2 p = Vector2.Lerp(start, to, k) + new Vector2(0f, 160f * k * (1f - k));
                    drop.rectTransform.anchoredPosition = p;
                    drop.enabled = k > 0f && k < 1f;
                });
                var puddle = Pic(s.G, InkSprites.Load("Vfx/dot_ripple"), at(), 130f * s.Pow, glue);
                puddle.preserveAspect = true;
                s.A.Tween(T, hold, k =>
                {
                    puddle.rectTransform.anchoredPosition = at() + new Vector2(0f, -18f);
                    float grow = Ease.OutBack(Mathf.Clamp01(k * 6f));
                    float wob = 1f + 0.05f * Mathf.Sin(k * 18f);
                    puddle.rectTransform.localScale = new Vector3(grow * wob, grow / wob, 1f);
                    float a = k < 0.8f ? 0.9f : 0.9f * (1f - k) / 0.2f;
                    puddle.color = Alpha(glue, a);
                });
            }
            s.A.At(T, () =>
            {
                UiConfetti.Sparks(s.G, top, glue, 10 + s.Lv * 2, 380f);
                Shock(s, new Vector2(0f, 0f), Alpha(glue, 0.8f), 120f, s.Half.x * 2.2f, T, 0.5f);
            });
            return T + hold + 0.1f;
        }

        // ---------- 高级：闹钟、能量饮料、冰块 ----------

        // 闹钟砸到屏幕中间狂响，一圈圈声波推满全屏；响完那一下全场褪色，时间停住。
        float Halt(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            Banner(s, d, 0f);
            Vector2 mid = new Vector2(0f, s.Half.y * 0.16f);
            var clock = Pic(s.G, InkSprites.Ui(d.Id), key, 220f * Mathf.Sqrt(s.Pow), Color.white);
            s.A.Move(clock.rectTransform, key, mid, 0f, 0.22f, Ease.OutBack)
                .Pop(clock.transform, 0f, 0.26f, 0.3f)
                .Tween(0.22f, T - 0.18f, k =>
                {
                    float wig = Mathf.Sin(k * 70f) * 16f * (1f - 0.3f * k);
                    clock.rectTransform.localRotation = Quaternion.Euler(0f, 0f, wig);
                    float b = 1f + 0.05f * Mathf.Abs(Mathf.Sin(k * 35f));
                    clock.rectTransform.localScale = new Vector3(b, b, 1f);
                })
                .Tween(T, 0.5f, k =>
                {
                    clock.rectTransform.localRotation = Quaternion.identity;
                    float sc = 1f + 0.25f * k;
                    clock.rectTransform.localScale = new Vector3(sc, sc, 1f);
                    clock.color = Alpha(Color.white, 1f - k);
                });
            int waves = 3 + (s.Lv >= 4 ? 1 : 0);
            for (int i = 0; i < waves; i++)
                Shock(s, mid, InkTheme.Word, 160f, s.Half.y * 2.6f, 0.24f + i * 0.13f, 0.55f);

            float stop = 1.2f + 0.4f * s.Lv;
            s.A.At(T, () =>
            {
                Flash(s, Color.white, T, 0.45f, 0.35f);
                var gray = Fill(s.G, new Color(0.55f, 0.6f, 0.72f, 0f));
                gray.transform.SetAsFirstSibling();
                s.A.Tween(Rel(s, T), stop, k =>
                {
                    float a = k < 0.1f ? k / 0.1f : k > 0.85f ? (1f - k) / 0.15f : 1f;
                    gray.color = new Color(0.55f, 0.6f, 0.72f, 0.22f * a);
                });
                // 每只怪头顶冒一圈小星星：被定住了。
                var list = Alive(w, 12);
                for (int i = 0; i < list.Count; i++)
                {
                    Func<Vector2> at = Track(w, list[i]);
                    var stars = Pic(s.G, InkSprites.Load("Vfx/dot_stun"), at(), 80f, Color.white);
                    s.A.Tween(Rel(s, T), stop, k =>
                    {
                        stars.rectTransform.anchoredPosition = at() + new Vector2(0f, 46f);
                        stars.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 20f) * 10f);
                        stars.color = Alpha(Color.white, k > 0.85f ? (1f - k) / 0.15f : Mathf.Clamp01(k * 10f));
                    });
                }
            });
            Vignette(s, InkTheme.Word, 0f, T + 0.2f, 0.5f);
            return T + stop + 0.1f;
        }

        // 易拉罐飞到炮上一口闷，炮口轰一团火；之后整屏四角烧着火边，直到增益结束。
        float Rage(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            Banner(s, d, 0f);
            Vector2 gun = BattleHud.WorldToCanvas(_layer, new Vector3(0f, GameConstants.EmitterY, 0f));
            Vector2 above = gun + new Vector2(70f, 150f);
            var can = Pic(s.G, InkSprites.Ui(d.Id), key, 130f, Color.white);
            s.A.Move(can.rectTransform, key, above, 0f, 0.22f, Ease.OutBack)
                .Rotate(can.transform, 0f, 40f, 0.2f, 0.12f)
                .Tween(0.3f, T - 0.3f, k => Squash(can.transform, 0.12f * Mathf.Sin(k * Mathf.PI * 6f)))
                .Tween(T, 0.3f, k =>
                {
                    can.rectTransform.anchoredPosition = Vector2.Lerp(above, gun, Ease.InQuad(k));
                    float sc = 1f - 0.8f * k;
                    can.rectTransform.localScale = new Vector3(sc, sc, 1f);
                    can.enabled = k < 1f;
                });

            float dur = 4f + s.Lv;
            int mul = 2 + (s.Lv - 1) / 2;
            s.A.At(T + 0.22f, () =>
            {
                Boom(s, "Vfx/fire_hit_", 4, gun, 340f * s.Pow, Color.white, T + 0.22f, 0.35f);
                Flash(s, InkTheme.Hex("FF8A3C"), T + 0.22f, 0.3f, 0.35f);
                UiConfetti.Sparks(s.G, gun, InkTheme.Fire, 18 + s.Lv * 3, 700f);
                var big = ResultKit.Headline(s.G, "mul", "伤害 ×" + mul, 54, gun + new Vector2(0f, 230f),
                    InkTheme.Hex("FFE25C"), InkTheme.Hex("A8300E"), 4f);
                s.A.Pop(big.transform, 0f, 0.35f)
                    .Move(big.rectTransform, gun + new Vector2(0f, 230f), gun + new Vector2(0f, 300f), 0.8f, 0.6f)
                    .Fade(big, 0.9f, 0.5f, 1f, 0f);
            });
            // 燃着的火边跟着真实增益走，被别处续上或提前结束也对得上。
            var edge = Pic(s.G, Vig(), Vector2.zero, 10f, Alpha(InkTheme.Fire, 0f));
            Stretch(edge.rectTransform);
            edge.preserveAspect = false;
            var flames = new List<Image>();
            int n = 2 + (s.Lv >= 3 ? 2 : 0);
            for (int i = 0; i < n; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float y = -s.Half.y + 40f + (i / 2) * 170f;
                var f = Pic(s.G, InkSprites.Load("Vfx/burn_body_00"), new Vector2(side * (s.Half.x - 50f), y), 220f, Color.white);
                f.color = Alpha(Color.white, 0f);
                flames.Add(f);
            }
            Sprite[] burn = Frames("Vfx/burn_body_", 4);
            s.A.Tween(T + 0.22f, dur + 0.4f, k =>
            {
                float t = k * (dur + 0.4f);
                bool on = w != null && w.RageTime > 0f;
                float a = on ? Mathf.Clamp01(t * 4f) : 0f;
                edge.color = Alpha(InkTheme.Fire, a * (0.42f + 0.12f * Mathf.Sin(t * 7f)));
                for (int i = 0; i < flames.Count; i++)
                {
                    flames[i].color = Alpha(Color.white, a * 0.95f);
                    if (burn.Length > 0) flames[i].sprite = burn[(int)(t * 12f + i) % burn.Length];
                }
            });
            return T + 0.22f + dur + 0.5f;
        }

        // 冰块飞到顶上咔嚓裂开；屏幕上沿一排冰棱砸下来，每只怪身上炸开冰花，雪花飘一阵。
        float Frost(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            Banner(s, d, 0f);
            Vector2 top = new Vector2(0f, s.Half.y * 0.52f);
            var cube = Pic(s.G, InkSprites.Ui(d.Id), key, 150f, Color.white);
            s.A.Move(cube.rectTransform, key, top, 0f, 0.25f, Ease.OutBack)
                .Rotate(cube.transform, -30f, 360f, 0f, 0.3f)
                .Punch(cube.transform, 0.34f, 0.3f, 0.2f)
                .Tween(T - 0.08f, 0.2f, k =>
                {
                    float sc = 1f + 0.6f * k;
                    cube.rectTransform.localScale = new Vector3(sc, sc, 1f);
                    cube.color = Alpha(Color.white, 1f - k);
                });
            s.A.At(T - 0.08f, () =>
            {
                AudioBus.HitIce();
                Boom(s, "Vfx/hit_ice_", 4, top, 320f * s.Pow, Color.white, T - 0.08f, 0.35f);
            });

            // 冰棱：从屏幕上沿外面掉下来挂住。
            Sprite[] crust = Frames("Vfx/ice_crust_", 4);
            int cols = Mathf.CeilToInt(s.Half.x * 2f / 112f) + 1;
            for (int i = 0; i < cols; i++)
            {
                float x = -s.Half.x + i * 112f + UnityEngine.Random.Range(-10f, 10f);
                var ice = Pic(s.G, crust.Length > 0 ? crust[i % crust.Length] : null, Vector2.zero, 130f, Color.white);
                Vector2 from = new Vector2(x, s.Half.y + 80f);
                Vector2 to = new Vector2(x, s.Half.y - 52f + UnityEngine.Random.Range(-14f, 8f));
                float t0 = T - 0.12f + Mathf.Abs(i - cols * 0.5f) * 0.018f;
                float hold = 1.6f + 0.3f * s.Lv;
                s.A.Move(ice.rectTransform, from, to, t0, 0.22f, Ease.OutBounce)
                    .Fade(ice, t0 + hold, 0.4f, 1f, 0f);
            }

            float slow = 1.6f + 0.3f * s.Lv;
            s.A.At(T, () =>
            {
                Flash(s, InkTheme.IceHi, T, 0.5f, 0.35f);
                var list = Alive(w, 12);
                for (int i = 0; i < list.Count; i++)
                {
                    Vector2 at = BattleHud.WorldToCanvas(_layer, list[i].Pos);
                    Boom(s, "Vfx/hit_ice_", 4, at, (150f + 20f * s.Lv), Color.white, T + i * 0.03f, 0.3f);
                }
                Snow(s, 16 + s.Lv * 5, T, slow + 0.4f);
            });
            Vignette(s, InkTheme.Ice, T - 0.1f, slow + 0.4f, 0.6f);
            return T + slow + 0.9f;
        }

        // ---------- 稀有：大扫把、辣椒酱、急救包 ----------

        // 亮相完，扫把从左下抡一个大弧扫到右上，一路墨浪翻滚，整屏速度线。
        float Sweep(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            const float Go = 0.62f;
            Image hero = Showcase(s, d, key, Go);
            Vector2 a = new Vector2(-s.Half.x - 40f, -s.Half.y * 0.1f);
            Vector2 b = new Vector2(s.Half.x + 60f, s.Half.y * 0.38f);
            Vector2 mid = Vector2.zero;
            s.A.Tween(Go, 0.12f, k =>
            {
                hero.rectTransform.anchoredPosition = Vector2.Lerp(mid, a, Ease.OutCubic(k));
                hero.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 70f, k));
            });
            float swing = T + 0.18f - (Go + 0.12f);
            Vector2 last = a;
            s.A.Tween(Go + 0.12f, swing, k =>
            {
                float e = Ease.InQuad(k);
                Vector2 p = Vector2.Lerp(a, b, e) + new Vector2(0f, -180f * Mathf.Sin(e * Mathf.PI));
                hero.rectTransform.anchoredPosition = p;
                hero.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(70f, -40f, e));
                float sc = 1.5f;
                hero.rectTransform.localScale = new Vector3(sc, sc, 1f);
                if (k > 0f) hero.enabled = k < 1f;
                // 扫过的路上翻起墨浪和尘土。
                if ((p - last).sqrMagnitude > 70f * 70f && k > 0f && k < 1f)
                {
                    last = p;
                    float now = s.A.Now;
                    Boom(s, "Vfx/hit_ink_", 4, p + new Vector2(0f, -40f), 200f * s.Pow, InkTheme.Outline, now, 0.34f);
                    var dust = Pic(s.G, InkFx.SoftDisc(), p + new Vector2(0f, -60f), 160f, Alpha(InkTheme.Hex("B9A893"), 0.7f));
                    s.A.Tween(0f, 0.6f, q =>
                    {
                        float sc2 = 1f + 0.8f * q;
                        dust.rectTransform.localScale = new Vector3(sc2, sc2, 1f);
                        dust.color = Alpha(InkTheme.Hex("B9A893"), 0.7f * (1f - q));
                    });
                }
            });

            s.A.At(T, () =>
            {
                AudioBus.Boom();
                Flash(s, Color.white, T, 0.55f, 0.32f);
                SpeedLines(s, 10 + s.Lv * 2, T, 0.45f);
                var list = Alive(w, 14);
                for (int i = 0; i < list.Count; i++)
                {
                    Vector2 at = BattleHud.WorldToCanvas(_layer, list[i].Pos);
                    Boom(s, "Vfx/hit_ink_", 4, at, 170f, InkTheme.Outline, T + 0.02f * i, 0.3f);
                }
                UiConfetti.Sparks(s.G, Vector2.zero, ItemCatalog.QualityColor(d.Quality), 20 + s.Lv * 4, 900f);
                UiConfetti.Sparks(s.G, Vector2.zero, InkTheme.Word, 14 + s.Lv * 3, 700f);
            });
            return T + 1.2f;
        }

        // 亮相完，辣椒酱飞到那一列顶上倒扣，一道红酱浇下去，整列腾起火柱，烧到灼烧结束。
        float Splash(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key)
        {
            float T = c.Impact;
            const float Go = 0.62f;
            Image hero = Showcase(s, d, key, Go);
            int col = c.Column >= 0 ? c.Column : GameConstants.Columns / 2;
            float x = BattleHud.WorldToCanvas(_layer, new Vector3(FieldLayout.ColumnX(col), 0f, 0f)).x;
            float yTop = s.Half.y - 210f;
            float yBot = BattleHud.WorldToCanvas(_layer, new Vector3(0f, FieldLayout.GridTop, 0f)).y;
            Vector2 pour = new Vector2(x, yTop);
            s.A.Tween(Go, 0.14f, k =>
            {
                hero.rectTransform.anchoredPosition = Vector2.Lerp(Vector2.zero, pour, Ease.OutCubic(k));
                hero.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 170f, k));
                float sc = Mathf.Lerp(1f, 0.7f, k);
                hero.rectTransform.localScale = new Vector3(sc, sc, 1f);
            });
            s.A.Tween(Go + 0.14f, T - Go - 0.14f, k => Squash(hero.transform, 0.12f * Mathf.Sin(k * Mathf.PI * 4f)));
            s.A.Fade(hero, T + 0.25f, 0.3f, 1f, 0f);

            Color sauce = InkTheme.Hex("D8261C");
            var stream = Bar(s.G, new Vector2(46f * s.Pow, 10f), sauce);
            var srt = stream.rectTransform;
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = pour + new Vector2(0f, -50f);
            float len = Mathf.Max(80f, pour.y - 50f - yBot);
            s.A.Tween(Go + 0.16f, T + 0.4f - (Go + 0.16f), k =>
            {
                float now = Go + 0.16f + k * (T + 0.4f - Go - 0.16f);
                float grow = Mathf.Clamp01((now - Go - 0.16f) / (T - Go - 0.16f));
                float thin = now > T ? 1f - (now - T) / 0.4f : 1f;
                srt.sizeDelta = new Vector2(46f * s.Pow * thin, len * Ease.InQuad(grow));
                stream.enabled = thin > 0.02f && grow > 0f;
            });

            float burn = 2.2f + 0.6f * s.Lv;
            s.A.At(T, () =>
            {
                AudioBus.HitFire();
                Flash(s, InkTheme.Hex("FF5A2A"), T, 0.38f, 0.35f);
                var glow = Pic(s.G, InkFx.SoftDisc(), new Vector2(x, (pour.y + yBot) * 0.5f - 60f), 10f, Alpha(InkTheme.Fire, 0f));
                glow.preserveAspect = false;
                glow.rectTransform.sizeDelta = new Vector2(320f * s.Pow, len + 360f);
                glow.transform.SetAsFirstSibling();
                Sprite[] fire = Frames("Vfx/burn_body_", 4);
                int n = Mathf.Max(3, Mathf.CeilToInt((pour.y - yBot + 200f) / 100f));
                var flames = new Image[n];
                for (int i = 0; i < n; i++)
                {
                    float y = yBot - 140f + i * 100f;
                    flames[i] = Pic(s.G, fire.Length > 0 ? fire[i % fire.Length] : null, new Vector2(x + UnityEngine.Random.Range(-14f, 14f), y),
                        150f * s.Pow, Alpha(Color.white, 0f));
                    Boom(s, "Vfx/fire_hit_", 4, new Vector2(x, y), 200f * s.Pow, Color.white, T + i * 0.025f, 0.3f);
                }
                s.A.Tween(Rel(s, T), burn, k =>
                {
                    float t = k * burn;
                    float a = Mathf.Clamp01(t * 6f) * (k > 0.8f ? (1f - k) / 0.2f : 1f);
                    glow.color = Alpha(InkTheme.Fire, 0.45f * a * (0.85f + 0.15f * Mathf.Sin(t * 9f)));
                    for (int i = 0; i < flames.Length; i++)
                    {
                        flames[i].color = Alpha(Color.white, a);
                        if (fire.Length > 0) flames[i].sprite = fire[(int)(t * 14f + i) % fire.Length];
                        float wob = 1f + 0.08f * Mathf.Sin(t * 11f + i);
                        flames[i].rectTransform.localScale = new Vector3(wob, 2f - wob, 1f);
                    }
                });
                Embers(s, new Vector2(x, yBot), 70f * s.Pow, Mathf.Max(100f, pour.y - yBot), 10 + s.Lv * 4, T, burn);
            });
            return T + burn + 0.3f;
        }

        // 亮相完，急救包飞到血条前打开，金光一闪，一颗大心落进血条，整屏飘起绿十字。
        float Mend(Stage s, ItemDef d, BattleWorld.ItemCast c, BattleWorld w, Vector2 key, Vector2 hearts)
        {
            float T = c.Impact;
            const float Go = 0.62f;
            Image hero = Showcase(s, d, key, Go);
            Vector2 at = hearts + new Vector2(0f, 120f);
            s.A.Tween(Go, 0.18f, k =>
            {
                hero.rectTransform.anchoredPosition = Vector2.Lerp(Vector2.zero, at, Ease.OutCubic(k));
                float sc = Mathf.Lerp(1f, 0.75f, k);
                hero.rectTransform.localScale = new Vector3(sc, sc, 1f);
            });
            s.A.Punch(hero.transform, Go + 0.2f, 0.3f, 0.2f);
            s.A.Fade(hero, T + 0.05f, 0.25f, 1f, 0f);

            Color heal = InkTheme.Hex("5CD07A");
            s.A.At(T - 0.06f, () =>
            {
                Boom(s, "Vfx/hit_gold_", 4, at, 300f * s.Pow, Color.white, T - 0.06f, 0.35f);
                Flash(s, heal, T, 0.3f, 0.4f);
            });
            var heart = Pic(s.G, InkSprites.Load("icon_heart"), at, 120f, Color.white);
            s.A.Pop(heart.transform, T, 0.3f)
                .Move(heart.rectTransform, at, hearts, T + 0.32f, 0.3f, Ease.InQuad)
                .Fade(heart, T + 0.6f, 0.12f, 1f, 0f);
            s.A.At(T + 0.62f, () =>
            {
                Shock(s, hearts, InkTheme.Heart, 60f, 360f * s.Pow, T + 0.62f, 0.4f);
                UiConfetti.Sparks(s.G, hearts, InkTheme.Heart, 16 + s.Lv * 3, 560f);
                UiConfetti.Sparks(s.G, hearts, heal, 12 + s.Lv * 2, 440f);
            });
            Crosses(s, heal, 14 + s.Lv * 4, T, 1.6f);
            Vignette(s, heal, T - 0.05f, 1.2f, 0.45f);
            return T + 2f;
        }

        // ---------- 排场 ----------

        // 普通：键边上冒一个品质色的小名牌。
        void Tag(Stage s, ItemDef d, Vector2 key)
        {
            Color q = ItemCatalog.QualityColor(d.Quality);
            var t = ResultKit.Headline(s.G, "tag", d.Name + "！", 34, key + new Vector2(-150f, 20f),
                Color.white, ItemCatalog.QualityDeep(d.Quality), 3f);
            t.alignment = TextAnchor.MiddleCenter;
            Vector2 p = key + new Vector2(-150f, 20f);
            s.A.Pop(t.transform, 0f, 0.28f)
                .Move(t.rectTransform, p, p + new Vector2(0f, 40f), 0.35f, 0.5f)
                .Fade(t, 0.55f, 0.3f, 1f, 0f);
            UiConfetti.Sparks(s.G, key, q, 8, 320f);
        }

        // 高级：屏幕上三分之一横过一条品质色的名字横幅。
        void Banner(Stage s, ItemDef d, float at)
        {
            Color q = ItemCatalog.QualityColor(d.Quality);
            Color deep = ItemCatalog.QualityDeep(d.Quality);
            Vector2 p = new Vector2(0f, s.Half.y * 0.42f);
            var band = UiKit.Stroke(s.G, "band", p, new Vector2(360f, 82f), Pin.Center, 5f, null, q, 41f);
            var icon = UiKit.Icon(band, InkSprites.Ui(d.Id), new Vector2(-128f, 4f), 92f);
            var name = ResultKit.Headline(band, "n", d.Name + "！", 40, new Vector2(30f, 0f), Color.white, deep, 3f);
            name.rectTransform.sizeDelta = new Vector2(260f, 60f);
            var sh = s.G.Find("band_sh");
            s.A.Tween(at, 0.3f, k =>
            {
                float x = Mathf.Lerp(-s.Half.x - 220f, 0f, Ease.OutBack(k));
                band.anchoredPosition = new Vector2(x, p.y);
                if (sh is RectTransform r) r.anchoredPosition = band.anchoredPosition + new Vector2(0f, -8f);
            })
                .Punch(icon.transform, at + 0.28f, 0.25f, 0.3f)
                .Tween(at + 0.9f, 0.25f, k =>
                {
                    float x = Mathf.Lerp(0f, s.Half.x + 240f, Ease.InQuad(k));
                    band.anchoredPosition = new Vector2(x, p.y);
                    if (sh is RectTransform r) r.anchoredPosition = band.anchoredPosition + new Vector2(0f, -8f);
                });
            UiConfetti.Sparks(s.G, p, q, 10, 420f);
        }

        // 稀有：全场压暗，品质光芒转起来，大图标从键里蹦到正中，名字砸下来。返回那个大图标，
        // 调用方在 go 秒后把它带去干活。
        Image Showcase(Stage s, ItemDef d, Vector2 key, float go)
        {
            Color q = ItemCatalog.QualityColor(d.Quality);
            Color deep = ItemCatalog.QualityDeep(d.Quality);
            var dim = Fill(s.G, new Color(0.08f, 0.05f, 0.12f, 0f));
            s.A.Tween(0f, go + 0.3f, k =>
            {
                float t = k * (go + 0.3f);
                float a = t < 0.15f ? t / 0.15f : t > go ? 1f - (t - go) / 0.3f : 1f;
                dim.color = new Color(0.08f, 0.05f, 0.12f, 0.5f * a);
            });
            var rays = Pic(s.G, InkSprites.Load("Ui/result_rays"), Vector2.zero, 520f, Alpha(q, 0f));
            s.A.Spin(rays.transform, 50f)
                .Tween(0.05f, go + 0.1f, k =>
                {
                    float a = k < 0.2f ? k / 0.2f : k > 0.8f ? (1f - k) / 0.2f : 1f;
                    rays.color = Alpha(Color.Lerp(q, Color.white, 0.25f), 0.85f * a);
                    float sc = 0.6f + 0.4f * Ease.OutBack(Mathf.Clamp01(k * 3f));
                    rays.rectTransform.localScale = new Vector3(sc, sc, 1f);
                });
            var halo = Pic(s.G, InkFx.SoftDisc(), Vector2.zero, 380f, Alpha(q, 0f));
            s.A.Fade(halo, 0.05f, 0.2f, 0f, 0.8f).Fade(halo, go, 0.25f, 0.8f, 0f);

            var hero = Pic(s.G, InkSprites.Ui(d.Id), key, 240f, Color.white);
            s.A.Move(hero.rectTransform, key, Vector2.zero, 0f, 0.26f, Ease.OutBack)
                .Pop(hero.transform, 0f, 0.3f, 0.3f)
                .Breathe(hero.transform, 0.32f, 0.04f, 2.2f);

            var pill = UiKit.Stroke(s.G, "q", new Vector2(0f, -178f), new Vector2(124f, 40f), Pin.Center, 3f, null, q, 20f);
            var pillSh = s.G.Find("q_sh");
            if (pillSh != null) Destroy(pillSh.gameObject);
            var ql = UiKit.Label(pill, "t", ItemCatalog.QualityName(d.Quality), 24, Vector2.zero, new Vector2(124f, 40f));
            ql.color = Color.white;
            UiKit.Bold(ql);
            var name = ResultKit.Headline(s.G, "name", d.Name, 64, new Vector2(0f, -240f), Color.white, deep, 4f);
            s.A.Pop(pill, 0.12f, 0.3f).Pop(name.transform, 0.16f, 0.34f)
                .Tween(go, 0.22f, k =>
                {
                    if (k <= 0f) return;
                    name.color = Alpha(Color.white, 1f - k);
                    pill.localScale = Vector3.one * (1f - k);
                });
            s.A.At(0.2f, () =>
            {
                UiConfetti.Sparks(s.G, Vector2.zero, q, 22, 760f);
                UiConfetti.Sparks(s.G, Vector2.zero, InkTheme.Word, 14, 600f);
            });
            // 呼吸是循环轨，会一直改亮相图标的缩放。出手时换一个替身接着演。
            var actor = Pic(s.G, hero.sprite, Vector2.zero, 240f, Color.white);
            actor.enabled = false;
            s.A.At(go, () =>
            {
                hero.enabled = false;
                actor.enabled = true;
            });
            return actor;
        }

        // ---------- 零件 ----------

        // 跟着一只怪走；它死了就停在死的地方。没有目标时落在格子上方一点。
        Func<Vector2> Track(BattleWorld w, EnemyActor e)
        {
            RectTransform layer = _layer;
            Vector2 last = BattleHud.WorldToCanvas(layer, new Vector3(0f, FieldLayout.GridTop + 2f, 0f));
            return () =>
            {
                if (e != null && !e.Dead && layer != null) last = BattleHud.WorldToCanvas(layer, e.Pos);
                return last;
            };
        }

        static List<EnemyActor> Alive(BattleWorld w, int max)
        {
            var list = new List<EnemyActor>();
            if (w == null) return list;
            for (int i = 0; i < w.Enemies.Count && list.Count < max; i++)
                if (!w.Enemies[i].Dead) list.Add(w.Enemies[i]);
            return list;
        }

        static Image Pic(Transform parent, Sprite sprite, Vector2 pos, float size, Color color)
        {
            var img = UiKit.Icon(parent, sprite, pos, size);
            img.color = color;
            return img;
        }

        static Image Bar(Transform parent, Vector2 size, Color color)
        {
            var go = new GameObject("bar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Image Fill(RectTransform parent, Color color)
        {
            var img = Bar(parent, Vector2.zero, color);
            Stretch(img.rectTransform);
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // 时间轴上的绝对时刻换成「从现在起再等多久」。At 回调里排的轨都要过这一道。
        static float Rel(Stage s, float at) => Mathf.Max(0f, at - s.A.Now);

        static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, Mathf.Clamp01(a));

        static void Squash(Transform t, float w)
        {
            if (t == null) return;
            t.localScale = new Vector3(1f + w, 1f - w, 1f);
        }

        static readonly Dictionary<string, Sprite[]> _frames = new Dictionary<string, Sprite[]>();

        static Sprite[] Frames(string prefix, int count)
        {
            if (_frames.TryGetValue(prefix, out Sprite[] got)) return got;
            var list = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                Sprite sp = InkSprites.Load(prefix + i.ToString("00"));
                if (sp != null) list.Add(sp);
            }
            got = list.ToArray();
            _frames[prefix] = got;
            return got;
        }

        // 一张序列帧特效在 pos 放一遍：边放边涨一点，末段淡出。
        static void Boom(Stage s, string prefix, int count, Vector2 pos, float size, Color tint, float at, float dur)
        {
            Sprite[] f = Frames(prefix, count);
            if (f.Length == 0) return;
            var img = Pic(s.G, f[0], pos, size, Alpha(tint, 0f));
            s.A.Tween(Rel(s, at), dur, k =>
            {
                img.sprite = f[Mathf.Min(f.Length - 1, (int)(k * f.Length))];
                float sc = 0.7f + 0.45f * Ease.OutCubic(k);
                img.rectTransform.localScale = new Vector3(sc, sc, 1f);
                img.color = Alpha(tint, k <= 0f ? 0f : k > 0.7f ? (1f - k) / 0.3f : 1f);
                if (k >= 1f) img.enabled = false;
            });
        }

        static void Flash(Stage s, Color c, float at, float peak, float dur)
        {
            var img = Fill(s.G, Alpha(c, 0f));
            s.A.Tween(Rel(s, at), dur, k =>
            {
                float a = k <= 0f ? 0f : k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
                img.color = Alpha(c, peak * a);
                if (k >= 1f) img.enabled = false;
            });
        }

        static void Vignette(Stage s, Color c, float at, float hold, float peak)
        {
            var img = Pic(s.G, Vig(), Vector2.zero, 10f, Alpha(c, 0f));
            img.preserveAspect = false;
            Stretch(img.rectTransform);
            img.transform.SetAsFirstSibling();
            s.A.Tween(Rel(s, at), hold, k =>
            {
                float a = k <= 0f ? 0f : k < 0.12f ? k / 0.12f : k > 0.75f ? (1f - k) / 0.25f : 1f;
                img.color = Alpha(c, peak * a);
            });
        }

        static void Shock(Stage s, Vector2 pos, Color c, float from, float to, float at, float dur)
        {
            var img = Pic(s.G, InkFx.SoftRing(), pos, from, Alpha(c, 0f));
            s.A.Tween(Rel(s, at), dur, k =>
            {
                float r = Mathf.Lerp(from, to, Ease.OutCubic(k));
                img.rectTransform.sizeDelta = new Vector2(r, r);
                img.color = Alpha(c, k <= 0f ? 0f : 0.9f * (1f - k));
            });
        }

        // 带重力的碎片，往上崩再落下。
        static void Debris(Stage s, Vector2 pos, Color c, int count, float at)
        {
            for (int i = 0; i < count; i++)
            {
                var bit = Bar(s.G, new Vector2(UnityEngine.Random.Range(10f, 16f), UnityEngine.Random.Range(18f, 26f)), Alpha(c, 0f));
                Vector2 v = new Vector2(UnityEngine.Random.Range(-420f, 420f), UnityEngine.Random.Range(380f, 820f));
                float spin = UnityEngine.Random.Range(-900f, 900f);
                Color tint = i % 3 == 0 ? InkTheme.Word : c;
                s.A.Tween(Rel(s, at), 1.1f, k =>
                {
                    float t = k * 1.1f;
                    bit.rectTransform.anchoredPosition = pos + v * t + new Vector2(0f, -1300f * t * t);
                    bit.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                    bit.color = Alpha(tint, k <= 0f ? 0f : k > 0.7f ? (1f - k) / 0.3f : 1f);
                });
            }
        }

        static void Snow(Stage s, int count, float at, float dur)
        {
            for (int i = 0; i < count; i++)
            {
                float x = UnityEngine.Random.Range(-s.Half.x, s.Half.x);
                float size = UnityEngine.Random.Range(16f, 30f);
                var flake = Pic(s.G, Star(), new Vector2(x, s.Half.y), size, Alpha(Color.white, 0f));
                float speed = UnityEngine.Random.Range(260f, 460f);
                float delay = UnityEngine.Random.Range(0f, dur * 0.4f);
                float sway = UnityEngine.Random.Range(1.5f, 3f);
                Color tint = i % 2 == 0 ? Color.white : InkTheme.IceHi;
                s.A.Tween(Rel(s, at + delay), dur, k =>
                {
                    float t = k * dur;
                    flake.rectTransform.anchoredPosition = new Vector2(x + Mathf.Sin(t * sway) * 30f, s.Half.y + 20f - speed * t);
                    flake.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 120f);
                    flake.color = Alpha(tint, k <= 0f ? 0f : k > 0.7f ? (1f - k) / 0.3f : 0.95f);
                });
            }
        }

        static void Embers(Stage s, Vector2 bottom, float width, float height, int count, float at, float dur)
        {
            for (int i = 0; i < count; i++)
            {
                float x = bottom.x + UnityEngine.Random.Range(-width, width);
                float y = bottom.y + UnityEngine.Random.Range(0f, height);
                var e = Pic(s.G, Dot(), new Vector2(x, y), UnityEngine.Random.Range(8f, 16f), Alpha(InkTheme.Word, 0f));
                float delay = UnityEngine.Random.Range(0f, dur * 0.7f);
                float rise = UnityEngine.Random.Range(140f, 260f);
                Color tint = i % 2 == 0 ? InkTheme.Word : InkTheme.Fire;
                s.A.Tween(Rel(s, at + delay), 0.9f, k =>
                {
                    e.rectTransform.anchoredPosition = new Vector2(x + Mathf.Sin(k * 9f) * 10f, y + rise * k);
                    e.color = Alpha(tint, k <= 0f ? 0f : 1f - k);
                });
            }
        }

        static void Crosses(Stage s, Color c, int count, float at, float dur)
        {
            for (int i = 0; i < count; i++)
            {
                float x = UnityEngine.Random.Range(-s.Half.x * 0.9f, s.Half.x * 0.9f);
                float y0 = UnityEngine.Random.Range(-s.Half.y * 0.8f, 0f);
                float size = UnityEngine.Random.Range(28f, 52f);
                var cross = Pic(s.G, Plus(), new Vector2(x, y0), size, Alpha(c, 0f));
                float delay = UnityEngine.Random.Range(0f, 0.6f);
                float rise = UnityEngine.Random.Range(220f, 420f);
                Color tint = i % 3 == 0 ? Color.white : c;
                s.A.Tween(Rel(s, at + delay), dur, k =>
                {
                    cross.rectTransform.anchoredPosition = new Vector2(x + Mathf.Sin(k * 6f + i) * 16f, y0 + rise * Ease.OutCubic(k));
                    float sc = Ease.OutBack(Mathf.Clamp01(k * 4f));
                    cross.rectTransform.localScale = new Vector3(sc, sc, 1f);
                    cross.color = Alpha(tint, k <= 0f ? 0f : k > 0.6f ? (1f - k) / 0.4f : 1f);
                });
            }
        }

        static void SpeedLines(Stage s, int count, float at, float dur)
        {
            for (int i = 0; i < count; i++)
            {
                float y = UnityEngine.Random.Range(-s.Half.y * 0.8f, s.Half.y * 0.8f);
                float len = UnityEngine.Random.Range(220f, 480f);
                var line = Bar(s.G, new Vector2(len, UnityEngine.Random.Range(4f, 9f)), Alpha(Color.white, 0f));
                line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 14f);
                float delay = UnityEngine.Random.Range(0f, 0.12f);
                s.A.Tween(Rel(s, at + delay), dur, k =>
                {
                    float x = Mathf.Lerp(-s.Half.x - len, s.Half.x + len, k);
                    line.rectTransform.anchoredPosition = new Vector2(x, y + 0.25f * x);
                    line.color = Alpha(Color.white, k <= 0f ? 0f : 0.85f * Mathf.Sin(k * Mathf.PI));
                });
            }
        }

        // ---------- 程序生成的小图 ----------

        static Sprite _dot, _vig, _plus;

        static Sprite Dot() => _dot != null ? _dot : (_dot = InkArt.Heap(InkShape.Circle, Color.white, 32));

        static Sprite Star() => ShotSparks.SpriteOf(SparkKind.Star) ?? ShotSparks.SpriteOf(SparkKind.Spark);

        // 四周浓、中间空的晕边。拉伸铺满全屏用。
        static Sprite Vig()
        {
            if (_vig != null) return _vig;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];
            float mid = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x - mid) / mid, dy = Mathf.Abs(y - mid) / mid;
                float d = Mathf.Max(dx, dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, d));
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _vig = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return _vig;
        }

        // 圆角十字，急救包飘的那种。
        static Sprite Plus()
        {
            if (_plus != null) return _plus;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];
            float mid = (n - 1) * 0.5f;
            const float arm = 7.5f, len = 21f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x - mid), dy = Mathf.Abs(y - mid);
                float h = Mathf.Max(dx - len, dy - arm);
                float v = Mathf.Max(dx - arm, dy - len);
                float d = Mathf.Min(h, v);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            _plus = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return _plus;
        }
    }
}
