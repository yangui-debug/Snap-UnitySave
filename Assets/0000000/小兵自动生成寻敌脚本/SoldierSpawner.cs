using UnityEngine;
using System.Collections;

// ==================== 枚举定义 (全部中文) ====================
public enum 生成模式类型
{
    区域随机分布生成,
    单点扇形喷射生成
}

public enum 生成形状类型
{
    方形,
    圆形,
    扇形,
    弧形
}

public class SoldierSpawner : MonoBehaviour
{
    [Header("========== 生成物品设置 ==========")]
    [Tooltip("你要生成的士兵预制体（Prefab），把它拖到这里。注意：预制体上必须挂载 SoldierAIMovement 和 NavMeshAgent 组件。")]
    public GameObject soldierPrefab;

    [Tooltip("生成的区域大小。X是宽，Z是长。\n【仅在[区域随机分布生成]模式且形状为[方形]时生效】")]
    public Vector3 spawnAreaSize = new Vector3(10, 0, 10);

    // ==============================================================
    [Header("========== 核心模式选择 ==========")]
    [Tooltip("选择生成士兵的方式：\n" +
             "· 区域随机分布生成：在指定形状范围（方形/圆形/扇形/弧形）内随机散落生成。\n" +
             "· 单点扇形喷射生成：从小兵营中心这一个点生成，并沿扇形方向快速向外喷射弹出。")]
    public 生成模式类型 生成模式 = 生成模式类型.区域随机分布生成;

    // ==============================================================
    [Header("========== 模式一：区域随机分布生成设置 ==========")]
    [Tooltip("选择区域分布的形状（支持方形、圆形、扇形、弧形）")]
    public 生成形状类型 生成形状 = 生成形状类型.方形;

    [Tooltip("【圆形 / 扇形 / 弧形】使用的最大外半径（即该形状的最大范围）。")]
    public float 生成半径 = 5.0f;

    [Tooltip("【扇形 / 弧形】的张角范围（单位：度，例如 90、120、180）。")]
    [Range(1f, 360f)]
    public float 扇形角度 = 120.0f;

    [Tooltip("【弧形】的内圆挖空半径。\n如果设为0则退化成实心扇形；数值越接近[生成半径]，生成的环带就越薄。")]
    public float 弧形内半径 = 3.0f;

    // ==============================================================
    [Header("========== 模式二：单点扇形喷射生成设置 ==========")]
    [Tooltip("从兵营中心点向外喷射的扇形开角范围（单位：度）。\n例如：120 度表示以兵营正前方(Z轴)为中心，向左右各 60 度的扇形区域内随机喷射。")]
    [Range(1f, 360f)]
    public float 喷射扇形角度 = 120.0f;

    [Tooltip("喷射在场景 Scene 窗口中的预览画线距离（仅用于在编辑器中查看喷射范围）。")]
    public float 喷射预览距离 = 5.0f;

    [Tooltip("小兵被喷射弹出时的最小初速度。")]
    public float 最小喷射速度 = 8.0f;

    [Tooltip("小兵被喷射弹出时的最大初速度。")]
    public float 最大喷射速度 = 12.0f;

    [Tooltip("喷射爆发力衰减归零所需的时间（秒）。\n在此时间内小兵受喷射推力滑行并逐渐减速，随后完全交由AI自主寻路接管。")]
    public float 喷射持续时间 = 0.5f;

    // ==============================================================
    [Header("========== 生成时间与数量 ==========")]
    [Tooltip("游戏开始后，延迟多久（秒）才开始刷出第一个兵？")]
    public float startSpawnDelay = 2.0f;

    [Tooltip("每隔多久（秒）刷出一个兵？\n【核心功能：设为 0 时，将在开局延迟结束后同时瞬间生成设定的全部总士兵】")]
    public float spawnInterval = 1.0f;

    [Tooltip("这个兵营总共最多刷出多少个兵？")]
    public int totalSpawnCount = 10;

    // 内部计数器，记录已经生成了多少个
    private int currentSpawned = 0;

    // ==============================================================
    // ================== 新增：桥路点分配系统 ==================
    [Header("========== 桥路点分配 (新增) ==========")]
    [Tooltip("两座桥的入口路点（红方这一侧的桥头）。\n请把场景里创建的空物体拖进来，数组长度建议为 2。")]
    public Transform[] 桥入口路点;

    [Tooltip("两座桥的出口路点（蓝方那一侧的桥尾）。\n顺序必须和【桥入口路点】一一对应！")]
    public Transform[] 桥出口路点;

    [Tooltip("距离容差。如果士兵离两座桥的距离差小于这个值，系统会认为“两座桥一样近”，从而按奇偶交替分配，避免全部挤到同一座桥。\n推荐值：2.0 ~ 5.0")]
    public float 距离容差 = 3.0f;

    // ==============================================================

