using UnityEngine;

namespace InkLine
{
    public static class WxRuntimeKeep
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Keep()
        {
            WxBridge.KeepRuntime();
        }
    }
}
