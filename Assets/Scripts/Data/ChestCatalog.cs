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

    public sealed class ChestLoot
    {
        public ChestTier Tier;
        public int Ink;
        public readonly List<CardStack> Cards = new List<CardStack>();
    }

    // 胜利宝箱。时长按「打一关一两分钟」定：木箱、银箱打下一关时就开好了，
    // 金箱、皇家箱要打几关，有点盼头又不至于劝退。
    public static class ChestCatalog
    {
        public const int Slots = 4;
        public const int SecondsPerDiamond = 30;

        static readonly ChestDef[] All =
        {
            new ChestDef
            {
                Tier = ChestTier.Wood, Name = "木宝箱", Key = "wood", Seconds = 60,
                InkMin = 8, InkMax = 12, Cards = 6
            },
            new ChestDef
            {
                Tier = ChestTier.Silver, Name = "银宝箱", Key = "silver", Seconds = 180,
                InkMin = 18, InkMax = 26, Cards = 14
            },
            new ChestDef
            {
                Tier = ChestTier.Gold, Name = "金宝箱", Key = "gold", Seconds = 480,
                InkMin = 42, InkMax = 58, Cards = 30, PurpleChance = 0.35f
            },
            new ChestDef
            {
                Tier = ChestTier.Royal, Name = "皇家宝箱", Key = "royal", Seconds = 900,
                InkMin = 90, InkMax = 120, Cards = 40, PurpleChance = 1f
            }
        };

        public static ChestDef Get(ChestTier t) => All[Mathf.Clamp((int)t, 0, All.Length - 1)];
        public static ChestDef Get(int t) => All[Mathf.Clamp(t, 0, All.Length - 1)];

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
        static void Plan(ChestTier t, List<(ItemQuality q, int n)> into)
        {
            ChestDef d = Get(t);
            switch (t)
            {
                case ChestTier.Wood:
                    into.Add((ItemQuality.Green, 4));
                    into.Add((ItemQuality.Green, 2));
                    break;
                case ChestTier.Silver:
                    into.Add((ItemQuality.Green, 7));
                    into.Add((ItemQuality.Green, 4));
                    into.Add((ItemQuality.Blue, 3));
                    break;
                case ChestTier.Gold:
                    if (Random.value < d.PurpleChance)
                    {
                        into.Add((ItemQuality.Green, 18));
                        into.Add((ItemQuality.Blue, 6));
                        into.Add((ItemQuality.Blue, 4));
                        into.Add((ItemQuality.Purple, 2));
                    }
                    else
                    {
                        into.Add((ItemQuality.Green, 12));
                        into.Add((ItemQuality.Green, 8));
                        into.Add((ItemQuality.Blue, 6));
                        into.Add((ItemQuality.Blue, 4));
                    }
                    break;
                default:
                    into.Add((ItemQuality.Green, 16));
                    into.Add((ItemQuality.Green, 5));
                    into.Add((ItemQuality.Blue, 10));
                    into.Add((ItemQuality.Blue, 6));
                    into.Add((ItemQuality.Purple, 3));
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
