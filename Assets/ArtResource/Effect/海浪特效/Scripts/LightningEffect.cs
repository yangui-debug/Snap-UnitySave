using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    public Light lightningLight; // 主光源
    public float minIntensity = 0f; // 最低光强度
    public float maxIntensity = 1.5f; // 最高光强度
    public float lightningDuration = 0.1f; // 闪电持续时间
    public float lightningInterval = 3f; // 闪电间隔时间
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lightningInterval)
        {
            StartCoroutine(FlashLightning());
            timer = 0f; // 重置计时器
        }
    }

    private System.Collections.IEnumerator FlashLightning()
    {
        lightningLight.intensity = maxIntensity;
        yield return new WaitForSeconds(lightningDuration);
        lightningLight.intensity = minIntensity;
    }
}
