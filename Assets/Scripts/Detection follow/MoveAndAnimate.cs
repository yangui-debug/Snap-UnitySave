using UnityEngine;

public class MoveAndAnimate : MonoBehaviour
{
    [Header("目标物体")]
    public Transform target; // 物体A的Transform

    [Header("参数设置")]
    [Tooltip("检测范围")]
    public float detectionRange = 5f; // 检测范围

    [Tooltip("移动速度")]
    public float moveSpeed = 2f; // 移动速度

    [Header("停止距离设置")]
    [Tooltip("主物体Z停止跟随物体A的距离")]
    public float stopDistance = 0.5f; // 停止距离

    [Header("动画控制")]
    public Animator animator; // Animator组件
    public string moveAnimation; // 向物体A移动的动画触发器
    public string stopAnimation; // 停止时播放的动画触发器
    public string idleAnimation; // 停止后播放的待机动画

    private bool isFollowing = false; // 用于跟随状态的标志
    private bool isStopped = false; // 用于判断是否已停止的标志
    private bool isAnimationPlaying = false; // 判断动画是否正在播放
    private Vector3 lastTargetPosition; // 记录物体A的上一个位置

    private void Update()
    {
        // 检测物体A是否在范围内
        if (Vector3.Distance(transform.position, target.position) < detectionRange)
        {
            // 物体A在范围内，触发跟随
            StartFollowing();
            MoveTowardsTarget();
        }
        else
        {
            // 物体A不在范围内，播放待机动画
            if (isStopped && !isAnimationPlaying)
            {
                animator.SetTrigger(idleAnimation);
            }
            isFollowing = false; // 停止跟随
        }

        // 检查物体A是否移动
        if (isFollowing && Vector3.Distance(lastTargetPosition, target.position) > 0)
        {
            // 物体A移动时播放动画
            animator.SetTrigger(moveAnimation); // 播放移动动画
            MoveTowardsTarget();
        }

        lastTargetPosition = target.position; // 更新上一个位置
    }

    private void MoveTowardsTarget()
    {
        // 计算移动方向
        Vector3 direction = (target.position - transform.position).normalized;

        // 修改朝向，使主物体Z朝向物体A
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime);

        // 移动主物体Z到目标的止步位置，使用可调节的移动速度
        transform.position = Vector3.MoveTowards(transform.position, target.position - direction * stopDistance, moveSpeed * Time.deltaTime);

        // 检查是否接近物体A并停止在指定距离
        if (Vector3.Distance(transform.position, target.position) < stopDistance)
        {
            if (!isAnimationPlaying)
            {
                isStopped = true; // 设置为已停止
                PlayStopAnimation(); // 播放停止动画
            }
        }
        else
        {
            isStopped = false; // 如果不在停止范围，重置停止状态
        }
    }

    private void StartFollowing()
    {
        // 只有在没有跟随时才开始播放移动动画
        if (!isFollowing)
        {
            isFollowing = true; // 更新跟随状态
        }
    }

    private void PlayStopAnimation()
    {
        // 播放停止动画并设置标志
        isAnimationPlaying = true;
        animator.SetTrigger(stopAnimation);

        // 等待动画播放完成
        StartCoroutine(WaitForAnimationCompletion());
    }

    private System.Collections.IEnumerator WaitForAnimationCompletion()
    {
        // 等待动画播放完成（根据动画长度调整）
        yield return new WaitForSeconds(animator.runtimeAnimatorController.animationClips[0].length); // 这里假设使用第一个动画的长度，你可以替换成具体动画长度
        isAnimationPlaying = false; // 动画播放完毕
    }

    private void OnDrawGizmos()
    {
        // 设置Gizmos的颜色
        Gizmos.color = Color.red;

        // 绘制检测范围的球体
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

