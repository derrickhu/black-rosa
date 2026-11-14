using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 局内暂停设置弹窗（Figma Panel/其他弹窗/05_设置）：
    // 预制体 Prefabs/Settings，主视觉突出「继续游戏」（明黄大号）与「撤退」（红色大号），底部弱化承托音乐 / 音效开关。
    public sealed class SettingsPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/Settings";
        public const string Mark = "settings_figma_v1";

        const float BoardW = 560f;
        const float BoardH = 468f;

        public static RectTransform Show(RectTransform layer, string leaveNote, Action onStay, Action onLeave)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "settings", Mark, onStay, BuildPrefab, out RectTransform stage);
            var board = stage.Find("board") as RectTransform;

            ModalKit.BindClose(board, onStay);
            ModalKit.BindBtn(board, "stay", onStay);
            ModalKit.BindBtn(board, "leave", onLeave);

            var noteTxt = board.Find("note")?.GetComponent<Text>();
            if (noteTxt != null && !string.IsNullOrEmpty(leaveNote))
                noteTxt.text = leaveNote;

            BindAudio(board, "music", () => AudioBus.MusicOn, v => AudioBus.MusicOn = v);
            BindAudio(board, "sfx", () => AudioBus.SfxOn, v => AudioBus.SfxOn = v);

            PanelKit.Open(board);
            return dim;
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "settings", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "设置", null, null, null, null,
                () => { }, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            var stayBtn = ModalKit.HexBtn(board, "lg_yellow", 132f, 112f, "继续游戏", null, out _);
            stayBtn.gameObject.name = "stay";

            var leaveBtn = ModalKit.HexBtn(board, "lg_red", 132f, 226f, "撤退", null, out _);
            leaveBtn.gameObject.name = "leave";

            ModalKit.SolidText(board, "note", "撤退后没有奖励，体力也不退还", 16, 28f, 334f, 504f, 24f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, false);

            ModalKit.Divider(board, "div", 44f, 374f, 472f, 2f, false);

            BuildAudioPair(board, "音乐", "music", 64f, 126f);
            BuildAudioPair(board, "音效", "sfx", 302f, 364f);
            return dim;
        }

        static void BuildAudioPair(RectTransform board, string title, string name, float labelX, float btnX)
        {
            ModalKit.SolidText(board, name + "l", title, 20, labelX, 400f, 52f, 28f,
                ModalKit.TextDark, TextAnchor.MiddleCenter, true);
            var btn = ModalKit.HexBtn(board, "sm_green", btnX, 392f, "开", null, out _);
            btn.gameObject.name = name;
        }

        static void BindAudio(RectTransform board, string name, Func<bool> get, Action<bool> set)
        {
            var btn = board.Find(name)?.GetComponent<Button>();
            if (btn == null) return;
            var label = btn.transform.Find("t")?.GetComponent<Text>();
            PaintSwitch(btn, label, get());
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                AudioBus.Tap();
                bool next = !get();
                set(next);
                PaintSwitch(btn, label, next);
            });
        }

        static void PaintSwitch(Button btn, Text label, bool on)
        {
            ModalKit.SetHexBtnGray(btn, "sm_green", "sm_gray", !on);
            if (label != null) label.text = on ? "开" : "关";
        }
    }
}
