using System.Collections.Generic;
using Com.i89.InstanPool;
using UnityEngine;
using UnityEngine.Rendering;

namespace Com.i89.AreaFaction
{
    /// <summary>
    /// 势力范围渲染视图 - 只负责渲染，计算逻辑由 AreaCalculatorBase 子类处理
    /// </summary>
    public class AreaFactionView : MonoBehaviour
    {
        [SerializeField] private Transform plane; // 背景Plane
        [SerializeField] private AreaFactionAssetObject assetObject;
        [SerializeField] private int areaEdgeOrder = 101;
        [SerializeField] private int areaEdge2Order = 102;
        [SerializeField] private bool disableSite; // 是否禁用区域渲染（只画线）

        public Transform container;

        // 平面选择（Unity地形通常使用XZ平面），以及在XZ平面上的高度（Y）
        public Plane2D computePlane = Plane2D.XZ;
        public float xzHeight = 0f;
        public float fillHeightOffset = 0.1f; // 势力填充mesh抬高，避免与地形Z-Fighting（可按地形高度调大）
        public bool uvNormalizeToBounds = true; // UV按多边形包围盒归一化到0-1
        public float boundarySnapEpsilon = 0.01f; // 合并不规则边界时的点吸附阈值（世界单位）

        // 轮廓绘制
        public bool drawOutlines = true;
        public float outlineInset = 0.05f; // 第一条描边(areaEdge)向内缩的距离
        public float outlineInset2 = 0.05f; // 第二条描边(areaEdge2)向内缩的距离
        public float outlineHeightOffset = 0.11f; // 轮廓抬高避免与填充重叠
        public bool outlineSmooth = true; // 是否对轮廓折线进行平滑
        public int outlineSmoothIterations = 2; // 平滑迭代次数（Chaikin）
        public float outlineResampleSegmentLength = 0.2f; // 平滑后按该步长重采样，保证顶点密度均匀

        [Tooltip("最小段长 = max(重采样步长, 该系数×线宽)，保证粗线在密点处不溢出；保持线宽时建议 0.5")]
        public float outlineResampleMinFractionOfWidth = 0.5f;

        public int outlineCornerVertices = 4; // LineRenderer拐角细分
        public int outlineCapVertices = 2; // LineRenderer端点细分

        // 直线段简化（仅移除直线段多余点，拐点保持不变，并可在拐点两侧保留N个点）
        public bool simplifyStraightSegments = true; // 是否启用直线段简化
        public float simplifyCornerAngleDeg = 8f; // 将顶点视为拐点的最小转角（度）
        public float simplifyStraightAngleDeg = 1.5f; // 视为直线的小转角阈值（度）
        public int simplifyKeepPointsEachSide = 1; // 拐点两侧需要保留的点数

        // 资产导出
        public bool saveAsAssets; // 是否在Build后保存为Unity资产

        // 区域计算器（可替换为不同的实现）
        private AreaCalculatorBase calculator;
        private AreaResult areaResult;

        // 每阵营+高度的合并Mesh缓存与签名（用于轮廓与资产导出）
        private Dictionary<Vector2Int, Mesh> cachedCombinedPerFactionHeight;
        private Dictionary<Vector2Int, ulong> combinedHashPerFactionHeight;

        private Dictionary<Vector2Int, List<Mesh>> factionHeightToMeshes;

        // CullingGroup相关缓存
        private CullingGroup cullingGroup;
        private BoundingSphere[] cullingSpheres;

        // 站点数据：用于剔除与延迟实例化
        private class SiteData
        {
            public int index;
            public int beforeFaction;
            public int faction;
            public Mesh mesh;
            public MeshRenderer meshRenderer;
            public GameObject go;
            public Vector3 center;
            public float radius;
        }

        private List<SiteData> sites;
        private readonly List<SiteData> buildChanges = new(); // 本次build变化的数据
        private readonly HashSet<int> factionChanges = new(); // 本次build变化的阵营
        private readonly Dictionary<int, SimpleInstantiatePool<LineRenderer>> factionLinePool = new(); // 阵营对应的LineRenderer对象池

        // 动画状态管理
        private readonly HashSet<int> tilesWaitingIndex = new(); // 等待动画完成的地块索引
        private readonly Dictionary<int, int> pendingInvasionSources = new(); // 目标地块 -> 进攻地块
        private AreaFactionInvasionAnimator invasionAnimator;

        // 外部控制：退出（释放）剔除系统
        public void Exit()
        {
            DisposeCulling();
        }

        /// <summary>
        /// 设置侵蚀动画管理器
        /// </summary>
        public void SetInvasionAnimator(AreaFactionInvasionAnimator animator)
        {
            invasionAnimator = animator;
            if (invasionAnimator)
            {
                invasionAnimator.Init(this);
            }
        }

        /// <summary>
        /// 设置自定义的区域计算器（必须在 Build 之前调用）
        /// </summary>
        public void SetCalculator(AreaCalculatorBase customCalculator)
        {
            calculator = customCalculator;
        }

        public void SetPlaneSize(float x, float z)
        {
            if (plane)
            {
                plane.transform.localScale = new Vector3(x * 0.1f, 1, z * 0.1f);
            }
        }

        /// <summary>
        /// 构建&刷新整个范围（首次调用时计算区域，后续调用只更新 faction）
        /// </summary>
        public void Build(Vector3[] posList, int[] factionList, MeshFilter[] boundaryMeshFilters, Camera sceneCamera)
        {
            buildChanges.Clear();
            factionChanges.Clear();

            if (calculator == null)
            {
                Debug.LogError("[AreaFactionView] calculator is null");
                return;
            }

            // 首次构建时计算区域
            if (sites == null)
            {
                calculator.Init(computePlane, boundarySnapEpsilon);
                List<Vector2> boundary = calculator.BuildBoundaryFromMeshFilters(boundaryMeshFilters);
                areaResult = calculator.Compute(posList, boundary);
                InitSitesFromResult(areaResult);
            }

            cullingGroup ??= CreateCulling(sceneCamera);

            // 更新阵营并渲染
            ApplyFactionsAndRender(factionList);
        }

