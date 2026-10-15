using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class UICoinSpawner : MonoBehaviour
{
    public enum ScatterShape { 圆形, 方形或矩形, 椭圆, 圆环, 扇形, 图片蒙版 }

    [Header("UI 发射器设置")]
    [Tooltip("拖入带 RectTransform 和 UICoin 的金币预制体。弹跳高度、飞行时间在预制体的 UICoin 上调。")]
    public GameObject 金币预制体;
    [Tooltip("金币生成到哪个 UI 容器下面。建议指定同一个 Canvas 中的空 RectTransform，缩放为 1，避开 Layout Group。留空使用本物体。")]
    public Transform 金币父容器;
    [Tooltip("这次播放一共生成多少枚金币。建议 15～50；0 表示不生成。")]
    [Min(0)] public int 爆出总数量 = 15;
    [Tooltip("每一批同时生成几枚。建议 5～10；等于总数量时一次全部爆出。运行时至少按 1 处理。")]
    [Min(1)] public int 每批生成数量 = 5;
    [Tooltip("两批之间的间隔，单位秒。建议 0.1～0.5；0 表示全部在同一帧生成。")]
    [Min(0f)] public float 批次间隔时间 = 0.5f;
    [Tooltip("每次 Play 调用后先等多久才开始生成，单位秒。建议 0；需要配合镜头时可设 0.2～1。")]
    [Min(0f)] public float 发射初始延迟 = 0f;
    [Tooltip("勾选后 Start 时自动播放一次。用按钮或 Timeline Signal 触发时取消勾选，调用 Play()。")]
    public bool 启动时自动播放 = true;
    [Tooltip("勾选后生成、弹跳、等待、旋转、吸收均忽略 Time.timeScale。暂停界面也要播金币时开启。")]
    public bool 使用不受暂停影响的时间 = false;

    [Header("吸收设置")]
    [Tooltip("金币最后飞到的 UI 目标。建议拖入同 Canvas 的金币图标。留空时弹跳和等待后直接销毁。")]
    public RectTransform 吸收目标UI;
    [Tooltip("所有弹跳结束后等待多少秒再吸收。建议 0.3～1。")]
    [Min(0f)] public float 落地后等待时间 = 1f;
    [Tooltip("为每枚金币额外随机增加 0～该值的等待时间。建议 0.1～0.4；0 表示同一批一起吸收。")]
    [Range(0f, 1f)] public float 错落感随机延迟 = 0.4f;
    [Tooltip("金币从原尺寸的 10% 放大到原尺寸所需秒数。建议 0.08～0.2；0 表示直接原尺寸。")]
    [Min(0f)] public float 弹出缩放耗时 = 0.08f;

    [Header("自转设置")]
    [Tooltip("局部自转轴。建议 (0,1,0) 做翻硬币；(0,0,1) 在 UI 面内转；(0,0,0) 不自转。")]
    public Vector3 自转轴 = new Vector3(0f, 1f, 0f);
    [Tooltip("每秒自转度数。建议 360～720；0 不旋转，负数反向。每枚金币会有 ±20% 的速度差异。")]
    public float 自转速度 = 720f;

    [Header("散落范围：控制首次弹跳后的落点")]
    [Tooltip("圆形用半径；矩形和椭圆用宽高；圆环用内外半径；扇形用半径与角度；图片蒙版用透明度筛选落点。弹跳与吸收路径不受此范围裁切。")]
    public ScatterShape 散落形状 = ScatterShape.圆形;
    [Tooltip("圆形、圆环、扇形的外半径。UI 模式下为父容器的 UI 单位。建议 100～200；150 的直径为 300。")]
    [Min(0f)] public float 散落范围半径 = 150f;
    [Tooltip("矩形、椭圆、图片蒙版的完整宽高，不是半径。建议 (300,180)；方形设 (300,300)。")]
    public Vector2 范围宽高 = new Vector2(300f, 300f);
    [Tooltip("落点范围中心相对爆出点的偏移。建议 (0,0)；(0,-80) 表示落点整体下移。金币仍从本物体位置爆出。")]
    public Vector2 范围中心偏移 = Vector2.zero;
    [Tooltip("把落点形状在 UI 平面内逆时针旋转几度。建议 0；矩形设 45 可变成菱形。")]
    public float 范围旋转角度 = 0f;
    [Tooltip("可选：用一个 RectTransform 的中心、方向、缩放定义范围。矩形、椭圆、图片模式直接使用它的 rect 宽高，忽略范围宽高。圆形/圆环/扇形仍用半径。留空以爆出点为中心、父容器为坐标系。")]
    public RectTransform 范围参考UI;
    [Tooltip("建议开启：半径、宽高和弹跳高度按 UI 单位计算，随 Canvas 缩放。关闭使用旧版世界坐标；关闭时范围参考UI不生效。")]
    public bool 使用UI坐标 = true;

    [Header("圆环 / 扇形（仅对应形状生效）")]
    [Tooltip("圆环的内半径，中间不会产生落点。建议外半径 150、内半径 80；运行时限制为不超过外半径。")]
    [Min(0f)] public float 圆环内半径 = 80f;
    [Tooltip("扇形展开角度。建议 180 做半圆，90 做四分之一圆，360 做整圆。")]
    [Range(0f, 360f)] public float 扇形张角 = 180f;
    [Tooltip("扇形朝向：0 向右、90 向上、180 向左、270 向下。建议向上半圆用 90；另外叠加范围旋转角度。")]
    public float 扇形朝向角 = 90f;

    [Header("图片蒙版（仅图片蒙版形状生效）")]
    [Tooltip("拖入带透明背景的 PNG 纹理：不透明部分允许落点，透明部分排除；颜色不影响范围。需在图片导入设置开启 Read/Write Enabled 后 Apply。建议 128～512 像素、Compression=None。使用原纹理整体，非 Sprite 图集裁切区域；无需添加 UI Mask 组件。")]
    public Texture2D 范围蒙版图片;
    [Tooltip("允许落点的最低透明度。建议 0.5；提高可去掉淡边缘。只按透明度，不按黑白亮度；纯白背景图会形成完整矩形。")]
    [Range(0.01f, 1f)] public float 蒙版透明度阈值 = 0.5f;

    [Header("Scene 范围预览")]
    [Tooltip("选中本物体时显示黄色边界，需要 Scene 视图 Gizmos 开启。图片模式仅预览外框，实际落点按图片透明度。")]
    public bool 显示范围预览 = true;

    [Header("Game 视图预览（仅编辑状态，运行时自动隐藏）")]
    [Tooltip("不进入 Play 就在 Game 视图显示范围边界和爆出点十字。需要本物体在 Canvas 下；进入运行自动移除，预览不会保存进场景或打包。")]
    public bool 编辑时Game显示范围 = true;
    [Tooltip("Game 视图边界颜色。建议黄色，透明度 0.8～1。图片蒙版同时显示低透明度填充。")]
    public Color Game预览颜色 = new Color(1f, 0.85f, 0f, 0.9f);
    [Tooltip("Game 预览边线宽度，以所在 Canvas 的 UI 单位计。建议 2～4，不影响金币。")]
    [Min(0.5f)] public float Game预览线宽 = 2f;

    private Coroutine spawnRoutine;
    private Texture2D cachedMask;
    private float cachedThreshold = -1f;
    private int maskWidth;
    private int maskHeight;
    private readonly List<int> allowedPixels = new List<int>();

    private void Start() { if (Application.isPlaying && 启动时自动播放) Play(); }
    private void OnDisable()
    {
        StopSpawning();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= UpdateGamePreview;
        DestroyGamePreview();
#endif
    }

#if UNITY_EDITOR
    private GameObject gamePreview;
    private UICoinRangePreview previewOutline;
    private UICoinRangePreview previewFill;
    private int previewSignature;
    private bool hasPreviewSignature;

    private void OnEnable()
    {
        UnityEditor.EditorApplication.update -= UpdateGamePreview;
        UnityEditor.EditorApplication.update += UpdateGamePreview;
    }

    private void OnDestroy()
    {
        UnityEditor.EditorApplication.update -= UpdateGamePreview;
        DestroyGamePreview();
    }

    private void UpdateGamePreview()
    {
        if (this == null) return;
        if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
            || !isActiveAndEnabled || !编辑时Game显示范围 || UnityEditor.EditorUtility.IsPersistent(gameObject))
        {
            DestroyGamePreview();
            return;
        }
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { DestroyGamePreview(); return; }
        if (gamePreview != null && gamePreview.transform.parent != canvas.transform) DestroyGamePreview();
        if (gamePreview == null)
        {
            gamePreview = new GameObject("UICoin 编辑预览（临时）", typeof(RectTransform), typeof(CanvasRenderer), typeof(UICoinRangePreview));
            gamePreview.hideFlags = HideFlags.HideAndDontSave;
            RectTransform rect = (RectTransform)gamePreview.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            previewOutline = gamePreview.GetComponent<UICoinRangePreview>();
            previewOutline.owner = this;
            previewOutline.raycastTarget = false;
            GameObject fill = new GameObject("图片蒙版填充（临时）", typeof(RectTransform), typeof(CanvasRenderer), typeof(UICoinRangePreview));
            fill.hideFlags = HideFlags.HideAndDontSave;
            RectTransform fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(rect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            previewFill = fill.GetComponent<UICoinRangePreview>();
            previewFill.owner = this;
            previewFill.maskFill = true;
            previewFill.raycastTarget = false;
        }
        int signature = GetPreviewSignature(canvas);
        if (hasPreviewSignature && signature == previewSignature) return;
        hasPreviewSignature = true;
        previewSignature = signature;
        // 临时预览直接放 Canvas 顶层以免被 Mask 裁切；只有参数变化时重建图形。
        previewOutline.Refresh();
        previewFill.Refresh();
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }

    private int GetPreviewSignature(Canvas canvas)
    {
        unchecked
        {
            int hash = GetAreaMatrix().GetHashCode();
            hash = hash * 31 + canvas.transform.localToWorldMatrix.GetHashCode();
            hash = hash * 31 + transform.position.GetHashCode();
            hash = hash * 31 + GetSize().GetHashCode();
            hash = hash * 31 + (int)散落形状;
            hash = hash * 31 + 散落范围半径.GetHashCode();
            hash = hash * 31 + 圆环内半径.GetHashCode();
            hash = hash * 31 + 扇形张角.GetHashCode();
            hash = hash * 31 + 扇形朝向角.GetHashCode();
            hash = hash * 31 + Game预览颜色.GetHashCode();
            hash = hash * 31 + Game预览线宽.GetHashCode();
            hash = hash * 31 + (范围蒙版图片 != null ? 范围蒙版图片.GetInstanceID() : 0);
            return hash;
        }
    }

    private void DestroyGamePreview()
    {
        if (gamePreview != null) DestroyImmediate(gamePreview);
        gamePreview = null;
        previewOutline = null;
        previewFill = null;
        hasPreviewSignature = false;
    }

    // 仅供编辑预览读取，与实际落点共用相同坐标变换。
    public Matrix4x4 EditorPreviewAreaMatrix() { return GetAreaMatrix(); }
    public Vector2 EditorPreviewSize() { return GetSize(); }
#endif

    /// <summary>按钮或 Timeline Signal 可调用；重启生成批次，已爆出的金币继续运动。</summary>
    public void Play()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        StopSpawning();
        if (金币预制体 == null || 金币预制体.GetComponent<UICoin>() == null)
        {
            Debug.LogWarning("UICoinSpawner：请配置带 UICoin 的金币预制体。", this);
            return;
        }
        if (金币父容器 == null) 金币父容器 = transform;
        if (散落形状 == ScatterShape.图片蒙版 && !PrepareMask()) return;
        // 先 yield 一帧，避免同步完成的协程留下已经结束的 Coroutine 句柄。
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>停止后续生成；已经爆出的金币继续完成弹跳和吸收。</summary>
    public void StopSpawning()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    /// <summary>运行时修改了同一张纹理内容后调用，下次 Play 会重新读取透明度。</summary>
    public void RefreshMaskCache() { cachedMask = null; cachedThreshold = -1f; allowedPixels.Clear(); }

    private bool PrepareMask()
    {
        float threshold = Mathf.Clamp(蒙版透明度阈值, 0.01f, 1f);
        if (范围蒙版图片 == cachedMask && Mathf.Approximately(threshold, cachedThreshold)
            && allowedPixels.Count > 0) return true;
        RefreshMaskCache();
        if (范围蒙版图片 == null || !范围蒙版图片.isReadable)
        {
            Debug.LogWarning("UICoinSpawner：图片蒙版需要纹理，且导入设置必须开启 Read/Write Enabled 并 Apply。本次不生成金币。", this);
            return false;
        }
        try
        {
            Color32[] pixels = 范围蒙版图片.GetPixels32();
            maskWidth = 范围蒙版图片.width;
            maskHeight = 范围蒙版图片.height;
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].a / 255f >= threshold) allowedPixels.Add(i);
        }
        catch (System.Exception exception)
        {
            RefreshMaskCache();
            Debug.LogWarning("UICoinSpawner：无法读取蒙版，请尝试 Compression=None 后 Apply。" + exception.Message, this);
            return false;
        }
        if (allowedPixels.Count == 0)
        {
            Debug.LogWarning("UICoinSpawner：蒙版没有满足透明度阈值的像素，请降低阈值或检查图片。本次不生成金币。", this);
            return false;
        }
        cachedMask = 范围蒙版图片;
        cachedThreshold = threshold;
        return true;
    }

    private IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0f, duration))
        {
            elapsed += 使用不受暂停影响的时间 ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        yield return null;
        yield return Wait(发射初始延迟);
        int total = Mathf.Max(0, 爆出总数量);
        int spawned = 0;
        while (spawned < total)
        {
            // 允许运行中切换形状/纹理，失败时不静默退回其他范围。
            if (散落形状 == ScatterShape.图片蒙版 && !PrepareMask()) break;
            int count = Mathf.Min(Mathf.Max(1, 每批生成数量), total - spawned);
            SpawnCoinsBatch(count);
            spawned += count;
            if (spawned < total) yield return Wait(批次间隔时间);
        }
        spawnRoutine = null;
    }

    private Vector2 GetSize()
    {
        Vector2 size = 使用UI坐标 && 范围参考UI != null ? 范围参考UI.rect.size : 范围宽高;
        return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    private Vector2 SampleOffset()
    {
        float radius = Mathf.Max(0f, 散落范围半径);
        Vector2 size = GetSize();
        switch (散落形状)
        {
            case ScatterShape.方形或矩形:
                return new Vector2(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.5f, 0.5f) * size.y);
            case ScatterShape.椭圆:
                Vector2 disk = Random.insideUnitCircle;
                return new Vector2(disk.x * size.x * 0.5f, disk.y * size.y * 0.5f);
            case ScatterShape.圆环:
                float inner = Mathf.Clamp(圆环内半径, 0f, radius);
                float ringRadius = Mathf.Sqrt(Mathf.Lerp(inner * inner, radius * radius, Random.value));
                return Direction(Random.Range(0f, 360f)) * ringRadius;
            case ScatterShape.扇形:
                float angle = Mathf.Clamp(扇形张角, 0f, 360f);
                return Direction(扇形朝向角 + Random.Range(-angle * 0.5f, angle * 0.5f)) * (Mathf.Sqrt(Random.value) * radius);
            case ScatterShape.图片蒙版:
                // 直接从有效像素中均匀取样，不用随机试错，稀疏蒙版也不会卡顿或越界回退。
                int pixel = allowedPixels[Random.Range(0, allowedPixels.Count)];
                // 使用像素中心，确保取样点属于已通过阈值的像素。
                return new Vector2(((pixel % maskWidth + 0.5f) / maskWidth - 0.5f) * size.x,
                    ((pixel / maskWidth + 0.5f) / maskHeight - 0.5f) * size.y);
            default:
                return Random.insideUnitCircle * radius;
        }
    }

    private static Vector2 Direction(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private Matrix4x4 GetAreaMatrix()
    {
        if (!使用UI坐标)
            return Matrix4x4.TRS(transform.position + (Vector3)范围中心偏移,
                Quaternion.Euler(0f, 0f, 范围旋转角度), Vector3.one);
        if (范围参考UI != null)
            return 范围参考UI.localToWorldMatrix * Matrix4x4.TRS(
                (Vector3)(范围参考UI.rect.center + 范围中心偏移), Quaternion.Euler(0f, 0f, 范围旋转角度), Vector3.one);
        Transform space = 金币父容器 != null ? 金币父容器 : transform;
        Vector3 center = space.InverseTransformPoint(transform.position) + (Vector3)范围中心偏移;
        return space.localToWorldMatrix * Matrix4x4.TRS(center,
            Quaternion.Euler(0f, 0f, 范围旋转角度), Vector3.one);
    }

    private void SpawnCoinsBatch(int count)
    {
        Transform space = 金币父容器 != null ? 金币父容器 : transform;
        Matrix4x4 area = GetAreaMatrix();
        for (int i = 0; i < count; i++)
        {
            Vector3 groundWorld = area.MultiplyPoint3x4((Vector3)SampleOffset());
            // 保留预制体原始缩放，以父容器的 UI 平面摆放金币。
            GameObject coin = Instantiate(金币预制体, space, false);
            coin.transform.position = transform.position;
            coin.transform.localRotation = 金币预制体.transform.localRotation;
            UICoin coinScript = coin.GetComponent<UICoin>();
            float finalDelay = Mathf.Max(0f, 落地后等待时间) + Random.Range(0f, Mathf.Max(0f, 错落感随机延迟));
            if (使用UI坐标)
                coinScript.InitializeInUIPlane(吸收目标UI, finalDelay, 弹出缩放耗时,
                    space.InverseTransformPoint(groundWorld), 自转轴, 自转速度, space, 使用不受暂停影响的时间);
            else
            {
                coinScript.useUnscaledTime = 使用不受暂停影响的时间;
                coinScript.Initialize(吸收目标UI, finalDelay, 弹出缩放耗时, groundWorld, 自转轴, 自转速度);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!显示范围预览) return;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = GetAreaMatrix();
        Gizmos.color = Color.yellow;
        Vector2 size = GetSize();
        float radius = Mathf.Max(0f, 散落范围半径);
        switch (散落形状)
        {
            case ScatterShape.方形或矩形:
            case ScatterShape.图片蒙版:
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0f));
                break;
            case ScatterShape.椭圆:
                DrawArc(size * 0.5f, 0f, 360f);
                break;
            case ScatterShape.圆环:
                DrawArc(Vector2.one * radius, 0f, 360f);
                DrawArc(Vector2.one * Mathf.Clamp(圆环内半径, 0f, radius), 0f, 360f);
                break;
            case ScatterShape.扇形:
                float angle = Mathf.Clamp(扇形张角, 0f, 360f);
                float start = 扇形朝向角 - angle * 0.5f;
                DrawArc(Vector2.one * radius, start, angle);
                if (angle < 360f)
                {
                    Gizmos.DrawLine(Vector3.zero, (Vector3)(Direction(start) * radius));
                    Gizmos.DrawLine(Vector3.zero, (Vector3)(Direction(start + angle) * radius));
                }
                break;
            default:
                DrawArc(Vector2.one * radius, 0f, 360f);
                break;
        }
        Gizmos.DrawLine(new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f));
        Gizmos.DrawLine(new Vector3(0f, -5f, 0f), new Vector3(0f, 5f, 0f));
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private static void DrawArc(Vector2 radii, float startDegrees, float sweepDegrees)
    {
        Vector2 previous = Vector2.Scale(Direction(startDegrees), radii);
        for (int i = 1; i <= 64; i++)
        {
            Vector2 next = Vector2.Scale(Direction(startDegrees + sweepDegrees * i / 64f), radii);
            Gizmos.DrawLine((Vector3)previous, (Vector3)next);
            previous = next;
        }
    }
}
