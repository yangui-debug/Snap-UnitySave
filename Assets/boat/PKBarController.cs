using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PKBarController : MonoBehaviour
{
    [Header("UI 引用")]
    public Image blueFillImage;
    public Image redFillImage;
    public TextMeshProUGUI blueText;
    public TextMeshProUGUI redText;

    [Header("数值设定")]
    public float minPercent = 15.53f;
    public float maxPercent = 93.89f;

    [Header("两段式速度设置")]
    [Tooltip("59% 之前的初始速度")]
    public float slowSpeed = 0.02f;
    [Tooltip("达到界限后，变快的速度")]
    public float fastSpeed = 2.0f;
    [Tooltip("触发加速的百分比界限")]
    public float speedUpThreshold = 59.0f;

    [Header("动画设置")]
    public float delayTime = 5f;

    [Range(0, 100)]
    public float blueTargetPercent = 15.53f;

    private float currentPercent;

    void Start()
    {
        currentPercent = minPercent;
        blueTargetPercent = minPercent;
        UpdateVisuals(currentPercent);

        StartCoroutine(GrowthCountdown());
    }

    void Update()
    {
        if (Mathf.Abs(currentPercent - blueTargetPercent) > 0.001f)
        {
            // --- 核心变速逻辑 ---
            // 判断当前进度：如果大于等于 59%，就用快速度；否则用慢速度
            float currentSpeed = currentPercent >= speedUpThreshold ? fastSpeed : slowSpeed;

            // 应用计算出来的当前速度
            currentPercent = Mathf.Lerp(currentPercent, blueTargetPercent, Time.deltaTime * currentSpeed);

            UpdateVisuals(currentPercent);
        }
    }

    private IEnumerator GrowthCountdown()
    {
        yield return new WaitForSeconds(delayTime);
        blueTargetPercent = maxPercent;
    }

    private void UpdateVisuals(float percent)
    {
        float redPercent = 100f - percent;

        if (blueFillImage != null)
            blueFillImage.fillAmount = percent / 100f;

        if (redFillImage != null)
            redFillImage.fillAmount = redPercent / 100f;

        if (blueText != null)
            blueText.text = percent.ToString("F2") + "%";

        if (redText != null)
            redText.text = redPercent.ToString("F2") + "%";
    }
}