using UnityEngine;

[ExecuteAlways] // 确保在 Timeline 编辑模式下拖拽也能实时看到转头和倾斜
public class DirectLookAtEnemy : MonoBehaviour
{
    [Header("【1】敌人目标设置")]
    [Tooltip("大家要死死盯着的那个敌人目标物体")]
    public Transform enemyTarget;

    [Header("【2】强行控制的子物体（支持多个）")]
    [Tooltip("把你那两个在 Timeline 里调动画的【英雄】和【武器】子物体直接拖进这个列表里！")]
    public Transform[] targetTransforms;

    [Header("【3】朝向与倾斜修正设置")]
    [Tooltip("【新增】手动调节或修正 X 轴的旋转角度（通常用来调上下低头、抬头倾斜）。")]
    public float rotationOffsetX = 0f;

    [Tooltip("如果转头后水平方向不对，在这里微调 Y 轴度数修正（通常填 180 或 90）")]
    public float rotationOffsetY = 0f;

    [Header("【4】开关控制")]
    [Tooltip("是否开启始终朝向？")]
    public bool isTracking = true;

    // LateUpdate 是截获并强改 Timeline 动画数据的唯一铁律
    void LateUpdate()
    {
        // 安全检查
        if (enemyTarget == null || !isTracking || targetTransforms == null || targetTransforms.Length == 0) return;

        // 无论游戏运行还是 Timeline 拖拽，都在最后一帧强行拧转
        for (int i = 0; i < targetTransforms.Length; i++)
        {
            Transform t = targetTransforms[i];
            if (t == null) continue;

            // 1. 核心：位置绝对不动！死死保留子物体当前在 Timeline 里被算出来的坐标
            Vector3 currentPos = t.position;

            // 2. 抹平 Y 轴高度差，实现纯粹的水平面转头
            Vector3 enemyPosFlattened = new Vector3(enemyTarget.position.x, currentPos.y, enemyTarget.position.z);

            // 3. 计算从该子物体指向敌人的水平方向向量
            Vector3 direction = enemyPosFlattened - currentPos;

            if (direction.sqrMagnitude > 0.001f)
            {
                // 计算出正对敌人的基础水平世界旋转角度（只包含了 Y 轴偏转）
                Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

                // 4. 【核心升级】：同时叠加 X 轴（上下低头）和 Y 轴（左右转头）的美术修正偏置
                // 乘法的顺序很重要，先给它注入你指定的 X 轴上下倾斜，再乘以基础的看敌方向
                Quaternion finalRotation = lookRotation * Quaternion.Euler(rotationOffsetX, rotationOffsetY, 0f);

                // 5. 暴力覆盖！无视 Timeline 的动画锁死，强行把计算好的旋转塞给它
                // 因为我们只改了 t.rotation，完全没碰 t.position，所以位置雷打不动！
                t.rotation = finalRotation;
            }
        }
    }
}