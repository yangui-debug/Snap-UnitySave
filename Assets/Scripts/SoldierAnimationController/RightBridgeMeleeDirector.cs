using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 右桥近战规则的唯一配置入口。挂在本场景的“蓝木盾卫士”上；
/// 只管理其下的近战盾兵，以及进入指定世界区域的红方近战，不改变索敌、伤害或 Timeline。
/// 编组父节点继续由 Timeline 驱动，单兵自身节点才允许进行战斗位移。
/// </summary>
[DisallowMultipleComponent]
public sealed class RightBridgeMeleeDirector : MonoBehaviour
{
    [Header("生效范围与总开关")]
    [Tooltip("关闭后所有已接入单位停止新增跟进、等候、击退接管及来源闪白，恢复原脚本控制；不把已移动的角色瞬移回初始点。")]
    public bool enablePushPull = true;
    [Tooltip("场景名称必须完全匹配才生效，防止复制编组到其他场景后误启用。")]
    public string sceneName = "20261012-PVE收关敌万军";
    [Tooltip("蓝方木盾兵组根节点。只接入此节点下具有攻击脚本和生命脚本的近战单位；本场景填写自身。")]
    public Transform shieldRoot;
    [Tooltip("允许接入的红方阵营。不会接入远程单位或其他场景的单位。")]
    public FactionType opponentFaction = FactionType.Red;
    [Tooltip("右桥交战区世界坐标中心；不随父节点或 Timeline 移动。红方仅在此区域内接入新增行为。")]
    public Vector3 combatCenter = new Vector3(9f, 1f, -13f);
    [Tooltip("交战区世界尺寸（X宽、Y高、Z深）；绿色框外红方保持原寻路。不要扩大至左桥。")]
    public Vector3 combatSize = new Vector3(10f, 8f, 14f);
    [Tooltip("蓝方向敌方推进的世界方向，自动投影到 XZ 平面。前后界限均沿此固定轴计算。")]
    public Vector3 forwardAxis = Vector3.forward;

    [Header("蓝方有限跟进")]
    [Tooltip("开启后盾兵可沿战线轴向跟进；关闭后仅保留其他启用的交战规则。")]
    public bool enableShieldFollow = true;
    [Min(0f), Tooltip("相对每名盾兵首次接战时固定世界站位的最大前进距离。不随每次补位重设基准。")]
    public float maxAdvance = 1.5f;
    [Min(0f), Tooltip("相对固定站位的最大后退距离；击退也受此边界限制，碰撞不足时可更短。")]
    public float maxRetreat = 1f;
    [Min(0f), Tooltip("盾兵跟进速度（世界单位/秒），只控制位移，不改变攻击动画速度。")]
    public float followSpeed = 1.6f;
    [Min(0f), Tooltip("盾兵跟进停止距离（世界单位），仅控制移动，不改变攻击脚本的检测/伤害范围。")]
    public float shieldStopDistance = 1.1f;
    [Min(0f), Tooltip("到达停止距离后额外超出此值才重新跟进，避免距离临界点反复走停。")]
    public float followHysteresis = 0.15f;

    [Header("前排退路与后排等候")]
    [Tooltip("开启后，同阵营后排检测到前排退让通道时暂停主动推进。不会改变已存在的攻击事件。")]
    public bool enableQueueSpacing = true;
    [Min(0f), Tooltip("前排身后保留的退让净空间（世界单位），另加双方碰撞半径。")]
    public float retreatClearance = 2f;
    [Min(0.05f), Tooltip("每名单位用于预留位置和通道检测的最小水平半径。实际碰撞体较大时采用其实际半径。")]
    public float minimumUnitRadius = 0.4f;
    [Min(0f), Tooltip("同列检测的额外横向余量（世界单位），避免斜向后排挤入前排退路。")]
    public float lanePadding = 0.15f;
    [Min(0.02f), Tooltip("友军至少领先此纵向距离才被视为前排（世界单位）。避免木盾兵斜列中略微靠前的同排邻兵互相阻挡；默认 0.6。")]
    public float minimumFrontSeparation = 0.6f;

