using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// 自动画线工具优化版，对象池、预设
/// </summary>
public class MultiLineRendererTo3DMesh : MonoBehaviour
{
    [Header("线条源（可以拖多个 LineRenderer 进来）")]
    public List<LineRenderer> sourceLines = new List<LineRenderer>();

    [Header("线条参数")]
    public float lineWidth = 0.5f;
    public float lineHeight = 1.0f;
    public int cornerSegments = 4;

    [Header("颜色参数")]
    public Color topColor = new Color(0.2f, 0.6f, 1.0f);
    public Color sideColor = new Color(0.1f, 0.3f, 0.5f);
    
    [Header("线条闭合选项")]
    public bool loopLines = false; // 默认不闭合
    
    [Header("材质设置")]
    public bool useManualMaterial = false; // 是否使用手动指定的材质
    public List<Material> manualMaterials = new List<Material>(); // 手动材质列表

    // 预分配的对象池
    private Queue<GameObject> meshObjectPool = new Queue<GameObject>();
    private Queue<Mesh> meshPool = new Queue<Mesh>();
    private Queue<Material[]> materialPool = new Queue<Material[]>(); // 材质实例对象池
    private const int INITIAL_POOL_SIZE = 10;
    
    // 预分配的数组和列表
    private List<Vector3> verts = new List<Vector3>(); 
    private List<Color> cols = new List<Color>();
    private List<int> topTris = new List<int>();
    private List<int> sideTris = new List<int>();
    
    // 预分配的着色器
    [Header("预分配的着色器")]
    public Shader shader;
    
    // 预分配的数组
    private Vector3[][] rightsPerPoint;
    private Vector3[] forwardSeg;
    private Vector3[] rightSeg;
    
    // lineGo公共父节点
    private Transform parentTrans;
    
    // 材质索引计数器
    private int materialIndex = 0;

    void Awake()
    {
        // 预分配着色器
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        }

        // 创建lineGo公共父节点
        if (!parentTrans)
        {
            var go = new GameObject("Root");
            parentTrans = go.transform;
            parentTrans.SetParent(transform);
        }
        
