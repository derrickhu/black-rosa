using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public enum ChestTier { Wood, Silver, Gold, Royal }

    public struct ChestDef
    {
        public ChestTier Tier;
        public string Name;
        public string Key;        // 美术名：Ui/chest_<Key>、chest_<Key>_open
        public int Seconds;       // 解锁要等多久
        public int InkMin;
        public int InkMax;
        public int Cards;
        public float PurpleChance;
    }

    // 开箱掉的一叠卡：哪个道具、几张。Spare 是满级后折成的墨，已经算进 ChestLoot.Ink。
    public struct CardStack
    {
        public int Item;
        public int Count;
        public int Spare;
    }

    // 炮台宝箱开出来的一叠炮台碎片。Have / Need 是入账之后的进度。
    public struct ShardStack
    {
        public int Skin;
        public int Count;
        public int Have;
        public int Need;
        public bool Unlocked;
    }

    public sealed class ChestLoot
    {
        public ChestTier Tier;
        public int Ink;
        public readonly List<CardStack> Cards = new List<CardStack>();
        public string Title;     // 有值时盖过品阶名，炮台宝箱用
        public string ArtKey;    // 有值时用 Ui/chest_<ArtKey>
        public string Note;
        public readonly List<ShardStack> Shards = new List<ShardStack>();
    }

    // 胜利宝箱。一关大约一两分钟：木箱打完下一关就开好，银箱再打一两关，
    // 金箱、皇家箱留着慢慢等。
    public static class ChestCatalog
    {
        public const int Slots = 4;
        public const int SecondsPerDiamond = 30;

        static readonly ChestDef[] All =
        {
            new ChestDef
            {
                Tier = ChestTier.Wood, Name = "木宝箱", Key = "wood", Seconds = 120,
                InkMin = 8, InkMax = 12, Cards = 2
            },
            new ChestDef
            {
                Tier = ChestTier.Silver, Name = "银宝箱", Key = "silver", Seconds = 300,
                InkMin = 18, InkMax = 26, Cards = 4
            },
            new ChestDef
            {
                Tier = ChestTier.Gold, Name = "金宝箱", Key = "gold", Seconds = 720,
                InkMin = 42, InkMax = 58, Cards = 6, PurpleChance = 0.35f
            },
            new ChestDef
            {
                Tier = ChestTier.Royal, Name = "皇家宝箱", Key = "royal", Seconds = 1200,
                InkMin = 90, InkMax = 120, Cards = 8, PurpleChance = 1f
            }
        };

        public static ChestDef Get(ChestTier t) => All[Mathf.Clamp((int)t, 0, All.Length - 1)];
        public static ChestDef Get(int t) => All[Mathf.Clamp(t, 0, All.Length - 1)];

        // 炮台宝箱。章节满星会发一只，以后别的活动也可以发同一只。
        // 只从「集碎片到手」的炮里抽。稀有单独占一小截，其余炮平分剩下的概率。
        public const string CannonName = "炮台宝箱";
        public const string CannonArt = "cannon";
        public const float CannonRare = 0.05f;
        public const int CannonShards = 2;

        public struct CannonRoll
        {
            public int Skin;
            public int Count;
        }

        public static CannonRoll RollCannon()
        {
            var common = new List<int>();
            var rare = new List<int>();
            for (int i = 0; i < SkinCatalog.Count; i++)
            {
                SkinDef d = SkinCatalog.Get(i);
                if (d.Way != SkinWay.Shard || d.Shards <= 0) continue;
                if (d.Rarity == SkinRarity.Rare) rare.Add(i);
                else common.Add(i);
            }
            bool wantRare = rare.Count > 0 && Random.value < CannonRare;
            List<int> pool = wantRare ? rare : common;
            if (pool.Count == 0) pool = rare.Count > 0 ? rare : common;
            int skin = pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : SkinCatalog.Gilt;
            return new CannonRoll { Skin = skin, Count = CannonShards };
        }

        // 普通关按这张表轮着发，不纯靠随机。boss 关、章底另算。
        static readonly ChestTier[] Cycle =
        {
            ChestTier.Wood, ChestTier.Wood, ChestTier.Silver, ChestTier.Wood,
            ChestTier.Wood, ChestTier.Silver, ChestTier.Wood, ChestTier.Gold
        };

        public static int CycleLength => Cycle.Length;

        public static ChestTier ForStage(StageDef s, bool firstClear, int cycle)
        {
            if (s.Finale) return firstClear ? ChestTier.Royal : ChestTier.Gold;
            if (s.HasBoss) return ChestTier.Gold;
            return Cycle[((cycle % Cycle.Length) + Cycle.Length) % Cycle.Length];
        }

        public static int InkAvg(ChestTier t)
        {
            ChestDef d = Get(t);
            return (d.InkMin + d.InkMax) / 2;
        }

        public static int DiamondCost(int secondsLeft) =>
            Mathf.Max(1, Mathf.CeilToInt(secondsLeft / (float)SecondsPerDiamond));

        public static string Clock(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            if (seconds >= 3600) return $"{seconds / 3600}时{seconds / 60 % 60:00}分";
            return $"{seconds / 60}:{seconds % 60:00}";
        }

        public static string Span(int seconds) =>
            seconds >= 60 ? (seconds / 60) + " 分钟" : seconds + " 秒";

        // 一个箱子里分几叠、每叠什么品质。数字是每叠的张数，合计等于 Cards。
        // 单叠停在解锁线下面：绿要 6 张、蓝要 4 张、紫要 5 张。一箱只推进一步。
        static void Plan(ChestTier t, List<(ItemQuality q, int n)> into)
        {
            ChestDef d = Get(t);
            switch (t)
            {
                case ChestTier.Wood:
                    into.Add((ItemQuality.Green, 2));
                    break;
                case ChestTier.Silver:
                    into.Add((ItemQuality.Green, 2));
                    into.Add((ItemQuality.Green, 1));
                    into.Add((ItemQuality.Blue, 1));
                    break;
                case ChestTier.Gold:
                    if (Random.value < d.PurpleChance)
                    {
                        into.Add((ItemQuality.Green, 3));
                        into.Add((ItemQuality.Blue, 2));
                        into.Add((ItemQuality.Purple, 1));
                    }
                    else
                    {
                        into.Add((ItemQuality.Green, 3));
                        into.Add((ItemQuality.Green, 1));
                        into.Add((ItemQuality.Blue, 2));
                    }
                    break;
                default:
                    into.Add((ItemQuality.Green, 4));
                    into.Add((ItemQuality.Green, 1));
                    into.Add((ItemQuality.Blue, 2));
                    into.Add((ItemQuality.Purple, 1));
                    break;
            }
        }

        // 开箱。只算掉什么，不碰存档 —— MetaProgress.OpenChest 拿结果去入账。
        // level / equipped 用来挑卡：没满级的才进池，装着的道具多一份权重，
        // 皇家箱的紫卡优先给还没解锁的道具。
        public static ChestLoot Roll(ChestTier t, int[] level, int[] equipped)
        {
            ChestDef d = Get(t);
            var loot = new ChestLoot { Tier = t, Ink = Random.Range(d.InkMin, d.InkMax + 1) };
            var plan = new List<(ItemQuality q, int n)>();
            Plan(t, plan);
            var used = new List<int>();
            for (int i = 0; i < plan.Count; i++)
            {
                int pick = PickItem(plan[i].q, level, equipped, used, t == ChestTier.Royal && plan[i].q == ItemQuality.Purple);
                if (pick < 0)
                {
                    loot.Ink += plan[i].n * ItemCatalog.SpareInkOf(new ItemDef { Quality = plan[i].q });
                    continue;
                }
                used.Add(pick);
                loot.Cards.Add(new CardStack { Item = pick, Count = plan[i].n });
            }
            loot.Cards.Sort((a, b) => ItemCatalog.Get(a.Item).Quality.CompareTo(ItemCatalog.Get(b.Item).Quality));
            return loot;
        }

        // 新手第一关的木箱：墨照常，卡固定是弹弓。普通木箱仍走 Roll。
        public static ChestLoot GuideSlingshot(ChestTier t)
        {
            ChestDef d = Get(t);
            var loot = new ChestLoot { Tier = t, Ink = Random.Range(d.InkMin, d.InkMax + 1) };
            loot.Cards.Add(new CardStack { Item = (int)ItemId.Snipe, Count = GameConstants.GuideSlingshot });
            return loot;
        }

        static int PickItem(ItemQuality q, int[] level, int[] equipped, List<int> used, bool preferLocked)
        {
            var pool = new List<int>();
            var weight = new List<int>();
            for (int pass = 0; pass < 2 && pool.Count == 0; pass++)
            {
                for (int i = 0; i < ItemCatalog.Count; i++)
                {
                    ItemDef def = ItemCatalog.Get(i);
                    if (def.Quality != q) continue;
                    int lv = level != null && i < level.Length ? level[i] : 0;
                    if (lv >= ItemCatalog.MaxLevel) continue;
                    // 第一轮不和同箱已有的叠重复，凑不出来才放开。
                    if (pass == 0 && used.Contains(i)) continue;
                    int w = 10;
                    if (Worn(equipped, i)) w += 4;
                    if (preferLocked && lv <= 0) w += 30;
                    pool.Add(i);
                    weight.Add(w);
                }
            }
            if (pool.Count == 0) return -1;
            int sum = 0;
            for (int i = 0; i < weight.Count; i++) sum += weight[i];
            int roll = Random.Range(0, sum);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weight[i];
                if (roll < 0) return pool[i];
            }
            return pool[pool.Count - 1];
        }

        static bool Worn(int[] equipped, int item)
        {
            if (equipped == null) return false;
            for (int i = 0; i < equipped.Length; i++)
                if (equipped[i] == item) return true;
            return false;
        }
    }
}
