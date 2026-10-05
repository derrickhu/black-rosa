using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 养成的三档庆祝：解锁新道具整屏放光；道具升级在详情弹窗里闪一下；词条升级只在那一行上亮一下。
    public static class ItemCelebrate
    {
        // 解锁新道具：压暗、品质色光芒、「获得新道具」飘带、图标弹出迸星、彩纸。
        public static void Unlock(RectTransform layer, MetaProgress meta, int item, Action done)
        {
            ItemDef d = ItemCatalog.Get(item);
            Color q = ItemCatalog.QualityColor(d.Quality);
            Color hi = Color.Lerp(q, Color.white, 0.55f);

            var dim = UiKit.Dimmer(layer);
            dim.name = "item_unlock";
            dim.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.03f, 0.84f);
            var anim = UiAnim.On(dim);
            ResultKit.SkipCatcher(dim, anim.Finish);
            var g = ResultKit.Group(dim, "g", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));

            var rays = ResultKit.Rays(g, new Vector2(0f, 120f), 820f, new Color(hi.r, hi.g, hi.b, 0.9f));
            anim.Fade(rays, 0f, 0.4f, 0f, 0.9f).Spin(rays.transform, 22f).Pop(rays.transform, 0f, 0.5f, 0.3f);
            var halo = UiKit.Icon(g, InkFx.SoftDisc(), new Vector2(0f, 120f), 420f);
            halo.color = new Color(q.r, q.g, q.b, 0.7f);
            anim.Fade(halo, 0.4f, 0.3f, 0f, 0.7f).Breathe(halo.transform, 0.8f, 0.06f, 1.1f);

            var rib = UiKit.Stroke(g, "rib", new Vector2(0f, 360f), new Vector2(360f, 66f), Pin.Center, 5f,
                fill: InkTheme.Seal, radius: 30f);
            var rt = UiKit.Label(rib, "t", "获得新道具", 34, Vector2.zero, new Vector2(360f, 66f));
            rt.color = Color.white;
            UiKit.Bold(rt);
            anim.Move(rib, new Vector2(0f, 760f), new Vector2(0f, 360f), 0.05f, 0.36f, Ease.OutBack)
                .Punch(rib, 0.41f, 0.12f, 0.3f)
                .At(0.38f, AudioBus.Stamp);

            var icon = UiKit.Icon(g, InkSprites.Ui(d.Id), new Vector2(0f, 120f), 240f);
            anim.Pop(icon.transform, 0.45f, 0.45f, 0f)
                .Breathe(icon.transform, 1.0f, 0.05f, 1.2f)
                .At(0.6f, () =>
                {
                    AudioBus.UnlockSting();
                    UiConfetti.Sparks(dim, new Vector2(0f, 120f), hi, 24, 660f);
                    UiConfetti.Burst(dim, 80);
                });

            var name = ResultKit.Headline(g, "name", d.Name, 60, new Vector2(0f, -60f),
                InkTheme.Hex("FFF3C8"), InkTheme.Hex("7A1E14"), 3f);
            var pill = UiKit.Panel(g, "q", new Vector2(0f, -124f), new Vector2(150f, 40f), q);
            var pi = pill.GetComponent<Image>();
            pi.sprite = UiSprites.Fill(UiSprites.Tier(20f));
            pi.type = Image.Type.Sliced;
            pi.raycastTarget = false;
            var qn = UiKit.Label(pill, "t", ItemCatalog.QualityName(d.Quality) + "道具", 24, Vector2.zero, new Vector2(150f, 40f));
            qn.color = Color.white;
            UiKit.Bold(qn);
            var desc = UiKit.Label(g, "desc", ItemCatalog.Blurb(d, 1, meta.ShotBase), 28,
                new Vector2(0f, -184f), new Vector2(620f, 40f));
            desc.color = InkTheme.Hex("FFE7B8");
            var when = UiKit.Label(g, "when", "自动触发：" + d.When, 24, new Vector2(0f, -230f), new Vector2(620f, 34f));
            when.color = InkTheme.Hex("CDBFA8");
            bool worn = meta.EquippedSlot(item) >= 0;
            var note = UiKit.Label(g, "note", worn ? "已装进道具栏，下一局就会用" : "道具栏满了，去道具页换上", 24,
                new Vector2(0f, -272f), new Vector2(620f, 34f));
            note.color = InkTheme.Hex("8FE3A2");
            UiKit.Bold(note);
            anim.Fade(name, 0.75f, 0.25f, 0f, 1f).Pop(name.transform, 0.75f, 0.3f, 1.6f)
                .Pop(pill, 0.9f, 0.28f, 0f)
                .Fade(desc, 0.95f, 0.25f, 0f, 1f)
                .Fade(when, 1.05f, 0.25f, 0f, 1f)
                .Fade(note, 1.15f, 0.25f, 0f, 1f);

            var ok = ResultKit.Group(g, "ok", new Vector2(0f, -380f), new Vector2(280f, 92f));
            UiKit.Btn(ok, "b", "收下", Vector2.zero, new Vector2(280f, 92f), () =>
            {
                AudioBus.Tap();
                UnityEngine.Object.Destroy(dim.gameObject);
                done?.Invoke();
            }, true);
            anim.Pop(ok, 1.25f, 0.3f).Breathe(ok, 1.6f, 0.045f, 1.3f);
        }

        // 道具升级：详情弹窗的展台闪一下，图标从大弹回，品质色迸星，飘一个「升级!」。
        public static void Upgrade(UiAnim anim, RectTransform layer, RectTransform stage, Image icon, Image halo,
            RectTransform lv, Color q)
        {
            AudioBus.CardRare();
            var flash = UiKit.Panel(stage, "flash", Vector2.zero, stage.rect.size, Color.white);
            var fi = flash.GetComponent<Image>();
            fi.sprite = UiSprites.Fill(UiSprites.Tier(24f));
            fi.type = Image.Type.Sliced;
            fi.raycastTarget = false;
            anim.Fade(fi, 0f, 0.45f, 0.85f, 0f)
                .Pop(icon.transform, 0f, 0.4f, 1.45f)
                .Fade(halo, 0f, 0.6f, 1f, 0.45f)
                .Punch(lv, 0.25f, 0.3f, 0.35f)
                .At(0.08f, () => UiConfetti.Sparks(layer, Local(layer, icon.rectTransform),
                    Color.Lerp(q, Color.white, 0.4f), 16, 520f));

            var up = ResultKit.Headline(stage, "up", "升级!", 44, new Vector2(0f, 20f),
                InkTheme.Hex("FFF3C8"), InkTheme.Hex("7A1E14"), 3f);
            var rt = up.rectTransform;
            anim.Pop(rt, 0.05f, 0.3f, 1.8f)
                .Move(rt, new Vector2(0f, 20f), new Vector2(0f, 90f), 0.35f, 0.7f, Ease.OutCubic)
                .Fade(up, 0.75f, 0.3f, 1f, 0f);
        }

        // 词条升级：那一行亮一下、轻轻顶一下，右侧飘「Lv.N」，几颗金星。
        public static void Row(RectTransform layer, RectTransform row, string text)
        {
            if (row == null || layer == null) return;
            var anim = UiAnim.On(row);
            Vector2 size = row.rect.size;
            var flash = UiKit.Panel(row, "up_flash", Vector2.zero, size, new Color(1f, 0.93f, 0.6f, 1f));
            var fi = flash.GetComponent<Image>();
            fi.sprite = UiSprites.Fill(UiSprites.Tier(24f));
            fi.type = Image.Type.Sliced;
            fi.raycastTarget = false;
            var up = ResultKit.Headline(row, "up_lv", text, 34, new Vector2(size.x * 0.18f, 0f),
                InkTheme.Hex("C8F0C4"), InkTheme.Hex("1F5A2E"), 2.5f);
            var rt = up.rectTransform;
            AudioBus.Chime();
            UiConfetti.Sparks(layer, Local(layer, row), InkTheme.GoldHi, 10, 420f);
            anim.Fade(fi, 0f, 0.5f, 0.55f, 0f)
                .Punch(row, 0f, 0.05f, 0.3f)
                .Pop(rt, 0f, 0.25f, 1.6f)
                .Move(rt, new Vector2(size.x * 0.18f, 0f), new Vector2(size.x * 0.18f, 56f), 0.3f, 0.6f, Ease.OutCubic)
                .Fade(up, 0.6f, 0.3f, 1f, 0f)
                .At(0.95f, () =>
                {
                    if (flash != null) UnityEngine.Object.Destroy(flash.gameObject);
                    if (up != null) UnityEngine.Object.Destroy(up.gameObject);
                });
        }

        static Vector2 Local(RectTransform layer, RectTransform target)
        {
            Vector3 world = target.TransformPoint(target.rect.center);
            return layer.InverseTransformPoint(world);
        }
    }
}
