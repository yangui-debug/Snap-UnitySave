#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

public class ArrangeObjectsInGrid : EditorWindow
{
    // 行数和列数
    public int rows = 3;
    public int columns = 3;
    public float verticalSpacing = 2.0f; // 垂直间距
    public float horizontalSpacing = 2.0f; // 水平间距

    [MenuItem("Tools/Arrange Selected Objects In Grid")]
    static void ShowWindow()
    {
        // 打开自定义窗口
        ArrangeObjectsInGrid window = (ArrangeObjectsInGrid) EditorWindow.GetWindow(typeof(ArrangeObjectsInGrid));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

        rows = EditorGUILayout.IntField("Rows", rows);
        columns = EditorGUILayout.IntField("Columns", columns);
        verticalSpacing = EditorGUILayout.FloatField("Vertical Spacing", verticalSpacing);
        horizontalSpacing = EditorGUILayout.FloatField("Horizontal Spacing", horizontalSpacing);

        if (GUILayout.Button("Arrange Objects"))
        {
            ArrangeObjects();
        }
    }

    void ArrangeObjects()
    {
        // 获取选中的所有物体
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected!");
            return;
        }

        // 根据字母排序物体名称
        selectedObjects = selectedObjects.OrderBy(obj => obj.name).ToArray();

        // 初始化起始位置
        Vector3 startPosition = Vector3.zero;

        // 计算排列
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            GameObject obj = selectedObjects[i];

            // 计算当前物体的行列位置
            int row = i / columns;
            int column = i % columns;

            // 计算物体的位置
            float xPos = column * horizontalSpacing;
            float yPos = row * verticalSpacing;

            // 将物体位置更新为新的计算值
            obj.transform.position = startPosition + new Vector3(xPos, 0, yPos);

            // 打印物体的新位置
            Debug.Log($"Object {obj.name} new position: {obj.transform.position}");
        }

        // 给出反馈
        EditorUtility.DisplayDialog("Arrange Objects", "Objects have been arranged in grid layout.", "OK");
    }
}
#endif