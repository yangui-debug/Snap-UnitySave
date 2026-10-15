using System.Collections.Generic;
using UnityEngine;

namespace ArtOfWar_II_Art.Integration.Scripts
{
    public static class MapConfigHandler
    {
        public static Dictionary<Vector3Int, int> LoadConfig()
        {
            string config = Resources.Load<TextAsset>("map1").text;
            Dictionary<Vector3Int, int> result = new Dictionary<Vector3Int, int>();

            Vector3Int topLeft = new Vector3Int(-20, 0, 20);

            string[] lines = config.Split('\n');
            for (int z = 0; z < lines.Length; z++)
            {
                string[] columns = lines[z].Split(',');

                for (int x = 0; x < columns.Length; x++)
                {
                    if (int.TryParse(columns[x].Trim(), out int id))
                    {
                        Vector3Int p = new Vector3Int(topLeft.x + x, 0, topLeft.z - z);
                        result[p] = id;
                    }
                }
            }

            return result;
        }
    }
}