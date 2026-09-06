using System.IO;
using UnityEditor;
using UnityEngine;

public static class InkDraftPrefabBaker
{
    const string Path = "Assets/Resources/UI/DraftPanel.prefab";

    [InitializeOnLoadMethod]
    static void AutoBake()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Path))
                Bake();
        };
    }

    const string PreviewKey = "InkLine.PreviewFill";

    [InitializeOnLoadMethod]
    static void SyncPreview()
    {
        if (!EditorPrefs.HasKey(PreviewKey + ".off"))
        {
            EditorPrefs.SetBool(PreviewKey, false);
            EditorPrefs.SetBool(PreviewKey + ".off", true);
        }
        InkLine.BattleWorld.PreviewFill = EditorPrefs.GetBool(PreviewKey, false);
    }

    [MenuItem("墨弹防线/预览工具铺格")]
    public static void TogglePreviewFill()
    {
        bool on = !EditorPrefs.GetBool(PreviewKey, false);
        EditorPrefs.SetBool(PreviewKey, on);
        InkLine.BattleWorld.PreviewFill = on;
        Menu.SetChecked("墨弹防线/预览工具铺格", on);
        Debug.Log(on ? "预览铺格已开：进任意关会铺满 8 种工具" : "预览铺格已关");
    }

    [MenuItem("墨弹防线/预览工具铺格", true)]
    public static bool TogglePreviewFillValidate()
    {
        Menu.SetChecked("墨弹防线/预览工具铺格", EditorPrefs.GetBool(PreviewKey, false));
        return true;
    }

    [MenuItem("墨弹防线/Bake 三选一 Prefab")]
    public static void Bake()
    {
        string dir = "Assets/Resources/UI";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");

        var host = new GameObject("_DraftBake", typeof(RectTransform));
        var view = InkLine.DraftView.BuildTemplate(host.transform);
        GameObject root = view.gameObject;
        root.transform.SetParent(null, false);
        Object.DestroyImmediate(host);

        PrefabUtility.SaveAsPrefabAsset(root, Path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Draft prefab baked → " + Path);
    }
}
