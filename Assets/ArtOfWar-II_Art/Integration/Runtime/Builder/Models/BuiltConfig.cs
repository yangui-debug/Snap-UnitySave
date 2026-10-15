using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace ArtOfWar_II_Art.Integration.Scripts.Builder.Models
{
    [Serializable]
    public class BuiltConfig
    {
        public MapSize mapSize;
        public List<Area> areas;
    }

    [Serializable]
    public class MapSize
    {
        public V3d pos;
        public float width;
        public float height;
    }

    [Serializable]
    public class V3d
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class Area
    {
        public int areaId;
        public List<BuiltItem> decorates;
    }

    [Serializable]
    public class BuiltItem
    {
        public int type;
        public int level;
        public V3d pos;
        public V3d rot;
    }

    [Serializable]
    public class ItemSizeConfig
    {
        public List<ItemSize> sizes;
    }

    [Serializable]
    public class ItemSize
    {
        public int type;
        public int[] pos;
    }
}