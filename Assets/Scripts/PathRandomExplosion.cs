using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathRandomExplosion : MonoBehaviour
{
    [Header("核心配置")]
    public GameObject explosionPrefab;

    [Header("路径配置")]
    public List<GameObject> pathCubes = new List<GameObject>();

    [Header("波次规模随机")]
    public int minCountPerWave = 3;
    public int maxCountPerWave = 8;

    [Header("错落感设置 (重要)")]
    [Tooltip("同一波次内，每个爆炸点之间的最大随机延迟（秒）。值越大，错落感越明显")]
    public float maxIndividualDelay = 0.5f;

    [Header("位置偏移随机")]
    public float randomRadius = 1.2f;

    [Header("波次间隔随机 (Loop Settings)")]
    public bool autoLoop = true;
    public float minWaveInterval = 0.5f;
    public float maxWaveInterval = 2.5f;

    [Header("销毁时间随机 (Life Time)")]
    public float minLifeTime = 0.5f;
    public float maxLifeTime = 1.5f;

    private float timer = 0f;
    private float currentWaveInterval = 0f;

    private void Start()
    {
        currentWaveInterval = Random.Range(minWaveInterval, maxWaveInterval);
    }

    private void Update()
    {
        if (Application.isPlaying && autoLoop)
        {
            timer += Time.deltaTime;
            if (timer >= currentWaveInterval)
            {
                // 改为调用协程来实现错落生成
                StartCoroutine(ExecuteExplosionRoutine());
                timer = 0f;
                currentWaveInterval = Random.Range(minWaveInterval, maxWaveInterval);
            }
        }
    }

    [ContextMenu("手动执行一次随机爆炸")]
    public void ManualTrigger()
    {
        if (Application.isPlaying)
            StartCoroutine(ExecuteExplosionRoutine());
        else
            Debug.LogWarning("手动测试错落效果请在 Play 模式下执行");
    }

    // 核心改进：使用协程让爆炸在时间上散开
    private IEnumerator ExecuteExplosionRoutine()
    {
        if (explosionPrefab == null || pathCubes == null || pathCubes.Count == 0) yield break;

        int count = Random.Range(minCountPerWave, maxCountPerWave + 1);

        for (int i = 0; i < count; i++)
        {
            // 给每一个爆炸点计算一个独立的微小延迟
            float individualDelay = Random.Range(0f, maxIndividualDelay);

            // 启动一个独立的随机生成逻辑，不阻塞循环
            StartCoroutine(SpawnWithDelay(individualDelay));
        }
        yield return null;
    }

    private IEnumerator SpawnWithDelay(float delay)
    {
        // 等待随机的毫秒数
        if (delay > 0) yield return new WaitForSeconds(delay);

        int randomIndex = Random.Range(0, pathCubes.Count);
        GameObject targetCube = pathCubes[randomIndex];
        if (targetCube == null) yield break;

        // 生成位置与旋转
        Vector3 spawnPos = targetCube.transform.position + (Random.insideUnitSphere * randomRadius);
        Quaternion randomRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

        GameObject effectInstance = Instantiate(explosionPrefab, spawnPos, randomRotation);

        // 随机销毁时间
        float randomLifeTime = Random.Range(minLifeTime, maxLifeTime);
        Destroy(effectInstance, randomLifeTime);
    }
}