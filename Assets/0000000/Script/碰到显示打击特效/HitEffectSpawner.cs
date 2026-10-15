using UnityEngine;

public class HitEffectSpawner : MonoBehaviour
{
    [Header("【1】打击特效预制体")]
    [Tooltip("把你的粒子打中特效（比如火花、爆炸、劈砍特效）拖到这里")]
    public GameObject hitEffectPrefab;

    [Header("【2】触发标签过滤")]
    [Tooltip("只有带有这个 Tag 的物体撞过来才会触发特效。默认填 Cube 的标签（比如 Player 或 Cube）")]
    public string targetTag = "Cube";

    [Header("【3】特效生命周期")]
    [Tooltip("打击特效生成后，隔多少秒自动销毁？防止留在场景里卡顿。")]
    public float effectDestroyDelay = 2.0f;

    // ==================== 【全新新增：可调节特效高度】 ====================
    [Header("【4】特效生成高度微调")]
    [Tooltip("在此输入正数或负数，可以控制生成的特效在撞击点的基础上往上抬高或往下压低。默认是 0")]
    public float heightOffset = 0.0f;
    // =========================================================================

    /// <summary>
    /// 当有别的物体冲进我的“触发身体”里时，Unity 会自动调用这个方法
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 1. 安全拦截：检查撞过来的物体是不是我们指定的那个 Cube（根据 Tag 标签判断）
        if (other.CompareTag(targetTag))
        {
            // 2. 算出力学上的精准撞击点（如果对方有最接近的接触点就用接触点，没有就用对方的中心点）
            Vector3 spawnPosition = other.ClosestPointOnBounds(transform.position);

            // ==================== 【全新修改：应用高度偏移】 ====================
            // 直接在最终计算出来的撞击点 Y 轴上，叠加你设置的偏移高度值
            spawnPosition.y += heightOffset;
            // =========================================================================

            // 3. 在撞击位置，瞬间生出打击特效
            if (hitEffectPrefab != null)
            {
                // 生成特效，并让特效的旋转方向和预制体保持一致
                GameObject effectInstance = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);

                // 4. 判死刑：到了时间自动把这个打击特效销毁，不占内存
                Destroy(effectInstance, effectDestroyDelay);
            }

            // 【可选可选】：如果这个预制体是“一次性道具/金币/怪物”，被 Cube 撞完后自己也要消失，可以把下面这行的注释放开：
            // Destroy(gameObject);
        }
    }
}