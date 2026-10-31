using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using WeChatWASM;

// 打微信包，和编辑器里「生成并转换」走同一条路，导出到 wechat-minigame。
// 命令行：Tuanjie -batchmode -nographics -quit -projectPath <repo> -executeMethod WxPack.Build
// 编辑器里用菜单「微信小游戏/打微信包」，打完不退出。
// 首包不进代码包。导出成功后按 game.js 里的文件名上传到 MiniGameConfig 的 CDN，云上的旧文件留着。
public static class WxPack
{
    [MenuItem("微信小游戏/打微信包")]
    public static void Build()
    {
        WXConvertCore.WXExportError err = WXConvertCore.DoExport(true);
        UnityEngine.Debug.Log("Wx pack: " + err);
        bool ok = err == WXConvertCore.WXExportError.SUCCEED && UploadDataPackage();
        if (Application.isBatchMode)
            EditorApplication.Exit(ok ? 0 : 1);
    }

    // 只上传这一份带哈希的首包。失败就让打包失败，避免代码包先发出去、玩家却下不到资源。
    static bool UploadDataPackage()
    {
        string python = FindPython();
        if (python == null)
        {
            UnityEngine.Debug.LogError("Wx pack: 找不到 python3，首包没有上传");
            return false;
        }
        string root = Path.GetDirectoryName(Application.dataPath);
        string script = Path.Combine(root, "Tools/upload_wx_data.py");
        var start = new ProcessStartInfo
        {
            FileName = python,
            Arguments = "\"" + script + "\"",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using (Process proc = Process.Start(start))
        {
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (!string.IsNullOrEmpty(stdout))
                UnityEngine.Debug.Log(stdout.Trim());
            if (proc.ExitCode != 0)
            {
                UnityEngine.Debug.LogError("Wx pack: 首包上传失败 " + stderr.Trim());
                return false;
            }
            return true;
        }
    }

    static string FindPython()
    {
        string[] candidates =
        {
            "/opt/homebrew/bin/python3",
            "/usr/local/bin/python3",
            "/usr/bin/python3",
        };
        for (int i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
                return candidates[i];
        }
        return null;
    }
}
