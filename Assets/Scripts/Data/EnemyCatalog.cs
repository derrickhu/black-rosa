using UnityEngine;

namespace InkLine
{
    public readonly struct EnemyDef
    {
        public readonly EnemyId Id;
        public readonly float Hp;
        public readonly float Speed;
        // 掉落是怪自己的固定属性，不再由关卡发预算往下摊。这样「补墨 5 金、
        // 厚甲 6 金」是一条学得会的知识，先打谁才有得算，也才做得了图鉴。
        public readonly int Gold;
        // 墨是小数：按需要的量级，一只杂兵大约只值 0.1 墨，取整会全抹成 0。
        // 零头由 BattleWorld 攒着，满 1 才淌一摊 —— 表现上和以前一模一样。
        public readonly float Ink;
        public readonly float Radius;
        public readonly bool HasShield;
        public readonly bool Strafe;
        public readonly bool PreferEmpty;
        public readonly bool IsBoss;
        public readonly bool ColorsInPhase2;

        // 以下五条是关底用的特性。设计意图是「互相制衡的答题卡」而不是数值梯度：
        // 每条专门惩罚一种偷懒配法，逼玩家换思路。见 docs/敌人设计.md。

        // 每发固定减伤。惩罚多段小伤害（分裂弹），奖励重击。保底仍掉 1 点，
        // 不做成完全免疫 —— 否则配错字的玩家会卡死，只会以为游戏坏了。
        public readonly float Armor;
        // 血量过半后的速度倍率，1 表示不狂化。惩罚「打到半血就换目标」。
        public readonly float RageSpeed;
        // 免定身：冻和晕都不吃，降级成等时长的缓。惩罚纯控制流派。
        public readonly bool StunImmune;
        // 免击退：退和风都推不动。惩罚只堆位移的配法。
        public readonly bool NoKnock;
        // 每秒给范围内的**其他**敌人回多少血，0 表示不回。不回自己 ——
        // 这样「先杀奶妈」才是正解，单独一只落单的奶妈也不会变成打不死的肉盾。
        public readonly float HealAura;
        // 死后在原地裂出几只墨粒。惩罚无脑范围清场，奖励单点集火。
        public readonly int SplitCount;

        public EnemyDef(EnemyId id, float hp, float speed, int gold, float ink, float radius,
            bool shield = false, bool strafe = false, bool preferEmpty = false,
            bool boss = false, bool colorPhase = false, float armor = 0f,
            float rageSpeed = 1f, bool stunImmune = false, bool noKnock = false,
            float healAura = 0f, int splitCount = 0)
        {
            Id = id;
            Hp = hp;
            Speed = speed;
            Gold = gold;
            Ink = ink;
            Radius = radius;
            HasShield = shield;
            Strafe = strafe;
            PreferEmpty = preferEmpty;
            IsBoss = boss;
            ColorsInPhase2 = colorPhase;
            Armor = armor;
            RageSpeed = rageSpeed;
            StunImmune = stunImmune;
            NoKnock = noKnock;
            HealAura = healAura;
            SplitCount = splitCount;
        }

        // 套上关卡倍率。血按 t 走，掉落按缓得多的 dropMul 走，甲也按 t 走 ——
        // 甲是固定减伤，不跟着放大的话后期伤害一上来，厚甲和铁桶那一课就没了。
        // 金币原本有掉落的保底还留 1，免得低章节四舍五入成 0。
        EnemyDef(EnemyDef s, float t, float dropMul)
        {
            Id = s.Id;
            Hp = s.Hp * t;
            Speed = s.Speed;
            Gold = Drop(s.Gold, dropMul);
            Ink = s.Ink * dropMul;
            Radius = s.Radius;
            HasShield = s.HasShield;
            Strafe = s.Strafe;
            PreferEmpty = s.PreferEmpty;
            IsBoss = s.IsBoss;
            ColorsInPhase2 = s.ColorsInPhase2;
            Armor = s.Armor * t;
            RageSpeed = s.RageSpeed;
            StunImmune = s.StunImmune;
            NoKnock = s.NoKnock;
            HealAura = s.HealAura;
            SplitCount = s.SplitCount;
        }

        public EnemyDef Scale(float t, float dropMul) => new EnemyDef(this, t, dropMul);

        static int Drop(int v, float mul) => EnemyCatalog.Drop(v, mul);
    }

    public static class EnemyCatalog
    {
        // 奶光环的半径，约一格半。再大就会隔着好几列偷偷奶到，玩家看不出因果。
        public const float HealRange = 1.4f;

