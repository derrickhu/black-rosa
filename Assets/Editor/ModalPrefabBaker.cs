using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InkLine
{
    // 重构新页面的统一预制体烘焙器：
    // 将所有弹窗壳烘成 Assets/Resources/Prefabs/*.prefab，只挂 Assets/Resources/Art/ 下的 Figma @2x 切图与子集字体。
    // 任一预制体缺失或版本标记升级时，编辑器编译后自动补烘。
    public static class ModalPrefabBaker
    {
        struct Entry
        {
            public string Path;
            public string Mark;
            public Func<RectTransform> Build;
        }

        static readonly Entry[] All =
        {
            new Entry { Path = "Assets/Resources/Prefabs/CheckIn.prefab",     Mark = CheckInPanel.Mark,     Build = CheckInPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/GameClub.prefab",    Mark = GameClubPanel.Mark,    Build = GameClubPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/Mail.prefab",        Mark = MailPanel.Mark,        Build = MailPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/StarterGift.prefab", Mark = StarterGiftPanel.Mark, Build = StarterGiftPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/Rank.prefab",        Mark = RankPanel.Mark,        Build = RankPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/Event.prefab",       Mark = EventPanel.Mark,       Build = EventPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/GuideGift.prefab",   Mark = GuideGiftPanel.Mark,   Build = GuideGiftPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/DailyPuzzle.prefab", Mark = DailyPuzzlePanel.Mark, Build = DailyPuzzlePanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/Endless.prefab",     Mark = EndlessPanel.Mark,     Build = EndlessPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/GlyphDetail.prefab", Mark = GlyphDetailPanel.Mark, Build = GlyphDetailPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/Settings.prefab",    Mark = SettingsPanel.Mark,    Build = SettingsPanel.BuildPrefab },
            new Entry { Path = "Assets/Resources/Prefabs/Confirm.prefab",     Mark = ConfirmPanel.Mark,     Build = ConfirmPanel.BuildPrefab },
        };

        [MenuItem("墨字防线/烘全部弹窗预制")]
        public static void BakeAllMenu()
        {
            Bake(forceAll: true);
        }

        public static void Bake(bool forceAll)
        {
            bool anyDirty = forceAll;
            if (!anyDirty)
            {
                for (int i = 0; i < All.Length; i++)
                {
                    if (NeedsBake(All[i])) { anyDirty = true; break; }
                }
            }
            if (!anyDirty) return;

            Directory.CreateDirectory("Assets/Resources/Prefabs");
            AssetDatabase.ImportAsset("Assets/Resources/Art/Ui",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            ModalKit.SpriteOf = key => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art/" + key + ".png");
            try
            {
                int count = 0;
                for (int i = 0; i < All.Length; i++)
                {
                    if (!forceAll && !NeedsBake(All[i])) continue;
                    Save(All[i].Build(), All[i].Path);
                    count++;
                }
                if (count > 0) AssetDatabase.SaveAssets();
            }
            finally
            {
                ModalKit.SpriteOf = null;
            }
        }

        static bool NeedsBake(Entry e)
        {
            if (!File.Exists(e.Path)) return true;
            string yaml = File.ReadAllText(e.Path);
            return !yaml.Contains("m_Name: " + e.Mark);
        }

        static void Save(RectTransform root, string path)
        {
            if (root == null) return;
            var board = root.Find("stage/board") as RectTransform;
            if (board != null)
            {
                var anim = board.GetComponent<UiAnim>();
                if (anim != null)
                {
                    anim.Finish();
                    UnityEngine.Object.DestroyImmediate(anim);
                }
                ModalKit.SettleBoard(board);
            }
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, path, out bool saved);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (saved) Debug.Log("弹窗预制已写入 " + path);
            else Debug.LogError("弹窗预制写入失败 " + path);
        }

        [InitializeOnLoad]
        static class ModalPrefabHook
        {
            static ModalPrefabHook()
            {
                EditorApplication.delayCall += Once;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }

            static void OnPlayModeChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += Once;
            }

            static void Once()
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                Bake(forceAll: false);
            }
        }
    }
}
