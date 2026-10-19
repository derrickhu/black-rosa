using UnityEngine;

namespace InkLine
{
    // 精英：一章九关全部三星之后，这章开困难版。关卡布局、波次、字池原样，
    // 血量 ×HardHp，三成五的杂兵带一条魔化词缀，关底一律带魔护体。
    // 章内要按顺序打：第一关随章开，后面每关要先过上一关的精英。
    public static partial class StageCatalog
    {
        public const float HardHp = 1.5f;
        public const float HardDemonChance = 0.35f;

        static StageDef[] _hard;

        public static StageDef HardStage(int index)
        {
            if (_hard == null) _hard = new StageDef[GameConstants.StageCount];
            index = Mathf.Clamp(index, 0, GameConstants.StageCount - 1);
            if (_hard[index] != null) return _hard[index];
            StageDef s = Get(index).Clone();
            s.Hard = true;
            s.Hp *= HardHp;
            s.DemonChance = HardDemonChance;
            s.Name = "精英 · " + s.Name;
            s.TeachDraft = false;
            s.TeachStar = false;
            s.Fresh = new CardId[0];
            Price(s);
            _hard[index] = s;
            return s;
        }
    }
}
