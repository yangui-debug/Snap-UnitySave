using UnityEngine;
using System.Collections.Generic; // 引入集合库用于维护共享锁定表

public enum FactionType { Blue, Yellow, Green, Orange, Red, Neutral }
public enum TargetSelectionMode { Nearest, Farthest }
public enum TargetPriority { DistanceOnly, BossFirst, MinionFirst }

// ==================== 箭矢脚本选择枚举 ====================
public enum 箭矢脚本类型
{
    HomingArrowTest, // 默认使用的测试箭矢脚本
    HomingArrow      // 进阶抛物线下落追踪箭矢脚本
}

public class SoldierAnimationController : MonoBehaviour
{
    [Header("攻击自然随机（默认仅目标场景生效）")]
    [Tooltip("新增随机功能总开关。关闭后保留原有攻击延迟、攻击速度和站位行为。")]
    public bool enableNaturalAttackRandomness = true;
    [Tooltip("仅在此名称的场景启用新增随机功能（包括运行时生成的士兵）。留空则所有场景生效；默认防止影响其他场景。")]
    public string randomizationSceneName = "20261012-PVE收关敌万军";
    [Tooltip("首次准备攻击的等待范围，单位秒；0.08~0.25 即 80~250ms。在本功能启用时替代原近战/远程首次延迟，不叠加。最小值大于最大值时自动交换；负数按零处理。")]
    public Vector2 attackStartupDelay = new Vector2(0.08f, 0.25f);
    [Tooltip("每次攻击命中事件之后、下一次攻击之前的额外停顿范围（秒）。默认 0~0.3；每次独立抽取，不与旧的每次攻击延迟叠加。停顿通过关闭攻击条件实现，动画实际退出仍遵循原 Animator 过渡。")]
    public Vector2 attackPause = new Vector2(0f, 0.3f);
    [Range(0f, 0.5f)]
    [Tooltip("每个士兵的固定攻速差异比例。0.1 表示原 Animator 速度的 90%~110%；启用时首次攻击抽取，整个生命周期保持，避免每帧抖动。只在指定攻击状态生效，移动/胜利状态恢复原速度。")]
    public float attackSpeedVariation = 0.1f;
    [Tooltip("允许调整速度的 Animator 基础层状态名。填写实际攻击状态短名称；未匹配的状态保持原速度。无需修改 Animator Controller。")]
    public string[] randomizedAttackStateNames = { "Attack" };
    [Tooltip("每个角色固定的前后站位偏移绝对值范围（世界单位）。默认随机取 0.2~0.4，再随机正负；正值离敌人更远，负值更近。由 SoldierAIMovement 在停止距离上应用，不瞬移、不修改桥路径。")]
    public Vector2 attackStandingOffset = new Vector2(0.2f, 0.4f);

    /// <summary>将新增行为限定在指定场景，动态生成的角色也按其所属场景判断。</summary>
    public bool NaturalAttackRandomnessActive => enableNaturalAttackRandomness &&
        (string.IsNullOrEmpty(randomizationSceneName) || gameObject.scene.name == randomizationSceneName);

    private bool naturalRandomInitialized;
    private float naturalAttackSpeed = 1f;
    private float naturalStandingOffset;
    private float pauseUntil;
    private bool speedOverridden;
    private float originalAnimatorSpeed;

    // 固定参数名只计算一次哈希；仍在原来的帧与原来的分支写入相同参数值。
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int NearEnemyParameter = Animator.StringToHash("IsNearEnemy");
    private static readonly int NearBuildingParameter = Animator.StringToHash("IsNearBuilding");
    private static readonly int NearTreeParameter = Animator.StringToHash("IsNearTree");
    private static readonly int VictoriousParameter = Animator.StringToHash("IsVictorious");
    // 同步检查公开状态名，支持运行期间在 Inspector 改名或替换数组，不永久缓存旧配置。
    private string[] cachedAttackStateNames;
    private int[] cachedAttackStateHashes;

