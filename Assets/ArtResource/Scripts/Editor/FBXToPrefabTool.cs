using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FBXToPrefabTool : EditorWindow
{
    // 当前选择的Tab页索引
    private int selectedTab = 0;
    private string[] tabs = { "生成模型预制体", "修改模型预制体" };
    private GameObject selectedPrefab;
    
    string fbxDirectory = "";
    string prefabDirectory = "";
    private float targetScale = 5f;
    private List<string> draggedFolders = new List<string>();
    
    private const string FBX_DIR_KEY = "FBXToPrefab_LastFBXDirectory";
    private const string PREFAB_DIR_KEY = "FBXToPrefab_LastPrefabDirectory";
    private const string TARGET_SCALE_KEY = "FBXToPrefab_LastTargetScale";

    [MenuItem("Tools/FBX To Prefab Converter")]
    public static void ShowWindow()
    {
        var window = GetWindow<FBXToPrefabTool>("FBX To Prefab Converter");
        
        window.minSize = new Vector2(300, 200);  // 最小尺寸
    }

    private void OnEnable()
    {
        // Load the previously saved paths
        fbxDirectory = EditorPrefs.GetString(FBX_DIR_KEY, "");
        prefabDirectory = EditorPrefs.GetString(PREFAB_DIR_KEY, "");
        targetScale = EditorPrefs.GetFloat(TARGET_SCALE_KEY, 5f);
    }
    
    void OnGUI()
    {
        // Tab切换
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        switch (selectedTab)
        {
            case 0:
                DrawFBXToPrefabConverter();
                break;
            case 1:
                DrawModifyPrefabScale();
                break;
        }
    }

    private void DrawFBXToPrefabConverter()
    {
        GUILayout.Label("生成模型预制体", EditorStyles.boldLabel);

        GUILayout.Label("模型路径: " + fbxDirectory);
        if (GUILayout.Button("选择模型路径", GUILayout.Width(150)))
        {
            // fbxDirectory = EditorUtility.OpenFolderPanel("Select Folder Containing FBX Files", "", "");
            
            // Open folder panel, using the last saved path if available
            string selectedFbxDir = EditorUtility.OpenFolderPanel("Select Folder Containing FBX Files", fbxDirectory, "");
            
            if (!string.IsNullOrEmpty(selectedFbxDir))
            {
                fbxDirectory = selectedFbxDir;
                EditorPrefs.SetString(FBX_DIR_KEY, fbxDirectory); // Save the path
            }
        }
        
        GUILayout.Label("保存路径: " + prefabDirectory);
        if (GUILayout.Button("选择保存路径", GUILayout.Width(150)))
        {
            string selectedPrefabDir = EditorUtility.OpenFolderPanel("Select Folder to Save Prefabs", prefabDirectory, "");

            if (!string.IsNullOrEmpty(selectedPrefabDir))
            {
                prefabDirectory = selectedPrefabDir;
                EditorPrefs.SetString(PREFAB_DIR_KEY, prefabDirectory); // Save the path
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("设置目标尺寸", GUILayout.Width(150));
        targetScale = EditorGUILayout.Slider(targetScale, 0.01f, 1000f, GUILayout.Width(200));
        EditorPrefs.SetFloat(TARGET_SCALE_KEY, targetScale); // Save the scale
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("拖拽文件夹到下方区域:", EditorStyles.boldLabel);
        Rect dragArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dragArea, "将文件夹拖拽到此区域", EditorStyles.helpBox);

        HandleFolderDrag(dragArea);

        GUILayout.Label("已选择的文件夹:");
        if (draggedFolders.Count > 0)
        {
            for (int i = 0; i < draggedFolders.Count; i++)
            {
                // GUILayout.Label(folder, EditorStyles.label);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(draggedFolders[i], EditorStyles.label);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    draggedFolders.RemoveAt(i);
                    i--; // 因为列表改变了长度，所以需要调整索引
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label("无", EditorStyles.label);
        }

        if (GUILayout.Button("处理模型", GUILayout.Width(150)))
        {
            // ConvertFBXToPrefab();
            
            foreach (var folder in draggedFolders)
            {
                ProcessFBXInFolder(folder);
                
            }
EditorUtility.DisplayDialog("完成", "预制体生成完毕!", "好的");
        }
    }
    
    private void DrawModifyPrefabScale()
    {
        GUILayout.Label("修改模型预制体", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("选择目标预制体", GUILayout.Width(150));
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("选择预制体", selectedPrefab, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("设置目标尺寸", GUILayout.Width(150));
        targetScale = EditorGUILayout.Slider(targetScale, 0.01f, 1000f, GUILayout.Width(200));
        EditorPrefs.SetFloat(TARGET_SCALE_KEY, targetScale); // Save the scale
        EditorGUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // 左侧空白

        // 修改并保存预制体的按钮
        if (GUILayout.Button("保存修改", GUILayout.Width(200)))
        {
            ModifyPrefabScale();
        }
        
        GUILayout.FlexibleSpace(); // 右侧空白
        GUILayout.EndHorizontal();
    }
    
    private void HandleFolderDrag(Rect dropArea)
    {
        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
        {
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        if (Directory.Exists(draggedPath) && !draggedFolders.Contains(draggedPath))
                        {
                            draggedFolders.Add(draggedPath);
                        }
                    }
                }
            }
        }
    }
    
    private void ProcessFBXInFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(prefabDirectory))
        {
            Debug.LogError("Folder path or Prefab save folder not selected!");
            return;
        }

        string[] fbxFiles = Directory.GetFiles(folderPath, "*.fbx", SearchOption.AllDirectories);

        foreach (string fbxFile in fbxFiles)
        {
            // string relativeFbxFilePath = "Assets" + fbxFile.Substring(Application.dataPath.Length);
            string relativeFbxFilePath = fbxFile;
            GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(relativeFbxFilePath);

            if (fbxObject != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxObject);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                ChangeModelScale(instance, targetScale);

                GameObject root = new GameObject(fbxObject.name);
                instance.transform.SetParent(root.transform);
                root.transform.position = Vector3.zero;

                string relativePrefabPath = "Assets" + prefabDirectory.Substring(Application.dataPath.Length);
                string prefabPath = Path.Combine(relativePrefabPath, fbxObject.name + ".prefab");

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                DestroyImmediate(root);

                Debug.Log("Prefab created at: " + prefabPath);
            }
            else
            {
                Debug.LogError("Failed to load FBX: " + relativeFbxFilePath);
            }
        }

        AssetDatabase.Refresh();
    }
    
    void ConvertFBXToPrefab()
    {
        if (string.IsNullOrEmpty(fbxDirectory) || string.IsNullOrEmpty(prefabDirectory))
        {
            Debug.LogError("FBX folder or Prefab save folder not selected!");
            return;
        }

        // Get relative paths for the directories
        string relativeFbxPath = "Assets" + fbxDirectory.Substring(Application.dataPath.Length);
        string relativePrefabPath = "Assets" + prefabDirectory.Substring(Application.dataPath.Length);

        // Find all FBX files in the selected folder
        string[] fbxFiles = Directory.GetFiles(fbxDirectory, "*.fbx", SearchOption.AllDirectories);

        foreach (string fbxFile in fbxFiles)
        {
            // Convert the full path to a relative path
            string relativeFbxFilePath = "Assets" + fbxFile.Substring(Application.dataPath.Length);

            // Load the FBX as a GameObject
            GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(relativeFbxFilePath);

            if (fbxObject != null)
            {
                // Instantiate the FBX object in the scene
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxObject);
                
                // Detach the object from its prefab connection to make it an independent object
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                // 处理预制体尺寸和层级规范
                ChangeModelScale(instance, targetScale);

                GameObject root = new GameObject(fbxObject.name);
                root.transform.position = Vector3.zero;
                instance.transform.SetParent(root.transform);
                root.transform.localPosition = Vector3.zero;
                
                
                // Generate prefab path
                string prefabPath = Path.Combine(relativePrefabPath, fbxObject.name + ".prefab");
                
                if (File.Exists(prefabPath))
                {
                    // Prompt the user to either replace or skip the prefab
                    bool replace = EditorUtility.DisplayDialog("Prefab Exists", 
                        $"The prefab '{fbxObject.name}' already exists. Do you want to replace it?", 
                        "Replace", "Skip");

                    if (!replace)
                    {
                        // User chose to skip, so we continue to the next prefab
                        DestroyImmediate(root);
                        continue;
                    }
                }

                // Save the instantiated object as a prefab
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                // Optionally destroy the instance after saving
                DestroyImmediate(root);

                Debug.Log("Prefab created at: " + prefabPath);
            }
            else
            {
                Debug.LogError("Failed to load FBX: " + relativeFbxFilePath);
            }
        }

        AssetDatabase.Refresh();
        
        // 弹出执行成功的提示框
        EditorUtility.DisplayDialog("完成", "预制体生成完毕!", "好的");
    }

    void ChangeModelScale(GameObject model, float targetScale)
    {
        if (model != null)
        {
            // 获取模型中的 Mesh 对象
            var renderer = model.GetComponentInChildren<Renderer>();
            // MeshFilter meshFilter = fbxModel.GetComponentInChildren<MeshFilter>();
            if (renderer != null)
            {
                // 尺寸信息
                Vector3 meshSize = renderer.bounds.size;
                Debug.Log("Mesh Size: " + meshSize);

                var realSize = targetScale / renderer.bounds.size.y;

                var originalScale = model.transform.localScale;
                    
                model.transform.localScale = new Vector3(originalScale.x * realSize, originalScale.y * realSize, originalScale.z * realSize);
            }
            else
            {
                Debug.LogError("No renderer found in the FBX model.");
            }
                
            // DestroyImmediate(fbxModel);
        }
        else
        {
            Debug.LogError("Failed to load FBX model as GameObject.");
        }
    }
    
    private void ModifyPrefabScale()
    {
        if (selectedPrefab == null)
        {
            Debug.LogError("Please select a prefab.");
            return;
        }

        // 获取预制体的路径
        string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Failed to get prefab path.");
            return;
        }

        // 实例化预制体来修改其子节点
        GameObject instantiatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);

        // 处理预制体尺寸和层级规范
        ChangeModelScale(instantiatedPrefab.transform.GetChild(0).gameObject, targetScale);

        // 保存修改后的预制体
        PrefabUtility.SaveAsPrefabAsset(instantiatedPrefab, prefabPath);
        DestroyImmediate(instantiatedPrefab);

        Debug.Log("Prefab modified and saved successfully!");
        
        // 弹出执行成功的提示框
        EditorUtility.DisplayDialog("完成", "预制体修改成功!", "好的");
    }
}
