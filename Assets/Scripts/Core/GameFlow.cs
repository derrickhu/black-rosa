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
        int _stakedStamina;
        int _inkEarned;
        bool _inkDoubled;
        bool _resultWin;
        int _resultStars;
        CardId[] _offer = new CardId[3];
        CardId _held;
        bool _rerolled;
        int _autoDrafts;
        bool _taughtStar;
        bool _dragging;
        string _tip;
        Transform _overlay;
        int _shotPage = -1;
        readonly bool[] _shotGot = new bool[4];

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
                    if (_world.CanDraft && _autoDrafts < AutoDraftLimit() && !(BattleWorld.PreviewFill && _world.BattleTime < 40f))
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

        // 回到首页时把还押着的体力退回去。通关那条路径会先把押注清零，
        // 所以净效果是「只有通关才真的扣 2 点」，撤退、失败、续命再通关都算得对。
        void ShowHome()
        {
            _meta.RefundStamina(_stakedStamina);
            _stakedStamina = 0;
            _screen = Screen.Lobby;
            ClearLayer();
            if (_view != null) { _view.Dispose(); _view = null; }
            _world = null;
            _hud = null;
            _home = HomeScreen.Build(_layer, _meta, StartStage);
        }

        void StartStage(int index)
        {
            _stakedStamina = 0;
            if (!BattleWorld.PreviewFill)
            {
                int cost = _meta.StageCost(index);
                if (!_meta.CanEnter(index)) return;
                _meta.SpendStamina(cost);
                _stakedStamina = cost;
            }
            _home = null;
            _pickStage = index;
            _autoDrafts = 0;
            _taughtStar = false;
            _inkEarned = 0;
            _tip = index == 0 ? "滑到底下那一串，对准敌人。" : "";
            _world = new BattleWorld();
            _world.Begin(StageCatalog.Get(index), _meta.Forged, _meta.Equipped);
            if (_view != null) _view.Dispose();
            _view = new BattleView(null);
            _view.EmitterTint = _meta.SkinTint;
            BuildBattleHud();
            _screen = Screen.Battle;
        }

        void BuildBattleHud()
        {
            ClearLayer();
            _hud = BattleHud.Build(_layer, _world, ShowHome, () =>
            {
                if (_screen != Screen.Battle || !_world.CanDraft) return;
                OpenDraft(false);
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

        // 自动弹三选一是教学推力，不是发牌渠道。头几关只开两三格，弹多了没处放，
        // 反而是在教「字可以乱堆」。所以上限跟着这一关开放的格子数走。
        int AutoDraftLimit() => Mathf.Clamp(_world.Stage.OpenCount / 3, 1, 3);

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
            // 同一排里没开的格子也点得到（TryCellAt 只按行卡范围），得在这儿拦下来，
            // 否则会悄悄放进一个画成灰底的格子里。
            if (result == BattleWorld.PlaceResult.LockedRow)
            {
                _world.ShowToast("这格还没开");
                return;
            }
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
            for (int r = 0; r < GameConstants.Rows; r++)
            {
                // 没开的格子直接不碰。不能拿 Color.clear 去「清」它 ——
                // HighlightCell 把低 alpha 当成「无高亮」，会刷成纯白，
                // 反而让锁住的格子看起来是开的。它的灰底由每帧的 Sync 负责。
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

        // 结算只算一次。看完双倍广告走 ShowResult 重画面板，不要再回 Finish，
        // 否则 ApplyResult 会按「重刷」再发一笔墨。
        void Finish(bool win)
        {
            _screen = Screen.Result;
            _world.Paused = true;
            _resultWin = win;
            _resultStars = 0;
            _inkEarned = 0;
            _inkDoubled = false;
            if (win)
            {
                // 按丢了多少血算，不是按剩多少血。否则局外加基地血会自动放水。
                int lost = _world.MaxBaseHp - _world.BaseHp;
                _resultStars = _world.RevivesUsed > 0 ? 1 : (lost <= 1 ? 3 : 2);
                _inkEarned = _meta.ApplyResult(_pickStage, _resultStars);
                _stakedStamina = 0;
            }
            ShowResult();
        }

        void ShowResult()
        {
            DropOverlay();
            bool next = _resultWin
                        && _pickStage + 1 < GameConstants.ChapterStageCount
                        && _meta.Unlocked(_pickStage + 1);
            int shownInk = _inkDoubled ? _inkEarned * 2 : _inkEarned;
            _overlay = ResultPanel.Show(
                _layer,
                _resultWin,
                _resultStars,
                shownInk,
                !_resultWin && _world.RevivesUsed < GameConstants.MaxRevives,
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
                _resultWin && _inkEarned > 0 && !_inkDoubled
                    ? () => AdStub.Reward("double", () =>
                    {
                        _meta.AddInk(_inkEarned);
                        _inkDoubled = true;
                        ShowResult();
                    })
                    : (System.Action)null,
                ShowHome,
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
