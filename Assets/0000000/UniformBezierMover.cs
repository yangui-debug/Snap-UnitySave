using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways] // 确保在编辑模式下也能在 Scene 窗口看到精准的绿色匀速路径线
public class UniformBezierMover : MonoBehaviour
{
    [Header("【1】路径节点设置")]
    public Transform pointA;
    [Tooltip("控制点（B点）：用来把路径往上拉成抛物线。B点有多高，抛物线就拱多高。")]
    public Transform pointB;
    public Transform pointC;

    [Header("【2】匀速移动配置")]
    [Tooltip("物体的移动速度（单位：米/秒）。无论是直道还是大弯道，物体都会死死保持这个物理速度匀速前进！")]
    public float moveSpeed = 5f;

    [Header("【3】测试开关")]
    [Tooltip("在游戏运行时，勾选此开关或者在代码里调用 StartMove() 即可触发移动")]
    public bool triggerMove = false;

    // 内部高精匀速数据
    private float m_TotalLength = 0f;          // 抛物线的绝对总长度（米）
    private List<float> m_DistanceTable = new List<float>(); // 距离采样表
    private const int SAMPLE_COUNT = 100;     // 采样精度（切成100份足够极为精准）

    private float m_CurrentDistance = 0f;     // 当前已经走过的物理距离（米）
    private bool m_IsMoving = false;
    private bool m_LastTriggerState = false;

    void Start()
    {
        if (Application.isPlaying)
        {
            InitializeUniformPath();
        }
    }

    void Update()
    {
        // 安全检查
        if (pointA == null || pointB == null || pointC == null) return;

        // ================== 运行时的匀速移动逻辑 ==================
        if (Application.isPlaying)
        {
            // 方便在面板上勾选测试
            if (triggerMove && !m_LastTriggerState && !m_IsMoving)
            {
                StartMove();
            }
            m_LastTriggerState = triggerMove;

            if (m_IsMoving)
            {
                // 1. 根据时间和设定的 moveSpeed，计算这一帧物体【应该走过多少米】
                m_CurrentDistance += moveSpeed * Time.deltaTime;

                // 2. 检查是否到达终点
                if (m_CurrentDistance >= m_TotalLength)
                {
                    transform.position = pointC.position;
                    m_IsMoving = false;
                    triggerMove = false;
                    m_CurrentDistance = 0f;
                    Debug.Log("[匀速到达]：物体已完美匀速抵达终点 C 点！");
                    return;
                }

                // 3. 【核心黑科技】：根据走过的物理米数，反向映射出绝对匀速的贝塞尔 t 进度
                float uniformT = GetTFromDistance(m_CurrentDistance);

                // 4. 更新物体坐标
                transform.position = CalculateBezierPoint(uniformT, pointA.position, pointB.position, pointC.position);
            }
        }
    }

    /// <summary>
    /// 公开触发函数：供你的碰撞、卡牌点击或 Timeline 事件一键调用
    /// </summary>
    public void StartMove()
    {
        if (pointA == null || pointB == null || pointC == null) return;

        // 重新初始化路径（防止你在游戏运行时动态挪动了 A B C 的位置）
        InitializeUniformPath();

        m_CurrentDistance = 0f;
        transform.position = pointA.position;
        m_IsMoving = true;
        triggerMove = true;
    }

    /// <summary>
    /// 【预计算核心】将曲线切丝，建立“时间/进度 ➡️ 物理距离”的映射表
    /// </summary>
    private void InitializeUniformPath()
    {
        m_DistanceTable.Clear();
        m_DistanceTable.Add(0f);
        m_TotalLength = 0f;

        Vector3 previousPosition = pointA.position;

        // 采样 100 个点，累加线段长度
        for (int i = 1; i <= SAMPLE_COUNT; i++)
        {
            float t = i / (float)SAMPLE_COUNT;
            Vector3 currentPosition = CalculateBezierPoint(t, pointA.position, pointB.position, pointC.position);

            // 累加这一小段的空间物理距离
            m_TotalLength += Vector3.Distance(previousPosition, currentPosition);
            m_DistanceTable.Add(m_TotalLength);

            previousPosition = currentPosition;
        }
    }

    /// <summary>
    /// 【高精映射】二分法查找：根据当前走过的距离，算出真正的、被修正后的匀速 t 百分比
    /// </summary>
    private float GetTFromDistance(float targetDistance)
    {
        if (targetDistance <= 0f) return 0f;
        if (targetDistance >= m_TotalLength) return 1f;

        // 在采样表里用二分法快速定位
        int low = 0;
        int high = SAMPLE_COUNT;

        while (low < high - 1)
        {
            int mid = (low + high) / 2;
            if (m_DistanceTable[mid] > targetDistance) high = mid;
            else low = mid;
        }

        // 进行线性插值，保证绝对丝滑，没有任何肉眼可见的卡顿
        float distLow = m_DistanceTable[low];
        float distHigh = m_DistanceTable[high];
        float tLow = low / (float)SAMPLE_COUNT;
        float tHigh = high / (float)SAMPLE_COUNT;

        float segmentPercent = (targetDistance - distLow) / (distHigh - distLow);
        return Mathf.Lerp(tLow, tHigh, segmentPercent);
    }

    /// <summary>
    /// 标准二阶贝塞尔曲线公式
    /// </summary>
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
    }

    // 在编辑器 Scene 窗口画出精细的绿色匀速轨迹线
    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null || pointC == null) return;

        Gizmos.color = Color.green;
        Vector3 prev = pointA.position;
        InitializeUniformPath(); // 编辑器下实时刷新

        // 每隔固定米数点一个点，如果点与点之间间距完全一样，证明绝对匀速成功！
        for (float d = 0; d <= m_TotalLength; d += m_TotalLength / 20f)
        {
            float t = GetTFromDistance(d);
            Vector3 curr = CalculateBezierPoint(t, pointA.position, pointB.position, pointC.position);
            Gizmos.DrawLine(prev, curr);
            Gizmos.DrawSphere(curr, 0.05f); // 画出匀速轨迹粒子点
            prev = curr;
        }
    }
}