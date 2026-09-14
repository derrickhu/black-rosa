using UnityEngine;

namespace InkLine
{
    // 招牌两两。每对只做两件事：换体槽那一张图，加一条小加成。
    // 结算骨架一行都不动，所以三合、四合照样按五槽叠色兜底。
    public struct SignaturePair
    {
        public CardId A;
        public CardId B;
        public string Name;
        public ShotFx Form;
        public string Note;
    }

    public static class SignaturePairs
    {
        public static readonly SignaturePair[] All =
        {
            new SignaturePair { A = CardId.Fire, B = CardId.Ice, Name = "霜火",
                Form = ShotFx.FormFrostFire, Note = "灼烧 DPS +25%" },
            new SignaturePair { A = CardId.Fire, B = CardId.Thunder, Name = "焦雷",
                Form = ShotFx.FormScorchBolt, Note = "晕结束时补一次灼烧 DPS ×2 的伤害" },
            new SignaturePair { A = CardId.Ice, B = CardId.Thunder, Name = "霰雷",
                Form = ShotFx.FormHailBolt, Note = "对冻结目标施晕改为碎冰，本次伤害 +40%" },
            new SignaturePair { A = CardId.Fire, B = CardId.Poison, Name = "燎毒",
                Form = ShotFx.FormBlightFire, Note = "毒层数上限 +1" },
            new SignaturePair { A = CardId.Water, B = CardId.Thunder, Name = "导电",
                Form = ShotFx.FormConduct, Note = "雷连带数 +1，范围 +30%" },
            new SignaturePair { A = CardId.Gold, B = CardId.Fire, Name = "熔金",
                Form = ShotFx.FormMoltenGold, Note = "灼烧改为按目标最大生命 1%/s 追加" },
            new SignaturePair { A = CardId.Gold, B = CardId.Heavy, Name = "镇金",
                Form = ShotFx.FormWardGold, Note = "加算部分也吃重的乘算" },
            new SignaturePair { A = CardId.Wood, B = CardId.Poison, Name = "蚀生",
                Form = ShotFx.FormRotLife, Note = "毒造成的伤害 100% 计入吸血池" },
            new SignaturePair { A = CardId.Earth, B = CardId.Heavy, Name = "夯土",
                Form = ShotFx.FormRamEarth, Note = "上推距离 +50%" },
            new SignaturePair { A = CardId.Wind, B = CardId.Ice, Name = "寒风",
                Form = ShotFx.FormColdWind, Note = "横移后附带缓" },
            new SignaturePair { A = CardId.Explode, B = CardId.Fire, Name = "烈爆",
                Form = ShotFx.FormBlaze, Note = "爆圈内留 1.5s 火地，持续 DPS 50%" },
            new SignaturePair { A = CardId.Slash, B = CardId.Thunder, Name = "雷决",
                Form = ShotFx.FormThunderCut, Note = "斩杀阈值 +8%" }
        };

        public static bool Lit(ShotMods m, int index)
        {
            SignaturePair p = All[index];
            return m.Star(p.A) > 0 && m.Star(p.B) > 0;
        }

        static bool Lit(ShotMods m, CardId a, CardId b) => m.Star(a) > 0 && m.Star(b) > 0;

        // 在第一次命中前跑一次。只改已经收集好的数值，不新增结算步骤，
        // 所以三合、四合仍然按五槽叠色 + 普通结算兜底。
        public static void Tune(ShotMods m)
        {
            if (m.Tuned) return;
            m.Tuned = true;

            if (Lit(m, CardId.Fire, CardId.Ice))                    // 霜火
                m.ScaleStatus(StatusKind.Burn, 1.25f);
            if (Lit(m, CardId.Fire, CardId.Thunder))                // 焦雷
                m.Shock = m.StatusPower(StatusKind.Burn) * 2f;
            if (Lit(m, CardId.Ice, CardId.Thunder))                 // 霰雷
                m.Shatter = true;
            if (Lit(m, CardId.Fire, CardId.Poison))                 // 燎毒
                m.BumpStacks(StatusKind.Poison, 1);
            if (Lit(m, CardId.Water, CardId.Thunder))               // 导电
                m.WidenStatus(StatusKind.Stun, 1, 1.3f);
            if (Lit(m, CardId.Gold, CardId.Fire))                   // 熔金
                m.BurnByMaxHp = true;
            if (Lit(m, CardId.Gold, CardId.Heavy))                  // 镇金
                m.GoldRidesMul = true;
            if (Lit(m, CardId.Wood, CardId.Poison))                 // 蚀生
                m.PoisonLeech = Mathf.Max(1f, m.LeechCap);
            if (Lit(m, CardId.Earth, CardId.Heavy))                 // 夯土
                m.PushColumn *= 1.5f;
            if (Lit(m, CardId.Wind, CardId.Ice))                    // 寒风
                m.AddStatus(new StatusHit { Kind = StatusKind.Slow, Power = 0.7f, Time = 1.2f });
            if (Lit(m, CardId.Explode, CardId.Fire))                // 烈爆
                m.BlazeGround = true;
            if (Lit(m, CardId.Slash, CardId.Thunder))               // 雷决
            {
                m.ExecuteHp += 0.08f;
                m.ExecuteBoss += 0.08f;
            }
        }
    }
}
