using UnityEngine;
using UnityEngine.AI;
/// <summary>仅供新版蓝盾父控制器安装的单兵执行器。不含任何红兵排队、受击、闪白配置。</summary>
[DisallowMultipleComponent, AddComponentMenu("")]
public sealed class BlueShieldMember : MonoBehaviour
{
    public BlueShieldAdvance Owner { get; private set; }
    public SoldierAnimationController Attack { get; private set; }
    public bool IsFirstRow { get; set; }
    public string State { get; private set; }
    public bool Live => isActiveAndEnabled && health != null && !health.isDead && health.isActiveAndEnabled && Attack != null && Attack.isActiveAndEnabled;
    // 原位本地坐标只记录一次：编组由 Timeline 搬动时原位跟随编组，战斗补位不会刷新它。
    public Vector3 Home => root != null && root.parent != null ? root.parent.TransformPoint(homeLocal) : homeLocal;
    public Transform MotionRoot => root;
    public SoldierHealth Target => target;
    [SerializeField, HideInInspector] private Transform root;
    [SerializeField, HideInInspector] private Vector3 homeLocal;
    [SerializeField, HideInInspector] private bool homeRecorded;
    private Vector3 knockVelocity, targetSnapshot;
    private Rigidbody body;
    private NavMeshAgent agent;
    private SoldierHealth health, target;
    private MeleeBodyMotor motor;
    private Collider motionCollider;
    private Collider[] friendlyContacts = new Collider[16];
    private float knockUntil, hitUntil, retreatAt = -1f, remaining;
    private bool started, wroteVelocity;
    [SerializeField, HideInInspector] private bool acquired, originalPosition, originalRotation;
    public void Configure(BlueShieldAdvance owner, SoldierAnimationController attack)
    {
        Owner = owner; Attack = attack; health = GetComponent<SoldierHealth>(); root = transform;
        // 不移动 Animator 模型，避免动画写回。只提升到单个士兵的外容器，绝不移动 Timeline 编组或整个父物体。
        while (root.parent != null && root.parent != owner.transform && root.parent.parent != owner.transform)
        {
            var candidate = root.parent;
            if (candidate.GetComponent<Animator>() != null) break;
            int count = 0; foreach (var a in candidate.GetComponentsInChildren<SoldierAnimationController>(true)) if (a.isActiveAndEnabled) count++;
            if (count != 1) break;
            root = candidate;
        }
        // 仅首次记录原位；脚本域重载重新接线时也不能把现有推进距离累加为新的原位。
        if (!homeRecorded) { homeLocal = root.parent != null ? root.localPosition : root.position; homeRecorded = true; }
        body = root.GetComponent<Rigidbody>(); agent = root.GetComponent<NavMeshAgent>();
        var c = root.GetComponent<Collider>(); if (c == null || !c.enabled) c = GetComponent<Collider>();
        motionCollider = c;
        motor = new MeleeBodyMotor(root, c, owner.minimumRadius, owner.collisionSkin, owner.blockingLayers);
        Release();
    }
    public bool BeginHit(Vector3 direction, float distance, float duration, float stun, Vector3 axes)
    {
        if (Owner == null || !Owner.Active || !Owner.ContainsHome(Home) || !Owner.hitPriority || !Live) return false;
        Vector3 dir = Vector3.Scale(direction.normalized, axes);
        bool moves = dir.sqrMagnitude > .001f && Mathf.Abs(distance) > .000001f;
        float time = moves ? Mathf.Max(.01f, duration) : 0f;
        knockVelocity = moves ? dir.normalized * (distance / time) : Vector3.zero;
        knockUntil = Time.time + time; hitUntil = Time.time + Mathf.Max(time, Mathf.Max(0f, stun));
        return true;
    }
    private Vector3 Limited(Vector3 velocity)
    {
        motor.UpdateSettings(Owner.minimumRadius, Owner.collisionSkin, Owner.blockingLayers);
        Vector3 axis = Owner.Axis, side = Vector3.Cross(Vector3.up, axis);
        Vector3 offset = Vector3.ProjectOnPlane(root.position - Home, Vector3.up), step = velocity * Time.fixedDeltaTime;
        // 只能缩短本帧移动，不用边界钳制瞬移，也不把外力导致的越界瞬间拉回。
        for (int i = 0; i < 2; i++)
        {
            bool longitudinal = i == 0; Vector3 item = longitudinal ? axis : side;
            float current = Vector3.Dot(offset, item), wanted = Vector3.Dot(step, item);
            float min = longitudinal ? -Mathf.Max(0f, Owner.maxBackward) : -Mathf.Max(0f, Owner.maxLateral);
            float max = longitudinal ? Mathf.Max(0f, Owner.maxForward) : Mathf.Max(0f, Owner.maxLateral);
            float actual = Mathf.Clamp(current + wanted, min, max) - current;
            actual = wanted >= 0f ? Mathf.Clamp(actual, 0f, wanted) : Mathf.Clamp(actual, wanted, 0f);
            step += item * (actual - wanted);
        }
        return motor.SafeVelocity(step / Time.fixedDeltaTime);
    }
    private void StartAdvance(SoldierHealth next)
    {
        target = next; targetSnapshot = next.transform.position; started = true;
        remaining = Random.Range(Mathf.Max(0f, Mathf.Min(Owner.advanceDistance.x, Owner.advanceDistance.y)), Mathf.Max(0f, Mathf.Max(Owner.advanceDistance.x, Owner.advanceDistance.y)));
    }
    /// <summary>
    /// 多名盾兵可能同时选中第一名入场红兵。直接沿斜线靠近会向中央收拢，被同排友军挡死。
    /// 只有斜向推进被限制时才比较沿列线的合法位移；两种方案都经过同一套碰撞与边界裁剪，
    /// 选择前进量更大的合法方案，绝不忽略友军碰撞，也不把击退或退守改成前进。
    /// </summary>
    private Vector3 SafeAdvance(Vector3 desired)
    {
        Vector3 safe = Limited(desired);
        if (!Owner.keepLaneWhenBlocked || target == null) return safe;
        Vector3 axis = Owner.Axis;
        if (Vector3.Dot(desired, axis) <= .000001f || Vector3.Dot(safe, axis) >= Vector3.Dot(desired, axis) - .000001f) return safe;
        Vector3 delta = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
        float forward = Vector3.Dot(delta, axis);
        // 用停止圆与前进轴的交点限制步长；目标在斜前方时不能简单用 forward-stopDistance。
        // 侧向已超出停止半径时最多走到与目标同一纵向位置，不能从侧面越过目标。
        float lateralSquared = Mathf.Max(0f, delta.sqrMagnitude - forward * forward);
        float stop = Mathf.Max(0f, Owner.stopDistance);
        float stopAlongAxis = Mathf.Sqrt(Mathf.Max(0f, stop * stop - lateralSquared));
        float step = Mathf.Min(desired.magnitude * Time.fixedDeltaTime, Mathf.Max(0f, forward - stopAlongAxis));
        Vector3 alongLane = Limited(axis * (step / Time.fixedDeltaTime));
        if (Vector3.Dot(alongLane, axis) > Vector3.Dot(safe, axis) + .000001f) safe = alongLane;
        if (Vector3.Dot(safe, axis) >= Vector3.Dot(desired, axis) - .000001f || motionCollider == null) return safe;
        // 斜排相邻胶囊原本可能轻微相交，正前进也可能被略靠前的邻兵阻挡。
        // 只投影掉朝本队活兵体内的速度，保留沿接触面的分量；不施加分离瞬移，不忽略碰撞。
        Vector3 slide = desired;
        int count;
        Bounds bounds = motionCollider.bounds;
        do
        {
            count = Physics.OverlapSphereNonAlloc(bounds.center, bounds.extents.magnitude + desired.magnitude * Time.fixedDeltaTime,
                friendlyContacts, Owner.blockingLayers, QueryTriggerInteraction.Ignore);
            if (count < friendlyContacts.Length) break;
            System.Array.Resize(ref friendlyContacts, friendlyContacts.Length * 2);
        } while (true);
        for (int i = 0; i < count; i++)
        {
            Collider other = friendlyContacts[i];
            if (other == null || other.transform == root || other.transform.IsChildOf(root)) continue;
            var friend = other.GetComponentInParent<BlueShieldMember>();
            if (friend == null || friend.Owner != Owner || !friend.Live) continue;
            Vector3 normal; float depth;
            bool touches = Physics.ComputePenetration(motionCollider, motionCollider.transform.position, motionCollider.transform.rotation,
                other, other.transform.position, other.transform.rotation, out normal, out depth);
            if (!touches) touches = Physics.ComputePenetration(motionCollider, motionCollider.transform.position + slide * Time.fixedDeltaTime, motionCollider.transform.rotation,
                other, other.transform.position, other.transform.rotation, out normal, out depth);
            if (!touches) continue;
            normal = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
            float inward = Vector3.Dot(slide, normal);
            if (inward < 0f) slide -= normal * inward;
        }
        // 投影不能增加原随机路程预算，也不能从目标侧面越过其纵向位置。
        slide = Vector3.ClampMagnitude(slide, desired.magnitude);
        float axialStep = Vector3.Dot(slide, axis) * Time.fixedDeltaTime;
        float maxStep = Mathf.Max(0f, forward - stopAlongAxis);
        if (axialStep > maxStep) slide -= axis * ((axialStep - maxStep) / Time.fixedDeltaTime);
        Vector3 aroundFriend = Limited(slide);
        return Vector3.Dot(aroundFriend, axis) > Vector3.Dot(safe, axis) + .000001f ? aroundFriend : safe;
    }
    public void Tick(bool selected, bool outnumbered)
    {
        if (!Live) { Release(); State = "死亡或禁用"; return; }
        if (!Owner.ContainsHome(Home)) { Release(); State = "离开右桥交还原控制"; return; }
        if (agent != null && !acquired)
        { originalPosition = agent.updatePosition; originalRotation = agent.updateRotation; agent.updatePosition = agent.updateRotation = false; acquired = true; }
        Vector3 velocity = Vector3.zero;
        if (Time.time < hitUntil && Owner.hitPriority)
        { State = "受击优先"; velocity = Time.time < knockUntil ? knockVelocity : Vector3.zero; }
        else if (!selected) State = "后排原位";
        else if (outnumbered)
        {
            if (retreatAt < 0f) retreatAt = Time.time + Random.Range(Mathf.Max(0f, Mathf.Min(Owner.retreatDelay.x, Owner.retreatDelay.y)), Mathf.Max(0f, Mathf.Max(Owner.retreatDelay.x, Owner.retreatDelay.y)));
            State = Time.time < retreatAt ? "退守随机等待" : "退回原位";
            if (Time.time >= retreatAt)
            {
                Vector3 delta = Vector3.ProjectOnPlane(Home - root.position, Vector3.up);
                if (delta.magnitude > Owner.homeTolerance) velocity = delta.normalized * Mathf.Min(Mathf.Max(0f, Owner.retreatSpeed), delta.magnitude / Time.fixedDeltaTime);
                else State = "原位守卫";
            }
            started = false;
        }
        else
        {
            retreatAt = -1f;
            var next = Owner.FindTarget(this);
            if (next == null) { State = "无目标等候"; target = null; started = false; }
            else
            {
                // 目标死亡/离区换目标、首次接战或原目标被击退后，才重新抽取推进路程。
                if (!started || target != next || (remaining <= .001f && Vector3.ProjectOnPlane(next.transform.position - targetSnapshot, Vector3.up).magnitude > Owner.targetMoveThreshold)) StartAdvance(next);
                Vector3 delta = Vector3.ProjectOnPlane(next.transform.position - transform.position, Vector3.up);
                float distance = delta.magnitude;
                float step = Mathf.Min(remaining, Mathf.Max(0f, distance - Mathf.Max(0f, Owner.stopDistance)), Mathf.Max(0f, Owner.advanceSpeed) * Time.fixedDeltaTime);
                if (step > .000001f) { velocity = delta.normalized * (step / Time.fixedDeltaTime); State = "有限推进"; }
                else State = "接战站立";
            }
        }
        Vector3 safe = State == "有限推进" ? SafeAdvance(velocity) : Limited(velocity);
        if (State == "有限推进") remaining = Mathf.Max(0f, remaining - safe.magnitude * Time.fixedDeltaTime);
        if (body != null && !body.isKinematic) { body.velocity = safe + Vector3.up * body.velocity.y; wroteVelocity = true; }
        else root.position += safe * Time.fixedDeltaTime;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.nextPosition = root.position;
    }
    /// <summary>退出时只释放本组件输出及 Agent 所有权，不归零位移，不修改 Timeline。</summary>
    public void Release()
    {
        hitUntil = knockUntil = 0f; retreatAt = -1f; started = false; target = null;
        if (acquired && agent != null) { agent.updatePosition = originalPosition; agent.updateRotation = originalRotation; }
        if (wroteVelocity && body != null && !body.isKinematic) body.velocity = Vector3.up * body.velocity.y;
        wroteVelocity = false;
        acquired = false;
    }
    void OnDisable() { Release(); }
}
