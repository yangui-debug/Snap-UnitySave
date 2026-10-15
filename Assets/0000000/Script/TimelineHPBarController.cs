using UnityEngine;

[ExecuteAlways] // 【核心】确保不运行游戏、只拖拽 Timeline 时也能实时预览血条缩放
public class TimelineHPBarController : MonoBehaviour
{
    [Header("【1】血条左轴心指定")]
    [Tooltip("把你之前搭建的、位于红血条父级的空物体‘HP_Pivot_Left’拖到这里")]
    public Transform hpPivotLeft;

    [Header("【2】Timeline 驱动参数")]
    [Range(0f, 1f)]
    [Tooltip("当前血量百分比（1代表满血，0代表没血）。你只需要在 Timeline 的动画轨道里对这个变量 K 帧！")]
    public float hpProgress = 1f;

    // 记录上一帧的进度，防止在 Update 里重复刷新性能消耗
    private float m_LastProgress = -1f;

    void Update()
    {
        // 安全检查
        if (hpPivotLeft == null) return;

        // 当 Timeline 进度发生变化时，实时强刷轴心的 X 轴缩放
        if (!Mathf.Approximately(hpProgress, m_LastProgress))
        {
            // 锁定 Y 轴和 Z 轴保持原本的 1 不变，只根据 hpProgress 动态缩放 X 轴
            hpPivotLeft.localScale = new Vector3(hpProgress, 1f, 1f);

            m_LastProgress = hpProgress;
        }
    }
}