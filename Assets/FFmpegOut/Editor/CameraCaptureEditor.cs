// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace FFmpegOut
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CameraCapture))]
    public class CameraCaptureEditor : Editor
    {
        SerializedProperty _width;
        SerializedProperty _height;
        SerializedProperty _preset;
        SerializedProperty _frameRate;

        GUIContent[] _presetLabels;
        int[] _presetOptions;

        // It shows the render format options when:
        // - Editing multiple objects.
        // - No target texture is specified in the camera.
        bool ShouldShowFormatOptions
        {
            get {
                if (targets.Length > 1) return true;
                Camera camera = ((Component)target).GetComponent<Camera>();
                return camera.targetTexture == null;
            }
        }

        void OnEnable()
        {
            _width = serializedObject.FindProperty("m_width");
            _height = serializedObject.FindProperty("m_height");
            _preset = serializedObject.FindProperty("m_preset");
            _frameRate = serializedObject.FindProperty("m_frameRate");

            Array presets = FFmpegPreset.GetValues(typeof(FFmpegPreset));
            _presetLabels = presets.Cast<FFmpegPreset>().
                Select(p => new GUIContent(p.GetDisplayName())).ToArray();
            _presetOptions = presets.Cast<int>().ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ShouldShowFormatOptions)
            {
                EditorGUILayout.PropertyField(_width);
                EditorGUILayout.PropertyField(_height);
            }

            EditorGUILayout.IntPopup(_preset, _presetLabels, _presetOptions);
            EditorGUILayout.PropertyField(_frameRate);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
