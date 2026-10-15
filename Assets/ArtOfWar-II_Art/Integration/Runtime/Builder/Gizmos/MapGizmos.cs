#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArtOfWar_II_Art.Integration.Scripts.Gizmos
{
    public enum MapGizmosType
    {
        None = 0,
        Grids = 1,
        Label = 2,
        All = 3,
    }

    [ExecuteInEditMode]
    public class MapGizmos : MonoBehaviour
    {
        [SerializeField] private int fontSize = 10;
        [SerializeField] private MapGizmosType drawType = MapGizmosType.All;

        [SerializeField] private int sizeX = 40;
        [SerializeField] private int sizeY = 40;
        [SerializeField] private Vector3 cellSize = Vector3.one;

        public void OnDrawGizmos()
        {
            switch (drawType)
            {
                case MapGizmosType.All:
                    DrawGrids();
                    DrawLabel();
                    break;
                case MapGizmosType.Grids:
                    DrawGrids();
                    break;
                case MapGizmosType.Label:
                    DrawLabel();
                    break;
            }
        }

        private void DrawGrids()
        {
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.fontSize = fontSize;

            Vector3Int mapSize = new Vector3Int(sizeX, 1, sizeY);
            (Vector3Int min, Vector3Int max) = CalcMinMax(mapSize);

            float cox = (mapSize.x % 2 == 0 ? 0.5f : 0) * cellSize.x;
            float coz = (mapSize.z % 2 == 0 ? -0.5f : 0) * cellSize.z; // 0.5f 同上, 和服务器计算规则保持一致
            Vector3 centerOffset = new Vector3(cox, 0, coz);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3 pos = CalcWorldPosition(new Vector3Int(x, 0, z), cellSize, centerOffset);
                    UnityEngine.Gizmos.DrawWireCube(pos + Vector3.up * -0.49f, cellSize);
                }
            }
        }

        private void DrawLabel()
        {
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.fontSize = fontSize;
            style.alignment = TextAnchor.MiddleRight;

            Vector3Int mapSize = new Vector3Int(sizeX, 1, sizeY);
            (Vector3Int min, Vector3Int max) = CalcMinMax(mapSize);

            float cox = (mapSize.x % 2 == 0 ? 0.5f : 0) * cellSize.x;
            float coz = (mapSize.z % 2 == 0 ? -0.5f : 0) * cellSize.z; // 0.5f 同上, 和服务器计算规则保持一致
            Vector3 centerOffset = new Vector3(cox, 0, coz);

            for (int x = min.x; x <= max.x; x++)
            {
                style.alignment = TextAnchor.MiddleCenter;
                Vector3 labelPos = CalcWorldPosition(new Vector3Int(x, 0, max.z + 1), cellSize, centerOffset);
                Handles.Label(labelPos, $"{x}", style);
            }

            for (int z = min.z; z <= max.z; z++)
            {
                style.alignment = TextAnchor.MiddleRight;
                Vector3 labelPos = CalcWorldPosition(new Vector3Int(min.x - 1, 0, z), cellSize, centerOffset);
                Handles.Label(labelPos, $"{z}", style);
            }
        }

        private static (Vector3Int, Vector3Int) CalcMinMax(Vector3Int size)
        {
            // 和服务器计算规则保持一致
            Vector3Int max = new Vector3Int((size.x - 1) / 2, 1, size.z / 2);
            Vector3Int min = new Vector3Int(-size.x / 2, 0, (-size.z + 1) / 2);
            return (min, max);
        }

        private static Vector3 CalcWorldPosition(Vector3Int p, Vector3 cellSize, Vector3 centerOffset)
        {
            float px = p.x * cellSize.x;
            float py = p.y * cellSize.y;
            float pz = p.z * cellSize.z;
            return centerOffset + new Vector3(px, py, pz);
        }
    }
}
#endif