using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CameraOverlapRender : MonoBehaviour
{
    public Material mat;

    public bool enableMaterial = true;

    private void OnEnable()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }

    private void OnDisable()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.None;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!enableMaterial || !mat)
        {
            Graphics.Blit(src, dest);
        }
        else
        {
            Graphics.Blit(src, dest, mat);
        }
    }
}