using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArtOfWar_II_Art.Editor
{
    public class BattleUnitFbxImporter : AssetPostprocessor
    {
        private static readonly Dictionary<string, bool> loopSettings = new Dictionary<string, bool>
        {
            {"Idle", true},
            {"Move", true},
            {"Victory", true},
        };

        // 别改顺序, 下面有用index的地方
        private static readonly List<string> checkPaths = new List<string>
        {
            "Assets/ArtOfWar-II_Art/Fbx/BattleUnit",
            "Assets/ArtOfWar-II_Art/Fbx/Building",
        };

        // This method is called after a model is imported
        void OnPostprocessModel(GameObject g)
        {
            if (assetImporter is ModelImporter importer && checkPaths.Any(x => assetPath.StartsWith(x)))
            {
                bool change = false;
                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                Array.Copy(importer.clipAnimations, clips, importer.clipAnimations.Length);

                foreach (ModelImporterClipAnimation clip in clips)
                {
                    loopSettings.TryGetValue(clip.name, out bool loop);

                    change |= clip.loopTime != loop;
                    clip.loopTime = clip.loopPose = loop;
                }

                // 建筑额外的校验逻辑, index 1: 建筑
                change |= CheckBuildingIdleClips(clips);

                if (change)
                {
                    importer.clipAnimations = clips;

                    // Apply the changes to the importer
                    importer.SaveAndReimport();
                }
            }
        }

        private bool CheckBuildingIdleClips(ModelImporterClipAnimation[] clips)
        {
            bool change = false;
            if (assetPath.StartsWith(checkPaths[1]))
            {
                if (clips.Length > 0)
                {
                    // 校验idle动画
                    int id = 1;
                    foreach (ModelImporterClipAnimation clip in clips.Where(x => x.name.StartsWith("Idle_")))
                    {
                        change |= clip.loopTime;
                        clip.loopTime = clip.loopPose = false;

                        string idleClipName = $"Idle_{id}";
                        change |= !clip.name.Equals(idleClipName);
                        clip.name = idleClipName;

                        id++;
                    }

                    if (id == 1)
                    {
                        Debug.LogError($"{assetPath} idle clip count is 0");
                    }
                }
            }

            return change;
        }
    }
}