        // 一只写在关卡表里的兵，实际刷几只。
        //
        // 关卡表里那些 1~3 只的批量摆在 6 列的场上太稀了。这里把轻甲杂兵按倍数
        // 铺开，每只吃满表血、也掉满自己那份 —— 人多就该更难打，也该更值钱。
        //
        // 有机制的那几只（奶妈、厚甲、盾、墨尊、镇守）和全部关底都留 1。
        // 它们各自在教一条规矩，复制一份只会把那一课变成数值消耗战。
        public static int Density(EnemyId id)
        {
            switch (id)
            {
                case EnemyId.Swarm:
                    return 3;
                case EnemyId.Ball:
                case EnemyId.Walker:
                case EnemyId.Tall:
                case EnemyId.Chubby:
                case EnemyId.BigHead:
                case EnemyId.Runner:
                case EnemyId.Strafer:
                case EnemyId.Belt:
                case EnemyId.Crawler:
                case EnemyId.Sprinter:
                case EnemyId.Splitter:
                    return 2;
                default:
                    return 1;
            }
        }

        // 掉落的章节系数。血量吃满 t（第八章 4.99 倍），掉落吃得比它缓得多，
        // 否则后期每点伤害换来的金币会一路跌到五分之一。开 0.6 次方后
        // 第八章约 2.62 倍：后期刷更值，但「厚甲 6 金」这条知识全章都成立。
        public static float DropMul(float t) => Mathf.Pow(Mathf.Max(0.1f, t), 0.6f);

        // 给掉落套倍率。本来有掉落的怪保底还留 1，免得倍率把它抹成 0。
        // 关卡定价（StageCatalog.Price）和局内发钱（BattleWorld.Make）都走这一份，
        // 各写一遍迟早会算出两个不一样的总收入。
        public static int Drop(int v, float mul) =>
            v <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(v * mul));

        // t 是关卡血量倍率，见 StageCatalog.HpOf。
        public static EnemyDef Get(EnemyId id, float t)
        {
            t = Mathf.Max(0.1f, t);
            return Base(id).Scale(t, DropMul(t));
        }

