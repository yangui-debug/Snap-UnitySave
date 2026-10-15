using UnityEngine;

public class WobbleEffect : MonoBehaviour
{
    [Header("晃动节奏")]
    [Tooltip("晃动的快慢")]
    public float wobbleSpeed = 5f;

    [Header("晃动幅度")]
    [Tooltip("前后平移幅度 (局部Z轴)")]
    public float moveDistance = 0.5f;

    [Tooltip("前后俯仰角度 (如果摄像机是俯视，稍微加点X轴倾斜会很有立体感)")]
    public float tiltAngle = 10f;

    private Vector3 startLocalPos;
    private Quaternion startLocalRot;
    private float randomTimeOffset;

    void Start()
    {
        // 记录它相对于父物体的初始位置和角度
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;

        // ?? 神仙细节：给每个物体一个随机的时间偏移！
        // 这样它们就不会像军队一样整齐划一地晃，而是各有各的节奏，非常自然！
        randomTimeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // 用 Sin 函数算出一个 -1 到 1 之间平滑波动的数值
        float wave = Mathf.Sin((Time.time + randomTimeOffset) * wobbleSpeed);

        // 1. 前后平移效果 (Z轴偏移)
        transform.localPosition = startLocalPos + new Vector3(0, 0, wave * moveDistance);

        // 2. 前后俯仰/摇摆效果 (X轴旋转偏移)
        transform.localRotation = startLocalRot * Quaternion.Euler(wave * tiltAngle, 0, 0);
    }
}