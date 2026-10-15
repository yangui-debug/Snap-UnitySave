using System.Collections;
using Cinemachine;
using UnityEngine;

/// <summary>
/// 统一管理普通命中震屏、Hit Stop 与伤害飘字。
/// 适配 Unity 2022.3 与 Cinemachine 2.x。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Combat/Hit Feel Manager")]
public class HitFeelManager : MonoBehaviour
{
    public static HitFeelManager Instance;

    [Header("1. Cinemachine 震屏来源")]
    [Tooltip("用于发出震屏信号的 Cinemachine Impulse Source。推荐把 Impulse Source 与本脚本放在同一个虚拟相机物体上并留空，本脚本会自动获取；也可以手动拖入其他物体上的 Source。")]
    public CinemachineImpulseSource impulseSource;

    [Header("2. 普通受击震屏")]
    [Tooltip("普通、未致死攻击的震屏强度。90°正交镜头、Orthographic Size 约 35 时推荐 0.20 ~ 0.45；默认 0.30。")]
    [Min(0f)] public float shakeForce = 0.3f;

    [Header("3. 卡肉顿帧（Hit Stop）")]
    [Tooltip("是否在武器命中时短暂降低时间速度。推荐勾选；死亡时只触发这次顿帧，不会同时触发普通受击震屏。")]
    public bool enableHitStop = true;

    [Tooltip("顿帧期间的 Time.timeScale。推荐 0.08 ~ 0.18；默认 0.10。")]
    [Range(0f, 1f)] public float hitStopTimeScale = 0.1f;

    [Tooltip("顿帧持续的真实时间。Clash Mini 风格推荐 0.035 ~ 0.060 秒；默认 0.05。")]
    [Min(0f)] public float hitStopDuration = 0.05f;

    [Header("4. 伤害飘字")]
    [Tooltip("伤害数字预制体，需要挂载 DamageText。可以留空。")]
    public GameObject damageTextPrefab;

    [Tooltip("飘字使用的 Canvas 或父节点。可以留空，此时飘字生成在场景根节点。")]
    public Transform floatingTextCanvas;

    private bool isHitStopping;
    private float timeScaleBeforeHitStop = 1f;
    private float appliedHitStopScale = 1f;
    private bool warnedMissingImpulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("HitFeelManager：场景中存在多个实例，将使用最后启用的实例。建议只保留一个。", this);
        }

        Instance = this;
        ResolveImpulseSource();
    }

    private void OnDestroy()
    {
        RestoreTimeScaleIfNeeded();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
    }

    /// <summary>
    /// 普通、未致死攻击：播放普通震屏并触发 Hit Stop。
    /// </summary>
    public void PlayHitFeedback()
    {
        GenerateImpulse(shakeForce);
        PlayHitStopOnly();
    }

    /// <summary>
    /// 致命攻击命中的第一拍：只做短暂停顿，震屏留到死亡替身真正起飞时触发。
    /// </summary>
    public void PlayHitStopOnly()
    {
        if (enableHitStop && !isHitStopping && isActiveAndEnabled)
        {
            StartCoroutine(HitStopRoutine());
        }
    }

    /// <summary>
    /// 在指定世界位置附近生成伤害飘字。
    /// </summary>
    public void ShowDamageText(Vector3 position, int damage, bool isCrit = false)
    {
        if (damageTextPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = position + new Vector3(
            Random.Range(-0.5f, 0.5f),
            1.5f,
            Random.Range(-0.5f, 0.5f));
        GameObject textObject = Instantiate(
            damageTextPrefab,
            spawnPosition,
            Quaternion.identity,
            floatingTextCanvas);

        DamageText damageText = textObject.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Setup(damage, isCrit);
        }
    }

    private void GenerateImpulse(float force)
    {
        if (force <= 0f)
        {
            return;
        }

        ResolveImpulseSource();
        if (impulseSource == null)
        {
            if (!warnedMissingImpulseSource)
            {
                warnedMissingImpulseSource = true;
                Debug.LogWarning(
                    "HitFeelManager：找不到 CinemachineImpulseSource。请在本物体上添加该组件；虚拟相机还需要添加 CinemachineImpulseListener Extension。",
                    this);
            }
            return;
        }

        impulseSource.GenerateImpulse(force);
    }

    private void ResolveImpulseSource()
    {
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }
    }

    private IEnumerator HitStopRoutine()
    {
        isHitStopping = true;
        timeScaleBeforeHitStop = Time.timeScale;
        // 只允许进一步减速，不能把 Timeline 慢动作或暂停状态意外加速。
        appliedHitStopScale = Mathf.Min(timeScaleBeforeHitStop, Mathf.Clamp01(hitStopTimeScale));
        Time.timeScale = appliedHitStopScale;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, hitStopDuration));

        RestoreTimeScaleIfNeeded();
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!isHitStopping)
        {
            return;
        }

        // 如果 Timeline 或其他系统在顿帧期间主动修改了 timeScale，就不覆盖它的新值。
        if (Mathf.Approximately(Time.timeScale, appliedHitStopScale))
        {
            Time.timeScale = timeScaleBeforeHitStop;
        }

        isHitStopping = false;
    }

    private void OnValidate()
    {
        shakeForce = Mathf.Max(0f, shakeForce);
        hitStopTimeScale = Mathf.Clamp01(hitStopTimeScale);
        hitStopDuration = Mathf.Max(0f, hitStopDuration);
    }
}
