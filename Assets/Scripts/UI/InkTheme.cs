using UnityEngine;

namespace InkLine
{
    public static class InkTheme
    {
        public static readonly Color Paper = Hex("F4EFE4");
        public static readonly Color PaperInner = Hex("FCFCF6");
        public static readonly Color Stage = Hex("F4EFE4");
        public static readonly Color StageLift = Hex("F7F3EA");
        public static readonly Color Bone = Hex("E4DDD2");
        public static readonly Color BoneMid = Hex("B8B0A6");
        public static readonly Color Ink = Hex("1A1410");
        public static readonly Color Graphite = Hex("2E2E2E");
        public static readonly Color GraphiteMid = Hex("454545");
        public static readonly Color GraphiteHi = Hex("6A6A6A");
        public static readonly Color Ghost = Hex("EDE8DF");
        public static readonly Color Locked = Hex("C8C4BC");
        public static readonly Color CtaOff = Hex("8C8880");
        public static readonly Color Heart = Hex("E05446");
        public static readonly Color Fire = Hex("EB6114");
        public static readonly Color FireMid = Hex("F08428");
        public static readonly Color FireHi = Hex("F6C44A");
        public static readonly Color Ice = Hex("269EDB");
        public static readonly Color IceMid = Hex("5EC4E8");
        public static readonly Color IceHi = Hex("D8F2FA");
        public static readonly Color Accel = Hex("3CB86A");
        public static readonly Color AccelMid = Hex("7ED48A");
        public static readonly Color AccelHi = Hex("C8F0C4");
        public static readonly Color Track = Hex("9E47DB");
        public static readonly Color Explode = Hex("DB3847");
        public static readonly Color Word = Hex("E8B43A");
        public static readonly Color Gold = Hex("C9922A");
        public static readonly Color GoldHi = Hex("F0D27A");
        public static readonly Color Wood = Hex("4E7A33");
        public static readonly Color WoodHi = Hex("A7C97A");
        public static readonly Color Water = Hex("2A6FA8");
        public static readonly Color WaterHi = Hex("9FD4E8");
        public static readonly Color Earth = Hex("8A6134");
        public static readonly Color EarthHi = Hex("C9A472");
        public static readonly Color Wind = Hex("6F9C86");
        public static readonly Color WindHi = Hex("CFE3D6");
        public static readonly Color Thunder = Hex("5B4FD1");
        public static readonly Color ThunderHi = Hex("C6C0FF");
        public static readonly Color Poison = Hex("7A3FA0");
        public static readonly Color PoisonHi = Hex("A8D84B");
        public static readonly Color Confuse = Hex("B5479B");
        public static readonly Color ConfuseHi = Hex("EFB6E2");
        // ---- 界面层调色板（休闲卡片风）----
        // 只给 UiKit / 各屏用。上面那批墨色还在给战场表现和字牌用，别混。
        public static readonly Color Outline = Hex("3A2A20");     // 统一深描边
        public static readonly Color CardFace = Hex("FFFFFF");
        public static readonly Color CardDim = Hex("EFE9DF");     // 锁定态卡面
        public static readonly Color LineDim = Hex("ACA296");     // 锁定态描边
        public static readonly Color BgTop = Hex("FFEED2");
        public static readonly Color BgBot = Hex("EFD2C0");
        public static readonly Color TextDark = Hex("3A2A20");
        public static readonly Color TextMid = Hex("8E806E");
        public static readonly Color TextDim = Hex("A89A88");
        public static readonly Color Cta = Hex("F58A34");         // 主按钮
        public static readonly Color CtaDeep = Hex("C46018");     // 主按钮底唇
        public static readonly Color Plain = Hex("FFFFFF");       // 次按钮
        public static readonly Color PlainDeep = Hex("E2DACE");
        public static readonly Color CoinFace = Hex("F6BE3C");
        public static readonly Color CoinDeep = Hex("BE761E");
        public static readonly Color Teal = Hex("36B0B0");
        public static readonly Color Violet = Hex("8C6EDC");
        public static readonly Color Rose = Hex("E84E4E");
        public static readonly Color TabOn = Hex("FFE1BE");       // 底栏选中格的底色
        public static readonly Color Scrim = new Color(0.16f, 0.11f, 0.08f, 0.55f);

        public static readonly Color PlaceOk = new Color(0.22f, 0.62f, 0.32f, 0.32f);
        public static readonly Color PlaceUp = new Color(0.90f, 0.72f, 0.16f, 0.40f);
        public static readonly Color PlaceBad = new Color(0.70f, 0.16f, 0.16f, 0.22f);
        public static readonly Color Dim = new Color(0.10f, 0.09f, 0.08f, 0.42f);

        public static Color Accent(CardId id)
        {
            switch (id)
            {
                case CardId.Fire: return Fire;
                case CardId.Ice: return Ice;
                case CardId.Track: return Track;
                case CardId.Explode: return Explode;
                case CardId.Stun: return Graphite;
                case CardId.Gold: return Gold;
                case CardId.Wood: return Wood;
                case CardId.Water: return Water;
                case CardId.Earth: return Earth;
                case CardId.Wind: return Wind;
                case CardId.Thunder: return Thunder;
                case CardId.Poison: return Poison;
                case CardId.Confuse: return Confuse;
                case CardId.Sec:
                case CardId.Kill:
                case CardId.Myriad:
                case CardId.Arrow:
                case CardId.Strike:
                case CardId.Back:
                case CardId.Link:
                case CardId.Slash: return Word;
                default: return Graphite;
            }
        }

        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }
    }
}
