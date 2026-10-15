using UnityEngine;

[ExecuteAlways] // 确保在编辑模式下拖拽 Timeline 进度条也能实时看到英雄转头
public class HeroLookAtEnemy : MonoBehaviour
{
    [Header("【1】敌人目标设置")]
    [Tooltip("英雄要死死盯着的那个敌人目标物体（比如怪物、BOSS、或者某个 Cube）")]
    public Transform enemyTarget;

    [Header("【2】朝向修正设置")]
    [Tooltip("如果英雄转头后，背对着敌人或者侧对着敌人（美术模型本身的正面轴向问题），在这里微调 Y 轴度数修正（通常填 180 或 90）")]
    public float rotationOffsetY = 0f;

    [Header("【3】开关控制")]
    [Tooltip("是否开启始终朝向？可以在 Timeline 里通过自定义事件或者关键帧来动态开关它")]
    public bool isTracking = true;

    // 使用 LateUpdate 确保在所有动画、Timeline 位移计算完毕后的最后一帧，强行拧转英雄的朝向
    void LateUpdate()
    {
        // 安全检查：如果没有指定敌人，或者关闭了追踪，直接沿用原有的旋转
        if (enemyTarget == null || !isTracking) return;

        // 1. 抓取英雄当前的位置
        Vector3 heroPos = transform.position;

        // 2. 核心：把敌人坐标的 Y 轴高度抹平（用英雄自己的 y 代替敌人的 y）
        // 这样计算出来的方向向量就只有水平面上的变化，保证英雄只会左右转，身体绝对不会前后倾斜塌陷！
        Vector3 enemyPosFlattened = new Vector3(enemyTarget.position.x, heroPos.y, enemyTarget.position.z);

        // 3. 计算从英雄指向敌人的方向向量
        Vector3 direction = enemyPosFlattened - heroPos;

        // 4. 确保英雄和敌人没有完全重合（距离太近会导致数学计算报错，产生乱抖）
        if (direction.sqrMagnitude > 0.001f)
        {
            // 计算出正对敌人的基准 Quaternion 旋转角度（以世界坐标的正上方 Vector3.up 为旋转轴）
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            // 5. 叠加你自定义的 Y 轴美术修正偏置
            Quaternion finalRotation = lookRotation * Quaternion.Euler(0f, rotationOffsetY, 0f);

            // 6. 强行覆盖当前帧英雄的旋转，死死控住朝向
            transform.rotation = finalRotation;
        }
    }
}