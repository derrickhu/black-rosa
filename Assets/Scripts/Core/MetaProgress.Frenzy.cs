using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public struct FrenzyGrant
    {
        public int Kills;
        public int Foil;
        public int Ink;
        public bool NewBest;
        public bool HasSeal;
        public SealDrop Seal;
    }

    // 百鬼狂潮与字帖（偏爱 / 封存）。
    public sealed partial class MetaProgress
    {
        public int FrenzyDay;
        public int FrenzyPlays;
        public bool FrenzyAdDone;
        public int FrenzyBestKills;
        public int FrenzyBestFoil;
        // 字帖：单字存 CardId (1..IdCount-1)，成词存 CopyWordBase + WordId，避免新增单字顶歪成词编码。
        public const int CopyWordBase = 1000;
        public int[] CopyFav = new int[0];
        public int[] CopyBan = new int[0];

        public static int CopyKeyOf(CodexEntry e) =>
            e.Kind == CodexKind.Word ? CopyWordBase + e.Index : e.Index;
        public static int CopyKeyOf(CardId id) => (int)id;
        public static int CopyKeyOf(WordId w) => CopyWordBase + (int)w;

        public bool FrenzyOpen => ChapterCleared(GameConstants.FrenzyOpenChapter);

        public int FrenzyPlaysCap => GameConstants.FrenzyPlaysPerDay + (FrenzyAdDone ? GameConstants.FrenzyAdPlays : 0);

        public int FrenzyPlaysLeft
        {
            get
            {
                RollFrenzy();
                return Mathf.Max(0, FrenzyPlaysCap - FrenzyPlays);
            }
        }

        public bool CanFrenzyAd
        {
            get { RollFrenzy(); return !FrenzyAdDone; }
        }

        void NormalizeFrenzy()
        {
            FrenzyPlays = Mathf.Max(0, FrenzyPlays);
            FrenzyBestKills = Mathf.Max(0, FrenzyBestKills);
            FrenzyBestFoil = Mathf.Max(0, FrenzyBestFoil);
            CopyFav = SanitizeKeys(CopyFav, GameConstants.CopybookFavMax, null);
            CopyBan = SanitizeKeys(CopyBan, GameConstants.CopybookBanMax, CopyFav);
        }

        static int[] SanitizeKeys(int[] src, int cap, int[] exclude)
        {
            if (src == null || src.Length == 0) return new int[0];
            var list = new List<int>(cap);
            for (int i = 0; i < src.Length && list.Count < cap; i++)
            {
                int k = src[i];
                // 兼容早期用 CardCatalog.IdCount(37) + WordId 编码的成词键值
                if (k >= CardCatalog.IdCount && k < CardCatalog.IdCount + CardCatalog.WordCount)
                    k = CopyWordBase + (k - CardCatalog.IdCount);
                bool validGlyph = k > 0 && k < CardCatalog.IdCount && CardCatalog.Get((CardId)k).Wake != CardWake.WordPart;
                bool validWord = k > CopyWordBase && k < CopyWordBase + CardCatalog.WordCount;
                if ((!validGlyph && !validWord) || list.Contains(k)) continue;
                if (exclude != null && System.Array.IndexOf(exclude, k) >= 0) continue;
                list.Add(k);
            }
            return list.ToArray();
        }

        bool RollFrenzy()
        {
            int today = Today;
            if (FrenzyDay == today) return false;
            FrenzyDay = today;
            FrenzyPlays = 0;
            FrenzyAdDone = false;
            return true;
        }

        public bool SpendFrenzyPlay()
        {
            if (FrenzyPlaysLeft <= 0) return false;
            FrenzyPlays++;
            Save();
            return true;
        }

        public bool GrantFrenzyAdPlays()
        {
            RollFrenzy();
            if (FrenzyAdDone) return false;
            FrenzyAdDone = true;
            Save();
            return true;
        }

        // 百鬼狂潮结算：HUD 实时显示的累计金箔（击破档位 + 不倒金翁喷落）直接入账，外加一笔墨；
        // 击破达标且已开放印章掉落时，额外赠一枚随机印章。
        public FrenzyGrant ApplyFrenzy(int kills, int earnedFoil)
        {
            var g = new FrenzyGrant { Kills = Mathf.Max(0, kills) };
            int baseFoil = Mathf.Clamp(earnedFoil, 4, BattleWorld.FrenzyFoilCap);
            int ink = 28 + Mathf.Min(92, g.Kills / 2);
            g.Foil = baseFoil;
            g.Ink = ink;
            if (g.Kills > FrenzyBestKills)
            {
                FrenzyBestKills = g.Kills;
                g.NewBest = true;
            }
            if (g.Foil > FrenzyBestFoil) FrenzyBestFoil = g.Foil;
            if (StudyOpen) Foil += g.Foil;
            Ink += g.Ink;
            if (SealDropOpen && g.Kills >= StageCatalog.FrenzySealKillNeed)
            {
                SealDrop drop = SealCatalog.Roll(SealCatalog.Source.Gold);
                var one = new List<SealDrop>(1) { drop };
                GrantSeals(one);
                g.HasSeal = true;
                g.Seal = drop;
            }
            Save();
            return g;
        }

        public void DoubleFrenzyReward(int extraFoil, int extraInk)
        {
            if (StudyOpen && extraFoil > 0) Foil += extraFoil;
            if (extraInk > 0) Ink += extraInk;
            if (StudyOpen && extraFoil > 0 && extraFoil * 2 > FrenzyBestFoil)
                FrenzyBestFoil = extraFoil * 2;
            Save();
        }

        // ---- 字帖（偏爱 / 封存） ----

        public int CopyFavCount => CopyFav != null ? CopyFav.Length : 0;
        public int CopyBanCount => CopyBan != null ? CopyBan.Length : 0;
        public int CopyTotalCount => CopyFavCount + CopyBanCount;

        public bool IsCopyFav(CodexEntry e) => HasCopyKey(CopyFav, CopyKeyOf(e));
        public bool IsCopyBan(CodexEntry e) => HasCopyKey(CopyBan, CopyKeyOf(e));

        static bool HasCopyKey(int[] arr, int key) =>
            arr != null && key > 0 && System.Array.IndexOf(arr, key) >= 0;

        public bool ToggleCopyFav(CodexEntry e)
        {
            if (!StudyOpen || !StudyOwned(e)) return false;
            int key = CopyKeyOf(e);
            if (HasCopyKey(CopyFav, key))
            {
                CopyFav = RemoveKey(CopyFav, key);
                Save();
                return true;
            }
            if (CopyFavCount >= GameConstants.CopybookFavMax) return false;
            CopyBan = RemoveKey(CopyBan, key);
            CopyFav = AppendKey(CopyFav, key);
            Save();
            return true;
        }

        public bool ToggleCopyBan(CodexEntry e)
        {
            if (!StudyOpen || !StudyOwned(e)) return false;
            int key = CopyKeyOf(e);
            if (HasCopyKey(CopyBan, key))
            {
                CopyBan = RemoveKey(CopyBan, key);
                Save();
                return true;
            }
            if (CopyBanCount >= GameConstants.CopybookBanMax) return false;
            CopyFav = RemoveKey(CopyFav, key);
            CopyBan = AppendKey(CopyBan, key);
            Save();
            return true;
        }

        public void ClearCopybook()
        {
            if (CopyFavCount == 0 && CopyBanCount == 0) return;
            CopyFav = new int[0];
            CopyBan = new int[0];
            Save();
        }

        static int[] AppendKey(int[] src, int key)
        {
            int n = src != null ? src.Length : 0;
            var dst = new int[n + 1];
            for (int i = 0; i < n; i++) dst[i] = src[i];
            dst[n] = key;
            return dst;
        }

        static int[] RemoveKey(int[] src, int key)
        {
            if (src == null || src.Length == 0) return new int[0];
            var list = new List<int>(src.Length);
            for (int i = 0; i < src.Length; i++)
                if (src[i] != key) list.Add(src[i]);
            return list.ToArray();
        }

        public bool CardBanned(CardId id)
        {
            if (!StudyOpen || CopyBanCount == 0 || id == CardId.None) return false;
            if (HasCopyKey(CopyBan, CopyKeyOf(id))) return true;
            CardDef def = CardCatalog.Get(id);
            if (def.Wake == CardWake.WordPart && def.Word != WordId.None)
                return HasCopyKey(CopyBan, CopyKeyOf(def.Word));
            return false;
        }

        public float CopybookLuck(CardId id)
        {
            if (!StudyOpen || CopyFavCount == 0 || id == CardId.None) return 1f;
            if (HasCopyKey(CopyFav, CopyKeyOf(id))) return GameConstants.CopybookFavMul;
            CardDef def = CardCatalog.Get(id);
            if (def.Wake == CardWake.WordPart && def.Word != WordId.None && HasCopyKey(CopyFav, CopyKeyOf(def.Word)))
                return GameConstants.CopybookFavMul;
            return 1f;
        }

        static readonly (WordId Word, CardId A, CardId B, int BaseScore)[] SkipWordPairs =
        {
            (WordId.Sweep, CardId.Heng, CardId.Sao, 18),
            (WordId.Crush, CardId.Qian, CardId.Jun, 16),
            (WordId.Cleave, CardId.Link, CardId.Slash, 15),
            (WordId.ArrowRain, CardId.Myriad, CardId.Arrow, 14),
            (WordId.InstantKill, CardId.Sec, CardId.Kill, 12),
            (WordId.Knockback, CardId.Strike, CardId.Back, 8),
        };

        // 无尽破阵直通开局预铺：从玩家当前可用字池中优先挑 2 组主力成词（★2）摆在第 2、3 列，
        // 严格尊重字帖偏爱/封存、已镶嵌印章品质与字卡等级；不足 2 组时用高练度单字补齐。
        public PresetGlyph[] BuildEndlessSkipPreset(CardId[] rawPool)
        {
            CardId[] activePool = StudyOpen ? AccountPool(null) : rawPool;
            var poolSet = new HashSet<CardId>();
            if (activePool != null)
                for (int i = 0; i < activePool.Length; i++) poolSet.Add(activePool[i]);

            var wordPicks = new List<(CardId A, CardId B, int Score)>();
            for (int i = 0; i < SkipWordPairs.Length; i++)
            {
                var p = SkipWordPairs[i];
                if (!poolSet.Contains(p.A) || !poolSet.Contains(p.B)) continue;
                if (CardBanned(p.A) || CardBanned(p.B)) continue;
                int score = p.BaseScore;
                if (StudyOpen && HasCopyKey(CopyFav, CopyKeyOf(p.Word))) score += 10000;
                if (StudyOpen && WordLevel != null && (int)p.Word < WordLevel.Length)
                    score += WordLevel[(int)p.Word] * 25;
                if (SealOpen && WordSlotSeal != null)
                {
                    int baseIdx = (int)p.Word * SealCatalog.SlotsPer;
                    for (int k = 0; k < SealCatalog.SlotsPer && baseIdx + k < WordSlotSeal.Length; k++)
                        score += WordSlotSeal[baseIdx + k] * 120;
                }
                wordPicks.Add((p.A, p.B, score));
            }
            wordPicks.Sort((x, y) => y.Score.CompareTo(x.Score));

            var presets = new List<PresetGlyph>(4);
            int[] cols = { 2, 3 };
            int pairUsed = Mathf.Min(2, wordPicks.Count);
            for (int i = 0; i < pairUsed; i++)
            {
                presets.Add(new PresetGlyph(cols[i], 0, wordPicks[i].A, 2));
                presets.Add(new PresetGlyph(cols[i], 1, wordPicks[i].B, 2));
            }

            if (pairUsed < 2 && activePool != null)
            {
                var singles = new List<(CardId Id, int Score)>();
                for (int i = 0; i < activePool.Length; i++)
                {
                    CardId id = activePool[i];
                    if (id == CardId.None || CardBanned(id)) continue;
                    if (CardCatalog.Get(id).Wake == CardWake.WordPart) continue;
                    int score = 10;
                    if (StudyOpen && HasCopyKey(CopyFav, CopyKeyOf(id))) score += 10000;
                    if (StudyOpen && GlyphLevel != null && (int)id < GlyphLevel.Length)
                        score += GlyphLevel[(int)id] * 25;
                    if (SealOpen && SlotSeal != null)
                    {
                        int baseIdx = (int)id * SealCatalog.SlotsPer;
                        for (int k = 0; k < SealCatalog.SlotsPer && baseIdx + k < SlotSeal.Length; k++)
                            score += SlotSeal[baseIdx + k] * 120;
                    }
                    singles.Add((id, score));
                }
                singles.Sort((x, y) => y.Score.CompareTo(x.Score));
                int sIdx = 0;
                for (int c = pairUsed; c < 2; c++)
                for (int r = 0; r < 2 && sIdx < singles.Count; r++)
                {
                    presets.Add(new PresetGlyph(cols[c], r, singles[sIdx++].Id, 2));
                }
            }
            return presets.ToArray();
        }
    }
}