        /// <summary>
        /// 完全重建视图（当 posList 变化时调用）
        /// </summary>
        public void Rebuild(Vector3[] posList, int[] factionList, MeshFilter[] boundaryMeshFilters, Camera sceneCamera)
        {
            buildChanges.Clear();
            factionChanges.Clear();

            if (calculator == null)
            {
                Debug.LogError("[AreaFactionView] calculator is null");
                return;
            }

            // 清理旧数据（保留 calculator 和 container）
            ClearSitesAndCulling();

            // 重新计算区域
            calculator.Init(computePlane, boundarySnapEpsilon);
            List<Vector2> boundary = calculator.BuildBoundaryFromMeshFilters(boundaryMeshFilters);
            areaResult = calculator.Compute(posList, boundary);

            // 重新初始化
            InitSitesFromResult(areaResult);

            cullingGroup ??= CreateCulling(sceneCamera);

            // 更新阵营并渲染
            ApplyFactionsAndRender(factionList);
        }

        /// <summary>
        /// 应用阵营并渲染（Build 和 Rebuild 共用的渲染逻辑）
        /// </summary>
        private void ApplyFactionsAndRender(int[] factionList)
        {
            // 更新阵营并收集变化
            List<SiteData> changes = UpdateFactions(factionList);

            // 如果是初始化，处理所有变化的地块；否则只处理需要重新build的地块
            bool isInitializing = sites == null;
            foreach (SiteData change in changes)
            {
                factionChanges.Add(change.beforeFaction);
                factionChanges.Add(change.faction);
                // 不用 culling 做首次创建门控：Culling 首帧尚未更新，会漏创建
                // GO 始终创建，culling 通过 OnCullingStateChanged 控制 renderer.enabled
                EnsureSiteGoVisible(change);
            }

            // 初始化时重置动画状态
            if (isInitializing)
            {
                tilesWaitingIndex.Clear();
            }

            Dictionary<Vector2Int, Mesh> combinedPerFactionHeight = null;
            if (drawOutlines)
            {
                // 更新轮廓（按阵营+高度分组）
                FactionHeightToMeshes(factionChanges);
                combinedPerFactionHeight = EmitFactionHeightMeshes(factionHeightToMeshes);
                EmitFactionHeightOutlinesFromCombined(combinedPerFactionHeight);
            }

            SaveGeneratedAssetsIfEnabled(combinedPerFactionHeight);
        }

        public void SetViewActive(bool active)
        {
            if (container)
            {
                container.gameObject.SetActive(active);
            }
        }

        public void Release()
        {
            DisposeCulling();
            DestroyAllObjects();
            cullingSpheres = null;
            areaResult = null;
        }

        private void OnDestroy()
        {
            Release();
        }

        #region 初始化

        private void InitSitesFromResult(AreaResult result)
        {
            sites = new List<SiteData>(result.cells.Count);
            cullingSpheres = new BoundingSphere[result.cells.Count];

            for (int i = 0; i < result.cells.Count; i++)
            {
                AreaCell cell = result.cells[i];

                Mesh mesh = null;
                if (cell.vertices != null && cell.vertices.Count >= 3)
                {
                    mesh = CreateCellMesh(cell.vertices, cell.center.y);
                }

                SiteData site = new SiteData
                {
                    index = i,
                    center = cell.center,
                    radius = cell.radius,
                    mesh = mesh,
                    faction = -1,
                    beforeFaction = -1
                };
                sites.Add(site);

                cullingSpheres[i] = new BoundingSphere
                {
                    position = cell.center,
                    radius = cell.radius
                };
            }
        }

        private List<SiteData> UpdateFactions(int[] factionList)
        {
            for (int i = 0; i < factionList.Length && i < sites.Count; i++)
            {
                SiteData site = sites[i];
                if (site.faction != factionList[i] && !tilesWaitingIndex.Contains(site.index))
                {
                    site.beforeFaction = site.faction;
                    site.faction = factionList[i];
                    buildChanges.Add(site);
                }
            }

            return buildChanges;
        }

        /// <summary>
        /// 标记地块等待动画完成（使用index）
        /// </summary>
        public void MarkTileWaitingAnimation(int tileIndex)
        {
            tilesWaitingIndex.Add(tileIndex);
        }

        /// <summary>
        /// 移除地块的动画等待标记（使用index）
        /// </summary>
        public void RemoveTileWaitingAnimation(int tileIndex)
        {
            tilesWaitingIndex.Remove(tileIndex);
        }

        /// <summary>
        /// 指定下次地块切换时的进攻来源，用于决定动画从哪条共享边开始推进。
        /// </summary>
        public void SetPendingInvasionSource(int tileIndex, int sourceTileIndex)
        {
            pendingInvasionSources[tileIndex] = sourceTileIndex;
        }

        #endregion

        #region Culling

        /// <summary>
        /// 清理 sites 和 culling 数据（保留 calculator、container、factionLinePool）
        /// 用于 Rebuild 时重建数据
        /// </summary>
        private void ClearSitesAndCulling()
        {
            // 清理 CullingGroup
            DisposeCulling();

            // 清理 sites（销毁 mesh 和 go）
            if (sites != null)
            {
                for (int i = 0; i < sites.Count; i++)
                {
                    if (sites[i].mesh)
                    {
                        Destroy(sites[i].mesh);
                    }

                    if (sites[i].go)
                    {
                        Destroy(sites[i].go);
                    }
                }

                sites.Clear();
                sites = null;
            }

            // 清理 cullingSpheres
            cullingSpheres = null;

            // 清理 cachedCombinedPerFactionHeight（需要重新合并 mesh）
            if (cachedCombinedPerFactionHeight != null)
            {
                foreach (Mesh mesh in cachedCombinedPerFactionHeight.Values)
                {
                    if (mesh)
                    {
                        Destroy(mesh);
                    }
                }

                cachedCombinedPerFactionHeight.Clear();
            }

            // 清理 combinedHashPerFactionHeight
            combinedHashPerFactionHeight?.Clear();

            // 清理 areaResult
            areaResult = null;

            // 回收 factionLinePool 中的 LineRenderer（不销毁，复用）
            foreach (SimpleInstantiatePool<LineRenderer> pool in factionLinePool.Values)
            {
                pool.HideAll();
            }
        }

