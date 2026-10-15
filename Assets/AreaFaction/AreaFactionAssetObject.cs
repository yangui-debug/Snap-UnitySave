using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.AreaFaction
{
    [Serializable]
    public class FactionPair
    {
        public int factionId;
        public Material areaMat; // 势力范围填充材质
        public Material areaEdgeMat; // 势力轮廓光晕材质
        public Material areaEdge2Mat; // 势力轮廓描边材质
        public float areaEdgeWidth; // 势力轮廓光晕宽度
        public float areaEdge2Width; // 势力轮廓描边宽度
    }

    [CreateAssetMenu(fileName = "AreaFactionAssetObject", menuName = "ScriptableObjects/AreaFactionAssetObject", order = 1)]
    public class AreaFactionAssetObject : ScriptableObject
    {
        [SerializeField] private int defaultFactionId;
        [SerializeField] private List<FactionPair> factionPairs;

        private Dictionary<int, FactionPair> factionPairDict;

        public FactionPair GetFactionPair(int factionId)
        {
            if (factionPairDict == null)
            {
                factionPairDict = new Dictionary<int, FactionPair>();
                foreach (FactionPair pair in factionPairs)
                {
                    factionPairDict[pair.factionId] = pair;
                }
            }

            if (factionPairDict.TryGetValue(factionId, out var factionPair))
            {
                return factionPair;
            }

            return factionPairDict.GetValueOrDefault(defaultFactionId);
        }
    }
}