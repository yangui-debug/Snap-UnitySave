using UnityEngine;
using System.Collections;

public class TimedEffectSpawner : MonoBehaviour
{
    [Header("特效配置")]
    public GameObject effectPrefab; // 特效预制体
    public Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f); // 生成范围的大小
    public int spawnCount = 10; // 每次生成的特效数量
    public float spawnInterval = 1f; // 生成间隔时间（秒）

    [Header("时间控制")]
    public float startDelay = 0f; // 延迟开始时间（秒）
    public float spawnDuration = 10f; // 持续生成时间（秒）
    public float stopDelay = 0f; // 停止后保持的时间（秒）

    [Header("生成位置调整")]
    public bool alignToSurface = false; // 是否对齐到地面（Y轴自动调整）
    public LayerMask groundLayer; // 地面层，用于检测地面高度

    private Coroutine currentSpawner; // 当前生成的协程

    private void Start()
    {
        // 延迟开始生成
        StartCoroutine(StartTimedSpawner());
    }

    private IEnumerator StartTimedSpawner()
    {
        // 等待开始延迟
        if (startDelay > 0)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 开始生成特效
        StartSpawning();

        // 等待持续生成时间
        if (spawnDuration > 0)
        {
            yield return new WaitForSeconds(spawnDuration);
        }

        // 停止生成特效
        StopSpawning();

        // 停止后保持时间（如果有）
        if (stopDelay > 0)
        {
            yield return new WaitForSeconds(stopDelay);
        }

        // 可在这里触发后续逻辑，例如销毁对象或执行其他事件
        Debug.Log("特效生成任务完成");
    }

    /// <summary>
    /// 开始生成特效
    /// </summary>
    public void StartSpawning()
    {
        if (currentSpawner == null)
        {
            currentSpawner = StartCoroutine(SpawnEffects());
        }
    }

    /// <summary>
    /// 停止生成特效
    /// </summary>
    public void StopSpawning()
    {
        if (currentSpawner != null)
        {
            StopCoroutine(currentSpawner);
            currentSpawner = null;
        }
    }

    private IEnumerator SpawnEffects()
    {
        while (true)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPosition = GetRandomPosition();
                if (alignToSurface)
                {
                    spawnPosition = AlignToGround(spawnPosition);
                }

                Instantiate(effectPrefab, spawnPosition, Quaternion.identity);
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// 在指定范围内生成随机位置
    /// </summary>
    /// <returns>生成的随机位置</returns>
    private Vector3 GetRandomPosition()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );

        return transform.position + randomOffset;
    }

    /// <summary>
    /// 对齐生成位置到地面
    /// </summary>
    /// <param name="position">原始生成位置</param>
    /// <returns>对齐到地面的生成位置</returns>
    private Vector3 AlignToGround(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
        {
            position.y = hit.point.y;
        }
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize); // 显示生成范围
    }
}

