using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Expanding shockwave for suspended / top-down stages. Replaces BombTrapSkyLaunch
/// on a bomb; keeps the same public TriggerExplosion() Signal Receiver entry point.
/// Each soldier's own SoldierHealth.deathEffectPrefab is used as a visual proxy.
/// No ground Collider, Rigidbody impulse or SoldierHealth modification is required.
/// </summary>
public class BombTrapShockwave : MonoBehaviour
{
    public enum WaveShape { FullCircle, ForwardSemicircle }
    public enum LandingHeightMode { SoldierStartY, FixedWorldY }

    [Header("触发 / 目标")]
    [Tooltip("Timeline 的 Signal Receiver 选择 TriggerExplosion()。勾选时，也能由目标碰到炸弹 Trigger 引爆。")]
    public bool explodeOnTouch = false;
    [Tooltip("一般填 Soldier。士兵根物体或其子碰撞体带此 Tag 即可；查找目标不依赖 Collider。")]
    public string targetTag = "Soldier";
    [Tooltip("勾选炸所有阵营；取消则跳过与 Bomb Faction 同阵营的士兵。")]
    public bool damageAllFactions = true;
    [Tooltip("仅在关闭无差别攻击时生效。")]
    public FactionType bombFaction;

    [Header("冲击波：圆边扫到目标才击飞")]
    [Tooltip("360° 整圆，或按炸弹 Transform.forward 朝向的前方半圆。使用半圆时绕世界 Y 旋转炸弹调整方向。")]
    public WaveShape waveShape = WaveShape.FullCircle;
    [Tooltip("冲击波最大半径（世界单位）。建议先试 4～6；绿色 Scene 线框显示最终范围。")]
    [Min(0.01f)] public float maximumRadius = 5f;
    [Tooltip("从中心扩大到最大半径所需时间。建议 0.22～0.45 秒，数值越小越猛烈。")]
    [Min(0.02f)] public float expansionSeconds = 0.32f;
    [Tooltip("扩散减速程度。1=匀速；2=起步快、到外圈变慢，内圈士兵受力更强。建议 1.5～2.5。")]
    [Min(1f)] public float expansionBurstPower = 2f;
    [Tooltip("士兵世界 Y 与炸弹 Y 的最大差值。建议 1～3；避免桥上和桥下 XZ 重叠时一起被击飞。")]
    [Min(0f)] public float maximumVerticalDifference = 2f;
    [Tooltip("被推出的方向允许的随机偏角（±度）。建议 5～15；填 0 则严格从圆心向外。")]
    [Range(0f, 90f)] public float directionJitterDegrees = 10f;
    [Tooltip("外圈最低受力比例。建议 0.55～0.75；内圈随圆边扩散速度获得更强推力。")]
    [Range(0.05f, 1f)] public float minimumImpulseFactor = 0.65f;

    [Header("爆炸与扩散圆外观")]
    [Tooltip("爆炸中心特效；可留空。它与程序生成的扩散圆分别播放。")]
    public GameObject explosionEffect;
    [Tooltip("中心特效自动销毁时间（秒）；0 表示不自动销毁。")]
    [Min(0f)] public float explosionEffectLifetime = 2f;
    [Tooltip("勾选会创建一个跟随真实命中半径扩张的圆线。正式美术也可以用下方材质与爆炸特效替换。")]
    public bool drawShockwaveRing = true;
    [Tooltip("可选：URP 下建议拖入透明 Unlit/Particle 材质。留空使用 Unity 的默认 Line Renderer 材质。")]
    public Material ringMaterial;
    [Tooltip("圆线宽度，世界单位。建议 0.12～0.3；士兵多且镜头很远时适当加宽。")]
    [Min(0.01f)] public float ringWidth = 0.18f;
    [Tooltip("扩散圆颜色，末尾随半径增大逐渐淡出。材质需要支持顶点颜色才能正确显示此颜色。")]
    public Color ringColor = new Color(1f, 0.75f, 0.25f, 0.9f);
    [Tooltip("扩散圆相对炸弹的高度偏移。被场景遮住时可上调一点，例如 0.1～0.3。")]
    public float ringYPositionOffset = 0.1f;

