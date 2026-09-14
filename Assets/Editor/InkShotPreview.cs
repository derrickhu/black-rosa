using System.IO;
using UnityEditor;
using UnityEngine;

public static class InkShotPreview
{
    const string PreviewKey = "InkLine.PreviewFill";
    static int _enterIn = -1;
    static int _watch;

    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static string PlayFlag => Path.Combine(Root, "Library/ink_shot_preview.play");
    static string DoneFlag => Path.Combine(Root, "Library/ink_shot_preview.done");

    [InitializeOnLoadMethod]
    static void Boot()
    {
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling) return;
        _watch++;
        if (_watch % 30 == 1)
            File.WriteAllText(Path.Combine(Root, "Library/ink_shot_preview.beat"), _watch.ToString());

        if (_enterIn > 0)
        {
            _enterIn--;
            if (_enterIn == 0 && !EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;
            return;
        }

        if (File.Exists(PlayFlag) && !EditorApplication.isPlaying)
        {
            File.Delete(PlayFlag);
            EditorPrefs.SetBool(PreviewKey, true);
            InkLine.BattleWorld.PreviewFill = true;
            _enterIn = 4;
            return;
        }

        if (File.Exists(DoneFlag) && EditorApplication.isPlaying)
        {
            File.Delete(DoneFlag);
            EditorApplication.isPlaying = false;
            EditorPrefs.SetBool(PreviewKey, false);
            InkLine.BattleWorld.PreviewFill = false;
        }
    }

    [MenuItem("墨字防线/实机核对炮弹")]
    public static void Run()
    {
        EditorPrefs.SetBool(PreviewKey, true);
        InkLine.BattleWorld.PreviewFill = true;
        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
    }
}
