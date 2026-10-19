using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 由右桥管理器按对象/区域自动安装的单兵移动仲裁器。配置统一公开在管理器，避免运行时单兵参数分叉。
/// 红方：SoldierAIMovement 提交期望速度，本组件返回受优先级约束的速度，仍由原脚本写 Rigidbody。
/// 盾兵：原移动脚本未启用时，本组件独立写 Rigidbody；不移动 Timeline 绑定的编组节点。
/// </summary>
[DisallowMultipleComponent]
public sealed class RightBridgeMeleeUnit : MonoBehaviour
{
    public RightBridgeMeleeDirector Director { get; private set; }
    public SoldierAnimationController Controller { get; private set; }
    public bool IsShield { get; private set; }
    public float Radius { get; private set; }
    public Vector3 ReservedPosition { get; set; }
    public float ReservationUntil { get; set; }
    public bool Controlled => isActiveAndEnabled && Director != null && Director.Active &&
        Controller != null && Controller.isActiveAndEnabled && health != null && !health.isDead && gameObject.activeInHierarchy &&
        Director.Contains(transform.position);
    public bool HitMovementActive => Controlled && Director.enableHitMovementPriority && Time.time < stunUntil;
    public bool WaitingForRefill => Controlled && (queueBlocked || needsRefill && Time.time < refillAt);
    public Vector3 FixedAnchor => anchor;
    public bool HasFixedAnchor => anchorSet;
    public Transform MotionRoot => motionRoot != null ? motionRoot : transform;
    /// <summary>运行时诊断状态，仅供验收工具读取，不参与控制。</summary>
    public string LastMovementDecision { get; private set; }

    private SoldierHealth health;
    private Rigidbody body;
    private NavMeshAgent agent;
    private SoldierAIMovement movement;
    private Collider ownCollider;
    private Transform motionRoot;
    private bool agentOwnership;
    private bool originalAgentPosition;
    private bool originalAgentRotation;
    private RaycastHit[] sweepHits = new RaycastHit[16];
    private Collider[] overlapHits = new Collider[16];
    private Vector3 anchor;
    private bool anchorSet;
    private bool following;
    private bool queueBlocked;
    private bool needsRefill;
    private float refillAt = -1f;
    private Transform refillTarget;
    private SoldierHealth previousTargetHealth;
    private float knockbackUntil;
    private float stunUntil;
    private Vector3 knockbackVelocity;
    private bool submittedVelocity;

    public void Configure(RightBridgeMeleeDirector director, SoldierAnimationController controller, bool shield)
    {
        // 组件重新启用/域重载后重新取得非序列化依赖；保留既有固定基准及等待状态。
        Director = director;
        Controller = controller;
        IsShield = shield;
        health = GetComponent<SoldierHealth>();
        body = GetComponent<Rigidbody>();
        motionRoot = transform;
        if (shield)
        {
            // 本场景部分盾兵实例已移除 Rigidbody，攻击脚本位于 Animator 模型根上。
            // 必须移动模型外层的单兵容器，否则动画会把模型本地位置写回原点。
            // Timeline 绑定的是 shieldRoot 的直接子编组，绝不向上提升到这些节点。
            while (motionRoot.parent != null && motionRoot.parent != director.shieldRoot &&
                   motionRoot.parent.parent != director.shieldRoot)
            {
                Transform candidateRoot = motionRoot.parent;
                if (candidateRoot.GetComponent<Animator>() != null) break;
                int count = 0;
                foreach (var candidate in candidateRoot.GetComponentsInChildren<SoldierAnimationController>(true))
                    if (candidate.isActiveAndEnabled) count++;
                if (count != 1) break;
                motionRoot = candidateRoot;
            }
            body = MotionRoot.GetComponent<Rigidbody>();
        }
        if (body == null && shield)
        {
            var parentBody = GetComponentInParent<Rigidbody>();
            if (parentBody != null && parentBody.transform != director.shieldRoot)
            {
                int activeControllers = 0;
                foreach (var candidate in parentBody.GetComponentsInChildren<SoldierAnimationController>(true))
                    if (candidate.isActiveAndEnabled) activeControllers++;
                // 只提升到当前单兵的物理根，绝不提升到包含多名士兵的公共节点。
                if (activeControllers == 1) { body = parentBody; motionRoot = parentBody.transform; }
            }
        }
        agent = MotionRoot.GetComponent<NavMeshAgent>();
        movement = GetComponent<SoldierAIMovement>();
        ownCollider = MotionRoot.GetComponent<Collider>();
        if (ownCollider == null || !ownCollider.enabled) ownCollider = GetComponent<Collider>();
        Radius = Mathf.Max(0.05f, director.minimumUnitRadius);
        if (ownCollider != null)
            Radius = Mathf.Max(Radius, Mathf.Min(ownCollider.bounds.extents.x, ownCollider.bounds.extents.z));
    }

