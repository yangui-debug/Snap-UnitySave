using UnityEngine;

public class FovScreenAdjust : MonoBehaviour
{
    [SerializeField] private float fov = 50;
    [SerializeField] private int fovAdjust = 1;

    private Camera targetCamera;

    private void Start()
    {
        if (!targetCamera)
        {
            targetCamera = GetComponent<Camera>();
        }
    }

    private void Update()
    {
        if (!targetCamera)
        {
            return;
        }

        targetCamera.fieldOfView = fovAdjust == 1 ? AdjustFovByWidth(fov) : AdjustFovByHeight(fov);
    }

    public static float AdjustFovByHeight(float src)
    {
        float targetAspect = 9f / 16; // 目标宽高比
        float windowAspect = (float) Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;
        if (scaleHeight > 1.0f)
        {
            return src * scaleHeight;
        }

        return src;
    }

    public static float AdjustFovByWidth(float src)
    {
        float targetAspect = 9f / 16; // 目标宽高比
        float windowAspect = (float) Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;
        if (scaleHeight > 1.0f)
        {
            return src;
        }

        return src / scaleHeight;
    }
}