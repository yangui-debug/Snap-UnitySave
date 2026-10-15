using UnityEngine;

namespace i89Trillion.Art.Tools
{
    [System.Serializable]
    public class LightmapScheme
    {
        public int scheme; // 对应的方案编号
        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
    }
}