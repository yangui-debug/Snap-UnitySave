using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 给可复制的模型挂这个脚本；当遇到 NumberGate 时，会按数字复制自身
/// </summary>
[DisallowMultipleComponent]
public class AutoCloner : MonoBehaviour
{
    [Header("生成摆放")]
    public SpawnPattern pattern = SpawnPattern.Radial; // 摆放方式
    public float radius = 1.0f;                        // Radial 半径 / Line 间距
    public Vector3 offset = Vector3.zero;              // 相对原始位置的偏移
    public bool randomizeOrder = true;                 // 随机打乱角度/序号

    public enum SpawnPattern { Radial, ForwardLine, Stack, RandomInArea }

    [Header("RandomInArea - 随机区域（XZ 矩形）")]
    public Vector2 areaSize = new Vector2(5, 5);       // 区域宽度(X)与深度(Z)
    public bool areaUseLocalSpace = true;              // true=区域相对自身的 right/forward；false=世界坐标
    [Tooltip("判定“同一位置”的最小间距：小于该值则算重合")]
    public float minSeparation = 0.5f;

    [Tooltip("=ON 时：仍在区域内随机，但不堆叠；若落点太近会换点，最多尝试 maxRandomTries 次")]
    public bool noStacking = false;
    [Min(1)] public int maxRandomTries = 12;

    [Tooltip("=OFF 时：允许堆叠，高度步进如下两项控制")]
    public float stackStep = 0.5f;                     // 堆叠高度步进
    public bool autoStackStepFromCollider = true;      // 自动用模型高度作为堆叠步进

    [Header("Random Y Rotation（仅 RandomInArea）")]
    public bool randomYRotation = true;
    public Vector2 yRotationRange = new Vector2(0f, 360f); // 随机朝向范围（度）

    [Header("克隆参数")]
    public Transform parentForClones;                  // 生成到哪个父物体（为空则与原对象同层）
    public bool copyVelocityIfRigidbody = true;        // 原对象有刚体时，克隆继承速度
    public bool inheritScale = true;                   // 继承缩放
    public bool inheritRotation = true;                // 继承旋转（除非 RandomInArea 指定随机Y）

    [Header("连锁保护")]
    public bool disableClonerOnClones = true;          // 防止复制体继续复制
    [Min(0)] public int maxTotalClones = 0;            // 全局上限；0=不限制
    private static int _totalClonesAlive;

    [Header("一次触发的上限")]
    [Min(0)] public int maxPerEvent = 100;

    // 对外接口：由 NumberGate 调用
    public void CloneSelf(int extraCopies)
    {
        if (extraCopies <= 0) return;
        int count = Mathf.Min(extraCopies, maxPerEvent);

        // 全局上限检查
        if (maxTotalClones > 0)
        {
            int room = Mathf.Max(0, maxTotalClones - _totalClonesAlive);
            count = Mathf.Min(count, room);
            if (count <= 0) return;
        }

        // 计算位置
        var positions = CalculateSpawnPositions(count);

        for (int i = 0; i < count; i++)
        {
            var pos = positions[i] + offset;

            // 旋转
            Quaternion rot;
            if (pattern == SpawnPattern.RandomInArea && randomYRotation)
            {
                // 随机水平朝向（可在其基础上继承）
                float yaw = Random.Range(yRotationRange.x, yRotationRange.y);
                rot = Quaternion.Euler(0f, yaw, 0f) * (inheritRotation ? transform.rotation : Quaternion.identity);
            }
            else
            {
                rot = inheritRotation ? transform.rotation : Quaternion.identity;
            }

            var scale = inheritScale ? transform.localScale : Vector3.one;

            var clone = Instantiate(gameObject, pos, rot, parentForClones ? parentForClones : transform.parent);
            clone.transform.localScale = scale;

            // 禁用克隆体的 Cloner，避免连锁倍增
            if (disableClonerOnClones)
            {
                var c = clone.GetComponent<AutoCloner>();
                if (c) c.enabled = false;
            }

            // 继承刚体速度
            if (copyVelocityIfRigidbody)
            {
                var rbSrc = GetComponent<Rigidbody>();
                var rbDst = clone.GetComponent<Rigidbody>();
                if (rbSrc && rbDst)
                {
                    rbDst.velocity = rbSrc.velocity;
                    rbDst.angularVelocity = rbSrc.angularVelocity;
                }
            }

            _totalClonesAlive++;
            clone.AddComponent<CloneLifeHook>().onDestroyed += () => _totalClonesAlive--;
        }
    }

