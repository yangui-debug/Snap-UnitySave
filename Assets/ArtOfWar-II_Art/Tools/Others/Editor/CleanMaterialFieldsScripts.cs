using UnityEditor;
using UnityEngine;

public class CleanMaterialFieldsScripts : EditorWindow
{
    public DefaultAsset folder;

    [MenuItem("Tools/Art/Clean Material Fields")]
    static void ShowWindow()
    {
        GetWindow<CleanMaterialFieldsScripts>("Clean Material Fields Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Clean Material Fields", EditorStyles.boldLabel);
        folder = EditorGUILayout.ObjectField("FBX Folder Path:", folder, typeof(DefaultAsset), false) as DefaultAsset;
        if (GUILayout.Button("Clean"))
        {
            string tempFolder = AssetDatabase.GetAssetPath(folder);
            CleanMaterials(tempFolder);
        }
    }

    private static void CleanMaterials(string folderPath)
    {
        // 获取目标文件夹中的所有材质
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        int cleanedCount = 0;
        foreach (string guid in guids)
        {
            // 获取材质的路径和实例
            string materialPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                continue;
            }

            CleanMaterialFields(material);
            cleanedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Finished cleaning materials. Total cleaned: {cleanedCount}");
    }

    [MenuItem("Assets/Clean Material by Creating Clean Copy", true)]
    private static bool ValidateCleanMaterialByCreatingCopy()
    {
        // 确保选中的是一个材质球
        return Selection.activeObject is Material;
    }

    [MenuItem("Assets/Clean Material by Copying Matching Properties")]
    private static void CleanMaterialByMatchingCopying()
    {
        // 获取选中的材质
        Material oldMaterial = Selection.activeObject as Material;
        if (!oldMaterial)
        {
            Debug.LogError("Please select a material to clean.");
            return;
        }

        CleanMaterialFields(oldMaterial);
        AssetDatabase.SaveAssets();
    }

    private static void CleanMaterialFields(Material mat)
    {
        // 获取材质的 Shader
        Shader shader = mat.shader;
        if (shader == null)
        {
            Debug.LogError($"Material '{mat.name}' does not have a valid shader.");
            return;
        }

        // 创建一个新的材质
        Material newMaterial = new Material(shader);

        // 使用 CopyMatchingPropertiesFromMaterial 复制与 Shader 匹配的属性
        newMaterial.CopyMatchingPropertiesFromMaterial(mat);

        // 保存新的材质并覆盖旧材质
        string oldMaterialPath = AssetDatabase.GetAssetPath(mat);
        AssetDatabase.CreateAsset(newMaterial, oldMaterialPath);
    }
}