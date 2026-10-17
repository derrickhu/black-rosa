#if PROMO_CAPTURE
using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 买量视频的自动演示：满级存档直接进关，机器人拖炮排、挑改装、放字，录到通关页为止。
    // 命令行：-promoOut 帧目录 [-promoStage 关下标] [-promoGold 金币倍数] [-promoSeconds 最长秒数]
    //         [-promoSkin 皮肤] [-promoWarm 预热秒数] [-promoSeed 随机种子]
    public sealed partial class GameFlow
    {
        sealed class PromoPlan
        {
            public string Out;
            public int Stage = 44;
            public int GoldMul = 3;
            public float Seconds = 180f;
            public int Skin = 4;
            // 第一次进关只为把 CDN 背景拉下来，等这么久再重进一次开录。
            public float Warm = 6f;
            public int Seed = 7;
            // 开打后这几个时间点各加一门炮，演示「加一门炮」。
            public float[] GunAt = { 5f, 13f };
        }

        PromoPlan _plan;
        PromoRecorder _rec;
        int _pPhase;
        float _pReal;
        float _pClock;
        float _pEnd;
        Screen _pScreen = Screen.Lobby;
        float _pStep;
        bool _pTapped;
        int _pCard = -1;
        int _pCol;
        int _pRow;
        bool _pOverwrite;
        bool _pCellSet;
        int _pGun;
        float _pIdle;
        Image _pHand;
        Vector2 _pHandAt;
        Vector2 _pHandFrom;
        float _pPress;
        bool _pQuit;

        static string PromoArg(string key)
        {
            string[] a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == key) return a[i + 1];
            return null;
        }

        partial void PromoStart(ref bool took)
        {
            string dir = PromoArg("-promoOut");
            if (string.IsNullOrEmpty(dir)) return;
            _plan = new PromoPlan { Out = dir };
            if (int.TryParse(PromoArg("-promoStage"), out int stage))
                _plan.Stage = Mathf.Clamp(stage, 0, GameConstants.StageCount - 1);
            if (int.TryParse(PromoArg("-promoGold"), out int gold)) _plan.GoldMul = Mathf.Max(1, gold);
            if (float.TryParse(PromoArg("-promoSeconds"), out float secs)) _plan.Seconds = secs;
            if (int.TryParse(PromoArg("-promoSkin"), out int skin)) _plan.Skin = skin;
            if (float.TryParse(PromoArg("-promoWarm"), out float warm)) _plan.Warm = warm;
            if (int.TryParse(PromoArg("-promoSeed"), out int seed)) _plan.Seed = seed;
            took = true;
            Application.runInBackground = true;
            // captureFramerate 只管游戏时间，界面动画和音效间隔走真实时间。
            // 真实帧率也锁到录制帧率，两边才对得上，不然界面在成片里是慢动作。
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = PromoRecorder.Fps;
            Debug.Log($"[Promo] screen {UnityEngine.Screen.width}x{UnityEngine.Screen.height} stage {_plan.Stage}");
        }

        partial void PromoBoot(ref bool took)
        {
            if (_plan == null) return;
            took = true;
            MetaProgress.Wipe();
            _meta = MetaProgress.Load();
            _meta.SetGuide(MetaProgress.GuideDone);
            _meta.SetItemGuideDone();
            _meta.UnlockStages();
            _meta.UnlockSkins();
            _meta.MaxForge();
            _meta.UnlockCodex();
            for (int i = 0; i < _meta.ItemLevel.Length && i < ItemCatalog.Count; i++)
                _meta.ItemLevel[i] = ItemCatalog.MaxLevel;
            _meta.EquipAt((int)ItemId.Dart, 0);
            _meta.EquipAt((int)ItemId.Sweep, 1);
            _meta.EquipAt((int)ItemId.Burst, 2);
            _meta.EquipSkin(_plan.Skin);
            _meta.FillStamina();
            AudioBus.Unlock();
            BattleWorld.GmGoldMul = _plan.GoldMul;
            _pReal = Time.realtimeSinceStartup;
            StartStage(_plan.Stage);
        }

        partial void PromoTick()
        {
            if (_plan == null || _pQuit) return;
            _leftGame = false;
            if (_askingRetreat)
            {
                Transform s = _layer.Find("settings");
                if (s != null) Destroy(s.gameObject);
                _askingRetreat = false;
                if (_world != null) _world.Paused = false;
            }
            if (_pPhase == 0)
            {
                if (_world != null) _world.Paused = true;
                if (Time.realtimeSinceStartup - _pReal < _plan.Warm) return;
                UnityEngine.Random.InitState(_plan.Seed);
                _meta.FillStamina();
                StartStage(_plan.Stage);
                _rec = PromoRecorder.Begin(_plan.Out);
                _pPhase = 1;
                _pClock = 0f;
                return;
            }
            float dt = 1f / PromoRecorder.Fps;
            _pClock += dt;
            PromoLog();
            if (_pPhase == 1)
            {
                PromoDrive(dt);
                if (_screen == Screen.Result)
                {
                    _pPhase = 2;
                    _pEnd = _pClock + 5f;
                    if (_pHand != null) _pHand.gameObject.SetActive(false);
                }
                else if (_pClock > _plan.Seconds) PromoWrap();
            }
            else if (_pClock > _pEnd) PromoWrap();
        }

        void PromoWrap()
        {
            if (_pQuit) return;
            _pQuit = true;
            if (_rec != null) _rec.End();
            System.IO.File.WriteAllText(System.IO.Path.Combine(_plan.Out, "events.csv"), _pLog.ToString());
            Application.Quit();
        }

        // 逐帧记一行局面，剪片时按它找高光：连杀、清波、成词、道具、关底。
        readonly System.Text.StringBuilder _pLog =
            new System.Text.StringBuilder("frame,screen,alive,combo,rush,wave,waves,boss,bossDead,filled,stars,guns,item,word,victory\n");

        void PromoLog()
        {
            if (_rec == null || _world == null) return;
            int filled = 0, stars = 0;
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++)
            {
                if (!_world.Grid[c, r].HasValue) continue;
                filled++;
                stars += _world.Stars[c, r];
            }
            bool item = _world.ItemResolving || (_hud != null && _hud.ItemShow);
            string word = _world.RevealTime > 1.5f ? _world.LastReveal : "";
            _pLog.Append(_rec.Frames).Append(',').Append(_screen).Append(',')
                .Append(_world.AliveEnemies).Append(',').Append(_world.Combo).Append(',')
                .Append(_world.RushTime > 0f ? 1 : 0).Append(',').Append(_world.WaveIndex).Append(',')
                .Append(_world.Stage.Waves.Length).Append(',').Append(_world.BossSpawned ? 1 : 0).Append(',')
                .Append(_world.BossKilled ? 1 : 0).Append(',').Append(filled).Append(',').Append(stars).Append(',')
                .Append(_world.EmitterCount).Append(',').Append(item ? 1 : 0).Append(',').Append(word).Append(',')
                .Append(_world.Victory ? 1 : 0).Append('\n');
        }

        void PromoDrive(float dt)
        {
            PromoHand();
            if (_screen != _pScreen)
            {
                _pScreen = _screen;
                _pStep = 0f;
                _pTapped = false;
                _pHandFrom = _pHandAt;
                if (_screen == Screen.Draft) _pCard = -1;
            }
            _pStep += dt;
            if (_screen == Screen.Battle) PromoBattle(dt);
            else if (_screen == Screen.Draft) PromoDraft();
            else if (_screen == Screen.Place) PromoPlace();
            _pPress = Mathf.Max(0f, _pPress - dt);
            float s = _pPress > 0f ? 0.84f : 1f;
            var rt = _pHand.rectTransform;
            rt.localScale = new Vector3(s, s, 1f);
            rt.anchoredPosition = _pHandAt;
            rt.SetAsLastSibling();
        }

        void PromoHand()
        {
            if (_pHand != null) return;
            _pHand = UiKit.Icon(_layer, InkSprites.Load("Ui/guide_hand"), Vector2.zero, 150f);
            _pHand.gameObject.name = "promo_hand";
            _pHand.raycastTarget = false;
            var rt = _pHand.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(90f, 150f);
            rt.pivot = new Vector2(0.2f, 0.97f);
            _pHandAt = BattleHud.WorldToCanvas(_layer, new Vector3(0f, GameConstants.EmitterY - 0.5f, 0f));
        }

        void PromoBattle(float dt)
        {
            if (_pGun < _plan.GunAt.Length && _pClock >= _plan.GunAt[_pGun])
            {
                _pGun++;
                if (_world.EmitterCount < GameConstants.AdEmitterCap) _world.AddEmitter();
            }
            // 格子满了不会自动弹改装，演示里接着点开，把字升星。
            bool busy = _world.ItemResolving || (_hud != null && _hud.ItemShow);
            if (_world.BoardFull && _world.CanDraft && !busy)
            {
                _pIdle += dt;
                if (_pIdle > 1.2f)
                {
                    _pIdle = 0f;
                    OpenDraft();
                    return;
                }
            }
            else _pIdle = 0f;

            float half = (_world.EmitterCount - 1) * GameConstants.CellWidth * 0.5f;
            float center = _world.RailX + half;
            float next = Mathf.MoveTowards(center, PromoAim(center), 7.5f * dt);
            _world.SetRailFromWorldX(next, false);
            float shown = _world.RailX + half;
            Vector2 at = BattleHud.WorldToCanvas(_layer, new Vector3(shown, GameConstants.EmitterY - 0.55f, 0f));
            _pHandAt = Vector2.Lerp(_pHandAt, at, 1f - Mathf.Exp(-dt * 16f));
        }

        // 对着怪群的重心，越靠近防线权重越大，关底再加重；没怪时炮排轻轻晃，手不闲着。
        float PromoAim(float center)
        {
            float top = GameConstants.SpawnY - 0.8f;
            float sum = 0f, weight = 0f;
            for (int i = 0; i < _world.Enemies.Count; i++)
            {
                EnemyActor e = _world.Enemies[i];
                if (e.Dead || e.Pos.y > top) continue;
                float w = Mathf.Exp(-(e.Pos.y - GameConstants.LeakY) / 3f) * (e.IsBoss ? 3f : 1f);
                sum += e.Pos.x * w;
                weight += w;
            }
            return weight > 0f ? sum / weight : Mathf.Sin(_pClock * 0.9f) * 0.9f;
        }

        void PromoDraft()
        {
            var view = _overlay != null ? _overlay.GetComponent<DraftView>() : null;
            if (view == null || view.Cards == null || _offer == null) return;
            if (_pCard < 0) _pCard = PromoChooseCard();
            if (_pCard < 0 || _pCard >= view.Cards.Length || view.Cards[_pCard] == null) return;
            Vector2 to = RewardFly.Local(_layer, view.Cards[_pCard].transform) + new Vector2(-6f, -18f);
            float u = Mathf.Clamp01((_pStep - 0.4f) / 0.3f);
            _pHandAt = Vector2.Lerp(_pHandFrom, to, u * u * (3f - 2f * u));
            if (_pStep >= 0.82f && !_pTapped)
            {
                _pTapped = true;
                _pPress = 0.14f;
            }
            if (_pStep >= 0.94f)
            {
                AudioBus.Tap();
                Pick(_offer[_pCard]);
            }
        }

        void PromoPlace()
        {
            if (!_pCellSet) PromoBestCell(_held, out _pCol, out _pRow, out _pOverwrite);
            _pCellSet = true;
            Vector2 to = BattleHud.WorldToCanvas(_layer, FieldLayout.CellPos(_pCol, _pRow));
            float u = Mathf.Clamp01((_pStep - 0.12f) / 0.3f);
            _pHandAt = Vector2.Lerp(_pHandFrom, to, u * u * (3f - 2f * u));
            if (_pStep >= 0.55f && !_pTapped)
            {
                _pTapped = true;
                _pPress = 0.14f;
            }
            if (_pStep >= 0.66f)
            {
                _pCellSet = false;
                _world.Place(_held, _pCol, _pRow, _pOverwrite);
                AfterPlace();
            }
        }

        int PromoChooseCard()
        {
            int pick = -1;
            float best = float.MinValue;
            for (int i = 0; i < _offer.Length; i++)
            {
                float score = PromoBestCell(_offer[i], out int c, out int r, out bool ow)
                              + UnityEngine.Random.Range(0f, 3f);
                if (score <= best) continue;
                best = score;
                pick = i;
                _pCol = c;
                _pRow = r;
                _pOverwrite = ow;
            }
            _pCellSet = pick >= 0;
            return pick;
        }

        // 能升星先升星；成词字往搭档那一列放；其它放空格，偏中间、偏少字的列。
        // 实在没空位就覆盖星最低的一格。
        float PromoBestCell(CardId id, out int col, out int row, out bool overwrite)
        {
            col = 0;
            row = 0;
            overwrite = false;
            float best = float.MinValue;
            CardDef def = CardCatalog.Get(id);
            CardId mate = def.Wake == CardWake.WordPart ? CardCatalog.Partner(id) : CardId.None;
            for (int c = 0; c < GameConstants.Columns; c++)
            {
                int filled = 0;
                bool hasMate = false;
                for (int r = 0; r < GameConstants.Rows; r++)
                {
                    if (_world.Grid[c, r].HasValue) filled++;
                    if (mate != CardId.None && _world.Grid[c, r] == mate) hasMate = true;
                }
                for (int r = 0; r < GameConstants.Rows; r++)
                {
                    BattleWorld.PlaceResult p = _world.PeekPlace(id, c, r);
                    float score;
                    if (p == BattleWorld.PlaceResult.Upgraded) score = 40f - _world.Stars[c, r];
                    else if (p == BattleWorld.PlaceResult.Placed)
                        score = (hasMate ? 46f : 20f) - filled * 1.5f - Mathf.Abs(c - 2.5f) * 0.4f;
                    else if (p == BattleWorld.PlaceResult.NeedConfirm) score = 4f - _world.Stars[c, r];
                    else continue;
                    if (score <= best) continue;
                    best = score;
                    col = c;
                    row = r;
                    overwrite = p == BattleWorld.PlaceResult.NeedConfirm;
                }
            }
            return best;
        }
    }
}
#endif