    [Header("每个兵种自己的死亡预制体")]
    [Tooltip("建议勾选：优先读取被击中士兵的 SoldierHealth → Death Effect Prefab。不同兵种会自动使用不同外观。")]
    public bool preferSoldierDeathPrefab = true;
    [Tooltip("士兵没有配置 Death Effect Prefab 时的备用飞行预制体。实例里的生命、AI、旧击飞和碰撞会被关闭。")]
    public GameObject fallbackFlyingPrefab;
    [Tooltip("勾选：复制原士兵朝向。关闭则使用所选飞行预制体自身朝向。")]
    public bool copySoldierRotation = true;
    [Tooltip("默认关闭并使用死亡预制体设好的尺寸；只有预制体根缩放为 1 且与原士兵模型一致时才勾选。")]
    public bool copySoldierWorldScale = false;

    [Header("起飞、自由下落（不需要地面）")]
    [Tooltip("受力最强时在 XZ 平面的水平起飞速度。先试 6～10 世界单位/秒；外圈按最低受力比例降低。")]
    [Min(0f)] public float maximumHorizontalSpeed = 8f;
    [Tooltip("受力最强时的世界 Y 起飞速度。先试 7～10；90° 俯视时需配合画面放大和暗影才能看清。")]
    [Min(0.1f)] public float maximumUpwardSpeed = 8f;
    [Tooltip("落下加速度。建议 15～24；越大越快落下。飞行时间由起飞速度和重力自然决定。")]
    [Min(0.1f)] public float gravity = 18f;
    [Tooltip("水平减速。0=保持惯性；建议 1～2.5，让初段冲出、后段放缓。")]
    [Min(0f)] public float horizontalDrag = 1.3f;
    [Tooltip("起飞后在镜头画面向上方向的附加位移（世界 XZ 单位），用于 90° 俯视的视觉高度。建议 0.5～1.5；不需要可设 0。")]
    [Min(0f)] public float screenLiftAtPeak = 1f;
    [Tooltip("可指定实际录制相机；留空寻找 Main Camera。用于计算俯视时的画面向上方向。")]
    public Camera viewCamera;
    [Tooltip("飞到最高点时的模型尺寸倍率。建议 1.5～2；用大小变化让俯视镜头读出高度。")]
    [Min(1f)] public float peakScale = 1.7f;
    [Tooltip("飞行期间轻微摇晃的最大角度，不会连续转圈。建议 0～20°；填 0 完全不转。")]
    [Range(0f, 90f)] public float wobbleDegrees = 15f;

    [Header("受冲击瞬间的压缩与伸展")]
    [Tooltip("先从脚底受压的时长。建议 0.05～0.1 秒；过长像被踩住。")]
    [Min(0f)] public float squashSeconds = 0.07f;
    [Tooltip("受压时模型 Y 高度倍率。建议 0.55～0.75。")]
    [Range(0.1f, 1f)] public float squashHeight = 0.62f;
    [Tooltip("受压时 XZ 轮廓加宽倍率，让 90° 俯视也能看见压缩。建议 1.1～1.25。")]
    [Min(1f)] public float squashWidth = 1.18f;
    [Tooltip("压缩后向上伸直的时长。建议 0.07～0.14 秒。")]
    [Min(0f)] public float stretchSeconds = 0.1f;
    [Tooltip("伸直时模型 Y 高度倍率。建议 1.2～1.5。")]
    [Min(1f)] public float stretchHeight = 1.35f;
    [Tooltip("伸直时 XZ 轮廓收窄倍率。建议 0.85～0.95。")]
    [Range(0.5f, 1f)] public float stretchWidth = 0.9f;

