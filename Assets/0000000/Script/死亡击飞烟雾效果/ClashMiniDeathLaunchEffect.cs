using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 把普通字符串字段显示为 Unity Tag 下拉菜单。
/// 运行时不依赖 UnityEditor，打包时编辑器代码会被自动排除。
/// </summary>
public sealed class ClashMiniCameraTagAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ClashMiniCameraTagAttribute))]
public sealed class ClashMiniCameraTagDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        EditorGUI.EndProperty();
    }
}
#endif

/// <summary>
/// Clash Mini 风格的死亡击飞效果。
/// 适配 Unity 2022.3，并为 X 轴约 90°的俯视正交摄影机提供可见的屏幕空间抛物线。
/// 本组件应挂在 SoldierHealth.deathEffectPrefab 使用的死亡替身预制体上。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Combat/Clash Mini Death Launch Effect")]
public class ClashMiniDeathLaunchEffect : MonoBehaviour
{
    public enum TrajectoryMode
    {
        OrthographicCameraArc,
        WorldSpaceArc
    }

    public enum DeathFlashMode
    {
        None,
        EmissionFlash,
        WhiteMaterialSwap
    }

    public enum OrthographicScreenDirectionMode
    {
        NearestHorizontalEdge,
        ManualScreenDirection
    }

    [Header("1. 基础控制")]
    [Tooltip("死亡替身生成后是否自动播放。推荐：勾选。只有需要由 Timeline 或其他脚本手动调用 PlayDeathLaunch() 时才取消。")]
    public bool playOnStart = true;

    [Tooltip("真正被抛起、缩放和旋转的角色节点。推荐：拖入模型总根节点；留空时使用当前物体。")]
    public Transform flightRoot;

    [Tooltip("是否把旧 Animator 停在第一帧。推荐：勾选，可保留正常站姿并阻止旧死亡动画干扰代码运动。")]
    public bool freezeLegacyAnimator = true;

    [Tooltip("是否关闭死亡替身上的碰撞体和刚体物理。推荐：勾选，防止物理系统与代码抛物线争夺位置。")]
    public bool disablePhysics = true;

    [Header("1.5 初始受击闪白")]

    [Tooltip("死亡替身出现时播放的初始视觉反馈")]
    public DeathFlashMode initialFlashMode = DeathFlashMode.EmissionFlash;

    [Tooltip("闪白持续时间，推荐 0.05 ~ 0.15 秒")]
    [Range(0f, 1f)]
    public float initialFlashDuration = 0.08f;

    [Tooltip("需要闪白的 Renderer。身体、武器等全部拖入。")]
    public Renderer[] flashRenderers;

    [Header("Emission 闪白设置")]

    public string emissionPropertyName = "_EmissionColor";

    [ColorUsage(false, true)]
    public Color flashColor = Color.white;

    [Range(0f, 20f)]
    public float emissionIntensity = 5f;

    [Header("白模替换设置")]

    [Tooltip("纯白 Unlit 材质，仅在 WhiteMaterialSwap 模式使用")]
    public Material whiteMaterial;

    private Dictionary<Renderer, Material[]> originalMaterials =
        new Dictionary<Renderer, Material[]>();

    [Header("2. 摄影机与轨迹模式")]
    [Tooltip("轨迹计算模式。90°俯视正交镜头推荐 Orthographic Camera Arc；普通斜俯视或透视镜头可用 World Space Arc。")]
    public TrajectoryMode trajectoryMode = TrajectoryMode.OrthographicCameraArc;

    [ClashMiniCameraTag]
    [Tooltip("用于自动寻找摄影机的 Tag。Unity 默认主摄影机 Tag 是 MainCamera（没有空格）；也可以在下拉菜单中选择你创建的其他 Tag。推荐保证场景里只有一台启用中的摄影机使用该 Tag。")]
    public string cameraTag = "MainCamera";

    [Tooltip("当带指定 Tag 的物体本身没有 Camera 时，是否继续在它的子物体中查找。推荐：勾选；适用于把 Tag 标在摄影机父节点上的层级。")]
    public bool searchCameraInChildren = true;

    [Header("3. 动作节奏")]
    [Tooltip("新死亡模型出现后完全静止的时间。参考视频的停顿很短。推荐：0.08 ~ 0.14 秒；默认 0.10。")]
    [Min(0f)] public float stillDuration = 0.1f;

    [Tooltip("起飞前压低身体并播放地面预备特效的时间。参考视频推荐：0.08 ~ 0.14 秒；默认 0.10。")]
    [Min(0f)] public float anticipationDuration = 0.1f;

    [Tooltip("从离地到飞出画面的总时间。参考视频速度很快。推荐：0.35 ~ 0.50 秒；默认 0.42。")]
    [Min(0.01f)] public float flightDuration = 0.42f;

