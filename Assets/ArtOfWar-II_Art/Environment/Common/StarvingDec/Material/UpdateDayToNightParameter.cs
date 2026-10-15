using UnityEngine;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class UpdateDayToNightParameter : MonoBehaviour
{
    [Range(0, 1)] public float dayToNightValue = 0f; // 公开参数，以便在Inspector中调整

    [SerializeField] private string[] shaderNames; // 在Inspector中配置支持的shader名称

    private MaterialPropertyBlock mpb;
    private Renderer[] renderers;

    private PlayableDirector playableDirector;

#if UNITY_EDITOR

    void OnEnable()
    {
        mpb = new MaterialPropertyBlock();
        renderers = FindObjectsOfType<Renderer>();
        EditorApplication.update += OnEditorUpdate;
        playableDirector = GetComponent<PlayableDirector>();
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        // 仅在未播放模式下更新材质参数，避免干扰Timeline
        if (!Application.isPlaying && (playableDirector == null || playableDirector.state != PlayState.Playing))
        {
            UpdateAllMaterials();
        }
    }

    void UpdateAllMaterials()
    {
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && IsShaderSupported(mat.shader.name))
                {
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetFloat("_DayToNight", dayToNightValue);
                    renderer.SetPropertyBlock(mpb);
                }
            }
        }
    }

    bool IsShaderSupported(string shaderName)
    {
        foreach (var name in shaderNames)
        {
            if (name == shaderName)
            {
                return true;
            }
        }

        return false;
    }
#endif
}