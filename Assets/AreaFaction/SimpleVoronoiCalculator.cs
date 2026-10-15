using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.AreaFaction
{
    /// <summary>
    /// 简单的 Voronoi 区域计算器 - 用于测试
    /// </summary>
    public class SimpleVoronoiCalculator : AreaCalculatorBase
    {
        private const int GRID_RESOLUTION = 50;

        public override AreaResult Compute(Vector3[] positions, List<Vector2> boundaryPolygon = null)
        {
            if (positions == null || positions.Length == 0)
            {
                Debug.LogError("[SimpleVoronoiCalculator] positions is null or empty");
                return null;
            }

            AreaResult result = new AreaResult
            {
                cells = new List<AreaCell>()
            };

            Bounds bounds = CalculateBounds(positions);
            result.bounds = new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z);

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 pos = positions[i];
                List<Vector2> vertices = GenerateCellVertices(pos, positions, bounds);

                AreaCell cell = new AreaCell
                {
                    index = i,
                    center = pos,
                    vertices = vertices,
                    radius = CalculateRadius(pos, vertices)
                };

                result.cells.Add(cell);
            }

            return result;
        }

        private Bounds CalculateBounds(Vector3[] positions)
        {
            Vector3 min = positions[0];
            Vector3 max = positions[0];

            foreach (Vector3 pos in positions)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            float padding = Mathf.Max((max.x - min.x), (max.z - min.z)) * 0.1f;
            min -= new Vector3(padding, 0, padding);
            max += new Vector3(padding, 0, padding);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private List<Vector2> GenerateCellVertices(Vector3 center, Vector3[] allPositions, Bounds bounds)
        {
            List<Vector2> vertices = new List<Vector2>();
            float cellSize = Mathf.Max(bounds.size.x, bounds.size.z) / GRID_RESOLUTION;

            for (int angle = 0; angle < 360; angle += 30)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));

                float maxDist = FindVoronoiBoundary(center, dir, allPositions, bounds);
                Vector3 point = center + dir * maxDist;
                vertices.Add(new Vector2(point.x, point.z));
            }

            return vertices;
        }

        private float FindVoronoiBoundary(Vector3 center, Vector3 direction, Vector3[] allPositions, Bounds bounds)
        {
            float maxDist = Mathf.Max(bounds.size.x, bounds.size.z);
            float step = maxDist / 20f;

            for (float dist = step; dist < maxDist; dist += step)
            {
                Vector3 testPoint = center + direction * dist;

                if (!bounds.Contains(testPoint))
                    return dist - step;

                int closestIndex = FindClosestPoint(testPoint, allPositions);
                int centerIndex = FindClosestPoint(center, allPositions);

                if (closestIndex != centerIndex)
                    return dist - step;
            }

            return maxDist * 0.5f;
        }

        private int FindClosestPoint(Vector3 point, Vector3[] positions)
        {
            int closest = 0;
            float minDist = Vector3.Distance(point, positions[0]);

            for (int i = 1; i < positions.Length; i++)
            {
                float dist = Vector3.Distance(point, positions[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = i;
                }
            }

            return closest;
        }

        private float CalculateRadius(Vector3 center, List<Vector2> vertices)
        {
            float maxDist = 0f;
            Vector2 center2D = new Vector2(center.x, center.z);

            foreach (Vector2 v in vertices)
            {
                float dist = Vector2.Distance(center2D, v);
                if (dist > maxDist)
                    maxDist = dist;
            }

            return maxDist;
        }
    }
}
