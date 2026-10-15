using UnityEngine;

public class AnimationControl : MonoBehaviour
{
    public Animator animator; // 拖入目标 Animator 组件
    public string animationName = "Move"; // 动画名称
    public float startTime = 0f; // 动画的开始时间 (0 - 1)

    void Start()
    {
        // 确保 Animator 存在
        if (animator == null)
        {
            Debug.LogError("Animator 未设置，请在 Inspector 面板拖入 Animator 对象！");
            return;
        }

        // 播放动画并设置开始时间
        PlayAnimationAtStartTime();
    }

    void PlayAnimationAtStartTime()
    {
        // 播放动画: 动画名称、层级为 0、标准化时间 startTime (0 到 1)
        animator.Play(animationName, 0, startTime);
        Debug.Log($"播放动画 {animationName}，开始时间：{startTime}");
    }
}