        private CullingGroup CreateCulling(Camera sceneCamera)
        {
            DisposeCulling();

            cullingGroup = new CullingGroup { targetCamera = sceneCamera };
            cullingGroup.SetBoundingSpheres(cullingSpheres);
            cullingGroup.SetBoundingSphereCount(cullingSpheres.Length);
            cullingGroup.onStateChanged += OnCullingStateChanged;
            return cullingGroup;
        }

        private void DisposeCulling()
        {
            if (cullingGroup != null)
            {
                cullingGroup.onStateChanged -= OnCullingStateChanged;
                cullingGroup.Dispose();
                cullingGroup = null;
            }
        }

        private void OnCullingStateChanged(CullingGroupEvent ev)
        {
            int idx = ev.index;
            if (idx < 0 || sites == null || idx >= sites.Count) return;
            SiteData sd = sites[idx];
            bool visible = ev.isVisible;
            if (visible)
            {
                EnsureSiteGoVisible(sd);
            }
            else
            {
                if (sd.meshRenderer)
                {
                    sd.meshRenderer.enabled = false;
                }
            }
        }

        #endregion

        #region Mesh 生成与渲染

        private Mesh CreateCellMesh(List<Vector2> vertices, float cellHeight)
        {
            // 保证顶点为CCW
            if (vertices.Count >= 3 && AreaCalculatorBase.PolygonArea(vertices) < 0f)
            {
                vertices = new List<Vector2>(vertices);
                vertices.Reverse();
            }

            List<Vector3> verts = new List<Vector3>(vertices.Count);
            List<Vector2> uvs = new List<Vector2>(vertices.Count);

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

            if (uvNormalizeToBounds)
            {
                foreach (Vector2 v in vertices)
                {
                    if (v.x < minX) minX = v.x;
                    if (v.y < minY) minY = v.y;
                    if (v.x > maxX) maxX = v.x;
                    if (v.y > maxY) maxY = v.y;
                }
            }

            float height = xzHeight + cellHeight + (computePlane == Plane2D.XZ ? fillHeightOffset : 0f);

            foreach (Vector2 v in vertices)
            {
                if (computePlane == Plane2D.XZ)
                {
                    verts.Add(new Vector3(v.x, height, v.y));
                }
                else
                {
                    verts.Add(new Vector3(v.x, v.y, 0f));
                }

                if (uvNormalizeToBounds)
                {
                    float denomX = Mathf.Max(1e-6f, maxX - minX);
                    float denomY = Mathf.Max(1e-6f, maxY - minY);
                    uvs.Add(new Vector2((v.x - minX) / denomX, (v.y - minY) / denomY));
                }
                else
                {
                    uvs.Add(new Vector2(v.x, v.y));
                }
            }

            List<int> idx = new List<int>((vertices.Count - 2) * 3);
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                // XZ 平面的顶视渲染需要让三角形正面朝 +Y，否则会被默认背面剔除裁掉。
                if (computePlane == Plane2D.XZ)
                {
                    idx.Add(0);
                    idx.Add(i + 1);
                    idx.Add(i);
                }
                else
                {
                    idx.Add(0);
                    idx.Add(i);
                    idx.Add(i + 1);
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(idx, 0);
            m.RecalculateBounds();
            return m;
        }

        private void EnsureSiteGoVisible(SiteData sd)
        {
            if (disableSite) return; // 禁用区域填充渲染
            if (sd == null || sd.mesh == null) return;
            if (!sd.go)
            {
                sd.go = new GameObject($"faction_{sd.faction}_site_{sd.index}");
                if (container) sd.go.transform.SetParent(container, false);

                sd.meshRenderer = sd.go.AddComponent<MeshRenderer>();
                MeshFilter mf = sd.go.AddComponent<MeshFilter>();
                mf.sharedMesh = sd.mesh;
            }

            if (sd.meshRenderer) sd.meshRenderer.enabled = true;

            // 检查是否需要播放侵蚀动画
            if (invasionAnimator && sd.beforeFaction >= 0 && sd.beforeFaction != sd.faction)
            {
                int sourceSiteIndex = -1;
                if (pendingInvasionSources.TryGetValue(sd.index, out int pendingSource))
                {
                    sourceSiteIndex = pendingSource;
                    pendingInvasionSources.Remove(sd.index);
                }

                invasionAnimator.StartInvasion(sd.index, sd.beforeFaction, sd.faction, sd.meshRenderer, sourceSiteIndex);
            }
            else
            {
                // 直接切换材质（无动画）
                FactionPair factionPair = assetObject.GetFactionPair(sd.faction);
                Material desiredMat = factionPair?.areaMat;
                if (sd.meshRenderer.sharedMaterial != desiredMat) sd.meshRenderer.sharedMaterial = desiredMat;
            }
        }

        #endregion

        #region 阵营 Mesh 合并与轮廓

        private void FactionHeightToMeshes(HashSet<int> changes)
        {
            factionHeightToMeshes ??= new Dictionary<Vector2Int, List<Mesh>>();
            factionHeightToMeshes.Clear();

            foreach (SiteData site in sites)
            {
                if (!changes.Contains(site.faction))
                {
                    continue;
                }

                // 按整数部分分组（向下取整）
                int groupHeight = Mathf.FloorToInt(site.center.y);
                var key = new Vector2Int(site.faction, groupHeight);
                if (!factionHeightToMeshes.TryGetValue(key, out List<Mesh> list))
                {
                    list = new List<Mesh>();
                    factionHeightToMeshes[key] = list;
                }

                list.Add(site.mesh);
            }
        }

        private Dictionary<Vector2Int, Mesh> EmitFactionHeightMeshes(Dictionary<Vector2Int, List<Mesh>> factionHeightToMeshes)
        {
            var result = new Dictionary<Vector2Int, Mesh>(factionHeightToMeshes.Count);

            cachedCombinedPerFactionHeight ??= new Dictionary<Vector2Int, Mesh>();
            combinedHashPerFactionHeight ??= new Dictionary<Vector2Int, ulong>();

            foreach (var kv in factionHeightToMeshes)
            {
                var key = kv.Key;
                List<Mesh> meshes = kv.Value;
                if (meshes == null || meshes.Count == 0) continue;

                ulong sig = ComputeMeshesSignature(meshes);
                if (combinedHashPerFactionHeight.TryGetValue(key, out ulong prevSig) &&
                    prevSig == sig &&
                    cachedCombinedPerFactionHeight.TryGetValue(key, out Mesh cachedCombined) &&
                    cachedCombined)
                {
                    result[key] = cachedCombined;
                }
                else
                {
                    CombineInstance[] combines = new CombineInstance[meshes.Count];
                    for (int m = 0; m < meshes.Count; m++)
                        combines[m] = new CombineInstance { mesh = meshes[m], transform = Matrix4x4.identity };

                    if (!cachedCombinedPerFactionHeight.TryGetValue(key, out Mesh combined) || !combined)
                    {
                        combined = new Mesh();
                        cachedCombinedPerFactionHeight[key] = combined;
                    }
                    else
                    {
                        combined.Clear();
                    }

                    combined.indexFormat = IndexFormat.UInt32;
                    combined.CombineMeshes(combines, true, true, false);
                    combinedHashPerFactionHeight[key] = sig;
                    result[key] = combined;
                }
            }

            return result;
        }

        private LineRenderer linePrefab;

        private void EmitFactionHeightOutlinesFromCombined(Dictionary<Vector2Int, Mesh> combinedPerFactionHeight)
        {
            HashSet<int> processedFactions = new HashSet<int>();

            foreach (var kv in combinedPerFactionHeight)
            {
                int faction = kv.Key.x;
                float height = kv.Key.y;
                Mesh mesh = kv.Value;
                if (!mesh) continue;

                List<List<Vector2>> loops = GetBoundaryLoopsFromMesh(mesh);
                bool hasLoops = loops != null && loops.Count > 0;
                List<List<Vector2>> polylines = hasLoops ? null : GetOpenPolylinesFromMesh(mesh);
                bool hasPolylines = polylines != null && polylines.Count > 0;
                if (!hasLoops && !hasPolylines) continue;

                if (!factionLinePool.TryGetValue(faction, out SimpleInstantiatePool<LineRenderer> pool))
                {
                    if (!linePrefab)
                    {
                        linePrefab = new GameObject("LineRendererPrefab").AddComponent<LineRenderer>();
                        linePrefab.positionCount = 0;
                        linePrefab.transform.SetParent(container);
                    }

                    factionLinePool[faction] = pool = new SimpleInstantiatePool<LineRenderer>(linePrefab);
                }

                if (!processedFactions.Contains(faction))
                {
                    pool.HideAll();
                    processedFactions.Add(faction);
                }

                FactionPair fp = assetObject.GetFactionPair(faction);
                Material areaEdgeMat = fp?.areaEdgeMat;
                float areaEdgeWidth = fp?.areaEdgeWidth ?? 0f;
                Material areaEdge2Mat = fp?.areaEdge2Mat;
                float areaEdge2Width = fp?.areaEdge2Width ?? 0f;

                if (hasLoops)
                {
                    for (int i = 0; i < loops.Count; i++)
                    {
                        List<Vector2> loop = loops[i];
                        if (loop == null || loop.Count < 3) continue;
                        if (AreaCalculatorBase.PolygonArea(loop) < 0f) loop.Reverse();
                        EmitOutlinePair(pool, $"outline_faction_{faction}_h{height}_{i}", loop, true,
                            areaEdgeMat, areaEdgeWidth, areaEdge2Mat, areaEdge2Width, height);
                    }
                }
                else
                {
                    for (int i = 0; i < polylines.Count; i++)
                    {
                        List<Vector2> pl = polylines[i];
                        if (pl == null || pl.Count < 2) continue;
                        EmitOutlinePair(pool, $"outline_faction_{faction}_h{height}_open_{i}", pl, false,
                            areaEdgeMat, areaEdgeWidth, areaEdge2Mat, areaEdge2Width, height);
                    }
                }
            }
        }

        /// <summary>
        /// 对一条轮廓（闭环或开环）做内缩、平滑后生成两条描边 LineRenderer。
        /// </summary>
        private void EmitOutlinePair(SimpleInstantiatePool<LineRenderer> pool, string namePrefix, List<Vector2> pts, bool isClosed,
            Material areaEdgeMat, float areaEdgeWidth, Material areaEdge2Mat, float areaEdge2Width, float height)
        {
            float minEdge = GetMinEdgeLength(pts, isClosed);
            float effInset = ClampInsetByMinEdge(Mathf.Max(0f, outlineInset), minEdge);
            float effInset2 = ClampInsetByMinEdge(Mathf.Max(0f, outlineInset2), minEdge);

            List<Vector2> inset = isClosed
                ? AreaCalculatorBase.InsetPolygon(pts, effInset)
                : InsetOpenPolyline(pts, effInset);
            List<Vector2> inset2 = isClosed
                ? AreaCalculatorBase.InsetPolygon(pts, effInset2)
                : InsetOpenPolyline(pts, effInset2);

            int minPts = isClosed ? 3 : 2;
            if (inset == null || inset.Count < minPts) inset = pts;
            if (inset2 == null || inset2.Count < minPts) inset2 = pts;

            List<Vector2> smoothed = ApplyOutlinePostProcess(inset, isClosed, areaEdgeWidth);
            List<Vector2> smoothed2 = ApplyOutlinePostProcess(inset2, isClosed, areaEdge2Width);

            CreateLineRendererGO(pool, namePrefix, smoothed, isClosed, areaEdgeMat, areaEdgeWidth, areaEdgeOrder, height);
            CreateLineRendererGO(pool, namePrefix + "_2", smoothed2, isClosed, areaEdge2Mat, areaEdge2Width, areaEdge2Order, height);
        }

        private void CreateLineRendererGO(SimpleInstantiatePool<LineRenderer> pool, string name, List<Vector2> pts, bool loop, Material mat, float size,
            int order, float cellHeight)
        {
            LineRenderer lr = pool.InstantiateOne(container);

            lr.name = name;
            lr.alignment = LineAlignment.TransformZ;
            lr.textureMode = LineTextureMode.Tile;
            lr.numCornerVertices = Mathf.Max(0, outlineCornerVertices);
            lr.numCapVertices = Mathf.Max(0, outlineCapVertices);
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.transform.eulerAngles = new Vector3(90, 0, 0);
            lr.loop = loop;
            lr.sortingOrder = order;
            lr.widthMultiplier = size;
            lr.sharedMaterial = mat;

            int count = pts.Count;
            lr.positionCount = count;

            if (computePlane == Plane2D.XZ)
            {
                for (int k = 0; k < count; k++)
                {
                    Vector2 v = pts[k];
                    lr.SetPosition(k, new Vector3(v.x, xzHeight + cellHeight + outlineHeightOffset, v.y));
                }
            }
            else
            {
                for (int k = 0; k < count; k++)
                {
                    Vector2 v = pts[k];
                    lr.SetPosition(k, new Vector3(v.x, v.y, 0f));
                }
            }
        }

        private List<Vector2> ApplyOutlinePostProcess(List<Vector2> pts, bool closed, float maxLineWidth = 0f)
        {
            List<Vector2> result = pts;

            // 按步长重采样；若传入线宽则保证最小段长 >= 系数×线宽，避免粗线在密点处溢出
            float segmentLength = Mathf.Max(1e-6f, outlineResampleSegmentLength);
            if (maxLineWidth > 1e-6f && outlineResampleMinFractionOfWidth > 1e-6f)
            {
                segmentLength = Mathf.Max(segmentLength, outlineResampleMinFractionOfWidth * maxLineWidth);
            }

            result = AreaCalculatorBase.ResampleByDistance(result, segmentLength, closed);
            if (simplifyStraightSegments)
            {
                result = SimplifyStraightSegments(result, closed, simplifyCornerAngleDeg, simplifyStraightAngleDeg, Mathf.Max(0, simplifyKeepPointsEachSide));
            }

            if (outlineSmooth)
            {
                result = closed
                    ? AreaCalculatorBase.SmoothChaikinClosed(result, Mathf.Max(0, outlineSmoothIterations))
                    : AreaCalculatorBase.SmoothChaikinOpen(result, Mathf.Max(0, outlineSmoothIterations));
            }

            return result;
        }

        #endregion

        #region 轮廓处理辅助方法

        /// <summary>
        /// 计算多边形/折线的最短边长，用于自适应内缩，避免点间距过小时描边溢出。
        /// </summary>
        private static float GetMinEdgeLength(List<Vector2> pts, bool closed)
        {
            if (pts == null || pts.Count < 2) return 0f;
            int n = pts.Count;
            float minLen = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                int j = closed ? (i + 1) % n : i + 1;
                if (j >= n) break;
                float len = Vector2.Distance(pts[i], pts[j]);
                if (len < minLen) minLen = len;
            }

            return minLen == float.MaxValue ? 0f : minLen;
        }

