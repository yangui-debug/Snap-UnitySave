using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways] // 确保不运行游戏、只拖拽 Timeline 时也能实时计算位置
public class TimelinePathMover : MonoBehaviour
{
    [Header("【1】路径节点设置")]
    [Tooltip("按顺序放入平地路点")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("【2】Timeline 核心驱动进度")]
    [Range(0f, 1f)]
    [Tooltip("【最关键参数】总移动进度：0代表在起点，1代表在终点。你只需要在 Timeline 里对这个变量 K 帧，就能在编辑模式下边拖边看！")]
    public float globalProgress = 0f;

    [Header("【3】旋转配置")]
    public bool lookAtTarget = true;
    [Tooltip("在运行或拖拽时，身体转向路径前方的平滑度")]
    public float turnSpeed = 10f;

    // ==================== 【全新新增：Timeline 预览状态下的固定倾斜角度调节】 ====================
    [Header("【4】移动过程中的固定倾斜角度 (X, Y, Z)")]
    [Tooltip("在这里自由调整行进和 Timeline 预览中的倾斜度。例如想让身体全程向前倾斜50度，把X改成 -50 即可")]
    public Vector3 movementTiltEulerAngles = new Vector3(-50f, 0f, 0f);
    // ===================================================================================

    // 内部高精路点距离缓存
    private List<float> m_SegmentLengths = new List<float>();
    private float m_TotalLength = 0f;
    private float m_LastProgress = -1f;

    void Update()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        // 核心优化：只有当进度发生变化时才重新计算，节省性能且防止死锁
        if (!Mathf.Approximately(globalProgress, m_LastProgress))
        {
            // 1. 重新计算并缓存整条路径的每段长度（防止你在编辑器里乱动路点位置）
            CalculatePathLengths();

            if (m_TotalLength > 0.001f)
            {
                // 2. 将 0~1 的总进度映射到真实的物理总长度（米）上
                float targetDistance = globalProgress * m_TotalLength;

                // 3. 根据当前走过的米数，算出应该踩在哪个路点之间，并更新位置和朝向
                UpdatePositionAndRotation(targetDistance);
            }

            m_LastProgress = globalProgress;
        }
    }

    /// <summary>
    /// 计算并缓存整条折线的总长度和每段长度
    /// </summary>
    private void CalculatePathLengths()
    {
        m_SegmentLengths.Clear();
        m_TotalLength = 0f;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null) continue;

            // 抹平 Y 轴计算平地真实距离
            Vector3 p1 = new Vector3(waypoints[i].position.x, transform.position.y, waypoints[i].position.z);
            Vector3 p2 = new Vector3(waypoints[i + 1].position.x, transform.position.y, waypoints[i + 1].position.z);

            float dist = Vector3.Distance(p1, p2);
            m_SegmentLengths.Add(dist);
            m_TotalLength += dist;
        }
    }

    /// <summary>
    /// 根据当前米数，绝对匀速地计算出当前位置
    /// </summary>
    private void UpdatePositionAndRotation(float currentDistance)
    {
        float accumulatedDistance = 0f;

        for (int i = 0; i < m_SegmentLengths.Count; i++)
        {
            float segmentLength = m_SegmentLengths[i];

            // 检查目标距离是否落在这两个路点之间
            if (currentDistance <= accumulatedDistance + segmentLength)
            {
                // 算在这段路里的局部百分比
                float remainDistance = currentDistance - accumulatedDistance;
                float segmentPercent = remainDistance / segmentLength;

                Transform startPt = waypoints[i];
                Transform endPt = waypoints[i + 1];

                if (startPt != null && endPt != null)
                {
                    Vector3 p1 = new Vector3(startPt.position.x, transform.position.y, startPt.position.z);
                    Vector3 p2 = new Vector3(endPt.position.x, transform.position.y, endPt.position.z);

                    // 【真·匀速平移】：在这两个路点之间做纯线性插值
                    transform.position = Vector3.Lerp(p1, p2, segmentPercent);

                    // 【转向下一个点】：朝向目标终点
                    if (lookAtTarget)
                    {
                        Vector3 dir = p2 - p1; // 始终沿着当前线段的方向看
                        if (dir.sqrMagnitude > 0.001f)
                        {
                            // 1. 计算出原本沿着线段基础的前进旋转
                            Quaternion baseLookRotation = Quaternion.LookRotation(dir, Vector3.up);

                            // 2. 将面板上配置的自定义 Vector3 倾斜角转换为四元数
                            Quaternion tiltCorrection = Quaternion.Euler(movementTiltEulerAngles);

                            // 3. 核心缝合：叠加自定义倾斜量到前进朝向中
                            Quaternion targetRot = baseLookRotation * tiltCorrection;

                            // 如果在运行状态则平滑过渡，非运行状态拖拽则瞬间对齐，防止预览时卡顿
                            if (Application.isPlaying)
                                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                            else
                                transform.rotation = targetRot;
                        }
                    }
                }
                return;
            }

            accumulatedDistance += segmentLength;
        }

        // 防呆边界：如果超过总长度，死死定在最后一个点
        if (waypoints[waypoints.Count - 1] != null)
        {
            transform.position = new Vector3(waypoints[waypoints.Count - 1].position.x, transform.position.y, waypoints[waypoints.Count - 1].position.z);
        }
    }

    // 在 Scene 窗口画出精美的青色路径折线
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Vector3 p1 = new Vector3(waypoints[i].position.x, transform.position.y, waypoints[i].position.z);
                Vector3 p2 = new Vector3(waypoints[i + 1].position.x, transform.position.y, waypoints[i + 1].position.z);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawWireSphere(p1, 0.15f);
                if (i == waypoints.Count - 2) Gizmos.DrawWireSphere(p2, 0.15f);
            }
        }
    }
}