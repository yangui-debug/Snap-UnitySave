using UnityEngine;

public class SetRenderQueue : MonoBehaviour
{
    [SerializeField]
    private Renderer targetRenderer; // 需要设置的目标Renderer
    [SerializeField]
    private int renderQueue = 3000;  // 默认的渲染队列值

    private void Start()
    {
        // 检查目标Renderer是否存在
        if (targetRenderer != null && targetRenderer.material != null)
        {
            // 设置材质的Render Queue
            targetRenderer.material.renderQueue = renderQueue;
            Debug.Log($"Render Queue of {targetRenderer.name} set to {renderQueue}");
        }
        else
        {
            Debug.LogError("Target Renderer or Material is missing!");
        }
    }
}