using UnityEngine;

namespace InkLine
{
    public static class CardCatalog
    {
        static readonly CardDef[] All =
        {
            new CardDef(CardId.Fire, "火", "过格灼烧", InkShape.Flame),
            new CardDef(CardId.Ice, "冰", "过格减速", InkShape.Diamond),
            new CardDef(CardId.Split, "分", "穿过后裂成多发", InkShape.Triangle),
            new CardDef(CardId.Track, "瞄", "拐弯并拖紫尾", InkShape.Arc),
            new CardDef(CardId.Accel, "速", "穿过后飞得更快", InkShape.Arrow),
            new CardDef(CardId.Pierce, "穿", "蓄满的那发能穿透", InkShape.Bar, CardWake.Charge, WordId.None, 2),
            new CardDef(CardId.Explode, "炸", "蓄满的那发命中爆炸", InkShape.Burst, CardWake.Charge, WordId.None, 2),
            new CardDef(CardId.Heavy, "重", "蓄满的那发变粗必暴", InkShape.Square, CardWake.Charge, WordId.None, 2),
            new CardDef(CardId.Stun, "晕", "蓄满的那发命中短晕", InkShape.Ring, CardWake.Charge, WordId.None, 2),
            new CardDef(CardId.Sec, "秒", "和「杀」同列才醒", InkShape.Star, CardWake.WordPart, WordId.InstantKill, 3),
            new CardDef(CardId.Kill, "杀", "和「秒」同列才醒", InkShape.Star, CardWake.WordPart, WordId.InstantKill, 3),
            new CardDef(CardId.Myriad, "万", "和「箭」同列才醒", InkShape.Burst, CardWake.WordPart, WordId.ArrowRain, 4),
            new CardDef(CardId.Arrow, "箭", "和「万」同列才醒", InkShape.Bar, CardWake.WordPart, WordId.ArrowRain, 4),
            new CardDef(CardId.Strike, "击", "和「退」同列才醒", InkShape.Square, CardWake.WordPart, WordId.Knockback, 2),
            new CardDef(CardId.Back, "退", "和「击」同列才醒", InkShape.Square, CardWake.WordPart, WordId.Knockback, 2),
            new CardDef(CardId.Link, "连", "和「斩」同列才醒", InkShape.Arc, CardWake.WordPart, WordId.Cleave, 3),
            new CardDef(CardId.Slash, "斩", "和「连」同列才醒", InkShape.Bar, CardWake.WordPart, WordId.Cleave, 3),
            new CardDef(CardId.Gold, "金", "过格加伤，最先算", InkShape.Diamond),
            new CardDef(CardId.Wood, "木", "伤害转吸血，不加伤", InkShape.Arc),
            new CardDef(CardId.Water, "水", "命中处一圈减速", InkShape.Circle),
            new CardDef(CardId.Earth, "土", "把敌人往回推", InkShape.Square),
            new CardDef(CardId.Wind, "风", "把敌人吹到别列", InkShape.Arc),
            new CardDef(CardId.Thunder, "雷", "命中处一圈短晕", InkShape.Burst),
            new CardDef(CardId.Poison, "毒", "可叠层的长毒", InkShape.Triangle),
            new CardDef(CardId.Confuse, "惑", "让敌人打自己人", InkShape.Ring)
        };

        // 给按 CardId 索引的数组用，加字时跟着枚举一起长。
        public const int IdCount = (int)CardId.Confuse + 1;

        public static CardDef Get(CardId id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return All[0];
        }

        public static Color Accent(CardId id) => InkTheme.Accent(id);

        public static CardId Partner(CardId id)
        {
            switch (id)
            {
                case CardId.Sec: return CardId.Kill;
                case CardId.Kill: return CardId.Sec;
                case CardId.Myriad: return CardId.Arrow;
                case CardId.Arrow: return CardId.Myriad;
                case CardId.Strike: return CardId.Back;
                case CardId.Back: return CardId.Strike;
                case CardId.Link: return CardId.Slash;
                case CardId.Slash: return CardId.Link;
                default: return CardId.None;
            }
        }

        public static string WordName(WordId word)
        {
            switch (word)
            {
                case WordId.InstantKill: return "秒杀";
                case WordId.ArrowRain: return "万箭";
                case WordId.Knockback: return "击退";
                case WordId.Cleave: return "连斩";
                default: return "";
            }
        }

        // 以下数值一律读 GlyphTable，这里只留调用点习惯的名字。
        public static int WordChargeNeed(WordId word) => GlyphTable.Word(word).Charge;

        public static int SplitCount(int star) => GlyphTable.Get(CardId.Split).Count.IntAt(star);
        public static float BurnDps(int star) => GlyphTable.Get(CardId.Fire).Power.At(star);
        public static float SlowFactor(int star) => GlyphTable.Get(CardId.Ice).Power.At(star);
        public static float Homing(int star) => GlyphTable.Get(CardId.Track).Ballistic.At(star);
        public static int Pierce(int star) => GlyphTable.Get(CardId.Pierce).Count.IntAt(star);
        public static float ExplodeRadius(int star) => GlyphTable.Get(CardId.Explode).Radius.At(star);
        public static float AccelMul(int star) => GlyphTable.Get(CardId.Accel).Ballistic.At(star);
        public static float SizeMul(int star) => GlyphTable.Get(CardId.Heavy).Size.At(star);
        public static float HeavyMul(int star) => GlyphTable.Get(CardId.Heavy).MulDamage.At(star);
        public static bool FreezeOnHit(int star) => GlyphTable.FreezeOnHit(star);
        public static bool BurnPop(int star) => GlyphTable.BurnPop(star);
        public static float StunTime(int star) => GlyphTable.Get(CardId.Stun).Time.At(star);
        public static float ExecuteBoss(int star) => GlyphTable.Word(WordId.InstantKill).ExecuteBoss.At(star);
        public static int ArrowCount(int star) => GlyphTable.Word(WordId.ArrowRain).Count.IntAt(star);
        public static float ArrowDamage(int star) => GlyphTable.Word(WordId.ArrowRain).Damage.At(star);
        public static float Knock(int star) => GlyphTable.Word(WordId.Knockback).Move.At(star);
        public static int CleaveJumps(int star) => GlyphTable.Word(WordId.Cleave).Count.IntAt(star);
    }
}
