using System.Collections;
using UnityEngine;

public class MeshTileHelper : MonoBehaviour
{
    private Renderer m_Renderer;
    private MaterialPropertyBlock m_PropBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private Coroutine m_PulseCoroutine;

    void Awake()
    {
        m_Renderer = GetComponentInChildren<Renderer>();
        m_PropBlock = new MaterialPropertyBlock();
    }

    public void StartEmissionRamp(Color color, float maxIntensity, float duration)
    {
        if (m_PulseCoroutine != null) StopCoroutine(m_PulseCoroutine);
        m_PulseCoroutine = StartCoroutine(RampCoroutine(color, maxIntensity, duration));
    }

    private IEnumerator RampCoroutine(Color baseColor, float maxIntensity, float duration)
    {
        float timer = 0f;
        float halfDuration = duration / 2f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            ApplyEmission(baseColor * Mathf.Lerp(0, maxIntensity, t));
            yield return null;
        }
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            ApplyEmission(baseColor * Mathf.Lerp(maxIntensity, 0, t));
            yield return null;
        }
        ApplyEmission(Color.black);
        m_PulseCoroutine = null;
    }

    private void ApplyEmission(Color color)
    {
        if (m_Renderer == null) return;
        m_Renderer.GetPropertyBlock(m_PropBlock);
        m_PropBlock.SetColor(EmissionColorID, color);
        m_Renderer.SetPropertyBlock(m_PropBlock);
    }
}