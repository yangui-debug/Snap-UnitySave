using UnityEngine;

namespace ArtOfWar_II_Art.Integration.Scripts
{
    public static class MGEditorCalc
    {
        public static Vector3 CalcPlacePosition(Vector3Int p, Vector3Int size, MGEditorSetting settings)
        {
            Vector3 p1 = CalcWorldPosition(p, settings);
            float ox = (size.x * 0.5f - 0.5f) * settings.cellSize.x;
            float oz = (-size.z * 0.5f + 0.5f) * settings.cellSize.z;
            return p1 + new Vector3(ox, 0, oz);
        }

        public static Vector3 CalcWorldPosition(Vector3Int p, MGEditorSetting settings)
        {
            Vector3 capOffset = new Vector3(p.x * settings.gap.x, p.y * settings.gap.y, p.z * settings.gap.z);

            float px = p.x * settings.cellSize.x;
            float py = p.y * settings.cellSize.y;
            float pz = p.z * settings.cellSize.z;
            return settings.centerOffset + capOffset + new Vector3(px, py, pz);
        }

        public static Vector3Int CalcHitPosition(Vector3 worldPos, MGEditorSetting settings)
        {
            worldPos -= settings.centerOffset;

            Vector3 halfCellSize = settings.halfCellSize;
            float xf = worldPos.x + (worldPos.x > 0 ? halfCellSize.x : -halfCellSize.x);
            float yf = worldPos.y + (worldPos.y > 0 ? halfCellSize.y : -halfCellSize.y);
            float zf = worldPos.z + (worldPos.z > 0 ? halfCellSize.z : -halfCellSize.z);

            int x = (int) (xf / (settings.cellSize.x + settings.gap.x));
            int y = (int) (yf / (settings.cellSize.y + settings.gap.y));
            int z = (int) (zf / (settings.cellSize.z + settings.gap.z));
            return new Vector3Int(x, y, z);
        }

        public static Vector3Int PlaceToIntPos(Vector3 placePos, Vector3Int size, MGEditorSetting setting)
        {
            placePos.x = size.x % 2 == 0 ? placePos.x + setting.cellSize.x / 2 : placePos.x;
            placePos.z = size.z % 2 == 0 ? placePos.z + setting.cellSize.z / 2 : placePos.z;

            Vector3Int p = CalcHitPosition(placePos, setting);
            p.x -= size.x / 2;
            p.z += (size.z - 1) / 2;
            return p;
        }

        public static (Vector3Int, Vector3Int) CalcMinMax(Vector3Int position, Vector3Int size)
        {
            Vector3Int min = position + new Vector3Int(0, 0, -size.z + 1);
            Vector3Int max = position + new Vector3Int(size.x - 1, size.y - 1, 0);
            return (min, max);
        }

        public static Bounds CalcBounds(Vector3Int p, Vector3Int size, MGEditorSetting settings)
        {
            Vector3Int bMin = new Vector3Int(p.x, p.y, p.z - size.z + 1);
            Vector3Int bMax = new Vector3Int(p.x + size.x - 1, p.y + size.y - 1, p.z);

            Vector3 center = CalcPlacePosition(p, size, settings);

            Vector3 half = settings.cellSize * 0.5f;
            Vector3 min = CalcWorldPosition(bMin, settings) - half;
            Vector3 max = CalcWorldPosition(bMax, settings) + half;
            return new Bounds(center, max - min);
        }

        public static Vector3 CalcWorldSize(Vector3Int size, MGEditorSetting settings)
        {
            float sx = size.x * settings.cellSize.x;
            float sy = size.y * settings.cellSize.y;
            float sz = size.z * settings.cellSize.z;

            return new Vector3(sx, sy, sz);
        }

        public static bool InBounds(Vector3Int position, Vector3Int min, Vector3Int max)
        {
            return position.x >= min.x && position.x <= max.x &&
                   position.y >= min.y && position.y <= max.y &&
                   position.z >= min.z && position.z <= max.z;
        }
    }
}