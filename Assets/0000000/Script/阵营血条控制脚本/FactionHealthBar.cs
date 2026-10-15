using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FactionHealthBar : MonoBehaviour
{
    [Header("阵营设置")]
    public FactionType targetFaction = FactionType.Red; //[cite: 1]

    [Header("UI 组件引用")]
    public Image healthBarImage;
    public Text legacyText;
    public TMP_Text tmpText;

    [Header("动画设置")]
    public float lerpSpeed = 5.0f;

    private int maxCount = 0;
    private float currentPercentage = 1f;
    private float targetPercentage = 1f;

    // 控制是否开启实时血条检测的开关
    private bool isTracking = false;

    void Start()
    {
        // 初始状态保持锁定假 100%
        LockBarAtFull();
    }

    /// <summary>
    /// 【准备阶段调用】：锁定血条在 100% 不动，暂停检测
    /// </summary>
    public void LockBarAtFull()
    {
        isTracking = false;
        currentPercentage = 1f;
        targetPercentage = 1f;
        UpdateDisplay(1f);
    }

    /// <summary>
    /// 【战斗开始调用】：士兵已生成完毕，以此刻数量为 100% 并开启实时检测
    /// </summary>
    public void InitializeRound()
    {
        maxCount = GetActiveSoldierCount();
        if (maxCount == 0) maxCount = 1;

        currentPercentage = 1f;
        targetPercentage = 1f;
        UpdateDisplay(1f);

        // 开启检测
        isTracking = true;
    }

    void Update()
    {
        // 如果处于“假 100%”锁定阶段，直接不执行后续代码
        if (!isTracking || maxCount <= 0) return;

        int currentCount = GetActiveSoldierCount();
        targetPercentage = Mathf.Clamp01((float)currentCount / maxCount);
        currentPercentage = Mathf.Lerp(currentPercentage, targetPercentage, Time.deltaTime * lerpSpeed);

        UpdateDisplay(currentPercentage);
    }

    private int GetActiveSoldierCount()
    {
        SoldierAnimationController[] allSoldiers = FindObjectsOfType<SoldierAnimationController>();
        int count = 0;

        foreach (var soldier in allSoldiers)
        {
            if (soldier != null &&
                soldier.gameObject.activeInHierarchy &&
                soldier.myFaction == targetFaction) //[cite: 1]
            {
                count++;
            }
        }
        return count;
    }

    private void UpdateDisplay(float pct)
    {
        if (healthBarImage != null) healthBarImage.fillAmount = pct;
        string pctText = $"{Mathf.RoundToInt(pct * 100)}%";
        if (legacyText != null) legacyText.text = pctText;
        if (tmpText != null) tmpText.text = pctText;
    }
}