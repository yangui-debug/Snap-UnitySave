#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using ArtOfWar_II_Art.Playables.PlayableAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ArtOfWar_II_Art.Playables
{
    public static class TrackConvert
    {
        [MenuItem("Tools/Run Playable Track Convert")]
        public static void Run()
        {
            string directoryPath = "Assets/ArtOfWar-II_Art/Playables/Units";
            string[] assetGUIDs = AssetDatabase.FindAssets("", new[] {directoryPath});

            // 遍历所有的GUID并加载资源
            foreach (string guid in assetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                PlayableDirector asset = AssetDatabase.LoadAssetAtPath<PlayableDirector>(assetPath);

                if (asset && int.TryParse(asset.name, out int id))
                {
                    Convert(asset, id);
                }
            }
        }

        private static void Convert(PlayableDirector director, int id)
        {
            TimelineAsset timelineAsset = (TimelineAsset) director.playableAsset;
            List<GroupTrack> taskGroups = timelineAsset.GetRootTracks().OfType<GroupTrack>().ToList();

            foreach (GroupTrack group in taskGroups)
            {
                foreach (TrackAsset track in group.GetChildTracks())
                {
                    if (track is PlayableTrack playable)
                    {
                        foreach (TimelineClip clip in playable.GetClips())
                        {
                            // if (clip.asset is DynamicTargetAsset before)
                            // {
                            //     TimelineClip newAsset = playable.CreateClip<DynamicEffectPlayableAsset>();
                            //     newAsset.start = clip.start;
                            //     newAsset.duration = clip.duration;
                            //
                            //     Object effect = director.GetReferenceValue(before.psEffect.exposedName, out bool obj);
                            //     if (effect == null)
                            //     {
                            //         effect = before.psEffect.defaultValue;
                            //     }
                            //
                            //     if (newAsset.asset is DynamicEffectPlayableAsset after)
                            //     {
                            //         // 创建一个新的ExposedReference实例
                            //         ExposedReference<GameObject> newExposedReference = new ExposedReference<GameObject>();
                            //         newExposedReference.exposedName = before.psEffect.exposedName;
                            //         newExposedReference.defaultValue = effect;
                            //
                            //         // 为ExposedReference生成一个唯一的名称，并将其分配给目标对象
                            //         director.SetReferenceValue(newExposedReference.exposedName, effect);
                            //         after.sourceEffect = newExposedReference;
                            //         after.playMode = before.playMode;
                            //     }
                            //
                            //     // for (int i = director.transform.childCount - 1; i >= 0; i--)
                            //     // {
                            //     //     Transform c = director.transform.GetChild(i);
                            //     //     if (c.gameObject.name.Equals(effect.name))
                            //     //     {
                            //     //         // Object.DestroyImmediate(c.gameObject, true);
                            //     //     }
                            //     // }
                            //
                            //     playable.DeleteClip(clip);
                            //     PrefabUtility.SavePrefabAsset(director.gameObject);
                            //
                            //     Debug.Log($"convert id: {id}");
                            // }
                        }
                    }
                }
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif