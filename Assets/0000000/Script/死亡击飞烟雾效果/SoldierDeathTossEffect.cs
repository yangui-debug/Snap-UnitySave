using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Combat/Soldier Death Toss Effect")]
public class SoldierDeathTossEffect : MonoBehaviour
{
    [Header("基础引用设置")]
    [Tooltip("飞行与姿态控制的根节点。如果为空，则默认使用挂载此脚本的自身 Transform。\n常用于只需要抛飞子层级模型，而保持父层级根节点不动的场景。")]
    public Transform flightRoot;

    [Tooltip("是否在生命周期 Start 时自动触发击飞死亡效果。\n勾选时生成即起飞；若取消勾选，需在外部（如 SoldierHealth）手动调用 PlayDeathToss()。")]
    public bool playOnStart = true;

    // ================= 新增：击飞方向控制 =================
    [Header("飞行方向设置")]
    [Tooltip("击飞基准方向的相对偏转角度（基于物体的正前方 flightRoot.forward）。\n180 表示向正后方击飞，0 表示向正前方，-90 或 90 表示向左右两侧。")]
    [Range(-180f, 180f)]
    public float baseTossAngle = 180f;

    [Tooltip("击飞方向的随机扇形角度范围（散布角度）。\n例如设为 60，则最终击飞方向会在基准方向的【左偏30度 ~ 右偏30度】范围内随机生成。")]
    [Range(0f, 360f)]
    public float tossAngleSpread = 60f;
    // ======================================================

    [Header("抛物线与运动参数")]
    [Tooltip("模拟垂直方向的下落重力。重力越大，落地越快、动作越干脆。\n滞空时间会通过（高度与重力）自动反算，无需手动指定飞行时间。")]
    [Min(0.1f)] public float gravity = 36f;

    [Tooltip("抛物线弧顶相对于起飞点的垂直高度随机区间（最小值，最大值）。")]
    public Vector2 tossHeightRange = new Vector2(7.5f, 12f);

    [Tooltip("从起点沿击飞方向抛出的水平位移距离随机区间（最小值，最大值）。")]
    public Vector2 horizontalDistanceRange = new Vector2(1.5f, 4.5f);

    [Tooltip("空中飞行时沿随机3D轴心翻滚的旋转角速度随机区间（最小值，最大值），单位：度/秒。")]
    public Vector2 spinSpeedRange = new Vector2(225f, 450f);

    [Header("落地与销毁设置")]
    [Tooltip("刚落地时，从空中的杂乱翻滚姿态平滑过渡到贴地横躺姿势所需的过渡时间（秒）。\n可以平滑消除触地瞬间的姿态突变。")]
    [Min(0.01f)] public float settleDuration = 0.15f;

    [Tooltip("完全摆正躺平后，尸体在地面上保留的时间（秒）。\n倒计时结束后会自动调用 Destroy 销毁整个尸体对象。")]
    [Min(0f)] public float corpseDuration = 3f;

    [Header("地面碰撞与贴合设置")]
    [Tooltip("射线检测指定的地面层（Layer）。\n用于计算落点实际高度和地形坡度法线（Normal），使尸体能贴合斜坡。")]
    public LayerMask groundLayers = 16;

    [Tooltip("计算落点时，射线发射起点向上抬高的高度。\n防止落点初始位置位于有起伏的斜坡或台阶内部导致射线穿透漏测。")]
    [Min(0.1f)] public float groundRayHeight = 5f;

    [Tooltip("从抬高后的发射起点，垂直向下投射射线的最大探测距离。\n若该距离内没扫到地面，则默认保持起飞时的高度。")]
    [Min(0.1f)] public float groundRayDistance = 20f;

    // --- 内部状态缓存 ---
    private bool hasPlayed;                     // 防止被多次重复调用击飞
    private Renderer[] modelRenderers;          // 缓存渲染器，用于落地时计算包围盒，避免模型插地

    private void Start()
    {
        // 勾选 playOnStart 时自动播放
        if (playOnStart) PlayDeathToss();
    }

