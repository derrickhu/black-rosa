using UnityEngine;

namespace InkLine
{
    public static class InkTheme
    {
        public static readonly Color Paper = Hex("F4EFE4");
        public static readonly Color PaperInner = Hex("FCFCF6");
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
        public static readonly Color Track = Hex("9E47DB");
        public static readonly Color Explode = Hex("DB3847");
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
