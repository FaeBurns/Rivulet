// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using System.Collections;

namespace FFmpegOut
{
    [AddComponentMenu("FFmpegOut/Frame Rate Controller")]
    public sealed class FrameRateController : MonoBehaviour
    {
        [SerializeField] float _frameRate = 60;
        [SerializeField] bool _offlineMode = true;

        int _originalFrameRate;
        int _originalVSyncCount;

        void OnEnable()
        {
            int ifps = Mathf.RoundToInt(_frameRate);

            if (_offlineMode)
            {
                _originalFrameRate = Time.captureFramerate;
                Time.captureFramerate = ifps;
            }
            else
            {
                _originalFrameRate = Application.targetFrameRate;
                _originalVSyncCount = QualitySettings.vSyncCount;
                Application.targetFrameRate = ifps;
                QualitySettings.vSyncCount = 0;
            }
        }

        void OnDisable()
        {
            if (_offlineMode)
            {
                Time.captureFramerate = _originalFrameRate;
            }
            else
            {
                Application.targetFrameRate = _originalFrameRate;
                QualitySettings.vSyncCount = _originalVSyncCount;
            }
        }
    }
}
