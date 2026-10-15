using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Timeline 专用的实时弯曲箭头。
/// 使用四个控制点决定三次贝塞尔曲线，并通过 Progress（0~1）控制箭头生长。
/// 不需要任何箭头图片或贴图。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class TimelineCurvedArrow : MonoBehaviour
{
    [Header("曲线路径")]
    [Tooltip("箭头起点。留空时会使用当前物体的位置。")]
    public Transform startPoint;

    [Tooltip("第一个控制点，主要决定箭头刚离开起点时朝哪个方向弯。")]
    public Transform controlPointA;

    [Tooltip("第二个控制点，主要决定箭头接近终点时的弯曲方向。")]
    public Transform controlPointB;

    [Tooltip("箭头最终到达的位置。")]
    public Transform endPoint;

    [Header("Timeline 动画参数")]
    [Tooltip("箭头显示进度：0 完全隐藏，1 完整显示。把这个参数加入 Timeline 动画轨道并打关键帧。")]
    [Range(0f, 1f)] public float progress = 1f;

    [Tooltip("从箭头尾端向箭头尖擦除的进度：0 不擦除，1 完全消失。通常在 Progress 到达 1 后，再用 Timeline 将它从 0 动画到 1。")]
    [Range(0f, 1f)] public float disappearProgress = 0f;

    [Header("外观")]
    [Tooltip("箭身颜色。使用 HDR 颜色面板，可通过 Intensity 配合 URP Bloom 调整发光强度。")]
    [ColorUsage(true, true)]
    [FormerlySerializedAs("arrowColor")]
    public Color bodyColor = new Color(0.34f, 0.66f, 0.95f, 0.92f);

    [Tooltip("箭头尖颜色。默认和箭身一致，也可以单独设置；同样支持 HDR Intensity 发光。")]
    [ColorUsage(true, true)]
    public Color headColor = new Color(0.34f, 0.66f, 0.95f, 0.92f);

    [Tooltip("箭身宽度。默认按 Orthographic Size 约 90 的远景设置；较近的相机可适当减小。")]
    [Min(0.01f)] public float bodyWidth = 1.1f;

    [Tooltip("箭头尖的长度。推荐为箭身宽度的 2.5~3.5 倍。")]
    [Min(0.02f)] public float headLength = 3.2f;

    [Tooltip("箭头尖底边的宽度。推荐为箭身宽度的 2~2.8 倍。")]
    [Min(0.02f)] public float headWidth = 2.6f;

    [Tooltip("整体向世界 Y 轴抬高的距离，避免箭头和地面闪烁。推荐 0.03~0.15。")]
    public float surfaceOffset = 0.15f;

    [Header("显示层级")]
    [Tooltip("箭头使用的 Sorting Layer 名称，必须和 Unity 中创建的名称完全一致，例如 Default、Effect、UIEffect。")]
    public string sortingLayerName = "Default";

    [Tooltip("箭头在同一个 Sorting Layer 中的显示顺序。数值越大越靠前。箭身和箭头尖会始终使用相同数值。")]
    public int orderInLayer = 20;

    [Header("平滑度")]
    [Tooltip("整条曲线由多少小段组成。推荐 40；近景可提高到 60。")]
    [Range(8, 100)] public int segmentCount = 40;

    [Tooltip("圆角精细度。推荐 8；如果移动端性能紧张可降到 4。")]
    [Range(0, 16)] public int roundness = 8;

    private LineRenderer line;
    private GameObject headObject;
    private Mesh headMesh;
    private MeshFilter headFilter;
    private MeshRenderer headRenderer;
    private Material runtimeBodyMaterial;
    private Material runtimeHeadMaterial;

    private readonly List<Vector3> fullPath = new List<Vector3>();
    private readonly List<float> accumulatedLengths = new List<float>();
    private readonly List<Vector3> visiblePath = new List<Vector3>();

    // 复用箭头 Mesh 数据，避免 Timeline 播放时每帧 new 数组造成 GC 峰值。
    private readonly Vector3[] headVertices = new Vector3[3];
    private readonly int[] headTriangles = { 0, 1, 2, 0, 2, 1 };
    private readonly Color[] headColors = { Color.white, Color.white, Color.white };
    private readonly Vector2[] headUVs = { new Vector2(0.5f, 1f), Vector2.zero, Vector2.right };


    private void Reset()
    {
        CreateDefaultControlPoints();
        progress = 1f;
        EnsureObjects();
        RebuildArrow();
    }

    private void OnEnable()
    {
        EnsureObjects();
        RebuildArrow();
    }

    private void LateUpdate()
    {
        // Timeline/Animator 通常会在 LateUpdate 之前完成本帧属性写入。
        // 播放时只在这里统一重建一次，避免同一帧重复改 LineRenderer / Mesh。
        if (line == null || headObject == null || headMesh == null || headRenderer == null)
            EnsureObjects();

        RebuildArrow();
    }

    private void OnValidate()
    {
        bodyWidth = Mathf.Max(0.01f, bodyWidth);
        headLength = Mathf.Max(0.02f, headLength);
        headWidth = Mathf.Max(0.02f, headWidth);
        segmentCount = Mathf.Clamp(segmentCount, 8, 100);
        roundness = Mathf.Clamp(roundness, 0, 16);

        EnsureObjects();
        RebuildArrow();
    }

    private void OnDidApplyAnimationProperties()
    {
        // 播放模式下交给 LateUpdate 统一刷新，防止同一帧被 Timeline 连续重建两次。
        // 编辑模式拖动 Timeline 播放头时仍然立即刷新，保留实时预览。
        if (!Application.isPlaying)
        {
            EnsureObjects();
            RebuildArrow();
        }
    }

    [ContextMenu("自动创建默认控制点")]
    public void CreateDefaultControlPoints()
    {
        if (startPoint == null)
            startPoint = CreatePoint("Arrow_Start", new Vector3(0f, 0f, 0f));

        if (controlPointA == null)
            controlPointA = CreatePoint("Arrow_Control_A", new Vector3(0f, 0f, 3f));

        if (controlPointB == null)
            controlPointB = CreatePoint("Arrow_Control_B", new Vector3(3f, 0f, 5f));

        if (endPoint == null)
            endPoint = CreatePoint("Arrow_End", new Vector3(5f, 0f, 5f));
    }

    [ContextMenu("隐藏箭头")]
    public void HideArrow()
    {
        disappearProgress = 1f;
        ApplyVisibility(false);
    }

    [ContextMenu("完整显示箭头")]
    public void ShowFullArrow()
    {
        progress = 1f;
        disappearProgress = 0f;
        RebuildArrow();
    }

    private Transform CreatePoint(string pointName, Vector3 localPosition)
    {
        Transform existing = transform.Find(pointName);
        if (existing != null)
            return existing;

        GameObject point = new GameObject(pointName);
        point.transform.SetParent(transform, false);
        point.transform.localPosition = localPosition;
        return point.transform;
    }

    private void EnsureObjects()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        if (line == null)
            return;

        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = roundness;
        line.numCapVertices = roundness;
        line.startWidth = bodyWidth;
        line.endWidth = bodyWidth;
        // 颜色统一交给同一份材质控制，LineRenderer 本身保持白色，
        // 避免不同 Shader 对线条顶点色和 Mesh 顶点色的处理不一致。
        line.startColor = Color.white;
        line.endColor = Color.white;
        ApplySortingLayer(line);

        EnsureMaterials();

        if (headObject == null)
        {
            Transform found = transform.Find("Generated_Arrow_Head");
            if (found != null)
                headObject = found.gameObject;
        }

        if (headObject == null)
        {
            headObject = new GameObject("Generated_Arrow_Head");
            headObject.transform.SetParent(transform, false);
        }

        // 旧版本可能把整个箭头头部 GameObject 设成 inactive。
        // 新版本始终保持对象激活，只切 MeshRenderer.enabled，避免 Timeline 激活/失活闪帧。
        if (!headObject.activeSelf)
            headObject.SetActive(true);

        if (headFilter == null)
            headFilter = headObject.GetComponent<MeshFilter>();
        if (headFilter == null)
            headFilter = headObject.AddComponent<MeshFilter>();

        if (headRenderer == null)
            headRenderer = headObject.GetComponent<MeshRenderer>();
        if (headRenderer == null)
            headRenderer = headObject.AddComponent<MeshRenderer>();

        headRenderer.sharedMaterial = runtimeHeadMaterial;
        ApplySortingLayer(headRenderer);
        headObject.layer = gameObject.layer;

        if (headMesh == null)
        {
            headMesh = new Mesh
            {
                name = "Generated Timeline Arrow Head",
                hideFlags = HideFlags.HideAndDontSave
            };
            headMesh.MarkDynamic();

            // 三角形拓扑、颜色和 UV 永远不变，只初始化一次。
            headMesh.vertices = headVertices;
            headMesh.triangles = headTriangles;
            headMesh.colors = headColors;
            headMesh.uv = headUVs;
        }

        headFilter.sharedMesh = headMesh;
    }

    private void EnsureMaterials()
    {
        // 材质已经创建后不要再每帧 Shader.Find。
        if (runtimeBodyMaterial != null && runtimeHeadMaterial != null)
        {
            line.sharedMaterial = runtimeBodyMaterial;
            headRendererSafeAssign();
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        if (runtimeBodyMaterial == null)
            runtimeBodyMaterial = CreateRuntimeMaterial(shader, "Generated Arrow Body Material");

        if (runtimeHeadMaterial == null)
            runtimeHeadMaterial = CreateRuntimeMaterial(shader, "Generated Arrow Head Material");

        line.sharedMaterial = runtimeBodyMaterial;
        headRendererSafeAssign();
        ApplyMaterialColors();
    }

    private void headRendererSafeAssign()
    {
        if (headRenderer != null)
            headRenderer.sharedMaterial = runtimeHeadMaterial;
    }

    private static Material CreateRuntimeMaterial(Shader shader, string materialName)
    {
        return new Material(shader)
        {
            name = materialName,
            color = Color.white,
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private void RebuildArrow()
    {
        if (line == null || headObject == null || headMesh == null)
            return;

        if (progress <= 0.0001f || endPoint == null)
        {
            ApplyVisibility(false);
            return;
        }

        Vector3 p0 = AddOffset(startPoint != null ? startPoint.position : transform.position);
        Vector3 p1 = AddOffset(controlPointA != null ? controlPointA.position : p0);
        Vector3 p2 = AddOffset(controlPointB != null ? controlPointB.position : endPoint.position);
        Vector3 p3 = AddOffset(endPoint.position);

        BuildFullPath(p0, p1, p2, p3);
        if (fullPath.Count < 2)
        {
            ApplyVisibility(false);
            return;
        }

        float totalLength = accumulatedLengths[accumulatedLengths.Count - 1];
        if (totalLength <= 0.0001f)
        {
            ApplyVisibility(false);
            return;
        }

        float wantedLength = totalLength * Mathf.Clamp01(progress);
        float tailLength = totalLength * Mathf.Clamp01(disappearProgress);

        // 当尾端的擦除位置追上当前箭头尖时，整条箭头隐藏。
        if (tailLength >= wantedLength - 0.0001f)
        {
            ApplyVisibility(false);
            return;
        }

        BuildVisiblePath(tailLength, wantedLength);

        if (visiblePath.Count < 2)
        {
            // 极短区间偶尔会因为浮点精度只得到 1 个点。
            // 不要为了这一帧主动隐藏，保留上一帧 Renderer 状态可避免“闪一下”。
            return;
        }

        Vector3 tip = visiblePath[visiblePath.Count - 1];
        Vector3 previous = visiblePath[visiblePath.Count - 2];
        Vector3 direction = tip - previous;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.000001f)
        {
            // 临界帧方向退化时保留上一帧，而不是把整条箭头关掉一帧。
            return;
        }

        direction.Normalize();

        // 箭头刚出现时同步缩小，避免一开始只冒出一个巨大三角形。
        float remainingLength = wantedLength - tailLength;
        float currentHeadScale = Mathf.Clamp01(remainingLength / Mathf.Max(headLength * 1.4f, 0.001f));
        float currentHeadLength = headLength * currentHeadScale;
        float currentHeadWidth = headWidth * currentHeadScale;
        Vector3 headBase = tip - direction * currentHeadLength;

        // 箭身停在箭头尖底部，使外轮廓更干净。
        TrimVisiblePathToHeadBase(headBase, direction);

        line.startWidth = bodyWidth;
        line.endWidth = bodyWidth;
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.numCornerVertices = roundness;
        line.numCapVertices = roundness;
        ApplySortingLayer(line);
        ApplySortingLayer(headRenderer);
        headObject.layer = gameObject.layer;
        // 不使用 visiblePath.ToArray()，避免每帧产生托管内存垃圾。
        line.positionCount = visiblePath.Count;
        for (int i = 0; i < visiblePath.Count; i++)
            line.SetPosition(i, visiblePath[i]);

        ApplyMaterialColors();

        UpdateHeadMesh(tip, headBase, direction, currentHeadWidth);
        ApplyVisibility(true);
    }

    private void BuildFullPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        fullPath.Clear();
        accumulatedLengths.Clear();

        float accumulated = 0f;
        Vector3 previous = p0;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 point = CubicBezier(p0, p1, p2, p3, t);

            if (i > 0)
                accumulated += Vector3.Distance(previous, point);

            fullPath.Add(point);
            accumulatedLengths.Add(accumulated);
            previous = point;
        }
    }

    private void BuildVisiblePath(float startLength, float endLength)
    {
        visiblePath.Clear();

        visiblePath.Add(GetPointAtLength(startLength));

        for (int i = 1; i < fullPath.Count; i++)
        {
            float pointLength = accumulatedLengths[i];

            if (pointLength > startLength && pointLength < endLength)
                visiblePath.Add(fullPath[i]);
        }

        Vector3 endPointOnPath = GetPointAtLength(endLength);
        if (Vector3.Distance(visiblePath[visiblePath.Count - 1], endPointOnPath) > 0.0001f)
            visiblePath.Add(endPointOnPath);
    }

    private Vector3 GetPointAtLength(float wantedLength)
    {
        if (wantedLength <= 0f)
            return fullPath[0];

        float totalLength = accumulatedLengths[accumulatedLengths.Count - 1];
        if (wantedLength >= totalLength)
            return fullPath[fullPath.Count - 1];

        for (int i = 1; i < fullPath.Count; i++)
        {
            float segmentEnd = accumulatedLengths[i];
            if (segmentEnd < wantedLength)
                continue;

            float segmentStart = accumulatedLengths[i - 1];
            float segmentLength = Mathf.Max(segmentEnd - segmentStart, 0.000001f);
            float segmentT = Mathf.Clamp01((wantedLength - segmentStart) / segmentLength);
            return Vector3.Lerp(fullPath[i - 1], fullPath[i], segmentT);
        }

        return fullPath[fullPath.Count - 1];
    }

    private void TrimVisiblePathToHeadBase(Vector3 headBase, Vector3 direction)
    {
        // 对很短的箭头保留至少两个点。
        while (visiblePath.Count > 2)
        {
            Vector3 candidate = visiblePath[visiblePath.Count - 2];
            if (Vector3.Dot(candidate - headBase, direction) <= 0f)
                break;

            visiblePath.RemoveAt(visiblePath.Count - 1);
        }

        visiblePath[visiblePath.Count - 1] = headBase;
    }

    private void UpdateHeadMesh(
        Vector3 tip,
        Vector3 headBase,
        Vector3 direction,
        float currentHeadWidth)
    {
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * (currentHeadWidth * 0.5f);
        Vector3 left = headBase + side;
        Vector3 right = headBase - side;

        headVertices[0] = headObject.transform.InverseTransformPoint(tip);
        headVertices[1] = headObject.transform.InverseTransformPoint(left);
        headVertices[2] = headObject.transform.InverseTransformPoint(right);

        // 拓扑不变，不再每帧 Clear / new triangles / new colors / new uv。
        // URP/Unlit 不依赖法线，因此也无需每帧 RecalculateNormals。
        headMesh.vertices = headVertices;
        headMesh.RecalculateBounds();
    }

    private void ApplyVisibility(bool visible)
    {
        // 保留上一帧的几何数据，只切 Renderer，不清空 positionCount，
        // 也不再 SetActive 整个箭头头部对象。这样 Timeline/Recorder 的临界帧
        // 不会经历“清空 -> 激活 -> 重建”的状态跳变。
        if (line != null)
            line.enabled = visible;

        if (headRenderer != null)
            headRenderer.enabled = visible;
    }

    private void ApplySortingLayer(Renderer targetRenderer)
    {
        if (targetRenderer == null)
            return;

        if (!string.IsNullOrEmpty(sortingLayerName))
            targetRenderer.sortingLayerName = sortingLayerName;

        targetRenderer.sortingOrder = orderInLayer;
    }

    private void ApplyMaterialColors()
    {
        ApplyOneMaterialColor(runtimeBodyMaterial, bodyColor);
        ApplyOneMaterialColor(runtimeHeadMaterial, headColor);
    }

    private static void ApplyOneMaterialColor(Material targetMaterial, Color targetColor)
    {
        if (targetMaterial == null)
            return;

        targetMaterial.color = targetColor;

        // 同时兼容 Sprites/Default 与 URP/Unlit 的颜色属性名称。
        if (targetMaterial.HasProperty("_Color"))
            targetMaterial.SetColor("_Color", targetColor);

        if (targetMaterial.HasProperty("_BaseColor"))
            targetMaterial.SetColor("_BaseColor", targetColor);
    }

    private Vector3 AddOffset(Vector3 point)
    {
        point.y += surfaceOffset;
        return point;
    }

    private static Vector3 CubicBezier(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * oneMinusT * p0
             + 3f * oneMinusT * oneMinusT * t * p1
             + 3f * oneMinusT * t * t * p2
             + t * t * t * p3;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            if (headMesh != null)
                Destroy(headMesh);
            if (runtimeBodyMaterial != null)
                Destroy(runtimeBodyMaterial);
            if (runtimeHeadMaterial != null)
                Destroy(runtimeHeadMaterial);
        }
        else
        {
            if (headMesh != null)
                DestroyImmediate(headMesh);
            if (runtimeBodyMaterial != null)
                DestroyImmediate(runtimeBodyMaterial);
            if (runtimeHeadMaterial != null)
                DestroyImmediate(runtimeHeadMaterial);
        }
    }
}
