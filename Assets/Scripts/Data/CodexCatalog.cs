using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public enum CodexKind { Glyph, Word, Pair }

    // 图鉴一格。Glyph 的 Index 是 CardId，Word 是 WordId，Pair 是 SignaturePairs.All 的下标。
    public readonly struct CodexEntry
    {
        public readonly CodexKind Kind;
        public readonly int Index;

        public CodexEntry(CodexKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }
    }

    public enum CodexState { Unknown, Seen, Known }

    // 字谱 = 单字 + 词（词的两个字算一格）；秘卷 = 招牌两两。
    // 数值一律现读 GlyphTable，改平衡不用回来改文案。
    public static class CodexCatalog
    {
        static List<CodexEntry> _base;
        static CodexEntry[] _pairs;

        // 按关卡里第一次出现的先后排，没进任何关的字垫在最后。
        public static IReadOnlyList<CodexEntry> Base
        {
            get
            {
                if (_base != null) return _base;
                _base = new List<CodexEntry>();
                var added = new HashSet<long>();
                for (int s = 0; s < GameConstants.StageCount; s++)
                {
                    StageDef st = StageCatalog.Get(s);
                    if (st.Pool != null)
                        for (int i = 0; i < st.Pool.Length; i++) AddCard(st.Pool[i], added);
                    if (st.Preset != null)
                        for (int i = 0; i < st.Preset.Length; i++) AddCard(st.Preset[i].Id, added);
                }
                for (int i = 1; i < CardCatalog.IdCount; i++) AddCard((CardId)i, added);
                return _base;
            }
        }

        public static IReadOnlyList<CodexEntry> Pairs
        {
            get
            {
                if (_pairs != null) return _pairs;
                _pairs = new CodexEntry[SignaturePairs.All.Length];
                for (int i = 0; i < _pairs.Length; i++) _pairs[i] = new CodexEntry(CodexKind.Pair, i);
                return _pairs;
            }
        }

        static void AddCard(CardId id, HashSet<long> added)
        {
            if (id == CardId.None) return;
            CardDef def = CardCatalog.Get(id);
            var e = def.Wake == CardWake.WordPart
                ? new CodexEntry(CodexKind.Word, (int)def.Word)
                : new CodexEntry(CodexKind.Glyph, (int)id);
            long key = ((long)e.Kind << 32) | (uint)e.Index;
            if (added.Add(key)) _base.Add(e);
        }

        public static CardId[] PartsOf(WordId word)
        {
            switch (word)
            {
                case WordId.InstantKill: return new[] { CardId.Sec, CardId.Kill };
                case WordId.ArrowRain: return new[] { CardId.Myriad, CardId.Arrow };
                case WordId.Knockback: return new[] { CardId.Strike, CardId.Back };
                case WordId.Cleave: return new[] { CardId.Link, CardId.Slash };
                default: return new CardId[0];
            }
        }

        public static string Title(CodexEntry e) => Title(e.Kind, e.Index);

        public static string Title(CodexKind kind, int index)
        {
            switch (kind)
            {
                case CodexKind.Glyph: return CardCatalog.Get((CardId)index).Name;
                case CodexKind.Word: return CardCatalog.WordName((WordId)index);
                default: return SignaturePairs.All[index].Name;
            }
        }

        // ---- 状态 ----

        // 已解锁关卡的牌池 / 预置字里出现过，就算「见过」：只露字，不露作用。
        public static bool[] SeenCards(MetaProgress meta)
        {
            var seen = new bool[CardCatalog.IdCount];
            for (int s = 0; s < GameConstants.StageCount; s++)
            {
                if (!meta.Unlocked(s)) break;
                StageDef st = StageCatalog.Get(s);
                if (st.Pool != null)
                    for (int i = 0; i < st.Pool.Length; i++) seen[(int)st.Pool[i]] = true;
                if (st.Preset != null)
                    for (int i = 0; i < st.Preset.Length; i++) seen[(int)st.Preset[i].Id] = true;
            }
            return seen;
        }

        public static CodexState StateOf(MetaProgress meta, CodexEntry e, bool[] seen)
        {
            if (meta.CodexKnows(e.Kind, e.Index)) return CodexState.Known;
            if (e.Kind == CodexKind.Pair) return CodexState.Unknown;
            if (e.Kind == CodexKind.Glyph) return seen[e.Index] ? CodexState.Seen : CodexState.Unknown;
            CardId[] parts = PartsOf((WordId)e.Index);
            for (int i = 0; i < parts.Length; i++)
                if (seen[(int)parts[i]]) return CodexState.Seen;
            return CodexState.Unknown;
        }

        // 秘卷的两个字都认得了，给一句暗示，不点破是哪两个。
        public static bool PairHinted(MetaProgress meta, int pair)
        {
            SignaturePair p = SignaturePairs.All[pair];
            return meta.CodexKnows(CodexKind.Glyph, (int)p.A) && KnowsCard(meta, p.B);
        }

        static bool KnowsCard(MetaProgress meta, CardId id)
        {
            CardDef def = CardCatalog.Get(id);
            return def.Wake == CardWake.WordPart
                ? meta.CodexKnows(CodexKind.Word, (int)def.Word)
                : meta.CodexKnows(CodexKind.Glyph, (int)id);
        }

        public static int CountKnown(MetaProgress meta, IReadOnlyList<CodexEntry> list)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (meta.CodexKnows(list[i].Kind, list[i].Index)) n++;
            return n;
        }

        // ---- 文案 ----

        // 只告诉玩家生效频率。族别、常驻、蓄力这些是内部划分，不进界面。
        public static string WakeNote(CardId id)
        {
            CardDef def = CardCatalog.Get(id);
            return def.Wake == CardWake.Charge
                ? $"每 {def.ChargeNeed} 发经过才生效一次"
                : "炮弹每次经过都生效";
        }

        public static string WakeNote(WordId word) => $"每 {CardCatalog.WordChargeNeed(word)} 发经过才生效一次";

        public static string Lore(CardId id)
        {
            switch (id)
            {
                case CardId.Fire: return "炮弹穿过这一格就带上火，命中让敌人灼烧。火伤高、烧得短，不叠层。";
                case CardId.Ice: return "命中让敌人减速，只冻住被打中的那一个，控得比水狠。";
                case CardId.Water: return "命中处溅开一圈水，圈里的敌人一起减速。比冰弱，但能打一片。";
                case CardId.Poison: return "命中上毒。毒伤低、持续久，同一个敌人可以叠好几层。";
                case CardId.Thunder: return "命中处炸开一圈电，把主目标和身边几个敌人一起晕住。";
                case CardId.Stun: return "这一列蓄满的那一发，命中把敌人晕住，单体控得久。";
                case CardId.Confuse: return "命中让敌人神志不清，掉头去打身边的同伴。头目不吃迷惑，改为减速。";
                case CardId.Gold: return "给炮弹加一笔固定伤害。它最先算，后面的乘算都会把它一起放大。";
                case CardId.Heavy: return "这一列蓄满的那一发变得又粗又沉，伤害成倍。";
                case CardId.Wood: return "把一部分伤害化作生机，攒满一点就给城墙补一格血。它本身不加伤害。";
                case CardId.Earth: return "命中把敌人往回推，落地后还会僵住一小会儿。";
                case CardId.Wind: return "命中把敌人吹到别的列去，打乱它们的队形。";
                case CardId.Split: return "炮弹穿过后一分为几，扇形散开。每一发的伤害会打折。";
                case CardId.Track: return "炮弹会拐弯去追敌人，拖出一道紫色的尾巴。";
                case CardId.Accel: return "炮弹穿过后飞得更快，拖出一道绿色的尾巴。";
                case CardId.Pierce: return "这一列蓄满的那一发能穿过敌人继续往前飞，每穿一个伤害略减。";
                case CardId.Explode: return "这一列蓄满的那一发命中时爆开，爆圈里的敌人都吃一部分伤害。";
                default: return CardCatalog.Get(id).Desc;
            }
        }

        public static string StarLine(CardId id, int s)
        {
            GlyphDef g = GlyphTable.Get(id);
            switch (id)
            {
                case CardId.Fire:
                    return $"灼烧 {F(g.Power.At(s))}/秒，烧 {F(g.Time.At(s))} 秒"
                           + (GlyphTable.BurnPop(s) ? "；烧死时炸出火花" : "");
                case CardId.Ice:
                    return $"移速降到 {P(g.Power.At(s))}，{F(g.Time.At(s))} 秒"
                           + (GlyphTable.FreezeOnHit(s) ? $"；{P(GlyphTable.IceFreezeChance)} 几率冻住" : "");
                case CardId.Water:
                    return $"半径 {F(g.Radius.At(s))} 内移速降到 {P(g.Power.At(s))}，{F(g.Time.At(s))} 秒";
                case CardId.Poison:
                    return $"每层 {F(g.Power.At(s))}/秒，毒 {F(g.Time.At(s))} 秒，最多 {g.Stacks.IntAt(s)} 层";
                case CardId.Thunder:
                    return $"晕 {F(g.Time.At(s))} 秒，半径 {F(g.Radius.At(s))}，连带 {g.Count.IntAt(s)} 个";
                case CardId.Stun:
                    return $"晕 {F(g.Time.At(s))} 秒";
                case CardId.Confuse:
                    return $"迷惑 {F(g.Time.At(s))} 秒；头目改为减速 {P(g.Power.At(s))}";
                case CardId.Gold:
                    return $"伤害 +{F(g.AddDamage.At(s))}（基础 1.8）";
                case CardId.Heavy:
                    return $"伤害 ×{F(g.MulDamage.At(s))}，体型 ×{F(g.Size.At(s))}";
                case CardId.Wood:
                    return $"{P(g.Leech.At(s))} 伤害转生机，每波最多回 {g.LeechCap.IntAt(s)} 格血";
                case CardId.Earth:
                    return $"往回推 {F(g.Move.At(s))} 格，僵住 {F(g.Time.At(s))} 秒";
                case CardId.Wind:
                    return $"吹到 {g.Move.IntAt(s)} 列外" + (g.Time.At(s) > 0f ? $"；落地减速 {F(g.Time.At(s))} 秒" : "");
                case CardId.Split:
                    return $"分成 {g.Count.IntAt(s)} 发，每发伤害 ×{F(g.Decay.At(s))}";
                case CardId.Track:
                    return $"转向力 {F(g.Ballistic.At(s))}";
                case CardId.Accel:
                    return $"弹速 ×{F(g.Ballistic.At(s))}";
                case CardId.Pierce:
                    return $"穿透 {g.Count.IntAt(s)} 个，每穿一个伤害 ×{F(g.Decay.At(s))}";
                case CardId.Explode:
                    return $"爆圈半径 {F(g.Radius.At(s))}，圈内吃 {P(g.Decay.At(s))} 伤害";
                default:
                    return "";
            }
        }

        public static string WordLore(WordId word)
        {
            CardId[] p = PartsOf(word);
            string pair = p.Length == 2 ? $"「{CardCatalog.Get(p[0]).Name}」「{CardCatalog.Get(p[1]).Name}」放进同一列才成词。" : "";
            int need = CardCatalog.WordChargeNeed(word);
            switch (word)
            {
                case WordId.InstantKill: return pair + $"每 {need} 发蓄满一次，那一发命中时，血量低于斩杀线的敌人直接倒下。";
                case WordId.ArrowRain: return pair + $"每 {need} 发蓄满一次，在这一列落下一阵箭雨。";
                case WordId.Knockback: return pair + $"每 {need} 发蓄满一次，那一发把敌人远远击退。";
                case WordId.Cleave: return pair + $"每 {need} 发蓄满一次，那一发命中后接连斩向身边的敌人。";
                default: return pair;
            }
        }

        public static string WordStarLine(WordId word, int s)
        {
            WordDef w = GlyphTable.Word(word);
            switch (word)
            {
                case WordId.InstantKill: return $"普通敌人血量低于 {P(w.ExecuteHp.At(s))} 即斩；头目 {P(w.ExecuteBoss.At(s))}";
                case WordId.ArrowRain: return $"{w.Count.IntAt(s)} 支箭，每支 {F(w.Damage.At(s))} 伤害";
                case WordId.Knockback: return $"击退 {F(w.Move.At(s))} 格";
                case WordId.Cleave: return $"连斩 {w.Count.IntAt(s)} 次，每跳伤害 ×{F(w.Decay.At(s))}";
                default: return "";
            }
        }

        static string F(float v) => v.ToString(Mathf.Abs(v - Mathf.Round(v)) < 0.005f ? "0" : "0.##");
        static string P(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
