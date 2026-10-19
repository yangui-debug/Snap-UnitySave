using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 只管理本父物体下的蓝方近战盾兵。检测器由用户拖入桥口 Collider，不生成、不移动检测器。
/// 红兵只作为只读目标和人数统计对象；本脚本不安装、配置或驱动任何红方组件。
/// Timeline 继续控制编组；每个成员移动自己的单兵容器，并以固定本地原位叠加战斗偏移。
/// </summary>
[DisallowMultipleComponent, AddComponentMenu("战斗/蓝盾有限推进与退守（新版）")]
public sealed class BlueShieldAdvance : MonoBehaviour
{
    [Header("开关与检测器")]
    [Tooltip("关闭全部新增蓝盾行为；释放速度和状态，不瞬移归位。红兵自身行为不受此开关影响。")]
    public bool enableBehaviour = true;
    [Tooltip("仅指定场景生效，避免复制物体后影响其他场景。")]
    public string sceneName = "20261012-PVE收关敌万军";
    [Tooltip("拖入你在右桥口自行放置的碰撞体触发器（Box/Sphere/Capsule，Is Trigger 开启）。未赋值不推进、不退守。检测体积以碰撞体为准。")]
    public Collider detectionTrigger;
    [Tooltip("人数和目标检测层。按存活士兵去重统计，忽略尸体和死亡替身；不改变攻击检测范围。")]
    public LayerMask detectionLayers = ~0;
    [Tooltip("检测器内作为敌方的人数和目标阵营。本场景红方。")]
    public FactionType enemyFaction = FactionType.Red;
    [Min(.02f), Tooltip("检测敌我人数及目标的刷新间隔，秒。物理移动仍逐物理帧执行。")]
    public float scanInterval = .1f;
    [Header("第一排与固定原位")]
    [Tooltip("蓝方向桥口的世界前进轴；水平投影后归一化。前后移动范围沿此轴限制。")]
    public Vector3 forwardAxis = Vector3.forward;
    [Tooltip("允许接管蓝盾原位的右桥世界区域中心；Timeline 将某排调往左桥后，自动停止该排新增控制。只筛选蓝盾，不配置红方行为。")]
    public Vector3 controlCenter = new Vector3(9, 1, -13);
    [Tooltip("蓝盾原位控制区域的世界尺寸；用于保留后续 Timeline 调动，不能扩至左桥。绿色范围随总开关和 Gizmos 开关显示。")]
    public Vector3 controlSize = new Vector3(10, 8, 14);
    [Tooltip("可手动指定第一排攻击组件；留空时在首次发现红兵时，逐列选择没有更前方友军的盾兵，支持斜排。手动指定也必须属于本父物体。")]
    public SoldierAnimationController[] firstRow = Array.Empty<SoldierAnimationController>();
    [Min(0), Tooltip("逐列自动选第一排的纵向同排容差，米；同列友军领先超过此值，才把自己视为后排。支持整个前排斜着摆放。")]
    public float firstRowDepth = .8f;
    [Min(.01f), Tooltip("逐列选第一排时的横向检测半宽，米。前方友军与本兵横向差小于此值才算同列；通常略大于后排错列量、小于相邻列间距。")]
    public float firstRowLaneHalfWidth = .7f;
    [Min(0), Tooltip("相对原位向前移动硬上限，米。随机推进及死亡空位补位都不能重设原位或突破上限。")]
    public float maxForward = 1.5f;
    [Min(0), Tooltip("相对原位向后移动硬上限，米。主要用于受击后退；主动人数劣势退守只回原位。")]
    public float maxBackward = 1f;
    [Min(0), Tooltip("相对原位允许的横向偏移，米。向斜前方敌人移动时也不能跨列无限游走。")]
    public float maxLateral = .4f;
    [Header("随机推进与死亡补位")]
    [Tooltip("每次开始推进/目标死亡后重新补位的随机路程下限、上限，米。累计位置仍受固定原位边界限制。")]
    public Vector2 advanceDistance = new Vector2(.8f, 1.5f);
    [Min(0), Tooltip("向前接敌速度，米/秒；不修改动画攻速、攻击距离或伤害。")]
    public float advanceSpeed = 1.6f;
    [Tooltip("目标在斜前方时，直接朝目标移动可能挤向同排友军。开启后若斜向位移受阻，先尝试沿 Forward Axis 保持列线前进，再尝试沿友军接触面小幅避让；不绕过敌人，仍执行完整碰撞检测、原位边界和目标停止距离，不穿过友军或越过目标。")]
    public bool keepLaneWhenBlocked = true;
    [Min(0), Tooltip("接近目标到此水平距离即停止推进，米；碰撞先发生则提前停止。不是攻击判定范围。")]
    public float stopDistance = 1.1f;
    [Min(0), Tooltip("达到随机路程后，原目标被推远至少此距离才开始下一次有限跟进，米。避免逐帧重复抽样。")]
    public float targetMoveThreshold = .35f;
    [Header("人数劣势退守")]
    [Tooltip("检测区内红兵人数大于蓝兵人数时，每名第一排盾兵独立随机等待后退回原位。关闭后只执行有限推进。")]
    public bool retreatWhenOutnumbered = true;
    [Tooltip("人数劣势退守等待范围，秒。每名独立抽样；等待期间停止推进，人数条件失效立即取消该次等待。")]
    public Vector2 retreatDelay = new Vector2(.2f, .9f);
    [Min(0), Tooltip("退回原位的速度，米/秒。受击优先，结束后重新检查人数。碰撞阻挡时等候，不穿过友军。")]
    public float retreatSpeed = .65f;
    [Min(.001f), Tooltip("判定已回到原位的水平距离容差，米；不瞬移校正尾差。")]
    public float homeTolerance = .025f;
    [Header("碰撞与受击优先级")]
    [Min(.05f), Tooltip("单兵最小检测半径，米；实际碰撞体更大时采用实际尺寸。")]
    public float minimumRadius = .4f;
    [Min(0), Tooltip("碰撞安全间隙，米。推进、退守、击退均先扫掠，允许实际移动被障碍缩短。")]
    public float collisionSkin = .025f;
    [Tooltip("位移阻挡层。忽略自身、Trigger 及已死亡单位，保留地面承托与重力。")]
    public LayerMask blockingLayers = ~0;
    [Tooltip("受击击退/僵直优先于本脚本推进和退守；距离、时长仍取每名盾兵生命脚本的原参数。")]
    public bool hitPriority = true;
    [Tooltip("开启时选中父物体显示检测体和成员前后范围；关闭总功能或未配置检测器时不显示。")]
    public bool showGizmos = true;

