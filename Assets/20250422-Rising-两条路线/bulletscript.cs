using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public GameObject bloodEffectPrefab; // 流血特效预制体

    void OnCollisionEnter(Collision collision)
    {
        // 检查子弹是否击中了怪物
        if (collision.gameObject.CompareTag("Monster"))
        {
            // 在怪物位置播放流血特效
            TriggerBloodEffect(collision.transform.position);

            // 销毁子弹（如果需要）
            Destroy(gameObject);
        }
    }

    // 播放流血特效
    void TriggerBloodEffect(Vector3 position)
    {
        // Instantiate 创建特效并设置位置
        Instantiate(bloodEffectPrefab, position, Quaternion.identity);
    }
}