    /// <summary>只在原受击脚本确认非致命受击时调用；保留原距离、方向轴向、时长和僵直配置。</summary>
    public bool BeginHit(Vector3 hitDirection, float distance, float duration, float stun, Vector3 axes)
    {
        if (!Controlled || !Director.enableHitMovementPriority) return false;
        Vector3 dir = hitDirection.normalized;
        dir = Vector3.Scale(dir, axes);
        float safeDuration = Mathf.Max(0.01f, duration);
        bool hasKnockback = dir.sqrMagnitude > 0.001f && Mathf.Abs(distance) > 0.000001f;
        knockbackVelocity = hasKnockback ? dir.normalized * (distance / safeDuration) : Vector3.zero;
        knockbackUntil = Time.time + (hasKnockback ? safeDuration : 0f);
        // 本场景盾兵原击退/僵直均可设为零。不能把防除零的最小时长变成隐性僵直，
        // 否则密集受击会不断刷新极短锁定，导致盾兵永远没有机会执行跟进。
        stunUntil = Time.time + Mathf.Max(hasKnockback ? safeDuration : 0f, Mathf.Max(0f, stun));
        following = false;
        CancelReservation();
        return true;
    }

    /// <summary>攻击脚本仅通知旧目标死亡以安排红方补位，绝不改动原索敌条件或当前攻击事件。</summary>
    private void ObserveTarget(Transform target)
    {
        if (!ReferenceEquals(previousTargetHealth, null) && (previousTargetHealth == null || previousTargetHealth.isDead) && !IsShield)
        {
            needsRefill = true;
            refillAt = -1f;
        }
        previousTargetHealth = target != null ? target.GetComponent<SoldierHealth>() : null;
    }