    [Tooltip("是否忽略 Time.timeScale。推荐：不勾选，让死亡动作跟随命中慢放；若游戏经常把 timeScale 设为 0 且仍要播放，再勾选。")]
    public bool useUnscaledTime = false;

    [Header("4. 90°正交镜头：画面内抛物线")]
    [Tooltip("正交镜头下如何决定飞出方向。Nearest Horizontal Edge 会根据角色当前屏幕位置，自动飞向距离更近的左边缘或右边缘；你的需求推荐选它。Manual Screen Direction 则继续使用下面的 Screen Travel Direction。")]
    public OrthographicScreenDirectionMode screenDirectionMode =
        OrthographicScreenDirectionMode.NearestHorizontalEdge;

    [Tooltip("最近边缘模式下，终点要超出屏幕左右边缘多少。单位是画面宽度比例；0.06 表示飞到边缘外约 6% 的位置，保证模型完整离开画面。推荐：0.04 ~ 0.10；默认 0.06。")]
    [Range(0f, 0.5f)] public float horizontalEdgeExitPadding = 0.06f;

    [Tooltip("最近边缘模式下，飞向左右边缘时同时沿画面竖直方向偏移多少。单位是画面高度比例，X=最小、Y=最大；正数向画面上方。推荐：(0.04, 0.12)，设为 (0, 0) 就会近似水平飞向最近边缘。")]
    public Vector2 edgeVerticalDriftRange = new Vector2(0.04f, 0.12f);

    [Tooltip("角色在摄影机画面里的基础飞行方向。X=左右，Y=上下。参考视频推荐：(0.18, 1)，即主要向画面上方、略向左右飞出。")]
    public Vector2 screenTravelDirection = new Vector2(0.18f, 1f);

    [Tooltip("仅 Manual Screen Direction 模式生效：是否随机选择向画面左侧或右侧飞。")]
    public bool randomizeScreenSide = true;

    [Tooltip("仅 Manual Screen Direction 模式生效：画面飞行方向的随机偏角。推荐：8° ~ 18°；默认 12°。")]
    [Range(0f, 90f)] public float screenDirectionJitter = 12f;

    [Tooltip("勾选后，正交镜头的飞行距离和弧度按摄影机画面高度计算，不受 Orthographic Size 影响。90°正交镜头强烈推荐：勾选。")]
    public bool useViewportRelativeDistance = true;

    [Tooltip("仅 Manual Screen Direction 模式生效：画面内飞行距离范围，X=最小、Y=最大。开启 Viewport Relative 时 1 代表一整个画面高度；参考视频推荐：(0.48, 0.60)。")]
    public Vector2 screenFlightDistanceRange = new Vector2(0.48f, 0.6f);

    [Tooltip("轨迹向左右弯曲的幅度范围，X=最小、Y=最大。开启 Viewport Relative 时按画面高度计算；参考视频推荐：(0.05, 0.10)。")]
    public Vector2 visibleArcHeightRange = new Vector2(0.05f, 0.1f);

    [Tooltip("同时沿世界 Y 轴真正抬高角色的高度范围。90°正上方不直接可见，但会影响灯光。推荐：(0.25, 0.50)。")]
    public Vector2 physicalLiftHeightRange = new Vector2(0.25f, 0.5f);

    [Tooltip("飞行中段的额外放大比例，用于正交镜头强调弹起。参考视频推荐：0.03 ~ 0.08；默认 0.05。")]
    [Range(0f, 0.35f)] public float orthographicApexScaleBoost = 0.05f;

    [Tooltip("飞出画面前角色缩小到原尺寸的比例。参考视频推荐：0.55 ~ 0.75；默认 0.65。设为 1 则不缩小。")]
    [Range(0.1f, 1f)] public float endScaleMultiplier = 0.65f;

    [Header("5. 普通镜头：世界空间抛物线")]
    [Tooltip("World Space Arc 模式下的局部飞行方向，Y 会被忽略。推荐：(0, 0, -1)，再通过预制体朝向控制飞出方向。")]
    public Vector3 localWorldFlightDirection = Vector3.back;

    [Tooltip("World Space Arc 模式下是否在水平面 360°随机飞出。推荐：通常不勾选；大量杂兵需要散开时可勾选。")]
    public bool randomizeWorldFlightDirection = false;

    [Tooltip("不使用 360°随机方向时的左右散射角。推荐：10° ~ 25°；默认 18°。")]
    [Range(0f, 180f)] public float worldDirectionSpread = 18f;

    [Tooltip("World Space Arc 模式下的水平飞行距离范围。按约 2 Unity 单位高的角色，推荐：(1.5, 2.2)。")]
    public Vector2 worldFlightDistanceRange = new Vector2(1.5f, 2.2f);

    [Tooltip("World Space Arc 模式下沿世界 Y 轴的抛物线高度范围。推荐：(2.2, 3.0)。")]
    public Vector2 worldArcHeightRange = new Vector2(2.2f, 3f);