    void Start()
    {
        // 游戏一开始，启动持续生成的协程（时间计时器）
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // 1. 先等待你设定的“开局延迟时间”
        if (startSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(startSpawnDelay);
        }

        // 2. 判断生成间隔：若间隔 <= 0，则在当前帧瞬间一次性生成所有士兵！
        if (spawnInterval <= 0f)
        {
            while (currentSpawned < totalSpawnCount)
            {
                SpawnSoldier();
                currentSpawned++;
            }
        }
        else
        {
            // 否则按设置的间隔时间逐个生成
            while (currentSpawned < totalSpawnCount)
            {
                SpawnSoldier();
                currentSpawned++;

                // 休息设定的“间隔时间”，再生成下一个
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    void SpawnSoldier()
    {
        if (soldierPrefab == null)
        {
            Debug.LogWarning("兵营找不到士兵预制体，请检查 Inspector 面板是否挂载了预制体！");
            return;
        }

        GameObject newSoldier = null;

        // ================== 1. 生成士兵 ==================
        if (生成模式 == 生成模式类型.区域随机分布生成)
        {
            // 模式一：在区域内随机算出一个坐标点生成
            Vector3 randomOffset = CalculateRandomSpawnPoint();
            Vector3 spawnPos = transform.position + randomOffset;

            // 保持兵营自身的朝向生成士兵
            newSoldier = Instantiate(soldierPrefab, spawnPos, transform.rotation);
        }
        else if (生成模式 == 生成模式类型.单点扇形喷射生成)
        {
            // 模式二：严格从小兵营正中心点生成
            Vector3 spawnPos = transform.position;

            // 1. 在设定的扇形角度范围内计算一个随机喷射方向
            float halfAngle = 喷射扇形角度 / 2f;
            float randomAngle = Random.Range(-halfAngle, halfAngle);
            Quaternion randomRot = Quaternion.Euler(0, randomAngle, 0);
            Vector3 ejectDirection = randomRot * transform.forward;

            // 2. 在最小与最大喷射速度之间随机抽取初速度
            float randomEjectSpeed = Random.Range(最小喷射速度, 最大喷射速度);

            // 3. 实例化生成士兵，让其初始朝向喷射方向
            newSoldier = Instantiate(soldierPrefab, spawnPos, Quaternion.LookRotation(ejectDirection));

            // 4. 将喷射初速度和方向传递给小兵身上的 AI 移动脚本
            SoldierAIMovement aiMovement = newSoldier.GetComponent<SoldierAIMovement>();
            if (aiMovement != null)
            {
                aiMovement.触发喷射(ejectDirection, randomEjectSpeed, 喷射持续时间);
            }
        }

        // ================== 2. 给新生成的士兵分配最近的桥 ==================
        if (newSoldier != null)
        {
            // 可选的红方兵营接入：只复制单兵配置，不改变生成随机数、数量、路线与喷射。
            var redSetup = GetComponent<RedMeleeSpawnSetup>();
            if (redSetup != null) redSetup.Install(newSoldier);
            SoldierAIMovement ai = newSoldier.GetComponent<SoldierAIMovement>();

            // 防御性检查：确保路点数组存在且长度大于 0，否则不分配，让士兵直接索敌
            if (ai != null && 桥入口路点 != null && 桥入口路点.Length > 0 && 桥出口路点 != null && 桥出口路点.Length > 0)
            {
                // 核心逻辑：寻找最近的桥
                int closestBridgeIndex = 0;
                float minDistance = Mathf.Infinity;

                for (int i = 0; i < 桥入口路点.Length; i++)
                {
                    if (桥入口路点[i] == null) continue; // 防止数组里有空元素

                    // 计算士兵当前生成位置到桥入口的距离（忽略 Y 轴高度差，只看平面距离）
                    Vector3 soldierPos = newSoldier.transform.position;
                    Vector3 bridgePos = 桥入口路点[i].position;
                    soldierPos.y = 0;
                    bridgePos.y = 0;
                    float dist = Vector3.Distance(soldierPos, bridgePos);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestBridgeIndex = i;
                    }
                }

                // ========== 负载均衡优化 ==========
                // 如果两座桥距离差不多（差值小于容差），说明兵营在中间。
                // 此时如果全按“最近”算，所有兵都会去同一座桥，导致拥堵。
                // 所以这种情况下，我们改用“奇偶交替”来强制分流。
                if (桥入口路点.Length >= 2)
                {
                    // 找出第二近的桥的距离
                    float secondMinDistance = Mathf.Infinity;
                    for (int i = 0; i < 桥入口路点.Length; i++)
                    {
                        if (i == closestBridgeIndex || 桥入口路点[i] == null) continue;
                        Vector3 soldierPos = newSoldier.transform.position;
                        Vector3 bridgePos = 桥入口路点[i].position;
                        soldierPos.y = 0;
                        bridgePos.y = 0;
                        float dist = Vector3.Distance(soldierPos, bridgePos);
                        if (dist < secondMinDistance)
                        {
                            secondMinDistance = dist;
                        }
                    }

                    // 如果最近的桥和第二近的桥距离差很小，说明它们一样近
                    if (Mathf.Abs(minDistance - secondMinDistance) < 距离容差)
                    {
                        // 按生成序号奇偶交替分配，强制分成两股
                        closestBridgeIndex = currentSpawned % 桥入口路点.Length;
                        // Debug.Log($"士兵生成位置到两座桥距离相近，启用交替分配，分配到桥 {closestBridgeIndex}");
                    }
                }
                // ==================================

                // 分配路点给 AI
                ai.设置桥路点(桥入口路点[closestBridgeIndex], 桥出口路点[closestBridgeIndex]);
            }
        }
    }

    // 辅助方法：根据选定的形状在区域内计算随机相对坐标
    Vector3 CalculateRandomSpawnPoint()
    {
        switch (生成形状)
        {
            case 生成形状类型.方形:
                return new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0,
                    Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f)
                );

            case 生成形状类型.圆形:
                float circleAngle = Random.Range(0f, Mathf.PI * 2f);
                float circleR = Mathf.Sqrt(Random.Range(0f, 1f)) * 生成半径;
                return new Vector3(Mathf.Cos(circleAngle) * circleR, 0, Mathf.Sin(circleAngle) * circleR);

            case 生成形状类型.扇形:
                float sectorHalfAngle = 扇形角度 / 2f;
                float sectorRandomAngle = Random.Range(-sectorHalfAngle, sectorHalfAngle);
                float sectorR = Mathf.Sqrt(Random.Range(0f, 1f)) * 生成半径;
                Quaternion sectorRot = Quaternion.Euler(0, sectorRandomAngle, 0);
                return sectorRot * transform.forward * sectorR;

            case 生成形状类型.弧形:
                float minR = Mathf.Min(弧形内半径, 生成半径);
                float maxR = Mathf.Max(弧形内半径, 生成半径);
                float arcR = Mathf.Sqrt(Random.Range(minR * minR, maxR * maxR));

                float arcHalfAngle = 扇形角度 / 2f;
                float arcRandomAngle = Random.Range(-arcHalfAngle, arcHalfAngle);
                Quaternion arcRot = Quaternion.Euler(0, arcRandomAngle, 0);
                return arcRot * transform.forward * arcR;

            default:
                return Vector3.zero;
        }
    }

