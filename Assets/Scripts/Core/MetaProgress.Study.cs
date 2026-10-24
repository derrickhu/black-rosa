using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 字卡和金箔。数值在 StudyCatalog。
    public sealed partial class MetaProgress
    {
        public int Foil;
        // 下标 CardId / WordId。成词的半边字不升级，一直是 0。
        public int[] GlyphLevel = new int[CardCatalog.IdCount];
        public int[] WordLevel = new int[CardCatalog.WordCount];
        // 墨换金箔、看广告翻倍、看广告试用：哪一天、当天用了几次。
        public int StudyDay;
        public int SwapCount;
        public bool SwapAdDone;
        public bool SwapGemDone;
        public int TrialCount;
        // 下一局要试用的字卡：Kind 是 CodexKind + 1，0 表示没有。
        public int TrialKind;
        public int TrialId;
        // 开放后点开过字卡页没有。没点过，底栏页签挂红点。
        public bool StudySeen;

        public bool StudyOpen => StudyCatalog.IsOpen(ClearedCount());
        public int StudyCap => StudyCatalog.CapOf(ClearedCount());

        void NormalizeStudy()
        {
            GlyphLevel = Fit(GlyphLevel, CardCatalog.IdCount);
            WordLevel = Fit(WordLevel, CardCatalog.WordCount);
            for (int i = 0; i < GlyphLevel.Length; i++)
                GlyphLevel[i] = Mathf.Clamp(GlyphLevel[i], 0, StudyCatalog.MaxLevel);
            for (int i = 0; i < WordLevel.Length; i++)
                WordLevel[i] = Mathf.Clamp(WordLevel[i], 0, StudyCatalog.MaxLevel);
            Foil = Mathf.Max(0, Foil);
            if (TrialKind < 0 || TrialKind > 2) TrialKind = 0;
        }

        bool RollStudy()
        {
            int today = Today;
            if (StudyDay == today) return false;
            StudyDay = today;
            SwapCount = 0;
            SwapAdDone = false;
            SwapGemDone = false;
            TrialCount = 0;
            // 试用只在当天有效。挂着没打出来的，过天清掉，不带到第二天。
            TrialKind = 0;
            TrialId = 0;
            return true;
        }

        public int StudyLevel(CodexEntry e)
        {
            if (e.Kind == CodexKind.Glyph)
                return GlyphLevel != null && e.Index >= 0 && e.Index < GlyphLevel.Length ? GlyphLevel[e.Index] : 0;
            if (e.Kind == CodexKind.Word)
                return WordLevel != null && e.Index >= 0 && e.Index < WordLevel.Length ? WordLevel[e.Index] : 0;
            return 0;
        }

        public bool StudyOwned(CodexEntry e) => CodexKnows(e.Kind, e.Index);

        public enum StudyBlock { None, Locked, NotOwned, Max, Cap, Foil }

        public StudyBlock UpgradeBlock(CodexEntry e)
        {
            if (!StudyOpen) return StudyBlock.Locked;
            if (!StudyOwned(e)) return StudyBlock.NotOwned;
            int lv = StudyLevel(e);
            if (lv >= StudyCatalog.MaxLevel) return StudyBlock.Max;
            if (lv >= StudyCap) return StudyBlock.Cap;
            if (Foil < StudyCatalog.CostOf(lv)) return StudyBlock.Foil;
            return StudyBlock.None;
        }

        public bool UpgradeStudy(CodexEntry e)
        {
            if (UpgradeBlock(e) != StudyBlock.None) return false;
            int lv = StudyLevel(e);
            Foil -= StudyCatalog.CostOf(lv);
            if (e.Kind == CodexKind.Glyph) GlyphLevel[e.Index] = lv + 1;
            else WordLevel[e.Index] = lv + 1;
            Save();
            return true;
        }

        // 底栏红点：有字现在就升得动。
        public bool AnyStudyReady
        {
            get
            {
                if (!StudyOpen) return false;
                if (!StudySeen) return true;
                IReadOnlyList<CodexEntry> all = StudyCatalog.All;
                for (int i = 0; i < all.Count; i++)
                    if (UpgradeBlock(all[i]) == StudyBlock.None) return true;
                return false;
            }
        }

        public void MarkStudySeen()
        {
            if (StudySeen || !StudyOpen) return;
            StudySeen = true;
            Save();
        }

        // ---- 墨换金箔 ----

        public int SwapLeft => Mathf.Max(0, StudyCatalog.SwapPerDay - SwapCount);

        public int SwapCost => StudyCatalog.SwapInkAt(SwapCount);

        public bool CanSwap => StudyOpen && SwapLeft > 0 && Ink >= SwapCost;

        public int SwapInk()
        {
            if (!CanSwap) return 0;
            Ink -= SwapCost;
            SwapCount++;
            Foil += StudyCatalog.SwapFoil;
            Save();
            return StudyCatalog.SwapFoil;
        }

        // 换完之后看广告再白拿一份，一天一次。
        public bool CanSwapAd => StudyOpen && SwapCount > 0 && !SwapAdDone;

        public int GrantSwapAd()
        {
            if (!CanSwapAd) return 0;
            SwapAdDone = true;
            Foil += StudyCatalog.SwapFoil;
            Save();
            return StudyCatalog.SwapFoil;
        }

        // 钻石换金箔，一天一次。
        public bool CanSwapGem => StudyOpen && !SwapGemDone && Diamond >= StudyCatalog.GemSwapCost;

        public int SwapGem()
        {
            if (!StudyOpen || SwapGemDone) return 0;
            if (!SpendDiamond(StudyCatalog.GemSwapCost)) return 0;
            SwapGemDone = true;
            Foil += StudyCatalog.GemSwapFoil;
            Save();
            return StudyCatalog.GemSwapFoil;
        }

        public void AddFoil(int n)
        {
            if (n <= 0) return;
            Foil += n;
            Save();
        }

        // ---- 试用 ----

        public int TrialLeft => Mathf.Max(0, StudyCatalog.TrialPerDay - TrialCount);

        public bool HasTrial(CodexEntry e) => TrialKind == (int)e.Kind + 1 && TrialId == e.Index;

        public bool TrialWaiting => TrialKind != 0;

        public string TrialTitle()
        {
            if (TrialKind == (int)CodexKind.Glyph + 1) return CodexCatalog.Title(CodexKind.Glyph, TrialId);
            if (TrialKind == (int)CodexKind.Word + 1) return CodexCatalog.Title(CodexKind.Word, TrialId);
            return "";
        }

        public bool CanTrial(CodexEntry e) =>
            StudyOpen && StudyOwned(e) && TrialLeft > 0 && !HasTrial(e);

        public void GrantTrial(CodexEntry e)
        {
            if (RollStudy()) Save();
            if (!CanTrial(e)) return;
            TrialCount++;
            TrialKind = (int)e.Kind + 1;
            TrialId = e.Index;
            Save();
        }

        // 这一局如果真的用上了试用的字或词，才把试用用掉。没出现、没选，留到今天后面的局。
        public void NoteTrialUse(CardId placed, WordId formed)
        {
            bool glyph = TrialKind == (int)CodexKind.Glyph + 1 && TrialId == (int)placed;
            bool word = formed != WordId.None && TrialKind == (int)CodexKind.Word + 1 && TrialId == (int)formed;
            if (!glyph && !word) return;
            TrialKind = 0;
            TrialId = 0;
            Save();
        }

        // 开局取这一局的等级。试用先挂上，不在这里用掉：没打出来就还在。
        // 每日残局大家同一盘，不吃字卡。trialGlyph / trialWord 是这一局在试用的那一个（0 没有）。
        public void TakeStudy(bool daily, out int[] glyph, out int[] word, out int trialGlyph, out int trialWord)
        {
            glyph = null;
            word = null;
            trialGlyph = 0;
            trialWord = 0;
            if (RollStudy()) Save();
            if (daily || !StudyOpen) return;
            glyph = new int[CardCatalog.IdCount];
            word = new int[CardCatalog.WordCount];
            for (int i = 0; i < glyph.Length; i++)
            {
                bool part = i == 0 || CardCatalog.Get((CardId)i).Wake == CardWake.WordPart;
                glyph[i] = part ? -1 : GlyphLevel[i];
            }
            word[0] = -1;
            for (int i = 1; i < word.Length; i++) word[i] = WordLevel[i];
            if (TrialKind == 0) return;
            if (TrialKind == (int)CodexKind.Glyph + 1 && TrialId > 0 && TrialId < glyph.Length && glyph[TrialId] >= 0)
            {
                glyph[TrialId] += StudyCatalog.TrialBonus;
                trialGlyph = TrialId;
            }
            else if (TrialKind == (int)CodexKind.Word + 1 && TrialId > 0 && TrialId < word.Length)
            {
                word[TrialId] += StudyCatalog.TrialBonus;
                trialWord = TrialId;
            }
        }
    }
}
