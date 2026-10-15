using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
[ExecuteAlways] // 极其重要：允许在编辑器 Timeline 拖拽时实时预览，不用跑游戏
public class AESliderCounter : MonoBehaviour
{
    [Header("AE 风格滑轨：在这里打关键帧")]
    public float currentNumber = 0f;

    [Header("显示设置")]
    [Tooltip("小数点后保留几位。0 表示纯整数")]
    [Range(0, 7)]
    public int decimalPlaces = 0;

    [Tooltip("是否使用逗号分隔 (例如 1,000,000)")]
    public bool useCommaSeparator = false;

    [Space(5)]
    [Tooltip("中间数字的放大比例 (1.0 = 与前后缀一样大，1.5 = 放大1.5倍)")]
    [Range(0.5f, 3f)]
    public float numberSizeScale = 1.2f;

    [Header("文本内容及颜色")]
    [Tooltip("前缀 (例如：Lv. 或 $)")]
    public string prefix = "";
    public Color prefixColor = Color.white;

    [Space(5)]
    [Tooltip("数字中间部分的颜色")]
    public Color numberColor = Color.white;

    [Space(5)]
    [Tooltip("后缀 (例如：万 或 %)")]
    public string suffix = "";
    public Color suffixColor = Color.white;

    private TextMeshProUGUI _textMesh;
    private float _previousNumber = float.NaN;
    private int _previousDecimals = -1;
    private bool _previousComma = false;
    private float _prevNumberSizeScale = -1f;
    
    private string _prevPrefix = null;
    private string _prevSuffix = null;
    
    private Color _prevPrefixColor = Color.clear;
    private Color _prevNumberColor = Color.clear;
    private Color _prevSuffixColor = Color.clear;

    void OnEnable()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
        // 确保 Inspector 开启了富文本支持
        if (_textMesh != null) _textMesh.richText = true; 
        UpdateText(); // 激活时立刻刷新一次
    }

    void Update()
    {
        // 脏标记模式：只要有任何数值、设置、颜色、大小发生变化，就重新生成
        if (currentNumber != _previousNumber || 
            decimalPlaces != _previousDecimals || 
            useCommaSeparator != _previousComma ||
            numberSizeScale != _prevNumberSizeScale ||
            prefix != _prevPrefix || 
            suffix != _prevSuffix ||
            prefixColor != _prevPrefixColor ||
            numberColor != _prevNumberColor ||
            suffixColor != _prevSuffixColor)
        {
            UpdateText();
            
            _previousNumber = currentNumber;
            _previousDecimals = decimalPlaces;
            _previousComma = useCommaSeparator;
            _prevNumberSizeScale = numberSizeScale;
            _prevPrefix = prefix;
            _prevSuffix = suffix;
            _prevPrefixColor = prefixColor;
            _prevNumberColor = numberColor;
            _prevSuffixColor = suffixColor;
        }
    }

    private void UpdateText()
    {
        if (_textMesh == null) return;

        // 1. C# 字符串格式化逻辑 (提取数字)
        string format = useCommaSeparator ? "N" + decimalPlaces : "F" + decimalPlaces;
        string numStr = currentNumber.ToString(format);

        // 2. 将 Unity Color 转换为 TextMeshPro 能识别的 Hex 颜色代码
        string hexPrefix = ColorUtility.ToHtmlStringRGBA(prefixColor);
        string hexNumber = ColorUtility.ToHtmlStringRGBA(numberColor);
        string hexSuffix = ColorUtility.ToHtmlStringRGBA(suffixColor);

        // 3. 计算百分比大小 (1.5倍 -> 150)
        int sizePercentage = Mathf.RoundToInt(numberSizeScale * 100f);

        // 4. 构建富文本标签
        // 如果前缀或后缀为空，就不生成对应的颜色标签，节省性能
        string formattedPrefix = string.IsNullOrEmpty(prefix) ? "" : $"<color=#{hexPrefix}>{prefix}</color>";
        // 数字部分同时嵌套了 <size> 和 <color> 标签
        string formattedNumber = $"<size={sizePercentage}%><color=#{hexNumber}>{numStr}</color></size>";
        string formattedSuffix = string.IsNullOrEmpty(suffix) ? "" : $"<color=#{hexSuffix}>{suffix}</color>";

        // 5. 组合并赋值
        _textMesh.text = $"{formattedPrefix}{formattedNumber}{formattedSuffix}";
    }
}