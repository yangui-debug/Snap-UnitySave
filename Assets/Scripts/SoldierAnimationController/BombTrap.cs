using System.Collections;
using UnityEngine;

public class BombTrap : MonoBehaviour
{
    [Header("阵营与友伤设置")]
    [Tooltip("是否开启无差别攻击？勾选后炸所有角色；取消勾选则不炸同阵营角色")]
    public bool damageAllFactions = true; 
    [Tooltip("炸弹所属阵营（仅在取消勾选上方无差别攻击时生效）")]
    public FactionType bombFaction; // 直接复用你之前写的 FactionType 枚举

    [Header("瞬间伤害设置 (爆炸瞬间)")]
    [Tooltip("炸弹爆炸瞬间造成的伤害力度")]
    public int attackPower = 10; 
    public float explosionRadius = 5.0f;  // 爆炸和持续伤害的范围
    public GameObject explosionEffect;    // 爆炸瞬间的特效
    
    [Header("持续伤害设置 (火海/毒气)")]
    public bool enableContinuousDamage = true; // 是否开启持续伤害？
    [Tooltip("每次持续伤害扣除的血量")]
    public int continuousDamage = 2; 
    [Tooltip("这片伤害区域总共存在多少秒？")]
    public float continuousDuration = 5.0f; 
    [Tooltip("每隔多少秒扣一次血？(例如填1，就是1秒扣一次)")]
    public float damageInterval = 1.0f;
    [Tooltip("(可选) 持续留在地上的特效，比如火圈")]
    public GameObject continuousEffectPrefab; 

    [Header("触发设置")]
    public bool explodeOnTouch = false;    // 是否碰到就炸（踩踏陷阱）
    public string targetTag = "Soldier";  // 炸谁？
    
    private bool hasExploded = false;

    public void TriggerExplosion()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(fx, 2.0f);
        }

        DealDamageInArea(attackPower, true);

        HideBombMeshAndCollider();

        if (enableContinuousDamage)
        {
            StartCoroutine(ContinuousDamageRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ContinuousDamageRoutine()
    {
        GameObject contFx = null;
        if (continuousEffectPrefab != null)
        {
            contFx = Instantiate(continuousEffectPrefab, transform.position, Quaternion.identity);
        }

        float timer = 0f;
        while (timer < continuousDuration)
        {
            yield return new WaitForSeconds(damageInterval);
            timer += damageInterval;

            DealDamageInArea(continuousDamage, false);
        }

        if (contFx != null) Destroy(contFx);
        Destroy(gameObject);
    }

    // ==========================================
    // 核心修改：在结算伤害时进行阵营判断
    // ==========================================
    private void DealDamageInArea(int damageValue, bool applyKnockback)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hitCol in hitColliders)
        {
            if (hitCol.CompareTag(targetTag))
            {
                // 如果【不是无差别攻击】，就需要判断阵营
                if (!damageAllFactions)
                {
                    // 获取目标身上的动画控制器（里面存了目标阵营）
                    SoldierAnimationController targetController = hitCol.GetComponentInParent<SoldierAnimationController>();
                    
                    // 如果目标有阵营，并且和炸弹是同一个阵营，则跳过这次伤害（豁免）
                    if (targetController != null && targetController.myFaction == this.bombFaction)
                    {
                        continue; // 终止当前循环，直接检查下一个 collider
                    }
                }

                // 顺利通过阵营检查，开始扣血逻辑
                SoldierHealth health = hitCol.GetComponentInParent<SoldierHealth>();
                if (health != null && !health.isDead)
                {
                    Vector3 pushDirection = Vector3.zero;
                    
                    if (applyKnockback)
                    {
                        pushDirection = (hitCol.transform.position - transform.position);
                        pushDirection.y = 0; 
                        pushDirection = pushDirection.normalized;
                    }

                    health.TakeDamage(damageValue, pushDirection);
                }
            }
        }
    }

    private void HideBombMeshAndCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (explodeOnTouch && !hasExploded && other.CompareTag(targetTag))
        {
            TriggerExplosion();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}