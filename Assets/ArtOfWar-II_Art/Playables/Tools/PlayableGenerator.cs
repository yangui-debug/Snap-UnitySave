#if UNITY_EDITOR
using Playables.Core;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;

namespace ArtOfWar_II_Art.Playables.Editor
{
    public class PlayableGenerator : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera = default;
        [SerializeField] private Camera uiCamera = default;
        
        private static readonly string fbxPath = "Assets/ArtOfWar-II_Art/Fbx";
        private static readonly string sourcePath = $"{fbxPath}/BattleUnit_";
        private static readonly string playablePath = "Assets/ArtOfWar-II_Art/Playables/Units";

        private static GameObject testUnit;

        public void OnGenerationClick(int id)
        {
#if UNITY_EDITOR
            testUnit = new GameObject(id.ToString());

            GameObject model = CreateModel(id);
            model.transform.SetParent(testUnit.transform);
            Animator animator = model.GetComponent<Animator>();
            if (animator != null)
            {
                string newPath = $"Assets/ArtOfWar-II_Art/AnimationController/BattleUnit/{id}.overrideController";
                AnimatorOverrideController runAnim = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(newPath);
                animator.runtimeAnimatorController = runAnim;
            }

            GameObject playable = CreatePlayable(id);
            playable.name = id.ToString();
            playable.transform.SetParent(testUnit.transform);
#endif
        }

        public void OnPlayClick()
        {
#if UNITY_EDITOR
            // EditorApplication.isPlaying = !EditorApplication.isPlaying;

            if (testUnit)
            {
                testUnit.GetComponentInChildren<PlayableAgent>().InitSetup();
                testUnit.GetComponentInChildren<PlayableAgent>().GetUiPosFunc = GetUiPos;
                testUnit.GetComponentInChildren<PlayableDirector>().Play();
            }
#endif
        }

#if UNITY_EDITOR

        private GameObject CreateModel(int id)
        {
            string[] assetGUIDs = AssetDatabase.FindAssets("t:Model", new[] {$"{sourcePath}{id}"});
            if (assetGUIDs.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                return Instantiate(prefab);
            }

            return null;
        }

        private GameObject CreatePlayable(int id)
        {
            string path = $"{playablePath}/{id}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return Instantiate(prefab);
        }
        
#endif
        
        private Vector3 GetUiPos(Vector3 pos)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(gameCamera, pos);
            Vector2 uiFromPos = uiCamera.ScreenToWorldPoint(screenPoint);
            Vector3 fromPos = uiFromPos;

            return fromPos;
        }
    }
}