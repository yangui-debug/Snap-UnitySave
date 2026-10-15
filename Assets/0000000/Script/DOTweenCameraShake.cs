using UnityEngine;
using DG.Tweening; // 确保你项目里导入了 DOTween 插件

public class DOTweenCameraShake : MonoBehaviour
{
    [Header("【1】时间轴频率控制")]
    [Tooltip("每隔多少秒震一次")]
    public float shakeInterval = 4f;

    [Tooltip("每次震动持续时间")]
    public float shakeDuration = 0.4f;

    [Header("【2】DOTween 强度控制")]
    [Tooltip("震动强度。可以用 Vector3 分别控制 X 轴和 Y 轴的剧烈程度")]
    public Vector3 shakeStrength = new Vector3(0.3f, 0.3f, 0f);

    [Tooltip("震动频次。数值越高，在一秒内抖动的次数越多（建议填 20~30）")]
    public int vibrato = 25;

    [Range(0f, 180f)]
    [Tooltip("随机方向角度，默认 90 即可")]
    public float randomness = 90f;

    private float m_Timer = 0f;

    void Start()
    {
        // 开局先给计时器赋初值
        m_Timer = shakeInterval;
    }

    void Update()
    {
        m_Timer -= Time.deltaTime;

        if (m_Timer <= 0f)
        {
            TriggerDOTweenShake();
            // 重新进入下一轮冷却循环
            m_Timer = shakeInterval;
        }
    }

    /// <summary>
    /// 触发 DOTween 的高级物理抖动
    /// </summary>
    public void TriggerDOTweenShake()
    {
        // 释放一枪 DOTween 抖动
        // SetUpdate(true) 确保即使游戏被暂停了（Time.timeScale = 0），Timeline 运镜里的相机也照样能震
        transform.DOShakePosition(shakeDuration, shakeStrength, vibrato, randomness)
                 .SetUpdate(true);
    }
}