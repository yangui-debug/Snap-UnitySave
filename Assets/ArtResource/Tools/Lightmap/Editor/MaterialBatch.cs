using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace i89Trillion.Art.Tools
{
    public class MaterialQueueModifier : MonoBehaviour
    {
        [MenuItem("Tools/Modify Material Render Queue")]
        private static void ModifyRenderQueue()
        {
            // 设置起始Render Queue值
            int startRenderQueue = 3000;

            // 固定的材质文件夹路径
            string[] materialFolders =
            {
                "Assets/ArtResource/Effects/Common_FX/Materials",
                "Assets/ArtResource/Effect/Materials"
            };

            // 用于存储所有材质文件路径的列表
            List<string> materialPaths = new List<string>();

            // 遍历每个文件夹并递归获取所有材质文件
            foreach (string folderPath in materialFolders)
            {
                GetMaterialPaths(folderPath, materialPaths);
                Debug.LogWarning($"特效合批文件夹路径{folderPath}");
            }

            // 递增Render Queue
            int currentRenderQueue = startRenderQueue;

            // 修改每个材质的Render Queue
            foreach (string path in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                {
                    material.renderQueue = currentRenderQueue;
                    currentRenderQueue++; // 每次递增
                    Debug.Log($"Modified Render Queue of {material.name} to {material.renderQueue}");
                }
                else
                {
                    Debug.LogWarning($"Failed to load material at path: {path}");
                }
            }

            // 保存所有修改
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Completed modifying material Render Queue.");
        }

        // 递归获取指定文件夹及其子文件夹中的所有材质文件
        private static void GetMaterialPaths(string folderPath, List<string> materialPaths)
        {
            // 获取当前文件夹中的所有材质文件
            string[] materialsInFolder = Directory.GetFiles(folderPath, "*.mat", SearchOption.AllDirectories);

            // 添加到列表中
            materialPaths.AddRange(materialsInFolder);
        }
    }
}