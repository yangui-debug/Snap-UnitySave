using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A lethal blast with stylized, collider-free victim flights.
/// Put this on a bomb instead of BombTrap, never alongside it.
/// The flight prefab must be a visual-only soldier (no SoldierHealth or auto-launch script).
/// </summary>
public class BombTrapSkyLaunch : MonoBehaviour
{
    [Header("阵营 / 目标")]
    [Tooltip("勾选：无差别炸所有目标；取消：跳过与炸弹同阵营的士兵。测试画面时可先勾选。")]
    public bool damageAllFactions = true;
    [Tooltip("炸弹所属阵营；只有关闭 Damage All Factions 时才用于友军豁免。")]
    public FactionType bombFaction;
    [Tooltip("目标 Tag，通常填 Soldier。平面检测时可在士兵根节点或其碰撞体上；3D 检测时必须在被命中的 Collider 上。")]
    public string targetTag = "Soldier";
    [Tooltip("勾选后，带目标 Tag 的碰撞体进入炸弹 Trigger 时自动引爆；使用 Timeline Signal 时建议关闭。")]
    public bool explodeOnTouch = false;

    [Header("爆炸")]
    [Tooltip("爆炸范围，世界单位。开启平面检测时看 XZ 平面半径；先试 5，再对照 Scene 视图的圆圈调整。")]
    [Min(0f)] public float explosionRadius = 5f;
    [Tooltip("默认关闭，使用 3D 球形范围检测，可避免上下层同 XZ 的士兵一起被炸。只在目标碰撞体无法命中时才开启 XZ 平面检测。")]
    public bool usePlanarSoldierDetection = false;
    [Tooltip("只在平面检测时生效。Y 高差超过此值不命中；0 表示完全忽略 Y，会同时炸到上下层！分层场景建议填 1～3。")]
    [Min(0f)] public float maximumVerticalDifference = 2f;
    [Tooltip("排查时勾选：每次爆炸在 Console 打印找到的士兵数、平面范围内数、Tag 合格数和实际击飞数。确认效果后可关闭。")]
    public bool logTargetDiagnostics = true;
    [Tooltip("爆炸瞬间生成的粒子或动画预制体；可留空。建议中心对齐炸弹位置。")]
    public GameObject explosionEffect;
    [Tooltip("爆炸特效自动销毁时间（秒）。建议略长于特效本身，通常 1.5～3；填 0 不自动销毁。")]
    [Min(0f)] public float explosionEffectLifetime = 2f;
    [Tooltip("仅在关闭致命击飞，或飞行预制体未配置/不合要求时生效。走 SoldierHealth 扣血；想立刻击杀可设为不低于士兵 Max Hits。")]
    public int attackPower = 10;
    [Tooltip("建议勾选以获得参考视频的成片飞人效果：命中者立刻退出战斗，不检查剩余血量，也不触发 SoldierHealth 的旧死亡替身。")]
    public bool lethalSkyLaunch = true;
    [Tooltip("建议开启：每名士兵优先使用自己的 SoldierHealth → Death Effect Prefab。不同兵种会自动采用不同外观，无需在炸弹上重复配表。")]
    public bool preferSoldierDeathPrefab = true;
    [Tooltip("备用的单兵飞行预制体。士兵未配置 Death Effect Prefab，或关闭上方优先选项时使用。飞行副本的生命、AI、旧击飞和物理组件会自动关闭。")]
    public GameObject flyingSoldierPrefab;
    [Tooltip("建议勾选：飞行替身继承被炸士兵的朝向。若替身模型预先摆好了特殊姿态，可关闭。")]
    public bool copySoldierRotation = true;
    [Tooltip("默认关闭并使用飞行预制体的尺寸。只有预制体根缩放为 (1,1,1) 且模型尺寸与原士兵一致时才建议勾选。")]
    public bool copySoldierWorldScale = false;

