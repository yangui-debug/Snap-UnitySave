using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UICoin : MonoBehaviour
{
    [Header("爆出与弹跳（在金币预制体上设置）")]
    [Tooltip("第一次弹跳的最高高度。新生成器按 UI 单位计算；建议 60～150，0 表示平移散开。")]
    [Min(0f)] public float initialBounceHeight = 100f;
    [Tooltip("从爆出点飞到散落点的时间，单位秒。建议 0.35～0.65；0 表示直接到达。")]
    [Min(0f)] public float firstBounceDuration = 0.5f;
    [Tooltip("第一次落地后再小跳几次。每次高度和时间会衰减；建议 0～2。")]
    [Min(0)] public int extraBounces = 2;

    [Header("吸收飞行（在金币预制体上设置）")]
    [Tooltip("吸收飞行的向上弧线高度。建议 80～200；0 表示直线飞向目标。")]
    [Min(0f)] public float arcHeight = 150f;
    [Tooltip("飞向吸收目标的时间，单位秒。建议 0.5～1.0；0 表示立即吸收。")]
    [Min(0f)] public float moveDuration = 1f;
    [Tooltip("单独调用 Initialize 时是否忽略 Time.timeScale。使用新版生成器时由生成器统一控制。")]
    public bool useUnscaledTime = false;

    private RectTransform target;
    private Transform motionSpace;
    private bool useUIPlane;
    private bool initialized;
    private float delay;
    private float scaleDuration;
    private Vector3 originalScale;
    private Vector3 groundPosition;
    private Vector3 axisToRotate;
    private float spinSpeed;
    private float DeltaTime { get { return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; } }

    // 保留原来的接口，旧脚本调用时仍使用世界坐标。
    public void Initialize(RectTransform targetTransform, float delayBeforeMove, float scaleTime,
        Vector3 targetGroundPos, Vector3 rotationAxis, float rotationSpeed)
    {
        Begin(targetTransform, delayBeforeMove, scaleTime, targetGroundPos, rotationAxis,
            rotationSpeed, false, null, useUnscaledTime);
    }

    // 新生成器使用父容器的 UI 平面，避免 Canvas 缩放改变范围和弹跳高度。
    public void InitializeInUIPlane(RectTransform targetTransform, float delayBeforeMove, float scaleTime,
        Vector3 targetGroundPos, Vector3 rotationAxis, float rotationSpeed, Transform uiSpace, bool unscaledTime)
    {
        Begin(targetTransform, delayBeforeMove, scaleTime, targetGroundPos, rotationAxis,
            rotationSpeed, true, uiSpace, unscaledTime);
    }

    private void Begin(RectTransform targetTransform, float delayBeforeMove, float scaleTime,
        Vector3 targetGroundPos, Vector3 rotationAxis, float rotationSpeed,
        bool uiPlane, Transform space, bool unscaledTime)
    {
        StopAllCoroutines();
        if (!initialized) originalScale = transform.localScale;
        initialized = true;
        target = targetTransform;
        delay = Mathf.Max(0f, delayBeforeMove);
        scaleDuration = Mathf.Max(0f, scaleTime);
        groundPosition = targetGroundPos;
        axisToRotate = rotationAxis.normalized;
        spinSpeed = rotationSpeed * Random.Range(0.8f, 1.2f);
        useUIPlane = uiPlane;
        motionSpace = space;
        useUnscaledTime = unscaledTime;
        transform.localScale = originalScale * 0.1f;
        StartCoroutine(ScaleUpRoutine());
        StartCoroutine(SpinRoutine());
        StartCoroutine(LifeCycleRoutine());
    }

    private Vector3 GetMotionPosition()
    {
        return useUIPlane && motionSpace != null
            ? motionSpace.InverseTransformPoint(transform.position) : transform.position;
    }

    private void SetMotionPosition(Vector3 position)
    {
        transform.position = useUIPlane && motionSpace != null
            ? motionSpace.TransformPoint(position) : position;
    }

    private IEnumerator Bounce(Vector3 start, Vector3 end, float duration, float height)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += DeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            Vector3 position = Vector3.Lerp(start, end, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * Mathf.Max(0f, height);
            SetMotionPosition(position);
            yield return null;
        }
        SetMotionPosition(end);
    }

    private IEnumerator LifeCycleRoutine()
    {
        yield return Bounce(GetMotionPosition(), groundPosition, Mathf.Max(0f, firstBounceDuration), initialBounceHeight);
        float height = Mathf.Max(0f, initialBounceHeight) * 0.3f;
        float duration = Mathf.Max(0f, firstBounceDuration) * 0.6f;
        for (int i = 0; i < Mathf.Max(0, extraBounces); i++)
        {
            if (duration <= 0f || height <= 0f) break;
            yield return Bounce(groundPosition, groundPosition, duration, height);
            height *= 0.4f;
            duration *= 0.7f;
        }

        float timer = 0f;
        while (timer < delay) { timer += DeltaTime; yield return null; }

        if (target != null)
        {
            Vector3 start = GetMotionPosition();
            float flightTime = Mathf.Max(0f, moveDuration);
            timer = 0f;
            while (timer < flightTime && target != null)
            {
                timer += DeltaTime;
                float progress = Mathf.Clamp01(timer / flightTime);
                Vector3 end = useUIPlane && motionSpace != null
                    ? motionSpace.InverseTransformPoint(target.position) : target.position;
                Vector3 position = Vector3.Lerp(start, end, progress);
                position.y += Mathf.Sin(progress * Mathf.PI) * Mathf.Max(0f, arcHeight);
                SetMotionPosition(position);
                yield return null;
            }
            if (target != null)
            {
                transform.position = target.position;
                // 只有真正飞抵目标时通知一次，不在生成/开始飞行/销毁时触发。
                UICoinAbsorbBounce receiver = target.GetComponent<UICoinAbsorbBounce>();
                if (receiver != null) receiver.NotifyCoinArrived(useUnscaledTime);
            }
        }
        // 未配置吸收目标时，也会正常弹跳、等待并销毁，不留下静止金币。
        Destroy(gameObject);
    }

    private IEnumerator ScaleUpRoutine()
    {
        float timer = 0f;
        while (timer < scaleDuration)
        {
            timer += DeltaTime;
            transform.localScale = Vector3.Lerp(originalScale * 0.1f, originalScale,
                Mathf.Clamp01(timer / scaleDuration));
            yield return null;
        }
        transform.localScale = originalScale;
    }

    private IEnumerator SpinRoutine()
    {
        while (true)
        {
            transform.Rotate(axisToRotate, spinSpeed * DeltaTime, Space.Self);
            yield return null;
        }
    }
}
