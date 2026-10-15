using UnityEngine;
using System.Collections.Generic;

public class LauncherProjectile : MonoBehaviour
{
    [Header("【飞行设置】")]
    public float flySpeed = 20f;
    public float hitCheckRadius = 1.0f; // 碰撞检测的判定大小
    public float maxLifeTime = 5.0f;    // 防止子弹永远飞行的兜底销毁时间

    [Header("【缩放出现/消失效果】")]
    public float scaleInSpeed = 10f; // 缩放的速度

    // === 接收发射器传来的参数 ===
    private Transform target;
    private FactionType spawnerFaction;
    private int damagePower;
    private WeaponLauncher.AttackMode mode;
    private bool tracking;
    private float aoeRange;
    private int remainingPierce;
    private Vector3 offset;
    private GameObject[] effects;
    private float effectTime;

    // === 距离控制变量 ===
    private Vector3 startPos;         // 记录发射时的初始位置
    private float maxFlyDistance;     // 最大飞行距离
    private bool isFadingOut = false; // 是否正在消散

    // === 内部状态 ===
    private Vector3 moveDirection;
    private Vector3 targetScale;
    private HashSet<GameObject> alreadyHitObjects = new HashSet<GameObject>(); // 用于穿透模式，防止对同一人造成多次伤害

    public void Initialize(Transform targetTrans, FactionType faction, int power, WeaponLauncher.AttackMode atkMode, bool isTracking, float aoeRad, int pierceNum, Vector3 aimOffset, GameObject[] hitVfx, float vfxTime, float maxDist)
    {
        target = targetTrans;
        spawnerFaction = faction;
        damagePower = power;
        mode = atkMode;
        tracking = isTracking;
        aoeRange = aoeRad;
        remainingPierce = pierceNum;
        offset = aimOffset;
        effects = hitVfx;
        effectTime = vfxTime;

        // 记录起始位置和最大射程
        startPos = transform.position;
        maxFlyDistance = maxDist;

        // 1. 设置初始飞行的方向（如果不追踪，就按这个方向一直飞）
        if (target != null)
        {
            moveDirection = (target.position + offset - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // 2. 武器 0~1 的缩放逻辑初始化
        targetScale = transform.localScale; // 记住预制体原本的大小
        transform.localScale = Vector3.zero; // 初始设为 0

        // 3. 兜底销毁
        Destroy(gameObject, maxLifeTime);
    }

    void Update()
    {
        // === 如果处于消散状态，执行缩小消失逻辑 ===
        if (isFadingOut)
        {
            // 平滑缩小到 0
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, scaleInSpeed * Time.deltaTime);
            // 顺着惯性继续往前飞一点
            transform.position += transform.forward * flySpeed * Time.deltaTime;
            
            // 彻底缩小没影了，就销毁
            if (transform.localScale == Vector3.zero)
            {
                Destroy(gameObject);
            }
            return; // 处于消散状态时，直接 return，不再追踪，也不再检测伤害
        }

        // === 正常的缩放出现逻辑 ===
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, scaleInSpeed * Time.deltaTime);
        }

        // === 移动与追踪逻辑 ===
        if (tracking && target != null && target.gameObject.activeInHierarchy)
        {
            // 实时修正方向
            moveDirection = (target.position + offset - transform.position).normalized;
        }
        
        // 向前飞行
        if (moveDirection != Vector3.zero)
        {
            transform.position += moveDirection * flySpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // === 检查是否飞出最大射程 ===
        if (Vector3.Distance(startPos, transform.position) >= maxFlyDistance)
        {
            isFadingOut = true; // 触发消散状态，能量耗尽
            return; // 马上返回，避免在超界的一瞬间还去触发伤害
        }

        // === 命中检测逻辑 ===
        CheckHit();
    }

