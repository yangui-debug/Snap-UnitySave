using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Timeline 专用的 UI 弯曲箭头。
/// 
/// 适用于 Unity 2022.3.x / URP，以及 Screen Space Overlay、Screen Space Camera、World Space Canvas。
/// 使用 Start / Control A / Control B / End 四个 RectTransform 形成三次贝塞尔曲线，
/// 通过 Progress 控制箭头从起点向终点生长，通过 Disappear Progress 从尾端向前擦除。
/// 
/// 第一次挂载组件时会自动创建：
/// Arrow_Start / Arrow_Control_A / Arrow_Control_B / Arrow_End / UI_Arrow_Graphic
/// 并自动给 UI_Arrow_Graphic 预设 Default UI Material。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class TimelineUICurvedArrow : MonoBehaviour
{
    [Header("UI 曲线路径（首次挂载会自动创建）")]
    [Tooltip("箭头起点。")]
    public RectTransform startPoint;

    [Tooltip("第一个控制点，决定箭头离开起点时的弯曲方向。")]
    public RectTransform controlPointA;

    [Tooltip("第二个控制点，决定箭头接近终点时的弯曲方向。")]
    public RectTransform controlPointB;

    [Tooltip("箭头终点。")]
    public RectTransform endPoint;

    [Header("Timeline 动画参数")]
    [Tooltip("箭头显示进度：0 完全隐藏，1 完整显示。可直接在 Timeline 中打关键帧。")]
    [Range(0f, 1f)]
    public float progress = 1f;

    [Tooltip("从箭尾向箭头尖擦除：0 不擦除，1 完全消失。通常在 Progress=1 后再从 0 动画到 1。")]
    [Range(0f, 1f)]
    public float disappearProgress = 0f;

    [Header("UI 外观")]
    [Tooltip("箭身颜色。UI 默认材质会直接读取这里的顶点颜色。")]
    public Color bodyColor = new Color(0.34f, 0.66f, 0.95f, 0.92f);

    [Tooltip("箭头尖颜色。")]
    public Color headColor = new Color(0.34f, 0.66f, 0.95f, 0.92f);

    [Tooltip("UI 像素单位下的箭身宽度。")]
    [Min(0.1f)]
    public float bodyWidth = 14f;

    [Tooltip("UI 像素单位下的箭头尖长度。")]
    [Min(0.1f)]
    public float headLength = 38f;

    [Tooltip("UI 像素单位下的箭头尖底边宽度。")]
    [Min(0.1f)]
    public float headWidth = 30f;

    [Header("平滑度")]
    [Tooltip("贝塞尔曲线采样段数。UI 推荐 40~64。")]
    [Range(8, 128)]
    public int segmentCount = 48;

    [Header("材质（已自动预设）")]
    [Tooltip("默认会自动使用 Unity 内置的 Default UI Material。也可以拖入你自己的 UI 材质。")]
    public Material uiMaterial;

    [Header("自动生成的箭头对象")]
    [Tooltip("真正负责在 Canvas 上绘制箭身与箭头尖的 UI Graphic。首次挂载会自动创建。")]
    [SerializeField]
    private TimelineUICurvedArrowGraphic arrowGraphic;

    private RectTransform cachedRect;

    private readonly List<Vector2> fullPath = new List<Vector2>(129);
    private readonly List<float> accumulatedLengths = new List<float>(129);
    private readonly List<Vector2> visiblePath = new List<Vector2>(129);

    public TimelineUICurvedArrowGraphic ArrowGraphic => arrowGraphic;

    private void Reset()
    {
        cachedRect = GetComponent<RectTransform>();
        progress = 1f;
        disappearProgress = 0f;
        CreateDefaultSetup();
        RefreshGraphic();
    }

    private void OnEnable()
    {
        cachedRect = GetComponent<RectTransform>();
        EnsureDefaultSetup();
        RefreshGraphic();
    }

    private void LateUpdate()
    {
        EnsureDefaultSetup();
        RefreshGraphic();
    }

    private void OnValidate()
    {
        bodyWidth = Mathf.Max(0.1f, bodyWidth);
        headLength = Mathf.Max(0.1f, headLength);
        headWidth = Mathf.Max(0.1f, headWidth);
        segmentCount = Mathf.Clamp(segmentCount, 8, 128);

        if (cachedRect == null)
            cachedRect = GetComponent<RectTransform>();

        EnsureDefaultSetup();
        RefreshGraphic();
    }

    private void OnDidApplyAnimationProperties()
    {
        // Timeline / Animator 修改 Progress 后立即刷新。
        RefreshGraphic();
    }

    [ContextMenu("重建 / 补全默认 UI 箭头配置")]
    public void CreateDefaultSetup()
    {
        if (cachedRect == null)
            cachedRect = GetComponent<RectTransform>();

        if (startPoint == null)
            startPoint = CreateControlPoint("Arrow_Start", new Vector2(-180f, -180f));

        if (controlPointA == null)
            controlPointA = CreateControlPoint("Arrow_Control_A", new Vector2(-180f, -20f));

        if (controlPointB == null)
            controlPointB = CreateControlPoint("Arrow_Control_B", new Vector2(120f, 100f));

        if (endPoint == null)
            endPoint = CreateControlPoint("Arrow_End", new Vector2(180f, 220f));

        EnsureGraphicObject();
        EnsureMaterial();
        ApplyGraphicSettings();
    }

    [ContextMenu("恢复默认竖直 UI 路径")]
    public void ResetControlPointPositions()
    {
        EnsureDefaultSetup();

        SetAnchoredPosition(startPoint, new Vector2(-180f, -180f));
        SetAnchoredPosition(controlPointA, new Vector2(-180f, -20f));
        SetAnchoredPosition(controlPointB, new Vector2(120f, 100f));
        SetAnchoredPosition(endPoint, new Vector2(180f, 220f));

        RefreshGraphic();
    }

    [ContextMenu("隐藏箭头")]
    public void HideArrow()
    {
        disappearProgress = 1f;
        RefreshGraphic();
    }

    [ContextMenu("完整显示箭头")]
    public void ShowFullArrow()
    {
        progress = 1f;
        disappearProgress = 0f;
        RefreshGraphic();
    }

    private void EnsureDefaultSetup()
    {
        if (cachedRect == null)
            cachedRect = GetComponent<RectTransform>();

        if (startPoint == null)
            startPoint = FindRectTransform("Arrow_Start");
        if (controlPointA == null)
            controlPointA = FindRectTransform("Arrow_Control_A");
        if (controlPointB == null)
            controlPointB = FindRectTransform("Arrow_Control_B");
        if (endPoint == null)
            endPoint = FindRectTransform("Arrow_End");

        // 如果这是刚刚新加的组件，直接把整套预设补齐。
        if (startPoint == null || controlPointA == null || controlPointB == null || endPoint == null)
        {
            if (startPoint == null)
                startPoint = CreateControlPoint("Arrow_Start", new Vector2(-180f, -180f));
            if (controlPointA == null)
                controlPointA = CreateControlPoint("Arrow_Control_A", new Vector2(-180f, -20f));
            if (controlPointB == null)
                controlPointB = CreateControlPoint("Arrow_Control_B", new Vector2(120f, 100f));
            if (endPoint == null)
                endPoint = CreateControlPoint("Arrow_End", new Vector2(180f, 220f));
        }

        EnsureGraphicObject();
        EnsureMaterial();
        ApplyGraphicSettings();
    }

    private RectTransform CreateControlPoint(string objectName, Vector2 anchoredPosition)
    {
        RectTransform existing = FindRectTransform(objectName);
        if (existing != null)
            return existing;

        GameObject point = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = point.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(24f, 24f);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private RectTransform FindRectTransform(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child as RectTransform : null;
    }

    private static void SetAnchoredPosition(RectTransform target, Vector2 position)
    {
        if (target != null)
            target.anchoredPosition = position;
    }

    private void EnsureGraphicObject()
    {
        if (arrowGraphic == null)
        {
            Transform found = transform.Find("UI_Arrow_Graphic");
            if (found != null)
                arrowGraphic = found.GetComponent<TimelineUICurvedArrowGraphic>();
        }

        if (arrowGraphic == null)
        {
            GameObject graphicObject = new GameObject(
                "UI_Arrow_Graphic",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TimelineUICurvedArrowGraphic));

            RectTransform rect = graphicObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            arrowGraphic = graphicObject.GetComponent<TimelineUICurvedArrowGraphic>();
        }

        if (arrowGraphic != null)
        {
            arrowGraphic.SetOwner(this);
            arrowGraphic.raycastTarget = false;
            arrowGraphic.maskable = true;

            // 放到本 Root 的最后一个子级，保证箭头本身在这套 UI 里最后绘制。
            arrowGraphic.rectTransform.SetAsLastSibling();
        }
    }

    private void EnsureMaterial()
    {
        if (uiMaterial == null)
            uiMaterial = Graphic.defaultGraphicMaterial;
    }

    private void ApplyGraphicSettings()
    {
        if (arrowGraphic == null)
            return;

        arrowGraphic.SetOwner(this);
        arrowGraphic.color = Color.white;
        arrowGraphic.raycastTarget = false;

        // uiMaterial 默认就是 Unity 内置 Default UI Material，因此 Inspector 不会是空材质。
        if (uiMaterial != null && arrowGraphic.material != uiMaterial)
            arrowGraphic.material = uiMaterial;
    }

    private void RefreshGraphic()
    {
        if (arrowGraphic == null)
            return;

        ApplyGraphicSettings();
        arrowGraphic.SetVerticesDirty();
        arrowGraphic.SetMaterialDirty();
    }

    internal void BuildGraphicMesh(VertexHelper vh, RectTransform graphicRect)
    {
        vh.Clear();

        if (graphicRect == null || endPoint == null)
            return;

        float clampedProgress = Mathf.Clamp01(progress);
        float clampedDisappear = Mathf.Clamp01(disappearProgress);

        if (clampedProgress <= 0.0001f)
            return;

        Vector2 p0 = ToGraphicLocal(startPoint != null ? startPoint : cachedRect, graphicRect);
        Vector2 p1 = ToGraphicLocal(controlPointA != null ? controlPointA : startPoint, graphicRect);
        Vector2 p2 = ToGraphicLocal(controlPointB != null ? controlPointB : endPoint, graphicRect);
        Vector2 p3 = ToGraphicLocal(endPoint, graphicRect);

        BuildFullPath(p0, p1, p2, p3);
        if (fullPath.Count < 2)
            return;

        float totalLength = accumulatedLengths[accumulatedLengths.Count - 1];
        if (totalLength <= 0.0001f)
            return;

        float wantedLength = totalLength * clampedProgress;
        float tailLength = totalLength * clampedDisappear;

        if (tailLength >= wantedLength - 0.0001f)
            return;

        BuildVisiblePath(tailLength, wantedLength);
        if (visiblePath.Count < 2)
            return;

        Vector2 tip = visiblePath[visiblePath.Count - 1];
        Vector2 previous = visiblePath[visiblePath.Count - 2];
        Vector2 direction = tip - previous;

        if (direction.sqrMagnitude <= 0.000001f)
            return;

        direction.Normalize();

        float remainingLength = wantedLength - tailLength;
        float headScale = Mathf.Clamp01(remainingLength / Mathf.Max(headLength * 1.4f, 0.001f));
        float currentHeadLength = headLength * headScale;
        float currentHeadWidth = headWidth * headScale;
        Vector2 headBase = tip - direction * currentHeadLength;

        TrimVisiblePathToHeadBase(headBase, direction);

        AddBodyMesh(vh);
        AddHeadMesh(vh, tip, headBase, direction, currentHeadWidth);
    }

    private Vector2 ToGraphicLocal(RectTransform point, RectTransform graphicRect)
    {
        if (point == null)
            return Vector2.zero;

        Vector3 local = graphicRect.InverseTransformPoint(point.position);
        return new Vector2(local.x, local.y);
    }

    private void BuildFullPath(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        fullPath.Clear();
        accumulatedLengths.Clear();

        float accumulated = 0f;
        Vector2 previous = p0;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector2 point = CubicBezier(p0, p1, p2, p3, t);

            if (i > 0)
                accumulated += Vector2.Distance(previous, point);

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

        Vector2 endPointOnPath = GetPointAtLength(endLength);
        if (visiblePath.Count == 0 || Vector2.Distance(visiblePath[visiblePath.Count - 1], endPointOnPath) > 0.0001f)
            visiblePath.Add(endPointOnPath);
    }

    private Vector2 GetPointAtLength(float wantedLength)
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
            return Vector2.Lerp(fullPath[i - 1], fullPath[i], segmentT);
        }

        return fullPath[fullPath.Count - 1];
    }

    private void TrimVisiblePathToHeadBase(Vector2 headBase, Vector2 direction)
    {
        while (visiblePath.Count > 2)
        {
            Vector2 candidate = visiblePath[visiblePath.Count - 2];
            if (Vector2.Dot(candidate - headBase, direction) <= 0f)
                break;

            visiblePath.RemoveAt(visiblePath.Count - 1);
        }

        if (visiblePath.Count >= 2)
            visiblePath[visiblePath.Count - 1] = headBase;
    }

    private void AddBodyMesh(VertexHelper vh)
    {
        if (visiblePath.Count < 2)
            return;

        int baseVertex = vh.currentVertCount;
        float halfWidth = bodyWidth * 0.5f;

        float totalVisibleLength = 0f;
        for (int i = 1; i < visiblePath.Count; i++)
            totalVisibleLength += Vector2.Distance(visiblePath[i - 1], visiblePath[i]);

        float traveled = 0f;

        for (int i = 0; i < visiblePath.Count; i++)
        {
            Vector2 tangent;
            if (i == 0)
                tangent = visiblePath[1] - visiblePath[0];
            else if (i == visiblePath.Count - 1)
                tangent = visiblePath[i] - visiblePath[i - 1];
            else
                tangent = visiblePath[i + 1] - visiblePath[i - 1];

            if (tangent.sqrMagnitude <= 0.000001f)
                tangent = Vector2.up;
            else
                tangent.Normalize();

            Vector2 normal = new Vector2(-tangent.y, tangent.x) * halfWidth;
            Vector2 left = visiblePath[i] + normal;
            Vector2 right = visiblePath[i] - normal;

            if (i > 0)
                traveled += Vector2.Distance(visiblePath[i - 1], visiblePath[i]);

            float v = totalVisibleLength > 0.0001f ? traveled / totalVisibleLength : 0f;

            AddVertex(vh, left, bodyColor, new Vector2(0f, v));
            AddVertex(vh, right, bodyColor, new Vector2(1f, v));
        }

        for (int i = 0; i < visiblePath.Count - 1; i++)
        {
            int i0 = baseVertex + i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            vh.AddTriangle(i0, i2, i1);
            vh.AddTriangle(i2, i3, i1);
        }
    }

    private void AddHeadMesh(VertexHelper vh, Vector2 tip, Vector2 headBase, Vector2 direction, float currentHeadWidth)
    {
        if (currentHeadWidth <= 0.0001f)
            return;

        Vector2 side = new Vector2(-direction.y, direction.x).normalized * (currentHeadWidth * 0.5f);
        Vector2 left = headBase + side;
        Vector2 right = headBase - side;

        int baseVertex = vh.currentVertCount;
        AddVertex(vh, tip, headColor, new Vector2(0.5f, 1f));
        AddVertex(vh, left, headColor, Vector2.zero);
        AddVertex(vh, right, headColor, Vector2.right);

        vh.AddTriangle(baseVertex, baseVertex + 1, baseVertex + 2);
    }

    private static void AddVertex(VertexHelper vh, Vector2 position, Color color, Vector2 uv)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = new Vector3(position.x, position.y, 0f);
        vertex.color = color;
        vertex.uv0 = uv;
        vh.AddVert(vertex);
    }

    private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * oneMinusT * p0
             + 3f * oneMinusT * oneMinusT * t * p1
             + 3f * oneMinusT * t * t * p2
             + t * t * t * p3;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (startPoint == null || controlPointA == null || controlPointB == null || endPoint == null)
            return;

        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.DrawLine(startPoint.position, controlPointA.position);
        Gizmos.DrawLine(controlPointB.position, endPoint.position);

        float handleSize = HandleWorldSize();
        Gizmos.DrawWireSphere(startPoint.position, handleSize);
        Gizmos.DrawWireSphere(controlPointA.position, handleSize);
        Gizmos.DrawWireSphere(controlPointB.position, handleSize);
        Gizmos.DrawWireSphere(endPoint.position, handleSize);
    }

    private float HandleWorldSize()
    {
        if (cachedRect == null)
            cachedRect = GetComponent<RectTransform>();

        if (cachedRect == null)
            return 4f;

        float scale = Mathf.Max(cachedRect.lossyScale.x, cachedRect.lossyScale.y);
        return Mathf.Max(4f * scale, 0.01f);
    }
