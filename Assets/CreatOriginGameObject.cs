using UnityEngine;
using UnityEditor;
using System.Linq;

public class UnifiedParentCreator
{
    private const string PARENT_NAME = "OriginParent";
    
    [MenuItem("GameObject/创建原点父级", false, -1)]
    static void CreateUnifiedParent()
    {
        // 智能查找现有父级
        GameObject parent = FindExistingParent() ?? CreateNewParent();
        
        // 智能过滤无效对象
        var validTargets = Selection.transforms
            .Where(t => t != null && t != parent.transform)
            .ToArray();

        // 批量处理
        foreach (var target in validTargets)
        {
            Undo.SetTransformParent(target, parent.transform, "Reparenting");
        }

        // 自动定位到父级
        Selection.activeGameObject = parent;
        SceneView.lastActiveSceneView.FrameSelected();
    }

    static GameObject FindExistingParent()
    {
        return GameObject.Find(PARENT_NAME);
    }

    static GameObject CreateNewParent()
    {
        var parent = new GameObject(PARENT_NAME);
        Undo.RegisterCreatedObjectUndo(parent, "Create Parent");
        parent.transform.position = Vector3.zero;
        return parent;
    }

    [MenuItem("GameObject/统一原点父级", true)]
    static bool ValidateSelection()
    {
        return Selection.transforms.Length > 0;
    }
}