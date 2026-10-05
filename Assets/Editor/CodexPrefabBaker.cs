using UnityEditor;
using UnityEngine;

namespace InkLine
{
    // 图鉴主页和详情两张预制。只挂工程里的 Sprite，格子里的字图 / 敌人图运行时再填。
    public static class CodexPrefabBaker
    {
        const string Main = "Assets/Resources/Prefabs/Codex.prefab";
        const string Detail = "Assets/Resources/Prefabs/CodexDetail.prefab";

        [MenuItem("墨字防线/烘图鉴预制")]
        public static void BakeMenu()
        {
            if ((System.IO.File.Exists(Main) || System.IO.File.Exists(Detail)) &&
                !EditorUtility.DisplayDialog(
                    "重烘会覆盖微调",
                    "图鉴预制已经在。重烘会按 CodexLayout 重新生成，编辑器里拖过的位置和尺寸都会被盖掉。",
                    "仍然重烘",
                    "取消"))
                return;
            Bake();
        }

        public static void Bake()
        {
            Save(CodexLayout.BuildMain(null), Main);
            Save(CodexLayout.BuildDetail(null), Detail);
            AssetDatabase.SaveAssets();
        }

        static void Save(RectTransform root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, path, out bool saved);
            Object.DestroyImmediate(root.gameObject);
            if (saved) Debug.Log("图鉴预制已写入 " + path);
            else Debug.LogError("图鉴预制写入失败 " + path);
        }
    }
}