#endif
}

/// <summary>
/// 只负责把 TimelineUICurvedArrow 提供的几何数据真正画到 Canvas 上。
/// 一般不需要手动添加或修改它。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class TimelineUICurvedArrowGraphic : MaskableGraphic
{
    [SerializeField, HideInInspector]
    private TimelineUICurvedArrow owner;

    public void SetOwner(TimelineUICurvedArrow newOwner)
    {
        if (owner == newOwner)
            return;

        owner = newOwner;
        SetVerticesDirty();
        SetMaterialDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;

        if (owner == null)
            owner = GetComponentInParent<TimelineUICurvedArrow>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;

        if (owner == null)
            owner = GetComponentInParent<TimelineUICurvedArrow>();

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if (owner == null)
            owner = GetComponentInParent<TimelineUICurvedArrow>();

        if (owner == null)
        {
            vh.Clear();
            return;
        }

        owner.BuildGraphicMesh(vh, rectTransform);
    }
}


#if UNITY_EDITOR
/// <summary>
/// Unity 顶部菜单：GameObject > UI > Timeline UI Curved Arrow
/// 一键创建已经配置好的 UI 弯曲箭头，不需要先手动搭空父级。
/// </summary>
public static class TimelineUICurvedArrowEditorCreator
{
    [UnityEditor.MenuItem("GameObject/UI/Timeline UI Curved Arrow", false, 2035)]
    private static void CreateTimelineUICurvedArrow(UnityEditor.MenuCommand menuCommand)
    {
        GameObject root = new GameObject("Timeline_UI_Curved_Arrow", typeof(RectTransform));
        UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Create Timeline UI Curved Arrow");

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600f, 600f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        GameObject context = menuCommand != null ? menuCommand.context as GameObject : null;
        Transform parent = null;

        if (context != null)
        {
            RectTransform contextRect = context.GetComponent<RectTransform>();
            Canvas contextCanvas = context.GetComponentInParent<Canvas>();
            if (contextRect != null || contextCanvas != null)
                parent = context.transform;
        }
        else if (UnityEditor.Selection.activeGameObject != null)
        {
            GameObject selected = UnityEditor.Selection.activeGameObject;
            RectTransform selectedRect = selected.GetComponent<RectTransform>();
            Canvas selectedCanvas = selected.GetComponentInParent<Canvas>();
            if (selectedRect != null || selectedCanvas != null)
                parent = selected.transform;
        }

        // 没有选中 UI 时，优先放进场景中已有 Canvas；如果场景没有 Canvas，就自动建一个。
        if (parent == null)
        {
            Canvas existingCanvas = Object.FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                parent = existingCanvas.transform;
            }
            else
            {
                GameObject canvasObject = new GameObject(
                    "Canvas_UI_Arrow",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                UnityEditor.Undo.RegisterCreatedObjectUndo(canvasObject, "Create UI Arrow Canvas");
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
                parent = canvasObject.transform;
            }
        }

        UnityEditor.Undo.SetTransformParent(root.transform, parent, "Parent Timeline UI Curved Arrow");
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = Vector2.zero;

        TimelineUICurvedArrow arrow = UnityEditor.Undo.AddComponent<TimelineUICurvedArrow>(root);
        arrow.CreateDefaultSetup();
        arrow.ShowFullArrow();

        UnityEditor.Selection.activeGameObject = root;
        UnityEditor.EditorGUIUtility.PingObject(root);
    }
}
#endif
