using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TextNumberAnimator : MonoBehaviour
{
    [Header("数字显示目标")]
    public TextMeshProUGUI targetText;

    [Header("动画驱动的值")]
    public float animatedValue = 0f;

    [Header("保留小数位数 (0-2)")]
    [Range(0, 2)]
    public int decimalPlaces = 0;

    private float _lastValue = -999999f;
    private int _lastDecimal = -1;

    void Update()
    {
        UpdateTextIfChanged();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UpdateTextIfChanged();
    }

    void OnEnable()
    {
        UnityEditor.EditorApplication.update += EditorUpdate;
    }

    void OnDisable()
    {
        UnityEditor.EditorApplication.update -= EditorUpdate;
    }

    void EditorUpdate()
    {
        if (!Application.isPlaying)
        {
            UpdateTextIfChanged();
        }
    }
#endif

    private void UpdateTextIfChanged()
    {
        if (targetText == null) return;

        if (_lastValue != animatedValue || _lastDecimal != decimalPlaces)
        {
            string format = "F" + decimalPlaces;
            targetText.text = animatedValue.ToString(format);
            _lastValue = animatedValue;
            _lastDecimal = decimalPlaces;
        }
    }
}
