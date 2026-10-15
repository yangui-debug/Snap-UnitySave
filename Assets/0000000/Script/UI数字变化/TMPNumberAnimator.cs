using UnityEngine;
using TMPro;

public class TMPNumberAnimator : MonoBehaviour
{
    [Header("绑定 TMP 文本组件")]
    [SerializeField] private TextMeshProUGUI textComponent;

    [Header("Timeline 控制这个变量（范围 0-500）")]
    [Range(0f, 500f)]
    public float currentNumber = 0f;

    [Header("==== 全新新增：颜色控制功能 ====")]
    [Header("是否开启变色功能")]
    [SerializeField] private bool enableColorChange = true;

    [Header("变色阈值（大于这个数变白）")]
    [SerializeField] private float thresholdValue = 200f;

    [Header("基础颜色（未达到阈值时的颜色）")]
    [SerializeField] private Color baseColor = Color.red;

    [Header("目标颜色（达到阈值时，或未勾选功能时的颜色）")]
    [SerializeField] private Color targetColor = Color.white;

    private void Awake()
    {
        // 如果漏挂组件，自动在自身物体上获取
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (textComponent != null)
        {
            // 1. 更新文本数字 ("F0" 表示显示为整数)
            textComponent.text = currentNumber.ToString("F0");

            // 2. 颜色逻辑判断
            if (enableColorChange)
            {
                // 如果开启了变色，大于设定的阈值就变成白色（targetColor），否则保持红色（baseColor）
                if (currentNumber > thresholdValue)
                {
                    textComponent.color = targetColor;
                }
                else
                {
                    textComponent.color = baseColor;
                }
            }
            else
            {
                // 如果不勾选变色功能，一直保持白色（targetColor）
                textComponent.color = targetColor;
            }
        }
    }
}