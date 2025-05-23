// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using UnityEditor;

namespace FFmpegOut
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FrameRateController))]
    public class FrameRateControllerEditor : Editor
    {
        SerializedProperty _frameRate;
        SerializedProperty _offlineMode;

        void OnEnable()
        {
            _frameRate = serializedObject.FindProperty("m_frameRate");
            _offlineMode = serializedObject.FindProperty("m_offlineMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_frameRate);
            EditorGUILayout.PropertyField(_offlineMode);

            if (!Application.isPlaying &&
                !_frameRate.hasMultipleDifferentValues &&
                !_offlineMode.hasMultipleDifferentValues)
            {
                if (_offlineMode.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "Offline mode enabled: Time interval will be fixed " +
                        "to the specified value to keep exact speed on " +
                        "recorded videos. This stops synchronizing game " +
                        "time to wall clock time.", MessageType.None
                    );
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "V-sync will be disabled because the specified " +
                        "frame rate is not divisible by the screen " +
                        "refresh rate.", MessageType.None
                    );
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
