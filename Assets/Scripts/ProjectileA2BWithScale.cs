using UnityEngine;
using System.Collections;

public class ProjectileA2BWithScale : MonoBehaviour
{
    [Header("预制体配置")]
    public GameObject prefab1; // 移动物体
    public GameObject prefab2; // 目的地特效

    [Header("缩放调节")]
    [Tooltip("移动物体（预制体1）的等比例缩放倍数")]
    [Range(0.1f, 10f)] public float prefab1Scale = 1.0f;
    [Tooltip("爆炸特效（预制体2）的等比例缩放倍数")]
    [Range(0.1f, 10f)] public float prefab2Scale = 1.0f;

    [Header("位置配置 (拖入 Cube)")]
    public GameObject cubeA;
    public GameObject cubeB;

    [Header("时间参数")]
    public float initialDelay = 1.0f;

    [Header("移动错落性")]
    public float minMoveDuration = 1.5f;
    public float maxMoveDuration = 2.5f;

    [Header("循环与寿命")]
    public float prefab2LifeTime = 1.0f;
    public float minLoopInterval = 0.5f;
    public float maxLoopInterval = 2.0f;

    private void Start()
    {
        if (cubeA == null || cubeB == null || prefab1 == null)
        {
            Debug.LogError("请检查脚本上的物体赋值是否完整！");
            return;
        }

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

            // --- 应用预制体1的缩放 ---
            p1.transform.localScale = Vector3.one * prefab1Scale;

            float moveTime = Random.Range(minMoveDuration, maxMoveDuration);

            // 3. 移动过程
            float timer = 0;
            Vector3 startPos = cubeA.transform.position;

            while (timer < moveTime)
            {
                if (p1 == null) yield break;

                timer += Time.deltaTime;
                float t = timer / moveTime;

                Vector3 endPos = cubeB.transform.position;

                p1.transform.position = Vector3.Lerp(startPos, endPos, t);
                p1.transform.LookAt(endPos);

                yield return null;
            }

            // 4. 到达目标，销毁 P1，生成 P2
            Destroy(p1);

            if (prefab2 != null)
            {
                GameObject p2 = Instantiate(prefab2, cubeB.transform.position, Quaternion.identity);

                // --- 应用预制体2的缩放 ---
                p2.transform.localScale = Vector3.one * prefab2Scale;

                Destroy(p2, prefab2LifeTime);
            }

            // 5. 随机间隔
            float nextInterval = Random.Range(minLoopInterval, maxLoopInterval);
            yield return new WaitForSeconds(nextInterval);
        }
    }
}