    /// <summary>仅在状态名或数量变化时重新计算哈希，避免每个角色每帧重复进行字符串哈希。</summary>
    private bool IsRandomizedAttackState(int shortNameHash)
    {
        int count = randomizedAttackStateNames.Length;
        if (cachedAttackStateNames == null || cachedAttackStateNames.Length != count)
        {
            cachedAttackStateNames = new string[count];
            cachedAttackStateHashes = new int[count];
        }
        for (int i = 0; i < count; i++)
        {
            string stateName = randomizedAttackStateNames[i];
            if (string.IsNullOrEmpty(stateName)) continue;
            if (cachedAttackStateNames[i] != stateName)
            {
                cachedAttackStateNames[i] = stateName;
                cachedAttackStateHashes[i] = Animator.StringToHash(stateName);
            }
            if (shortNameHash == cachedAttackStateHashes[i]) return true;
        }
        return false;
    }

    /// <summary>懒初始化避免移动脚本和攻击脚本 Start 顺序不同。每个实例只抽取一次。</summary>
    private void InitializeNaturalRandomness()
    {
        if (naturalRandomInitialized) return;
        naturalRandomInitialized = true;
        float variation = Mathf.Clamp(attackSpeedVariation, 0f, 0.5f);
        naturalAttackSpeed = Random.Range(1f - variation, 1f + variation);
        naturalStandingOffset = SampleNonNegativeRange(attackStandingOffset) * (Random.value < 0.5f ? -1f : 1f);
    }

    /// <summary>返回每人固定的站位偏移；禁用功能时为零，不改变原停止距离。</summary>
    public float GetAttackStandingOffset()
    {
        if (!NaturalAttackRandomnessActive) return 0f;
        InitializeNaturalRandomness();
        return naturalStandingOffset;
    }

    /// <summary>统一处理 Inspector 中填反的范围和负值，单位由调用方定义。</summary>
    private static float SampleNonNegativeRange(Vector2 range)
    {
        return Random.Range(Mathf.Max(0f, Mathf.Min(range.x, range.y)), Mathf.Max(0f, Mathf.Max(range.x, range.y)));
    }

    /// <summary>在动画更新前调整攻击状态播放速度；同时检查过渡目标，退出攻击立即恢复。</summary>
    void LateUpdate()
    {
        bool attackState = false;
        if (animator != null && animator.isActiveAndEnabled && NaturalAttackRandomnessActive && !hasWon && randomizedAttackStateNames != null)
        {
            AnimatorStateInfo state = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            attackState = IsRandomizedAttackState(state.shortNameHash);
        }
        if (attackState)
        {
            InitializeNaturalRandomness();
            if (!speedOverridden) { originalAnimatorSpeed = animator.speed; speedOverridden = true; }
            animator.speed = originalAnimatorSpeed * naturalAttackSpeed;
        }
        else RestoreAnimatorSpeed();
    }

    /// <summary>离开攻击、关闭随机或禁用组件时恢复进入攻击前的速度，避免影响其他动作。</summary>
    private void RestoreAnimatorSpeed()
    {
        if (speedOverridden && animator != null) animator.speed = originalAnimatorSpeed;
        speedOverridden = false;
    }

    [Header("阵营设置")]
    public FactionType myFaction;

    [Header("身份与攻击倾向设置")]
    public bool isBoss = false;
    public TargetPriority targetPriority = TargetPriority.DistanceOnly;

    [Header("攻击设置")]
    [Tooltip("该士兵每次攻击造成的伤害力度（普通士兵填1，强力士兵可以填5）")]
    public int attackPower = 5;

    // =========================================================================
    // ==================== 远程防集火（分散索敌）设置 ==========================
    // =========================================================================
    [Header("========== 远程防集火 (目标分散) 设置 ==========")]
    [Tooltip("勾选后，同一阵营的多个远程士兵在短时间内不会集火同一个敌人，而是优先分散攻击不同敌人。")]
    public bool 启用远程防集火分散 = true;

    [Tooltip("当一个敌人被某个友方远程士兵锁定后，在多少秒内其他友军远程士兵会尽量避开他（寻找其他未被锁定的目标）。\n" +
             "推荐：1.0 ~ 2.5 秒（通常与射击动画或攻击间隔相匹配）。")]
    public float 远程防集火保护时长 = 1.5f;

    [Tooltip("敌人处于被友军锁定保护期时被扣除的索敌权重。\n" +
             "默认 2000 分。只要周围还有其他敌人士兵，就会绝对优先打别人；如果周围只剩这 1 个敌人，依然会作为兜底目标正常攻击，绝不会发呆。")]
    public int 防集火扣除权重 = 2000;