    // 计算摆放点
    private List<Vector3> CalculateSpawnPositions(int count)
    {
        var list = new List<Vector3>(count);
        Vector3 origin = transform.position;

        switch (pattern)
        {
            case SpawnPattern.Radial:
            {
                float step = 360f / count;
                int startIndex = randomizeOrder ? Random.Range(0, count) : 0;
                for (int i = 0; i < count; i++)
                {
                    float ang = (startIndex + i) * step * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    list.Add(origin + dir * radius);
                }
                break;
            }

            case SpawnPattern.ForwardLine:
            {
                Vector3 fwd = transform.forward;
                float half = (count - 1) * 0.5f;
                for (int i = 0; i < count; i++)
                {
                    float t = i - half;
                    list.Add(origin + fwd * t * radius);
                }
                break;
            }

            case SpawnPattern.Stack:
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 jitter = new Vector3(
                        Random.Range(-0.15f, 0.15f),
                        Random.Range(0f, 0.05f),
                        Random.Range(-0.15f, 0.15f));
                    list.Add(origin + jitter);
                }
                break;
            }

            case SpawnPattern.RandomInArea:
            {
                // 基础高度：与“父物体”一致；若未指定父物体，则用自身高度
                float baseY = (parentForClones ? parentForClones.position.y : transform.position.y);

                // 确定区域轴向
                Vector3 right = areaUseLocalSpace ? transform.right : Vector3.right;
                Vector3 forward = areaUseLocalSpace ? transform.forward : Vector3.forward;

                // 叠放步进：若自动，就估计当前对象高度
                float stepY = autoStackStepFromCollider ? EstimateObjectHeight() : stackStep;
                stepY = Mathf.Max(0.0001f, stepY);

                // 用简单双循环做距离判定（数量通常不大）
                var accepted = new List<Vector3>(count);

                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = Vector3.zero;
                    bool placed = false;

                    // 多次尝试找“不过近”的点
                    int tries = Mathf.Max(1, maxRandomTries);
                    for (int t = 0; t < tries; t++)
                    {
                        float rx = Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
                        float rz = Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);
                        Vector3 flat = origin + right * rx + forward * rz;

                        // 计算应叠的层数（当 noStacking=false 时）
                        int level = 0;

                        if (!noStacking)
                        {
                            for (int j = 0; j < accepted.Count; j++)
                            {
                                Vector3 p = accepted[j];
                                // 只看水平距离
                                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(flat.x, flat.z)) < minSeparation)
                                {
                                    // 与已有点太近 -> 在其上方叠一层
                                    // 估算该位置已有多少层：通过已有点Y与baseY的差除以stepY
                                    int existingLevel = Mathf.RoundToInt((p.y - baseY) / stepY);
                                    level = Mathf.Max(level, existingLevel + 1);
                                }
                            }
                        }
                        else
                        {
                            // 不堆叠：若太近就换点
                            bool tooClose = false;
                            for (int j = 0; j < accepted.Count; j++)
                            {
                                Vector3 p = accepted[j];
                                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(flat.x, flat.z)) < minSeparation)
                                {
                                    tooClose = true; break;
                                }
                            }
                            if (tooClose) continue; // 重试
                        }

                        pos = new Vector3(flat.x, baseY + level * stepY, flat.z);
                        placed = true;
                        break;
                    }

                    // 尝试用尽仍放不下：退化为最后一个随机点（不堆叠时仍用 baseY）
                    if (!placed)
                    {
                        float rx = Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
                        float rz = Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);
                        Vector3 flat = origin + right * rx + forward * rz;
                        pos = new Vector3(flat.x, baseY, flat.z);
                    }

                    accepted.Add(pos);
                }

                list.AddRange(accepted);
                break;
            }
        }

        return list;
    }

    // 估算当前对象的高度（优先 Collider，其次 Renderer）
    private float EstimateObjectHeight()
    {
        float h = 0f;

        // 先看 Collider
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
            h = Mathf.Max(h, c.bounds.size.y);

        // 没有 Collider 就看 Renderer
        if (h <= 0f)
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
                h = Mathf.Max(h, r.bounds.size.y);
        }

        // 兜底
        if (h <= 0f) h = 0.5f;
        return h;
    }

    // 用于在克隆销毁时回调，维护全局计数
    private class CloneLifeHook : MonoBehaviour
    {
        public System.Action onDestroyed;
        private void OnDestroy() => onDestroyed?.Invoke();
    }
}
