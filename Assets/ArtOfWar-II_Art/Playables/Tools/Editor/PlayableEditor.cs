using UnityEditor;
using UnityEngine;

namespace ArtOfWar_II_Art.Playables.Editor
{
    [CustomEditor(typeof(PlayableGenerator))]
    public class PlayableEditor : UnityEditor.Editor
    {
        private string inputText = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlayableGenerator script = (PlayableGenerator) target;

            inputText = EditorGUILayout.TextField("要测试的单位ID", inputText);

            if (GUILayout.Button("创建", GUILayout.Width(100), GUILayout.Height(20)))
            {
                if (int.TryParse(inputText, out int id))
                {
                    script.OnGenerationClick(id);
                }
                else
                {
                    EditorGUILayout.HelpBox("请输入正确的ID", MessageType.Error);
                }
            }

            if (GUILayout.Button("播放", GUILayout.Width(100), GUILayout.Height(20)))
            {
                script.OnPlayClick();
            }
        }
    }
}