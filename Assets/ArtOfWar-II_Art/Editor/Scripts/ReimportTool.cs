using System;
using UnityEditor;
using UnityEngine;

public class ReimportTool
{
    // 参数格式: -reimportPath Assets/Textures/MyTexture.png
    public static void ReimportFromArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-reimportPath")
            {
                string assetPath = args[i + 1];
                Debug.Log("Reimporting asset at path: " + assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                break;
            }
        }

        // 退出 Unity
        EditorApplication.Exit(0);
    }
}