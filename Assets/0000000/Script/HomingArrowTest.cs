using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingArrowTest : MonoBehaviour
{
    [Header("飞行速度与时间设置")]
    [Tooltip("参考距离下的初始水平前向速度")]
    public float forwardSpeed = 20f;

    [Tooltip("初始侧向速度：垂直于出发点与命中点直线方向的速度")]
    public float initialLateralSpeed = 8f;

    [Tooltip("第二阶段（产生朝向目标速度拉回）的持续时间(秒)")]
    public float phase2Duration = 0.5f;

    [Tooltip("速度等于 Inspector 中基础值时对应的发射距离；实际发射时各方向速度按距离/此值缩放")]
    public float speedReferenceDistance = 0.1f;

    [Header("Y轴初始速度设置")]
    [Tooltip("参考距离下的初始垂直速度；实际发射时按距离/参考距离缩放")]
    public float initialVerticalSpeed = 16f;

    [Tooltip("瞄准偏移量")]
    public Vector3 aimOffset = new Vector3(0, 1.2f, 0);

    [Header("弹道与索敌机制")]
    [Tooltip("是否开启实时追踪")]
    public bool isHoming = true;
    [Tooltip("非追踪模式下的命中判定半径")]
    public float hitRadius = 1.5f;

    [Header("范围伤害 (AoE) 设置")]
    public bool isAoE = false;
    public float aoeRadius = 3.0f;

    [Header("命中后表现")]
    public bool destroyImmediately = false;
    public float stickTime = 3.0f;

    // 私有控制变量
    private Transform target;
    private GameObject[] hitEffects;
    private float effectDestroyTime;
    private int incomingDamage = 1;

    private Vector3 startPosition;
    private Vector3 lastTargetPos;
    private float elapsedTime = 0f;
    private bool hasHit = false;

    // 每次发射锁定的速度，避免目标移动后速度跟着改变
    private float distanceSpeedScale = 1f;
    private float shotForwardSpeed;
    private float shotLateralSpeed;
    private float shotVerticalSpeed;

    private float lateralSign = 1f; // 1 表示向左外扩，-1 表示向右外扩

    public void Fire(Transform enemyTarget, GameObject[] effects, float destroyTime, int damage)
    {
        // 支持对象池复用：每次发射都重新初始化状态
        elapsedTime = 0f;
        hasHit = false;

        target = enemyTarget;
        hitEffects = effects;
        effectDestroyTime = destroyTime;
        incomingDamage = damage;

        startPosition = transform.position;

        if (target != null)
        {
            lastTargetPos = target.position + aimOffset;
        }
        else
        {
            lastTargetPos = startPosition + transform.forward * 10f;
        }

        // 发射瞬间的三维距离决定本支箭的速度比例。
        float launchDistance = Vector3.Distance(startPosition, lastTargetPos);
        if (target != null)
        {
            Debug.Log($"[HomingArrowTest] 发射箭矢，目标：{target.name}，发射时距离：{launchDistance:F2} 米", this);
        }
        else
        {
            Debug.LogWarning($"[HomingArrowTest] 发射箭矢时目标为空，使用备用落点，距离：{launchDistance:F2} 米", this);
        }
        distanceSpeedScale = launchDistance / Mathf.Max(speedReferenceDistance, 0.01f);

        // 防止极近距离导致速度变成 0；真正的近距离命中仍由 distXZ 判定处理。
        distanceSpeedScale = Mathf.Max(distanceSpeedScale, 0.01f);

        float baseForwardSpeed = Mathf.Max(forwardSpeed, 0.1f);
        shotForwardSpeed = baseForwardSpeed * distanceSpeedScale;
        shotLateralSpeed = initialLateralSpeed * distanceSpeedScale;

        shotVerticalSpeed = initialVerticalSpeed * distanceSpeedScale;

        // 判断出发点位于命中点的世界坐标左侧还是右侧：
        // 出发点 X < 命中点 X -> 处于左侧，侧向速度向左
        // 出发点 X > 命中点 X -> 处于右侧，侧向速度向右
        // 出发点 X == 命中点 X -> 默认向左
        if (startPosition.x > lastTargetPos.x)
        {
            lateralSign = -1f; // 右侧向量
        }
        else
        {
            lateralSign = 1f;  // 左侧向量 (包含 < 与 ==)
        }
    }

    void Update()
    {
        if (hasHit) return;

        // 动态追踪：更新目标最新世界坐标
        bool isTargetAlive = (target != null && target.gameObject.activeInHierarchy);
        if (isTargetAlive && isHoming)
        {
            lastTargetPos = target.position + aimOffset;
        }

        // 计算 XZ 平面坐标基准
        Vector3 startXZ = new Vector3(startPosition.x, 0, startPosition.z);
        Vector3 targetXZ = new Vector3(lastTargetPos.x, 0, lastTargetPos.z);
        float distXZ = Vector3.Distance(startXZ, targetXZ);

        if (distXZ <= 0.01f)
        {
            HitTarget(isTargetAlive);
            return;
        }

        // 计算第一阶段（前向投影到达目标线）所需时间 T1
        float T1 = distXZ / Mathf.Max(shotForwardSpeed, 0.1f);
        float T2 = Mathf.Max(phase2Duration, 0.05f);
        float totalFlightTime = T1 + T2; // 总飞行时间

        elapsedTime += Time.deltaTime;

        // 飞行结束判定
        if (elapsedTime >= totalFlightTime)
        {
            transform.position = lastTargetPos;
            HitTarget(isTargetAlive);
            return;
        }

        // ==================== 1. XZ 平面速度分解与位置计算 ====================
        Vector3 forwardDir = (targetXZ - startXZ).normalized;
        Vector3 localLeftDir = Vector3.Cross(Vector3.up, forwardDir).normalized; // 本地左侧单位向量
        Vector3 sideDir = localLeftDir * lateralSign;                           // 根据规则确定的侧向向量

        // 第一阶段结束时的最大侧向偏移距离 L_max
        float L_max = 0.5f * shotLateralSpeed * T1;

        Vector3 currentXZ;

        if (elapsedTime <= T1)
        {
            // ---- 第一阶段：前向匀速 + 侧向线性减速至0 ----
            float t1 = elapsedTime;
            float x_dist = shotForwardSpeed * t1;
            // 侧向速度 v_lat(t) = shotLateralSpeed * (1 - t/T1)
            // 侧向位移为积分：y_dist = shotLateralSpeed * (t - t^2 / (2*T1))
            float y_dist = shotLateralSpeed * (t1 - (t1 * t1) / (2f * T1));

            currentXZ = startXZ + forwardDir * x_dist + sideDir * y_dist;
        }
        else
        {
            // ---- 第二阶段：超越目标线，并受到向目标靠拢的加速度 ----
            // 此时生成一个从 0 开始增大、方向朝向目标的速度，拉回箭矢
            float t2 = elapsedTime - T1;

            // 前向受反向速度作用，轨迹越过目标后回拢
            float x_dist = distXZ + shotForwardSpeed * t2 * (1f - t2 / T2);
            // 侧向位移由 L_max 加速归零
            float y_dist = L_max * (1f - (t2 * t2) / (T2 * T2));

            currentXZ = startXZ + forwardDir * x_dist + sideDir * y_dist;
        }

        // ==================== 2. Y 轴初速度与加速度计算 ====================
        // 以锁定的初始垂直速度起飞；加速度自动计算，保证飞行结束时到达目标高度。
        float verticalAcceleration = 2f * (lastTargetPos.y - startPosition.y - shotVerticalSpeed * totalFlightTime)
                                   / (totalFlightTime * totalFlightTime);
        float currentY = startPosition.y
                       + shotVerticalSpeed * elapsedTime
                       + 0.5f * verticalAcceleration * elapsedTime * elapsedTime;

        // 合成 3D 坐标
        Vector3 currentPos = new Vector3(currentXZ.x, currentY, currentXZ.z);

        // ==================== 3. 姿态朝向更新 ====================
        Vector3 moveDirection = currentPos - transform.position;
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        transform.position = currentPos;
    }

    void HitTarget(bool isTargetAlive)
    {
        hasHit = true;
        bool actuallyHitEnemy = false;
        Vector3 effectSpawnPosition = transform.position;

        if (isTargetAlive)
        {
            if (isHoming) { actuallyHitEnemy = true; }
            else
            {
                float distanceToEnemy = Vector3.Distance(transform.position, target.position + aimOffset);
                if (distanceToEnemy <= hitRadius) { actuallyHitEnemy = true; }
            }
        }

        if (actuallyHitEnemy)
        {
            effectSpawnPosition = target.position + aimOffset;
            if (isAoE)
            {
                Collider[] hitColliders = Physics.OverlapSphere(effectSpawnPosition, aoeRadius);
                foreach (Collider hitCol in hitColliders)
                {
                    SoldierHealth health = hitCol.GetComponentInParent<SoldierHealth>();
                    if (health != null && !health.isDead)
                    {
                        Vector3 hitDirection = (health.transform.position - effectSpawnPosition);
                        hitDirection.y = 0;
                        health.TakeDamage(incomingDamage, hitDirection.normalized);
                    }
                }
            }
            else
            {
                SoldierHealth health = target.GetComponent<SoldierHealth>();
                if (health != null && !health.isDead)
                {
                    Vector3 hitDirection = (target.position - transform.position);
                    hitDirection.y = 0;
                    health.TakeDamage(incomingDamage, hitDirection.normalized);
                }
            }

            if (hitEffects != null && hitEffects.Length > 0)
            {
                int randomIndex = Random.Range(0, hitEffects.Length);
                GameObject selectedEffect = hitEffects[randomIndex];
                if (selectedEffect != null)
                {
                    GameObject vfx = Instantiate(selectedEffect, effectSpawnPosition, Quaternion.identity);
                    Destroy(vfx, effectDestroyTime);
                }
            }

            if (!destroyImmediately && target != null && !isAoE)
            {
                SoldierHealth targetHealth = target.GetComponent<SoldierHealth>();
                if (targetHealth == null || !targetHealth.isDead)
                {
                    transform.SetParent(target);
                }
            }
        }

        if (destroyImmediately || (isAoE && actuallyHitEnemy))
        {
            Destroy(gameObject);
            return;
        }

        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null) { trail.emitting = false; }
        Destroy(gameObject, stickTime);
    }
}
