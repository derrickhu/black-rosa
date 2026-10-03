using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 失败页「提升以下能力」那几张卡。只推当下真能动手的：
    // 门槛已到的炮台升级、碎片集满的技能、能买的加成皮肤；都没有时补一条打法提示。
    public struct Advice
    {
        public Sprite Icon;
        public string Title;
        public string Line;
        public string Need;   // 「现在就能升」/「还差 30 墨」
        public bool Ready;
        public int Tab;       // HomeScreen 页签；-1 = 没地方可去，只是提示
    }

    public static class DefeatAdvice
    {
        public static List<Advice> Build(MetaProgress meta, int max = 3)
        {
            var list = new List<Advice>();
            Forge(meta, list);
            Spell(meta, list);
            Skin(meta, list);
            list.Sort((a, b) => b.Ready.CompareTo(a.Ready));
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            if (list.Count < 2)
                list.Add(new Advice
                {
                    Icon = InkSprites.Ui("star"), Title = "同字叠星", Line = "同一个字放上去升 2 星，威力大涨",
                    Need = "下局就试试", Ready = true, Tab = -1
                });
            return list;
        }

        static string NeedInk(int have, int cost) => have >= cost ? "现在就能升" : $"还差 {cost - have} 墨";

        static void Forge(MetaProgress meta, List<Advice> into)
        {
            int cleared = meta.ClearedCount();
            int best = -1, bestCost = int.MaxValue;
            for (int i = 0; i < ForgeCatalog.LineCount; i++)
            {
                int lv = meta.ForgeLevel(i);
                if (ForgeCatalog.Get(i).Coin != ForgeCoin.Ink) continue;
                if (!meta.ForgeShown(i) || lv >= ForgeCatalog.MaxLevel(i)) continue;
                if (cleared < ForgeCatalog.Gate(i, lv)) continue;
                int cost = ForgeCatalog.Cost(i, lv);
                if (cost < bestCost) { bestCost = cost; best = i; }
            }
            if (best < 0) return;
            ForgeDef d = ForgeCatalog.Get(best);
            into.Add(new Advice
            {
                Icon = InkSprites.Ui(d.Icon), Title = "升级" + d.Name, Line = ForgeCatalog.Step(best),
                Need = NeedInk(meta.Ink, bestCost), Ready = meta.Ink >= bestCost, Tab = HomeScreen.TabForge
            });
        }

        static void Spell(MetaProgress meta, List<Advice> into)
        {
            int pick = -1;
            float bestFill = 0f;
            for (int i = 0; i < ItemCatalog.Count; i++)
            {
                int rank = meta.ItemRank(i);
                if (rank >= ItemCatalog.MaxLevel) continue;
                int need = ItemCatalog.NextCards(ItemCatalog.Get(i), rank);
                float fill = meta.ItemCardCount(i) / (float)Mathf.Max(1, need);
                if (fill > bestFill) { bestFill = fill; pick = i; }
            }
            if (pick < 0) return;
            ItemDef d = ItemCatalog.Get(pick);
            int r = meta.ItemRank(pick);
            int needCards = ItemCatalog.NextCards(d, r);
            int have = meta.ItemCardCount(pick);
            int price = ItemCatalog.NextPrice(d, r);
            bool full = have >= needCards;
            into.Add(new Advice
            {
                Icon = InkSprites.Ui(d.Id), Title = (r > 0 ? "升级" : "解锁") + d.Name,
                Line = r > 0 ? "道具更强、冷却更快" : "战斗里多一件自动道具",
                Need = full ? NeedInk(meta.Ink, price) : $"道具卡 {have}/{needCards}",
                Ready = full && meta.Ink >= price, Tab = HomeScreen.TabSpell
            });
        }

        static void Skin(MetaProgress meta, List<Advice> into)
        {
            for (int i = 0; i < SkinCatalog.Count; i++)
            {
                SkinDef d = SkinCatalog.Get(i);
                if (d.DamageAdd <= 0.01f && d.GoldAdd <= 0 && SkinCatalog.LineOf(i) < 0) continue;
                if (meta.SkinOwned[i]) continue;
                bool ok = meta.CanBuySkin(i, out string why);
                if (!ok && !why.StartsWith("差")) continue;
                into.Add(new Advice
                {
                    Icon = InkSprites.Ui("skin_" + d.Key),
                    Title = "换上" + d.Name, Line = SkinCatalog.PerkOf(i),
                    Need = ok ? "现在就能换" : "还" + why, Ready = ok, Tab = HomeScreen.TabForge
                });
                return;
            }
        }
    }
}
