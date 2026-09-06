using UnityEngine;

namespace InkLine
{
    public static class FieldLayout
    {
        public static float FieldWidth => GameConstants.Columns * GameConstants.CellWidth;

        public static float ColumnX(int col)
        {
            float left = -FieldWidth * 0.5f + GameConstants.CellWidth * 0.5f;
            return left + col * GameConstants.CellWidth;
        }

        public static int ColumnAtX(float x)
        {
            float left = -FieldWidth * 0.5f;
            int col = Mathf.FloorToInt((x - left) / GameConstants.CellWidth);
            return Mathf.Clamp(col, 0, GameConstants.Columns - 1);
        }

        public static float GridBottom => RowY(0) - GameConstants.CellHeight * 0.5f;

        public static float GridTop => RowY(GameConstants.Rows - 1) + GameConstants.CellHeight * 0.5f;

        public static float RowY(int row)
        {
            float bottom = GameConstants.GridCenterY - (GameConstants.Rows - 1) * 0.5f * GameConstants.CellHeight;
            return bottom + row * GameConstants.CellHeight;
        }

        public static float RowApplyY(int row) => RowY(row) - GameConstants.CellHeight * 0.42f;

        public static Vector3 CellPos(int col, int row) => new Vector3(ColumnX(col), RowY(row), 0f);

        public static bool TryCellAt(Vector3 world, int openRows, out int col, out int row)
        {
            col = ColumnAtX(world.x);
            float bottom = RowY(0) - GameConstants.CellHeight * 0.5f;
            float top = RowY(openRows - 1) + GameConstants.CellHeight * 0.5f;
            if (world.y < bottom || world.y > top)
            {
                row = -1;
                return false;
            }
            row = Mathf.Clamp(Mathf.FloorToInt((world.y - bottom) / GameConstants.CellHeight), 0, openRows - 1);
            return Mathf.Abs(world.x - ColumnX(col)) <= GameConstants.CellWidth * 0.5f;
        }
    }
}
