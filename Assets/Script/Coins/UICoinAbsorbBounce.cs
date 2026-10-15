using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UICoinAbsorbBounce : MonoBehaviour
{
    [Header("挂在 UICoinSpawner 的吸收目标 UI 上")]
    [Tooltip("开启后金币真正到达时触发一次缩放弹动。关闭会停止弹动并恢复原尺寸。建议开启。")]
    public bool 启用弹动 = true;
    [Tooltip("实际缩放哪个 UI。留空缩放本物体；建议指定金币图标子物体，数字和其他布局就不会跟着放大。组件本身仍挂在生成器指定的吸收目标上。")]
    public RectTransform 弹动物体;
    [Tooltip("相对原尺寸的最大放大量。0.12 表示最多变成 112%；建议 0.08～0.18。连续金币不会叠加幅度。仅缩放 X/Y，Z 保持原值。")]
    [Range(0f, 0.5f)] public float 弹动幅度 = 0.12f;
    [Tooltip("一次放大再回到原尺寸的总秒数。建议 0.18～0.3；0 表示不播放。起点和终点速度为零，避免突然跳尺寸。")]
    [Min(0f)] public float 单次弹动时长 = 0.24f;
    [Tooltip("前一次弹动结束后，补播前至少停多久，单位秒。建议 0～0.06；数值越大节奏越慢。")]
    [Min(0f)] public float 两次弹动间隔 = 0.04f;
    [Tooltip("开启：弹动过程中再收到金币，最多记下一次补播，持续收币会稳定重复。关闭：只在空闲时响应，弹动/间隔中收到的金币不补播。多枚同帧到达会合并，不会排出长队列。建议开启。")]
    public bool 连续到达时补播一次 = true;
    [Tooltip("建议开启：每次弹动跟随触发它的金币时间模式。生成器忽略暂停时，吸收 UI 的弹动也忽略暂停。")]
    public bool 时间跟随金币 = true;
    [Tooltip("时间跟随金币关闭时，或手动调用无参数 NotifyCoinArrived() 时使用此设置。建议暂停界面开启，普通游戏按需关闭。")]
    public bool 使用不受暂停影响的时间 = false;

    private RectTransform visual;
    private Vector3 baseScale;
    private bool hasBaseScale;
    private bool pulsing;
    private bool pending;
    private bool pulseUnscaled;
    private bool pendingUnscaled;
    private float elapsed;
    private float pulseDuration;
    private float pulseAmplitude;
    private float cooldown;

    private void OnEnable() { PrepareVisual(); }
    private void OnDisable() { StopAndRestore(); }
    private void OnDestroy() { StopAndRestore(); }

    /// <summary>可由按钮/Signal 手动测试一次；使用本组件的时间设置。</summary>
    public void NotifyCoinArrived() { NotifyCoinArrived(使用不受暂停影响的时间); }

    /// <summary>由 UICoin 真正到达目标时调用。同一组件最多保留一次待播。</summary>
    public void NotifyCoinArrived(bool coinUsesUnscaledTime)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !启用弹动) return;
        PrepareVisual();
        if (visual == null || 单次弹动时长 <= 0f || 弹动幅度 <= 0f) return;
        bool unscaled = 时间跟随金币 ? coinUsesUnscaledTime : 使用不受暂停影响的时间;
        if (!pulsing && cooldown <= 0f)
            BeginPulse(unscaled);
        else if (连续到达时补播一次)
        {
            pending = true;
            pendingUnscaled = unscaled;
        }
    }

    private void PrepareVisual()
    {
        RectTransform desired = 弹动物体 != null ? 弹动物体 : (RectTransform)transform;
        if (visual == desired && hasBaseScale) return;
        StopAndRestore();
        visual = desired;
        if (visual != null)
        {
            baseScale = visual.localScale;
            hasBaseScale = true;
        }
    }

    private void BeginPulse(bool unscaled)
    {
        if (visual == null) return;
        pending = false;
        // 空闲时重新记录基础尺寸，支持两次效果之间主动调整图标大小。
        baseScale = visual.localScale;
        hasBaseScale = true;
        pulseUnscaled = unscaled;
        pulseDuration = Mathf.Max(0f, 单次弹动时长);
        pulseAmplitude = Mathf.Clamp(弹动幅度, 0f, 0.5f);
        elapsed = 0f;
        pulsing = pulseDuration > 0f && pulseAmplitude > 0f;
    }

    private void LateUpdate()
    {
        if (!启用弹动)
        {
            StopAndRestore();
            return;
        }
        PrepareVisual();
        if (visual == null) return;
        if (!连续到达时补播一次) pending = false;
        float dt = pulseUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        if (pulsing)
        {
            elapsed += dt;
            float progress = Mathf.Clamp01(elapsed / pulseDuration);
            float wave = Mathf.Sin(progress * Mathf.PI);
            // 固定基础尺寸上的有界脉冲，不从当前放大尺寸继续乘，不重启正在进行的动作。
            float factor = 1f + pulseAmplitude * wave * wave;
            visual.localScale = new Vector3(baseScale.x * factor, baseScale.y * factor, baseScale.z);
            if (progress >= 1f)
            {
                visual.localScale = baseScale;
                pulsing = false;
                cooldown = Mathf.Max(0f, 两次弹动间隔);
            }
        }
        else
        {
            // 等待补播时以待播金币的时间模式计时，避免暂停后队列无法继续。
            float waitDt = pending
                ? (pendingUnscaled ? Time.unscaledDeltaTime : Time.deltaTime) : dt;
            cooldown = Mathf.Max(0f, cooldown - waitDt);
            if (cooldown <= 0f && pending)
            {
                bool unscaled = pendingUnscaled;
                pending = false;
                BeginPulse(unscaled);
            }
        }
    }

    /// <summary>停止当前和待播弹动，恢复开始弹动前的基础尺寸。</summary>
    public void StopAndRestore()
    {
        if ((pulsing || pending) && hasBaseScale && visual != null) visual.localScale = baseScale;
        pulsing = false;
        pending = false;
        elapsed = 0f;
        cooldown = 0f;
    }
}
