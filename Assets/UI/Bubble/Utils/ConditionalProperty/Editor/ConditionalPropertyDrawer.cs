using UnityEditor;
using UnityEngine;

namespace UI.Bubble.Utils.ConditionalProperty
{
    [CustomPropertyDrawer(typeof(ConditionalPropertyAttribute))]
    public class ConditionalPropertyDrawer : PropertyDrawer
    {
        private bool showField = true;
        private ConditionalPropertyAttribute target;

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            target = attribute as ConditionalPropertyAttribute;

            if (target == null) return;

            var condition = property.serializedObject.FindProperty(target.conditionField);

            if (condition == null)
            {
                ShowError(position, label,
                    "Condition \"" + target.conditionField +
                    "\" not found. Please check the conditionField of the Attribute.");
                return;
            }

            if (condition.propertyType != SerializedPropertyType.Boolean)
            {
                ShowError(position, label, "Condition must be a bool variable.");
                return;
            }

            showField = condition.boolValue == target.expectedValue;
            if (!showField) return;

            EditorGUI.BeginDisabledGroup(target.disable);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndDisabledGroup();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (showField)
                return EditorGUI.GetPropertyHeight(property);
            return -EditorGUIUtility.standardVerticalSpacing;
        }

        private void ShowError(Rect position, GUIContent label, string errorText)
        {
            EditorGUI.HelpBox(position, errorText, MessageType.Error);
        }
    }
}