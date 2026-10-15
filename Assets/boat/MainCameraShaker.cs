using UnityEngine;
using DG.Tweening;

public class MainCameraShaker : MonoBehaviour
{
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

    // 缓存主相机和它的初始位置
    private Camera mainCam;
    private Vector3 originalLocalPos;
    private Tweener shakeTweener; // 用于记录当前的震动动画

    private void Start()
    {
        // 自动获取场景中的 MainCamera
        mainCam = Camera.main;

        if (mainCam != null)
        {
            // 记录相机最原始的局部坐标（防偏移的核心）
            originalLocalPos = mainCam.transform.localPosition;
        }
        else
        {
            Debug.LogError("找不到主相机！请确保你的相机带有 'MainCamera' 的 Tag。");
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
        if (mainCam == null) return;

        // 【防偏移机制 1】：如果当前正在震动，先强制停止，并瞬间归位
        if (shakeTweener != null && shakeTweener.IsActive())
        {
            shakeTweener.Kill();
            mainCam.transform.localPosition = originalLocalPos;
        }

        // 【触发新震动】：记录动画句柄，并使用 DOTween 摇晃
        shakeTweener = mainCam.transform.DOShakePosition(
            duration,
            strength,
            vibrato,
            randomness,
            false,
            fadeOut
        ).OnComplete(() =>
        {
            // 【防偏移机制 2】：震动彻底结束后，强制确保坐标完美归位
            mainCam.transform.localPosition = originalLocalPos;
        });
    }
}