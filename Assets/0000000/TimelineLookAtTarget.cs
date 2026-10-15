using UnityEngine;

[ExecuteAlways] // 确保在编辑模式下拖拽 Timeline 进度条也能实时看到转头效果
public class TimelineLookAtTarget : MonoBehaviour
{
    [Header("【1】追踪目标设置")]
    [Tooltip("角色眼睛要死死盯着的那个目标物体（比如 BOSS、某个 Cube、或者移动的道具）")]
    public Transform target;

    [Header("【新增：武器保护】")]
    [Tooltip("【重要】把你不想跟着人物一起乱转的武器预制体/物体拖到这里。代码会保护它，让它的朝向独立，不受人物转体的影响。")]
    public Transform weaponTransform;

    [Header("【2】朝向修正设置")]
    [Tooltip("如果角色转头后，背对着目标或者侧对着目标（美术模型本身的正面轴向问题），在这里微调 Y 轴度数修正（通常填 180 或 90）")]
    public float rotationOffsetY = 0f;

    [Header("【3】开关控制")]
    [Tooltip("是否开启追踪？可以在 Timeline 里用自定义事件或者动画关键帧来动态开关它")]
    public bool isTracking = true;

    // 使用 LateUpdate 是解决 Timeline 动作冲突的唯一铁律！
    void LateUpdate()
    {
        // 安全检查：如果没有指定目标，或者关闭了追踪，直接沿用 Timeline 自身的旋转
        if (target == null || !isTracking) return;

        // 1. 计算从角色当前位置，指向目标的“平面方向向量”
        Vector3 currentPos = transform.position;
        Vector3 targetPosFlattened = new Vector3(target.position.x, currentPos.y, target.position.z);

        // 计算方向
        Vector3 direction = targetPosFlattened - currentPos;

        // 2. 确保角色和目标没有完全重合
        if (direction.sqrMagnitude > 0.001f)
        {
            // ============ 【核心修改：武器旋转拦截保护】 ============
            Quaternion originalWeaponRotation = Quaternion.identity;
            bool hasWeapon = (weaponTransform != null);

            if (hasWeapon)
            {
                // 在人物转体之前，先把武器当前在世界坐标系下的【绝对朝向】偷偷记录下来
                originalWeaponRotation = weaponTransform.rotation;
            }
            // ========================================================

            // 计算出正对目标的 Quaternion 旋转角度
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            // 叠加你自定义的 Y 轴美术修正偏置
            Quaternion finalRotation = lookRotation * Quaternion.Euler(0f, rotationOffsetY, 0f);

            // 强行覆盖当前帧人物的旋转！
            transform.rotation = finalRotation;

            // ============ 【核心修改：武器旋转强制还原】 ============
            if (hasWeapon)
            {
                // 人物身体转完之后，强行把武器的绝对朝向还原回去！
                // 这样无论人物怎么原地打转，武器都会在世界坐标下保持原有的角度（听从骨骼动作或者原动画的指挥）
                weaponTransform.rotation = originalWeaponRotation;
            }
            // ========================================================
        }
    }
}