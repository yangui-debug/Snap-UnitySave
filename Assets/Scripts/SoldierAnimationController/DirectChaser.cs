using UnityEngine;

public class DirectChaser : MonoBehaviour
{
    [Header("目标设置")]
    public Transform target;
    [Tooltip("目标瞄准的偏移量，通常用于对准目标的胸部或头部 (y轴)")]
    public Vector3 aimOffset = new Vector3(0, 1.2f, 0);
    [Tooltip("可选：指定一个 Collider 作为目标的停止区域。如果设置，怪物到达 Collider 边界即停止")]
    public Collider targetStopArea;
    [Tooltip("检测和仇恨范围")]
    public float detectionRange = 10.0f;

    [Header("移动控制")]
    [Tooltip("追击速度")]
    public float moveSpeed = 3.0f;          
    [Tooltip("转向目标的旋转速度")]
    public float rotationSpeed = 10.0f;     
    [Tooltip("如果没有指定 targetStopArea，则以此距离作为停止距离")]
    public float stopDistance = 1.5f;

    [Header("可选设置（如果没有动画可为空）")]
    public Animator animator;

    // 内部状态记录
    private Vector3 lastPosition;
    private Vector3 currentTargetPos;
    private bool isInStoppedState = false;
    
    // === 【优化】记录当前是否处于追击状态，用于判断何时发送切换消息 ===
    private bool isCurrentlyChasing = false; 

    void Start()
    {
        lastPosition = transform.position;
        // 如果没有拖入 Animator，尝试在自身获取
        if (animator == null) animator = GetComponent<Animator>();
        isInStoppedState = false;
    }

    void Update()
    {
        // === 【优化】动画计算移到最前面 ===
        // 无论是由谁（追击脚本还是游荡脚本）控制的移动，只要有真实的物理位移，就计算出实际速度
        float currentActualMoveSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        // 1. 如果没有指定目标，或者目标被隐藏/销毁了
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            // === 【优化】目标消失时，确保恢复游荡 ===
            if (isCurrentlyChasing)
            {
                isCurrentlyChasing = false;
                // 通知父级恢复游荡
                SendMessageUpwards("ResumeWander", transform, SendMessageOptions.DontRequireReceiver);
            }

            // === 【优化】更新游荡时的动画并退出后续追击逻辑 ===
            UpdateAnimation(currentActualMoveSpeed > 0.01f ? currentActualMoveSpeed : 0f);
            lastPosition = transform.position;
            return;
        }

        // 计算目标点（应用偏移）
        currentTargetPos = target.TransformPoint(aimOffset);
        // 计算水平距离 (不考虑y轴)
        float distToTargetCenter = GetFlatDistance(transform.position, currentTargetPos);
        
        // 判断当前帧是否“应该”追击
        bool shouldChase = distToTargetCenter <= detectionRange;

        // === 【优化核心逻辑：状态发生改变时，通知父级的游荡脚本】 ===
        if (shouldChase != isCurrentlyChasing)
        {
            isCurrentlyChasing = shouldChase;
            if (isCurrentlyChasing)
            {
                // 通知父级暂停游荡，自己接管（不需要互相引用，无游荡脚本也不会报错）
                SendMessageUpwards("SuspendWander", transform, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // 通知父级恢复游荡
                SendMessageUpwards("ResumeWander", transform, SendMessageOptions.DontRequireReceiver);
            }
        }

        // 4. 判断是否在仇恨（触发）范围内
        if (shouldChase)
        {
            bool isInsideCollider = false;
            float distanceToEdge = 0f;

            // 处理停止逻辑
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

            float stoppingBuffer = 0.1f; // 停止缓冲区
            bool isTooClose = isInsideCollider || distanceToEdge < (stopDistance - stoppingBuffer);
            bool isTooFar = distanceToEdge > (stopDistance + stoppingBuffer);

            // 移动和转向逻辑
            if (isTooClose || isTooFar)
            {
                isInStoppedState = false;

                // 转向目标
                Vector3 lookDir = new Vector3(currentTargetPos.x, transform.position.y, currentTargetPos.z) - transform.position;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                // 前进或后退
                if (isTooClose)
                {
                    // 太近了，后退
                    transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.Self);
                }
                else
                {
                    // 太远了，前进
                    transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
                }
            }
            else
            {
                // 在停止范围内
                TurnDirectlyToFace();
                isInStoppedState = true;
            }
        }
        else
        {
            // 目标不在范围内
            isInStoppedState = true;
        }

        // 5. 处理自动播放动画
        // === 【优化】处理自动播放动画 (过滤掉微小的抖动) ===
        if (isInStoppedState && currentActualMoveSpeed < 0.01f) 
        {
             currentActualMoveSpeed = 0f;
        }
        
        UpdateAnimation(currentActualMoveSpeed);
        lastPosition = transform.position;
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
            // 如果动画参数名不是 "Speed"，请修改这里
            animator.SetFloat("Speed", speed);
        }
    }

    // 计算平面距离 (x, z)
    float GetFlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    // === 【你要的可视化 Gizmos】就在这里！ ===
    // 只在物体被选中时绘制 Gizmos
    void OnDrawGizmosSelected()
    {
        // 1. 目标相关的可视化
        if (target != null && target.gameObject.activeInHierarchy)
        {
            // 计算应用了偏移后的实际瞄准点位置
            Vector3 centerPos = target.TransformPoint(aimOffset);
            
            // 绘制绿色球体，表示怪物眼中的目标点
            Gizmos.color = new Color(0, 1, 0, 0.5f); // 半透明绿色
            Gizmos.DrawSphere(centerPos, 0.3f); 
            
            // 2. 怪物范围相关的可视化
            // 绘制黄色线框球体，表示检测和仇恨范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 3. 停止距离可视化 (如果没有 targetStopArea)
            if (targetStopArea == null)
            {
                // 绘制红色线框球体，表示怪物会在此距离停下
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, stopDistance);
            }
            // 注意：如果使用了 targetStopArea Collider，因为 Collider 的形状多种多样，
            // 最好直接查看 Collider 组件本身的可视化线框。
        }
    }
}