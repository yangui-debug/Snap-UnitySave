using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

[ExecuteAlways]
public class TimelineHealthBarController : MonoBehaviour
{
    [Header("【1】UI 血条组件指定")]
    [Tooltip("拖入红血条 Image（注意：该 Image 的 Image Type 必须设置为 Filled）")]
    public Image healthBarImage;

    [Header("【2】检测的目标物体")]
    [Tooltip("Timeline 中用 Activation Track 控制出现/消失的那个目标物体")]
    public GameObject targetObject;

    [Header("【3】Timeline 控制器")]
    [Tooltip("把你场景里挂着 Playable Director 组件的那个 Timeline 核心物体拖进来")]
    public PlayableDirector timelineDirector;

    [Header("【4】物体总寿命 (彻底解决提前见底的关键)")]
    [Min(0.1f)]
    [Tooltip("【核心填空】请看一眼你的 Timeline 激活轨道，这个物体【总共会亮起多少秒】。比如绿块长度是 15 秒，这里就死死填 15。填对了这个，第一遍播放就绝对精准！")]
    public float targetObjectTotalDuration = 10f;

    [Header("【5】分段精准控血配置")]
    [Tooltip("最后高潮爆发高伤害的持续时间（单位：秒）。比如填 5，代表临消失前的最后 5 秒会快速掉血。")]
    public float finalStageDuration = 5.0f;

    [Range(0f, 100f)]
    [Tooltip("进入最后爆发期时，血条应该刚好剩多少血（百分比）。比如填 20，代表最后 5 秒开始的一瞬间，血量正好卡在 20%。")]
    public float finalStageStartHpPercent = 20.0f;

    // 内部高精状态机
    private float m_ActiveStartTime = -1f;
    private bool m_LastActiveState = false;

    void OnEnable()
    {
        ResetSystem();
    }

    void Update()
    {
        if (healthBarImage == null || targetObject == null || timelineDirector == null) return;

        bool isTargetActive = targetObject.activeInHierarchy;
        UpdateHealthBar(isTargetActive);
    }

    private void UpdateHealthBar(bool isTargetActive)
    {
        if (isTargetActive)
        {
            // 【状态 A：物体亮起中】

            // 突变检测：当物体亮起的第一帧（或鼠标刚拖进绿块）
            if (!m_LastActiveState)
            {
                m_ActiveStartTime = (float)timelineDirector.time;
                m_LastActiveState = true;
                healthBarImage.fillAmount = 1f;
                return;
            }

            // 计算当前这一发绿块已经安全流逝了多少秒
            float currentTime = (float)timelineDirector.time;
            float elapsed = currentTime - m_ActiveStartTime;

            // 提取面板上配置的血量分水岭
            float targetHpPivot = finalStageStartHpPercent / 100f;

            // 核心数学：计算普通阶段的持续时间
            // 比如你在面板填了总长 20 秒，最后 5 秒爆发。那普通期就是 20 - 5 = 15 秒。
            float normalStageDuration = targetObjectTotalDuration - finalStageDuration;

            // 防呆机制：如果总时间填错了，或者总时间比爆发时间还短
            if (normalStageDuration <= 0)
            {
                float progress = elapsed / targetObjectTotalDuration;
                healthBarImage.fillAmount = Mathf.Clamp01(1f - progress);
                return;
            }

            // ================== 【真·分段高精绝对映射】 ==================
            if (elapsed < normalStageDuration)
            {
                // 【阶段一：前半段普通期】
                // 时间从 0 走到 15 秒，血量极其精准、匀速地从 1.0 慢慢磨到 0.2
                float progress = elapsed / normalStageDuration;
                healthBarImage.fillAmount = Mathf.Lerp(1f, targetHpPivot, progress);
            }
            else
            {
                // 【阶段二：最后 5 秒决战爆发期】
                // 时间从 15 秒走到 20 秒，血量从 0.2 顺着你的高攻击演出，疯狂匀速暴跌到 0
                float elapsedInFinal = elapsed - normalStageDuration;
                float progress = elapsedInFinal / finalStageDuration;
                healthBarImage.fillAmount = Mathf.Lerp(targetHpPivot, 0f, progress);
            }
        }
        else
        {
            // 【状态 B：物体在 Timeline 里消失的一瞬间】
            // 只要绿块到头，物体变黑，血条“啪”地一下绝对强制归零 0%
            healthBarImage.fillAmount = 0f;
            m_LastActiveState = false;
        }
    }

    private void ResetSystem()
    {
        m_ActiveStartTime = -1f;
        m_LastActiveState = false;

        if (healthBarImage != null && targetObject != null)
        {
            healthBarImage.fillAmount = targetObject.activeInHierarchy ? 1f : 0f;
        }
    }
}