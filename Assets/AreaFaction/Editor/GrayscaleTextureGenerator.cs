using UnityEngine;
using UnityEditor;
using System.IO;

public class GrayscaleTextureGenerator
{
    [MenuItem("Assets/Tools/1. 生成灰白底图 (Grayscale)")]
    public static void GenerateGrayscale()
    {
        Texture2D selectedTex = Selection.activeObject as Texture2D;
        if (selectedTex == null)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口选中一张贴图！", "确定");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedTex);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer == null)
        {
            EditorUtility.DisplayDialog("错误", "无法读取该文件的导入设置，请确保选中了一张真实的贴图文件。", "确定");
            return;
        }

        // 1. 自动开启 Read/Write 以及强行转成未压缩格式（否则 GetPixels 会失败）
        bool wasReadable = importer.isReadable;
        TextureImporterCompression wasCompression = importer.textureCompression;
        
        if (!wasReadable || wasCompression != TextureImporterCompression.Uncompressed)
        {
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path);
        }

        // 2. 核心修复：强制使用 TextureFormat.RGBA32 这种万能未压缩格式来接收像素！
        Texture2D newTex = new Texture2D(selectedTex.width, selectedTex.height, TextureFormat.RGBA32, false);
        
        Color[] pixels = selectedTex.GetPixels();
        
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            // 经典的灰度计算公式，保留完美的明暗对比
            float gray = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            pixels[i] = new Color(gray, gray, gray, c.a);
        }
        
        newTex.SetPixels(pixels);
        newTex.Apply();

        // 3. 恢复原贴图的设置，不破坏你的原资产
        if (!wasReadable || wasCompression != TextureImporterCompression.Uncompressed)
        {
            importer.isReadable = wasReadable;
            importer.textureCompression = wasCompression;
            AssetDatabase.ImportAsset(path);
        }

        // 4. 保存新文件
        byte[] bytes = newTex.EncodeToPNG();
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string newPath = Path.Combine(dir, name + "_Gray.png");
        
        File.WriteAllBytes(newPath, bytes);
        AssetDatabase.Refresh();
        
        Debug.Log($"[成功] 完美灰白贴图已生成并保存在: {newPath}");
    }
}