using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.AreaFaction
{
    /// <summary>
    /// 使用预定义多边形直接构建地块，用于固定版图演示。
    /// </summary>
    public class PredefinedPolygonAreaCalculator : AreaCalculatorBase
    {
        private readonly Vector2[][] polygons;

        public PredefinedPolygonAreaCalculator(Vector2[][] polygons)
        {
            this.polygons = polygons;
        }

        public override AreaResult Compute(Vector3[] positions, List<Vector2> boundaryPolygon = null)
        {
            if (polygons == null || polygons.Length == 0)
            {
                Debug.LogError("[PredefinedPolygonAreaCalculator] polygons is null or empty");
                return null;
            }

            int count = positions == null ? polygons.Length : Mathf.Min(positions.Length, polygons.Length);
            if (count == 0)
            {
                Debug.LogError("[PredefinedPolygonAreaCalculator] positions is empty");
                return null;
            }

            AreaResult result = new AreaResult
            {
                cells = new List<AreaCell>(count)
            };

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                List<Vector2> vertices = new List<Vector2>(polygons[i]);
                EnsureCounterClockwise(vertices);

                Vector2 centroid2D = ComputeCentroid(vertices);
                float height = positions != null && i < positions.Length ? positions[i].y : 0f;
                Vector3 center = computePlane == Plane2D.XZ
                    ? new Vector3(centroid2D.x, height, centroid2D.y)
                    : new Vector3(centroid2D.x, centroid2D.y, 0f);

                foreach (Vector2 vertex in vertices)
                {
                    if (vertex.x < minX) minX = vertex.x;
                    if (vertex.y < minY) minY = vertex.y;
                    if (vertex.x > maxX) maxX = vertex.x;
                    if (vertex.y > maxY) maxY = vertex.y;
                }

                result.cells.Add(new AreaCell
                {
                    index = i,
                    center = center,
                    vertices = vertices,
                    radius = CalculateRadius(centroid2D, vertices)
                });
            }

            result.bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            return result;
        }

        private static void EnsureCounterClockwise(List<Vector2> vertices)
        {
            if (vertices.Count >= 3 && PolygonArea(vertices) < 0f)
            {
                vertices.Reverse();
            }
        }

        private static Vector2 ComputeCentroid(List<Vector2> vertices)
        {
            float signedArea = 0f;
            float cx = 0f;
            float cy = 0f;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 current = vertices[i];
                Vector2 next = vertices[(i + 1) % vertices.Count];
                float cross = current.x * next.y - next.x * current.y;
                signedArea += cross;
                cx += (current.x + next.x) * cross;
                cy += (current.y + next.y) * cross;
            }

            signedArea *= 0.5f;
            if (Mathf.Abs(signedArea) < 1e-5f)
            {
                Vector2 average = Vector2.zero;
                for (int i = 0; i < vertices.Count; i++)
                {
                    average += vertices[i];
                }

                return average / Mathf.Max(vertices.Count, 1);
            }

            float factor = 1f / (6f * signedArea);
            return new Vector2(cx * factor, cy * factor);
        }

        private static float CalculateRadius(Vector2 center, List<Vector2> vertices)
        {
            float maxDistance = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                float distance = Vector2.Distance(center, vertices[i]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            return maxDistance;
        }
    }
}
