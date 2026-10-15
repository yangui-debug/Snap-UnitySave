using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class GlobalWaterShaderController : MonoBehaviour
{
    public enum UpdateMode { OnChange, EveryFrame, Manual }

    [Header("全局控制器设置")]
    [Tooltip("控制更新频率")]
    public UpdateMode updateMode = UpdateMode.OnChange;

    [Header("水位设置")]
    [Tooltip("水位高度")]
    [Range(-10f, 10f)] public float waterHeight = -2.5f;

    [Header("水面颜色渐变")]
    [Tooltip("近处水色")]
    public Color nearColor = new Color(0.1803922f, 0.3019608f, 0.6039216f, 1f);
    [Tooltip("中间水色")]
    public Color midColor = new Color(0.1176471f, 0.6156863f, 0.8196079f, 1f);
    [Tooltip("远处水色")]
    public Color farColor = new Color(0.4039216f, 0.8274511f, 0.8666667f, 1f);
    [Tooltip("远处渐变距离")]
    [Range(0f, 1f)] public float farDistance = 0.32f;
    [Tooltip("近处渐变距离")]
    [Range(0f, 1f)] public float nearDistance = 0f;
    
    [Header("水下效果")]
    [Tooltip("水下颜色")]
    public Color underWaterColor = new Color(0.1843137f, 0.3176471f, 0.5215687f, 1f);
    [Tooltip("深度平滑度")]
    [Range(0f, 100f)] public float deepSmooth = 75f;
    [Tooltip("深度范围")]
    [Range(-10f, 0f)] public float deepRange = -0.5f;
    
    [Header("水花效果")]
    [Tooltip("水花噪波贴图")]
    public Texture2D foamNoiseTex;
    [Tooltip("水花颜色")]
    public Color foamColor = new Color(0.854902f, 0.9215687f, 1f, 1f);
    [Tooltip("水花平铺密度")]
    public Vector2 foamTiling = new Vector2(20f, 20f);
    [Tooltip("水花流动速度")]
    public Vector2 foamSpeed = new Vector2(3.7f, 1f);
    [Tooltip("水花宽度")]
    [Range(0f, 5f)] public float foamWidth = 2.5f;
    [Tooltip("水花混合度")]
    [Range(0f, 1f)] public float foamBlend = 0.26f;

    // 属性ID缓存（移除 WaterKeywordID）
    private static readonly int WaterHeightID = Shader.PropertyToID("_WaterHeight");
    private static readonly int NearColorID = Shader.PropertyToID("_NearColor");
    private static readonly int MidColorID = Shader.PropertyToID("_MidColor");
    private static readonly int FarColorID = Shader.PropertyToID("_FarColor");
    private static readonly int FarDisID = Shader.PropertyToID("_FarDis");
    private static readonly int NearDisID = Shader.PropertyToID("_NearDis");
    private static readonly int UnderWaterColorID = Shader.PropertyToID("_UnderWaterColor");
    private static readonly int DeepSmoothID = Shader.PropertyToID("_DeepSmooth");
    private static readonly int DeepRangeID = Shader.PropertyToID("_DeepRange");
    private static readonly int FoamNoiseTexID = Shader.PropertyToID("_FoamNoiseTex");
    private static readonly int FormaColorID = Shader.PropertyToID("_FormaColor");
    private static readonly int FoamTillingID = Shader.PropertyToID("_FoamTilling");
    private static readonly int FoamSpeedID = Shader.PropertyToID("_FoamSpeed");
    private static readonly int FoamWidthID = Shader.PropertyToID("_FoamWidth");
    private static readonly int FoamBlendID = Shader.PropertyToID("_FoamBlend");

    private List<Material> waterMaterials = new List<Material>();

    // 移除 lastEnableWater
    private float lastWaterHeight;
    private Color lastNearColor;
    private Color lastMidColor;
    private Color lastFarColor;
    private float lastFarDistance;
    private float lastNearDistance;
    private Color lastUnderWaterColor;
    private float lastDeepSmooth;
    private float lastDeepRange;
    private Texture2D lastFoamNoiseTex;
    private Color lastFoamColor;
    private Vector2 lastFoamTiling;
    private Vector2 lastFoamSpeed;
    private float lastFoamWidth;
    private float lastFoamBlend;

    private void OnEnable()
    {
        CacheAllWaterMaterials();
        SaveCurrentState();
        UpdateAllMaterials();
    }

    private void OnValidate()
    {
        if (updateMode == UpdateMode.OnChange && Application.isPlaying)
        {
            UpdateAllMaterials();
        }
        #if UNITY_EDITOR
        else if (!Application.isPlaying)
        {
            CacheAllWaterMaterials();
            UpdateAllMaterials();
        }
        #endif
    }

    private void Update()
    {
        if (updateMode == UpdateMode.EveryFrame)
        {
            UpdateAllMaterials();
        }
    }

    private void CacheAllWaterMaterials()
    {
        waterMaterials.Clear();
        Renderer[] allRenderers = FindObjectsOfType<Renderer>(true);

        foreach (Renderer renderer in allRenderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader != null && mat.shader.name == "SceneCommon")
                {
                    if (!waterMaterials.Contains(mat))
                    {
                        waterMaterials.Add(mat);
                    }
                }
            }
        }
    }

    private void SaveCurrentState()
    {
        lastWaterHeight = waterHeight;
        lastNearColor = nearColor;
        lastMidColor = midColor;
        lastFarColor = farColor;
        lastFarDistance = farDistance;
        lastNearDistance = nearDistance;
        lastUnderWaterColor = underWaterColor;
        lastDeepSmooth = deepSmooth;
        lastDeepRange = deepRange;
        lastFoamNoiseTex = foamNoiseTex;
        lastFoamColor = foamColor;
        lastFoamTiling = foamTiling;
        lastFoamSpeed = foamSpeed;
        lastFoamWidth = foamWidth;
        lastFoamBlend = foamBlend;
    }

    public void UpdateAllMaterials()
    {
        bool needsUpdate = updateMode == UpdateMode.OnChange && (
            lastWaterHeight != waterHeight ||
            lastNearColor != nearColor ||
            lastMidColor != midColor ||
            lastFarColor != farColor ||
            lastFarDistance != farDistance ||
            lastNearDistance != nearDistance ||
            lastUnderWaterColor != underWaterColor ||
            lastDeepSmooth != deepSmooth ||
            lastDeepRange != deepRange ||
            lastFoamNoiseTex != foamNoiseTex ||
            lastFoamColor != foamColor ||
            lastFoamTiling != foamTiling ||
            lastFoamSpeed != foamSpeed ||
            lastFoamWidth != foamWidth ||
            lastFoamBlend != foamBlend
        );

        if (needsUpdate || updateMode == UpdateMode.EveryFrame || updateMode == UpdateMode.Manual)
        {
            foreach (Material mat in waterMaterials)
            {
                if (mat != null)
                {
                    UpdateMaterialProperties(mat);
                }
            }
            
            SaveCurrentState();
        }
    }

    private void UpdateMaterialProperties(Material mat)
    {
        // 不再控制 _WATER_ON，由材质自己管理
        mat.SetFloat(WaterHeightID, waterHeight);
        mat.SetColor(NearColorID, nearColor);
        mat.SetColor(MidColorID, midColor);
        mat.SetColor(FarColorID, farColor);
        mat.SetFloat(FarDisID, farDistance);
        mat.SetFloat(NearDisID, nearDistance);
        
        mat.SetColor(UnderWaterColorID, underWaterColor);
        mat.SetFloat(DeepSmoothID, deepSmooth);
        mat.SetFloat(DeepRangeID, deepRange);
        
        if (foamNoiseTex != null) mat.SetTexture(FoamNoiseTexID, foamNoiseTex);
        mat.SetColor(FormaColorID, foamColor);
        mat.SetVector(FoamTillingID, new Vector4(foamTiling.x, foamTiling.y, 0, 0));
        mat.SetVector(FoamSpeedID, new Vector4(foamSpeed.x, foamSpeed.y, 0, 0));
        mat.SetFloat(FoamWidthID, foamWidth);
        mat.SetFloat(FoamBlendID, foamBlend);
    }

    public void ReloadWaterMaterials()
    {
        CacheAllWaterMaterials();
        UpdateAllMaterials();
    }

    public int GetWaterObjectCount()
    {
        return waterMaterials.Count;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (waterMaterials != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Material mat in waterMaterials)
            {
                if (mat != null)
                {
                    Renderer renderer = GetRendererFromMaterial(mat);
                    if (renderer != null)
                    {
                        Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
                    }
                }
            }
        }
    }

    private Renderer GetRendererFromMaterial(Material material)
    {
        Renderer[] allRenderers = FindObjectsOfType<Renderer>(true);
        foreach (Renderer renderer in allRenderers)
        {
            Material[] materials = renderer.sharedMaterials;
            foreach (Material mat in materials)
            {
                if (mat == material)
                {
                    return renderer;
                }
            }
        }
        return null;
    }
    #endif
}