namespace InkLine
{
    public readonly struct EnemyDef
    {
        public readonly EnemyId Id;
        public readonly float Hp;
        public readonly float Speed;
        public readonly int Gold;
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

        public EnemyDef(EnemyId id, float hp, float speed, int gold, float radius,
            bool shield = false, bool strafe = false, bool preferEmpty = false,
            bool boss = false, bool colorPhase = false, float armor = 0f,
            float rageSpeed = 1f, bool stunImmune = false, bool noKnock = false,
            float healAura = 0f, int splitCount = 0)
        {
            Id = id;
            Hp = hp;
            Speed = speed;
            Gold = gold;
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
    }

    public static class EnemyCatalog
    {
        // 奶光环的半径，约一格半。再大就会隔着好几列偷偷奶到，玩家看不出因果。
        public const float HealRange = 1.4f;

        // 一只写在关卡表里的兵，实际刷几只。
        //
        // 关卡表里那些 1~3 只的批量摆在 6 列的场上太稀了，一炮打死一只、屏幕
        // 空半天，既没有压迫感也没有割草感。这里把轻甲杂兵按倍数铺开，
        // BattleWorld.Spawn 会把这一批的血量和赏金按同样的倍数分摊下去 ——
        // 一波的总血量和总收入不动，变的只是「几只厚的」换成「一群薄的」。
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

        public static EnemyDef Get(EnemyId id, int stageIndex)
        {
            float t = 1f + stageIndex * 0.08f;
            switch (id)
            {
                case EnemyId.Runner:
                    return new EnemyDef(id, 3.5f * t, 1.15f, 2, 0.22f, preferEmpty: true);
                case EnemyId.Shield:
                    return new EnemyDef(id, 10f * t, 0.42f, 4, 0.32f, shield: true);
                case EnemyId.Swarm:
                    return new EnemyDef(id, 3f * t, 0.7f, 1, 0.16f);
                case EnemyId.Strafer:
                    return new EnemyDef(id, 6.5f * t, 0.52f, 3, 0.24f, strafe: true);

                // 纯墨四只。一个特性都不给，只有体型、血、速的差别 —— 前六关就是
                // 要玩家先把「大的慢、小的快」这条最朴素的关系摸熟。之后每多一处
                // 颜色才有「又多了一条规矩」的意义；一开局就上带色的兵，
                // 「颜色越多越强」这条规则连对照物都没有。
                case EnemyId.Chubby:    // 胖墨 T0：矮胖，血厚步慢
                    return new EnemyDef(id, 8f * t, 0.40f, 2, 0.30f);
                case EnemyId.Tall:      // 高墨 T0：瘦高，步子大走得快
                    return new EnemyDef(id, 5f * t, 0.74f, 2, 0.24f);
                case EnemyId.Ball:      // 团墨 T0：圆球，最快也最脆
                    return new EnemyDef(id, 2.5f * t, 0.92f, 1, 0.18f);
                case EnemyId.BigHead:   // 大头墨 T0：头大身小，个头大好命中
                    return new EnemyDef(id, 6f * t, 0.50f, 2, 0.28f);

                // 束墨 T1芥黄：第一个带颜色的兵，只有一处配件、一个特性。
                // 走空列和快脚同特性，但一个快而脆、一个慢而肉，撞不到一起。
                // 它登场那关正好是第二排只开中间几格的时候 ——
                // 「中间堆满也挡不住外侧」这一课就靠它来上。
                case EnemyId.Belt:
                    return new EnemyDef(id, 12f * t, 0.46f, 3, 0.32f, preferEmpty: true);

                // 中段常规兵。每只专门惩罚一种偷懒配法，不是单纯的数值梯度。
                case EnemyId.Crawler:   // 爬子 T1黄：走得慢但推不动，惩罚只堆位移
                    return new EnemyDef(id, 7f * t, 0.34f, 3, 0.22f, noKnock: true);
                case EnemyId.Splitter:  // 双生 T2紫：死后裂成两只，惩罚无脑范围清场
                    return new EnemyDef(id, 9f * t, 0.55f, 3, 0.28f, splitCount: 2);
                case EnemyId.Sprinter:  // 惊风 T2橙：半血翻倍到 1.6，比快脚还快，惩罚打半血就换目标
                    return new EnemyDef(id, 6f * t, 0.80f, 4, 0.24f,
                        colorPhase: true, rageSpeed: 2f);
                case EnemyId.Mender:    // 补墨 T2绿：奶别人不奶自己，惩罚慢慢磨
                    return new EnemyDef(id, 8f * t, 0.46f, 5, 0.26f, healAura: 1.2f);
                case EnemyId.Bulwark:   // 厚甲 T2灰蓝：每发减 1，惩罚多段小伤害
                    return new EnemyDef(id, 22f * t, 0.26f, 6, 0.38f, armor: 1f);

                case EnemyId.Elite:     // 墨尊 T3：横移 + 半血狂化
                    return new EnemyDef(id, 28f * t, 0.38f, 8, 0.42f,
                        strafe: true, colorPhase: true);
                case EnemyId.Warden:    // 镇守 T3：挡一发 + 免定身，惩罚纯控制流
                    return new EnemyDef(id, 34f * t, 0.30f, 10, 0.44f,
                        shield: true, stunImmune: true);

                // 关底八只。colorPhase 只给真的会狂化的那几只 —— 半血染红这个
                // 反馈现在专门表示「它加速了」，不再是单纯的装饰。
                // 血量是按「算上关卡系数后、整个关底波的总血量单调上升」倒推的，
                // 不是照机制强度随手写的。乘上 t 之后每关关底总血约
                // 70 / 92 / 139 / 186 / 231 / 280 / 318 / 624，最后一关翻倍是章末该有的。
                // 双首基础血特意压到别人一半 —— 它是两只，总量才对得上。
                case EnemyId.BossDrum:      // 一关 · 鼓面：挡一发 + 横移，考基础输出
                    return new EnemyDef(id, 70f * t, 0.22f, 14, 0.62f,
                        shield: true, strafe: true, boss: true);
                case EnemyId.BossInkbag:    // 二关 · 墨囊：死后裂成四只，考死时别松懈
                    return new EnemyDef(id, 85f * t, 0.26f, 16, 0.62f,
                        boss: true, splitCount: 4);
                case EnemyId.BossIron:      // 三关 · 铁桶：每发减 2 + 免击退，考重击
                    return new EnemyDef(id, 120f * t, 0.18f, 18, 0.66f,
                        boss: true, armor: 2f, noKnock: true);
                case EnemyId.BossTwin:      // 四关 · 双首：成对刷两只 + 半血加速，考火力分配
                    return new EnemyDef(id, 75f * t, 0.30f, 10, 0.58f,
                        boss: true, colorPhase: true, rageSpeed: 1.5f);
                case EnemyId.BossWarden:    // 五关 · 牢头：免定身 + 奶周围，考控制流
                    return new EnemyDef(id, 175f * t, 0.22f, 22, 0.66f,
                        boss: true, stunImmune: true, healAura: 1.8f);
                case EnemyId.BossThunder:   // 六关 · 奔雷：半血速度翻倍 + 走空列，考收尾速度
                    return new EnemyDef(id, 200f * t, 0.40f, 24, 0.56f,
                        boss: true, colorPhase: true, preferEmpty: true, rageSpeed: 2f);
                case EnemyId.BossMedic:     // 七关 · 墨医：强力群奶 + 挡一发，考先杀谁
                    return new EnemyDef(id, 215f * t, 0.24f, 26, 0.62f,
                        boss: true, shield: true, healAura: 3.5f);
                case EnemyId.BossKing:      // 八关 · 墨王：前面七关的机制各来一点
                    return new EnemyDef(id, 260f * t, 0.20f, 32, 0.72f,
                        boss: true, colorPhase: true, shield: true, armor: 1f,
                        stunImmune: true, rageSpeed: 1.5f);

                default:
                    return new EnemyDef(EnemyId.Walker, 4.5f * t, 0.62f, 2, 0.26f);
            }
        }
    }
}
