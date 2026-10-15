using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ArtOfWar_II_Art.Editor.Importers
{
    public class UnitDetailImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith(".prefab"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab && prefab.name.StartsWith("UnitDetail"))
                    {
                        Animator animator = FindAnimator(prefab);
                        if (animator && animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                        {
                            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                            EditorUtility.SetDirty(prefab);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }
        }

        private static Animator FindAnimator(GameObject g)
        {
            PlayableDirector director = g.GetComponentInChildren<PlayableDirector>();
            if (!director)
            {
                return null;
            }

            TimelineAsset timelineAsset = (TimelineAsset) director.playableAsset;
            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                if (track is AnimationTrack animationTrack)
                {
                    Animator animator = (Animator) director.GetGenericBinding(animationTrack);
                    return animator;
                }
            }

            return null;
        }
    }
}