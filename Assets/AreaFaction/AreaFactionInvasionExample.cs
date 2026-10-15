using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.AreaFaction
{
    /// <summary>
    /// 侵蚀动画演示 - 模拟HOI4风格地图征服动画
    /// 空场景挂载即可，无需任何外部依赖
    /// </summary>
    public class AreaFactionInvasionExample : MonoBehaviour
    {
        private readonly struct AttackEdge
        {
            public readonly int attacker;
            public readonly int defender;

            public AttackEdge(int attacker, int defender)
            {
                this.attacker = attacker;
                this.defender = defender;
            }
        }

        [Header("组件引用（留空则运行时自动创建）")]
        [SerializeField] private AreaFactionView areaView;
        [SerializeField] private AreaFactionInvasionAnimator invasionAnimator;
        [SerializeField] private AreaFactionAssetObject assetObject;
        [SerializeField] private Camera sceneCamera;

        [Header("地图配置")]
        [Range(2, 4)] [SerializeField] private int factionCount = 3;
        [Range(4f, 30f)] [SerializeField] private float mapScale = 10f;

        [Header("侵蚀调参")]
        [Min(0.2f)] [SerializeField] private float invasionDuration = 2.4f;
        [SerializeField] private AnimationCurve invasionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private Color borderGlowColor = new Color(1f, 0.86f, 0.25f, 1f);
        [Range(0.01f, 0.2f)] [SerializeField] private float borderWidth = 0.08f;
        [Range(0f, 3f)] [SerializeField] private float borderIntensity = 1.4f;
        [Range(0f, 20f)] [SerializeField] private float flickerSpeed = 8f;
        [Range(0f, 0.5f)] [SerializeField] private float flickerAmount = 0.28f;
        [Range(0f, 10f)] [SerializeField] private float noiseScale = 3.2f;
        [Range(0f, 0.5f)] [SerializeField] private float noiseAmount = 0.18f;
        [Range(0.02f, 0.35f)] [SerializeField] private float battleBandWidth = 0.18f;
        [Range(1f, 40f)] [SerializeField] private float battleDotDensity = 18f;
        [Range(0.05f, 0.3f)] [SerializeField] private float battleDotSize = 0.16f;
        [Range(0f, 3f)] [SerializeField] private float battleIntensity = 1.25f;
        [Range(0f, 20f)] [SerializeField] private float battleFlowSpeed = 8f;

        [Header("自动演示")]
        [SerializeField] private bool autoDemo = true;
        [Min(0.2f)] [SerializeField] private float changeFactionInterval = 3f;

        [Header("相机控制")]
        [SerializeField] private bool enableCameraControl = true;
        [Range(0f, 8f)] [SerializeField] private float cameraPadding = 1.5f;
        [Range(1f, 40f)] [SerializeField] private float cameraPanSpeed = 12f;
        [Range(0.5f, 20f)] [SerializeField] private float cameraZoomSpeed = 6f;
        [Range(0.001f, 0.05f)] [SerializeField] private float cameraDragSensitivity = 0.012f;
        [Range(1f, 20f)] [SerializeField] private float minOrthographicSize = 4f;
        [Range(5f, 80f)] [SerializeField] private float maxOrthographicSize = 40f;

        [Header("编辑态预览")]
        [SerializeField] private bool enableEditorPreview;
        [Range(0, 11)] [SerializeField] private int editorPreviewAttackIndex;
        [Range(0f, 1f)] [SerializeField] private float editorPreviewProgress = 0.35f;

        // 预设的不规则多边形领土（模拟HOI4地图分块）
        // 每个领土是一组顶点（XZ平面），围绕地图中心排布
        private static readonly Vector2[][] TerritoryPolygons = {
            // 左上领土（类法国）
            new Vector2[] {
                new Vector2(-9, 2), new Vector2(-6, 5), new Vector2(-2, 6),
                new Vector2(-1, 3), new Vector2(-3, 1), new Vector2(-7, 0)
            },
            // 右上领土（类德国）
            new Vector2[] {
                new Vector2(-1, 3), new Vector2(-2, 6), new Vector2(2, 7),
                new Vector2(5, 5), new Vector2(4, 2), new Vector2(1, 1)
            },
            // 右中领土（类波兰）
            new Vector2[] {
                new Vector2(4, 2), new Vector2(5, 5), new Vector2(8, 4),
                new Vector2(9, 1), new Vector2(7, -1), new Vector2(5, 0)
            },
            // 下中领土（类意大利）
            new Vector2[] {
                new Vector2(-3, 1), new Vector2(-1, 3), new Vector2(1, 1),
                new Vector2(2, -2), new Vector2(-1, -4), new Vector2(-4, -2)
            },
            // 左下领土（类西班牙）
            new Vector2[] {
                new Vector2(-7, 0), new Vector2(-3, 1), new Vector2(-4, -2),
                new Vector2(-6, -5), new Vector2(-9, -3), new Vector2(-8, -1)
            },
            // 右下领土（类巴尔干）
            new Vector2[] {
                new Vector2(1, 1), new Vector2(4, 2), new Vector2(5, 0),
                new Vector2(4, -3), new Vector2(2, -5), new Vector2(0, -4),
                new Vector2(2, -2)
            },
        };

        private Vector3[] sitePositions;  // 每块领土的中心点
        private int[] siteFactions;       // 每块领土的阵营ID
        private Vector2[][] scaledTerritoryPolygons;
        private List<AttackEdge> attackEdges;
        private int nextAttackEdgeIndex;
        private Coroutine demoCoroutine;
        private Rect mapBounds;
        private bool isDraggingCamera;
        private Vector3 lastMousePosition;

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            scaledTerritoryPolygons = BuildScaledPolygons();
            if (!SetupCamera(true) || !SetupComponents(true))
            {
                return;
            }

            SetupSites();
            BuildInitial();

            if (autoDemo)
                demoCoroutine = StartCoroutine(AutoDemoCoroutine());
        }

        private void OnDestroy()
        {
            if (demoCoroutine != null)
                StopCoroutine(demoCoroutine);
        }

        private void OnValidate()
        {
            invasionDuration = Mathf.Max(0.2f, invasionDuration);
            changeFactionInterval = Mathf.Max(0.2f, changeFactionInterval);
            maxOrthographicSize = Mathf.Max(minOrthographicSize + 0.01f, maxOrthographicSize);
            scaledTerritoryPolygons = BuildScaledPolygons();

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (Application.isPlaying)
            {
                ApplyRuntimeTuning();
                return;
            }

            if (enableEditorPreview)
            {
                PreviewSelectedAttackInEditor();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            HandleCameraInput();
        }

        private bool SetupCamera(bool createMissing)
        {
            if (!sceneCamera)
            {
                sceneCamera = Camera.main;
            }

            if (!sceneCamera)
            {
                if (!createMissing)
                {
                    return false;
                }

                GameObject camObj = new GameObject("DemoCamera");
                sceneCamera = camObj.AddComponent<Camera>();
            }

            mapBounds = CalculateBounds(scaledTerritoryPolygons);
            Vector2 mapCenter = mapBounds.center;
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = CalculateFittedOrthographicSize(mapBounds);
            sceneCamera.transform.position = new Vector3(mapCenter.x, 18f, mapCenter.y);
            sceneCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            sceneCamera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            sceneCamera.nearClipPlane = 0.01f;
            sceneCamera.farClipPlane = 100f;
            return true;
        }

        private bool SetupComponents(bool createMissing)
        {
            if (!areaView)
            {
                if (!createMissing)
                {
                    return false;
                }

                GameObject viewObj = new GameObject("AreaFactionView");
                viewObj.transform.SetParent(transform);
                areaView = viewObj.AddComponent<AreaFactionView>();
            }

            if (!areaView.container)
            {
                if (!createMissing)
                {
                    return false;
                }

                GameObject container = new GameObject("Container");
                container.transform.SetParent(areaView.transform);
                areaView.container = container.transform;
            }

            areaView.computePlane = Plane2D.XZ;
            areaView.fillHeightOffset = 0.05f;
            areaView.outlineHeightOffset = 0.06f;
            areaView.SetCalculator(new PredefinedPolygonAreaCalculator(scaledTerritoryPolygons));

            if (!assetObject)
            {
                if (!createMissing)
                {
                    return false;
                }

                assetObject = CreateAssetObject();
            }

            SetPrivateField(areaView, "assetObject", assetObject);

            if (!invasionAnimator)
            {
                if (!createMissing)
                {
                    return false;
                }

                GameObject animObj = new GameObject("InvasionAnimator");
                animObj.transform.SetParent(transform);
                invasionAnimator = animObj.AddComponent<AreaFactionInvasionAnimator>();
            }

            SetPrivateField(invasionAnimator, "invasionMaterialTemplate", CreateInvasionMaterial());
            ApplyAnimatorTuning();
            areaView.SetInvasionAnimator(invasionAnimator);
            return true;
        }

        private void SetupSites()
        {
            int count = scaledTerritoryPolygons.Length;
            sitePositions = new Vector3[count];
            siteFactions = new int[count];
            attackEdges = BuildAttackEdges(scaledTerritoryPolygons);

            for (int i = 0; i < count; i++)
            {
                Vector2 center = ComputePolygonCenter(scaledTerritoryPolygons[i]);
                sitePositions[i] = new Vector3(center.x, 0, center.y);
                siteFactions[i] = i % factionCount;
            }
        }

        private void BuildInitial()
        {
            // 首次Build初始化区域（beforeFaction=-1，无动画）
            areaView.Build(sitePositions, siteFactions, null, sceneCamera);
        }

        /// <summary>
        /// 自动演示：按真实相邻边界推进，占领目标地块
        /// </summary>
        private IEnumerator AutoDemoCoroutine()
        {
            yield return new WaitForSeconds(1f); // 等待首次渲染完成

            while (true)
            {
                yield return new WaitForSeconds(changeFactionInterval);
                TriggerNextAttack("Demo");
            }
        }

        /// <summary>
        /// 手动触发：改变随机领土阵营
        /// </summary>
        [ContextMenu("Test Change Faction")]
        public void TestChangeFaction()
        {
            TriggerNextAttack("Test");
        }

        [ContextMenu("Reset Demo")]
        public void ResetDemo()
        {
            ResetSiteFactions();
            areaView.Build(sitePositions, siteFactions, null, sceneCamera);
        }

        [ContextMenu("Focus Map")]
        public void FocusMap()
        {
            if (!sceneCamera)
            {
                return;
            }

            mapBounds = CalculateBounds(scaledTerritoryPolygons);
            Vector2 mapCenter = mapBounds.center;
            sceneCamera.transform.position = new Vector3(mapCenter.x, sceneCamera.transform.position.y, mapCenter.y);
            sceneCamera.orthographicSize = CalculateFittedOrthographicSize(mapBounds);
        }

        [ContextMenu("Preset/HOI4 Thin Front")]
        public void ApplyPresetHoi4ThinFront()
        {
            invasionDuration = 3.0f;
            invasionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            borderGlowColor = new Color(1f, 0.92f, 0.55f, 1f);
            borderWidth = 0.04f;
            borderIntensity = 1.1f;
            flickerSpeed = 9f;
            flickerAmount = 0.22f;
            noiseScale = 3.8f;
            noiseAmount = 0.12f;
            battleBandWidth = 0.10f;
            battleDotDensity = 20f;
            battleDotSize = 0.10f;
            battleIntensity = 1.2f;
            battleFlowSpeed = 9f;
            changeFactionInterval = 3.4f;

            ApplyRuntimeTuning();
        }

        [ContextMenu("Preset/Intense Tug Of War")]
        public void ApplyPresetIntenseTugOfWar()
        {
            invasionDuration = 2.1f;
            invasionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            borderGlowColor = new Color(1f, 0.78f, 0.3f, 1f);
            borderWidth = 0.06f;
            borderIntensity = 1.6f;
            flickerSpeed = 11f;
            flickerAmount = 0.34f;
            noiseScale = 4.5f;
            noiseAmount = 0.22f;
            battleBandWidth = 0.16f;
            battleDotDensity = 24f;
            battleDotSize = 0.14f;
            battleIntensity = 1.55f;
            battleFlowSpeed = 12f;
            changeFactionInterval = 2.7f;

            ApplyRuntimeTuning();
        }

        [ContextMenu("Preset/Stalemate Then Collapse")]
        public void ApplyPresetStalemateThenCollapse()
        {
            invasionDuration = 3.4f;
            invasionCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 0.06f),
                new Keyframe(0.42f, 0.15f),
                new Keyframe(0.60f, 0.26f),
                new Keyframe(0.78f, 0.55f),
                new Keyframe(1f, 1f));
            borderGlowColor = new Color(1f, 0.9f, 0.58f, 1f);
            borderWidth = 0.045f;
            borderIntensity = 1.2f;
            flickerSpeed = 9.5f;
            flickerAmount = 0.36f;
            noiseScale = 4.2f;
            noiseAmount = 0.20f;
            battleBandWidth = 0.12f;
            battleDotDensity = 22f;
            battleDotSize = 0.11f;
            battleIntensity = 1.4f;
            battleFlowSpeed = 10f;
            changeFactionInterval = 3.9f;

            ApplyRuntimeTuning();
        }

        [ContextMenu("Preset/Elastic Front")]
        public void ApplyPresetElasticFront()
        {
            invasionDuration = 3.0f;
            invasionCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.16f, 0.05f),
                new Keyframe(0.34f, 0.14f),
                new Keyframe(0.55f, 0.32f),
                new Keyframe(0.78f, 0.62f),
                new Keyframe(1f, 1f));
            borderGlowColor = new Color(1f, 0.84f, 0.42f, 1f);
            borderWidth = 0.05f;
            borderIntensity = 1.3f;
            flickerSpeed = 10.5f;
            flickerAmount = 0.30f;
            noiseScale = 4.0f;
            noiseAmount = 0.17f;
            battleBandWidth = 0.14f;
            battleDotDensity = 20f;
            battleDotSize = 0.12f;
            battleIntensity = 1.3f;
            battleFlowSpeed = 9.5f;
            changeFactionInterval = 3.4f;

            ApplyRuntimeTuning();
        }

        private void TriggerNextAttack(string logPrefix)
        {
            if (attackEdges == null || attackEdges.Count == 0)
            {
                Debug.LogWarning("[AreaFactionInvasionExample] 没有可用的相邻攻防对");
                return;
            }

            if (!TrySelectNextAttack(out AttackEdge attack))
            {
                ResetSiteFactions();
                Debug.Log($"[{logPrefix}] 所有相邻地块已同色，重置阵营布局");
                areaView.Build(sitePositions, siteFactions, null, sceneCamera);
                return;
            }

            int attackerFaction = siteFactions[attack.attacker];
            int defenderFaction = siteFactions[attack.defender];
            siteFactions[attack.defender] = attackerFaction;
            areaView.SetPendingInvasionSource(attack.defender, attack.attacker);

            Debug.Log($"[{logPrefix}] 地块 {attack.attacker}({attackerFaction}) -> 地块 {attack.defender}({defenderFaction})");
            areaView.Build(sitePositions, siteFactions, null, sceneCamera);
        }

        private bool TrySelectNextAttack(out AttackEdge attack)
        {
            for (int i = 0; i < attackEdges.Count; i++)
            {
                AttackEdge candidate = attackEdges[(nextAttackEdgeIndex + i) % attackEdges.Count];
                if (siteFactions[candidate.attacker] == siteFactions[candidate.defender])
                {
                    continue;
                }

                nextAttackEdgeIndex = (nextAttackEdgeIndex + i + 1) % attackEdges.Count;
                attack = candidate;
                return true;
            }

            attack = default;
            return false;
        }

        private void ResetSiteFactions()
        {
            nextAttackEdgeIndex = 0;
            for (int i = 0; i < siteFactions.Length; i++)
            {
                siteFactions[i] = i % factionCount;
            }
        }

        private Vector2[][] BuildScaledPolygons()
        {
            float scale = Mathf.Max(0.01f, mapScale / 10f);
            Vector2[][] result = new Vector2[TerritoryPolygons.Length][];
            for (int i = 0; i < TerritoryPolygons.Length; i++)
            {
                Vector2[] polygon = TerritoryPolygons[i];
                Vector2[] scaled = new Vector2[polygon.Length];
                for (int j = 0; j < polygon.Length; j++)
                {
                    scaled[j] = polygon[j] * scale;
                }

                result[i] = scaled;
            }

            return result;
        }

        private void ApplyAnimatorTuning()
        {
            SetPrivateField(invasionAnimator, "invasionDuration", invasionDuration);
            SetPrivateField(invasionAnimator, "invasionCurve", invasionCurve);
            SetPrivateField(invasionAnimator, "borderGlowColor", borderGlowColor);
            SetPrivateField(invasionAnimator, "borderWidth", borderWidth);
            SetPrivateField(invasionAnimator, "borderIntensity", borderIntensity);
            SetPrivateField(invasionAnimator, "flickerSpeed", flickerSpeed);
            SetPrivateField(invasionAnimator, "flickerAmount", flickerAmount);
            SetPrivateField(invasionAnimator, "noiseScale", noiseScale);
            SetPrivateField(invasionAnimator, "noiseAmount", noiseAmount);
            SetPrivateField(invasionAnimator, "battleBandWidth", battleBandWidth);
            SetPrivateField(invasionAnimator, "battleDotDensity", battleDotDensity);
            SetPrivateField(invasionAnimator, "battleDotSize", battleDotSize);
            SetPrivateField(invasionAnimator, "battleIntensity", battleIntensity);
            SetPrivateField(invasionAnimator, "battleFlowSpeed", battleFlowSpeed);
        }

        public void ApplyRuntimeTuning()
        {
            if (!sceneCamera)
            {
                scaledTerritoryPolygons ??= BuildScaledPolygons();
                if (!SetupCamera(Application.isPlaying))
                {
                    return;
                }
            }

            if (RequiresComponentSetup())
            {
                if (!SetupComponents(Application.isPlaying))
                {
                    return;
                }
            }
            else
            {
                ApplyAnimatorTuning();
            }

            if (Application.isPlaying)
            {
                invasionAnimator?.RefreshActiveVisuals();
            }

            if (!Application.isPlaying && enableEditorPreview)
            {
                PreviewSelectedAttackInEditor();
            }
        }

        private bool RequiresComponentSetup()
        {
            return !areaView || !invasionAnimator || !assetObject || !areaView.container;
        }

        public void PreviewSelectedAttackInEditor()
        {
            if (Application.isPlaying)
            {
                return;
            }

            scaledTerritoryPolygons ??= BuildScaledPolygons();
            if (!SetupCamera(false) || !SetupComponents(false))
            {
                Debug.LogWarning("[AreaFactionInvasionExample] 编辑器预览需要先绑定 sceneCamera、areaView、invasionAnimator 和 assetObject；这些对象现在只会在运行时自动创建。", this);
                return;
            }

            SetupSites();
            ResetSiteFactions();
            BuildInitial();

            if (attackEdges == null || attackEdges.Count == 0)
            {
                return;
            }

            int previewIndex = Mathf.Clamp(editorPreviewAttackIndex, 0, attackEdges.Count - 1);
            AttackEdge attack = attackEdges[previewIndex];
            int attackerFaction = siteFactions[attack.attacker];
            int defenderFaction = siteFactions[attack.defender];

            invasionAnimator.ClearAllPreviews();
            if (areaView.TryGetSiteRenderer(attack.defender, out MeshRenderer renderer))
            {
                invasionAnimator.PreviewInvasion(
                    attack.defender,
                    defenderFaction,
                    attackerFaction,
                    renderer,
                    editorPreviewProgress,
                    attack.attacker);
            }
        }

        public void ClearEditorPreview()
        {
            invasionAnimator?.ClearAllPreviews();
            if (!Application.isPlaying && sitePositions != null && sceneCamera && areaView)
            {
                ResetSiteFactions();
                areaView.Build(sitePositions, siteFactions, null, sceneCamera);
            }
        }

        private void HandleCameraInput()
        {
            if (!enableCameraControl || !sceneCamera)
            {
                return;
            }

            float zoomInput = Input.mouseScrollDelta.y;
            if (Mathf.Abs(zoomInput) > Mathf.Epsilon)
            {
                sceneCamera.orthographicSize = Mathf.Clamp(
                    sceneCamera.orthographicSize - zoomInput * cameraZoomSpeed,
                    minOrthographicSize,
                    maxOrthographicSize);
            }

            Vector3 pan = Vector3.zero;
            pan.x = Input.GetAxisRaw("Horizontal");
            pan.z = Input.GetAxisRaw("Vertical");
            if (pan.sqrMagnitude > 0f)
            {
                pan = pan.normalized * (cameraPanSpeed * Time.deltaTime * Mathf.Max(sceneCamera.orthographicSize * 0.25f, 1f));
                sceneCamera.transform.position += pan;
            }

            bool dragPressed = Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1);
            if (dragPressed)
            {
                isDraggingCamera = true;
                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(1))
            {
                isDraggingCamera = false;
            }

            if (isDraggingCamera && (Input.GetMouseButton(2) || Input.GetMouseButton(1)))
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                float scale = sceneCamera.orthographicSize * cameraDragSensitivity;
                sceneCamera.transform.position += new Vector3(-mouseDelta.x * scale, 0f, -mouseDelta.y * scale);
                lastMousePosition = Input.mousePosition;
            }
        }

        private Rect CalculateBounds(Vector2[][] polygons)
        {
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < polygons.Length; i++)
            {
                Vector2[] polygon = polygons[i];
                for (int j = 0; j < polygon.Length; j++)
                {
                    Vector2 vertex = polygon[j];
                    if (vertex.x < minX) minX = vertex.x;
                    if (vertex.y < minY) minY = vertex.y;
                    if (vertex.x > maxX) maxX = vertex.x;
                    if (vertex.y > maxY) maxY = vertex.y;
                }
            }

            if (float.IsInfinity(minX))
            {
                return new Rect(-5f, -5f, 10f, 10f);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private float CalculateFittedOrthographicSize(Rect bounds)
        {
            float aspect = sceneCamera ? Mathf.Max(sceneCamera.aspect, 0.01f) : (16f / 9f);
            float halfHeight = bounds.height * 0.5f + cameraPadding;
            float halfWidth = bounds.width * 0.5f + cameraPadding;
            float fitted = Mathf.Max(halfHeight, halfWidth / aspect);
            return Mathf.Clamp(fitted, minOrthographicSize, maxOrthographicSize);
        }

        private static List<AttackEdge> BuildAttackEdges(Vector2[][] polygons)
        {
            List<AttackEdge> edges = new List<AttackEdge>();
            for (int i = 0; i < polygons.Length; i++)
            {
                for (int j = 0; j < polygons.Length; j++)
                {
                    if (i == j || !ShareEdge(polygons[i], polygons[j]))
                    {
                        continue;
                    }

                    edges.Add(new AttackEdge(i, j));
                }
            }

            return edges;
        }

        private static bool ShareEdge(Vector2[] polygonA, Vector2[] polygonB)
        {
            const float epsilon = 0.02f;
            for (int i = 0; i < polygonA.Length; i++)
            {
                Vector2 a0 = polygonA[i];
                Vector2 a1 = polygonA[(i + 1) % polygonA.Length];
                for (int j = 0; j < polygonB.Length; j++)
                {
                    Vector2 b0 = polygonB[j];
                    Vector2 b1 = polygonB[(j + 1) % polygonB.Length];
                    bool sameDirection = Approximately(a0, b0, epsilon) && Approximately(a1, b1, epsilon);
                    bool oppositeDirection = Approximately(a0, b1, epsilon) && Approximately(a1, b0, epsilon);
                    bool shared = sameDirection || oppositeDirection;
                    if (shared)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Approximately(Vector2 a, Vector2 b, float epsilon)
        {
            return (a - b).sqrMagnitude <= epsilon * epsilon;
        }

        private static Vector2 ComputePolygonCenter(Vector2[] polygon)
        {
            float signedArea = 0f;
            float cx = 0f;
            float cy = 0f;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 current = polygon[i];
                Vector2 next = polygon[(i + 1) % polygon.Length];
                float cross = current.x * next.y - next.x * current.y;
                signedArea += cross;
                cx += (current.x + next.x) * cross;
                cy += (current.y + next.y) * cross;
            }

            signedArea *= 0.5f;
            if (Mathf.Abs(signedArea) < 1e-5f)
            {
                Vector2 average = Vector2.zero;
                for (int i = 0; i < polygon.Length; i++)
                {
                    average += polygon[i];
                }

                return average / Mathf.Max(polygon.Length, 1);
            }

            float factor = 1f / (6f * signedArea);
            return new Vector2(cx * factor, cy * factor);
        }

        private AreaFactionAssetObject CreateAssetObject()
        {
            AreaFactionAssetObject asset = ScriptableObject.CreateInstance<AreaFactionAssetObject>();
            var pairs = new List<FactionPair>();

            Color[] factionColors = {
                new Color(0.85f, 0.25f, 0.2f),   // 红（阵营0）
                new Color(0.25f, 0.45f, 0.85f),   // 蓝（阵营1）
                new Color(0.3f, 0.7f, 0.3f),      // 绿（阵营2）
                new Color(0.8f, 0.7f, 0.2f),      // 黄（阵营3）
            };

            for (int i = 0; i < Mathf.Max(factionCount, factionColors.Length); i++)
            {
                Color c = factionColors[i % factionColors.Length];
                pairs.Add(new FactionPair
                {
                    factionId = i,
                    areaMat = CreateMaterial(c),
                    areaEdgeMat = CreateMaterial(Color.white * 0.9f),
                    areaEdge2Mat = CreateMaterial(Color.black),
                    areaEdgeWidth = 0.12f,
                    areaEdge2Width = 0.06f
                });
            }

            SetPrivateField(asset, "factionPairs", pairs);
            SetPrivateField(asset, "defaultFactionId", 0);
            return asset;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Color")
                            ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        private static Material CreateInvasionMaterial()
        {
            Shader shader = Shader.Find("Custom/AreaFactionInvasion");
            if (!shader)
            {
                Debug.LogError("[AreaFactionInvasion] 找不到 Custom/AreaFactionInvasion 着色器，动画不会生效");
                shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            }
            return new Material(shader);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
