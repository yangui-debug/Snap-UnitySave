using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class TimelineSquadManager : MonoBehaviour
{
    [Header("【1】路径节点设置")]
    [Tooltip("整个大部队行进的平地路点")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("【2】小兵与建筑目标配置")]
    [Tooltip("直接把 6 个小兵物体拖入这里")]
    public List<GameObject> soldiers = new List<GameObject>();

    [Tooltip("小兵在【建筑1】对应的 6 个 Cube 目标点")]
    public List<Transform> building1Spots = new List<Transform>();

    [Tooltip("小兵在【建筑2】对应的 6 个 Cube 目标点")]
    public List<Transform> building2Spots = new List<Transform>();

    [Tooltip("小兵在【建筑3】对应的 6 个 Cube 目标点")]
    public List<Transform> building3Spots = new List<Transform>();

    [Tooltip("小兵在【建筑4】对应的 6 个 Cube 目标点")]
    public List<Transform> building4Spots = new List<Transform>();

    [Header("【2.6】建筑物本尊中心点（用于自动计算朝向）")]
    public Transform building1Transform;
    public Transform building2Transform;
    public Transform building3Transform;
    public Transform building4Transform;

    [Header("【2.7】目标建筑选择（Timeline控制）")]
    [Tooltip("当前散开的目标建筑：1 = 建筑1, 2 = 建筑2, 3 = 建筑3, 4 = 建筑4")]
    [Range(1, 4)]
    public int activeBuildingIndex = 1;

    [Header("【3】队列间距微调")]
    public float spaceX = 1.5f;
    public float spaceZ = 1.5f;
    [Range(1, 4)]
    public int teamColumns = 2;

    [Header("【4】Timeline 核心控制参数")]
    [Range(0f, 1f)]
    public float globalProgress = 0f;

    [Range(0f, 1f)]
    public float scatterProgress = 0f;

    [Header("【5】散开/集合平滑与加速")]
    [Tooltip("散开/集合的整体速度倍率。")]
    public float scatterSpeedMultiplier = 1.5f;

    [Tooltip("自定义散开运动曲线")]
    public AnimationCurve scatterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("【6】旋转与平滑")]
    public float turnSpeed = 10f;

    // ==================== 【全新新增：固定X轴旋转角度】 ====================
    [Tooltip("强行固定的X轴旋转角度（例如：30）")]
    public float fixedXRotation = 30f;
    // ===================================================================

    private List<float> m_SegmentLengths = new List<float>();
    private float m_TotalLength = 0f;

    private void Start()
    {
        CalculatePathLengths();
    }

    private void OnValidate()
    {
        CalculatePathLengths();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count < 2 || soldiers == null || soldiers.Count == 0) return;

        if (!Application.isPlaying)
        {
            CalculatePathLengths();
        }

        if (m_TotalLength > 0.001f)
        {
            float leaderDistance = globalProgress * m_TotalLength;

            float rawT = Mathf.Clamp01(scatterProgress * scatterSpeedMultiplier);
            float evaluatedT = scatterCurve.Evaluate(rawT);

            Transform currentBuilding = GetCurrentBuildingTransform();

            for (int i = 0; i < soldiers.Count; i++)
            {
                if (soldiers[i] == null) continue;

                int row = i / teamColumns;
                int col = i % teamColumns;

                float offsetX = (col - (teamColumns - 1) * 0.5f) * spaceX;
                float offsetZ = -row * spaceZ;

                float soldierDistance = leaderDistance + offsetZ;

                Vector3 pathPos = GetPathPositionConstantSpeed(soldierDistance, out Vector3 moveDir);
                Quaternion moveRot = moveDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(moveDir, Vector3.up) : transform.rotation;

                Vector3 rightDir = moveRot * Vector3.right;
                Vector3 formationWorldPos = pathPos + rightDir * offsetX;

                Transform targetSpot = GetCurrentTargetSpot(i);
                Vector3 finalPos = formationWorldPos;
                Quaternion finalRot = moveRot;

                if (targetSpot != null)
                {
                    Vector3 spotWorldPos = new Vector3(targetSpot.position.x, soldiers[i].transform.position.y, targetSpot.position.z);

                    // 1. 位置插值
                    finalPos = Vector3.Lerp(formationWorldPos, spotWorldPos, evaluatedT);

                    // 2. 自动转向逻辑
                    if (currentBuilding != null)
                    {
                        Vector3 lookAtBuildingDir = new Vector3(currentBuilding.position.x, soldiers[i].transform.position.y, currentBuilding.position.z) - soldiers[i].transform.position;
                        Quaternion buildingRot = lookAtBuildingDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookAtBuildingDir.normalized, Vector3.up) : moveRot;

                        finalRot = Quaternion.Lerp(moveRot, buildingRot, evaluatedT);
                    }
                }

                soldiers[i].transform.position = finalPos;

                // ==================== 【修改逻辑：注入固定的X轴角度】 ====================
                // 1. 提取当前目标朝向的 Y 轴旋转（欧拉角），保留原汁原味的左右转身
                float currentEulerY = finalRot.eulerAngles.y;

                // 2. 重新组合旋转：强行注入 fixedXRotation (30度)，Z轴保持0
                Quaternion targetFixedRot = Quaternion.Euler(fixedXRotation, currentEulerY, 0f);

                // 3. 应用旋转（支持运行时平滑插值，以及编辑模式下直接对齐）
                if (Application.isPlaying)
                    soldiers[i].transform.rotation = Quaternion.Slerp(soldiers[i].transform.rotation, targetFixedRot, turnSpeed * Time.deltaTime);
                else
                    soldiers[i].transform.rotation = targetFixedRot;
                // =======================================================================
            }
        }
    }

    private Transform GetCurrentTargetSpot(int soldierIndex)
    {
        if (activeBuildingIndex == 1)
        {
            if (building1Spots != null && soldierIndex < building1Spots.Count) return building1Spots[soldierIndex];
        }
        else if (activeBuildingIndex == 2)
        {
            if (building2Spots != null && soldierIndex < building2Spots.Count) return building2Spots[soldierIndex];
        }
        else if (activeBuildingIndex == 3)
        {
            if (building3Spots != null && soldierIndex < building3Spots.Count) return building3Spots[soldierIndex];
        }
        else if (activeBuildingIndex == 4)
        {
            if (building4Spots != null && soldierIndex < building4Spots.Count) return building4Spots[soldierIndex];
        }
        return null;
    }

    private Transform GetCurrentBuildingTransform()
    {
        switch (activeBuildingIndex)
        {
            case 1: return building1Transform;
            case 2: return building2Transform;
            case 3: return building3Transform;
            case 4: return building4Transform;
            default: return null;
        }
    }

    private void CalculatePathLengths()
    {
        m_SegmentLengths.Clear();
        m_TotalLength = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null) continue;
            Vector3 p1 = new Vector3(waypoints[i].position.x, transform.position.y, waypoints[i].position.z);
            Vector3 p2 = new Vector3(waypoints[i + 1].position.x, transform.position.y, waypoints[i + 1].position.z);
            float dist = Vector3.Distance(p1, p2);
            m_SegmentLengths.Add(dist);
            m_TotalLength += dist;
        }
    }

    private Vector3 GetPathPositionConstantSpeed(float currentDistance, out Vector3 forwardDir)
    {
        forwardDir = Vector3.forward;

        if (currentDistance < 0f)
        {
            if (waypoints.Count >= 2 && waypoints[0] != null && waypoints[1] != null)
            {
                Vector3 p1 = new Vector3(waypoints[0].position.x, transform.position.y, waypoints[0].position.z);
                Vector3 p2 = new Vector3(waypoints[1].position.x, transform.position.y, waypoints[1].position.z);
                forwardDir = (p2 - p1).normalized;
                return p1 + forwardDir * currentDistance;
            }
        }

        if (currentDistance > m_TotalLength)
        {
            if (waypoints.Count >= 2)
            {
                Transform lastPt = waypoints[waypoints.Count - 1];
                Transform penPt = waypoints[waypoints.Count - 2];
                if (lastPt != null && penPt != null)
                {
                    Vector3 pPen = new Vector3(penPt.position.x, transform.position.y, penPt.position.z);
                    Vector3 pLast = new Vector3(lastPt.position.x, transform.position.y, lastPt.position.z);
                    forwardDir = (pLast - pPen).normalized;
                    float overDistance = currentDistance - m_TotalLength;
                    return pLast + forwardDir * overDistance;
                }
            }
        }

        float accumulatedDistance = 0f;
        for (int i = 0; i < m_SegmentLengths.Count; i++)
        {
            float segmentLength = m_SegmentLengths[i];
            if (currentDistance <= accumulatedDistance + segmentLength)
            {
                float remainDistance = currentDistance - accumulatedDistance;
                Vector3 p1 = new Vector3(waypoints[i].position.x, transform.position.y, waypoints[i].position.z);
                Vector3 p2 = new Vector3(waypoints[i + 1].position.x, transform.position.y, waypoints[i + 1].position.z);
                forwardDir = (p2 - p1).normalized;
                return p1 + forwardDir * remainDistance;
            }
            accumulatedDistance += segmentLength;
        }

        if (waypoints.Count > 0 && waypoints[waypoints.Count - 1] != null)
        {
            Transform lastPt = waypoints[waypoints.Count - 1];
            return new Vector3(lastPt.position.x, transform.position.y, lastPt.position.z);
        }
        return transform.position;
    }
}