        /// <summary>
        /// 根据最短边长限制内缩距离，防止短边时 InsetPolygon 交点外飘导致描边溢出。
        /// </summary>
        private static float ClampInsetByMinEdge(float requestedInset, float minEdgeLength, float maxFractionOfEdge = 0.4f)
        {
            if (minEdgeLength <= 1e-6f) return 0f;
            float maxSafe = maxFractionOfEdge * minEdgeLength;
            return Mathf.Min(requestedInset, Mathf.Max(0f, maxSafe));
        }

        private static List<Vector2> SimplifyStraightSegments(List<Vector2> pts, bool closed, float cornerAngleDeg, float straightAngleDeg, int keepEachSide)
        {
            if (pts == null || pts.Count <= 2) return pts;
            int n = pts.Count;
            List<Vector2> result = new List<Vector2>(n);

            bool IsCorner(int i)
            {
                int i0 = (i - 1 + n) % n;
                int i1 = i;
                int i2 = (i + 1) % n;
                if (!closed && (i == 0 || i == n - 1)) return true;

                Vector2 a = pts[i0];
                Vector2 b = pts[i1];
                Vector2 c = pts[i2];
                Vector2 v1 = (b - a);
                Vector2 v2 = (c - b);
                if (v1.sqrMagnitude < 1e-12f || v2.sqrMagnitude < 1e-12f) return false;
                v1.Normalize();
                v2.Normalize();
                float dot = Mathf.Clamp(Vector2.Dot(v1, v2), -1f, 1f);
                float ang = Mathf.Acos(dot) * Mathf.Rad2Deg;
                return ang >= Mathf.Max(0f, cornerAngleDeg);
            }

            bool IsStraight(int i)
            {
                int i0 = (i - 1 + n) % n;
                int i1 = i;
                int i2 = (i + 1) % n;
                if (!closed && (i == 0 || i == n - 1)) return false;

                Vector2 a = pts[i0];
                Vector2 b = pts[i1];
                Vector2 c = pts[i2];
                Vector2 v1 = (b - a);
                Vector2 v2 = (c - b);
                if (v1.sqrMagnitude < 1e-12f || v2.sqrMagnitude < 1e-12f) return false;
                v1.Normalize();
                v2.Normalize();
                float dot = Mathf.Clamp(Vector2.Dot(v1, v2), -1f, 1f);
                float ang = Mathf.Acos(dot) * Mathf.Rad2Deg;
                return ang <= Mathf.Max(0f, straightAngleDeg);
            }

            bool[] isCorner = new bool[n];
            for (int i = 0; i < n; i++) isCorner[i] = IsCorner(i);

            int protect = Mathf.Max(0, keepEachSide);
            bool[] protectedIdx = new bool[n];
            if (protect > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (!isCorner[i]) continue;
                    for (int k = -protect; k <= protect; k++)
                    {
                        int j = i + k;
                        if (closed)
                        {
                            j = (j % n + n) % n;
                            protectedIdx[j] = true;
                        }
                        else
                        {
                            if (j >= 0 && j < n) protectedIdx[j] = true;
                        }
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (isCorner[i])
                {
                    result.Add(pts[i]);
                }
                else
                {
                    bool keep = protectedIdx[i] || !IsStraight(i);
                    if (keep) result.Add(pts[i]);
                }
            }

            if (closed)
            {
                if (result.Count >= 2 && result[0] == result[^1]) result.RemoveAt(result.Count - 1);
                if (result.Count < 3) return new List<Vector2>(pts);
            }
            else
            {
                if (result.Count < 2) return new List<Vector2>(pts);
            }

            return result;
        }

        private static List<Vector2> InsetOpenPolyline(List<Vector2> pl, float inset)
        {
            if (pl == null || pl.Count < 2 || inset <= 1e-7f) return pl;
            List<Vector2> outPts = new List<Vector2>(pl.Count);
            for (int i = 0; i < pl.Count - 1; i++)
            {
                Vector2 a = pl[i];
                Vector2 b = pl[i + 1];
                Vector2 dir = (b - a).normalized;
                Vector2 inward = new Vector2(-dir.y, dir.x);
                if (i == 0) outPts.Add(a + inward * inset);
                outPts.Add(b + inward * inset);
            }

            return outPts;
        }

        #endregion

        #region 从 Mesh 提取边界

        private List<List<Vector2>> GetBoundaryLoopsFromMesh(Mesh combined)
        {
            List<List<Vector2>> loops = new List<List<Vector2>>();
            if (!combined) return loops;
            Vector3[] verts3 = combined.vertices;
            int[] tris = combined.triangles;
            if (verts3 == null || tris == null || tris.Length == 0) return loops;
            float eps = Mathf.Max(1e-6f, boundarySnapEpsilon);

            Dictionary<(int, int), int> quantToNew = new Dictionary<(int, int), int>(verts3.Length);
            List<Vector2> welded = new List<Vector2>();
            int[] oldToNew = new int[verts3.Length];
            for (int i = 0; i < verts3.Length; i++)
            {
                Vector3 v = verts3[i];
                Vector2 p = computePlane == Plane2D.XZ ? new Vector2(v.x, v.z) : new Vector2(v.x, v.y);
                int qx = Mathf.RoundToInt(p.x / eps);
                int qy = Mathf.RoundToInt(p.y / eps);
                (int, int) key = (qx, qy);
                if (!quantToNew.TryGetValue(key, out int ni))
                {
                    ni = welded.Count;
                    quantToNew[key] = ni;
                    welded.Add(p);
                }

                oldToNew[i] = ni;
            }

            List<int> newTris = new List<int>(tris.Length);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a0 = oldToNew[tris[i]], b0 = oldToNew[tris[i + 1]], c0 = oldToNew[tris[i + 2]];
                if (a0 == b0 || b0 == c0 || c0 == a0) continue;
                Vector2 pa = welded[a0], pb = welded[b0], pc = welded[c0];
                float area2 = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
                if (Mathf.Abs(area2) <= eps * eps) continue;
                newTris.Add(a0);
                newTris.Add(b0);
                newTris.Add(c0);
            }

            if (newTris.Count == 0) return loops;

            Dictionary<(int, int), int> edgeCount = new Dictionary<(int, int), int>(newTris.Count);

            void AddEdge(int i0, int i1)
            {
                if (i0 > i1) (i0, i1) = (i1, i0);
                if (edgeCount.TryGetValue((i0, i1), out int c)) edgeCount[(i0, i1)] = c + 1;
                else edgeCount[(i0, i1)] = 1;
            }

            for (int i = 0; i < newTris.Count; i += 3)
            {
                int a = newTris[i], b = newTris[i + 1], c = newTris[i + 2];
                AddEdge(a, b);
                AddEdge(b, c);
                AddEdge(c, a);
            }

            List<(int a, int b)> boundaryEdgesIdx = new List<(int a, int b)>();
            for (int i = 0; i < newTris.Count; i += 3)
            {
                int a = newTris[i], b = newTris[i + 1], c = newTris[i + 2];

                void TryAdd(int u, int v)
                {
                    int x = u, y = v;
                    if (x > y) (x, y) = (y, x);
                    if (edgeCount[(x, y)] == 1) boundaryEdgesIdx.Add((u, v));
                }

                TryAdd(a, b);
                TryAdd(b, c);
                TryAdd(c, a);
            }

            Dictionary<int, List<int>> nexts = new Dictionary<int, List<int>>();
            for (int i = 0; i < boundaryEdgesIdx.Count; i++)
            {
                int a = boundaryEdgesIdx[i].a, b = boundaryEdgesIdx[i].b;
                if (!nexts.TryGetValue(a, out List<int> listA))
                {
                    listA = new List<int>();
                    nexts[a] = listA;
                }

                listA.Add(b);
                if (!nexts.TryGetValue(b, out List<int> listB))
                {
                    listB = new List<int>();
                    nexts[b] = listB;
                }

                listB.Add(a);
            }

            HashSet<(int, int)> used = new HashSet<(int, int)>();
            foreach (var kvp in nexts)
            {
                int start = kvp.Key;
                foreach (int nb in kvp.Value)
                {
                    if (used.Contains((start, nb)) || used.Contains((nb, start))) continue;
                    List<int> loopIdx = new List<int> { start };
                    int prev = start, cur = nb;
                    used.Add((prev, cur));
                    int guard = 0, guardMax = boundaryEdgesIdx.Count * 4 + 8;
                    while (guard++ < guardMax)
                    {
                        loopIdx.Add(cur);
                        if (cur == start) break;

                        int next = -1;
                        if (nexts.TryGetValue(cur, out List<int> outs))
                        {
                            for (int k = 0; k < outs.Count; k++)
                            {
                                int cand = outs[k];
                                if (cand == prev) continue;
                                if (used.Contains((cur, cand)) || used.Contains((cand, cur))) continue;
                                next = cand;
                                break;
                            }

                            if (next == -1)
                            {
                                for (int k = 0; k < outs.Count; k++)
                                {
                                    int cand = outs[k];
                                    if (used.Contains((cur, cand)) || used.Contains((cand, cur))) continue;
                                    next = cand;
                                    break;
                                }
                            }
                        }

                        if (next == -1) break;
                        used.Add((cur, next));
                        prev = cur;
                        cur = next;
                    }

                    if (loopIdx.Count >= 3 && loopIdx[0] == loopIdx[^1])
                    {
                        loopIdx.RemoveAt(loopIdx.Count - 1);
                        List<Vector2> loop = new List<Vector2>(loopIdx.Count);
                        for (int t = 0; t < loopIdx.Count; t++) loop.Add(welded[loopIdx[t]]);
                        if (loop.Count >= 3) loops.Add(loop);
                    }
                }
            }

            return loops;
        }

