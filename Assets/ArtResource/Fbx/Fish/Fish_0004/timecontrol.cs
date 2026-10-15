using UnityEngine;

public class PlayAnimationFromTime : MonoBehaviour
{
    public Animator animator; // 在 Inspector 面板中绑定 Animator

    void Start()
    {
        // 播放动画 "MyAnimationState"，从 50% 的时间点开始
        animator.Play("MyAnimationState", 0, 0.5f);
    }
}