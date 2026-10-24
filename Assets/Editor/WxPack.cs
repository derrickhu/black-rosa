using UnityEditor;
using WeChatWASM;

// 命令行打微信包，和编辑器里「生成并转换」走同一条路，导出到 wechat-minigame。
//   Tuanjie -batchmode -nographics -quit -projectPath <repo> -executeMethod WxPack.Build -logFile -
public static class WxPack
{
    public static void Build()
    {
        WXConvertCore.WXExportError err = WXConvertCore.DoExport(true);
        UnityEngine.Debug.Log("Wx pack: " + err);
        EditorApplication.Exit(err == WXConvertCore.WXExportError.SUCCEED ? 0 : 1);
    }
}
