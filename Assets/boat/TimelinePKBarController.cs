using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 定义时间轴节点的数据结构
[System.Serializable]
public class PKTimelineNode
{
    [Tooltip("在第几秒触发这个变化")]
    public float time;

    [Tooltip("蓝方要达到的目标百分比（0-100，红方会自动计算）")]
    [Range(0, 100)]
    public float blueTargetPercent;

    [Tooltip("变化到该数值的平滑过渡速度")]
    public float transitionSpeed = 2f;
}

public class TimelinePKBarController : MonoBehaviour
{
    [Header("UI 引用")]
    public Image blueFillImage;
    public Image redFillImage;
    public TextMeshProUGUI blueText;
    public TextMeshProUGUI redText;

    [Header("初始设置")]
    [Tooltip("游戏刚开始时，蓝方的百分比（默认 50）")]
    [Range(0, 100)]
    public float initialPercent = 50f;

    [Header("时间轴节点设置 (可任意添加)")]
    [Tooltip("按照时间顺序在这里添加节点")]
    public List<PKTimelineNode> timelineNodes = new List<PKTimelineNode>();

    private float currentPercent;
    private float targetPercent;
    private float currentLerpSpeed = 2f;
    private float elapsedTime = 0f;
    private int currentNodeIndex = 0;

    void Start()
    {
        // 初始化数值，一开始设为初始值 (默认50%)
        currentPercent = initialPercent;
        targetPercent = initialPercent;
        UpdateVisuals(currentPercent);

        // 为了防止你在面板里填乱了顺序，代码会自动按时间从小到大排序
        timelineNodes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    void Update()
    {
        // 游戏时间累加
        elapsedTime += Time.deltaTime;

        // 检查是否还有未触发的节点，且当前时间是否到达了该节点设定的时间
        if (currentNodeIndex < timelineNodes.Count)
        {
            if (elapsedTime >= timelineNodes[currentNodeIndex].time)
            {
                // 触发当前节点：更新目标百分比和过渡速度
                targetPercent = timelineNodes[currentNodeIndex].blueTargetPercent;
                currentLerpSpeed = timelineNodes[currentNodeIndex].transitionSpeed;

                // 指向下一个节点
                currentNodeIndex++;
            }
        }

        // --- 核心平滑动画逻辑 ---
        if (Mathf.Abs(currentPercent - targetPercent) > 0.001f)
        {
            currentPercent = Mathf.Lerp(currentPercent, targetPercent, Time.deltaTime * currentLerpSpeed);
            UpdateVisuals(currentPercent);
        }
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