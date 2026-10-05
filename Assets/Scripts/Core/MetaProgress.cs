using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public enum ChestState { Empty, Locked, Timing, Ready }

    [Serializable]
    public sealed class MetaProgress
    {
        const string KeyV2 = "inkline.meta.v2";
        const string KeyV1 = "inkline.meta.v1";

        public int Ink;
        public int Stamina = GameConstants.StaminaMax;
        public long StaminaTick;
        public int AdStaminaToday;
        // 1：体力上限从 12 提到 20。当时是满的，补到新上限。
        public int StaminaRulesVer;
        public int LastDay;
        public bool DailyWinDone;
        public bool StarterGranted;
        public int[] Stars = new int[GameConstants.StageCount];
        public int[] Forge = new int[ForgeCatalog.LineCount];
        public int Skin;
        public bool[] SkinOwned = new bool[SkinCatalog.Count];
        // 炮台碎片。下标跟皮肤走，集满 SkinDef.Shards 就到手。
        public int[] SkinShards = new int[SkinCatalog.Count];
        // 章节满星宝箱领过没有。位下标是章。
        public int ChapterChest;
        // 道具：等级 0 是还没解锁；卡是攒着的、还没花掉的张数。
        public int[] ItemLevel = new int[ItemCatalog.Count];
        public int[] ItemCards = new int[ItemCatalog.Count];
        public int[] Equipped = { -1, -1, -1 };
        public bool ItemStarterDone;
        // 1：取消开局白送道具。老档里那只还停在 1 级、旁边没有别的道具的鞭炮收回。
        public int ItemRulesVer;
        // 胜利宝箱位：0 是空，其余是 ChestTier + 1。ChestDone 是解锁完成的 UTC 毫秒，0 表示还没开始解锁。
        public int[] ChestSlot = new int[ChestCatalog.Slots];
        public long[] ChestDone = new long[ChestCatalog.Slots];
        // 刚打开或换成墨之后，后面的箱子要滑到前面。下标是新格子，值是这只原来的格子。不进存档。
        [System.NonSerialized] public int[] ChestSlideFrom;
        // 开箱演出还挡着的时候先别滑，等演出关掉、格子重新摆出来再滑。
        [System.NonSerialized] public bool DeferChestSlide;
        public int ChestCycle;
        public int PendingChest;   // 结算页上还没处理的宝箱，品阶 + 1；0 表示没有
        // 宝箱位满了、玩家还没决定的箱子。先寄在这里，不悄悄折成墨。品阶 + 1，0 是空。
        // 第二只留给「结算和签到撞在一起」这种很少见的情况。
        public int OverflowChest;
        public int OverflowExtra;
        // 钻石：只买时间和便利 —— 宝箱立即开、补体力。
        public int Diamond;
        public int DiamondStamCount;
        public int GiftAds;
        public bool GiftClaimed;
        public int ClubDay;
        public int CheckDay;
        public int CheckRun;
        public int CheckAdDay;
        // 签到页自动弹出记在哪一天。跟签没签无关，当天弹过就不再弹。
        public int CheckPopDay;
        public bool CheckSkinDone;
        public int CheckLoops;
        // 图鉴：位掩码。字按 CardId、词按 WordId、秘卷按 SignaturePairs 下标。
        // New 系列是「收录了还没点开看过」，给红点用。
        public long CodexGlyph;
        public int CodexWord;
        public int CodexPair;
        public long CodexNewGlyph;
        public int CodexNewWord;
        public int CodexNewPair;
        public int CodexEnemy;
        public int CodexNewEnemy;
        // 三栏各三枚印，第 tab*3+i 位表示第 i 枚已经领过。
        public int CodexMileClaim;
        // 词条：Seen 是「已经亮过相」，过关时没亮过的才弹解锁卡；
        // New 是「亮了还没在炮台页看过」，给那一行挂「新」。位下标是词条下标。
        public long ForgeSeen;
        public long ForgeNew;
        public int ForgeSeenVer;
        // 皮肤开放（到了门槛、可以买）有没有弹过「新炮台开放」卡。位下标是皮肤下标。
        public int SkinSeen;
        // 开放了、炮台页还没去看过的皮肤，底栏炮台页签挂红点。
        public int SkinNew;
        // 活动货币，留给皮肤专属词条。ForgeCoin.Token 从这里扣。
        public int Token;
        // 招财进宝：期号、聚宝盆累计、里程碑领取位、当天打了几局、当天广告加次数和翻倍用过没。
        // 期号和 EventCatalog.Id 对不上就整份清零。
        public int EventId;
        public int EventBank;
        public int EventClaims;
        public int EventDay;
        public int EventPlays;
        public bool EventAdDone;
        public bool EventDoubleDone;
        // 打赢过的地点。位下标是档位，赢了上一处才开下一处。
        public int EventClears;
        // 新手指引走到哪一步。跟着整份存档上云，清缓存、换机都不重走。
        public int GuideStep;
        public const int GuideBattle = 0;
        public const int GuideForge = 1;
        public const int GuideChest = 2;
        public const int GuideGift = 3;
        public const int GuideSortie = 4;
        // 领完新手奖励后教弹弓解锁、装备。比出征大一号，是后加的一步，教完再回到 GuideSortie。
        public const int GuideItem = 5;
        public const int GuideDone = 9;
        // 第一次攒够卡能解锁道具时教解锁、装备。教过一次就不再教。
        public bool ItemGuideDone;

        public bool Guiding => GuideStep < GuideDone;

        public void SetGuide(int step)
        {
            if (GuideStep == step) return;
            GuideStep = step;
            Save();
            CloudSync.FlushNow("guide");
        }

        // 第一个攒够卡、还没解锁的道具；没有返回 -1。
        public int FirstUnlockable
        {
            get
            {
                for (int i = 0; i < ItemCatalog.Count; i++)
                    if (ItemRank(i) <= 0 && CanUpgradeItem(i, out _)) return i;
                return -1;
            }
        }

        public int FirstChestSlot
        {
            get
            {
                for (int i = 0; i < ChestSlot.Length; i++)
                    if (ChestStateOf(i) != ChestState.Empty) return i;
                return -1;
            }
        }

        public void SetItemGuideDone()
        {
            if (ItemGuideDone) return;
            ItemGuideDone = true;
            Save();
        }

        public void GrantGuideGift()
        {
            if (GuideStep != GuideGift) return;
            Diamond += GameConstants.GuideDiamond;
            Ink += GameConstants.GuideInk;
            Stamina += GameConstants.GuideStamina;
            int snipe = (int)ItemId.Snipe;
            if (snipe >= 0 && snipe < ItemCards.Length) ItemCards[snipe] += GameConstants.GuideSlingshot;
            GuideStep = GuideItem;
            Save();
            CloudSync.FlushNow("guide");
        }

        public static MetaProgress Load()
        {
            var m = new MetaProgress();
            string raw = PlayerPrefs.GetString(KeyV2, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try { JsonUtility.FromJsonOverwrite(raw, m); }
                catch (Exception) { m = new MetaProgress(); }
            }
            else m.MigrateV1();
            m.Normalize();
            m.GrantStarter();
            m.Refresh();
            return m;
        }

        // v1 是管道字符串，只有章通关和满星两个布尔。折算成对应的升级等级，
        // 再按已有星数补一笔墨，免得老存档进来面对空的升级树。
        void MigrateV1()
        {
            string raw = PlayerPrefs.GetString(KeyV1, "");
            if (string.IsNullOrEmpty(raw)) return;
            string[] parts = raw.Split('|');
            if (parts.Length < 2) return;
            bool chapterCleared = parts[0] == "1";
            bool goldBonus = parts[1] == "1";
            if (parts.Length > 2)
            {
                string[] stars = parts[2].Split(',');
                for (int i = 0; i < stars.Length && i < Stars.Length; i++)
                    int.TryParse(stars[i], out Stars[i]);
            }
            if (chapterCleared) Forge[(int)ForgeLine.Emitters] = 1;
            if (goldBonus) Forge[(int)ForgeLine.StartGold] = 1;
            Ink += ClearedCount() * 12;
        }

        void Normalize()
        {
            Stars = Fit(Stars, GameConstants.StageCount);
            Forge = Fit(Forge, ForgeCatalog.LineCount);
            SkinOwned = Fit(SkinOwned, SkinCatalog.Count);
            SkinShards = Fit(SkinShards, SkinCatalog.Count);
            ItemLevel = Fit(ItemLevel, ItemCatalog.Count);
            ItemCards = Fit(ItemCards, ItemCatalog.Count);
            if (Equipped == null || Equipped.Length != GameConstants.ItemSlots)
            {
                var eq = new int[GameConstants.ItemSlots];
                for (int s = 0; s < eq.Length; s++) eq[s] = Equipped != null && s < Equipped.Length ? Equipped[s] : -1;
                Equipped = eq;
            }
            ChestSlot = Fit(ChestSlot, ChestCatalog.Slots);
            ChestDone = Fit(ChestDone, ChestCatalog.Slots);
            for (int i = 0; i < Forge.Length; i++)
                Forge[i] = Mathf.Clamp(Forge[i], 0, ForgeCatalog.MaxLevel(i));
            for (int i = 0; i < Stars.Length; i++)
                Stars[i] = Mathf.Clamp(Stars[i], 0, GameConstants.MaxStar);
            for (int i = 0; i < SkinShards.Length; i++)
                SkinShards[i] = Mathf.Max(0, SkinShards[i]);
            ChapterChest &= (1 << GameConstants.Chapters) - 1;
            SkinOwned[0] = true;
            bool rulesChanged = false;
            if (!ItemStarterDone)
            {
                ItemStarterDone = true;
                rulesChanged = true;
            }
            if (ItemRulesVer < 1)
            {
                ItemRulesVer = 1;
                rulesChanged = true;
                RevokeGiftItem();
            }
            if (StaminaRulesVer < 1)
            {
                StaminaRulesVer = 1;
                rulesChanged = true;
                if (Stamina >= 12) Stamina = GameConstants.StaminaMax;
            }
            for (int i = 0; i < ItemLevel.Length; i++)
            {
                ItemLevel[i] = Mathf.Clamp(ItemLevel[i], 0, ItemCatalog.MaxLevel);
                ItemCards[i] = Mathf.Max(0, ItemCards[i]);
            }
            for (int i = 0; i < ChestSlot.Length; i++)
            {
                if (ChestSlot[i] < 0 || ChestSlot[i] > 4) ChestSlot[i] = 0;
                if (ChestSlot[i] == 0) ChestDone[i] = 0L;
            }
            if (PendingChest < 0 || PendingChest > 4) PendingChest = 0;
            if (OverflowChest < 0 || OverflowChest > 4) OverflowChest = 0;
            if (OverflowExtra < 0 || OverflowExtra > 4) OverflowExtra = 0;
            if (OverflowChest <= 0) PromoteOverflow();
            // 结算页上被杀进程，宝箱照常放进宝箱位。位满了就寄着，等玩家回来选，不折成墨。
            // 老档中间有空格的，先按原来的先后靠拢，新箱子才进得了最后一格。
            bool chestMoved = PackChests(false);
            chestMoved = ResolvePending() || chestMoved;
            while (OverflowChest > 0 && AddChest(OverflowTier) >= 0)
            {
                OverflowChest = 0;
                PromoteOverflow();
                chestMoved = true;
            }
            if (chestMoved) rulesChanged = true;
            Skin = SkinOwned[Mathf.Clamp(Skin, 0, SkinCatalog.Count - 1)] ? Mathf.Clamp(Skin, 0, SkinCatalog.Count - 1) : 0;
            for (int s = 0; s < Equipped.Length; s++)
            {
                int id = Equipped[s];
                if (id < 0 || id >= ItemCatalog.Count || ItemLevel[id] <= 0 || !ItemSlotOpen(s)) Equipped[s] = -1;
                for (int t = 0; t < s; t++)
                    if (Equipped[t] == Equipped[s]) Equipped[s] = -1;
            }
            Ink = Mathf.Max(0, Ink);
            Diamond = Mathf.Max(0, Diamond);
            Token = Mathf.Max(0, Token);
            // 奖励来的体力可以超过上限，这里只防负数，否则重进游戏多出来的就被削掉。
            Stamina = Mathf.Max(0, Stamina);
            if (GuideStep == GuideBattle && ClearedCount() > 0)
            {
                GuideStep = Forge[(int)ForgeLine.Damage] > 0 || ClearedCount() > 1 ? GuideDone : GuideForge;
                rulesChanged = true;
            }
            if (!ItemGuideDone)
            {
                for (int i = 0; i < ItemLevel.Length; i++)
                    if (ItemLevel[i] > 0)
                    {
                        ItemGuideDone = true;
                        rulesChanged = true;
                        break;
                    }
            }
            if (rulesChanged) Save();
            if (ForgeSeenVer < 1)
            {
                // 加解锁卡之前就有的五条，老存档早就见过，不再补弹。
                for (int i = 0; i < ForgeCatalog.LineCount && i <= (int)ForgeLine.BaseHp; i++)
                    if (ForgeShown(i)) ForgeSeen |= 1L << i;
                ForgeSeenVer = 1;
            }
            if (ForgeSeenVer < 2)
            {
                // 暴击、金币收益改成了赤焰、福袋的专属词条，词条本身不再弹卡，跟着皮肤一起亮。
                // 原有四款皮肤早就摆在炮台页上，只给新加的两款补「新炮台开放」。
                for (int i = 0; i < ForgeCatalog.LineCount; i++)
                    if (ForgeCatalog.Get(i).Exclusive) ForgeSeen |= 1L << i;
                for (int i = 0; i < SkinCatalog.Count && i < SkinCatalog.Flame; i++) SkinSeen |= 1 << i;
                ForgeSeenVer = 2;
            }
        }

        // 花墨买的皮肤到了章节门槛，结算页弹「新炮台开放」。买没买另说。
        public bool SkinOpened(int i)
        {
            SkinDef d = SkinCatalog.Get(i);
            return d.Way == SkinWay.Ink && (d.Chapter < 0 || ChapterCleared(d.Chapter));
        }

        // 炮台页上现在就能拿到：看广告，或者章节到了可以花钱。签到和活动不在这一页领。
        public bool SkinReady(int i)
        {
            SkinDef d = SkinCatalog.Get(i);
            return d.Way == SkinWay.Ad || SkinOpened(i);
        }

        int[] TakeSkinReveals()
        {
            var got = new List<int>();
            for (int i = 0; i < SkinCatalog.Count; i++)
            {
                int bit = 1 << i;
                if ((SkinSeen & bit) != 0 || SkinOwned[i] || !SkinOpened(i)) continue;
                SkinSeen |= bit;
                SkinNew |= bit;
                got.Add(i);
            }
            return got.ToArray();
        }

        static int[] Fit(int[] src, int len)
        {
            if (src != null && src.Length == len) return src;
            var dst = new int[len];
            if (src != null)
                for (int i = 0; i < src.Length && i < len; i++) dst[i] = src[i];
            return dst;
        }

        static long[] Fit(long[] src, int len)
        {
            if (src != null && src.Length == len) return src;
            var dst = new long[len];
            if (src != null)
                for (int i = 0; i < src.Length && i < len; i++) dst[i] = src[i];
            return dst;
        }

        static bool[] Fit(bool[] src, int len)
        {
            if (src != null && src.Length == len) return src;
            var dst = new bool[len];
            if (src != null)
                for (int i = 0; i < src.Length && i < len; i++) dst[i] = src[i];
            return dst;
        }

        // 开局不发道具。白送的那一只如果还是 1 级、也没解锁过别的，收回去。
        // 卡片留着：宝箱掉的卡照旧攒着，够数了自己解锁。
        void RevokeGiftItem()
        {
            int first = (int)ItemId.Burst;
            if (first >= ItemLevel.Length || ItemLevel[first] != 1) return;
            for (int i = 0; i < ItemLevel.Length; i++)
                if (i != first && ItemLevel[i] > 0) return;
            ItemLevel[first] = 0;
        }

        void GrantStarter()
        {
            if (StarterGranted) return;
            StarterGranted = true;
            Ink += GameConstants.StarterInk;
            Stamina = GameConstants.StaminaMax;
            StaminaTick = DateTime.UtcNow.Ticks;
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetString(KeyV2, JsonUtility.ToJson(this));
            CloudSync.Touch();
            PlayerPrefs.Save();
        }

        // 清档后紧接着 Load 会写出新档并标脏，调用方再 CloudSync.FlushNow，云端一起清。
        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(KeyV2);
            PlayerPrefs.DeleteKey(KeyV1);
            PlayerPrefs.Save();
        }

        public static bool HasLocal => PlayerPrefs.HasKey(KeyV2) || PlayerPrefs.HasKey(KeyV1);

        public static string RawLocal => PlayerPrefs.GetString(KeyV2, "");

        // 云端下行用，不标脏。
        public static void WriteRaw(string json)
        {
            PlayerPrefs.SetString(KeyV2, json);
            PlayerPrefs.DeleteKey(KeyV1);
        }

        public void FillStamina()
        {
            Stamina = GameConstants.StaminaMax;
            StaminaTick = DateTime.UtcNow.Ticks;
            Save();
        }

        // GM：每个道具都塞 n 张卡。
        public void GrantCards(int n)
        {
            if (n <= 0) return;
            for (int i = 0; i < ItemCards.Length && i < ItemCatalog.Count; i++)
                if (ItemLevel[i] < ItemCatalog.MaxLevel) ItemCards[i] += n;
            Save();
        }

        public void UnlockStages()
        {
            for (int i = 0; i < Stars.Length; i++)
                Stars[i] = GameConstants.MaxStar;
            Save();
        }

        public void UnlockSkins()
        {
            for (int i = 0; i < SkinOwned.Length; i++)
                SkinOwned[i] = true;
            Save();
        }

        public void UnlockItems()
        {
            for (int i = 0; i < ItemLevel.Length && i < ItemCatalog.Count; i++)
                if (ItemLevel[i] <= 0) ItemLevel[i] = 1;
            Save();
        }

        public void MaxForge()
        {
            for (int i = 0; i < Forge.Length && i < ForgeCatalog.LineCount; i++)
                Forge[i] = ForgeCatalog.MaxLevel(i);
            Save();
        }

        // 大厅每帧调。体力恢复和日重置都是纯读时间的，放一起省心。
        public void Refresh()
        {
            bool dirty = RollDay();
            if (RollEvent()) dirty = true;
            if (RegenStamina()) dirty = true;
            if (TickChests()) dirty = true;
            if (dirty) Save();
        }

        static int Today => DayKey(DateTime.Now);

        static int DayKey(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;

        static int ShiftDay(int key, int days)
        {
            if (key <= 0) return key;
            var d = new DateTime(key / 10000, key / 100 % 100, key % 100);
            return DayKey(d.AddDays(days));
        }

        bool RollDay()
        {
            int today = Today;
            if (LastDay == today) return false;
            LastDay = today;
            AdStaminaToday = 0;
            DiamondStamCount = 0;
            DailyWinDone = false;
            return true;
        }

        bool RollEvent()
        {
            bool dirty = false;
            if (EventId != EventCatalog.Id)
            {
                EventId = EventCatalog.Id;
                EventBank = 0;
                EventClaims = 0;
                EventClears = 0;
                EventDay = 0;
                dirty = true;
            }
            int today = Today;
            if (EventDay != today)
            {
                EventDay = today;
                EventPlays = 0;
                EventAdDone = false;
                EventDoubleDone = false;
                dirty = true;
            }
            return dirty;
        }

        // 招财进宝：常驻，通完第一章才露入口。
        public bool EventOpen => ChapterCleared(0);

        // 里程碑全领完了：还能打，但不再存钱，也没有翻倍。
        public bool EventDone
        {
            get
            {
                for (int i = 0; i < EventCatalog.Milestones.Length; i++) if (!EventClaimed(i)) return false;
                return true;
            }
        }

        public bool EventTierWon(int t) => t >= 0 && (EventClears & (1 << t)) != 0;

        // 小集一直开着。后面每一处要打赢紧挨着的上一处。
        public bool EventTierOpen(int t) => t <= 0 || EventTierWon(t - 1);

        // 这处第一次打赢时记下，用来开下一处。已经赢过返回 false。
        public bool MarkEventTier(int t)
        {
            if (t < 0 || t >= EventCatalog.TierCount) return false;
            int bit = 1 << t;
            if ((EventClears & bit) != 0) return false;
            EventClears |= bit;
            Save();
            return true;
        }

        public int EventPlaysLeft
        {
            get
            {
                RollEvent();
                int cap = EventCatalog.PlaysPerDay + (EventAdDone ? EventCatalog.AdPlays : 0);
                return Mathf.Max(0, cap - EventPlays);
            }
        }

        public bool SpendEventPlay()
        {
            if (EventPlaysLeft <= 0) return false;
            EventPlays++;
            Save();
            return true;
        }

        public bool GrantEventAdPlays()
        {
            RollEvent();
            if (EventAdDone) return false;
            EventAdDone = true;
            Save();
            return true;
        }

        public void BankEvent(int gold)
        {
            if (gold <= 0 || EventDone) return;
            RollEvent();
            EventBank += gold;
            Save();
        }

        public bool CanDoubleEvent
        {
            get { RollEvent(); return !EventDoubleDone && !EventDone; }
        }

        public bool DoubleEvent(int gold)
        {
            if (gold <= 0 || !CanDoubleEvent) return false;
            EventDoubleDone = true;
            EventBank += gold;
            Save();
            return true;
        }

        public bool EventReached(int i) => EventBank >= EventCatalog.Milestones[i].Need;
        public bool EventClaimed(int i) => (EventClaims & (1 << i)) != 0;
        public bool EventClaimable(int i) => EventReached(i) && !EventClaimed(i);

        public bool EventHasClaim()
        {
            for (int i = 0; i < EventCatalog.Milestones.Length; i++) if (EventClaimable(i)) return true;
            return false;
        }

            // 已有福袋时皮肤那档折成活动币，refunded 告诉界面换说法。
        // chest 是宝箱进了哪个位：-2 这场不是宝箱，-1 位满了先寄着，-3 寄不下才折成墨。
        public bool ClaimEvent(int i, out bool refunded, out int chest)
        {
            refunded = false;
            chest = -2;
            if (i < 0 || i >= EventCatalog.Milestones.Length || !EventClaimable(i)) return false;
            EventMilestone m = EventCatalog.Milestones[i];
            EventClaims |= 1 << i;
            switch (m.Prize)
            {
                case EventPrize.Ink: Ink += m.Amount; break;
                case EventPrize.Token: Token += m.Amount; break;
                case EventPrize.Chest: chest = GrantChest((ChestTier)m.Amount); break;
                default:
                    if (!GrantSkin(m.Amount))
                    {
                        Token += EventCatalog.SkinTokenRefund;
                        refunded = true;
                    }
                    break;
            }
            Save();
            return true;
        }

        public void GmEventBank(int gold)
        {
            RollEvent();
            EventBank += gold;
            Save();
        }

        // 进度、领取、打下的地点、当天次数全清，期号不动。
        public void GmEventReset()
        {
            EventBank = 0;
            EventClaims = 0;
            EventClears = 0;
            EventDay = 0;
            RollEvent();
            Save();
        }

        // 活动关的字池：打到哪关，就用那关的字池。
        public CardId[] EventPool()
        {
            int top = 0;
            for (int i = 0; i < Stars.Length && i < GameConstants.StageCount; i++) if (Stars[i] > 0) top = i;
            return StageCatalog.Get(top).Pool;
        }

        static long RegenTicks => TimeSpan.TicksPerMinute * GameConstants.StaminaRegenMinutes;

        bool RegenStamina()
        {
            long now = DateTime.UtcNow.Ticks;
            if (StaminaTick <= 0L || Stamina >= GameConstants.StaminaMax)
            {
                // 满体力时只把锚点贴到当下，不报脏 —— 否则大厅每帧写一次 PlayerPrefs。
                // 锚点不落盘也没事：加载时若仍是满的会再贴一次，离开满格由 SpendStamina 落盘。
                StaminaTick = now;
                return false;
            }
            // 把系统时间往回调不给恢复，只把锚点拉到当下。往前调挡不住，
            // 要彻底堵得靠云存档时间戳，见 docs/局外成长.md。
            if (now < StaminaTick)
            {
                StaminaTick = now;
                return true;
            }
            long gain = (now - StaminaTick) / RegenTicks;
            if (gain <= 0L) return false;
            int add = (int)Math.Min(gain, GameConstants.StaminaMax);
            Stamina = Mathf.Min(GameConstants.StaminaMax, Stamina + add);
            StaminaTick = Stamina >= GameConstants.StaminaMax ? now : StaminaTick + add * RegenTicks;
            return true;
        }

        public int SecondsToNextStamina()
        {
            if (Stamina >= GameConstants.StaminaMax) return 0;
            long due = StaminaTick + RegenTicks - DateTime.UtcNow.Ticks;
            return due <= 0L ? 0 : (int)(due / TimeSpan.TicksPerSecond);
        }

        // 进关就收，打过没打过一样。失败、中途回首页都不退。
        public int StageCost(int stage)
        {
            if (stage < 0 || stage >= Stars.Length) return 0;
            return StageCatalog.Get(stage).StaminaCost;
        }

        public bool CanEnter(int stage) => Stamina >= StageCost(stage);

        public void SpendStamina(int n)
        {
            if (n <= 0) return;
            if (Stamina >= GameConstants.StaminaMax) StaminaTick = DateTime.UtcNow.Ticks;
            Stamina = Mathf.Max(0, Stamina - n);
            Save();
        }

        // 上限只管自动回复停不停。奖励、购买来的体力一律照加，可以超过上限。
        public bool CanAdStamina => AdStaminaToday < GameConstants.AdStaminaPerDay;

        public void GrantAdStamina()
        {
            AdStaminaToday++;
            Stamina += GameConstants.AdStaminaGain;
            Save();
        }

        public bool Unlocked(int stage) => stage <= 0 || Stars[stage - 1] > 0;

        // 已通关几关。锻造、皮肤的门槛都按这个数。
        public int ClearedCount()
        {
            int n = 0;
            for (int i = 0; i < Stars.Length; i++) if (Stars[i] > 0) n++;
            return n;
        }

        // chapter 从 0 数。
        public bool ChapterCleared(int chapter)
        {
            int last = (chapter + 1) * GameConstants.ChapterSize - 1;
            return last >= 0 && last < Stars.Length && Stars[last] > 0;
        }

        // 技能上的「通关解锁」指通关第三章。皮肤各自写在 SkinDef 上。
        public const int ClearGateChapter = 2;

        public ForgeStats Forged
        {
            get
            {
                return ForgeCatalog.Stats(Forge, Skin);
            }
        }
        // 技能和成词伤害的标尺：一发不带字的炮弹打多少。加成页要按玩家当前的
        // 锻造和皮肤把倍数换算成点数显示，所以这里也要有一份。
        public float ShotBase =>
            (ShotMods.DefaultBase + SkinCatalog.Get(Skin).DamageAdd) * Forged.DamageMul;

        public int StartEmitters => Forged.Emitters;
        public int StartGold => Forged.StartGold;
        public Color SkinTint => SkinCatalog.Get(Skin).Tint;

        // 结算的墨就是这一局亲手拾到的墨，关卡不再另发一笔。星级只留历史最高；
        // 过没过仍按 Stars > 0 判，旧存档里只有 1 的照样算通关。
        public int SkinShardCount(int i) =>
            SkinShards != null && i >= 0 && i < SkinShards.Length ? SkinShards[i] : 0;

        public bool ChapterChestClaimed(int chapter) =>
            chapter >= 0 && (ChapterChest & (1 << chapter)) != 0;

        public int ChapterStarGaps(int chapter)
        {
            int n = GameConstants.ChapterSize;
            int start = chapter * n;
            int end = Mathf.Min(start + n, Stars == null ? 0 : Stars.Length);
            int gaps = 0;
            for (int i = start; i < end; i++)
                if (Stars[i] < GameConstants.MaxStar) gaps++;
            return start >= end ? GameConstants.ChapterSize : gaps;
        }

        public bool ChapterFullStars(int chapter) =>
            chapter >= 0 && chapter < GameConstants.Chapters && ChapterStarGaps(chapter) == 0;

        // 章节满星送一只炮台宝箱，当场开。已经有这门炮时，碎片换成墨。集满就直接到手。
        public ChestLoot ClaimStarChest(int chapter)
        {
            if (!ChapterFullStars(chapter) || ChapterChestClaimed(chapter)) return null;
            ChapterChest |= 1 << chapter;
            return OpenCannonChest();
        }

        // 开一只炮台宝箱。不记章节，活动以后也可以调。
        public ChestLoot OpenCannonChest()
        {
            ChestCatalog.CannonRoll drop = ChestCatalog.RollCannon();
            SkinDef skin = SkinCatalog.Get(drop.Skin);
            SkinShards = Fit(SkinShards, SkinCatalog.Count);
            var loot = new ChestLoot { Title = ChestCatalog.CannonName, ArtKey = ChestCatalog.CannonArt };
            if (drop.Skin >= 0 && drop.Skin < SkinOwned.Length && SkinOwned[drop.Skin])
            {
                int ink = drop.Count * SkinCatalog.ShardInk(skin.Rarity);
                Ink += ink;
                loot.Ink = ink;
                loot.Note = "已有" + skin.Name + "，碎片换成了墨";
                Save();
                return loot;
            }
            SkinShards[drop.Skin] += drop.Count;
            int need = Mathf.Max(1, skin.Shards);
            bool unlocked = false;
            if (skin.Way == SkinWay.Shard && SkinShards[drop.Skin] >= need)
                unlocked = GrantSkin(drop.Skin);
            else
            {
                SkinNew |= 1 << drop.Skin;
                Save();
            }
            loot.Shards.Add(new ShardStack
            {
                Skin = drop.Skin,
                Count = drop.Count,
                Have = SkinShards[drop.Skin],
                Need = need,
                Unlocked = unlocked
            });
            return loot;
        }

        public bool TryCraftSkin(int i)
        {
            if (i < 0 || i >= SkinCatalog.Count) return false;
            SkinDef d = SkinCatalog.Get(i);
            if (d.Way != SkinWay.Shard || SkinOwned[i]) return false;
            if (SkinShardCount(i) < d.Shards) return false;
            return GrantSkin(i);
        }

        public ResultInfo ApplyResult(int stage, int collected, int stars, int hpLeft, int hpMax, bool revived)
        {
            var r = new ResultInfo { Chest = -1 };
            if (stage < 0 || stage >= Stars.Length) return r;
            StageDef def = StageCatalog.Get(stage);
            stars = Mathf.Clamp(stars, 1, GameConstants.MaxStar);
            r.Stars = stars;
            r.HpLeft = Mathf.Clamp(hpLeft, 0, Mathf.Max(1, hpMax));
            r.HpMax = Mathf.Max(1, hpMax);
            r.Revived = revived;
            r.FirstClear = Stars[stage] <= 0;
            r.NewBest = stars > Stars[stage];
            if (r.FirstClear)
            {
                r.Diamonds = def.Finale ? GameConstants.DiamondFinale
                    : def.HasBoss ? GameConstants.DiamondBoss : GameConstants.DiamondStage;
                Diamond += r.Diamonds;
            }
            ChestTier tier = ChestCatalog.ForStage(def, r.FirstClear, ChestCycle);
            if (!def.Finale && !def.HasBoss) ChestCycle++;
            // 宝箱先挂在结算页上：看广告当场开就不占位，离开结算页才放进宝箱位。
            ResolvePending();
            PendingChest = (int)tier + 1;
            r.Chest = (int)tier;
            r.ChestFull = ChestsFull;
            r.ChestInk = r.ChestFull ? ChestCatalog.InkAvg(tier) : 0;
            if (r.NewBest) Stars[stage] = stars;
            int chapter = stage / GameConstants.ChapterSize;
            r.StarChest = chapter;
            r.HasStarChest = ChapterFullStars(chapter) && !ChapterChestClaimed(chapter);
            r.NewLines = TakeForgeReveals();
            r.NewSkins = TakeSkinReveals();
            int ink = Mathf.Max(0, collected);
            if (!DailyWinDone)
            {
                DailyWinDone = true;
                ink *= 2;
                r.DailyDouble = true;
            }
            Ink += ink;
            r.Ink = ink;
            // 新手打完第一关要能当场升一级伤害。一局掉得太少时悄悄补齐，结算页的数不变。
            if (stage == 0 && r.FirstClear && GuideStep == GuideBattle)
            {
                Ink = Mathf.Max(Ink, ForgeCatalog.Get((int)ForgeLine.Damage).Cost[0]);
                GuideStep = GuideForge;
            }
            Save();
            return r;
        }

        public bool GiftReady => !GiftClaimed && GiftAds >= GameConstants.GiftAds;

        public void AddGiftAd()
        {
            if (GiftClaimed || GiftAds >= GameConstants.GiftAds) return;
            GiftAds++;
            Save();
        }

        // 礼包里的银宝箱不进宝箱位，领了当场开。钻石和墨先不入账，等飞进顶栏再 GrantGiftWallet。
        // 返回开出来的东西，没领成返回 null。
        public ChestLoot ClaimGift()
        {
            if (!GiftReady) return null;
            GiftClaimed = true;
            ChestLoot loot = ChestCatalog.Roll(GameConstants.GiftChest, ItemLevel, Equipped);
            ApplyLoot(loot);
            Save();
            return loot;
        }

        public void GrantGiftWallet()
        {
            AddInk(GameConstants.GiftInk);
            AddDiamond(GameConstants.GiftDiamond);
        }

        public bool ClubClaimedToday => ClubDay == Today;

        // slot 是木宝箱进了哪个位。-1 位满了先寄着，-3 寄不下才折成墨。
        public bool ClaimClub(out int slot)
        {
            slot = -1;
            if (ClubClaimedToday) return false;
            ClubDay = Today;
            Ink += GameConstants.ClubInk;
            slot = GrantChest(ChestTier.Wood);
            Save();
            return true;
        }

        public bool CheckedToday => CheckDay == Today;

        // 新手指引走完，并且今天还没自动弹过签到页。
        public bool CheckPopDue => !Guiding && CheckPopDay != Today;

        public void MarkCheckPop()
        {
            if (CheckPopDay == Today) return;
            CheckPopDay = Today;
            Save();
        }

        public bool CheckAdDoneToday => CheckAdDay == Today;

        // 这一轮已签几天（0~7）。昨天没签、或者上一轮已满 7 天，从 0 算起。
        public int CheckShown
        {
            get
            {
                if (CheckedToday) return Mathf.Clamp(CheckRun, 0, GameConstants.CheckDays);
                bool alive = CheckDay == ShiftDay(Today, -1) && CheckRun < GameConstants.CheckDays;
                return alive ? Mathf.Max(0, CheckRun) : 0;
            }
        }

        // 第 7 天是否还送皮肤：没送过，而且玩家还没有那一款。
        public bool CheckSkinPending => !CheckSkinDone && !(GameConstants.CheckSkin < SkinOwned.Length
                                                            && SkinOwned[GameConstants.CheckSkin]);

        public static int CheckDiamondOf(int day)
        {
            int[] table = GameConstants.CheckDiamonds;
            return table[Mathf.Clamp(day, 1, table.Length) - 1];
        }

        // 返回今天是第几天（1~7），签过了返回 0。skin 表示这一签送了签到皮肤。
        // chest 是第 7 天的金宝箱进了哪个位：-2 今天没有宝箱，-1 位满了先寄着，-3 寄不下才折成墨。
        public int CheckIn(bool doubled, out bool skin, out int chest)
        {
            skin = false;
            chest = -2;
            if (CheckedToday) return 0;
            int day = CheckShown + 1;
            CheckRun = day;
            CheckDay = Today;
            int times = doubled ? 2 : 1;
            if (doubled) CheckAdDay = Today;
            Ink += GameConstants.CheckInk * times;
            Diamond += CheckDiamondOf(day) * times;
            Stamina += GameConstants.CheckStamina * times;
            if (day == GameConstants.CheckDays)
            {
                chest = GrantChest(ChestTier.Gold);
                CheckLoops++;
                if (!CheckSkinDone)
                {
                    CheckSkinDone = true;
                    if (GrantSkin(GameConstants.CheckSkin)) skin = true;
                }
            }
            Save();
            return day;
        }

        // 签完没翻倍的，当天还能补看一次广告再领一份。
        public bool CheckAdBonus()
        {
            if (!CheckedToday || CheckAdDoneToday) return false;
            CheckAdDay = Today;
            Ink += GameConstants.CheckInk;
            Diamond += CheckDiamondOf(CheckRun);
            Stamina += GameConstants.CheckStamina;
            Save();
            return true;
        }

        // GM：把签到记录往前挪一天，等于跨天，连签不断。
        public void GmCheckNextDay()
        {
            CheckDay = ShiftDay(CheckDay, -1);
            CheckAdDay = ShiftDay(CheckAdDay, -1);
            CheckPopDay = ShiftDay(CheckPopDay, -1);
            Save();
        }

        // ---- 图鉴 ----

        public bool CodexKnows(CodexKind kind, int i)
        {
            switch (kind)
            {
                case CodexKind.Glyph: return (CodexGlyph & (1L << i)) != 0;
                case CodexKind.Word: return (CodexWord & (1 << i)) != 0;
                case CodexKind.Enemy: return (CodexEnemy & (1 << i)) != 0;
                default: return (CodexPair & (1 << i)) != 0;
            }
        }

        public bool CodexIsNew(CodexKind kind, int i)
        {
            switch (kind)
            {
                case CodexKind.Glyph: return (CodexNewGlyph & (1L << i)) != 0;
                case CodexKind.Word: return (CodexNewWord & (1 << i)) != 0;
                case CodexKind.Enemy: return (CodexNewEnemy & (1 << i)) != 0;
                default: return (CodexNewPair & (1 << i)) != 0;
            }
        }

        public bool CodexHasNew =>
            CodexNewGlyph != 0 || CodexNewWord != 0 || CodexNewPair != 0
            || CodexNewEnemy != 0 || CodexHasMile;
        public bool CodexPairNew => CodexNewPair != 0;
        public bool CodexBaseNew => CodexNewGlyph != 0 || CodexNewWord != 0;
        public bool CodexEnemyNew => CodexNewEnemy != 0;

        public bool CodexTabNew(CodexTab tab)
        {
            switch (tab)
            {
                case CodexTab.Pair: return CodexPairNew || TabHasMile(tab);
                case CodexTab.Enemy: return CodexEnemyNew || TabHasMile(tab);
                default: return CodexBaseNew || TabHasMile(tab);
            }
        }

        public bool CodexHasMile =>
            TabHasMile(CodexTab.Glyph) || TabHasMile(CodexTab.Pair) || TabHasMile(CodexTab.Enemy);

        public bool TabHasMile(CodexTab tab)
        {
            IReadOnlyList<CodexEntry> list = CodexCatalog.Of(tab);
            int known = CodexCatalog.CountKnown(this, list);
            CodexMile[] miles = CodexCatalog.Miles(tab);
            for (int i = 0; i < miles.Length; i++)
                if (known >= miles[i].Need && !MileClaimed(tab, i)) return true;
            return false;
        }

        public bool MileClaimed(CodexTab tab, int i) =>
            (CodexMileClaim & (1 << ((int)tab * 3 + i))) != 0;

        // 领到了返回墨数，还没到或已经领过返回 0。
        public int ClaimMile(CodexTab tab, int i)
        {
            CodexMile[] miles = CodexCatalog.Miles(tab);
            if (i < 0 || i >= miles.Length) return 0;
            if (MileClaimed(tab, i)) return 0;
            if (CodexCatalog.CountKnown(this, CodexCatalog.Of(tab)) < miles[i].Need) return 0;
            CodexMileClaim |= 1 << ((int)tab * 3 + i);
            AddInk(miles[i].Ink);
            return miles[i].Ink;
        }

        // 第一次收录返回 true。
        public bool CodexLearn(CodexKind kind, int i)
        {
            if (CodexKnows(kind, i)) return false;
            switch (kind)
            {
                case CodexKind.Glyph: CodexGlyph |= 1L << i; CodexNewGlyph |= 1L << i; break;
                case CodexKind.Word: CodexWord |= 1 << i; CodexNewWord |= 1 << i; break;
                case CodexKind.Enemy: CodexEnemy |= 1 << i; CodexNewEnemy |= 1 << i; break;
                default: CodexPair |= 1 << i; CodexNewPair |= 1 << i; break;
            }
            Save();
            return true;
        }

        public void CodexSeen(CodexKind kind, int i)
        {
            if (!CodexIsNew(kind, i)) return;
            switch (kind)
            {
                case CodexKind.Glyph: CodexNewGlyph &= ~(1L << i); break;
                case CodexKind.Word: CodexNewWord &= ~(1 << i); break;
                case CodexKind.Enemy: CodexNewEnemy &= ~(1 << i); break;
                default: CodexNewPair &= ~(1 << i); break;
            }
            Save();
        }

        public void UnlockCodex()
        {
            for (int i = 1; i < CardCatalog.IdCount; i++)
                if (CardCatalog.Get((CardId)i).Wake != CardWake.WordPart) CodexLearn(CodexKind.Glyph, i);
            for (int i = (int)WordId.InstantKill; i <= (int)WordId.Cleave; i++) CodexLearn(CodexKind.Word, i);
            for (int i = 0; i < SignaturePairs.All.Length; i++) CodexLearn(CodexKind.Pair, i);
            for (int i = 0; i < EnemyCatalog.IdCount; i++) CodexLearn(CodexKind.Enemy, i);
        }

        public void AddInk(int n)
        {
            if (n <= 0) return;
            Ink += n;
            Save();
        }

        public int ForgeLevel(int line) => Forge[Mathf.Clamp(line, 0, ForgeCatalog.LineCount - 1)];

        // 炮台页上有没有这一行。通用看通关数，专属看有没有那款皮肤。
        public bool ForgeShown(int line)
        {
            ForgeDef d = ForgeCatalog.Get(line);
            int lv = ForgeLevel(line);
            if (!d.Exclusive) return ForgeCatalog.Exposed(line, ClearedCount(), lv);
            return lv > 0 || (d.Skin >= 0 && d.Skin < SkinOwned.Length && SkinOwned[d.Skin]);
        }

        public bool ForgeFresh(int line) => (ForgeNew & (1L << line)) != 0;

        // 炮台页看过就摘掉「新」。返回摘之前的那份，好让这一屏还能接着显示。
        public long TakeForgeNew()
        {
            long fresh = ForgeNew;
            if (fresh == 0) return 0;
            ForgeNew = 0;
            Save();
            return fresh;
        }

        // 刚亮相、还没弹过解锁卡的词条。按炮台页的排列给，取了就算弹过。
        int[] TakeForgeReveals()
        {
            var got = new List<int>();
            for (int i = 0; i < ForgeCatalog.LineCount; i++)
            {
                long bit = 1L << i;
                if ((ForgeSeen & bit) != 0 || !ForgeShown(i)) continue;
                ForgeSeen |= bit;
                ForgeNew |= bit;
                got.Add(i);
            }
            got.Sort((a, b) => ForgeCatalog.Rank(a).CompareTo(ForgeCatalog.Rank(b)));
            return got.ToArray();
        }

        public int Wallet(ForgeCoin coin) => coin == ForgeCoin.Token ? Token : Ink;

        static string CoinName(ForgeCoin coin) => coin == ForgeCoin.Token ? "活动币" : "墨";

        // 买不起 / 没到门槛时 why 里放要显示给玩家的那句话。
        public bool CanBuyForge(int line, out string why)
        {
            int lv = ForgeLevel(line);
            ForgeDef d = ForgeCatalog.Get(line);
            if (!ForgeShown(line)) { why = "未开放"; return false; }
            if (lv >= d.MaxLevel) { why = "已满级"; return false; }
            int gate = ForgeCatalog.Gate(line, lv);
            if (gate > GameConstants.StageCount)
            {
                why = string.IsNullOrEmpty(d.LockNote) ? "暂未开放" : d.LockNote;
                return false;
            }
            if (ClearedCount() < gate) { why = $"通关 {gate} 关"; return false; }
            int cost = ForgeCatalog.Cost(line, lv);
            int have = Wallet(d.Coin);
            if (have < cost) { why = $"差 {cost - have} {CoinName(d.Coin)}"; return false; }
            why = "";
            return true;
        }

        public bool BuyForge(int line)
        {
            if (!CanBuyForge(line, out _)) return false;
            int cost = ForgeCatalog.Cost(line, ForgeLevel(line));
            if (ForgeCatalog.Get(line).Coin == ForgeCoin.Token) Token -= cost;
            else Ink -= cost;
            Forge[line]++;
            Save();
            return true;
        }

        public bool CanBuySkin(int i, out string why)
        {
            SkinDef d = SkinCatalog.Get(i);
            if (i < 0 || i >= SkinOwned.Length || SkinOwned[i]) { why = ""; return false; }
            if (d.Way != SkinWay.Ink) { why = SkinCatalog.LockText(d); return false; }
            if (d.Chapter >= 0 && !ChapterCleared(d.Chapter)) { why = SkinCatalog.LockText(d); return false; }
            if (Ink < d.Price) { why = $"差 {d.Price - Ink} 墨"; return false; }
            why = "";
            return true;
        }

        public bool BuySkin(int i)
        {
            if (!CanBuySkin(i, out _)) return false;
            Ink -= SkinCatalog.Get(i).Price;
            return GrantSkin(i);
        }

        // 广告、签到、活动发皮肤都走这里。到手就换上，专属词条跟着挂「新」。
        public bool GrantSkin(int i)
        {
            if (i < 0 || i >= SkinCatalog.Count) return false;
            SkinOwned = Fit(SkinOwned, SkinCatalog.Count);
            if (SkinOwned[i]) return false;
            SkinOwned[i] = true;
            Skin = i;
            SkinSeen |= 1 << i;
            SkinNew |= 1 << i;
            for (int k = 0; k < ForgeCatalog.LineCount; k++)
            {
                ForgeDef d = ForgeCatalog.Get(k);
                if (!d.Exclusive || d.Skin != i) continue;
                ForgeSeen |= 1L << k;
                ForgeNew |= 1L << k;
            }
            Save();
            return true;
        }

        public void EquipSkin(int i)
        {
            if (i < 0 || i >= SkinCatalog.Count || !SkinOwned[i]) return;
            Skin = i;
            Save();
        }

        // ---- 道具 ----

        public int ItemRank(int i) =>
            ItemLevel != null && i >= 0 && i < ItemLevel.Length ? ItemLevel[i] : 0;

        public int ItemCardCount(int i) =>
            ItemCards != null && i >= 0 && i < ItemCards.Length ? ItemCards[i] : 0;

        // 解锁只看卡，升级要卡和墨。缺什么 why 里写给玩家看。
        public bool CanUpgradeItem(int i, out string why)
        {
            if (i < 0 || i >= ItemCatalog.Count) { why = ""; return false; }
            int rank = ItemRank(i);
            if (rank >= ItemCatalog.MaxLevel) { why = "已满级"; return false; }
            ItemDef d = ItemCatalog.Get(i);
            int need = ItemCatalog.NextCards(d, rank);
            int have = ItemCardCount(i);
            if (have < need) { why = have + "/" + need; return false; }
            int price = ItemCatalog.NextPrice(d, rank);
            if (Ink < price) { why = $"差 {price - Ink} 墨"; return false; }
            why = "";
            return true;
        }

        // equip=false 时解锁后不自动装上，新手指引要让玩家自己点一次「装备」。
        public bool UpgradeItem(int i, bool equip = true)
        {
            if (!CanUpgradeItem(i, out _)) return false;
            int rank = ItemRank(i);
            ItemDef d = ItemCatalog.Get(i);
            Ink -= ItemCatalog.NextPrice(d, rank);
            ItemCards[i] -= ItemCatalog.NextCards(d, rank);
            ItemLevel[i] = rank + 1;
            if (equip && rank <= 0 && EquippedSlot(i) < 0 && FreeItemSlot >= 0) Equipped[FreeItemSlot] = i;
            Save();
            return true;
        }

        // 底栏道具页的红点：有道具攒够了卡、墨也够，能解锁或升级。
        public bool AnyItemReady
        {
            get
            {
                for (int i = 0; i < ItemCatalog.Count; i++)
                    if (CanUpgradeItem(i, out _)) return true;
                return false;
            }
        }

        // ---- 胜利宝箱 ----

        public static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public ChestState ChestStateOf(int slot)
        {
            if (slot < 0 || slot >= ChestSlot.Length || ChestSlot[slot] <= 0) return ChestState.Empty;
            long done = ChestDone[slot];
            if (done <= 0L) return ChestState.Locked;
            return NowMs >= done ? ChestState.Ready : ChestState.Timing;
        }

        public ChestTier ChestTierOf(int slot) =>
            (ChestTier)Mathf.Clamp(slot >= 0 && slot < ChestSlot.Length ? ChestSlot[slot] - 1 : 0, 0, 3);

        public int ChestSecondsLeft(int slot)
        {
            ChestState st = ChestStateOf(slot);
            if (st == ChestState.Locked) return ChestCatalog.Get(ChestTierOf(slot)).Seconds;
            if (st != ChestState.Timing) return 0;
            long left = ChestDone[slot] - NowMs;
            return left <= 0L ? 0 : (int)((left + 999L) / 1000L);
        }

        public int TimingSlot
        {
            get
            {
                for (int i = 0; i < ChestSlot.Length; i++)
                    if (ChestStateOf(i) == ChestState.Timing) return i;
                return -1;
            }
        }

        public bool ChestsFull
        {
            get
            {
                for (int i = 0; i < ChestSlot.Length; i++)
                    if (ChestSlot[i] <= 0) return false;
                return true;
            }
        }

        public bool AnyChestReady
        {
            get
            {
                for (int i = 0; i < ChestSlot.Length; i++)
                    if (ChestStateOf(i) == ChestState.Ready) return true;
                return false;
            }
        }

        // 同一时间只解一个。没有在解的就自动接着解下一个，玩家不用回首页点。
        // 系统时间往回拨会让剩余时间比整箱时长还长，拉回整箱时长，免得卡死。
        bool TickChests()
        {
            bool dirty = false;
            long now = NowMs;
            int timing = -1;
            for (int i = 0; i < ChestSlot.Length; i++)
            {
                if (ChestStateOf(i) != ChestState.Timing) continue;
                long full = ChestCatalog.Get(ChestTierOf(i)).Seconds * 1000L;
                if (ChestDone[i] - now > full)
                {
                    ChestDone[i] = now + full;
                    dirty = true;
                }
                timing = i;
            }
            if (timing >= 0) return dirty;
            for (int i = 0; i < ChestSlot.Length; i++)
            {
                if (ChestStateOf(i) != ChestState.Locked) continue;
                ChestDone[i] = now + ChestCatalog.Get(ChestTierOf(i)).Seconds * 1000L;
                return true;
            }
            return dirty;
        }

        // 靠到最后一个空位。前面有洞时先按原来的先后靠拢，新箱子不插队。满了返回 -1。不落盘。
        int AddChest(ChestTier t)
        {
            ChestSlot = Fit(ChestSlot, ChestCatalog.Slots);
            ChestDone = Fit(ChestDone, ChestCatalog.Slots);
            PackChests(false);
            int dest = -1;
            for (int i = 0; i < ChestSlot.Length; i++)
            {
                if (ChestSlot[i] > 0) continue;
                dest = i;
                break;
            }
            if (dest < 0) return -1;
            ChestSlot[dest] = (int)t + 1;
            ChestDone[dest] = 0L;
            TickChests();
            return dest;
        }

        // 空格留给后面的箱子。slide 为真时记下每只从哪一格来，界面用来滑过去。
        bool PackChests(bool slide)
        {
            ChestSlot = Fit(ChestSlot, ChestCatalog.Slots);
            ChestDone = Fit(ChestDone, ChestCatalog.Slots);
            int n = ChestSlot.Length;
            var slot = new int[n];
            var done = new long[n];
            var from = new int[n];
            int w = 0;
            bool moved = false;
            for (int i = 0; i < n; i++)
            {
                from[i] = -1;
                if (ChestSlot[i] <= 0) continue;
                slot[w] = ChestSlot[i];
                done[w] = ChestDone[i];
                from[w] = i;
                if (i != w) moved = true;
                w++;
            }
            if (!moved)
            {
                if (slide) ChestSlideFrom = null;
                return false;
            }
            ChestSlot = slot;
            ChestDone = done;
            if (slide) ChestSlideFrom = from;
            return true;
        }

        public bool HasOverflow => OverflowChest > 0;

        public ChestTier OverflowTier => (ChestTier)Mathf.Clamp(OverflowChest - 1, 0, 3);

        // 签到、游戏圈、活动、GM 发箱子走这里。
        // 返回位号；-1 是位满了，箱子寄在 OverflowChest，等玩家选；-3 是两只都寄不下，已折成墨。
        public int GrantChest(ChestTier t)
        {
            int slot = AddChest(t);
            if (slot >= 0)
            {
                Save();
                return slot;
            }
            if (ParkChest(t))
            {
                Save();
                return -1;
            }
            Ink += ChestCatalog.InkAvg(t);
            Save();
            return -3;
        }

        bool ParkChest(ChestTier t)
        {
            int v = (int)t + 1;
            if (v < 1 || v > 4) return false;
            if (OverflowChest <= 0) { OverflowChest = v; return true; }
            if (OverflowExtra <= 0)
            {
                // 新来的先给玩家看，原来那只排在后面。
                OverflowExtra = OverflowChest;
                OverflowChest = v;
                return true;
            }
            return false;
        }

        void PromoteOverflow()
        {
            if (OverflowChest > 0) return;
            OverflowChest = OverflowExtra;
            OverflowExtra = 0;
        }

        // 把寄着的那只放进空位。没空位返回 -1，箱子还在。
        public int PlaceOverflow()
        {
            if (OverflowChest <= 0) return -1;
            int slot = AddChest(OverflowTier);
            if (slot < 0) return -1;
            OverflowChest = 0;
            PromoteOverflow();
            Save();
            return slot;
        }

        // 寄着的那只不占格子，当场开掉。看广告走这条。
        public ChestLoot OpenOverflow()
        {
            if (OverflowChest <= 0) return null;
            ChestTier tier = OverflowTier;
            OverflowChest = 0;
            PromoteOverflow();
            ChestLoot loot = ChestCatalog.Roll(tier, ItemLevel, Equipped);
            ApplyLoot(loot);
            Save();
            return loot;
        }

        // 寄着的那只换成箱里的平均墨，卡不要了。
        public int ScrapOverflow()
        {
            if (OverflowChest <= 0) return 0;
            int ink = ChestCatalog.InkAvg(OverflowTier);
            OverflowChest = 0;
            PromoteOverflow();
            Ink += ink;
            Save();
            return ink;
        }

        // 还在等的那只换成平均墨，腾出格子。已经可以打开的不走这里，打开更划算。
        public int ScrapWaitingChest(int slot)
        {
            ChestState st = ChestStateOf(slot);
            if (st != ChestState.Locked && st != ChestState.Timing) return 0;
            int ink = ChestCatalog.InkAvg(ChestTierOf(slot));
            ChestSlot[slot] = 0;
            ChestDone[slot] = 0L;
            PackChests(true);
            Ink += ink;
            TickChests();
            Save();
            return ink;
        }

        public ChestLoot OpenChest(int slot)
        {
            if (ChestStateOf(slot) != ChestState.Ready) return null;
            ChestTier tier = ChestTierOf(slot);
            // 指引里打开的就是第一关那只木箱，卡固定 3 张弹弓。之后的木箱仍随机。
            ChestLoot loot = GuideStep == GuideChest && tier == ChestTier.Wood
                ? ChestCatalog.GuideSlingshot(tier)
                : ChestCatalog.Roll(tier, ItemLevel, Equipped);
            ChestSlot[slot] = 0;
            ChestDone[slot] = 0L;
            PackChests(true);
            ApplyLoot(loot);
            TickChests();
            Save();
            return loot;
        }

        void ApplyLoot(ChestLoot loot)
        {
            if (loot == null) return;
            Ink += loot.Ink;
            for (int k = 0; k < loot.Cards.Count; k++)
            {
                CardStack c = loot.Cards[k];
                if (c.Item < 0 || c.Item >= ItemCards.Length) continue;
                ItemCards[c.Item] += c.Count;
            }
        }

        public int ChestRushCost(int slot) => ChestCatalog.DiamondCost(ChestSecondsLeft(slot));

        public bool RushChest(int slot)
        {
            ChestState st = ChestStateOf(slot);
            if (st != ChestState.Locked && st != ChestState.Timing) return false;
            int cost = ChestRushCost(slot);
            if (!SpendDiamond(cost)) return false;
            ChestDone[slot] = NowMs;
            TickChests();
            Save();
            return true;
        }

        public bool AdRushChest(int slot)
        {
            ChestState st = ChestStateOf(slot);
            if (st != ChestState.Locked && st != ChestState.Timing) return false;
            ChestDone[slot] = NowMs;
            TickChests();
            Save();
            return true;
        }

        public bool HasPendingChest => PendingChest > 0;

        // 结算页看广告：当场开，不进宝箱位，位满了也能开。
        public ChestLoot OpenPending()
        {
            if (PendingChest <= 0) return null;
            ChestLoot loot = ChestCatalog.Roll((ChestTier)(PendingChest - 1), ItemLevel, Equipped);
            PendingChest = 0;
            ApplyLoot(loot);
            Save();
            return loot;
        }

        // 离开结算页：放进宝箱位。满了就寄着，等玩家选，不折成墨。
        public void StorePending()
        {
            if (PendingChest <= 0) return;
            ResolvePending();
            Save();
        }

        bool ResolvePending()
        {
            if (PendingChest <= 0) return false;
            var t = (ChestTier)Mathf.Clamp(PendingChest - 1, 0, 3);
            PendingChest = 0;
            if (AddChest(t) >= 0) return true;
            if (!ParkChest(t)) Ink += ChestCatalog.InkAvg(t);
            return true;
        }

        // GM：把在解的那个箱子直接解完。
        public void GmFinishChests()
        {
            for (int i = 0; i < ChestSlot.Length; i++)
                if (ChestStateOf(i) == ChestState.Timing) ChestDone[i] = NowMs;
            TickChests();
            Save();
        }

        // ---- 钻石 ----

        public void AddDiamond(int n)
        {
            if (n <= 0) return;
            Diamond += n;
            Save();
        }

        public bool SpendDiamond(int n)
        {
            if (n <= 0) return true;
            if (Diamond < n) return false;
            Diamond -= n;
            return true;
        }

        // 今天下一次用钻石补体力的价格，次数用完返回 -1。
        public int StaminaDiamondPrice =>
            DiamondStamCount < GameConstants.DiamondStaminaPerDay ? GameConstants.DiamondStaminaPrice : -1;

        public bool CanBuyStamina =>
            StaminaDiamondPrice > 0 && Diamond >= StaminaDiamondPrice;

        public bool BuyStamina()
        {
            if (!CanBuyStamina) return false;
            Diamond -= StaminaDiamondPrice;
            DiamondStamCount++;
            Stamina += GameConstants.DiamondStaminaGain;
            Save();
            return true;
        }

        public int EquippedSlot(int item)
        {
            for (int s = 0; s < Equipped.Length; s++)
                if (Equipped[s] == item) return s;
            return -1;
        }

        public bool ItemSlotOpen(int slot)
        {
            int[] gate = GameConstants.ItemSlotChapter;
            if (slot < 0 || slot >= gate.Length) return false;
            return gate[slot] < 0 || ChapterCleared(gate[slot]);
        }

        public int ItemSlotsOpen
        {
            get
            {
                int n = 0;
                for (int s = 0; s < GameConstants.ItemSlots; s++)
                    if (ItemSlotOpen(s)) n++;
                return n;
            }
        }

        // 第一个已开放的空道具栏，没有返回 -1。
        public int FreeItemSlot
        {
            get
            {
                for (int s = 0; s < Equipped.Length; s++)
                    if (Equipped[s] < 0 && ItemSlotOpen(s)) return s;
                return -1;
            }
        }

        // 装进第一个空栏；栏满了返回 false，由道具页让玩家挑一格替换。
        public bool Equip(int item)
        {
            if (item < 0 || item >= ItemCatalog.Count || ItemRank(item) <= 0) return false;
            if (EquippedSlot(item) >= 0) return true;
            int free = FreeItemSlot;
            if (free < 0) return false;
            Equipped[free] = item;
            Save();
            return true;
        }

        // 换到指定那一栏。原来就在别的栏上的，两格对调。
        public bool EquipAt(int item, int slot)
        {
            if (item < 0 || item >= ItemCatalog.Count || ItemRank(item) <= 0) return false;
            if (slot < 0 || slot >= Equipped.Length || !ItemSlotOpen(slot)) return false;
            int from = EquippedSlot(item);
            if (from >= 0) Equipped[from] = Equipped[slot];
            Equipped[slot] = item;
            Save();
            return true;
        }

        public void Unequip(int item)
        {
            int at = EquippedSlot(item);
            if (at < 0) return;
            Equipped[at] = -1;
            Save();
        }
    }
}
