using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class GameFlow : MonoBehaviour
    {
        enum Screen { Lobby, Intro, Battle, Draft, Place, Confirm, Reveal, Result }

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
        // 金币够了自动弹改装。够了先等一小下再弹，免得刚捡到钱画面就被盖住；
        // 玩家自己关掉改装框，说明这会儿想攒钱，静默一阵再弹。
        float _autoWait;
        float _autoMute;
        const float AutoDelay = 0.35f;
        const float AutoMuteAfterClose = 6f;
        string _tip;
        // 第一关：字放下之后才提示怎么瞄准，手指一滑就收掉。
        bool _teachAim;
        const string AimTip = "按住底部左右滑，对准敌人";
        Transform _overlay;
        int _shotPage = -1;
        readonly bool[] _shotGot = new bool[4];
        readonly Queue<string> _codexToasts = new Queue<string>();
        readonly Queue<CodexEntry> _discoveries = new Queue<CodexEntry>();
        float _audioSweep;

        void Start()
        {
            WxBridge.InitSdk(() =>
            {
                AdStub.Warm();
                CloudSync.Startup(Begin);
            });
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
            WxBridge.OnHide(NoteLeft);
            WxBridge.OnShow(NoteBack);
            ShowHome();
            if (BattleWorld.PreviewFill) StartStage(0);
        }

        void OnApplicationPause(bool paused)
        {
            // 编辑器一切出游戏视图就会暂停，不在这里弹设置。真机切后台走微信的 OnHide。
            if (Application.isEditor) return;
            if (paused) NoteLeft();
            else NoteBack();
        }

        void Update()
        {
            if (_canvas == null) return;
            if (_leftGame)
            {
                _leftGame = false;
                PauseForReturn();
            }
            ScreenFit.Apply(Camera.main, _canvas);
            AudioBus.Duck(_screen == Screen.Draft || _screen == Screen.Confirm || _screen == Screen.Reveal || _screen == Screen.Result);
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
            if (_screen == Screen.Intro || _screen == Screen.Battle || _screen == Screen.Place || _screen == Screen.Draft
                || _screen == Screen.Confirm || _screen == Screen.Reveal)
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
                    // 切后台再回来，这一帧的 deltaTime 能到几十秒。按这个往前算，
                    // 波次和道具时长会一下跳完，看起来就是卡住。
                    _world.Tick(Mathf.Min(Time.deltaTime, 0.05f));
                    PumpCodexToast();
                    PumpDiscovery();
                    TickAutoDraft();
                }
                if (_world != null && _view != null)
                {
                    _view.Sync(_world);
                    if (_screen == Screen.Place) PaintPlaceHighlights();
                }
                // 先把最后一帧画出来再结算。原先胜负一成立就 return，
                // 死亡当帧的画面被跳过，怪还站在场上通关页就盖上来了。
                if (_world != null && _screen == Screen.Battle && !_askingRetreat)
                {
                    if (_world.Victory) { Finish(true); return; }
                    if (_world.Defeat) { Finish(false); return; }
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

        bool _askingRetreat;
        bool _leftGame;

        // 切出去、锁屏、被盖住。回来时设置页已经在上面，点继续才接着打。
        void NoteLeft()
        {
            _leftGame = true;
            PauseForReturn();
        }

        void NoteBack() => _leftGame = true;

        void PauseForReturn()
        {
            if (_world == null || _hud == null || _askingRetreat) return;
            if (_screen == Screen.Lobby || _screen == Screen.Result) return;
            OpenSettings();
        }

        // 战斗里的设置：音乐、音效、继续，或者撤退。体力进关就扣了，撤退不退。
        void OpenSettings()
        {
            if (_world == null || _askingRetreat || _hud == null) return;
            if (_screen == Screen.Lobby) return;
            _askingRetreat = true;
            bool wasPaused = _world.Paused;
            _world.Paused = true;
            var dim = UiKit.Dimmer(_layer);
            dim.name = "settings";
            void Stay()
            {
                if (!_askingRetreat) return;
                AudioBus.Tap();
                _askingRetreat = false;
                if (_world != null) _world.Paused = wasPaused;
                if (dim != null) Destroy(dim.gameObject);
            }
            void Leave()
            {
                if (!_askingRetreat) return;
                AudioBus.Tap();
                _askingRetreat = false;
                if (dim != null) Destroy(dim.gameObject);
                ShowHome();
            }
            var board = PanelKit.Board(dim, "设置", Vector2.zero, new Vector2(520f, 440f), Pin.Center, Stay);

            UiKit.Btn(board, "stay", "继续游戏", new Vector2(0f, 108f), new Vector2(400f, 92f), Stay, true, Pin.Top);
            UiKit.Btn(board, "leave", "撤退", new Vector2(0f, 214f), new Vector2(400f, 80f), Leave, false, Pin.Top);
            var note = UiKit.Label(board, "note", "撤退后没有奖励，体力也不退还", 22, new Vector2(0f, 316f),
                new Vector2(440f, 32f), TextAnchor.MiddleCenter, Pin.Top);
            note.color = InkTheme.TextMid;

            AudioPair(board, "音乐", "music", -115f, 20f, () => AudioBus.MusicOn, v => AudioBus.MusicOn = v);
            AudioPair(board, "音效", "sfx", 115f, 20f, () => AudioBus.SfxOn, v => AudioBus.SfxOn = v);
        }

        static void AudioPair(RectTransform board, string title, string name, float x, float y,
            System.Func<bool> get, System.Action<bool> set)
        {
            var label = UiKit.Label(board, name + "l", title, 24, new Vector2(x - 48f, y),
                new Vector2(72f, 46f), TextAnchor.MiddleRight, Pin.Bottom);
            UiKit.Bold(label);
            Switch(board, name, new Vector2(x + 40f, y), new Vector2(88f, 46f), get, set);
        }

        static void Switch(RectTransform board, string name, Vector2 pos, Vector2 size,
            System.Func<bool> get, System.Action<bool> set)
        {
            Button btn = null;
            btn = UiKit.Btn(board, name, get() ? "开" : "关", pos, size, () =>
            {
                bool next = !get();
                set(next);
                PaintSwitch(btn, next);
            }, false, Pin.Bottom);
            var label = btn.GetComponentInChildren<Text>();
            if (label != null) label.fontSize = 22;
            PaintSwitch(btn, get());
        }

        static void PaintSwitch(Button btn, bool on)
        {
            if (on) UiKit.PaintBtn(btn, InkTheme.Accel, InkTheme.Hex("1E7A42"), InkTheme.CardFace);
            else UiKit.PaintBtn(btn, InkTheme.CtaOff, InkTheme.Hex("5E5A54"), InkTheme.CardFace);
            var label = btn != null ? btn.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = on ? "开" : "关";
        }

        void ShowHome(int tab)
        {
            _meta.StorePending();
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
            _meta.StorePending();
            if (!BattleWorld.PreviewFill)
            {
                int cost = _meta.StageCost(index);
                if (!_meta.CanEnter(index)) return;
                _meta.SpendStamina(cost);
            }
            _home = null;
            _pickStage = index;
            _taughtStar = false;
            _teachAim = index == 0;
            _tip = "";
            _world = new BattleWorld();
            _world.ItemRanks = _meta.ItemLevel;
            _world.Begin(StageCatalog.Get(index), _meta.Forged, _meta.Equipped);
            _world.ApplySkin(_meta.Skin);
            _world.CodexHit = OnCodex;
            _codexToasts.Clear();
            _discoveries.Clear();
            if (_view != null) _view.Dispose();
            _view = new BattleView(null);
            _view.SetBackdrop(_world.Stage.Chapter);
            _view.EmitterSkin = _meta.Skin;
            _view.EmitterTint = _meta.SkinTint;
            BuildBattleHud();
            _autoWait = 0f;
            _autoMute = 0f;
            AudioBus.Music("bgm_battle");
            if (BattleWorld.PreviewFill)
            {
                _screen = Screen.Battle;
                return;
            }
            _screen = Screen.Intro;
            BattleBanner.Show(_layer, () =>
            {
                if (_world == null || _screen != Screen.Intro) return;
                _screen = Screen.Battle;
            });
        }

        void TickAutoDraft()
        {
            if (_askingRetreat || _world == null || BattleWorld.PreviewFill || _world.Victory || _world.Defeat) return;
            if (_screen != Screen.Battle) return;
            float dt = Time.unscaledDeltaTime;
            if (_autoMute > 0f) _autoMute -= dt;
            if (!_world.CanDraft || _autoMute > 0f)
            {
                _autoWait = 0f;
                return;
            }
            // 正按着滑炮台的时候不弹，否则松手那一下会点到牌上。
            if (_dragging || InkPointer.Held) return;
            _autoWait += dt;
            if (_autoWait < AutoDelay) return;
            _autoWait = 0f;
            OpenDraft();
        }

        void OnCodex(CodexKind kind, int index)
        {
            if (_world == null || !_meta.CodexLearn(kind, index)) return;
            if (kind == CodexKind.Enemy)
            {
                _codexToasts.Enqueue($"墨谱新页 · {CodexCatalog.Title(kind, index)}");
                return;
            }
            _discoveries.Enqueue(new CodexEntry(kind, index));
        }

        // 新字、新词、隐藏组合第一次打出来时弹卡。只在纯战斗态弹：
        // 抽牌、摆字、确认、撤退询问时都先排着，等回到战斗再弹，一次一张。
        void PumpDiscovery()
        {
            if (_discoveries.Count == 0 || _askingRetreat || _dragging || InkPointer.Held) return;
            if (_world.Victory || _world.Defeat) return;
            CodexEntry e = _discoveries.Dequeue();
            bool wasPaused = _world.Paused;
            _world.Paused = true;
            _screen = Screen.Reveal;
            DiscoveryCard.Show(_layer, e.Kind, e.Index, () =>
            {
                if (_world == null || _screen != Screen.Reveal) return;
                _world.Paused = wasPaused;
                _screen = Screen.Battle;
            });
        }

        // 和成词、升星提示共用一条 toast，排队等上一条放完，免得互相顶掉。
        void PumpCodexToast()
        {
            if (_codexToasts.Count == 0 || _world.ToastTime > 0f) return;
            _world.ShowToast(_codexToasts.Dequeue(), 1.8f);
        }

        void BuildBattleHud()
        {
            ClearLayer();
            _hud = BattleHud.Build(_layer, _world, OpenSettings, () =>
            {
                if (_screen != Screen.Battle || !_world.CanDraft) return;
                OpenDraft();
            }, () =>
            {
                if (_screen != Screen.Battle || _world == null) return;
                if (_world.EmitterCount >= GameConstants.AdEmitterCap) return;
                AdStub.Reward("emitter", () =>
                {
                    if (_world != null) _world.AddEmitter();
                });
            });
            _hud.GunSkin = _meta.Skin;
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
            {
                _dragging = true;
                if (_teachAim)
                {
                    _teachAim = false;
                    if (_tip == AimTip) _tip = "";
                }
            }
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

        // 后期牌池有二十多个字，均匀抽几乎凑不出同字升星，也很难第一时间摸到本关的新字。
        // 本关新字没上过盘时最重；盘上已有的字次之，方便升星；成词另一半在盘上时也抬高。
        int OfferWeight(CardId id)
        {
            CardDef def = CardCatalog.Get(id);
            bool onBoard = HasSameOnBoard(id);
            if (!onBoard && IsFresh(id)) return 26;
            if (def.Wake == CardWake.WordPart)
            {
                CardId mate = CardCatalog.Partner(id);
                if (mate != CardId.None && HasSameOnBoard(mate)) return 22;
                return onBoard ? 12 : 6;
            }
            return onBoard ? 16 : 10;
        }

        bool IsFresh(CardId id)
        {
            CardId[] fresh = _world.Stage.Fresh;
            if (fresh == null) return false;
            for (int i = 0; i < fresh.Length; i++)
                if (fresh[i] == id) return true;
            return false;
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
            string title = _offer != null && _offer.Length <= 1 ? "点下面的字" : "";
            _overlay = DraftPanel.Show(_layer, title, _offer, _rerolled, Pick, () =>
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
            _autoMute = AutoMuteAfterClose;
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
                _world.DenyCell(col, row);
                _world.ShowToast("这格还没开");
                return;
            }
            if (result == BattleWorld.PlaceResult.RejectedMaxStar)
            {
                _world.DenyCell(col, row);
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
            AfterPlace();
        }

        void ShowConfirm(int col, int row)
        {
            DropOverlay();
            _overlay = ConfirmPanel.Show(_layer, () =>
            {
                _world.Place(_held, col, row, true);
                AfterPlace();
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

        void AfterPlace()
        {
            _tip = _teachAim ? AimTip : "";
            ResumeBattle();
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
                RankService.Submit(_meta.ClearedCount());
                ShowVictory(_pickStage, _result);
            }
            else if (_world.RevivesUsed < GameConstants.MaxRevives) ShowRevive();
            else ShowDefeat(_pickStage, _world.Progress);
        }

        void ShowVictory(int stage, ResultInfo info, bool preview = false)
        {
            DropOverlay();
            int next = stage + 1;
            bool hasNext = next < GameConstants.StageCount && (preview || _meta.Unlocked(next));
            // 一个广告同时管墨翻倍和当场开这关的宝箱，宝箱位满了或今天开箱广告用完就只翻倍。
            bool canInk = info.Ink > 0;
            bool canChest = !preview && info.Chest >= 0 && _meta.HasPendingChest;
            VictoryPanel panel = null;
            panel = VictoryPanel.Show(_layer, new VictoryArgs
            {
                Stage = stage,
                Info = info,
                NextCost = hasNext ? _meta.StageCost(next) : -1,
                NextAffordable = hasNext && (preview || _meta.CanEnter(next)),
                Next = preview ? (System.Action)ShowHome : () => StartStage(next),
                DoubleInk = !canInk && !canChest ? (System.Action)null : () =>
                {
                    if (_inkDoubled) return;
                    AdStub.Reward("double", () =>
                    {
                        if (_inkDoubled) return;
                        _inkDoubled = true;
                        if (canInk && !preview) _meta.AddInk(info.Ink);
                        ChestLoot loot = canChest ? _meta.OpenPending() : null;
                        if (loot == null)
                        {
                            if (panel != null) panel.Doubled(false);
                            return;
                        }
                        // 先开箱，收下之后再回结算页滚翻倍的墨，两段演出不抢画面。
                        ChestOpenView.Show(_layer, _meta, loot, () => { if (panel != null) panel.Doubled(true); });
                    });
                },
                DoubleText = canInk && canChest ? "墨翻倍 + 开宝箱" : canInk ? "墨翻倍" : "立即开宝箱",
                Home = ShowHome,
                Forge = () => ShowHome(HomeScreen.TabForge),
                Skin = skin =>
                {
                    ShowHome(HomeScreen.TabForge);
                    _home?.FocusSkin(skin);
                }
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
                    Ink = 86, DailyDouble = true, Diamonds = 5,
                    Chest = (int)ChestTier.Gold, ChestFull = true, ChestInk = 50, Stars = 3, NewBest = true, FirstClear = true,
                    NewSkins = new[] { SkinCatalog.Flame, SkinCatalog.Lucky },
                    NewLines = new[] { (int)ForgeLine.BaseHp }
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
