using UnityEngine;

public class AutoDamagePopup : MonoBehaviour
{
    [Header("生成配置")]
    public GameObject damagePopupPrefab; // 拖入飘字特效的预制体
    public Transform headAnchor;         // 生成位置（如头部节点），如果不填默认用自身位置

    [Header("时间控制")]
    public float delayTime = 0f;         // 延迟几秒后飘出（0表示瞬间飘出）

    [Header("伤害数值范围设置")]
    public int minDamage = 100;
    public int maxDamage = 200;
    public int critThreshold = 150;

    // Timeline 激活这个模型时，会自动调用 OnEnable
    private void OnEnable()
    {
        if (delayTime > 0f)
        {
            Invoke(nameof(SpawnDamage), delayTime);
        }
        else
        {
            SpawnDamage();
        }
    }

    // Timeline 隐藏/销毁这个模型时，取消还没来得及生成的飘字，防止报错
    private void OnDisable()
    {
        CancelInvoke(nameof(SpawnDamage));
    }

    private void SpawnDamage()
    {
        Vector3 spawnPos = headAnchor != null ? headAnchor.position : transform.position;
        spawnPos += Vector3.up * 0.5f;

        if (damagePopupPrefab != null)
        {
            GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
            
            DamagePopup script = popupObj.GetComponent<DamagePopup>();
            if (script != null)
            {
                script.Setup(minDamage, maxDamage, critThreshold); 
            }
        }
    }
}