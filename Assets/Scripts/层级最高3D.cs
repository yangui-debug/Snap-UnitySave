using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AlwaysOnTop3D : MonoBehaviour
{
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        int maxQueue = int.MinValue;
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();

        foreach (var r in allRenderers)
        {
            if (r != rend && r.sharedMaterial != null)
            {
                maxQueue = Mathf.Max(maxQueue, r.sharedMaterial.renderQueue);
            }
        }

        // 创建一个新材质实例，防止影响共享材质
        if (rend.material.renderQueue <= maxQueue)
        {
            rend.material.renderQueue = maxQueue + 1;
        }
    }
}
