using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 通用二次确认弹窗（预制体 Prefabs/Confirm）：
    // 复用标准 Panel/信息面板 与两枚并排中号尖角按钮，替代旧的程序化粗描边矩形。
    public static class ConfirmPanel
    {
        public const string PrefabPath = "Prefabs/Confirm";
        public const string Mark = "confirm_figma_v1";

        const float BoardW = 540f;
        const float BoardH = 340f;

        public static RectTransform Show(RectTransform layer, Action yes, Action no)
        {
            return Spawn(layer, "覆盖这格？星级会清零。", "取消", "md_gray", no, "覆盖", "md_yellow", yes);
        }

        // keep 是建议保留的一边（左，明黄）；drop 是会放弃或继续执行的一边（右，青/灰）。
        public static void Ask(RectTransform layer, string text, string keep, string drop, Action onDrop)
        {
            Spawn(layer, text, keep, "md_yellow", null, drop, "md_gray", onDrop);
        }

        static RectTransform Spawn(RectTransform layer, string text,
            string leftLabel, string leftKind, Action onLeft,
            string rightLabel, string rightKind, Action onRight)
        {
            RectTransform dim = null;
            void Shut()
            {
                if (dim != null) UnityEngine.Object.Destroy(dim.gameObject);
            }

            dim = ModalKit.SpawnPrefab(layer, PrefabPath, "confirm", Mark, null, BuildPrefab, out RectTransform stage);
            var board = stage.Find("board") as RectTransform;

            var msg = board.Find("msg")?.GetComponent<Text>();
            if (msg != null) msg.text = text ?? "";

            var leftBtn = board.Find("btn_left")?.GetComponent<Button>();
            if (leftBtn != null)
            {
                ModalKit.SetHexBtnGray(leftBtn, leftKind, "md_gray", leftKind == "md_gray");
                ModalKit.SetBtnLabel(leftBtn, leftKind, leftLabel);
                leftBtn.onClick.RemoveAllListeners();
                leftBtn.onClick.AddListener(() =>
                {
                    AudioBus.Tap();
                    Shut();
                    onLeft?.Invoke();
                });
            }

            var rightBtn = board.Find("btn_right")?.GetComponent<Button>();
            if (rightBtn != null)
            {
                ModalKit.SetHexBtnGray(rightBtn, rightKind, "md_gray", rightKind == "md_gray");
                ModalKit.SetBtnLabel(rightBtn, rightKind, rightLabel);
                rightBtn.onClick.RemoveAllListeners();
                rightBtn.onClick.AddListener(() =>
                {
                    AudioBus.Tap();
                    Shut();
                    onRight?.Invoke();
                });
            }

            PanelKit.Open(board);
            return dim;
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "confirm", 0.80f, null, out RectTransform stage);
            var board = ModalKit.Shell(stage, BoardW, BoardH, "提示", null, null, null, null,
                null, out _, out _, animate: false);
            ModalKit.AddMark(board, Mark);

            var msg = ModalKit.SolidText(board, "msg", "", 20, 36f, 108f, BoardW - 72f, 118f,
                ModalKit.TextDark, TextAnchor.MiddleCenter, true);
            msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            msg.lineSpacing = 1.25f;

            // (540 - (176 + 28 + 176)) / 2 = 80
            var left = ModalKit.HexBtn(board, "md_yellow", 80f, 246f, "取消", null, out _);
            left.gameObject.name = "btn_left";

            var right = ModalKit.HexBtn(board, "md_gray", 284f, 246f, "确定", null, out _);
            right.gameObject.name = "btn_right";

            return dim;
        }
    }
}
