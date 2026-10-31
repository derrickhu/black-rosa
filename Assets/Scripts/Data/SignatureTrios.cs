namespace InkLine
{
    // 三字诀。和秘卷一样是隐藏配方，只是要同一列凑满三个字。
    // 词占两格，第三格再放那个单字也算。三个字照常生效，诀只多加一条。
    public struct SignatureTrio
    {
        public CardId A;
        public CardId B;
        public CardId C;
        public string Name;
        public string Note;
    }

    public static class SignatureTrios
    {
        // 图鉴按下标记位，只能往后接。
        public static readonly SignatureTrio[] All =
        {
            new SignatureTrio
            {
                A = CardId.Fire, B = CardId.Wind, C = CardId.Rebound, Name = "风火轮",
                Note = "折返卷出火轮，回程烧地，风再吹远一列"
            },
            new SignatureTrio
            {
                A = CardId.Myriad, B = CardId.Arrow, C = CardId.Pierce, Name = "穿云箭",
                Note = "万箭打中一只不散，继续射穿上面那只"
            },
            new SignatureTrio
            {
                A = CardId.Ice, B = CardId.Earth, C = CardId.Gather, Name = "定海针",
                Note = "聚过来的敌人被钉住，移速降到四成，持续 2 秒"
            }
        };

        public static bool Lit(ShotMods m, int index)
        {
            SignatureTrio t = All[index];
            return m.Has(t.A) && m.Has(t.B) && m.Has(t.C);
        }

        // 风火轮、定海针的字都是常驻，炮弹爬完这一列就收集齐了。
        // 穿云箭里的「穿」是蓄力字，落箭那条看格子，不看这发有没有蓄满。
        public static void Apply(ShotMods m)
        {
            if (m.Has(CardId.Fire) && m.Has(CardId.Wind) && m.Has(CardId.Rebound))
                m.FireWheel = true;
            if (m.Has(CardId.Ice) && m.Has(CardId.Earth) && m.Has(CardId.Gather))
                m.Pin = true;
        }
    }
}
