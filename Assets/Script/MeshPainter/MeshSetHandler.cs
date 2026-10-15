using UnityEngine;
using System.Collections;

public class MeshSetHandler : MonoBehaviour
{
    // 扩展模式：增加 ComboAndParticle (白膜+特效+发光)
    public enum FeedbackMode { None, VisualCombo, ParticleEffect, ComboAndParticle }

    [Header("【1】替换内容开关")]
    public bool shouldReplaceMesh = true;     
    public bool shouldReplaceMaterial = true; 

    [Header("【2】资源配置")]
    public Mesh[] meshSets;             
    public Material[] materialSets;     

    [Header("【3】反馈模式选择")]
    public FeedbackMode feedbackMode = FeedbackMode.VisualCombo;

    [Header("【4】视觉表现设置")]
    public Material whiteMaskMaterial; 
    public float maskDuration = 0.08f; 
    [Space(10)]
    public Color glowColor = Color.white; 
    [Range(0, 50f)] public float maxIntensity = 25f; 
    public float glowDuration = 0.4f; 

    [Header("【5】粒子特效")]
    public GameObject effectPrefab;
    public Vector3 effectOffset = Vector3.up; // 特效偏移

    private Renderer m_Renderer;
    private MeshFilter m_MeshFilter;
    private int m_CurrentIndex = -1;
    private MeshTileHelper m_Helper;
    private bool m_IsSwapping = false;

    void Awake()
    {
        m_Renderer = GetComponentInChildren<Renderer>();
        m_MeshFilter = GetComponentInChildren<MeshFilter>();
        m_Helper = GetComponent<MeshTileHelper>();
        if (m_Helper == null) m_Helper = gameObject.AddComponent<MeshTileHelper>();
        
        if (m_Renderer != null && m_Renderer.sharedMaterial != null)
            m_Renderer.sharedMaterial.EnableKeyword("_EMISSION");
    }

    public void SwitchToSet(int index)
    {
        // 安全检查
        if (!shouldReplaceMesh && !shouldReplaceMaterial) return;
        if (shouldReplaceMesh && (meshSets == null || index < 0 || index >= meshSets.Length)) return;
        if (shouldReplaceMaterial && (materialSets == null || index < 0 || index >= materialSets.Length)) return;
        
        // 关键：只有当目标索引和当前不同时才触发，防止重复触发
        if (m_CurrentIndex == index || m_IsSwapping) return;

        // 根据模式进入不同的逻辑流程
        switch (feedbackMode)
        {
            case FeedbackMode.None:
                ApplyChanges(index);
                break;
            case FeedbackMode.VisualCombo:
            case FeedbackMode.ComboAndParticle:
                // 开启协程处理带有时间轴的反馈
                StartCoroutine(TransitionSequence(index));
                break;
            case FeedbackMode.ParticleEffect:
                // 仅特效模式：立即触发特效并换装
                SpawnParticle();
                ApplyChanges(index);
                break;
        }
    }

    // 核心变换协程：控制“变色时刻”和“特效时刻”
    private IEnumerator TransitionSequence(int newIndex)
    {
        m_IsSwapping = true;
        Material originalMaterial = m_Renderer.sharedMaterial;

        // --- 阶段 A: 前奏 (变白膜) ---
        if (whiteMaskMaterial != null)
            m_Renderer.sharedMaterial = whiteMaskMaterial;

        // 此时换掉网格（在白膜包裹下换，不会闪烁突兀）
        if (shouldReplaceMesh && m_MeshFilter != null)
            m_MeshFilter.sharedMesh = meshSets[newIndex];

        // 等待白膜停留时间
        yield return new WaitForSecondsRealtime(maskDuration);

        // --- 阶段 B: 变色时刻 (材质正式切换) ---
        
        // 1. 如果是“全能模式”，在这个瞬间炸开粒子特效
        if (feedbackMode == FeedbackMode.ComboAndParticle)
        {
            SpawnParticle();
        }

        // 2. 应用最终材质
        Material targetMat = originalMaterial;
        if (shouldReplaceMaterial && materialSets[newIndex] != null)
        {
            targetMat = materialSets[newIndex];
        }
        m_Renderer.sharedMaterial = targetMat;
        m_CurrentIndex = newIndex;

        // 3. 触发发光渐变表现
        m_Helper.StartEmissionRamp(glowColor, maxIntensity, glowDuration);

        m_IsSwapping = false;
    }

    // 纯粹的资源应用逻辑
    private void ApplyChanges(int index)
    {
        if (shouldReplaceMesh && m_MeshFilter != null)
            m_MeshFilter.sharedMesh = meshSets[index];

        if (shouldReplaceMaterial && m_Renderer != null && materialSets[index] != null)
            m_Renderer.sharedMaterial = materialSets[index];
        
        m_CurrentIndex = index;
    }

    // 统一的粒子生成方法
    private void SpawnParticle()
    {
        if (effectPrefab != null)
        {
            GameObject fx = Instantiate(effectPrefab, transform.position + effectOffset, Quaternion.identity);
            Destroy(fx, 2f); // 2秒后自动清理
        }
    }
}