    [Header("受击移动优先级")]
    [Tooltip("开启后由交战单元统一执行击退，击退/僵直期间常规寻路和跟进让位。关闭后保留原有受击位移。")]
    public bool enableHitMovementPriority = true;
    [Min(0f), Tooltip("碰撞扫掠保留的安全间隙（世界单位），防止高速击退跨过墙体或其他角色。")]
    public float collisionSkin = 0.025f;
    [Tooltip("跟进、击退和位置预留使用的阻挡层。默认全部；忽略 Trigger、自身和已死亡单位。")]
    public LayerMask blockingLayers = ~0;

    [Header("红方补位与位置预留")]
    [Tooltip("红方从前方友军阻挡状态恢复，或接战目标死亡后，先随机等待再向有效新目标补位。")]
    public bool enableRedRefillDelay = true;
    [Tooltip("红方独立随机补位等待范围（秒）。仅影响主动移动，不禁止有目标时的攻击；条件失效则取消。")]
    public Vector2 refillDelay = new Vector2(0.18f, 0.55f);
    [Min(0.02f), Tooltip("空位预留的有效时间（秒）；移动时持续刷新，死亡、停用、失去目标或等待时立即释放。")]
    public float reservationLifetime = 0.12f;

    [Header("按伤害来源区分闪白（仅接入单位）")]
    [Tooltip("启用独立近战闪白配置。关闭表示关闭近战闪白，不关闭伤害、击退、僵直、缩放及死亡反馈。")]
    public bool enableMeleeFlash = true;
    [Min(0f), Tooltip("近战受击闪白持续时间（秒）。只接管原 EmissionFlash/WhiteMaterialSwap，不改变加速闪红的独立流程。")]
    public float meleeFlashDuration = 0.08f;
    [Min(0f), Tooltip("近战自发光闪白强度相对原 emissionIntensity 的倍率。白模替换为二值效果，只调整持续时间和启用开关。")]
    public float meleeFlashMultiplier = 0.25f;
    [Tooltip("启用独立远程闪白。默认沿用每名角色原有强度和持续时间；关闭仅禁用远程闪白。")]
    public bool enableRangedFlash = true;
    [Tooltip("开启时远程保留角色原有闪白强度和时长；关闭后使用下方远程参数。")]
    public bool preserveOriginalRangedFlash = true;
    [Min(0f), Tooltip("不保留原配置时，远程闪白时长（秒）。")]
    public float rangedFlashDuration = 0.3f;
    [Min(0f), Tooltip("不保留原配置时，远程自发光强度相对角色原强度的倍率。")]
    public float rangedFlashMultiplier = 1f;

    private static readonly List<RightBridgeMeleeDirector> directors = new List<RightBridgeMeleeDirector>();
    private readonly List<RightBridgeMeleeUnit> units = new List<RightBridgeMeleeUnit>();
    public bool Active => isActiveAndEnabled && enablePushPull && gameObject.scene.name == sceneName;
    public Vector3 Axis => Vector3.ProjectOnPlane(forwardAxis, Vector3.up).sqrMagnitude > 0.0001f
        ? Vector3.ProjectOnPlane(forwardAxis, Vector3.up).normalized : Vector3.forward;

    void OnEnable()
    {
        if (!directors.Contains(this)) directors.Add(this);
        // 播放中脚本重载、组件重新启用时 Start 不一定再次执行，必须重建编组与单兵的关联。
        // 只对已明确配置根节点的场景管理器恢复；新加组件尚未配置时不接管其他对象。
        if (shieldRoot != null) InitializeUnits();
    }
    void OnDisable()
    {
        directors.Remove(this);
        foreach (var unit in units) if (unit != null) unit.ReleaseControl();
    }

    void Start()
    {
        if (shieldRoot == null) shieldRoot = transform;
        InitializeUnits();
    }

