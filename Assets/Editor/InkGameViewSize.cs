using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class InkGameViewSize
{
    const int W = 720;
    const int H = 1280;
    const string Label = "墨弹防线 720x1280";

    static InkGameViewSize()
    {
        EditorApplication.delayCall += Register;
    }

    static void Register()
    {
        try
        {
            EnsureSize(GameViewSizeGroupType.Standalone);
            EnsureSize(GameViewSizeGroupType.Android);
            EnsureSize(GameViewSizeGroupType.iOS);
        }
        catch (Exception e)
        {
            Debug.LogWarning("无法注册 Game 竖屏预设: " + e.Message);
        }
    }

    static void EnsureSize(GameViewSizeGroupType groupType)
    {
        Assembly asm = typeof(Editor).Assembly;
        Type sizesType = asm.GetType("UnityEditor.GameViewSizes");
        Type sizeType = asm.GetType("UnityEditor.GameViewSize");
        Type sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
        if (sizesType == null || sizeType == null || sizeTypeEnum == null) return;

        Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        object sizes = singleton.GetProperty("instance").GetValue(null, null);
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)groupType });
        Type groupT = group.GetType();
        int count = (int)groupT.GetMethod("GetTotalCount").Invoke(group, null);
        for (int i = 0; i < count; i++)
        {
            object existing = groupT.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            int w = (int)existing.GetType().GetProperty("width").GetValue(existing, null);
            int h = (int)existing.GetType().GetProperty("height").GetValue(existing, null);
            if (w == W && h == H) return;
        }

        object fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");
        object size = Activator.CreateInstance(sizeType, fixedRes, W, H, Label);
        groupT.GetMethod("AddCustomSize").Invoke(group, new[] { size });
    }
}
