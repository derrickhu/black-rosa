using UnityEditor;
using UnityEngine;
using WeChatWASM;

// 打微信包，和编辑器里「生成并转换」走同一条路，导出到 wechat-minigame。
// 命令行：Tuanjie -batchmode -nographics -quit -projectPath <repo> -executeMethod WxPack.Build
// 编辑器里用菜单「微信小游戏/打微信包」，打完不退出。
public static class WxPack
{
    [MenuItem("微信小游戏/打微信包")]
    public static void Build()
    {
        WXConvertCore.WXExportError err = WXConvertCore.DoExport(true);
        UnityEngine.Debug.Log("Wx pack: " + err);
        if (Application.isBatchMode)
            EditorApplication.Exit(err == WXConvertCore.WXExportError.SUCCEED ? 0 : 1);
    }
}
