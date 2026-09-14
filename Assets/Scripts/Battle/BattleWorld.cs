using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    public sealed class EnemyActor
    {
        public int Id;
        public EnemyId Type;
        public Vector2 Pos;
        public float Hp;
        public float MaxHp;
        public float Speed;
        public float Radius;
        public int Gold;
        public bool Shield;
        public bool Strafe;
        public bool PreferEmpty;
        public bool IsBoss;
        public bool ColorPhase;
        public float Armor;        // 每发固定减伤，保底仍掉 1
        public float RageSpeed;    // 半血后的速度倍率，1 表示不狂化
        public bool StunImmune;    // 冻/晕降级成缓
        public bool NoKnock;       // 退/风推不动
        public float HealAura;     // 每秒奶范围内的其他敌人
        public int SplitCount;     // 死后在原地裂出几只墨粒
        public bool Dead;
        // 成群刷出来时分到的血量份额。裂出来的墨粒照这个份额继承 ——
        // 不继承的话，一只被拆成两只的双生就会裂出双份满血的墨粒。
        public float HpShare = 1f;
        public float BurnDps;
        public float BurnTime;
        public float PoisonDps;
        public float PoisonTime;
        public int PoisonStacks;
        public float Slow = 1f;
        public float SlowTime;
        // 硬控同时只生效一个，输的那个降级成等时长的缓。
        public StatusKind Hard = StatusKind.None;
        public float HardTime;
        public float StunGuard;
        public float HoldTime;     // 土的落地硬直，不算硬控
        public float ConfuseCd;
        public float PoisonLeech;  // 蚀生：毒伤入吸血池的每波上限，0 表示不入
        public float Shock;        // 焦雷：晕结束时要补的那一下
        public float StrafeDir = 1f;
        public float HitFlash;
        // 挨打时往后一顿。纯表现，不进碰撞，由视图加到绘制位置上。
        public Vector2 Recoil;
        public bool Colored;

        public bool Held => HardTime > 0f && (Hard == StatusKind.Freeze || Hard == StatusKind.Stun);
        public bool Confused => HardTime > 0f && Hard == StatusKind.Confuse;
        public bool Frozen => HardTime > 0f && Hard == StatusKind.Freeze;
    }

    // 只留运动学，所有字带来的改动都在 Mods 里。
    public sealed class BulletActor
    {
        public int Id;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Radius = 0.11f;
        public int NextRow;
        public bool Dead;
        public bool Mother = true;
        public int ChargedMask;
        public int SplitMask;
        public readonly ShotMods Mods = new ShotMods();
        public readonly HashSet<int> HitIds = new HashSet<int>();
    }

    // 飘字的四种读法。数字和字要长得不一样，否则满屏都是同一种噪音。
    public enum PopKind { Damage, Crit, Word, Heal }

    public sealed class FloatText
    {
        public int Id;
        // 同一只怪身上的连续伤害并进同一个数往上滚。一炮打十下就飘十个 1，
        // 是这套战斗最吵也最看不懂的地方 —— 累加之后玩家读到的是「这一轮打掉多少」。
        public int OwnerId;
        public PopKind Kind;
        public Vector2 Pos;
        public Vector2 Vel;
        public string Text;
        public float Value;
        public Color Color;
        public float Scale = 1f;
        // 每次并入都重新弹一下，视图按它做那一瞬的放大。
        public float Punch = 1f;
        public float Life = 0.72f;
        public float MaxLife = 0.72f;
    }

    public enum DropKind { Gold, Ink }

    // 掉落物。弹出来、落地、躺一下，然后飞进顶栏的药丸里。
    // 到账放在飞到的那一刻，不在击杀那一刻 —— 让「打死」和「我变富了」
    // 是同一件看得见的事，而不是角落里一个数字自己跳了一下。
    public sealed class DropItem
    {
        public int Id;
        public DropKind Kind;
        public int Amount;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Ground;
        public float Rest;
        public float Fly;       // >0 表示已经起飞，1 到账
        public Vector2 From;
        public float Seed;
        public bool Dead;
    }

    // 一次死亡。视图拿去铺地上那摊墨和剪影爆白，世界这边不留状态。
    public struct DeathFx
    {
        public Vector2 Pos;
        public EnemyId Type;
        public float Radius;
        public bool Boss;
    }

    // 这一下伤害是哪来的。域和连斩不再走斩杀，也不再触发新的域。
    public enum HitSource { Direct, Cleave, Area, Arrow }

    // 命中三层的投递单：底层永远是墨溅，Kind 是唯一那个元素层，
    // Event 是斩杀 / 晕 / 位移这类事件层，被压掉的元素退化成 Dots 个色点。
    public struct FxBurst
    {
        public Vector2 Pos;
        public int Kind;
        public int Event;
        public Color Tint;
        public Color Dot0;
        public Color Dot1;
        public int Dots;
        public float Scale;
    }

    public sealed partial class BattleWorld
    {
        public bool Paused;
        public float HitStop;
        public int Gold;
        public int BaseHp = GameConstants.BaseHp;
        public int MaxBaseHp = GameConstants.BaseHp;
        public int RevivesUsed;
        public int DraftCount;
        public int OpenRows = 1;

        // 哪些格子能放字。粒度到格不到行 —— 新手关只开第一排中间两格，
        // 不是开一整排。OpenRows 留着给「按行往上扫」的那几处循环用
        // （没开的格子永远没有字，扫到最高开放行就够了）。
        public bool[,] Open = new bool[GameConstants.Columns, GameConstants.Rows];

        public bool IsOpen(int col, int row) =>
            col >= 0 && col < GameConstants.Columns &&
            row >= 0 && row < GameConstants.Rows && Open[col, row];

        public int EmitterCount = 2;
        public float RailX;
        public bool Victory;
        public bool Defeat;
        public static bool PreviewFill = false;
        public int PreviewPage;
        public float PreviewAge;
        public string PreviewLabel = "";
        float _previewClock;
        public bool BossSpawned;
        public bool BossKilled;
        public int WaveIndex;
        public float WaveTime;
        public float BattleTime;
        public int NextActorId = 1;
        public StageDef Stage;
        public CardId?[,] Grid;
        public int[,] Stars;
        public readonly List<EnemyActor> Enemies = new List<EnemyActor>();
        public readonly List<BulletActor> Bullets = new List<BulletActor>();
        public readonly List<FloatText> Floats = new List<FloatText>();
        public readonly List<FxBurst> Bursts = new List<FxBurst>();
        public readonly List<DropItem> Drops = new List<DropItem>();
        public readonly List<DeathFx> Deaths = new List<DeathFx>();

        // 顶栏两个药丸在世界里的位置，由 BattleView 每帧按画布算好塞进来。
        // 给一组兜底值，省得视图还没跑起来时掉落物往原点飞。
        public Vector2 GoldChip = new Vector2(-2.2f, 6.6f);
        public Vector2 InkChip = new Vector2(2.2f, 6.6f);
        // 到账脉冲：顶栏拿去弹一下。视图每帧自己衰减，世界只负责踢。
        public float GoldPop;
        public float InkPop;
        // 震屏请求。视图取走就清零，衰减归视图管 —— 顿帧期间世界是停的，
        // 震动不能跟着一起停，否则最该有反馈的那一下反而是静止的。
        public float ShakeWanted;
        public string LastReveal;
        public float RevealTime;
        public string Toast;
        public float ToastTime;

        // 局外升级只改这两个系数和开局条件，结算骨架一步都不动。
        float _damageMul = 1f;
        float _intervalMul = 1f;

        readonly float[] _fireCd = new float[GameConstants.MaxEmitters];
        readonly List<int> _deadBullets = new List<int>();
        float _leechPool;
        float _leechGiven;
        const int ChPierce = 0;
        const int ChExplode = 1;
        const int ChHeavy = 2;
        const int ChStun = 3;
        const int ChWord = 4;
        const int ChCount = 5;
        int[,] _charge;
        WordId[] _word;
        int[] _wordStar;
        int[] _pierceStar;
        int[] _explodeStar;
        int[] _heavyStar;
        int[] _stunStar;

        public int DraftCost => GameConstants.FirstDraftCost + DraftCount * GameConstants.DraftCostStep;
        public bool CanDraft => Gold >= DraftCost && !Victory && !Defeat;
        public int AliveEnemies
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Enemies.Count; i++) if (!Enemies[i].Dead) n++;
                return n;
            }
        }

        public void Begin(StageDef stage, ForgeStats forge, int[] equipped)
        {
            Stage = stage;
            OpenRows = Mathf.Clamp(stage.OpenRows, 1, GameConstants.Rows);
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++)
                Open[c, r] = StageCatalog.CellOpen(stage.OpenCells, c, r);
            Grid = new CardId?[GameConstants.Columns, GameConstants.Rows];
            Stars = new int[GameConstants.Columns, GameConstants.Rows];
            _charge = new int[GameConstants.Columns, ChCount];
            _word = new WordId[GameConstants.Columns];
            _wordStar = new int[GameConstants.Columns];
            _pierceStar = new int[GameConstants.Columns];
            _explodeStar = new int[GameConstants.Columns];
            _heavyStar = new int[GameConstants.Columns];
            _stunStar = new int[GameConstants.Columns];
            Gold = forge.StartGold;
            EmitterCount = Mathf.Clamp(forge.Emitters, 1, GameConstants.MaxEmitters);
            RailX = FieldLayout.ColumnX(Mathf.Max(0, (GameConstants.Columns - EmitterCount) / 2));
            _damageMul = Mathf.Max(0.1f, forge.DamageMul);
            _intervalMul = Mathf.Clamp(forge.IntervalMul, 0.3f, 2f);
            MaxBaseHp = Mathf.Max(1, forge.BaseHp);
            BaseHp = MaxBaseHp;
            BeginSpells(equipped);
            Enemies.Clear();
            Bullets.Clear();
            Floats.Clear();
            Bursts.Clear();
            Drops.Clear();
            Deaths.Clear();
            if (PreviewFill)
            {
                OpenRows = GameConstants.Rows;
                EmitterCount = GameConstants.MaxEmitters;
                PreviewPage = 0;
                PreviewAge = 0f;
                ApplyPreviewFill();
                RailX = FieldLayout.ColumnX(0);
            }
            for (int c = 0; c < GameConstants.Columns; c++) RefreshCol(c);
        }

        void WipePreviewGrid()
        {
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++)
            {
                Grid[c, r] = null;
                Stars[c, r] = 0;
            }
        }

        const int PreviewPages = 7;

        void ApplyPreviewFill()
        {
            OpenRows = GameConstants.Rows;
            // 预览台要摆满整盘，不受关卡的格子开放限制。
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++)
                Open[c, r] = true;
            WipePreviewGrid();
            switch (PreviewPage % PreviewPages)
            {
                case 0:
                    PutPreview(0, 0, CardId.Fire, 1);
                    PutPreview(1, 0, CardId.Ice, 1);
                    PutPreview(2, 0, CardId.Track, 1);
                    PutPreview(3, 0, CardId.Accel, 1);
                    PreviewLabel = "火 · 冰 · 瞄 · 速";
                    break;
                case 1:
                    PutPreview(0, 0, CardId.Split, 1);
                    PutPreview(1, 0, CardId.Pierce, 1);
                    PutPreview(2, 0, CardId.Explode, 1);
                    PutPreview(3, 0, CardId.Heavy, 1);
                    PreviewLabel = "分 · 穿 · 炸 · 重";
                    break;
                case 2:
                    PutPreview(0, 0, CardId.Stun, 1);
                    PutPreview(1, 0, CardId.Sec, 1);
                    PutPreview(1, 1, CardId.Kill, 1);
                    PutPreview(2, 0, CardId.Myriad, 1);
                    PutPreview(2, 1, CardId.Arrow, 1);
                    PutPreview(3, 0, CardId.Strike, 1);
                    PutPreview(3, 1, CardId.Back, 1);
                    PreviewLabel = "晕 · 秒杀 · 万箭 · 击退";
                    break;
                case 3:
                    PutPreview(0, 0, CardId.Link, 1);
                    PutPreview(0, 1, CardId.Slash, 1);
                    PutPreview(1, 0, CardId.Fire, 1);
                    PutPreview(1, 1, CardId.Ice, 1);
                    PutPreview(2, 0, CardId.Track, 1);
                    PutPreview(2, 1, CardId.Accel, 1);
                    PutPreview(3, 0, CardId.Explode, 1);
                    PutPreview(3, 1, CardId.Heavy, 1);
                    PreviewLabel = "连斩 · 火冰 · 瞄速 · 炸重";
                    break;
                // 后面三页是还没进关卡牌池的新元素和招牌两两，只在预览里看长相。
                case 4:
                    PutPreview(0, 0, CardId.Gold, 2);
                    PutPreview(1, 0, CardId.Wood, 2);
                    PutPreview(2, 0, CardId.Water, 2);
                    PutPreview(3, 0, CardId.Earth, 2);
                    PreviewLabel = "金 · 木 · 水 · 土";
                    break;
                case 5:
                    PutPreview(0, 0, CardId.Wind, 2);
                    PutPreview(1, 0, CardId.Thunder, 2);
                    PutPreview(2, 0, CardId.Poison, 2);
                    PutPreview(3, 0, CardId.Confuse, 2);
                    PreviewLabel = "风 · 雷 · 毒 · 惑";
                    break;
                default:
                    PutPreview(0, 0, CardId.Fire, 2);
                    PutPreview(0, 1, CardId.Ice, 2);
                    PutPreview(1, 0, CardId.Fire, 2);
                    PutPreview(1, 1, CardId.Thunder, 2);
                    PutPreview(2, 0, CardId.Gold, 2);
                    PutPreview(2, 1, CardId.Heavy, 2);
                    PutPreview(3, 0, CardId.Wood, 2);
                    PutPreview(3, 1, CardId.Poison, 2);
                    PreviewLabel = "霜火 · 焦雷 · 镇金 · 蚀生";
                    break;
            }
            Toast = PreviewLabel;
            ToastTime = 5.5f;
        }

        void TickPreview(float dt)
        {
            PreviewAge += dt;
            _previewClock += dt;
            if (_previewClock < 6.5f) return;
            _previewClock = 0f;
            PreviewAge = 0f;
            PreviewPage = (PreviewPage + 1) % PreviewPages;
            ApplyPreviewFill();
            for (int c = 0; c < GameConstants.Columns; c++) RefreshCol(c);
        }

        void PutPreview(int col, int row, CardId id, int star)
        {
            Grid[col, row] = id;
            Stars[col, row] = Mathf.Clamp(star, 1, GameConstants.MaxStar);
        }

        public void Tick(float dt)
        {
            if (Paused || Victory || Defeat) return;
            if (HitStop > 0f)
            {
                HitStop -= dt;
                // 顿帧停的是战斗，不是反馈。飘字和掉落照走，否则一次重击的
                // 0.12 秒里画面整个僵住，读起来像是卡了而不是「打得重」。
                TickFloats(dt);
                TickDrops(dt);
                return;
            }

            BattleTime += dt;
            if (RageTime > 0f) RageTime -= dt;
            if (PreviewFill) TickPreview(dt);
            TickWaves(dt);
            TickEmitters(dt);
            TickBullets(dt);
            TickEnemies(dt);
            TickFloats(dt);
            TickDrops(dt);
            if (ToastTime > 0f) ToastTime -= dt;
            if (RevealTime > 0f) RevealTime -= dt;
            CheckEnd();
        }

        void TickWaves(float dt)
        {
            if (WaveIndex >= Stage.Waves.Length) return;
            WaveDef wave = Stage.Waves[WaveIndex];
            WaveTime += dt;
            for (int i = 0; i < wave.Spawns.Length; i++)
            {
                SpawnSpec s = wave.Spawns[i];
                if (WaveTime - dt < s.Time && WaveTime >= s.Time)
                    Spawn(s);
            }
            if (WaveTime >= wave.Duration)
            {
                WaveIndex++;
                WaveTime = 0f;
                _leechGiven = 0f;
            }
        }

        void Spawn(SpawnSpec spec)
        {
            int mul = EnemyCatalog.Density(spec.Id);
            int count = spec.Count * mul;
            // 这一批的总血量和总赏金按关卡表原来那几只算，再摊到铺开的每一只头上。
            // 赏金保底 1 —— 掉不出东西的怪打起来没意义，宁可让最便宜那档小小超发。
            int purse = Mathf.Max(1, spec.Count * EnemyCatalog.Get(spec.Id, Stage.Index).Gold);
            for (int i = 0; i < count; i++)
            {
                int col = spec.Column;
                if (col < 0) col = UnityEngine.Random.Range(0, GameConstants.Columns);
                col = Mathf.Clamp(col + Fan(i), 0, GameConstants.Columns - 1);
                // 每三只往上退一排，进场是一队一队而不是叠在一个点上。
                var at = new Vector2(
                    FieldLayout.ColumnX(col) + UnityEngine.Random.Range(-0.1f, 0.1f),
                    GameConstants.SpawnY + i / 3 * 0.62f + UnityEngine.Random.Range(0f, 0.12f));
                int gold = Mathf.Max(1, purse / count + (i < purse % count ? 1 : 0));
                Enemies.Add(Make(spec.Id, at, 1f / count * spec.Count, gold));
            }
        }

        // 成群进场时第 i 只站哪一列：从中间往两边交替铺开。
        static int Fan(int i)
        {
            int k = (i % 5 + 1) / 2;
            return i % 5 % 2 == 1 ? -k : k;
        }

        // 特性拷贝只写这一遍。裂出来的墨粒也走这里 —— 否则哪天加了新特性，
        // 很容易只在 Spawn 里拷了，分裂出来的那批悄悄少一个字段。
        EnemyActor Make(EnemyId id, Vector2 at, float hpShare = 1f, int gold = -1)
        {
            EnemyDef def = EnemyCatalog.Get(id, Stage.Index);
            float hp = Mathf.Max(1f, def.Hp * hpShare);
            var e = new EnemyActor
            {
                Id = NextActorId++,
                Type = id,
                Pos = at,
                Hp = hp,
                MaxHp = hp,
                HpShare = hpShare,
                Speed = def.Speed,
                Radius = def.Radius,
                Gold = gold >= 0 ? gold : def.Gold,
                Shield = def.HasShield,
                Strafe = def.Strafe,
                PreferEmpty = def.PreferEmpty,
                IsBoss = def.IsBoss,
                ColorPhase = def.ColorsInPhase2,
                Armor = def.Armor,
                RageSpeed = def.RageSpeed,
                StunImmune = def.StunImmune,
                NoKnock = def.NoKnock,
                HealAura = def.HealAura,
                SplitCount = def.SplitCount
            };
            if (e.IsBoss) BossSpawned = true;
            return e;
        }

        // 裂：在死亡位置就地刷墨粒，不回顶部出生点。
        // 往 Enemies 里追加是安全的 —— 全工程对 Enemies 都是索引循环，没有 foreach。
        void SpawnSplit(EnemyActor from, int count)
        {
            float min = FieldLayout.ColumnX(0);
            float max = FieldLayout.ColumnX(GameConstants.Columns - 1);
            for (int i = 0; i < count; i++)
            {
                float ox = (i - (count - 1) * 0.5f) * 0.34f;
                Enemies.Add(Make(EnemyId.Swarm,
                    new Vector2(Mathf.Clamp(from.Pos.x + ox, min, max), from.Pos.y), from.HpShare));
            }
            ShowFloat(from.Pos + Vector2.up * 0.3f, "裂", InkTheme.Explode, 1.15f);
        }

        void TickEmitters(float dt)
        {
            float maxLeft = FieldLayout.ColumnX(0);
            float maxRight = FieldLayout.ColumnX(GameConstants.Columns - EmitterCount);
            RailX = Mathf.Clamp(RailX, maxLeft, maxRight);
            for (int i = 0; i < EmitterCount; i++)
            {
                int col = FieldLayout.ColumnAtX(RailX + i * GameConstants.CellWidth);
                float interval = GameConstants.BaseFireInterval * _intervalMul;
                _fireCd[i] -= dt;
                if (_fireCd[i] > 0f) continue;
                _fireCd[i] = interval;
                float x = RailX + i * GameConstants.CellWidth;
                FireBullet(new Vector2(x, GameConstants.EmitterY + 0.35f), Vector2.up);
            }
        }

        BulletActor FireBullet(Vector2 pos, Vector2 dir)
        {
            var b = new BulletActor
            {
                Id = NextActorId++,
                Pos = pos,
                Vel = dir.normalized * GameConstants.BulletSpeed,
                NextRow = 0
            };
            // 局外伤害升级和「强攻」都抬基础弹伤，不动加算/乘算两个池子，
            // 免得和镇金那条招牌抢规则。分裂弹靠 CopyFrom 继承。
            b.Mods.BaseDamage *= _damageMul;
            if (RageTime > 0f) b.Mods.BaseDamage *= 2f;
            Bullets.Add(b);
            return b;
        }

        void TickBullets(float dt)
        {
            _deadBullets.Clear();
            for (int i = 0; i < Bullets.Count; i++)
            {
                BulletActor b = Bullets[i];
                if (b.Dead) continue;
                if (b.Mods.Homing > 0f)
                    Steer(b, dt);
                b.Pos += b.Vel * dt;
                ApplyRows(b);
                if (b.Pos.y > GameConstants.WorldHalfHeight + 1f)
                    b.Dead = true;
                else
                    HitTest(b);
                if (b.Dead) _deadBullets.Add(i);
            }
            for (int i = _deadBullets.Count - 1; i >= 0; i--)
                Bullets.RemoveAt(_deadBullets[i]);
        }

        void Steer(BulletActor b, float dt)
        {
            EnemyActor best = null;
            float bestD = 999f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                float d = (e.Pos - b.Pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            if (best == null) return;
            Vector2 want = (best.Pos - b.Pos).normalized;
            b.Vel = Vector2.Lerp(b.Vel.normalized, want, b.Mods.Homing * dt).normalized * b.Vel.magnitude;
        }

        void ApplyRows(BulletActor b)
        {
            while (b.NextRow < OpenRows && b.Pos.y >= FieldLayout.RowApplyY(b.NextRow))
            {
                int col = FieldLayout.ColumnAtX(b.Pos.x);
                if (b.Mother && (_charge != null) && (b.ChargedMask & (1 << col)) == 0)
                    StampColumn(b, col);
                CardId? id = Grid[col, b.NextRow];
                if (id.HasValue)
                {
                    CardDef def = CardCatalog.Get(id.Value);
                    if (def.Wake == CardWake.Always)
                        ApplyMod(b, id.Value, Stars[col, b.NextRow]);
                }
                b.NextRow++;
            }
        }

        void StampColumn(BulletActor b, int col)
        {
            b.ChargedMask |= 1 << col;
            ShotMods m = b.Mods;
            // 先记下这列有什么，蓄力没满也要让炮弹带上对应的长相。
            if (_pierceStar[col] > 0) m.Mark(CardId.Pierce, _pierceStar[col]);
            if (_explodeStar[col] > 0) m.Mark(CardId.Explode, _explodeStar[col]);
            if (_heavyStar[col] > 0) m.Mark(CardId.Heavy, _heavyStar[col]);
            if (_stunStar[col] > 0) m.Mark(CardId.Stun, _stunStar[col]);
            if (_word[col] != WordId.None) m.WordLook = _word[col];

            if (_pierceStar[col] > 0 && PullCharge(col, ChPierce, 2))
            {
                GlyphDef g = GlyphTable.Get(CardId.Pierce);
                m.Pierce += g.Count.IntAt(_pierceStar[col]);
                m.PierceDecay = g.Decay.At(_pierceStar[col]);
            }
            if (_explodeStar[col] > 0 && PullCharge(col, ChExplode, 2))
            {
                GlyphDef g = GlyphTable.Get(CardId.Explode);
                m.ExplodeR = Mathf.Max(m.ExplodeR, g.Radius.At(_explodeStar[col]));
                m.ExplodeShare = g.Decay.At(_explodeStar[col]);
            }
            if (_heavyStar[col] > 0 && PullCharge(col, ChHeavy, 2))
            {
                // 只放大视觉，碰撞半径不动 —— 之前连 Radius 一起乘，炮弹会撑满格。
                m.MulDamage *= GlyphTable.Get(CardId.Heavy).MulDamage.At(_heavyStar[col]);
            }
            if (_stunStar[col] > 0 && PullCharge(col, ChStun, 2))
            {
                m.AddStatus(new StatusHit
                {
                    Kind = StatusKind.Stun,
                    Time = GlyphTable.Get(CardId.Stun).Time.At(_stunStar[col])
                });
            }
            if (_word[col] != WordId.None && PullCharge(col, ChWord, CardCatalog.WordChargeNeed(_word[col])))
            {
                WordId id = _word[col];
                WordDef w = GlyphTable.Word(id);
                int ws = Mathf.Max(1, _wordStar[col]);
                m.Word = id;
                switch (id)
                {
                    case WordId.InstantKill:
                        m.InstantKill = true;
                        m.ExecuteHp = Mathf.Max(m.ExecuteHp, w.ExecuteHp.At(ws));
                        m.ExecuteBoss = Mathf.Max(m.ExecuteBoss, w.ExecuteBoss.At(ws));
                        break;
                    case WordId.ArrowRain:
                        RainArrows(col, ws, b);
                        break;
                    case WordId.Knockback:
                        m.PushColumn = Mathf.Max(m.PushColumn, w.Move.At(ws));
                        break;
                    case WordId.Cleave:
                        m.CleaveLeft = Mathf.Max(m.CleaveLeft, w.Count.IntAt(ws));
                        m.CleaveDecay = w.Decay.At(ws);
                        break;
                }
            }
        }

        bool PullCharge(int col, int kind, int need)
        {
            if (PreviewFill) return true;
            if (need <= 0) return true;
            if (_charge[col, kind] >= need)
            {
                _charge[col, kind] = 0;
                return true;
            }
            _charge[col, kind]++;
            return false;
        }

        void RainArrows(int col, int star, BulletActor src)
        {
            WordDef w = GlyphTable.Word(WordId.ArrowRain);
            float dmg = w.Damage.At(star);
            int cap = w.Count.IntAt(star);
            int n = 0;
            float x = FieldLayout.ColumnX(col);
            for (int i = 0; i < Enemies.Count && n < cap; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if (Mathf.Abs(e.Pos.x - x) > GameConstants.CellWidth * 0.55f) continue;
                n++;
                ResolveHit(e, src.Mods, src, HitSource.Arrow, dmg);
            }
            if (n > 0) ShowFloat(new Vector2(x, 2.2f), "箭", InkTheme.Fire, 1.1f);
        }

        // 常驻字过格时生效。全部按族查表，加字不用改这里。
        void ApplyMod(BulletActor b, CardId id, int star)
        {
            GlyphDef g = GlyphTable.Get(id);
            ShotMods m = b.Mods;
            m.Mark(id, star);

            switch (g.Family)
            {
                case GlyphFamily.Hurt:
                    m.AddDamage += g.AddDamage.At(star);
                    if (g.MulDamage.Any) m.MulDamage *= g.MulDamage.At(star);
                    if (g.Leech.Any)
                    {
                        m.Leech += g.Leech.At(star);
                        m.LeechCap = Mathf.Max(m.LeechCap, g.LeechCap.At(star));
                    }
                    break;
                case GlyphFamily.Status:
                    m.AddStatus(new StatusHit
                    {
                        Kind = g.Status,
                        Power = g.Power.At(star),
                        Time = g.Time.At(star),
                        Radius = g.Radius.At(star),
                        Stacks = g.Stacks.IntAt(star),
                        Chain = g.Count.IntAt(star)
                    });
                    // 冰 ★3 才有几率把缓升成冻，掷点留到命中时
                    if (id == CardId.Fire) m.BurnPop = m.BurnPop || GlyphTable.BurnPop(star);
                    break;
                case GlyphFamily.Move:
                    if (g.Axis == MoveAxis.Column)
                    {
                        m.PushColumn = Mathf.Max(m.PushColumn, g.Move.At(star));
                        m.PushHold = Mathf.Max(m.PushHold, g.Time.At(star));
                    }
                    else if (g.Axis == MoveAxis.Lateral)
                    {
                        m.PushLateral = Mathf.Max(m.PushLateral, g.Move.IntAt(star));
                        // 风 ★3 带一小段缓
                        if (g.Time.At(star) > 0f)
                            m.AddStatus(new StatusHit
                            {
                                Kind = StatusKind.Slow,
                                Power = 0.8f,
                                Time = g.Time.At(star)
                            });
                    }
                    break;
                case GlyphFamily.Ballistic:
                    if (id == CardId.Track) m.Homing = Mathf.Max(m.Homing, g.Ballistic.At(star));
                    else if (id == CardId.Accel)
                        b.Vel = b.Vel.normalized * (b.Vel.magnitude * g.Ballistic.At(star));
                    else if (id == CardId.Split)
                    {
                        int col = FieldLayout.ColumnAtX(b.Pos.x);
                        if ((b.SplitMask & (1 << col)) == 0)
                        {
                            b.SplitMask |= 1 << col;
                            Split(b, star);
                        }
                    }
                    break;
            }
            PaintBodyColor(m);
        }

        // 弹体主色取所有元素字的均值，槽位表现另算。
        static void PaintBodyColor(ShotMods m)
        {
            Color sum = Color.clear;
            int n = 0;
            for (int i = 0; i < CardCatalog.IdCount; i++)
            {
                var id = (CardId)i;
                if (m.Star(id) <= 0) continue;
                Color a = CardCatalog.Accent(id);
                float sat = Mathf.Max(a.r, Mathf.Max(a.g, a.b)) - Mathf.Min(a.r, Mathf.Min(a.g, a.b));
                if (sat < 0.12f) continue;
                sum += a;
                n++;
            }
            if (n > 0) m.Color = sum / n;
        }

        void Split(BulletActor source, int star)
        {
            GlyphDef g = GlyphTable.Get(CardId.Split);
            int extra = g.Count.IntAt(star) - 1;
            float spread = 13f;
            for (int i = 0; i < extra; i++)
            {
                float side = extra == 1 ? (i == 0 ? -1f : 1f) : (i - (extra - 1) * 0.5f);
                float ang = side * spread;
                Vector2 dir = Quaternion.Euler(0f, 0f, ang) * Vector2.up;
                BulletActor clone = FireBullet(source.Pos, dir);
                clone.Mods.CopyFrom(source.Mods);
                clone.Mods.Decay *= g.Decay.At(star);
                clone.Radius = source.Radius;
                clone.NextRow = source.NextRow + 1;
                clone.Vel = dir * source.Vel.magnitude;
                clone.Mother = false;
                clone.ChargedMask = source.ChargedMask;
                clone.SplitMask = source.SplitMask;
            }
        }

        void HitTest(BulletActor b)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || b.HitIds.Contains(e.Id)) continue;
                if ((e.Pos - b.Pos).sqrMagnitude > (e.Radius + b.Radius) * (e.Radius + b.Radius)) continue;
                b.HitIds.Add(e.Id);
                ResolveHit(e, b.Mods, b, HitSource.Direct);
                while (b.Mods.CleaveLeft > 0) Cleave(b);
                if (b.Mods.ExplodeR > 0f) Explode(b.Pos, b.Mods, b);
                if (b.Mods.Pierce > 0) { b.Mods.Pierce--; b.Mods.Decay *= b.Mods.PierceDecay; }
                else
                {
                    b.Dead = true;
                    break;
                }
            }
        }

        void Cleave(BulletActor b)
        {
            EnemyActor best = null;
            float bestD = 2.4f * 2.4f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || b.HitIds.Contains(e.Id)) continue;
                float d = (e.Pos - b.Pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            // 找不到下一个就把跳数清零，否则 HitTest 的 while 会空转
            if (best == null)
            {
                b.Mods.CleaveLeft = 0;
                return;
            }
            b.Mods.CleaveLeft--;
            b.HitIds.Add(best.Id);
            ResolveHit(best, b.Mods, b, HitSource.Cleave);
            ShowFloat(best.Pos + Vector2.up * 0.1f, "斩", InkTheme.Ink, 0.88f);
        }

        void Explode(Vector2 pos, ShotMods m, BulletActor src)
        {
            float r = m.ExplodeR;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if ((e.Pos - pos).sqrMagnitude > r * r) continue;
                if (!src.HitIds.Contains(e.Id))
                {
                    src.HitIds.Add(e.Id);
                    ResolveHit(e, m, src, HitSource.Area);
                }
                // 烈爆：爆圈里再补一层半程灼烧，等效于地上留了 1.5s 的火
                if (!m.BlazeGround) continue;
                e.BurnDps = Mathf.Max(e.BurnDps, m.StatusPower(StatusKind.Burn) * 0.5f);
                e.BurnTime = Mathf.Max(e.BurnTime, 1.5f);
            }
        }

        // 十步结算。所有伤害、状态、位移都从这里过，叠加规则只有这一份。
        // 第 9 步（域触发额外结算点）留在 HitTest，免得域里再套域。
        void ResolveHit(EnemyActor e, ShotMods m, BulletActor src, HitSource from, float flat = -1f)
        {
            // 招牌两两只在第一次命中前调一次数值，之后骨架照常走
            SignaturePairs.Tune(m);
            // 斩杀只在本体命中时生效，域伤害不再走斩杀
            bool execute = m.InstantKill && from == HitSource.Direct;

            // 0 盾：普通弹只破盾；斩杀破盾后继续
            if (e.Shield)
            {
                e.Shield = false;
                e.HitFlash = 0.16f;
                PulseHitStop(0.06f);
                ShowFloat(e.Pos + Vector2.up * 0.18f, "挡", InkTheme.Ink, 0.92f);
                if (!execute) return;
            }

            // 2 基础弹伤 × 道族衰减
            float dmg = flat >= 0f ? flat : m.BaseDamage * m.Decay;
            if (from == HitSource.Cleave) dmg *= m.CleaveDecay;
            else if (from == HitSource.Area) dmg *= m.ExplodeShare;
            // 3 加算池先全部求和，4 乘算池最后连乘。
            // 加算默认不吃乘算 —— 否则「镇金」那条招牌就没意义了。
            if (m.GoldRidesMul) dmg = (dmg + m.AddDamage) * m.MulDamage;
            else dmg = dmg * m.MulDamage + m.AddDamage;
            // 霰雷：打在已冻结的目标上算碎冰
            if (m.Shatter && e.Frozen) dmg *= 1.4f;

            // 4.5 甲：每发固定减伤，放在乘算之后、斩杀之前。
            // 放这里有两个原因：一是减伤要吃满所有增伤，不然重击流被削两次；
            // 二是斩杀靠 Max 抬伤害，排在甲后面才不会被甲削掉。
            if (e.Armor > 0f && dmg > 0f) dmg = Mathf.Max(1f, dmg - e.Armor);

            // 1 斩杀优先级最高：只会把伤害抬上去，不会被别的字压低
            if (execute)
            {
                if (e.IsBoss) dmg = Mathf.Max(dmg, e.MaxHp * m.ExecuteBoss);
                else if (e.Hp <= e.MaxHp * m.ExecuteHp) dmg = Mathf.Max(dmg, e.Hp);
                ShowFloat(e.Pos + Vector2.up * 0.32f, e.IsBoss ? "创" : "斩", InkTheme.Heart, 1.35f);
            }

            // 5 木：按真正吃进去的量记吸血池
            if (m.Leech > 0f) PourLeech(Mathf.Min(dmg, e.Hp) * m.Leech, m.LeechCap);

            // 6 扣血
            e.Hp -= dmg;
            bool heavy = m.Has(CardId.Heavy);
            e.HitFlash = heavy || execute ? 0.2f : 0.14f;

            // 7 状族
            ApplyStatusSet(e, m);

            // 8 位族
            ApplyPush(e, m);

            Bursts.Add(BuildBurst(e.Pos, m, execute));
            PulseHitStop(HitStopOf(m, execute));
            // 挨打往炮弹来的方向顿一下。本体命中才推，域伤害不推 ——
            // 一圈爆炸把周围的怪全推歪，看着像是被吹散而不是被炸到。
            if (from == HitSource.Direct && src != null)
                Recoil(e, src.Vel, heavy || execute ? 0.3f : 0.16f);
            // 普通命中不震屏。每秒六到二十下都震，屏幕就一直在抖，
            // 真正该有分量的那几下反而分不出来了。
            AddShake(execute ? 0.34f : heavy || m.ExplodeR > 0.01f ? 0.18f : 0f);
            Color ink = m.Color.r + m.Color.g + m.Color.b < 0.12f ? InkTheme.Ink : m.Color;
            ShowDamage(e, dmg, execute ? InkTheme.Heart : ink, heavy ? 1.42f : 1f, heavy || execute);
            if (e.Hp <= 0f) Kill(e, m);
        }

        void ApplyStatusSet(EnemyActor e, ShotMods m)
        {
            var list = m.Status;
            for (int i = 0; i < list.Count; i++)
            {
                StatusHit s = list[i];
                ApplyStatus(e, s, m);
                if (s.Radius > 0f) SpreadStatus(e, s, m);
            }
        }

        // 水是范围内全体，雷是主目标外再连带 Chain 个。
        void SpreadStatus(EnemyActor hit, StatusHit s, ShotMods m)
        {
            int cap = s.Chain > 0 ? s.Chain : int.MaxValue;
            int n = 0;
            for (int i = 0; i < Enemies.Count && n < cap; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || e.Id == hit.Id) continue;
                if ((e.Pos - hit.Pos).sqrMagnitude > s.Radius * s.Radius) continue;
                ApplyStatus(e, s, m);
                n++;
            }
        }

        void ApplyStatus(EnemyActor e, StatusHit s, ShotMods m)
        {
            StatusKind kind = s.Kind;
            float time = s.Time;
            // 冰 ★3 有几率把「缓」升级成「冻」
            if (kind == StatusKind.Slow
                && GlyphTable.FreezeOnHit(m.Star(CardId.Ice))
                && UnityEngine.Random.value < GlyphTable.IceFreezeChance)
            {
                kind = StatusKind.Freeze;
                time = GlyphTable.IceFreezeTime;
            }
            if (GlyphTable.IsHard(kind))
            {
                ApplyHard(e, kind, time, s.Power);
                // 焦雷：这一下的账记在敌人身上，晕结束时再结
                if (kind == StatusKind.Stun && m.Shock > 0f)
                    e.Shock = Mathf.Max(e.Shock, m.Shock);
                return;
            }
            switch (kind)
            {
                case StatusKind.Burn:
                    // 熔金：灼烧改成按目标最大生命计，对高血量目标才有意义
                    float dps = m.BurnByMaxHp ? Mathf.Max(s.Power, e.MaxHp * 0.01f) : s.Power;
                    e.BurnDps = Mathf.Max(e.BurnDps, dps);
                    e.BurnTime = Mathf.Max(e.BurnTime, time);
                    break;
                case StatusKind.Poison:
                    // 毒是唯一可叠层的，层数吃字的上限
                    e.PoisonStacks = Mathf.Min(Mathf.Max(1, s.Stacks), e.PoisonStacks + 1);
                    e.PoisonDps = Mathf.Max(e.PoisonDps, s.Power);
                    e.PoisonTime = Mathf.Max(e.PoisonTime, time);
                    e.PoisonLeech = Mathf.Max(e.PoisonLeech, m.PoisonLeech);
                    break;
                case StatusKind.Slow:
                    e.Slow = Mathf.Min(e.Slow, s.Power);
                    e.SlowTime = Mathf.Max(e.SlowTime, time);
                    break;
            }
        }

        // 硬控同时只生效一个：冻 > 晕 > 惑，输的那个降级成等时长的缓。
        void ApplyHard(EnemyActor e, StatusKind kind, float time, float power)
        {
            if (time <= 0f) return;
            // 头目不吃惑，按比例掉输出代替
            if (kind == StatusKind.Confuse && e.IsBoss)
            {
                e.Slow = Mathf.Min(e.Slow, 1f - power);
                e.SlowTime = Mathf.Max(e.SlowTime, time);
                return;
            }
            // 免定身的照惑那套降级成缓，不要直接吞掉 —— 完全没反应玩家会以为控没生效，
            // 降级成缓至少能看到它变慢了，配合飘出来的「稳」字才知道是这只免控。
            if (e.StunImmune && (kind == StatusKind.Stun || kind == StatusKind.Freeze))
            {
                e.Slow = Mathf.Min(e.Slow, 0.6f);
                e.SlowTime = Mathf.Max(e.SlowTime, time);
                ShowFloat(e.Pos + Vector2.right * 0.12f, "稳", InkTheme.GraphiteHi, 0.95f);
                return;
            }
            if (kind == StatusKind.Stun && e.StunGuard > 0f) return;
            int rank = GlyphTable.HardRank(kind);
            int cur = e.HardTime > 0f ? GlyphTable.HardRank(e.Hard) : 0;
            if (rank < cur)
            {
                e.Slow = Mathf.Min(e.Slow, 0.5f);
                e.SlowTime = Mathf.Max(e.SlowTime, time);
                return;
            }
            if (rank == cur && e.Hard == kind)
            {
                e.HardTime = Mathf.Max(e.HardTime, time);
                return;
            }
            e.Hard = kind;
            e.HardTime = Mathf.Max(e.HardTime, time);
            if (kind == StatusKind.Stun) e.StunGuard = time + 0.35f;
            ShowFloat(e.Pos + Vector2.right * 0.12f, HardWord(kind), InkTheme.Ink, 0.95f);
        }

        static string HardWord(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Freeze: return "冻";
                case StatusKind.Stun: return "晕";
                default: return "惑";
            }
        }

        // 纵横两轴都执行；同轴取最大在收集阶段就做过了。
        void ApplyPush(EnemyActor e, ShotMods m)
        {
            // 免击退的要飘个字。一声不响地不动，玩家只会觉得「退」这个字坏了。
            if (e.NoKnock)
            {
                if (m.PushColumn > 0f || m.PushLateral > 0)
                    ShowFloat(e.Pos, "稳", InkTheme.GraphiteHi, 0.9f);
                return;
            }
            if (m.PushColumn > 0f)
            {
                e.Pos.y = Mathf.Min(GameConstants.SpawnY - 0.35f, e.Pos.y + m.PushColumn);
                if (m.PushHold > 0f) e.HoldTime = Mathf.Max(e.HoldTime, m.PushHold);
                ShowFloat(e.Pos, "退", InkTheme.GraphiteHi, 0.9f);
            }
            if (m.PushLateral > 0)
            {
                int col = FieldLayout.ColumnAtX(e.Pos.x);
                int step = UnityEngine.Random.Range(1, m.PushLateral + 1);
                if (UnityEngine.Random.value < 0.5f) step = -step;
                int to = Mathf.Clamp(col + step, 0, GameConstants.Columns - 1);
                e.Pos.x = FieldLayout.ColumnX(to);
                ShowFloat(e.Pos, "风", InkTheme.Wind, 0.9f);
            }
        }

        // 木：攒够一点就补一格城墙，每波有上限。
        void PourLeech(float amount, float cap)
        {
            if (amount <= 0f) return;
            _leechPool += amount;
            while (_leechPool >= 1f
                   && BaseHp < MaxBaseHp
                   && (cap <= 0f || _leechGiven < cap))
            {
                _leechPool -= 1f;
                _leechGiven += 1f;
                BaseHp++;
                Push(PopKind.Heal, 0, new Vector2(0f, GameConstants.EmitterY + 0.7f), "+1", InkTheme.Wood, 1.15f, 0.9f);
            }
        }

        static Color HitTint(ShotMods m, bool execute)
        {
            if (execute) return InkTheme.Heart;
            if (m.ExplodeR > 0f) return InkTheme.Explode;
            if (m.Has(CardId.Fire)) return Color.white;
            if (m.Has(CardId.Ice)) return InkTheme.IceHi;
            if (m.HasStatus(StatusKind.Stun)) return InkTheme.Word;
            if (m.Color.maxColorComponent > 0.12f) return m.Color;
            return Color.white;
        }

        // 命中只播优先级最高的那一层，其余元素靠墨溅上的色点表示。
        static readonly CardId[] HitOrder =
        {
            CardId.Explode, CardId.Heavy, CardId.Thunder, CardId.Stun, CardId.Fire,
            CardId.Ice, CardId.Water, CardId.Poison, CardId.Earth, CardId.Wind,
            CardId.Gold, CardId.Wood, CardId.Confuse
        };

        static FxBurst BuildBurst(Vector2 pos, ShotMods m, bool execute)
        {
            var fx = new FxBurst
            {
                Pos = pos,
                Kind = HitKindOf(m, execute),
                Event = HitEventOf(m, execute),
                Tint = HitTint(m, execute),
                Scale = HitScale(m, execute)
            };
            // 没抢到元素层的字，各留一个自己颜色的小点，最多两个。
            int shown = 0;
            for (int i = 0; i < HitOrder.Length && fx.Dots < 2; i++)
            {
                CardId id = HitOrder[i];
                if (m.Star(id) <= 0) continue;
                if (shown == 0 && GlyphTable.Get(id).Hit == fx.Kind) { shown = 1; continue; }
                Color c = CardCatalog.Accent(id);
                if (fx.Dots == 0) fx.Dot0 = c;
                else fx.Dot1 = c;
                fx.Dots++;
            }
            return fx;
        }

        static int HitEventOf(ShotMods m, bool execute)
        {
            if (execute) return HitEvent.Kill;
            if (m.HasStatus(StatusKind.Stun)) return HitEvent.Stun;
            if (m.PushColumn > 0f || m.PushLateral > 0) return HitEvent.Push;
            if (m.Pierce > 0) return HitEvent.Pierce;
            return HitEvent.None;
        }

        static int HitKindOf(ShotMods m, bool execute)
        {
            if (execute) return HitFx.Kill;
            if (m.Word == WordId.Cleave || m.WordLook == WordId.Cleave) return HitFx.Cleave;
            if (m.PushColumn > 0f || m.WordLook == WordId.Knockback) return HitFx.Knock;
            if (m.WordLook == WordId.ArrowRain) return HitFx.Arrow;
            if (m.Has(CardId.Fire) && m.Has(CardId.Ice)) return HitFx.FireIce;
            for (int i = 0; i < HitOrder.Length; i++)
                if (m.Has(HitOrder[i])) return GlyphTable.Get(HitOrder[i]).Hit;
            return HitFx.Ink;
        }

        static float HitScale(ShotMods m, bool execute)
        {
            if (m.ExplodeR > 0.01f) return 1.72f;
            if (execute) return 1.55f;
            if (m.Has(CardId.Heavy)) return 1.42f;
            return 1.2f;
        }

        static float HitStopOf(ShotMods m, bool execute)
        {
            if (execute) return 0.15f;
            if (m.ExplodeR > 0.01f || m.Has(CardId.Heavy)) return 0.12f;
            return 0.07f;
        }

        void PulseHitStop(float time)
        {
            if (HitStop <= 0.02f) HitStop = time;
        }

        void Kill(EnemyActor e, ShotMods m)
        {
            e.Dead = true;
            Deaths.Add(new DeathFx { Pos = e.Pos, Type = e.Type, Radius = e.Radius, Boss = e.IsBoss });
            DropLoot(e);
            // 小兵成片地死，一只震一下就是连续抖动。只有块头够大的才配震。
            AddShake(e.IsBoss ? 0.42f : e.Radius > 0.3f ? 0.12f : 0f);
            if (e.IsBoss) BossKilled = true;
            if (e.SplitCount > 0) SpawnSplit(e, e.SplitCount);
            if (m != null && m.BurnPop) BurnPop(e.Pos, m);
        }

        public void AddShake(float amount)
        {
            if (amount > 0f) ShakeWanted = Mathf.Max(ShakeWanted, amount);
        }

        static void Recoil(EnemyActor e, Vector2 dir, float power)
        {
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            e.Recoil = Vector2.ClampMagnitude(e.Recoil + dir.normalized * power, 0.42f);
        }

        // 场上最多同时挂多少件掉落物。超了的直接记账 ——
        // 割草关一秒能死十几只，全都弹出来既看不清也白烧渲染。
        // 72 试过了，太多：一次清屏的技能能同时挂五六十件，全都沿同一条线飞向顶栏，
        // 堆成一坨糊住半个战场，反而看不见自己赚了什么。留一条看得清的流就够。
        const int DropCap = 24;
        const float DropRestTime = 0.26f;
        const float DropFlyTime = 0.38f;

        // 一只怪掉几枚金币、几滴墨。拆成几份是为了「一片金币叮叮当当飞过去」，
        // 一份一大枚反而没有收获感；份数跟着体型走，关底死时该铺满半个屏。
        void DropLoot(EnemyActor e)
        {
            int ink = Mathf.Max(1, e.Gold * GameConstants.InkPerGold);
            Scatter(e, DropKind.Gold, e.Gold, Mathf.Clamp(e.Gold, 1, e.IsBoss ? 8 : 2));
            Scatter(e, DropKind.Ink, ink, Mathf.Clamp(ink / 4, 1, e.IsBoss ? 5 : 1));
        }

        void Scatter(EnemyActor e, DropKind kind, int total, int pieces)
        {
            if (total <= 0) return;
            pieces = Mathf.Max(1, Mathf.Min(pieces, total));
            for (int i = 0; i < pieces; i++)
            {
                int amount = total / pieces + (i < total % pieces ? 1 : 0);
                if (amount <= 0) continue;
                if (Drops.Count >= DropCap)
                {
                    Collect(kind, amount);
                    continue;
                }
                float side = pieces == 1 ? UnityEngine.Random.Range(-1f, 1f) : (i - (pieces - 1) * 0.5f) / pieces * 2f;
                Drops.Add(new DropItem
                {
                    Id = NextActorId++,
                    Kind = kind,
                    Amount = amount,
                    Pos = e.Pos,
                    Vel = new Vector2(side * 1.9f + UnityEngine.Random.Range(-0.35f, 0.35f),
                        UnityEngine.Random.Range(2.4f, 3.8f)),
                    // 落点别掉到漏怪线底下 —— 那儿已经是炮台和界面的地盘了。
                    Ground = Mathf.Max(GameConstants.LeakY + 0.3f, e.Pos.y - UnityEngine.Random.Range(0.3f, 0.9f)),
                    Rest = DropRestTime + UnityEngine.Random.Range(0f, 0.16f),
                    Seed = UnityEngine.Random.value * 10f
                });
            }
        }

        void TickDrops(float dt)
        {
            for (int i = Drops.Count - 1; i >= 0; i--)
            {
                DropItem d = Drops[i];
                if (d.Fly > 0f)
                {
                    // 越飞越快，落点那一下才有「被吸进去」的收束感。
                    d.Fly += dt / DropFlyTime;
                    Vector2 to = d.Kind == DropKind.Gold ? GoldChip : InkChip;
                    float u = Mathf.Clamp01(d.Fly);
                    float e = u * u;
                    // 起手先往侧上方甩一点再拐向药丸，直线飞过去像是在瞬移。
                    Vector2 bend = new Vector2((to.x - d.From.x) * 0.25f + Mathf.Sin(d.Seed) * 0.9f,
                        Mathf.Max(d.From.y, to.y) + 1.1f);
                    Vector2 a = Vector2.Lerp(d.From, bend, e);
                    Vector2 b = Vector2.Lerp(bend, to, e);
                    d.Pos = Vector2.Lerp(a, b, e);
                    if (u >= 1f)
                    {
                        Collect(d.Kind, d.Amount);
                        Drops.RemoveAt(i);
                    }
                    continue;
                }
                if (d.Pos.y > d.Ground)
                {
                    d.Vel.y -= 15f * dt;
                    d.Pos += d.Vel * dt;
                    if (d.Pos.y <= d.Ground)
                    {
                        d.Pos.y = d.Ground;
                        // 弹一下再躺平。掉地上「咚」的那半下是掉落物的全部重量感。
                        if (d.Vel.y < -2.6f) d.Vel = new Vector2(d.Vel.x * 0.4f, -d.Vel.y * 0.32f);
                        else d.Vel = Vector2.zero;
                    }
                    continue;
                }
                d.Rest -= dt;
                if (d.Rest > 0f) continue;
                d.Fly = 0.0001f;
                d.From = d.Pos;
            }
        }

        void Collect(DropKind kind, int amount)
        {
            if (kind == DropKind.Gold)
            {
                Gold += amount;
                GoldPop = 1f;
            }
            else
            {
                Ink = Mathf.Min(GameConstants.InkMax, Ink + amount);
                InkPop = 1f;
            }
        }

        // 一局收尾时把还在天上飞的全部记账。不结的话最后一波的收入会凭空少一截，
        // 而那一截恰好是玩家刚刚看着掉出来的。
        void FlushDrops()
        {
            for (int i = 0; i < Drops.Count; i++) Collect(Drops[i].Kind, Drops[i].Amount);
            Drops.Clear();
        }

        // 火 ★2 的死亡爆燃。走独立的一次性溅射，不再回到 ResolveHit，免得连锁。
        void BurnPop(Vector2 pos, ShotMods m)
        {
            float dmg = Mathf.Max(1.2f, m.StatusPower(StatusKind.Burn) * 1.4f);
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if ((e.Pos - pos).sqrMagnitude > 0.25f) continue;
                e.Hp -= dmg;
                e.HitFlash = 0.14f;
                if (e.Hp <= 0f) Kill(e, null);
            }
            Bursts.Add(new FxBurst
            {
                Pos = pos, Kind = HitFx.Fire, Tint = InkTheme.FireHi, Scale = 1.35f
            });
        }

        // 奶光环：每秒给范围内的**其他**敌人回血。不回自己 —— 这样「先杀奶妈」
        // 才是正解，落单的奶妈也不会变成打不死的肉盾。
        void TickHealAura(EnemyActor src, float dt)
        {
            float amount = src.HealAura * dt;
            float r2 = EnemyCatalog.HealRange * EnemyCatalog.HealRange;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead || e.Id == src.Id || e.Hp >= e.MaxHp) continue;
                if ((e.Pos - src.Pos).sqrMagnitude > r2) continue;
                e.Hp = Mathf.Min(e.MaxHp, e.Hp + amount);
            }
        }

        void TickEnemies(float dt)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor e = Enemies[i];
                if (e.Dead) continue;
                if (e.HitFlash > 0f) e.HitFlash -= dt;
                if (e.Recoil.sqrMagnitude > 0.0001f)
                    e.Recoil = Vector2.Lerp(e.Recoil, Vector2.zero, 1f - Mathf.Exp(-16f * dt));
                if (e.StunGuard > 0f) e.StunGuard -= dt;
                if (e.ColorPhase && e.Hp <= e.MaxHp * 0.5f) e.Colored = true;
                if (e.HealAura > 0f) TickHealAura(e, dt);
                if (e.BurnTime > 0f)
                {
                    e.BurnTime -= dt;
                    e.Hp -= e.BurnDps * dt;
                    if (e.Hp <= 0f) { Kill(e, null); continue; }
                }
                if (e.PoisonTime > 0f)
                {
                    e.PoisonTime -= dt;
                    float tick = e.PoisonDps * e.PoisonStacks * dt;
                    e.Hp -= tick;
                    // 蚀生：毒造成的伤害全额进吸血池
                    if (e.PoisonLeech > 0f) PourLeech(tick, e.PoisonLeech);
                    if (e.PoisonTime <= 0f) { e.PoisonStacks = 0; e.PoisonLeech = 0f; }
                    if (e.Hp <= 0f) { Kill(e, null); continue; }
                }
                if (e.HardTime > 0f)
                {
                    bool wasStun = e.Hard == StatusKind.Stun;
                    e.HardTime -= dt;
                    if (e.HardTime <= 0f)
                    {
                        e.Hard = StatusKind.None;
                        // 焦雷：晕一结束就补上那一下
                        if (wasStun && e.Shock > 0f)
                        {
                            e.Hp -= e.Shock;
                            e.HitFlash = 0.16f;
                            ShowDamage(e, e.Shock, InkTheme.ThunderHi, 1.1f, false);
                            e.Shock = 0f;
                            if (e.Hp <= 0f) { Kill(e, null); continue; }
                        }
                    }
                }
                if (e.Held) continue;
                if (e.HoldTime > 0f) { e.HoldTime -= dt; continue; }
                if (e.SlowTime > 0f) e.SlowTime -= dt;
                else e.Slow = 1f;
                if (e.Confused) { TickConfused(e, dt); continue; }

                float speed = e.Speed * e.Slow;
                // 狂化：半血后提速。这几只同时开了 ColorPhase，所以染红那个
                // 反馈现在就是「它加速了」的信号，不再只是装饰。
                if (e.RageSpeed > 1f && e.Hp <= e.MaxHp * 0.5f) speed *= e.RageSpeed;
                if (e.PreferEmpty)
                {
                    int col = FieldLayout.ColumnAtX(e.Pos.x);
                    if (!ColumnHasMod(col)) speed *= 1.28f;
                }
                e.Pos.y -= speed * dt;
                if (e.Strafe)
                {
                    e.Pos.x += e.StrafeDir * 0.55f * dt;
                    float min = FieldLayout.ColumnX(0);
                    float max = FieldLayout.ColumnX(GameConstants.Columns - 1);
                    if (e.Pos.x < min || e.Pos.x > max)
                    {
                        e.StrafeDir *= -1f;
                        e.Pos.x = Mathf.Clamp(e.Pos.x, min, max);
                    }
                }
                if (e.Pos.y <= GameConstants.LeakY)
                {
                    e.Dead = true;
                    BaseHp = Mathf.Max(0, BaseHp - 1);
                    AddShake(0.7f);
                    ShowToast("防线被突破");
                }
            }
            Enemies.RemoveAll(e => e.Dead);
        }

        // 惑：掉头往回走，并定期砍最近的同类。
        void TickConfused(EnemyActor e, float dt)
        {
            e.Pos.y = Mathf.Min(GameConstants.SpawnY - 0.35f, e.Pos.y + e.Speed * e.Slow * 0.5f * dt);
            e.ConfuseCd -= dt;
            if (e.ConfuseCd > 0f) return;
            e.ConfuseCd = 0.5f;
            EnemyActor best = null;
            float bestD = 1.2f * 1.2f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyActor other = Enemies[i];
                if (other.Dead || other.Id == e.Id) continue;
                float d = (other.Pos - e.Pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = other; }
            }
            if (best == null) return;
            float dmg = e.MaxHp * 0.08f;
            best.Hp -= dmg;
            best.HitFlash = 0.14f;
            ShowDamage(best, dmg, InkTheme.Confuse, 0.9f, false);
            Bursts.Add(new FxBurst
            {
                Pos = best.Pos, Kind = HitFx.Confuse, Tint = InkTheme.ConfuseHi, Scale = 1f
            });
            if (best.Hp <= 0f) Kill(best, null);
        }

        bool ColumnHasMod(int col)
        {
            for (int r = 0; r < OpenRows; r++)
                if (Grid[col, r].HasValue) return true;
            return false;
        }

        void TickFloats(float dt)
        {
            for (int i = Floats.Count - 1; i >= 0; i--)
            {
                FloatText f = Floats[i];
                f.Life -= dt;
                // 抛出去再落回来。原来是匀速上飘，一排数字像电梯一样齐步走，
                // 看不出哪个是刚打的；给点初速和重力，新的那个自己会跳出来。
                f.Vel.y -= 5.2f * dt;
                f.Pos += f.Vel * dt;
                if (f.Punch > 0f) f.Punch = Mathf.Max(0f, f.Punch - dt * 6f);
                if (f.Life <= 0f) Floats.RemoveAt(i);
            }
        }

        void CheckEnd()
        {
            if (BaseHp <= 0) { Defeat = true; Paused = true; FlushDrops(); return; }
            // 前两关是教学关，没有关底。原来这里硬要求 BossKilled，
            // 无 boss 的关会永远停在最后一波打不完。
            bool bossDone = !Stage.HasBoss || BossKilled;
            if (bossDone && AliveEnemies == 0 && WaveIndex >= Stage.Waves.Length)
            {
                Victory = true;
                Paused = true;
                FlushDrops();
            }
        }

        public bool TryRevive()
        {
            if (RevivesUsed >= GameConstants.MaxRevives || (!Defeat && BaseHp > 0)) return false;
            RevivesUsed++;
            Defeat = false;
            Paused = false;
            BaseHp = Mathf.Max(1, BaseHp);
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i].Pos.y < GameConstants.GridCenterY)
                    Enemies.RemoveAt(i);
            }
            ShowToast("防线重整");
            return true;
        }

        public enum PlaceResult { Placed, Upgraded, NeedConfirm, RejectedMaxStar, LockedRow }

        public PlaceResult PeekPlace(CardId id, int col, int row)
        {
            if (!IsOpen(col, row)) return PlaceResult.LockedRow;
            if (!Grid[col, row].HasValue) return PlaceResult.Placed;
            if (Grid[col, row] == id)
                return Stars[col, row] >= GameConstants.MaxStar ? PlaceResult.RejectedMaxStar : PlaceResult.Upgraded;
            return PlaceResult.NeedConfirm;
        }

        public PlaceResult Place(CardId id, int col, int row, bool overwrite)
        {
            PlaceResult peek = PeekPlace(id, col, row);
            if (peek == PlaceResult.LockedRow || peek == PlaceResult.RejectedMaxStar) return peek;
            if (peek == PlaceResult.NeedConfirm && !overwrite) return peek;
            if (peek == PlaceResult.Upgraded)
            {
                Stars[col, row]++;
                ShowToast($"{CardCatalog.Get(id).Name} {Stars[col, row]} 星");
            }
            else
            {
                Grid[col, row] = id;
                Stars[col, row] = 1;
            }
            WordId before = _word != null ? _word[col] : WordId.None;
            RefreshCol(col);
            if (_word[col] != WordId.None && _word[col] != before)
            {
                LastReveal = CardCatalog.WordName(_word[col]);
                RevealTime = 1.6f;
                ShowToast("成词 · " + LastReveal);
            }
            return peek == PlaceResult.NeedConfirm ? PlaceResult.Placed : peek;
        }

        void RefreshCol(int col)
        {
            if (_word == null) return;
            _word[col] = WordId.None;
            _wordStar[col] = 0;
            _pierceStar[col] = 0;
            _explodeStar[col] = 0;
            _heavyStar[col] = 0;
            _stunStar[col] = 0;
            bool sec = false, kill = false, myriad = false, arrow = false, strike = false, back = false, link = false, slash = false;
            int secS = 0, killS = 0, myriadS = 0, arrowS = 0, strikeS = 0, backS = 0, linkS = 0, slashS = 0;
            for (int r = 0; r < OpenRows; r++)
            {
                if (!Grid[col, r].HasValue) continue;
                CardId id = Grid[col, r].Value;
                int star = Stars[col, r];
                if (id == CardId.Pierce) _pierceStar[col] = Mathf.Max(_pierceStar[col], star);
                if (id == CardId.Explode) _explodeStar[col] = Mathf.Max(_explodeStar[col], star);
                if (id == CardId.Heavy) _heavyStar[col] = Mathf.Max(_heavyStar[col], star);
                if (id == CardId.Stun) _stunStar[col] = Mathf.Max(_stunStar[col], star);
                if (id == CardId.Sec) { sec = true; secS = star; }
                if (id == CardId.Kill) { kill = true; killS = star; }
                if (id == CardId.Myriad) { myriad = true; myriadS = star; }
                if (id == CardId.Arrow) { arrow = true; arrowS = star; }
                if (id == CardId.Strike) { strike = true; strikeS = star; }
                if (id == CardId.Back) { back = true; backS = star; }
                if (id == CardId.Link) { link = true; linkS = star; }
                if (id == CardId.Slash) { slash = true; slashS = star; }
            }
            if (sec && kill) { _word[col] = WordId.InstantKill; _wordStar[col] = Mathf.Max(secS, killS); }
            else if (myriad && arrow) { _word[col] = WordId.ArrowRain; _wordStar[col] = Mathf.Max(myriadS, arrowS); }
            else if (strike && back) { _word[col] = WordId.Knockback; _wordStar[col] = Mathf.Max(strikeS, backS); }
            else if (link && slash) { _word[col] = WordId.Cleave; _wordStar[col] = Mathf.Max(linkS, slashS); }
        }

        public bool CellAsleep(int col, int row)
        {
            if (Grid == null || !Grid[col, row].HasValue) return false;
            CardDef def = CardCatalog.Get(Grid[col, row].Value);
            return def.Wake == CardWake.WordPart && _word[col] != def.Word;
        }

        public bool CellWordLit(int col, int row)
        {
            if (Grid == null || !Grid[col, row].HasValue) return false;
            CardDef def = CardCatalog.Get(Grid[col, row].Value);
            return def.Wake == CardWake.WordPart && _word[col] == def.Word;
        }

        public void CellCharge(int col, int row, out int now, out int need)
        {
            now = 0;
            need = 0;
            if (Grid == null || _charge == null || !Grid[col, row].HasValue) return;
            CardDef def = CardCatalog.Get(Grid[col, row].Value);
            if (def.Wake == CardWake.Charge)
            {
                need = def.ChargeNeed;
                now = _charge[col, ChargeKind(def.Id)];
            }
            else if (def.Wake == CardWake.WordPart && _word[col] == def.Word)
            {
                need = CardCatalog.WordChargeNeed(def.Word);
                now = _charge[col, ChWord];
            }
        }

        static int ChargeKind(CardId id)
        {
            if (id == CardId.Pierce) return ChPierce;
            if (id == CardId.Explode) return ChExplode;
            if (id == CardId.Heavy) return ChHeavy;
            if (id == CardId.Stun) return ChStun;
            return ChWord;
        }

        public void ShowToast(string text)
        {
            Toast = text;
            ToastTime = 1.4f;
        }

        public void ShowFloat(Vector2 pos, string text, Color color, float scale = 1f)
        {
            Push(PopKind.Word, 0, pos, text, color, scale, 0.72f);
        }

        // 伤害数字。同一只怪在半秒内挨的所有伤害并成一个数往上滚 ——
        // 分裂弹一轮能打七八下，逐下飘出来就是一屏「1 1 1 1」，
        // 既读不出打了多少，也把真正的大数字盖住了。
        public void ShowDamage(EnemyActor e, float dmg, Color color, float scale, bool crit)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(dmg));
            PopKind kind = crit ? PopKind.Crit : PopKind.Damage;
            for (int i = 0; i < Floats.Count; i++)
            {
                FloatText f = Floats[i];
                if (f.OwnerId != e.Id) continue;
                if (f.Kind != PopKind.Damage && f.Kind != PopKind.Crit) continue;
                if (f.Life < f.MaxLife - 0.5f) continue;
                f.Value += n;
                f.Text = Mathf.RoundToInt(f.Value).ToString();
                // 越滚越大，但压住上限，免得一串连击把字撑满半个屏。
                f.Scale = Mathf.Max(f.Scale, scale) * (1f + Mathf.Min(0.45f, f.Value * 0.012f));
                f.Punch = 1f;
                f.Life = f.MaxLife;
                f.Vel = new Vector2(f.Vel.x * 0.5f, 1.5f);
                f.Pos = Vector2.Lerp(f.Pos, e.Pos + Vector2.up * 0.3f, 0.45f);
                if (crit) { f.Kind = PopKind.Crit; f.Color = color; }
                return;
            }
            FloatText pop = Push(kind, e.Id, e.Pos + Vector2.up * 0.24f, n.ToString(), color, scale, 0.78f);
            pop.Value = n;
        }

        FloatText Push(PopKind kind, int owner, Vector2 pos, string text, Color color, float scale, float life)
        {
            var f = new FloatText
            {
                Id = NextActorId++,
                OwnerId = owner,
                Kind = kind,
                Pos = pos + new Vector2(UnityEngine.Random.Range(-0.16f, 0.16f), UnityEngine.Random.Range(0f, 0.08f)),
                Vel = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(1.7f, 2.3f)),
                Text = text,
                Color = color,
                Scale = scale,
                Punch = 1f,
                Life = life,
                MaxLife = life
            };
            Floats.Add(f);
            return f;
        }

        public void SetRailFromWorldX(float x, bool snap)
        {
            float left = x - (EmitterCount - 1) * GameConstants.CellWidth * 0.5f;
            float maxLeft = FieldLayout.ColumnX(0);
            float maxRight = FieldLayout.ColumnX(GameConstants.Columns - EmitterCount);
            left = Mathf.Clamp(left, maxLeft, maxRight);
            if (snap)
            {
                int col = FieldLayout.ColumnAtX(left);
                col = Mathf.Clamp(col, 0, GameConstants.Columns - EmitterCount);
                RailX = FieldLayout.ColumnX(col);
            }
            else RailX = left;
        }
    }
}
