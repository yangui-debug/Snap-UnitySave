using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderTMProController : MonoBehaviour
{
    // 必须的UI组件引用
    [Header("UI 组件")]
    [Tooltip("拖拽Slider组件到这里")]
    public Slider valueSlider;

    [Tooltip("拖拽TextMeshPro Text组件到这里")]
    public TMP_Text valueDisplayText;

    // 显示设置
    [Header("显示设置")]
    [Tooltip("数字前面的文字，如'价格：'")]
    public string prefix = "数值：";

    [Tooltip("数字后面的单位，如'元'")]
    public string suffix = "";

    [Tooltip("数字格式：F0=整数，F1=1位小数，F2=2位小数")]
    public string numberFormat = "F0";

    // 数值映射设置
    [Header("数值映射")]
    [Tooltip("启用后使用曲线映射数值（非线性转换）")]
    public bool useValueMapping = false;

    [Tooltip("数值映射曲线，X轴是0-1，Y轴是映射后的0-1")]
    public AnimationCurve valueCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // 颜色反馈设置
    [Header("颜色反馈")]
    [Tooltip("是否根据数值变化改变文字颜色")]
    public bool useColorFeedback = true;

    [Tooltip("最小数值时的颜色")]
    public Color minColor = Color.red;

    [Tooltip("中间数值时的颜色")]
    public Color midColor = Color.yellow;

    [Tooltip("最大数值时的颜色")]
    public Color maxColor = Color.green;

    // 事件
    [Header("事件")]
    [Tooltip("当数值变化时触发的事件")]
    public UnityEngine.Events.UnityEvent<float> onValueChanged;

    // 私有变量
    private float currentValue;
    private float displayValue;

    void Start()
    {
        // 安全检查
        if (valueSlider == null)
        {
            valueSlider = GetComponent<Slider>();
            if (valueSlider == null)
            {
                Debug.LogError("未找到Slider组件！请将Slider拖拽到脚本或检查挂载对象");
                return;
            }
        }

        if (valueDisplayText == null)
        {
            Debug.LogError("未设置TextMeshPro Text组件！");
            return;
        }

        // 初始化事件监听
        SetupSliderEvents();

        // 初始更新显示
        UpdateDisplay(valueSlider.value);

        Debug.Log($"Slider控制器初始化完成，范围：{valueSlider.minValue} - {valueSlider.maxValue}");
    }

    void SetupSliderEvents()
    {
        // 清除旧的事件监听（避免重复）
        valueSlider.onValueChanged.RemoveAllListeners();

        // 添加新的监听
        valueSlider.onValueChanged.AddListener((value) =>
        {
            // 更新显示
            UpdateDisplay(value);

            // 触发自定义事件
            onValueChanged?.Invoke(value);

            // 保存当前值
            currentValue = value;
        });
    }

    public void UpdateDisplay(float rawValue)
    {
        // 处理数值映射
        if (useValueMapping && valueCurve != null)
        {
            // 将原始值归一化到0-1范围
            float normalizedValue = Mathf.InverseLerp(valueSlider.minValue, valueSlider.maxValue, rawValue);

            // 通过曲线映射
            float mappedNormalized = valueCurve.Evaluate(normalizedValue);

            // 转换回实际范围
            displayValue = Mathf.Lerp(valueSlider.minValue, valueSlider.maxValue, mappedNormalized);
        }
        else
        {
            displayValue = rawValue;
        }

        // 更新文本显示
        UpdateTextDisplay();

        // 更新颜色反馈
        if (useColorFeedback)
        {
            UpdateColorFeedback();
        }
    }

   public void UpdateTextDisplay()
    {
        // 构建完整的显示文本
        string formattedNumber = displayValue.ToString(numberFormat);
        valueDisplayText.text = $"{prefix}{formattedNumber}{suffix}";
    }

    void UpdateColorFeedback()
    {
        // 计算归一化值（0-1）
        float normalizedValue = Mathf.InverseLerp(valueSlider.minValue, valueSlider.maxValue, displayValue);

        // 根据数值范围插值颜色
        if (normalizedValue <= 0.5f)
        {
            // 0-0.5：从minColor到midColor
            float t = normalizedValue * 2f;
            valueDisplayText.color = Color.Lerp(minColor, midColor, t);
        }
        else
        {
            // 0.5-1：从midColor到maxColor
            float t = (normalizedValue - 0.5f) * 2f;
            valueDisplayText.color = Color.Lerp(midColor, maxColor, t);
        }
    }

    // ========== 公开方法（供其他脚本调用）==========

    /// <summary>
    /// 设置Slider的数值
    /// </summary>
    /// <param name="newValue">新数值</param>
    public void SetValue(float newValue)
    {
        if (valueSlider != null)
        {
            newValue = Mathf.Clamp(newValue, valueSlider.minValue, valueSlider.maxValue);
            valueSlider.value = newValue;
            UpdateDisplay(newValue);
        }
    }

    /// <summary>
    /// 获取当前显示值（经过映射处理后的值）
    /// </summary>
    public float GetDisplayValue()
    {
        return displayValue;
    }

    /// <summary>
    /// 获取原始Slider值
    /// </summary>
    public float GetRawValue()
    {
        return currentValue;
    }

    /// <summary>
    /// 设置数值范围
    /// </summary>
    public void SetRange(float min, float max)
    {
        if (valueSlider != null)
        {
            valueSlider.minValue = min;
            valueSlider.maxValue = max;
            valueSlider.value = Mathf.Clamp(valueSlider.value, min, max);
            UpdateDisplay(valueSlider.value);
        }
    }

    /// <summary>
    /// 重置为默认值
    /// </summary>
    public void ResetToDefault(float defaultValue)
    {
        SetValue(defaultValue);
    }

    /// <summary>
    /// 启用或禁用Slider
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (valueSlider != null)
        {
            valueSlider.interactable = interactable;
            valueDisplayText.alpha = interactable ? 1f : 0.5f;
        }
    }
}
