using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class CanvasGroupGrayscale : MonoBehaviour
{
    [Header("【把刚刚生成的材质球拖到这里】")]
    public Material grayScaleMaterial;

    [Header("数字控制量 (0 = 黑白, 1 = 彩色)")]
    [Range(0f, 1f)]
    public float colorProgress = 1f;

    // 内部私有变量：用来存放这组卡片“独占”的材质实例
    private Material m_UniqueMaterial;
    private float lastProgress = -1f;

    void Update()
    {
        // 只有当数字发生变化时才刷新
        if (!Mathf.Approximately(colorProgress, lastProgress))
        {
            SetColorValue(colorProgress);
            lastProgress = colorProgress;
        }
    }

    /// <summary>
    /// 初始化并确保获得唯一的独立材质
    /// </summary>
    private void CheckAndCreateUniqueMaterial()
    {
        if (grayScaleMaterial == null) return;

        // 如果独立材质还没创建，或者在编辑器下材质丢失了，就克隆一个临时的独立材质
        if (m_UniqueMaterial == null)
        {
            // 核心克隆逻辑：这样每一组卡就会在内存里拥有自己独立的 Shader 变量副本，不再共享
            m_UniqueMaterial = new Material(grayScaleMaterial.shader);
            m_UniqueMaterial.name = gameObject.name + "_Unique_GrayMaterial";
            m_UniqueMaterial.hideFlags = HideFlags.DontSave; // 防止在编辑器里留下垃圾文件
        }
    }

    public void SetColorValue(float value)
    {
        colorProgress = Mathf.Clamp01(value);

        // 确保独立材质就位
        CheckAndCreateUniqueMaterial();

        if (m_UniqueMaterial == null)
            return;

        // 【核心修改】：只修改这组卡独占的材质，不影响硬盘上的原始文件
        m_UniqueMaterial.SetFloat("_EffectAmount", colorProgress);

        // 刷新自己和所有子物体的图片材质
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            if (img != null && img.material != m_UniqueMaterial)
            {
                // 将克隆出来的独特材质赋给子物体
                img.material = m_UniqueMaterial;

#if UNITY_EDITOR
                if (!Application.isPlaying) { UnityEditor.EditorUtility.SetDirty(img); }
#endif
            }
        }

        // 处理文字 Text (TMP) 的去色
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI txt in allTexts)
        {
            if (txt != null)
            {
                float luma = 0.299f * txt.color.r + 0.587f * txt.color.g + 0.114f * txt.color.b;
                Color grayColor = new Color(luma, luma, luma, txt.color.a);
                Color originalColor = new Color(1f, 1f, 1f, txt.color.a);

                txt.color = Color.Lerp(grayColor, originalColor, colorProgress);

#if UNITY_EDITOR
                if (!Application.isPlaying) { UnityEditor.EditorUtility.SetDirty(txt); }
#endif
            }
        }
    }

    // 防止内存泄漏，销毁时自动清理生成的材质实例
    private void OnDestroy()
    {
        if (m_UniqueMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(m_UniqueMaterial);
            else
                DestroyImmediate(m_UniqueMaterial);
        }
    }
}