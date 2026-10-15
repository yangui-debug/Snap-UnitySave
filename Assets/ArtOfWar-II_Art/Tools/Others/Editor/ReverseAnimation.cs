using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArtOfWar_II_Art.Integration.Scripts
{
    public class ReverseAnimation
    {
        [MenuItem("Assets/Reverse Animation Clip")]
        static void ReverseClip()
        {
            AnimationClip originalClip = Selection.activeObject as AnimationClip;
            if (originalClip == null)
            {
                Debug.LogWarning("No AnimationClip selected.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            AnimationClip reversedClip = new AnimationClip();
            reversedClip.name = originalClip.name + "_Reversed";

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(originalClip))
            {
                AnimationCurve originalCurve = AnimationUtility.GetEditorCurve(originalClip, binding);
                AnimationCurve reversedCurve = new AnimationCurve();

                for (int i = originalCurve.keys.Length - 1; i >= 0; i--)
                {
                    Keyframe originalKey = originalCurve.keys[i];
                    Keyframe reversedKey = new Keyframe(originalClip.length - originalKey.time, originalKey.value, -originalKey.outTangent, -originalKey.inTangent);
                    reversedCurve.AddKey(reversedKey);
                }

                reversedCurve.preWrapMode = originalCurve.postWrapMode;
                reversedCurve.postWrapMode = originalCurve.preWrapMode;

                AnimationUtility.SetEditorCurve(reversedClip, binding, reversedCurve);
            }

            AssetDatabase.CreateAsset(reversedClip, $"{Path.GetDirectoryName(assetPath)}" + reversedClip.name + ".anim");
        }
    }
}