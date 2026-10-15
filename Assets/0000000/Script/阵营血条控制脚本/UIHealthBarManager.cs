using UnityEngine;

public class UIHealthBarManager : MonoBehaviour
{
    // 供 Timeline 左侧信号调用：锁定所有血条在假 100%
    public void LockAllHealthBars()
    {
        FactionHealthBar[] healthBars = FindObjectsOfType<FactionHealthBar>();
        foreach (var bar in healthBars)
        {
            bar.LockBarAtFull();
        }
    }

    // 供 Timeline 右侧信号调用：士兵全部刷完，正式开算
    public void ResetAllHealthBars()
    {
        FactionHealthBar[] healthBars = FindObjectsOfType<FactionHealthBar>();
        foreach (var bar in healthBars)
        {
            bar.InitializeRound();
        }
    }
}