    private readonly List<BlueShieldMember> members = new List<BlueShieldMember>();
    private readonly List<SoldierHealth> enemies = new List<SoldierHealth>();
    private readonly HashSet<SoldierHealth> counted = new HashSet<SoldierHealth>();
    private Collider[] overlaps = new Collider[64];
    private float nextScan;
    private bool rowChosen;
    public int RedCount { get; private set; }
    public int BlueCount { get; private set; }
    public bool Active => isActiveAndEnabled && enableBehaviour && gameObject.scene.name == sceneName && detectionTrigger != null &&
        detectionTrigger.enabled && detectionTrigger.isTrigger && detectionTrigger.gameObject.activeInHierarchy;
    public Vector3 Axis => Vector3.ProjectOnPlane(forwardAxis, Vector3.up).sqrMagnitude > .001f ? Vector3.ProjectOnPlane(forwardAxis, Vector3.up).normalized : Vector3.forward;
    public bool ContainsHome(Vector3 home) => new Bounds(controlCenter, controlSize).Contains(home);
    void Start() { if (members.Count == 0) Initialize(); }
    void OnEnable()
    {
        rowChosen = false; nextScan = 0f;
        // 播放中重新编译只会再次触发 OnEnable，不能依赖只执行一次的 Start 重建单兵引用。
        if (Application.isPlaying) Initialize();
    }
    void Initialize()
    {
        members.Clear();
        foreach (var attack in GetComponentsInChildren<SoldierAnimationController>(true))
        {
            if (!attack.isActiveAndEnabled || attack.是远程单位 || attack.myFaction != FactionType.Blue || attack.GetComponent<SoldierHealth>() == null) continue;
            var ai = attack.GetComponent<SoldierAIMovement>();
            // 有自主寻路输出的盾兵不接管，避免两个脚本争抢同一刚体；本场景实际蓝盾没有启用该组件。
            if (ai != null && ai.isActiveAndEnabled) continue;
            var member = attack.GetComponent<BlueShieldMember>();
            if (member == null) member = attack.gameObject.AddComponent<BlueShieldMember>();
            member.Configure(this, attack); members.Add(member);
        }
    }
    void Scan()
    {
        RedCount = BlueCount = 0; enemies.Clear(); counted.Clear();
        Bounds b = detectionTrigger.bounds;
        int count;
        do
        {
            count = Physics.OverlapBoxNonAlloc(b.center, b.extents, overlaps, Quaternion.identity, detectionLayers, QueryTriggerInteraction.Ignore);
            if (count < overlaps.Length) break;
            Array.Resize(ref overlaps, overlaps.Length * 2);
        } while (true);
        for (int i = 0; i < count; i++)
        {
            var c = overlaps[i]; var h = c.GetComponentInParent<SoldierHealth>();
            if (h == null || h.isDead || !h.isActiveAndEnabled || h.gameObject.scene != gameObject.scene || counted.Contains(h)) continue;
            // 广相包围盒之后再检测真实触发器体积，旋转盒和球体不会按 AABB 错计人数。
            if ((detectionTrigger.ClosestPoint(c.bounds.center) - c.bounds.center).sqrMagnitude > .000001f &&
                !Physics.ComputePenetration(detectionTrigger, detectionTrigger.transform.position, detectionTrigger.transform.rotation,
                    c, c.transform.position, c.transform.rotation, out _, out _)) continue;
            var a = h.GetComponent<SoldierAnimationController>();
            if (a == null || !a.isActiveAndEnabled) continue;
            counted.Add(h);
            if (a.myFaction == enemyFaction) { RedCount++; enemies.Add(h); }
            else if (a.myFaction == FactionType.Blue) BlueCount++;
        }
    }
    void ChooseRow()
    {
        Vector3 side = Vector3.Cross(Vector3.up, Axis);
        foreach (var m in members)
        {
            if (m == null) continue;
            m.IsFirstRow = m.Live && ContainsHome(m.Home);
            if (!m.IsFirstRow) continue;
            if (firstRow != null && firstRow.Length > 0) { m.IsFirstRow = Array.IndexOf(firstRow, m.Attack) >= 0; continue; }
            // 每列独立选前排，不能用全队最大 Z 截一条水平线，否则斜列只有最前的几个兵会被选中。
            foreach (var other in members)
            {
                if (other == null || other == m || !other.Live || !ContainsHome(other.Home)) continue;
                Vector3 delta = other.transform.position - m.transform.position;
                if (Vector3.Dot(delta, Axis) > Mathf.Max(0f, firstRowDepth) &&
                    Mathf.Abs(Vector3.Dot(delta, side)) <= Mathf.Max(.01f, firstRowLaneHalfWidth))
                { m.IsFirstRow = false; break; }
            }
        }
        rowChosen = true;
    }
    public SoldierHealth FindTarget(BlueShieldMember member)
    {
        SoldierHealth best = null; float nearest = float.PositiveInfinity;
        foreach (var e in enemies)
        {
            if (e == null || e.isDead || !e.gameObject.activeInHierarchy) continue;
            Vector3 d = Vector3.ProjectOnPlane(e.transform.position - member.transform.position, Vector3.up);
            if (Vector3.Dot(d, Axis) < 0f) continue; // 不越过敌人，也不向身后追击。
            if (e == member.Target) return e; // 有效目标保持稳定，避免邻兵距离略变导致反复重抽推进距离。
            if (d.sqrMagnitude < nearest) { best = e; nearest = d.sqrMagnitude; }
        }
        return best;
    }
    void FixedUpdate()
    {
        if (!Active) { foreach (var m in members) if (m != null) m.Release(); return; }
        if (Time.time >= nextScan) { Scan(); nextScan = Time.time + Mathf.Max(.02f, scanInterval); }
        if (!rowChosen && RedCount > 0) ChooseRow();
        foreach (var m in members) if (m != null) m.Tick(rowChosen && m.IsFirstRow, retreatWhenOutnumbered && RedCount > BlueCount);
    }
    void OnDisable() { foreach (var m in members) if (m != null) m.Release(); }
    void OnDrawGizmosSelected()
    {
        if (!isActiveAndEnabled || !showGizmos || !enableBehaviour || gameObject.scene.name != sceneName || detectionTrigger == null ||
            !detectionTrigger.enabled || !detectionTrigger.gameObject.activeInHierarchy || !detectionTrigger.isTrigger) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(controlCenter, controlSize);
        Matrix4x4 old = Gizmos.matrix;
        if (detectionTrigger is BoxCollider box) { Gizmos.matrix = box.transform.localToWorldMatrix; Gizmos.DrawWireCube(box.center, box.size); }
        else if (detectionTrigger is SphereCollider sphere)
        {
            // Unity 球碰撞体使用最大缩放轴作为半径，非等比缩放时不能把它画成椭球。
            Vector3 scale = sphere.transform.lossyScale;
            Gizmos.DrawWireSphere(sphere.transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }
        else if (detectionTrigger is CapsuleCollider capsule)
        {
            Vector3 scale = capsule.transform.lossyScale;
            Vector3 localAxis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
            float lengthScale = Mathf.Abs(scale[capsule.direction]);
            float radiusScale = Mathf.Max(Mathf.Abs(scale[(capsule.direction + 1) % 3]), Mathf.Abs(scale[(capsule.direction + 2) % 3]));
            float radius = capsule.radius * radiusScale;
            Vector3 axis = capsule.transform.TransformDirection(localAxis).normalized;
            Vector3 center = capsule.transform.TransformPoint(capsule.center);
            Vector3 offset = axis * Mathf.Max(0f, capsule.height * lengthScale * .5f - radius);
            Vector3 a = center + offset, b = center - offset;
            Gizmos.DrawWireSphere(a, radius); Gizmos.DrawWireSphere(b, radius);
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < .9f ? Vector3.up : Vector3.right).normalized * radius;
            Vector3 v = Vector3.Cross(axis, u).normalized * radius;
            Gizmos.DrawLine(a + u, b + u); Gizmos.DrawLine(a - u, b - u);
            Gizmos.DrawLine(a + v, b + v); Gizmos.DrawLine(a - v, b - v);
        }
        else { Gizmos.DrawWireCube(detectionTrigger.bounds.center, detectionTrigger.bounds.size); }
        Gizmos.matrix = old;
        if (!Application.isPlaying)
        {
            // 编辑态也可查看每名候选盾兵的范围；正式第一排在接敌时确定，或由 firstRow 显式指定。
            foreach (var a in GetComponentsInChildren<SoldierAnimationController>(true))
                if (a.isActiveAndEnabled && a.myFaction == FactionType.Blue && !a.是远程单位 &&
                    (firstRow == null || firstRow.Length == 0 || Array.IndexOf(firstRow, a) >= 0)) DrawBoundary(a.transform.position);
            return;
        }
        foreach (var m in members)
        {
            if (m == null || !m.IsFirstRow) continue;
            DrawBoundary(m.Home);
        }
    }
    private void DrawBoundary(Vector3 home)
    {
        Gizmos.color = Color.cyan; Gizmos.DrawLine(home - Axis * maxBackward, home + Axis * maxForward);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(home + Axis * ((maxForward - maxBackward) * .5f), Quaternion.LookRotation(Axis), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(maxLateral * 2f, .2f, maxForward + maxBackward));
        Gizmos.matrix = old;
    }
}
