using UnityEngine;

namespace InkLine
{
    public static class CardCatalog
    {
        static readonly CardDef[] All =
        {
            new CardDef(CardId.Split, "分化", "穿过后裂成多发", InkShape.Triangle),
            new CardDef(CardId.Fire, "加火", "子弹变橙并灼烧", InkShape.Flame),
            new CardDef(CardId.Ice, "加冰", "子弹变青并减速", InkShape.Diamond),
            new CardDef(CardId.Track, "追踪", "拐弯并拖紫尾", InkShape.Arc),
            new CardDef(CardId.Pierce, "穿透", "穿过更多敌人", InkShape.Bar),
            new CardDef(CardId.Explode, "爆炸", "命中溅开墨点", InkShape.Burst),
            new CardDef(CardId.Accel, "加速", "穿过后飞得更快", InkShape.Arrow),
            new CardDef(CardId.Heavy, "重击", "更粗、会暴击", InkShape.Square)
        };

        public static CardDef Get(CardId id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return All[0];
        }

        public static Color Accent(CardId id) => InkTheme.Accent(id);

        public static int SplitCount(int star) => star <= 1 ? 2 : star == 2 ? 3 : 4;
        public static float BurnDps(int star) => star <= 1 ? 1.6f : star == 2 ? 2.8f : 4.4f;
        public static float SlowFactor(int star) => star <= 1 ? 0.72f : star == 2 ? 0.55f : 0.38f;
        public static float Homing(int star) => star <= 1 ? 2.4f : star == 2 ? 4.2f : 6.4f;
        public static int Pierce(int star) => star;
        public static float ExplodeRadius(int star) => star <= 1 ? 0.55f : star == 2 ? 0.82f : 1.12f;
        public static float AccelMul(int star) => star <= 1 ? 1.18f : star == 2 ? 1.34f : 1.5f;
        public static float CritChance(int star) => star <= 1 ? 0.15f : star == 2 ? 0.28f : 0.42f;
        public static float SizeMul(int star) => star <= 1 ? 1.15f : star == 2 ? 1.35f : 1.6f;
        public static bool FreezeOnHit(int star) => star >= 3;
        public static bool BurnPop(int star) => star >= 2;
    }
}
