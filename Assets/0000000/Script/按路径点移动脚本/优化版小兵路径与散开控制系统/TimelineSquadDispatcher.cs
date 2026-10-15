using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 散开目标建筑的数据配置项
/// </summary>
[System.Serializable]
public class ScatterTargetConfig
{
    [Tooltip("目标标识（仅用于 Inspector 中辨识，如：B1大楼 / 兵营A）")]
    public string targetName = "新目标建筑";

    [Tooltip("建筑中心 Transform（用于计算小兵的面向朝向）")]
    public Transform buildingCenter;

    [Tooltip("对应各个小兵的散开落脚点（按小兵列表中索引顺序对应）")]
    public List<Transform> scatterSpots = new List<Transform>();
}

[ExecuteAlways]
public class TimelineSquadDispatcher : MonoBehaviour
{
    [Header("【1】路线节点配置")]
    [Tooltip("队伍行进的全局路径点列表")]
    public List<Transform> pathWaypoints = new List<Transform>();

    [Header("【2】小兵单位配置")]
    [Tooltip("参与编队的所有小兵 GameObject")]
    public List<GameObject> squadMembers = new List<GameObject>();

    [Header("【3】散开建筑目标池（动态列表）")]
    [Tooltip("点击 '+' 即可自由添加无限个目标建筑及落脚点配置")]
    public List<ScatterTargetConfig> targetBuildings = new List<ScatterTargetConfig>();

    [Header("【4】Timeline 控制句柄")]
    [Tooltip("当前散开的目标建筑序号（从 1 开始，对应上方列表的第 N 项）")]
    public int targetBuildingIndex = 1;

    [Range(0f, 1f)]
    [Tooltip("全局路径推进进度 (0 = 起点, 1 = 终点)")]
    public float routeProgress = 0f;

    [Range(0f, 1f)]
    [Tooltip("离队散开/归队集合进度 (0 = 保持队列, 1 = 完全散开)")]
    public float scatterProgress = 0f;

    [Header("【5】队列阵型设置")]
    public float columnSpacing = 1.5f;
    public float rowSpacing = 1.5f;
    [Range(1, 10)]
    public int totalColumns = 2;

    [Header("【6】散开/归队动画微调")]
    [Tooltip("散开/集合的过渡速度倍率")]
    public float scatterSpeedScale = 1.5f;

