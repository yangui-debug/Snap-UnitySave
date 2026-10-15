using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using System.Collections;
using System.Collections.Generic; // 引入泛型集合（用于字典缓存材质）

[ExecuteAlways]
public class SoldierHealth : MonoBehaviour
{
    [Header("肉鸽属性设置")]
    [Tooltip("需要攻击几次才会死（或总血量）")]
    public int maxHits = 3;
    private int currentHits;
    public bool isDead { get; private set; } = false;

    // ==========================================
    [Header("UI 基础设置")]
    [Tooltip("绑定血条的父节点：血条框（用于控制整体显隐）")]
    public GameObject healthBarFrame;
    [Tooltip("绑定到对应士兵头顶的血条UI（Image组件，需设置为Filled模式）")]
    public Image healthBarFill;

    [Header("血条显示模式")]
    [Tooltip("勾选：一直显示血条。不勾选：受击时才显示血条")]
    public bool alwaysShowHealthBar = false;
    [Tooltip("【仅受击显示模式有效】受击后血条持续显示的时间（秒）")]
    public float showDuration = 3.0f;
    private Coroutine hideBarCoroutine;

    [Header("血条颜色变色设置")]
    public Color highHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;

    // ==========================================
    [Header("受击位移设置 (击退效果)")]
    [Tooltip("死亡时要永久销毁的当前士兵根节点。SoldierHealth 挂在预制体最外层时推荐留空；若脚本在子节点，才拖入这一名士兵自己的最外层根节点。绝对不要拖入包含多名士兵的公共编队父节点。")]
    public Transform 独立士兵根节点;

    [Tooltip("击退的距离有多远。选中物体时，场景里会有一条橙色的线显示这个距离！")]
    public float 击退距离 = 2.0f;
    [Tooltip("击退过程花费的时间。越短感觉越重！推荐 0.1 到 0.2")]
    public float 击退时长 = 0.15f;

    [Tooltip("控制受击时在 XYZ 三个轴向上的击退比例。例如 (1, 0, 1) 表示只在 X 和 Z 轴移动，不会在 Y 轴（上下）移动。")]
    public Vector3 击退轴向控制 = new Vector3(1, 1, 1);

    [Tooltip("可选：真正执行击退的节点。使用 Timeline K 位移时，务必填写一个没有被 Timeline K 位移的子节点（推荐命名 CombatOffset）；留空则沿用独立士兵根节点。")]
    public Transform 击退位移节点;

    [Header("受击动画与僵直 (停顿感)")]
    [Tooltip("挨打后在原地停顿多久才继续走 (建议比击退时长略长一点，比如 0.4)")]
    public float 僵直停顿时间 = 0.4f;
    [Tooltip("挂载包含受击动画的 Animator，如果没有就不填")]
    public Animator 动画控制器;
    [Tooltip("直接填入Animator中受击状态的名字（比如 Hit），不需要配Trigger参数！")]
    public string 受击动画状态名 = "Hit";

    public enum TimelineHitPauseMode
    {
        不处理Timeline,
        僵直期间暂停指定Timeline
    }

    [Header("Timeline 受击停顿")]
    [Tooltip("使用 Timeline K 角色位移时，推荐选择“僵直期间暂停指定Timeline”。这会让该 Director 控制的整条 Timeline 一起暂停。")]
    public TimelineHitPauseMode Timeline停顿模式 = TimelineHitPauseMode.僵直期间暂停指定Timeline;

    [Tooltip("拖入当前战斗所用的 Playable Director。若同一条 Timeline 还控制相机、特效和其他角色，它们也会在僵直期间一起暂停。")]
    public PlayableDirector 受击时暂停的Timeline;

    [Tooltip("未手动指定 Director 时，自动从当前士兵的父节点中查找。找不到时不会擅自暂停场景中的其他 Timeline。")]
    public bool 自动查找父节点Timeline = true;

    public enum HitScalePulseCount
    {
        一次 = 1,
        三次 = 3
    }

    [Header("受击缩放反馈（可选）")]
    [Tooltip("勾选后，受击时会先压扁、再回弹。")]
    public bool 启用受击缩放 = false;

    [Tooltip("只拖入模型的视觉外层节点，推荐使用未被 Timeline 或动画 K 缩放的 ModelVisual/HitScale 节点。不要拖入带碰撞体、寻路或公共编队的根节点。")]
    public Transform 受击缩放节点;

    [Tooltip("一次：干净有力；三次：连续抖动感更强。")]
    public HitScalePulseCount 缩放次数 = HitScalePulseCount.一次;

    [Tooltip("每一次“压缩→回弹→恢复”的时间。参考视频建议 0.08～0.12 秒。")]
    [Min(0.02f)]
    public float 单次缩放时长 = 0.10f;

    [Tooltip("第一段压缩倍率。默认按 90°俯视、角色本地 Z 为前方设置：前后压短、左右撑开；若模型朝向不同，可交换 X/Z。")]
    public Vector3 压缩倍率 = new Vector3(1.12f, 1.00f, 0.82f);

    [Tooltip("压缩后的轻微反向回弹倍率。数值越接近 1，效果越利落、不果冻。")]
    public Vector3 回弹倍率 = new Vector3(0.97f, 1.00f, 1.07f);

