using UnityEngine;

public class ArmyMover : MonoBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 2f;

    [Header("目标物体（终点）")]
    public Transform targetTransform;

    [Header("停止距离")]
    public float stopDistance = 0.5f;

    private bool hasArrived = false;

    void Update()
    {
        if (hasArrived || targetTransform == null) return;

        Vector3 direction = (targetTransform.position - transform.position).normalized;

        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        if (distance <= stopDistance)
        {
            hasArrived = true;
            Debug.Log("军队已到达目标位置");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (targetTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetTransform.position, stopDistance);
        }
    }
}