        private List<List<Vector2>> GetOpenPolylinesFromMesh(Mesh combined)
        {
            List<List<Vector2>> result = new List<List<Vector2>>();
            if (!combined) return result;
            Vector3[] verts3 = combined.vertices;
            int[] tris = combined.triangles;
            if (verts3 == null || tris == null || tris.Length == 0) return result;

            float eps = Mathf.Max(1e-6f, boundarySnapEpsilon);
            Dictionary<(int, int), int> edgeCount = new Dictionary<(int, int), int>(tris.Length);
            List<Vector2> welded = new List<Vector2>();
            Dictionary<(int, int), int> quantToNew = new Dictionary<(int, int), int>(verts3.Length);
            int[] oldToNew = new int[verts3.Length];

            for (int i = 0; i < verts3.Length; i++)
            {
                Vector3 v = verts3[i];
                Vector2 p = computePlane == Plane2D.XZ ? new Vector2(v.x, v.z) : new Vector2(v.x, v.y);
                int qx = Mathf.RoundToInt(p.x / eps);
                int qy = Mathf.RoundToInt(p.y / eps);
                (int, int) key = (qx, qy);
                if (!quantToNew.TryGetValue(key, out int ni))
                {
                    ni = welded.Count;
                    quantToNew[key] = ni;
                    welded.Add(p);
                }

                oldToNew[i] = ni;
            }

            List<int> newTris = new List<int>(tris.Length);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a0 = oldToNew[tris[i]], b0 = oldToNew[tris[i + 1]], c0 = oldToNew[tris[i + 2]];
                if (a0 == b0 || b0 == c0 || c0 == a0) continue;
                newTris.Add(a0);
                newTris.Add(b0);
                newTris.Add(c0);
            }

