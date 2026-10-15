#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class DirectionTracking : MonoBehaviour
{
    public Transform target; // 要指向的目标物体
    [Header("追踪轴向")]
    public OnlyAxis Axis = OnlyAxis.x_Axis; // 选择旋转的轴向
    private OnlyAxis CurrentAxis = OnlyAxis.x_Axis; // 选择旋转的轴向


    private void Update()
    {
        SetAxis(Axis);
        // 获取目标方向
        Vector3 targetDirection = target.position - transform.position;
        Vector3 RotateAxis = transform.up;
        // 根据选择的轴向，获取目标方向在对应轴上的投影
        Vector3 axisDirection = Vector3.zero;
        switch (CurrentAxis)
        {
            case OnlyAxis.x_Axis:
                axisDirection = Vector3.ProjectOnPlane(targetDirection, transform.right);
                break;
            case OnlyAxis.y_Axis:
                axisDirection = Vector3.ProjectOnPlane(targetDirection, transform.up);
                break;
            case OnlyAxis.z_Axis:
                axisDirection = transform.forward;
                Vector3 dirTemp = transform.forward.normalized;
                RotateAxis = Vector3.ProjectOnPlane(targetDirection, dirTemp);
                break;
        }

        // 计算旋转角度
        Quaternion newRotation = Quaternion.LookRotation(axisDirection, RotateAxis);
        transform.rotation = newRotation;
    }
    void SetAxis(OnlyAxis mAxis)
    {
        if (CurrentAxis != mAxis)
        {
            CurrentAxis = mAxis;
        }
    }
    public enum OnlyAxis
    {
        x_Axis,
        y_Axis,
        z_Axis
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        DrawCircle();
    }
    public bool is3DDraw = false;
    public float radius = 0.1f;
    public float arrowLength = 0.01f;
    int segments = 20;

    private void DrawCircle()
    {
        float Radius = is3DDraw ? radius : radius * Vector3.Distance(transform.position, SceneView.lastActiveSceneView.camera.transform.position);
        float ArrowLength = is3DDraw ? arrowLength : arrowLength * Vector3.Distance(transform.position, SceneView.lastActiveSceneView.camera.transform.position);

        Gizmos.color = Color.green;
        Vector3 center = transform.position;
        Quaternion rotation = transform.rotation;
        float angleIncrement = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleIncrement;
            Vector3 point = Vector3.zero;
            float nextAngle = (i + 1) * angleIncrement;
            Vector3 nextPoint = Vector3.zero;

            // 根据所选轴向，调整旋转
            switch (Axis)
            {
                case OnlyAxis.x_Axis:
                    point = center + rotation * Quaternion.Euler(angle, 0, 0) * (Vector3.up * Radius);
                    nextPoint = center + rotation * Quaternion.Euler(nextAngle, 0, 0) * (Vector3.up * Radius);
                    if (i == 0)
                    {
                        Gizmos.DrawLine(nextPoint, center + rotation * Quaternion.Euler(angle + angleIncrement / 2, 0, 0) * (Vector3.up * (Radius + ArrowLength)));
                        Gizmos.DrawLine(nextPoint, center + rotation * Quaternion.Euler(angle + angleIncrement / 2, 0, 0) * (Vector3.up * (Radius - ArrowLength)));
                    }
                    break;
                case OnlyAxis.y_Axis:
                    point = center + rotation * Quaternion.Euler(0, angle, 0) * (Vector3.right * Radius);
                    nextPoint = center + rotation * Quaternion.Euler(0, nextAngle, 0) * (Vector3.right * Radius);
                    if (i == 0)
                    {
                        Gizmos.DrawLine(nextPoint, center + rotation * Quaternion.Euler(0, angle + angleIncrement / 2, 0) * (Vector3.right * (Radius + ArrowLength)));
                        Gizmos.DrawLine(nextPoint, center + rotation * Quaternion.Euler(0, angle + angleIncrement / 2, 0) * (Vector3.right * (Radius + -ArrowLength)));
                    }
                    break;
                case OnlyAxis.z_Axis:
                    point = center + rotation * Quaternion.Euler(0, 0, angle) * (Vector3.right * Radius);
                    nextPoint = center + rotation * Quaternion.Euler(0, 0, nextAngle) * (Vector3.right * Radius);
                    if (i == 0)
                    {
                        Gizmos.DrawLine(nextPoint, center + rotation * Quaternion.Euler(0, 0, angle + angleIncrement / 2) * (Vector3.right * (Radius + ArrowLength)));
                        Gizmos.DrawLine(nextPoint, center + rotation * Quaternion.Euler(0, 0, angle + angleIncrement / 2) * (Vector3.right * (Radius + -ArrowLength)));
                    }
                    break;
            }
            if (i != 1) Gizmos.DrawLine(point, nextPoint);
        }
    }
#endif
}
