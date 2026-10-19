using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每个红方近战自己的战斗状态。生命脚本提交明确伤害来源，移动脚本提交当前期望速度。
/// 本组件不寻找蓝方管理器，不修改索敌、伤害或攻击事件，也不直接写位置。
/// 优先级固定为：死亡 > 原 SoldierHealth 击退/僵直 > 等候/补位 > 原寻路。
/// 常规速度由 SoldierAIMovement 执行；受击时它让路，由原生命脚本独占击退位移。
/// </summary>
[DisallowMultipleComponent, AddComponentMenu("战斗/红兵自主交战（新版）")]
public sealed class RedMeleeSelf : MonoBehaviour
{
    [Tooltip("本红兵的等候、补位、空间检测和来源闪白配置；由兵营复制给新克隆。击退距离、时长、轴向及僵直只在原 SoldierHealth 调整，此处不再接管击退位移。")]
    public RedMeleeSettings settings = new RedMeleeSettings();
    private static readonly List<RedMeleeSelf> units = new List<RedMeleeSelf>();
    // 友军登记表只读共享，不驱动任何单位。每个物理帧筛选一次右桥活动红兵，
    // 避免每名接战红兵反复扫描全场几千名尚在生成区/左桥的红兵。
    private static readonly List<RedMeleeSelf> activeUnits = new List<RedMeleeSelf>();
    private static float cachedPhysicsTime = float.NaN;
    private static List<RedMeleeSelf> Peers
    {
        get
        {
            if (cachedPhysicsTime != Time.fixedTime)
            {
                cachedPhysicsTime = Time.fixedTime; activeUnits.Clear();
                foreach (var unit in units) if (unit != null && unit.Controlled) activeUnits.Add(unit);
            }
            return activeUnits;
        }
    }
    private SoldierAnimationController attack;
    private SoldierHealth health;
    private SoldierAIMovement movement;
    private MeleeBodyMotor motor;
    private float refillAt = -1f, reservedUntil;
    private Vector3 reservedPoint;
    private Transform delayTarget;
    private SoldierHealth lastTarget;
    private bool needsRefill;
    public string State { get; private set; } = "常规寻路";
    public bool Controlled => isActiveAndEnabled && settings.enabled && attack != null && attack.isActiveAndEnabled &&
        attack.myFaction == FactionType.Red && !attack.是远程单位 && health != null && !health.isDead &&
        gameObject.scene.name == settings.sceneName && new Bounds(settings.center, settings.size).Contains(transform.position);
    // 只读原 SoldierHealth 的受击状态，不再保存或执行独立的击退距离、时长和速度。
    public bool HitMovementActive => health != null && health.OriginalHitMovementActive;
    public float Radius => motor != null ? motor.Radius : settings.minimumRadius;
    void Awake() { Cache(); }
    void OnEnable() { Cache(); if (!units.Contains(this)) units.Add(this); cachedPhysicsTime = float.NaN; }
    void OnDisable() { units.Remove(this); cachedPhysicsTime = float.NaN; ResetState(); }
    /// <summary>生成器在实例化后复制配置；不能共享同一个可变设置对象。</summary>
    public void Configure(RedMeleeSettings template) { settings = JsonUtility.FromJson<RedMeleeSettings>(JsonUtility.ToJson(template)); cachedPhysicsTime = float.NaN; Cache(); }
    private void Cache()
    {
        attack = GetComponent<SoldierAnimationController>(); health = GetComponent<SoldierHealth>();
        movement = GetComponent<SoldierAIMovement>();
        motor = new MeleeBodyMotor(transform, GetComponent<Collider>(), settings.minimumRadius, settings.skin, settings.layers);
    }
    private void ResetState()
    { reservedUntil = 0f; refillAt = -1f; needsRefill = false; delayTarget = null; lastTarget = null; State = "常规寻路"; }
    /// <summary>只选择闪白参数；未指定伤害来源、加速闪红由生命脚本继续使用原设置。</summary>
    public void Flash(SoldierDamageSource source, ref float duration, ref float strength)
    {
        if (!Controlled || source == SoldierDamageSource.Unspecified) return;
        if (source == SoldierDamageSource.Melee)
        { duration = settings.meleeFlash ? settings.meleeDuration : 0f; strength = settings.meleeFlash ? settings.meleeStrength : 0f; }
        else if (!settings.rangedFlash) { duration = strength = 0f; }
        else if (!settings.keepRangedFlash) { duration = settings.rangedDuration; strength = settings.rangedStrength; }
    }
    private static bool Live(Transform target)
    { if (target == null || !target.gameObject.activeInHierarchy) return false; var h = target.GetComponent<SoldierHealth>(); return h != null && !h.isDead; }
    /// <summary>后排只检查同一场景的存活友兵。前兵正交战/受击或等候队列延伸时，为它保留净退路。</summary>
    private bool Blocked(Vector3 direction)
    {
        if (!settings.queueSpacing) return false;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        foreach (var other in Peers)
        {
            if (other == null || other == this || !other.Controlled || other.gameObject.scene != gameObject.scene) continue;
            Vector3 delta = other.transform.position - transform.position;
            float forward = Vector3.Dot(delta, direction), radii = Radius + other.Radius;
            // 跟随前兵原 SoldierHealth 的实际击退距离留退路，不覆盖或另存一套击退距离。
            float clearance = Mathf.Max(settings.retreatSpace, Mathf.Abs(other.health.击退距离));
            // 后排在下一物理帧才停止，额外预留自身一帧最大行程及碰撞皮肤，避免刹停后挤掉前排退路。
            float brakingSpace = movement != null ? Mathf.Max(0f, Mathf.Max(movement.最小移动速度, movement.最大移动速度)) * Time.fixedDeltaTime : 0f;
            clearance += brakingSpace + Mathf.Max(0f, settings.skin);
            if (forward > settings.frontSeparation && forward < clearance + radii &&
                Mathf.Abs(Vector3.Dot(delta, side)) < radii + settings.lanePadding) return true;
        }
        return false;
    }
    /// <summary>单线程物理帧内立刻预留未来接战空位；不把尸体或已禁用对象当占位者。</summary>
    private bool Reserve(Vector3 point)
    {
        foreach (var other in Peers)
        {
            if (other == null || other == this || !other.Controlled || other.gameObject.scene != gameObject.scene || other.reservedUntil <= Time.time) continue;
            float separation = Radius + other.Radius;
            if (Vector3.ProjectOnPlane(point - other.reservedPoint, Vector3.up).sqrMagnitude < separation * separation) return false;
        }
        reservedPoint = point; reservedUntil = Time.time + Mathf.Max(.02f, settings.reservationTime); return true;
    }
    public Vector3 FilterVelocity(Vector3 desired, Transform target)
    {
        if (health != null && health.isDead) { ResetState(); State = "死亡"; return Vector3.zero; }
        if (!Controlled) { ResetState(); return desired; }
        motor.UpdateSettings(settings.minimumRadius, settings.skin, settings.layers);
        if (HitMovementActive)
        {
            State = health.OriginalKnockbackActive ? "受击后退" : "受击僵直";
            reservedUntil = 0f; refillAt = -1f; delayTarget = null;
            return Vector3.zero; // 原生命脚本正在移动，补位和寻路不得产生第二份位移。
        }
        if (!ReferenceEquals(lastTarget, null) && (lastTarget == null || lastTarget.isDead)) { needsRefill = true; refillAt = -1f; }
        lastTarget = Live(target) ? target.GetComponent<SoldierHealth>() : null;
        Vector3 direction = Vector3.ProjectOnPlane(settings.forward, Vector3.up).normalized;
        if (direction.sqrMagnitude < .001f) direction = Vector3.back;
        // 排队检查先于目标检查：后排尚未索到敌人时，同样不能挤占前排退路。
        if (Blocked(direction))
        { needsRefill = true; refillAt = -1f; delayTarget = null; reservedUntil = 0f; State = "为前排留空间"; return Vector3.zero; }
        if (!Live(target))
        { refillAt = -1f; delayTarget = null; reservedUntil = 0f; State = "常规寻路"; return desired; }
        if (needsRefill && settings.refillDelayEnabled)
        {
            if (refillAt < 0f || delayTarget != target)
            { refillAt = Time.time + Random.Range(Mathf.Max(0f, Mathf.Min(settings.refillDelay.x, settings.refillDelay.y)), Mathf.Max(0f, Mathf.Max(settings.refillDelay.x, settings.refillDelay.y))); delayTarget = target; }
            if (Time.time < refillAt) { reservedUntil = 0f; State = "补位随机等待"; return Vector3.zero; }
        }
        Vector3 delta = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        float stop = movement != null ? movement.stopDistance : 1.8f;
        if (Vector3.ProjectOnPlane(desired, Vector3.up).sqrMagnitude < .000001f) { reservedUntil = 0f; State = "接战站立"; return desired; }
        if (!Reserve(target.position - delta.normalized * stop))
        { needsRefill = true; refillAt = -1f; delayTarget = null; reservedUntil = 0f; State = "等待空位"; return Vector3.zero; }
        needsRefill = false; refillAt = -1f; delayTarget = null; State = "补位或寻路";
        return motor.SafeVelocity(desired);
    }
    void OnDrawGizmosSelected()
    {
        if (!isActiveAndEnabled || settings == null || !settings.enabled || gameObject.scene.name != settings.sceneName) return;
        Gizmos.color = Color.green; Gizmos.DrawWireCube(settings.center, settings.size);
        if (settings.queueSpacing)
        {
            Vector3 direction = Vector3.ProjectOnPlane(settings.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < .001f) direction = Vector3.back;
            var ownHealth = health != null ? health : GetComponent<SoldierHealth>();
            float space = Mathf.Max(settings.retreatSpace, ownHealth != null ? Mathf.Abs(ownHealth.击退距离) : 0f);
            Gizmos.color = Color.yellow; Gizmos.DrawLine(transform.position, transform.position - direction * space);
            Gizmos.DrawWireSphere(transform.position - direction * space, Radius);
        }
        if (Application.isPlaying && reservedUntil > Time.time) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(reservedPoint, Radius); }
    }
}