    private void InitializeUnits()
    {
        foreach (var controller in shieldRoot.GetComponentsInChildren<SoldierAnimationController>(true))
        {
            if (!controller.isActiveAndEnabled || controller.是远程单位 || controller.GetComponent<SoldierHealth>() == null) continue;
            Attach(controller, true);
        }
        if (!Application.isPlaying || !Active) return;
        // 重载前已接入的红方组件也重新登记，避免队列/空位表在域重载后丢失。
        foreach (var unit in FindObjectsOfType<RightBridgeMeleeUnit>())
        {
            if (unit.gameObject.scene != gameObject.scene || unit.transform.IsChildOf(shieldRoot)) continue;
            var controller = unit.GetComponent<SoldierAnimationController>();
            if (controller != null && controller.myFaction == opponentFaction && !controller.是远程单位 && Contains(unit.transform.position))
                Attach(controller, false);
        }
    }

    /// <summary>红方移动脚本在物理帧调用：动态生成、过桥分流均保持原有规则，只在右桥区接入。</summary>
    public static RightBridgeMeleeUnit FindOrAttach(SoldierAnimationController controller)
    {
        if (controller == null || !controller.isActiveAndEnabled || controller.是远程单位) return null;
        foreach (var director in directors)
            if (director != null && director.Active && director.gameObject.scene == controller.gameObject.scene &&
                controller.myFaction == director.opponentFaction && director.Contains(controller.transform.position))
                return director.Attach(controller, false);
        return null;
    }

    private RightBridgeMeleeUnit Attach(SoldierAnimationController controller, bool shield)
    {
        var unit = controller.GetComponent<RightBridgeMeleeUnit>();
        if (unit == null) unit = controller.gameObject.AddComponent<RightBridgeMeleeUnit>();
        unit.Configure(this, controller, shield);
        if (!units.Contains(unit)) units.Add(unit);
        return unit;
    }

    public bool Contains(Vector3 position) => new Bounds(combatCenter, combatSize).Contains(position);

    /// <summary>同列后排不得进入前排身后的净退路。已死亡、失活的单位不占位。</summary>
    public bool IsQueueBlocked(RightBridgeMeleeUnit unit, Vector3 direction)
    {
        if (!enableQueueSpacing) return false;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        foreach (var other in units)
        {
            if (other == null || other == unit || !other.Controlled || other.Controller.myFaction != unit.Controller.myFaction) continue;
            Vector3 delta = other.transform.position - unit.transform.position;
            float ahead = Vector3.Dot(delta, direction);
            float radii = unit.Radius + other.Radius;
            if (ahead > Mathf.Max(0.02f, minimumFrontSeparation) && ahead < retreatClearance + radii &&
                Mathf.Abs(Vector3.Dot(delta, side)) < radii + lanePadding) return true;
        }
        return false;
    }

    /// <summary>原子式预留目标位置；前一个单位预留后，后续单位同一物理帧便会看到占用。</summary>
    public bool TryReserve(RightBridgeMeleeUnit unit, Vector3 destination)
    {
        foreach (var other in units)
        {
            if (other == null || other == unit || !other.Controlled || other.Controller.myFaction != unit.Controller.myFaction) continue;
            // 此表只互斥未来空位；当前站位由队列退路和实际碰撞检查负责。
            // 原队形存在接触/轻微重叠时，不能用远端目标点再把离开重叠的移动也封死。
            if (other.ReservationUntil <= Time.time) continue;
            Vector3 point = other.ReservedPosition;
            Vector3 delta = Vector3.ProjectOnPlane(destination - point, Vector3.up);
            float separation = unit.Radius + other.Radius;
            if (delta.sqrMagnitude < separation * separation) return false;
        }
        unit.ReservedPosition = destination;
        unit.ReservationUntil = Time.time + Mathf.Max(0.02f, reservationLifetime);
        return true;
    }

    public void Forget(RightBridgeMeleeUnit unit) { units.Remove(unit); }

    void OnDrawGizmosSelected()
    {
        if (!enablePushPull || gameObject.scene.name != sceneName) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(combatCenter, combatSize);
        if (enableShieldFollow)
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = shieldRoot != null ? shieldRoot.position : transform.position;
            Gizmos.DrawLine(origin - Axis * maxRetreat, origin + Axis * maxAdvance);
        }
    }
}