    [Header("飞行轨迹：不需要地面 Collider")]
    [Tooltip("最快的飞行时长（秒）。建议 0.55～0.8；与最大值形成随机差异。")]
    [Min(0.01f)] public float minimumFlightSeconds = 0.65f;
    [Tooltip("最慢的飞行时长（秒）。建议 1～1.4；越大越像缓慢漂浮。")]
    [Min(0.01f)] public float maximumFlightSeconds = 1.1f;
    [Tooltip("从爆点向外飞的最短水平距离，世界单位。建议约为爆炸半径的 0.4～0.6 倍。")]
    [Min(0f)] public float minimumHorizontalDistance = 2f;
    [Tooltip("从爆点向外飞的最远水平距离，世界单位。先试与爆炸半径相近；太大会飞出镜头。")]
    [Min(0f)] public float maximumHorizontalDistance = 5f;
    [Tooltip("水平位移的起步冲劲。1=匀速；2～3=爆炸瞬间冲出大半距离，后段逐渐减速。建议 2.5。")]
    [Min(1f)] public float horizontalBurstPower = 2.5f;
    [Tooltip("最小世界 Y 方向抛物线高度。先试 1.5；纯俯视镜头可主要调 Screen Up Arc。")]
    [Min(0f)] public float minimumArcHeight = 1.5f;
    [Tooltip("最大世界 Y 方向抛物线高度。先试 3.5；镜头太俯视时单独增大此值可能仍不明显。")]
    [Min(0f)] public float maximumArcHeight = 3.5f;
    [Tooltip("俯视镜头的画面向上附加弧线。建议 1～2.5；世界 Y 抬高不明显时增加它，填 0 关闭。它会实际改变 XZ 位置。")]
    [Min(0f)] public float screenUpArc = 1.5f;
    [Tooltip("使用 Screen Up Arc 的镜头方向。多相机/Recorder 场景建议指定实际录制相机；留空则寻找带 MainCamera Tag 的主相机。")]
    public Camera viewCamera;
    [Tooltip("不同士兵起飞延迟范围 0～此值（秒）。建议 0.03～0.12；填 0 为同时起飞。")]
    [Min(0f)] public float maximumLaunchDelay = 0.08f;
    [Tooltip("偏离爆点径向的随机角度范围（±度）。先试 25～45；填 0 则严格向外飞。")]
    [Range(0f, 180f)] public float directionJitterDegrees = 35f;
    [Tooltip("X=最少翻滚圈数，Y=最多圈数；每人随机取值。建议 (0.75, 2)，填 (0, 0) 则不翻滚。")]
    public Vector2 tumbleTurns = new Vector2(0.75f, 2f);

    [Header("90° 俯视镜头的飞起感")]
    [Tooltip("飞行中点的模型放大倍率，用尺寸模拟向镜头飞来。1=不放大；90° 俯视建议先试 1.8～2.5，太大会互相遮挡。")]
    [Min(1f)] public float peakFlightScale = 2f;
    [Tooltip("建议勾选：绕镜头视线轴旋转，90° 俯视时能明显看到人物转动。原本的 X 轴翻滚仍可同时存在。")]
    public bool spinInCameraPlane = true;
    [Tooltip("X=俯视旋转最少圈数，Y=最多圈数，随机顺/逆时针。建议 (0.5, 1.5)；设为 (0, 0) 关闭此旋转。")]
    public Vector2 cameraSpinTurns = new Vector2(0.5f, 1.5f);

    public enum LandingHeightMode { EachSoldierStartY, FixedWorldY }
    [Header("虚拟落点高度")]
    [Tooltip("没有地面 Collider 也能落下。与士兵起点同高选 EachSoldierStartY；要落到固定世界高度选 FixedWorldY。")]
    public LandingHeightMode landingHeightMode = LandingHeightMode.EachSoldierStartY;
    [Tooltip("只有选择 FixedWorldY 才使用。填想象中地面/桥面的世界 Y 坐标，建议用 Scene 视图核对。")]
    public float fixedWorldY = 0f;
    [Tooltip("在所选落点高度基础上再降低。建议 0：落在同高位置；2～5：看起来掉出桥面。此值也会降低盒子和落地特效。")]
    [Min(0f)] public float landingDrop = 0f;
    [Tooltip("落点高度随机浮动 ±此值。地面或盒子需要整齐时填 0；仅表现坠落可试 0.2～0.6。")]
    [Min(0f)] public float landingHeightJitter = 0f;