    private class 目标锁定记录
    {
        public SoldierAnimationController 锁定者;
        public FactionType 阵营;
        public float 过期时间;
    }

    private static readonly Dictionary<Transform, 目标锁定记录> 远程共享锁定表 = new Dictionary<Transform, 目标锁定记录>();
    // =========================================================================

    // =========================================================================
    // ==================== 原有攻击前随机延迟设置 ==============================
    // 下列近战/远程延迟与“每次攻击后重新计算延迟”在新增自然随机关闭或场景不匹配时生效。
    // 自然随机开启时统一使用 attackStartupDelay 和 attackPause，不会把两套等待累加。
    // =========================================================================
    [Header("========== 攻击前随机延迟设置 ==========")]
    [Tooltip("新增自然随机未生效时使用。近战士兵（未挂载弓箭）进入攻击距离后，切换到攻击动画前的最小随机等待时间（秒）。\n" +
             "推荐：0.1 ~ 0.2 秒。可以让冲到脸前的近战兵错开挥刀时机，避免所有人动作完全同步。")]
    public float 近战攻击前最小延迟 = 0.1f;

    [Tooltip("新增自然随机未生效时使用。近战士兵进入攻击距离后，切换到攻击动画前的最大随机等待时间（秒）。\n" +
             "推荐：0.3 ~ 0.5 秒。")]
    public float 近战攻击前最大延迟 = 0.4f;

    [Tooltip("新增自然随机未生效时使用。远程士兵（挂载了弓箭预制体）进入射程后，切换到射击动画前的最小随机等待时间（秒）。\n" +
             "推荐：0.2 ~ 0.4 秒。让弓箭手们错开拉弓节奏。")]
    public float 远程攻击前最小延迟 = 0.2f;

    [Tooltip("新增自然随机未生效时使用。远程士兵进入射程后，切换到射击动画前的最大随机等待时间（秒）。\n" +
             "推荐：0.6 ~ 0.9 秒。")]
    public float 远程攻击前最大延迟 = 0.8f;

    [Tooltip("新增自然随机生效时由 attackPause 接管此选项。【进阶选项】是否在每次攻击完成后也重新计算随机延迟？\n" +
             "· 勾选：每次砍完一刀或射出一箭后，下一次出招前都再次随机等待（攻击节奏更自然、有起伏）。\n" +
             "· 不勾选（默认）：仅在刚靠近敌人【首次准备切换到攻击动画】时随机延迟，之后保持连续攻击。")]
    public bool 每次攻击后重新计算延迟 = false;

    private float 当前攻击延迟计时 = 0f;
    private bool 正在等待攻击延迟 = false;
    private bool 已通过攻击延迟 = false;

    public bool 是远程单位 => (arrowPrefab != null && firePoint != null);
    // =========================================================================

    [Header("目标标签设置")]
    public string soldierTag = "Soldier";
    public string buildingTag = "Building";
    public string treeTag = "Tree";

    [Header("==========目标检测范围==========")]

    [Tooltip(
    "敌人搜索范围。\n" +
    "单位：Unity世界单位。\n" +
    "推荐：普通近战 3~5，远程单位 8~15。\n" +
    "范围越大，AI计算量越高。"
    )]
    [Range(1f, 100f)]
    public float enemyDetectRange = 10.0f;

    [Tooltip(
    "建筑搜索范围。\n" +
    "影响单位是否会主动攻击建筑。\n" +
    "推荐：3~8。"
    )]
    [Range(1f, 20f)]
    public float buildingDetectRange = 5.0f;

    [Tooltip(
    "树木/资源搜索范围。\n" +
    "如果单位负责采集，可以提高。\n" +
    "推荐：3~8。"
    )]
    [Range(1f, 20f)]
    public float treeDetectRange = 5.0f;

    [Header("索敌偏好设置")]
    public TargetSelectionMode targetMode = TargetSelectionMode.Nearest;

    [Header("朝向与视野设置")]
    [Tooltip(
    "是否优先攻击正面的敌人。\n" +
    "开启后单位会更像近战战斗。\n" +
    "推荐：近战开启，远程关闭。"
    )]
    public bool prioritizeFrontal = true;

