using UnityEngine;

public class SimplePatrol : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("前后移动的最大距离")]
    public float moveDistance = 2f;

    [Tooltip("移动的速度")]
    public float moveSpeed = 3f;

    // 记录刚出生时的局部位置
    private Vector3 startLocalPos;

    void Start()
    {
        // 游戏开始时，记住自己相对于父级（或者自己当前）的位置
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        // 使用 Sin 正弦函数，算出一个平滑的来回往复的偏移量
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;

        // 强制只在局部的 Z 轴（前后）进行移动
        transform.localPosition = startLocalPos + new Vector3(0, 0, offset);
    }
}