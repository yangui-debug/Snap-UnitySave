using UnityEngine;
using UnityEngine.UI; // 如果你用的是TextMeshPro，这里换成 using TMPro;

/// <summary>
/// 挂在伤害飘字预制体上，控制它向上飘并逐渐消失
/// </summary>
public class DamageText : MonoBehaviour
{
    [Tooltip("拖入自身的 Text 组件")]
    public Text textComponent; // 如果你用的是TextMeshPro，换成 public TextMeshProUGUI textComponent;
    
    [Tooltip("向上飘移的速度")]
    public float floatSpeed = 2.0f;
    [Tooltip("文字消失变透明的速度")]
    public float fadeSpeed = 1.5f;

    private Color textColor;

    public void Setup(int damageAmount, bool isCrit)
    {
        if (textComponent == null) textComponent = GetComponent<Text>();
        
        textComponent.text = "-" + damageAmount.ToString();
        
        if (isCrit)
        {
            textComponent.color = Color.yellow; // 暴击是醒目的黄色
            textComponent.fontSize += 10;       // 暴击字号变大，更有爽感
            textComponent.text = "CRIT! " + textComponent.text;
        }
        else
        {
            textComponent.color = Color.red;    // 普通伤害是红色
        }
        
        textColor = textComponent.color;
        // 自动销毁，防止满屏文字导致手机发烫
        Destroy(gameObject, 2f);
    }

    void Update()
    {
        // 向上漂移
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // 逐渐变透明
        textColor.a -= fadeSpeed * Time.deltaTime;
        if (textComponent != null) textComponent.color = textColor;
    }
}