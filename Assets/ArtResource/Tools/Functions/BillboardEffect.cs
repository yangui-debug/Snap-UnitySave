using System;
using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    public static Func<Camera> GetCameraFunc;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = GetCameraFunc?.Invoke();
    }

    void LateUpdate()
    {
        if (!mainCamera)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.LookAt(transform.position + mainCamera.transform.forward);
    }
}