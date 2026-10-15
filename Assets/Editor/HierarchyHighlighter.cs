#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyHighlighter
{
    static HierarchyHighlighter()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        // 只要名字以 "==" 开头（例如 ==回合2==）
        if (go.name.StartsWith("=="))
        {
            // 1. 改为黄色背景 (RGB: 1.0, 0.8, 0.2)
            Color bgColor = new Color(1.0f, 0.8f, 0.2f, 1.0f);
            EditorGUI.DrawRect(selectionRect, bgColor);

            // 2. 绘制居中的黑色粗体文字（黄色背景配黑字更清晰）
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };

            EditorGUI.LabelField(selectionRect, go.name, labelStyle);
        }
    }

    [MenuItem("GameObject/--- Create Header ---", false, 0)]
    private static void CreateHeader(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("==新分类== ");
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create Header");
        Selection.activeObject = go;
    }
}
#endif