using UnityEngine;
using TMPro; // 【核心】必须引入 TextMeshPro 命名空间

[ExecuteAlways] // 确保在编辑器模式下改动数字也能实时看到变色，调 UI 极方便
public class TMP_ColorThresholdController : MonoBehaviour
{
    [Header("【1】组件关联")]
    [Tooltip("拖入你需要控制变色的 TextMeshPro - Text UI 组件")]
    public TextMeshProUGUI textComponent;

    [Header("【2】阈值与颜色配置")]
    [Tooltip("当前的数字大小。可以在别的脚本里修改它，或者在 Timeline 里对它 K 帧！")]
    public int currentNumber = 0;

    [Tooltip("临界数字。超过或等于这个数字，文本就会变白。")]
    public int thresholdNumber = 100;

    [Space]
    [Tooltip("没有超过阈值时的颜色（默认红色）")]
    public Color normalColor = Color.red;

    [Tooltip("超过阈值后的高亮颜色（默认白色）")]
    public Color thresholdColor = Color.white;

    // 内部缓存，防止在 Update 里由于文本频繁打桩导致内存碎片（GC Alloc）
    private int m_LastNumber = -1;

    void Awake()
    {
        // 防呆：如果没拖，尝试自动获取
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (textComponent == null) return;

        // 核心优化：只有当数字真正发生改变时，才去刷新文本和颜色
        if (currentNumber != m_LastNumber)
        {
            // 1. 更新文本框里显示的数字
            textComponent.text = currentNumber.ToString();

            // 2. 核心逻辑判断：如果当前数字 >= 阈值，狠狠变白，否则变红
            if (currentNumber >= thresholdNumber)
            {
                textComponent.color = thresholdColor;
            }
            else
            {
                textComponent.color = normalColor;
            }

            m_LastNumber = currentNumber;
        }
    }

    /// <summary>
    /// 公开的外部调用方法。如果不想在 Update 里跑，
    /// 可以在你的战斗、扣血或者连击脚本里直接调用这个方法来改数字。
    /// </summary>
    public void UpdateNumber(int newNumber)
    {
        currentNumber = newNumber;
    }
}