    // ================= 核心修改：受击反馈模式选择 =================
    public enum HitVisualMode
    {
        EmissionFlash,       // 自发光模式（原本的效果）：受击时按“闪烁时长”做一次淡出闪白
        WhiteMaterialSwap,   // 替换纯白材质模式：受击时临时替换成纯白 Unlit 材质
        AcceleratingRedFlash // 加速闪红模式：首次受击后独立播放一段“频率越来越快”的自发光闪烁，
                             // 后续受击不会打断它，也不会重新开始。
    }

    [Header("受击闪烁材质设置")]
    [Tooltip("选择受击反馈的视觉效果。\n" +
             "· EmissionFlash：常规自发光闪白，一次淡出，最通用。\n" +
             "· WhiteMaterialSwap：整体替换为纯白 Unlit 材质，风格最硬。\n" +
             "· AcceleratingRedFlash：首次受击后播放一段加速闪红（专属参数见下方 [模式C]），\n" +
             "  后续受击不会打断或重开，直到本次播放完毕。")]
    public HitVisualMode 闪烁模式 = HitVisualMode.EmissionFlash;

    [Tooltip("闪光效果持续的时间，推荐 0.3 到 0.5 秒。\n" +
             "注意：选择“加速闪红模式”后，这项只对前两种模式生效；\n" +
             "加速闪红的时长请调下方 [模式C] 里的“加速闪烁总时长”。")]
    public float 闪烁时长 = 0.4f;

    [Tooltip("当前这一名士兵需要闪白、并在死亡时消失的全部 Renderer。务必把身体、武器等都拖入；不要拖入其他士兵的 Renderer。")]
    public Renderer[] modelRenderers;

    [Header("--- [模式A] 自发光设置 ---")]
    [Tooltip("自发光属性名。URP/Standard Shader 默认就是 \"_EmissionColor\"；\n" +
             "如果用的是其他自定义 Shader，请改成对应的自发光属性名。\n" +
             "注意：这个字段对 [模式C] 加速闪红同样生效。")]
    public string emissionPropertyName = "_EmissionColor";

    [ColorUsage(showAlpha: false, hdr: true)]
    [Tooltip("闪烁的颜色，如果想要闪白，请在这里选成纯白色。\n" +
             "仅在 [模式A] 自发光模式下生效。")]
    public Color flashColor = Color.white;

    [Tooltip("自发光强度倍率。实际写入的自发光颜色 = flashColor × emissionIntensity × 当前闪烁强度。\n" +
             "数值越大越亮，推荐 3～8。仅在 [模式A] 自发光模式下生效。")]
    public float emissionIntensity = 5.0f;

    [Header("--- [模式B] 白模替换设置 ---")]
    [Tooltip("拖入一个 Unlit(不受光) 且为纯白色的材质。仅在 白模替换模式 下生效。")]
    public Material whiteUnlitMaterial;

