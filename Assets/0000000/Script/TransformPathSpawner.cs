using UnityEngine;

public class TransformPathSpawner : MonoBehaviour
{
    [Header("【1】跟随的目标 Cube")]
    [Tooltip("拖入你在 Timeline 或用 DOTween 控制移动的那个 Cube")]
    public Transform targetCube;

    [Header("【2】释放的路径特效")]
    [Tooltip("你想沿路丢下的特效预制体（比如火焰、地刺、脚印等粒子）")]
    public GameObject pathEffectPrefab;

    [Header("【3】释放密度控制")]
    [Tooltip("Cube 每移动多少米，就释放一个特效。数值越小，特效越密集。")]
    public float spawnDistanceInterval = 0.5f;

    [Tooltip("特效生成的高度修正（比如让特效刚好贴在地面上）")]
    public float effectHeightOffset = 0f;

    [Header("【4】开关控制")]
    [Tooltip("是否开启路径释放？可以用 Timeline 的激活轨道或者别的脚本动态开关它。")]
    public bool isSpawning = true;

    // 内部记录变量
    private Vector3 m_LastSpawnPosition;
    private bool m_HasInitialized = false;

    void Start()
    {
        if (targetCube != null)
        {
            m_LastSpawnPosition = targetCube.position;
            m_HasInitialized = true;

            // 开局在起点先默认放一个
            if (isSpawning) SpawnEffect(m_LastSpawnPosition);
        }
    }

    void LateUpdate()
    {
        if (targetCube == null || pathEffectPrefab == null || !isSpawning) return;

        // 如果中途才挂载或赋值，做个安全初始化
        if (!m_HasInitialized)
        {
            m_LastSpawnPosition = targetCube.position;
            m_HasInitialized = true;
            return;
        }

        // 1. 核心算法：计算 Cube 当前位置和上一次释放特效的位置之间的物理距离
        float distanceMoved = Vector3.Distance(targetCube.position, m_LastSpawnPosition);

        // 2. 如果移动距离超过了你设定的阈值（比如 0.5 米），立刻“盖章”释放特效！
        if (distanceMoved >= spawnDistanceInterval)
        {
            // 算出精准的释放点（可以加上高度偏移）
            Vector3 spawnPos = targetCube.position;
            spawnPos.y += effectHeightOffset;

            SpawnEffect(spawnPos);

            // 更新上一次的释放坐标，等待下一次跨步
            m_LastSpawnPosition = targetCube.position;
        }
    }

    /// <summary>
    /// 释放单个特效并自动计算寿命销毁
    /// </summary>
    private void SpawnEffect(Vector3 position)
    {
        // 实例化特效，并且让特效的朝向直接同步当前 Cube 的朝向，防止粒子歪斜
        GameObject effectGo = Instantiate(pathEffectPrefab, position, targetCube.rotation);

        // 【安全核心】：自动根据粒子系统中最长寿命的粒子，计算销毁时间，防止内存爆棚
        ParticleSystem[] particles = effectGo.GetComponentsInChildren<ParticleSystem>();
        float maxDuration = 0f;

        foreach (var ps in particles)
        {
            float totalTime = ps.main.duration + ps.main.startLifetime.constantMax;
            maxDuration = Mathf.Max(maxDuration, totalTime);
        }

        // 寿终正寝，自动销毁该段路径上的特效
        Destroy(effectGo, maxDuration > 0 ? maxDuration : 5f);
    }
}