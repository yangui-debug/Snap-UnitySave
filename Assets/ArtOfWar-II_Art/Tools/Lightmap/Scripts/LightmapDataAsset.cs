using System.Collections.Generic;
using UnityEngine;

namespace i89Trillion.Art.Tools
{
    [System.Serializable]
    public class LightmapSavedData
    {
        public int index;
        public Texture2D light;
        public Texture2D dir;
        public Texture2D shadowMask;
    }

    public class LightmapDataAsset : ScriptableObject
    {
        public List<LightmapSavedData> lightmapDataList = new List<LightmapSavedData>();
    }
}