using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 使用原生 Text 组件需要引入

// 如果你使用的是 TextMeshPro，请把下面这行前面的双斜杠 // 删掉
// using TMPro; 

public class TextNumberScheduler : MonoBehaviour
{
    [System.Serializable]
    public struct NumberStage
    {
        [Tooltip("什么时候【开始】让数字变化（单位：秒）")]
        public float startTime;

        [Tooltip("这次要滚动到的【目标数字】")]
        public int targetNumber;

        [Tooltip("这次数字变化滚动的【持续时间】（单位：秒）。填 0 表示瞬间变化。")]
        public float duration;
    }

    [Header("【1】UI 文本组件指定")]
    [Tooltip("拖入你需要控制的 UI Text 组件")]
    public Text uiText;

    // 如果你使用的是 TextMeshPro，请解除下面这行的注释，并把上面的 uiText 注释掉
    // public TMP_Text tmpText; 

    [Header("【2】数字阶段排班表")]
    [Tooltip("在这里添加多个时间点、目标数字和滚动时间")]
    public List<NumberStage> numberStages = new List<NumberStage>();

    // 内部状态控制
    private float m_TimelineTimer = 0f;
    private int m_CurrentDisplayNumber = 0;

    // 用来记录每一个阶段的实时状态
    private class StageState
    {
        public bool hasStarted = false;
        public bool hasFinished = false;
        public int startNumber = 0;
    }
    private List<StageState> m_StageStates = new List<StageState>();

    void OnEnable()
    {
        // 每次 Timeline 激活或者重新倒回播放时，彻底重置
        ResetSystem();
    }

    void Update()
    {
        // 1. 累加时间軸时间
        m_TimelineTimer += Time.deltaTime;

        // 2. 遍历所有排班阶段
        for (int i = 0; i < numberStages.Count; i++)
        {
            NumberStage stage = numberStages[i];
            StageState state = m_StageStates[i];

            // 检查是否到了该阶段的启动时间
            if (m_TimelineTimer >= stage.startTime && !state.hasFinished)
            {
                // 如果是刚刚进入这个阶段，记录下进入时的“初始数字”
                if (!state.hasStarted)
                {
                    state.hasStarted = true;
                    state.startNumber = m_CurrentDisplayNumber;
                }

                // 计算当前阶段已经过去了多少时间
                float elapsedInStage = m_TimelineTimer - stage.startTime;

                if (stage.duration <= 0f || elapsedInStage >= stage.duration)
                {
                    // 如果持续时间设为0，或者时间已经到了，直接卡死到目标数字
                    m_CurrentDisplayNumber = stage.targetNumber;
                    state.hasFinished = true;
                }
                else
                {
                    // 核心逻辑：在持续时间内，利用 Mathf.Lerp 进行平滑的数字递增/递减插值
                    float progress = elapsedInStage / stage.duration;

                    // Mathf.RoundToInt 可以实现完美的整数滚动效果
                    m_CurrentDisplayNumber = Mathf.RoundToInt(Mathf.Lerp(state.startNumber, stage.targetNumber, progress));
                }

                // 刷新 UI 显示
                UpdateTextDisplay(m_CurrentDisplayNumber.ToString());

                // 【重要】因为是多阶段顺序执行，当前阶段正在滚动时，后面的阶段不应该提前介入，所以直接跳出循环
                break;
            }
        }
    }

    private void ResetSystem()
    {
        m_TimelineTimer = 0f;
        m_CurrentDisplayNumber = 0;

        // 重新初始化所有阶段的状态机
        m_StageStates.Clear();
        for (int i = 0; i < numberStages.Count; i++)
        {
            m_StageStates.Add(new StageState());
        }

        // 初始显示为 0 
        UpdateTextDisplay("0");
    }

    // 统一的文本刷新方法
    private void UpdateTextDisplay(string content)
    {
        if (uiText != null) uiText.text = content;

        // 如果使用的是 TextMeshPro，解除下面这行的注释
        // if (tmpText != null) tmpText.text = content;
    }
}