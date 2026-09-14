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
    }

    public static class ShotLook
    {
        // 体槽抢位顺序：词的大场面 > 域 > 元素 > 弹道形。
        static readonly CardId[] FormRank =
        {
            CardId.Explode, CardId.Fire, CardId.Ice, CardId.Water, CardId.Poison,
            CardId.Earth, CardId.Pierce
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
            v.Trail = Pick(m, TrailRank, v.Mixed, g => g.Trail);
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

            ShotFx pair = Signature(m);
            if (pair != ShotFx.None)
                v.Form = new SlotLook { Fx = pair, Tint = v.Mixed, Star = v.Form.Star };

            return v;
        }

        static SlotLook Pick(ShotMods m, CardId[] rank, Color mixed, System.Func<GlyphDef, ShotFx> slot)
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
                    Tint = Color.Lerp(CardCatalog.Accent(id), mixed, 0.35f),
                    Star = star
                };
            }
            return default;
        }

        // 招牌两两：只换体槽这一张图，结算骨架完全不动。
        public static ShotFx Signature(ShotMods m)
        {
            for (int i = 0; i < SignaturePairs.All.Length; i++)
            {
                SignaturePair p = SignaturePairs.All[i];
                if (m.Star(p.A) > 0 && m.Star(p.B) > 0) return p.Form;
            }
            return ShotFx.None;
        }

        // 专图没出之前退回主元素那张，画面不会开天窗。
        public static ShotFx Fallback(ShotFx fx)
        {
            switch (fx)
            {
                case ShotFx.FormFrostFire: return ShotFx.FormFire;
                case ShotFx.FormScorchBolt: return ShotFx.FormFire;
                case ShotFx.FormHailBolt: return ShotFx.FormIce;
                case ShotFx.FormBlightFire: return ShotFx.FormFire;
                case ShotFx.FormConduct: return ShotFx.FormWater;
                case ShotFx.FormMoltenGold: return ShotFx.FormFire;
                case ShotFx.FormWardGold: return ShotFx.FormExplode;
                case ShotFx.FormRotLife: return ShotFx.FormPoison;
                case ShotFx.FormRamEarth: return ShotFx.FormEarth;
                case ShotFx.FormColdWind: return ShotFx.FormIce;
                case ShotFx.FormBlaze: return ShotFx.FormExplode;
                case ShotFx.FormThunderCut: return ShotFx.FormKill;
                case ShotFx.FormWater: return ShotFx.FormIce;
                case ShotFx.FormPoison: return ShotFx.FormIce;
                case ShotFx.FormEarth: return ShotFx.FormKnock;
                default: return ShotFx.None;
            }
        }
    }
}
