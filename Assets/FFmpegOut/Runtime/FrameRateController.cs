// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

namespace FFmpegOut
{
    [AddComponentMenu("FFmpegOut/Frame Rate Controller")]
    public sealed class FrameRateController : MonoBehaviour
    {
        [FormerlySerializedAs("_frameRate")]
        [SerializeField]
        private float m_frameRate = 60;
        [FormerlySerializedAs("_offlineMode")]
        [SerializeField]
        private bool m_offlineMode = true;

        private int m_originalFrameRate;
        private int m_originalVSyncCount;

        private void OnEnable()
        {
            int ifps = Mathf.RoundToInt(m_frameRate);

            if (m_offlineMode)
            {
                m_originalFrameRate = Time.captureFramerate;
                Time.captureFramerate = ifps;
            }
            else
            {
                m_originalFrameRate = Application.targetFrameRate;
                m_originalVSyncCount = QualitySettings.vSyncCount;
                Application.targetFrameRate = ifps;
                QualitySettings.vSyncCount = 0;
            }
        }

        private void OnDisable()
        {
            if (m_offlineMode)
            {
                Time.captureFramerate = m_originalFrameRate;
            }
            else
            {
                Application.targetFrameRate = m_originalFrameRate;
                QualitySettings.vSyncCount = m_originalVSyncCount;
            }
        }
    }
}
