using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 字卡升一级的演出。挂在详情面板 board 最上层，自带 UiAnim：面板重绘会清掉主动画，这里不受影响。
    // 节拍跟 study_up 音效走：前 0.3 秒金箔从价钱那里吸进卡、卡蓄力发亮；
    // 0.3 秒爆点换成新等级的卡面，白闪、冲击环、火花、等级字冲出来。品质变了再加光芒、横幅，满级撒彩带。
    public sealed class StudyUpFx : MonoBehaviour
    {
        public const float Hit = 0.3f;
        const float Life = 2.1f;
        const int FoilBits = 6;

        RectTransform _root;
        UiAnim _a;

        // reveal 在爆点被调用：重绘面板，返回新卡。
        public static void Play(RectTransform board, Vector2 boardHome, RectTransform oldCard, RectTransform from,
            int level, bool tierUp, Func<RectTransform> reveal)
        {
            var go = new GameObject("study_up_fx", typeof(RectTransform));
            go.transform.SetParent(board, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();
            var fx = go.AddComponent<StudyUpFx>();
            fx._root = rt;
            fx._a = UiAnim.On(fx);
            fx.Run(board, boardHome, oldCard, from, level, tierUp, reveal);
        }

        // t 的中心在 space 里的位置，按 space 的矩形中心算（和锚在中间的 Icon 的 anchoredPosition 同一套）。
        public static Vector2 Local(RectTransform space, RectTransform t)
        {
            Vector3 world = t.TransformPoint(t.rect.center);
            return (Vector2)space.InverseTransformPoint(world) - space.rect.center;
        }

        void Run(RectTransform board, Vector2 boardHome, RectTransform oldCard, RectTransform from,
            int level, bool tierUp, Func<RectTransform> reveal)
        {
            StudyCatalog.Tier tier = StudyCatalog.TierOf(level);
            Color col = StudyCatalog.TierColor(tier);
            Color deep = StudyCatalog.TierDeep(tier);
            Color hot = Color.Lerp(col, Color.white, 0.55f);
            Vector2 card = oldCard != null ? Local(_root, oldCard) : Vector2.zero;
            Vector2 size = oldCard != null ? oldCard.rect.size : new Vector2(210f, 270f);

            // 蓄力：卡后面一团光慢慢亮，卡自己胀一点、抖起来。
            var glow = Pic(InkFx.SoftDisc(), card, size.y * 1.7f, Alpha(hot, 0f));
            _a.Tween(0f, Hit, k =>
            {
                glow.color = Alpha(hot, 0.6f * k);
                glow.rectTransform.localScale = Vector3.one * (0.7f + 0.35f * k);
                if (oldCard == null) return;
                float s = 1f + 0.07f * Ease.InQuad(k);
                oldCard.localScale = new Vector3(s, s, 1f);
                oldCard.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 55f) * 2.5f * k);
            })
                .Tween(Hit, 0.45f, k =>
                {
                    glow.color = Alpha(hot, 0.6f * (1f - k));
                    glow.rectTransform.localScale = Vector3.one * (1.05f + 0.5f * Ease.OutCubic(k));
                });

            // 金箔从价钱那一行飞进卡心，左右交替绕一点弧。
            if (from != null)
            {
                Vector2 a = Local(_root, from);
                for (int i = 0; i < FoilBits; i++)
                {
                    float delay = i * 0.028f;
                    float dur = Hit - delay - 0.02f;
                    float side = i % 2 == 0 ? -1f : 1f;
                    Vector2 ctrl = (a + card) * 0.5f + new Vector2(side * (120f + 30f * i), 60f);
                    var bit = Pic(FoilArt.Icon, a, 44f, Color.white);
                    bit.enabled = false;
                    _a.Tween(delay, dur, k =>
                    {
                        bit.enabled = k > 0f && k < 1f;
                        float e = Ease.InQuad(k);
                        float u = 1f - e;
                        bit.rectTransform.anchoredPosition = u * u * a + 2f * u * e * ctrl + e * e * card;
                        float s = Mathf.Lerp(1f, 0.45f, e);
                        bit.rectTransform.localScale = new Vector3(s, s, 1f);
                        bit.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side * 360f * k);
                    });
                }
            }

            _a.At(Hit, () =>
            {
                RectTransform fresh = reveal?.Invoke();
                if (fresh != null)
                {
                    card = Local(_root, fresh);
                    size = fresh.rect.size;
                }
                Burst(card, size, level, col, deep, hot);
                if (tierUp) TierUp(board, boardHome, fresh, card, size, level, tier, col, deep);
            });
            _a.At(Life, () => Destroy(gameObject));
        }

        void Burst(Vector2 card, Vector2 size, int level, Color col, Color deep, Color hot)
        {
            // 卡形白闪，往外胀着淡掉。
            var flash = UiKit.Panel(_root, "flash", card, size, Color.white);
            var fi = flash.GetComponent<Image>();
            fi.sprite = UiSprites.Fill(UiSprites.Tier(size.x * 0.16f));
            fi.type = Image.Type.Sliced;
            fi.raycastTarget = false;
            _a.Tween(0f, 0.36f, k =>
            {
                fi.color = Alpha(Color.white, 0.95f * (1f - Ease.OutCubic(k)));
                float s = 1f + 0.14f * Ease.OutCubic(k);
                flash.localScale = new Vector3(s, s, 1f);
            });

            // 两道冲击环：品质色的大环在前，白色小环跟一拍。
            Ring(card, 230f, col, 0f, 0.5f, 3.6f);
            Ring(card, 180f, hot, 0.07f, 0.42f, 2.6f);

            UiConfetti.Sparks(_root, card, col, 20, 760f);
            UiConfetti.Sparks(_root, card, FoilArt.Face, 12, 560f);

            // 等级字从卡下半截冲出来，往上飘着淡掉。
            string text = level >= StudyCatalog.MaxLevel ? "MAX" : "Lv." + level;
            var lv = ResultKit.Headline(_root, "lv", text, 76, card + new Vector2(0f, -30f), Color.white, deep, 4f);
            var lt = lv.rectTransform;
            Vector2 a = card + new Vector2(0f, -30f), b = card + new Vector2(0f, 70f);
            _a.Pop(lt, 0f, 0.32f, 0.2f)
                .Tween(0.28f, 0.95f, k =>
                {
                    lt.anchoredPosition = Vector2.LerpUnclamped(a, b, Ease.OutCubic(k));
                    lv.color = Alpha(Color.white, k < 0.55f ? 1f : 1f - (k - 0.55f) / 0.45f);
                });
        }

        void TierUp(RectTransform board, Vector2 boardHome, RectTransform fresh, Vector2 card, Vector2 size,
            int level, StudyCatalog.Tier tier, Color col, Color deep)
        {
            AudioBus.StudyTier();

            // 光芒插在新卡后面（卡所在那一层的最底下），转一圈淡掉。
            if (fresh != null && fresh.parent is RectTransform body)
            {
                var rays = UiKit.Icon(body, InkSprites.Load("Ui/result_rays"), Local(body, fresh), size.y * 1.9f);
                rays.transform.SetAsFirstSibling();
                rays.color = Alpha(col, 0f);
                _a.Spin(rays.transform, 70f)
                    .Tween(0f, 1.7f, k =>
                    {
                        if (rays == null) return;
                        float al = k < 0.15f ? k / 0.15f : k > 0.7f ? (1f - k) / 0.3f : 1f;
                        rays.color = Alpha(Color.Lerp(col, Color.white, 0.2f), 0.9f * al);
                        float s = 0.5f + 0.5f * Ease.OutBack(Mathf.Clamp01(k * 4f));
                        rays.rectTransform.localScale = new Vector3(s, s, 1f);
                        if (k >= 1f) Destroy(rays.gameObject);
                    });
            }

            // 横幅压在卡底下：品质色药丸 + 白字。
            Vector2 at = card + new Vector2(0f, -size.y * 0.5f - 6f);
            var band = ResultKit.Group(_root, "tier", at, new Vector2(300f, 64f));
            var pill = UiKit.Stroke(band, "pill", Vector2.zero, new Vector2(300f, 64f), Pin.Center, 4f, deep, col, 32f);
            var t = ResultKit.Headline(pill, "t", "品质提升 · " + StudyCatalog.TierName(tier), 32, Vector2.zero,
                Color.white, deep, 3f);
            t.rectTransform.sizeDelta = new Vector2(300f, 60f);
            _a.Pop(band, 0.1f, 0.38f)
                .Tween(1.45f, 0.25f, k =>
                {
                    float s = 1f - Ease.InQuad(k);
                    band.localScale = new Vector3(s, s, 1f);
                });
            _a.At(0.1f, () => UiConfetti.Sparks(_root, at, col, 14, 520f));

            // 面板轻轻一震。home 由调用方给，连点也不会把面板抖偏。
            _a.Tween(0f, 0.28f, k =>
            {
                if (board == null) return;
                float w = (1f - k) * 10f;
                board.anchoredPosition = boardHome + new Vector2(Mathf.Sin(k * 60f) * w, Mathf.Cos(k * 47f) * w * 0.6f);
            });

            if (level >= StudyCatalog.MaxLevel && board.parent is RectTransform dim)
                UiConfetti.Burst(dim, 90);
        }

        void Ring(Vector2 at, float size, Color col, float delay, float dur, float grow)
        {
            var ring = Pic(InkFx.SoftRing(), at, size, Alpha(col, 0f));
            _a.Tween(delay, dur, k =>
            {
                if (k <= 0f) return;
                float e = Ease.OutCubic(k);
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, grow, e);
                ring.color = Alpha(col, 1f - e);
            });
        }

        Image Pic(Sprite s, Vector2 at, float size, Color c)
        {
            var img = UiKit.Icon(_root, s, at, size);
            img.color = c;
            return img;
        }

        static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
