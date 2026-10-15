using UnityEngine;

[ExecuteAlways]
public class SpriteGroupGrayscale : MonoBehaviour
{
    [Header("【把刚刚生成的精灵材质球拖到这里】")]
    public Material spriteGrayMaterial;

    [Header("数字控制量 (0 = 黑白, 1 = 彩色)")]
    [Range(0f, 1f)]
    public float colorProgress = 1f;

    private float lastProgress = -1f;
    private MaterialPropertyBlock m_PropBlock;

    // 缓存 Shader 内部控制去色比例的变量 ID
    private static readonly int EffectAmountId = Shader.PropertyToID("_EffectAmount");

    void Update()
    {
        if (!Mathf.Approximately(colorProgress, lastProgress))
        {
            SetSpriteGroupColor(colorProgress);
            lastProgress = colorProgress;
        }
    }

    public void SetSpriteGroupColor(float value)
    {
        colorProgress = Mathf.Clamp01(value);

        if (spriteGrayMaterial == null) return;
        if (m_PropBlock == null) m_PropBlock = new MaterialPropertyBlock();

        // 1. 抓取自己和所有子物体身上的 SpriteRenderer
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
            {
                // 2. 【核心修复】：首次使用时，自动把普通的默认材质替换为我们新写的去色材质
                if (sr.sharedMaterial != spriteGrayMaterial)
                {
                    sr.sharedMaterial = spriteGrayMaterial;
                }

                // 3. 独立控制：利用属性块控制当前这组卡片的黑白程度
                sr.GetPropertyBlock(m_PropBlock);
                m_PropBlock.SetFloat(EffectAmountId, colorProgress);
                sr.SetPropertyBlock(m_PropBlock);

#if UNITY_EDITOR
                if (!Application.isPlaying) { UnityEditor.EditorUtility.SetDirty(sr); }
#endif
            }
        }
    }
}