using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TimelineMultiPathSpawner : MonoBehaviour
{
    [Header("【1】多路径配置（支持无限扩展）")]
    [Tooltip("在这里配置多条路径，每条路径包含自己独立的起点和终点")]
    public List<PathRoute> routes = new List<PathRoute>();

    [Header("【2】预制体设置")]
    public GameObject prefabA;
    public GameObject prefabB;

    [Header("【等比例大小控制】")]
    public float prefabAScaleMultiplier = 1.0f;
    public float prefabBScaleMultiplier = 1.0f;

    [Header("【3】移动与生命周期控制")]
    public float moveSpeed = 5f;
    public float prefabBDestroyDelay = 1.5f;
    [Tooltip("这一轮所有路径发完后，间隔几秒再放下一轮")]
    public float loopInterval = 4f;

    [Header("【4】首次延迟控制")]
    public float startDelay = 3f;

    [Header("【5】高度与朝向修正")]
    public float spawnHeight = 0f;
    [Tooltip("如果模型默认方向不对，可以在这里进行局部旋转修正")]
    public Vector3 fixRotationOffset = Vector3.zero;

    // 内部运行期实例管理
    private class ActiveInstancePair
    {
        public GameObject instanceA;
        public GameObject instanceB;
        public float bTimer;
        public bool isBActive;
        public bool isAMoving;
    }

    private List<ActiveInstancePair> m_ActivePairs = new List<ActiveInstancePair>();
    private float m_IntervalTimer = 0f;
    private float m_DelayTimer = 0f;
    private bool m_IsWaitingForNextLoop = false;
    private bool m_HasStarted = false;

    void OnEnable()
    {
        ResetSystem();
    }

    void OnDisable()
    {
        ClearAllInstances();
    }

    void LateUpdate()
    {
        if (routes == null || routes.Count == 0 || prefabA == null || prefabB == null) return;

        // 非运行模式：走 Timeline 编辑器下的实时位置预览
        if (!Application.isPlaying)
        {
            UpdateEditorPreview();
            return;
        }

        // 检查首次生成延迟
        if (!m_HasStarted)
        {
            m_DelayTimer += Time.deltaTime;
            if (m_DelayTimer >= startDelay)
            {
                m_HasStarted = true;
                SpawnNewRound();
            }
            return;
        }

        // 核心多路径状态机轮询
        bool anyUnitActive = false;

        for (int i = 0; i < routes.Count; i++)
        {
            if (i >= m_ActivePairs.Count) continue;
            var pair = m_ActivePairs[i];
            var route = routes[i];

            if (route.startPoint == null || route.endPoint == null) continue;

            // 1. 逻辑 A：处理该路径上的爆裂粒子 B 黏随和倒计时消失
            if (pair.isBActive && pair.instanceB != null)
            {
                anyUnitActive = true;
                pair.instanceB.transform.position = GetHeightAdjustedPos(route.endPoint.position);
                pair.instanceB.transform.rotation = route.endPoint.rotation * Quaternion.Euler(fixRotationOffset);

                pair.bTimer += Time.deltaTime;
                if (pair.bTimer >= prefabBDestroyDelay)
                {
                    pair.instanceB.SetActive(false);
                    pair.isBActive = false;
                }
            }

            // 2. 逻辑 B：处理该路径上的子弹 A 凌空翻滚和飞向终点
            if (pair.isAMoving && pair.instanceA != null)
            {
                anyUnitActive = true;
                Vector3 targetPos = GetHeightAdjustedPos(route.endPoint.position);

                // 位置推进
                pair.instanceA.transform.position = Vector3.MoveTowards(pair.instanceA.transform.position, targetPos, moveSpeed * Time.deltaTime);

                // 电影级翻滚：正面对准当前路径终点，Up轴死死咬住该路径起点的up轴
                Vector3 moveDirection = targetPos - pair.instanceA.transform.position;
                if (moveDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion pathRotation = Quaternion.LookRotation(moveDirection, route.startPoint.up);
                    pair.instanceA.transform.rotation = pathRotation * Quaternion.Euler(fixRotationOffset);
                }

                // 抵达终点，触发撞击爆裂
                if (Vector3.Distance(pair.instanceA.transform.position, targetPos) < 0.05f)
                {
                    TriggerCollisionEvent(i);
                }
            }
        }

        // 3. 循环等待逻辑：如果这一轮所有的 A 飞完、B 炸完了，开始等 loopInterval 进下一轮
        if (!anyUnitActive || m_IsWaitingForNextLoop)
        {
            m_IsWaitingForNextLoop = true;
            m_IntervalTimer += Time.deltaTime;
            if (m_IntervalTimer >= loopInterval)
            {
                SpawnNewRound();
            }
        }
    }

    private void ResetSystem()
    {
        ClearAllInstances();
        m_IntervalTimer = 0f;
        m_DelayTimer = 0f;
        m_IsWaitingForNextLoop = false;
        m_HasStarted = false;

        m_ActivePairs.Clear();
        for (int i = 0; i < routes.Count; i++)
        {
            m_ActivePairs.Add(new ActiveInstancePair());
        }
    }

    private void SpawnNewRound()
    {
        m_IsWaitingForNextLoop = false;
        m_IntervalTimer = 0f;

        for (int i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            if (route.startPoint == null || route.endPoint == null) continue;

            var pair = m_ActivePairs[i];

            // 清理上一轮残留
            if (pair.instanceB != null) pair.instanceB.SetActive(false);
            pair.isBActive = false;
            if (pair.instanceA != null) Destroy(pair.instanceA);

            // 在各自路径的起点分别吐出子弹 A
            Vector3 spawnPos = GetHeightAdjustedPos(route.startPoint.position);
            pair.instanceA = Instantiate(prefabA, spawnPos, route.startPoint.rotation, this.transform);
            pair.instanceA.transform.localScale = Vector3.one * prefabAScaleMultiplier;
            pair.isAMoving = true;

            // 初始帧朝向
            Vector3 targetPos = GetHeightAdjustedPos(route.endPoint.position);
            Vector3 moveDirection = targetPos - spawnPos;
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                pair.instanceA.transform.rotation = Quaternion.LookRotation(moveDirection, route.startPoint.up) * Quaternion.Euler(fixRotationOffset);
            }
        }
    }

    private void TriggerCollisionEvent(int routeIndex)
    {
        var pair = m_ActivePairs[routeIndex];
        var route = routes[routeIndex];

        // 销毁对应的子弹 A
        if (pair.instanceA != null)
        {
            Destroy(pair.instanceA);
            pair.instanceA = null;
        }
        pair.isAMoving = false;

        // 在对应终点炸开特效 B
        Vector3 targetPos = GetHeightAdjustedPos(route.endPoint.position);
        if (pair.instanceB == null)
        {
            pair.instanceB = Instantiate(prefabB, targetPos, route.endPoint.rotation * Quaternion.Euler(fixRotationOffset), this.transform);
        }

        pair.instanceB.transform.position = targetPos;
        pair.instanceB.SetActive(true);
        pair.instanceB.transform.localScale = Vector3.one * prefabBScaleMultiplier;

        pair.isBActive = true;
        pair.bTimer = 0f;
    }

    /// <summary>
    /// 【高级优化】：不Play游戏、在时间轴拖动时，让所有的终点特效 B 同时在多个终点闪现预览！
    /// </summary>
    private void UpdateEditorPreview()
    {
        if (routes == null || routes.Count == 0) return;

        // 收集当前场景下的子物体进行多终点对齐
        int bIndex = 0;
        foreach (Transform child in transform)
        {
            if (child.name.Contains(prefabB.name))
            {
                if (bIndex < routes.Count)
                {
                    var route = routes[bIndex];
                    if (route.endPoint != null)
                    {
                        child.position = GetHeightAdjustedPos(route.endPoint.position);
                        child.rotation = route.endPoint.rotation * Quaternion.Euler(fixRotationOffset);
                        child.localScale = Vector3.one * prefabBScaleMultiplier;
                    }
                    bIndex++;
                }
            }
        }
    }

    private Vector3 GetHeightAdjustedPos(Vector3 originalPos)
    {
        return new Vector3(originalPos.x, spawnHeight, originalPos.z);
    }

    private void ClearAllInstances()
    {
        foreach (var pair in m_ActivePairs)
        {
            if (pair.instanceA != null) { if (Application.isPlaying) Destroy(pair.instanceA); else DestroyImmediate(pair.instanceA); }
            if (pair.instanceB != null) { if (Application.isPlaying) Destroy(pair.instanceB); else DestroyImmediate(pair.instanceB); }
        }
        m_ActivePairs.Clear();
    }
}

// ==================== 多路径核心解耦数据结构 ====================
[Serializable]
public class PathRoute
{
    [Header("路径识别别名")]
    public string routeName = "Route_01";

    [Header("该路径的起点 (控制子弹A发射和凌空翻滚)")]
    public Transform startPoint;

    [Header("该路径的终点 (控制命中和特效B黏随)")]
    public Transform endPoint;
}