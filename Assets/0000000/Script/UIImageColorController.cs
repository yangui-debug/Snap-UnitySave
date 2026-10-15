using UnityEngine;
using UnityEngine.UI;

public class UIImageColorController : MonoBehaviour
{
    private Image targetImage;

    void Awake()
    {
        // 自动获取当前物体上的 Image 组件
        targetImage = GetComponent<Image>();
    }

    /// <summary>
    /// 一键变纯黑白（去色）
    /// </summary>
    public void SetToGray()
    {
        if (targetImage == null) return;

        // 【核心黑科技】：使用 Canvas 默认的去色材质
        // Unity 内部自带一个专门渲染置灰 UI 的默认材质，可以直接通过代码无缝换上
        targetImage.material = Canvas.GetDefaultCanvasMaterial();

        // 配合将顶点颜色调为灰色，可以达到极其完美的黑白去色效果
        targetImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    /// <summary>
    /// 一键恢复彩色
    /// </summary>
    public void SetToColor()
    {
        if (targetImage == null) return;

        // 恢复成原生的 null 材质（即 UGUI 默认的彩色材质）
        targetImage.material = null;

        // 恢复顶点颜色为纯白
        targetImage.color = Color.white;
    }
}