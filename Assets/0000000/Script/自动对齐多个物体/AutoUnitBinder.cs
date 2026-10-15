using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways] // 让它在编辑模式下也实时同步，方便你拖动 Timeline 时间轴预览
public class AutoUnitAligner : MonoBehaviour
{
    [Header("【有色/活着的士兵列表】")]
    [Tooltip("作为位置基准的、有颜色的物体（Timeline 带着它们位移）")]
    public List<GameObject> colorUnits = new List<GameObject>();

    [Header("【无色/死后的士兵列表】")]
    [Tooltip("需要对齐位置的、没上色的物体（顺序需要和上面一一对应）")]
    public List<GameObject> colorlessUnits = new List<GameObject>();

    [Header("【对齐设置】")]
    [Tooltip("是否实时同步位置（开启后，在 Timeline 拖动预览时无色小兵会自动吸附到有色小兵身上）")]
    public bool realTimeSync = true;

    private void Update()
    {
        if (realTimeSync)
        {
            AlignAll();
        }
    }

    [ContextMenu("手动一键对齐 (编辑器下可用)")]
    public void AlignAll()
    {
        if (colorUnits == null || colorlessUnits == null) return;

        int count = Mathf.Min(colorUnits.Count, colorlessUnits.Count);
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject colorUnit = colorUnits[i];
            GameObject colorlessUnit = colorlessUnits[i];

            if (colorUnit == null || colorlessUnit == null) continue;

            // 强行让无色士兵的位置、旋转、缩放死死贴住有色士兵
            colorlessUnit.transform.position = colorUnit.transform.position;
            colorlessUnit.transform.rotation = colorUnit.transform.rotation;
            colorlessUnit.transform.localScale = colorUnit.transform.localScale;
        }
    }
}