    [Tooltip("散开运动的时间插值曲线")]
    public AnimationCurve scatterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("【7】旋转与姿态管控")]
    public float rotationSmoothSpeed = 10f;

    [Tooltip("强制锁定小兵的 X 轴俯仰角（例如：30）")]
    public float pitchAngleLock = 30f;

    // 内部路径缓存数据
    private readonly List<float> m_SegmentDistances = new List<float>();
    private float m_TotalRouteLength = 0f;

    private void Start()
    {
        RefreshPathCache();
    }

    private void OnValidate()
    {
        RefreshPathCache();

        // 越界安全防护：自动限制 targetBuildingIndex 在合法范围内
        if (targetBuildings != null && targetBuildings.Count > 0)
        {
            targetBuildingIndex = Mathf.Clamp(targetBuildingIndex, 1, targetBuildings.Count);
        }
        else
        {
            targetBuildingIndex = 1;
        }
    }

    private void Update()
    {
        if (pathWaypoints == null || pathWaypoints.Count < 2 || squadMembers == null || squadMembers.Count == 0)
            return;

        if (!Application.isPlaying)
        {
            RefreshPathCache();
        }

        if (m_TotalRouteLength <= 0.001f) return;

        // 1. 计算队长当前在路径上的行进距离
        float leaderTravelDistance = routeProgress * m_TotalRouteLength;

        // 2. 计算当前平滑后的散开权重 T (0 ~ 1)
        float rawT = Mathf.Clamp01(scatterProgress * scatterSpeedScale);
        float transitionT = scatterCurve.Evaluate(rawT);

        // 3. 获取当前 Timeline 选择的建筑数据项
        ScatterTargetConfig activeBuilding = GetActiveTargetConfig();
        Transform activeBuildingTransform = activeBuilding != null ? activeBuilding.buildingCenter : null;

        // 4. 逐个更新小兵位置与朝向
        for (int i = 0; i < squadMembers.Count; i++)
        {
            if (squadMembers[i] == null) continue;

            // ---- 队列矩阵偏移量计算 ----
            int row = i / totalColumns;
            int col = i % totalColumns;

            float offsetX = (col - (totalColumns - 1) * 0.5f) * columnSpacing;
            float offsetZ = -row * rowSpacing;

            float memberDistance = leaderTravelDistance + offsetZ;

            // ---- 路径点位置及方向计算 ----
            Vector3 pathPos = GetPositionAtDistance(memberDistance, out Vector3 moveDir);
            Quaternion baseMoveRot = moveDir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(moveDir, Vector3.up)
                : transform.rotation;

            Vector3 rightVector = baseMoveRot * Vector3.right;
            Vector3 squadFormationPos = pathPos + rightVector * offsetX;

            // ---- 目标落脚点与转向计算 ----
            Transform spotTransform = GetMemberSpotTransform(activeBuilding, i);
            Vector3 calculatedPos = squadFormationPos;
            Quaternion calculatedRot = baseMoveRot;

            if (spotTransform != null)
            {
                Vector3 spotWorldPos = new Vector3(spotTransform.position.x, squadMembers[i].transform.position.y, spotTransform.position.z);

                // A. 位置插值 (队列位置 -> 散开落脚点)
                calculatedPos = Vector3.Lerp(squadFormationPos, spotWorldPos, transitionT);

                // B. 面向建筑方向插值
                if (activeBuildingTransform != null)
                {
                    Vector3 toBuildingDir = new Vector3(activeBuildingTransform.position.x, squadMembers[i].transform.position.y, activeBuildingTransform.position.z) - squadMembers[i].transform.position;
                    Quaternion lookAtBuildingRot = toBuildingDir.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(toBuildingDir.normalized, Vector3.up)
                        : baseMoveRot;

                    calculatedRot = Quaternion.Lerp(baseMoveRot, lookAtBuildingRot, transitionT);
                }
            }

            // 更新应用位置
            squadMembers[i].transform.position = calculatedPos;

            // ---- 姿态锁相：注入固定 X 轴俯仰角 ----
            float currentEulerY = calculatedRot.eulerAngles.y;
            Quaternion targetPitchRot = Quaternion.Euler(pitchAngleLock, currentEulerY, 0f);

            if (Application.isPlaying)
            {
                squadMembers[i].transform.rotation = Quaternion.Slerp(
                    squadMembers[i].transform.rotation,
                    targetPitchRot,
                    rotationSmoothSpeed * Time.deltaTime
                );
            }
            else
            {
                squadMembers[i].transform.rotation = targetPitchRot;
            }
        }
    }

    /// <summary>
    /// 获取当前生效的建筑配置结构
    /// </summary>
    private ScatterTargetConfig GetActiveTargetConfig()
    {
        if (targetBuildings == null || targetBuildings.Count == 0) return null;

        int listIndex = targetBuildingIndex - 1; // 转换为从 0 开始的 List 索引
        if (listIndex >= 0 && listIndex < targetBuildings.Count)
        {
            return targetBuildings[listIndex];
        }
        return null;
    }

    /// <summary>
    /// 获取小兵对应的 Spot Transform
    /// </summary>
    private Transform GetMemberSpotTransform(ScatterTargetConfig config, int memberIndex)
    {
        if (config != null && config.scatterSpots != null)
        {
            if (memberIndex >= 0 && memberIndex < config.scatterSpots.Count)
            {
                return config.scatterSpots[memberIndex];
            }
        }
        return null;
    }

    /// <summary>
    /// 缓存并计算整条路径的总长度及各分段长度
    /// </summary>
    private void RefreshPathCache()
    {
        m_SegmentDistances.Clear();
        m_TotalRouteLength = 0f;

        for (int i = 0; i < pathWaypoints.Count - 1; i++)
        {
            if (pathWaypoints[i] == null || pathWaypoints[i + 1] == null) continue;

            Vector3 startPin = new Vector3(pathWaypoints[i].position.x, transform.position.y, pathWaypoints[i].position.z);
            Vector3 endPin = new Vector3(pathWaypoints[i + 1].position.x, transform.position.y, pathWaypoints[i + 1].position.z);

            float segmentDist = Vector3.Distance(startPin, endPin);
            m_SegmentDistances.Add(segmentDist);
            m_TotalRouteLength += segmentDist;
        }
    }

    /// <summary>
    /// 匀速路径采样
    /// </summary>
    private Vector3 GetPositionAtDistance(float travelDistance, out Vector3 forwardDir)
    {
        forwardDir = Vector3.forward;

        // 起点前的延伸线
        if (travelDistance < 0f)
        {
            if (pathWaypoints.Count >= 2 && pathWaypoints[0] != null && pathWaypoints[1] != null)
            {
                Vector3 p1 = new Vector3(pathWaypoints[0].position.x, transform.position.y, pathWaypoints[0].position.z);
                Vector3 p2 = new Vector3(pathWaypoints[1].position.x, transform.position.y, pathWaypoints[1].position.z);
                forwardDir = (p2 - p1).normalized;
                return p1 + forwardDir * travelDistance;
            }
        }

        // 终点后的延伸线
        if (travelDistance > m_TotalRouteLength)
        {
            if (pathWaypoints.Count >= 2)
            {
                Transform lastPt = pathWaypoints[pathWaypoints.Count - 1];
                Transform penPt = pathWaypoints[pathWaypoints.Count - 2];
                if (lastPt != null && penPt != null)
                {
                    Vector3 pPen = new Vector3(penPt.position.x, transform.position.y, penPt.position.z);
                    Vector3 pLast = new Vector3(lastPt.position.x, transform.position.y, lastPt.position.z);
                    forwardDir = (pLast - pPen).normalized;
                    float excessDist = travelDistance - m_TotalRouteLength;
                    return pLast + forwardDir * excessDist;
                }
            }
        }

        // 路径段内采样
        float currentAccumulatedDist = 0f;
        for (int i = 0; i < m_SegmentDistances.Count; i++)
        {
            float segLength = m_SegmentDistances[i];
            if (travelDistance <= currentAccumulatedDist + segLength)
            {
                float localDistance = travelDistance - currentAccumulatedDist;
                Vector3 p1 = new Vector3(pathWaypoints[i].position.x, transform.position.y, pathWaypoints[i].position.z);
                Vector3 p2 = new Vector3(pathWaypoints[i + 1].position.x, transform.position.y, pathWaypoints[i + 1].position.z);
                forwardDir = (p2 - p1).normalized;
                return p1 + forwardDir * localDistance;
            }
            currentAccumulatedDist += segLength;
        }

        if (pathWaypoints.Count > 0 && pathWaypoints[pathWaypoints.Count - 1] != null)
        {
            Transform lastPt = pathWaypoints[pathWaypoints.Count - 1];
            return new Vector3(lastPt.position.x, transform.position.y, lastPt.position.z);
        }

        return transform.position;
    }
}