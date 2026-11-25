using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace InkLine
{
    public static class McpAutoConnect
    {
        public static void Boot()
        {
            Application.runInBackground = true;
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetBool("MCPForUnity.ResumeHttpAfterReload", true);

            try
            {
                ModalPrefabBaker.Bake(forceAll: false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[McpAutoConnect] Bake warning: " + ex.Message);
            }

            _ = ConnectAsync();
        }

        static async Task ConnectAsync()
        {
            try
            {
                var locator = Type.GetType("MCPForUnity.Editor.Services.MCPServiceLocator, MCPForUnity.Editor");
                if (locator == null)
                {
                    Debug.LogWarning("[McpAutoConnect] MCPServiceLocator not found");
                    return;
                }
                var bridgeProp = locator.GetProperty("Bridge", BindingFlags.Public | BindingFlags.Static);
                object bridge = bridgeProp?.GetValue(null);
                if (bridge == null) return;

                var isRunningProp = bridge.GetType().GetProperty("IsRunning", BindingFlags.Public | BindingFlags.Instance);
                if (isRunningProp != null && (bool)isRunningProp.GetValue(bridge))
                {
                    Debug.Log("[McpAutoConnect] MCP Bridge already running");
                    return;
                }

                var startMethod = bridge.GetType().GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance);
                if (startMethod != null)
                {
                    var task = startMethod.Invoke(bridge, null) as Task<bool>;
                    bool ok = task != null && await task;
                    Debug.Log("[McpAutoConnect] MCP Bridge StartAsync result=" + ok);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[McpAutoConnect] ConnectAsync warning: " + ex.Message);
            }
        }
    }
}
