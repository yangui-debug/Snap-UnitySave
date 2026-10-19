using System;
using UnityEngine;
/// <summary>
/// 无阵营、无战斗决策的碰撞位移工具。只裁剪调用者提交的速度，不自行更新 Transform。
/// 胶囊扫掠防高速穿透，落点真实碰撞检测防继续挤入；允许沿分离方向退出既有重叠。
/// 地板承托、触发器、自身层级、已死亡单位不阻断水平移动；动态扩容避免密集战场漏检。
/// </summary>
public sealed class MeleeBodyMotor
{
    public Transform MotionRoot { get; private set; }
    public float Radius { get; private set; }
    private Collider ownCollider;
    private float skin;
    private LayerMask layers;
    private RaycastHit[] sweepHits = new RaycastHit[16];
    private Collider[] overlapHits = new Collider[16];
    public MeleeBodyMotor(Transform root, Collider collider, float minimumRadius, float collisionSkin, LayerMask blockingLayers)
    {
        MotionRoot = root; ownCollider = collider;
        UpdateSettings(minimumRadius, collisionSkin, blockingLayers);
    }
    /// <summary>支持播放中调参；复用检测缓冲，不因设置调整逐帧分配数组。</summary>
    public void UpdateSettings(float minimumRadius, float collisionSkin, LayerMask blockingLayers)
    {
        skin = Mathf.Max(0f, collisionSkin); layers = blockingLayers;
        Radius = Mathf.Max(.05f, minimumRadius);
        if (ownCollider != null) Radius = Mathf.Max(Radius, Mathf.Min(ownCollider.bounds.extents.x, ownCollider.bounds.extents.z));
    }
    /// <summary>速度单位为世界单位/秒；返回碰撞允许的速度，不写刚体，不改变重力。</summary>
    public Vector3 SafeVelocity(Vector3 velocity)
    {
        Vector3 step = velocity * Time.fixedDeltaTime;
        float distance = step.magnitude;
        if (distance <= 0.000001f) {  return Vector3.zero; }
        Vector3 direction = step / distance;
        Bounds bounds = ownCollider != null ? ownCollider.bounds : new Bounds(MotionRoot.position + Vector3.up, new Vector3(Radius * 2f, 1.8f, Radius * 2f));
        float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) - skin);
        float height = Mathf.Max(0f, bounds.extents.y - radius);
        Vector3 a = bounds.center + Vector3.up * height;
        Vector3 b = bounds.center - Vector3.up * height;
        int count;
        do
        {
            count = Physics.CapsuleCastNonAlloc(a, b, radius, direction, sweepHits, distance + skin, layers, QueryTriggerInteraction.Ignore);
            if (count < sweepHits.Length) break;
            Array.Resize(ref sweepHits, sweepHits.Length * 2); // 不截断密集兵群结果。
        } while (true);
        for (int i = 0; i < count; i++)
            if (IsBlocker(sweepHits[i].collider) && !IsSupportingContact(sweepHits[i].collider, sweepHits[i].normal, direction) &&
                !IsEscapingOverlap(sweepHits[i].collider, direction))
            {
                float allowed = Mathf.Max(0f, sweepHits[i].distance - skin);
                
                distance = Mathf.Min(distance, allowed);
            }
        // Cast 不报告起点已重叠的 Collider；额外检查落点，允许从重叠中脱离，但禁止越挤越深。
        Vector3 limitedStep = direction * distance;
        do
        {
            count = Physics.OverlapCapsuleNonAlloc(a + limitedStep, b + limitedStep, radius, overlapHits, layers, QueryTriggerInteraction.Ignore);
            if (count < overlapHits.Length) break;
            Array.Resize(ref overlapHits, overlapHits.Length * 2);
        } while (true);
        for (int i = 0; i < count; i++)
        {
            Collider other = overlapHits[i];
            if (!IsBlocker(other)) continue;
            if (ownCollider != null)
            {
                // 包围胶囊只负责广相检测，最终用实际 Collider 判断穿入方向。
                // 桥面/基地地板可能已经接触脚底，沿面移动不代表向墙内穿入，不能按包围盒中心误拦。
                if (Physics.ComputePenetration(ownCollider, ownCollider.transform.position + limitedStep, ownCollider.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out Vector3 normal, out float depth))
                {
                    if (IsSupportingContact(other, normal, direction)) continue;
                    if (Vector3.Dot(normal, limitedStep) < -0.00001f)
                    {  return Vector3.zero; }
                }
            }
            else
            {
                Vector3 away = bounds.center - other.bounds.center;
                if (Vector3.Dot(away, limitedStep) <= 0f) {  return Vector3.zero; }
            }
        }
        return limitedStep / Time.fixedDeltaTime;
    }

    /// <summary>只对近水平运动识别承托面；保留墙、士兵及向下撞入地面的阻挡。</summary>
    private bool IsSupportingContact(Collider other, Vector3 normal, Vector3 direction)
    {
        if (Mathf.Abs(direction.y) > 0.05f || other.GetComponentInParent<SoldierHealth>() != null) return false;
        if (normal.y > 0.65f) return true;
        // CapsuleCast 起点已接触时可能返回与运动相反的法线，改用真实碰撞体分离法线。
        return ownCollider != null && Physics.ComputePenetration(ownCollider, ownCollider.transform.position, ownCollider.transform.rotation,
            other, other.transform.position, other.transform.rotation, out Vector3 separation, out float depth) && separation.y > 0.65f;
    }

    /// <summary>
    /// CapsuleCast 起点已重叠时会返回距离零，不能用它阻止“离开已有重叠”的移动。
    /// 只放行沿分离方向或切线的移动，落点仍接受下面的真实穿入方向检查；绝不放行向对方体内移动。
    /// </summary>
    private bool IsEscapingOverlap(Collider other, Vector3 direction)
    {
        return ownCollider != null && Physics.ComputePenetration(ownCollider, ownCollider.transform.position,
            ownCollider.transform.rotation, other, other.transform.position, other.transform.rotation,
            out Vector3 separation, out float depth) && Vector3.Dot(separation, direction) >= -0.00001f;
    }

    private bool IsBlocker(Collider other)
    {
        if (other == null || other.transform == MotionRoot || other.transform.IsChildOf(MotionRoot)) return false;
        if (Physics.GetIgnoreLayerCollision(MotionRoot.gameObject.layer, other.gameObject.layer)) return false;
        var otherHealth = other.GetComponentInParent<SoldierHealth>();
        return otherHealth == null || !otherHealth.isDead;
    }

}
