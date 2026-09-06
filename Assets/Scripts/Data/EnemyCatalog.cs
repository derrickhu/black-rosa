namespace InkLine
{
    public readonly struct EnemyDef
    {
        public readonly EnemyId Id;
        public readonly float Hp;
        public readonly float Speed;
        public readonly int Gold;
        public readonly float Radius;
        public readonly bool HasShield;
        public readonly bool Strafe;
        public readonly bool PreferEmpty;
        public readonly bool IsBoss;
        public readonly bool ColorsInPhase2;

        public EnemyDef(EnemyId id, float hp, float speed, int gold, float radius, bool shield, bool strafe, bool preferEmpty, bool boss, bool colorPhase)
        {
            Id = id;
            Hp = hp;
            Speed = speed;
            Gold = gold;
            Radius = radius;
            HasShield = shield;
            Strafe = strafe;
            PreferEmpty = preferEmpty;
            IsBoss = boss;
            ColorsInPhase2 = colorPhase;
        }
    }

    public static class EnemyCatalog
    {
        public static EnemyDef Get(EnemyId id, int stageIndex)
        {
            float t = 1f + stageIndex * 0.08f;
            switch (id)
            {
                case EnemyId.Runner:
                    return new EnemyDef(id, 3.5f * t, 1.15f, 2, 0.22f, false, false, true, false, false);
                case EnemyId.Shield:
                    return new EnemyDef(id, 10f * t, 0.42f, 4, 0.32f, true, false, false, false, false);
                case EnemyId.Swarm:
                    return new EnemyDef(id, 3f * t, 0.7f, 1, 0.16f, false, false, false, false, false);
                case EnemyId.Strafer:
                    return new EnemyDef(id, 6.5f * t, 0.52f, 3, 0.24f, false, true, false, false, false);
                case EnemyId.Elite:
                    return new EnemyDef(id, 28f * t, 0.38f, 8, 0.42f, false, true, false, false, true);
                case EnemyId.Boss:
                    return new EnemyDef(id, 70f * t, 0.22f, 14, 0.62f, true, true, false, true, true);
                default:
                    return new EnemyDef(EnemyId.Walker, 4.5f * t, 0.62f, 2, 0.26f, false, false, false, false, false);
            }
        }
    }
}
