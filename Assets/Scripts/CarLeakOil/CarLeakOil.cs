using UnityEngine;
using System.Collections.Generic;

public class CarLeakOil : MonoBehaviour
{
    [Header("油渍预制体 (必须是 Decal Projector)")]
    public GameObject oilDecalPrefab;

    [Header("漏油点（可以添加多个）")]
    public List<Transform> leakPoints = new List<Transform>();

    [Header("生成参数")]
    public float spawnInterval = 0.5f;   // 油渍生成间隔
    public Vector2 decalSizeRange = new Vector2(0.5f, 1.5f);
    public float randomRotation = 360f;

    [Header("时间控制")]
    public float startDelay = 0f;        // 延迟多久开始
    public float stopAfterTime = -1f;    // 多久后停止 (-1=不停止)

    private float timer;
    private float totalTime;
    private bool hasStarted = false;
    private bool hasStopped = false;

    void Update()
    {
        totalTime += Time.deltaTime;

        // 控制开始时间
        if (!hasStarted)
        {
            if (totalTime >= startDelay)
            {
                hasStarted = true;
                timer = 0f;
            }
            else
            {
                return;
            }
        }

        // 控制停止时间
        if (!hasStopped && stopAfterTime > 0)
        {
            if (totalTime >= startDelay + stopAfterTime)
            {
                hasStopped = true;
                return;
            }
        }

        if (hasStopped) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnOilDecal();
            timer = 0f;
        }
    }

    void SpawnOilDecal()
    {
        if (oilDecalPrefab == null || leakPoints.Count == 0)
            return;

        foreach (var leakPoint in leakPoints)
        {
            if (leakPoint == null) continue;

            // 生成 Decal
            GameObject decal =
                Instantiate(
                    oilDecalPrefab,
                    leakPoint.position,
                    Quaternion.identity
                );

            var projector =
                decal.GetComponent<UnityEngine.Rendering.Universal.DecalProjector>();

            if (projector != null)
            {
                float size =
                    Random.Range(
                        decalSizeRange.x,
                        decalSizeRange.y
                    );

                projector.size =
                    new Vector3(
                        size,
                        size,
                        projector.size.z
                    );

                // 随机旋转
                decal.transform.rotation =
                    Quaternion.Euler(
                        90f,
                        0f,
                        Random.Range(
                            0,
                            randomRotation
                        )
                    );
            }

            // 微微抬高避免Z-fighting
            decal.transform.position =
                new Vector3(
                    decal.transform.position.x,
                    decal.transform.position.y + 0.01f,
                    decal.transform.position.z
                );
        }
    }
}