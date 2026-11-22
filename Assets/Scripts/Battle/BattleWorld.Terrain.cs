using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 活棋盘地形格（灵砚四星格 / 墨晶宝矿格 / 古碑格）与「百鬼狂潮」局内爽点逻辑。
    public sealed partial class BattleWorld
    {
        public readonly CellTerrain[,] Terrain = new CellTerrain[GameConstants.Columns, GameConstants.Rows];
        public readonly int[,] CrystalHp = new int[GameConstants.Columns, GameConstants.Rows];
        public const int CrystalMaxHp = 10;

        // 百鬼狂潮局内统计：打不倒金翁喷出的额外金箔、免费增援炮台阶段。
        public int DarumaFoil;
        int _darumaNotch;
        int _frenzyBoostStage;
        public bool DarumaActive;

        public float FrenzyLeft => Stage != null && Stage.Frenzy
            ? Mathf.Max(0f, GameConstants.FrenzySeconds - BattleTime)
            : 0f;

        public int FrenzyKillFoil
        {
            get
            {
                if (Stage == null || !Stage.Frenzy) return 0;
                int f = 3 + Kills / 28;
                for (int i = 0; i < StageCatalog.FrenzyKillGoals.Length; i++)
                    if (Kills >= StageCatalog.FrenzyKillGoals[i]) f += 3;
                return Mathf.Min(22, f);
            }
        }

        public int FrenzyTotalFoil => FrenzyKillFoil + DarumaFoil;

        // 算上灵砚格 (+1 星，最高突破 ★4 极意) 后的实际生效星级。
        public int EffectiveStar(int col, int row)
        {
            if (Stars == null || col < 0 || col >= GameConstants.Columns || row < 0 || row >= GameConstants.Rows)
                return 0;
            int s = Stars[col, row];
            if (s <= 0) return 0;
            return Terrain[col, row] == CellTerrain.Inkwell ? s + 1 : s;
        }

        public bool HasCrystal(int col, int row) =>
            col >= 0 && col < GameConstants.Columns && row >= 0 && row < GameConstants.Rows && CrystalHp[col, row] > 0;

        public bool IsStele(int col, int row) =>
            col >= 0 && col < GameConstants.Columns && row >= 0 && row < GameConstants.Rows && Terrain[col, row] == CellTerrain.Stele;

        void BeginTerrainAndFrenzy()
        {
            DarumaFoil = 0;
            _darumaNotch = 0;
            _frenzyBoostStage = 0;
            DarumaActive = false;

            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++)
            {
                CellTerrain t = Stage != null ? Stage.TerrainAt(c, r) : CellTerrain.None;
                Terrain[c, r] = t;
                if (t == CellTerrain.Crystal)
                {
                    CrystalHp[c, r] = CrystalMaxHp;
                    // 即使在铲子章（11/12章），墨晶矿显露在盘面上供炮弹敲击，敲爆后自动解锁该格！
                }
                else
                {
                    CrystalHp[c, r] = 0;
                }
            }

            if (Stage != null && Stage.Frenzy)
            {
                // 百鬼狂潮：开局保底 4 门炮 + 射速狂飙！
                EmitterCount = Mathf.Max(EmitterCount, 4);
                _intervalMul *= 0.62f;
                RailX = FieldLayout.ColumnX(Mathf.Max(0, (GameConstants.Columns - EmitterCount) / 2));
            }
        }

        // 炮弹向上飞经第 row 行时，若这格立着墨晶宝矿，顺手敲一下！敲满 10 下爽脆爆金币 + 白送 ★2 字！
        void CheckCrystalPass(BulletActor b, int col, int row)
        {
            if (!b.Mother || row < 0 || row >= GameConstants.Rows || CrystalHp[col, row] <= 0) return;
            CrystalHp[col, row]--;
            Vector2 pos = FieldLayout.CellPos(col, row);
            if (CrystalHp[col, row] > 0)
            {
                if (CrystalHp[col, row] % 3 == 0)
                {
                    Bursts.Add(new FxBurst { Pos = pos, Kind = HitFx.Ice, Tint = InkTheme.GoldHi, Scale = 0.72f });
                    AudioBus.HitIce();
                }
                return;
            }

            // 敲碎墨晶！自动开格 + 爆金币 + 原地送一张随机 2 星常驻/蓄力好字！
            Terrain[col, row] = CellTerrain.None;
            if (!Open[col, row])
            {
                Open[col, row] = true;
                OpenRows = Mathf.Max(OpenRows, row + 1);
            }
            int bonusGold = Stage != null && Stage.Frenzy ? 16 : 22;
            Gold += bonusGold;
            GoldPop = 1f;
            AudioBus.CardRare();
            AudioBus.Coin();
            AddShake(0.22f);
            Bursts.Add(new FxBurst { Pos = pos, Kind = HitFx.Kill, Tint = InkTheme.GoldHi, Scale = 1.45f });

            CardId gift = RollCrystalGift(col);
            if (gift != CardId.None && !Grid[col, row].HasValue)
            {
                Grid[col, row] = gift;
                Stars[col, row] = 2;
                RefreshCol(col);
                Beat(CellBeat.Upgrade, col, row);
                ShowFloat(pos + Vector2.up * 0.32f, $"破晶得「{CardCatalog.Get(gift).Name}」★2  +{bonusGold}金", InkTheme.GoldHi, 1.08f);
            }
            else
            {
                Beat(CellBeat.Unlock, col, row);
                ShowFloat(pos + Vector2.up * 0.32f, $"墨晶爆裂 +{bonusGold}金", InkTheme.GoldHi, 1.05f);
            }
        }

        CardId RollCrystalGift(int col)
        {
            CardId[] pool = Stage != null ? Stage.Pool : null;
            if (pool == null || pool.Length == 0) return CardId.Fire;
            var cands = new List<CardId>();
            for (int i = 0; i < pool.Length; i++)
            {
                CardDef d = CardCatalog.Get(pool[i]);
                if (d.Id != CardId.None && d.Wake != CardWake.WordPart) cands.Add(d.Id);
            }
            if (cands.Count == 0) return CardId.Fire;
            return cands[Random.Range(0, cands.Count)];
        }

        void TickFrenzy(float dt)
        {
            if (Stage == null || !Stage.Frenzy) return;
            if (_frenzyBoostStage == 0 && BattleTime >= 28f)
            {
                _frenzyBoostStage = 1;
                if (EmitterCount < GameConstants.AdEmitterCap) EmitterCount++;
                ClampRail();
                RushTime = Mathf.Max(RushTime, 4.5f);
                Shouts.Add(new Shout { Kind = ShoutKind.Rush, Text = "狂热升温！", Sub = "免费增援一门火炮" });
                AudioBus.Rush();
            }
            else if (_frenzyBoostStage == 1 && BattleTime >= 56f)
            {
                _frenzyBoostStage = 2;
                EmitterCount = GameConstants.AdEmitterCap;
                ClampRail();
                RushTime = Mathf.Max(RushTime, 5.0f);
                Shouts.Add(new Shout { Kind = ShoutKind.Rush, Text = "六炮齐射！", Sub = "全列满编无限火力" });
                AudioBus.Rush();
            }
            else if (_frenzyBoostStage == 2 && WaveIndex >= StageCatalog.FrenzyWaves - 1 && !DarumaActive)
            {
                DarumaActive = true;
                Shouts.Add(new Shout { Kind = ShoutKind.Horde, Text = "不倒金翁驾到！", Sub = "痛扁金翁狂爆金箔" });
                AudioBus.SlotJackpot();
                AddShake(0.24f);
            }
        }

        void NoteDarumaHit(EnemyActor e, float prevHp)
        {
            if (e == null || e.Type != EnemyId.Daruma || e.MaxHp <= 1f) return;
            float lostFrac = Mathf.Clamp01(1f - Mathf.Max(0f, e.Hp) / e.MaxHp);
            int targetNotch = Mathf.Min(12, Mathf.FloorToInt(lostFrac * 12f));
            while (_darumaNotch < targetNotch)
            {
                _darumaNotch++;
                DarumaFoil++;
                Gold += 5;
                GoldPop = 1f;
                Vector2 jitter = new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0.25f, 0.55f));
                ShowFloat(e.Pos + jitter, "金箔 +1", InkTheme.GoldHi, 1.22f);
                AudioBus.Coin();
            }
            if (e.Hp <= 0f && prevHp > 0f)
            {
                DarumaFoil += 3;
                ShowFloat(e.Pos + Vector2.up * 0.55f, "击破金翁！金箔 +3", InkTheme.GoldHi, 1.4f);
                AudioBus.SlotJackpot();
                AddShake(0.35f);
            }
        }

        // 百鬼狂潮绝不失败：怪碰到底线时触发金光震波弹回，零挫败爽玩到底！
        bool HandleFrenzyLeak(EnemyActor e)
        {
            if (Stage == null || !Stage.Frenzy) return false;
            if (e.Type == EnemyId.Daruma)
            {
                e.Pos.y = GameConstants.LeakY + 2.4f;
                ShowFloat(e.Pos + Vector2.up * 0.3f, "不倒翁弹回！", InkTheme.GoldHi, 1.05f);
                AudioBus.CellDrop();
                return true;
            }
            e.Pos.y = Mathf.Min(GameConstants.SpawnY - 0.6f, e.Pos.y + 2.6f);
            e.HoldTime = Mathf.Max(e.HoldTime, 0.7f);
            Bursts.Add(new FxBurst { Pos = new Vector2(e.Pos.x, GameConstants.LeakY + 0.2f), Kind = HitFx.Knock, Tint = InkTheme.GoldHi, Scale = 1.1f });
            AudioBus.HitWind();
            return true;
        }
    }
}
