using System;
using System.Collections.Generic;
using UnityEngine;

namespace i89Trillion.Art.Tools
{
    [Serializable]
    public class LightmapExceptedObject
    {
        public int scheme;
        public GameObject[] exceptedObjects;
    }

    public class LightmapsManager : MonoBehaviour
    {
        [SerializeField] private LightmapDataAsset[] lightmapDataAssets = default;
        [SerializeField] private int defaultScheme = 1;
        [SerializeField] private LightmapExceptedObject[] exceptedObjectsMap;

        private static LightmapsManager instance;
        public static LightmapsManager Instance => instance;

        private List<LightmapSwitcher> lightmapSwitchers = new List<LightmapSwitcher>();

        private int currentScheme = 1;
        public int CurrentScheme => currentScheme;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            // TODO review 合适时机
            ApplyScheme(defaultScheme);
        }

        public void ApplyScheme(int scheme)
        {
            if (scheme > lightmapDataAssets.Length)
            {
                Debug.LogError($"Invalid lightmap scheme id: {scheme}");
                return;
            }

            // 设置lightmaps
            LightmapDataAsset dataAsset = lightmapDataAssets[scheme - 1];
            if (dataAsset != null && dataAsset.lightmapDataList != null)
            {
                // 创建新的LightmapData数组并从LightmapDataAsset中填充数据
                LightmapData[] newLightmaps = new LightmapData[dataAsset.lightmapDataList.Count];
                for (int i = 0; i < newLightmaps.Length; i++)
                {
                    newLightmaps[i] = new LightmapData
                    {
                        lightmapColor = dataAsset.lightmapDataList[i].light,
                        lightmapDir = dataAsset.lightmapDataList[i].dir,
                        shadowMask = dataAsset.lightmapDataList[i].shadowMask
                    };
                }

                LightmapSettings.lightmaps = newLightmaps;
            }

            // 记录当前方案id
            currentScheme = scheme;

            // 改变物体Renderer的Lightmap数据
            foreach (var lightmapSwitcher in lightmapSwitchers)
            {
                lightmapSwitcher.ApplyLightmapSettings(scheme);
            }

            // 改变物体的可见性
            ChangeObjsVisibility(scheme);
        }

        public void AddSwitcher(LightmapSwitcher lightmapSwitcher)
        {
            if (!lightmapSwitchers.Contains(lightmapSwitcher))
            {
                lightmapSwitchers.Add(lightmapSwitcher);
            }
        }

        public void RemoveSwitcher(LightmapSwitcher lightmapSwitcher)
        {
            lightmapSwitchers.Remove(lightmapSwitcher);
        }

        public void ChangeObjsVisibility(int scheme)
        {
            foreach (var exceptedObject in exceptedObjectsMap)
            {
                foreach (var obj in exceptedObject.exceptedObjects)
                {
                    if (exceptedObject.scheme == scheme)
                    {
                        obj.SetActive(false);
                    }
                    else
                    {
                        obj.SetActive(true);
                    }
                }
            }
        }

#if UNITY_EDITOR
        int curScheme = 1;
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.L))
            {
                curScheme++;
                if (curScheme > lightmapDataAssets.Length)
                {
                    curScheme = 1;
                }

                ApplyScheme(curScheme);
            }
        }
#endif
    }
}