using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tools.Art.Assets
{
    [System.Serializable]
    public class FBXInfo
    {
        public string fileName;
        public string filePath;
        public int triangleCount;
        public int vertexCount;
        public int meshCount;
        public long fileSize;
        public bool isExpanded;
        
        // 缓存预览图，避免重复获取
        private Texture2D _cachedPreview;
        private bool _previewCached = false;
        
        public string FileSizeText => GetFileSizeString(fileSize);
        
        // 获取预览图（带缓存）
        public Texture2D GetPreviewTexture(bool useCustomRender = true)
        {
            if (_previewCached)
                return _cachedPreview;
            
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
            if (fbxAsset == null)
            {
                _previewCached = true;
                _cachedPreview = null;
                return null;
            }
            
            if (useCustomRender)
            {
                // 优先尝试生成自定义预览（这个更可靠）
                _cachedPreview = GenerateCustomMeshPreview(fbxAsset);
                
                // 如果自定义预览失败，尝试从mesh组件获取预览
                if (_cachedPreview == null)
                {
                    _cachedPreview = GetMeshPreview(fbxAsset);
                }
            }
            else
            {
                // 标准模式：优先尝试mesh预览
                _cachedPreview = GetMeshPreview(fbxAsset);
            }
            
            // 如果以上都失败，回退到标准预览（这通常会显示FBX图标）
            if (_cachedPreview == null)
            {
                _cachedPreview = AssetPreview.GetAssetPreview(fbxAsset);
            }
            
            // 最后尝试MiniThumbnail
            if (_cachedPreview == null)
            {
                _cachedPreview = AssetPreview.GetMiniThumbnail(fbxAsset);
            }
            
            _previewCached = true;
            return _cachedPreview;
        }
        
        // 获取网格预览图
        private Texture2D GetMeshPreview(GameObject fbxAsset)
        {
            // 查找第一个包含Mesh的组件
            MeshFilter[] meshFilters = fbxAsset.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length > 0 && meshFilters[0].sharedMesh != null)
            {
                return AssetPreview.GetAssetPreview(meshFilters[0].sharedMesh);
            }
            
            // 查找SkinnedMeshRenderer
            SkinnedMeshRenderer[] skinnedMeshes = fbxAsset.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshes.Length > 0 && skinnedMeshes[0].sharedMesh != null)
            {
                return AssetPreview.GetAssetPreview(skinnedMeshes[0].sharedMesh);
            }
            
            return null;
        }
        
        // 生成自定义网格预览
        private Texture2D GenerateCustomMeshPreview(GameObject fbxAsset)
        {
            PreviewRenderUtility previewRenderUtility = null;
            try
            {
                Debug.Log($"开始生成自定义预览: {filePath}");
                
                // 创建预览渲染器
                previewRenderUtility = new PreviewRenderUtility();
                
                // 获取所有Mesh和Transform信息
                var meshData = new List<(Mesh mesh, Material material, Matrix4x4 matrix)>();
                
                // 收集MeshFilter中的mesh
                MeshFilter[] meshFilters = fbxAsset.GetComponentsInChildren<MeshFilter>();
                Debug.Log($"找到 {meshFilters.Length} 个MeshFilter组件");
                
                foreach (var mf in meshFilters)
                {
                    if (mf.sharedMesh != null)
                    {
                        var renderer = mf.GetComponent<MeshRenderer>();
                        Material material = null;
                        
                        if (renderer != null && renderer.sharedMaterial != null)
                        {
                            material = renderer.sharedMaterial;
                        }
                        else
                        {
                            // 创建Unity标准的默认材质
                            material = new Material(Shader.Find("Standard"));
                            material.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Unity标准灰色
                        }
                        
                        meshData.Add((mf.sharedMesh, material, mf.transform.localToWorldMatrix));
                    }
                }
                
                // 收集SkinnedMeshRenderer中的mesh
                SkinnedMeshRenderer[] skinnedMeshes = fbxAsset.GetComponentsInChildren<SkinnedMeshRenderer>();
                Debug.Log($"找到 {skinnedMeshes.Length} 个SkinnedMeshRenderer组件");
                
                foreach (var smr in skinnedMeshes)
                {
                    if (smr.sharedMesh != null)
                    {
                        Material material = null;
                        if (smr.sharedMaterial != null)
                        {
                            material = smr.sharedMaterial;
                        }
                        else
                        {
                            material = new Material(Shader.Find("Standard"));
                            material.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Unity标准灰色
                        }
                        
                        meshData.Add((smr.sharedMesh, material, smr.transform.localToWorldMatrix));
                    }
                }
                
                if (meshData.Count == 0)
                {
                    Debug.Log("没有找到可渲染的mesh");
                    previewRenderUtility?.Cleanup();
                    return null;
                }
                
                Debug.Log($"准备渲染 {meshData.Count} 个mesh");
                
                // 计算所有mesh的合并包围盒
                Bounds combinedBounds = new Bounds();
                bool boundsInitialized = false;
                
                foreach (var data in meshData)
                {
                    Bounds meshBounds = data.mesh.bounds;
                    // 应用变换矩阵到包围盒
                    Vector3 center = data.matrix.MultiplyPoint3x4(meshBounds.center);
                    Vector3 size = data.matrix.MultiplyVector(meshBounds.size);
                    
                    Bounds transformedBounds = new Bounds(center, size);
                    
                    if (!boundsInitialized)
                    {
                        combinedBounds = transformedBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(transformedBounds);
                    }
                }
                
                // 设置相机
                float distance = Mathf.Max(combinedBounds.size.magnitude * 1.5f, 2f);
                Vector3 cameraPos = combinedBounds.center + new Vector3(distance * 0.6f, distance * 0.4f, distance * 0.8f);
                
                previewRenderUtility.camera.transform.position = cameraPos;
                previewRenderUtility.camera.transform.LookAt(combinedBounds.center);
                previewRenderUtility.camera.nearClipPlane = 0.01f;
                previewRenderUtility.camera.farClipPlane = distance * 5f;
                previewRenderUtility.camera.fieldOfView = 45f;
                
                // 使用Unity Editor标准预览灯光设置
                previewRenderUtility.lights[0].intensity = 1.2f;
                previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(25f, 25f, 0f);
                previewRenderUtility.lights[0].color = Color.white;
                previewRenderUtility.ambientColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                
                // 添加Unity Editor标准的填充光
                if (previewRenderUtility.lights.Length > 1)
                {
                    previewRenderUtility.lights[1].intensity = 0.6f;
                    previewRenderUtility.lights[1].transform.rotation = Quaternion.Euler(-25f, -25f, 0f);
                    previewRenderUtility.lights[1].color = Color.white;
                }
                
                // 使用Unity Editor标准的背景色
                previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewRenderUtility.camera.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f);
                
                // 开始预览渲染
                previewRenderUtility.BeginPreview(new Rect(0, 0, 256, 256), GUIStyle.none);
                
                // 渲染所有mesh
                foreach (var data in meshData)
                {
                    previewRenderUtility.DrawMesh(data.mesh, data.matrix, data.material, 0);
                }
                
                previewRenderUtility.camera.Render();
                
                // 获取渲染结果
                Texture previewTexture = previewRenderUtility.EndPreview();
                
                if (previewTexture != null)
                {
                    Debug.Log($"成功生成预览，尺寸: {previewTexture.width}x{previewTexture.height}");
                    
                    // 创建Texture2D副本
                    Texture2D result = new Texture2D(previewTexture.width, previewTexture.height, TextureFormat.RGB24, false);
                    
                    // 使用Graphics.CopyTexture或ReadPixels
                    if (previewTexture is RenderTexture rt)
                    {
                        RenderTexture.active = rt;
                        result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        result.Apply();
                        RenderTexture.active = null;
                    }
                    else if (previewTexture is Texture2D tex2d)
                    {
                        Graphics.CopyTexture(tex2d, result);
                    }
                    
                    // 对预览图进行轻微亮度调整
                    result = AdjustBrightness(result, 1.08f); // 提升8%亮度
                    
                    return result;
                }
                else
                {
                    Debug.Log("预览渲染返回null");
                }
                
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"生成自定义预览失败 [{filePath}]: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                previewRenderUtility?.Cleanup();
            }
        }
        
        // 清除预览图缓存
        public void ClearPreviewCache()
        {
            _previewCached = false;
            _cachedPreview = null;
        }
        
        // 调整纹理亮度
        private Texture2D AdjustBrightness(Texture2D source, float brightnessMultiplier)
        {
            try
            {
                // 创建可读的纹理副本
                RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
                Graphics.Blit(source, rt);
                
                RenderTexture.active = rt;
                Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                result.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                
                // 调整像素亮度
                Color[] pixels = result.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].r = Mathf.Clamp01(pixels[i].r * brightnessMultiplier);
                    pixels[i].g = Mathf.Clamp01(pixels[i].g * brightnessMultiplier);
                    pixels[i].b = Mathf.Clamp01(pixels[i].b * brightnessMultiplier);
                }
                result.SetPixels(pixels);
                result.Apply();
                
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"调整亮度失败: {e.Message}");
                return source; // 返回原纹理
            }
        }
        
        private string GetFileSizeString(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            return $"{bytes / (1024 * 1024):F1} MB";
        }
    }

    public class FBXPolygonViewer : EditorWindow
    {
        private string folderPath = "Assets/ArtOfWar-II_Art/Fbx"; // 默认目录
        private List<FBXInfo> fbxInfoList = new List<FBXInfo>();
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private bool showDetailsView = true;
        private bool showPreviewImages = true;
        private bool useCustomPreviewRender = true;
        private bool isScanning = false;
        
        // 虚拟化滚动相关
        private const float ITEM_HEIGHT_SIMPLE = 20f;
        private const float ITEM_HEIGHT_DETAILED_COLLAPSED = 80f;
        private const float ITEM_HEIGHT_DETAILED_EXPANDED = 150f;
        private bool useVirtualizedScrolling = true;
        
        // 统计数据
        private int totalTriangles = 0;
        private int totalVertices = 0;
        private int totalFiles = 0;
        private long totalFileSize = 0;
        
        // 排序选项
        private enum SortOption { Name, Triangles, Vertices, FileSize }
        private SortOption currentSort = SortOption.Triangles;
        private bool sortDescending = true;
        
        // 过滤选项
        private int minTriangles = 0;
        private int maxTriangles = 100000;
        private bool enableTriangleFilter = false;

        [MenuItem("Tools/FBX Polygons Viewer")]
        static void Init()
        {
            FBXPolygonViewer window = (FBXPolygonViewer) EditorWindow.GetWindow(typeof(FBXPolygonViewer));
            window.titleContent = new GUIContent("FBX面数查看器");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            // 标题区域
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // 目录选择区域
            DrawFolderSelection();
            
            EditorGUILayout.Space(10);
            
            // 控制按钮区域
            DrawControlButtons();
            
            EditorGUILayout.Space(10);
            
            // 统计信息区域
            DrawStatistics();
            
            EditorGUILayout.Space(10);
            
            // 搜索和过滤区域
            DrawSearchAndFilter();
            
            EditorGUILayout.Space(10);
            
            // FBX列表区域
            DrawFBXList();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal("Box");
            GUILayout.Label("FBX 面数查看器", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("帮助", GUILayout.Width(50)))
            {
                ShowHelp();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFolderSelection()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("目录选择", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            folderPath = EditorGUILayout.TextField("FBX目录路径:", folderPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择FBX目录", folderPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 转换为相对路径
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    folderPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 拖拽区域
            DrawDragAndDropArea();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDragAndDropArea()
        {
            EditorGUILayout.Space(5);
            
            // 创建拖拽区域
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            
            // 绘制拖拽区域背景
            GUI.Box(dropArea, "", "TextField");
            
            // 拖拽区域文本和图标
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            centeredStyle.normal.textColor = Color.gray;
            
            GUI.Label(dropArea, "📁 拖拽文件夹到此处", centeredStyle);
            
            // 处理拖拽事件
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;
                    
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        
                        foreach (string draggedPath in DragAndDrop.paths)
                        {
                            // 检查是否为文件夹
                            if (AssetDatabase.IsValidFolder(draggedPath))
                            {
                                folderPath = draggedPath;
                                GUI.changed = true;
                                break; // 只取第一个有效文件夹
                            }
                        }
                        
                        // 也支持拖拽引用对象
                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            string assetPath = AssetDatabase.GetAssetPath(draggedObject);
                            if (AssetDatabase.IsValidFolder(assetPath))
                            {
                                folderPath = assetPath;
                                GUI.changed = true;
                                break; // 只取第一个有效文件夹
                            }
                        }
                    }
                    
                    Event.current.Use();
                    break;
            }
            
            // 拖拽时的视觉反馈
            if (dropArea.Contains(Event.current.mousePosition) && 
                (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform))
            {
                // 绘制高亮边框
                EditorGUI.DrawRect(new Rect(dropArea.x, dropArea.y, dropArea.width, 2), Color.blue);
                EditorGUI.DrawRect(new Rect(dropArea.x, dropArea.y + dropArea.height - 2, dropArea.width, 2), Color.blue);
                EditorGUI.DrawRect(new Rect(dropArea.x, dropArea.y, 2, dropArea.height), Color.blue);
                EditorGUI.DrawRect(new Rect(dropArea.x + dropArea.width - 2, dropArea.y, 2, dropArea.height), Color.blue);
            }
        }
        
        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal("Box");
            
            EditorGUI.BeginDisabledGroup(isScanning);
            if (GUILayout.Button(isScanning ? "扫描中..." : "扫描FBX文件", GUILayout.Height(30)))
            {
                ScanFBXFiles();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(fbxInfoList.Count == 0);
            if (GUILayout.Button("导出CSV", GUILayout.Width(80), GUILayout.Height(30)))
            {
                ExportToCSV();
            }
            
            if (GUILayout.Button("清空列表", GUILayout.Width(80), GUILayout.Height(30)))
            {
                ClearList();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatistics()
        {
            if (fbxInfoList.Count == 0) return;
            
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"文件总数: {totalFiles}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"总三角面数: {totalTriangles:N0}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"总顶点数: {totalVertices:N0}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"总文件大小: {GetFileSizeString(totalFileSize)}");
            EditorGUILayout.EndHorizontal();
            
            if (totalFiles > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"平均面数: {totalTriangles / totalFiles:N0}", GUILayout.Width(150));
                EditorGUILayout.LabelField($"平均顶点数: {totalVertices / totalFiles:N0}", GUILayout.Width(200));
                EditorGUILayout.LabelField($"平均文件大小: {GetFileSizeString(totalFileSize / totalFiles)}");
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSearchAndFilter()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("搜索与过滤", EditorStyles.boldLabel);
            
            // 搜索栏
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("清空", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            
            // 排序选项
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("排序方式:", GUILayout.Width(80));
            SortOption newSort = (SortOption)EditorGUILayout.EnumPopup(currentSort, GUILayout.Width(100));
            if (newSort != currentSort)
            {
                currentSort = newSort;
                SortFBXList();
            }
            
            bool newSortOrder = EditorGUILayout.Toggle("降序", sortDescending, GUILayout.Width(60));
            if (newSortOrder != sortDescending)
            {
                sortDescending = newSortOrder;
                SortFBXList();
            }
            EditorGUILayout.EndHorizontal();
            
            // 三角面数过滤
            EditorGUILayout.BeginHorizontal();
            enableTriangleFilter = EditorGUILayout.Toggle("面数过滤:", enableTriangleFilter, GUILayout.Width(80));
            if (enableTriangleFilter)
            {
                EditorGUILayout.LabelField("最小:", GUILayout.Width(35));
                minTriangles = EditorGUILayout.IntField(minTriangles, GUILayout.Width(80));
                EditorGUILayout.LabelField("最大:", GUILayout.Width(35));
                maxTriangles = EditorGUILayout.IntField(maxTriangles, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();
            
            // 视图选项
            showDetailsView = EditorGUILayout.Toggle("详细视图", showDetailsView);
            if (showDetailsView)
            {
                showPreviewImages = EditorGUILayout.Toggle("显示预览图", showPreviewImages);
            }
            
            if (showPreviewImages)
            {
                useCustomPreviewRender = EditorGUILayout.Toggle("自定义预览渲染", useCustomPreviewRender);
            }
            
            // 性能选项
            useVirtualizedScrolling = EditorGUILayout.Toggle("虚拟化滚动", useVirtualizedScrolling);
            if (GUILayout.Button("清除预览缓存", GUILayout.Width(100)))
            {
                ClearAllPreviewCache();
            }
            if (GUILayout.Button("强制刷新预览", GUILayout.Width(100)))
            {
                ForceRefreshPreviews();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFBXList()
        {
            if (fbxInfoList.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到FBX文件。请点击'扫描FBX文件'按钮开始扫描。", MessageType.Info);
                return;
            }
            
            var filteredList = GetFilteredFBXList();
            
            EditorGUILayout.BeginVertical("Box");
            
            // 列表标题和展开/折叠按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"FBX文件列表 (显示 {filteredList.Count}/{fbxInfoList.Count} 个文件)", EditorStyles.boldLabel);
            
            if (showDetailsView && filteredList.Count > 0)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("全部展开", GUILayout.Width(80)))
                {
                    ExpandAll(filteredList, true);
                }
                if (GUILayout.Button("全部折叠", GUILayout.Width(80)))
                {
                    ExpandAll(filteredList, false);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (showDetailsView)
            {
                DrawDetailedList(filteredList);
            }
            else
            {
                DrawSimpleList(filteredList);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDetailedList(List<FBXInfo> list)
        {
            if (useVirtualizedScrolling && list.Count > 20)
            {
                DrawVirtualizedDetailedList(list);
            }
            else
            {
                DrawStandardDetailedList(list);
            }
        }
        
        private void DrawStandardDetailedList(List<FBXInfo> list)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var fbxInfo in list)
            {
                DrawDetailedItem(fbxInfo);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawVirtualizedDetailedList(List<FBXInfo> list)
        {
            // 计算可视区域
            float viewHeight = position.height - 300f; // 减去其他UI元素的高度
            
            // 计算总高度
            float totalHeight = 0f;
            foreach (var item in list)
            {
                totalHeight += GetItemHeight(item);
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(viewHeight));
            
            // 计算可见项目范围
            int startIndex, endIndex;
            CalculateVisibleRange(list, viewHeight, out startIndex, out endIndex);
            
            // 绘制上方空白区域
            float topSpace = 0f;
            for (int i = 0; i < startIndex; i++)
            {
                topSpace += GetItemHeight(list[i]);
            }
            if (topSpace > 0)
            {
                GUILayout.Space(topSpace);
            }
            
            // 绘制可见项目
            for (int i = startIndex; i <= endIndex && i < list.Count; i++)
            {
                DrawDetailedItem(list[i]);
            }
            
            // 绘制下方空白区域
            float bottomSpace = 0f;
            for (int i = endIndex + 1; i < list.Count; i++)
            {
                bottomSpace += GetItemHeight(list[i]);
            }
            if (bottomSpace > 0)
            {
                GUILayout.Space(bottomSpace);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawDetailedItem(FBXInfo fbxInfo)
        {
            EditorGUILayout.BeginVertical("Box");
            
            // 主要信息行
            EditorGUILayout.BeginHorizontal();
            
            // 预览图(如果启用) - 延迟加载
            if (showPreviewImages)
            {
                // 只有在项目可见时才获取预览图
                Texture2D preview = null;
                if (Event.current.type == EventType.Repaint)
                {
                    preview = fbxInfo.GetPreviewTexture(useCustomPreviewRender);
                }
                
                EditorGUILayout.BeginVertical();
                if (preview != null)
                {
                    // 使预览图可点击，添加边框样式
                    GUIStyle previewStyle = new GUIStyle(GUI.skin.button);
                    previewStyle.padding = new RectOffset(2, 2, 2, 2);
                    previewStyle.border = new RectOffset(1, 1, 1, 1);
                    
                    if (GUILayout.Button(preview, previewStyle, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        PingObject(fbxInfo.filePath);
                    }
                }
                else
                {
                    // 显示占位符，也可点击
                    GUIStyle placeholderStyle = new GUIStyle(GUI.skin.button);
                    placeholderStyle.normal.textColor = Color.gray;
                    placeholderStyle.fontSize = 10;
                    
                    if (GUILayout.Button("无预览", placeholderStyle, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        PingObject(fbxInfo.filePath);
                    }
                }
                

                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.BeginVertical();
            fbxInfo.isExpanded = EditorGUILayout.Foldout(fbxInfo.isExpanded, fbxInfo.fileName, true);
            
            // 关键信息显示
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"面数: {fbxInfo.triangleCount:N0}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"顶点: {fbxInfo.vertexCount:N0}", GUILayout.Width(120));
            EditorGUILayout.LabelField(fbxInfo.FileSizeText, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("定位", GUILayout.Width(50)))
            {
                PingObject(fbxInfo.filePath);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // 展开的详细信息
            if (fbxInfo.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("文件路径:", fbxInfo.filePath);
                EditorGUILayout.LabelField("网格数量:", fbxInfo.meshCount.ToString());
                
                // 预览图状态信息
                if (showPreviewImages)
                {
                    Texture2D preview = fbxInfo.GetPreviewTexture(useCustomPreviewRender);
                    EditorGUILayout.LabelField("预览状态:", preview != null ? "可用" : "无预览");
                }
                
                if (fbxInfo.triangleCount > 10000)
                {
                    EditorGUILayout.HelpBox("高面数模型，可能影响性能！", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private float GetItemHeight(FBXInfo item)
        {
            if (showDetailsView)
            {
                return item.isExpanded ? ITEM_HEIGHT_DETAILED_EXPANDED : ITEM_HEIGHT_DETAILED_COLLAPSED;
            }
            else
            {
                return ITEM_HEIGHT_SIMPLE;
            }
        }
        
        private void CalculateVisibleRange(List<FBXInfo> list, float viewHeight, out int startIndex, out int endIndex)
        {
            startIndex = 0;
            endIndex = list.Count - 1;
            
            float currentY = 0f;
            float scrollY = scrollPosition.y;
            
            // 找到开始索引
            for (int i = 0; i < list.Count; i++)
            {
                float itemHeight = GetItemHeight(list[i]);
                if (currentY + itemHeight > scrollY)
                {
                    startIndex = Mathf.Max(0, i - 1); // 稍微提前一个项目
                    break;
                }
                currentY += itemHeight;
            }
            
            // 找到结束索引
            currentY = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                float itemHeight = GetItemHeight(list[i]);
                currentY += itemHeight;
                if (currentY > scrollY + viewHeight)
                {
                    endIndex = Mathf.Min(list.Count - 1, i + 1); // 稍微延后一个项目
                    break;
                }
            }
        }
        
        private void DrawSimpleList(List<FBXInfo> list)
        {
            if (useVirtualizedScrolling && list.Count > 50)
            {
                DrawVirtualizedSimpleList(list);
            }
            else
            {
                DrawStandardSimpleList(list);
            }
        }
        
        private void DrawStandardSimpleList(List<FBXInfo> list)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var fbxInfo in list)
            {
                DrawSimpleItem(fbxInfo);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawVirtualizedSimpleList(List<FBXInfo> list)
        {
            float viewHeight = position.height - 300f;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(viewHeight));
            
            // 计算可见范围
            int startIndex = Mathf.FloorToInt(scrollPosition.y / ITEM_HEIGHT_SIMPLE);
            int visibleCount = Mathf.CeilToInt(viewHeight / ITEM_HEIGHT_SIMPLE) + 2;
            int endIndex = Mathf.Min(startIndex + visibleCount, list.Count - 1);
            
            startIndex = Mathf.Max(0, startIndex);
            
            // 上方空白
            if (startIndex > 0)
            {
                GUILayout.Space(startIndex * ITEM_HEIGHT_SIMPLE);
            }
            
            // 绘制可见项目
            for (int i = startIndex; i <= endIndex && i < list.Count; i++)
            {
                DrawSimpleItem(list[i]);
            }
            
            // 下方空白
            int remainingItems = list.Count - endIndex - 1;
            if (remainingItems > 0)
            {
                GUILayout.Space(remainingItems * ITEM_HEIGHT_SIMPLE);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSimpleItem(FBXInfo fbxInfo)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(ITEM_HEIGHT_SIMPLE));
            EditorGUILayout.LabelField(fbxInfo.fileName, GUILayout.Width(200));
            EditorGUILayout.LabelField($"{fbxInfo.triangleCount:N0}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"{fbxInfo.vertexCount:N0}", GUILayout.Width(80));
            EditorGUILayout.LabelField(fbxInfo.FileSizeText, GUILayout.Width(80));
            
            if (GUILayout.Button("定位", GUILayout.Width(50)))
            {
                PingObject(fbxInfo.filePath);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private List<FBXInfo> GetFilteredFBXList()
        {
            var filtered = fbxInfoList.Where(fbx => 
            {
                // 文件名搜索过滤
                bool matchesSearch = string.IsNullOrEmpty(searchFilter) || 
                                   fbx.fileName.ToLower().Contains(searchFilter.ToLower());
                
                // 三角面数过滤
                bool matchesTriangleFilter = !enableTriangleFilter || 
                                           (fbx.triangleCount >= minTriangles && fbx.triangleCount <= maxTriangles);
                
                return matchesSearch && matchesTriangleFilter;
            }).ToList();
            
            return filtered;
        }
        
        private void ExpandAll(List<FBXInfo> list, bool expanded)
        {
            foreach (var fbxInfo in list)
            {
                fbxInfo.isExpanded = expanded;
            }
            
            // 强制重绘界面
            Repaint();
        }
        
        private void ClearAllPreviewCache()
        {
            foreach (var fbxInfo in fbxInfoList)
            {
                fbxInfo.ClearPreviewCache();
            }
            Repaint();
        }
        
        private void ForceRefreshPreviews()
        {
            Debug.Log("开始强制刷新所有预览图...");
            
            foreach (var fbxInfo in fbxInfoList)
            {
                fbxInfo.ClearPreviewCache();
                // 立即尝试生成新的预览图
                var preview = fbxInfo.GetPreviewTexture(useCustomPreviewRender);
                if (preview != null)
                {
                    Debug.Log($"成功刷新预览: {fbxInfo.fileName}");
                }
                else
                {
                    Debug.Log($"刷新预览失败: {fbxInfo.fileName}");
                }
            }
            
            Repaint();
            Debug.Log("预览图刷新完成！");
        }
        

        
        private void ScanFBXFiles()
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("错误", "请指定有效的目录路径！", "确定");
                return;
            }
            
            isScanning = true;
            fbxInfoList.Clear();
            
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
                
                for (int i = 0; i < guids.Length; i++)
                {
                    string guid = guids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    // 显示进度
                    float progress = (float)i / guids.Length;
                    EditorUtility.DisplayProgressBar("扫描FBX文件", $"正在分析: {Path.GetFileName(path)}", progress);
                    
                    // 只处理FBX文件
                    if (Path.GetExtension(path).ToLower() == ".fbx")
                    {
                        AnalyzeFBXFile(path);
                    }
                }
                
                CalculateStatistics();
                SortFBXList();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isScanning = false;
            }
        }
        
        private void AnalyzeFBXFile(string path)
        {
            try
            {
                    GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (fbx == null) return;
                
                FBXInfo info = new FBXInfo();
                info.fileName = Path.GetFileName(path);
                info.filePath = path;
                
                // 获取文件大小
                string fullPath = Path.Combine(Application.dataPath, path.Substring(7)); // 移除"Assets/"
                if (File.Exists(fullPath))
                {
                    info.fileSize = new FileInfo(fullPath).Length;
                }
                
                // 分析网格信息
                MeshFilter[] meshFilters = fbx.GetComponentsInChildren<MeshFilter>();
                SkinnedMeshRenderer[] skinnedMeshes = fbx.GetComponentsInChildren<SkinnedMeshRenderer>();
                
                info.meshCount = meshFilters.Length + skinnedMeshes.Length;
                
                // 计算MeshFilter的面数和顶点数
                foreach (MeshFilter mf in meshFilters)
                    {
                        if (mf.sharedMesh != null)
                        {
                        info.triangleCount += mf.sharedMesh.triangles.Length / 3;
                        info.vertexCount += mf.sharedMesh.vertexCount;
                    }
                }
                
                // 计算SkinnedMeshRenderer的面数和顶点数
                foreach (SkinnedMeshRenderer smr in skinnedMeshes)
                {
                    if (smr.sharedMesh != null)
                    {
                        info.triangleCount += smr.sharedMesh.triangles.Length / 3;
                        info.vertexCount += smr.sharedMesh.vertexCount;
                    }
                }
                
                fbxInfoList.Add(info);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"分析FBX文件失败: {path}, 错误: {e.Message}");
            }
        }
        
        private void CalculateStatistics()
        {
            totalFiles = fbxInfoList.Count;
            totalTriangles = fbxInfoList.Sum(f => f.triangleCount);
            totalVertices = fbxInfoList.Sum(f => f.vertexCount);
            totalFileSize = fbxInfoList.Sum(f => f.fileSize);
        }
        
        private void SortFBXList()
        {
            switch (currentSort)
            {
                case SortOption.Name:
                    fbxInfoList = sortDescending 
                        ? fbxInfoList.OrderByDescending(f => f.fileName).ToList()
                        : fbxInfoList.OrderBy(f => f.fileName).ToList();
                    break;
                case SortOption.Triangles:
                    fbxInfoList = sortDescending 
                        ? fbxInfoList.OrderByDescending(f => f.triangleCount).ToList()
                        : fbxInfoList.OrderBy(f => f.triangleCount).ToList();
                    break;
                case SortOption.Vertices:
                    fbxInfoList = sortDescending 
                        ? fbxInfoList.OrderByDescending(f => f.vertexCount).ToList()
                        : fbxInfoList.OrderBy(f => f.vertexCount).ToList();
                    break;
                case SortOption.FileSize:
                    fbxInfoList = sortDescending 
                        ? fbxInfoList.OrderByDescending(f => f.fileSize).ToList()
                        : fbxInfoList.OrderBy(f => f.fileSize).ToList();
                    break;
            }
        }
        
        private void ExportToCSV()
        {
            string csvPath = EditorUtility.SaveFilePanel("导出CSV文件", "", "fbx_polygon_report", "csv");
            if (string.IsNullOrEmpty(csvPath)) return;
            
            try
            {
                using (StreamWriter writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8))
                {
                    // 写入标题行
                    writer.WriteLine("文件名,文件路径,三角面数,顶点数,网格数量,文件大小(字节)");
                    
                    // 写入数据行
                    foreach (var fbx in fbxInfoList)
                    {
                        writer.WriteLine($"{fbx.fileName},{fbx.filePath},{fbx.triangleCount},{fbx.vertexCount},{fbx.meshCount},{fbx.fileSize}");
                    }
                }
                
                EditorUtility.DisplayDialog("导出成功", $"CSV文件已保存到: {csvPath}", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导出失败", $"无法保存CSV文件: {e.Message}", "确定");
            }
        }
        
        private void ClearList()
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空当前列表吗？", "确定", "取消"))
            {
                fbxInfoList.Clear();
                totalFiles = 0;
                totalTriangles = 0;
                totalVertices = 0;
                totalFileSize = 0;
            }
        }
        
        private void PingObject(string path)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }
        
        private void ShowHelp()
        {
            string helpText = @"FBX面数查看器使用说明：

1. 目录选择：手动输入路径、浏览选择或拖拽文件夹
2. 扫描FBX文件：分析指定目录下的所有FBX文件
3. 搜索与过滤：按文件名搜索，按面数范围过滤
4. 排序：按文件名、面数、顶点数或文件大小排序
5. 详细视图：切换详细信息显示模式
6. 定位：点击定位按钮或预览图定位到文件
7. 导出CSV：将结果导出为CSV表格文件

预览图功能：
- 显示真实3D模型预览（非文件图标）
- 点击预览图可直接定位到文件
- 支持自定义预览渲染模式

性能优化：
- 虚拟化滚动：大列表时自动启用
- 预览缓存：避免重复生成
- 一键清除缓存释放内存

提示：
- 高面数模型（>10000面）会有性能警告
- 支持MeshFilter和SkinnedMeshRenderer组件";

            EditorUtility.DisplayDialog("帮助", helpText, "确定");
        }
        
        private string GetFileSizeString(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            return $"{bytes / (1024 * 1024):F1} MB";
        }
    }
}