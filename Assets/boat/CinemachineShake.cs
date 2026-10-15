using UnityEngine;
using Cinemachine; // 必须引入 Cinemachine 命名空间

public class CinemachineShake : MonoBehaviour
{
    [Header("虚拟相机引用")]
    public CinemachineVirtualCamera vCam;

    [Header("自动循环设置")]
    public bool autoShake = false;
    public float shakeInterval = 3.0f;

    [Header("抖动参数")]
    public float shakeAmplitude = 0.5f; // 震动幅度（力量）
    public float shakeFrequency = 2.0f;  // 震动频率
    public float shakeDuration = 0.3f;   // 震动持续时间

    private float timer = 0f;
    private float shakeTimer = 0f;
    private CinemachineBasicMultiChannelPerlin noise;

    private void Start()
    {
        if (vCam == null) vCam = GetComponent<CinemachineVirtualCamera>();

        // 获取噪声控制组件
        if (vCam != null)
        {
            noise = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    private void Update()
    {
        if (noise == null) return;

        // 1. 自动循环逻辑
        if (autoShake)
        {
            timer += Time.deltaTime;
            if (timer >= shakeInterval)
            {
                TriggerShake();
                timer = 0f;
            }
        }

        // 2. 抖动计时结束逻辑
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0)
            {
                // 停止抖动
                noise.m_AmplitudeGain = 0;
            }
        }
    }

    [ContextMenu("手动测试：虚拟相机抖动")]
    public void TriggerShake()
    {
        if (noise == null) return;

        // 设置震动参数
        noise.m_AmplitudeGain = shakeAmplitude;
        noise.m_FrequencyGain = shakeFrequency;

        // 开启计时
        shakeTimer = shakeDuration;
    }
}