    // ==============================================================
    // [模式C] 加速闪红专用参数
    // ==============================================================
    [Header("--- [模式C] 加速闪红设置（仅首次受击触发，后续受击不打断）---")]
    [Tooltip("加速闪烁时使用的自发光颜色。默认纯红色；开启 HDR 后可以选超过 1 的亮度，闪起来更刺眼。\n" +
             "注意：这里改的是自发光（Emission），所以材质必须开启 Emission 关键词，\n" +
             "否则看不到效果，这一点和 [模式A] 完全一致。")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color 加速闪烁颜色 = Color.red;

    [Tooltip("加速闪烁的自发光强度倍率。实际写入的自发光颜色 = 加速闪烁颜色 × 这个倍率 × 当前闪烁强度。\n" +
             "数值越大越亮，推荐 3～8。")]
    public float 加速闪烁强度 = 5.0f;

    [Tooltip("【关键：延时开始】首次受击后，等待多少秒才真正开始播放加速闪红。\n" +
             "· 0 表示受击当帧立即开始（和原来一样）。\n" +
             "· 大于 0 时：延时期间完全不施加任何闪红，角色保持正常外观；\n" +
             "  延时结束后才进入“起始满闪 → 加速闪烁”的流程。\n" +
             "· 注意：整段闪烁的计时（加速闪烁总时长）从延时结束后才开始算，\n" +
             "  也就是说“总表现时间 ≈ 加速闪烁开始延时 + 加速闪烁总时长”。\n" +
             "· 若士兵在延时期间死亡或被禁用，协程会被安全打断，不会残留红光。")]
    [Min(0f)]
    public float 加速闪烁开始延时 = 0f;

    [Tooltip("整段加速闪烁从开始到结束的总时长（秒）。时间一到就自动熄灭并恢复正常材质。\n" +
             "注意这是“整段闪烁”的长度，不是单次亮灭的时间；单次快慢由下面的频率曲线控制。\n" +
             "整段闪烁的计时从“加速闪烁开始延时”结束之后才开始算。")]
    [Min(0.05f)]
    public float 加速闪烁总时长 = 1.2f;

    [Tooltip("【闪烁频率变化曲线】X 轴 = 闪烁进度（0 = 刚开始，1 = 即将结束）；Y 轴 = 该时刻每秒闪烁多少次。\n" +
             "· 想做出“越闪越快”，只要把曲线右端抬高即可（默认就是从 3 次/秒 涨到 15 次/秒）。\n" +
             "· 想全程匀速，把两端 Y 值设成一样即可。\n" +
             "· Y = 0 表示该时刻不闪（可以做出“先停一下再猛闪”的节奏）。")]
    public AnimationCurve 加速闪烁频率曲线 = new AnimationCurve(
        new Keyframe(0f, 3f),
        new Keyframe(1f, 15f));

    [Tooltip("【闪烁亮占比曲线】X 轴 = 闪烁进度（0 = 刚开始，1 = 即将结束）；Y 轴 = 单个闪烁周期里“亮”占的比例（会被限制在 0.05～0.95）。\n" +
             "· 0.5 表示亮、暗各占一半，最常规。\n" +
             "· 数值越小，亮的时间越短、间隔越长，看起来越像尖锐的爆闪。\n" +
             "· 想前后变化，也可以把它做成一条从 0.6 掉到 0.25 的曲线。")]
    public AnimationCurve 加速闪烁亮占比曲线 = AnimationCurve.Constant(0f, 1f, 0.5f);

    // 缓存最开始的材质，防止材质丢失
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    // ==============================================================

    [Header("死亡替身设置")]
    [Tooltip("死亡后生成的新角色预制体。该预制体或其子节点必须挂载 ClashMiniDeathLaunchEffect 或 SoldierDeathTossEffect。它会在原士兵的世界位置生成并负责后续击飞。")]
    public GameObject deathEffectPrefab;

    [Tooltip("死亡替身生成后多久销毁整个替身实例。推荐 1.5 ~ 2.5 秒，并确保大于静止、预备和飞行时间的总和；默认 2 秒。小于等于 0 表示不自动销毁。")]
    public float deathEffectDuration = 2.0f;

    [Tooltip("是否让死亡替身采用原士兵的世界缩放。相同角色模型推荐勾选；死亡预制体已经单独调好尺寸时可取消。")]
    public bool autoScaleEffect = true;

    [Tooltip("是否把原士兵的世界旋转复制给死亡替身。你的死亡预制体内部已经设置了 Y=180°，因此推荐不勾选，保留死亡预制体自身朝向。")]
    public bool copyOriginalRotationToDeathEffect = false;

    [Tooltip("死亡替身相对原士兵世界位置的偏移。你要求完全原位替换，推荐保持 (0, 0, 0)。注意 90°俯视正交镜头下，世界 Y 主要是离地高度，不是画面向上。")]
    public Vector3 deathEffectPositionOffset = Vector3.zero;

    [Header("编辑器颜色预览 (不运行也能调)")]
    [Range(0f, 1f)]
    [Tooltip("滑动此滑块可直接在场景中预览受击闪烁效果。\n" +
             "· 当前是 [模式A] 自发光时：预览闪白；\n" +
             "· 当前是 [模式C] 加速闪红时：预览的是加速闪烁颜色与强度的效果。")]
    public float previewHitEffect = 0f;

    private MaterialPropertyBlock propBlock;
    private float lastPreviewValue = -1f;
    private bool hasWarnedUnsafeSoldierRoot = false;
    private bool hasWarnedUnsafeKnockbackTarget = false;

    private Coroutine hitReactionCoroutine;
    private Transform activeScaleTarget;
    private Vector3 scaleTargetOriginalLocalScale = Vector3.one;
    private Vector3 currentScaleMultiplier = Vector3.one;
    private bool scaleReactionActive = false;

    // ---- [模式C] 加速闪红运行时状态 ----
    // 说明：acceleratingFlashStarted 一旦为 true，后续任何一次受击都不会再重新触发加速闪红，
    // 也不会改变它当前已经跑到的节奏；直到这个士兵被禁用/启用（OnDisable → OnEnable）才会复位。
    private bool acceleratingFlashStarted = false;
    private Coroutine acceleratingFlashCoroutine;

    // 多名士兵可能共用同一个 Director。引用计数可防止后受击的士兵还没结束，
    // 前一个士兵就提前把 Timeline 恢复播放。
    private static readonly Dictionary<PlayableDirector, int> timelinePauseRefCounts =
        new Dictionary<PlayableDirector, int>();
    private static readonly HashSet<PlayableDirector> timelinesPausedBySoldierHealth =
        new HashSet<PlayableDirector>();
    private PlayableDirector heldTimelineDirector;
    private bool holdsTimelinePause = false;

    void OnEnable()
    {
        currentHits = maxHits;
        isDead = false;
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        // 每次重新启用，都允许加速闪红重新由“第一次受击”触发。
        acceleratingFlashStarted = false;
        acceleratingFlashCoroutine = null;

        CacheOriginalMaterials(); // 缓存原始材质
        UpdateHealthBar();

        if (Application.isPlaying && healthBarFrame != null)
        {
            healthBarFrame.SetActive(alwaysShowHealthBar);
        }
    }

    void OnDisable()
    {
        ReleaseTimelinePause();
        RestoreScaleImmediately();

        // 加速闪红协程如果在跑，禁用时必须停掉，否则对象销毁后协程可能还持有旧引用。
        if (acceleratingFlashCoroutine != null)
        {
            StopCoroutine(acceleratingFlashCoroutine);
            acceleratingFlashCoroutine = null;
        }

        // 保护机制：无论是运行结束还是在编辑器里，确保禁用时恢复正常材质，防止白模存进场景
        RestoreOriginalMaterials();
        ClearEmissionPropBlock();
    }

    void LateUpdate()
    {
        // 放在 LateUpdate 里应用，尽量晚于 Animator 与 Timeline 的常规更新。
        // 缩放节点仍建议使用未被动画轨道 K 缩放的独立视觉子节点。
        if (Application.isPlaying && scaleReactionActive && activeScaleTarget != null)
        {
            activeScaleTarget.localScale = Vector3.Scale(
                scaleTargetOriginalLocalScale,
                currentScaleMultiplier);
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            // 编辑器下实时预览：拖动 previewHitEffect 滑块，就会立即看到对应模式的效果。
            if (Mathf.Abs(previewHitEffect - lastPreviewValue) > 0.001f)
            {
                ApplyHitVisuals(previewHitEffect);
                lastPreviewValue = previewHitEffect;
            }
        }
    }

    public void TakeDamage(int damage, Vector3 hitDirection, bool isCritical = false)
    {
        if (isDead) return;

        currentHits -= damage;
        UpdateHealthBar();

        bool isLethalHit = currentHits <= 0;

        if (HitFeelManager.Instance != null && Application.isPlaying)
        {
            HitFeelManager.Instance.ShowDamageText(transform.position, damage, isCritical);

            if (isLethalHit)
            {
                // 致命命中先只做短暂停顿；震屏由死亡替身真正离地的那一帧触发，避免重复震动。
                HitFeelManager.Instance.PlayHitStopOnly();
            }
            else
            {
                HitFeelManager.Instance.PlayHitFeedback();
            }
        }

        if (isLethalHit)
        {
            Die();
        }
        else if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            if (!alwaysShowHealthBar)
            {
                ShowHealthBar();
            }

            // 连续受击时重新开始反馈，避免多条协程同时改位置、材质与缩放。
            if (hitReactionCoroutine != null)
            {
                StopCoroutine(hitReactionCoroutine);
                hitReactionCoroutine = null;
                ReleaseTimelinePause();

                // 加速闪红模式下，受击视觉由加速闪红协程独占，
                // 这里绝不能把它清零，否则后面的受击会把正在闪的红光抹掉。
                if (闪烁模式 != HitVisualMode.AcceleratingRedFlash)
                {
                    ApplyHitVisuals(0f);
                }

                RestoreScaleImmediately();
            }

            // —— [模式C] 加速闪红：只由“第一次受击”触发，之后无论挨多少下都不会重开、也不会加速 ——
            // 逻辑：acceleratingFlashStarted 这个开关一旦打开，后面所有受击都会跳过这个 if，
            // 现有协程继续按自己的曲线跑到自然结束。
            if (闪烁模式 == HitVisualMode.AcceleratingRedFlash &&
                !acceleratingFlashStarted &&
                gameObject.activeInHierarchy)
            {
                acceleratingFlashStarted = true;
                acceleratingFlashCoroutine = StartCoroutine(AcceleratingRedFlashRoutine());
            }

            hitReactionCoroutine = StartCoroutine(HitReactionRoutine(hitDirection));
        }
    }

