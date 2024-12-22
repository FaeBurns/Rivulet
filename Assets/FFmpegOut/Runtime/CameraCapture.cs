// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FFmpegOut
{
    [AddComponentMenu("FFmpegOut/Camera Capture")]
    public class CameraCapture : MonoBehaviour
    {
        private RenderTexture m_sourceRenderTexture;

        private float m_targetFrameRate;

        [FormerlySerializedAs("_preset")]
        [SerializeField]
        private FFmpegPreset m_preset;

        public FFmpegPreset Preset {
            get => m_preset;
            set => m_preset = value;
        }

        private FFmpegSession m_session;

        protected virtual FFmpegSession GetSession( int texWidth, int texHeight, float frameRate )
        {
            return FFmpegSession.Create(
                gameObject.name,
                texWidth,
                texHeight,
                frameRate, m_preset
            );
        }

        private int m_frameCount;
        private float m_startTime;
        private int m_frameDropCount;

        private void WarnFrameDrop()
        {
            if (++m_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }

        private async void Start()
        {
            while (Application.isPlaying)
            {
                await Awaitable.EndOfFrameAsync();
                m_session?.CompletePushFrames();
            }
        }

        public void DoFrameUpdate()
        {
            if (m_session == null)
                return;

            float frameTime = m_startTime + (m_frameCount - 0.5f) / m_targetFrameRate;
            float gap = Time.time - frameTime;
            float delta = 1 / m_targetFrameRate;

            if (gap < 0)
            {
                // Update without frame data.
                m_session.PushFrame(null);
            }
            else if (gap < delta)
            {
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                m_session.PushFrame(m_sourceRenderTexture);
                m_frameCount++;
            }
            else if (gap < delta * 2)
            {
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme
                m_session.PushFrame(m_sourceRenderTexture);
                m_session.PushFrame(m_sourceRenderTexture);
                m_frameCount += 2;
            }
            else
            {
                // Show a warning message about the situation.
                WarnFrameDrop();

                // Push the current frame to FFmpeg.
                m_session.PushFrame(m_sourceRenderTexture);

                // Compensate the time delay.
                m_frameCount += Mathf.FloorToInt(gap * m_targetFrameRate);
            }
        }

        public void OnDisable()
        {
            // dispose session when disabled - OnDisable is called when play ends
            m_session?.Dispose();
        }

        public void InitSession(int width, int height, float framerate, RenderTexture sourceRenderTexture)
        {
            m_sourceRenderTexture = sourceRenderTexture;
            m_targetFrameRate = framerate;

            // Start an FFmpeg session.
            m_session = GetSession(width, height, framerate);

            m_startTime = Time.time;
            m_frameCount = 0;
            m_frameDropCount = 0;
        }

        public void EndSession()
        {
            if (m_session != null)
            {
                // Close and dispose the FFmpeg session.
                m_session.Close();
                m_session.Dispose();
                m_session = null;
            }
        }
    }
}
