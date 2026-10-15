using UnityEngine;

namespace ArtOfWar_II_Art.Integration.Scripts
{
    public class MGEditorSetting
    {
        public Vector3Int mapSize = Vector3Int.one;
        public Vector3 centerOffset = Vector3.zero;
        public Vector3 gap = Vector3.zero;

        public Vector3 cellSize = Vector3.one;
        public Vector3 halfCellSize => cellSize * 0.5f;

        public static MGEditorSetting BuildIn()
        {
            MGEditorSetting settings = new MGEditorSetting();
            settings.mapSize = new Vector3Int(40, 1, 40);
            float cox = (settings.mapSize.x % 2 == 0 ? 0.5f : 0) * settings.cellSize.x;
            float coz = (settings.mapSize.z % 2 == 0 ? -0.5f : 0) * settings.cellSize.z; // 0.5f 同上, 和服务器计算规则保持一致
            settings.centerOffset = new Vector3(cox, 0, coz);
            return settings;
        }
    }
}