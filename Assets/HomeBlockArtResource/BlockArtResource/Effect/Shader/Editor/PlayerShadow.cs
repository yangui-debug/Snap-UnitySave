using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PlayerShadow : MonoBehaviour
{
    [SerializeField] public Transform lightDirection;

    private List<Material> mMatList = new List<Material>();

    private void Awake()
    {
        UpdateMaterial();
    }

    void Update()
    {
        UpdateShader();
    }

    public void UpdateMaterial()
    {
        mMatList.Clear();
        SkinnedMeshRenderer[] skinnedRenderList = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var render in skinnedRenderList)
        {
            if (render == null)
                continue;
            mMatList.Add(render.material);
        }

        MeshRenderer[] renderList = GetComponentsInChildren<MeshRenderer>();
        foreach (var render in renderList)
        {
            if (render == null)
                continue;
            mMatList.Add(render.material);
        }
    }

    private void UpdateShader()
    {
        if (!lightDirection)
        {
            return;
        }

        //Vector4 worldpos = transform.position;
        Vector4 worldpos = new Vector4(transform.position.x, 0, transform.position.z);
        //Vector4 projdir = new Vector4(-0.06323785f, -0.9545552f, -0.2912483f, 1.0f);
        //mLight.transform.rotation = Quaternion.LookRotation(projdir);
        Vector4 projdir = lightDirection.forward;
        foreach (var mat in mMatList)
        {
            if (mat == null)
                continue;
            mat.SetVector("_WorldPos", worldpos);
            mat.SetVector("_ShadowProjDir", projdir);
            mat.SetVector("_ShadowPlane", new Vector4(0.0f, 1.0f, 0.0f, 0.1f));
            mat.SetVector("_ShadowFadeParams", new Vector4(0.0f, 1.5f, 0.7f, 0.0f));
            mat.SetFloat("_ShadowFalloff", 1.35f);
        }
    }
}