    [Header("落地成盒 / 特效")]
    [Tooltip("落下时生成一次的烟尘、闪光等特效；可留空。没有地面也会生成在虚拟落点。")]
    public GameObject landingEffectPrefab;
    [Tooltip("落地特效自动销毁时间（秒）。建议 1～3；填 0 表示不自动销毁。")]
    [Min(0f)] public float landingEffectLifetime = 2f;
    [Tooltip("落地生成的盒子预制体；可留空。场景没有可见地面时，盒子可能悬在空中，请调落点或仅使用特效。")]
    public GameObject landingBoxPrefab;
    [Tooltip("盒子自动销毁时间（秒）。建议 0 保留；要画面恢复干净可试 3～6。")]
    [Min(0f)] public float landingBoxLifetime = 0f;
    [Tooltip("建议勾选：到达落点后小兵快速缩小，配合盒子或烟尘遮挡替换瞬间。关闭则直接消失。")]
    public bool shrinkOnLanding = true;
    [Tooltip("落地缩小所用时间（秒）。建议 0.08～0.15；填 0 则直接消失。")]
    [Min(0f)] public float landingShrinkSeconds = 0.1f;

    [Header("原版持续伤害（可选）")]
    [Tooltip("默认关闭。勾选后，爆炸结束仍在范围内的幸存士兵继续按间隔扣血；首次被抛飞者已经退出战斗。")]
    public bool enableContinuousDamage = false;
    [Tooltip("每次持续伤害的扣血数，只影响仍在爆炸范围内的活着的士兵。建议先试 1～2。")]
    public int continuousDamage = 2;
    [Tooltip("持续伤害总时间（秒）。建议 3～5；填 0 不产生持续区域。")]
    [Min(0f)] public float continuousDuration = 5f;
    [Tooltip("每隔多少秒造成一次持续伤害。建议 0.5～1；必须小于等于持续时间才会造成伤害。")]
    [Min(0.02f)] public float damageInterval = 1f;
    [Tooltip("持续伤害期间显示的地面/空中范围特效；可留空。没有地面时建议用悬浮烟雾或光圈。")]
    public GameObject continuousEffectPrefab;

    private bool hasExploded;
    private bool continuousFinished;
    private int activeFlights;
    private bool warnedAboutPrefab;

    // Timeline Signal Receiver can select this public method, just as with BombTrap.
    public void TriggerExplosion()
    {
        if (hasExploded) return;
        hasExploded = true;

        SpawnEffect(explosionEffect, transform.position, explosionEffectLifetime);
        DealDamageInArea(true);
        HideBombMeshAndCollider();

        if (enableContinuousDamage && continuousDuration > 0f)
        {
            StartCoroutine(ContinuousDamageRoutine());
        }
        else
        {
            continuousFinished = true;
            TryFinish();
        }
    }

