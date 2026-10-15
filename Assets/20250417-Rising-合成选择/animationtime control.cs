using System.Collections;
using UnityEngine;

public class AnimationSpeedController : MonoBehaviour
{
    public Animator animator;  // 动画控制器
    public float[] speedChangeTimes;  // 时间点数组，每两个时间点组成一个变速区间（A-B, C-D, ...）
    public float[] speeds;  // 每个时间段的目标速度（例如：0.5表示减速到0.5倍，2.0表示加速2倍）

    private float originalSpeed;  // 原始动画速度
    private int currentIndex = 0;  // 当前的时间段索引

    void Start()
    {
        // 获取当前动画的默认速度
        originalSpeed = animator.speed;
        StartCoroutine(ChangeAnimationSpeedAtTime());
    }

    IEnumerator ChangeAnimationSpeedAtTime()
    {
        // 持续检查
        while (currentIndex < speedChangeTimes.Length)
        {
            // 判断是否是变速区间的开始时间
            if (currentIndex % 2 == 0) // 偶数索引表示区间的起始时间点（例如A, C）
            {
                // 等待直到当前时间超过起始时间
                while (Time.time < speedChangeTimes[currentIndex])
                {
                    yield return null;
                }

                // 获取目标速度（根据当前区间的速度）
                float targetSpeed = speeds[currentIndex / 2];
                float startTime = Time.time;

                // 在当前时间段内平滑过渡到目标速度
                while (Time.time < speedChangeTimes[currentIndex + 1])
                {
                    float lerpFactor = (Time.time - startTime) / (speedChangeTimes[currentIndex + 1] - startTime);
                    animator.speed = Mathf.Lerp(originalSpeed, targetSpeed, lerpFactor);
                    yield return null;
                }
            }
            currentIndex++;
        }

        // 恢复到原始速度
        animator.speed = originalSpeed;
    }
}
