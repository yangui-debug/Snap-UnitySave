using System;
using UnityEngine;

namespace i89Trillion.Art.Tools
{
    public class LightmapSwitcher : MonoBehaviour
    {
        public LightmapScheme[] lightmapSchemes;
        private Renderer rendererComponent;

        private void Awake()
        {
            rendererComponent = GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            if (LightmapsManager.Instance != null)
            {
                LightmapsManager.Instance.AddSwitcher(this);
            }
        }

        private void OnDisable()
        {
            if (LightmapsManager.Instance != null)
            {
                LightmapsManager.Instance.RemoveSwitcher(this);
            }
        }

        void Start()
        {
            if (LightmapsManager.Instance != null)
            {
                ApplyLightmapSettings(LightmapsManager.Instance.CurrentScheme);
            }
        }

        public void ApplyLightmapSettings(int schemeId)
        {
            if (schemeId >= 0 && schemeId <= lightmapSchemes.Length)
            {
                foreach (var lightmapScheme in lightmapSchemes)
                {
                    if (lightmapScheme != null && schemeId == lightmapScheme.scheme)
                    {
                        // 应用存储的lightmap设置到Renderer组件
                        rendererComponent.lightmapIndex = lightmapScheme.lightmapIndex;

                        // TODO 警告：
                        // rendererComponent.lightmapScaleOffset = lightmapScheme.lightmapScaleOffset;
                        // message：
                        // The renderer Xxx(GameObject Name) is a part of a static batch. Setting the lightmap scale and offset will not affect the rendering. The scale and offset is already burnt into the lightmapping UVs in the static batch.
                        // UnityEngine.Renderer:set_lightmapScaleOffset (UnityEngine.Vector4)

                        break;
                    }
                }
            }
            else
            {
                Debug.LogError("Invalid lightmap scheme index.");
            }
        }
    }
}