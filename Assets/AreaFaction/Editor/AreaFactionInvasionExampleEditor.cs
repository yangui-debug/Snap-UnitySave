using UnityEditor;
using UnityEngine;

namespace Com.i89.AreaFaction.Editor
{
    [CustomEditor(typeof(AreaFactionInvasionExample))]
    public class AreaFactionInvasionExampleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);

            AreaFactionInvasionExample example = (AreaFactionInvasionExample) target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("HOI4 Thin Front"))
                {
                    Undo.RecordObject(example, "Apply HOI4 Thin Front Preset");
                    example.ApplyPresetHoi4ThinFront();
                    EditorUtility.SetDirty(example);
                }

                if (GUILayout.Button("Intense Tug Of War"))
                {
                    Undo.RecordObject(example, "Apply Intense Tug Of War Preset");
                    example.ApplyPresetIntenseTugOfWar();
                    EditorUtility.SetDirty(example);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stalemate Then Collapse"))
                {
                    Undo.RecordObject(example, "Apply Stalemate Then Collapse Preset");
                    example.ApplyPresetStalemateThenCollapse();
                    EditorUtility.SetDirty(example);
                }

                if (GUILayout.Button("Elastic Front"))
                {
                    Undo.RecordObject(example, "Apply Elastic Front Preset");
                    example.ApplyPresetElasticFront();
                    EditorUtility.SetDirty(example);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Runtime Tuning"))
                {
                    example.ApplyRuntimeTuning();
                    EditorUtility.SetDirty(example);
                }

                if (GUILayout.Button("Focus Map"))
                {
                    example.FocusMap();
                    EditorUtility.SetDirty(example);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Selected Attack"))
                {
                    example.PreviewSelectedAttackInEditor();
                    EditorUtility.SetDirty(example);
                }

                if (GUILayout.Button("Clear Preview"))
                {
                    example.ClearEditorPreview();
                    EditorUtility.SetDirty(example);
                }
            }
        }
    }
}
