using UnityEngine;

namespace InkLine
{
    // 把一发子弹带的所有字翻译成五个槽位。
    // 同槽只亮优先级最高的那一个形，颜色向全体元素的均色靠一点；
    // 异槽各亮各的，所以「叠了什么」一眼能看出来。
    public struct SlotLook
    {
        public ShotFx Fx;
        public Color Tint;
        public int Star;
        public CardId Card;     // 赢下这个槽的是哪个字。体槽用它算「还剩哪些元素没被画进图里」

        public bool On => Fx != ShotFx.None;
    }

    public struct ShotView
    {
        public SlotLook Form;
        public SlotLook Trail;
        public SlotLook Halo;
        public SlotLook Orbit;
        public SlotLook Bloom;
        public Color Mixed;
        public int Elements;

        // 「元素小卫星」：体槽那张图**没**表达到的元素，各挂一颗纯色小圆绕着弹体转。
        // 叠的元素越多挂得越多（最多 2 颗，因为元素最多 3 个），所以玩家能直接数出
        // 这发弹身上叠了几个字 —— 混色是均色，3 个元素混完反而看不出是 3 个。
        // 招牌两两把两个元素都画进图里了，所以只在有第 3 个元素时才挂。
        public Color MoteA;
        public Color MoteB;
        public int Motes;
    }

    public static class ShotLook
    {
        // 体槽抢位顺序：词的大场面 > 域 > 元素 > 弹道形。
        static readonly CardId[] FormRank =
        {
            CardId.Explode, CardId.Fire, CardId.Ice, CardId.Water, CardId.Poison,
            CardId.Earth, CardId.Thunder, CardId.Wind, CardId.Pierce
        };

        static readonly CardId[] TrailRank = { CardId.Accel, CardId.Track, CardId.Wind };
        static readonly CardId[] HaloRank = { CardId.Thunder, CardId.Stun, CardId.Gold };
        static readonly CardId[] OrbitRank = { CardId.Wind, CardId.Wood };
        static readonly CardId[] BloomRank = { CardId.Heavy, CardId.Gold };

        // 参与「颜色可混」的元素字。道族不带色，免得把弹体搅浑。
        static readonly CardId[] Elements =
        {
            CardId.Fire, CardId.Ice, CardId.Water, CardId.Poison, CardId.Earth,
            CardId.Wind, CardId.Thunder, CardId.Gold, CardId.Wood, CardId.Confuse
        };

        public static ShotView Resolve(ShotMods m)
        {
            var v = new ShotView();
            Color sum = Color.clear;
            int n = 0;
            for (int i = 0; i < Elements.Length; i++)
            {
                if (m.Star(Elements[i]) <= 0) continue;
                sum += CardCatalog.Accent(Elements[i]);
                n++;
            }
            v.Elements = n;
            v.Mixed = n > 0 ? sum / n : InkTheme.Ink;

            v.Form = Pick(m, FormRank, v.Mixed, g => g.Form);
            // 尾槽混得比别的槽少：尾巴的颜色是用来认「哪个道族字亮着」的
            // —— 速是绿、瞄是紫。按 0.35 往混色偏，配上火就成了橄榄绿，
            // 那点识别度正好被冲掉。
            v.Trail = Pick(m, TrailRank, v.Mixed, g => g.Trail, 0.12f);
            v.Halo = Pick(m, HaloRank, v.Mixed, g => g.Halo);
            v.Orbit = Pick(m, OrbitRank, v.Mixed, g => g.Orbit);
            v.Bloom = Pick(m, BloomRank, v.Mixed, g => g.Bloom);

            // 词组字压过元素占体槽：秒杀/连斩/击退/万箭都是一眼要认出来的大招。
            WordId word = m.Word != WordId.None ? m.Word : m.WordLook;
            if (word != WordId.None)
            {
                WordDef w = GlyphTable.Word(word);
                if (w.Form != ShotFx.None && (word == WordId.InstantKill || !v.Form.On))
                    v.Form = new SlotLook { Fx = w.Form, Tint = InkTheme.Word, Star = 1 };
            }

            // 招牌两两抢体槽，并记下它把哪两个元素画进图里了。
            CardId pairA = CardId.None, pairB = CardId.None;
            ShotFx pair = Signature(m, ref pairA, ref pairB);
            if (pair != ShotFx.None)
                v.Form = new SlotLook
                {
                    Fx = pair, Tint = v.Mixed, Star = v.Form.Star, Card = pairA
                };

            Motes(m, ref v, pairA, pairB);
            return v;
        }

        // 挑出「体槽图里没有的元素」当小卫星。体槽是单元素时排掉它自己，
        // 是招牌两两时排掉那一对，是词组 / 道族时一个都不排（那张图不带元素色）。
        static void Motes(ShotMods m, ref ShotView v, CardId pairA, CardId pairB)
        {
            for (int i = 0; i < Elements.Length && v.Motes < 2; i++)
            {
                CardId id = Elements[i];
                if (m.Star(id) <= 0) continue;
                // 招牌两两时 pairA/pairB 已经代表了体槽画进去的两个元素，
                // 此时 Form.Card 就是 pairA，不用再单独排一次。
                if (id == pairA || id == pairB || id == v.Form.Card) continue;
                if (v.Motes == 0) v.MoteA = CardCatalog.Accent(id);
                else v.MoteB = CardCatalog.Accent(id);
                v.Motes++;
            }
        }

        static SlotLook Pick(ShotMods m, CardId[] rank, Color mixed,
            System.Func<GlyphDef, ShotFx> slot, float blend = 0.35f)
        {
            for (int i = 0; i < rank.Length; i++)
            {
                CardId id = rank[i];
                int star = m.Star(id);
                if (star <= 0) continue;
                ShotFx fx = slot(GlyphTable.Get(id));
                if (fx == ShotFx.None) continue;
                return new SlotLook
                {
                    Fx = fx,
                    // 赢下槽位的字保留自己的色，再往混色偏一点，叠字才看得出来
                    Tint = Color.Lerp(CardCatalog.Accent(id), mixed, blend),
                    Star = star,
                    Card = id
                };
            }
            return default;
        }

        // 招牌两两：只换体槽这一张图，结算骨架完全不动。
        // 回报命中的那一对是哪两个字，好让小卫星知道哪两个元素已经画在图里了。
        public static ShotFx Signature(ShotMods m) 
        {
            CardId a = CardId.None, b = CardId.None;
            return Signature(m, ref a, ref b);
        }

        public static ShotFx Signature(ShotMods m, ref CardId a, ref CardId b)
        {
            for (int i = 0; i < SignaturePairs.All.Length; i++)
            {
                SignaturePair p = SignaturePairs.All[i];
                if (m.Star(p.A) <= 0 || m.Star(p.B) <= 0) continue;
                a = p.A;
                b = p.B;
                return p.Form;
            }
            return ShotFx.None;
        }

        // 体槽现在全员有图（元素 6 + 道族词组 5 + 招牌两两 12），所以这张退化表空了。
        // 原来它把 12 对招牌退回各自的主元素、把水/毒/土退回冰和击退，只为不开天窗
        // —— 代价是毒退成一颗蓝水滴这种明显的读错。留着这个入口，是因为以后新增
        // 招牌对时，图没画完的那几天还得靠它兜底。
        public static ShotFx Fallback(ShotFx fx) => ShotFx.None;
    }
}
