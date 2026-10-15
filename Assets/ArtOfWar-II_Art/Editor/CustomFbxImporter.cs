using System;
using UnityEditor;
using UnityEngine;

namespace ArtOfWar_II_Art.Editor
{
    public class CustomFbxImporter : AssetPostprocessor
    {
        // This method is called after a model is imported
        void OnPostprocessModel(GameObject g)
        {
            if (assetImporter is ModelImporter importer && assetPath.StartsWith("Assets/ArtOfWar-II_Art/Fbx"))
            {
                bool change = false;

                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                Array.Copy(importer.clipAnimations, clips, importer.clipAnimations.Length);

                foreach (ModelImporterClipAnimation clip in clips)
                {
                    if (clip.name.Equals("Idle") || clip.name.Equals("Move") || clip.name.Equals("Victory"))
                    {
                        clip.loopTime = true;
                        change = true;
                    }
                }

                if (change)
                {
                    importer.clipAnimations = clips;

                    // Apply the changes to the importer
                    importer.SaveAndReimport();
                }
            }
        }
    }
}