    /// <summary>
    /// 手动播放击飞效果的外部接口。被调用后立即进入物理剥离与位移动画。
    /// </summary>
    public void PlayDeathToss()
    {
        if (!Application.isPlaying || hasPlayed) return;
        hasPlayed = true;

        if (flightRoot == null) flightRoot = transform;

        // 1. 关闭所有碰撞体，防止在空中或落地后阻挡其他角色
        foreach (Collider bodyCollider in GetComponentsInChildren<Collider>(true))
            bodyCollider.enabled = false;

        // 2. 将刚体设为运动学（Kinematic），禁用物理引擎接管，改用脚本纯代码算抛物线
        foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        // 3. 关闭动画器，防止角色受待机、跑动等动画的影响导致姿态错乱
        foreach (Animator animator in GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            animator.Update(0f);
            animator.enabled = false;
        }
        foreach (Animation animation in GetComponentsInChildren<Animation>(true))
            animation.enabled = false;

        // 缓存所有渲染器以备落地计算
        modelRenderers = flightRoot.GetComponentsInChildren<Renderer>(true);

        // 启动击飞协程
        StartCoroutine(TossRoutine());
    }

    /// <summary>
    /// 核心击飞抛物线协程
    /// </summary>
    private IEnumerator TossRoutine()
    {
        Vector3 start = flightRoot.position;
        Quaternion startRotation = flightRoot.rotation;

        // ================= 【核心修改：控制向后一定范围内的击飞方向】 =================
        // 获取对象水平面上的正前方
        Vector3 flatForward = flightRoot.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        // 计算随机偏移角度（在基础角度基础上增加正负一半的散布范围）
        float halfSpread = tossAngleSpread * 0.5f;
        float randomOffsetAngle = Random.Range(-halfSpread, halfSpread);
        float finalAngle = baseTossAngle + randomOffsetAngle;

        // 生成最终的击飞方向向量
        Vector3 direction = Quaternion.AngleAxis(finalAngle, Vector3.up) * flatForward;
        // ==============================================================================

        // 计算目标落点 (起点 + 方向 * 随机水平距离)
        Vector3 end = start + direction * Random.Range(horizontalDistanceRange.x, horizontalDistanceRange.y);
        Vector3 normal = Vector3.up;

        // 射线检测地形，寻找真实的地面高度与法线（贴坡度）
        if (Physics.Raycast(end + Vector3.up * groundRayHeight, Vector3.down,
            out RaycastHit hit, groundRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            normal = hit.normal;
        }
        else
        {
            // fallback: 没有检测到有效碰撞体时沿用出生时的高度
            end.y = start.y;
        }

        // 随机空中翻滚轴和角速度
        Vector3 spinAxis = Random.onUnitSphere;
        float spinSpeed = Random.Range(spinSpeedRange.x, spinSpeedRange.y);

        // 随机抛飞高度并反算飞行持续时间
        float height = Random.Range(tossHeightRange.x, tossHeightRange.y);
        float duration = CalculateFlightDuration(height, gravity);

        float elapsed = 0f;

        // --- 第一阶段：空中抛物线飞行 ---
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 依据时间 t (0~1) 计算当前抛物线位置
            flightRoot.position = EvaluateArc(start, end, height, t);
            // 依据时间和角速度持续翻转
            flightRoot.rotation = Quaternion.AngleAxis(spinSpeed * elapsed, spinAxis) * startRotation;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- 第二阶段：落地姿态平滑修正 ---
        Quaternion impactRotation = flightRoot.rotation;

        // 计算最终躺平的旋转：基于地形法线（Vector3.up 对齐 normal），水平面再做随机偏转，再翻倒90度
        Quaternion lyingRotation = Quaternion.FromToRotation(Vector3.up, normal)
            * Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up)
            * Quaternion.Euler(90f, 0f, 0f);

        float settleTime = Mathf.Max(0.01f, settleDuration);
        elapsed = 0f;

        while (elapsed < settleTime)
        {
            // 使用球面插值平滑贴合地面
            flightRoot.rotation = Quaternion.Slerp(impactRotation, lyingRotation, elapsed / settleTime);
            PlaceOnGround(end, normal);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- 第三阶段：静止保留与销毁 ---
        flightRoot.rotation = lyingRotation;
        PlaceOnGround(end, normal); // 确保最后一次完美贴合

        yield return new WaitForSeconds(Mathf.Max(0f, corpseDuration));
        Destroy(gameObject);
    }

    /// <summary>
    /// 基于包围盒（Bounds）将角色完美贴合在地面上，防止粗壮部分（如肩膀）穿模进地下
    /// </summary>
    private void PlaceOnGround(Vector3 point, Vector3 normal)
    {
        flightRoot.position = point;
        float lowest = float.PositiveInfinity;

        // 遍历所有的模型渲染器，找出包围盒最底下的一个点
        foreach (Renderer modelRenderer in modelRenderers)
        {
            if (modelRenderer == null || !modelRenderer.enabled ||
                modelRenderer is ParticleSystemRenderer || modelRenderer is TrailRenderer ||
                modelRenderer is LineRenderer) continue;

            Bounds bounds = modelRenderer.bounds;
            Vector3 extents = bounds.extents;
            // 计算当前包围盒在法线方向上的投影半径
            float projectedRadius = Mathf.Abs(normal.x) * extents.x
                + Mathf.Abs(normal.y) * extents.y + Mathf.Abs(normal.z) * extents.z;

            lowest = Mathf.Min(lowest, Vector3.Dot(bounds.center - point, normal) - projectedRadius);
        }

        // 如果找到了有效最低点，则沿着地形法线向上微调位置，留出 0.02f 的防穿插余量
        if (!float.IsPositiveInfinity(lowest))
            flightRoot.position += normal * (0.02f - lowest);
    }

#if UNITY_EDITOR
    [ContextMenu("Validate Toss Arc")]
    private void ValidateTossArc()
    {
        Vector3 start = new Vector3(1f, 2f, 3f);
        Vector3 end = new Vector3(4f, 1f, 5f);
        Debug.Assert((EvaluateArc(start, end, 3f, 0f) - start).sqrMagnitude < 0.0001f);
        Debug.Assert((EvaluateArc(start, end, 3f, 1f) - end).sqrMagnitude < 0.0001f);
        Vector3 apex = (start + end) * 0.5f + Vector3.up * 3f;
        Debug.Assert((EvaluateArc(start, end, 3f, 0.5f) - apex).sqrMagnitude < 0.0001f);
        Debug.Assert((EvaluateArc(start, end, 3f, 2f) - end).sqrMagnitude < 0.0001f);
        Debug.Assert(Mathf.Abs(CalculateFlightDuration(12f, 24f) - 2f) < 0.0001f);
        Debug.Assert(Mathf.Abs(CalculateFlightDuration(3f, 24f) - 1f) < 0.0001f);
        Debug.Assert(CalculateFlightDuration(12f, 24f) > CalculateFlightDuration(7.5f, 24f));
    }
#endif

    /// <summary>
    /// 根据抛起的最高点与重力，反向推导计算出滞空飞行时间
    /// 公式：同高度起落的抛物线 滞空时间 = sqrt(8 * 抛起高度 / 重力)
    /// </summary>
    internal static float CalculateFlightDuration(float height, float gravity)
    {
        return Mathf.Max(0.1f, Mathf.Sqrt(8f * Mathf.Max(0f, height) / Mathf.Max(0.1f, gravity)));
    }

    /// <summary>
    /// 二次贝塞尔/抛物线插值，计算飞行过程中任意时刻 t 的三维位置
    /// </summary>
    internal static Vector3 EvaluateArc(Vector3 start, Vector3 end, float height, float t)
    {
        t = Mathf.Clamp01(t);
        // 水平方向使用线性插值 (Lerp)，垂直方向通过二次函数附加抛起高度
        return Vector3.Lerp(start, end, t) + Vector3.up * (4f * height * t * (1f - t));
    }
}