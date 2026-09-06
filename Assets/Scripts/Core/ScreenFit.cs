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
                cam.backgroundColor = InkTheme.Paper;
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
            TopPad = (h - sa.yMax) / scale;
            BottomPad = sa.yMin / scale;
            if (TopPad < 36f) TopPad = 36f;
            if (BottomPad < 24f) BottomPad = 24f;
        }
    }
}
