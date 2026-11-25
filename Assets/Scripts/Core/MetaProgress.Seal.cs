using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 插槽和印章。数值在 SealCatalog。
    // 单字槽存 SlotType / SlotSeal（下标 = CardId × 3 + 槽位），成词槽独立存 WordSlotType / WordSlotSeal（下标 = WordId × 3 + 槽位），
    // 彻底解耦 CardCatalog.IdCount 扩容，防止后续新增字牌再次顶歪成词插槽！
    // SlotType 是 (int)SealType，0 是还没刷出；SlotSeal 是镶着的印章品质 + 1，0 是空。
    public sealed partial class MetaProgress
    {
        public const int CurrentSealLayoutVer = 1;

        public int[] SlotType = new int[CardCatalog.IdCount * SealCatalog.SlotsPer];
        public int[] SlotSeal = new int[CardCatalog.IdCount * SealCatalog.SlotsPer];
        public int[] WordSlotType = new int[CardCatalog.WordCount * SealCatalog.SlotsPer];
        public int[] WordSlotSeal = new int[CardCatalog.WordCount * SealCatalog.SlotsPer];
        public int SealLayoutVer;
        // 背包按 类型 × 品质 叠，见 SealCatalog.BagIndex。
        public int[] SealBag = new int[SealCatalog.BagSize];
        // 背包里有几枚是打开背包之后才拿到的，字卡页「印章」挂红点。
        public int SealNew;

        // 插槽要通关第五章。掉落跟着字卡一起开，第五章打的时候先攒着。
        public bool SealOpen => SealCatalog.IsOpen(ClearedCount());
        public bool SealDropOpen => StudyOpen;

        // 字卡页的「印章」按钮：开了插槽，或者已经攒到过印章。
        public bool SealBagShown => SealOpen || SealTotal > 0 || AnySocketed;

        bool SlotRef(CodexEntry e, int k, out int[] types, out int[] seals, out int idx)
        {
            if (e.Kind == CodexKind.Word)
            {
                types = WordSlotType;
                seals = WordSlotSeal;
            }
            else
            {
                types = SlotType;
                seals = SlotSeal;
            }
            idx = e.Index * SealCatalog.SlotsPer + k;
            return types != null && seals != null && k >= 0 && k < SealCatalog.SlotsPer
                && idx >= 0 && idx < types.Length && idx < seals.Length;
        }

        bool NormalizeSeals()
        {
            SealBag = Fit(SealBag, SealCatalog.BagSize);
            for (int i = 0; i < SealBag.Length; i++) SealBag[i] = Mathf.Max(0, SealBag[i]);

            bool migrated = false;
            if (SealLayoutVer < CurrentSealLayoutVer)
            {
                MigrateLegacyWordSeals();
                SealLayoutVer = CurrentSealLayoutVer;
                migrated = true;
            }

            int glyphSlots = CardCatalog.IdCount * SealCatalog.SlotsPer;
            int wordSlots = CardCatalog.WordCount * SealCatalog.SlotsPer;
            SlotType = Fit(SlotType, glyphSlots);
            SlotSeal = Fit(SlotSeal, glyphSlots);
            WordSlotType = Fit(WordSlotType, wordSlots);
            WordSlotSeal = Fit(WordSlotSeal, wordSlots);

            CleanSlotArrays(SlotType, SlotSeal, false, ref migrated);
            CleanSlotArrays(WordSlotType, WordSlotSeal, true, ref migrated);

            SealNew = Mathf.Max(0, SealNew);
            return migrated;
        }

        // 无损迁移旧版混排在 SlotType/SlotSeal 尾部的成词插槽与印章：
        // 兼容两种存档状态：
        //   1) 14 章旧档直接升上来（OldIdCount = 33，词 1..5 存在 entry 34..38）；
        //   2) 已在 16 章首版打开过的档（数组被垫长到 132，原词 1..5 仍留在 entry 34..38，且 39..43 可能新刷/新镶了槽）。
        // 任何无法原位安放或超出槽数的已镶嵌印章一律退回 SealBag，确保零丢失！
        void MigrateLegacyWordSeals()
        {
            const int S = SealCatalog.SlotsPer;
            const int OldIdCount = 33; // Phase B 前 CardCatalog.IdCount = 33（Fire=1..Seal=32）
            const int OldWordMax = 5;  // InstantKill(1)..Crush(5)
            const int PhaseBIdCount = 37;

            int rawLen = Mathf.Min(SlotType != null ? SlotType.Length : 0, SlotSeal != null ? SlotSeal.Length : 0);
            WordSlotType = Fit(WordSlotType, CardCatalog.WordCount * S);
            WordSlotSeal = Fit(WordSlotSeal, CardCatalog.WordCount * S);
            if (rawLen <= OldIdCount * S) return;

            for (int i = 0; i < rawLen; i++)
            {
                if (SlotType[i] < 0 || SlotType[i] > SealCatalog.TypeCount) SlotType[i] = 0;
                if (SlotSeal[i] < 0 || SlotSeal[i] > SealCatalog.QualityCount || SlotType[i] == 0) SlotSeal[i] = 0;
            }

            int ReadT(int entry, int k)
            {
                int idx = entry * S + k;
                return idx >= 0 && idx < rawLen ? SlotType[idx] : 0;
            }
            int ReadQ(int entry, int k)
            {
                int idx = entry * S + k;
                return idx >= 0 && idx < rawLen ? SlotSeal[idx] : 0;
            }
            void Refund(int t, int q1)
            {
                if (t > 0 && t <= SealCatalog.TypeCount && q1 > 0 && q1 <= SealCatalog.QualityCount)
                {
                    SealBag[SealCatalog.BagIndex((SealType)t, (SealQuality)(q1 - 1))]++;
                    SealNew++;
                }
            }

            // 判断是否带有 OldIdCount=33 时期的旧成词槽位数据：
            // 1) 数组长度仍是 117（<= 39*3）；
            // 2) entry 34（扫，半边字不可能开槽）或 entry 37（WordId.None，不可能开槽）非空；
            // 3) entry 35/36（财/护）有槽数据但对应新字等级还不到开槽线（<4级）；
            // 4) entry 38 有槽数据，且秒杀未到 4 级而千钧已到 4 级。
            bool hasOld33 = rawLen <= (OldIdCount + OldWordMax + 1) * S;
            if (!hasOld33)
            {
                for (int k = 0; k < S; k++)
                {
                    if (ReadT(34, k) != 0 || ReadT(37, k) != 0) { hasOld33 = true; break; }
                    if (ReadT(35, k) != 0 && SealCatalog.SlotsAt(GlyphLevel[35]) <= k) { hasOld33 = true; break; }
                    if (ReadT(36, k) != 0 && SealCatalog.SlotsAt(GlyphLevel[36]) <= k) { hasOld33 = true; break; }
                    if (ReadT(38, k) != 0
                        && SealCatalog.SlotsAt(WordLevel[(int)WordId.InstantKill]) <= k
                        && SealCatalog.SlotsAt(WordLevel[(int)WordId.Crush]) > k)
                    {
                        hasOld33 = true;
                        break;
                    }
                }
            }

            if (hasOld33)
            {
                // 1) 先把 OldIdCount=33 下 entry 34..38 里的原成词（秒杀..千钧）迁回 WordSlot[1..5]
                for (int w = 1; w <= OldWordMax && w < CardCatalog.WordCount; w++)
                {
                    int oldEntry = OldIdCount + w;
                    // 若极端情况下玩家今天已把「财(35)」或「护(36)」升到 4 级且原「万箭/击退」根本没开槽，则保留给财/护
                    if ((oldEntry == 35 || oldEntry == 36)
                        && SealCatalog.SlotsAt(WordLevel[w]) == 0
                        && SealCatalog.SlotsAt(GlyphLevel[oldEntry]) > 0)
                        continue;

                    for (int k = 0; k < S; k++)
                    {
                        int t = ReadT(oldEntry, k);
                        int q1 = ReadQ(oldEntry, k);
                        int srcIdx = oldEntry * S + k;
                        if (srcIdx < rawLen) { SlotType[srcIdx] = 0; SlotSeal[srcIdx] = 0; }
                        if (t <= 0) continue;

                        // entry 38 同时是旧「千钧(w=5)」和新版误映射的「秒杀(w=1)」：
                        // 若千钧没开这个槽而秒杀开了槽且秒杀该槽为空，则归还给秒杀。
                        int dstW = w;
                        if (oldEntry == 38 && SealCatalog.SlotsAt(WordLevel[w]) <= k
                            && SealCatalog.SlotsAt(WordLevel[(int)WordId.InstantKill]) > k
                            && WordSlotType[(int)WordId.InstantKill * S + k] == 0)
                        {
                            dstW = (int)WordId.InstantKill;
                        }

                        int dst = dstW * S + k;
                        if (WordSlotType[dst] == 0)
                        {
                            WordSlotType[dst] = t;
                            WordSlotSeal[dst] = q1;
                        }
                        else
                        {
                            Refund(t, q1);
                        }
                    }
                }

                // 2) 若玩家在 16 章首版里还在 entry 39..43（万箭..横扫）新刷了槽或新镶了章，合并或无损退回背包
                for (int w = 2; w < CardCatalog.WordCount; w++)
                {
                    int bEntry = PhaseBIdCount + w;
                    for (int k = 0; k < S; k++)
                    {
                        int t = ReadT(bEntry, k);
                        int q1 = ReadQ(bEntry, k);
                        int srcIdx = bEntry * S + k;
                        if (srcIdx < rawLen) { SlotType[srcIdx] = 0; SlotSeal[srcIdx] = 0; }
                        if (t <= 0) continue;

                        int dst = w * S + k;
                        if (WordSlotType[dst] == 0)
                        {
                            WordSlotType[dst] = t;
                            WordSlotSeal[dst] = q1;
                        }
                        else if (q1 > 0 && WordSlotSeal[dst] == 0 && WordSlotType[dst] == t)
                        {
                            WordSlotSeal[dst] = q1;
                        }
                        else
                        {
                            Refund(t, q1);
                        }
                    }
                }
            }
            else
            {
                // 纯 16 章首版新档：成词在 PhaseBIdCount(37) + w
                for (int w = 1; w < CardCatalog.WordCount; w++)
                {
                    int bEntry = PhaseBIdCount + w;
                    for (int k = 0; k < S; k++)
                    {
                        int t = ReadT(bEntry, k);
                        int q1 = ReadQ(bEntry, k);
                        int srcIdx = bEntry * S + k;
                        if (srcIdx < rawLen) { SlotType[srcIdx] = 0; SlotSeal[srcIdx] = 0; }
                        if (t <= 0) continue;
                        int dst = w * S + k;
                        WordSlotType[dst] = t;
                        WordSlotSeal[dst] = q1;
                    }
                }
            }

            // 3) 兜底清扫：超出 CardCatalog.IdCount 的旧残留槽位若还有印章，全部退回背包
            for (int idx = CardCatalog.IdCount * S; idx < rawLen; idx++)
            {
                if (SlotSeal[idx] > 0) Refund(SlotType[idx], SlotSeal[idx]);
                SlotType[idx] = 0;
                SlotSeal[idx] = 0;
            }
        }

        // 校验每个槽位是否合法（非半边字、等级达标、词条允许）；不合法槽上的印章一律退回背包，绝不吞章。
        void CleanSlotArrays(int[] types, int[] seals, bool isWord, ref bool changed)
        {
            const int S = SealCatalog.SlotsPer;
            int count = types.Length / S;
            for (int id = 0; id < count; id++)
            {
                bool validHost;
                int openSlots;
                int key;
                if (isWord)
                {
                    validHost = id > 0 && id < CardCatalog.WordCount;
                    openSlots = validHost ? SealCatalog.SlotsAt(WordLevel[id]) : 0;
                    key = validHost ? SealCatalog.KeyOf((WordId)id) : 0;
                }
                else
                {
                    validHost = id > 0 && id < CardCatalog.IdCount
                        && CardCatalog.Get((CardId)id).Wake != CardWake.WordPart;
                    openSlots = validHost ? SealCatalog.SlotsAt(GlyphLevel[id]) : 0;
                    key = id;
                }

                for (int k = 0; k < S; k++)
                {
                    int s = id * S + k;
                    int t = types[s];
                    int q1 = seals[s];
                    if (t < 0 || t > SealCatalog.TypeCount) { t = 0; types[s] = 0; changed = true; }
                    if (q1 < 0 || q1 > SealCatalog.QualityCount || t == 0) { q1 = 0; seals[s] = 0; }

                    bool slotValid = validHost && k < openSlots && (t == 0 || SealCatalog.Allowed(key, (SealType)t));
                    if (!slotValid && (t != 0 || q1 != 0))
                    {
                        if (t > 0 && q1 > 0)
                        {
                            SealBag[SealCatalog.BagIndex((SealType)t, (SealQuality)(q1 - 1))]++;
                            SealNew++;
                        }
                        types[s] = 0;
                        seals[s] = 0;
                        changed = true;
                    }
                }
            }
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
                    if (!SlotRef(e, k, out int[] types, out _, out int s)) continue;
                    if (types[s] != 0) continue;
                    types[s] = (int)SealCatalog.RollSlot(key);
                    changed = true;
                }
            }
            return changed;
        }

        public void EnsureSlots()
        {
            if (FillSlots()) Save();
        }

        // ---- 读 ----

        public int SlotsOpen(CodexEntry e) => SealOpen ? SealCatalog.SlotsAt(StudyLevel(e)) : 0;

        public SealType SlotTypeOf(CodexEntry e, int k) =>
            SlotRef(e, k, out int[] types, out _, out int s) ? (SealType)types[s] : SealType.None;

        // 镶着的印章品质；空槽返回 false。
        public bool SlotSealOf(CodexEntry e, int k, out SealQuality q)
        {
            q = SealQuality.Green;
            if (!SlotRef(e, k, out _, out int[] seals, out int s) || seals[s] <= 0) return false;
            q = (SealQuality)(seals[s] - 1);
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
                if (WordSlotSeal != null)
                    for (int i = 0; i < WordSlotSeal.Length; i++) if (WordSlotSeal[i] > 0) return true;
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

        // 三选一的字运倍率。词的字运作用在两个半边字上；同一个字两边都有时取大的；再叠乘字帖「偏爱」倍率。
        public float OfferLuck(CardId id)
        {
            float copy = CopybookLuck(id);
            if (!SealOpen) return copy;
            float best = LuckAt(new CodexEntry(CodexKind.Glyph, (int)id));
            CardDef def = CardCatalog.Get(id);
            if (def.Wake == CardWake.WordPart && def.Word != WordId.None)
                best = Mathf.Max(best, LuckAt(new CodexEntry(CodexKind.Word, (int)def.Word)));
            return best * copy;
        }

        // 这个字上镶着的字运倍率叠乘。空着的字运槽不算。
        float LuckAt(CodexEntry e)
        {
            int open = SlotsOpen(e);
            float m = 1f;
            for (int k = 0; k < open; k++)
                if (SlotTypeOf(e, k) == SealType.Luck && SlotSealOf(e, k, out SealQuality q))
                    m *= SealCatalog.LuckOf(q);
            return m;
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

        public enum SealBlock { None, Locked, NoSlot, Empty, Diamond }

        public bool Socket(CodexEntry e, int k, SealQuality q)
        {
            if (!SealOpen || k < 0 || k >= SlotsOpen(e)) return false;
            if (!SlotRef(e, k, out int[] types, out int[] seals, out int s)) return false;
            SealType t = (SealType)types[s];
            if (t == SealType.None || BagCount(t, q) <= 0) return false;
            if (seals[s] > 0) SealBag[SealCatalog.BagIndex(t, (SealQuality)(seals[s] - 1))]++;
            SealBag[SealCatalog.BagIndex(t, q)]--;
            seals[s] = (int)q + 1;
            Save();
            return true;
        }

        public bool Unsocket(CodexEntry e, int k)
        {
            if (!SealOpen || k < 0 || k >= SealCatalog.SlotsPer) return false;
            if (!SlotRef(e, k, out int[] types, out int[] seals, out int s)) return false;
            if (seals[s] <= 0) return false;
            ReturnSlot(types, seals, s);
            Save();
            return true;
        }

        void ReturnSlot(int[] types, int[] seals, int s)
        {
            if (seals[s] <= 0 || types[s] <= 0) return;
            SealBag[SealCatalog.BagIndex((SealType)types[s], (SealQuality)(seals[s] - 1))]++;
            seals[s] = 0;
        }

        public SealBlock RerollBlock(CodexEntry e)
        {
            if (!SealOpen) return SealBlock.Locked;
            if (SlotsOpen(e) <= 0) return SealBlock.NoSlot;
            return Diamond >= SealCatalog.RerollDiamond ? SealBlock.None : SealBlock.Diamond;
        }

        // 一次换掉这个字上所有已开的槽，只换类型。先把镶着的印章卸回背包，再重抽。可能刷出同一种，字运也可以三个都是。
        public int Reroll(CodexEntry e)
        {
            if (RollStudy()) Save();
            if (RerollBlock(e) != SealBlock.None) return 0;
            if (!SpendDiamond(SealCatalog.RerollDiamond)) return 0;
            int key = SealCatalog.KeyOf(e);
            int open = SlotsOpen(e);
            for (int k = 0; k < open; k++)
            {
                if (!SlotRef(e, k, out int[] types, out int[] seals, out int s)) continue;
                ReturnSlot(types, seals, s);
                types[s] = 0;
            }
            for (int k = 0; k < open; k++)
            {
                if (!SlotRef(e, k, out int[] types, out _, out int s)) continue;
                types[s] = (int)SealCatalog.RollSlot(key);
            }
            Save();
            return open;
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

        // 灵墨拓印：花费灵墨抽取一枚金箱品阶池的随机印章。
        public bool TryMintSeal(out SealDrop drop)
        {
            drop = default;
            if (Ink < SealCatalog.MintInkCost) return false;
            Ink -= SealCatalog.MintInkCost;
            drop = SealCatalog.Roll(SealCatalog.Source.Gold);
            GrantSeals(new List<SealDrop>(1) { drop });
            Save();
            return true;
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
