using UnityEngine;

public enum Team
{
    Red,
    Blue
}

public class BattleAnimationController : MonoBehaviour
{
    [Header("阵营设置")]
    public Team team;

    [Header("检测范围（米）")]
    public float combatRange = 0.5f;

    [Header("动画器")]
    public Animator animator;

    private Vector3 lastPosition;
    private Transform currentEnemy;

    void Start()
    {
        lastPosition = transform.position;
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 计算移动速度
        float moveSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        animator.SetFloat("Speed", moveSpeed);
        lastPosition = transform.position;

        // 查找最近敌方单位
        GameObject enemy = FindClosestEnemy();
        if (enemy != null)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            animator.SetBool("IsInCombat", dist < combatRange);
        }
        else
        {
            animator.SetBool("IsInCombat", false);
        }
    }

    GameObject FindClosestEnemy()
    {
        BattleAnimationController[] allUnits = FindObjectsOfType<BattleAnimationController>();
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var unit in allUnits)
        {
            if (unit == this) continue;
            if (unit.team == this.team) continue; // 只查找敌方

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < minDist)
            {
                closest = unit.gameObject;
                minDist = dist;
            }
        }

        return closest;
    }

    public void TriggerVictory()
    {
        animator.SetTrigger("Victory");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combatRange);
    }
}
