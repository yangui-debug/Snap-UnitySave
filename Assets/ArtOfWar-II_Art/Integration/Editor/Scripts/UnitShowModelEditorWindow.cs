using System.Collections.Generic;
using System.IO;
using ArtOfWar_II_Art.Tools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtOfWar_II_Art.Scripts
{
    public class UnitShowModelEditorWindow : EditorWindow
    {
        private GameObject fbx;
        private int unitId;
        private int start, end;

        // Default paths for Animator Controllers
        private readonly string unitControllerPath = "Assets/ArtOfWar-II_Art/Anim/AnimationController/Unit/UnitShowBase.controller";

        // Root directory to search for FBX files
        private readonly string fbxSearchRootPath = "Assets/ArtOfWar-II_Art/Fbx/BattleUnit";

        // Fields for storing loaded AnimatorControllers
        private AnimatorController unitBaseController;

        [MenuItem("Rise/战斗单位展示Prefab生成")]
        public static void ShowWindow()
        {
            GetWindow<UnitShowModelEditorWindow>("FBX To Prefab");
        }

        private void OnEnable()
        {
            // Load default AnimatorControllers on window open
            unitBaseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(unitControllerPath);
        }

        private void OnGUI()
        {
            GUILayout.Label("生成战斗单位展示Prefab", EditorStyles.boldLabel);

            unitId = EditorGUILayout.IntField("Unit ID", unitId);

            // Automatically find the FBX file based on Unit ID
            if (GUILayout.Button("Find FBX"))
            {
                fbx = FindFbxByUnitId(unitId);
                if (fbx != null)
                {
                    Debug.Log($"Found FBX: {fbx.name}");
                }
                else
                {
                    Debug.LogWarning("FBX not found.");
                }
            }

            fbx = (GameObject) EditorGUILayout.ObjectField("FBX Object", fbx, typeof(GameObject), false);

            unitBaseController =
                (AnimatorController) EditorGUILayout.ObjectField("Equip Unit Base Animator Controller", unitBaseController, typeof(AnimatorController),
                    false);

            if (fbx)
            {
                if (GUILayout.Button("Create Prefab"))
                {
                    CreatePrefab();
                }
            }

            GUILayout.Space(20);
            start = EditorGUILayout.IntField("Start ID", start);
            end = EditorGUILayout.IntField("End ID", end);
            if (GUILayout.Button("Create All"))
            {
                CreateAll();
            }
        }

        private GameObject FindFbxByUnitId(int unitId)
        {
            string searchRootPath = fbxSearchRootPath;
            string[] directories = Directory.GetDirectories(searchRootPath, "*", SearchOption.AllDirectories);

            foreach (string directory in directories)
            {
                if (directory.Contains($"BattleUnit_{unitId}") || directory.Contains($"Hero_{unitId}"))
                {
                    string[] files = Directory.GetFiles(directory, "*.fbx", SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        // Take the first FBX found in the directory
                        return AssetDatabase.LoadAssetAtPath<GameObject>(files[0]);
                    }

                    files = Directory.GetFiles(directory, "*.prefab", SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        // Take the first prefab found in the directory
                        return AssetDatabase.LoadAssetAtPath<GameObject>(files[0]);
                    }
                }
            }

            return null;
        }

        private void CreatePrefab()
        {
            // Select the correct base controller based on the model type
            AnimatorController baseController = unitBaseController;
            if (!baseController)
            {
                Debug.LogError("Base Animator Controller is not assigned.");
                return;
            }

            if (!fbx)
            {
                Debug.LogError("FBX is not assigned.");
                return;
            }

            // Create directories if they don't exist
            string directoryPath = $"Assets/ArtOfWar-II_Art/Fbx/UnitShow/Unit_{unitId}";
            EditorUtilities.EnsureDirectoryExists(directoryPath);

            // Create a new empty GameObject to act as the prefab root
            GameObject prefabRoot = new GameObject($"UnitShow_{unitId}");

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

            // Create an AnimationOverrideController
            string overrideControllerPath = Path.Combine(directoryPath, $"UnitShowAnim_{unitId}.overrideController");
            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

            // Bind FBX animation clips to the override controller
            BindAnimClips(overrideController);
            animator.runtimeAnimatorController = overrideController;

            // Save as a prefab
            string prefabPath = Path.Combine(directoryPath, $"UnitShow_{unitId}.prefab");
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

            // Clean up
            DestroyImmediate(prefabRoot);

            // Refresh and focus on the new prefab
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);

            Debug.Log($"Prefab created at: {prefabPath}");
        }

        private void BindAnimClips(AnimatorOverrideController overrideController)
        {
            Dictionary<string, AnimationClip> clipNameToClip = EditorUtilities.GetFbxAnimClips(fbx);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
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

        private void CreateAll()
        {
            for (int i = start; i <= end; i++)
            {
                unitId = i;
                fbx = FindFbxByUnitId(unitId);
                CreatePrefab();
            }
        }
    }
}