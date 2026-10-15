using UnityEngine;

/// <summary>
/// 基于标签的目标追踪器
/// 自动寻找场景中特定标签的角色，并在进入检测范围后进行追击。
/// </summary>
public class TagBasedChaser : MonoBehaviour
{
    [Header("【目标搜索设置】")]
    [Tooltip("要追踪的角色标签，例如：Soldier")]
    public string targetTag = "Soldier";
    [Tooltip("搜索目标的频率（秒）。设为0.5代表每半秒扫描一次最近的目标，比每帧扫描更节省性能")]
    public float searchInterval = 0.5f;

    [Header("【当前目标状态（自动更新，无需手动拖拽）】")]
    [Tooltip("当前锁定的最近目标")]
    public Transform currentTarget;
    [Tooltip("目标瞄准的偏移量，通常用于对准目标的胸部或头部 (Y轴)")]
    public Vector3 aimOffset = new Vector3(0, 1.2f, 0);

    [Header("【范围与停止设置】")]
    [Tooltip("检测和仇恨触发的最大范围")]
    public float detectionRange = 10.0f;
    [Tooltip("如果没有指定停止区域，则以此距离作为停止距离")]
    public float stopDistance = 1.5f;
    [Tooltip("可选：指定的停止区域。如果为空，代码会自动在场景中寻找名为【停止用范围】的物体")]
    public Collider targetStopArea;

    [Header("【移动控制】")]
    [Tooltip("追击时的移动速度")]
    public float moveSpeed = 3.0f;          
    [Tooltip("转身看向目标的旋转速度")]
    public float rotationSpeed = 10.0f;     

    [Header("【动画设置】")]
    [Tooltip("控制角色移动动画的Animator（如果为空会自动尝试获取自身组件）")]
    public Animator animator;

    // --- 内部状态记录 ---
    private Vector3 lastPosition;
    private Vector3 currentTargetPos;
    private bool isInStoppedState = false;
    private bool isCurrentlyChasing = false;
    private float searchTimer = 0f;

    void Start()
    {
        lastPosition = transform.position;
        
        if (animator == null) 
            animator = GetComponent<Animator>();
        
        isInStoppedState = false;

        // 核心优化：自动寻找名为【停止用范围】的碰撞体
        if (targetStopArea == null)
        {
            GameObject stopAreaObj = GameObject.Find("【停止用范围】");
            if (stopAreaObj != null)
            {
                targetStopArea = stopAreaObj.GetComponent<Collider>();
                Debug.Log($"[{gameObject.name}] 已成功自动绑定【停止用范围】");
            }
        }
    }

    void Update()
    {
        // 1. 无论是由谁控制的移动，只要有真实的物理位移，就计算出实际速度用于动画
        float currentActualMoveSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        // 2. 计时器：定期搜索范围内最近的目标
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            FindClosestTarget();
            searchTimer = searchInterval;
        }

        // 3. 如果当前没有目标，或者目标已失活/被销毁
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            // 目标消失时，确保恢复游荡
            if (isCurrentlyChasing)
            {
                isCurrentlyChasing = false;
                SendMessageUpwards("ResumeWander", transform, SendMessageOptions.DontRequireReceiver);
            }

            // 更新游荡时的动画并退出后续追击逻辑
            UpdateAnimation(currentActualMoveSpeed > 0.01f ? currentActualMoveSpeed : 0f);
            lastPosition = transform.position;
            return;
        }

        // 4. 计算目标点（应用偏移）并计算水平距离 (不考虑Y轴高度差)
        currentTargetPos = currentTarget.TransformPoint(aimOffset);
        float distToTargetCenter = GetFlatDistance(transform.position, currentTargetPos);
        
        // 判断当前帧是否“应该”追击
        bool shouldChase = distToTargetCenter <= detectionRange;

        // 状态发生改变时，通知父级的游荡脚本
        if (shouldChase != isCurrentlyChasing)
        {
            isCurrentlyChasing = shouldChase;
            if (isCurrentlyChasing)
            {
                // 通知父级暂停游荡，接管控制权
                SendMessageUpwards("SuspendWander", transform, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // 通知父级恢复游荡
                SendMessageUpwards("ResumeWander", transform, SendMessageOptions.DontRequireReceiver);
            }
        }

        // 5. 如果在仇恨（触发）范围内，执行移动和转向
        if (shouldChase)
        {
            bool isInsideCollider = false;
            float distanceToEdge = 0f;

            // 处理停止逻辑：基于 Collider 还是 距离
            if (targetStopArea != null)
            {
                Vector3 closestPoint = targetStopArea.ClosestPoint(transform.position);
                // 判断是否在 Collider 内部
                isInsideCollider = (closestPoint - transform.position).sqrMagnitude < 0.001f;
                distanceToEdge = GetFlatDistance(transform.position, closestPoint);
            }
            else
            {
                distanceToEdge = distToTargetCenter;
            }

            float stoppingBuffer = 0.1f; // 停止缓冲区，防止抽搐抖动
            bool isTooClose = isInsideCollider || distanceToEdge < (stopDistance - stoppingBuffer);
            bool isTooFar = distanceToEdge > (stopDistance + stoppingBuffer);

            // 移动和转向逻辑
            if (isTooClose || isTooFar)
            {
                isInStoppedState = false;

                // 平滑转向目标
                Vector3 lookDir = new Vector3(currentTargetPos.x, transform.position.y, currentTargetPos.z) - transform.position;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                // 前进或后退
                if (isTooClose)
                {
                    transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.Self);
                }
                else
                {
                    transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
                }
            }
            else
            {
                // 在理想的停止范围内
                TurnDirectlyToFace();
                isInStoppedState = true;
            }
        }
        else
        {
            // 目标超出追踪范围
            isInStoppedState = true;
        }

        // 6. 处理自动播放动画 (过滤掉微小的抖动)
        if (isInStoppedState && currentActualMoveSpeed < 0.01f) 
        {
             currentActualMoveSpeed = 0f;
        }
        
        UpdateAnimation(currentActualMoveSpeed);
        lastPosition = transform.position;
    }

    /// <summary>
    /// 寻找带有指定标签且距离最近的目标
    /// </summary>
    private void FindClosestTarget()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);
        float closestDistance = detectionRange; // 只关心在检测范围内的目标
        Transform bestTarget = null;

        foreach (GameObject obj in potentialTargets)
        {
            // 确保不把自身当做目标，并且目标是激活状态
            if (obj == this.gameObject || !obj.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestTarget = obj.transform;
            }
        }

        currentTarget = bestTarget;
    }

    // 瞬时转向目标
    void TurnDirectlyToFace()
    {
        Vector3 lookDir = new Vector3(currentTargetPos.x, transform.position.y, currentTargetPos.z) - transform.position;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = targetRotation; 
        }
    }

    // 更新动画混合树的 Speed 参数
    void UpdateAnimation(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
        }
    }

    // 计算平面距离 (X, Z)
    float GetFlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    // 可视化 Gizmos：只在物体被选中时在 Scene 窗口绘制
    void OnDrawGizmosSelected()
    {
        // 1. 怪物范围相关的可视化 (黄色线框代表侦测/仇恨范围)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 2. 停止距离可视化 (如果没有指定的 targetStopArea)
        if (targetStopArea == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }

        // 3. 目标相关的可视化
        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            Vector3 centerPos = currentTarget.TransformPoint(aimOffset);
            
            // 绘制半透明绿色球体，表示怪物眼中瞄准的实际点位
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawSphere(centerPos, 0.3f); 
            
            // 画一条线连接自身和目标
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, centerPos);
        }
    }
}