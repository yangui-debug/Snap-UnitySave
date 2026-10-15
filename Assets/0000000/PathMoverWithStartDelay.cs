using UnityEngine;
using System.Collections.Generic;

public class PathMoverWithStartDelay : MonoBehaviour
{
    [Header("【1】路径节点设置")]
    [Tooltip("按顺序放入平地路点，物体会一口气走完它们")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("【2】速度与启动延迟配置")]
    [Tooltip("物体的移动速度（米/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("【核心新增】延时启动时间（秒）。游戏开始（或调用脚本）后，物体会在起点原地发呆这么久，然后才动身。")]
    public float startDelayTime = 3.0f;

    [Tooltip("判定到达路点的距离阈值")]
    public float arrivalDistance = 0.05f;

    [Header("【3】旋转配置")]
    public bool lookAtTarget = true;
    public float turnSpeed = 10f;

    [Header("【4】自动启动控制")]
    [Tooltip("如果勾选，游戏一运行（Awake/Start）就会立刻进入倒计时，时间到了自己出发。如果不勾选，则需要通过 Timeline 事件或别的脚本调用 StartPathMoveWithDelay() 来手动开启倒计时。")]
    public bool autoStartOnPlay = true;

    // 内部状态机变量
    private int m_CurrentWaypointIndex = 0;
    private bool m_IsMoving = false;

    // 延时启动核心控制变量
    private bool m_IsInDelayStage = false;
    private float m_DelayTimer = 0f;

    void Start()
    {
        // 如果勾选了自动启动，游戏一开局就直接进入延时准备状态
        if (Application.isPlaying && autoStartOnPlay)
        {
            StartPathMoveWithDelay();
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        // ================== 【核心新增】开局延时等待逻辑 ==================
        if (m_IsInDelayStage)
        {
            m_DelayTimer -= Time.deltaTime; // 倒计时

            // 【高级视觉优化】：在起点发呆等待的这几秒内，让物体提前把身体转下接下来要去的第二个点
            if (lookAtTarget && waypoints.Count > 1 && waypoints[1] != null)
            {
                Vector3 dir = new Vector3(waypoints[1].position.x, transform.position.y, waypoints[1].position.z) - transform.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
            }

            // 倒计时结束，正式起兵出发！
            if (m_DelayTimer <= 0f)
            {
                m_IsInDelayStage = false;
                m_IsMoving = true;
                m_CurrentWaypointIndex = 1; // 目标直接锁定第二个路点
                Debug.Log("[延时结束]：时间已到，物体正式开始向路径终点发起全速冲锋！");
            }

            return; // 在延时准备期间，死死卡住后面的位移代码，让物体在起点绝对不准动
        }

        // ================== 连续不断的标准匀速平移逻辑 ==================
        if (m_IsMoving)
        {
            Transform targetPoint = waypoints[m_CurrentWaypointIndex];
            if (targetPoint == null)
            {
                NextWaypoint();
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 targetPosFlattened = new Vector3(targetPoint.position.x, currentPos.y, targetPoint.position.z);

            // 移动时的身体转向
            if (lookAtTarget)
            {
                Vector3 dir = targetPosFlattened - currentPos;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
            }

            // 绝对匀速移动
            transform.position = Vector3.MoveTowards(currentPos, targetPosFlattened, moveSpeed * Time.deltaTime);

            // 踩到路点判定
            if (Vector3.Distance(transform.position, targetPosFlattened) <= arrivalDistance)
            {
                NextWaypoint(); // 到站了，不停留！立刻切到下一个路点继续走
            }
        }
    }

    /// <summary>
    /// 公开触发函数：开启延时启动倒计时。
    /// 可以由 Timeline 的 Signal（信号轨道）或者自定义方法在几秒时一脚踹开这个函数。
    /// </summary>
    public void StartPathMoveWithDelay()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        // 1. 强制瞬间把物体传送到第一个点（路点 0）作为起始驻扎地
        if (waypoints[0] != null)
        {
            transform.position = new Vector3(waypoints[0].position.x, transform.position.y, waypoints[0].position.z);
        }

        // 2. 进入延时倒计时阶段
        m_IsInDelayStage = true;
        m_IsMoving = false;
        m_DelayTimer = startDelayTime;

        Debug.Log($"[准备就绪]：物体已就位起点，开始进入 {startDelayTime} 秒发呆倒计时...");
    }

    /// <summary>
    /// 连续切点状态机
    /// </summary>
    private void NextWaypoint()
    {
        m_CurrentWaypointIndex++;

        // 如果后面还有路点，由于我们没有加到站停顿逻辑，下一帧 Update 就会无缝继续匀速走过去
        if (m_CurrentWaypointIndex >= waypoints.Count)
        {
            m_IsMoving = false;
            m_IsInDelayStage = false;
            m_CurrentWaypointIndex = 0;
            Debug.Log("[全线通车]：物体已连续一口气走完所有路点，顺利抵达终点！");
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