using UnityEditor;
using UnityEngine;

public class RemoveMissingScriptsFromSelectedPrefab : MonoBehaviour
{
    [MenuItem("Tools/Remove Missing Scripts From Selected Prefab")]
    static void RemoveMissingScripts()
    {
        // 获取当前选中的对象
        GameObject selectedPrefab = Selection.activeGameObject;

        if (selectedPrefab == null)
        {
            Debug.LogError("No prefab selected. Please select a prefab in the hierarchy or project window.");
            return;
        }

        // 确保选中的对象是一个Prefab实例
        if (PrefabUtility.GetPrefabAssetType(selectedPrefab) == PrefabAssetType.NotAPrefab)
        {
            Debug.LogError("Selected object is not a prefab.");
            return;
        }

        // 实例化Prefab
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);

        int removedScriptsCount = RemoveMissingScriptsInGameObject(prefabInstance);

        if (removedScriptsCount > 0)
        {
            PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.UserAction);
            Debug.Log($"Removed {removedScriptsCount} missing scripts from selected prefab: {selectedPrefab.name}");
        }
        else
        {
            Debug.Log("No missing scripts found on selected prefab.");
        }

        DestroyImmediate(prefabInstance);
    }

    static int RemoveMissingScriptsInGameObject(GameObject obj)
    {
        int count = 0;

        // 遍历对象上的所有组件
        Component[] components = obj.GetComponents<Component>();
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (components[i] == null)
            {
                // 移除丢失的组件
                Undo.RegisterCompleteObjectUndo(obj, "Remove Missing Script");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                count++;
            }
        }

        // 递归检查并移除子对象上的丢失脚本
        foreach (Transform child in obj.transform)
        {
            count += RemoveMissingScriptsInGameObject(child.gameObject);
        }

        return count;
    }
}
