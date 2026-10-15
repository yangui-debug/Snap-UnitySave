using UnityEditor;
using UnityEngine;

public class HierarchyTools : MonoBehaviour
{
    [MenuItem("GameObject/重置原点", false, 20)]
    static void ResetParentToZero()
    {
        GameObject parentGO = Selection.activeGameObject;

        if (parentGO == null)
        {
            Debug.LogError("请选择一个父物体！");
            return;
        }

        if (parentGO.transform.parent != null)
        {
            Debug.LogError("此操作只能用于根层级的父物体！");
            return;
        }

        // 获取所有子物体
        Transform[] children = new Transform[parentGO.transform.childCount];
        Vector3[] worldPositions = new Vector3[children.Length];
        Quaternion[] worldRotations = new Quaternion[children.Length];
        Vector3[] localScales = new Vector3[children.Length];

        for (int i = 0; i < children.Length; i++)
        {
            children[i] = parentGO.transform.GetChild(i);
            worldPositions[i] = children[i].position;
            worldRotations[i] = children[i].rotation;
            localScales[i] = children[i].localScale;
        }

        // 解除所有子物体
        foreach (Transform child in children)
        {
            child.SetParent(null);
        }

        // 重置父物体
        parentGO.transform.position = Vector3.zero;
        parentGO.transform.rotation = Quaternion.identity;

        // 重新设置父物体并恢复子物体状态
        for (int i = 0; i < children.Length; i++)
        {
            children[i].SetParent(parentGO.transform);
            children[i].position = worldPositions[i];
            children[i].rotation = worldRotations[i];
            children[i].localScale = localScales[i];
        }

        Debug.Log($"已将父物体{parentGO.name}重置到原点，所有子物体的世界坐标保持不变。");
    }

    [MenuItem("GameObject/Reset Parent To Zero Without Changing Children", true)]
    static bool ValidateResetParentToZero()
    {
        // 验证菜单项是否可用：当选中一个物体并且它是根物体（没有父物体）时才可用
        return Selection.activeGameObject != null && Selection.activeGameObject.transform.parent == null;
    }
}