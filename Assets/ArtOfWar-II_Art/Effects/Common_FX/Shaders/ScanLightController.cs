using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class ScanLightController : MonoBehaviour
{
    [Header("预览")]
    [SerializeField] private bool preview;
    
    [Header("扫光目标")]
    [SerializeField] private RectTransform scanImage;
    [SerializeField] private Image scanImageComponent;

    [Header("扫光设置")]
    [SerializeField] private float moveDistance = 500f;   // 移动距离（沿方向向量延伸的长度）
    [SerializeField] private float duration = 2f;
    [SerializeField] private float angle = 45f;           // 旋转角度（视觉上的旋转）
    [SerializeField] private float directionAngle = 0f;   // 扫光方向（角度，0 = 向右，90 = 向上）
    [SerializeField] private float width = 1f;
    [SerializeField] private float height = 1f;
    [SerializeField] private Color color = Color.white;

    [Header("透明度渐变")]
    [Range(0f, 1f)] [SerializeField] private float startAlpha = 0.2f;   // 开始透明度
    [Range(0f, 1f)] [SerializeField] private float endAlpha = 1f;       // 结束透明度
    
    [Header(("移动缓动曲线"))]
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("透明度缓动曲线")]
    [SerializeField] private AnimationCurve alphaEase = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("控制选项")]
    [SerializeField] private bool loop = true;
    [SerializeField] private bool autoPlay = true;

    private Tween moveTween;
    private Tween fadeTween;

#if UNITY_EDITOR
    // 编辑器运行时的预览方法，正式环境下不需要触发
    void OnValidate()
    {
        if (!preview)
        {
            return;
        }
        
        if (Application.isPlaying && autoPlay)
        {
            PlayScan();
        }
    }
#endif

    public void PlayScan()
    {
        StopScan();
        ApplyStaticParams();

        // 根据 directionAngle 计算方向单位向量
        Vector2 dir = new Vector2(Mathf.Cos(directionAngle * Mathf.Deg2Rad), Mathf.Sin(directionAngle * Mathf.Deg2Rad)).normalized;

        // 起点和终点
        Vector2 startPos = -dir * moveDistance / 2f;
        Vector2 endPos = dir * moveDistance / 2f;

        scanImage.anchoredPosition = startPos;

        // 同时做位置动画 + 透明度渐变动画
        moveTween = scanImage.DOAnchorPos(endPos, duration)
            .SetEase(moveEase)
            .SetLoops(loop ? -1 : 0, LoopType.Restart);

        if (scanImageComponent != null)
        {
            Color c = color;
            c.a = startAlpha;
            scanImageComponent.color = c;

            fadeTween = scanImageComponent.DOFade(endAlpha, duration)
                .SetEase(alphaEase)
                .SetLoops(loop ? -1 : 0, LoopType.Restart);
        }
    }

    public void StopScan()
    {
        moveTween?.Kill();
        fadeTween?.Kill();
    }

    private void ApplyStaticParams()
    {
        if (scanImage == null)
        {
            return;
        }

        scanImage.localRotation = Quaternion.Euler(0, 0, angle);
        scanImage.localScale = new Vector3(width, height, 1);
    }

    private void SetToInitialState()
    {
        if (scanImage == null)
        {
            return;
        }

        Vector2 dir = new Vector2(Mathf.Cos(directionAngle * Mathf.Deg2Rad), Mathf.Sin(directionAngle * Mathf.Deg2Rad)).normalized;
        Vector2 startPos = -dir * moveDistance / 2f;
        scanImage.anchoredPosition = startPos;

        if (scanImageComponent != null)
        {
            Color c = color;
            c.a = startAlpha;
            scanImageComponent.color = c;
        }
    }

    private void OnDisable()
    {
        StopScan();
    }
    
    private void OnEnable()
    {
        ApplyStaticParams();
        SetToInitialState();
        
        if (autoPlay)
        {
            PlayScan();
        }
    }

    private void OnDestroy()
    {
        StopScan(); // 停止动画
    }
}
