using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FBXAnimationRenamer : EditorWindow
{
    private string beforeStr;
    private string afterStr;
    private DefaultAsset folder;

    [MenuItem("Tools/FBX Animation Renamer")]
    private static void ShowWindow()
    {
        GetWindow<FBXAnimationRenamer>("FBX Animation Renamer");
    }

    private void OnGUI()
    {
        GUILayout.Label("指定目录", EditorStyles.boldLabel);
        folder = EditorGUILayout.ObjectField("FBX Folder Path:", folder, typeof(DefaultAsset), false) as DefaultAsset;
        beforeStr = EditorGUILayout.TextField("Before:", beforeStr);
        afterStr = EditorGUILayout.TextField("After (空=删除):", afterStr);

        if (GUILayout.Button("开始处理"))
        {
            if (folder == null)
            {
                Debug.LogError("请选择一个文件夹！");
                return;
            }

            string tempFolder = AssetDatabase.GetAssetPath(folder);
            ProcessFBXFiles(beforeStr.Split(","), afterStr.Split(","), tempFolder);
        }
    }

    private void ProcessFBXFiles(string[] beforeArr, string[] afterArr, string directoryPath)
    {
        string[] fbxFiles = Directory.GetFiles(directoryPath, "*.fbx", SearchOption.AllDirectories);

        foreach (string fbxPath in fbxFiles)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

            if (importer == null)
            {
                Debug.LogWarning($"无法获取 ModelImporter: {fbxPath}");
                continue;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                continue;
            }

            bool hasChanged = false;

            for (int i = 0; i < beforeArr.Length; i++)
            {
                string before = beforeArr[i].Trim();
                string after = afterArr.Length > i ? afterArr[i].Trim() : string.Empty;
                if (string.IsNullOrEmpty(after))
                {
                    // 删除指定动画
                    ModelImporterClipAnimation[] newClips = clips.Where(c => c.name != before).ToArray();
                    if (newClips.Length != clips.Length)
                    {
                        importer.clipAnimations = newClips;
                        hasChanged = true;
                        Debug.Log($"已删除动画 {beforeArr}：{fbxPath}");
                    }
                }
                else
                {
                    // 改名指定动画
                    foreach (ModelImporterClipAnimation clip in clips)
                    {
                        if (clip.name == before)
                        {
                            clip.name = after;
                            hasChanged = true;
                            Debug.Log($"修改动画：{fbxPath} 中的 {before} -> {after}");
                        }
                    }

                    if (hasChanged)
                    {
                        importer.clipAnimations = clips;
                    }
                }
            }

            if (hasChanged)
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"已保存修改：{fbxPath}");
            }
        }

        Debug.Log("处理完毕！");
    }
}