using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 第二幕终章战场表现层：
    // 1) 「护」字底线金盾光栏；2) 判官「朱砂劫印」靶标；3) 魔尊「渊墨护壁」暗紫星璇。
    public sealed partial class BattleView
    {
        readonly List<SpriteRenderer> _guardBars = new List<SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> _seals = new Dictionary<int, SpriteRenderer>();

        void SyncFinale(BattleWorld w)
        {
            EnsureGuardBars();
            for (int c = 0; c < GameConstants.Columns; c++)
            {
                int charges = w.GuardCharges[c];
                SpriteRenderer sr = _guardBars[c];
                if (charges <= 0 && w.GuardFlash[c] <= 0.01f)
                {
                    sr.enabled = false;
                    continue;
                }
                sr.enabled = true;
                float flash = Mathf.Clamp01(w.GuardFlash[c] / 0.45f);
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 5.2f + c);
                float thick = 0.09f + charges * 0.035f + flash * 0.08f;
                sr.transform.position = new Vector3(FieldLayout.ColumnX(c), GameConstants.LeakY + 0.08f, 0f);
                sr.transform.localScale = new Vector3(GameConstants.CellWidth * 0.92f, thick, 1f);
                Color col = Color.Lerp(InkTheme.Gold, InkTheme.GoldHi, 0.45f + flash * 0.55f);
                col.a = charges > 0 ? Mathf.Clamp01(0.78f * pulse + flash * 0.22f) : flash * 0.75f;
                sr.color = col;
            }

            _seen.Clear();
            Sprite sealArt = InkSprites.Load("Vfx/judge_seal") ?? InkFx.SoftDisc();
            for (int i = 0; i < w.JudgeSeals.Count; i++)
            {
                JudgeSealActor s = w.JudgeSeals[i];
                _seen.Add(s.Id);
                float u = 1f - Mathf.Clamp01(s.Timer / Mathf.Max(0.1f, s.MaxTimer));
                float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * (6f + u * 10f) + s.Id);
                float punch = 1f + 0.22f * Mathf.Clamp01(s.Flash / 0.18f);
                Color tint = s.Flash > 0.02f
                    ? Color.Lerp(Color.white, InkTheme.GoldHi, 0.5f)
                    : Color.Lerp(Color.white, InkTheme.Explode, u * 0.28f);
                SpriteRenderer sr = Bind(_seals, s.Id, sealArt, s.Pos, 1.15f * pulse * punch, 12, tint);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 3.5f + s.Id) * 6f);
            }
            Purge(_seals);

            // 魔尊开启渊墨护壁时，身后旋开一轮暗紫金边漩涡。
            for (int i = 0; i < w.Enemies.Count; i++)
            {
                EnemyActor e = w.Enemies[i];
                if (e.Dead || !_enemies.TryGetValue(e.Id, out SpriteRenderer host) || host == null) continue;
                PaintAbyssVortex(host, e);
            }
        }

        void EnsureGuardBars()
        {
            if (_guardBars.Count == GameConstants.Columns) return;
            Sprite bar = InkArt.Heap(InkShape.Bar, Color.white, 64);
            for (int c = 0; c < GameConstants.Columns; c++)
            {
                var sr = Make("guardBar" + c, bar, new Vector3(FieldLayout.ColumnX(c), GameConstants.LeakY + 0.08f, 0f), 1f);
                sr.sortingOrder = 6;
                sr.enabled = false;
                InkFx.PaintSprite(sr, InkTheme.GoldHi);
                _guardBars.Add(sr);
            }
        }

        static void PaintAbyssVortex(SpriteRenderer host, EnemyActor e)
        {
            Transform t = host.transform.Find("abyssVortex");
            SpriteRenderer sr = t != null ? t.GetComponent<SpriteRenderer>() : null;
            if (e.AbyssVortex <= 0f)
            {
                if (sr != null) sr.enabled = false;
                return;
            }
            if (sr == null)
            {
                var go = new GameObject("abyssVortex");
                go.transform.SetParent(host.transform, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = InkFlowArt.VortexMask();
                InkFx.PaintSprite(sr, Color.white);
            }
            sr.enabled = true;
            sr.sortingOrder = host.sortingOrder - 1;
            sr.transform.localPosition = Vector3.zero;
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 160f);
            float pulse = 1.65f + 0.14f * Mathf.Sin(Time.unscaledTime * 7f);
            sr.transform.localScale = Vector3.one * pulse;
            sr.color = new Color(0.56f, 0.26f, 0.92f, 0.72f);
        }
    }
}
