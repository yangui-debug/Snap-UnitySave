using System.IO;
using UnityEditor;
using UnityEngine;

public class FBXCompressionEditor : EditorWindow
{
    public DefaultAsset folder;
    private float sizeThreshold = 10;

    [MenuItem("Tools/Art/Set FBX Animation Compression to Optimal")]
    static void ShowWindow()
    {
        GetWindow<FBXCompressionEditor>("FBX Compression Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Set FBX Animation Compression to Optimal", EditorStyles.boldLabel);
        folder = EditorGUILayout.ObjectField("FBX Folder Path:", folder, typeof(DefaultAsset), false) as DefaultAsset;
        sizeThreshold = EditorGUILayout.FloatField("Size Threshold:", sizeThreshold);

        if (GUILayout.Button("Set Compression to Optimal"))
        {
            string tempFolder = AssetDatabase.GetAssetPath(folder);
            SetFBXCompressionToOptimal(tempFolder, sizeThreshold);
        }
    }

    static void SetFBXCompressionToOptimal(string folderPath, float compressionSizeThreshold)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("The specified folder path does not exist.");
            return;
        }

        // 获取目标文件夹中的所有fbx
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(assetPath) is not ModelImporter modelImporter)
            {
                continue;
            }

            bool changed = false;
            if (modelImporter.importAnimation && modelImporter.animationCompression != ModelImporterAnimationCompression.Optimal)
            {
                modelImporter.animationCompression = ModelImporterAnimationCompression.Optimal;
                changed = true;
            }

            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            MeshFilter[] meshFilters = fbxAsset.GetComponentsInChildren<MeshFilter>();

            float maxSize = 0;
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh)
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    Bounds bounds = mesh.bounds;
                    Vector3 localSize = bounds.size;
                    maxSize = Mathf.Max(maxSize, localSize.x, localSize.y, localSize.z);
                }
            }

            ModelImporterMeshCompression meshCompression =
                maxSize < compressionSizeThreshold ? ModelImporterMeshCompression.High : ModelImporterMeshCompression.Off;
            if (modelImporter.meshCompression != meshCompression)
            {
                modelImporter.meshCompression = meshCompression;
                changed = true;
            }

            if (changed)
            {
                modelImporter.SaveAndReimport(); // 保存更改并重新导入FBX
                Debug.Log($"Set animation compression to Optimal for: {assetPath}");
            }
        }

        Debug.Log("Compression settings updated for all FBX files in the folder.");
    }
}