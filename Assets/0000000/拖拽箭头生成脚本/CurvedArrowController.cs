using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CurvedArrowController : MonoBehaviour
{
    // 缓动类型枚举，方便在 Inspector 里直接下拉切换
    public enum EaseType
    {
        Linear,         // 线性
        EaseIn,         // 缓入 (先慢后快)
        EaseOut,        // 缓出 (先快后慢)
        EaseInOut,      // 缓入缓出 (两头慢中间快)
        CustomCurve     // 使用自定义 AnimationCurve 曲线
    }

    [Header("轨迹控制点")]
    public Transform startPoint;   // 拖拽起点
    public Transform controlPoint; // 弯曲控制点
    public Transform endPoint;     // 拖拽终点

    [Header("平滑度设置")]
    public int segmentCount = 30;  // 曲线细分段数

    [Header("宽度控制")]
    [Tooltip("整体线条的宽度缩放")]
    public float lineWidth = 0.2f;

    [Tooltip("是否使用宽度曲线（例如让尾部窄、头部宽）")]
    public bool useWidthCurve = false;
    public AnimationCurve widthCurve = AnimationCurve.Linear(0, 0.1f, 1, 0.5f);

    [Header("Timeline 控制参数")]
    [Range(0f, 1f)] public float drawProgress = 0f;  // 划出进度 (0~1)
    [Range(0f, 1f)] public float eraseProgress = 0f; // 消失进度 (0~1)

    [Header("动画曲线设置")]
    [Tooltip("选择动画缓动类型")]
    public EaseType easeType = EaseType.EaseOut;

    [Tooltip("当 EaseType 选择 CustomCurve 时生效的自定义曲线")]
    public AnimationCurve customDrawCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        UpdateArrowWidth();
        UpdateArrow();
    }

    // 实时更新宽度设置
    void UpdateArrowWidth()
    {
        if (lineRenderer == null) return;

        lineRenderer.widthMultiplier = lineWidth;

        if (useWidthCurve)
        {
            lineRenderer.widthCurve = widthCurve;
        }
    }

    void UpdateArrow()
    {
        if (startPoint == null || controlPoint == null || endPoint == null) return;

        // 1. 将 Timeline 传入的原始 0~1 线性进度，经过缓动函数转换成平滑后的 t 概率
        float smoothedDraw = EvaluateEase(drawProgress);
        float smoothedErase = EvaluateEase(eraseProgress);

        // 2. 根据转换后的平滑进度，计算激活的段数
        int activeSegments = Mathf.FloorToInt(segmentCount * (smoothedDraw - smoothedErase));

        if (activeSegments <= 0 || smoothedDraw <= smoothedErase)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = activeSegments + 1;

        // 3. 在 [smoothedErase, smoothedDraw] 范围内采样贝塞尔曲线
        for (int i = 0; i <= activeSegments; i++)
        {
            float step = (float)i / activeSegments;
            float t = Mathf.Lerp(smoothedErase, smoothedDraw, step);

            Vector3 position = CalculateQuadraticBezierPoint(t, startPoint.position, controlPoint.position, endPoint.position);
            lineRenderer.SetPosition(i, position);
        }
    }

    // 缓动函数计算
    private float EvaluateEase(float value)
    {
        value = Mathf.Clamp01(value);

        switch (easeType)
        {
            case EaseType.Linear:
                return value;

            case EaseType.EaseIn:
                // 平方缓入 (二次方)
                return value * value;

            case EaseType.EaseOut:
                // 翻转平方缓出 (模拟拖拽后半段减速手感)
                return 1f - (1f - value) * (1f - value);

            case EaseType.EaseInOut:
                // 平滑 S 型曲线
                return value * value * (3f - 2f * value);

            case EaseType.CustomCurve:
                // 使用在 Inspector 里自由绘制的曲线
                return customDrawCurve != null ? customDrawCurve.Evaluate(value) : value;

            default:
                return value;
        }
    }

    // 二阶贝塞尔曲线计算公式
    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;

        return p;
    }
}
