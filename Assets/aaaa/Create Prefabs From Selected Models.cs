using UnityEngine;
using UnityEditor;
using System.IO;

public class FbxToPrefabConverter
{
    [MenuItem("Assets/Create Prefabs From Selected FBX", false, 20)]
    private static void CreatePrefabsFromFbx()
    {
        string savePath = EditorUtility.OpenFolderPanel("Save Prefabs To Folder", "Assets", "");

        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogWarning("No folder selected to save prefabs to.");
            return;
        }

        savePath = savePath.Replace(Application.dataPath, "Assets");

        foreach (var obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (!assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"Skipping {assetPath} - Not an FBX file.");
                continue;
            }

            GameObject fbxInstance = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (fbxInstance == null)
            {
                Debug.LogWarning($"Failed to load FBX: {assetPath}");
                continue;
            }

            // 创建同名的父 GameObject
            GameObject parentObject = new GameObject(fbxInstance.name);

            // 实例化 FBX 对象，并设置为父 GameObject 的子节点
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxInstance);
            instance.transform.SetParent(parentObject.transform, false); // 保持本地坐标

            // 创建 Prefab 并保存
            string prefabPath = Path.Combine(savePath, $"{fbxInstance.name}.prefab");
            PrefabUtility.SaveAsPrefabAsset(parentObject, prefabPath);

            // 清理临时创建的对象
            Object.DestroyImmediate(parentObject);
        }

        AssetDatabase.Refresh();
        Debug.Log("Prefabs created successfully.");
    }

    [MenuItem("Assets/Create/Create Prefabs From Selected FBX", true)]
    private static bool ValidateCreatePrefabs()
    {
        foreach (var obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                return true; // 只要有一个 FBX 选中，就允许显示选项
            }
        }
        return false;
    }
}
