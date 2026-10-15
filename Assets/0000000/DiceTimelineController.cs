using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceTimelineController : MonoBehaviour
{
    [System.Serializable]
    public struct DiceSchedule
    {
        [Tooltip("触发摇骰子的时间点（单位：秒）")]
        public float triggerTime;

        [Range(1, 6)]
        [Tooltip("这次要摇出的精准点数 (1 ~ 6)")]
        public int targetNumber;
    }

    [Header("【1】时间与点数排班表")]
    [Tooltip("可以在这里添加多个时间点和对应的点数")]
    public List<DiceSchedule> rollSchedules = new List<DiceSchedule>();

    [Header("【2】旋转设置")]
    [Tooltip("单次投掷动画持续的时间（秒），注意：请确保触发间隔大于这个时间")]
    public float rollDuration = 2.0f;
    public float maxRotationSpeed = 1000f;

    [Header("停止时的回弹效果")]
    public float bounceFrequency = 15f;
    public float bounceAmplitude = 0.1f;

    [Header("【3】基础视觉倾斜姿态")]
    [Tooltip("骰子最终停下来时的整体主倾斜角")]
    public Vector3 baseTargetAngles = new Vector3(20f, 45f, 20f);

    [Header("【4】骰子六个面的局部旋转校准 (核心)")]
    [Tooltip("当骰子1点朝上时，它自身的局部旋转（通常默认是0,0,0）")]
    public Vector3 face1Rotation = new Vector3(0, 0, 0);
    public Vector3 face2Rotation = new Vector3(0, 0, 90);
    public Vector3 face3Rotation = new Vector3(90, 0, 0);
    public Vector3 face4Rotation = new Vector3(-90, 0, 0);
    public Vector3 face5Rotation = new Vector3(0, 0, -90);
    public Vector3 face6Rotation = new Vector3(180, 0, 0);

    private float m_Timer = 0f;
    private HashSet<int> m_TriggeredIndices = new HashSet<int>();
    private Coroutine m_RollCoroutine;

    // 当 Timeline 激活或反复显示时，重置时间计器和触发记录
    void OnEnable()
    {
        m_Timer = 0f;
        m_TriggeredIndices.Clear();
        if (m_RollCoroutine != null)
        {
            StopCoroutine(m_RollCoroutine);
            m_RollCoroutine = null;
        }
    }

    void Update()
    {
        // 累加游戏运行（或 Timeline 播放）的时间
        m_Timer += Time.deltaTime;

        // 检查排班表里有没有时间到了的
        for (int i = 0; i < rollSchedules.Count; i++)
        {
            // 如果时间到了，且之前没触发过
            if (m_Timer >= rollSchedules[i].triggerTime && !m_TriggeredIndices.Contains(i))
            {
                m_TriggeredIndices.Add(i); // 标记为已触发

                // 触发这一把的精准控点投掷
                TriggerTargetRoll(rollSchedules[i].targetNumber);
            }
        }
    }

    private void TriggerTargetRoll(int number)
    {
        if (m_RollCoroutine != null) StopCoroutine(m_RollCoroutine);

        // 1. 根据点数，获取对应的模型局部旋转
        Vector3 localFaceAngle = GetRotationForNumber(number);
        Quaternion faceOffset = Quaternion.Euler(localFaceAngle);

        // 2. 结合你指定的固定视觉倾斜角，算出最终在世界坐标下的角度
        Quaternion baseRotation = Quaternion.Euler(baseTargetAngles);
        Quaternion targetRotation = baseRotation * faceOffset;

        // 3. 随机一个乱转轴，让空中过程自然
        Vector3 randomAxis = Random.insideUnitSphere.normalized;

        // 4. 冲！
        m_RollCoroutine = StartCoroutine(RollSequence(targetRotation, randomAxis));
    }

    // 根据点数返回对应的旋转角（如果你的模型点数不对，直接在面板上改对应的 Face 属性即可）
    private Vector3 GetRotationForNumber(int number)
    {
        switch (number)
        {
            case 1: return face1Rotation;
            case 2: return face2Rotation;
            case 3: return face3Rotation;
            case 4: return face4Rotation;
            case 5: return face5Rotation;
            case 6: return face6Rotation;
            default: return Vector3.zero;
        }
    }

    private IEnumerator RollSequence(Quaternion targetRot, Vector3 axis)
    {
        float elapsed = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rollDuration;

            // 前半段疯狂旋转
            float speedMultiplier = Mathf.Pow(1f - t, 3f);
            float currentSpeed = maxRotationSpeed * speedMultiplier;
            transform.Rotate(axis, currentSpeed * Time.deltaTime, Space.World);

            // 后半段向目标靠拢减速
            if (t > 0.5f)
            {
                float blendT = (t - 0.5f) / 0.5f;
                float bounce = Mathf.Sin(blendT * bounceFrequency) * bounceAmplitude * (1f - blendT);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, blendT) * Quaternion.Euler(bounce, bounce, bounce);
            }

            yield return null;
        }

        transform.rotation = targetRot;
        m_RollCoroutine = null;
    }
}