    [Tooltip(
    "是否限制只能攻击正面目标。\n" +
    "开启后背后的敌人不会被攻击。\n" +
    "推荐：动作游戏开启，策略游戏关闭。"
    )]
    public bool strictFrontalAttackOnly = true;

    [Tooltip(
    "正面攻击角度范围。\n" +
    "数值越小，要求敌人在越正前方。\n" +
    "推荐：\n" +
    "严格动作战斗：15~30度\n" +
    "普通战斗：45~90度"
    )]
    [Range(5, 180)]
    public float frontalAngleThreshold = 20f;

    [Header("攻击高度差限制（目标Y - 自身Y）")]
    public float minRelativeHeight = -10.0f;
    public float maxRelativeHeight = 5.0f;

    [Header("索敌降级机制")]
    public bool allowHeightFallback = true;

    [Header("组件与自动化设置")]
    [Tooltip("负责播放攻击、移动和胜利动作的 Animator；留空时 Start 自动读取同物体组件。随机攻速仅作用于所配置的攻击状态。")]
    public Animator animator;

    [Header("远程武器设置 (近战请留空)")]
    [Tooltip("选择发射箭矢时调用的脚本类型：\n" +
             "· HomingArrowTest：测试/原本的箭矢脚本（默认）。\n" +
             "· HomingArrow：进阶抛物线下落追踪箭矢脚本。\n" +
             "请确保你的 arrowPrefab 预制体上挂载了对应的脚本。")]
    public 箭矢脚本类型 箭矢脚本选择 = 箭矢脚本类型.HomingArrowTest;

    [Tooltip("远程箭矢预制体；与 firePoint 同时赋值时按远程单位处理，留空则走近战伤害流程。预制体应带有所选箭矢脚本。")]
    public GameObject arrowPrefab;
    [Tooltip("箭矢生成的世界位置与旋转来源；由攻击动画的 OnActionComplete 事件触发发射。")]
    public Transform firePoint;
    [Tooltip("传给箭矢脚本的攻击特效列表，具体播放方式由所选箭矢脚本决定。")]
    public GameObject[] attackEffects;

    [Header("近战与特效设置")]
    [Tooltip("近战攻击动画事件触发时生成的刀光特效预制体；留空则不生成刀光，不影响伤害。")]
    public GameObject slashEffectPrefab;    // 挥砍刀光特效
    [Tooltip("刀光生成的位置和朝向；与刀光预制体同时赋值才生成。")]
    public Transform slashSpawnPoint;       // 刀光生成点
    [Tooltip("伤害命中活着的 SoldierHealth 时生成的特效；位置为目标世界坐标加近战受击特效偏移。")]
    public GameObject hitImpactEffectPrefab;// 命中敌人时的打击特效
    [Tooltip("刀光与命中特效的自动销毁时间（秒）；同时传给远程箭矢脚本用于管理其特效。")]
    public float effectDestroyTime = 2.0f;

    // =========================================================================
    // ==================== 新增：受击特效偏移暴露参数 ==========================
    // =========================================================================
    [Tooltip("命中敌人时打击特效的位置相对敌人中心坐标的偏移量（X左右，Y高低，Z前后）。\n" +
             "· Y 默认 1.2 米，通常在角色胸口位置。\n" +
             "· 如果特效产生在脚底，调大 Y；如果偏高，调小 Y。")]
    public Vector3 近战受击特效偏移 = new Vector3(0, 1.2f, 0);
    // =========================================================================

    [Header("近战范围伤害(AoE)设置")]
    [Tooltip("开启后攻击事件伤害身边扇形区域内的合法敌人；关闭后只伤害当前目标。随机停顿不会改变此伤害计算。")]
    public bool isAoEMelee = true;
    [Tooltip("近战范围伤害检测球的半径（世界单位），以角色位置为中心。")]
    public float meleeAoERadius = 6.0f;
    [Tooltip("以角色正前方为中心的近战伤害扇形总角度（度），左右各占一半。")]
    [Range(0, 360)]
    public float meleeAoEAngle = 110.0f;

    [Header("其他系统设置 (折叠)")]
    public bool dynamicRetargeting = true;
    public float moveThreshold = 0.01f;
    public float victoryCheckInterval = 1.0f;

    private float victoryCheckTimer;
    private bool hasWon = false;

    private Vector3 lastPosition;
    public Transform CurrentTarget => currentTarget; // 公开当前锁定的目标，供移动脚本读取
    private Transform currentTarget;

