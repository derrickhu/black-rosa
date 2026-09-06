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
        Heavy
    }

    public enum EnemyId
    {
        Walker,
        Runner,
        Shield,
        Swarm,
        Strafer,
        Elite,
        Boss
    }

    public readonly struct CardDef
    {
        public readonly CardId Id;
        public readonly string Name;
        public readonly string Desc;
        public readonly InkShape Shape;

        public CardDef(CardId id, string name, string desc, InkShape shape)
        {
            Id = id;
            Name = name;
            Desc = desc;
            Shape = shape;
        }
    }
}
