using System;
using PostProcessing;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(PostProcessingEffectParameter))]
    public class PostProcessingEffectParameterDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty nameProperty = property.FindPropertyRelative(nameof(PostProcessingEffectParameter.Name));
            SerializedProperty typeProperty = property.FindPropertyRelative(nameof(PostProcessingEffectParameter.Type));

            Rect nameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(nameRect, nameProperty, new GUIContent("Property"));

            Rect typeRect = new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);
            Rect valueRect = new Rect(
                position.x + typeRect.width + EditorGUIUtility.standardVerticalSpacing,
                typeRect.y,
                position.width - (EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing),
                EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(typeRect, typeProperty, GUIContent.none);

            EffectParameterType type = (EffectParameterType)typeProperty.enumValueIndex;
            GuiDrawValueProperty(valueRect, type, property);

            EditorGUI.EndProperty();
            property.serializedObject.ApplyModifiedProperties();
        }

        private void GuiDrawValueProperty(Rect valueRect, EffectParameterType type, SerializedProperty rootProperty)
        {
            switch (type)
            {
                case EffectParameterType.FLAG:
                    EditorGUI.PropertyField(valueRect, rootProperty.FindPropertyRelative(nameof(PostProcessingEffectParameter.FlagValue)), GUIContent.none);
                    break;
                case EffectParameterType.INT:
                    EditorGUI.PropertyField(valueRect, rootProperty.FindPropertyRelative(nameof(PostProcessingEffectParameter.IntValue)), GUIContent.none);
                    break;
                case EffectParameterType.FLOAT:
                    EditorGUI.PropertyField(valueRect, rootProperty.FindPropertyRelative(nameof(PostProcessingEffectParameter.FloatValue)), GUIContent.none);
                    break;
                case EffectParameterType.COLOR:
                    EditorGUI.PropertyField(valueRect, rootProperty.FindPropertyRelative(nameof(PostProcessingEffectParameter.ColorValue)), GUIContent.none);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}