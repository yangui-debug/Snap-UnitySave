using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 强制要求挂载该脚本的物体必须有 Rigidbody（防穿模）和 NavMeshAgent（智能寻路）
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class SoldierAIMovement : MonoBehaviour
{
    [Header("========== 核心引用获取 (自动获取，无需拖拽) ==========")]
    [Tooltip("读取原脚本里的阵营、标签等信息。如果留空，脚本会自动在同一个物体上找。")]
    public SoldierAnimationController animationController;

    private Rigidbody rb;           // 物理刚体，负责实际移动和防穿模
    private NavMeshAgent agent;     // 寻路组件，只负责算路径，不直接控制移动
    // 仅右桥管理器会返回有效仲裁器；左桥、远程和其他场景完全沿用原移动流程。
    private RightBridgeMeleeUnit bridgeCombat;
    private SoldierHealth movementHealth;
    // 新版红兵自主状态只返回速度；实际 Rigidbody 与 Agent 同步仍由本移动脚本统一负责。
    private RedMeleeSelf redSelf;

    [Header("========== 移动与追逐参数 ==========")]
    [Tooltip("士兵随机移动速度的最小值。推荐：普通步兵填 5.0。")]
    public float 最小移动速度 = 5.0f;

    [Tooltip("士兵随机移动速度的最大值。推荐：普通步兵填 8.0。")]
    public float 最大移动速度 = 8.0f;

    [Tooltip("士兵转身面向移动方向/敌人的速度。越大转身越快。推荐：8~12。")]
    public float rotationSpeed = 10.0f;

    [Tooltip("过桥后的全局寻敌范围（以自己为圆心）。推荐 30~50，确保过桥后立刻能看见敌人，绝不原地发呆。")]
    public float searchRadius = 40.0f;

    [Tooltip("停止移动的基础攻击距离（世界单位）。近战推荐 1.5~2，远程推荐 8~10。攻击脚本的自然随机启用时叠加每人固定的前后偏移，并限制在攻击检测范围内；不改变桥路点或喷射物理。")]
    public float stopDistance = 1.8f;

    [Header("========== 桥路点与过桥设置 ==========")]
    [Tooltip("士兵要去的桥入口（红方这一侧桥头）。")]
    public Transform 桥入口;

    [Tooltip("士兵要去的桥出口（蓝方那一侧桥尾）。如果该物体上挂有 Collider（勾选 Is Trigger），小兵只要进入该碰撞体范围即算过桥完成！")]
    public Transform 桥出口;

    [Tooltip("判定到达桥头/桥尾的距离容差。\nScene 视图中会在桥入口和桥出口处画出【青色圆球】线框展示该判定范围。推荐 2.5 ~ 3.5。")]
    public float 路点到达判定距离 = 3.0f;

    [Tooltip("过桥途中若此范围内出现敌人，立刻停下并迎战，避免被堵门白打。\nScene 视图中会在士兵脚底画出【橙色圆球】线框展示该警戒范围。推荐 4.0 ~ 6.0。")]
    public float 途中迎战警戒距离 = 5.0f;

    [Header("========== 出口碰撞体触发器设置 ==========")]
    [Tooltip("【核心功能】是否允许通过出口物体的 Collider 触发器判定过桥？\n" +
             "勾选后，只要桥出口物体上挂有勾选了 [Is Trigger] 的碰撞体，小兵只要踩入该碰撞体范围内就立刻算过桥完毕！")]
    public bool 启用出口触发器判定 = true;

    [Tooltip("是否在桥出口碰撞体范围内为每个士兵随机分配一个专属到达点？\n" +
             "· 勾选：士兵过桥时不会全扎堆跑向正中心，而是分散在整个出口碰撞体区域（例如横向排开），视觉更自然。\n" +
             "· 不勾选：依然朝桥出口中心跑，但只要碰到边缘碰撞体就判定完成。")]
    public bool 开启出口区域分散到达 = true;

    // 内部引用的出口碰撞体组件（自动读取，无需手动赋值）
    private Collider 桥出口触发器;
    private Vector3 实际出口目标点;
    private bool 已生成出口目标点 = false;

    // 内部三段式过桥状态定义：杜绝往返抽搐的核心
    private enum 过桥阶段状态
    {
        前往入口,
        穿越桥身,
        已过桥自由战斗
    }
    private 过桥阶段状态 当前过桥状态 = 过桥阶段状态.前往入口;

    // 内部状态：当前锁定的敌人
    private Transform targetEnemy;

    // 内部变量：生成时随机决定的最终自主寻路移动速度
    private float 实际移动速度;

    // ========== 喷射物理向量控制变量（与AI寻路完全独立） ==========
    private Vector3 当前喷射方向 = Vector3.zero;
    private float 初始喷射速度 = 0f;
    private float 喷射衰减时间计数 = 0f;
    private float 喷射总持续时间 = 0.5f;

    void Start()
    {
        // 1. 获取组件
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        movementHealth = GetComponent<SoldierHealth>();

        // 2. 核心设置：让 NavMeshAgent 只算路径，不直接控制刚体位置和旋转（防穿模核心）
        agent.updatePosition = false;
        agent.updateRotation = false;

        // 3. 设置 Agent 参数
        agent.acceleration = 60f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0f;
        agent.autoBraking = false;

        // 降低避障计算消耗，避免拥挤停滞
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(0, 100);

        // 4. 自动获取战斗逻辑脚本
        if (animationController == null)
        {
            animationController = GetComponent<SoldierAnimationController>();
        }

        // 5. 随机速度，并同步给 NavMeshAgent
        实际移动速度 = Random.Range(最小移动速度, 最大移动速度);
        agent.speed = 实际移动速度;
    }

    /// <summary>
    /// 由 SoldierSpawner 兵营在生成小兵时调用，分配桥路点。
    /// </summary>
    public void 设置桥路点(Transform 入口, Transform 出口)
    {
        桥入口 = 入口;
        桥出口 = 出口;
        当前过桥状态 = 过桥阶段状态.前往入口;
        已生成出口目标点 = false;

        // 自动检测出口物体上是否挂了碰撞体
        if (桥出口 != null)
        {
            桥出口触发器 = 桥出口.GetComponent<Collider>();
            实际出口目标点 = 桥出口.position;
        }
    }

    /// <summary>
    /// 由 SoldierSpawner 兵营在生成小兵时调用，赋予初始喷射推力。
    /// </summary>
    public void 触发喷射(Vector3 喷射方向, float 喷射速度, float 持续时间)
    {
        喷射方向.y = 0;
        当前喷射方向 = 喷射方向.normalized;
        初始喷射速度 = 喷射速度;
        喷射总持续时间 = 持续时间 > 0f ? 持续时间 : 0.5f;
        喷射衰减时间计数 = 喷射总持续时间;

        if (当前喷射方向.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(当前喷射方向);
        }
    }

    // 物理触发检测：碰到出口触发器碰撞体，立刻判定过桥完成
    void OnTriggerEnter(Collider other)
    {
        if (启用出口触发器判定 && 当前过桥状态 == 过桥阶段状态.穿越桥身)
        {
            if (桥出口触发器 != null && (other == 桥出口触发器 || other.transform == 桥出口))
            {
                完成过桥进入战斗();
            }
        }
    }

    void FixedUpdate()
    {
        if (redSelf == null) redSelf = GetComponent<RedMeleeSelf>();
        // 死亡高于所有移动；击退/僵直先于寻路更新，避免 Agent.Warp 抵消击退。
        if (redSelf != null && movementHealth != null && movementHealth.isDead)
        { rb.velocity = Vector3.up * rb.velocity.y; return; }
        if (movementHealth != null && movementHealth.OriginalHitMovementActive)
        {
            // 原 SoldierHealth 是此时唯一的击退位移写入者；AI 暂停 Warp、路径更新及主动速度。
            // 保留重力和寻路位置同步，结束后按当前目标重算，不能恢复受击前的过期追逐方向。
            if (redSelf != null) redSelf.FilterVelocity(Vector3.zero, animationController != null ? animationController.CurrentTarget : null);
            rb.velocity = Vector3.up * rb.velocity.y;
            if (agent.enabled && agent.isOnNavMesh) agent.nextPosition = transform.position;
            return;
        }
        if (bridgeCombat == null) bridgeCombat = GetComponent<RightBridgeMeleeUnit>();
        if (bridgeCombat == null || bridgeCombat.Director == null || bridgeCombat.Controller == null)
            bridgeCombat = RightBridgeMeleeDirector.FindOrAttach(animationController);
        if (bridgeCombat != null && movementHealth != null && movementHealth.isDead)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }
        // 盾兵的单兵推进由仲裁器执行；不让原自由寻路再写同一个 Rigidbody。
        if (bridgeCombat != null && bridgeCombat.Controlled && bridgeCombat.IsShield) return;
        if (bridgeCombat != null && bridgeCombat.HitMovementActive)
        {
            // 击退期间连寻路路径更新也暂时让位；恢复后下一物理帧重新按当前目标算路径。
            Vector3 hitVelocity = bridgeCombat.FilterVelocity(Vector3.zero, targetEnemy);
            hitVelocity.y += rb.velocity.y;
            rb.velocity = hitVelocity;
            if (agent.enabled && agent.isOnNavMesh) agent.nextPosition = transform.position;
            return;
        }
        // 安全检查：如果 Agent 不在 NavMesh 上，安全拉回
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                return;
            }
        }

        // 核心单向状态机分支
        switch (当前过桥状态)
        {
            case 过桥阶段状态.前往入口:
            case 过桥阶段状态.穿越桥身:
                // 在前往桥或过桥途中，保持近距离警戒（防偷袭/堵门）
                FindTarget(途中迎战警戒距离);

                if (targetEnemy != null)
                {
                    // 附近有敌人堵门，优先停下迎战！
                    agent.SetDestination(targetEnemy.position);
                }
                else
                {
                    // 附近安全，继续推进过桥流程
                    执行过桥流程();
                }
                break;

            case 过桥阶段状态.已过桥自由战斗:
                // 完全过桥后，进行大范围索敌追逐
                FindTarget(searchRadius);

                if (targetEnemy != null)
                {
                    agent.SetDestination(targetEnemy.position);
                }
                else
                {
                    // 如果大范围内没敌人，顺着桥尾方向慢速推进，绝不傻站着
                    if (桥出口 != null)
                    {
                        Vector3 forwardPoint = 桥出口.position + 桥出口.forward * 10f;
                        agent.SetDestination(forwardPoint);
                    }
                }
                break;
        }

        // 移动与物理叠加
        MoveWithAgentAndEject();
    }

    void 执行过桥流程()
    {
        if (桥入口 == null || 桥出口 == null)
        {
            完成过桥进入战斗();
            return;
        }

        if (当前过桥状态 == 过桥阶段状态.前往入口)
        {
            agent.SetDestination(桥入口.position);

            // 当到达桥入口附近时，单向切换为【穿越桥身】，状态不可逆，绝不再回头！
            if (水平距离(transform.position, 桥入口.position) <= 路点到达判定距离)
            {
                当前过桥状态 = 过桥阶段状态.穿越桥身;

                // 核心：进入穿越桥身阶段时，计算专属的出口到达点（若有Collider则在范围内分散）
                确定出口寻路点();
                agent.SetDestination(实际出口目标点);
            }
        }
        else if (当前过桥状态 == 过桥阶段状态.穿越桥身)
        {
            // 判定方式 1：如果开启了触发器判定，且小兵已经在碰撞体的 Bounds 包围盒内
            if (启用出口触发器判定 && 桥出口触发器 != null)
            {
                if (桥出口触发器.bounds.Contains(transform.position))
                {
                    完成过桥进入战斗();
                    return;
                }
            }

            // 判定方式 2：保底距离判定（到达了计算好的出口分散点或中心点）
            if (水平距离(transform.position, 实际出口目标点) <= 路点到达判定距离)
            {
                完成过桥进入战斗();
            }
            else
            {
                agent.SetDestination(实际出口目标点);
            }
        }
    }

    private void 确定出口寻路点()
    {
        if (已生成出口目标点) return;

        if (开启出口区域分散到达 && 桥出口触发器 != null)
        {
            Vector3 randomPoint = 获取碰撞体内随机NavMesh点(桥出口触发器);
            实际出口目标点 = randomPoint;
        }
        else
        {
            实际出口目标点 = 桥出口.position;
        }

        已生成出口目标点 = true;
    }

    private Vector3 获取碰撞体内随机NavMesh点(Collider col)
    {
        if (col == null) return 桥出口.position;

        Bounds b = col.bounds;
        Vector3 randomPos = new Vector3(
            Random.Range(b.min.x + b.size.x * 0.1f, b.max.x - b.size.x * 0.1f),
            b.center.y,
            Random.Range(b.min.z + b.size.z * 0.1f, b.max.z - b.size.z * 0.1f)
        );

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return 桥出口.position;
    }

    private void 完成过桥进入战斗()
    {
        当前过桥状态 = 过桥阶段状态.已过桥自由战斗;
    }

    float 水平距离(Vector3 a, Vector3 b)
    {
        a.y = 0; b.y = 0;
        return Vector3.Distance(a, b);
    }

    void FindTarget(float 当前有效索敌范围)
    {
        // 1. 优先读取攻击脚本（SoldierAnimationController）公开的 CurrentTarget
        if (animationController != null && animationController.CurrentTarget != null)
        {
            SoldierHealth health = animationController.CurrentTarget.GetComponent<SoldierHealth>();
            if (health != null && !health.isDead && 水平距离(transform.position, animationController.CurrentTarget.position) <= 当前有效索敌范围)
            {
                targetEnemy = animationController.CurrentTarget;
                return;
            }
        }

        // 如果旧目标依旧有效且活着，继续追逐
        if (targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
        {
            SoldierHealth health = targetEnemy.GetComponent<SoldierHealth>();
            if (health != null && !health.isDead && 水平距离(transform.position, targetEnemy.position) <= 当前有效索敌范围 * 1.2f)
            {
                return;
            }
        }

        targetEnemy = null;
        if (animationController == null) return;

        // 2. 保底与全方位索敌：如果攻击脚本尚未锁定目标，移动脚本主动 360 度扫描
        Collider[] hits = Physics.OverlapSphere(transform.position, 当前有效索敌范围);
        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (!hit.CompareTag(animationController.soldierTag)) continue;

            SoldierAnimationController otherSoldier = hit.GetComponent<SoldierAnimationController>();
            SoldierHealth otherHealth = hit.GetComponent<SoldierHealth>();

            // 死亡检测与阵营排查
            if (otherHealth != null && otherHealth.isDead) continue;
            if (otherSoldier == null || otherSoldier.myFaction == animationController.myFaction) continue;

            float dist = 水平距离(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = hit.transform;
            }
        }

        targetEnemy = bestTarget;
    }

    void MoveWithAgentAndEject()
    {
        Vector3 aiMoveVelocity = Vector3.zero;

        // 1. 判断是否已到达攻击距离内
        bool reachAttackRange = false;
        // 只改变战斗接近时的停止半径，保留原来的 Rigidbody、NavMesh 与避障流程。
        // 每个角色偏移固定，避免每帧随机导致前后抽搐；上限保证士兵不会停在自身射程之外。
        float effectiveStopDistance = stopDistance;
        if (animationController != null && animationController.NaturalAttackRandomnessActive)
            effectiveStopDistance = Mathf.Clamp(stopDistance + animationController.GetAttackStandingOffset(), 0.05f, Mathf.Max(0.05f, animationController.enemyDetectRange));
        if (targetEnemy != null && 水平距离(transform.position, targetEnemy.position) <= effectiveStopDistance)
        {
            reachAttackRange = true;
        }

        // 2. 如果未进入攻击距离，沿路径移动
        if (!reachAttackRange && agent.hasPath && !agent.isPathStale)
        {
            Vector3 dir = (agent.steeringTarget - transform.position);
            dir.y = 0;

            if (dir.magnitude > 0.1f)
            {
                aiMoveVelocity = dir.normalized * 实际移动速度;
            }
        }

        // 3. 喷射冲量线性衰减（独立叠加）
        Vector3 ejectVelocity = Vector3.zero;
        if (喷射衰减时间计数 > 0f)
        {
            喷射衰减时间计数 -= Time.fixedDeltaTime;
            float 衰减比例 = Mathf.Clamp01(喷射衰减时间计数 / 喷射总持续时间);
            ejectVelocity = 当前喷射方向 * (初始喷射速度 * 衰减比例);
        }

        // 4. 速度叠加并赋给 Rigidbody，保持物理重力与碰撞
        Vector3 finalVelocity = aiMoveVelocity + ejectVelocity;
        // 保留本帧原寻路/过桥/喷射计算，仅在指定右桥单位上仲裁最终速度。
        // 受击时由同一 Rigidbody 写入者执行击退，避免受击协程同时写 Transform。
        if (redSelf != null && redSelf.Controlled)
            finalVelocity = redSelf.FilterVelocity(finalVelocity, targetEnemy);
        else if (bridgeCombat != null && bridgeCombat.Controlled)
            finalVelocity = bridgeCombat.FilterVelocity(finalVelocity, targetEnemy);
        finalVelocity.y = rb.velocity.y;
        rb.velocity = finalVelocity;

        // 5. 同步物理位置给 Agent
        agent.nextPosition = transform.position;

        // 6. 旋转处理：锁定敌人时强行转身面向敌人
        if (targetEnemy != null)
        {
            Vector3 dir = targetEnemy.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else if (aiMoveVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(aiMoveVelocity);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // =========================================================================
    // 可视化绘制调试范围 (Gizmos)
    // =========================================================================
    void OnDrawGizmosSelected()
    {
        // 1. 【黄色】全局索敌范围（小兵脚底）
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        // 2. 【红色】攻击停止距离（小兵脚底）
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // 3. 【橙色】途中迎战警戒距离（小兵脚底：过桥途中此范围内出现敌人会停下反击）
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, 途中迎战警戒距离);

        // 4. 桥入口处绘制：路线与【青色】路点到达判定距离圆球
        if (桥入口 != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, 桥入口.position);

            // 在桥入口画出到达判定圆球
            Gizmos.color = new Color(0f, 1f, 1f, 0.7f);
            Gizmos.DrawWireSphere(桥入口.position, 路点到达判定距离);
        }

        // 5. 桥出口处绘制：路线、【青色】到达判定距离圆球以及【浅蓝色】碰撞体触发区域
        if (桥出口 != null)
        {
            Vector3 exitPos = (实际出口目标点 != Vector3.zero) ? 实际出口目标点 : 桥出口.position;

            if (桥入口 != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(桥入口.position, exitPos);
            }

            // 在桥出口处画出到达判定圆球
            Gizmos.color = new Color(0f, 1f, 1f, 0.7f);
            Gizmos.DrawWireSphere(exitPos, 路点到达判定距离);

            // 如果挂载了 Collider 触发器，将该触发器的外包围盒画出
            if (桥出口触发器 != null)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.45f);
                Gizmos.DrawWireCube(桥出口触发器.bounds.center, 桥出口触发器.bounds.size);
            }
        }
    }
}
