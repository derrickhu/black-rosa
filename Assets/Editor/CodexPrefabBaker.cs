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
            AssetDatabase.ImportAsset("Assets/Resources/Art/Ui",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            ModalKit.SpriteOf = key => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art/" + key + ".png");
            try
            {
                Save(CodexLayout.BuildMain(null), Main);
                Save(CodexLayout.BuildDetail(null), Detail);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                ModalKit.SpriteOf = null;
            }
        }

        static void Save(RectTransform root, string path)
        {
            // Shell 的弹出动画会立刻把面板缩到 0.72 并挪开。编辑器里没有下一帧，
            // 不拉回去的话，预制体会永远停在这一帧，打开图鉴就整页偏到右边。
            var board = root.Find("stage/board") as RectTransform;
            if (board != null)
            {
                var anim = board.GetComponent<UiAnim>();
                if (anim != null)
                {
                    anim.Finish();
                    Object.DestroyImmediate(anim);
                }
                board.localScale = Vector3.one;
                float mx = Mathf.Round((ModalKit.DesignW - board.sizeDelta.x) * 0.5f);
                float my = Mathf.Round((ModalKit.DesignH - board.sizeDelta.y) * 0.5f);
                board.anchoredPosition = new Vector2(mx, -my);
            }
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, path, out bool saved);
            Object.DestroyImmediate(root.gameObject);
            if (saved) Debug.Log("图鉴预制已写入 " + path);
            else Debug.LogError("图鉴预制写入失败 " + path);
        }

        // 旧木框图鉴预制缺 Mark 节点时，编辑器编译后自动重烘为新 Figma 切图预制。
        [InitializeOnLoad]
        static class CodexFigmaHook
        {
            static CodexFigmaHook()
            {
                EditorApplication.delayCall += Once;
            }

            static void Once()
            {
                if (!System.IO.File.Exists(Main) || !System.IO.File.Exists(Detail))
                {
                    Bake();
                    return;
                }
                string a = System.IO.File.ReadAllText(Main);
                string b = System.IO.File.ReadAllText(Detail);
                if (a.Contains("m_Name: " + CodexLayout.Mark) && b.Contains("m_Name: " + CodexLayout.Mark))
                    return;
                Bake();
            }
        }
    }
}
