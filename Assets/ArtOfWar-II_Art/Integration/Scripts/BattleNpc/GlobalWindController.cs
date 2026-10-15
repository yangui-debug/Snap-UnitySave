using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GlobalWindController : MonoBehaviour
{
    private static readonly int Speed = Shader.PropertyToID("_AddSpeed");
    private static readonly int AddWind = Shader.PropertyToID("_GlobalAddWind");
    private static readonly int Intensity = Shader.PropertyToID("_AddIntensity");

    [Tooltip("目标Shader名称（区分大小写）"), SerializeField]
    private string targetShaderName = "Raid/DecShadow";

    [Header("基础参数"), Range(0, 1), SerializeField]
    private float _globalAddWind = 0;

    [SerializeField] private float _AddSpeed = 2f;
    [SerializeField] private float _AddIntensity = 2f;
    [SerializeField] private Light _light;

    [Header("状态跟踪")] private float _lastWindValue = -1;
    private float _lastAddSpeed = -1;
    private float _lastAddIntensity = -1;
    private float _lastLightIntensity = -1;
    private Color _lastLightColor = Color.white;
    private Material[] _cachedMaterials;

    // 属性访问器
    public float GlobalAddWind
    {
        get => _globalAddWind;
        set
        {
            _globalAddWind = Mathf.Clamp01(value);
            UpdateMaterials();
        }
    }

    public float AddSpeed
    {
        get => _AddSpeed;
        set
        {
            _AddSpeed = Mathf.Max(0, value);
            UpdateMaterials();
        }
    }

    public float AddIntensity
    {
        get => _AddIntensity;
        set
        {
            _AddIntensity = Mathf.Max(0, value);
            UpdateMaterials();
        }
    }

    public void ResetState()
    {
        _globalAddWind = _lastWindValue;
        _AddSpeed = _lastAddSpeed;
        _AddIntensity = _lastAddIntensity;
        _light.intensity = _lastLightIntensity;
        _light.color = _lastLightColor;
        UpdateMaterials();
    }

    private void OnEnable()
    {
        InitializeParameters();
        RefreshMaterials();
        UpdateMaterials();
    }

    private void Update()
    {
        // 编辑模式参数变化检测
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CheckParameterChanges();
            return;
        }
#endif

        // 运行时参数更新逻辑
        RuntimeWindUpdate();
    }

    [ContextMenu("刷新材质")]
    public void RefreshMaterials()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        var materialSet = new HashSet<Material>();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader.name == targetShaderName)
                {
                    materialSet.Add(mat);
                }
            }
        }

        _cachedMaterials = new Material[materialSet.Count];
        materialSet.CopyTo(_cachedMaterials);
    }

    private void UpdateMaterials()
    {
        if (_cachedMaterials == null) return;

        foreach (var mat in _cachedMaterials)
        {
            if (mat == null) continue;

            mat.SetFloat(AddWind, _globalAddWind);
            mat.SetFloat(Speed, _AddSpeed);
            mat.SetFloat(Intensity, _AddIntensity);
        }
    }

    #region 新增功能模块

    private void InitializeParameters()
    {
        _lastWindValue = _globalAddWind;
        _lastAddSpeed = _AddSpeed;
        _lastAddIntensity = _AddIntensity;
        _lastLightColor = _light.color;
        _lastLightIntensity = _light.intensity;
    }

    private void CheckParameterChanges()
    {
        bool parametersChanged =
            !Mathf.Approximately(_globalAddWind, _lastWindValue) ||
            !Mathf.Approximately(_AddSpeed, _lastAddSpeed) ||
            !Mathf.Approximately(_AddIntensity, _lastAddIntensity);

        if (parametersChanged)
        {
            UpdateMaterials();
            InitializeParameters();
        }
    }

    private void RuntimeWindUpdate()
    {
        // 示例：运行时动态风效（可按需扩展）
        _globalAddWind = Mathf.PingPong(Time.time * 0.2f, 1f);
        UpdateMaterials();
    }

    #endregion
}