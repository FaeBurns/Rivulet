// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace FFmpegOut
{
    public class FFmpegSession : System.IDisposable
    {
        #region Factory methods

        public static FFmpegSession Create(
            string name,
            int width, int height, float frameRate,
            FFmpegPreset preset
        )
        {
            name += System.DateTime.Now.ToString(" yyyy MMdd HHmmss");
            string path = name.Replace(" ", "_") + preset.GetSuffix();
            return CreateWithOutputPath(path, width, height, frameRate, preset);
        }

        public static FFmpegSession CreateWithOutputPath(
            string outputPath,
            int width, int height, float frameRate,
            FFmpegPreset preset
        )
        {
            return new FFmpegSession(
                "-y -f rawvideo -vcodec rawvideo -pixel_format rgba"
                + " -colorspace bt709"
                + " -video_size " + width + "x" + height
                + " -framerate " + frameRate
                + " -loglevel warning -i - " + preset.GetOptions()
                + " " + outputPath
            );
        }

        public static FFmpegSession CreateWithArguments(string arguments)
        {
            return new FFmpegSession(arguments);
        }

        #endregion

        #region Public properties and members

        public void PushFrame(Texture source)
        {
            if (m_pipe != null)
            {
                ProcessQueue();
                if (source != null) QueueFrame(source);
            }
        }

        public void CompletePushFrames()
        {
            m_pipe?.SyncFrameData();
        }

        public void Close()
        {
            if (m_pipe != null)
            {
                string error = m_pipe.CloseAndGetOutput();

                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning(
                        "FFmpeg returned with warning/error messages. " +
                        "See the following lines for details:\n" + error
                    );

                m_pipe.Dispose();
                m_pipe = null;
            }

            if (m_blitMaterial != null)
            {
                UnityEngine.Object.Destroy(m_blitMaterial);
                m_blitMaterial = null;
            }
        }

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region Private objects and constructor/destructor

        private FFmpegPipe m_pipe;
        private Material m_blitMaterial;

        protected FFmpegSession(string arguments)
        {
            if (!FFmpegPipe.IsAvailable)
                Debug.LogWarning(
                    "Failed to initialize an FFmpeg session due to missing " +
                    "executable file. Please check FFmpeg installation."
                );
            else if (!UnityEngine.SystemInfo.supportsAsyncGPUReadback)
                Debug.LogWarning(
                    "Failed to initialize an FFmpeg session due to lack of " +
                    "async GPU readback support. Please try changing " +
                    "graphics API to readback-enabled one."
                );
            else
            {
                m_pipe = new FFmpegPipe(arguments);
                Debug.Log("Initialized Pipe with no errors");
            }
        }

        ~FFmpegSession()
        {
            if (m_pipe != null)
                Debug.LogError(
                    "An unfinalized FFmpegCapture object was detected. " +
                    "It should be explicitly closed or disposed " +
                    "before being garbage-collected."
                );
        }

        #endregion

        #region Frame readback queue

        private List<AsyncGPUReadbackRequest> m_readbackQueue =
            new List<AsyncGPUReadbackRequest>(4);

        private void QueueFrame(Texture source)
        {
            if (m_readbackQueue.Count > 6)
            {
                Debug.LogWarning("Too many GPU readback requests.");
                return;
            }

            // Lazy initialization of the preprocessing blit shader
            if (m_blitMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/FFmpegOut/Preprocess");
                m_blitMaterial = new Material(shader);
            }

            // Blit to a temporary texture and request readback on it.
            RenderTexture rt = RenderTexture.GetTemporary
                (source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt, m_blitMaterial, 0);
            m_readbackQueue.Add(AsyncGPUReadback.Request(rt));
            RenderTexture.ReleaseTemporary(rt);
        }

        private void ProcessQueue()
        {
            while (m_readbackQueue.Count > 0)
            {
                // Check if the first entry in the queue is completed.
                if (!m_readbackQueue[0].done)
                {
                    // Detect out-of-order case (the second entry in the queue
                    // is completed before the first entry).
                    if (m_readbackQueue.Count > 1 && m_readbackQueue[1].done)
                    {
                        // We can't allow the out-of-order case, so force it to
                        // be completed now.
                        m_readbackQueue[0].WaitForCompletion();
                    }
                    else
                    {
                        // Nothing to do with the queue.
                        break;
                    }
                }

                // Retrieve the first entry in the queue.
                AsyncGPUReadbackRequest req = m_readbackQueue[0];
                m_readbackQueue.RemoveAt(0);

                // Error detection
                if (req.hasError)
                {
                    Debug.LogWarning("GPU readback error was detected.");
                    continue;
                }

                // Feed the frame to the FFmpeg pipe.
                m_pipe.PushFrameData(req.GetData<byte>());
            }
        }

        #endregion
    }
}
