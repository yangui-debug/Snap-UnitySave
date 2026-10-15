using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexScaleControlForLineEffect : MonoBehaviour
{
    public enum ScaleAxis
    {
        X,
        Y,
        Z
    }
    
    // [SerializeField] private float texScaleCoef = 1f;
    [SerializeField] private Transform effectTransform;
    [SerializeField] private ScaleAxis scaleAxis = ScaleAxis.X;
    [SerializeField] private bool isInverseTilingAxis = false;
    [SerializeField] private List<float> texScaleCoefs = new List<float>(); 
    [SerializeField] private List<GameObject> particleSystems = new List<GameObject>();
    public string textureProperty = "_Main_Tex";
    private List<Material> particleSystemMats = new List<Material>();
    private List<Vector2> oriValues = new List<Vector2>();

    // Start is called before the first frame update
    void Start()
    {
        if (texScaleCoefs.Count != particleSystems.Count)
        {
            Debug.LogError("请保证参数列表texScaleCoefs与特效列表particleSystems的长度相同");
        }
        
        if (particleSystems.Count > 0)
        {
            for (int i = 0; i < particleSystems.Count; i++)
            {
                var particleSystem = particleSystems[i].GetComponent<ParticleSystem>();

                if (particleSystem != null)
                {
                    particleSystemMats.Add(particleSystem.GetComponent<Renderer>().material);
                }
                else
                {
                    particleSystemMats.Add(particleSystems[i].GetComponent<Renderer>().material);
                }
                
                oriValues.Add(particleSystemMats[i].GetTextureScale(textureProperty));
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        var value = 1 * GetSelectedScaleValue();

        if (particleSystems.Count > 0)
        {
            for (int i = 0; i < particleSystems.Count; i++)
            {
                if (!isInverseTilingAxis)
                {
                    var tempVec = oriValues[i];
                    tempVec.y = value * texScaleCoefs[i];
                    oriValues[i] = tempVec;
                }
                else
                {
                    var tempVec = oriValues[i];
                    tempVec.x = value * texScaleCoefs[i];
                    oriValues[i] = tempVec;
                }
                
                particleSystemMats[i].SetTextureScale(textureProperty, oriValues[i]);
            }
        }
    }
    
    float GetSelectedScaleValue()
    {
        switch (scaleAxis)
        {
            case ScaleAxis.X:
                return effectTransform.localScale.x;
            case ScaleAxis.Y:
                return effectTransform.localScale.y;
            case ScaleAxis.Z:
                return effectTransform.localScale.z;
            default:
                return effectTransform.localScale.x; // 默认使用X轴
        }
    }
}