    [Header("6. 随机翻滚")]
    [Tooltip("飞行时的随机旋转速度范围，单位为度/秒。参考视频推荐：(950, 1450)，短时间内快速翻滚约 1 ~ 1.7 圈。")]
    public Vector2 spinSpeedRange = new Vector2(950f, 1450f);

    [Tooltip("是否随机选择翻滚轴。推荐：勾选；代码会避免接近纯竖直轴，减少只原地转圈的情况。")]
    public bool randomizeSpinAxis = true;

    [Tooltip("关闭随机翻滚轴时使用的局部旋转轴。推荐：(1, 0.2, 0.35)，会产生带侧翻的前滚。")]
    public Vector3 localSpinAxis = new Vector3(1f, 0.2f, 0.35f);

    [Header("7. 起飞前形变")]
    [Tooltip("起飞前角色被压扁的程度。意义：表现受力蓄积。推荐：0.15 ~ 0.22；默认 0.18。")]
    [Range(0f, 0.45f)] public float squashAmount = 0.18f;

    [Tooltip("起飞瞬间角色被纵向拉长的程度。意义：强化爆发速度。推荐：0.10 ~ 0.18；默认 0.14。")]
    [Range(0f, 0.35f)] public float stretchAmount = 0.14f;

    [Tooltip("拉伸状态用飞行总时长的多少比例恢复。数值越小回弹越快。推荐：0.15 ~ 0.25；默认 0.20。")]
    [Range(0.01f, 1f)] public float stretchRecovery = 0.2f;

    [Header("8. 起飞震屏与脚下特效")]
    [Tooltip("角色真正离地、脚下烟雾出现的同一帧，是否由死亡替身自己的 Cinemachine Impulse Source 直接发出震屏。推荐勾选。")]
    public bool triggerCameraShakeOnTakeoff = true;

    [Tooltip("死亡替身自己使用的 Cinemachine Impulse Source。推荐在这个死亡预制体上添加 Source 后拖入；留空时会自动从当前物体、父物体或子物体查找，不再依赖 HitFeelManager。")]
    public CinemachineImpulseSource takeoffImpulseSource;

    [Tooltip("起飞瞬间直接传给 Impulse Source 的力度。普通士兵推荐 0.8 ~ 1.2；Boss 推荐 1.3 ~ 1.8；默认 1。")]
    [Range(0f, 3f)] public float takeoffShakeForce = 1f;

    [Tooltip("起飞前在地面播放的预备效果，例如冲击圈、裂纹或闪光。推荐：持续约 0.2 ~ 0.4 秒的短特效；可以留空。")]
    public GameObject preLaunchEffectPrefab;

    [Tooltip("角色起飞瞬间留在脚下的烟雾。推荐：向外快速扩散、持续约 0.4 ~ 0.8 秒的烟尘。")]
    public GameObject takeoffSmokePrefab;

    [Tooltip("飞行途中连续生成的烟雾团，形成参考视频里的弧形尾迹。推荐：使用会自动播放、带 Billboard 粒子的短烟雾预制体；可以留空。")]
    public GameObject flightTrailSmokePrefab;

    [Tooltip("飞行烟雾从哪个点生成。推荐：拖入角色脚底或身体下方空物体；留空则从 flightRoot 中心稍后方生成。")]
    public Transform flightTrailPoint;

    [Tooltip("两团飞行烟雾之间的生成间隔。数值越小尾迹越密。参考视频推荐：0.045 ~ 0.070 秒；默认 0.055。")]
    [Min(0.01f)] public float trailSpawnInterval = 0.055f;

    [Tooltip("每团飞行烟雾的随机缩放范围，X=最小、Y=最大。推荐：(0.80, 1.20)，让尾迹不显得机械重复。")]
    public Vector2 trailScaleRange = new Vector2(0.8f, 1.2f);

    [Tooltip("每团飞行烟雾生成后多久销毁。推荐：1.0 ~ 1.8 秒，并确保略长于粒子自身播放时间；默认 1.4。")]
    [Min(0f)] public float trailEffectLifetime = 1.4f;

    [Tooltip("飞行结束时的落地烟雾或冲击效果。Clash Mini 式飞出消失可留空；需要落地反馈时再绑定。")]
    public GameObject landingEffectPrefab;

    [Tooltip("脚底定位点。推荐：在死亡替身脚底建立一个空物体并拖入；留空则使用 flightRoot 的位置。")]
    public Transform groundEffectPoint;

    [Tooltip("允许地面射线检测的层。推荐：只勾选 Ground，避免烟雾吸附到其他角色或武器碰撞体上。")]
    public LayerMask groundLayers = ~0;

    [Tooltip("从脚底点上方多高的位置开始向下检测。推荐：角色身高约 2 单位时设 1.0 ~ 1.8；默认 1.5。")]
    [Min(0f)] public float groundRayStartHeight = 1.5f;

