using System.Collections.Generic;
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
        Canvas _canvas;
        RectTransform _layer;
        Screen _screen = Screen.Lobby;
        int _pickStage;
        CardId[] _offer = new CardId[3];
        CardId _held;
        bool _rerolled;
        int _autoDrafts;
        bool _taughtStar;
        bool _dragging;
        string _tip;
        Transform _overlay;

        void Start()
        {
            WxBridge.InitSdk(Begin);
        }

        void Begin()
        {
            if (_canvas != null) return;
            _meta = MetaProgress.Load();
            _canvas = UiKit.CreateCanvas("InkUI");
            _layer = _canvas.GetComponent<RectTransform>();
            ScreenFit.Apply(Camera.main, _canvas);
            ShowLobby();
        }

        void Update()
        {
            if (_canvas == null) return;
            ScreenFit.Apply(Camera.main, _canvas);
            InkPointer.Pump();
            bool uiHit = EventSystemOverUi();
            if (_screen == Screen.Battle || _screen == Screen.Place || _screen == Screen.Draft || _screen == Screen.Confirm)
            {
                if (_world != null && _screen == Screen.Battle)
                {
                    if (!uiHit) HandleRail();
                    _world.Tick(Time.deltaTime);
                    if (_world.CanDraft && _autoDrafts < AutoDraftLimit() && !(BattleWorld.PreviewFill && _world.BattleTime < 8f))
                    {
                        _autoDrafts++;
                        _tip = _autoDrafts == 1
                            ? "金币够了。选一张，点到格子上。"
                            : "又能改装了。铺开占线，或叠到同牌上升星。";
                        OpenDraft(true);
                    }
                    if (_world.Victory) { Finish(true); return; }
                    if (_world.Defeat) { Finish(false); return; }
                }
                if (_world != null && _view != null)
                {
                    _view.Sync(_world);
                    if (_screen == Screen.Place) PaintPlaceHighlights();
                }
                if (_hud != null) _hud.Refresh(_world, _screen == Screen.Battle, _tip);
                if (!uiHit && _screen == Screen.Place) HandlePlaceClick();
            }
        }

        void ShowLobby()
        {
            _screen = Screen.Lobby;
            ClearLayer();
            if (_view != null) { _view.Dispose(); _view = null; }
            _world = null;
            _hud = null;
            LobbyScreen.Build(_layer, _meta, StartStage);
        }

        void StartStage(int index)
        {
            _pickStage = index;
            _autoDrafts = 0;
            _taughtStar = false;
            _tip = index == 0 ? "滑到底下那一串，对准敌人。" : "";
            _world = new BattleWorld();
            _world.Begin(StageCatalog.Get(index), _meta.StartGold, _meta.StartEmitters);
            if (_view != null) _view.Dispose();
            _view = new BattleView(null);
            BuildBattleHud();
            _screen = Screen.Battle;
        }

        void BuildBattleHud()
        {
            ClearLayer();
            _hud = BattleHud.Build(_layer, ShowLobby, () =>
            {
                if (_screen != Screen.Battle || !_world.CanDraft) return;
                OpenDraft(false);
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

        int AutoDraftLimit() => _pickStage <= 1 ? 3 : 2;

        void OpenDraft(bool forced)
        {
            if (!_world.CanDraft && !forced) return;
            if (_world.Gold < _world.DraftCost) return;
            _world.Gold -= _world.DraftCost;
            _world.DraftCount++;
            _world.Paused = true;
            _rerolled = false;
            RollOffer();
            _screen = Screen.Draft;
            ShowDraftPanel();
        }

        void RollOffer()
        {
            List<CardId> pool = new List<CardId>(_world.Stage.Pool);
            for (int i = 0; i < 3; i++)
            {
                int n = Random.Range(0, pool.Count);
                _offer[i] = pool[n];
                if (pool.Count > 3) pool.RemoveAt(n);
            }
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
            });
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
            if (result == BattleWorld.PlaceResult.RejectedMaxStar)
            {
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
            for (int r = 0; r < _world.OpenRows; r++)
            {
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

        void Finish(bool win)
        {
            _screen = Screen.Result;
            _world.Paused = true;
            int stars = 0;
            if (win)
            {
                if (_world.RevivesUsed > 0) stars = 1;
                else if (_world.BaseHp <= 1) stars = 2;
                else stars = 3;
                _meta.ApplyResult(_pickStage, stars);
            }
            DropOverlay();
            bool next = win && _pickStage + 1 < GameConstants.ChapterStageCount && _meta.Unlocked(_pickStage + 1);
            _overlay = ResultPanel.Show(
                _layer,
                win,
                stars,
                !win && _world.RevivesUsed < GameConstants.MaxRevives,
                () =>
                {
                    AdStub.Reward("revive", () =>
                    {
                        if (_world.TryRevive())
                        {
                            DropOverlay();
                            _screen = Screen.Battle;
                        }
                    });
                },
                ShowLobby,
                next ? () => StartStage(_pickStage + 1) : (System.Action)null);
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
    }
}
