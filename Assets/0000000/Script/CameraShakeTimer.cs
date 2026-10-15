using UnityEngine;

public class CameraShakeTimer : MonoBehaviour
{
    [Header("【1】时间轴频率控制")]
    [Tooltip("每隔多少秒触发一次震动？（单位：秒）")]
    public float shakeInterval = 5f;

    [Tooltip("每次震动持续多长时间？（单位：秒）")]
    public float shakeDuration = 0.5f;

    [Header("【2】震动强度控制")]
    [Tooltip("震动的猛烈程度。数值越大，摇晃幅度越大")]
    public float shakeMagnitude = 0.2f;

    [Tooltip("震动的频率/速度。数值越大，摇晃得越快（频率越高）")]
    public float shakeFrequency = 25f;

    // 内部状态控制
    private Vector3 m_OriginalPos;
    private float m_IntervalTimer = 0f;
    private float m_CurrentShakeTime = 0f;
    private bool m_IsShaking = false;

    void OnEnable()
    {
        // 记住相机刚开始的初始位置，防止震完后相机“回不去了”
        m_OriginalPos = transform.localPosition;
        m_IntervalTimer = 0f;
    }

    void Update()
    {
        if (m_IsShaking)
        {
            // ==================== 正在震动状态 ====================
            m_CurrentShakeTime -= Time.deltaTime;

            if (m_CurrentShakeTime <= 0f)
            {
                // 震动结束，强行把相机送回原位，并重置闭环计时器
                m_IsShaking = false;
                transform.localPosition = m_OriginalPos;
                m_IntervalTimer = shakeInterval;
            }
            else
            {
                // 计算衰减系数（让震动从最猛烈慢慢平息下来，效果更真实）
                float damper = m_CurrentShakeTime / shakeDuration;

                // 利用 Mathf.Sin 或 随机球体算法 计算震动偏移量
                float offsetX = (Mathf.Sin(Time.time * shakeFrequency) + Random.Range(-0.5f, 0.5f)) * shakeMagnitude * damper;
                float offsetY = (Mathf.Cos(Time.time * shakeFrequency) + Random.Range(-0.5f, 0.5f)) * shakeMagnitude * damper;

                // 叠加到相机原位置上
                transform.localPosition = new Vector3(m_OriginalPos.x + offsetX, m_OriginalPos.y + offsetY, m_OriginalPos.z);
            }
        }
        else
        {
            // ==================== 等待下一次震动的冷却状态 ====================
            m_IntervalTimer -= Time.deltaTime;

            if (m_IntervalTimer <= 0f)
            {
                // 冷却时间到，激活震动！
                TriggerShake();
            }
        }
    }

    /// <summary>
    /// 【手动引爆接口】：除了自动循环，你也可以在别的地方通过代码随时手动调用这个函数让相机震一下
    /// </summary>
    public void TriggerShake()
    {
        if (shakeDuration <= 0f) return;

        // 重新锚定当前初始位置，防止期间相机因为运镜移动了位置
        m_OriginalPos = transform.localPosition;
        m_CurrentShakeTime = shakeDuration;
        m_IsShaking = true;
    }
}