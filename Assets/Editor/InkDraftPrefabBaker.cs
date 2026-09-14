using System.IO;
using UnityEditor;
using UnityEngine;

// 原来还负责把三选一烘成 Resources/UI/DraftPanel.prefab。已经去掉：
// UiKit 的圆角/描边/投影是 UiSprites 运行时烘的 Texture2D，不是工程资源，
// 预制体存不住这种引用，烘出来是一屏裸矩形。三选一改成运行时搭，见 DraftPanel.Show。
public static class InkDraftPrefabBaker
{
    const string PreviewKey = "InkLine.PreviewFill";

    [InitializeOnLoadMethod]
    static void SyncPreview()
    {
        if (!EditorPrefs.HasKey(PreviewKey + ".off"))
        {
            EditorPrefs.SetBool(PreviewKey, false);
            EditorPrefs.SetBool(PreviewKey + ".off", true);
        }
        if (File.Exists("Library/ink_shot_preview.force"))
            InkLine.BattleWorld.PreviewFill = true;
        else
            InkLine.BattleWorld.PreviewFill = EditorPrefs.GetBool(PreviewKey, false);
    }

    [MenuItem("墨字防线/预览工具铺格")]
    public static void TogglePreviewFill()
    {
        bool on = !EditorPrefs.GetBool(PreviewKey, false);
        EditorPrefs.SetBool(PreviewKey, on);
        InkLine.BattleWorld.PreviewFill = on;
        Menu.SetChecked("墨字防线/预览工具铺格", on);
        Debug.Log(on ? "预览铺格已开：进关后轮播核对每字炮弹" : "预览铺格已关");
    }

    [MenuItem("墨字防线/预览工具铺格", true)]
    public static bool TogglePreviewFillValidate()
    {
        Menu.SetChecked("墨字防线/预览工具铺格", EditorPrefs.GetBool(PreviewKey, false));
        return true;
    }

}