    private bool TargetValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        var targetHealth = target.GetComponent<SoldierHealth>();
        return targetHealth != null && !targetHealth.isDead;
    }

    /// <summary>红方原寻路/喷射速度的唯一仲裁入口；死亡由外层脚本停止，击退优先于喷射和追逐。</summary>
    public Vector3 FilterVelocity(Vector3 desiredVelocity, Transform target)
    {
        if (!Controlled) { ReleaseControl(); return desiredVelocity; }
        submittedVelocity = true;
        ObserveTarget(target);
        if (HitMovementActive)
        {
            LastMovementDecision = "hit_priority";
            CancelReservation();
            return Time.time < knockbackUntil ? SafeVelocity(knockbackVelocity) : Vector3.zero;
        }
        Vector3 horizontal = Vector3.ProjectOnPlane(desiredVelocity, Vector3.up);
        if (!TargetValid(target))
        {
            LastMovementDecision = "no_live_target";
            CancelRefillTimer();
            CancelReservation();
            // 失去目标时不允许“向旧空位补位”；非补位情形保留原过桥/行进速度。
            return needsRefill ? Vector3.zero : desiredVelocity;
        }
        Vector3 facing = IsShield ? Director.Axis : -Director.Axis;
        bool blocked = Director.IsQueueBlocked(this, facing);
        if (blocked)
        {
            LastMovementDecision = "reserve_retreat_space";
            queueBlocked = true;
            needsRefill = !IsShield;
            CancelRefillTimer();
            CancelReservation();
            return Vector3.zero;
        }
        queueBlocked = false;
        if (!IsShield && needsRefill && Director.enableRedRefillDelay)
        {
            // 前路重新开放才启动随机等待。中途换目标/再次被阻挡时重新判断，不沿旧点冲刺。
            if (refillAt < 0f || refillTarget != target)
            {
                float min = Mathf.Max(0f, Mathf.Min(Director.refillDelay.x, Director.refillDelay.y));
                float max = Mathf.Max(min, Mathf.Max(Director.refillDelay.x, Director.refillDelay.y));
                refillAt = Time.time + UnityEngine.Random.Range(min, max);
                refillTarget = target;
            }
            if (Time.time < refillAt) { LastMovementDecision = "refill_delay"; CancelReservation(); return Vector3.zero; }
        }
        if (horizontal.sqrMagnitude < 0.000001f) { CancelReservation(); return desiredVelocity; }
        // 预留最终接战空位，而不是只预留当前一小步；防止多个后排同时抢同一缺口。
        Vector3 toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        float stop = IsShield ? Director.shieldStopDistance : (movement != null ? movement.stopDistance : 1.8f);
        Vector3 destination = target.position - toTarget.normalized * stop;
        destination.y = transform.position.y;
        if (!Director.TryReserve(this, destination))
        {
            LastMovementDecision = "slot_reserved";
            if (!IsShield) { needsRefill = true; CancelRefillTimer(); }
            CancelReservation();
            return Vector3.zero;
        }
        needsRefill = false;
        LastMovementDecision = "move";
        CancelRefillTimer();
        return SafeVelocity(desiredVelocity);
    }

    void FixedUpdate()
    {
        if (!Controlled) { ReleaseControl(); return; }
        if (IsShield && agent != null && !agentOwnership)
        {
            // 单兵物理根上的旧 Agent 也不得与战斗位移争抢位置；退出控制时恢复原配置。
            originalAgentPosition = agent.updatePosition; originalAgentRotation = agent.updateRotation;
            agent.updatePosition = false; agent.updateRotation = false; agentOwnership = true;
        }
        // 红方速度由现有移动组件统一写入，本组件不再产生第二个位置/速度写入者。
        if (!IsShield && movement != null && movement.isActiveAndEnabled) return;
        Vector3 velocity = Vector3.zero;
        Transform target = Controller.CurrentTarget;
        if (HitMovementActive)
            velocity = Time.time < knockbackUntil ? SafeVelocity(knockbackVelocity) : Vector3.zero;
        else if (IsShield && Director.enableShieldFollow && TargetValid(target))
        {
            if (!anchorSet) { anchor = transform.position; anchorSet = true; }
            Vector3 delta = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
            float stop = Mathf.Max(0f, Director.shieldStopDistance + Controller.GetAttackStandingOffset());
            float axialGap = Vector3.Dot(delta, Director.Axis);
            if (delta.magnitude > stop + (following ? 0f : Director.followHysteresis) && axialGap > 0f)
            {
                following = true;
                // 目标可能在斜前方。用停止圆与战线轴的交点计算剩余前进量，
                // 不能直接 axialGap-stop，否则斜向距离尚远时会错误算出零步长。
                float lateralSquared = Mathf.Max(0f, delta.sqrMagnitude - axialGap * axialGap);
                float stopAlongAxis = Mathf.Sqrt(Mathf.Max(0f, stop * stop - lateralSquared));
                float step = Mathf.Min(Director.followSpeed * Time.fixedDeltaTime, Mathf.Max(0f, axialGap - stopAlongAxis));
                velocity = FilterVelocity(Director.Axis * (step / Time.fixedDeltaTime), target);
            }
            else { following = false; CancelReservation(); }
        }
        else { LastMovementDecision = "no_follow_target_or_disabled"; following = false; CancelReservation(); }
        WriteVelocity(velocity);
    }

    private void WriteVelocity(Vector3 velocity)
    {
        submittedVelocity = true;
        if (body != null && !body.isKinematic)
        {
            // 水平击退保留物理重力；配置了垂直击退时保留原受击方向上的垂直分量。
            velocity.y += body.velocity.y;
            body.velocity = velocity;
        }
        else MotionRoot.position += velocity * Time.fixedDeltaTime;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.nextPosition = MotionRoot.position;
    }

    /// <summary>将单帧位移限制到固定战线边界，再用真实 Collider 包围尺寸扫掠阻挡物。</summary>
    private Vector3 SafeVelocity(Vector3 velocity)
    {
        Vector3 step = velocity * Time.fixedDeltaTime;
        if (IsShield)
        {
            if (!anchorSet) { anchor = transform.position; anchorSet = true; }
            Vector3 axis = Director.Axis;
            float current = Vector3.Dot(transform.position - anchor, axis);
            float requested = Vector3.Dot(step, axis);
            float allowed = Mathf.Clamp(current + requested, -Mathf.Max(0f, Director.maxRetreat), Mathf.Max(0f, Director.maxAdvance));
            // 边界收窄/外力推过界时不反向瞬移，只允许逐步朝合法区域返回。
            float actual = allowed - current;
            if (requested >= 0f) actual = Mathf.Clamp(actual, 0f, requested);
            else actual = Mathf.Clamp(actual, requested, 0f);
            step += axis * (actual - requested);
        }
        float distance = step.magnitude;
        if (distance <= 0.000001f) { LastMovementDecision = "fixed_boundary"; return Vector3.zero; }
        Vector3 direction = step / distance;
        Bounds bounds = ownCollider != null ? ownCollider.bounds : new Bounds(transform.position + Vector3.up, new Vector3(Radius * 2f, 1.8f, Radius * 2f));
        float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) - Director.collisionSkin);
        float height = Mathf.Max(0f, bounds.extents.y - radius);
        Vector3 a = bounds.center + Vector3.up * height;
        Vector3 b = bounds.center - Vector3.up * height;
        int count;
        do
        {
            count = Physics.CapsuleCastNonAlloc(a, b, radius, direction, sweepHits, distance + Director.collisionSkin, Director.blockingLayers, QueryTriggerInteraction.Ignore);
            if (count < sweepHits.Length) break;
            Array.Resize(ref sweepHits, sweepHits.Length * 2); // 不截断密集兵群结果。
        } while (true);
        for (int i = 0; i < count; i++)
            if (IsBlocker(sweepHits[i].collider) && !IsSupportingContact(sweepHits[i].collider, sweepHits[i].normal, direction) &&
                !IsEscapingOverlap(sweepHits[i].collider, direction))
            {
                float allowed = Mathf.Max(0f, sweepHits[i].distance - Director.collisionSkin);
                if (allowed < distance) LastMovementDecision = "collision_sweep";
                distance = Mathf.Min(distance, allowed);
            }
        // Cast 不报告起点已重叠的 Collider；额外检查落点，允许从重叠中脱离，但禁止越挤越深。
        Vector3 limitedStep = direction * distance;
        do
        {
            count = Physics.OverlapCapsuleNonAlloc(a + limitedStep, b + limitedStep, radius, overlapHits, Director.blockingLayers, QueryTriggerInteraction.Ignore);
            if (count < overlapHits.Length) break;
            Array.Resize(ref overlapHits, overlapHits.Length * 2);
        } while (true);
        for (int i = 0; i < count; i++)
        {
            Collider other = overlapHits[i];
            if (!IsBlocker(other)) continue;
            if (ownCollider != null)
            {
                // 包围胶囊只负责广相检测，最终用实际 Collider 判断穿入方向。
                // 桥面/基地地板可能已经接触脚底，沿面移动不代表向墙内穿入，不能按包围盒中心误拦。
                if (Physics.ComputePenetration(ownCollider, ownCollider.transform.position + limitedStep, ownCollider.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out Vector3 normal, out float depth))
                {
                    if (IsSupportingContact(other, normal, direction)) continue;
                    if (Vector3.Dot(normal, limitedStep) < -0.00001f)
                    { LastMovementDecision = "collision_overlap"; return Vector3.zero; }
                }
            }
            else
            {
                Vector3 away = bounds.center - other.bounds.center;
                if (Vector3.Dot(away, limitedStep) <= 0f) { LastMovementDecision = "collision_overlap"; return Vector3.zero; }
            }
        }
        return limitedStep / Time.fixedDeltaTime;
    }

    /// <summary>只对近水平运动识别承托面；保留墙、士兵及向下撞入地面的阻挡。</summary>
    private bool IsSupportingContact(Collider other, Vector3 normal, Vector3 direction)
    {
        if (Mathf.Abs(direction.y) > 0.05f || other.GetComponentInParent<SoldierHealth>() != null) return false;
        if (normal.y > 0.65f) return true;
        // CapsuleCast 起点已接触时可能返回与运动相反的法线，改用真实碰撞体分离法线。
        return ownCollider != null && Physics.ComputePenetration(ownCollider, ownCollider.transform.position, ownCollider.transform.rotation,
            other, other.transform.position, other.transform.rotation, out Vector3 separation, out float depth) && separation.y > 0.65f;
    }

    /// <summary>
    /// CapsuleCast 起点已重叠时会返回距离零，不能用它阻止“离开已有重叠”的移动。
    /// 只放行沿分离方向或切线的移动，落点仍接受下面的真实穿入方向检查；绝不放行向对方体内移动。
    /// </summary>
    private bool IsEscapingOverlap(Collider other, Vector3 direction)
    {
        return ownCollider != null && Physics.ComputePenetration(ownCollider, ownCollider.transform.position,
            ownCollider.transform.rotation, other, other.transform.position, other.transform.rotation,
            out Vector3 separation, out float depth) && Vector3.Dot(separation, direction) >= -0.00001f;
    }

    private bool IsBlocker(Collider other)
    {
        if (other == null || other.transform == MotionRoot || other.transform.IsChildOf(MotionRoot)) return false;
        if (Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer)) return false;
        var otherHealth = other.GetComponentInParent<SoldierHealth>();
        return otherHealth == null || !otherHealth.isDead;
    }

    private void CancelReservation() { ReservationUntil = 0f; }
    private void CancelRefillTimer() { refillAt = -1f; refillTarget = null; }

    /// <summary>禁用、离区、死亡后立刻释放占位和锁定；不恢复旧位置、不让死亡对象继续占位。</summary>
    public void ReleaseControl()
    {
        CancelReservation();
        CancelRefillTimer();
        needsRefill = queueBlocked = following = false;
        knockbackUntil = stunUntil = 0f;
        previousTargetHealth = null;
        if (agentOwnership && agent != null)
        {
            agent.updatePosition = originalAgentPosition; agent.updateRotation = originalAgentRotation;
            agentOwnership = false;
        }
        if (submittedVelocity && body != null && !body.isKinematic)
            body.velocity = new Vector3(0f, body.velocity.y, 0f);
        submittedVelocity = false;
    }
    void OnDisable() { ReleaseControl(); }
    void OnDestroy() { if (Director != null) Director.Forget(this); }

    void OnDrawGizmosSelected()
    {
        if (!Controlled) return;
        if (IsShield && Director.enableShieldFollow && anchorSet)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(anchor - Director.Axis * Director.maxRetreat, anchor + Director.Axis * Director.maxAdvance);
        }
        if (Director.enableQueueSpacing)
        {
            Vector3 facing = IsShield ? Director.Axis : -Director.Axis;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position - facing * Director.retreatClearance);
            Gizmos.DrawWireSphere(transform.position - facing * Director.retreatClearance, Radius);
        }
        if (ReservationUntil > Time.time)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ReservedPosition, Radius);
        }
    }
}
