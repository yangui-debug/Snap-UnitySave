using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArtOfWar_II_Art.Tools
{
    public static class EditorUtilities
    {
        /// <summary>
        /// 确保对应目录存在, 如果不存在则会自动创建一个
        /// </summary>
        /// <param name="path"></param>
        public static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentDirectory = Path.GetDirectoryName(path);
                if (!AssetDatabase.IsValidFolder(parentDirectory))
                {
                    EnsureDirectoryExists(parentDirectory);
                }

                AssetDatabase.CreateFolder(parentDirectory, Path.GetFileName(path));
            }
        }

        /// <summary>
        /// 返回一个名字精准匹配的资源
        /// </summary>
        /// <returns></returns>
        public static T FindAsset<T>(string name) where T : Object
        {
            return FindAsset<T>(name, "Assets");
        }

        public static T FindAsset<T>(string name, string path) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name} {name}", new[] { path });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetFileNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);

                if (assetFileNameWithoutExtension == name)
                {
                    return AssetDatabase.LoadAssetAtPath<T>(assetPath);
                }
            }

            return default;
        }

        /// <summary>
        /// 获取fbx上所有的动画clip
        /// </summary>
        public static Dictionary<string, AnimationClip> GetFbxAnimClips(string fbxPath)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            return GetFbxAnimClips(fbx);
        }

        /// <summary>
        /// 获取fbx上所有的动画clip
        /// </summary>
        public static Dictionary<string, AnimationClip> GetFbxAnimClips(GameObject fbx)
        {
            string fbxPath;
            PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(fbx);
            if (assetType == PrefabAssetType.Variant)
            {
                GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(fbx);
                fbxPath = AssetDatabase.GetAssetPath(src); // 原始资源
            }
            else
            {
                fbxPath = AssetDatabase.GetAssetPath(fbx); // 获取FBX文件的路径
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            return assets.OfType<AnimationClip>().ToDictionary(k => k.name, v => v);
        }
    }
}