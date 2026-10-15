using System.Collections;
using UnityEngine;

// 允许在不运行游戏的情况下，直接在编辑器里拖拽滑块看效果
[ExecuteAlways] 
public class SingleTileInvasionTester : MonoBehaviour
{
    [Header("目标设置")]
    [Tooltip("要把变色效果应用到哪个渲染器上？（通常拖入自己）")]
    public MeshRenderer targetRenderer;

    [Header("侵蚀控制参数")]
    [Tooltip("拖动这个滑块，实时观察格子的变色进度！")]
    [Range(0f, 1f)] 
    public float invasionProgress = 0f;

    [Tooltip("侵蚀后的目标颜色")]
    public Color newFactionColor = new Color(0.85f, 0.25f, 0.2f, 1f);

    [Tooltip("侵蚀从哪个方向推过来？(世界坐标向量)")]
    public Vector3 borderDirection = new Vector3(1, 0, 0);

    [Header("动画测试")]
    public float animationDuration = 1.5f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Shader 中的变量名（必须与 Shader 里一模一样）
    private readonly string propProgress = "_InvasionProgress";
    private readonly string propNewColor = "_InvasionNewColor";
    private readonly string propCenter = "_BorderCenter";
    private readonly string propDirection = "_BorderDirection";

    private MaterialPropertyBlock mpb;

    private void OnEnable()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<MeshRenderer>();
    }

    // 当你在 Inspector 面板里修改任何数值时，这个方法会被自动调用
    private void OnValidate()
    {
        ApplyShaderProperties(invasionProgress);
    }

    /// <summary>
    /// 核心逻辑：使用 MaterialPropertyBlock 控制变色
    /// </summary>
    private void ApplyShaderProperties(float currentProgress)
    {
        if (targetRenderer == null) return;

        // 1. 初始化或获取当前的属性块
        if (mpb == null) mpb = new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(mpb);

        // 2. 写入数据
        mpb.SetFloat(propProgress, currentProgress);
        mpb.SetColor(propNewColor, newFactionColor);
        
        // 计算边界中心（测试时假设从格子的边缘开始）
        Vector3 center = transform.position - borderDirection.normalized * 0.5f;
        mpb.SetVector(propCenter, new Vector4(center.x, 0, center.z, 0));
        
        // 确保垂直于推进方向
        Vector3 dir = borderDirection.normalized;
        Vector2 borderDir = new Vector2(dir.z, -dir.x);
        mpb.SetVector(propDirection, new Vector4(borderDir.x, 0, borderDir.y, 0));

        // 3. 把属性块还给渲染器（此时 Shader 就会根据新数据更新画面）
        targetRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// 右键脚本组件，点击 "测试完整播放动画" 即可触发
    /// </summary>
    [ContextMenu("▶ 测试完整播放动画")]
    public void TestAnimation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在点击顶部的 Play 运行游戏后，再测试播放动画！");
            return;
        }
        StopAllCoroutines();
        StartCoroutine(PlayInvasionAnimation());
    }

    private IEnumerator PlayInvasionAnimation()
    {
        float timer = 0f;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            
            // 计算当前进度
            float normalizedTime = timer / animationDuration;
            float curvedProgress = animationCurve.Evaluate(normalizedTime);
            
            // 更新滑块显示
            invasionProgress = curvedProgress;
            
            // 发送给 Shader
            ApplyShaderProperties(curvedProgress);
            
            yield return null;
        }
        
        // 确保最终定格在 1
        invasionProgress = 1f;
        ApplyShaderProperties(1f);
        Debug.Log("动画播放完成！");
    }

    [ContextMenu("↺ 重置状态")]
    public void ResetTile()
    {
        invasionProgress = 0f;
        ApplyShaderProperties(0f);
    }
}