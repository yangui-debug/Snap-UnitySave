using UnityEngine;
using DG.Tweening;

public class CameraShakeDOTween : MonoBehaviour
{
    [Header("目标物体 (拖入你新建的 CameraShakeWrapper)")]
    public Transform targetTransform;

    [Header("自动循环设置")]
    public bool autoShake = false;
    public float shakeInterval = 3.0f;

    [Header("微小抖动参数")]
    public float duration = 0.3f;
    public float strength = 0.15f;
    public int vibrato = 15;
    public float randomness = 90f;
    public bool fadeOut = true;

    private float timer = 0f;

    private void Start()
    {
        if (targetTransform == null)
        {
            // 如果没赋值，默认摇晃自己
            targetTransform = this.transform;
        }
    }

    private void Update()
    {
        if (autoShake)
        {
            timer += Time.deltaTime;
            if (timer >= shakeInterval)
            {
                TriggerShake();
                timer = 0f;
            }
        }
    }

    [ContextMenu("手动测试：微小抖动")]
    public void TriggerShake()
    {
        if (targetTransform == null) return;

        // 停止之前的动画，防止位置偏移回不来
        targetTransform.DOComplete();

        // 触发震动
        targetTransform.DOShakePosition(
            duration,
            strength,
            vibrato,
            randomness,
            false,
            fadeOut
        );
    }
}