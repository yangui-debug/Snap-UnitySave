using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;

    [Header("外观与颜色配置")]
    public Color normalColor = Color.white;
    public Color critColor = Color.yellow;
    private float normalScale = 1.0f;
    private float critScale = 1.2f;

    [Header("动画节奏配置")]
    [Range(0.05f, 0.3f)] public float popInDuration = 0.15f;    // 缩放出现时间
    [Range(0f, 1.0f)] public float stayDuration = 0.5f;       // 停顿时间
    [Range(0.1f, 1.0f)] public float fadeDuration = 0.3f;       // 淡出时间

    [Header("漂浮高度配置")]
    [Range(0.1f, 5.0f)] public float popRiseDistance = 0.3f;    // 刚出现时向上冲的高度
    [Range(0.01f, 0.2f)] public float stayLingerDistance = 0.1f;// 停顿期间极慢漂浮的距离
    [Range(0.1f, 5.0f)] public float fadeFloatOutDistance = 0.4f;// 淡出时继续飘散的距离

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();

        // 【关键修复】：直接通过代码强行修改材质参数，无视 Inspector 里的限制！
        // 数字 8 代表 UnityEngine.Rendering.CompareFunction.Always (永远在最前面)
        if (textMesh != null && textMesh.fontMaterial != null)
        {
            textMesh.fontMaterial.SetFloat("_ZTestMode", 8);
        }
    }

    /// <summary>
    /// 初始化数值范围和是否暴击
    /// </summary>
    public void Setup(int min, int max, int critThreshold)
    {
        int amount = Random.Range(min, max + 1);
        textMesh.text = amount.ToString();

        bool isCrit = amount >= critThreshold;
        textMesh.color = isCrit ? critColor : normalColor;
        float targetScaleValue = isCrit ? critScale : normalScale;

        StartCoroutine(PlayEnhancedAnimation(targetScaleValue));
    }

    private IEnumerator PlayEnhancedAnimation(float targetScale)
    {
        Vector3 spawnPos = transform.position;
        Color startColor = textMesh.color;
        transform.localScale = Vector3.zero;

        float elapsed = 0;

        // --- Phase 1: 缩放并向上弹出 ---
        while (elapsed < popInDuration)
        {
            float t = elapsed / popInDuration;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * targetScale, t);
            transform.position = spawnPos + Vector3.up * (popRiseDistance * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one * targetScale;
        Vector3 phase1EndPos = spawnPos + Vector3.up * popRiseDistance;
        transform.position = phase1EndPos;

        // --- Phase 2: 停顿并轻微漂浮 ---
        elapsed = 0;
        while (elapsed < stayDuration)
        {
            float t = elapsed / stayDuration;
            transform.position = phase1EndPos + Vector3.up * (stayLingerDistance * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Vector3 phase2EndPos = phase1EndPos + Vector3.up * stayLingerDistance;
        transform.position = phase2EndPos;

        // --- Phase 3: 淡出并飘散 ---
        elapsed = 0;
        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1.0f - t);
            transform.position = phase2EndPos + Vector3.up * (fadeFloatOutDistance * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 0);

        Destroy(gameObject);
    }

    private void Update()
    {
        // 永远朝向摄像机
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }
}