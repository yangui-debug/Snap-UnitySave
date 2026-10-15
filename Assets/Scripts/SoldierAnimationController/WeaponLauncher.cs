using UnityEngine;

public class WeaponLauncher : MonoBehaviour
{
    public enum AttackMode { Single, AoE, Pierce }

    [Header("【基础阵营与目标设置】")]
    public FactionType myFaction;
    public string targetTag = "Soldier";

    [Header("【发射器控制】")]
    [Tooltip("每次发射的间隔时间（秒）")]
    public float fireInterval = 2.0f;
    [Tooltip("发射点位置")]
    public Transform firePoint;
    [Tooltip("投射物预制体 (必须挂载 LauncherProjectile 脚本)")]
    public GameObject projectilePrefab;
    [Tooltip("该发射器的攻击力度，直接传给 SoldierHealth")]
    public int attackPower = 2;

    [Header("【视野与索敌设置】")]
    public float detectRadius = 12.0f;
    [Range(0, 360)]
    public float fovAngle = 90.0f;
    [Tooltip("目标瞄准的Y轴偏移量（打胸口）")]
    public Vector3 aimOffset = new Vector3(0, 1.2f, 0);

    [Header("【投射物模式设置】")]
    [Tooltip("攻击模式：Single(单体), AoE(范围), Pierce(穿透)")]
    public AttackMode attackMode = AttackMode.Single;
    [Tooltip("是否追踪敌人？如果不追踪，则只向发射时的目标位置直线飞行")]
    public bool isTracking = true;

    [Header("【特殊模式参数】")]
    [Tooltip("如果是AoE模式，爆炸的范围")]
    public float aoeRadius = 3.0f;
    [Tooltip("如果是Pierce模式，最大可穿透的敌人数量")]
    public int maxPierceCount = 3;

    [Header("【特效设置】")]
    public GameObject[] hitEffects;
    public float effectDestroyTime = 2.0f;

    private float fireTimer = 0f;

    void Update()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            Transform bestTarget = FindBestTargetInFOV();
            if (bestTarget != null)
            {
                FireWeapon(bestTarget);
                fireTimer = fireInterval; // 重置发射间隔
            }
        }
    }

    private Transform FindBestTargetInFOV()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectRadius);
        Transform bestTarget = null;
        float minDistance = float.MaxValue;

        foreach (Collider col in hitColliders)
        {
            if (IsValidEnemy(col.gameObject))
            {
                Vector3 dirToTarget = (col.transform.position - transform.position);
                dirToTarget.y = 0; // 只计算水平夹角

                if (Vector3.Angle(transform.forward, dirToTarget) <= fovAngle / 2f)
                {
                    float dist = dirToTarget.sqrMagnitude;
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestTarget = col.transform;
                    }
                }
            }
        }

        return bestTarget;
    }

    private void FireWeapon(Transform target)
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            LauncherProjectile projectile = projObj.GetComponent<LauncherProjectile>();

            if (projectile != null)
            {
                // 初始化发射物，传递所有自定义参数，并加上 detectRadius + 1.0f 作为最大飞行距离
                projectile.Initialize(
                    target, 
                    myFaction, 
                    attackPower, 
                    attackMode, 
                    isTracking, 
                    aoeRadius, 
                    maxPierceCount, 
                    aimOffset, 
                    hitEffects, 
                    effectDestroyTime,
                    detectRadius + 1.0f // 加上一点缓冲距离，让子弹飞出边缘一点点再消失
                );
            }
        }
    }

    private bool IsValidEnemy(GameObject obj)
    {
        if (obj == this.gameObject || !obj.activeInHierarchy) return false;

        if (obj.CompareTag(targetTag))
        {
            // 获取目标的阵营，确保不打自己人
            SoldierAnimationController other = obj.GetComponent<SoldierAnimationController>();
            if (other != null && other.myFaction != this.myFaction)
            {
                SoldierHealth health = obj.GetComponent<SoldierHealth>();
                if (health != null && !health.isDead) return true;
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Vector3 leftBoundary = Quaternion.Euler(0, -fovAngle / 2f, 0) * transform.forward * detectRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, fovAngle / 2f, 0) * transform.forward * detectRadius;

        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}