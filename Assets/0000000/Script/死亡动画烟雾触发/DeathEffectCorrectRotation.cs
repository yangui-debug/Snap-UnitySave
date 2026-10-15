using UnityEngine;

// 脚本名称：死亡动画特效（进阶修正版）
public class DeathEffectCorrectRotation : MonoBehaviour
{
    [Header("1. 拖入你的烟雾特效预制体")]
    public GameObject deathSmokePrefab; 
    
    [Header("2. 拖入角色身上落地的骨骼 (比如 Bip001)")]
    public Transform spawnPoint; // 继续使用它获取位置

    // 动画事件触发的方法
    // Trigger = 触发，Death = 死亡，SmokeEffect = 烟雾特效
    public void TriggerDeathSmokeEffectCorrectRotation() 
    {
        if (deathSmokePrefab != null && spawnPoint != null)
        {
            // *** 核心修改点 ***
            // 1. 获取 Bip001 在平躺时飞出去的地面位置。
            Vector3 targetPosition = spawnPoint.position;

            // 2. 我们不能使用平躺骨骼的旋转。
            // 相反，我们计算一个“修正”过的旋转：
            // 让特效的前方保持和角色父物体的一致 (transform.forward)，
            // 但它的上方向 (Up) 必须是世界坐标系的正上方 (Vector3.up)。
            // 这样烟雾看起来就是从地面正立爆开的。
            Quaternion correctedRotation = Quaternion.LookRotation(transform.forward, Vector3.up);

            // 在指定骨骼的位置，生成一个方向被纠正为正立的特效
            Instantiate(deathSmokePrefab, targetPosition, correctedRotation);
        }
        else
        {
            Debug.LogWarning("特效或生成点没有赋值！");
        }
    }
}