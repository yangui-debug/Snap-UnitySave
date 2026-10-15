using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public Transform targetPosition;  // 吸引目标位置
    public float delayBeforeMove = 1f;  // 启动吸引的延迟时间
    public float arcHeight = 3f;  // 抛物线的高度（控制最高点）
    public float moveDuration = 2f;  // 抛物线完成的时间
    public float scaleTime = 0.08f;  // 缩放持续时间（确保从外部能修改）

    private bool isAttracting = false;
    private Vector3 startPosition;  // 初始位置
    private Vector3 targetPos;  // 目标位置
    private float startTime;  // 动画开始的时间

    private Vector3 originalScale;  // 原始缩放

    void Start()
    {
        startPosition = transform.position;
        targetPos = targetPosition.position;
        originalScale = transform.localScale;

        // 启动协程，延迟后开始抛物线运动
        StartCoroutine(DelayedMove());
    }

    // 使用协程来延迟启动
    private IEnumerator DelayedMove()
    {
        // 等待指定的延迟时间
        yield return new WaitForSeconds(delayBeforeMove);

        // 启动移动逻辑
        isAttracting = true;
        startTime = Time.time;

        // 启动缩放过程
        StartCoroutine(ScaleUp());
    }

    // 处理缩放动画
    private IEnumerator ScaleUp()
    {
        float elapsedTime = 0f;

        while (elapsedTime < scaleTime)
        {
            transform.localScale = Vector3.Lerp(originalScale * 0.1f, originalScale, elapsedTime / scaleTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    void Update()
    {
        if (isAttracting)
        {
            // 计算经过的时间
            float elapsedTime = Time.time - startTime;

            // 计算抛物线的进度
            float progress = Mathf.Clamp01(elapsedTime / moveDuration);  // 确保进度在0到1之间

            // 计算抛物线的 y 坐标（通过一个二次方程模拟）
            float height = Mathf.Sin(progress * Mathf.PI) * arcHeight;  // 使用正弦曲线来模拟抛物线的高度

            // 在平面内平滑地从起始点到目标点
            Vector3 horizontalProgress = Vector3.Lerp(startPosition, targetPos, progress);

            // 结合垂直方向的高度和水平方向的移动，得到抛物线轨迹
            transform.position = horizontalProgress + Vector3.up * height;

            // 同步更新目标位置
            targetPos = targetPosition.position;

            // 到达目标位置后销毁物体
            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}

