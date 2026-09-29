using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class GameFlow : MonoBehaviour
    {
        enum Screen { Lobby, Battle, Draft, Place, Confirm, Result }

        MetaProgress _meta;
        BattleWorld _world;
        BattleView _view;
        BattleHud _hud;
        HomeScreen _home;
        Canvas _canvas;
        RectTransform _layer;
        Screen _screen = Screen.Lobby;
        int _pickStage;
        ResultInfo _result;
        bool _inkDoubled;
        CardId[] _offer = new CardId[3];
        CardId _held;
        bool _rerolled;
        int _draftPaid;
        bool _taughtStar;
        bool _dragging;
        string _tip;
        Transform _overlay;
        int _shotPage = -1;
        readonly bool[] _shotGot = new bool[4];
        readonly Queue<string> _codexToasts = new Queue<string>();
        float _audioSweep;

        void Start()
        {
            WxBridge.InitSdk(() => CloudSync.Startup(Begin));
            CloudSync.Imported += OnCloudImported;
        }

        void OnDestroy()
        {
            CloudSync.Imported -= OnCloudImported;
        }

        // 进大厅后才到的云端档（启动超时后晚到、或上行被 409 打回）。
        // 战斗里只换存档不打断，结算时就记到新档上。
        void OnCloudImported()
        {
            if (_canvas == null) return;
            _meta = MetaProgress.Load();
            if (_screen == Screen.Lobby) ShowHome();
        }

        void Begin()
        {
            if (_canvas != null) return;
            _meta = MetaProgress.Load();
            _canvas = UiKit.CreateCanvas("InkUI");
            _layer = _canvas.GetComponent<RectTransform>();
            GmBar.Preview = GmPreview;
            ScreenFit.Apply(Camera.main, _canvas);
            string force = Path.Combine(Application.dataPath, "../Library/ink_shot_preview.force");
            if (File.Exists(force))
                BattleWorld.PreviewFill = true;
            ShowHome();
            if (BattleWorld.PreviewFill) StartStage(0);
        }

        void Update()
        {
            if (_canvas == null) return;
            ScreenFit.Apply(Camera.main, _canvas);
            AudioBus.Duck(_screen == Screen.Draft || _screen == Screen.Confirm || _screen == Screen.Result);
            AudioBus.Tick();
            _audioSweep -= Time.unscaledDeltaTime;
            if (_audioSweep <= 0f)
            {
                _audioSweep = 0.35f;
                AudioBus.Sweep(_layer);
            }
            InkPointer.Pump();
            bool uiHit = EventSystemOverUi();
            if (_screen == Screen.Lobby && _home != null) _home.Tick();
            if (_screen == Screen.Battle || _screen == Screen.Place || _screen == Screen.Draft || _screen == Screen.Confirm)
            {
                if (_world != null && _hud != null)
                {
                    // 掉落物要飞进顶栏，得先知道两个药丸现在在世界的哪儿 ——
                    // 安全区一变（转屏、不同机型）位置就不一样，不能写死。
                    _world.GoldChip = BattleHud.ChipInWorld(_hud.GoldChip, _world.GoldChip);
                    _world.InkChip = BattleHud.ChipInWorld(_hud.InkChip, _world.InkChip);
                }
                if (_world != null && _screen == Screen.Battle)
                {
                    if (!uiHit) HandleRail();
                    _world.Tick(Time.deltaTime);
                    PumpCodexToast();
                    if (_world.Victory) { Finish(true); return; }
                    if (_world.Defeat) { Finish(false); return; }
                }
                if (_world != null && _view != null)
                {
                    _view.Sync(_world);
                    if (_screen == Screen.Place) PaintPlaceHighlights();
                }
                if (_hud != null)
                {
                    if (BattleWorld.PreviewFill && _world != null && !string.IsNullOrEmpty(_world.PreviewLabel))
                        _tip = _world.PreviewLabel;
                    _hud.Refresh(_world, _screen == Screen.Battle, _tip);
                }
                if (BattleWorld.PreviewFill && _world != null && _screen == Screen.Battle)
                    TryCapturePreview();
                if (!uiHit && _screen == Screen.Place) HandlePlaceClick();
            }
        }

        void ReloadSave()
        {
            _meta = MetaProgress.Load();
            ShowHome();
        }

        void ShowHome() => ShowHome(HomeScreen.TabSortie);

        void ShowHome(int tab)
        {
            _screen = Screen.Lobby;
            ClearLayer();
            if (_view != null) { _view.Dispose(); _view = null; }
            _world = null;
            _hud = null;
            BattleHud.ReleaseCamera();
            _home = HomeScreen.Build(_layer, _meta, StartStage, ReloadSave, tab);
            AudioBus.Music("bgm_home");
            AudioBus.Warm("bgm_battle");
        }

        void StartStage(int index)
        {
            if (!BattleWorld.PreviewFill)
            {
                int cost = _meta.StageCost(index);
                if (!_meta.CanEnter(index)) return;
                _meta.SpendStamina(cost);
            }
            _home = null;
            _pickStage = index;
            _taughtStar = false;
            _tip = index == 0 ? "滑到底下那一串，对准敌人。" : "";
            _world = new BattleWorld();
            _world.Begin(StageCatalog.Get(index), _meta.Forged, _meta.Equipped);
            _world.ApplySkin(_meta.Skin);
            _world.SpellRanks = _meta.SpellLevel;
            _world.OfferShards(_meta.SpellLevel, _meta.SpellShards);
            _world.CodexHit = OnCodex;
            _codexToasts.Clear();
            if (_view != null) _view.Dispose();
            _view = new BattleView(null);
            _view.SetBackdrop(_world.Stage.Chapter);
            _view.EmitterSkin = _meta.Skin;
            _view.EmitterTint = _meta.SkinTint;
            BuildBattleHud();
            _screen = Screen.Battle;
            AudioBus.Music("bgm_battle");
        }

        void OnCodex(CodexKind kind, int index)
        {
            if (_world == null || !_meta.CodexLearn(kind, index)) return;
            string head = kind == CodexKind.Pair ? "秘卷现世"
                : kind == CodexKind.Enemy ? "墨谱新页" : "图鉴收录";
            _codexToasts.Enqueue($"{head} · {CodexCatalog.Title(kind, index)}");
        }

        // 和关卡规则、成词提示共用一条 toast，排队等上一条放完，免得互相顶掉。
        void PumpCodexToast()
        {
            if (_codexToasts.Count == 0 || _world.ToastTime > 0f) return;
            _world.ShowToast(_codexToasts.Dequeue(), 1.8f);
        }

        void BuildBattleHud()
        {
            ClearLayer();
            _hud = BattleHud.Build(_layer, _world, ShowHome, () =>
            {
                if (_screen != Screen.Battle || !_world.CanDraft) return;
                OpenDraft();
            }, slot =>
            {
                if (_screen == Screen.Battle) _world.CastSpell(slot);
            });
        }

        static bool EventSystemOverUi()
        {
            if (EventSystem.current == null || !InkPointer.Down && !InkPointer.Held) return false;
            var ped = new PointerEventData(EventSystem.current) { position = InkPointer.ScreenPos };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                var g = hits[i].gameObject.GetComponent<Graphic>();
                if (g != null && g.raycastTarget) return true;
                if (hits[i].gameObject.GetComponentInParent<Button>() != null) return true;
            }
            return false;
        }

        void HandleRail()
        {
            if (InkPointer.Down && InRailZone(InkPointer.WorldOnPlane()))
                _dragging = true;
            if (_dragging && InkPointer.Held)
                _world.SetRailFromWorldX(InkPointer.WorldOnPlane().x, false);
            if (_dragging && InkPointer.Up)
            {
                _world.SetRailFromWorldX(InkPointer.WorldOnPlane().x, true);
                _dragging = false;
            }
        }

        static bool InRailZone(Vector3 world)
        {
            return world.y < GameConstants.LeakY - 0.15f;
        }

        void OpenDraft()
        {
            if (!_world.CanDraft) return;
            _draftPaid = _world.DraftCost;
            _world.Gold -= _draftPaid;
            _world.DraftCount++;
            _world.Paused = true;
            _rerolled = false;
            RollOffer();
            _screen = Screen.Draft;
            ShowDraftPanel();
            AudioBus.Draft();
        }

        // 牌池不足三个字时就少发几张。第一关只有「分」一个字，发三张一样的看着像
        // 出了 bug。顺带修掉原来那句 `pool.Count > 3`：池子正好三个时它不去重，
        // 会发出重复项。
        void RollOffer()
        {
            List<CardId> pool = new List<CardId>(_world.Stage.Pool);
            var weight = new List<int>();
            for (int i = 0; i < pool.Count; i++)
                weight.Add(OfferWeight(pool[i]));
            int n = Mathf.Min(3, pool.Count);
            if (_offer == null || _offer.Length != n) _offer = new CardId[n];
            for (int i = 0; i < n; i++)
            {
                int pick = WeightedIndex(pool, weight);
                _offer[i] = pool[pick];
                pool.RemoveAt(pick);
                weight.RemoveAt(pick);
            }
        }

        int OfferWeight(CardId id)
        {
            CardDef def = CardCatalog.Get(id);
            if (def.Wake != CardWake.WordPart) return 10;
            CardId mate = CardCatalog.Partner(id);
            if (mate != CardId.None && HasSameOnBoard(mate)) return 22;
            return 6;
        }

        static int WeightedIndex(List<CardId> pool, List<int> weight)
        {
            int sum = 0;
            for (int i = 0; i < weight.Count; i++) sum += weight[i];
            int roll = Random.Range(0, Mathf.Max(1, sum));
            for (int i = 0; i < weight.Count; i++)
            {
                roll -= weight[i];
                if (roll < 0) return i;
            }
            return pool.Count - 1;
        }

        void ShowDraftPanel()
        {
            DropOverlay();
            _overlay = DraftPanel.Show(_layer, _tip, _offer, _rerolled, Pick, () =>
            {
                AdStub.Reward("reroll", () =>
                {
                    _rerolled = true;
                    RollOffer();
                    ShowDraftPanel();
                });
            }, CloseDraft);
        }

        void CloseDraft()
        {
            if (_world == null || _screen != Screen.Draft) return;
            _world.Gold += _draftPaid;
            _world.DraftCount = Mathf.Max(0, _world.DraftCount - 1);
            _tip = "";
            ResumeBattle();
            AudioBus.Back();
        }

        void Pick(CardId id)
        {
            _held = id;
            if (_world.Stage.TeachStar && !_taughtStar && HasSameOnBoard(id))
            {
                _taughtStar = true;
                _tip = "点空格铺开，或点已有同牌升到 2 星。";
            }
            else _tip = "点一个格子放下。";
            _screen = Screen.Place;
            DropOverlay();
        }

        bool HasSameOnBoard(CardId id)
        {
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < _world.OpenRows; r++)
                if (_world.Grid[c, r] == id) return true;
            return false;
        }

        void HandlePlaceClick()
        {
            if (!InkPointer.Down || Camera.main == null) return;
            Vector3 w = InkPointer.WorldOnPlane();
            if (!FieldLayout.TryCellAt(w, _world.OpenRows, out int col, out int row)) return;
            var result = _world.PeekPlace(_held, col, row);
            // 同一排里没开的格子也点得到（TryCellAt 只按行卡范围），得在这儿拦下来，
            // 否则会悄悄放进一个画成灰底的格子里。
            if (result == BattleWorld.PlaceResult.LockedRow)
            {
                AudioBus.Deny();
                _world.ShowToast("这格还没开");
                return;
            }
            if (result == BattleWorld.PlaceResult.RejectedMaxStar)
            {
                AudioBus.Deny();
                _world.ShowToast("已满星");
                return;
            }
            if (result == BattleWorld.PlaceResult.NeedConfirm)
            {
                _screen = Screen.Confirm;
                ShowConfirm(col, row);
                return;
            }
            _world.Place(_held, col, row, false);
            _tip = "";
            ResumeBattle();
        }

        void ShowConfirm(int col, int row)
        {
            DropOverlay();
            _overlay = ConfirmPanel.Show(_layer, () =>
            {
                _world.Place(_held, col, row, true);
                _tip = "";
                ResumeBattle();
            }, () =>
            {
                _screen = Screen.Place;
                DropOverlay();
            });
        }

        void PaintPlaceHighlights()
        {
            for (int c = 0; c < GameConstants.Columns; c++)
            for (int r = 0; r < GameConstants.Rows; r++)
            {
                // 没开的格子不画。能放的格子只留黑框，这里不再铺绿色。
                if (!_world.IsOpen(c, r)) continue;
                var p = _world.PeekPlace(_held, c, r);
                Color col = InkTheme.PlaceBad;
                if (p == BattleWorld.PlaceResult.Placed) col = InkTheme.PlaceOk;
                else if (p == BattleWorld.PlaceResult.Upgraded) col = InkTheme.PlaceUp;
                else if (p == BattleWorld.PlaceResult.RejectedMaxStar) col = new Color(0, 0, 0, 0.06f);
                _view.HighlightCell(c, r, col);
            }
        }

        void ResumeBattle()
        {
            DropOverlay();
            _world.Paused = false;
            _screen = Screen.Battle;
        }

        // 结算只算一次。翻倍广告只让面板把墨数再滚一遍，不要再回 Finish，
        // 否则 ApplyResult 会把这一局拾到的墨再入一次账。
        // 输了先给续命页；没得续、不续、续了又死，才放失败动画和失败页。
        void Finish(bool win)
        {
            _screen = Screen.Result;
            _world.Paused = true;
            _inkDoubled = false;
            if (win)
            {
                AudioBus.Win();
                _result = _meta.ApplyResult(_pickStage, _world.Ink, _world.StarsEarned);
                _meta.AddShards(_world.ShardGot);
                _result.Shards = Sum(_world.ShardGot);
                RankService.Submit(_meta.ClearedCount());
                ShowVictory(_pickStage, _result);
            }
            else if (_world.RevivesUsed < GameConstants.MaxRevives) ShowRevive();
            else ShowDefeat(_pickStage, _world.Progress);
        }

        static int Sum(int[] a)
        {
            int s = 0;
            if (a != null)
                for (int i = 0; i < a.Length; i++) s += Mathf.Max(0, a[i]);
            return s;
        }

        void ShowVictory(int stage, ResultInfo info, bool preview = false)
        {
            DropOverlay();
            int next = stage + 1;
            bool hasNext = next < GameConstants.StageCount && (preview || _meta.Unlocked(next));
            VictoryPanel panel = null;
            panel = VictoryPanel.Show(_layer, new VictoryArgs
            {
                Stage = stage,
                Info = info,
                NextCost = hasNext ? _meta.StageCost(next) : -1,
                NextAffordable = hasNext && (preview || _meta.CanEnter(next)),
                Next = preview ? (System.Action)ShowHome : () => StartStage(next),
                DoubleInk = info.Ink <= 0 ? (System.Action)null : () =>
                {
                    if (_inkDoubled) return;
                    AdStub.Reward("double", () =>
                    {
                        if (_inkDoubled) return;
                        _inkDoubled = true;
                        if (!preview) _meta.AddInk(info.Ink);
                        if (panel != null) panel.Doubled();
                    });
                },
                Home = ShowHome
            });
            _overlay = panel.Root;
        }

        void ShowRevive()
        {
            DropOverlay();
            int left = GameConstants.MaxRevives - _world.RevivesUsed;
            _overlay = RevivePanel.Show(_layer, _world.Progress, left, () =>
            {
                AdStub.Reward("revive", () =>
                {
                    if (_world == null || !_world.TryRevive()) return;
                    DropOverlay();
                    _screen = Screen.Battle;
                });
            }, () => ShowDefeat(_pickStage, _world != null ? _world.Progress : 0f)).Root;
        }

        void ShowDefeat(int stage, float progress, bool preview = false)
        {
            DropOverlay();
            _overlay = DefeatPanel.Show(_layer, new DefeatArgs
            {
                Stage = stage,
                Progress = progress,
                RetryCost = _meta.StageCost(stage),
                CanRetry = preview || _meta.CanEnter(stage),
                Advice = DefeatAdvice.Build(_meta),
                Retry = preview ? (System.Action)ShowHome : () => StartStage(stage),
                Improve = ShowHome,
                Home = ShowHome
            }).Root;
        }

        // GM 里预览三张结算页，不碰存档。
        void GmPreview(int kind)
        {
            if (_screen != Screen.Lobby) return;
            int stage = Mathf.Clamp(_meta.ClearedCount(), 0, GameConstants.StageCount - 2);
            _inkDoubled = false;
            if (kind == 0)
            {
                ShowVictory(stage, new ResultInfo
                {
                    Ink = 86, DailyDouble = true,
                    Shards = 1, FinaleShard = -1, Stars = 3, NewBest = true, FirstClear = true
                }, true);
            }
            else if (kind == 1)
            {
                DropOverlay();
                _overlay = RevivePanel.Show(_layer, 0.83f, GameConstants.MaxRevives,
                    DropOverlay, () => ShowDefeat(stage, 0.83f, true)).Root;
            }
            else ShowDefeat(stage, 0.64f, true);
        }

        void DropOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }

        void ClearLayer()
        {
            DropOverlay();
            for (int i = _layer.childCount - 1; i >= 0; i--)
                Destroy(_layer.GetChild(i).gameObject);
            _hud = null;
        }

        void TryCapturePreview()
        {
            int page = _world.PreviewPage;
            if (_world.PreviewAge < 2.2f) return;
            if (page == _shotPage) return;
            if (page < 0 || page >= _shotGot.Length) return;
            _shotPage = page;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../game_assets/black-rosa/美术/runtime/vfx/preview"));
            Directory.CreateDirectory(dir);
            DumpGame(Path.Combine(dir, "page_" + page + ".png"));
            _shotGot[page] = true;
            bool all = true;
            for (int i = 0; i < _shotGot.Length; i++)
                if (!_shotGot[i]) all = false;
            if (all)
                File.WriteAllText(Path.Combine(Application.dataPath, "../Library/ink_shot_preview.done"), "ok");
        }

        static void DumpGame(string path)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            const int w = 720;
            const int h = 1280;
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 24);
            RenderTexture prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply(false, false);
            cam.targetTexture = prev;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
