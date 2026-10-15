using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.AreaFaction
{
    public enum Plane2D
    {
        XY,
        XZ
    }

    /// <summary>
    /// 区域计算结果
    /// </summary>
    public class AreaResult
    {
        public List<AreaCell> cells;
        public Rect bounds;
    }

    /// <summary>
    /// 区域单元格数据
    /// </summary>
    public class AreaCell
    {
        public int index; // 对应输入点的索引
        public Vector3 center; // 站点中心（3D，包含高度）
        public List<Vector2> vertices; // 单元格顶点（CCW 顺序，XZ平面）
        public float radius; // 包围半径
    }

    /// <summary>
    /// 区域计算器抽象基类 - 定义区域划分的通用接口
    /// </summary>
    public abstract class AreaCalculatorBase
    {
        protected Plane2D computePlane;
        protected float boundarySnapEpsilon;

        public void Init(Plane2D plane = Plane2D.XZ, float snapEpsilon = 0.01f)
        {
            computePlane = plane;
            boundarySnapEpsilon = snapEpsilon;
        }

        /// <summary>
        /// 计算区域划分（抽象方法，子类必须实现）
        /// </summary>
        /// <param name="positions">站点位置列表（3D）</param>
        /// <param name="boundaryPolygon">边界多边形（可选，用于裁剪）</param>
        /// <returns>区域计算结果</returns>
        public abstract AreaResult Compute(Vector3[] positions, List<Vector2> boundaryPolygon = null);

        /// <summary>
        /// 从 MeshFilter 数组构建 AABB 边界多边形
        /// </summary>
        public virtual List<Vector2> BuildBoundaryFromMeshFilters(MeshFilter[] meshFilters)
        {
            if (meshFilters == null || meshFilters.Length == 0) return null;

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            bool any = false;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter mf = meshFilters[i];
                if (!mf || !mf.sharedMesh) continue;
                any = true;
                Mesh m = mf.sharedMesh;
                Vector3[] vs = m.vertices;
                Matrix4x4 l2w = mf.transform.localToWorldMatrix;
                for (int v = 0; v < vs.Length; v++)
                {
                    Vector3 w = l2w.MultiplyPoint3x4(vs[v]);
                    Vector2 p = computePlane == Plane2D.XZ ? new Vector2(w.x, w.z) : new Vector2(w.x, w.y);
                    if (p.x < minX) minX = p.x;
                    if (p.y < minY) minY = p.y;
                    if (p.x > maxX) maxX = p.x;
                    if (p.y > maxY) maxY = p.y;
                }
            }

            if (!any || float.IsInfinity(minX)) return null;

            return new List<Vector2>
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY)
            };
        }

        #region 静态辅助方法（子类和外部可用）

        /// <summary>
        /// 计算多边形面积（带符号，正为 CCW，负为 CW）
        /// </summary>
        public static float PolygonArea(List<Vector2> poly)
        {
            double s = 0.0;
            for (int i = 0, n = poly.Count; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                s += (double) a.x * b.y - (double) a.y * b.x;
            }

            return (float) (0.5 * s);
        }

        /// <summary>
        /// 多边形内缩
        /// </summary>
        public static List<Vector2> InsetPolygon(List<Vector2> loop, float inset)
        {
            int n = loop.Count;
            if (n < 3 || inset <= 1e-7f) return new List<Vector2>(loop);
            List<Vector2> result = new List<Vector2>(n);

            for (int i = 0; i < n; i++)
            {
                Vector2 prev = loop[(i - 1 + n) % n];
                Vector2 cur = loop[i];
                Vector2 next = loop[(i + 1) % n];

                Vector2 e1 = (cur - prev);
                Vector2 e2 = (next - cur);
                if (e1.sqrMagnitude < 1e-12f || e2.sqrMagnitude < 1e-12f)
                {
                    result.Add(cur);
                    continue;
                }

                e1.Normalize();
                e2.Normalize();

                Vector2 n1 = new Vector2(-e1.y, e1.x);
                Vector2 n2 = new Vector2(-e2.y, e2.x);
                Vector2 bis = n1 + n2;

                if (bis.sqrMagnitude < 1e-12f)
                {
                    Vector2 inward = (n1 + n2) * 0.5f;
                    if (inward.sqrMagnitude < 1e-12f) inward = n1;
                    result.Add(cur + inward.normalized * inset);
                    continue;
                }

                Vector2 bisN = bis.normalized;
                float denom = Vector2.Dot(bisN, n1);
                if (Mathf.Abs(denom) < 1e-4f)
                {
                    result.Add(cur + bisN * inset);
                }
                else
                {
                    float scale = inset / denom;
                    result.Add(cur + bisN * scale);
                }
            }

            return result;
        }

        /// <summary>
        /// Chaikin 平滑（闭合多边形）
        /// </summary>
        public static List<Vector2> SmoothChaikinClosed(List<Vector2> ring, int iterations)
        {
            if (ring == null || ring.Count < 3 || iterations <= 0) return ring;
            List<Vector2> cur = new List<Vector2>(ring);

            for (int it = 0; it < iterations; it++)
            {
                int n = cur.Count;
                List<Vector2> next = new List<Vector2>(n * 2);
                for (int i = 0; i < n; i++)
                {
                    Vector2 p0 = cur[i];
                    Vector2 p1 = cur[(i + 1) % n];
                    Vector2 q = 0.75f * p0 + 0.25f * p1;
                    Vector2 r = 0.25f * p0 + 0.75f * p1;
                    next.Add(q);
                    next.Add(r);
                }

                cur = next;
            }

            return cur;
        }

        /// <summary>
        /// Chaikin 平滑（开放折线）
        /// </summary>
        public static List<Vector2> SmoothChaikinOpen(List<Vector2> pl, int iterations)
        {
            if (pl == null || pl.Count < 2 || iterations <= 0) return pl;
            List<Vector2> cur = new List<Vector2>(pl);

            for (int it = 0; it < iterations; it++)
            {
                int n = cur.Count;
                if (n < 2) break;
                List<Vector2> next = new List<Vector2>(n * 2);
                next.Add(cur[0]);
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 p0 = cur[i];
                    Vector2 p1 = cur[i + 1];
                    Vector2 q = 0.75f * p0 + 0.25f * p1;
                    Vector2 r = 0.25f * p0 + 0.75f * p1;
                    next.Add(q);
                    next.Add(r);
                }

                next.Add(cur[n - 1]);
                cur = next;
            }

            return cur;
        }

        /// <summary>
        /// 按距离重采样
        /// </summary>
        public static List<Vector2> ResampleByDistance(List<Vector2> pts, float step, bool closed)
        {
            if (pts == null || pts.Count < 2) return pts;
            List<Vector2> result = new List<Vector2>();
            float total = 0f;

            for (int i = 0; i < pts.Count - 1; i++)
                total += Vector2.Distance(pts[i], pts[i + 1]);
            if (closed)
                total += Vector2.Distance(pts[pts.Count - 1], pts[0]);

            if (total < step * 0.5f)
                return new List<Vector2>(pts);

            result.Add(pts[0]);
            float acc = 0f;

            for (int i = 0; i < (closed ? pts.Count : pts.Count - 1); i++)
            {
                Vector2 a = pts[i];
                Vector2 b = (i == pts.Count - 1) ? (closed ? pts[0] : pts[i]) : pts[i + 1];
                if (a == b) continue;
                Vector2 dir = (b - a);
                float segLen = dir.magnitude;
                dir /= segLen;
                float rem = segLen;

                while (acc + rem >= step)
                {
                    float t = step - acc;
                    Vector2 p = a + dir * t;
                    result.Add(p);
                    a = p;
                    rem = segLen - (segLen - rem + t);
                    acc = 0f;
                }

                acc += rem;
            }

            if (!closed)
            {
                Vector2 last = pts[pts.Count - 1];
                if (result[result.Count - 1] != last)
                    result.Add(last);
            }

            return result;
        }

        #endregion
    }
}