    [Header("虚拟落地 / 成盒")]
    [Tooltip("默认落回每位士兵出发时的世界 Y；也可统一设置固定落点 Y。不会检测 Collider。")]
    public LandingHeightMode landingHeightMode = LandingHeightMode.SoldierStartY;
    [Tooltip("仅选择 FixedWorldY 时生效。填落点高度的世界 Y 坐标，应低于或等于士兵起飞高度。")]
    public float fixedWorldY = 0f;
    [Tooltip("落点额外下降距离；想让人坠入悬崖可试 2～5。盒子和落地特效也会出现在下降后的高度。")]
    [Min(0f)] public float landingDrop = 0f;
    [Tooltip("可选：人物飞行时的投影/暗影预制体，会跟随 XZ 位移而留在虚拟落点高度。没有可见地面时可留空。")]
    public GameObject flightShadowPrefab;
    [Tooltip("暗影相对虚拟落点的 Y 偏移；防止被视觉地面遮住。")]
    public float shadowYPositionOffset = 0.05f;
    [Tooltip("落到虚拟高度时生成的特效；可留空。")]
    public GameObject landingEffectPrefab;
    [Tooltip("落地特效自动销毁时间（秒）；0 保留。")]
    [Min(0f)] public float landingEffectLifetime = 2f;
    [Tooltip("落点生成的盒子预制体；可留空。没有视觉地面时，盒子可能看起来悬空。")]
    public GameObject landingBoxPrefab;
    [Tooltip("盒子自动销毁时间（秒）；0 保留。")]
    [Min(0f)] public float landingBoxLifetime = 0f;
    [Tooltip("落地时替身缩小消失的时长。建议 0.06～0.12；0 直接消失。")]
    [Min(0f)] public float landingShrinkSeconds = 0.08f;

    [Header("排查")]
    [Tooltip("每次爆炸打印候选、击飞、预制体来源与根节点安全检查计数。调好后可关闭。")]
    public bool logDiagnostics = true;

    private bool hasExploded;
    private bool waveFinished;
    private int activeFlights;
    private bool warnedMissingPrefab;
    private bool warnedLandingAboveStart;

    public void TriggerExplosion()
    {
        if (hasExploded) return;
        hasExploded = true;
        SpawnTimed(explosionEffect, transform.position, explosionEffectLifetime);
        HideBombRenderersAndColliders();
        StartCoroutine(ExpandAndHit());
    }

