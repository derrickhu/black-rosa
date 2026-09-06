using UnityEngine;

namespace InkLine
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Application.targetFrameRate = 60;
            Input.simulateMouseWithTouches = true;
            if (Object.FindObjectOfType<GameFlow>() != null) return;
            var root = new GameObject("InkLine");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameFlow>();
        }
    }
}
