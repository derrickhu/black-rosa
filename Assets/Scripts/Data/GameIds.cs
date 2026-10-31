namespace InkLine
{
    public enum CardId
    {
        None = 0,
        Split,
        Fire,
        Ice,
        Track,
        Pierce,
        Explode,
        Accel,
        Heavy,
        Stun,
        Sec,
        Kill,
        Myriad,
        Arrow,
        Strike,
        Back,
        Link,
        Slash,
        Gold,
        Wood,
        Water,
        Earth,
        Wind,
        Thunder,
        Poison,
        Confuse,
        // 第二幕。存档里的图鉴按 id 记位，只能往后接。
        Break,
        Gather,
        Rebound,
        Qian,
        Jun,
        // 第十三、十四章。图鉴和字卡按下标记位，只能往后接。
        Shadow,
        Seal
    }

    public enum CardWake
    {
        Always,
        Charge,
        WordPart
    }

    public enum WordId
    {
        None,
        InstantKill,
        ArrowRain,
        Knockback,
        Cleave,
        Crush
    }

    public enum EnemyId
    {
        Walker,
        Runner,
        Shield,
        Swarm,
        Strafer,
        // 新增的常规兵。色阶（身上几处颜色）和强度是绑定的，见 docs/敌人设计.md。
        // 第一幕的兵排在 BossDrum 之前；第二幕起图鉴存档已经按 id 记了位，只能接在最后。
        // 纯墨的四只只靠体型和步态区分：矮胖 / 瘦高 / 圆球 / 大头。前六关全靠
        // 它们撑，带色的一只都不出 —— 否则「颜色越多越强」这条规则一开局就没了
        // 对照物，玩家看到的只是一堆花花绿绿的怪。
        Chubby,
        Tall,
        Ball,
        BigHead,
        Belt,
        Crawler,
        Splitter,
        Sprinter,
        Mender,
        Bulwark,
        Elite,
        Warden,
        // 关底：一关一只，各考这一关刚教的东西。八只的血量、机制、外观见
        // docs/敌人设计.md。原来八关共用一个 Boss、只靠关卡系数放大血量，
        // 打到第八关还是第一关那只。
        BossDrum,
        BossInkbag,
        BossIron,
        BossTwin,
        BossWarden,
        BossThunder,
        BossMedic,
        BossKing,
        // 第二幕：墨影、墨僧、影姬分出来的幻影，再是两只章底。
        Shade,
        Monk,
        Phantom,
        BossShade,
        BossBell
    }

    public static class EnemyIds
    {
        // 只给视图层和关卡表用：程序化兜底画法只拿得到 id，拿不到 EnemyDef。
        // 数值上谁是关底以 EnemyCatalog 里的 boss: 参数为准，这里要和那边一致。
        public static bool IsBoss(EnemyId id) =>
            (id >= EnemyId.BossDrum && id <= EnemyId.BossKing) || id >= EnemyId.BossShade;
    }

    public readonly struct CardDef
    {
        public readonly CardId Id;
        public readonly string Name;
        public readonly string Desc;
        public readonly InkShape Shape;
        public readonly CardWake Wake;
        public readonly WordId Word;
        public readonly int ChargeNeed;

        public CardDef(CardId id, string name, string desc, InkShape shape, CardWake wake = CardWake.Always, WordId word = WordId.None, int chargeNeed = 0)
        {
            Id = id;
            Name = name;
            Desc = desc;
            Shape = shape;
            Wake = wake;
            Word = word;
            ChargeNeed = chargeNeed;
        }
    }
}
