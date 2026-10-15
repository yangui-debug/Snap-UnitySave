using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SoldierFormationController : MonoBehaviour
{
    public struct PathPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public PathPoint(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    [Header("基础引用")]
    public GameObject soldierPrefab;
    public Transform leader;        

    [Header("Timeline 可控制参数")]
    [Range(0, 100)] 
    public int soldierCount = 5;
    
    [Tooltip("0 = 蜿蜒跟随主角, 1 = 包围最近的建筑")]
    [Range(0f, 1f)]
    public float surroundWeight = 0f; 

    [Header("队伍跟随设置")]
    [Min(0.1f)] 
    public float followSpacing = 1.5f; 
    
    [Min(0.1f)] 
    public float recordSpacing = 0.5f; 
    
    // ==========================================
    // 新增：动态补位机制
    // ==========================================
    [Header("动态补位设置")]
    [Tooltip("当队伍出现缺口时，后面士兵往前跑来填补缺口的速度")]
    public float gapCloseSpeed = 3.0f;

    [Header("阵型包围设置")]
    public float surroundRadius = 5f;  

    [Header("旋转平滑")]
    [Range(1f, 20f)]
    public float rotationLerpSpeed = 10f;

    [Header("动画设置 (需预制体挂载 Animator)")]
    public string speedParam = "Speed";
    public string attackParam = "IsAttacking";
    [Tooltip("当包围权重超过此值时，触发攻击动画")]
    [Range(0f, 1f)]
    public float attackWeightThreshold = 0.8f;

    // --- 内部状态 ---
    private Transform currentTargetBuilding;
    private TargetBuilding[] allBuildings;
    private float lastSearchTime = 0f;

    [SerializeField, HideInInspector] 
    private List<GameObject> soldiers = new List<GameObject>();
    private List<PathPoint> pathHistory = new List<PathPoint>();
    private Vector3 lastLeaderPos;

    // 新增：记录每个士兵当前实际跟在队长后面的距离
    private List<float> currentDistances = new List<float>();

    private Dictionary<GameObject, Animator> animatorCache = new Dictionary<GameObject, Animator>();

    void Update()
    {
        if (leader == null) return;

        recordSpacing = Mathf.Max(0.1f, recordSpacing);
        followSpacing = Mathf.Max(0.1f, followSpacing);

        UpdatePathHistory();
        FindNearestBuilding();
        UpdateFormation();
    }

    private void ResetPathHistory()
    {
        pathHistory.Clear();
        float maxRequiredDist = 100 * followSpacing; 
        int neededPoints = Mathf.CeilToInt(maxRequiredDist / recordSpacing) + 5;

        for (int i = 0; i < neededPoints; i++)
        {
            Vector3 fakePos = leader.position - leader.forward * (i * recordSpacing);
            pathHistory.Add(new PathPoint(fakePos, leader.rotation));
        }
        lastLeaderPos = leader.position;
    }

    private void UpdatePathHistory()
    {
        if (pathHistory.Count == 0)
        {
            ResetPathHistory();
            return;
        }

        if (Vector3.Distance(leader.position, pathHistory[0].position) > 10f)
        {
            ResetPathHistory();
            return;
        }

        float distSinceLastRecord = Vector3.Distance(leader.position, pathHistory[0].position);
        
        while (distSinceLastRecord >= recordSpacing)
        {
            Vector3 dir = (leader.position - pathHistory[0].position).normalized;
            Vector3 newPos = pathHistory[0].position + dir * recordSpacing;
            Quaternion newRot = Quaternion.Slerp(pathHistory[0].rotation, leader.rotation, recordSpacing / distSinceLastRecord);
            
            pathHistory.Insert(0, new PathPoint(newPos, newRot));
            distSinceLastRecord = Vector3.Distance(leader.position, pathHistory[0].position);
        }

        int maxHistory = Mathf.CeilToInt((100 * followSpacing) / recordSpacing) + 20;
        if (pathHistory.Count > maxHistory)
        {
            pathHistory.RemoveRange(maxHistory, pathHistory.Count - maxHistory);
        }
    }

    private void FindNearestBuilding()
    {
        if (Application.isPlaying && Time.time - lastSearchTime < 0.5f) return;
        if (!Application.isPlaying && Time.realtimeSinceStartup - lastSearchTime < 0.5f) return;
        
        lastSearchTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        allBuildings = Object.FindObjectsByType<TargetBuilding>(FindObjectsSortMode.None);

        if (allBuildings == null || allBuildings.Length == 0)
        {
            currentTargetBuilding = null;
            return;
        }

        Transform nearest = null;
        float minDistance = float.MaxValue;
        Vector3 leaderPos = leader.position;

        foreach (var building in allBuildings)
        {
            if (building == null) continue;
            float dist = Vector3.SqrMagnitude(building.transform.position - leaderPos);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = building.transform;
            }
        }
        currentTargetBuilding = nearest;
    }

    public void UpdateFormation()
    {
        if (soldierPrefab == null) return;
        SyncExistingSoldiers();

        while (soldiers.Count < soldierCount)
        {
            GameObject newSoldier = null;
#if UNITY_EDITOR
            if (!Application.isPlaying && PrefabUtility.IsPartOfAnyPrefab(soldierPrefab))
                newSoldier = (GameObject)PrefabUtility.InstantiatePrefab(soldierPrefab, transform);
            else
                newSoldier = Instantiate(soldierPrefab, transform);
#else
            newSoldier = Instantiate(soldierPrefab, transform);
#endif
            soldiers.Add(newSoldier);
        }

        float currentOffset = Vector3.Distance(leader.position, pathHistory[0].position);

        for (int i = 0; i < soldiers.Count; i++)
        {
            var soldier = soldiers[i];
            if (soldier == null) continue;

            // 确保距离数组长度一致
            if (i >= currentDistances.Count)
            {
                currentDistances.Add(followSpacing * (i + 1));
            }

            // 检查士兵是否已经阵亡
            SoldierHealth health = soldier.GetComponent<SoldierHealth>();
            if (health != null && health.isDead)
            {
                if (Application.isPlaying)
                {
                    soldierCount--; // 动态减少总长度
                    soldiers.RemoveAt(i);
                    currentDistances.RemoveAt(i); // 同步移除该距离数据
                    i--; 
                    continue;
                }
            }

            if (i < soldierCount)
            {
                if (!soldier.activeSelf) soldier.SetActive(true);

                Vector3 oldPos = soldier.transform.position;
                Vector3 followPos = leader.position;
                Quaternion followRot = leader.rotation;

                // ==========================================
                // 核心修改：平滑计算当前士兵应该在的距离
                // ==========================================
                float idealTargetDist = followSpacing * (i + 1);
                
                if (Application.isPlaying)
                {
                    // 动态向目标距离靠近，形成“跑去填补空缺”的效果
                    currentDistances[i] = Mathf.MoveTowards(currentDistances[i], idealTargetDist, gapCloseSpeed * Time.deltaTime);
                }
                else
                {
                    // 编辑器下直接就位
                    currentDistances[i] = idealTargetDist;
                }

                float usedDist = currentDistances[i];
                float searchDist = usedDist - currentOffset;

                // 根据实际的 usedDist 去历史轨迹里找位置
                if (searchDist <= 0)
                {
                    float t = usedDist / Mathf.Max(0.001f, currentOffset);
                    followPos = Vector3.Lerp(leader.position, pathHistory[0].position, t);
                    followRot = Quaternion.Slerp(leader.rotation, pathHistory[0].rotation, t);
                }
                else
                {
                    int index = Mathf.FloorToInt(searchDist / recordSpacing);
                    if (index >= pathHistory.Count - 1)
                    {
                        followPos = pathHistory[pathHistory.Count - 1].position;
                        followRot = pathHistory[pathHistory.Count - 1].rotation;
                    }
                    else
                    {
                        float t = (searchDist % recordSpacing) / recordSpacing;
                        followPos = Vector3.Lerp(pathHistory[index].position, pathHistory[index + 1].position, t);
                        followRot = Quaternion.Slerp(pathHistory[index].rotation, pathHistory[index + 1].rotation, t);
                    }
                }

                Vector3 surroundPos = followPos; 
                Quaternion surroundRot = followRot;
                
                if (currentTargetBuilding != null)
                {
                    float angle = i * Mathf.PI * 2f / soldierCount;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * surroundRadius, 0, Mathf.Sin(angle) * surroundRadius);
                    surroundPos = new Vector3(currentTargetBuilding.position.x + offset.x, leader.position.y, currentTargetBuilding.position.z + offset.z);
                    
                    Vector3 dirToBuilding = currentTargetBuilding.position - surroundPos;
                    dirToBuilding.y = 0;
                    if (dirToBuilding.sqrMagnitude > 0.001f) surroundRot = Quaternion.LookRotation(dirToBuilding);
                }

                Vector3 targetPos = Vector3.Lerp(followPos, surroundPos, surroundWeight);
                Quaternion targetRot = Quaternion.Slerp(followRot, surroundRot, surroundWeight);

                // 依旧严格贴合路线，绝不抄近道
                soldier.transform.position = targetPos;

                if (Application.isPlaying)
                {
                    soldier.transform.rotation = Quaternion.Lerp(soldier.transform.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
                    
                    if (!animatorCache.TryGetValue(soldier, out Animator anim))
                    {
                        anim = soldier.GetComponentInChildren<Animator>();
                        animatorCache[soldier] = anim;
                    }

                    if (anim != null)
                    {
                        float dt = Mathf.Max(0.0001f, Time.deltaTime);
                        float moveSpeed = Vector3.Distance(oldPos, targetPos) / dt;
                        
                        if (moveSpeed < 0.1f) moveSpeed = 0f;
                        anim.SetFloat(speedParam, moveSpeed);

                        bool isAttacking = surroundWeight >= attackWeightThreshold;
                        anim.SetBool(attackParam, isAttacking);
                    }
                }
                else
                {
                    soldier.transform.rotation = targetRot;
                }
            }
            else
            {
                if (soldier.activeSelf) soldier.SetActive(false);
            }
        }
    }

    private void SyncExistingSoldiers()
    {
        soldiers.RemoveAll(item => item == null);
        foreach (Transform child in transform)
        {
            SoldierHealth health = child.GetComponent<SoldierHealth>();
            if (health != null && health.isDead) continue;

            if (!soldiers.Contains(child.gameObject))
            {
                soldiers.Add(child.gameObject);
            }
        }
    }
}