            void AddEdge(int i0, int i1)
            {
                if (i0 > i1) (i0, i1) = (i1, i0);
                if (edgeCount.TryGetValue((i0, i1), out int c)) edgeCount[(i0, i1)] = c + 1;
                else edgeCount[(i0, i1)] = 1;
            }

            for (int i = 0; i < newTris.Count; i += 3)
            {
                int a = newTris[i], b = newTris[i + 1], c = newTris[i + 2];
                AddEdge(a, b);
                AddEdge(b, c);
                AddEdge(c, a);
            }

            Dictionary<int, List<int>> adj = new Dictionary<int, List<int>>();
            for (int i = 0; i < newTris.Count; i += 3)
            {
                int a = newTris[i], b = newTris[i + 1], c = newTris[i + 2];

                void Use(int u, int v)
                {
                    int x = u, y = v;
                    if (x > y) (x, y) = (y, x);
                    if (edgeCount[(x, y)] == 1)
                    {
                        if (!adj.TryGetValue(u, out List<int> lu))
                        {
                            lu = new List<int>();
                            adj[u] = lu;
                        }

                        if (!adj.TryGetValue(v, out List<int> lv))
                        {
                            lv = new List<int>();
                            adj[v] = lv;
                        }

                        lu.Add(v);
                        lv.Add(u);
                    }
                }

                Use(a, b);
                Use(b, c);
                Use(c, a);
            }

