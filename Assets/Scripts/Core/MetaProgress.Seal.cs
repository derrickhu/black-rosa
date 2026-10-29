using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 插槽和印章。数值在 SealCatalog。
    // 槽下标 = 条目号 × 3 + 第几个槽。SlotType 是 (int)SealType，0 是还没刷出；
    // SlotSeal 是镶着的印章品质 + 1，0 是空。镶上的印章从背包里扣掉，卸下或刷新时退回。
    public sealed partial class MetaProgress
    {
        public int[] SlotType = new int[SealCatalog.SlotCount];
        public int[] SlotSeal = new int[SealCatalog.SlotCount];
        // 背包按 类型 × 品质 叠，见 SealCatalog.BagIndex。
        public int[] SealBag = new int[SealCatalog.BagSize];
        // 当天看广告刷槽用了几次，跟着 StudyDay 一起过天清零。
        public int SealAdCount;
        // 背包里有几枚是打开背包之后才拿到的，字卡页「印章」挂红点。
        public int SealNew;

        // 插槽要通关第五章。掉落跟着字卡一起开，第五章打的时候先攒着。
        public bool SealOpen => SealCatalog.IsOpen(ClearedCount());
        public bool SealDropOpen => StudyOpen;

        // 字卡页的「印章」按钮：开了插槽，或者已经攒到过印章。
        public bool SealBagShown => SealOpen || SealTotal > 0 || AnySocketed;

        void NormalizeSeals()
        {
            SlotType = Fit(SlotType, SealCatalog.SlotCount);
            SlotSeal = Fit(SlotSeal, SealCatalog.SlotCount);
            SealBag = Fit(SealBag, SealCatalog.BagSize);
            for (int i = 0; i < SlotType.Length; i++)
            {
                if (SlotType[i] < 0 || SlotType[i] > SealCatalog.TypeCount) SlotType[i] = 0;
                if (SlotSeal[i] < 0 || SlotSeal[i] > SealCatalog.QualityCount || SlotType[i] == 0) SlotSeal[i] = 0;
            }
            for (int i = 0; i < SealBag.Length; i++) SealBag[i] = Mathf.Max(0, SealBag[i]);
            SealAdCount = Mathf.Clamp(SealAdCount, 0, SealCatalog.RerollAdPerDay);
            SealNew = Mathf.Max(0, SealNew);
        }

        // 到了等级、还没刷出类型的槽，现在刷。老档第一次开放插槽时一次补齐。
        bool FillSlots()
        {
            if (!SealOpen) return false;
            bool changed = false;
            IReadOnlyList<CodexEntry> all = StudyCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                CodexEntry e = all[i];
                int key = SealCatalog.KeyOf(e);
                int open = SealCatalog.SlotsAt(StudyLevel(e));
                for (int k = 0; k < open; k++)
                {
                    int s = key * SealCatalog.SlotsPer + k;
                    if (SlotType[s] != 0) continue;
                    SlotType[s] = (int)SealCatalog.RollSlot(key, HasLuck(key, k));
                    changed = true;
                }
            }
            return changed;
        }

        public void EnsureSlots()
        {
            if (FillSlots()) Save();
        }

        bool HasLuck(int key, int except)
        {
            for (int k = 0; k < SealCatalog.SlotsPer; k++)
                if (k != except && SlotType[key * SealCatalog.SlotsPer + k] == (int)SealType.Luck) return true;
            return false;
        }

        // ---- 读 ----

        public int SlotsOpen(CodexEntry e) => SealOpen ? SealCatalog.SlotsAt(StudyLevel(e)) : 0;

        public SealType SlotTypeOf(CodexEntry e, int k)
        {
            int s = SealCatalog.KeyOf(e) * SealCatalog.SlotsPer + k;
            return s >= 0 && s < SlotType.Length ? (SealType)SlotType[s] : SealType.None;
        }

        // 镶着的印章品质；空槽返回 false。
        public bool SlotSealOf(CodexEntry e, int k, out SealQuality q)
        {
            int s = SealCatalog.KeyOf(e) * SealCatalog.SlotsPer + k;
            q = SealQuality.Green;
            if (s < 0 || s >= SlotSeal.Length || SlotSeal[s] <= 0) return false;
            q = (SealQuality)(SlotSeal[s] - 1);
            return true;
        }

        public int BagCount(SealType t, SealQuality q)
        {
            if (t == SealType.None) return 0;
            int i = SealCatalog.BagIndex(t, q);
            return i >= 0 && i < SealBag.Length ? SealBag[i] : 0;
        }

        public int BagCount(SealType t)
        {
            int n = 0;
            for (int q = 0; q < SealCatalog.QualityCount; q++) n += BagCount(t, (SealQuality)q);
            return n;
        }

        public int SealTotal
        {
            get
            {
                int n = 0;
                for (int i = 0; i < SealBag.Length; i++) n += SealBag[i];
                return n;
            }
        }

        bool AnySocketed
        {
            get
            {
                for (int i = 0; i < SlotSeal.Length; i++) if (SlotSeal[i] > 0) return true;
                return false;
            }
        }

        // 这个字（或词）镶着字运没有。列表卡上的「运」看这个。
        public bool HasLuckSeal(CodexEntry e)
        {
            if (!SealOpen) return false;
            int open = SlotsOpen(e);
            for (int k = 0; k < open; k++)
                if (SlotTypeOf(e, k) == SealType.Luck && SlotSealOf(e, k, out _)) return true;
            return false;
        }

        // 有空槽、背包里正好有这种印章。列表卡挂红点。
        public bool CanFillSlot(CodexEntry e)
        {
            int open = SlotsOpen(e);
            for (int k = 0; k < open; k++)
            {
                SealType t = SlotTypeOf(e, k);
                if (t != SealType.None && !SlotSealOf(e, k, out _) && BagCount(t) > 0) return true;
            }
            return false;
        }

        // 三选一的字运倍率。词的字运作用在两个半边字上；同一个字两边都有时取大的。
        public float OfferLuck(CardId id)
        {
            if (!SealOpen) return 1f;
            float best = LuckAt(new CodexEntry(CodexKind.Glyph, (int)id));
            CardDef def = CardCatalog.Get(id);
            if (def.Wake == CardWake.WordPart && def.Word != WordId.None)
                best = Mathf.Max(best, LuckAt(new CodexEntry(CodexKind.Word, (int)def.Word)));
            return best;
        }

        float LuckAt(CodexEntry e)
        {
            int open = SlotsOpen(e);
            for (int k = 0; k < open; k++)
                if (SlotTypeOf(e, k) == SealType.Luck && SlotSealOf(e, k, out SealQuality q))
                    return SealCatalog.LuckOf(q);
            return 1f;
        }

        // 开局把印章摊平给战斗。每日残局、插槽没开返回 null。
        public SealPack TakeSeals(bool daily)
        {
            if (daily || !SealOpen) return null;
            var pack = new SealPack();
            IReadOnlyList<CodexEntry> all = StudyCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                CodexEntry e = all[i];
                int key = SealCatalog.KeyOf(e);
                int open = SlotsOpen(e);
                for (int k = 0; k < open; k++)
                {
                    SealType t = SlotTypeOf(e, k);
                    if (t != SealType.None && SlotSealOf(e, k, out SealQuality q)) pack.Add(key, t, q);
                }
            }
            return pack;
        }

        // ---- 改 ----

        public enum SealBlock { None, Locked, NoSlot, Empty, Diamond, AdUsed }

        public bool Socket(CodexEntry e, int k, SealQuality q)
        {
            if (!SealOpen || k < 0 || k >= SlotsOpen(e)) return false;
            SealType t = SlotTypeOf(e, k);
            if (t == SealType.None || BagCount(t, q) <= 0) return false;
            int s = SealCatalog.KeyOf(e) * SealCatalog.SlotsPer + k;
            if (SlotSeal[s] > 0) SealBag[SealCatalog.BagIndex(t, (SealQuality)(SlotSeal[s] - 1))]++;
            SealBag[SealCatalog.BagIndex(t, q)]--;
            SlotSeal[s] = (int)q + 1;
            Save();
            return true;
        }

        public bool Unsocket(CodexEntry e, int k)
        {
            if (!SealOpen || k < 0 || k >= SealCatalog.SlotsPer) return false;
            int s = SealCatalog.KeyOf(e) * SealCatalog.SlotsPer + k;
            if (SlotSeal[s] <= 0) return false;
            ReturnSeal(s);
            Save();
            return true;
        }

        void ReturnSeal(int s)
        {
            if (SlotSeal[s] <= 0 || SlotType[s] <= 0) return;
            SealBag[SealCatalog.BagIndex((SealType)SlotType[s], (SealQuality)(SlotSeal[s] - 1))]++;
            SlotSeal[s] = 0;
        }

        public int SealAdLeft => Mathf.Max(0, SealCatalog.RerollAdPerDay - SealAdCount);

        public SealBlock RerollBlock(CodexEntry e, int k, bool ad)
        {
            if (!SealOpen) return SealBlock.Locked;
            if (k < 0 || k >= SlotsOpen(e) || SlotTypeOf(e, k) == SealType.None) return SealBlock.NoSlot;
            if (ad) return SealAdLeft > 0 ? SealBlock.None : SealBlock.AdUsed;
            return Diamond >= SealCatalog.RerollDiamond ? SealBlock.None : SealBlock.Diamond;
        }

        // 只换类型。槽里的印章退回背包。可能刷出同一种。
        public SealType Reroll(CodexEntry e, int k, bool ad)
        {
            if (RollStudy()) Save();
            if (RerollBlock(e, k, ad) != SealBlock.None) return SealType.None;
            if (ad) SealAdCount++;
            else if (!SpendDiamond(SealCatalog.RerollDiamond)) return SealType.None;
            int key = SealCatalog.KeyOf(e);
            int s = key * SealCatalog.SlotsPer + k;
            ReturnSeal(s);
            SlotType[s] = (int)SealCatalog.RollSlot(key, HasLuck(key, k));
            Save();
            return (SealType)SlotType[s];
        }

        // 分解：picks 是背包下标 → 分几枚。返回得到的金箔。
        public int Scrap(IReadOnlyDictionary<int, int> picks)
        {
            int foil = 0;
            foreach (var kv in picks)
            {
                if (kv.Key < 0 || kv.Key >= SealBag.Length) continue;
                int n = Mathf.Clamp(kv.Value, 0, SealBag[kv.Key]);
                if (n <= 0) continue;
                SealBag[kv.Key] -= n;
                foil += n * SealCatalog.ScrapOf((SealQuality)(kv.Key % SealCatalog.QualityCount));
            }
            if (foil <= 0) return 0;
            Foil += foil;
            SealNew = Mathf.Min(SealNew, SealTotal);
            Save();
            return foil;
        }

        public void MarkSealsSeen()
        {
            if (SealNew == 0) return;
            SealNew = 0;
            Save();
        }

        // 掉落入账。调用方负责 Save。
        void GrantSeals(List<SealDrop> drops)
        {
            if (drops == null) return;
            for (int i = 0; i < drops.Count; i++)
            {
                SealDrop d = drops[i];
                if (d.Type == SealType.None) continue;
                SealBag[SealCatalog.BagIndex(d.Type, d.Quality)]++;
                SealNew++;
            }
        }

        // 测试面板用：每种类型、每个品质各发一枚。
        public void GmGrantSeals()
        {
            var all = new List<SealDrop>();
            for (int t = 1; t <= SealCatalog.TypeCount; t++)
                for (int q = 0; q < SealCatalog.QualityCount; q++)
                    all.Add(new SealDrop { Type = (SealType)t, Quality = (SealQuality)q });
            GrantSeals(all);
            Save();
        }
    }
}
