using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class TextureSettingsModifier : EditorWindow
{
    public DefaultAsset folder;

    [MenuItem("Tools/Art/Modify Textures in Folder")]
    static void ShowWindow()
    {
        GetWindow<TextureSettingsModifier>("Texture Compression Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Modify Textures", EditorStyles.boldLabel);
        folder = EditorGUILayout.ObjectField("Texture Folder Path:", folder, typeof(DefaultAsset), false) as DefaultAsset;
        if (GUILayout.Button("Modify Textures"))
        {
            string tempFolder = AssetDatabase.GetAssetPath(folder);
            HashSet<string> packableAssets = ModifyAtlas(tempFolder);
            ModifyTextures(tempFolder, packableAssets);
        }
    }

    private static void ModifyTextures(string folderPath, HashSet<string> packableAssets)
    {
        // 获取目标文件夹中的所有纹理资源
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (packableAssets.Contains(assetPath))
            {
                continue;
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!textureImporter)
            {
                continue;
            }

            bool changed = false;

            // 关闭 Mip Maps
            if (textureImporter.mipmapEnabled)
            {
                textureImporter.mipmapEnabled = false;
                changed = true;
            }

            TextureImporterPlatformSettings androidSettings = textureImporter.GetPlatformTextureSettings("Android");
            if (!androidSettings.overridden)
            {
                changed |= SetupPlatformTextureSettings(androidSettings);
            }

            if (changed)
            {
                textureImporter.SetPlatformTextureSettings(androidSettings);

                // 保存修改
                AssetDatabase.ImportAsset(assetPath);
                Debug.Log($"Modified texture: {assetPath}");
            }
        }

        // 刷新 AssetDatabase 以应用更改
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Texture modification complete.");
    }

    private static HashSet<string> ModifyAtlas(string folderPath)
    {
        HashSet<string> packableAssets = new HashSet<string>();

        // 获取目标文件夹中的所有纹理资源
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);

            // 获取 SpriteAtlas 中被打包的资源
            foreach (Object packable in atlas.GetPackables())
            {
                string pPath = AssetDatabase.GetAssetPath(packable);
                if (!string.IsNullOrEmpty(pPath))
                {
                    if (AssetDatabase.IsValidFolder(pPath))
                    {
                        // 如果是文件夹，遍历其下所有的 Sprite
                        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { pPath });
                        foreach (string spriteGuid in spriteGuids)
                        {
                            string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);
                            packableAssets.Add(spritePath);
                        }
                    }
                    else
                    {
                        packableAssets.Add(assetPath);
                    }
                }
            }

            continue;
            TextureImporterPlatformSettings androidSettings = atlas.GetPlatformSettings("Android");
            if (androidSettings.overridden)
            {
                continue;
            }

            if (SetupPlatformTextureSettings(androidSettings))
            {
                atlas.SetPlatformSettings(androidSettings);

                // 保存修改
                EditorUtility.SetDirty(atlas);
                AssetDatabase.ImportAsset(assetPath);
                Debug.Log($"Modified texture: {assetPath}");
            }
        }

        Debug.Log("Texture modification complete.");
        return packableAssets;
    }

    private static bool SetupPlatformTextureSettings(TextureImporterPlatformSettings settings)
    {
        bool changed = false;
        if (!settings.overridden)
        {
            settings.overridden = true;
            changed = true;
        }

        if (settings.format != TextureImporterFormat.ETC2_RGBA8Crunched)
        {
            settings.format = TextureImporterFormat.ETC2_RGBA8Crunched;
            changed = true;
        }

        return changed;
    }
}