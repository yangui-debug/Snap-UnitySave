using UnityEngine;
using UnityEngine.AI; // 引入导航AI命名空间

[RequireComponent(typeof(NavMeshAgent))] // 自动添加NavMeshAgent组件
public class NavMeshChaser : MonoBehaviour
{
    [Header("目标设置")]
    [Tooltip("直接把场景里的目标拖到这里")]
    public Transform target;
    public float detectionRange = 10.0f;

    [Header("可选设置")]
    public Animator animator;

    private NavMeshAgent agent;

    void Start()
    {
        // 获取导航组件
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            agent.isStopped = true;
            UpdateAnimation(0);
            return;
        }

        // 计算距离
        float distToTarget = Vector3.Distance(transform.position, target.position);

        // 如果在勾引范围内
        if (distToTarget <= detectionRange)
        {
            agent.isStopped = false; // 允许移动
            agent.SetDestination(target.position); // 把目标位置扔给导航系统，它会自动算路和避让！
        }
        else
        {
            agent.isStopped = true; // 超出范围就停下
            agent.ResetPath(); // 清除路径
        }

        // NavMeshAgent 会自己计算当前速度，直接拿来给动画用即可
        UpdateAnimation(agent.velocity.magnitude);
    }

    void UpdateAnimation(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}