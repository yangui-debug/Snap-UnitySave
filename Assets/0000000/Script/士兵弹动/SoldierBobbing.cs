using UnityEngine;

public class SoldierBobbing : MonoBehaviour
{
    [Header("跳动参数设置")]
    public float bounceSpeed = 14f;
    public float bounceHeight = 0.2f;
    public float resetSpeed = 10f;

    [Header("轴向自定义（适配 FBX 模型轴向偏差）")]
    [Tooltip("士兵头顶对应的局部轴，默认 (0,1,0) 即 Y 轴。若模型头顶朝向 Z 轴可改为 (0,0,1)")]
    public Vector3 localUpAxis = Vector3.up;

    private Animator animator;
    private Vector3 initialLocalPos;

    void Start()
    {
        initialLocalPos = transform.localPosition;
        animator = GetComponentInParent<Animator>();
    }

    void LateUpdate()
    {
        if (animator == null) return;

        // 读取由 SoldierAnimationController 更新的 Animator 参数[cite: 1, 2]
        bool isMoving = animator.GetFloat("Speed") > 0.1f; 
        bool isFighting = animator.GetBool("IsNearEnemy")
                       || animator.GetBool("IsNearBuilding")
                       || animator.GetBool("IsNearTree"); 

        if (isMoving || isFighting)
        {
            float bounceOffset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;

            // 核心修改：通过 localRotation 将轴向向量转换为父节点坐标系下的正确方向
            Vector3 worldUpInParentSpace = transform.localRotation * localUpAxis;
            transform.localPosition = initialLocalPos + worldUpInParentSpace * bounceOffset;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos, Time.deltaTime * resetSpeed);
        }
    }
}