    private string currentAction = "";

    void Start()
    {
        lastPosition = transform.position;
        if (animator == null) animator = GetComponent<Animator>();
        victoryCheckTimer = victoryCheckInterval;
    }

    void OnDisable()
    {
        RestoreAnimatorSpeed();
        pauseUntil = 0f;
        重置攻击延迟状态();
        释放自身的远程锁定();
    }

    void Update()
    {
        float moveSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, moveSpeed);
        }
        lastPosition = transform.position;

        victoryCheckTimer -= Time.deltaTime;
        if (victoryCheckTimer <= 0f)
        {
            victoryCheckTimer = victoryCheckInterval;
            CheckVictoryCondition();
        }

        if (hasWon) return;

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            float currentDist = GetFlatDistance(transform.position, currentTarget.position);

            float limitRange = enemyDetectRange;
            if (currentAction == buildingTag) limitRange = buildingDetectRange;
            if (currentAction == treeTag) limitRange = treeDetectRange;

            float relativeY = currentTarget.position.y - transform.position.y;
            bool isHeightValid = (relativeY >= minRelativeHeight && relativeY <= maxRelativeHeight);

            if (currentDist <= limitRange && (isHeightValid || allowHeightFallback))
            {
                if (!dynamicRetargeting)
                {
                    UpdateAnimatorStates(currentDist);
                    return;
                }
            }
            else
            {
                currentTarget = null;
            }
        }

        GameObject target = FindBestTarget(out string targetType, out float targetDistance);

        if (target != null)
        {
            if (currentTarget != target.transform)
            {
                重置攻击延迟状态();
            }

            currentTarget = target.transform;
            currentAction = targetType;

            if (是远程单位 && 启用远程防集火分散 && currentAction == "Enemy")
            {
                注册远程锁定(currentTarget);
            }

            float dist = GetFlatDistance(transform.position, currentTarget.position);
            UpdateAnimatorStates(dist);
        }
        else
        {
            ResetAllActionStates();
            currentTarget = null;
            currentAction = "";
        }
    }

    void CheckVictoryCondition()
    {
        GameObject[] allSoldiers = GameObject.FindGameObjectsWithTag(soldierTag);
        bool enemyExists = false;

        foreach (GameObject soldierObj in allSoldiers)
        {
            if (soldierObj == null || !soldierObj.activeInHierarchy) continue;

            SoldierAnimationController otherSoldier = soldierObj.GetComponent<SoldierAnimationController>();
            if (otherSoldier != null && otherSoldier.myFaction != this.myFaction)
            {
                enemyExists = true;
                break;
            }
        }

        if (!enemyExists)
        {
            if (!hasWon)
            {
                hasWon = true;
                currentTarget = null;
                currentAction = "";
                ResetAllActionStates();

                if (animator != null)
                {
                    animator.SetBool(VictoriousParameter, true);
                }
                Debug.Log($"【{myFaction}】阵营获得了胜利！");
            }
        }
        else
        {
            if (hasWon)
            {
                hasWon = false;
                currentTarget = null;
                Debug.Log($"【{myFaction}】阵营发现新目标，继续战斗！");

                if (animator != null)
                {
                    animator.SetBool(VictoriousParameter, false);
                }
            }
        }
    }

    /// <summary>
    /// 原攻击动画事件入口：先向当前有效目标发射箭矢或结算近战伤害，再安排下一击的随机停顿。
    /// 保留原动画事件的位置；因此停顿从本事件开始，实际动作间隔还包含剩余动画与 Controller 过渡时间。
    /// 不随机伤害、不改变特效命中位置、不修改动画资源，以免破坏现有战斗规则。
    /// </summary>
    public void OnActionComplete()
    {
        // 旧动画事件仍负责伤害/发射时机；新增停顿只控制下一次攻击，不移动命中帧。
        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            Vector3 hitDir = (currentTarget.position - transform.position).normalized;

            // 1. 远程攻击逻辑
            if (arrowPrefab != null && firePoint != null)
            {
                if (启用远程防集火分散)
                {
                    注册远程锁定(currentTarget);
                }

                GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);

                if (箭矢脚本选择 == 箭矢脚本类型.HomingArrowTest)
                {
                    HomingArrowTest arrowScript = arrowObj.GetComponent<HomingArrowTest>();
                    if (arrowScript != null)
                    {
                        arrowScript.Fire(currentTarget, attackEffects, effectDestroyTime, attackPower);
                    }
                    else
                    {
                        Debug.LogWarning($"【{gameObject.name}】发射的箭矢预制体上未找到 HomingArrowTest 组件，请检查预制体或切换“箭矢脚本选择”选项！", arrowObj);
                    }
                }
                else if (箭矢脚本选择 == 箭矢脚本类型.HomingArrow)
                {
                    HomingArrow arrowScript = arrowObj.GetComponent<HomingArrow>();
                    if (arrowScript != null)
                    {
                        arrowScript.Fire(currentTarget, attackEffects, effectDestroyTime, attackPower);
                    }
                    else
                    {
                        Debug.LogWarning($"【{gameObject.name}】发射的箭矢预制体上未找到 HomingArrow 组件，请检查预制体或切换“箭矢脚本选择”选项！", arrowObj);
                    }
                }
            }
            // 2. 近战攻击逻辑
            else
            {
                if (slashEffectPrefab != null && slashSpawnPoint != null)
                {
                    GameObject slash = Instantiate(slashEffectPrefab, slashSpawnPoint.position, slashSpawnPoint.rotation);
                    Destroy(slash, effectDestroyTime);
                }

                if (isAoEMelee)
                {
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAoERadius);
                    foreach (Collider hitCol in hitColliders)
                    {
                        if (IsValidEnemy(hitCol.gameObject))
                        {
                            Vector3 dirToTarget = (hitCol.transform.position - transform.position);
                            dirToTarget.y = 0;
                            if (Vector3.Angle(transform.forward, dirToTarget) <= meleeAoEAngle / 2f)
                            {
                                ApplyDamageAndEffect(hitCol.gameObject, dirToTarget.normalized);
                            }
                        }
                    }
                }
                else
                {
                    ApplyDamageAndEffect(currentTarget.gameObject, hitDir);
                }
            }

            if (NaturalAttackRandomnessActive || 每次攻击后重新计算延迟)
            {
                if (NaturalAttackRandomnessActive) pauseUntil = Time.time + SampleNonNegativeRange(attackPause);
                已通过攻击延迟 = false;
                正在等待攻击延迟 = false;
                if (animator != null)
                {
                    animator.SetBool(NearEnemyParameter, false);
                }
            }
        }
    }

    // 执行伤害和命中特效（使用暴露的“受击特效偏移”参数）
    private void ApplyDamageAndEffect(GameObject targetObj, Vector3 hitDirection)
    {
        SoldierHealth targetHealth = targetObj.GetComponent<SoldierHealth>();
        if (targetHealth != null && !targetHealth.isDead)
        {
            targetHealth.TakeDamage(attackPower, hitDirection, source: SoldierDamageSource.Melee);

            if (hitImpactEffectPrefab != null)
            {
                // 动态应用自定义偏移量
                Vector3 hitPos = targetObj.transform.position + 近战受击特效偏移;
                GameObject impact = Instantiate(hitImpactEffectPrefab, hitPos, Quaternion.identity);
                Destroy(impact, effectDestroyTime);
            }
        }
    }

    private bool IsValidEnemy(GameObject obj)
    {
        if (obj == this.gameObject) return false;

        if (obj.CompareTag(soldierTag))
        {
            SoldierAnimationController other = obj.GetComponent<SoldierAnimationController>();
            if (other != null && other.myFaction != this.myFaction) return true;
        }
        else if (obj.CompareTag(buildingTag) || obj.CompareTag(treeTag))
        {
            return true;
        }
        return false;
    }

    float GetFlatDistance(Vector3 a, Vector3 b)
    {
        Vector2 aFlat = new Vector2(a.x, a.z);
        Vector2 bFlat = new Vector2(b.x, b.z);
        return Vector2.Distance(aFlat, bFlat);
    }

    float GetInitialBestDist()
    {
        return (targetMode == TargetSelectionMode.Nearest) ? Mathf.Infinity : -1f;
    }

    void UpdateAnimatorStates(float dist)
    {
        if (animator == null) return;

        bool inEnemyRange = (currentAction == "Enemy" && dist <= enemyDetectRange);

        // 命中后的停顿与首次启动分开计时。停顿结束后直接放行，避免重复叠加首次等待。
        if (NaturalAttackRandomnessActive && inEnemyRange && pauseUntil > 0f)
        {
            animator.SetBool(NearEnemyParameter, Time.time >= pauseUntil);
            animator.SetBool(NearBuildingParameter, false);
            animator.SetBool(NearTreeParameter, false);
            if (Time.time >= pauseUntil) { pauseUntil = 0f; 已通过攻击延迟 = true; }
            return;
        }

        if (inEnemyRange)
        {
            if (!已通过攻击延迟)
            {
                if (!正在等待攻击延迟)
                {
                    正在等待攻击延迟 = true;
                    float minWait = 是远程单位 ? 远程攻击前最小延迟 : 近战攻击前最小延迟;
                    float maxWait = 是远程单位 ? 远程攻击前最大延迟 : 近战攻击前最大延迟;
                    // 保留旧场景的独立近战/远程参数；目标场景使用统一的启动错开范围。
                    当前攻击延迟计时 = NaturalAttackRandomnessActive ? SampleNonNegativeRange(attackStartupDelay) : Random.Range(Mathf.Min(minWait, maxWait), Mathf.Max(minWait, maxWait));
                }
                else
                {
                    当前攻击延迟计时 -= Time.deltaTime;
                    if (当前攻击延迟计时 <= 0f)
                    {
                        正在等待攻击延迟 = false;
                        已通过攻击延迟 = true;
                    }
                }
            }
        }
        else
        {
            重置攻击延迟状态();
        }

        bool canPlayAttack = (inEnemyRange && 已通过攻击延迟);
        animator.SetBool(NearEnemyParameter, canPlayAttack);

        animator.SetBool(NearBuildingParameter, currentAction == buildingTag && dist <= buildingDetectRange);
        animator.SetBool(NearTreeParameter, currentAction == treeTag && dist <= treeDetectRange);
    }

    void ResetAllActionStates()
    {
        重置攻击延迟状态();
        if (animator == null) return;
        animator.SetBool(NearEnemyParameter, false);
        animator.SetBool(NearBuildingParameter, false);
        animator.SetBool(NearTreeParameter, false);
    }

    private void 重置攻击延迟状态()
    {
        // 换目标、离开范围或停止动作时取消上一目标的等待，下一目标重新抽取首次延迟。
        pauseUntil = 0f;
        正在等待攻击延迟 = false;
        已通过攻击延迟 = false;
        当前攻击延迟计时 = 0f;
    }

    private void 注册远程锁定(Transform targetTransform)
    {
        if (targetTransform == null) return;

        if (!远程共享锁定表.TryGetValue(targetTransform, out 目标锁定记录 record) || record == null)
        {
            record = new 目标锁定记录();
            远程共享锁定表[targetTransform] = record;
        }

        record.锁定者 = this;
        record.阵营 = this.myFaction;
        record.过期时间 = Time.time + 远程防集火保护时长;
    }

    private void 释放自身的远程锁定()
    {
        if (currentTarget != null && 远程共享锁定表.TryGetValue(currentTarget, out 目标锁定记录 record))
        {
            if (record != null && record.锁定者 == this)
            {
                远程共享锁定表.Remove(currentTarget);
            }
        }
    }

    GameObject FindBestTarget(out string type, out float detectDistance)
    {
        // 一次同步查询内角色位置和朝向不变，复用快照，减少候选数量倍增的原生 Transform 访问。
        // 保留原 Physics 查询、碰撞体遍历顺序、距离算法和同分目标比较，避免改变选敌结果。
        Vector3 searchPosition = transform.position;
        Vector3 searchForward = transform.forward;
        type = "";
        detectDistance = Mathf.Infinity;

        GameObject bestEnemy = null;
        int bestEnemyScore = -100000;
        float bestEnemyDist = GetInitialBestDist();

        GameObject bestBuilding = null; float bestBuildingDist = GetInitialBestDist();
        GameObject bestTree = null; float bestTreeDist = GetInitialBestDist();

        float maxSearchRadius = Mathf.Max(enemyDetectRange, buildingDetectRange, treeDetectRange);
        float heightOffset = Mathf.Max(Mathf.Abs(minRelativeHeight), Mathf.Abs(maxRelativeHeight));
        Collider[] hitColliders = Physics.OverlapSphere(searchPosition, maxSearchRadius + heightOffset);

        foreach (Collider hit in hitColliders)
        {
            GameObject obj = hit.gameObject;
            if (obj == this.gameObject) continue;

            Transform candidateTransform = obj.transform;
            Vector3 candidatePosition = candidateTransform.position;
            float flatDist = GetFlatDistance(searchPosition, candidatePosition);

            if (obj.CompareTag(soldierTag))
            {
                SoldierAnimationController otherSoldier = obj.GetComponent<SoldierAnimationController>();
                if (otherSoldier != null && otherSoldier.myFaction != this.myFaction)
                {
                    if (flatDist <= enemyDetectRange)
                    {
                        float relativeHeight = candidatePosition.y - searchPosition.y;
                        bool isHeightValid = (relativeHeight >= minRelativeHeight && relativeHeight <= maxRelativeHeight);
                        if (!isHeightValid && !allowHeightFallback) continue;

                        bool isFrontal = true;
                        if (prioritizeFrontal || strictFrontalAttackOnly)
                        {
                            Vector3 dirToTarget = candidatePosition - searchPosition;
                            dirToTarget.y = 0;
                            float angle = Vector3.Angle(searchForward, dirToTarget);
                            isFrontal = (angle <= frontalAngleThreshold);
                        }

                        if (strictFrontalAttackOnly && !isFrontal) continue;

                        int score = 0;
                        if (targetPriority == TargetPriority.BossFirst && otherSoldier.isBoss) score += 1000;
                        if (targetPriority == TargetPriority.MinionFirst && !otherSoldier.isBoss) score += 1000;
                        if (isHeightValid) score += 100;
                        if (isFrontal) score += 10;

                        if (是远程单位 && 启用远程防集火分散)
                        {
                            if (远程共享锁定表.TryGetValue(candidateTransform, out 目标锁定记录 record))
                            {
                                if (record != null &&
                                    record.锁定者 != null &&
                                    record.锁定者 != this &&
                                    record.阵营 == this.myFaction &&
                                    Time.time < record.过期时间)
                                {
                                    score -= 防集火扣除权重;
                                }
                            }
                        }

                        if (bestEnemy == null ||
                            score > bestEnemyScore ||
                            (score == bestEnemyScore && IsBetterTarget(flatDist, bestEnemyDist)))
                        {
                            bestEnemy = obj;
                            bestEnemyScore = score;
                            bestEnemyDist = flatDist;
                        }
                    }
                }
            }
            else if (obj.CompareTag(buildingTag))
            {
                if (flatDist <= buildingDetectRange && IsBetterTarget(flatDist, bestBuildingDist))
                {
                    bestBuilding = obj;
                    bestBuildingDist = flatDist;
                }
            }
            else if (obj.CompareTag(treeTag))
            {
                if (flatDist <= treeDetectRange && IsBetterTarget(flatDist, bestTreeDist))
                {
                    bestTree = obj;
                    bestTreeDist = flatDist;
                }
            }
        }

        if (bestEnemy != null) { type = "Enemy"; detectDistance = enemyDetectRange; return bestEnemy; }
        if (bestBuilding != null) { type = buildingTag; detectDistance = buildingDetectRange; return bestBuilding; }
        if (bestTree != null) { type = treeTag; detectDistance = treeDetectRange; return bestTree; }

        return null;
    }

    bool IsBetterTarget(float newDist, float currentBestDist)
    {
        if (targetMode == TargetSelectionMode.Nearest)
            return newDist < currentBestDist;
        else
            return newDist > currentBestDist;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyDetectRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, buildingDetectRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, treeDetectRange);

        if (isAoEMelee)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Vector3 leftRayPoint = Quaternion.Euler(0, -meleeAoEAngle / 2f, 0) * transform.forward * meleeAoERadius;
            Vector3 rightRayPoint = Quaternion.Euler(0, meleeAoEAngle / 2f, 0) * transform.forward * meleeAoERadius;

            Gizmos.DrawRay(transform.position, leftRayPoint);
            Gizmos.DrawRay(transform.position, rightRayPoint);
            Gizmos.DrawLine(transform.position + leftRayPoint, transform.position + rightRayPoint);
        }
    }
}