        // 图鉴口径的基础值：血、金、墨、甲都按「第一章那一只」写。
        // 关卡倍率一律由 Get 统一套，别在这张表里乘。
        public static EnemyDef Base(EnemyId id)
        {
            switch (id)
            {
                case EnemyId.Runner:
                    return new EnemyDef(id, 3.5f, 1.15f, 2, 0.1f, 0.22f, preferEmpty: true);
                case EnemyId.Shield:
                    return new EnemyDef(id, 10f, 0.42f, 4, 0.29f, 0.32f, shield: true);
                case EnemyId.Swarm:
                    return new EnemyDef(id, 3f, 0.7f, 1, 0.1f, 0.16f);
                case EnemyId.Strafer:
                    return new EnemyDef(id, 6.5f, 0.52f, 3, 0.24f, 0.24f, strafe: true);

                // 纯墨四只。一个特性都不给，只有体型、血、速的差别 —— 前六关就是
                // 要玩家先把「大的慢、小的快」这条最朴素的关系摸熟。之后每多一处
                // 颜色才有「又多了一条规矩」的意义；一开局就上带色的兵，
                // 「颜色越多越强」这条规则连对照物都没有。
                case EnemyId.Chubby:    // 胖墨 T0：矮胖，血厚步慢
                    return new EnemyDef(id, 8f, 0.40f, 2, 0.18f, 0.30f);
                case EnemyId.Tall:      // 高墨 T0：瘦高，步子大走得快
                    return new EnemyDef(id, 5f, 0.74f, 2, 0.18f, 0.24f);
                case EnemyId.Ball:      // 团墨 T0：圆球，最快也最脆
                    return new EnemyDef(id, 2.5f, 0.92f, 1, 0.1f, 0.18f);
                case EnemyId.BigHead:   // 大头墨 T0：头大身小，个头大好命中
                    return new EnemyDef(id, 6f, 0.50f, 2, 0.18f, 0.28f);

                // 束墨 T1芥黄：第一个带颜色的兵，只有一处配件、一个特性。
                // 走空列和快脚同特性，但一个快而脆、一个慢而肉，撞不到一起。
                // 它登场那关正好是第二排只开中间几格的时候 ——
                // 「中间堆满也挡不住外侧」这一课就靠它来上。
                case EnemyId.Belt:
                    return new EnemyDef(id, 12f, 0.46f, 3, 0.24f, 0.32f, preferEmpty: true);

                // 中段常规兵。每只专门惩罚一种偷懒配法，不是单纯的数值梯度。
                case EnemyId.Crawler:   // 爬子 T1黄：走得慢但推不动，惩罚只堆位移
                    return new EnemyDef(id, 7f, 0.34f, 3, 0.24f, 0.22f, noKnock: true);
                case EnemyId.Splitter:  // 双生 T2紫：死后裂成两只，惩罚无脑范围清场
                    return new EnemyDef(id, 9f, 0.55f, 3, 0.25f, 0.28f, splitCount: 2);
                case EnemyId.Sprinter:  // 惊风 T2橙：半血翻倍到 1.6，比快脚还快，惩罚打半血就换目标
                    return new EnemyDef(id, 6f, 0.80f, 4, 0.31f, 0.24f,
                        colorPhase: true, rageSpeed: 2f);
                case EnemyId.Mender:    // 补墨 T2绿：奶别人不奶自己，惩罚慢慢磨
                    return new EnemyDef(id, 8f, 0.46f, 5, 0.36f, 0.26f, healAura: 1.2f);
                case EnemyId.Bulwark:   // 厚甲 T2灰蓝：每发减 1，惩罚多段小伤害
                    return new EnemyDef(id, 22f, 0.26f, 6, 0.39f, 0.38f, armor: 1f);

                case EnemyId.Elite:     // 墨尊 T3：横移 + 半血狂化
                    return new EnemyDef(id, 28f, 0.38f, 8, 0.5f, 0.42f,
                        strafe: true, colorPhase: true);
                case EnemyId.Warden:    // 镇守 T3：挡一发 + 免定身，惩罚纯控制流
                    return new EnemyDef(id, 34f, 0.30f, 10, 0.6f, 0.44f,
                        shield: true, stunImmune: true);

                // 关底八只。colorPhase 只给真的会狂化的那几只 —— 半血染红这个
                // 反馈现在专门表示「它加速了」，不再是单纯的装饰。
                // 八只按章顺序当章底，基础血按出场顺序递增，乘上关卡倍率后
                // 章底 boss 血量逐章单调上升（check_stages.py 会核）。
                // 双首基础血特意压到别人一半 —— 它是两只，总量才对得上。
                case EnemyId.BossDrum:      // 一关 · 鼓面：挡一发 + 横移，考基础输出
                    return new EnemyDef(id, 70f, 0.22f, 14, 3.5f, 0.62f,
                        shield: true, strafe: true, boss: true);
                case EnemyId.BossInkbag:    // 二关 · 墨囊：死后裂成四只，考死时别松懈
                    return new EnemyDef(id, 85f, 0.26f, 16, 4.0f, 0.62f,
                        boss: true, splitCount: 4);
                case EnemyId.BossIron:      // 三关 · 铁桶：每发减 2 + 免击退，考重击
                    return new EnemyDef(id, 120f, 0.18f, 18, 4.5f, 0.66f,
                        boss: true, armor: 2f, noKnock: true);
                case EnemyId.BossTwin:      // 四关 · 双首：成对刷两只 + 半血加速，考火力分配
                    return new EnemyDef(id, 75f, 0.30f, 10, 2.5f, 0.58f,
                        boss: true, colorPhase: true, rageSpeed: 1.5f);
                case EnemyId.BossWarden:    // 五关 · 牢头：免定身 + 奶周围，考控制流
                    return new EnemyDef(id, 175f, 0.22f, 22, 5.5f, 0.66f,
                        boss: true, stunImmune: true, healAura: 1.8f);
                case EnemyId.BossThunder:   // 六关 · 奔雷：半血速度翻倍 + 走空列，考收尾速度
                    return new EnemyDef(id, 200f, 0.40f, 24, 6.0f, 0.56f,
                        boss: true, colorPhase: true, preferEmpty: true, rageSpeed: 2f);
                case EnemyId.BossMedic:     // 七关 · 墨医：强力群奶 + 挡一发，考先杀谁
                    return new EnemyDef(id, 215f, 0.24f, 26, 6.5f, 0.62f,
                        boss: true, shield: true, healAura: 3.5f);
                case EnemyId.BossKing:      // 八关 · 墨王：前面七关的机制各来一点
                    return new EnemyDef(id, 260f, 0.20f, 32, 8.0f, 0.72f,
                        boss: true, colorPhase: true, shield: true, armor: 1f,
                        stunImmune: true, rageSpeed: 1.5f);

                default:
                    return new EnemyDef(EnemyId.Walker, 4.5f, 0.62f, 2, 0.18f, 0.26f);
            }
        }
    }
}
