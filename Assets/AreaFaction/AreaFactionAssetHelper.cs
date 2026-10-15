using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Com.i89.AreaFaction
{
    /// <summary>
    /// 资产保存/加载帮助类（仅 Editor 可用）
    /// </summary>
    public static class AreaFactionAssetHelper
    {
        private const string DefaultFolder = "Assets/Generated/AreaFaction";

        /// <summary>
        /// 保存阵营 Mesh 资产
        /// </summary>
        /// <param name="combinedPerFaction">阵营 ID -> 合并后的 Mesh</param>
        /// <param name="folder">保存目录（需在 Assets 下）</param>
        public static void SaveFactionMeshes(Dictionary<int, Mesh> combinedPerFaction, string folder = null)
        {
#if UNITY_EDITOR
            if (!Application.isEditor || combinedPerFaction == null) return;

            folder = ValidateFolder(folder);
            EnsureFolderExists(folder);

            foreach (KeyValuePair<int, Mesh> kv in combinedPerFaction)
            {
                int faction = kv.Key;
                Mesh mesh = kv.Value;
                if (!mesh) continue;

                string meshPath = Path.Combine(folder, $"mesh_faction_{faction}.asset");
                meshPath = meshPath.Replace('\\', '/');

                Object exist = AssetDatabase.LoadAssetAtPath<Object>(meshPath);
                if (exist) AssetDatabase.DeleteAsset(meshPath);

                // 创建 Mesh 副本以保存（避免保存运行时实例）
                Mesh meshCopy = Object.Instantiate(mesh);
                meshCopy.name = $"mesh_faction_{faction}";
                AssetDatabase.CreateAsset(meshCopy, meshPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// 保存阵营+高度 Mesh 资产
        /// </summary>
        /// <param name="combinedPerFactionHeight">(阵营 ID, 高度) -> 合并后的 Mesh</param>
        /// <param name="folder">保存目录（需在 Assets 下）</param>
        public static void SaveFactionHeightMeshes(Dictionary<Vector2Int, Mesh> combinedPerFactionHeight, string folder = null)
        {
#if UNITY_EDITOR
            if (!Application.isEditor || combinedPerFactionHeight == null) return;

            folder = ValidateFolder(folder);
            EnsureFolderExists(folder);

            foreach (KeyValuePair<Vector2Int, Mesh> kv in combinedPerFactionHeight)
            {
                int faction = kv.Key.x;
                int height = kv.Key.y;
                Mesh mesh = kv.Value;
                if (!mesh) continue;

                string meshPath = Path.Combine(folder, $"mesh_faction_{faction}_h{height}.asset");
                meshPath = meshPath.Replace('\\', '/');

                Object exist = AssetDatabase.LoadAssetAtPath<Object>(meshPath);
                if (exist) AssetDatabase.DeleteAsset(meshPath);

                // 创建 Mesh 副本以保存（避免保存运行时实例）
                Mesh meshCopy = Object.Instantiate(mesh);
                meshCopy.name = $"mesh_faction_{faction}_h{height}";
                AssetDatabase.CreateAsset(meshCopy, meshPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// 保存 GameObject 为 Prefab
        /// </summary>
        /// <param name="gameObjects">要保存的 GameObject 列表</param>
        /// <param name="folder">保存目录（需在 Assets 下）</param>
        public static void SaveGameObjectsAsPrefabs(IEnumerable<GameObject> gameObjects, string folder = null)
        {
#if UNITY_EDITOR
            if (!Application.isEditor || gameObjects == null) return;

            folder = ValidateFolder(folder);
            EnsureFolderExists(folder);

            foreach (GameObject go in gameObjects)
            {
                if (!go || !go.activeInHierarchy) continue;

                string prefabPath = Path.Combine(folder, go.name + ".prefab");
                prefabPath = prefabPath.Replace('\\', '/');
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// 保存 LineRenderer 为 Prefab
        /// </summary>
        /// <param name="lineRenderers">要保存的 LineRenderer 列表</param>
        /// <param name="folder">保存目录（需在 Assets 下）</param>
        public static void SaveLineRenderersAsPrefabs(IEnumerable<LineRenderer> lineRenderers, string folder = null)
        {
#if UNITY_EDITOR
            if (!Application.isEditor || lineRenderers == null) return;

            folder = ValidateFolder(folder);
            EnsureFolderExists(folder);

            foreach (LineRenderer lr in lineRenderers)
            {
                if (!lr) continue;
                GameObject go = lr.gameObject;
                if (!go || !go.activeInHierarchy) continue;

                string prefabPath = Path.Combine(folder, go.name + ".prefab");
                prefabPath = prefabPath.Replace('\\', '/');
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// 加载已保存的阵营 Mesh 资产
        /// </summary>
        /// <param name="folder">资产目录</param>
        /// <returns>阵营 ID -> Mesh 字典</returns>
        public static Dictionary<int, Mesh> LoadFactionMeshes(string folder = null)
        {
            Dictionary<int, Mesh> result = new Dictionary<int, Mesh>();

#if UNITY_EDITOR
            if (!Application.isEditor) return result;

            folder = ValidateFolder(folder);
            if (!AssetDatabase.IsValidFolder(folder)) return result;

            string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                // 解析文件名 mesh_faction_{id}
                if (fileName.StartsWith("mesh_faction_"))
                {
                    string idStr = fileName.Substring("mesh_faction_".Length);
                    if (int.TryParse(idStr, out int factionId))
                    {
                        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (mesh)
                        {
                            result[factionId] = mesh;
                        }
                    }
                }
            }
#else
            // Runtime: 使用 Resources.Load 或 Addressables
            Debug.LogWarning("AreaFactionAssetHelper.LoadFactionMeshes is only available in Editor. Use Resources or Addressables for runtime loading.");
#endif

            return result;
        }

        /// <summary>
        /// 检查指定目录是否存在已保存的资产
        /// </summary>
        public static bool HasSavedAssets(string folder = null)
        {
#if UNITY_EDITOR
            folder = ValidateFolder(folder);
            if (!AssetDatabase.IsValidFolder(folder)) return false;

            string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { folder });
            return guids.Length > 0;
#else
            return false;
#endif
        }

        /// <summary>
        /// 清除指定目录下的所有资产
        /// </summary>
        public static void ClearAssets(string folder = null)
        {
#if UNITY_EDITOR
            if (!Application.isEditor) return;

            folder = ValidateFolder(folder);
            if (!AssetDatabase.IsValidFolder(folder)) return;

            // 删除所有 Mesh 资产
            string[] meshGuids = AssetDatabase.FindAssets("t:Mesh", new[] { folder });
            foreach (string guid in meshGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
            }

            // 删除所有 Prefab 资产
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.Refresh();
#endif
        }

        #region 私有辅助方法

        private static string ValidateFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return DefaultFolder;
            if (!folder.StartsWith("Assets/")) return DefaultFolder;
            return folder;
        }

#if UNITY_EDITOR
        private static void EnsureFolderExists(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return;
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets") return;

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
#endif

        #endregion
    }
}