            HashSet<int> visited = new HashSet<int>();
            foreach (var kv in adj)
            {
                int node = kv.Key;
                if (visited.Contains(node)) continue;
                if (!adj.TryGetValue(node, out List<int> outs) || outs.Count != 1) continue;
                List<Vector2> pl = new List<Vector2>();
                int prev = -1;
                int cur = node;
                while (true)
                {
                    pl.Add(welded[cur]);
                    visited.Add(cur);
                    int next = -1;
                    if (adj.TryGetValue(cur, out List<int> nbs))
                    {
                        for (int i = 0; i < nbs.Count; i++)
                        {
                            int c = nbs[i];
                            if (c == prev) continue;
                            next = c;
                            break;
                        }
                    }

                    if (next == -1 || visited.Contains(next)) break;
                    prev = cur;
                    cur = next;
                }

                if (pl.Count >= 2) result.Add(pl);
            }

            foreach (var kv in adj)
            {
                int start = kv.Key;
                if (visited.Contains(start)) continue;
                if (!adj.TryGetValue(start, out List<int> outs) || outs.Count == 0) continue;
                List<Vector2> pl = new List<Vector2>();
                int prev = -1;
                int cur = start;
                for (int step = 0; step < 4096; step++)
                {
                    pl.Add(welded[cur]);
                    visited.Add(cur);
                    int next = -1;
                    if (adj.TryGetValue(cur, out List<int> nbs))
                    {
                        for (int i = 0; i < nbs.Count; i++)
                        {
                            int c = nbs[i];
                            if (c == prev) continue;
                            if (!visited.Contains(c))
                            {
                                next = c;
                                break;
                            }
                        }
                    }

                    if (next == -1) break;
                    prev = cur;
                    cur = next;
                }

                if (pl.Count >= 2) result.Add(pl);
            }