    [Tooltip("从射线起点向下允许检测的总距离。推荐：3 ~ 5；默认 4。若地面有高低差可以加大。")]
    [Min(0.01f)] public float groundRayDistance = 4f;

    [Tooltip("特效沿地面法线向上偏移的距离。推荐：0.01 ~ 0.04；默认 0.02，可防止粒子与地面闪烁。")]
    public float effectHeightOffset = 0.02f;

    [Tooltip("每个独立生成的地面特效多久后自动销毁。推荐：略长于粒子自身时长，通常 1.5 ~ 2.5 秒；默认 2。")]
    [Min(0f)] public float effectLifetime = 2f;

    [Header("9. 结束处理")]
    [Tooltip("飞行结束后是否隐藏死亡模型。Clash Mini 式飞出消失推荐：勾选；希望角色落地停留则取消。")]
    public bool hideModelAfterFlight = true;

    [Tooltip("飞出过程的速度曲线。参考视频推荐保留默认 Linear；需要更柔和的起落时再改成 Ease In Out。")]
    public AnimationCurve horizontalMotion = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Coroutine deathSequence;
    private bool preLaunchEffectPlayed;
    private bool takeoffSmokePlayed;
    private bool warnedMissingCamera;
    private bool warnedMissingTakeoffImpulseSource;

    private struct GroundPose
    {
        public Vector3 position;
        public Quaternion rotation;

        public GroundPose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }

    private struct FlightPath
    {
        public Vector3 travelDirection;
        public Vector3 visibleArcDirection;
        public float distance;
        public float visibleArcHeight;
        public float physicalLiftHeight;
        public bool useOrthographicScale;
        public Camera camera;
    }

