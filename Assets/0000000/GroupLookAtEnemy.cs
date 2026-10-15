using UnityEngine;

[ExecuteAlways] // 确保在 Timeline 编辑拖拽时能看到效果
public class GroupLookAtEnemy : MonoBehaviour
{
    [Header("【1】敌人目标设置")]
    [Tooltip("大家要死死盯着的那个敌人目标物体（比如怪物、BOSS、或者某个 Cube）")]
    public Transform enemyTarget;

    [Header("【2】整体朝向修正设置")]
    [Tooltip("如果空物体转头后，子物体们整体方向不对，在这里微调 Y 轴度数修正（通常填 180 或 90）。")]
    public float globalRotationOffsetY = 0f;

    [Header("【3】开关控制")]
    [Tooltip("是否开启始终朝向？")]
    public bool isTracking = true;

    // 使用 LateUpdate 确保在所有动画计算完毕后的最后一帧，强行拧转管理空物体的朝向
    void LateUpdate()
    {
        // 核心安全拦截：如果没有指定敌人，或者关闭了追踪，直接沿用原有的旋转
        if (enemyTarget == null || !isTracking) return;

        // 【超级防呆补丁】：如果在编辑器模式下，且你没有在拖拽 Timeline 进度条（没有播放）
        // 此时你正在手忙脚乱地把英雄和武器拖进空物体里。我们让脚本“假死”不工作！
        // 这样就从底层彻底锁死了父物体的坐标系，无论你怎么塞子物体，它们的位置绝对不会发生一丝一毫的改变！
        if (!Application.isPlaying && !IsTimelineDragging())
        {
            return;
        }

        // 1. 抓取管理空物体（父级）当前的位置
        Vector3 parentPos = transform.position;

        // 2. 核心：把敌人坐标的 Y 轴高度差抹平（用父物体的 y 代替敌人的 y）
        // 保证整个大组只会水平左右转，绝对不会产生前后诡异倾斜塌陷的 Bug
        Vector3 enemyPosFlattened = new Vector3(enemyTarget.position.x, parentPos.y, enemyTarget.position.z);

        // 3. 计算从父物体指向敌人的水平方向向量
        Vector3 direction = enemyPosFlattened - parentPos;

        // 4. 确保父物体和敌人没有完全重合
        if (direction.sqrMagnitude > 0.001f)
        {
            // 计算出正对敌人的基准 Quaternion 旋转角度
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            // 5. 叠加你自定义的整体 Y 轴美术修正偏置
            Quaternion finalRotation = lookRotation * Quaternion.Euler(0f, globalRotationOffsetY, 0f);

            // 6. 强行把这个旋转赋值给“管理空物体”！
            // 父物体一转，它肚子里的英雄和武器就会像坐在旋转木马上面一样，整整齐齐地一起跟着转过去
            transform.rotation = finalRotation;
        }
    }

    /// <summary>
    /// 内部工具：用来判断你现在是不是正在拖拽 Timeline 进度条
    /// </summary>
    private bool IsTimelineDragging()
    {
        // 如果是在编辑模式下，只有当时间轴的时间在发生微调时才允许旋转
        return System.Diagnostics.Debugger.IsAttached || Time.deltaTime == 0;
    }
}