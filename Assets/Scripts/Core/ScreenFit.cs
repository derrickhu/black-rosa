using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public static class ScreenFit
    {
        public const float DesignW = 720f;
        public const float DesignH = 1280f;

        public static float TopPad { get; private set; }
        public static float BottomPad { get; private set; }

        // 画布的真实尺寸，单位和 DesignW/DesignH 一样。
        // 别拿 DesignH 当页面高度用 —— 竖屏手机比 720x1280 更长，
        // CanvasScaler 是按宽匹配的，画布在 19.5:9 的机子上有 1558 个单位高。
        // 按 1280 去算「排完还剩多少」，底下会白留将近 280 个单位。
        public static float CanvasW { get; private set; } = DesignW;
        public static float CanvasH { get; private set; } = DesignH;

        public static void Apply(Camera cam, Canvas canvas)
        {
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            float aspect = w / h;
            float needHalfH = 7.4f;
            float needHalfW = GameConstants.Columns * GameConstants.CellWidth * 0.5f + 0.7f;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(needHalfH, needHalfW / aspect);
                cam.backgroundColor = InkTheme.Stage;
            }

            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(DesignW, DesignH);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = aspect < 0.7f ? 0f : 1f;
                }
            }

            Rect sa = Screen.safeArea;
            float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
            CanvasW = w / scale;
            CanvasH = h / scale;
            TopPad = (h - sa.yMax) / scale;
            BottomPad = sa.yMin / scale;

            // 团结导出的微信包里 Screen.safeArea 不一定填得对 —— 刘海机上常常退化成整屏，
            // 于是内缩算出来是 0，顶栏贴着刘海、底栏压在小白条上。微信自己报的安全区更准，
            // 两边取大的那个。
            Vector2 inset = WxBridge.SafeInsetFrac();
            if (inset.x >= 0f) TopPad = Mathf.Max(TopPad, inset.x * CanvasH);
            if (inset.y >= 0f) BottomPad = Mathf.Max(BottomPad, inset.y * CanvasH);

            if (TopPad < 36f) TopPad = 36f;
            if (BottomPad < 24f) BottomPad = 24f;

            // 胶囊按钮压在右上角，safeArea 管不到它。三个数值药丸横着要 570 个单位，
            // 720 宽里挪不出胶囊那 200 来个单位的位置，所以整条顶栏让到它下沿之下。
            // 小游戏基本都是这么处理的，让出来的是高度不是功能。
            float frac = WxBridge.CapsuleBottomFrac();
            if (frac > 0f) TopPad = Mathf.Max(TopPad, frac * CanvasH + 12f);
        }
    }
}
