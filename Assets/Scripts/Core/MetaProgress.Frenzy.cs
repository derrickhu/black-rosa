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
        // 字帖：存放 SealCatalog.KeyOf(entry) 编码（字 = CardId，词 = CardCatalog.IdCount + WordId）。
        public int[] CopyFav = new int[0];
        public int[] CopyBan = new int[0];

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
            int maxKey = SealCatalog.KeyCount;
            for (int i = 0; i < src.Length && list.Count < cap; i++)
            {
                int k = src[i];
                if (k <= 0 || k >= maxKey || list.Contains(k)) continue;
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
            int baseFoil = Mathf.Max(4, earnedFoil);
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

        public bool IsCopyFav(CodexEntry e) => HasCopyKey(CopyFav, SealCatalog.KeyOf(e));
        public bool IsCopyBan(CodexEntry e) => HasCopyKey(CopyBan, SealCatalog.KeyOf(e));

        static bool HasCopyKey(int[] arr, int key) =>
            arr != null && key > 0 && System.Array.IndexOf(arr, key) >= 0;

        public bool ToggleCopyFav(CodexEntry e)
        {
            if (!StudyOpen || !StudyOwned(e)) return false;
            int key = SealCatalog.KeyOf(e);
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
            int key = SealCatalog.KeyOf(e);
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
            if (HasCopyKey(CopyBan, SealCatalog.KeyOf(id))) return true;
            CardDef def = CardCatalog.Get(id);
            if (def.Wake == CardWake.WordPart && def.Word != WordId.None)
                return HasCopyKey(CopyBan, SealCatalog.KeyOf(def.Word));
            return false;
        }

        public float CopybookLuck(CardId id)
        {
            if (!StudyOpen || CopyFavCount == 0 || id == CardId.None) return 1f;
            if (HasCopyKey(CopyFav, SealCatalog.KeyOf(id))) return GameConstants.CopybookFavMul;
            CardDef def = CardCatalog.Get(id);
            if (def.Wake == CardWake.WordPart && def.Word != WordId.None && HasCopyKey(CopyFav, SealCatalog.KeyOf(def.Word)))
                return GameConstants.CopybookFavMul;
            return 1f;
        }
    }
}
