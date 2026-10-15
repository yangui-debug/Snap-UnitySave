using UnityEditor;
using UnityEngine;

public class RemoveCustomScripts : MonoBehaviour
{
    [MenuItem("Assets/Remove Custom Scripts From Prefab", true)]
    private static bool ValidateRemoveCustomScriptsFromPrefab()
    {
        // 检查是否选中了一个 Prefab
        return Selection.activeObject is GameObject && AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".prefab");
    }

    [MenuItem("Assets/Remove Custom Scripts From Prefab")]
    private static void RemoveCustomScriptsFromPrefab()
    {
        // 获取选中的 Prefab 路径
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("No valid prefab selected.");
            return;
        }

        // 加载 Prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("Failed to load prefab.");
            return;
        }

        // 创建一个临时实例来移除脚本
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError("Failed to instantiate prefab.");
            return;
        }

        // 移除所有自定义脚本
        RemoveAllCustomScripts(instance);

        // 替换 Prefab 中的内容
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        GameObject.DestroyImmediate(instance);

        Debug.Log($"All custom scripts removed from prefab at: {path}");
    }

    private static void RemoveAllCustomScripts(GameObject root)
    {
        // 遍历所有子物体
        Component[] components = root.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            // 跳过 Unity 内置的脚本
            if (component == null || IsUnityBuiltInType(component.GetType())) continue;

            // 移除自定义脚本
            GameObject.DestroyImmediate(component);
        }
    }

    private static bool IsUnityBuiltInType(System.Type type)
    {
        // 检查是否是 UnityEngine 命名空间中的类型
        return type.Namespace != null && type.Namespace.StartsWith("UnityEngine");
    }
}