    private void ShowHealthBar()
    {
        if (healthBarFrame != null)
        {
            healthBarFrame.SetActive(true);
            if (hideBarCoroutine != null) StopCoroutine(hideBarCoroutine);
            hideBarCoroutine = StartCoroutine(HideHealthBarAfterDelay());
        }
    }

    private IEnumerator HideHealthBarAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);
        if (healthBarFrame != null) healthBarFrame.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthRatio = 0f;
            if (maxHits > 0) healthRatio = Mathf.Clamp01((float)currentHits / maxHits);

            healthBarFill.fillAmount = healthRatio;

            if (healthRatio <= lowHealthThreshold) healthBarFill.color = lowHealthColor;
            else healthBarFill.color = highHealthColor;
        }
    }

    private IEnumerator HitReactionRoutine(Vector3 hitDir)
    {
        float timer = 0f;
        float safeKnockbackDuration = Mathf.Max(0.01f, 击退时长);
        float knockbackSpeed = 击退距离 / safeKnockbackDuration;
        Transform targetToMove = GetKnockbackTarget();

        if (动画控制器 != null && !string.IsNullOrEmpty(受击动画状态名))
        {
            动画控制器.CrossFade(受击动画状态名, 0.1f, 0, 0f);
        }
        // ApplyStun 仍发送给单兵逻辑根节点；击退位移节点可以只是一个纯视觉/偏移子节点。
        GetSafeSoldierRoot().SendMessage("ApplyStun", 僵直停顿时间, SendMessageOptions.DontRequireReceiver);

        AcquireTimelinePause();
        BeginScaleReaction();

        float scaleTotalDuration = 启用受击缩放
            ? Mathf.Max(0.02f, 单次缩放时长) * (int)缩放次数
            : 0f;
        float timelinePauseDuration = holdsTimelinePause
            ? Mathf.Max(0f, 僵直停顿时间)
            : 0f;

        // 加速闪红模式下，受击视觉不再由“闪烁时长”决定，所以这里把它当 0 处理，
        // 免得受击协程被一个和视觉无关的数值拖长。
        float effectiveFlashDuration =
            (闪烁模式 == HitVisualMode.AcceleratingRedFlash) ? 0f : 闪烁时长;

        float totalDuration = Mathf.Max(
            Mathf.Max(击退时长, effectiveFlashDuration),
            Mathf.Max(scaleTotalDuration, timelinePauseDuration));

        // 计算受轴向控制的击退方向
        Vector3 controlledDir = new Vector3(
            hitDir.normalized.x * 击退轴向控制.x,
            hitDir.normalized.y * 击退轴向控制.y,
            hitDir.normalized.z * 击退轴向控制.z
        );

        while (timer < totalDuration)
        {
            float deltaTime = Time.deltaTime;
            timer += deltaTime;

            if (timer < safeKnockbackDuration)
            {
                // 只有在方向向量不为0时才移动，避免全0导致规范化报错
                if (controlledDir.sqrMagnitude > 0.001f)
                {
                    targetToMove.position += controlledDir.normalized * (knockbackSpeed * deltaTime);
                }
            }

            // —— 受击闪烁：加速闪红模式下由 AcceleratingRedFlashRoutine 全权负责，
            //    这里直接跳过，避免两套逻辑抢同一个 MaterialPropertyBlock。 ——
            if (闪烁模式 != HitVisualMode.AcceleratingRedFlash)
            {
                float flashProgress = Mathf.Clamp01(timer / Mathf.Max(0.01f, 闪烁时长));
                float effectStrength = 1f - flashProgress;
                ApplyHitVisuals(effectStrength);
            }

            if (启用受击缩放)
            {
                UpdateScaleReaction(timer, scaleTotalDuration);
            }

            if (holdsTimelinePause && timer >= timelinePauseDuration)
            {
                ReleaseTimelinePause();
            }

            yield return null;
        }

        // 收尾同样要避开加速闪红模式，否则会把还在闪的红光提前掐灭。
        if (闪烁模式 != HitVisualMode.AcceleratingRedFlash)
        {
            ApplyHitVisuals(0f);
        }

        RestoreScaleImmediately();
        ReleaseTimelinePause();
        hitReactionCoroutine = null;
    }

    // ================== [模式C] 加速闪红协程 ==================
    /// <summary>
    /// 加速闪红的完整播放流程：
    /// 0. 若配置了“加速闪烁开始延时”，先原地等一段时间，期间不施加任何闪光；
    /// 1. 延时结束后，先来一发满强度的红光，保证“开始闪红”这件事有明确的视觉信号；
    /// 2. 之后按“加速闪烁频率曲线”不断加快闪烁节奏，单次亮占比由“加速闪烁亮占比曲线”决定；
    /// 3. 跑满“加速闪烁总时长”后自动熄灭并恢复原始材质。
    ///
    /// 该协程一旦启动，后续受击不会重启它，也不会修改它的节奏。
    /// </summary>
    private IEnumerator AcceleratingRedFlashRoutine()
    {
        // —— 阶段 0：延时等待 ——
        // 这里不写入任何自发光，让角色在延时期间保持正常外观。
        // 使用 WaitForSeconds 而不是逐帧累加，可避免 Time.timeScale = 0（例如受击顿帧）
        // 时延时被无限拉长；如需受 timeScale 影响可改成逐帧累加。
        if (加速闪烁开始延时 > 0f)
        {
            yield return new WaitForSeconds(加速闪烁开始延时);
        }

        float totalDuration = Mathf.Max(0.01f, 加速闪烁总时长);
        float elapsed = 0f;

        // 相位（phase）表示“已经闪了多少个周期”。整数部分无所谓，小数部分决定当前处在周期内的位置。
        // 初始相位偏移到“亮”区间的正中间，保证正式开始的瞬间就是最亮，而不是从 0 慢慢淡入。
        float initialDuty = Mathf.Clamp(加速闪烁亮占比曲线.Evaluate(0f), 0.05f, 0.95f);
        float phase = initialDuty * 0.5f;

        // 延时结束后，先来一发满强度闪光，让“开始闪烁”这件事有明确的视觉信号。
        ApplyHitVisuals(1f);

        while (elapsed < totalDuration)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;

            float progress = Mathf.Clamp01(elapsed / totalDuration);

            // 频率 = 每秒闪几次。对频率做时间积分就得到相位，这样即使频率在变化，
            // 闪烁节奏也是连续平滑的，不会出现“跳帧”或“突然停顿”。
            float frequency = Mathf.Max(0f, 加速闪烁频率曲线.Evaluate(progress));
            phase += frequency * deltaTime;

            // 单个周期内“亮”占的比例，数值越小越像尖锐爆闪。
            float duty = Mathf.Clamp(加速闪烁亮占比曲线.Evaluate(progress), 0.05f, 0.95f);
            float cyclePos = Mathf.Repeat(phase, 1f);

            float strength = 0f;
            if (cyclePos < duty)
            {
                // 用半个正弦波做淡入淡出，比硬切更顺眼，也不会糊成一团常亮。
                float t = cyclePos / duty;
                strength = Mathf.Sin(t * Mathf.PI);
            }

            ApplyHitVisuals(strength);
            yield return null;
        }

        // 收尾：熄灭并恢复原材质。
        ApplyHitVisuals(0f);
        acceleratingFlashCoroutine = null;
    }

    private void BeginScaleReaction()
    {
        if (!启用受击缩放)
        {
            return;
        }

        activeScaleTarget = GetScaleTarget();
        if (activeScaleTarget == null)
        {
            return;
        }

        scaleTargetOriginalLocalScale = activeScaleTarget.localScale;
        currentScaleMultiplier = Vector3.one;
        scaleReactionActive = true;
    }

    private void UpdateScaleReaction(float elapsedTime, float totalDuration)
    {
        if (!scaleReactionActive || totalDuration <= 0f)
        {
            return;
        }

        if (elapsedTime >= totalDuration)
        {
            currentScaleMultiplier = Vector3.one;
            return;
        }

        float pulseDuration = Mathf.Max(0.02f, 单次缩放时长);
        float pulseProgress = Mathf.Repeat(elapsedTime, pulseDuration) / pulseDuration;

        // 0%～28%：快速压缩；28%～62%：反向回弹；62%～100%：收回原尺寸。
        if (pulseProgress < 0.28f)
        {
            float t = Smooth01(pulseProgress / 0.28f);
            currentScaleMultiplier = Vector3.LerpUnclamped(Vector3.one, 压缩倍率, t);
        }
        else if (pulseProgress < 0.62f)
        {
            float t = Smooth01((pulseProgress - 0.28f) / 0.34f);
            currentScaleMultiplier = Vector3.LerpUnclamped(压缩倍率, 回弹倍率, t);
        }
        else
        {
            float t = Smooth01((pulseProgress - 0.62f) / 0.38f);
            currentScaleMultiplier = Vector3.LerpUnclamped(回弹倍率, Vector3.one, t);
        }
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private Transform GetScaleTarget()
    {
        if (受击缩放节点 != null)
        {
            return 受击缩放节点;
        }

        // 没有手动填写时优先使用 Animator 节点，通常它比 Timeline 位移根节点更接近模型视觉层。
        if (动画控制器 != null)
        {
            return 动画控制器.transform;
        }

        return transform;
    }

    private void RestoreScaleImmediately()
    {
        if (scaleReactionActive && activeScaleTarget != null)
        {
            activeScaleTarget.localScale = scaleTargetOriginalLocalScale;
        }

        currentScaleMultiplier = Vector3.one;
        scaleReactionActive = false;
        activeScaleTarget = null;
    }

    private void AcquireTimelinePause()
    {
        if (Timeline停顿模式 != TimelineHitPauseMode.僵直期间暂停指定Timeline ||
            僵直停顿时间 <= 0f)
        {
            return;
        }

        PlayableDirector director = 受击时暂停的Timeline;
        if (director == null && 自动查找父节点Timeline)
        {
            director = GetComponentInParent<PlayableDirector>();
        }

        if (director == null)
        {
            Debug.LogWarning(
                "SoldierHealth：已启用 Timeline 受击停顿，但没有找到 Playable Director。请把战斗所用的 Director 拖入“受击时暂停的Timeline”。",
                this);
            return;
        }

        int currentCount;
        if (timelinePauseRefCounts.TryGetValue(director, out currentCount))
        {
            timelinePauseRefCounts[director] = currentCount + 1;
            heldTimelineDirector = director;
            holdsTimelinePause = true;
            return;
        }

        // 原本就被用户或其他系统暂停的 Timeline，不由本脚本恢复。
        if (director.state != PlayState.Playing)
        {
            return;
        }

        timelinePauseRefCounts[director] = 1;
        timelinesPausedBySoldierHealth.Add(director);
        heldTimelineDirector = director;
        holdsTimelinePause = true;
        director.Pause();
    }

    private void ReleaseTimelinePause()
    {
        if (!holdsTimelinePause)
        {
            return;
        }

        PlayableDirector director = heldTimelineDirector;
        holdsTimelinePause = false;
        heldTimelineDirector = null;

        int currentCount;
        if (!timelinePauseRefCounts.TryGetValue(director, out currentCount))
        {
            return;
        }

        if (currentCount > 1)
        {
            timelinePauseRefCounts[director] = currentCount - 1;
            return;
        }

        timelinePauseRefCounts.Remove(director);
        bool shouldResume = timelinesPausedBySoldierHealth.Remove(director);

        if (director != null && shouldResume && director.state == PlayState.Paused)
        {
            director.Resume();
        }
    }

    // ================== 核心视觉表现切分逻辑 ==================
    private void ApplyHitVisuals(float strength)
    {
        if (modelRenderers == null) return;

        if (闪烁模式 == HitVisualMode.EmissionFlash)
        {
            RestoreOriginalMaterials(); // 确保从白模模式切回来时恢复正常
            ApplyEmissionToRenderers(flashColor, emissionIntensity, strength);
        }
        else if (闪烁模式 == HitVisualMode.AcceleratingRedFlash)
        {
            // 和 [模式A] 走完全相同的自发光写入方式，只是颜色/强度换成加速闪红专属参数。
            RestoreOriginalMaterials();
            ApplyEmissionToRenderers(加速闪烁颜色, 加速闪烁强度, strength);
        }
        else if (闪烁模式 == HitVisualMode.WhiteMaterialSwap)
        {
            ClearEmissionPropBlock(); // 确保从发光模式切回来时清除发光残留

            bool shouldBeWhite = strength > 0.01f; // 只要特效还有强度，就维持纯白

            foreach (var rend in modelRenderers)
            {
                if (rend == null || !originalMaterials.ContainsKey(rend)) continue;

                if (shouldBeWhite && whiteUnlitMaterial != null)
                {
                    // 替换成纯白材质 (如果模型有多个子材质，全部替换)
                    Material[] whiteMats = new Material[rend.sharedMaterials.Length];
                    for (int i = 0; i < whiteMats.Length; i++) whiteMats[i] = whiteUnlitMaterial;
                    rend.sharedMaterials = whiteMats;
                }
                else
                {
                    // 恢复原始材质
                    if (rend.sharedMaterials != originalMaterials[rend])
                    {
                        rend.sharedMaterials = originalMaterials[rend];
                    }
                }
            }
        }
    }

    /// <summary>
    /// 自发光模式的公共写入逻辑（[模式A] 与 [模式C] 共用）。
    /// 通过 MaterialPropertyBlock 写入自发光颜色，不会实例化材质，
    /// 因此多个士兵共用同一材质也不会互相影响。
    /// </summary>
    /// <param name="baseColor">基础自发光颜色</param>
    /// <param name="intensity">自发光强度倍率</param>
    /// <param name="strength">当前闪烁强度（0 = 全黑不发光，1 = 满强度）</param>
    private void ApplyEmissionToRenderers(Color baseColor, float intensity, float strength)
    {
        if (propBlock == null || modelRenderers == null) return;

        Color targetEmission = baseColor * intensity;
        Color currentEmission = Color.Lerp(Color.black, targetEmission, strength);

        foreach (var rend in modelRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(emissionPropertyName, currentEmission);
            rend.SetPropertyBlock(propBlock);
        }
    }

    private void CacheOriginalMaterials()
    {
        originalMaterials.Clear();
        if (modelRenderers != null)
        {
            foreach (var rend in modelRenderers)
            {
                if (rend != null && !originalMaterials.ContainsKey(rend))
                {
                    // 使用 sharedMaterials 可以防止在编辑器模式下意外实例化(Leaking)
                    originalMaterials[rend] = rend.sharedMaterials;
                }
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (modelRenderers == null) return;
        foreach (var rend in modelRenderers)
        {
            if (rend != null && originalMaterials.ContainsKey(rend))
            {
                if (rend.sharedMaterials != originalMaterials[rend])
                {
                    rend.sharedMaterials = originalMaterials[rend];
                }
            }
        }
    }

    private void ClearEmissionPropBlock()
    {
        if (propBlock != null && modelRenderers != null)
        {
            foreach (var rend in modelRenderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(propBlock);
                propBlock.SetColor(emissionPropertyName, Color.black);
                rend.SetPropertyBlock(propBlock);
            }
        }
    }
    // ========================================================

    private void Die()
    {
        isDead = true;

        // 必须先保存当前真正显示/击退节点的世界信息。Timeline Anchor 与 CombatOffset
        // 分层时，死亡替身应从已经后撤后的画面位置出现，而不是从 Timeline 锚点出现。
        Transform originalSoldierRoot = GetSafeSoldierRoot();
        Transform visibleSoldierRoot = GetKnockbackTarget();
        Vector3 originalWorldPosition = visibleSoldierRoot.position;
        Quaternion originalWorldRotation = visibleSoldierRoot.rotation;
        Vector3 originalWorldScale = visibleSoldierRoot.lossyScale;

        if (healthBarFrame != null)
        {
            healthBarFrame.SetActive(false);
        }

        SpawnDeathReplacement(
            originalWorldPosition,
            originalWorldRotation,
            originalWorldScale);

        // 立即停用可保证当前帧不再显示或参与碰撞；Destroy 会在本帧结束时永久删除。
        // 这里只处理通过安全检查的单兵根节点，绝不销毁公共编队父节点。
        originalSoldierRoot.gameObject.SetActive(false);
        Destroy(originalSoldierRoot.gameObject);
    }

    private void SpawnDeathReplacement(
        Vector3 originalWorldPosition,
        Quaternion originalWorldRotation,
        Vector3 originalWorldScale)
    {
        if (deathEffectPrefab == null)
        {
            Debug.LogWarning("SoldierHealth：没有设置 Death Effect Prefab；原士兵仍会被销毁，但不会生成死亡击飞替身。", this);
            return;
        }

        Vector3 spawnPosition = originalWorldPosition + deathEffectPositionOffset;
        Quaternion spawnRotation = copyOriginalRotationToDeathEffect
            ? originalWorldRotation
            : deathEffectPrefab.transform.rotation;

        GameObject deathReplacement = Instantiate(
            deathEffectPrefab,
            spawnPosition,
            spawnRotation);

        if (autoScaleEffect)
        {
            // 新实例在场景根层级，因此 localScale 就是它的世界缩放。
            deathReplacement.transform.localScale = originalWorldScale;
        }

        SoldierDeathTossEffect tossEffect =
            deathReplacement.GetComponentInChildren<SoldierDeathTossEffect>(true);
        if (tossEffect != null)
        {
            DisableModelRenderersOutsideFlightRoot(deathReplacement, tossEffect.flightRoot);
            tossEffect.PlayDeathToss();
            // 新效果负责落地后的停留与销毁，不使用旧效果的 2 秒寿命。
            return;
        }

        ClashMiniDeathLaunchEffect launchEffect =
            deathReplacement.GetComponentInChildren<ClashMiniDeathLaunchEffect>(true);

        if (launchEffect != null)
        {
            // 死亡预制体外层若误留了另一套角色模型，它不会跟随内层 Flight Root 飞行，
            // 会原地残留并被误认为原本体。这里只关闭 Flight Root 外的普通模型 Renderer，
            // ParticleSystem、Trail 和 Line Renderer 会保留，避免误伤烟雾与拖尾。
            DisableModelRenderersOutsideFlightRoot(deathReplacement, launchEffect.flightRoot);

            // 由 SoldierHealth 当场启动，确保替身已经采用原本体的位置后才记录击飞起点。
            // 若 Play On Start 同时勾选，脚本内部会阻止重复播放。
            launchEffect.PlayDeathLaunch();
        }
        else
        {
            Debug.LogWarning(
                "SoldierHealth：生成的死亡预制体及其子节点中找不到 ClashMiniDeathLaunchEffect，替身不会执行击飞。",
                deathReplacement);
        }

        if (deathEffectDuration > 0f)
        {
            Destroy(deathReplacement, deathEffectDuration);
        }
    }

    private static void DisableModelRenderersOutsideFlightRoot(
        GameObject deathReplacement,
        Transform flightRoot)
    {
        if (deathReplacement == null || flightRoot == null)
        {
            return;
        }

        Renderer[] replacementRenderers =
            deathReplacement.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in replacementRenderers)
        {
            if (rend == null ||
                rend is ParticleSystemRenderer ||
                rend is TrailRenderer ||
                rend is LineRenderer)
            {
                continue;
            }

            bool belongsToFlightRoot =
                rend.transform == flightRoot || rend.transform.IsChildOf(flightRoot);

            if (!belongsToFlightRoot)
            {
                rend.enabled = false;
            }
        }
    }

    private Transform GetSafeSoldierRoot()
    {
        // 留空是最安全的默认值：只处理挂着本组件的这一名士兵。
        if (独立士兵根节点 == null || 独立士兵根节点 == transform)
        {
            return transform;
        }

        Transform candidate = 独立士兵根节点;
        bool containsThisHealth = transform.IsChildOf(candidate);
        SoldierHealth[] healthComponents = candidate.GetComponentsInChildren<SoldierHealth>(true);
        bool containsOnlyThisSoldier = healthComponents.Length == 1 && healthComponents[0] == this;

        if (containsThisHealth && containsOnlyThisSoldier)
        {
            return candidate;
        }

        if (!hasWarnedUnsafeSoldierRoot && Application.isPlaying)
        {
            hasWarnedUnsafeSoldierRoot = true;
            Debug.LogWarning(
                "SoldierHealth：独立士兵根节点包含其他士兵，或并非当前士兵的父节点。为了防止整组士兵一起移动/消失，本次已自动改为只处理当前物体。",
                this);
        }

        return transform;
    }

    private Transform GetKnockbackTarget()
    {
        Transform safeSoldierRoot = GetSafeSoldierRoot();

        if (击退位移节点 == null)
        {
            return safeSoldierRoot;
        }

        bool belongsToThisSoldier =
            击退位移节点 == safeSoldierRoot || 击退位移节点.IsChildOf(safeSoldierRoot);

        if (belongsToThisSoldier)
        {
            return 击退位移节点;
        }

        if (!hasWarnedUnsafeKnockbackTarget && Application.isPlaying)
        {
            hasWarnedUnsafeKnockbackTarget = true;
            Debug.LogWarning(
                "SoldierHealth：击退位移节点不属于当前士兵。为避免移动到其他角色，本次已改用安全的单兵根节点。",
                this);
        }

        return safeSoldierRoot;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.8f);

        // 模拟一个默认的受击方向（物体正后方）并应用轴向控制
        Vector3 defaultDir = -transform.forward;
        Vector3 previewDir = new Vector3(
            defaultDir.x * 击退轴向控制.x,
            defaultDir.y * 击退轴向控制.y,
            defaultDir.z * 击退轴向控制.z
        );

        Transform previewTarget = 击退位移节点 != null ? 击退位移节点 : transform;
        Vector3 knockbackEndPos = previewTarget.position + (previewDir.sqrMagnitude > 0.001f ? previewDir.normalized : Vector3.zero) * 击退距离;
        Gizmos.DrawLine(previewTarget.position, knockbackEndPos);
        Gizmos.DrawSphere(knockbackEndPos, 0.3f);
    }
}