using UnityEngine;
using System.Collections;

public class ProjectileA2B : MonoBehaviour
{
    [Header("预制体配置")]
    public GameObject prefab1; // 移动物体（导弹、子弹）
    public GameObject prefab2; // 目的地特效（爆炸、光效）

    [Header("位置配置 (拖入 Cube)")]
    public GameObject cubeA;
    public GameObject cubeB;

    [Header("时间参数")]
    [Tooltip("延迟多少秒开始第一次循环")]
    public float initialDelay = 1.0f;

    [Tooltip("移动过程的错落性：给移动时间加一个随机范围")]
    public float minMoveDuration = 1.5f;
    public float maxMoveDuration = 2.5f;

    [Tooltip("爆炸预制体2的销毁时间")]
    public float prefab2LifeTime = 1.0f;

    [Tooltip("两轮发射之间的间隔")]
    public float minLoopInterval = 0.5f;
    public float maxLoopInterval = 2.0f;

    private void Start()
    {
        // 验证赋值
        if (cubeA == null || cubeB == null || prefab1 == null)
        {
            Debug.LogError("请检查脚本上的物体赋值是否完整！");
            return;
        }

        // 开启主循环
        StartCoroutine(MainProcess());
    }

    private IEnumerator MainProcess()
    {
        // 1. 初始延迟
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // 2. 生成预制体1
            GameObject p1 = Instantiate(prefab1, cubeA.transform.position, Quaternion.identity);

            // 随机本次移动的时间，增加错落感
            float moveTime = Random.Range(minMoveDuration, maxMoveDuration);

            // 3. 移动过程
            float timer = 0;
            Vector3 startPos = cubeA.transform.position;

            while (timer < moveTime)
            {
                if (p1 == null) yield break; // 容错处理

                timer += Time.deltaTime;
                float t = timer / moveTime;

                // 实时获取 B 的位置（防止 B 移动了）
                Vector3 endPos = cubeB.transform.position;

                // 平滑插值移动
                p1.transform.position = Vector3.Lerp(startPos, endPos, t);
                p1.transform.LookAt(endPos); // 始终朝向目标

                yield return null;
            }

            // 4. 到达目标，销毁 P1，生成 P2
            Destroy(p1);
            if (prefab2 != null)
            {
                GameObject p2 = Instantiate(prefab2, cubeB.transform.position, Quaternion.identity);
                Destroy(p2, prefab2LifeTime);
            }

            // 5. 随机间隔后再进行下一次发射
            float nextInterval = Random.Range(minLoopInterval, maxLoopInterval);
            yield return new WaitForSeconds(nextInterval);
        }
    }
}