            return result;
        }

        #endregion

        #region 资产保存

        private void SaveGeneratedAssetsIfEnabled(Dictionary<Vector2Int, Mesh> combinedPerFactionHeight)
        {
            if (!saveAsAssets) return;

            // 保存阵营+高度 Mesh（每个 faction+height 组合单独保存）
            AreaFactionAssetHelper.SaveFactionHeightMeshes(combinedPerFactionHeight);

            // 保存站点 GameObject
            if (sites != null)
            {
                List<GameObject> siteGos = new List<GameObject>();
                foreach (SiteData site in sites)
                {
                    if (site.go && site.go.activeInHierarchy)
                    {
                        siteGos.Add(site.go);
                    }
                }

                AreaFactionAssetHelper.SaveGameObjectsAsPrefabs(siteGos);
            }

            // 保存轮廓 LineRenderer
            if (factionLinePool != null)
            {
                List<LineRenderer> lineRenderers = new List<LineRenderer>();
                foreach (SimpleInstantiatePool<LineRenderer> pool in factionLinePool.Values)
                {
                    lineRenderers.AddRange(pool.GetAllActivationItem());
                }

                AreaFactionAssetHelper.SaveLineRenderersAsPrefabs(lineRenderers);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取地块中心位置（供动画管理器使用）
        /// </summary>
        public bool TryGetSiteCenter(int siteIndex, out Vector3 center)
        {
            center = Vector3.zero;
            if (sites == null || siteIndex < 0 || siteIndex >= sites.Count)
                return false;

            center = sites[siteIndex].center;
            return true;
        }

        /// <summary>
        /// 获取地块的顶点列表（供动画管理器计算边界）
        /// </summary>
        public bool TryGetSiteVertices(int siteIndex, out List<Vector2> vertices)
        {
            vertices = null;
            if (areaResult == null || siteIndex < 0 || siteIndex >= areaResult.cells.Count)
                return false;

            vertices = areaResult.cells[siteIndex].vertices;
            return vertices != null && vertices.Count > 0;
        }

        /// <summary>
        /// 获取地块外接圆半径（供动画着色器缩放阈值）
        /// </summary>
        public bool TryGetSiteRadius(int siteIndex, out float radius)
        {
            radius = 0f;
            if (sites == null || siteIndex < 0 || siteIndex >= sites.Count)
                return false;

            radius = sites[siteIndex].radius;
            return radius > 0f;
        }

        /// <summary>
        /// 获取地块渲染器（供运行时热更新与编辑态预览使用）。
        /// </summary>
        public bool TryGetSiteRenderer(int siteIndex, out MeshRenderer renderer)
        {
            renderer = null;
            if (sites == null || siteIndex < 0 || siteIndex >= sites.Count)
                return false;

            SiteData site = sites[siteIndex];
            EnsureSiteGoVisible(site);
            renderer = site.meshRenderer;
            return renderer;
        }

        /// <summary>
        /// 获取阵营材质（供动画管理器使用）
        /// </summary>
        public Material GetFactionMaterial(int factionId)
        {
            FactionPair factionPair = assetObject.GetFactionPair(factionId);
            return factionPair?.areaMat;
        }

        private static ulong ComputeMeshesSignature(List<Mesh> meshes)
        {
            const ulong FNV_OFFSET = 1469598103934665603UL;
            const ulong FNV_PRIME = 1099511628211UL;
            ulong h = FNV_OFFSET;
            unchecked
            {
                h ^= (ulong) (uint) meshes.Count;
                h *= FNV_PRIME;

                for (int i = 0; i < meshes.Count; i++)
                {
                    Mesh m = meshes[i];
                    int id = m ? m.GetInstanceID() : 0;
                    h ^= (uint) id;
                    h *= FNV_PRIME;
                }
            }

            return h;
        }

        private void DestroyAllObjects()
        {
            if (sites != null)
            {
                for (int i = 0; i < sites.Count; i++)
                {
                    if (sites[i].mesh)
                    {
                        Destroy(sites[i].mesh);
                    }

                    if (sites[i].go)
                    {
                        Destroy(sites[i].go);
                    }
                }

                sites.Clear();
                sites = null;
            }

            if (factionLinePool != null)
            {
                foreach (SimpleInstantiatePool<LineRenderer> pool in factionLinePool.Values)
                {
                    pool.Destroy();
                }

                factionLinePool.Clear();
            }

            if (cachedCombinedPerFactionHeight != null)
            {
                foreach (Mesh mesh in cachedCombinedPerFactionHeight.Values)
                {
                    if (mesh)
                    {
                        Destroy(mesh);
                    }
                }

                cachedCombinedPerFactionHeight.Clear();
            }

            if (linePrefab)
            {
                Destroy(linePrefab.gameObject);
            }
        }

        #endregion
    }
}
