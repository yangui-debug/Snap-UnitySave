using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [System.Serializable]
    public class EffectOption
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    // —— 新增：旋转模式 —— //
    public enum RotationMode
    {
        PrefabDefault,      // 使用预制体默认旋转
        FixedEulerWorld,    // 固定欧拉角（世界坐标）
        FixedEulerLocal,    // 固定欧拉角（相对父物体/本地坐标）
        RandomYawOnly,      // 只随机Y轴（水平朝向）
        RandomEulerXYZ      // XYZ三轴都随机
    }

    [Header("多预制体（随机生成）")]
    public List<EffectOption> effectOptions = new List<EffectOption>();
    [Tooltip("勾选后忽略权重，所有特效等概率随机")]
    public bool forceEqualProbability = false;

    [Header("全局延迟开始时间")]
    [Min(0f)] public float globalStartDelay = 0f;

    [Header("生成区域（以当前物体为中心）")]
    public Vector2 areaSize = new Vector2(10, 10);
    public float spawnHeight = 0f;

    [Header("固定时序（关闭随机时使用）")]
    [Min(0f)] public float startDelay = 1f;
    [Min(0f)] public float interval = 2f;

    [Header("随机时序（开启后：上一个结束后随机等待）")]
    public bool useRandomTiming = false;
    public Vector2 startDelayRange = new Vector2(0.2f, 1.5f);
    public Vector2 intervalRange   = new Vector2(0.3f, 2.0f);

    // ===== 旋转设置（重做） =====
    [Header("旋转设置（按Unity坐标系：度）")]
    public RotationMode rotationMode = RotationMode.RandomYawOnly;

    [Tooltip("用于 FixedEulerWorld / FixedEulerLocal 的固定欧拉角（度）")]
    public Vector3 fixedEuler = Vector3.zero;

    [Tooltip("RandomYawOnly 时使用的随机Y范围（度）")]
    public Vector2 randomYawRange = new Vector2(0f, 360f);

    [Tooltip("RandomEulerXYZ 时的最小欧拉角（度）")]
    public Vector3 randomEulerMin = new Vector3(0f, 0f, 0f);

    [Tooltip("RandomEulerXYZ 时的最大欧拉角（度）")]
    public Vector3 randomEulerMax = new Vector3(0f, 360f, 0f);

    [Tooltip("勾选后会把粒子系统的 Start Rotation 强制设为与欧拉角一致（单位：度 -> 弧度），避免Billboard等设置忽略旋转")]
    public bool forceParticleStartRotation = false;

    [Header("总时长限制")]
    public bool enableStopAfterTime = false;
    [Min(0f)] public float stopAfterTime = 10f;

    [Header("特效播放速度")]
    [Tooltip("影响粒子系统和动画速度")]
    [Min(0f)] public float effectSpeed = 1f;

    [Header("结束判定兜底")]
    [Tooltip("循环特效的最大等待时间，0 表示无限等待直到结束")]
    [Min(0f)] public float maxEffectDuration = 0f;

    private bool isSpawning = true;

    private void OnValidate()
    {
        if (startDelayRange.x > startDelayRange.y) (startDelayRange.x, startDelayRange.y) = (startDelayRange.y, startDelayRange.x);
        if (intervalRange.x   > intervalRange.y)   (intervalRange.x, intervalRange.y)     = (intervalRange.y, intervalRange.x);

        // 保证随机角度上下限合法
        if (randomYawRange.x > randomYawRange.y) (randomYawRange.x, randomYawRange.y) = (randomYawRange.y, randomYawRange.x);

        // clamp 三轴随机范围
        randomEulerMin = new Vector3(
            Mathf.Min(randomEulerMin.x, randomEulerMax.x),
            Mathf.Min(randomEulerMin.y, randomEulerMax.y),
            Mathf.Min(randomEulerMin.z, randomEulerMax.z)
        );
    }

    private void Start()
    {
        StartCoroutine(SpawnEffectRoutine());
        if (enableStopAfterTime) Invoke(nameof(StopSpawning), stopAfterTime);
    }

    private IEnumerator SpawnEffectRoutine()
    {
        if (globalStartDelay > 0f) yield return new WaitForSeconds(globalStartDelay);

        float firstDelay = useRandomTiming ? Random.Range(startDelayRange.x, startDelayRange.y) : startDelay;
        if (firstDelay > 0f) yield return new WaitForSeconds(firstDelay);

        while (isSpawning)
        {
            GameObject prefab = SelectPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("没有可用的特效预制体，请在 effectOptions 中添加。");
                yield return null;
                continue;
            }

            GameObject inst = SpawnOne(prefab);

            if (inst != null) yield return WaitUntilEffectFinished(inst);

            float extra = useRandomTiming ? Random.Range(intervalRange.x, intervalRange.y) : interval;
            if (extra > 0f) yield return new WaitForSeconds(extra);
            else yield return null;
        }
    }

    private GameObject SelectPrefab()
    {
        var candidates = new List<EffectOption>();
        foreach (var opt in effectOptions) if (opt?.prefab != null) candidates.Add(opt);
        if (candidates.Count == 0) return null;

        if (forceEqualProbability)
        {
            return candidates[Random.Range(0, candidates.Count)].prefab;
        }
        else
        {
            float total = 0f;
            foreach (var c in candidates) if (c.weight > 0f) total += c.weight;
            if (total <= 0f) return candidates[Random.Range(0, candidates.Count)].prefab;

            float r = Random.value * total, acc = 0f;
            foreach (var c in candidates)
            {
                if (c.weight <= 0f) continue;
                acc += c.weight;
                if (r <= acc) return c.prefab;
            }
            return candidates[candidates.Count - 1].prefab;
        }
    }

    private GameObject SpawnOne(GameObject prefab)
    {
        // —— 在 areaSize 内随机（以本物体为中心）——
        Vector3 local = new Vector3(
            Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
            spawnHeight,
            Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f)
        );
        Vector3 pos = transform.position + local;

        // —— 计算欧拉角（度）并应用 —— 
        Vector3 eulerDeg = ComputeEulerDegrees();
        Quaternion rot = Quaternion.Euler(eulerDeg);

        // 注意：FixedEulerLocal 需要先实例化再设 localRotation
        GameObject inst = Instantiate(prefab, pos, Quaternion.identity);

        if (rotationMode == RotationMode.FixedEulerLocal)
            inst.transform.localRotation = rot;
        else
            inst.transform.rotation = rot;

        // 同步特效速度
        ApplyEffectSpeed(inst, effectSpeed);

        // （可选）把粒子系统的 Start Rotation 也强制成对应角度（弧度）
        if (forceParticleStartRotation) ApplyParticleStartRotation(inst, eulerDeg);

        return inst;
    }

    private Vector3 ComputeEulerDegrees()
    {
        switch (rotationMode)
        {
            case RotationMode.PrefabDefault:
                // 使用预制体自身旋转：返回零即可，实例化时不会改它（上面我们用 identity 实例化了，再按模式赋）
                return Vector3.zero;

            case RotationMode.FixedEulerWorld:
            case RotationMode.FixedEulerLocal:
                return fixedEuler;

            case RotationMode.RandomYawOnly:
                return new Vector3(0f, Random.Range(randomYawRange.x, randomYawRange.y), 0f);

            case RotationMode.RandomEulerXYZ:
                return new Vector3(
                    Random.Range(randomEulerMin.x, randomEulerMax.x),
                    Random.Range(randomEulerMin.y, randomEulerMax.y),
                    Random.Range(randomEulerMin.z, randomEulerMax.z)
                );

            default:
                return Vector3.zero;
        }
    }

    private void ApplyEffectSpeed(GameObject go, float speed)
    {
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.simulationSpeed = speed;
        }
        foreach (var anim in go.GetComponentsInChildren<Animator>(true))
            anim.speed = speed;
    }

    // 把粒子 Start Rotation 设为与欧拉角一致（单位：度 -> 弧度）
    private void ApplyParticleStartRotation(GameObject go, Vector3 eulerDeg)
    {
        float rx = eulerDeg.x * Mathf.Deg2Rad;
        float ry = eulerDeg.y * Mathf.Deg2Rad;
        float rz = eulerDeg.z * Mathf.Deg2Rad;

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.startRotation3D = true; // 启用三轴旋转
            main.startRotationX = new ParticleSystem.MinMaxCurve(rx);
            main.startRotationY = new ParticleSystem.MinMaxCurve(ry);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(rz);
        }
    }

    private IEnumerator WaitUntilEffectFinished(GameObject inst)
    {
        float startT = Time.time;
        var pss = inst.GetComponentsInChildren<ParticleSystem>(true);

        if (pss.Length > 0)
        {
            foreach (var ps in pss) ps.Play(true);

            while (inst != null)
            {
                bool anyAlive = false;
                foreach (var ps in pss) { if (ps != null && ps.IsAlive(true)) { anyAlive = true; break; } }
                if (!anyAlive) break;

                if (maxEffectDuration > 0f && Time.time - startT >= maxEffectDuration) break;
                yield return null;
            }
        }
        else
        {
            float wait = Mathf.Max(0f, maxEffectDuration);
            if (wait > 0f) yield return new WaitForSeconds(wait);
            else yield return null;
        }
    }

    private void StopSpawning()
    {
        isSpawning = false;
        Debug.Log("特效生成已停止。");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            transform.position + new Vector3(0f, spawnHeight, 0f),
            new Vector3(areaSize.x, 0f, areaSize.y)
        );
    }
}
