// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FFmpegOut
{
    internal sealed class FFmpegPipe : System.IDisposable
    {
        #region Public methods

        public static bool IsAvailable {
            get { return System.IO.File.Exists(ExecutablePath); }
        }

        public FFmpegPipe(string arguments)
        {
            // Start FFmpeg subprocess.
            m_subprocess = Process.Start(new ProcessStartInfo
            {
                FileName = ExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            m_subprocess.EnableRaisingEvents = true;
            m_subprocess.Exited += (_, _) => Debug.Log($"FFmpeg process exited with code {m_subprocess.ExitCode}");

            m_subprocess.OutputDataReceived += (_, args) => Debug.Log("Received: " + args.Data);
            m_subprocess.ErrorDataReceived += (_, args) => Debug.LogError("Received Error: " + args.Data);

            // Start copy/pipe subthreads.

            m_copyThread = new Thread(CopyThread);
            m_pipeThread = new Thread(PipeThread);
            m_copyThread.Start();
            m_pipeThread.Start();
        }

        public void PushFrameData(NativeArray<byte> data)
        {
            // Update the copy queue and notify the copy thread with a ping.
            lock (m_copyQueue) m_copyQueue.Enqueue(data);
            m_copyPing.Set();
        }

        public void SyncFrameData()
        {
            // Wait for the copy queue to get emptied with using pong
            // notification signals sent from the copy thread.
            while (m_copyQueue.Count > 0) m_copyPong.WaitOne();

            // When using a slower codec (e.g. HEVC, ProRes), frames may be
            // queued too much, and it may end up with an out-of-memory error.
            // To avoid this problem, we wait for pipe queue entries to be
            // comsumed by the pipe thread.
            while (m_pipeQueue.Count > 4) m_pipePong.WaitOne();
        }

        public string CloseAndGetOutput()
        {
            // Terminate the subthreads.
            m_terminate = true;

            m_copyPing.Set();
            m_pipePing.Set();

            m_copyThread.Join();
            m_pipeThread.Join();

            // Close FFmpeg subprocess.
            m_subprocess.StandardInput.Close();
            m_subprocess.WaitForExit();

            StreamReader outputReader = m_subprocess.StandardError;
            string error = outputReader.ReadToEnd();

            m_subprocess.Close();
            m_subprocess.Dispose();

            outputReader.Close();
            outputReader.Dispose();

            // Nullify members (just for ease of debugging).
            m_subprocess = null;
            m_copyThread = null;
            m_pipeThread = null;
            m_copyQueue = null;
            m_pipeQueue = m_freeBuffer = null;

            return error;
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (!m_terminate) CloseAndGetOutput();
        }

        ~FFmpegPipe()
        {
            if (!m_terminate)
                UnityEngine.Debug.LogError(
                    "An unfinalized FFmpegPipe object was detected. " +
                    "It should be explicitly closed or disposed " +
                    "before being garbage-collected."
                );
        }

        #endregion

        #region Private members

        private Process m_subprocess;
        private Thread m_copyThread;
        private Thread m_pipeThread;

        private AutoResetEvent m_copyPing = new AutoResetEvent(false);
        private AutoResetEvent m_copyPong = new AutoResetEvent(false);
        private AutoResetEvent m_pipePing = new AutoResetEvent(false);
        private AutoResetEvent m_pipePong = new AutoResetEvent(false);
        private bool m_terminate;

        private Queue<NativeArray<byte>> m_copyQueue = new Queue<NativeArray<byte>>();
        private Queue<byte[]> m_pipeQueue = new Queue<byte[]>();
        private Queue<byte[]> m_freeBuffer = new Queue<byte[]>();

        public static string ExecutablePath
        {
            get {
                string basePath = UnityEngine.Application.streamingAssetsPath;
                RuntimePlatform platform = UnityEngine.Application.platform;

                if (platform == UnityEngine.RuntimePlatform.OSXPlayer ||
                    platform == UnityEngine.RuntimePlatform.OSXEditor)
                    return basePath + "/FFmpegOut/macOS/ffmpeg";

                if (platform == UnityEngine.RuntimePlatform.LinuxPlayer ||
                    platform == UnityEngine.RuntimePlatform.LinuxEditor)
                    return basePath + "/FFmpegOut/Linux/ffmpeg";

                return basePath + "/FFmpegOut/Windows/ffmpeg.exe";
            }
        }

        #endregion

        #region Subthread entry points

        // CopyThread - Copies frames given from the readback queue to the pipe
        // queue. This is required because readback buffers are not under our
        // control -- they'll be disposed before being processed by us. They
        // have to be buffered by end-of-frame.
        private void CopyThread()
        {
            while (!m_terminate)
            {
                // Wait for ping from the main thread.
                m_copyPing.WaitOne();

                // Process all entries in the copy queue.
                while (m_copyQueue.Count > 0)
                {
                    // Retrieve an copy queue entry without dequeuing it.
                    // (We don't want to notify the main thread at this point.)
                    NativeArray<byte> source;
                    lock (m_copyQueue) source = m_copyQueue.Peek();

                    // Try allocating a buffer from the free buffer list.
                    byte[] buffer = null;
                    if (m_freeBuffer.Count > 0)
                        lock (m_freeBuffer) buffer = m_freeBuffer.Dequeue();

                    // Copy the contents of the copy queue entry.
                    if (buffer == null || buffer.Length != source.Length)
                        buffer = source.ToArray();
                    else
                        source.CopyTo(buffer);

                    // Push the buffer entry to the pipe queue.
                    lock (m_pipeQueue) m_pipeQueue.Enqueue(buffer);
                    m_pipePing.Set(); // Ping the pipe thread.

                    // Dequeue the copy buffer entry and ping the main thread.
                    lock (m_copyQueue) m_copyQueue.Dequeue();
                    m_copyPong.Set();
                }
            }
        }

        // PipeThread - Receives frame entries from the copy thread and push
        // them into the FFmpeg pipe.
        private void PipeThread()
        {
            Stream pipe = m_subprocess.StandardInput.BaseStream;

            while (!m_terminate)
            {
                // Wait for the ping from the copy thread.
                m_pipePing.WaitOne();

                // Process all entries in the pipe queue.
                while (m_pipeQueue.Count > 0)
                {
                    // Retrieve a frame entry.
                    byte[] buffer;
                    lock (m_pipeQueue) buffer = m_pipeQueue.Dequeue();

                    // Write it into the FFmpeg pipe.
                    try
                    {
                        #if UNITY_EDITOR
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        #endif

                        pipe.Write(buffer, 0, buffer.Length);
                        pipe.Flush();

                        #if UNITY_EDITOR
                        stopwatch.Stop();
                        Debug.Log($"Pipe write took {stopwatch.Elapsed.TotalMilliseconds} ms");
                        #endif
                    }
                    catch
                    {
                        // Pipe.Write could raise an IO exception when ffmpeg
                        // is terminated for some reason. We just ignore this
                        // situation and assume that it will be resolved in the
                        // main thread. #badcode
                    }

                    // Add the buffer to the free buffer list to reuse later.
                    lock (m_freeBuffer) m_freeBuffer.Enqueue(buffer);
                    m_pipePong.Set();
                }
            }
        }

        #endregion
    }
}
