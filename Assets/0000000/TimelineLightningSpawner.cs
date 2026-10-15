using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TimelineLightningSpawner : MonoBehaviour
{
    [Header("【1】路径节点配置")]
    [Tooltip("【重要】把要在地上挨个挨个劈雷的 Cube 路径点按顺序拖进这个列表里。落雷会严格按照 0、1、2... 的顺序砸下来。")]
    public List<Transform> pathCubes = new List<Transform>();

    [Header("【2】粒子特效预制体")]
    [Tooltip("预制体 A：从天而降的闪电束。")]
    public GameObject prefabLightningA;

    [Tooltip("预制体 B：闪电触地后在地面炸开的特效（比如爆炸火光、地面龟裂）。")]
    public GameObject prefabExplosionB;

    [Header("【新增：特效存在时间控制】")]
    [Range(0.1f, 10f)]
    [Tooltip("【防呆机制】预制体 A (闪电) 在空中的最大允许存在时间（秒）。如果闪电因为速度太慢超过这个时间还没落地，会被强行拔除并触发地面爆炸，防止卡死。")]
    public float maxLightningALifetime = 2.0f;

    [Range(0.1f, 20f)]
    [Tooltip("预制体 B (地面炸裂特效) 产生后，在场景里维持多少秒才会彻底销毁。如果你的特效带有长久的地面焦黑或残存火焰，可以把这个时间调大。")]
    public float explosionBLifetime = 2.0f;

    [Header("【3】落雷动画核心控制")]
    [Range(10f, 300f)]
    [Tooltip("闪电（预制体A）从天空往下插的速度。数值越大，雷落得越快。")]
    public float lightningSpeed = 50f;

    [Range(2f, 50f)]
    [Tooltip("闪电出生的‘天空高度’（单位：米）。由于你是俯视角，强烈建议调小一点（比如 5 到 10 米）。")]
    public float skyHeight = 10f;

    [Range(0.05f, 5f)]
    [Tooltip("【连环雷节奏感控制】第一发雷砸完、地面特效B炸开的那一瞬间，需要等待多少秒才开始在下一个 Cube 的头顶刷第二发雷。")]
    public float strikeInterval = 0.3f;

    [Header("【4】等比例大小调节")]
    [Tooltip("预制体 A (闪电束) 整体的等比例放大缩小倍数。")]
    public float scaleA = 1f;

    [Tooltip("预制体 B (地面炸裂) 整体的等比例放大缩小倍数。")]
    public float scaleB = 1f;

    [Header("【5】Timeline 延迟控制")]
    [Tooltip("当脚本被 Timeline 激活（或者亮起）之后，需要先在原地等多少秒，才正式刷出第一发雷。")]
    public float startDelay = 2.0f;

    // 内部私有变量
    private float m_Timer = 0f;
    private float m_DelayTimer = 0f;
    private float m_LightningLifeTimer = 0f; // 针对 A 的超时计算器
    private int m_CurrentCubeIndex = 0;

    private GameObject m_ActiveLightningA;
    private Vector3 m_CurrentTargetGroundPos;

    private bool m_IsWaitingForNextStrike = false;
    private bool m_AllStrikesFinished = false;
    private bool m_HasDelayFinished = false;

    void OnEnable()
    {
        ResetSystem();
    }

    void OnDisable()
    {
        ClearActiveInstances();
    }

    void LateUpdate()
    {
        if (pathCubes == null || pathCubes.Count == 0 || prefabLightningA == null || prefabExplosionB == null) return;
        if (!Application.isPlaying) return;
        if (m_AllStrikesFinished) return;

        // 【延迟逻辑】检查是否还在“首次生成延迟”的等待期内
        if (!m_HasDelayFinished)
        {
            m_DelayTimer += Time.deltaTime;
            if (m_DelayTimer >= startDelay)
            {
                m_HasDelayFinished = true;
                SpawnLightningA();
            }
            return;
        }

        // ====== 状态 1：正在等待下一发雷的间隔时间 ======
        if (m_IsWaitingForNextStrike)
        {
            m_Timer += Time.deltaTime;
            if (m_Timer >= strikeInterval)
            {
                m_IsWaitingForNextStrike = false;
                m_Timer = 0f;

                m_CurrentCubeIndex++;
                if (m_CurrentCubeIndex >= pathCubes.Count)
                {
                    m_AllStrikesFinished = true;
                    return;
                }

                SpawnLightningA();
            }
            return;
        }

        // ====== 状态 2：当前这发闪电 A 正在从天而降 ======
        if (m_ActiveLightningA != null)
        {
            if (pathCubes[m_CurrentCubeIndex] != null)
            {
                m_CurrentTargetGroundPos = pathCubes[m_CurrentCubeIndex].position;
            }

            // 往下俯冲
            m_ActiveLightningA.transform.position = Vector3.MoveTowards(
                m_ActiveLightningA.transform.position,
                m_CurrentTargetGroundPos,
                lightningSpeed * Time.deltaTime
            );

            // 【核心修改】累加 A 在空中的存在时间，防止卡死
            m_LightningLifeTimer += Time.deltaTime;

            // 检查雷电是否已经“触地” 或者 已经达到了最大允许存在时间
            if (Vector3.Distance(m_ActiveLightningA.transform.position, m_CurrentTargetGroundPos) < 0.05f || m_LightningLifeTimer >= maxLightningALifetime)
            {
                TriggerGroundImpact();
            }
        }
    }

    private void ResetSystem()
    {
        ClearActiveInstances();
        m_Timer = 0f;
        m_DelayTimer = 0f;
        m_LightningLifeTimer = 0f;
        m_CurrentCubeIndex = 0;

        m_IsWaitingForNextStrike = false;
        m_AllStrikesFinished = false;
        m_HasDelayFinished = false;
    }

    private void SpawnLightningA()
    {
        if (pathCubes[m_CurrentCubeIndex] == null) return;

        m_CurrentTargetGroundPos = pathCubes[m_CurrentCubeIndex].position;
        Vector3 skySpawnPos = m_CurrentTargetGroundPos + Vector3.up * skyHeight;

        m_ActiveLightningA = Instantiate(prefabLightningA, skySpawnPos, prefabLightningA.transform.rotation, this.transform);
        m_ActiveLightningA.transform.localScale = Vector3.one * scaleA;
        m_LightningLifeTimer = 0f; // 重置 A 的寿命计算器

        ParticleSystem ps = m_ActiveLightningA.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();
        }
    }

    private void TriggerGroundImpact()
    {
        if (m_ActiveLightningA != null)
        {
            Destroy(m_ActiveLightningA);
            m_ActiveLightningA = null;
        }

        Quaternion impactRot = pathCubes[m_CurrentCubeIndex] != null ? pathCubes[m_CurrentCubeIndex].rotation : Quaternion.identity;
        GameObject fxB = Instantiate(prefabExplosionB, m_CurrentTargetGroundPos, impactRot, this.transform);
        fxB.transform.localScale = Vector3.one * scaleB;

        // 【核心修改】使用你在面板配置的 explosionBLifetime 来控制 B 的消失时间
        Destroy(fxB, explosionBLifetime);

        m_IsWaitingForNextStrike = true;
        m_Timer = 0f;
    }

    private void ClearActiveInstances()
    {
        if (m_ActiveLightningA != null)
        {
            if (Application.isPlaying) Destroy(m_ActiveLightningA);
            else DestroyImmediate(m_ActiveLightningA);
        }
        m_ActiveLightningA = null;
    }
}