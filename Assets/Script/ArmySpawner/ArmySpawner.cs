using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ArmySpawner : MonoBehaviour
{
    [Header("士兵设置")]
    public GameObject soldierPrefab;

    [Header("队伍尺寸")]
    public int rows = 5;       // 行数
    public int columns = 10;   // 列数

    [Header("队形间距")]
    public float rowSpacing = 2f;     // 行间距（Z轴方向）
    public float columnSpacing = 2f;  // 列间距（X轴方向）

    public void SpawnArmy()
    {
        if (soldierPrefab == null)
        {
            Debug.LogError("ArmySpawner：请先指定 Project 中的士兵预制体。", this);
            return;
        }

        ClearArmy();

        int count = 0;

        // 计算第一排中心偏移
        float totalWidth = (columns - 1) * columnSpacing;
        float centerOffsetX = totalWidth / 2f;
        float centerOffsetZ = 0f;
        Vector3 centerOffset = new Vector3(centerOffsetX, 0, centerOffsetZ);

        // 将父物体位置移动，使其原点对齐到第一排中心
        transform.position = transform.position + transform.TransformVector(centerOffset);

        // 实例化士兵（局部坐标相对中心偏移）
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                Vector3 localPos = new Vector3(j * columnSpacing, 0, i * rowSpacing) - centerOffset;
                GameObject soldier = CreateSoldierInstance();
                if (soldier == null)
                {
                    return;
                }

                soldier.transform.localPosition = localPos;
                soldier.name = soldierPrefab.name + "_" + count;
                count++;
            }
        }
    }

    private GameObject CreateSoldierInstance()
    {
#if UNITY_EDITOR
        // 编辑模式下必须通过 PrefabUtility 生成，才能保留与 Project 预制体的连接。
        if (!Application.isPlaying)
        {
            GameObject prefabAsset = soldierPrefab;

            // 即使误拖入的是场景中的预制体实例，也尽量找到它对应的原始预制体资源。
            if (!AssetDatabase.Contains(prefabAsset))
            {
                prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(soldierPrefab);
            }

            if (prefabAsset == null ||
                PrefabUtility.GetPrefabAssetType(prefabAsset) == PrefabAssetType.NotAPrefab)
            {
                Debug.LogError("ArmySpawner：soldierPrefab 必须引用 Project 中的预制体资源。", this);
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset, transform) as GameObject;
            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Spawn Army Soldier");
            }

            return instance;
        }
#endif

        // 运行模式下继续使用普通实例化；编辑器专用 API 不会进入最终构建。
        return Instantiate(soldierPrefab, transform);
    }

    public void ClearArmy()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(child);
                continue;
            }
#endif

            DestroyImmediate(child);
        }
    }
}
