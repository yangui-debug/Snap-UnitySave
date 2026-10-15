using System.Collections;
using UnityEngine;

public class StepRotator : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("每次旋转的角度 (例如想在Y轴每次转90度，就填 0, 90, 0)")]
    public Vector3 rotationStep = new Vector3(0f, 90f, 0f);

    [Tooltip("完成这一次旋转所需的时间（单位：秒）")]
    public float rotationDuration = 0.5f;

    [Tooltip("每次旋转完成后暂停的时间（单位：秒）")]
    public float pauseDuration = 1.0f;

    private void Start()
    {
        // 游戏开始时，启动自动旋转的协程
        StartCoroutine(RotateRoutine());
    }

    private IEnumerator RotateRoutine()
    {
        while (true) // 无限循环
        {
            // 1. 记录开始转动前当前的角度，以及我们要转到的目标角度
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(rotationStep);

            float elapsedTime = 0f;

            // 2. 在指定的时间内，平滑地转到目标角度
            // 如果 rotationDuration 设置为0，直接跳过这个平滑过程防止报错
            if (rotationDuration > 0f)
            {
                while (elapsedTime < rotationDuration)
                {
                    // 计算当前的时间进度百分比 (0 到 1 之间)
                    float t = elapsedTime / rotationDuration;
                    
                    // Slerp 是一种球面插值，能让物体的旋转非常平滑自然
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                    
                    // 累加经过的时间
                    elapsedTime += Time.deltaTime;
                    
                    // 等待下一帧再继续循环
                    yield return null; 
                }
            }

            // 3. 强行对齐目标角度（防止因为运行时的浮点数误差导致每次差零点几度）
            transform.rotation = targetRotation;

            // 4. 转完了，暂停指定的时间
            yield return new WaitForSeconds(pauseDuration);
        }
    }
}