    private void DealDamageInArea(bool initialBlast)
    {
        List<SoldierHealth> targets = new List<SoldierHealth>();
        HashSet<SoldierHealth> visited = new HashSet<SoldierHealth>();
        int found = 0;
        int inRange = 0;
        int tagMatched = 0;

        if (usePlanarSoldierDetection)
        {
            // This mode is suitable for suspended stages and does not require a collider.
            SoldierHealth[] soldiers = FindObjectsByType<SoldierHealth>(FindObjectsSortMode.None);
            float radiusSquared = explosionRadius * explosionRadius;
            foreach (SoldierHealth health in soldiers)
            {
                if (health == null || !health.enabled || health.isDead) continue;
                found++;
                Vector3 position = health.击退位移节点 != null
                    ? health.击退位移节点.position : health.transform.position;
                Vector3 delta = position - transform.position;
                if (delta.x * delta.x + delta.z * delta.z > radiusSquared) continue;
                if (maximumVerticalDifference > 0f &&
                    Mathf.Abs(delta.y) > maximumVerticalDifference) continue;
                inRange++;
                if (!HasTargetTag(health)) continue;
                tagMatched++;
                targets.Add(health);
            }
        }
        else
        {
            // Keep the original 3D Physics.OverlapSphere behaviour as an option.
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            found = colliders.Length;
            foreach (Collider hit in colliders)
            {
                inRange++;
                if (!hit.CompareTag(targetTag)) continue;
                tagMatched++;
                SoldierHealth health = hit.GetComponentInParent<SoldierHealth>();
                if (health != null && health.enabled && !health.isDead && visited.Add(health))
                    targets.Add(health);
            }
        }

        int friendlySkipped = 0;
        int launched = 0;
        int damaged = 0;
        int unsafeRoots = 0;
        int usedSoldierPrefabs = 0;
        int usedFallbackPrefab = 0;
        int missingFlightPrefab = 0;
        foreach (SoldierHealth health in targets)
        {
            if (health == null || !health.enabled || health.isDead) continue;

            if (!damageAllFactions)
            {
                SoldierAnimationController faction = health.GetComponentInParent<SoldierAnimationController>();
                if (faction != null && faction.myFaction == bombFaction)
                {
                    friendlySkipped++;
                    continue;
                }
            }

            Vector3 away = health.transform.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitSphere;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            away.Normalize();

            GameObject selectedPrefab = null;
            bool selectedFromSoldier = false;
            if (preferSoldierDeathPrefab && health.deathEffectPrefab != null)
            {
                selectedPrefab = health.deathEffectPrefab;
                selectedFromSoldier = true;
            }
            else selectedPrefab = flyingSoldierPrefab;

            if (initialBlast && lethalSkyLaunch && selectedPrefab != null)
            {
                if (LaunchLethally(health, away, selectedPrefab))
                {
                    launched++;
                    if (selectedFromSoldier) usedSoldierPrefabs++;
                    else usedFallbackPrefab++;
                }
                else unsafeRoots++;
            }
            else
            {
                if (initialBlast && lethalSkyLaunch && selectedPrefab == null)
                {
                    missingFlightPrefab++;
                    if (!warnedAboutPrefab)
                    {
                        Debug.LogWarning("BombTrapSkyLaunch: 这名士兵的 Death Effect Prefab 与炸弹备用 Flying Soldier Prefab 都未配置；本次改按原伤害逻辑处理。", health);
                        warnedAboutPrefab = true;
                    }
                }
                health.TakeDamage(initialBlast ? attackPower : continuousDamage,
                    initialBlast ? away : Vector3.zero);
                damaged++;
            }
        }

        if (initialBlast && logTargetDiagnostics)
            Debug.Log($"BombTrapSkyLaunch [{name}]: 模式={(usePlanarSoldierDetection ? "XZ平面" : "3D球体")}, " +
                $"找到={found}, 范围内={inRange}, Tag符合={tagMatched}, " +
                $"有效士兵={targets.Count}, 同阵营跳过={friendlySkipped}, 击飞={launched}, " +
                $"采用士兵预制体={usedSoldierPrefabs}, 采用备用预制体={usedFallbackPrefab}, " +
                $"缺少飞行预制体={missingFlightPrefab}, 普通扣血={damaged}, 根节点不安全={unsafeRoots}。" +
                "如果范围内=0，增大 Explosion Radius 或检查炸弹与队列的世界 X/Z 坐标。", this);
    }

    private bool HasTargetTag(SoldierHealth health)
    {
        if (health.CompareTag(targetTag)) return true;
        foreach (Collider collider in health.GetComponentsInChildren<Collider>(true))
            if (collider.CompareTag(targetTag)) return true;
        return false;
    }

