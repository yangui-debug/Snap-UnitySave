using UnityEngine;

/// <summary>
/// 让物体在屏幕中保持稳定的朝向：屏幕对齐 / 面向相机 / 保持初始世界旋转。
/// 可选保持恒定屏幕尺寸。
/// 适用于 3D TextMeshPro、世界空间 Canvas 元素、任意 3D 物体。
/// </summary>
[ExecuteAlways]
public class ScreenAlignedBillboard : MonoBehaviour
{
    public enum Mode
    {
        /// <summary>复制相机的朝向（在屏幕中看起来始终不变）。</summary>
        ScreenAligned,
        /// <summary>始终面向相机（广告牌）。</summary>
        FaceCamera,
        /// <summary>保持初始世界旋转（不受父物体旋转影响）。</summary>
        KeepInitialWorldRotation
    }

    [Header("Basic")]
    public Mode mode = Mode.ScreenAligned;
    public Camera targetCamera;               // 为空会自动找主相机
    public Vector3 eulerOffset;               // 额外叠加的欧拉角偏移（度）

    [Header("Axis Options (可选)")]
    public bool keepUpright = true;           // 竖直朝上（常用于 3D 文本）
    public bool lockX, lockY, lockZ;          // 锁定某些轴

    [Header("屏幕尺寸保持")]
    public bool keepConstantScreenSize = false;   // 是否启用
    public float referenceDistance = 10f;         // 参考距离（物体与相机相隔这么远时，scale=初始值）
    private Vector3 _initialScale;

    [Header("更新时机")]
    public bool updateInLateUpdate = true;    // 用 LateUpdate 覆盖动画/父物体的旋转

    Quaternion _initialWorldRotation;

    void Reset()
    {
        targetCamera = Camera.main;
        _initialWorldRotation = transform.rotation;
        _initialScale = transform.localScale;
    }

    void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;
        _initialWorldRotation = transform.rotation;
        _initialScale = transform.localScale;
    }

    void LateUpdate() { if (updateInLateUpdate) Apply(); }
    void Update()     { if (!updateInLateUpdate) Apply(); }

    void Apply()
    {
        if (!targetCamera && !(targetCamera = Camera.main)) return;

        Quaternion result;

        switch (mode)
        {
            case Mode.ScreenAligned:
                result = targetCamera.transform.rotation;
                if (keepUpright)
                {
                    Vector3 fwd = targetCamera.transform.forward;
                    Vector3 up = Vector3.up;
                    if (Vector3.Dot(fwd.normalized, up) > 0.999f) up = targetCamera.transform.up;
                    result = Quaternion.LookRotation(fwd, up);
                }
                break;

            case Mode.FaceCamera:
                if (keepUpright)
                {
                    Vector3 toCam = targetCamera.transform.position - transform.position;
                    Vector3 flat = Vector3.ProjectOnPlane(toCam, Vector3.up);
                    if (flat.sqrMagnitude < 1e-8f) flat = targetCamera.transform.forward;
                    result = Quaternion.LookRotation(flat, Vector3.up);
                }
                else
                {
                    result = Quaternion.LookRotation(
                        targetCamera.transform.position - transform.position,
                        targetCamera.transform.up);
                }
                break;

            case Mode.KeepInitialWorldRotation:
            default:
                result = _initialWorldRotation;
                break;
        }

        // 额外欧拉角偏移
        Vector3 finalEuler = (result * Quaternion.Euler(eulerOffset)).eulerAngles;

        // 轴锁定
        if (lockX || lockY || lockZ)
        {
            Vector3 cur = transform.rotation.eulerAngles;
            if (lockX) finalEuler.x = cur.x;
            if (lockY) finalEuler.y = cur.y;
            if (lockZ) finalEuler.z = cur.z;
        }
        transform.rotation = Quaternion.Euler(finalEuler);

        // 保持恒定屏幕尺寸
        if (keepConstantScreenSize)
        {
            float dist = Vector3.Distance(targetCamera.transform.position, transform.position);
            if (dist > 0.0001f)
            {
                float scaleFactor = dist / referenceDistance;
                transform.localScale = _initialScale * scaleFactor;
            }
        }
    }

    /// <summary>在运行时切换父节点/相机后，如需重置“保持初始旋转”或缩放，请手动调用。</summary>
    public void RegrabInitialSettings()
    {
        _initialWorldRotation = transform.rotation;
        _initialScale = transform.localScale;
    }
}
