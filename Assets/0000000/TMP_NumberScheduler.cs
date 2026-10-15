using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TMP_NumberScheduler : MonoBehaviour
{
    [System.Serializable]
    public struct NumberStage
    {
        [Header("--- 阶段时间配置 ---")]
        public float startTime;
        public float endTime;

        [Header("--- 阶段数字配置 ---")]
        public int startNumber;
        public int endNumber;
    }

    [Header("【1】UI 文本组件指定 (TMP)")]
    [Tooltip("直接拖入你的 TextMeshPro - Text 组件")]
    public TMP_Text tmpText;

    [Header("【2】数字阶段排班表")]
    [Tooltip("在这里添加多个阶段，指定时间段和数字插值即可，不用管变色")]
    public List<NumberStage> numberStages = new List<NumberStage>();

    // 内部状态控制
    private float m_TimelineTimer = 0f;
    private int m_CurrentDisplayNumber = 0;
    private List<bool> m_StageFinishedFlags = new List<bool>();

    void OnEnable()
    {
        // 每次重置
        ResetSystem();
    }

    void Update()
    {
        // 1. 实时累加绝对秒数
        m_TimelineTimer += Time.deltaTime;

        // 2. 遍历排班阶段进行数字 Lerp 插值
        for (int i = 0; i < numberStages.Count; i++)
        {
            NumberStage stage = numberStages[i];
            if (m_StageFinishedFlags[i]) continue;

            if (m_TimelineTimer >= stage.startTime)
            {
                if (m_TimelineTimer >= stage.endTime)
                {
                    m_CurrentDisplayNumber = stage.endNumber;
                    m_StageFinishedFlags[i] = true;
                }
                else
                {
                    float totalStageDuration = stage.endTime - stage.startTime;
                    if (totalStageDuration <= 0f)
                    {
                        m_CurrentDisplayNumber = stage.endNumber;
                    }
                    else
                    {
                        float elapsedInStage = m_TimelineTimer - stage.startTime;
                        float progress = elapsedInStage / totalStageDuration;
                        m_CurrentDisplayNumber = Mathf.RoundToInt(Mathf.Lerp(stage.startNumber, stage.endNumber, progress));
                    }
                }

                // 核心：把当前的【数字】和【绝对时间秒数】一起喂给显示拦截器
                UpdateTextDisplay(m_CurrentDisplayNumber, m_TimelineTimer);
                break;
            }
        }
    }

    private void ResetSystem()
    {
        m_TimelineTimer = 0f;
        m_StageFinishedFlags.Clear();
        for (int i = 0; i < numberStages.Count; i++)
        {
            m_StageFinishedFlags.Add(false);
        }

        if (numberStages.Count > 0)
        {
            m_CurrentDisplayNumber = numberStages[0].startNumber;
        }
        else
        {
            m_CurrentDisplayNumber = 0;
        }

        UpdateTextDisplay(m_CurrentDisplayNumber, m_TimelineTimer);
    }

    /// <summary>
    /// 【已全面硬编码重写】：严格执行策划死命令，不需要美术在面板上点任何配置！
    /// </summary>
    private void UpdateTextDisplay(int number, float currentSeconds)
    {
        if (tmpText == null) return;

        // 1. 强刷数字字面值
        tmpText.text = number.ToString();

        // 默认颜色是红色
        Color finalColor = Color.white;

        // ==================== 【策划硬编码核心逻辑区】 ====================

        // 阶段一：前 20 秒内 (0f 到 20f 秒)
        if (currentSeconds <= 20f)
        {
            // 只要数字到了 20，立刻变白
            if (number >= 50)
            {
                finalColor = Color.white;
            }
        }
        // 阶段二：20 秒以后 (过了 20.00 秒)
        else
        {
            // 只有数字到了 60，才会变白
            if (number >= 50)
            {
                finalColor = Color.white;
            }
        }

        // =================================================================

        // 2. 强行染色
        tmpText.color = finalColor;
    }
}