    private bool LaunchLethally(SoldierHealth health, Vector3 away, GameObject selectedPrefab)
    {
        // Never destroy an army/formation parent. A safe root contains exactly this one health.
        Transform root = health.独立士兵根节点 != null
            ? health.独立士兵根节点 : health.transform;
        SoldierHealth[] owners = root.GetComponentsInChildren<SoldierHealth>(true);
        if (owners.Length != 1 || owners[0] != health || !visibleBelongsToRoot(root, health))
        {
            Debug.LogWarning("BombTrapSkyLaunch: 独立士兵根节点必须只包含这一名士兵；已跳过不安全的击飞。", health);
            return false;
        }

        Transform visible = health.击退位移节点 != null ? health.击退位移节点 : root;
        Vector3 start = visible.position;
        Quaternion rotation = copySoldierRotation ? visible.rotation : selectedPrefab.transform.rotation;
        // Instantiate under an inactive temporary parent: old death launcher OnEnable/Start
        // cannot run before we disable it on the new visual instance.
        GameObject spawnGuard = new GameObject("SkyLaunch Spawn Guard");
        spawnGuard.SetActive(false);
        GameObject flying = Instantiate(selectedPrefab, start, rotation, spawnGuard.transform);
        if (copySoldierWorldScale) flying.transform.localScale = visible.lossyScale;
        // Turn a regular/death soldier prefab into a visual-only proxy while its
        // temporary parent is inactive; neither AI nor death logic can start.
        foreach (SoldierHealth proxyHealth in flying.GetComponentsInChildren<SoldierHealth>(true))
        {
            proxyHealth.enabled = false;
            Destroy(proxyHealth);
        }
        foreach (SoldierAnimationController proxyAI in
            flying.GetComponentsInChildren<SoldierAnimationController>(true))
            proxyAI.enabled = false;
        foreach (ClashMiniDeathLaunchEffect oldLaunch in
            flying.GetComponentsInChildren<ClashMiniDeathLaunchEffect>(true))
            oldLaunch.enabled = false;
        foreach (Collider collider in flying.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in flying.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        flying.SetActive(true);
        flying.transform.SetParent(null, true);
        Destroy(spawnGuard);

        // Disable immediately, before another bomb or damage tick can hit this soldier.
        root.gameObject.SetActive(false);
        Destroy(root.gameObject);

        float angle = Random.Range(-directionJitterDegrees, directionJitterDegrees);
        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * away;
        float distance = Random.Range(Mathf.Min(minimumHorizontalDistance, maximumHorizontalDistance),
            Mathf.Max(minimumHorizontalDistance, maximumHorizontalDistance));
        Vector3 end = start + direction * distance;
        end.y = (landingHeightMode == LandingHeightMode.FixedWorldY ? fixedWorldY : start.y)
            - landingDrop + Random.Range(-landingHeightJitter, landingHeightJitter);

        activeFlights++;
        StartCoroutine(FlyAndLand(flying, start, end,
            Random.Range(Mathf.Min(minimumFlightSeconds, maximumFlightSeconds),
                Mathf.Max(minimumFlightSeconds, maximumFlightSeconds)),
            Random.Range(Mathf.Min(minimumArcHeight, maximumArcHeight),
                Mathf.Max(minimumArcHeight, maximumArcHeight)),
            Random.Range(Mathf.Min(tumbleTurns.x, tumbleTurns.y),
                Mathf.Max(tumbleTurns.x, tumbleTurns.y)) * 360f,
            Random.Range(Mathf.Min(cameraSpinTurns.x, cameraSpinTurns.y),
                Mathf.Max(cameraSpinTurns.x, cameraSpinTurns.y)) * 360f *
                (Random.value < 0.5f ? -1f : 1f),
            Random.Range(0f, maximumLaunchDelay)));
        return true;
    }

    private static bool visibleBelongsToRoot(Transform root, SoldierHealth health)
    {
        Transform visible = health.击退位移节点 != null ? health.击退位移节点 : root;
        return visible == root || visible.IsChildOf(root);
    }

    private IEnumerator FlyAndLand(GameObject flying, Vector3 start, Vector3 end,
        float duration, float arcHeight, float tumbleDegrees,
        float cameraSpinDegrees, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (flying != null)
        {
            Quaternion originalRotation = flying.transform.rotation;
            Vector3 originalScale = flying.transform.localScale;
            Camera cameraForArc = viewCamera != null ? viewCamera : Camera.main;
            Vector3 screenDirection = cameraForArc != null ? cameraForArc.transform.up : Vector3.zero;
            screenDirection.y = 0f;
            screenDirection = screenDirection.sqrMagnitude > 0.0001f
                ? screenDirection.normalized : Vector3.zero;
            Vector3 cameraSpinAxis = cameraForArc != null
                ? cameraForArc.transform.forward : Vector3.down;
            float elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (elapsed < duration && flying != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // The arc and its landing position are calculated without Raycast/physics.
                float burst = 1f - Mathf.Pow(1f - t, Mathf.Max(1f, horizontalBurstPower));
                Vector3 position = Vector3.LerpUnclamped(start, end, burst);
                position.y = Mathf.LerpUnclamped(start.y, end.y, t);
                float arc = 4f * t * (1f - t);
                position.y += arcHeight * arc;
                position += screenDirection * (screenUpArc * arc);
                flying.transform.position = position;
                Quaternion viewSpin = spinInCameraPlane
                    ? Quaternion.AngleAxis(cameraSpinDegrees * t, cameraSpinAxis)
                    : Quaternion.identity;
                flying.transform.rotation = viewSpin * originalRotation *
                    Quaternion.Euler(tumbleDegrees * t, 0f, 0f);
                flying.transform.localScale = originalScale *
                    (1f + (Mathf.Max(1f, peakFlightScale) - 1f) * arc);
                yield return null;
            }

            if (flying != null)
            {
                flying.transform.position = end;
                SpawnEffect(landingEffectPrefab, end, landingEffectLifetime);
                SpawnEffect(landingBoxPrefab, end, landingBoxLifetime);

                if (shrinkOnLanding && landingShrinkSeconds > 0f)
                {
                    elapsed = 0f;
                    while (elapsed < landingShrinkSeconds && flying != null)
                    {
                        elapsed += Time.deltaTime;
                        flying.transform.localScale = originalScale *
                            (1f - Mathf.Clamp01(elapsed / landingShrinkSeconds));
                        yield return null;
                    }
                }
                if (flying != null) Destroy(flying);
            }
        }
        activeFlights--;
        TryFinish();
    }

    private IEnumerator ContinuousDamageRoutine()
    {
        GameObject effect = null;
        if (continuousEffectPrefab != null)
            effect = Instantiate(continuousEffectPrefab, transform.position, Quaternion.identity);

        float elapsed = 0f;
        float interval = Mathf.Max(0.02f, damageInterval);
        while (elapsed + interval <= continuousDuration + 0.0001f)
        {
            yield return new WaitForSeconds(interval);
            elapsed += interval;
            DealDamageInArea(false);
        }
        if (effect != null) Destroy(effect);
        continuousFinished = true;
        TryFinish();
    }

    private void TryFinish()
    {
        if (continuousFinished && activeFlights == 0) Destroy(gameObject);
    }

    private static void SpawnEffect(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null) return;
        GameObject instance = Instantiate(prefab, position, prefab.transform.rotation);
        if (lifetime > 0f) Destroy(instance, lifetime);
    }

    private void HideBombMeshAndCollider()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (explodeOnTouch && !hasExploded && other.CompareTag(targetTag)) TriggerExplosion();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (usePlanarSoldierDetection)
        {
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 from = transform.position +
                    new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * explosionRadius;
                Vector3 to = transform.position +
                    new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * explosionRadius;
                Gizmos.DrawLine(from, to);
            }
        }
        else Gizmos.DrawWireSphere(transform.position, explosionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maximumHorizontalDistance);
    }
}
