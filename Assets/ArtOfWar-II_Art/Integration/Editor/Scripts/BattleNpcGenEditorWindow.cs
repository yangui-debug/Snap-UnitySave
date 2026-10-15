#if LS_AOW2
using System.Collections.Generic;
using System.IO;
using Art.View;
using ArtOfWar_II_Art.Tools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ArtOfWar_II_Art.Scripts
{
    public class BattleNpcGenEditorWindow : EditorWindow
    {
        // Default paths for Animator Controllers
        private readonly string baseControllerPath = "Assets/ArtOfWar-II_Art/Fbx/DesertBazaarNpc/BaseAnim/BattleNpcAnimator.controller";

        // Root directory to search for FBX files
        private readonly string fbxSearchRootPath = "Assets/ArtOfWar-II_Art/Fbx/DesertBazaarNpc";

        // Fields for storing loaded AnimatorControllers
        private AnimatorController baseController;

        [MenuItem("Rise/战斗npcPrefab生成")]
        public static void ShowWindow()
        {
            GetWindow<BattleNpcGenEditorWindow>("FBX To Prefab");
        }

        private void OnEnable()
        {
            // Load default AnimatorControllers on window open
            baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
        }

        private void OnGUI()
        {
            GUILayout.Label("生成战斗npcPrefab", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Prefab"))
            {
                foreach (GameObject fbx in FindAllFbx())
                {
                    CreatePrefab(fbx);
                }
            }
        }

        private List<GameObject> FindAllFbx()
        {
            List<GameObject> list = new();
            string[] files = Directory.GetFiles(fbxSearchRootPath, "*.fbx", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                foreach (string file in files)
                {
                    // Take the first FBX found in the directory
                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                    list.Add(obj);
                }
            }

            return list;
        }

        private void CreatePrefab(GameObject fbx)
        {
            // Create a new empty GameObject to act as the prefab root
            GameObject prefabRoot = new GameObject(fbx.name);

            // Instantiate the FBX as a child of the root
            GameObject fbxInstance = PrefabUtility.InstantiatePrefab(fbx, prefabRoot.transform) as GameObject;
            if (fbxInstance == null)
            {
                Debug.LogError("Failed to instantiate FBX as a prefab.");
                DestroyImmediate(prefabRoot);
                return;
            }

            // Ensure the FBX has an Animator component
            Animator animator = fbxInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = fbxInstance.AddComponent<Animator>();
            }

            BattleNpc battleNpc = prefabRoot.AddComponent<BattleNpc>();
            battleNpc.SetAnimator(animator);

            // Create an AnimationOverrideController
            string overrideControllerPath = Path.Combine(fbxSearchRootPath, $"{fbx.name}.overrideController");
            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

            // Bind FBX animation clips to the override controller
            ProcessFbxSettings(fbx);
            BindAnimClips(overrideController, fbx);
            animator.runtimeAnimatorController = overrideController;

            // Save as a prefab
            string prefabPath = Path.Combine(fbxSearchRootPath, $"{fbx.name}.prefab");
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

            // Clean up
            DestroyImmediate(prefabRoot);

            // Refresh and focus on the new prefab
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);

            Debug.Log($"Prefab created at: {prefabPath}");
        }

        private void BindAnimClips(AnimatorOverrideController overrideController, GameObject fbx)
        {
            Dictionary<string, AnimationClip> clipNameToClip = EditorUtilities.GetFbxAnimClips(fbx);
            List<KeyValuePair<AnimationClip, AnimationClip>>
                overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip originalClip = overrides[i].Key;
                if (clipNameToClip.TryGetValue(originalClip.name, out AnimationClip newClip))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip);
                }
            }

            overrideController.ApplyOverrides(overrides);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ProcessFbxSettings(GameObject fbx)
        {
            // Get the ModelImporter for the FBX file
            ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(fbx)) as ModelImporter;
            if (modelImporter)
            {
                bool dirty = false;

                ModelImporterClipAnimation[] animations = modelImporter.clipAnimations;

                // 如果animations为空，尝试从defaultClipAnimations获取
                if (animations.Length == 0)
                {
                    animations = modelImporter.defaultClipAnimations;
                }

                // 遍历所有动画剪辑，设置指定的动画开启loop
                foreach (ModelImporterClipAnimation animation in animations)
                {
                    if (!animation.loopTime && (animation.name.StartsWith("Idle") || animation.name.StartsWith("Victory") || animation.name.StartsWith("Fail")))
                    {
                        dirty = true;
                        animation.loopTime = true;
                    }
                }

                if (dirty)
                {
                    // 将修改应用到导入设置中
                    modelImporter.clipAnimations = animations;
                }

                if (dirty)
                {
                    // Apply the changes to the FBX file
                    modelImporter.SaveAndReimport();
                }
            }
        }
    }
}

#endif