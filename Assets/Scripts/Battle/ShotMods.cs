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
        public float Amp;   // 烈度印章：冻 / 晕期间目标额外受伤的比例
    }

    // 子弹一路上收集到的所有改动。BulletActor 只留运动学，
    // 结算和表现都读这里，加字不用再往 BulletActor 上挂字段。
    public sealed class ShotMods
    {
        readonly int[] _star = new int[CardCatalog.IdCount];
        readonly List<StatusHit> _status = new List<StatusHit>();

        // 一发不带任何字的炮弹打多少。技能和成词伤害都按它的倍数写，
        // 这样锻造伤害线一升，它们跟着涨，新章节也不会失效。
        public const float DefaultBase = 1.8f;

        public float BaseDamage = DefaultBase;
        public float AddDamage;        // 金，加算池，最先算
        public float MulDamage = 1f;   // 重，乘算池，最后算
        // 局外成长（炮台伤害线、强攻）走这里，乘在加算和乘算都算完之后。
        // 乘在 BaseDamage 上的话，同列三个金字★3 那 +10.8 的加算完全吃不到，
        // 满级伤害线对一发总伤只剩 +19%。
        public float FinalMul = 1f;
        public float Decay = 1f;       // 道族衰减：分裂 / 穿透
        public float Leech;            // 木，结算总伤转吸血的比例
        public float LeechCap;

        public float PushColumn;       // 土 / 击退，纵向格数
        public float PushHold;         // 土的落地硬直
        public int PushLateral;        // 风，横向列数

        public float ExplodeR;
        public float ExplodeShare = 0.68f;
        public int CleaveLeft;
        public float CleaveDecay = 0.84f;

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
        // 下面几条只管表现：数值另有出处，开关告诉世界这一下要不要投秘卷特效。
        public int BurnLook;           // 灼烧火色：BurnLooks
        public bool Conduct;           // 导电
        public bool RamEarth;          // 夯土
        public bool ColdWind;          // 寒风
        public bool ThunderCut;        // 雷决
        public bool Collapse;          // 坍缩

        // 第二幕
        public float GatherR;          // 聚：拉多大一圈
        public float GatherPull;       // 聚：每只拉多远
        public float GatherHold;       // 聚 ★3：拉完定住多久
        public float Rebound;          // 返：折回那一程的伤害系数，0 表示不返
        public float Crush;            // 千钧：按目标当前生命追加的比例
        public bool Crumble;           // 崩山：破防目标无视甲
        public bool Whirl;             // 旋风：聚过来的整团贴到命中点那一列
        public bool Riposte;           // 回锋：折返时补一层穿，回程不衰减
        public float GhostShare;       // 影：影弹打几成。0 表示这发不再套影
        public bool GhostFire;         // 鬼火：影弹的灼烧不打折
        public bool FireWheel;         // 风火轮
        public bool Pin;               // 定海针

        // 印章
        public float Splash;           // 广域：没有半径的字命中时溅到身边，半径（格）
        // 烈度：被推开（土、击退）/ 吹开（风）/ 拉过来（聚）的怪之后两秒额外受伤
        public float MarkPush;
        public float MarkWind;
        public float MarkGather;
        public float LateralExtra;     // 位移：风多吹一列的几率
        public long SealOnce;          // 按字只乘一次的烈度，乘过的位记在这里

        public IReadOnlyList<StatusHit> Status => _status;

        // 这个字的「只乘一次」还没用过就记上并返回真。
        public bool FirstSeal(CardId id)
        {
            long bit = 1L << (int)id;
            if ((SealOnce & bit) != 0) return false;
            SealOnce |= bit;
            return true;
        }

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
                cur.Amp = Mathf.Max(cur.Amp, hit.Amp);
                _status[i] = cur;
                return;
            }
            _status.Add(hit);
        }

        public void ScaleTime(StatusKind kind, float mul)
        {
            for (int i = 0; i < _status.Count; i++)
            {
                if (_status[i].Kind != kind) continue;
                StatusHit cur = _status[i];
                cur.Time *= mul;
                _status[i] = cur;
                return;
            }
        }

        public float StatusTime(StatusKind kind)
        {
            for (int i = 0; i < _status.Count; i++)
                if (_status[i].Kind == kind) return _status[i].Time;
            return 0f;
        }

        // except 是 None 时全部打折。鬼火把灼烧留在外面。
        public void ScalePowers(float mul, StatusKind except)
        {
            for (int i = 0; i < _status.Count; i++)
            {
                if (except != StatusKind.None && _status[i].Kind == except) continue;
                StatusHit cur = _status[i];
                cur.Power *= mul;
                _status[i] = cur;
            }
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
            FinalMul = src.FinalMul;
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
            BurnLook = src.BurnLook;
            Conduct = src.Conduct;
            RamEarth = src.RamEarth;
            ColdWind = src.ColdWind;
            ThunderCut = src.ThunderCut;
            Collapse = src.Collapse;
            GatherR = src.GatherR;
            GatherPull = src.GatherPull;
            GatherHold = src.GatherHold;
            Rebound = src.Rebound;
            Crush = src.Crush;
            Crumble = src.Crumble;
            Whirl = src.Whirl;
            Riposte = src.Riposte;
            GhostShare = src.GhostShare;
            GhostFire = src.GhostFire;
            FireWheel = src.FireWheel;
            Pin = src.Pin;
            Splash = src.Splash;
            MarkPush = src.MarkPush;
            MarkWind = src.MarkWind;
            MarkGather = src.MarkGather;
            LateralExtra = src.LateralExtra;
            SealOnce = src.SealOnce;
        }
    }
}
