using UnityEngine;

public class AcceleratingFall : MonoBehaviour
{
    public float gravityMultiplier = 2f;  // 重力的加速倍数（可调节）
    private Rigidbody rb;

    void Start()
    {
        // 获取 Rigidbody 组件
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning("该物体没有 Rigidbody 组件！");
            return;
        }

        // 禁用物体默认的重力
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        // 每帧更新物体的速度，模拟加速效果
        if (rb != null)
        {
            // 应用重力加速度
            Vector3 gravityForce = Vector3.down * Physics.gravity.y * gravityMultiplier;
            rb.AddForce(gravityForce, ForceMode.Acceleration);
        }
    }
}
