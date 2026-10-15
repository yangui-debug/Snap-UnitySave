using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class StaticPathSpawner : MonoBehaviour
{
    [Header("实时更新开关")]
    [Tooltip("开启后，无论在编辑模式还是游戏运行(Play)中，拖动Cube都会实时跟随")]
    public bool autoUpdatePositions = true;

    [Header("生成单位设置")]
    public List<GameObject> prefabsToSpawn;
    public float unitScaleMultiplier = 1.0f;

    // ================= 新增：内置俯视晃动系统 =================
    [Header("动态晃动效果 (专为俯视Plane设计)")]
    [Tooltip("开启后，运行游戏时预制体会自身摇摆晃动")]
    public bool enableWobble = true;

    [Tooltip("晃动的快慢")]
    public float wobbleSpeed = 5f;

    [Tooltip("前后平移幅度")]
    public float wobbleMoveDistance = 0.5f;

    [Tooltip("前后俯仰角度 (让2D贴图有立体感)")]
    public float wobbleTiltAngle = 10f;
    // ==========================================================

    [Header("路径与位置控制")]
    public List<Transform> pathPoints;
    public int spawnCountPerSegment = 3;
    public float spawnHeight = 0f;
    public bool spawnAtPathPoints = true;

    [Header("角度控制")]
    public Vector3 unifiedRotation = Vector3.zero;

    [HideInInspector]
    public List<GameObject> spawnedUnits = new List<GameObject>();

    // 记录每个预制体独有的随机时间偏移，防止它们像机器人一样动作完全同步
    [HideInInspector]
    public List<float> randomTimeOffsets = new List<float>();

    // 用 LateUpdate 确保优先级最高，不和 Timeline 抢控制权
    void LateUpdate()
    {
        // 只要开启了自动跟随，或者开启了晃动且在Play模式下，就每帧更新
        if ((autoUpdatePositions || (enableWobble && Application.isPlaying)) && spawnedUnits.Count > 0)
        {
            SyncPositions();
        }
    }

    [ContextMenu("一键生成路径单位 (Generate Units)")]
    public void GenerateUnits()
    {
        ClearUnits();

        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0) return;
        if (pathPoints == null || pathPoints.Count < 2) return;

        Quaternion targetRotation = Quaternion.Euler(unifiedRotation);

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 startPos = pathPoints[i].position;
            Vector3 endPos = pathPoints[i + 1].position;

            if (spawnAtPathPoints) SpawnSingleUnit(startPos, targetRotation);

            int partitions = spawnCountPerSegment + 1;
            for (int j = 1; j <= spawnCountPerSegment; j++)
            {
                float t = (float)j / partitions;
                Vector3 midPos = Vector3.Lerp(startPos, endPos, t);
                SpawnSingleUnit(midPos, targetRotation);
            }

            if (spawnAtPathPoints && i == pathPoints.Count - 2)
            {
                SpawnSingleUnit(endPos, targetRotation);
            }
        }
    }

    [ContextMenu("清空已生成的单位 (Clear Units)")]
    public void ClearUnits()
    {
        for (int i = spawnedUnits.Count - 1; i >= 0; i--)
        {
            if (spawnedUnits[i] != null)
            {
                if (Application.isPlaying) Destroy(spawnedUnits[i]);
                else DestroyImmediate(spawnedUnits[i]);
            }
        }
        spawnedUnits.Clear();
        randomTimeOffsets.Clear(); // 清空随机时间记录
    }

    private void SpawnSingleUnit(Vector3 rawPosition, Quaternion rotation)
    {
        Vector3 finalPos = new Vector3(rawPosition.x, spawnHeight, rawPosition.z);
        GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)];
        GameObject spawned = Instantiate(prefab, finalPos, rotation, this.transform);
        spawned.transform.localScale = Vector3.one * unitScaleMultiplier;

        spawnedUnits.Add(spawned);
        randomTimeOffsets.Add(Random.Range(0f, 100f)); // 生成时给每个人发一个随机的“节奏卡”
    }

    private void SyncPositions()
    {
        if (pathPoints == null || pathPoints.Count < 2) return;

        int expectedCount = (pathPoints.Count - 1) * spawnCountPerSegment;
        if (spawnAtPathPoints) expectedCount += pathPoints.Count;

        if (spawnedUnits.Count != expectedCount || randomTimeOffsets.Count != expectedCount) return;

        int unitIndex = 0;
        Quaternion targetRotation = Quaternion.Euler(unifiedRotation);

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            if (pathPoints[i] == null || pathPoints[i + 1] == null) continue;

            Vector3 startPos = pathPoints[i].position;
            Vector3 endPos = pathPoints[i + 1].position;

            if (spawnAtPathPoints)
            {
                UpdateSingleUnit(spawnedUnits[unitIndex], startPos, targetRotation, randomTimeOffsets[unitIndex]);
                unitIndex++;
            }

            int partitions = spawnCountPerSegment + 1;
            for (int j = 1; j <= spawnCountPerSegment; j++)
            {
                float t = (float)j / partitions;
                Vector3 midPos = Vector3.Lerp(startPos, endPos, t);
                UpdateSingleUnit(spawnedUnits[unitIndex], midPos, targetRotation, randomTimeOffsets[unitIndex]);
                unitIndex++;
            }

            if (spawnAtPathPoints && i == pathPoints.Count - 2)
            {
                UpdateSingleUnit(spawnedUnits[unitIndex], endPos, targetRotation, randomTimeOffsets[unitIndex]);
                unitIndex++;
            }
        }
    }

    private void UpdateSingleUnit(GameObject unit, Vector3 rawPosition, Quaternion rotation, float timeOffset)
    {
        if (unit == null) return;

        Vector3 finalPos = new Vector3(rawPosition.x, spawnHeight, rawPosition.z);
        Quaternion finalRot = rotation;

        // ================= 核心代码：叠加晃动效果 =================
        if (enableWobble && Application.isPlaying)
        {
            // 用时间 + 每个人的专属偏移量，算出波浪曲线
            float wave = Mathf.Sin((Time.time + timeOffset) * wobbleSpeed);

            // 1. 前后平移叠加 (基于统一朝向的正前方)
            finalPos += finalRot * Vector3.forward * (wave * wobbleMoveDistance);

            // 2. 俯仰角度叠加 (基于X轴微微倾斜)
            finalRot = finalRot * Quaternion.Euler(wave * wobbleTiltAngle, 0, 0);
        }
        // ==========================================================

        if (unit.transform.position != finalPos) unit.transform.position = finalPos;
        if (unit.transform.rotation != finalRot) unit.transform.rotation = finalRot;

        Vector3 targetScale = Vector3.one * unitScaleMultiplier;
        if (unit.transform.localScale != targetScale) unit.transform.localScale = targetScale;
    }
}