using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ArtOfWar_II_Art.Editor.Importers
{
    [InitializeOnLoad]
    public class BattleUnitImporter : AssetPostprocessor
    {
        // private static readonly HashSet<string> texSuffix = new HashSet<string>
        // {
        //     "C",
        //     "A",
        //     "N",
        //     "M",
        //     "E",
        //     "O",
        // };
        //
        // void OnPostprocessTexture(Texture2D texture)
        // {
        //     return; // TODO 未完善, 先关闭
        //     if (!BasicCheck())
        //     {
        //         return;
        //     }
        //
        //     string suffix = GetSuffix(Path.GetFileNameWithoutExtension(assetPath));
        //     if (!texSuffix.Contains(suffix))
        //     {
        //         EditorUtility.DisplayDialog("错误", $"{assetPath} 后缀请按照贴图规范命名", "好的");
        //         AssetDatabase.DeleteAsset(assetPath);
        //         return;
        //     }
        //
        //     if (assetImporter is TextureImporter texImporter)
        //     {
        //         int maxSize = 1024;
        //         if (texture.texelSize.x > maxSize || texture.texelSize.y > maxSize)
        //         {
        //             float before = Mathf.Max(texture.texelSize.x, texture.texelSize.y);
        //             TextureImporterPlatformSettings defaultSetting = texImporter.GetDefaultPlatformTextureSettings();
        //             defaultSetting.maxTextureSize = 1024;
        //             texImporter.SetPlatformTextureSettings(defaultSetting);
        //
        //             EditorUtility.DisplayDialog("警告", $"{assetPath}贴图大小为: {before}, 已自动修改为{maxSize}", "好的");
        //         }
        //     }
        // }
        //
        // void OnPostprocessMaterial(Material material)
        // {
        //     return; // TODO 未完善, 先关闭
        //     if (!BasicCheck())
        //     {
        //         return;
        //     }
        // }
        //
        // void OnPostprocessModel(GameObject g)
        // {
        //     return; // TODO 未完善, 先关闭
        //     if (!BasicCheck())
        //     {
        //         return;
        //     }
        // }

        private bool BasicCheck()
        {
            if (!CanBeProcessed())
            {
                return false;
            }

            // 目录检查
            if (!FolderOK())
            {
                EditorUtility.DisplayDialog("错误", $"该目录的资源必须放在BattleUnit_x/ High或者Low 下, {assetPath}已自动删除, 请重新添加!", "好的");
                AssetDatabase.DeleteAsset(assetPath);
                return false;
            }

            // 命名检查
            if (!NameOk(out string prefix, out string other, out string[] allDirectories))
            {
                int choice = EditorUtility.DisplayDialogComplex("错误",
                    $"{assetPath} 命名和相关资源({other})不匹配, 请统一规范",
                    $"统一使用{prefix}",
                    "删除这个资源",
                    $"统一使用{other}");

                switch (choice)
                {
                    case 0:
                        rename(prefix);
                        break;
                    case 1:
                        AssetDatabase.DeleteAsset(assetPath);
                        break;
                    case 2:
                        rename(other);
                        break;
                }

                void rename(string newPrefix)
                {
                    foreach (string tmp in allDirectories)
                    {
                        RenameAllAssets(tmp, newPrefix);
                    }

                    AssetDatabase.SaveAssets();
                }

                return false;
            }

            return true;
        }

        private bool CanBeProcessed()
        {
            if (!assetPath.StartsWith(ImportSettings.CheckTexturePath))
            {
                return false;
            }

            return true;
        }

        private bool FolderOK()
        {
            string parentPath = Path.GetDirectoryName(assetPath);
            string dName = Path.GetFileName(parentPath);
            if (string.IsNullOrEmpty(dName) || (!dName.Equals("High") && !dName.Equals("Low")))
            {
                return false;
            }

            parentPath = Path.GetDirectoryName(parentPath);
            if (!Regex.IsMatch(Path.GetFileName(parentPath), @"BattleUnit_\d+"))
            {
                return false;
            }

            return true;
        }

        private bool NameOk(out string prefix, out string otherPrefix, out string[] allDirectories)
        {
            string parentDirectory = Path.GetDirectoryName(Path.GetDirectoryName(assetPath));
            allDirectories = AssetDatabase.GetSubFolders(parentDirectory);
            prefix = GetPrefix(Path.GetFileNameWithoutExtension(assetPath));

            foreach (string directory in allDirectories)
            {
                string[] allAssets = AssetDatabase.FindAssets("t:Object", new[] {directory});
                if (allAssets.Length > 1)
                {
                    foreach (string assetGUID in allAssets)
                    {
                        string assetFile = AssetDatabase.GUIDToAssetPath(assetGUID);
                        otherPrefix = GetPrefix(Path.GetFileNameWithoutExtension(assetFile));
                        if (prefix != otherPrefix)
                        {
                            return false;
                        }
                    }
                }
            }

            otherPrefix = string.Empty;
            return true;
        }

        private string GetPrefix(string fileName)
        {
            return fileName.Split('_').First();
        }

        private string GetSuffix(string fileName)
        {
            return fileName.Split('_').Last();
        }

        private void RenameAllAssets(string directory, string newPrefix)
        {
            string[] allAssets = AssetDatabase.FindAssets("t:Object", new[] {directory});
            foreach (string assetGUID in allAssets)
            {
                string tempPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                string fileName = Path.GetFileNameWithoutExtension(tempPath);
                string newFileName = newPrefix + fileName.Substring(GetPrefix(fileName).Length);
                AssetDatabase.RenameAsset(tempPath, newFileName);
            }
        }
    }
}