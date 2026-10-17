using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

// 买量录屏用的 Mac 包：带 PROMO_CAPTURE 宏，只在命令行跑，不进微信包。
public static class PromoBuild
{
    const string Out = "/tmp/black-rosa-promo/InkPromo.app";

    public static void Build()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-promoBuildOut");
        string path = at >= 0 && at + 1 < args.Length ? args[at + 1] : Out;
        var opts = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.StandaloneOSX,
            targetGroup = BuildTargetGroup.Standalone,
            extraScriptingDefines = new[] { "PROMO_CAPTURE" },
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        UnityEngine.Debug.Log("Promo build: " + report.summary.result);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
