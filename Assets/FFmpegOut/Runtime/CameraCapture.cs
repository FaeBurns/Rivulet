// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

namespace FFmpegOut
{
    [AddComponentMenu("FFmpegOut/Camera Capture")]
    public class CameraCapture : MonoBehaviour
    {
        #region Public properties

        [FormerlySerializedAs("_width")]
        [SerializeField]
        private int m_width = 1920;

        public int Width {
            get { return m_width; }
            set { m_width = value; }
        }

        [FormerlySerializedAs("_height")]
        [SerializeField]
        private int m_height = 1080;

        public int Height {
            get { return m_height; }
            set { m_height = value; }
        }

        [FormerlySerializedAs("_preset")]
        [SerializeField]
        private FFmpegPreset m_preset;

        public FFmpegPreset Preset {
            get { return m_preset; }
            set { m_preset = value; }
        }

        [FormerlySerializedAs("_frameRate")]
        [SerializeField]
        private float m_frameRate = 60;

        public float FrameRate {
            get { return m_frameRate; }
            set { m_frameRate = value; }
        }

        #endregion

        #region Private members

        private FFmpegSession m_session;
        private RenderTexture m_tempRT;
        private GameObject m_blitter;

        private RenderTextureFormat GetTargetFormat(Camera camera)
        {
            return camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        }

        private int GetAntiAliasingLevel(Camera camera)
        {
            return camera.allowMSAA ? QualitySettings.antiAliasing : 1;
        }

        #endregion

        #region Public members

        protected virtual FFmpegSession GetSession( int texWidth, int texHeight )
        {
            return FFmpegSession.Create(
                gameObject.name,
                texWidth,
                texHeight,
                m_frameRate, m_preset
            );
        }

        #endregion

        #region Time-keeping variables

        private int m_frameCount;
        private float m_startTime;
        private int m_frameDropCount;

        private float FrameTime {
            get { return m_startTime + (m_frameCount - 0.5f) / m_frameRate; }
        }

        private void WarnFrameDrop()
        {
            if (++m_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }

        #endregion

        #region MonoBehaviour implementation

        private void OnDisable()
        {
            if (m_session != null)
            {
                // Close and dispose the FFmpeg session.
                m_session.Close();
                m_session.Dispose();
                m_session = null;
            }

            if (m_tempRT != null)
            {
                // Dispose the frame texture.
                GetComponent<Camera>().targetTexture = null;
                Destroy(m_tempRT);
                m_tempRT = null;
            }

            if (m_blitter != null)
            {
                // Destroy the blitter game object.
                Destroy(m_blitter);
                m_blitter = null;
            }
        }

        private IEnumerator Start()
        {
            // Sync with FFmpeg pipe thread at the end of every frame.
            for (WaitForEndOfFrame eof = new WaitForEndOfFrame();;)
            {
                yield return eof;
                m_session?.CompletePushFrames();
            }
        }

        private void Update()
        {
            Camera camera = GetComponent<Camera>();

            // Lazy initialization
            if (m_session == null)
            {
                // Give a newly created temporary render texture to the camera
                // if it's set to render to a screen. Also create a blitter
                // object to keep frames presented on the screen.
                if (camera.targetTexture == null)
                {
                    m_tempRT = new RenderTexture(m_width, m_height, 24, GetTargetFormat(camera));
                    m_tempRT.antiAliasing = 1;
                    camera.targetTexture = m_tempRT;
                    m_blitter = Blitter.CreateInstance(camera);
                }

                // Start an FFmpeg session.
                m_session = GetSession(
                    camera.targetTexture.width,
                    camera.targetTexture.height
                );

                m_startTime = Time.time;
                m_frameCount = 0;
                m_frameDropCount = 0;
            }

            float gap = Time.time - FrameTime;
            float delta = 1 / m_frameRate;

            if (gap < 0)
            {
                // Update without frame data.
                m_session.PushFrame(null);
            }
            else if (gap < delta)
            {
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                m_session.PushFrame(camera.targetTexture);
                m_frameCount++;
            }
            else if (gap < delta * 2)
            {
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme
                m_session.PushFrame(camera.targetTexture);
                m_session.PushFrame(camera.targetTexture);
                m_frameCount += 2;
            }
            else
            {
                // Show a warning message about the situation.
                WarnFrameDrop();

                // Push the current frame to FFmpeg.
                m_session.PushFrame(camera.targetTexture);

                // Compensate the time delay.
                m_frameCount += Mathf.FloorToInt(gap * m_frameRate);
            }
        }

        #endregion
    }
}
