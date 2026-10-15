using UnityEngine;

[ExecuteAlways] // 确保在非运行模式下（比如拖拽 Timeline 进度条时）也能实时预览黑白/彩色
public class ToonSkeletonGroupGrayscale : MonoBehaviour
{
    [Header("数字控制量 (0 = 黑白, 1 = 彩色)")]
    [Range(0f, 1f)]
    public float colorProgress = 1f;

    private float lastProgress = -1f;
    private MaterialPropertyBlock m_PropBlock;

    // 核心硬核点：精准绑定你卡通材质面板上的两个颜色变量名
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int DarkColorPropertyId = Shader.PropertyToID("_DarkColor");

    void Update()
    {
        // 性能优化：只有当滑块数字发生改变时，才刷新显卡数据
        if (!Mathf.Approximately(colorProgress, lastProgress))
        {
            SetToonGroupColor(colorProgress);
            lastProgress = colorProgress;
        }
    }

    /// <summary>
    /// 卡通 Shader 专用独立数字变色控制函数
    /// </summary>
    /// <param name="value">0是完全黑白，1是完全恢复原本彩色</param>
    public void SetToonGroupColor(float value)
    {
        colorProgress = Mathf.Clamp01(value);

        if (m_PropBlock == null)
        {
            m_PropBlock = new MaterialPropertyBlock();
        }

        // 1. 顺藤摸瓜：自动抓取自己以及下面所有子物体身上的 MeshRenderer 渲染器
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        // 2. 遍历所有网格，实施显卡层面的独立颜色干预
        foreach (MeshRenderer mr in renderers)
        {
            if (mr != null && mr.sharedMaterial != null)
            {
                // 3. 抓取该网格当前的属性块（确保不破坏原材质的其他贴图和参数）
                mr.GetPropertyBlock(m_PropBlock);

                // 4. 读取原始材质面板上的初始预设颜色（避免把你的原图颜色配方写死在代码里）
                Color originalMainColor = mr.sharedMaterial.HasProperty(ColorPropertyId) ? mr.sharedMaterial.GetColor(ColorPropertyId) : Color.white;
                Color originalDarkColor = mr.sharedMaterial.HasProperty(DarkColorPropertyId) ? mr.sharedMaterial.GetColor(DarkColorPropertyId) : Color.gray;

                // 5. 核心去色数学计算：
                // 提取原始亮部和暗部颜色的真实光学亮度（Luma）
                float mainLuma = 0.299f * originalMainColor.r + 0.587f * originalMainColor.g + 0.114f * originalMainColor.b;
                float darkLuma = 0.299f * originalDarkColor.r + 0.587f * originalDarkColor.g + 0.114f * originalDarkColor.b;

                // 亮部灰色目标与暗部灰色目标
                Color targetMainGray = new Color(mainLuma, mainLuma, mainLuma, originalMainColor.a);
                Color targetDarkGray = new Color(darkLuma, darkLuma, darkLuma, originalDarkColor.a);

                // 根据 colorProgress 在黑白和彩色原色之间丝滑插值
                Color finalMainColor = Color.Lerp(targetMainGray, originalMainColor, colorProgress);
                Color finalDarkColor = Color.Lerp(targetDarkGray, originalDarkColor, colorProgress);

                // 6. 重新把计算完的独立色彩参数注入属性块并拍回渲染器
                m_PropBlock.SetColor(ColorPropertyId, finalMainColor);
                m_PropBlock.SetColor(DarkColorPropertyId, finalDarkColor);
                mr.SetPropertyBlock(m_PropBlock);

#if UNITY_EDITOR
                // 编辑器非运行模式下，手动标记脏数据以强刷 Scene 场景窗口视图
                if (!Application.isPlaying) { UnityEditor.EditorUtility.SetDirty(mr); }
#endif
            }
        }
    }
}