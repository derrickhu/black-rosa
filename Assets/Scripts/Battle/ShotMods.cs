using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 一次命中要施加的状态。同名在收集阶段就取强，
    // 硬控之间的互斥留到 BattleWorld.ApplyStatus 落地时再判。
    public struct StatusHit
    {
        public StatusKind Kind;
        public float Power;
        public float Time;
        public float Radius;
        public int Stacks;  // 可叠层数，目前只有毒用
        public int Chain;   // 范围内再连带几个，0 表示范围内全体
    }

    // 子弹一路上收集到的所有改动。BulletActor 只留运动学，
    // 结算和表现都读这里，加字不用再往 BulletActor 上挂字段。
    public sealed class ShotMods
    {
        readonly int[] _star = new int[CardCatalog.IdCount];
        readonly List<StatusHit> _status = new List<StatusHit>();

        public float BaseDamage = 2.4f;
        public float AddDamage;        // 金，加算池，最先算
        public float MulDamage = 1f;   // 重，乘算池，最后算
        public float Decay = 1f;       // 道族衰减：分裂 / 穿透
        public float Leech;            // 木，结算总伤转吸血的比例
        public float LeechCap;

        public float PushColumn;       // 土 / 击退，纵向格数
        public float PushHold;         // 土的落地硬直
        public int PushLateral;        // 风，横向列数

        public float ExplodeR;
        public float ExplodeShare = 0.6f;
        public int CleaveLeft;
        public float CleaveDecay = 0.75f;

        public bool InstantKill;
        public float ExecuteHp;        // 非头目直接死的血线
        public float ExecuteBoss;      // 头目按最大生命的比例

        public int Pierce;
        public float PierceDecay = 1f;  // 每穿过一个目标再乘一次
        public float Homing;
        public bool BurnPop;

        public WordId Word;            // 本发实际触发的词
        public WordId WordLook;        // 本列成了什么词，只管长相
        public Color Color = Color.black;

        // 招牌两两的开关，全部由 SignaturePairs.Tune 一次性写入
        public bool Tuned;
        public bool GoldRidesMul;      // 镇金：加算也吃乘算
        public bool Shatter;           // 霰雷：打冻结目标额外 +40%
        public bool BurnByMaxHp;       // 熔金：灼烧改按最大生命百分比
        public bool BlazeGround;       // 烈爆：爆圈内追加半程灼烧
        public float PoisonLeech;      // 蚀生：毒伤全额入吸血池，值是每波上限
        public float Shock;            // 焦雷：晕结束时补的那一下

        public IReadOnlyList<StatusHit> Status => _status;

        public int Star(CardId id) => _star[(int)id];
        public bool Has(CardId id) => _star[(int)id] > 0;

        public void Mark(CardId id, int star)
        {
            int i = (int)id;
            if (star > _star[i]) _star[i] = star;
        }

        public void AddStatus(StatusHit hit)
        {
            if (hit.Kind == StatusKind.None) return;
            for (int i = 0; i < _status.Count; i++)
            {
                if (_status[i].Kind != hit.Kind) continue;
                StatusHit cur = _status[i];
                // 缓是越小越强，其余越大越强
                cur.Power = hit.Kind == StatusKind.Slow
                    ? Mathf.Min(cur.Power, hit.Power)
                    : Mathf.Max(cur.Power, hit.Power);
                cur.Time = Mathf.Max(cur.Time, hit.Time);
                cur.Radius = Mathf.Max(cur.Radius, hit.Radius);
                cur.Stacks = Mathf.Max(cur.Stacks, hit.Stacks);
                cur.Chain = Mathf.Max(cur.Chain, hit.Chain);
                _status[i] = cur;
                return;
            }
            _status.Add(hit);
        }

        public void ScaleStatus(StatusKind kind, float mul)
        {
            for (int i = 0; i < _status.Count; i++)
            {
                if (_status[i].Kind != kind) continue;
                StatusHit cur = _status[i];
                cur.Power *= mul;
                _status[i] = cur;
                return;
            }
        }

        public void BumpStacks(StatusKind kind, int delta)
        {
            for (int i = 0; i < _status.Count; i++)
            {
                if (_status[i].Kind != kind) continue;
                StatusHit cur = _status[i];
                cur.Stacks += delta;
                _status[i] = cur;
                return;
            }
        }

        public void WidenStatus(StatusKind kind, int chain, float radiusMul)
        {
            for (int i = 0; i < _status.Count; i++)
            {
                if (_status[i].Kind != kind) continue;
                StatusHit cur = _status[i];
                if (cur.Chain > 0) cur.Chain += chain;
                cur.Radius *= radiusMul;
                _status[i] = cur;
                return;
            }
        }

        public bool HasStatus(StatusKind kind)
        {
            for (int i = 0; i < _status.Count; i++)
                if (_status[i].Kind == kind) return true;
            return false;
        }

        public float StatusPower(StatusKind kind)
        {
            for (int i = 0; i < _status.Count; i++)
                if (_status[i].Kind == kind) return _status[i].Power;
            return 0f;
        }

        public void CopyFrom(ShotMods src)
        {
            for (int i = 0; i < _star.Length; i++) _star[i] = src._star[i];
            _status.Clear();
            _status.AddRange(src._status);
            BaseDamage = src.BaseDamage;
            AddDamage = src.AddDamage;
            MulDamage = src.MulDamage;
            Decay = src.Decay;
            Leech = src.Leech;
            LeechCap = src.LeechCap;
            PushColumn = src.PushColumn;
            PushHold = src.PushHold;
            PushLateral = src.PushLateral;
            ExplodeR = src.ExplodeR;
            ExplodeShare = src.ExplodeShare;
            CleaveLeft = src.CleaveLeft;
            CleaveDecay = src.CleaveDecay;
            InstantKill = src.InstantKill;
            ExecuteHp = src.ExecuteHp;
            ExecuteBoss = src.ExecuteBoss;
            Pierce = src.Pierce;
            PierceDecay = src.PierceDecay;
            Homing = src.Homing;
            BurnPop = src.BurnPop;
            Word = src.Word;
            WordLook = src.WordLook;
            Color = src.Color;
            Tuned = src.Tuned;
            GoldRidesMul = src.GoldRidesMul;
            Shatter = src.Shatter;
            BurnByMaxHp = src.BurnByMaxHp;
            BlazeGround = src.BlazeGround;
            PoisonLeech = src.PoisonLeech;
            Shock = src.Shock;
        }
    }
}
