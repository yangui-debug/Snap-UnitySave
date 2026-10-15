using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TimelinePathSpawner : MonoBehaviour
{
    [Header("【1】路径节点 (由Timeline控制位移和旋转)")]
    public Transform cube1;
    public Transform cube2;

    [Header("【2】预制体设置")]
    public GameObject prefabA;
    public GameObject prefabB;

    [Header("【等比例大小控制】")]
    public float prefabAScaleMultiplier = 1.0f;
    public float prefabBScaleMultiplier = 1.0f;

    [Header("【3】移动与生命周期控制")]
    public float moveSpeed = 5f;
    public float prefabBDestroyDelay = 1.5f;
    public float loopInterval = 4f;

    [Header("【4】延迟控制")]
    public float startDelay = 3f;

    [Header("【5】路径高度与朝向修正")]
    [Tooltip("如果是靠路径自身旋转控制，建议高度设为 0，否则会脱离旋转中心")]
    public float spawnHeight = 0f;

    [Tooltip("如果模型默认方向不对，可以在这里进行局部旋转修正")]
    public Vector3 fixRotationOffset = Vector3.zero;

    // 内部私有变量
    private GameObject m_InstanceA;
    private GameObject m_InstanceB;

    private float m_IntervalTimer = 0f;
    private float m_BTimer = 0f;
    private float m_DelayTimer = 0f;

    private bool m_IsWaitingForNextLoop = false;
    private bool m_IsBActive = false;
    private bool m_HasStarted = false;

    void OnEnable()
    {
        ResetSystem();
    }

    void OnDisable()
    {
        ClearInstances();
    }

    void LateUpdate()
    {
        if (cube1 == null || cube2 == null || prefabA == null || prefabB == null) return;

        if (!Application.isPlaying)
        {
            UpdateEditorPreview();
            return;
        }

        // 检查是否还在“首次生成延迟”的等待期内
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

        // 逻辑 A：控制预制体 B 的“定时消失”以及“跟随 Timeline 的 Cube2”
        if (m_IsBActive && m_InstanceB != null)
        {
            m_InstanceB.transform.position = GetHeightAdjustedPos(cube2.position);
            // B 的旋转直接死死同步 Cube2 的实时旋转
            m_InstanceB.transform.rotation = cube2.rotation * Quaternion.Euler(fixRotationOffset);

            m_BTimer += Time.deltaTime;
            if (m_BTimer >= prefabBDestroyDelay)
            {
                m_InstanceB.SetActive(false);
                m_IsBActive = false;
            }
        }

        // 状态 1：正在整个大循环的等待期中
        if (m_IsWaitingForNextLoop)
        {
            m_IntervalTimer += Time.deltaTime;
            if (m_IntervalTimer >= loopInterval)
            {
                SpawnNewRound();
            }
            return;
        }

        // 状态 2：预制体 A 正在路径上移动
        if (m_InstanceA != null)
        {
            Vector3 targetPos = GetHeightAdjustedPos(cube2.position);

            // 1. 移动位置
            m_InstanceA.transform.position = Vector3.MoveTowards(m_InstanceA.transform.position, targetPos, moveSpeed * Time.deltaTime);

            // 2. 【核心修改】计算实时朝向：
            // 让它的 Z 轴（前方）对准 Cube2，但是它的 Up 轴（上方）直接借用 Cube1 的上方！
            // 这样只要 Cube1 发生了任何倾斜、翻滚旋转，A 就会完美跟着一起翻滚！
            Vector3 moveDirection = targetPos - m_InstanceA.transform.position;
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                // 使用 cube1.up 代替原先硬编码的 Vector3.up
                Quaternion pathRotation = Quaternion.LookRotation(moveDirection, cube1.up);
                m_InstanceA.transform.rotation = pathRotation * Quaternion.Euler(fixRotationOffset);
            }

            if (Vector3.Distance(m_InstanceA.transform.position, targetPos) < 0.05f)
            {
                TriggerCollisionEvent();
            }
        }
    }

    private void ResetSystem()
    {
        ClearInstances();
        m_IntervalTimer = 0f;
        m_BTimer = 0f;
        m_DelayTimer = 0f;
        m_IsWaitingForNextLoop = false;
        m_IsBActive = false;
        m_HasStarted = false;
    }

    private void SpawnNewRound()
    {
        m_IsWaitingForNextLoop = false;
        m_IntervalTimer = 0f;

        if (m_InstanceB != null) m_InstanceB.SetActive(false);
        m_IsBActive = false;

        Vector3 spawnPos = GetHeightAdjustedPos(cube1.position);
        if (m_InstanceA != null) Destroy(m_InstanceA);

        m_InstanceA = Instantiate(prefabA, spawnPos, cube1.rotation, this.transform);
        m_InstanceA.transform.localScale = Vector3.one * prefabAScaleMultiplier;

        // 初始帧朝向对齐
        Vector3 targetPos = GetHeightAdjustedPos(cube2.position);
        Vector3 moveDirection = targetPos - spawnPos;
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            m_InstanceA.transform.rotation = Quaternion.LookRotation(moveDirection, cube1.up) * Quaternion.Euler(fixRotationOffset);
        }
    }

    private void TriggerCollisionEvent()
    {
        if (m_InstanceA != null)
        {
            Destroy(m_InstanceA);
            m_InstanceA = null;
        }

        if (m_InstanceB == null)
        {
            Vector3 targetPos = GetHeightAdjustedPos(cube2.position);
            m_InstanceB = Instantiate(prefabB, targetPos, cube2.rotation * Quaternion.Euler(fixRotationOffset), this.transform);
        }

        m_InstanceB.transform.position = GetHeightAdjustedPos(cube2.position);
        m_InstanceB.SetActive(true);
        m_InstanceB.transform.localScale = Vector3.one * prefabBScaleMultiplier;

        m_IsBActive = true;
        m_BTimer = 0f;

        m_IsWaitingForNextLoop = true;
        m_IntervalTimer = 0f;
    }

    private void UpdateEditorPreview()
    {
        if (cube1 == null || cube2 == null) return;

        Vector3 targetPos = GetHeightAdjustedPos(cube2.position);
        foreach (Transform child in transform)
        {
            if (child.name.Contains(prefabB.name))
            {
                child.position = targetPos;
                // 编辑器预览也让它实时吃 Cube2 的旋转
                child.rotation = cube2.rotation * Quaternion.Euler(fixRotationOffset);
                child.localScale = Vector3.one * prefabBScaleMultiplier;
            }
        }
    }

    private Vector3 GetHeightAdjustedPos(Vector3 originalPos)
    {
        return new Vector3(originalPos.x, spawnHeight, originalPos.z);
    }

    private void ClearInstances()
    {
        if (m_InstanceA != null) { if (Application.isPlaying) Destroy(m_InstanceA); else DestroyImmediate(m_InstanceA); }
        if (m_InstanceB != null) { if (Application.isPlaying) Destroy(m_InstanceB); else DestroyImmediate(m_InstanceB); }
        m_InstanceA = null;
        m_InstanceB = null;
    }
}