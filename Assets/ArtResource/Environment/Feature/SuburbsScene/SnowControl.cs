using UnityEngine;

[ExecuteInEditMode] // 支持编辑器模式下运行
public class SnowControllerWithMidArea : MonoBehaviour
{
    [Range(0, 1)] public float snowValueA = 0f; // SuburbsScene_A_Area 的 _Snow 值
    [Range(0, 1)] public float snowValueB = 0f; // SuburbsScene_B_Area 的 _Snow 值

    public GameObject areaA; // 对应 SuburbsScene_A_Area 的空物体
    public GameObject areaB; // 对应 SuburbsScene_B_Area 的空物体
    public GameObject midArea; // 对应 Mid_Area 的空物体

    void OnValidate()
    {
        // 更新 AreaA 的子物体
        if (areaA != null)
        {
            UpdateObjectsSnow(areaA, snowValueA);
        }

        // 更新 AreaB 的子物体
        if (areaB != null)
        {
            UpdateObjectsSnow(areaB, snowValueB);
        }

        // 更新 Mid_Area 的子物体，值取 A 和 B 的最大值
        if (midArea != null)
        {
            float midSnowValue = (snowValueA + snowValueB) / 2f; // 取 A 和 B 的平均值
            UpdateObjectsSnow(midArea, midSnowValue);
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            OnValidate();
        }
    }

    void UpdateObjectsSnow(GameObject parent, float snowValue)
    {
        // 遍历该空物体的所有子物体
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat.HasProperty("_Snow"))
                {
                    // 更新材质的 _Snow 值
                    mat.SetFloat("_Snow", snowValue);
                }
            }
        }
    }
}