    private void Awake()
    {
        if (flightRoot == null)
        {
            flightRoot = transform;
        }

        CacheOriginalMaterials();

        ResolveTakeoffImpulseSource();
        PrepareLegacyComponents();
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayDeathLaunch();
        }
    }

    /// <summary>
    /// 手动播放死亡击飞。playOnStart 已勾选时不需要额外调用。
    /// </summary>
    public void PlayDeathLaunch()
    {
        if (!Application.isPlaying || deathSequence != null)
        {
            return;
        }

        if (flightRoot == null)
        {
            flightRoot = transform;
        }

        PrepareLegacyComponents();
        preLaunchEffectPlayed = false;
        takeoffSmokePlayed = false;
        deathSequence = StartCoroutine(DeathLaunchRoutine());
    }

    private IEnumerator DeathLaunchRoutine()
    {
        PlayInitialFlash();

        Vector3 startPosition = flightRoot.position;
        Quaternion startRotation = flightRoot.rotation;
        Vector3 startScale = flightRoot.localScale;
        Vector3 startGroundAnchor = GetGroundAnchorPosition();
        FlightPath path = BuildFlightPath(startRotation);

        yield return WaitForDuration(stillDuration);

        TriggerPreLaunchEffect();
        yield return PlayAnticipation(startScale);

        flightRoot.localScale = startScale;
        TriggerTakeoffSmoke();

        Vector3 endPosition = startPosition + path.travelDirection * path.distance;
        Vector3 stretchedScale = GetStretchedScale(startScale);
        Vector3 spinAxis = GetSpinAxis(startRotation, path.camera);
        float spinSpeed = RandomRange(spinSpeedRange);
        if (Random.value < 0.5f)
        {
            spinSpeed = -spinSpeed;
        }

        float elapsed = 0f;
        float safeTrailInterval = Mathf.Max(0.01f, trailSpawnInterval);
        float trailTimer = safeTrailInterval;
        while (elapsed < flightDuration)
        {
            float deltaTime = GetDeltaTime();
            elapsed += deltaTime;
            trailTimer += deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / flightDuration);
            float horizontalTime = horizontalMotion != null
                ? horizontalMotion.Evaluate(normalizedTime)
                : normalizedTime;
            float parabola = 4f * normalizedTime * (1f - normalizedTime);

            Vector3 position = Vector3.LerpUnclamped(startPosition, endPosition, horizontalTime);
            position += path.visibleArcDirection * (path.visibleArcHeight * parabola);
            position += Vector3.up * (path.physicalLiftHeight * parabola);
            flightRoot.position = position;

            float spinAngle = spinSpeed * elapsed;
            flightRoot.rotation = Quaternion.AngleAxis(spinAngle, spinAxis) * startRotation;

            float recoveryTime = Mathf.Clamp01(normalizedTime / Mathf.Max(0.01f, stretchRecovery));
            Vector3 recoveredScale = Vector3.Lerp(stretchedScale, startScale, Smooth01(recoveryTime));
            float flightScale = 1f;
            if (path.useOrthographicScale)
            {
                float shrinkingScale = Mathf.Lerp(1f, endScaleMultiplier, Smooth01(normalizedTime));
                flightScale = shrinkingScale + orthographicApexScaleBoost * parabola;
            }
            flightRoot.localScale = recoveredScale * flightScale;

            if (flightTrailSmokePrefab != null)
            {
                while (trailTimer >= safeTrailInterval)
                {
                    trailTimer -= safeTrailInterval;
                    SpawnFlightTrailSmoke();
                }
            }
            yield return null;
        }

        flightRoot.position = endPosition;
        flightRoot.localScale = path.useOrthographicScale
            ? startScale * endScaleMultiplier
            : startScale;

        if (landingEffectPrefab != null)
        {
            Vector3 landingSample = startGroundAnchor + path.travelDirection * path.distance;
            GroundPose landingPose = GetGroundPose(landingSample, startRotation * Vector3.forward);
            SpawnGroundEffect(landingEffectPrefab, landingPose);
        }

        if (hideModelAfterFlight)
        {
            HideModelRenderers();
        }

        deathSequence = null;
    }

    private FlightPath BuildFlightPath(Quaternion startRotation)
    {
        Camera activeCamera = ResolveCamera();
        bool useCameraArc = trajectoryMode == TrajectoryMode.OrthographicCameraArc && activeCamera != null;

        if (trajectoryMode == TrajectoryMode.OrthographicCameraArc && activeCamera == null && !warnedMissingCamera)
        {
            warnedMissingCamera = true;
            Debug.LogWarning(
                "ClashMiniDeathLaunchEffect：找不到 Tag 为 '" + cameraTag + "' 的可用 Camera，已临时改用世界空间抛物线。请确认该 Tag 已创建、拼写完全一致，并且摄影机物体处于启用状态。",
                this);
        }

        FlightPath path = new FlightPath();
        path.camera = activeCamera;
        path.useOrthographicScale = useCameraArc;

        if (useCameraArc)
        {
            float cameraHeightInWorldUnits = 1f;
            if (useViewportRelativeDistance && activeCamera.orthographic)
            {
                cameraHeightInWorldUnits = activeCamera.orthographicSize * 2f;
            }

            if (screenDirectionMode == OrthographicScreenDirectionMode.NearestHorizontalEdge)
            {
                path.travelDirection = GetNearestHorizontalEdgeTravel(
                    activeCamera,
                    out path.visibleArcDirection,
                    out path.distance);
            }
            else
            {
                path.travelDirection = GetCameraPlaneTravelDirection(
                    activeCamera,
                    out path.visibleArcDirection);
                path.distance = RandomRange(screenFlightDistanceRange) * cameraHeightInWorldUnits;
            }

            path.visibleArcHeight = RandomRange(visibleArcHeightRange) * cameraHeightInWorldUnits;
            path.physicalLiftHeight = RandomRange(physicalLiftHeightRange);
        }
        else
        {
            path.travelDirection = GetWorldTravelDirection(startRotation);
            path.visibleArcDirection = Vector3.zero;
            path.distance = RandomRange(worldFlightDistanceRange);
            path.visibleArcHeight = 0f;
            path.physicalLiftHeight = RandomRange(worldArcHeightRange);
        }

        return path;
    }

    private Camera ResolveCamera()
    {
        if (string.IsNullOrEmpty(cameraTag))
        {
            return Camera.main;
        }

        try
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(cameraTag);
            for (int i = 0; i < taggedObjects.Length; i++)
            {
                Camera foundCamera = taggedObjects[i].GetComponent<Camera>();
                if (foundCamera == null && searchCameraInChildren)
                {
                    foundCamera = taggedObjects[i].GetComponentInChildren<Camera>(false);
                }

                if (foundCamera != null && foundCamera.enabled && foundCamera.gameObject.activeInHierarchy)
                {
                    return foundCamera;
                }
            }
        }
        catch (UnityException)
        {
            // Tag 未在 Tags and Layers 中创建时，Unity 会抛出异常。
            // 这里不让死亡效果中断，BuildFlightPath 会给出更容易理解的警告并使用备用轨迹。
        }

        return null;
    }

    private Vector3 GetNearestHorizontalEdgeTravel(
        Camera activeCamera,
        out Vector3 curveDirection,
        out float travelDistance)
    {
        Transform source = flightRoot != null ? flightRoot : transform;
        Vector3 startWorldPosition = source.position;
        Vector3 startViewportPosition = activeCamera.WorldToViewportPoint(startWorldPosition);

        float distanceToLeftEdge = Mathf.Abs(startViewportPosition.x);
        float distanceToRightEdge = Mathf.Abs(1f - startViewportPosition.x);
        float edgeDistanceDifference = Mathf.Abs(distanceToLeftEdge - distanceToRightEdge);
        float sideSign = edgeDistanceDifference < 0.001f
            ? (Random.value < 0.5f ? -1f : 1f)
            : (distanceToLeftEdge < distanceToRightEdge ? -1f : 1f);

        float targetViewportX = sideSign < 0f
            ? -horizontalEdgeExitPadding
            : 1f + horizontalEdgeExitPadding;
        float verticalDrift = RandomRange(edgeVerticalDriftRange);

        // 保持与摄影机相同的深度，只在摄影机画面平面内计算左右边缘终点。
        // ViewportToWorldPoint 同时兼容正交与透视摄影机；本模式仍以正交俯视镜头为主要用途。
        Vector3 targetViewportPosition = new Vector3(
            targetViewportX,
            startViewportPosition.y + verticalDrift,
            Mathf.Max(0.01f, startViewportPosition.z));
        Vector3 targetWorldPosition = activeCamera.ViewportToWorldPoint(targetViewportPosition);

        Vector3 worldTravel = targetWorldPosition - startWorldPosition;
        worldTravel = Vector3.ProjectOnPlane(worldTravel, activeCamera.transform.forward);

        if (worldTravel.sqrMagnitude < 0.0001f)
        {
            worldTravel = activeCamera.transform.right * sideSign;
        }

        travelDistance = worldTravel.magnitude;

        // 横向飞行时，屏幕上的抛物线向画面上方鼓起，比继续向左右弯更容易读成“被击飞”。
        curveDirection = activeCamera.transform.up.normalized;
        return worldTravel.normalized;
    }

    private Vector3 GetCameraPlaneTravelDirection(Camera activeCamera, out Vector3 curveDirection)
    {
        Vector2 direction = screenTravelDirection;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.up;
        }

        float sideSign = direction.x < 0f ? -1f : 1f;
        if (randomizeScreenSide)
        {
            sideSign = Random.value < 0.5f ? -1f : 1f;
            direction.x = Mathf.Abs(direction.x) * sideSign;
        }

        direction = RotateVector2(direction.normalized, Random.Range(-screenDirectionJitter, screenDirectionJitter));
        Vector3 cameraRight = activeCamera.transform.right.normalized;
        Vector3 cameraUp = activeCamera.transform.up.normalized;
        Vector3 worldDirection = cameraRight * direction.x + cameraUp * direction.y;
        curveDirection = cameraRight * sideSign;

        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            worldDirection = cameraUp;
        }

        return worldDirection.normalized;
    }

    private Vector3 GetWorldTravelDirection(Quaternion startRotation)
    {
        if (randomizeWorldFlightDirection)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
        }

        Vector3 direction = startRotation * localWorldFlightDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.back;
        }

        direction.Normalize();
        return Quaternion.AngleAxis(Random.Range(-worldDirectionSpread, worldDirectionSpread), Vector3.up) * direction;
    }

    private Vector3 GetSpinAxis(Quaternion startRotation, Camera activeCamera)
    {
        Vector3 axis;
        if (randomizeSpinAxis)
        {
            if (trajectoryMode == TrajectoryMode.OrthographicCameraArc && activeCamera != null)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                axis = activeCamera.transform.right * Mathf.Cos(angle)
                    + activeCamera.transform.up * Mathf.Sin(angle)
                    + activeCamera.transform.forward * Random.Range(-0.2f, 0.2f);
            }
            else
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                axis = new Vector3(Mathf.Cos(angle), Random.Range(-0.35f, 0.35f), Mathf.Sin(angle));
            }
        }
        else
        {
            axis = startRotation * localSpinAxis;
        }

        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.right;
        }

        return axis.normalized;
    }

    private IEnumerator PlayAnticipation(Vector3 originalScale)
    {
        if (anticipationDuration <= 0f || squashAmount <= 0f)
        {
            yield break;
        }

        Vector3 squashedScale = new Vector3(
            originalScale.x * (1f + squashAmount * 0.35f),
            originalScale.y * (1f - squashAmount),
            originalScale.z * (1f + squashAmount * 0.35f));

        float elapsed = 0f;
        while (elapsed < anticipationDuration)
        {
            elapsed += GetDeltaTime();
            float normalizedTime = Mathf.Clamp01(elapsed / anticipationDuration);
            flightRoot.localScale = Vector3.Lerp(originalScale, squashedScale, Smooth01(normalizedTime));
            yield return null;
        }

        flightRoot.localScale = squashedScale;
    }

    private IEnumerator WaitForDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    /// <summary>
    /// 可由 Animation Event 单独调用，但自动播放模式下无需设置事件。
    /// </summary>
    public void TriggerPreLaunchEffect()
    {
        if (preLaunchEffectPlayed || preLaunchEffectPrefab == null)
        {
            return;
        }

        preLaunchEffectPlayed = true;
        GroundPose pose = GetGroundPose(GetGroundAnchorPosition(), GetFacingDirection());
        SpawnGroundEffect(preLaunchEffectPrefab, pose);
    }

    /// <summary>
    /// 可由 Animation Event 单独调用，但自动播放模式下无需设置事件。
    /// </summary>
    public void TriggerTakeoffSmoke()
    {
        if (takeoffSmokePlayed)
        {
            return;
        }

        takeoffSmokePlayed = true;

        if (triggerCameraShakeOnTakeoff)
        {
            TriggerDirectTakeoffImpulse();
        }

        if (takeoffSmokePrefab == null)
        {
            return;
        }

        GroundPose pose = GetGroundPose(GetGroundAnchorPosition(), GetFacingDirection());
        SpawnGroundEffect(takeoffSmokePrefab, pose);
    }

    private void TriggerDirectTakeoffImpulse()
    {
        ResolveTakeoffImpulseSource();

        if (takeoffImpulseSource != null)
        {
            takeoffImpulseSource.GenerateImpulse(takeoffShakeForce);
            return;
        }

        if (!warnedMissingTakeoffImpulseSource)
        {
            warnedMissingTakeoffImpulseSource = true;
            Debug.LogWarning(
                "ClashMiniDeathLaunchEffect：死亡替身中找不到 CinemachineImpulseSource，无法触发起飞震屏。请把 Impulse Source 添加到死亡预制体自身或它的父/子节点。",
                this);
        }
    }

    private void ResolveTakeoffImpulseSource()
    {
        if (takeoffImpulseSource != null)
        {
            return;
        }

        takeoffImpulseSource = GetComponent<CinemachineImpulseSource>();
        if (takeoffImpulseSource == null)
        {
            takeoffImpulseSource = GetComponentInParent<CinemachineImpulseSource>();
        }
        if (takeoffImpulseSource == null)
        {
            takeoffImpulseSource = GetComponentInChildren<CinemachineImpulseSource>(true);
        }
    }

    private void PrepareLegacyComponents()
    {
        if (freezeLegacyAnimator)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].applyRootMotion = false;
                animators[i].speed = 0f;
            }
        }

        if (!disablePhysics)
        {
            return;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].velocity = Vector3.zero;
            rigidbodies[i].angularVelocity = Vector3.zero;
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }
    }

    private Vector3 GetStretchedScale(Vector3 originalScale)
    {
        return new Vector3(
            originalScale.x * (1f - stretchAmount * 0.35f),
            originalScale.y * (1f + stretchAmount),
            originalScale.z * (1f - stretchAmount * 0.35f));
    }

    private Vector3 GetGroundAnchorPosition()
    {
        if (groundEffectPoint != null)
        {
            return groundEffectPoint.position;
        }

        return flightRoot != null ? flightRoot.position : transform.position;
    }

    private Vector3 GetFacingDirection()
    {
        Transform source = flightRoot != null ? flightRoot : transform;
        return source.forward;
    }

    private GroundPose GetGroundPose(Vector3 samplePosition, Vector3 facingDirection)
    {
        Vector3 groundPosition = samplePosition;
        Vector3 groundNormal = Vector3.up;
        Vector3 rayOrigin = samplePosition + Vector3.up * groundRayStartHeight;
        float rayLength = groundRayStartHeight + groundRayDistance;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayers, QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;
            groundNormal = hit.normal;
        }

        groundPosition += groundNormal * effectHeightOffset;
        Vector3 flatForward = Vector3.ProjectOnPlane(facingDirection, groundNormal);
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.ProjectOnPlane(Vector3.forward, groundNormal);
        }
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.right;
        }

        return new GroundPose(
            groundPosition,
            Quaternion.LookRotation(flatForward.normalized, groundNormal));
    }

    private void SpawnGroundEffect(GameObject prefab, GroundPose pose)
    {
        GameObject effectInstance = Instantiate(prefab, pose.position, pose.rotation);
        if (effectLifetime > 0f)
        {
            Destroy(effectInstance, effectLifetime);
        }
    }

    private void SpawnFlightTrailSmoke()
    {
        if (flightTrailSmokePrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = flightTrailPoint != null
            ? flightTrailPoint.position
            : flightRoot.position;

        GameObject smoke = Instantiate(
            flightTrailSmokePrefab,
            spawnPosition,
            flightTrailSmokePrefab.transform.rotation);

        float randomScale = RandomRange(trailScaleRange);
        smoke.transform.localScale *= randomScale;

        if (trailEffectLifetime > 0f)
        {
            Destroy(smoke, trailEffectLifetime);
        }
    }


    private void CacheOriginalMaterials()
    {
        if (flashRenderers == null)
            return;

        foreach (Renderer renderer in flashRenderers)
        {
            if (renderer == null)
                continue;

            originalMaterials[renderer] = renderer.sharedMaterials;
        }
    }


    private void PlayInitialFlash()
    {
        if (initialFlashMode == DeathFlashMode.None ||
            initialFlashDuration <= 0f)
            return;

        StartCoroutine(InitialFlashRoutine());
    }


    private IEnumerator InitialFlashRoutine()
    {
        if (initialFlashMode == DeathFlashMode.EmissionFlash)
        {
            SetEmissionFlash();
        }
        else if (initialFlashMode == DeathFlashMode.WhiteMaterialSwap)
        {
            SetWhiteMaterial();
        }

        yield return WaitForDuration(initialFlashDuration);

        RestoreMaterials();
    }


    private void SetEmissionFlash()
    {
        if (flashRenderers == null)
            return;

        foreach (Renderer renderer in flashRenderers)
        {
            if (renderer == null)
                continue;

            MaterialPropertyBlock block = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block);

            block.SetColor(
                emissionPropertyName,
                flashColor * emissionIntensity
            );

            renderer.SetPropertyBlock(block);
        }
    }


    private void SetWhiteMaterial()
    {
        if (whiteMaterial == null || flashRenderers == null)
            return;

        foreach (Renderer renderer in flashRenderers)
        {
            if (renderer == null)
                continue;

            Material[] materials =
                new Material[renderer.sharedMaterials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = whiteMaterial;
            }

            renderer.materials = materials;
        }
    }


    private void RestoreMaterials()
    {
        foreach (var item in originalMaterials)
        {
            if (item.Key != null)
            {
                item.Key.materials = item.Value;
            }
        }

        // 清除 Emission PropertyBlock
        if (flashRenderers != null)
        {
            foreach (Renderer renderer in flashRenderers)
            {
                if (renderer == null)
                    continue;

                renderer.SetPropertyBlock(null);
            }
        }
    }


    private void HideModelRenderers()
    {
        Renderer[] renderers = flightRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer ||
                renderers[i] is TrailRenderer ||
                renderers[i] is LineRenderer)
            {
                continue;
            }

            renderers[i].enabled = false;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private static Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);
        return new Vector2(
            vector.x * cosine - vector.y * sine,
            vector.x * sine + vector.y * cosine);
    }

    private static float Smooth01(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static float RandomRange(Vector2 range)
    {
        float minimum = Mathf.Min(range.x, range.y);
        float maximum = Mathf.Max(range.x, range.y);
        return Random.Range(minimum, maximum);
    }

    private void OnValidate()
    {
        stillDuration = Mathf.Max(0f, stillDuration);
        anticipationDuration = Mathf.Max(0f, anticipationDuration);
        flightDuration = Mathf.Max(0.01f, flightDuration);

        horizontalEdgeExitPadding = Mathf.Clamp(horizontalEdgeExitPadding, 0f, 0.5f);
        edgeVerticalDriftRange.x = Mathf.Clamp(edgeVerticalDriftRange.x, -1f, 1f);
        edgeVerticalDriftRange.y = Mathf.Clamp(edgeVerticalDriftRange.y, -1f, 1f);
        takeoffShakeForce = Mathf.Clamp(takeoffShakeForce, 0f, 3f);

        screenFlightDistanceRange.x = Mathf.Max(0f, screenFlightDistanceRange.x);
        screenFlightDistanceRange.y = Mathf.Max(0f, screenFlightDistanceRange.y);
        visibleArcHeightRange.x = Mathf.Max(0f, visibleArcHeightRange.x);
        visibleArcHeightRange.y = Mathf.Max(0f, visibleArcHeightRange.y);
        physicalLiftHeightRange.x = Mathf.Max(0f, physicalLiftHeightRange.x);
        physicalLiftHeightRange.y = Mathf.Max(0f, physicalLiftHeightRange.y);
        endScaleMultiplier = Mathf.Clamp(endScaleMultiplier, 0.1f, 1f);
        worldFlightDistanceRange.x = Mathf.Max(0f, worldFlightDistanceRange.x);
        worldFlightDistanceRange.y = Mathf.Max(0f, worldFlightDistanceRange.y);
        worldArcHeightRange.x = Mathf.Max(0f, worldArcHeightRange.x);
        worldArcHeightRange.y = Mathf.Max(0f, worldArcHeightRange.y);
        spinSpeedRange.x = Mathf.Max(0f, spinSpeedRange.x);
        spinSpeedRange.y = Mathf.Max(0f, spinSpeedRange.y);
        trailSpawnInterval = Mathf.Max(0.01f, trailSpawnInterval);
        trailScaleRange.x = Mathf.Max(0f, trailScaleRange.x);
        trailScaleRange.y = Mathf.Max(0f, trailScaleRange.y);
        trailEffectLifetime = Mathf.Max(0f, trailEffectLifetime);

        groundRayStartHeight = Mathf.Max(0f, groundRayStartHeight);
        groundRayDistance = Mathf.Max(0.01f, groundRayDistance);
        effectLifetime = Mathf.Max(0f, effectLifetime);
    }
}
