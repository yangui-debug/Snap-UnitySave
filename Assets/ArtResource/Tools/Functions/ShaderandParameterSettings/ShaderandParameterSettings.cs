using UnityEngine;

public class ShaderParameterController : MonoBehaviour
{
    [Header("Shader and Parameter Settings")]
    [Tooltip("Name of the Shader to target (e.g., 'Custom/MyShader')")]
    public string shaderName;

    [Tooltip("Name of the parameter to modify (e.g., '_Color', '_FloatParam')")]
    public string parameterName;

    [Header("Parameter Type")]
    public ParameterType parameterType = ParameterType.Float;

    [Header("Parameter Value")]
    [Tooltip("Set the value for Float or Int parameters")]
    public float floatValue;

    [Tooltip("Set the value for Color parameters")]
    public Color colorValue = Color.white;

    [Tooltip("Set the value for Vector parameters")]
    public Vector4 vectorValue = Vector4.zero;

    private Material[] materialsUsingShader;

    public enum ParameterType
    {
        Float,
        Color,
        Vector,
        Int
    }

    void Start()
    {
        FindMaterialsUsingShader();
    }

    void Update()
    {
        UpdateShaderParameter();
    }

    /// <summary>
    /// Finds all materials in the scene that use the specified shader.
    /// </summary>
    private void FindMaterialsUsingShader()
    {
        Shader targetShader = Shader.Find(shaderName);

        if (targetShader == null)
        {
            Debug.LogWarning($"Shader '{shaderName}' not found in the project.");
            return;
        }

        Renderer[] renderers = FindObjectsOfType<Renderer>();
        var matchingMaterials = new System.Collections.Generic.List<Material>();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader == targetShader)
                {
                    matchingMaterials.Add(mat);
                }
            }
        }

        materialsUsingShader = matchingMaterials.ToArray();

        Debug.Log($"Found {materialsUsingShader.Length} materials using Shader '{shaderName}'.");
    }

    /// <summary>
    /// Updates the specified parameter in all materials using the targeted shader.
    /// </summary>
    private void UpdateShaderParameter()
    {
        if (materialsUsingShader == null || materialsUsingShader.Length == 0)
        {
            return;
        }

        foreach (var mat in materialsUsingShader)
        {
            if (!mat.HasProperty(parameterName))
            {
                Debug.LogWarning($"Material '{mat.name}' does not have property '{parameterName}'.");
                continue;
            }

            switch (parameterType)
            {
                case ParameterType.Float:
                    mat.SetFloat(parameterName, floatValue);
                    break;

                case ParameterType.Int:
                    mat.SetInt(parameterName, Mathf.RoundToInt(floatValue));
                    break;

                case ParameterType.Color:
                    mat.SetColor(parameterName, colorValue);
                    break;

                case ParameterType.Vector:
                    mat.SetVector(parameterName, vectorValue);
                    break;
            }
        }
    }
}