    // 在编辑器 Scene 窗口下可视化绘制绿色预览线框
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);

        if (生成模式 == 生成模式类型.区域随机分布生成)
        {
            // 绘制区域生成的各形状线框
            switch (生成形状)
            {
                case 生成形状类型.方形:
                    Gizmos.DrawCube(transform.position, spawnAreaSize);
                    break;

                case 生成形状类型.圆形:
                    DrawGizmosCircle(transform.position, 生成半径);
                    break;

                case 生成形状类型.扇形:
                    DrawGizmosArc(transform.position, 0f, 生成半径, 扇形角度);
                    break;

                case 生成形状类型.弧形:
                    float minR = Mathf.Min(弧形内半径, 生成半径);
                    float maxR = Mathf.Max(弧形内半径, 生成半径);
                    DrawGizmosArc(transform.position, minR, maxR, 扇形角度);
                    break;
            }
        }
        else if (生成模式 == 生成模式类型.单点扇形喷射生成)
        {
            // 绘制单点生成点及喷射扇形线框
            Gizmos.DrawSphere(transform.position, 0.3f);
            DrawGizmosArc(transform.position, 0f, 喷射预览距离, 喷射扇形角度);
        }

        // 在 Scene 窗口用蓝色小球标出桥入口的位置，方便你调试
        if (桥入口路点 != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var entry in 桥入口路点)
            {
                if (entry != null)
                {
                    Gizmos.DrawSphere(entry.position, 0.5f);
                }
            }
        }
    }

    // 辅助画圆
    private void DrawGizmosCircle(Vector3 center, float radius)
    {
        int segments = 36;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i * 360f / segments) * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    // 辅助画扇形与弧形
    private void DrawGizmosArc(Vector3 center, float innerRadius, float outerRadius, float angle)
    {
        int segments = 24;
        float halfAngle = angle / 2f;

        Vector3 prevOuter = center + Quaternion.Euler(0, -halfAngle, 0) * transform.forward * outerRadius;
        Vector3 prevInner = center + Quaternion.Euler(0, -halfAngle, 0) * transform.forward * innerRadius;

        Gizmos.DrawLine(prevInner, prevOuter);

        for (int i = 1; i <= segments; i++)
        {
            float currentA = -halfAngle + (angle / segments) * i;
            Quaternion rot = Quaternion.Euler(0, currentA, 0);
            Vector3 nextOuter = center + rot * transform.forward * outerRadius;
            Vector3 nextInner = center + rot * transform.forward * innerRadius;

            Gizmos.DrawLine(prevOuter, nextOuter);
            if (innerRadius > 0.001f)
            {
                Gizmos.DrawLine(prevInner, nextInner);
            }

            prevOuter = nextOuter;
            prevInner = nextInner;
        }

        Gizmos.DrawLine(prevInner, prevOuter);
    }
}