    private IEnumerator ExpandAndHit()
    {
        List<SoldierHealth> candidates = new List<SoldierHealth>();
        SoldierHealth[] all = FindObjectsByType<SoldierHealth>(FindObjectsSortMode.None);
        foreach (SoldierHealth health in all)
        {
            if (health != null && health.enabled && !health.isDead && HasTargetTag(health))
                candidates.Add(health);
        }

        int launched = 0, fromSoldier = 0, fromFallback = 0, unsafeRoots = 0, missing = 0;
        GameObject ringObject = drawShockwaveRing ? CreateRing() : null;
        LineRenderer ring = ringObject != null ? ringObject.GetComponent<LineRenderer>() : null;
        float elapsed = 0f;
        float duration = Mathf.Max(0.02f, expansionSeconds);
        Vector3 center = transform.position;

        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            float t = Mathf.Clamp01(elapsed / duration);
            float power = Mathf.Max(1f, expansionBurstPower);
            float radius = maximumRadius * (1f - Mathf.Pow(1f - t, power));
            UpdateRing(ring, center, radius, t);

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                SoldierHealth health = candidates[i];
                if (health == null || !health.enabled || health.isDead)
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                Vector3 position = health.击退位移节点 != null
                    ? health.击退位移节点.position : health.transform.position;
                Vector3 delta = position - center;
                if (Mathf.Abs(delta.y) > maximumVerticalDifference) continue;
                Vector3 flat = new Vector3(delta.x, 0f, delta.z);
                if (flat.sqrMagnitude > radius * radius || !WithinShape(flat)) continue;
                candidates.RemoveAt(i); // Each soldier is touched only once by this wave.

                if (!damageAllFactions)
                {
                    SoldierAnimationController faction =
                        health.GetComponentInParent<SoldierAnimationController>();
                    if (faction != null && faction.myFaction == bombFaction) continue;
                }

                GameObject prefab = preferSoldierDeathPrefab ? health.deathEffectPrefab : null;
                bool ownPrefab = prefab != null;
                if (prefab == null) prefab = fallbackFlyingPrefab;
                if (prefab == null)
                {
                    missing++;
                    if (!warnedMissingPrefab)
                    {
                        Debug.LogWarning("BombTrapShockwave: 士兵死亡预制体和炸弹备用预制体都未配置；这名士兵不会被删除。", health);
                        warnedMissingPrefab = true;
                    }
                    continue;
                }

                // The outward ring slows as it grows; close targets take a stronger hit.
                float speedRatio = power <= 1f ? 1f : Mathf.Pow(1f - t, power - 1f);
                float impulse = Mathf.Lerp(minimumImpulseFactor, 1f, speedRatio);
                Vector3 direction = flat.sqrMagnitude > 0.0001f
                    ? flat.normalized : RandomFlatDirection();
                direction = Quaternion.AngleAxis(
                    Random.Range(-directionJitterDegrees, directionJitterDegrees),
                    Vector3.up) * direction;

                if (LaunchOne(health, prefab, direction, impulse))
                {
                    launched++;
                    if (ownPrefab) fromSoldier++;
                    else fromFallback++;
                }
                else unsafeRoots++;
            }
            yield return null;
        }

        if (ringObject != null) Destroy(ringObject);
        waveFinished = true;
        if (logDiagnostics)
            Debug.Log($"BombTrapShockwave [{name}]: 候选={all.Length}, 击飞={launched}, " +
                $"士兵自身预制体={fromSoldier}, 备用预制体={fromFallback}, " +
                $"根节点不安全={unsafeRoots}, 缺少预制体={missing}。", this);
        TryFinish();
    }

    private bool LaunchOne(SoldierHealth health, GameObject prefab,
        Vector3 direction, float impulse)
    {
        Transform root = health.独立士兵根节点 != null
            ? health.独立士兵根节点 : health.transform;
        Transform visible = health.击退位移节点 != null
            ? health.击退位移节点 : root;
        SoldierHealth[] owners = root.GetComponentsInChildren<SoldierHealth>(true);
        if (owners.Length != 1 || owners[0] != health ||
            (visible != root && !visible.IsChildOf(root)))
        {
            Debug.LogWarning("BombTrapShockwave: 独立士兵根节点必须只包含这一名士兵；已跳过击飞。", health);
            return false;
        }

        Vector3 start = visible.position;
        Quaternion rotation = copySoldierRotation ? visible.rotation : prefab.transform.rotation;
        GameObject spawnGuard = new GameObject("Shockwave Spawn Guard");
        spawnGuard.SetActive(false);
        GameObject flying = Instantiate(prefab, start, rotation, spawnGuard.transform);
        if (copySoldierWorldScale) flying.transform.localScale = visible.lossyScale;
        foreach (SoldierHealth proxyHealth in flying.GetComponentsInChildren<SoldierHealth>(true))
        {
            proxyHealth.enabled = false;
            Destroy(proxyHealth);
        }
        foreach (SoldierAnimationController ai in
            flying.GetComponentsInChildren<SoldierAnimationController>(true)) ai.enabled = false;
        foreach (ClashMiniDeathLaunchEffect oldFlight in
            flying.GetComponentsInChildren<ClashMiniDeathLaunchEffect>(true)) oldFlight.enabled = false;
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

        root.gameObject.SetActive(false);
        Destroy(root.gameObject);

        float floorY = (landingHeightMode == LandingHeightMode.FixedWorldY
            ? fixedWorldY : start.y) - landingDrop;
        if (floorY > start.y)
        {
            floorY = start.y;
            if (!warnedLandingAboveStart)
            {
                Debug.LogWarning("BombTrapShockwave: 固定落点 Y 高于士兵起点，已暂时限制为起点高度，避免落地时向上瞬移。", this);
                warnedLandingAboveStart = true;
            }
        }
        GameObject shadow = flightShadowPrefab != null
            ? Instantiate(flightShadowPrefab,
                new Vector3(start.x, floorY + shadowYPositionOffset, start.z),
                flightShadowPrefab.transform.rotation)
            : null;
        activeFlights++;
        StartCoroutine(FlyAndLand(flying, shadow, start, floorY, direction,
            maximumHorizontalSpeed * impulse, maximumUpwardSpeed * impulse,
            Random.Range(-wobbleDegrees, wobbleDegrees)));
        return true;
    }

    private IEnumerator FlyAndLand(GameObject flying, GameObject shadow,
        Vector3 start, float floorY, Vector3 direction,
        float horizontalSpeed, float upwardSpeed, float wobble)
    {
        Camera cameraForFlight = viewCamera != null ? viewCamera : Camera.main;
        Vector3 screenUp = cameraForFlight != null
            ? cameraForFlight.transform.up : Vector3.zero;
        screenUp.y = 0f;
        screenUp = screenUp.sqrMagnitude > 0.0001f ? screenUp.normalized : Vector3.zero;
        Vector3 cameraAxis = cameraForFlight != null
            ? cameraForFlight.transform.forward : Vector3.down;
        float acceleration = Mathf.Max(0.1f, gravity);
        float time = 0f;
        float airTime = (upwardSpeed + Mathf.Sqrt(upwardSpeed * upwardSpeed +
            2f * acceleration * Mathf.Max(0f, start.y - floorY))) / acceleration;
        airTime = Mathf.Max(0.08f, airTime);
        float peakHeight = upwardSpeed * upwardSpeed / (2f * acceleration);
        Quaternion baseRotation = flying.transform.rotation;
        Vector3 baseScale = flying.transform.localScale;

        while (time < airTime && flying != null)
        {
            time = Mathf.Min(airTime, time + Time.deltaTime);
            float drag = Mathf.Max(0f, horizontalDrag);
            float horizontalDistance = drag < 0.001f
                ? horizontalSpeed * time
                : horizontalSpeed * (1f - Mathf.Exp(-drag * time)) / drag;
            float worldY = start.y + upwardSpeed * time - 0.5f * acceleration * time * time;
            float lift01 = peakHeight > 0.001f
                ? Mathf.Clamp01((worldY - start.y) / peakHeight) : 0f;
            Vector3 flatPosition = start + direction * horizontalDistance;
            flying.transform.position = new Vector3(flatPosition.x, worldY, flatPosition.z) +
                screenUp * (screenLiftAtPeak * lift01);
            if (shadow != null)
                shadow.transform.position = new Vector3(flatPosition.x,
                    floorY + shadowYPositionOffset, flatPosition.z);

            float width = 1f, height = 1f;
            if (squashSeconds > 0f && time < squashSeconds)
            {
                float s = Mathf.Sin(Mathf.PI * 0.5f * time / squashSeconds);
                width = Mathf.Lerp(1f, squashWidth, s);
                height = Mathf.Lerp(1f, squashHeight, s);
            }
            else if (stretchSeconds > 0f &&
                time < squashSeconds + stretchSeconds)
            {
                float s = (time - squashSeconds) / stretchSeconds;
                width = Mathf.Lerp(squashWidth, stretchWidth, s);
                height = Mathf.Lerp(squashHeight, stretchHeight, s);
            }
            else if (stretchSeconds > 0f &&
                time < squashSeconds + 2f * stretchSeconds)
            {
                float s = (time - squashSeconds - stretchSeconds) / stretchSeconds;
                width = Mathf.Lerp(stretchWidth, 1f, s);
                height = Mathf.Lerp(stretchHeight, 1f, s);
            }
            float apparentSize = 1f + (Mathf.Max(1f, peakScale) - 1f) * lift01;
            flying.transform.localScale = Vector3.Scale(baseScale,
                new Vector3(width, height, width)) * apparentSize;
            float phase = time / airTime;
            flying.transform.rotation = Quaternion.AngleAxis(
                wobble * Mathf.Sin(Mathf.PI * phase), cameraAxis) * baseRotation;
            yield return null;
        }

        if (flying != null)
        {
            float drag = Mathf.Max(0f, horizontalDrag);
            float distance = drag < 0.001f ? horizontalSpeed * airTime
                : horizontalSpeed * (1f - Mathf.Exp(-drag * airTime)) / drag;
            Vector3 landing = start + direction * distance;
            landing.y = floorY;
            flying.transform.position = landing;
            flying.transform.rotation = baseRotation;
            flying.transform.localScale = baseScale;
            SpawnTimed(landingEffectPrefab, landing, landingEffectLifetime);
            SpawnTimed(landingBoxPrefab, landing, landingBoxLifetime);
            if (landingShrinkSeconds > 0f)
            {
                float shrinkTime = 0f;
                while (shrinkTime < landingShrinkSeconds && flying != null)
                {
                    shrinkTime += Time.deltaTime;
                    flying.transform.localScale = baseScale *
                        (1f - Mathf.Clamp01(shrinkTime / landingShrinkSeconds));
                    yield return null;
                }
            }
            if (flying != null) Destroy(flying);
        }
        if (shadow != null) Destroy(shadow);
        activeFlights--;
        TryFinish();
    }

    private GameObject CreateRing()
    {
        GameObject instance = new GameObject("Shockwave Expanding Ring");
        LineRenderer line = instance.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 49;
        line.loop = false;
        line.startWidth = ringWidth;
        line.endWidth = ringWidth;
        line.numCornerVertices = 2;
        if (ringMaterial != null) line.sharedMaterial = ringMaterial;
        return instance;
    }

    private void UpdateRing(LineRenderer line, Vector3 center, float radius, float t)
    {
        if (line == null) return;
        Color color = ringColor;
        color.a *= (1f - t);
        line.startColor = color;
        line.endColor = color;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        float arc = waveShape == WaveShape.FullCircle ? 360f : 180f;
        float startAngle = waveShape == WaveShape.FullCircle ? 0f : -90f;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = startAngle + arc * i / (line.positionCount - 1);
            Vector3 outward = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            line.SetPosition(i, center + outward * radius +
                Vector3.up * ringYPositionOffset);
        }
    }

    private bool WithinShape(Vector3 flat)
    {
        if (waveShape == WaveShape.FullCircle || flat.sqrMagnitude < 0.0001f)
            return true;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        return Vector3.Dot(flat, forward) >= 0f;
    }

    private bool HasTargetTag(SoldierHealth health)
    {
        if (health.CompareTag(targetTag)) return true;
        foreach (Collider collider in health.GetComponentsInChildren<Collider>(true))
            if (collider.CompareTag(targetTag)) return true;
        return false;
    }

    private static Vector3 RandomFlatDirection()
    {
        Vector2 point = Random.insideUnitCircle;
        return point.sqrMagnitude > 0.0001f
            ? new Vector3(point.x, 0f, point.y).normalized : Vector3.forward;
    }

    private static void SpawnTimed(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null) return;
        GameObject instance = Instantiate(prefab, position, prefab.transform.rotation);
        if (lifetime > 0f) Destroy(instance, lifetime);
    }

    private void HideBombRenderersAndColliders()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;
    }

    private void TryFinish()
    {
        if (waveFinished && activeFlights == 0) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (explodeOnTouch && !hasExploded && other.CompareTag(targetTag))
            TriggerExplosion();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        float arc = waveShape == WaveShape.FullCircle ? 360f : 180f;
        float start = waveShape == WaveShape.FullCircle ? 0f : -90f;
        for (int i = 0; i < 48; i++)
        {
            Vector3 a = Quaternion.AngleAxis(start + arc * i / 48f,
                Vector3.up) * forward;
            Vector3 b = Quaternion.AngleAxis(start + arc * (i + 1) / 48f,
                Vector3.up) * forward;
            Gizmos.DrawLine(transform.position + a * maximumRadius,
                transform.position + b * maximumRadius);
        }
        if (waveShape == WaveShape.ForwardSemicircle)
        {
            Gizmos.DrawLine(transform.position, transform.position + forward * maximumRadius);
        }
    }
}
