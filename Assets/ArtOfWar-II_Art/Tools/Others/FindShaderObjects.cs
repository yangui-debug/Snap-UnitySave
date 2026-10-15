using System.Collections.Generic;
using UnityEngine;

namespace Art.Tools
{
    public class FindShaderObjects : MonoBehaviour
    {
        private readonly Dictionary<string, int> shaderCounters = new();

        void Start()
        {
            FindAndPrintURPLitObjects();
        }

        void FindAndPrintURPLitObjects()
        {
            List<GameObject> urpLitObjects = new List<GameObject>();
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpLitShader == null)
            {
                Debug.LogError("URP Lit Shader not found. Make sure URP is installed and active.");
                return;
            }

            Renderer[] renderers = FindObjectsOfType<Renderer>(true);

            foreach (Renderer ren in renderers)
            {
                if (!ren.sharedMaterial)
                {
                    continue;
                }

                if (ren.sharedMaterial.shader == urpLitShader)
                {
                    urpLitObjects.Add(ren.gameObject);
                }

                shaderCounters.TryGetValue(ren.sharedMaterial.shader.name, out int count);
                shaderCounters[ren.sharedMaterial.shader.name] = count + 1;
            }

            Debug.Log($"<color=red>Found {urpLitObjects.Count} objects using URP Lit Shader:</color>");
            foreach (GameObject obj in urpLitObjects)
            {
                Debug.Log($"<color=red>{obj.name}, {obj}</color>");
            }

            Debug.Log("Shader usage:");
            foreach (KeyValuePair<string, int> pair in shaderCounters)
            {
                Debug.Log($"<color=green>{pair.Key}: {pair.Value}</color>");
            }
        }
    }
}