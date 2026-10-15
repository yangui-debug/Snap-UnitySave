using UnityEngine;
using System.Collections;

public class ProjectilePathLoop : MonoBehaviour
{
    [Header("预制体配置")]
    [Tooltip("预制体1：从A移动到B的物体（如导弹、子弹）")]
    public GameObject prefab1;
    [Tooltip("预制体2：在B点生成的物体（如爆炸特效）")]
    public GameObject prefab2;

    [Header("位置配置")]
    public Transform pointA;
    public Transform pointB;

    [Header("时间与延迟")]
    [Tooltip("游戏开始后，延迟多久才开始第一次循环")]
    public float startDelay = 1.0f;
    [Tooltip("预制体1 从A移动到B所需的时间")]
    public float moveDuration = 2.0f;
    [Tooltip("预制体2 生成后销毁的时间")]
    public float prefab2LifeTime = 1.0f;
    [Tooltip("两轮循环之间的间隔时间")]
    public float loopInterval = 1.0f;

    [Header("移动曲线")]
    public AnimationCurve moveCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private void Start()
    {
        // 开启主循环协程
        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        // 1. 初始延迟
        if (startDelay > 0)
            yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // 2. 生成预制体1 并执行移动
            yield return StartCoroutine(MovePrefab1());

            // 3. 循环间隔
            yield return new WaitForSeconds(loopInterval);
        }
    }

    private IEnumerator MovePrefab1()
    {
        if (prefab1 == null || pointA == null || pointB == null) yield break;

        // 生成预制体1
        GameObject p1Instance = Instantiate(prefab1, pointA.position, pointA.rotation);

        float timer = 0;
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;
            float curveT = moveCurve.Evaluate(t);

            // 插值移动位置
            p1Instance.transform.position = Vector3.Lerp(pointA.position, pointB.position, curveT);

            // 如果希望它面向目标移动
            p1Instance.transform.LookAt(pointB.position);

            yield return null;
        }

        // 到达B点，销毁预制体1
        Destroy(p1Instance);

        // 生成预制体2
        SpawnPrefab2();
    }

    private void SpawnPrefab2()
    {
        if (prefab2 == null || pointB == null) return;

        // 在B点生成预制体2
        GameObject p2Instance = Instantiate(prefab2, pointB.position, pointB.rotation);

        // 1秒后销毁预制体2
        Destroy(p2Instance, prefab2LifeTime);
    }
}