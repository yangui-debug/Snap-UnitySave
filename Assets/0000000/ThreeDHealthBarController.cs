using UnityEngine;

[ExecuteAlways] // 确保在编辑器模式下拖拽 Timeline 进度条时，也能实时看到血条锁定角度跟随
public class ThreeDHealthBarController : MonoBehaviour
{
    [Header("【1】跟随目标设置")]
    [Tooltip("这个 3D 血条要挂在哪个怪物的头顶？（把怪物头顶创建的 HP_Anchor 拖进来）")]
    public Transform monsterHeadAnchor;

    [Tooltip("血条相对于怪物头顶的局部坐标偏移。")]
    public Vector3 positionOffset = new Vector3(0f, 0.5f, 0f);

    [Header("【2】固定旋转角度锁定")]
    [Tooltip("【核心修改】只要开启追踪，血条的世界旋转角度会被死死锁在这个度数上！")]
    public Vector3 fixedRotation = new Vector3(-20f, -180f, 0f);

    [Header("【3】开关控制")]
    public bool isTracking = true;

    // 使用 LateUpdate 确保在所有动画、Timeline 位移计算完毕后，我们最后出手锁死坐标和角度
    void LateUpdate()
    {
        // 安全检查：如果没有指定目标，或者关闭了追踪，直接沿用原样
        if (monsterHeadAnchor == null || !isTracking) return;

        // ================== 核心功能 1：绝对死死跟随怪物头顶 ==================
        transform.position = monsterHeadAnchor.position + positionOffset;

        // ================== 核心功能 2：绝对锁定固定旋转角度 ==================
        // 直接用面板上的固定度数生成 Quaternion，暴力锁死！
        // 这样无论怪物怎么转体，或者摄像机怎么位移，血条的旋转角度永远是 (X: -20, Y: -180, Z: 0)
        transform.rotation = Quaternion.Euler(fixedRotation);
    }
}