    private void CheckHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitCheckRadius);
        bool shouldDestroySelf = false;

        foreach (Collider hitCol in hits)
        {
            GameObject hitObj = hitCol.gameObject;

            // 1. 验证是否是合法敌人
            if (!IsValidEnemy(hitObj)) continue;
            // 2. 验证是否已经打过这个人 (防止穿透模式在一帧内重复判定)
            if (alreadyHitObjects.Contains(hitObj)) continue;

            // 根据不同的攻击模式产生效果
            switch (mode)
            {
                case WeaponLauncher.AttackMode.Single:
                    ApplyDamage(hitObj);
                    // 传入 hitObj.transform 让特效跟随敌人
                    SpawnEffect(hitObj.transform.position + offset, hitObj.transform); 
                    shouldDestroySelf = true; 
                    break;

                case WeaponLauncher.AttackMode.AoE:
                    // 获取爆炸范围内的所有人
                    Collider[] aoeHits = Physics.OverlapSphere(transform.position, aoeRange);
                    foreach (Collider aoeCol in aoeHits)
                    {
                        if (IsValidEnemy(aoeCol.gameObject))
                        {
                            ApplyDamage(aoeCol.gameObject);
                        }
                    }
                    // AoE 爆炸在原地触发并自动放大，不需要跟随某个单独敌人，传 null 即可
                    SpawnEffect(transform.position, null);
                    shouldDestroySelf = true;
                    break;

                case WeaponLauncher.AttackMode.Pierce:
                    ApplyDamage(hitObj);
                    // 传入 hitObj.transform 让特效跟随着被穿透的敌人
                    SpawnEffect(hitObj.transform.position + offset, hitObj.transform); 
                    alreadyHitObjects.Add(hitObj); // 记录这个人，下次不再造成伤害
                    
                    remainingPierce--;
                    if (remainingPierce <= 0)
                    {
                        shouldDestroySelf = true;
                    }
                    break;
            }

            // 如果触发了销毁条件，立刻跳出循环
            if (shouldDestroySelf) break;
        }

        if (shouldDestroySelf)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDamage(GameObject enemyObj)
    {
        SoldierHealth health = enemyObj.GetComponent<SoldierHealth>();
        if (health != null && !health.isDead)
        {
            // 计算击退方向（水平向后）
            Vector3 knockbackDir = (enemyObj.transform.position - transform.position);
            knockbackDir.y = 0;
            
            // 直接传递 attackPower 给 SoldierHealth
            health.TakeDamage(damagePower, knockbackDir.normalized);
        }
    }

    private void SpawnEffect(Vector3 pos, Transform hitParent = null)
    {
        if (effects != null && effects.Length > 0)
        {
            // 随机抽取一个特效
            GameObject randomEffect = effects[Random.Range(0, effects.Length)];
            if (randomEffect != null)
            {
                // 生成特效
                GameObject vfx = Instantiate(randomEffect, pos, Quaternion.identity);
                
                // 1. 如果有命中的敌人，并且不是 AoE 模式，就让特效作为敌人的子物体（跟随敌人移动）
                if (hitParent != null && mode != WeaponLauncher.AttackMode.AoE)
                {
                    vfx.transform.SetParent(hitParent);
                }

                // 2. 如果是 AoE 模式，自动放大特效来匹配你设置的爆炸范围
                if (mode == WeaponLauncher.AttackMode.AoE)
                {
                    // 将特效等比例放大到 AoE 设定的范围
                    vfx.transform.localScale = Vector3.one * aoeRange; 
                }

                // 延迟销毁特效
                Destroy(vfx, effectTime);
            }
        }
    }

    private bool IsValidEnemy(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy) return false;

        SoldierAnimationController other = obj.GetComponent<SoldierAnimationController>();
        // 判断阵营，不打自己人
        if (other != null && other.myFaction != spawnerFaction)
        {
            SoldierHealth health = obj.GetComponent<SoldierHealth>();
            if (health != null && !health.isDead) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitCheckRadius);

        // 如果在 Inspector 选了 AoE 模式，画出爆炸范围预演
        if (mode == WeaponLauncher.AttackMode.AoE)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, aoeRange);
        }
    }
}