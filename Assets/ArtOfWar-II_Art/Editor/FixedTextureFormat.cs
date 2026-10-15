// using System;
// using System.Collections.Generic;
// using System.IO;
// using UnityEditor;
// using UnityEngine;
//
// public class FixedTextureFormat : AssetPostprocessor
// {
//     // 自动控制图片格式的文件夹路径(默认普通精度压缩)
//     private readonly HashSet<string> NormalPathList = new HashSet<string>
//     {
//         $"Assets/ArtResource/Sprites",
//     };
//
//     // 该文件夹下图片使用高精度压缩
//     private readonly HashSet<string> HighPathList = new HashSet<string>
//     {
//         // "Assets/IslandWar_ArtResource/0.UI/0.Common/Bg_noCrunch",
//     };
//
//     // 该文件夹下图片使用低精度压缩
//     private readonly HashSet<string> LowPathList = new HashSet<string>
//     {
//     };
//
//     //若 图片/文件夹 有该后缀，则使用高精度压缩
//     public const string NoCrunch = "_noCrunch";
//
//     //若 图片/文件夹 有该后缀，则忽略
//     public const string Ignore = "_ignore";
//
//     #region 内部方法
//
//     public const TextureImporterFormat NormalFormat = TextureImporterFormat.ETC2_RGBA8;
//     public const TextureImporterFormat NormalFormatJpg = TextureImporterFormat.ETC2_RGB4;
//     public const TextureImporterFormat HighFormat = TextureImporterFormat.ETC2_RGBA8;
//     public const TextureImporterFormat LowFormat = TextureImporterFormat.ETC2_RGBA8Crunched;
//
//     private enum FormatType
//     {
//         Normal,
//         High,
//         Low,
//     }
//
//     void OnPostprocessTexture(Texture2D texture)
//     {
//         string path = assetPath;
//         if (path.Contains(Ignore))
//         {
//             return;
//         }
//
//         bool needChange = false;
//         foreach (var white in NormalPathList)
//         {
//             if (path.StartsWith(white))
//             {
//                 needChange = true;
//             }
//         }
//
//         if (!needChange)
//         {
//             return;
//         }
//
//         TextureImporter textureImporter = assetImporter as TextureImporter;
//
//         if (textureImporter != null)
//         {
//             textureImporter.textureType = TextureImporterType.Sprite;
//             textureImporter.spriteImportMode = SpriteImportMode.Single;
//             textureImporter.alphaIsTransparency = true;
//             textureImporter.mipmapEnabled = false;
//             textureImporter.wrapMode = TextureWrapMode.Clamp;
//             if (textureImporter.maxTextureSize > 4096)
//             {
//                 textureImporter.maxTextureSize = 4096;
//             }
//
//             FormatType formatType;
//             if (path.Contains(NoCrunch) || IsContainsInPathList(path, HighPathList))
//             {
//                 formatType = FormatType.High;
//             }
//             else if (IsContainsInPathList(path, LowPathList))
//             {
//                 formatType = FormatType.Low;
//             }
//             else
//             {
//                 formatType = FormatType.Normal;
//             }
//
//             string extension = Path.GetExtension(path);
//             TextureImporterFormat textureImporterFormat = GetTextureImporterFormat(formatType, extension);
//
//             var settingAndroid = textureImporter.GetPlatformTextureSettings("Android");
//             settingAndroid.overridden = true;
//             settingAndroid.format = textureImporterFormat;
//             settingAndroid.compressionQuality = 100;
//             if (settingAndroid.maxTextureSize > 4096)
//             {
//                 settingAndroid.maxTextureSize = 4096;
//             }
//
//             textureImporter.SetPlatformTextureSettings(settingAndroid);
//
//             var settingIos = textureImporter.GetPlatformTextureSettings("iPhone");
//             settingIos.overridden = true;
//             settingIos.format = textureImporterFormat;
//             settingIos.compressionQuality = 100;
//             if (settingIos.maxTextureSize > 4096)
//             {
//                 settingIos.maxTextureSize = 4096;
//             }
//
//             textureImporter.SetPlatformTextureSettings(settingIos);
//         }
//     }
//
//     private static bool IsContainsInPathList(string path, HashSet<string> pathList)
//     {
//         bool isContainsInPath = false;
//         if (pathList != null && pathList.Count > 0)
//         {
//             foreach (var noCrunch in pathList)
//             {
//                 if (path.StartsWith(noCrunch))
//                 {
//                     isContainsInPath = true;
//                 }
//             }
//         }
//
//         return isContainsInPath;
//     }
//
//     private static TextureImporterFormat GetTextureImporterFormat(FormatType formatType, string extension)
//     {
//         switch (formatType)
//         {
//             case FormatType.High:
//                 return HighFormat;
//             case FormatType.Low:
//                 return LowFormat;
//             default:
//                 return !string.IsNullOrEmpty(extension) && extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ? NormalFormatJpg : NormalFormat;
//         }
//     }
//
//     #endregion
// }