        // 预分配对象池
        InitializePools();
    }

    private void Start()
    {
        GenerateAllLines();
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePools()
    {
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            // 创建Mesh对象池
            Mesh mesh = new Mesh();
            mesh.name = "Line3DMesh_Pooled";
            meshPool.Enqueue(mesh);
            
            // 创建GameObject对象池
            GameObject lineObj = new GameObject($"Line3D_Pooled_{i}");
            lineObj.SetActive(false);
            lineObj.transform.SetParent(parentTrans, false);
            
            // 预先添加组件
            lineObj.AddComponent<MeshFilter>();
            lineObj.AddComponent<SortingGroup>().sortingOrder = 102;
            MeshRenderer mr = lineObj.AddComponent<MeshRenderer>();
            
            // 创建材质实例并添加到材质池
            Material topMat = new Material(shader);
            Material sideMat = new Material(shader);
            Material[] materials = new Material[] { topMat, sideMat };
            materialPool.Enqueue(materials);
            
            // 设置材质
            mr.materials = materials;
            
            meshObjectPool.Enqueue(lineObj);
        }
        
        // 预分配数组
        rightsPerPoint = new Vector3[100][]; // 假设最多100个点
        for (int i = 0; i < rightsPerPoint.Length; i++)
        {
            rightsPerPoint[i] = new Vector3[10]; // 假设最多10个角分段
        }
        
        forwardSeg = new Vector3[100]; // 假设最多100个线段
        rightSeg = new Vector3[100];   // 假设最多100个线段
    }

    /// <summary>
    /// 从对象池获取一个Mesh对象和对应的材质
    /// </summary>
    private (GameObject, Material[]) GetPooledMeshObject()
    {
        if (meshObjectPool.Count > 0 && materialPool.Count > 0)
        {
            GameObject obj = meshObjectPool.Dequeue();
            Material[] materials = materialPool.Dequeue();
            obj.SetActive(true);
            
            // 确保材质已设置
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            mr.materials = materials;
            
            return (obj, materials);
        }
        
        // 如果池中没有对象，创建一个新的
        GameObject newObj = new GameObject("Line3D_New");
        newObj.transform.SetParent(parentTrans, false);
        
        newObj.AddComponent<MeshFilter>();
        newObj.AddComponent<SortingGroup>().sortingOrder = 102;
        MeshRenderer mrNew = newObj.AddComponent<MeshRenderer>();
        
        // 创建新的材质实例
        Material topMat = new Material(shader);
        Material sideMat = new Material(shader);
        Material[] newMaterials = new Material[] { topMat, sideMat };
        mrNew.materials = newMaterials;
        
        return (newObj, newMaterials);
    }

    /// <summary>
    /// 从对象池获取一个Mesh
    /// </summary>
    private Mesh GetPooledMesh()
    {
        if (meshPool.Count > 0)
        {
            return meshPool.Dequeue();
        }
        
        // 如果池中没有Mesh，创建一个新的
        Mesh newMesh = new Mesh();
        newMesh.name = "Line3DMesh_New";
        return newMesh;
    }

    /// <summary>
    /// 将对象返回到对象池
    /// </summary>
    private void ReturnToPool(GameObject obj, Material[] materials)
    {
        obj.SetActive(false);
        meshObjectPool.Enqueue(obj);
        materialPool.Enqueue(materials);
    }

    /// <summary>
    /// 将Mesh返回到对象池
    /// </summary>
    private void ReturnToPool(Mesh mesh)
    {
        mesh.Clear();
        meshPool.Enqueue(mesh);
    }

    /// <summary>
    /// 清除之前生成的子物体
    /// </summary>
    public void ClearGenerated()
    {
        foreach (Transform child in parentTrans)
        {
            if (child != null && child.gameObject.activeInHierarchy)
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                
                if (mf != null && mf.sharedMesh != null)
                {
                    ReturnToPool(mf.sharedMesh);
                }
                
                if (mr != null && mr.materials != null && mr.materials.Length >= 2)
                {
                    ReturnToPool(child.gameObject, mr.materials);
                }
            }
        }
        
        // 重置材质索引
        materialIndex = 0;
    }

    public void GenerateAllLines()
    {
        ClearGenerated();

        if (sourceLines == null || sourceLines.Count == 0)
        {
            Debug.LogWarning("MultiLineRendererTo3DMesh：没有可用的 LineRenderer。");
            return;
        }

        for (int i = 0; i < sourceLines.Count; i++)
        {
            var line = sourceLines[i];
            if (line == null || line.positionCount < 2)
                continue;

            // 从对象池获取对象和材质
            var (lineObj, materials) = GetPooledMeshObject();
            lineObj.name = $"Line3D_{i}";

            MeshFilter mf = lineObj.GetComponent<MeshFilter>();
            MeshRenderer mr = lineObj.GetComponent<MeshRenderer>();

            // 处理材质
            if (useManualMaterial && manualMaterials != null && manualMaterials.Count > 0)
            {
                // 使用手动指定的材质
                ApplyManualMaterials(mr, i);
            }
            else
            {
                // 使用动态生成的材质
                ApplyDynamicMaterials(mr, line, materials);
            }

            // 从对象池获取Mesh并构建
            Mesh mesh = GetPooledMesh();
            BuildMeshFromLineRenderer(line, topColor, sideColor, loopLines, mesh);
            mf.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// 应用手动指定的材质
    /// </summary>
    private void ApplyManualMaterials(MeshRenderer mr, int lineIndex)
    {
        // 计算需要多少个材质
        int materialsNeeded = 2; // 每个线需要两个材质（顶部和侧面）
        
        // 检查是否有足够的材质
        if (materialIndex + materialsNeeded > manualMaterials.Count)
        {
            Debug.LogWarning($"手动材质数量不足。需要 {materialsNeeded} 个材质，但只有 {manualMaterials.Count - materialIndex} 个可用。");
            // 可以选择循环使用材质或使用默认材质
            // 这里我们选择循环使用
            materialIndex = 0;
        }
        
        // 获取材质
        Material[] materialsToUse = new Material[materialsNeeded];
        for (int i = 0; i < materialsNeeded; i++)
        {
            materialsToUse[i] = manualMaterials[materialIndex % manualMaterials.Count];
            materialIndex++;
        }
        
        // 应用材质
        mr.materials = materialsToUse;
    }

    /// <summary>
    /// 应用动态生成的材质
    /// </summary>
    private void ApplyDynamicMaterials(MeshRenderer mr, LineRenderer line, Material[] materials)
    {
        // 读取 LineRenderer 材质颜色
        Color lineColor = topColor;
        if (line.material != null)
        {
            if (line.material.HasProperty("_Color"))
            {
                lineColor = line.material.color;
            }
          
            if (line.material.HasProperty("_TintColor"))
            {
                lineColor = line.material.GetColor("_TintColor");
            }
        }

        Color topMatColor = lineColor; 
        Color sideMatColor = Color.Lerp(lineColor, Color.black, 0.3f);

        // 更新材质实例颜色
        if (materials.Length >= 2)
        {
            materials[0].color = topMatColor;  // 顶部材质
            materials[1].color = sideMatColor; // 侧面材质
        }
        
        // 应用材质
        mr.materials = materials;
    }

    // 修改 BuildMeshFromLineRenderer 支持复用Mesh
    private void BuildMeshFromLineRenderer(LineRenderer sourceLine, Color topColor, Color sideColor, bool loop, Mesh mesh)
    {
        int count = sourceLine.positionCount;
        if (count < 2) return;

        Vector3[] points = new Vector3[count];
        sourceLine.GetPositions(points);

        float halfWidth = lineWidth * 0.5f;
        float height = lineHeight;

        int segCount = loop ? count : count - 1;
        
        // 确保数组足够大
        if (forwardSeg.Length < segCount) forwardSeg = new Vector3[segCount];
        if (rightSeg.Length < segCount) rightSeg = new Vector3[segCount];

        for (int i = 0; i < segCount; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[(i + 1) % count];
            Vector3 f = (end - start).normalized;
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
            forwardSeg[i] = f;

            Vector3 r = Vector3.Cross(Vector3.up, f);
            if (r.sqrMagnitude < 1e-6f)
            {
                r = Vector3.Cross(f, Vector3.right);
                if (r.sqrMagnitude < 1e-6f) r = Vector3.Cross(f, Vector3.forward);
            }
            rightSeg[i] = r.normalized * halfWidth;
        }

        int K = Mathf.Max(1, cornerSegments + 1);
        
        // 清空复用列表
        verts.Clear();
        cols.Clear();
        topTris.Clear();
        sideTris.Clear();

        // 确保数组足够大
        if (rightsPerPoint.Length < count)
        {
            rightsPerPoint = new Vector3[count][];
            for (int i = 0; i < count; i++)
            {
                rightsPerPoint[i] = new Vector3[K];
            }
        }
        
        for (int i = 0; i < count; i++)
        {
            if (rightsPerPoint[i] == null || rightsPerPoint[i].Length < K)
            {
                rightsPerPoint[i] = new Vector3[K];
            }
            
            int prevIdx = (i == 0) ? (loop ? count - 1 : 0) : i - 1;
            int nextIdx = (i == count - 1) ? (loop ? 0 : count - 2) : i;

            Vector3 rPrev = rightSeg[prevIdx % segCount];
            Vector3 rNext = rightSeg[nextIdx % segCount];

            if ((rPrev - rNext).sqrMagnitude < 1e-6f)
            {
                for (int k = 0; k < K; k++) rightsPerPoint[i][k] = rPrev;
            }
            else
            {
                for (int k = 0; k < K; k++)
                {
                    float t = (float)k / (K - 1);
                    Vector3 s = Vector3.Slerp(rPrev.normalized, rNext.normalized, t) * halfWidth;
                    rightsPerPoint[i][k] = s;
                }
            }
        }

        // 顶点与颜色生成
        for (int i = 0; i < count; i++)
        {
            for (int k = 0; k < K; k++)
            {
                Vector3 r = rightsPerPoint[i][k];
                Vector3 p = points[i];

                Vector3 bottomLeft = p - r;
                Vector3 bottomRight = p + r;
                Vector3 topLeft = bottomLeft + Vector3.up * height;
                Vector3 topRight = bottomRight + Vector3.up * height;

                verts.Add(bottomLeft);
                verts.Add(bottomRight);
                verts.Add(topLeft);
                verts.Add(topRight);

                cols.Add(sideColor);
                cols.Add(sideColor);
                cols.Add(topColor);
                cols.Add(topColor);
            }
        }

        int loopCount = loop ? count : count - 1;
        for (int i = 0; i < loopCount; i++)
        {
            for (int k = 0; k < K; k++)
            {
                int aBase = ((i) * K + k) * 4;
                int bBase = (((i + 1) % count) * K + k) * 4;

                // 顶面
                topTris.Add(aBase + 2);
                topTris.Add(bBase + 2);
                topTris.Add(aBase + 3);
                topTris.Add(bBase + 2);
                topTris.Add(bBase + 3);
                topTris.Add(aBase + 3);

                // 侧面
                sideTris.Add(aBase + 1); sideTris.Add(aBase + 3); sideTris.Add(bBase + 1);
                sideTris.Add(bBase + 1); sideTris.Add(aBase + 3); sideTris.Add(bBase + 3);

                sideTris.Add(aBase + 0); sideTris.Add(bBase + 0); sideTris.Add(aBase + 2);
                sideTris.Add(bBase + 0); sideTris.Add(bBase + 2); sideTris.Add(aBase + 2);

                // sideTris.Add(aBase + 0); sideTris.Add(aBase + 1); sideTris.Add(bBase + 0);
                // sideTris.Add(bBase + 0); sideTris.Add(aBase + 1); sideTris.Add(bBase + 1);
            }
        }

        // 使用预分配的列表设置Mesh数据
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(topTris, 0);
        mesh.SetTriangles(sideTris, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        // 清理资源
        while (meshPool.Count > 0)
        {
            Mesh mesh = meshPool.Dequeue();
            if (mesh != null) Destroy(mesh);
        }
        
        while (meshObjectPool.Count > 0)
        {
            GameObject obj = meshObjectPool.Dequeue();
            if (obj != null) Destroy(obj);
        }
        
        while (materialPool.Count > 0)
        {
            Material[] materials = materialPool.Dequeue();
            foreach (Material mat in materials)
            {
                if (mat != null) Destroy(mat);
            }
        }
    }
}