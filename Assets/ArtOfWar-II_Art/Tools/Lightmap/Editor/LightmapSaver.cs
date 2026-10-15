using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace i89Trillion.Art.Tools
{
    public class LightmapSaver : EditorWindow
    {
        private int schemeCount = 2;
        private int currentScheme = 1;
        private DefaultAsset folder;
        private string folderPath = "Assets/";

        [MenuItem("Tools/光照贴图/保存光照贴图", false, 778898)]
        public static void ShowWindow()
        {
            GetWindow(typeof(LightmapSaver));
        }

        void OnGUI()
        {
            GUILayout.Label("Save Lightmaps", EditorStyles.boldLabel);

            schemeCount = EditorGUILayout.IntField("Scheme Count:", schemeCount);
            currentScheme = EditorGUILayout.IntField("Current Scheme:", currentScheme);
            folder = EditorGUILayout.ObjectField("Save In Folder:", folder, typeof(DefaultAsset), false) as DefaultAsset;

            if (GUILayout.Button("Save Lightmaps"))
            {
                folderPath = Path.Combine(AssetDatabase.GetAssetPath(folder), GetFolderName());
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder(AssetDatabase.GetAssetPath(folder), GetFolderName());
                }

                // 保存光照贴图到选择的文件夹中
                SaveLightmaps();

                // 保存光照贴图索引和对应的贴图数据
                SaveRenderersLightmapData();

                // 增加LightmapSwitcher组件到场景中的所有Renderer组件
                AddLightmapSwitchers();

                EditorUtility.DisplayDialog("Success", "Lightmaps data saved successfully.", "OK");
                Debug.Log("Lightmaps saved to " + folderPath);
            }
        }

        private string GetFolderName()
        {
            return SceneManager.GetActiveScene().name + "_" + currentScheme;
        }

        void SaveLightmaps()
        {
            if (LightmapSettings.lightmaps == null || LightmapSettings.lightmaps.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No lightmaps found in the scene.", "OK");
                return;
            }

            for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            {
                Texture2D colorMap = LightmapSettings.lightmaps[i].lightmapColor;
                Texture2D dirMap = LightmapSettings.lightmaps[i].lightmapDir;
                Texture2D shadowMaskMap = LightmapSettings.lightmaps[i].shadowMask;

                CopyTexture(colorMap, i, "_light");
                CopyTexture(dirMap, i, "_dir");
                CopyTexture(shadowMaskMap, i, "_shadowmask");
            }

            AssetDatabase.Refresh();
        }

        void CopyTexture(Texture2D texture, int index, string suffix)
        {
            if (texture != null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(texture);
                string extension = GetExtension(suffix);
                string destinationPath = GetDestinationPath(index, suffix, extension);

                // 使用AssetDatabase.CopyAsset复制纹理
                if (AssetDatabase.CopyAsset(sourcePath, destinationPath))
                {
                    Debug.Log("Texture copied successfully to " + destinationPath);
                }
                else
                {
                    Debug.LogError("Failed to copy texture to " + destinationPath);
                }

                AssetDatabase.Refresh();
            }
        }

        private string GetExtension(string suffix)
        {
            string extension = "";
            switch (suffix)
            {
                case "_light":
                    extension = ".exr";
                    break;
                case "_dir":
                case "_shadowmask":
                    extension = ".png";
                    break;
            }

            return extension;
        }

        public void SaveRenderersLightmapData()
        {
            LightmapDataAsset lightmapDataAsset = CreateInstance<LightmapDataAsset>();

            for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            {
                var l = AssetDatabase.LoadAssetAtPath<Texture2D>(GetDestinationPath(i, "_light", ".exr"));
                var d = AssetDatabase.LoadAssetAtPath<Texture2D>(GetDestinationPath(i, "_dir", ".png"));
                var sm = AssetDatabase.LoadAssetAtPath<Texture2D>(GetDestinationPath(i, "_shadowmask", ".png"));
                LightmapSavedData savedData = new LightmapSavedData
                {
                    index = i,
                    light = l,
                    dir = d,
                    shadowMask = sm
                };
                lightmapDataAsset.lightmapDataList.Add(savedData);
            }

            string assetPath = Path.Combine(folderPath, GetLmDataName());
            AssetDatabase.CreateAsset(lightmapDataAsset, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("Saved lightmap data to " + assetPath);
        }

        private string GetDestinationPath(int index, string suffix, string extension)
        {
            return Path.Combine(folderPath, "Lightmap-" + index + "_comp" + suffix.ToLower() + extension);
        }

        private string GetLmDataName()
        {
            return SceneManager.GetActiveScene().name + "_" + currentScheme + "_LmData.asset";
        }

        private void AddLightmapSwitchers()
        {
            string assetPath = Path.Combine(folderPath, GetLmDataName());
            LightmapDataAsset lightmapDataAsset = AssetDatabase.LoadAssetAtPath<LightmapDataAsset>(assetPath);

            if (lightmapDataAsset == null)
            {
                Debug.LogError("Failed to load LightmapDataAsset at " + assetPath);
                return;
            }

            Renderer[] renderers = FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.lightmapIndex != -1)
                {
                    LightmapSwitcher switcher = renderer.gameObject.GetComponent<LightmapSwitcher>();
                    if (switcher == null)
                    {
                        // 初始化光照贴图方案数组
                        switcher = renderer.gameObject.AddComponent<LightmapSwitcher>();
                        switcher.lightmapSchemes = new LightmapScheme[schemeCount];
                    }
                    else if (switcher.lightmapSchemes.Length < schemeCount)
                    {
                        var newSchemes = new LightmapScheme[schemeCount];
                        for (int i = 0; i < switcher.lightmapSchemes.Length; i++)
                        {
                            newSchemes[i] = switcher.lightmapSchemes[i];
                        }

                        switcher.lightmapSchemes = newSchemes;
                    }

                    // 为每个方案设置光照贴图数据
                    switcher.lightmapSchemes[currentScheme - 1] = new LightmapScheme
                    {
                        scheme = currentScheme,
                        lightmapIndex = renderer.lightmapIndex,
                        lightmapScaleOffset = renderer.lightmapScaleOffset
                    };
                }
            }
        }
    }
}