/// <summary>本地配置值对象；只存参数，不拥有任何士兵，不接管蓝方位置。</summary>
[System.Serializable]
public sealed class RedMeleeSettings
{
    [Header("作用范围")]
    [Tooltip("关闭恢复原受击和寻路；不会瞬移归位，不改变攻击。仅控制本红兵。")]
    public bool enabled = true;
    [Tooltip("只在此场景生效，复制到其他场景默认不接管。")]
    public string sceneName = "20261012-PVE收关敌万军";
    [Tooltip("右桥区域的世界中心。区域外沿用原行为，不影响左桥。")]
    public Vector3 center = new Vector3(9, 1, -13);
    [Tooltip("右桥区域世界尺寸，绿色 Gizmo 仅在总开关开启时显示。")]
    public Vector3 size = new Vector3(10, 8, 14);
    [Tooltip("红方朝蓝方的世界前进方向，投影到水平面；本场景为 Z 负方向。")]
    public Vector3 forward = Vector3.back;
    [Header("等候和补位")]
    [Tooltip("开启后后排为前排留退路，关闭只取消排队，碰撞仍有效。")]
    public bool queueSpacing = true;
    [Min(0), Tooltip("前排身后保留的最小净退路，另加双方半径，米。若前兵 SoldierHealth 的击退距离更大，自动按更大值留距。黄色 Gizmo 随此开关显示。")]
    public float retreatSpace = 2f;
    [Min(.02f), Tooltip("友军至少领先的纵向距离；避免同排略斜的相邻士兵互相等待。")]
    public float frontSeparation = .6f;
    [Min(0), Tooltip("同列检测额外横向余量，米。")]
    public float lanePadding = .15f;
    [Tooltip("前路开放或目标死亡后，先独立随机等待再补位；只限制移动，不限制原攻击。")]
    public bool refillDelayEnabled = true;
    [Tooltip("补位等待秒数下限、上限；目标变更、再次拥堵、受击会取消并重新判断。")]
    public Vector2 refillDelay = new Vector2(.18f, .55f);
    [Min(.02f), Tooltip("未来战斗空位租约秒数，移动持续刷新；死亡、禁用和等候立即释放。")]
    public float reservationTime = .12f;
    [Header("占位与碰撞（击退参数在原 SoldierHealth）")]
    [Min(.05f), Tooltip("最小单兵占位半径；实际碰撞体更大时使用实际半径，米。")]
    public float minimumRadius = .4f;
    [Min(0), Tooltip("士兵碰撞时，身体周围预留的一圈看不见的小空隙（米），相当于套一层薄薄的空气垫：探路时把碰撞圈缩小一点，撞墙/撞到别人时提前这段距离就停下，不真贴上去，防止物理帧抖动造成的轻微穿模、挤脸抖动。默认0.025米≈2.5厘米，肉眼几乎无感。调大兵与兵之间缝更宽、队伍变松；调成0则贴脸走，人挤人时容易轻微穿插抖动。注意它不改变士兵实际体型大小。")]
    public float skin = .025f;
    [Tooltip("移动和击退阻挡层；忽略触发器、自身及已死亡战斗对象。")]
    public LayerMask layers = ~0;
    [Header("由本红兵按伤害来源决定闪白")]
    [Tooltip("近战闪白开关；关闭不影响伤害、击退、缩放、死亡。未指定来源保持旧表现。")]
    public bool meleeFlash = true;
    [Min(0), Tooltip("近战闪白秒数；不改变独立加速闪红。")]
    public float meleeDuration = .08f;
    [Min(0), Tooltip("近战自发光相对原强度的倍率；白模替换只有开关和时长可调。")]
    public float meleeStrength = .25f;
    [Tooltip("远程闪白开关；由攻击调用的明确来源决定，不按武器外观推断。")]
    public bool rangedFlash = true;
    [Tooltip("开启保留原远程闪白参数，关闭使用下面的时长和强度。")]
    public bool keepRangedFlash = true;
    [Min(0), Tooltip("远程闪白秒数，仅不保留原配置时生效。")]
    public float rangedDuration = .3f;
    [Min(0), Tooltip("远程闪白相对原自发光的倍率，仅不保留原配置时生效。")]
    public float rangedStrength = 1f;
}
