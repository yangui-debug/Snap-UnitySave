using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LineRenderer的曲线补点识别与优化工具
/// 识别和剔除曲线上为了平滑而添加的补点
/// </summary>
public class CurvePointOptimizer : MonoBehaviour
{
    [Header("LineRenderer 引用")]
    public LineRenderer lineRenderer;

    [Header("优化参数")]
    [Range(0.01f, 1f)] public float curvatureThreshold = 0.1f; // 曲率变化阈值
    [Range(0.01f, 0.5f)] public float distanceThreshold = 0.05f; // 距离阈值
    [Range(1, 10)] public int windowSize = 3; // 分析窗口大小

    [Header("操作选项")]
    public bool autoApply = false; // 是否自动应用优化
    public bool keepEndPoints = true; // 是否保留端点

    [Header("调试选项")]
    public bool visualizeOriginal = true; // 可视化原始点
    public bool visualizeOptimized = true; // 可视化优化后的点
    public Color originalColor = Color.red;
    public Color optimizedColor = Color.green;
    public float pointSize = 0.1f;

    private Vector3[] originalPoints;
    private Vector3[] optimizedPoints;
    private List<int> keptPointIndices = new List<int>();

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null && lineRenderer.positionCount > 0)
        {
            originalPoints = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(originalPoints);
            
            if (autoApply)
                OptimizeCurvePoints();
        }
    }

    void Update()
    {
        // 调试可视化
        if (visualizeOriginal && originalPoints != null)
        {
            VisualizePoints(originalPoints, originalColor);
        }

        if (visualizeOptimized && optimizedPoints != null)
        {
            VisualizePoints(optimizedPoints, optimizedColor);
        }
    }

    /// <summary>
    /// 优化曲线上的点
    /// </summary>
    public void OptimizeCurvePoints()
    {
        if (lineRenderer == null || lineRenderer.positionCount < 3)
        {
            Debug.LogWarning("LineRenderer 未设置或点数不足");
            return;
        }

        // 获取原始点
        originalPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(originalPoints);

        // 识别并剔除补点
        keptPointIndices = IdentifyKeyPoints(originalPoints);
        optimizedPoints = CreateOptimizedPoints(originalPoints, keptPointIndices);

        // 应用优化后的点
        lineRenderer.positionCount = optimizedPoints.Length;
        lineRenderer.SetPositions(optimizedPoints);

        Debug.Log($"优化完成: 从 {originalPoints.Length} 点减少到 {optimizedPoints.Length} 点");
    }

    /// <summary>
    /// 识别曲线上的关键点
    /// </summary>
    private List<int> IdentifyKeyPoints(Vector3[] points)
    {
        List<int> keyPointIndices = new List<int>();
        
        // 总是保留第一个点
        keyPointIndices.Add(0);

        // 分析每个点，确定是否为关键点
        for (int i = 1; i < points.Length - 1; i++)
        {
            // 计算当前点与前一个点的距离
            float prevDistance = Vector3.Distance(points[i], points[i - 1]);
            
            // 计算当前点与后一个点的距离
            float nextDistance = Vector3.Distance(points[i], points[i + 1]);
            
            // 如果两点距离都很小，可能是补点
            if (prevDistance < distanceThreshold && nextDistance < distanceThreshold)
            {
                // 检查曲率变化
                float curvature = CalculateCurvature(points, i, windowSize);
                
                // 如果曲率变化不大，则跳过这个点（视为补点）
                if (curvature < curvatureThreshold)
                {
                    continue;
                }
            }
            
            // 保留这个点
            keyPointIndices.Add(i);
        }

        // 总是保留最后一个点
        if (keepEndPoints)
        {
            keyPointIndices.Add(points.Length - 1);
        }

        return keyPointIndices;
    }

    /// <summary>
    /// 计算指定点处的曲率
    /// </summary>
    private float CalculateCurvature(Vector3[] points, int centerIndex, int window)
    {
        int start = Mathf.Max(0, centerIndex - window);
        int end = Mathf.Min(points.Length - 1, centerIndex + window);
        
        // 计算前后方向向量
        Vector3 forward = (points[end] - points[start]).normalized;
        
        // 计算中心点处的切线（使用前后点的方向）
        Vector3 tangent = (points[centerIndex + 1] - points[centerIndex - 1]).normalized;
        
        // 计算曲率（切线方向与整体方向的角度差）
        float curvature = 1f - Mathf.Abs(Vector3.Dot(forward, tangent));
        
        return curvature;
    }

    /// <summary>
    /// 根据保留的点索引创建优化后的点数组
    /// </summary>
    private Vector3[] CreateOptimizedPoints(Vector3[] originalPoints, List<int> keptIndices)
    {
        Vector3[] optimized = new Vector3[keptIndices.Count];
        
        for (int i = 0; i < keptIndices.Count; i++)
        {
            optimized[i] = originalPoints[keptIndices[i]];
        }
        
        return optimized;
    }

    /// <summary>
    /// 可视化点
    /// </summary>
    private void VisualizePoints(Vector3[] points, Color color)
    {
        for (int i = 0; i < points.Length; i++)
        {
            // 绘制点
            Debug.DrawLine(points[i] - Vector3.right * pointSize, 
                          points[i] + Vector3.right * pointSize, color);
            Debug.DrawLine(points[i] - Vector3.up * pointSize, 
                          points[i] + Vector3.up * pointSize, color);
            Debug.DrawLine(points[i] - Vector3.forward * pointSize, 
                          points[i] + Vector3.forward * pointSize, color);

            // 绘制连线
            if (i < points.Length - 1)
            {
                Debug.DrawLine(points[i], points[i + 1], color * 0.7f);
            }
        }
    }

    /// <summary>
    /// 恢复原始点
    /// </summary>
    public void RestoreOriginalPoints()
    {
        if (lineRenderer != null && originalPoints != null)
        {
            lineRenderer.positionCount = originalPoints.Length;
            lineRenderer.SetPositions(originalPoints);
            Debug.Log("已恢复原始点");
        }
    }

    /// <summary>
    /// 在Inspector中绘制按钮
    /// </summary>
    [ContextMenu("优化曲线点")]
    private void OptimizeCurvePointsContext()
    {
        OptimizeCurvePoints();
    }

    [ContextMenu("恢复原始点")]
    private void RestoreOriginalPointsContext()
    {
        RestoreOriginalPoints();
    }

    /// <summary>
    /// 显示保留的点索引（用于调试）
    /// </summary>
    public void PrintKeptPointIndices()
    {
        if (keptPointIndices != null && keptPointIndices.Count > 0)
        {
            string indices = "保留的点索引: ";
            foreach (int index in keptPointIndices)
            {
                indices += index + " ";
            }
            Debug.Log(indices);
        }
    }
}